"""Get Group Member — extract list anggota dari group ID/JID."""
from __future__ import annotations

from .. import register
from ..base import export_csv, get_session


@register(
    "get_group_member",
    label="Get Group Member",
    category="Data Extractors",
    description="Extract list anggota dari sebuah group berdasarkan group ID.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    group_id = params["group_id"]
    sess = get_session(account_id)
    members = sess.get_group_participants(group_id)
    csv_path = export_csv(
        members,
        f"group_members_{group_id.replace('@', '_')}",
        fieldnames=["phone", "id", "isAdmin", "isSuperAdmin"],
    )
    return {"count": len(members), "members": members, "export": csv_path}
