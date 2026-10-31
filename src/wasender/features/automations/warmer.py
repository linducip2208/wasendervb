"""WhatsApp Warmer — warmup akun baru dengan exchange pesan ke whitelist contacts."""
from __future__ import annotations

import json
import random

from .. import register
from ..base import get_session
from ...core import db
from ...core.logger import logger
from ...core.ratelimit import random_delay

DEFAULT_TEMPLATES = [
    "Halo, apa kabar?",
    "Selamat pagi 🙂",
    "Lagi sibuk?",
    "Just saying hi!",
    "Ada update terbaru?",
    "Semoga sehat selalu",
    "Terima kasih ya",
]


@register(
    "wa_warmer",
    label="WhatsApp Warmer",
    category="Automations",
    description="Warmup akun: kirim pesan random ke whitelist contacts, simulasi aktivitas natural.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    whitelist_phones = params.get("whitelist") or params.get("whitelist_phones", [])
    messages_count = int(params.get("count") or params.get("messages_count", 10))
    templates = params.get("templates") or DEFAULT_TEMPLATES

    if len(whitelist_phones) < 2:
        raise ValueError("Whitelist minimal 2 kontak biar pola natural")

    sess = get_session(account_id)
    sent = []
    for _ in range(messages_count):
        phone = random.choice(whitelist_phones)
        text = random.choice(templates)
        try:
            sess.eval_js(
                f"WAPI.sendMessageByPhone({json.dumps(phone)}, {json.dumps(text)}).catch(e => console.error(e))"
            )
            sent.append({"phone": phone, "text": text, "ok": True})
            db.insert("message_log", {
                "account_id": account_id, "phone": phone, "message": text, "status": "sent",
                "kind": "warmer",
            })
        except Exception as e:
            sent.append({"phone": phone, "text": text, "ok": False, "error": str(e)})
        random_delay()

    success = sum(1 for s in sent if s["ok"])
    logger.info(f"Warmup: {success}/{len(sent)} sent on account {account_id}")
    return {"requested": messages_count, "sent": success, "results": sent}
