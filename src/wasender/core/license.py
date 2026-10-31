"""License loading, validation & feature gating."""
from __future__ import annotations

import base64
import json
import os
import re
from dataclasses import dataclass, field
from datetime import date, datetime
from pathlib import Path

from .crypto import verify_signature
from .hwid import get_hwid
from .logger import logger
from .paths import is_frozen, license_path

ARMOR_RE = re.compile(
    r"-----BEGIN WASENDER LICENSE-----\s*(.*?)\s*-----END WASENDER LICENSE-----",
    re.DOTALL,
)

# Dev bypass hanya berlaku saat run dari source (tidak frozen).
# Di binary hasil PyInstaller (frozen) env ini diabaikan supaya tidak
# bisa dipakai untuk memakai app tanpa license.
_DEV_MODE = os.environ.get("WASENDER_DEV", "").lower() in ("1", "true", "yes") and not is_frozen()


@dataclass
class License:
    license_id: str
    hwid: str
    customer: str
    tier: str
    features: list[str]
    issued_at: str
    expires_at: str | None
    raw_payload: dict = field(default_factory=dict)

    def has_feature(self, key: str) -> bool:
        return "*" in self.features or key in self.features

    def is_expired(self) -> bool:
        if not self.expires_at:
            return False
        try:
            return date.fromisoformat(self.expires_at) < date.today()
        except ValueError:
            return True

    def days_remaining(self) -> int | None:
        if not self.expires_at:
            return None
        try:
            return (date.fromisoformat(self.expires_at) - date.today()).days
        except ValueError:
            return 0


class LicenseError(Exception):
    pass


def _strip_armor(text: str) -> str:
    m = ARMOR_RE.search(text)
    if not m:
        raise LicenseError("Format file license tidak valid (header/footer tidak ditemukan)")
    return re.sub(r"\s+", "", m.group(1))


def parse_license_text(text: str) -> License:
    blob = _strip_armor(text)
    if "." not in blob:
        raise LicenseError("Format license corrupt — missing payload/signature separator")
    payload_b64, sig_b64 = blob.split(".", 1)
    try:
        payload_bytes = base64.b64decode(payload_b64)
    except Exception as e:
        raise LicenseError(f"Gagal decode payload: {e}") from e

    if not verify_signature(payload_bytes, sig_b64):
        raise LicenseError("Signature tidak valid — license mungkin sudah dimodifikasi")

    try:
        payload = json.loads(payload_bytes.decode("utf-8"))
    except Exception as e:
        raise LicenseError(f"Payload bukan JSON valid: {e}") from e

    required = {"license_id", "hwid", "customer", "tier", "features", "issued_at"}
    missing = required - payload.keys()
    if missing:
        raise LicenseError(f"License kekurangan field: {missing}")

    if not isinstance(payload["license_id"], str) or not payload["license_id"]:
        raise LicenseError("License corrupt — license_id tidak valid")
    if not isinstance(payload["hwid"], str) or not payload["hwid"].strip():
        raise LicenseError("License corrupt — hwid tidak valid")
    if not isinstance(payload["tier"], str) or not payload["tier"]:
        raise LicenseError("License corrupt — tier tidak valid")
    if (
        not isinstance(payload["features"], list)
        or not payload["features"]
        or not all(isinstance(f, str) for f in payload["features"])
    ):
        raise LicenseError("License corrupt — features harus list of string")
    if "version" in payload and payload["version"] != 1:
        raise LicenseError(f"License version tidak didukung: {payload.get('version')}")
    try:
        date.fromisoformat(payload["issued_at"])
    except (ValueError, TypeError):
        raise LicenseError("License corrupt — issued_at bukan tanggal valid")
    expires_at = payload.get("expires_at")
    if expires_at is not None:
        try:
            date.fromisoformat(expires_at)
        except (ValueError, TypeError):
            raise LicenseError("License corrupt — expires_at bukan tanggal valid")

    return License(
        license_id=payload["license_id"],
        hwid=payload["hwid"],
        customer=payload["customer"],
        tier=payload["tier"],
        features=payload["features"],
        issued_at=payload["issued_at"],
        expires_at=expires_at,
        raw_payload=payload,
    )


def validate_for_machine(lic: License) -> None:
    if lic.is_expired():
        raise LicenseError(f"License sudah expired pada {lic.expires_at}")
    if lic.hwid.lower() != get_hwid().lower():
        raise LicenseError(
            "License ini tidak terdaftar untuk komputer ini. "
            "Minta vendor untuk re-issue dengan HWID yang baru."
        )


def load_active_license() -> License | None:
    if _DEV_MODE:
        logger.info("DEV MODE: using virtual license")
        return License(
            license_id="DEV-0000",
            hwid=get_hwid(),
            customer="Developer",
            tier="enterprise",
            features=["*"],
            issued_at="2026-01-01",
            expires_at=None,
        )

    path = Path(license_path())
    if not path.exists():
        return None
    try:
        text = path.read_text(encoding="utf-8")
        lic = parse_license_text(text)
        validate_for_machine(lic)
        return lic
    except LicenseError as e:
        logger.warning(f"License invalid: {e}")
        return None
    except Exception as e:
        logger.error(f"License load error: {e}")
        return None


def install_license(text: str) -> License:
    lic = parse_license_text(text)
    validate_for_machine(lic)
    Path(license_path()).write_text(text, encoding="utf-8")
    logger.info(f"License installed: {lic.license_id} ({lic.tier}, customer={lic.customer})")
    return lic


def uninstall_license() -> None:
    p = Path(license_path())
    if p.exists():
        p.unlink()
        logger.info("License uninstalled")
