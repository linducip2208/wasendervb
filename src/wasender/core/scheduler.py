"""APScheduler wrapper — one global scheduler, persistent jobs from DB."""
from __future__ import annotations

import json
import threading
from datetime import datetime
from typing import Callable

from apscheduler.schedulers.background import BackgroundScheduler
from apscheduler.triggers.cron import CronTrigger
from apscheduler.triggers.date import DateTrigger

from . import db
from .logger import logger

_scheduler: BackgroundScheduler | None = None
_lock = threading.Lock()
_handlers: dict[str, Callable[[dict], None]] = {}


def get_scheduler() -> BackgroundScheduler:
    global _scheduler
    with _lock:
        if _scheduler is None:
            _scheduler = BackgroundScheduler(timezone="UTC")
            _scheduler.start()
            logger.info("APScheduler started")
            _load_persistent_jobs()
        return _scheduler


def register_feature_handler(feature: str, handler: Callable[[dict], None]) -> None:
    _handlers[feature] = handler


def _execute_job(schedule_id: int) -> None:
    row = db.query_one("SELECT * FROM schedules WHERE id = ?", (schedule_id,))
    if not row or not row["enabled"]:
        return
    feature = row["feature"]
    cfg = json.loads(row["config"])
    handler = _handlers.get(feature)
    if not handler:
        logger.warning(f"No handler registered for feature={feature}")
        return
    try:
        handler(cfg)
        db.update("schedules", schedule_id, {"last_run": datetime.utcnow().isoformat()})
    except Exception as e:
        logger.exception(f"Scheduled job {schedule_id} ({feature}) failed: {e}")


def add_schedule(name: str, feature: str, config: dict, *, cron: str | None = None, run_at: str | None = None, enabled: bool = True) -> int:
    sid = db.insert("schedules", {
        "name": name,
        "feature": feature,
        "config": json.dumps(config),
        "cron": cron,
        "run_at": run_at,
        "enabled": 1 if enabled else 0,
    })
    if enabled:
        _add_to_scheduler(sid, cron, run_at)
    return sid


def _add_to_scheduler(schedule_id: int, cron: str | None, run_at: str | None) -> None:
    sched = get_scheduler()
    if cron:
        try:
            trigger = CronTrigger.from_crontab(cron)
        except ValueError as e:
            logger.error(f"Invalid cron `{cron}`: {e}")
            return
    elif run_at:
        try:
            trigger = DateTrigger(run_date=datetime.fromisoformat(run_at))
        except ValueError as e:
            logger.error(f"Invalid run_at `{run_at}`: {e}")
            return
    else:
        return
    sched.add_job(_execute_job, trigger, args=[schedule_id], id=f"sched-{schedule_id}", replace_existing=True)


def remove_schedule(schedule_id: int) -> None:
    sched = get_scheduler()
    try:
        sched.remove_job(f"sched-{schedule_id}")
    except Exception:
        pass
    db.delete("schedules", schedule_id)


def toggle_schedule(schedule_id: int, enabled: bool) -> None:
    db.update("schedules", schedule_id, {"enabled": 1 if enabled else 0})
    if enabled:
        row = db.query_one("SELECT cron, run_at FROM schedules WHERE id = ?", (schedule_id,))
        if row:
            _add_to_scheduler(schedule_id, row["cron"], row["run_at"])
    else:
        try:
            get_scheduler().remove_job(f"sched-{schedule_id}")
        except Exception:
            pass


def list_schedules() -> list[dict]:
    return db.query_all("SELECT * FROM schedules ORDER BY id DESC")


def _load_persistent_jobs() -> None:
    rows = db.query_all("SELECT * FROM schedules WHERE enabled = 1")
    for r in rows:
        _add_to_scheduler(r["id"], r["cron"], r["run_at"])
    logger.info(f"Loaded {len(rows)} scheduled jobs")
