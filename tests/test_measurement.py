"""Tests for measurement backend: inbox, campaigns, heatmap, health, alerts, export."""
from __future__ import annotations

from datetime import date

from wasender.api.bridge import Api
from wasender.core import db
from wasender.features.automations import responder_bot


def _acc(label="A", quota=200, sent=0):
    return db.insert("accounts", {
        "label": label, "profile_dir": "/tmp/p", "daily_quota": quota,
        "sent_today": sent, "last_reset": date.today().isoformat(),
    })


def test_migration_columns(tmp_app_data):
    cols = {r["name"] for r in db._conn().execute("PRAGMA table_info(message_log)")}
    assert {"kind", "direction", "campaign"} <= cols


def test_inbox_logged_and_groups_skipped(tmp_app_data):
    acc = _acc()
    responder_bot.on_message({"accountId": acc, "msg": {"body": "halo", "from": "628111111111@c.us"}})
    responder_bot.on_message({"accountId": acc, "msg": {"body": "grup", "from": "12345@g.us"}})
    responder_bot.on_message({"accountId": acc, "msg": {"body": "", "from": "628111111111@c.us"}})
    rows = db.query_all("SELECT * FROM message_log WHERE direction = 'in'")
    assert len(rows) == 1
    assert rows[0]["phone"] == "628111111111"
    assert rows[0]["status"] == "received"


def test_campaigns_reply_rate(tmp_app_data):
    api = Api()
    acc = _acc()
    for phone in ("6281", "6282", "6283"):
        db.insert("message_log", {"account_id": acc, "phone": phone, "status": "sent",
                                  "kind": "bulk", "direction": "out", "campaign": "Promo"})
    db.insert("message_log", {"account_id": acc, "phone": "6281", "status": "received",
                              "kind": "inbox", "direction": "in"})
    db.insert("message_log", {"account_id": acc, "phone": "6289", "status": "received",
                              "kind": "inbox", "direction": "in"})  # no outgoing → not counted
    camps = api.campaigns()["data"]
    assert len(camps) == 1
    c = camps[0]
    assert c["campaign"] == "Promo" and c["sent"] == 3 and c["phones"] == 3
    assert c["replies"] == 1
    assert c["reply_rate"] == round(1 / 3, 3)


def test_heatmap_health_alerts_export(tmp_app_data):
    api = Api()
    acc = _acc("Risky", quota=10, sent=9)
    for _ in range(6):
        db.insert("message_log", {"account_id": acc, "phone": "6281", "status": "sent",
                                  "kind": "bulk", "direction": "out"})
    for _ in range(4):
        db.insert("message_log", {"account_id": acc, "phone": "6281", "status": "failed",
                                  "error": "boom", "kind": "bulk", "direction": "out"})
    heat = api.send_heatmap(7)["data"]
    assert isinstance(heat, list) and len(heat) >= 1
    assert all(set(h) == {"dow", "hour", "sent", "failed"} for h in heat)
    assert 0 <= heat[0]["dow"] <= 6 and 0 <= heat[0]["hour"] <= 23

    health = api.account_health()["data"]
    assert len(health) == 1
    h = health[0]
    assert h["fail_rate_7d"] == 0.4 and h["quota_usage"] == 0.9
    assert h["score"] < 80 and h["status"] in ("warn", "bad")

    alerts = api.alerts()["data"]
    texts = " ".join(a["text"] for a in alerts)
    assert "Kuota" in texts and "Gagal kirim" in texts

    out = api.export_daily_report(7)["data"]
    assert out and out.endswith(".csv")
    import os
    assert os.path.exists(out)
