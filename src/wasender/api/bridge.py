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
