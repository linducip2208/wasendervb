"""Anti-ban throttling — random delay + daily cap per account."""
from __future__ import annotations

import random
import time
from datetime import date

from . import db, config
from .logger import logger


def random_delay() -> float:
    cfg = config.load()
    delay = random.uniform(cfg.delay_min_seconds, cfg.delay_max_seconds)
    time.sleep(delay)
    return delay


def reset_daily_counters_if_needed(account_id: int) -> None:
    row = db.query_one("SELECT last_reset, sent_today FROM accounts WHERE id = ?", (account_id,))
    if not row:
        return
    today = date.today().isoformat()
    if row["last_reset"] != today:
        db.update("accounts", account_id, {"sent_today": 0, "last_reset": today})


def check_quota(account_id: int) -> tuple[bool, int, int]:
    """Return (allowed, sent_today, quota)."""
    reset_daily_counters_if_needed(account_id)
    row = db.query_one(
        "SELECT sent_today, daily_quota FROM accounts WHERE id = ?", (account_id,)
    )
    if not row:
        return False, 0, 0
    return row["sent_today"] < row["daily_quota"], row["sent_today"], row["daily_quota"]


def increment_sent(account_id: int) -> None:
    db.execute(
        "UPDATE accounts SET sent_today = sent_today + 1 WHERE id = ?", (account_id,)
    )
    db._conn().commit()


def acquire_send_slot(account_id: int) -> None:
    """Block if quota exceeded. Apply random delay."""
    allowed, sent, quota = check_quota(account_id)
    if not allowed:
        raise RuntimeError(
            f"Daily quota exceeded for account {account_id} ({sent}/{quota}). "
            "Tunggu sampai besok atau naikkan quota di Settings."
        )
    delay = random_delay()
    logger.debug(f"Sent slot acquired (delay={delay:.1f}s, account={account_id})")
