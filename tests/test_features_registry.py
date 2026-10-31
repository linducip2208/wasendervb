"""Tests for the feature registry — catalog completeness and dispatch errors."""
from __future__ import annotations

import pytest


def test_registry_has_all_19_features():
    import wasender.features as features  # populates the registry on import

    cat = features.catalog()
    assert isinstance(cat, list)
    assert len(cat) >= 19, f"expected >= 19 features, got {len(cat)}: {[c['key'] for c in cat]}"


def test_features_have_required_metadata():
    import wasender.features as features

    for entry in features.catalog():
        assert entry.get("key"), f"feature missing key: {entry!r}"
        assert entry.get("label"), f"feature missing label: {entry!r}"
        assert entry.get("category"), f"feature missing category: {entry!r}"
        # description is optional but the field should be present
        assert "description" in entry


def test_run_unknown_feature_raises():
    import wasender.features as features

    with pytest.raises(KeyError):
        features.run("nonexistent_feature_xyz", {})
