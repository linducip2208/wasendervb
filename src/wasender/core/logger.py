"""Structured logging — file rotation + console."""
from __future__ import annotations

import sys
from loguru import logger

from .paths import logs_dir

_INITIALIZED = False


def setup_logging() -> None:
    global _INITIALIZED
    if _INITIALIZED:
        return
    logger.remove()
    logger.add(
        sys.stderr,
        level="INFO",
        format="<green>{time:HH:mm:ss}</green> | <level>{level: <7}</level> | <cyan>{name}</cyan> - {message}",
    )
    logger.add(
        logs_dir() / "wasender.log",
        rotation="10 MB",
        retention="30 days",
        compression="zip",
        level="DEBUG",
        format="{time:YYYY-MM-DD HH:mm:ss} | {level: <7} | {name}:{function}:{line} | {message}",
    )
    _INITIALIZED = True


__all__ = ["logger", "setup_logging"]
