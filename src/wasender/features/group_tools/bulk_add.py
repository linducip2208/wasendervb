"""Bulk Add Group Members — add banyak member ke group."""
from __future__ import annotations

import json

from .. import register
from ..base import get_session
from ...core.logger import logger
from ...core.ratelimit import random_delay


@register(
    "bulk_add_members",
    label="Bulk Add Group Members",
    category="Group Tools",
    description="Add list nomor ke group sekaligus. Hanya bisa kalau kamu admin di group itu.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    group_id = params["group_id"]
    phones = params["phones"]
    sess = get_session(account_id)

    results = []
    for phone in phones:
        try:
            js = f"""
            (async () => {{
              try {{
                const groupId = {json.dumps(group_id)};
                const phone = {json.dumps(phone)};
                const jid = WAPI.phoneToJid(phone);
                if (!WAPI.Store.GroupMetadata) return {{phone, ok:false, error:'no_group_store'}};
                const group = WAPI.Store.GroupMetadata.GroupMetadata.get(groupId);
                if (!group) return {{phone, ok:false, error:'group_not_found'}};
                if (typeof group.addParticipants === 'function') {{
                  await group.addParticipants([jid]);
                  return {{phone, ok:true}};
                }}
                if (typeof group.add === 'function') {{
                  await group.add([jid]);
                  return {{phone, ok:true}};
                }}
                return {{phone, ok:false, error:'no_add_method'}};
              }} catch(e) {{ return {{phone:{json.dumps(phone)}, ok:false, error:String(e)}}; }}
            }})().then(r => JSON.stringify(r));
            """
            raw = sess.eval_js(js)
            results.append(json.loads(raw) if raw else {"phone": phone, "ok": False, "error": "no_response"})
        except Exception as e:
            results.append({"phone": phone, "ok": False, "error": str(e)})
        random_delay()

    success = sum(1 for r in results if r.get("ok"))
    logger.info(f"Bulk add: {success}/{len(phones)} added to {group_id}")
    return {"total": len(results), "added": success, "results": results}
