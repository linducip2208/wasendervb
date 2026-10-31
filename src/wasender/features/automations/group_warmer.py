"""WhatsApp Group Warmer — kirim pesan ke list group dengan delay (untuk warmup grup)."""
from __future__ import annotations

import json
import random

from .. import register
from ..base import get_session
from ...core import db
from ...core.ratelimit import random_delay


@register(
    "group_warmer",
    label="WhatsApp Group Warmer",
    category="Automations",
    description="Warmup di list group — kirim pesan random ke setiap group dengan delay natural.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    group_ids = params["group_ids"]
    templates = params["templates"]
    rounds = int(params.get("rounds", 1))

    sess = get_session(account_id)
    sent_log = []
    for _round in range(rounds):
        random.shuffle(group_ids)
        for gid in group_ids:
            text = random.choice(templates)
            try:
                sess.eval_js(
                    f"WAPI.sendMessage({json.dumps(gid)}, {json.dumps(text)}).catch(e => console.error(e))"
                )
                sent_log.append({"group": gid, "text": text, "ok": True})
                db.insert("message_log", {
                    "account_id": account_id, "phone": gid, "message": text, "status": "sent",
                    "kind": "warmer",
                })
            except Exception as e:
                sent_log.append({"group": gid, "text": text, "ok": False, "error": str(e)})
            random_delay()

    success = sum(1 for s in sent_log if s["ok"])
    return {"sent": success, "total": len(sent_log), "results": sent_log}
