"""Get Poll Results — extract hasil poll message."""
from __future__ import annotations

from .. import register
from ..base import export_json, get_session


@register(
    "poll_results",
    label="Get Poll Results",
    category="Data Extractors",
    description="Extract hasil polling dari poll message di chat/group.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    chat_id = params["chat_id"]
    msg_id = params["msg_id"]

    sess = get_session(account_id)
    result = sess.get_poll_results(chat_id, msg_id)
    if result is None:
        return {"found": False, "message": "Poll tidak ditemukan atau bukan poll message"}

    json_path = export_json(result, f"poll_{msg_id.replace(':', '_')}")
    return {"found": True, "poll": result, "export": json_path}
