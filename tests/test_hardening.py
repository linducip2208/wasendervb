"""Regression tests for hardening fixes (audit batch 2)."""
from __future__ import annotations

import base64
import json
from datetime import date, timedelta

import pytest

from wasender.core import db, license as license_mod
from wasender.core.license import LicenseError, parse_license_text
from wasender.features.base import render_template


def _armor(blob: str) -> str:
    return (
        "-----BEGIN WASENDER LICENSE-----\n"
        + "\n".join(blob[i : i + 64] for i in range(0, len(blob), 64))
        + "\n-----END WASENDER LICENSE-----\n"
    )


def _sign(private_key, payload: dict) -> str:
    payload_bytes = json.dumps(payload, separators=(",", ":"), sort_keys=True).encode()
    sig = private_key.sign(payload_bytes)
    return _armor(
        base64.b64encode(payload_bytes).decode()
        + "."
        + base64.b64encode(sig).decode()
    )


def _valid_payload(**over):
    p = {
        "license_id": "WAS-TEST-0000-0000",
        "hwid": "a" * 64,
        "customer": "Test",
        "tier": "lifetime",
        "features": ["*"],
        "issued_at": date.today().isoformat(),
        "expires_at": None,
        "version": 1,
    }
    p.update(over)
    return p


def test_template_no_chained_expansion():
    out = render_template("{a}", {"a": "{b}", "b": "Evil"})
    assert out == "{b}"


def test_template_missing_left_intact():
    assert render_template("Hi {x}!", {}) == "Hi {x}!"


def test_license_rejects_bad_expires(dev_keypair):
    armored = _sign(dev_keypair, _valid_payload(expires_at="not-a-date"))
    with pytest.raises(LicenseError):
        parse_license_text(armored)


def test_license_rejects_bad_features(dev_keypair):
    armored = _sign(dev_keypair, _valid_payload(features="*"))
    with pytest.raises(LicenseError):
        parse_license_text(armored)


def test_license_rejects_bad_issued_at(dev_keypair):
    armored = _sign(dev_keypair, _valid_payload(issued_at="besok"))
    with pytest.raises(LicenseError):
        parse_license_text(armored)


def test_db_rejects_unknown_table(tmp_app_data):
    with pytest.raises(ValueError, match="Unknown table"):
        db.insert("users", {"name": "x"})
    with pytest.raises(ValueError, match="Unknown table"):
        db.update("users", 1, {"name": "x"})
    with pytest.raises(ValueError, match="Unknown table"):
        db.delete("users", 1)


def test_increment_resets_stale_counter(tmp_app_data):
    yesterday = (date.today() - timedelta(days=1)).isoformat()
    acc = db.insert("accounts", {
        "label": "A", "profile_dir": "/tmp/p", "daily_quota": 200,
        "sent_today": 10, "last_reset": yesterday,
    })
    from wasender.core import ratelimit
    ratelimit.increment_sent(acc)
    row = db.query_one("SELECT sent_today, last_reset FROM accounts WHERE id = ?", (acc,))
    assert row["sent_today"] == 1
    assert row["last_reset"] == date.today().isoformat()


def test_config_clamps(tmp_app_data):
    from wasender.core import config as config_mod
    db.set_setting("delay_min_seconds", "30")
    db.set_setting("delay_max_seconds", "5")
    db.set_setting("daily_quota_per_account", "0")
    cfg = config_mod.load()
    assert cfg.delay_min_seconds == 5
    assert cfg.delay_max_seconds == 30
    assert cfg.daily_quota_per_account == 1
