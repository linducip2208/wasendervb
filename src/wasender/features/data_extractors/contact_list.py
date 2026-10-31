"""Contact List Grabber — grab semua kontak dari akun."""
from __future__ import annotations

from .. import register
from ..base import export_csv, get_session
from ...core import db


@register(
    "contact_list",
    label="Contact List Grabber",
    category="Data Extractors",
    description="Grab semua kontak dari WhatsApp akun (wajib + non-wajib).",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    only_my_contacts = bool(params.get("only_my_contacts", False))
    save_to_db = bool(params.get("save_to_db", True))

    sess = get_session(account_id)
    contacts = sess.get_all_contacts()
    if only_my_contacts:
        contacts = [c for c in contacts if c.get("isMyContact")]

    if save_to_db:
        with db.transaction() as c:
            for ct in contacts:
                phone = ct.get("phone")
                if not phone:
                    continue
                c.execute(
                    "INSERT OR IGNORE INTO contacts (account_id, phone, name, extra) VALUES (?, ?, ?, ?)",
                    (account_id, phone, ct.get("name") or ct.get("pushname") or "", str(ct)),
                )

    csv_path = export_csv(
        contacts,
        f"contacts_acc{account_id}",
        fieldnames=["phone", "name", "pushname", "isMyContact", "isWAContact", "isBusiness"],
    )
    return {"count": len(contacts), "export": csv_path}
