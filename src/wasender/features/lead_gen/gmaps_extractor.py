"""Google Map Data Extractor — scrape business listings dari Google Maps."""
from __future__ import annotations

import asyncio
import re
from urllib.parse import quote

from .. import register
from ..base import export_csv
from ...core import db
from ...core.logger import logger


async def _scrape(query: str, location: str, limit: int) -> list[dict]:
    from playwright.async_api import async_playwright

    full_q = f"{query} in {location}" if location else query
    url = f"https://www.google.com/maps/search/{quote(full_q)}"
    results: list[dict] = []

    async with async_playwright() as p:
        browser = await p.chromium.launch(headless=True)
        ctx = await browser.new_context(
            user_agent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            viewport={"width": 1280, "height": 900},
        )
        page = await ctx.new_page()
        try:
            await page.goto(url, wait_until="networkidle", timeout=45000)
            await page.wait_for_selector("[role='feed']", timeout=20000)

            # Scroll feed until enough results or no more
            seen = 0
            for _ in range(30):
                items = await page.query_selector_all("[role='feed'] > div > div[jsaction]")
                if len(items) == seen and len(items) >= limit:
                    break
                seen = len(items)
                if seen >= limit:
                    break
                await page.evaluate("document.querySelector(\"[role='feed']\").scrollBy(0, 1500)")
                await page.wait_for_timeout(1200)

            items = await page.query_selector_all("[role='feed'] > div > div[jsaction]")
            for el in items[:limit]:
                try:
                    await el.click()
                    await page.wait_for_timeout(1500)
                    name = await page.evaluate("() => document.querySelector('h1')?.innerText || ''")
                    info_html = await page.evaluate("() => document.body.innerText")
                    phone_m = re.search(r"\+?\d[\d\s\-()]{7,}\d", info_html)
                    addr_btn = await page.query_selector("button[data-item-id^='address']")
                    address = await addr_btn.inner_text() if addr_btn else ""
                    site_btn = await page.query_selector("a[data-item-id^='authority']")
                    website = await site_btn.get_attribute("href") if site_btn else ""
                    results.append({
                        "name": name,
                        "phone": phone_m.group() if phone_m else "",
                        "address": address,
                        "website": website,
                        "source": "google_maps",
                    })
                except Exception as e:
                    logger.warning(f"GMaps item error: {e}")
        finally:
            await browser.close()
    return results


@register(
    "gmaps_extractor",
    label="Google Map Data Extractor",
    category="Lead Generation",
    description="Scrape business listings (nama, phone, alamat, website) dari Google Maps berdasarkan query.",
    requires_account=False,
)
def run(params: dict) -> dict:
    query = params["query"]
    location = params.get("location", "")
    limit = int(params.get("limit", 30))
    save_to_db = bool(params.get("save_to_db", True))

    rows = asyncio.run(_scrape(query, location, limit))
    if save_to_db:
        with db.transaction() as c:
            for r in rows:
                c.execute(
                    "INSERT INTO leads (source, name, phone, address, website, extra) VALUES (?,?,?,?,?,?)",
                    ("google_maps", r["name"], r["phone"], r["address"], r["website"], query),
                )
    csv_path = export_csv(rows, f"gmaps_{query.replace(' ', '_')}") if rows else ""
    return {"count": len(rows), "rows": rows, "export": csv_path}
