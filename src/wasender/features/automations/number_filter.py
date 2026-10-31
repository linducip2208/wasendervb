"""WhatsApp Number Filter — cek nomor terdaftar di WhatsApp atau tidak."""
from __future__ import annotations

from .. import register
from ..base import export_csv, get_session
from ...core.ratelimit import random_delay


@register(
    "number_filter",
    label="WhatsApp Number Filter",
    category="Automations",
    description="Cek list nomor — mana yang aktif di WhatsApp, mana yang tidak. Output CSV terpisah.",
)
def run(params: dict) -> dict:
    account_id = int(params["account_id"])
    phones = params["phones"]
    sess = get_session(account_id)

    valid, invalid = [], []
    raw_results = []
    for phone in phones:
        try:
            res = sess.check_number(phone)
            raw_results.append(res)
            if res.get("exists"):
                valid.append({"phone": phone})
            else:
                invalid.append({"phone": phone, "reason": res.get("error", "not_on_whatsapp")})
        except Exception as e:
            invalid.append({"phone": phone, "reason": str(e)})
        random_delay()

    valid_csv = export_csv(valid, "number_filter_valid", ["phone"]) if valid else ""
    invalid_csv = export_csv(invalid, "number_filter_invalid", ["phone", "reason"]) if invalid else ""
    return {
        "checked": len(phones),
        "valid": len(valid),
        "invalid": len(invalid),
        "valid_csv": valid_csv,
        "invalid_csv": invalid_csv,
    }
