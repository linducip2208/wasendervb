"""Website Email/Mobile Extractor — crawl URL & extract contact info."""
from __future__ import annotations

import re
from urllib.parse import urljoin, urlparse

import httpx
from selectolax.parser import HTMLParser

from .. import register
from ..base import export_csv
from ...core import db
from ...core.logger import logger

EMAIL_RE = re.compile(r"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+")
PHONE_RE = re.compile(r"\+?\d[\d\s\-()]{7,}\d")


def _crawl_one(url: str, max_pages: int, client: httpx.Client) -> dict:
    seen, queue = set(), [url]
    emails, phones = set(), set()
    domain = urlparse(url).netloc

    while queue and len(seen) < max_pages:
        u = queue.pop(0)
        if u in seen:
            continue
        seen.add(u)
        try:
            r = client.get(u, timeout=15)
            text = r.text
        except Exception as e:
            logger.debug(f"crawl error {u}: {e}")
            continue

        for m in EMAIL_RE.findall(text):
            if not m.lower().endswith((".png", ".jpg", ".gif", ".webp")):
                emails.add(m)
        for m in PHONE_RE.findall(text):
            cleaned = re.sub(r"[\s\-()]", "", m)
            if 8 <= len(cleaned) <= 16:
                phones.add(m.strip())

        try:
            tree = HTMLParser(text)
            for a in tree.css("a[href]"):
                href = a.attributes.get("href", "")
                full = urljoin(u, href)
                if urlparse(full).netloc == domain and full not in seen:
                    if any(k in full.lower() for k in ("contact", "about", "support", "kontak", "tentang")):
                        queue.insert(0, full)
                    else:
                        queue.append(full)
        except Exception:
            pass

    return {
        "url": url,
        "emails": sorted(emails),
        "phones": sorted(phones),
        "pages_crawled": len(seen),
    }


@register(
    "website_extractor",
    label="Website Email Mobile Extractor",
    category="Lead Generation",
    description="Crawl URL website & extract email + nomor HP.",
    requires_account=False,
)
def run(params: dict) -> dict:
    urls = params.get("urls") or [params["url"]]
    max_pages = int(params.get("max_pages_per_site", 20))
    save_to_db = bool(params.get("save_to_db", True))

    headers = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}
    rows = []
    flat = []
    with httpx.Client(headers=headers, follow_redirects=True) as client:
        for url in urls:
            res = _crawl_one(url, max_pages, client)
            rows.append(res)
            for e in res["emails"]:
                flat.append({"source": "website", "website": url, "email": e, "phone": "", "name": ""})
            for p in res["phones"]:
                flat.append({"source": "website", "website": url, "email": "", "phone": p, "name": ""})

    if save_to_db and flat:
        with db.transaction() as c:
            for r in flat:
                c.execute(
                    "INSERT INTO leads (source, phone, email, website) VALUES (?,?,?,?)",
                    (r["source"], r["phone"], r["email"], r["website"]),
                )

    csv_path = export_csv(flat, "website_extractor", ["source", "website", "email", "phone"]) if flat else ""
    return {"sites": len(rows), "leads": len(flat), "details": rows, "export": csv_path}
