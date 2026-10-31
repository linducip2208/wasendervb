"""Social Media Data Extractor — scrape public profiles untuk basic info.

Note: setiap platform punya anti-bot policy. Implementasi ini handle public-only data.
Instagram/Facebook butuh login → di-skip di MVP, hanya support Twitter/X public + LinkedIn search dorking.
"""
from __future__ import annotations

import re
from urllib.parse import quote

import httpx

from .. import register
from ..base import export_csv

EMAIL_RE = re.compile(r"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+")


def _google_dork(query: str, num: int = 30) -> list[str]:
    headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"}
    with httpx.Client(headers=headers, follow_redirects=True, timeout=20) as c:
        r = c.get(f"https://www.google.com/search?q={quote(query)}&num={num}")
        return list(set(re.findall(r"https?://[^\"\s>]+", r.text)))


@register(
    "social_extractor",
    label="Social Media Data Extractor",
    category="Lead Generation",
    description="Cari profil sosmed via Google dorking. Support: linkedin, twitter, instagram (public profile only).",
    requires_account=False,
)
def run(params: dict) -> dict:
    keyword = params["keyword"]
    platform = params.get("platform", "linkedin")  # linkedin | twitter | instagram
    site_map = {
        "linkedin": "site:linkedin.com/in",
        "twitter": "site:twitter.com OR site:x.com",
        "instagram": "site:instagram.com",
        "facebook": "site:facebook.com",
        "tiktok": "site:tiktok.com",
    }
    site = site_map.get(platform, "site:linkedin.com/in")
    query = f"{site} {keyword}"
    urls = _google_dork(query, num=int(params.get("limit", 30)))

    profiles = []
    for url in urls:
        if platform in url or "linkedin.com" in url or "twitter.com" in url or "x.com" in url or "instagram.com" in url:
            profiles.append({"platform": platform, "url": url, "keyword": keyword})

    csv_path = export_csv(profiles, f"social_{platform}_{keyword.replace(' ', '_')}") if profiles else ""
    return {"count": len(profiles), "profiles": profiles, "export": csv_path}
