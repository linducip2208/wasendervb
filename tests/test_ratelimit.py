"""Tests for the rate-limit / daily quota module.

Timing-based ``random_delay`` behaviour is intentionally not covered here —
``time.sleep`` makes the test slow and flaky on CI runners.
"""
from __future__ import annotations

from datetime import date, timedelta

from wasender.core import db, ratelimit


def _make_account(quota: int = 200, sent: int = 0, last_reset: str | None = None) -> int:
    return db.insert(
        "accounts",
        {
            "label": "Acc",
            "phone": "628111111111",
            "profile_dir": "/tmp/profile",
            "daily_quota": quota,
            "sent_today": sent,
            "last_reset": last_reset or date.today().isoformat(),
        },
    )


def test_quota_blocks_when_exceeded(tmp_app_data):
    account_id = _make_account(quota=2, sent=2)
    allowed, sent, quota = ratelimit.check_quota(account_id)
    assert allowed is False
    assert sent == 2
    assert quota == 2


def test_daily_reset(tmp_app_data):
    yesterday = (date.today() - timedelta(days=1)).isoformat()
    account_id = _make_account(quota=200, sent=10, last_reset=yesterday)

    ratelimit.reset_daily_counters_if_needed(account_id)

    row = db.query_one(
        "SELECT sent_today, last_reset FROM accounts WHERE id = ?", (account_id,)
    )
    assert row is not None
    assert row["sent_today"] == 0
    assert row["last_reset"] == date.today().isoformat()


def test_increment_sent(tmp_app_data):
    account_id = _make_account(quota=200, sent=0)
    for _ in range(3):
        ratelimit.increment_sent(account_id)
    row = db.query_one("SELECT sent_today FROM accounts WHERE id = ?", (account_id,))
    assert row is not None
    assert row["sent_today"] == 3
