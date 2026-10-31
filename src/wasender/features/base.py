"""Common helpers for feature implementations."""
from __future__ import annotations

import csv
import json
import re
from datetime import datetime
from pathlib import Path
from typing import Any

from ..core.paths import exports_dir


def export_csv(rows: list[dict], filename_prefix: str, fieldnames: list[str] | None = None) -> str:
    if not rows:
        return ""
    fieldnames = fieldnames or list(rows[0].keys())
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    path = exports_dir() / f"{filename_prefix}_{ts}.csv"
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(r)
    return str(path)


def export_json(data: Any, filename_prefix: str) -> str:
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    path = exports_dir() / f"{filename_prefix}_{ts}.json"
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding="utf-8")
    return str(path)


def get_session(account_id: int):
    from ..wa import account_manager
    sess = account_manager.get_session(account_id)
    if not sess or not sess.window:
        raise RuntimeError(f"WhatsApp account {account_id} belum terbuka. Buka dulu di tab Accounts.")
    if not sess.wait_for_wapi(timeout=30):
        raise RuntimeError("WAPI belum ready. Pastikan QR sudah scan & WhatsApp Web sudah loaded.")
    return sess


_VAR_RE = re.compile(r"\{([A-Za-z0-9_]+)\}")


def render_template(template: str, ctx: dict) -> str:
    """Simple {var} replacement. Missing vars left as-is.

    Single-pass: nilai ctx yang mengandung "{var}" TIDAK di-expand ulang.
    """
    def _sub(m: re.Match) -> str:
        key = m.group(1)
        return str(ctx[key]) if key in ctx else m.group(0)

    return _VAR_RE.sub(_sub, template)
