"""Grab Active Group Members — filter anggota yg pernah chat dalam N hari terakhir."""
from __future__ import annotations

import time

from .. import register
from ..base import export_csv, get_session


@register(
    "active_members",
    label="Grab Active Group Members",
    category="Data Extractors",
    description="Filter anggota group yang aktif (pernah kirim pesan) dalam N hari terakhir.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    group_id = params["group_id"]
    days = int(params.get("days", 30))
    msg_limit = int(params.get("message_limit", 1000))

    sess = get_session(account_id)
    msgs = sess.get_messages(group_id, count=msg_limit)
    cutoff = int(time.time()) - days * 86400

    active_jids: dict[str, dict] = {}
    for m in msgs:
        if m.get("timestamp", 0) < cutoff:
            continue
        sender = m.get("author") or m.get("from")
        if not sender:
            continue
        active_jids.setdefault(sender, {"jid": sender, "messages": 0, "last_seen": 0})
        active_jids[sender]["messages"] += 1
        active_jids[sender]["last_seen"] = max(active_jids[sender]["last_seen"], m.get("timestamp", 0))

    rows = sorted(active_jids.values(), key=lambda r: r["messages"], reverse=True)
    csv_path = export_csv(
        rows,
        f"active_members_{group_id.replace('@', '_')}",
        fieldnames=["jid", "messages", "last_seen"],
    )
    return {"count": len(rows), "active_members": rows, "export": csv_path}
