"""App configuration & defaults."""
from __future__ import annotations

from dataclasses import dataclass

from . import db


@dataclass
class Config:
    delay_min_seconds: int = 3
    delay_max_seconds: int = 15
    daily_quota_per_account: int = 200
    typing_simulation: bool = True
    warmup_required_for_new: bool = True
    warmup_days: int = 7
    warmup_messages_per_day: int = 20
    auto_responder_check_interval_seconds: int = 5
    selectors_remote_url: str | None = None  # opsional, kalau mau remote-update selectors


_DEFAULTS = {
    "delay_min_seconds": "3",
    "delay_max_seconds": "15",
    "daily_quota_per_account": "200",
    "typing_simulation": "true",
    "warmup_required_for_new": "true",
    "warmup_days": "7",
    "warmup_messages_per_day": "20",
}


def load() -> Config:
    cfg = Config()
    for k, default in _DEFAULTS.items():
        raw = db.get_setting(k, default) or default
        current = getattr(cfg, k)
        if isinstance(current, bool):
            setattr(cfg, k, raw.lower() in ("true", "1", "yes", "on"))
        elif isinstance(current, int):
            try:
                setattr(cfg, k, int(raw))
            except ValueError:
                pass
        else:
            setattr(cfg, k, raw)
    # Clamp ke rentang yang masuk akal — setting corrupt jangan bikin crash.
    cfg.delay_min_seconds = max(0, cfg.delay_min_seconds)
    cfg.delay_max_seconds = max(0, cfg.delay_max_seconds)
    if cfg.delay_min_seconds > cfg.delay_max_seconds:
        cfg.delay_min_seconds, cfg.delay_max_seconds = cfg.delay_max_seconds, cfg.delay_min_seconds
    cfg.daily_quota_per_account = max(1, cfg.daily_quota_per_account)
    cfg.warmup_days = max(1, cfg.warmup_days)
    cfg.warmup_messages_per_day = max(1, cfg.warmup_messages_per_day)
    cfg.auto_responder_check_interval_seconds = max(
        1, cfg.auto_responder_check_interval_seconds
    )
    return cfg


def save(cfg: Config) -> None:
    for k in _DEFAULTS:
        v = getattr(cfg, k)
        if isinstance(v, bool):
            db.set_setting(k, "true" if v else "false")
        else:
            db.set_setting(k, str(v))
