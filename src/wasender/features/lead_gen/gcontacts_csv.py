"""Google Contacts CSV Generator — convert contact list ke format Google Contacts CSV."""
from __future__ import annotations

import csv
from datetime import datetime

from .. import register
from ...core.paths import exports_dir


GOOGLE_CONTACTS_FIELDS = [
    "Name", "Given Name", "Family Name", "E-mail 1 - Value",
    "Phone 1 - Type", "Phone 1 - Value", "Notes", "Group Membership",
]


def _parse_csv_input(raw: str) -> list[dict]:
    if not raw or not raw.strip():
        return []
    lines = raw.strip().split("\n")
    if not lines:
        return []
    header = [h.strip() for h in lines[0].split(",")]
    rows = []
    for line in lines[1:]:
        if not line.strip():
            continue
        cols = [c.strip() for c in line.split(",")]
        r = {}
        for i, h in enumerate(header):
            r[h.lower()] = cols[i] if i < len(cols) else ""
        rows.append(r)
    return rows


@register(
    "gcontacts_csv",
    label="Google Contacts CSV Generator",
    category="Lead Generation",
    description="Convert list contact ke format Google Contacts CSV — ready untuk import ke Gmail.",
    requires_account=False,
)
def run(params: dict) -> dict:
    csv_input = params.get("csv", "")
    label = params.get("label", "WASender Import")
    contacts = _parse_csv_input(csv_input)

    if not contacts:
        raise ValueError("CSV input kosong — paste data dari Google Contacts export")

    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    path = exports_dir() / f"google_contacts_{ts}.csv"

    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=GOOGLE_CONTACTS_FIELDS)
        w.writeheader()
        for c in contacts:
            name = str(c.get("name", "")).strip()
            given, family = (name.split(" ", 1) + [""])[:2] if name else ("", "")
            w.writerow({
                "Name": name,
                "Given Name": given,
                "Family Name": family,
                "E-mail 1 - Value": str(c.get("email", "")),
                "Phone 1 - Type": "Mobile",
                "Phone 1 - Value": str(c.get("phone", "")),
                "Notes": str(c.get("notes", "")),
                "Group Membership": f"* {label}",
            })

    return {"count": len(contacts), "export": str(path)}
