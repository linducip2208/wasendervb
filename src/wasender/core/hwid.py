"""Hardware fingerprint — combine multiple stable identifiers into a SHA-256 hash."""
from __future__ import annotations

import hashlib
import platform
import subprocess
import uuid
from functools import lru_cache

from .logger import logger


def _wmic(query: str) -> str:
    """Run wmic; tolerate missing wmic on newer Win11 (deprecated)."""
    try:
        out = subprocess.check_output(
            ["wmic"] + query.split(),
            stderr=subprocess.DEVNULL,
            timeout=10,
            creationflags=0x08000000 if platform.system() == "Windows" else 0,
        )
        text = out.decode("utf-8", errors="ignore").strip().splitlines()
        return text[1].strip() if len(text) > 1 else ""
    except (FileNotFoundError, subprocess.SubprocessError, IndexError):
        return ""


def _powershell(cmd: str) -> str:
    try:
        out = subprocess.check_output(
            ["powershell", "-NoProfile", "-Command", cmd],
            stderr=subprocess.DEVNULL,
            timeout=10,
            creationflags=0x08000000 if platform.system() == "Windows" else 0,
        )
        return out.decode("utf-8", errors="ignore").strip()
    except (FileNotFoundError, subprocess.SubprocessError):
        return ""


def _bios_uuid() -> str:
    s = _wmic("csproduct get UUID")
    if not s:
        s = _powershell("(Get-CimInstance Win32_ComputerSystemProduct).UUID")
    return s.strip()


def _motherboard_serial() -> str:
    s = _wmic("baseboard get SerialNumber")
    if not s:
        s = _powershell("(Get-CimInstance Win32_BaseBoard).SerialNumber")
    return s.strip()


def _disk_serial() -> str:
    s = _wmic("diskdrive get SerialNumber")
    if not s:
        s = _powershell("(Get-CimInstance Win32_DiskDrive | Select-Object -First 1).SerialNumber")
    return s.strip()


def _mac() -> str:
    try:
        return f"{uuid.getnode():012x}"
    except Exception:
        return ""


@lru_cache(maxsize=1)
def get_hwid() -> str:
    """Stable per-machine SHA-256 hex digest. Same across reboots; changes if MB/CPU/disk replaced."""
    parts = [_bios_uuid(), _motherboard_serial(), _disk_serial(), _mac(), platform.machine()]
    raw = "|".join(p for p in parts if p)
    if not raw:
        # Semua probe gagal (VM minimal dsb) — JANGAN pakai konstanta global
        # ("fallback" akan sama di semua mesin!). Persist random UUID per-mesin.
        raw = _persistent_fallback_id()
        logger.warning("HWID: no stable identifiers found, using persisted random id — fingerprint weaker")
    digest = hashlib.sha256(raw.encode("utf-8")).hexdigest()
    return digest


def _persistent_fallback_id() -> str:
    """Random UUID yang disimpan di app data — stabil per-mesin walau probe gagal."""
    from .paths import app_data_dir

    marker = app_data_dir() / ".hwid_fallback"
    try:
        existing = marker.read_text(encoding="utf-8").strip()
        if existing:
            return existing
    except OSError:
        pass
    new_id = f"fallback-{uuid.uuid4().hex}"
    try:
        marker.write_text(new_id, encoding="utf-8")
    except OSError as e:
        logger.warning(f"HWID: cannot persist fallback id: {e}")
    return new_id


def get_hwid_short() -> str:
    """First 16 chars — friendly to display in UI."""
    return get_hwid()[:16].upper()
