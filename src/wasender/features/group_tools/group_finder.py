"""Group Finder — Google search untuk public WhatsApp group invite links."""
from __future__ import annotations

import re

import httpx

from .. import register
from ..base import export_csv

LINK_RE = re.compile(r"https?://chat\.whatsapp\.com/[A-Za-z0-9]+", re.IGNORECASE)


def _google_search(query: str, num: int = 30) -> list[str]:
    """Light scrape Google SERP. For production replace with SerpAPI/CSE for stability."""
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
        "Accept-Language": "en-US,en;q=0.9",
    }
    with httpx.Client(timeout=20, headers=headers, follow_redirects=True) as c:
        r = c.get(f"https://www.google.com/search?q={query}+site:chat.whatsapp.com&num={num}")
        return list(set(LINK_RE.findall(r.text)))


@register(
    "group_finder",
    label="Group Finder",
    category="Group Tools",
    description="Cari public WhatsApp group via Google dorking by keyword/topic.",
    requires_account=False,
)
def run(params: dict) -> dict:
    keyword = params["keyword"]
    limit = int(params.get("limit", 30))
    links = _google_search(keyword, num=limit)
    rows = [{"link": l, "keyword": keyword} for l in links]
    csv_path = export_csv(rows, f"group_finder_{keyword.replace(' ', '_')}") if rows else ""
    return {"count": len(rows), "links": links, "export": csv_path}
