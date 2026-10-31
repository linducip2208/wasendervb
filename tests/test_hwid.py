"""Tests for the hardware fingerprint module."""
from __future__ import annotations

import re

from wasender.core import hwid as hwid_mod


HEX_RE = re.compile(r"^[0-9a-f]+$")


def test_hwid_stable_across_calls():
    # Clear any cached value so we exercise the actual probe at least once,
    # then call twice and assert determinism on the same machine.
    hwid_mod.get_hwid.cache_clear()
    first = hwid_mod.get_hwid()
    second = hwid_mod.get_hwid()
    assert first == second


def test_hwid_format():
    hwid_mod.get_hwid.cache_clear()
    h = hwid_mod.get_hwid()
    assert len(h) == 64
    assert HEX_RE.match(h), f"HWID is not lowercase hex: {h!r}"


def test_hwid_short_format():
    hwid_mod.get_hwid.cache_clear()
    short = hwid_mod.get_hwid_short()
    assert len(short) == 16
    assert short == short.upper()
    # Uppercase hex characters only.
    assert re.match(r"^[0-9A-F]{16}$", short), f"short HWID not hex: {short!r}"
    # And it must be a prefix of the full digest (case-insensitive).
    assert hwid_mod.get_hwid().startswith(short.lower())
