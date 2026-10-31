"""SQLite manager — single connection, schema migration."""
from __future__ import annotations

import json
import sqlite3
import threading
from contextlib import contextmanager
from typing import Any, Iterable

from .logger import logger
from .paths import db_path

_LOCAL = threading.local()
_INIT_LOCK = threading.Lock()
_INITIALIZED = False

SCHEMA_SQL = """
CREATE TABLE IF NOT EXISTS accounts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    label TEXT NOT NULL,
    phone TEXT,
    profile_dir TEXT NOT NULL,
    last_login_at TEXT,
    daily_quota INTEGER DEFAULT 200,
    sent_today INTEGER DEFAULT 0,
    last_reset TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS contacts (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id INTEGER REFERENCES accounts(id) ON DELETE CASCADE,
    phone TEXT NOT NULL,
    name TEXT,
    tags TEXT,
    extra TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(account_id, phone)
);

CREATE TABLE IF NOT EXISTS message_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id INTEGER REFERENCES accounts(id) ON DELETE CASCADE,
    phone TEXT NOT NULL,
    message TEXT,
    media_path TEXT,
    status TEXT NOT NULL,
    error TEXT,
    sent_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS schedules (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    feature TEXT NOT NULL,
    config TEXT NOT NULL,
    cron TEXT,
    run_at TEXT,
    enabled INTEGER DEFAULT 1,
    last_run TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS auto_replies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    account_id INTEGER REFERENCES accounts(id) ON DELETE CASCADE,
    pattern TEXT NOT NULL,
    is_regex INTEGER DEFAULT 0,
    response TEXT NOT NULL,
    enabled INTEGER DEFAULT 1
);

CREATE TABLE IF NOT EXISTS leads (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    source TEXT NOT NULL,
    name TEXT,
    phone TEXT,
    email TEXT,
    address TEXT,
    website TEXT,
    extra TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT
);

CREATE INDEX IF NOT EXISTS idx_contacts_account ON contacts(account_id);
CREATE INDEX IF NOT EXISTS idx_msg_log_account ON message_log(account_id, sent_at);
CREATE INDEX IF NOT EXISTS idx_leads_phone ON leads(phone);
"""


def _init_if_needed() -> None:
    global _INITIALIZED
    if _INITIALIZED:
        return
    with _INIT_LOCK:
        if _INITIALIZED:
            return
        conn = sqlite3.connect(db_path())
        conn.row_factory = sqlite3.Row
        conn.executescript(SCHEMA_SQL)
        conn.commit()
        conn.close()
        _INITIALIZED = True
        logger.debug("DB schema initialized at %s", db_path())


def _conn() -> sqlite3.Connection:
    _init_if_needed()
    if not hasattr(_LOCAL, "conn"):
        c = sqlite3.connect(db_path(), check_same_thread=False)
        c.row_factory = sqlite3.Row
        c.execute("PRAGMA foreign_keys = ON")
        c.execute("PRAGMA journal_mode = WAL")
        _LOCAL.conn = c
    return _LOCAL.conn


@contextmanager
def transaction():
    c = _conn()
    try:
        yield c
        c.commit()
    except Exception:
        c.rollback()
        raise


def execute(sql: str, params: Iterable[Any] = ()) -> sqlite3.Cursor:
    return _conn().execute(sql, tuple(params))


def query_all(sql: str, params: Iterable[Any] = ()) -> list[dict]:
    cur = execute(sql, params)
    return [dict(row) for row in cur.fetchall()]


def query_one(sql: str, params: Iterable[Any] = ()) -> dict | None:
    cur = execute(sql, params)
    row = cur.fetchone()
    return dict(row) if row else None


_MUTABLE_TABLES = frozenset({
    "accounts", "contacts", "message_log", "schedules",
    "auto_replies", "leads", "settings",
})


def _check_table(table: str) -> None:
    if table not in _MUTABLE_TABLES:
        raise ValueError(f"Unknown table: {table!r}")


def insert(table: str, data: dict) -> int:
    _check_table(table)
    cols = ", ".join(data.keys())
    placeholders = ", ".join("?" * len(data))
    values = []
    for v in data.values():
        if isinstance(v, (dict, list)):
            values.append(json.dumps(v))
        else:
            values.append(v)
    with transaction() as c:
        cur = c.execute(f"INSERT INTO {table} ({cols}) VALUES ({placeholders})", values)
        return cur.lastrowid or 0


def update(table: str, row_id: int, data: dict) -> None:
    _check_table(table)
    if not data:
        return
    sets = ", ".join(f"{k} = ?" for k in data)
    values = []
    for v in data.values():
        if isinstance(v, (dict, list)):
            values.append(json.dumps(v))
        else:
            values.append(v)
    values.append(row_id)
    with transaction() as c:
        c.execute(f"UPDATE {table} SET {sets} WHERE id = ?", values)


def delete(table: str, row_id: int) -> None:
    _check_table(table)
    with transaction() as c:
        c.execute(f"DELETE FROM {table} WHERE id = ?", (row_id,))


def get_setting(key: str, default: str | None = None) -> str | None:
    row = query_one("SELECT value FROM settings WHERE key = ?", (key,))
    return row["value"] if row else default


def set_setting(key: str, value: str) -> None:
    with transaction() as c:
        c.execute(
            "INSERT INTO settings (key, value) VALUES (?, ?) "
            "ON CONFLICT(key) DO UPDATE SET value = excluded.value",
            (key, value),
        )
