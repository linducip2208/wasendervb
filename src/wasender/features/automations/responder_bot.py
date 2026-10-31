"""WhatsApp Auto Responder Bot — match incoming message by keyword/regex, reply automatically."""
from __future__ import annotations

import re
import time

from .. import register
from ..base import get_session
from ...core import db
from ...core.logger import logger


@register(
    "auto_responder",
    label="WhatsApp Auto Responder Bot",
    category="Automations",
    description="Auto-reply incoming message by keyword/regex match. Configure rules in DB.",
)
def run(params: dict) -> dict:
    """Toggle/configure responder. Actual handling in `on_message`."""
    action = params.get("action", "status")
    if action == "list":
        return {"rules": db.query_all("SELECT * FROM auto_replies ORDER BY id")}
    if action == "add":
        rid = db.insert("auto_replies", {
            "account_id": params["account_id"],
            "pattern": params["pattern"],
            "is_regex": 1 if params.get("is_regex") else 0,
            "response": params["response"],
            "enabled": 1,
        })
        return {"id": rid}
    if action == "delete":
        db.delete("auto_replies", int(params["id"]))
        return {"ok": True}
    if action == "toggle":
        db.update("auto_replies", int(params["id"]), {"enabled": 1 if params.get("enabled") else 0})
        return {"ok": True}
    if action == "start":
        # subscribe via WAPI; per-account
        sess = get_session(int(params["account_id"]))
        sess.register_incoming_handler(lambda payload: on_message(payload))
        return {"ok": True, "listening": True}
    return {"status": "ready"}


_recent: dict[str, float] = {}  # debounce: don't reply twice to same chat in <30s


def on_message(payload: dict) -> None:
    """Called by API bridge on every WAPI incoming message."""
    try:
        account_id = payload.get("accountId")
        msg = payload.get("msg", {})
        body = (msg.get("body") or "").strip()
        from_jid = msg.get("from")
        if not body or not from_jid:
            return

        # Measurement: catat semua pesan masuk individu (bukan grup) untuk
        # reply-rate. Tidak boleh menggagalkan auto-reply → try terpisah.
        try:
            if "@g.us" not in str(from_jid):
                phone = re.sub(r"\D", "", str(from_jid))
                if phone:
                    db.insert("message_log", {
                        "account_id": int(account_id) if account_id else 0,
                        "phone": phone, "message": body[:2000],
                        "status": "received", "kind": "inbox", "direction": "in",
                    })
        except Exception as e:
            logger.warning(f"Inbox log failed: {e}")

        rules = db.query_all(
            "SELECT * FROM auto_replies WHERE enabled=1 AND (account_id IS NULL OR account_id=?)",
            (account_id,),
        )
        matched = None
        for r in rules:
            pattern = r["pattern"]
            try:
                if r["is_regex"]:
                    if re.search(pattern, body, re.IGNORECASE):
                        matched = r
                        break
                else:
                    if pattern.lower() in body.lower():
                        matched = r
                        break
            except re.error:
                continue

        if not matched:
            return

        now = time.time()
        key = f"{account_id}:{from_jid}:{matched['id']}"
        if now - _recent.get(key, 0) < 30:
            return
        _recent[key] = now

        sess = get_session(account_id)
        sess.eval_js(
            f"WAPI.sendMessage({_json(from_jid)}, {_json(matched['response'])}).catch(e => console.error(e))"
        )
        logger.info(f"Auto-replied to {from_jid} via rule {matched['id']}")
    except Exception as e:
        logger.exception(f"Responder on_message error: {e}")


def _json(s: str) -> str:
    import json as _j
    return _j.dumps(s)
