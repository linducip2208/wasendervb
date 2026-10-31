"""Bulk Sender — main feature: kirim pesan ke list contact dengan template variabel + delay."""
from __future__ import annotations

from .. import register
from ..base import get_session, render_template
from ...core import db
from ...core.logger import logger
from ...core.ratelimit import acquire_send_slot, increment_sent


@register(
    "bulk_send",
    label="Bulk Sender",
    category="Messaging",
    description="Mass send dengan template variabel ({name}, {phone}, custom). Random delay + daily quota + log per-recipient.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    contacts = params["contacts"]  # [{phone, name, ...custom}, ...]
    template = params["message"]   # "Halo {name}, ada promo untuk kamu!"
    media_path = params.get("media_path")  # optional, path absolute
    campaign = (params.get("campaign") or "").strip() or None
    sess = get_session(account_id)

    sent, failed = 0, 0
    log = []
    for ct in contacts:
        phone = str(ct.get("phone", "")).strip()
        if not phone:
            continue
        try:
            acquire_send_slot(account_id)
            text = render_template(template, ct)

            if media_path:
                result = sess.send_media_by_phone(phone, media_path, caption=text)
            else:
                result = sess.send_message_by_phone(phone, text)
            if result.get("error"):
                raise RuntimeError(result["error"])

            db.insert("message_log", {
                "account_id": account_id, "phone": phone, "message": text,
                "media_path": media_path, "status": "sent", "kind": "bulk",
                "direction": "out", "campaign": campaign,
            })
            increment_sent(account_id)
            sent += 1
            log.append({"phone": phone, "ok": True})
        except Exception as e:
            failed += 1
            db.insert("message_log", {
                "account_id": account_id, "phone": phone, "message": template,
                "status": "failed", "error": str(e), "kind": "bulk",
                "direction": "out", "campaign": campaign,
            })
            log.append({"phone": phone, "ok": False, "error": str(e)})

    logger.info(f"Bulk send done: sent={sent}, failed={failed}, account={account_id}")
    return {"sent": sent, "failed": failed, "total": len(contacts), "log": log}
