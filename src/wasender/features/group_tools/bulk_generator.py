"""Bulk Group Generator — bikin banyak group sekaligus dengan template nama."""
from __future__ import annotations

import json

from .. import register
from ..base import get_session, render_template
from ...core.logger import logger
from ...core.ratelimit import random_delay


@register(
    "bulk_group_gen",
    label="Bulk Group Generator",
    category="Group Tools",
    description="Bikin N group dengan template nama dan seed members.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    name_template = params["name_template"]  # e.g. "Promo Day {n}"
    count = int(params["count"])
    seed_phones = params.get("seed_phones", [])  # min 1 admin selain kamu
    sess = get_session(account_id)

    if not seed_phones:
        raise ValueError("seed_phones wajib minimal 1 nomor (WhatsApp ga bisa bikin group kosong)")

    results = []
    for i in range(1, count + 1):
        name = render_template(name_template, {"n": str(i), "i": str(i)})
        try:
            js = f"""
            (async () => {{
              try {{
                const seeds = {json.dumps(seed_phones)}.map(p => WAPI.phoneToJid(p));
                const name = {json.dumps(name)};
                if (!WAPI.Store.Chat || !WAPI.Store.Chat.createGroup) {{
                  // fallback module find
                  const mod = Object.values(WAPI._modCache || {{}}).find(m => m && m.createGroup);
                  if (!mod) return {{ok:false, error:'no_create_group_api'}};
                  const r = await mod.createGroup(name, seeds);
                  return {{ok:true, name, id:r && r.gid && r.gid._serialized}};
                }}
                const r = await WAPI.Store.Chat.createGroup(name, seeds);
                return {{ok:true, name, id:r && r.gid && r.gid._serialized}};
              }} catch(e) {{ return {{ok:false, error:String(e)}}; }}
            }})().then(r => JSON.stringify(r));
            """
            raw = sess.eval_js(js)
            results.append(json.loads(raw) if raw else {"ok": False, "error": "no_response"})
        except Exception as e:
            results.append({"ok": False, "error": str(e), "name": name})
        random_delay()

    created = sum(1 for r in results if r.get("ok"))
    logger.info(f"Bulk group gen: {created}/{count} created")
    return {"requested": count, "created": created, "results": results}
