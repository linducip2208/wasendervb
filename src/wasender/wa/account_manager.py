"""Manage multiple WhatsApp accounts — each in its own pywebview window."""
from __future__ import annotations

import threading

from ..core import db
from ..core.logger import logger
from ..core.paths import wa_sessions_dir
from .session import WhatsAppSession

_sessions: dict[int, WhatsAppSession] = {}
_lock = threading.Lock()
_started_sessions: set[int] = set()


def list_accounts() -> list[dict]:
    return db.query_all("SELECT * FROM accounts ORDER BY id")


def add_account(label: str, daily_quota: int = 200) -> int:
    # Insert dulu supaya dapat id, lalu set profile_dir unik per akun.
    # (Sebelumnya selalu "acc_placeholder" sehingga semua akun berbagi folder
    #  yang sama, sedangkan session.py memakai "acc_{account_id}".)
    account_id = db.insert("accounts", {
        "label": label,
        "profile_dir": "",
        "daily_quota": daily_quota,
    })
    profile_dir = str(wa_sessions_dir() / f"acc_{account_id}")
    db.update("accounts", account_id, {"profile_dir": profile_dir})
    return account_id


def remove_account(account_id: int) -> None:
    close_session(account_id)
    db.delete("accounts", account_id)


def open_session(account_id: int, hidden: bool = False) -> WhatsAppSession | None:
    with _lock:
        existing = _sessions.get(account_id)
        if existing and existing.window is not None:
            logger.info(f"Reusing existing session for account {account_id}")
            return existing
        row = db.query_one("SELECT * FROM accounts WHERE id = ?", (account_id,))
        if not row:
            raise ValueError(f"Account {account_id} not found")
        try:
            sess = WhatsAppSession(account_id, row["label"])
            sess.open_window(hidden=hidden)
            _sessions[account_id] = sess
            logger.info(f"Opened WA session for account {account_id} ({row['label']})")
            return sess
        except Exception as e:
            logger.error(f"Failed to open session for account {account_id}: {e}")
            return None


def get_session(account_id: int) -> WhatsAppSession | None:
    return _sessions.get(account_id)


def close_session(account_id: int) -> None:
    with _lock:
        sess = _sessions.pop(account_id, None)
    if sess:
        try:
            sess.close()
        except Exception as e:
            logger.warning(f"Error closing session {account_id}: {e}")


def close_all() -> None:
    ids = list(_sessions.keys())
    for i in ids:
        close_session(i)
