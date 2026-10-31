"""Tests for the license parsing / validation flow."""
from __future__ import annotations

import base64
import json
from datetime import date, timedelta

import pytest

from wasender.core import license as license_mod
from wasender.core.license import (
    License,
    LicenseError,
    parse_license_text,
    validate_for_machine,
)

FAKE_HWID = "a" * 64


def _armor(blob: str) -> str:
    return (
        "-----BEGIN WASENDER LICENSE-----\n"
        + "\n".join(blob[i : i + 64] for i in range(0, len(blob), 64))
        + "\n-----END WASENDER LICENSE-----\n"
    )


def _issue(
    private_key,
    *,
    hwid: str = FAKE_HWID,
    customer: str = "Test Customer",
    tier: str = "lifetime",
    features=None,
    expires_at: str | None = None,
) -> tuple[str, dict, bytes, bytes]:
    """Sign a license payload with the supplied dev private key.

    Returns (armored_text, payload_dict, payload_bytes, signature_bytes) so that
    individual tests can re-encode after corrupting one or the other.
    """
    payload = {
        "license_id": "WAS-TEST-0000-0000",
        "hwid": hwid.lower(),
        "customer": customer,
        "tier": tier,
        "features": features if features is not None else ["*"],
        "issued_at": date.today().isoformat(),
        "expires_at": expires_at,
        "version": 1,
    }
    payload_bytes = json.dumps(payload, separators=(",", ":"), sort_keys=True).encode()
    signature = private_key.sign(payload_bytes)
    blob = (
        base64.b64encode(payload_bytes).decode()
        + "."
        + base64.b64encode(signature).decode()
    )
    return _armor(blob), payload, payload_bytes, signature


@pytest.fixture
def patch_hwid(monkeypatch):
    """Pin ``get_hwid`` to FAKE_HWID for the validate_for_machine path."""
    monkeypatch.setattr(license_mod, "get_hwid", lambda: FAKE_HWID)
    return FAKE_HWID


def test_parse_valid_license(dev_keypair, patch_hwid):
    armored, payload, _, _ = _issue(dev_keypair)
    lic = parse_license_text(armored)

    assert isinstance(lic, License)
    assert lic.license_id == payload["license_id"]
    assert lic.customer == "Test Customer"
    assert lic.tier == "lifetime"
    assert lic.hwid == FAKE_HWID
    # validate_for_machine should be silent for a fresh, matching license.
    validate_for_machine(lic)


def test_parse_corrupted_signature_fails(dev_keypair):
    _, _, payload_bytes, signature = _issue(dev_keypair)
    # Flip a single byte in the signature.
    bad_sig = bytearray(signature)
    bad_sig[0] ^= 0xFF
    blob = (
        base64.b64encode(payload_bytes).decode()
        + "."
        + base64.b64encode(bytes(bad_sig)).decode()
    )
    armored = _armor(blob)

    with pytest.raises(LicenseError, match="Signature"):
        parse_license_text(armored)


def test_parse_corrupted_payload_fails(dev_keypair):
    _, _, payload_bytes, signature = _issue(dev_keypair)
    # Flip a single byte in the payload — signature will no longer verify.
    bad_payload = bytearray(payload_bytes)
    bad_payload[0] ^= 0xFF
    blob = (
        base64.b64encode(bytes(bad_payload)).decode()
        + "."
        + base64.b64encode(signature).decode()
    )
    armored = _armor(blob)

    with pytest.raises(LicenseError):
        parse_license_text(armored)


def test_armor_format_required(dev_keypair):
    armored, _, _, _ = _issue(dev_keypair)
    # Strip the BEGIN/END markers entirely.
    inner = armored.replace("-----BEGIN WASENDER LICENSE-----", "")
    inner = inner.replace("-----END WASENDER LICENSE-----", "")

    with pytest.raises(LicenseError, match="header/footer"):
        parse_license_text(inner)


def test_expired_license(dev_keypair, patch_hwid):
    yesterday = (date.today() - timedelta(days=1)).isoformat()
    armored, _, _, _ = _issue(dev_keypair, expires_at=yesterday)
    lic = parse_license_text(armored)

    assert lic.is_expired() is True
    assert lic.days_remaining() == -1
    with pytest.raises(LicenseError, match="expired"):
        validate_for_machine(lic)


def test_hwid_mismatch(dev_keypair, monkeypatch):
    # License signed for FAKE_HWID, but the machine reports something else.
    armored, _, _, _ = _issue(dev_keypair, hwid=FAKE_HWID)
    monkeypatch.setattr(license_mod, "get_hwid", lambda: "b" * 64)
    lic = parse_license_text(armored)

    with pytest.raises(LicenseError, match="komputer"):
        validate_for_machine(lic)


def test_lifetime_no_expiry(dev_keypair, patch_hwid):
    armored, _, _, _ = _issue(dev_keypair, expires_at=None)
    lic = parse_license_text(armored)

    assert lic.expires_at is None
    assert lic.is_expired() is False
    assert lic.days_remaining() is None
    # Should pass machine validation without raising.
    validate_for_machine(lic)


def test_has_feature(dev_keypair):
    # Specific feature list — only the listed keys (and never others) are granted.
    armored, _, _, _ = _issue(dev_keypair, features=["bulk_send"])
    lic = parse_license_text(armored)
    assert lic.has_feature("bulk_send") is True
    assert lic.has_feature("warmer") is False

    # Wildcard — everything granted.
    armored2, _, _, _ = _issue(dev_keypair, features=["*"])
    lic2 = parse_license_text(armored2)
    assert lic2.has_feature("bulk_send") is True
    assert lic2.has_feature("warmer") is True
    assert lic2.has_feature("any_random_key") is True
