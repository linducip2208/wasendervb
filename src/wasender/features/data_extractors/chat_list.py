"""Grab Chat List — list semua chat (private + group)."""
from __future__ import annotations

from .. import register
from ..base import export_csv, get_session


@register(
    "chat_list",
    label="Grab Chat List",
    category="Data Extractors",
    description="Grab list semua chat di sidebar (private + group) dengan unread count.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    only_groups = bool(params.get("only_groups", False))
    only_unread = bool(params.get("only_unread", False))

    sess = get_session(account_id)
    chats = sess.get_all_chats()
    if only_groups:
        chats = [c for c in chats if c.get("isGroup")]
    if only_unread:
        chats = [c for c in chats if c.get("unread", 0) > 0]

    csv_path = export_csv(
        chats,
        f"chat_list_acc{account_id}",
        fieldnames=["id", "name", "isGroup", "unread", "timestamp", "lastMessage"],
    )
    return {"count": len(chats), "chats": chats, "export": csv_path}
