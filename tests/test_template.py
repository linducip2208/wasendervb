"""Tests for the simple {var} template renderer used by messaging features."""
from __future__ import annotations

from wasender.features.base import render_template


def test_render_simple_var():
    assert render_template("Hi {name}", {"name": "Bob"}) == "Hi Bob"


def test_render_missing_var_left_intact():
    out = render_template("Hello {nope}", {"name": "Bob"})
    assert out == "Hello {nope}"


def test_multiple_vars():
    template = "Halo {name}, no kamu {phone} — diskon untuk {tier}!"
    ctx = {"name": "Alice", "phone": "628111", "tier": "VIP"}
    assert render_template(template, ctx) == "Halo Alice, no kamu 628111 — diskon untuk VIP!"
