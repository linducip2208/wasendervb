"""Grab WhatsApp Group Links from web page."""
from __future__ import annotations

import re

import httpx

from .. import register
from ..base import export_csv

LINK_RE = re.compile(r"https?://chat\.whatsapp\.com/[A-Za-z0-9]+", re.IGNORECASE)


@register(
    "grab_group_links",
    label="Grab WhatsApp Group Links from web page",
    category="Group Tools",
    description="Scrape link group WhatsApp dari URL halaman web.",
    requires_account=False,
)
def run(params: dict) -> dict:
    urls = params.get("urls") or [params["url"]]
    timeout = int(params.get("timeout", 15))
    found: set[str] = set()
    visited = []

    with httpx.Client(timeout=timeout, follow_redirects=True, headers={
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
    }) as client:
        for url in urls:
            try:
                r = client.get(url)
                visited.append({"url": url, "status": r.status_code, "size": len(r.text)})
                for m in LINK_RE.findall(r.text):
                    found.add(m)
            except Exception as e:
                visited.append({"url": url, "status": "error", "size": str(e)})

    rows = [{"link": link} for link in sorted(found)]
    csv_path = export_csv(rows, "group_links", fieldnames=["link"]) if rows else ""
    return {"count": len(rows), "links": sorted(found), "visited": visited, "export": csv_path}
