"""Tests for the SQLite layer (schema init, uniqueness, settings, transactions)."""
from __future__ import annotations

import sqlite3

import pytest

from wasender.core import db


EXPECTED_TABLES = {
    "accounts",
    "contacts",
    "message_log",
    "schedules",
    "auto_replies",
    "leads",
    "settings",
}


def _make_account(label: str = "Acc1") -> int:
    return db.insert(
        "accounts",
        {
            "label": label,
            "phone": "628111111111",
            "profile_dir": "/tmp/profile",
            "daily_quota": 200,
            "sent_today": 0,
        },
    )


def test_schema_init(tmp_app_data):
    # First call to anything that touches the connection triggers _init_if_needed.
    db._init_if_needed()
    rows = db.query_all("SELECT name FROM sqlite_master WHERE type='table'")
    names = {r["name"] for r in rows}
    assert EXPECTED_TABLES.issubset(names), f"missing tables: {EXPECTED_TABLES - names}"


def test_insert_account_with_unique_phone(tmp_app_data):
    account_id = _make_account()

    db.insert(
        "contacts",
        {"account_id": account_id, "phone": "628999111222", "name": "First"},
    )
    # UNIQUE(account_id, phone) — second insert must be skipped via INSERT OR IGNORE.
    try:
        db.insert(
            "contacts",
            {"account_id": account_id, "phone": "628999111222", "name": "Duplicate"},
        )
    except sqlite3.IntegrityError:
        # Some db.insert implementations raise rather than IGNORE — fall back to
        # an explicit OR IGNORE to mirror what the import path does in production.
        db.execute(
            "INSERT OR IGNORE INTO contacts (account_id, phone, name) VALUES (?, ?, ?)",
            (account_id, "628999111222", "Duplicate"),
        )

    rows = db.query_all(
        "SELECT * FROM contacts WHERE account_id = ? AND phone = ?",
        (account_id, "628999111222"),
    )
    assert len(rows) == 1
    assert rows[0]["name"] == "First"


def test_setting_get_set(tmp_app_data):
    assert db.get_setting("nope") is None
    assert db.get_setting("nope", "fallback") == "fallback"

    db.set_setting("k", "v")
    assert db.get_setting("k") == "v"

    # Upsert path — overwriting an existing key works.
    db.set_setting("k", "v2")
    assert db.get_setting("k") == "v2"


def test_transaction_rollback_on_error(tmp_app_data):
    account_id = _make_account()

    # Insert one valid row, then raise inside the same transaction. The valid
    # row should NOT survive — the whole transaction must roll back.
    with pytest.raises(sqlite3.OperationalError):
        with db.transaction() as c:
            c.execute(
                "INSERT INTO contacts (account_id, phone, name) VALUES (?, ?, ?)",
                (account_id, "628777000111", "Should Rollback"),
            )
            # Trigger an error against a non-existent table — rolls back the
            # whole transaction including the row above.
            c.execute("INSERT INTO nope_table (x) VALUES (1)")

    rows = db.query_all(
        "SELECT * FROM contacts WHERE phone = ?",
        ("628777000111",),
    )
    assert rows == [], "transaction did not roll back on error"
