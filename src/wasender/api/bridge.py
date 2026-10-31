"""Python — JavaScript bridge — methods exposed to the UI via pywebview's js_api."""
from __future__ import annotations

import json
import threading
from typing import Any

from ..core import config, db, license as licmod, scheduler
from ..core.hwid import get_hwid, get_hwid_short
from ..core.logger import logger
from ..wa import account_manager
from .. import features


def _wrap(fn):
    def inner(*args, **kwargs):
        try:
            result = fn(*args, **kwargs)
            return {"ok": True, "data": result}
        except Exception as e:
            logger.exception(f"API {fn.__name__} failed: {e}")
            return {"ok": False, "error": str(e), "type": type(e).__name__}
    inner.__name__ = fn.__name__
    return inner


class Api:

    @_wrap
    def get_hwid(self) -> dict:
        return {"hwid": get_hwid(), "short": get_hwid_short()}

    @_wrap
    def license_status(self) -> dict | None:
        lic = licmod.load_active_license()
        if not lic:
            return None
        return {
            "license_id": lic.license_id,
            "customer": lic.customer,
            "tier": lic.tier,
            "features": lic.features,
            "expires_at": lic.expires_at,
            "days_remaining": lic.days_remaining(),
        }

    @_wrap
    def install_license(self, text: str) -> dict:
        lic = licmod.install_license(text)
        return {"license_id": lic.license_id, "tier": lic.tier}

    @_wrap
    def uninstall_license(self) -> bool:
        licmod.uninstall_license()
        return True

    @_wrap
    def get_config(self) -> dict:
        cfg = config.load()
        out = {}
        for k in cfg.__dataclass_fields__:
            out[k] = getattr(cfg, k)
        return out

    @_wrap
    def save_config(self, payload: dict) -> bool:
        cfg = config.load()
        for k, v in payload.items():
            if hasattr(cfg, k):
                try:
                    if isinstance(getattr(cfg, k), bool):
                        setattr(cfg, k, bool(v))
                    elif isinstance(getattr(cfg, k), int):
                        setattr(cfg, k, int(v))
                    else:
                        setattr(cfg, k, v)
                except (ValueError, TypeError):
                    pass
        config.save(cfg)
        return True

    @_wrap
    def list_accounts(self) -> list[dict]:
        return account_manager.list_accounts()

    @_wrap
    def add_account(self, label: str, daily_quota: int = 200) -> int:
        return account_manager.add_account(label, daily_quota)

    @_wrap
    def remove_account(self, account_id: int) -> bool:
        account_manager.remove_account(int(account_id))
        return True

    @_wrap
    def open_account(self, account_id: int) -> bool:
        sess = account_manager.open_session(int(account_id), hidden=False)
        return sess is not None

    @_wrap
    def close_account(self, account_id: int) -> bool:
        account_manager.close_session(int(account_id))
        return True

    @_wrap
    def account_status(self, account_id: int) -> dict:
        sess = account_manager.get_session(int(account_id))
        return {
            "open": sess is not None,
            "wapi_ready": bool(sess and sess._wapi_loaded),
        }

    @_wrap
    def list_contacts(self, account_id: int | None = None) -> list[dict]:
        if account_id:
            return db.query_all(
                "SELECT * FROM contacts WHERE account_id = ? ORDER BY id DESC", (int(account_id),)
            )
        return db.query_all("SELECT * FROM contacts ORDER BY id DESC LIMIT 1000")

    @_wrap
    def import_contacts(self, account_id: int, rows: list[dict]) -> int:
        count = 0
        with db.transaction() as c:
            for r in rows:
                phone = str(r.get("phone", "")).strip()
                if not phone:
                    continue
                try:
                    c.execute(
                        "INSERT OR IGNORE INTO contacts (account_id, phone, name, tags, extra) "
                        "VALUES (?, ?, ?, ?, ?)",
                        (
                            int(account_id),
                            phone,
                            str(r.get("name", "")),
                            ",".join(r.get("tags", [])) if isinstance(r.get("tags"), list) else str(r.get("tags", "")),
                            json.dumps(r.get("extra", {})),
                        ),
                    )
                    count += 1
                except Exception as e:
                    logger.warning(f"Failed to import contact {phone}: {e}")
        return count

    @_wrap
    def list_schedules(self) -> list[dict]:
        return scheduler.list_schedules()

    @_wrap
    def add_schedule(self, name: str, feature: str, config: dict, cron: str | None = None, run_at: str | None = None) -> int:
        return scheduler.add_schedule(name, feature, config, cron=cron, run_at=run_at)

    @_wrap
    def remove_schedule(self, schedule_id: int) -> bool:
        scheduler.remove_schedule(int(schedule_id))
        return True

    @_wrap
    def toggle_schedule(self, schedule_id: int, enabled: bool) -> bool:
        scheduler.toggle_schedule(int(schedule_id), enabled)
        return True

    @_wrap
    def message_log(self, account_id: int | None = None, limit: int = 200) -> list[dict]:
        if account_id:
            return db.query_all(
                "SELECT * FROM message_log WHERE account_id = ? ORDER BY id DESC LIMIT ?",
                (int(account_id), limit),
            )
        return db.query_all(
            "SELECT * FROM message_log ORDER BY id DESC LIMIT ?", (limit,)
        )

    @_wrap
    def stats(self) -> dict:
        total = db.query_one("SELECT COUNT(*) AS n FROM message_log")["n"]
        ok = db.query_one("SELECT COUNT(*) AS n FROM message_log WHERE status='sent'")["n"]
        fail = db.query_one("SELECT COUNT(*) AS n FROM message_log WHERE status='failed'")["n"]
        contacts = db.query_one("SELECT COUNT(*) AS n FROM contacts")["n"]
        leads = db.query_one("SELECT COUNT(*) AS n FROM leads")["n"]
        return {"messages_total": total, "sent": ok, "failed": fail, "contacts": contacts, "leads": leads}

    @_wrap
    def accounts_overview(self) -> list[dict]:
        from ..core.ratelimit import reset_daily_counters_if_needed
        out = []
        for acc in account_manager.list_accounts():
            try:
                reset_daily_counters_if_needed(int(acc["id"]))
            except Exception:
                pass
            row = db.query_one(
                "SELECT sent_today, daily_quota FROM accounts WHERE id = ?", (acc["id"],)
            ) or {}
            sess = account_manager.get_session(int(acc["id"]))
            quota = row.get("daily_quota") or 200
            sent = row.get("sent_today") or 0
            out.append({
                "id": acc["id"],
                "label": acc.get("label", ""),
                "open": sess is not None,
                "wapi_ready": bool(sess and sess._wapi_loaded),
                "sent_today": sent,
                "daily_quota": quota,
                "quota_pct": min(100, round(sent / quota * 100)) if quota else 0,
            })
        return out

    @_wrap
    def send_chart(self, days: int = 14) -> list[dict]:
        from datetime import date, timedelta
        days = max(1, min(int(days or 14), 90))
        since = (date.today() - timedelta(days=days - 1)).isoformat()
        rows = db.query_all(
            "SELECT date(sent_at, 'localtime') AS day, "
            "SUM(CASE WHEN status='sent' THEN 1 ELSE 0 END) AS sent, "
            "SUM(CASE WHEN status='failed' THEN 1 ELSE 0 END) AS failed "
            "FROM message_log WHERE date(sent_at, 'localtime') >= date(?) "
            "GROUP BY date(sent_at, 'localtime') ORDER BY day",
            (since,),
        )
        by_day = {r["day"]: r for r in rows}
        out = []
        for i in range(days):
            d = (date.today() - timedelta(days=days - 1 - i)).isoformat()
            r = by_day.get(d, {})
            out.append({"day": d, "sent": r.get("sent", 0), "failed": r.get("failed", 0)})
        return out

    @_wrap
    def leads_by_source(self) -> list[dict]:
        return db.query_all(
            "SELECT COALESCE(NULLIF(source, ''), 'unknown') AS source, COUNT(*) AS count "
            "FROM leads GROUP BY source ORDER BY count DESC"
        )

    @_wrap
    def fail_reasons(self, limit: int = 5) -> list[dict]:
        limit = max(1, min(int(limit or 5), 20))
        return db.query_all(
            "SELECT COALESCE(NULLIF(substr(error, 1, 80), ''), 'unknown error') AS error, "
            "COUNT(*) AS count FROM message_log WHERE status='failed' "
            "GROUP BY error ORDER BY count DESC LIMIT ?",
            (limit,),
        )

    @_wrap
    def send_heatmap(self, days: int = 30) -> list[dict]:
        """7x24 matrix of outgoing activity (local time). dow: 0=Minggu..6=Sabtu."""
        from datetime import date, timedelta
        days = max(1, min(int(days or 30), 90))
        since = (date.today() - timedelta(days=days - 1)).isoformat()
        rows = db.query_all(
            "SELECT CAST(strftime('%w', sent_at, 'localtime') AS INTEGER) AS dow, "
            "CAST(strftime('%H', sent_at, 'localtime') AS INTEGER) AS hour, "
            "SUM(CASE WHEN status='sent' THEN 1 ELSE 0 END) AS sent, "
            "SUM(CASE WHEN status='failed' THEN 1 ELSE 0 END) AS failed "
            "FROM message_log "
            "WHERE direction = 'out' AND date(sent_at, 'localtime') >= date(?) "
            "GROUP BY dow, hour ORDER BY dow, hour",
            (since,),
        )
        return [{"dow": r["dow"], "hour": r["hour"], "sent": r["sent"], "failed": r["failed"]} for r in rows]

    @_wrap
    def account_health(self) -> list[dict]:
        """Ban-risk score 0-100 per account.

        score = 100 - 60*fail_rate_7d - 20*quota_usage - 10*warmup_penalty
        """
        from ..core.ratelimit import reset_daily_counters_if_needed
        cfg = config.load()
        target = max(1, cfg.warmup_days * cfg.warmup_messages_per_day)
        out = []
        for acc in account_manager.list_accounts():
            aid = int(acc["id"])
            try:
                reset_daily_counters_if_needed(aid)
            except Exception:
                pass
            row = db.query_one(
                "SELECT sent_today, daily_quota FROM accounts WHERE id = ?", (aid,)
            ) or {}
            quota = row.get("daily_quota") or 200
            usage = min(1.0, (row.get("sent_today") or 0) / quota) if quota else 0.0
            week = db.query_one(
                "SELECT SUM(CASE WHEN status='sent' THEN 1 ELSE 0 END) AS s, "
                "SUM(CASE WHEN status='failed' THEN 1 ELSE 0 END) AS f "
                "FROM message_log WHERE account_id = ? AND direction = 'out' "
                "AND date(sent_at, 'localtime') >= date('now', 'localtime', '-7 days')",
                (aid,),
            ) or {}
            s, f = week.get("s", 0) or 0, week.get("f", 0) or 0
            fail_rate = (f / (s + f)) if (s + f) else 0.0
            w = db.query_one(
                "SELECT COUNT(*) AS n FROM message_log "
                "WHERE account_id = ? AND kind = 'warmer' AND status = 'sent'",
                (aid,),
            ) or {}
            bulk_total = db.query_one(
                "SELECT COUNT(*) AS n FROM message_log "
                "WHERE account_id = ? AND kind = 'bulk' AND status = 'sent'",
                (aid,),
            ) or {}
            warming_penalty = 1 if ((w.get("n", 0) < target) and (bulk_total.get("n", 0) > 0)) else 0
            score = max(0, min(100, round(100 - 60 * fail_rate - 20 * usage - 10 * warming_penalty)))
            status = "good" if score >= 80 else ("warn" if score >= 50 else "bad")
            out.append({
                "id": aid, "label": acc.get("label", ""), "score": score, "status": status,
                "fail_rate_7d": round(fail_rate, 3), "quota_usage": round(usage, 3),
                "warming_penalty": bool(warming_penalty),
            })
        return out

    @_wrap
    def alerts(self) -> list[dict]:
        """Proactive warnings for the dashboard banner."""
        items = []
        lic = licmod.load_active_license()
        if not lic:
            items.append({"level": "danger", "text": "Tidak ada license valid — aplikasi berjalan tanpa aktivasi."})
        elif lic.days_remaining() is not None and lic.days_remaining() <= 14:
            items.append({"level": "warn", "text": f"License {lic.license_id} tersisa {lic.days_remaining()} hari."})
        # (health dihitung per-akun di bawah; tidak dipakai ulang agar tetap read-only)
        for acc in account_manager.list_accounts():
            aid = int(acc["id"])
            row = db.query_one(
                "SELECT sent_today, daily_quota FROM accounts WHERE id = ?", (aid,)
            ) or {}
            quota = row.get("daily_quota") or 200
            sent = row.get("sent_today") or 0
            if quota and sent / quota >= 0.9:
                items.append({"level": "warn" if sent < quota else "danger",
                              "text": f"Kuota {acc.get('label', aid)}: {sent}/{quota} terpakai."})
            week = db.query_one(
                "SELECT SUM(CASE WHEN status='sent' THEN 1 ELSE 0 END) AS s, "
                "SUM(CASE WHEN status='failed' THEN 1 ELSE 0 END) AS f "
                "FROM message_log WHERE account_id = ? AND direction = 'out' "
                "AND date(sent_at, 'localtime') >= date('now', 'localtime', '-7 days')",
                (aid,),
            ) or {}
            s, f = week.get("s", 0) or 0, week.get("f", 0) or 0
            if (s + f) >= 5 and f / (s + f) > 0.3:
                items.append({"level": "warn",
                              "text": f"Gagal kirim tinggi di {acc.get('label', aid)}: {round(f/(s+f)*100)}% (7 hari)."})
        return items

    @_wrap
    def campaigns(self) -> list[dict]:
        """Per-campaign performance incl. reply-rate (replies matched by phone)."""
        rows = db.query_all(
            "SELECT COALESCE(NULLIF(campaign, ''), '(tanpa nama)') AS campaign, "
            "SUM(CASE WHEN status='sent' THEN 1 ELSE 0 END) AS sent, "
            "SUM(CASE WHEN status='failed' THEN 1 ELSE 0 END) AS failed, "
            "COUNT(DISTINCT CASE WHEN status='sent' THEN phone END) AS phones "
            "FROM message_log WHERE direction = 'out' "
            "GROUP BY COALESCE(NULLIF(campaign, ''), '(tanpa nama)') ORDER BY sent DESC"
        )
        out = []
        for r in rows:
            rep = db.query_one(
                "SELECT COUNT(DISTINCT m.phone) AS n FROM message_log m "
                "WHERE m.direction = 'in' AND EXISTS ("
                "SELECT 1 FROM message_log o WHERE o.direction = 'out' "
                "AND o.phone = m.phone "
                "AND COALESCE(NULLIF(o.campaign, ''), '(tanpa nama)') = ? "
                "AND o.sent_at <= m.sent_at)",
                (r["campaign"],),
            ) or {}
            replies = rep.get("n", 0) or 0
            phones = r["phones"] or 0
            out.append({
                "campaign": r["campaign"], "sent": r["sent"] or 0, "failed": r["failed"] or 0,
                "phones": phones, "replies": replies,
                "reply_rate": round(replies / phones, 3) if phones else 0.0,
            })
        return out

    @_wrap
    def export_daily_report(self, days: int = 30) -> str:
        from datetime import date, timedelta
        from ..features.base import export_csv
        days = max(1, min(int(days or 30), 90))
        since = (date.today() - timedelta(days=days - 1)).isoformat()
        rows = db.query_all(
            "SELECT date(sent_at, 'localtime') AS day, "
            "SUM(CASE WHEN direction='out' AND status='sent' THEN 1 ELSE 0 END) AS sent, "
            "SUM(CASE WHEN direction='out' AND status='failed' THEN 1 ELSE 0 END) AS failed, "
            "SUM(CASE WHEN direction='in' THEN 1 ELSE 0 END) AS received, "
            "COUNT(DISTINCT CASE WHEN direction='out' THEN account_id END) AS active_accounts "
            "FROM message_log WHERE date(sent_at, 'localtime') >= date(?) "
            "GROUP BY day ORDER BY day",
            (since,),
        )
        if not rows:
            return ""
        return export_csv(rows, "daily_report", ["day", "sent", "failed", "received", "active_accounts"])

    @_wrap
    def warmer_progress(self) -> list[dict]:
        cfg = config.load()
        target = max(1, cfg.warmup_days * cfg.warmup_messages_per_day)
        out = []
        for acc in account_manager.list_accounts():
            days = db.query_one(
                "SELECT COUNT(DISTINCT date(sent_at, 'localtime')) AS d FROM message_log "
                "WHERE account_id = ? AND kind = 'warmer' AND status = 'sent'",
                (acc["id"],),
            ) or {}
            total = db.query_one(
                "SELECT COUNT(*) AS n FROM message_log "
                "WHERE account_id = ? AND kind = 'warmer' AND status = 'sent'",
                (acc["id"],),
            ) or {}
            sent = total.get("n", 0)
            out.append({
                "id": acc["id"],
                "label": acc.get("label", ""),
                "days_active": days.get("d", 0),
                "warmup_days": cfg.warmup_days,
                "sent_total": sent,
                "target_total": target,
                "pct": min(100, round(sent / target * 100)),
            })
        return out

    @_wrap
    def feature_run(self, feature_key: str, params: dict) -> Any:
        return features.run(feature_key, params)

    @_wrap
    def feature_list(self) -> list[dict]:
        return features.catalog()

    def _on_wa_incoming(self, payload_json: str) -> None:
        try:
            payload = json.loads(payload_json)
            from ..features.automations import responder_bot
            threading.Thread(target=responder_bot.on_message, args=(payload,), daemon=True).start()
        except Exception as e:
            logger.exception(f"WA incoming dispatch error: {e}")
