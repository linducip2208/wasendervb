"""Cross-context path resolution. Works in both dev mode and PyInstaller frozen mode."""
from __future__ import annotations

import os
import sys
from pathlib import Path


def is_frozen() -> bool:
    return getattr(sys, "frozen", False)


def resource_dir() -> Path:
    """Path ke bundled resources (UI, JS, public_key.pem). Read-only."""
    if is_frozen():
        return Path(sys._MEIPASS)  # type: ignore[attr-defined]
    return Path(__file__).resolve().parent.parent


def app_data_dir() -> Path:
    """Path ke writable app data (DB, license, WA sessions). Per-user."""
    base = os.environ.get("APPDATA") or str(Path.home() / "AppData" / "Roaming")
    p = Path(base) / "WASender"
    p.mkdir(parents=True, exist_ok=True)
    return p


def db_path() -> Path:
    return app_data_dir() / "wasender.db"


def license_path() -> Path:
    return app_data_dir() / "license.lic"


def logs_dir() -> Path:
    p = app_data_dir() / "logs"
    p.mkdir(exist_ok=True)
    return p


def wa_sessions_dir() -> Path:
    p = app_data_dir() / "wa_sessions"
    p.mkdir(exist_ok=True)
    return p


def exports_dir() -> Path:
    p = app_data_dir() / "exports"
    p.mkdir(exist_ok=True)
    return p


def public_key_path() -> Path:
    return resource_dir() / "core" / "public_key.pem"


def ui_dir() -> Path:
    return resource_dir() / "ui"


def wapi_js_path() -> Path:
    return resource_dir() / "wa" / "wapi.js"


def selectors_path() -> Path:
    return resource_dir() / "wa" / "selectors.json"
