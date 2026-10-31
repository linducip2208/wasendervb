"""Auto Group Joiner — auto-join group dari list link dengan delay."""
from __future__ import annotations

import json
import time

from .. import register
from ..base import get_session
from ...core.logger import logger
from ...core.ratelimit import random_delay


@register(
    "auto_joiner",
    label="Auto Group Joiner",
    category="Group Tools",
    description="Auto-join sekumpulan link invite group secara berurutan dengan delay.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    links = params["links"]
    sess = get_session(account_id)

    results = []
    for link in links:
        if "chat.whatsapp.com/" not in link:
            results.append({"link": link, "status": "invalid"})
            continue
        try:
            sess.window.load_url(link)
            time.sleep(5)  # Wait for join button render
            js = """
            (async () => {
              const buttons = document.querySelectorAll('div[role="button"]');
              for (const b of buttons) {
                const t = (b.innerText || '').toLowerCase();
                if (t.includes('join') || t.includes('gabung')) {
                  b.click();
                  return { clicked: true, label: b.innerText };
                }
              }
              return { clicked: false };
            })().then(r => JSON.stringify(r));
            """
            raw = sess.eval_js(js)
            res = json.loads(raw) if raw else {}
            results.append({"link": link, **res})
            logger.info(f"Auto-join {link}: {res}")
        except Exception as e:
            results.append({"link": link, "status": "error", "error": str(e)})
        random_delay()

    # restore to WhatsApp main view
    try:
        sess.window.load_url("https://web.whatsapp.com/")
    except Exception:
        pass

    success = sum(1 for r in results if r.get("clicked"))
    return {"total": len(results), "joined": success, "results": results}
