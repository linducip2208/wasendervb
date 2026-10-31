"""Tests for dashboard backend: kind migration + aggregate bridge methods."""
from __future__ import annotations

from datetime import date, timedelta

from wasender.api.bridge import Api
from wasender.core import db


def _acc(label="A", quota=200, sent=0):
    return db.insert("accounts", {
        "label": label, "profile_dir": "/tmp/p", "daily_quota": quota,
        "sent_today": sent, "last_reset": date.today().isoformat(),
    })


def test_kind_column_migrated(tmp_app_data):
    cols = {r["name"] for r in db._conn().execute("PRAGMA table_info(message_log)")}
    assert "kind" in cols
    acc = _acc()
    db.insert("message_log", {"account_id": acc, "phone": "1", "status": "sent"})
    row = db.query_one("SELECT kind FROM message_log LIMIT 1")
    assert row["kind"] == "bulk"


def test_send_chart_fills_gaps(tmp_app_data):
    api = Api()
    acc = _acc()
    today = date.today().isoformat()
    old = (date.today() - timedelta(days=2)).isoformat()
    db.insert("message_log", {"account_id": acc, "phone": "1", "status": "sent"})
    db.insert("message_log", {"account_id": acc, "phone": "2", "status": "failed", "error": "bad"})
    # backdate one row
    db._conn().execute("UPDATE message_log SET sent_at = ? WHERE phone = '2'", (old,))
    db._conn().commit()
    res = api.send_chart(3)
    assert res["ok"] is True
    days = res["data"]
    assert len(days) == 3
    assert days[-1]["day"] == today
    assert days[-1]["sent"] == 1 and days[-1]["failed"] == 0
    assert days[-2]["sent"] == 0 and days[-2]["failed"] == 0  # gap filled
    assert days[0]["day"] == old and days[0]["failed"] == 1


def test_leads_fail_warmer_accounts(tmp_app_data):
    api = Api()
    acc = _acc("WA1", quota=100, sent=25)
    db.insert("leads", {"source": "gmaps", "phone": "1"})
    db.insert("leads", {"source": "gmaps", "phone": "2"})
    db.insert("leads", {"source": "website", "phone": "3"})
    db.insert("message_log", {"account_id": acc, "phone": "9", "status": "failed", "error": "boom x"})
    db.insert("message_log", {"account_id": acc, "phone": "8", "status": "failed", "error": "boom x"})
    db.insert("message_log", {"account_id": acc, "phone": "7", "status": "sent", "kind": "warmer"})

    leads = api.leads_by_source()["data"]
    assert {r["source"]: r["count"] for r in leads} == {"gmaps": 2, "website": 1}

    fails = api.fail_reasons(5)["data"]
    assert fails[0]["count"] == 2 and "boom" in fails[0]["error"]

    ov = api.accounts_overview()["data"]
    assert len(ov) == 1
    assert ov[0]["sent_today"] == 25 and ov[0]["daily_quota"] == 100
    assert ov[0]["quota_pct"] == 25
    assert ov[0]["open"] is False

    wp = api.warmer_progress()["data"]
    assert len(wp) == 1
    assert wp[0]["sent_total"] == 1 and wp[0]["days_active"] == 1
    assert wp[0]["pct"] <= 100
