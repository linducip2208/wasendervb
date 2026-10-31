"""Pytest fixtures for WASender test suite.

Provides:
    - tmp_app_data: per-test isolated app data dir (DB, license, sessions written here).
    - dev_keypair: session-scoped Ed25519 keypair, replaces embedded public key so
      tests can sign payloads with the matching private key without needing the
      real `keygen/private_key.pem`.
"""
from __future__ import annotations

import sys
from pathlib import Path

import pytest

# Make the `src/` layout importable when running `pytest` from repo root.
_SRC = Path(__file__).resolve().parent.parent / "src"
if str(_SRC) not in sys.path:
    sys.path.insert(0, str(_SRC))

from cryptography.hazmat.primitives.asymmetric.ed25519 import (
    Ed25519PrivateKey,
    Ed25519PublicKey,
)


@pytest.fixture
def tmp_app_data(tmp_path, monkeypatch):
    """Redirect every path resolver onto a clean tmp dir + reset DB module state.

    The DB module caches a sqlite connection in a thread-local and a module-level
    ``_INITIALIZED`` flag. Both must be reset between tests so each test gets a
    fresh database under ``tmp_path``.
    """
    from wasender.core import paths as paths_mod
    from wasender.core import db as db_mod

    app_dir = tmp_path / "WASender"
    app_dir.mkdir(parents=True, exist_ok=True)

    monkeypatch.setattr(paths_mod, "app_data_dir", lambda: app_dir)

    # Reset cached DB state so the next call re-initialises against the tmp path.
    db_mod._INITIALIZED = False
    if hasattr(db_mod._LOCAL, "conn"):
        try:
            db_mod._LOCAL.conn.close()
        except Exception:
            pass
        del db_mod._LOCAL.conn

    yield app_dir

    # Teardown: close any connection opened during the test.
    if hasattr(db_mod._LOCAL, "conn"):
        try:
            db_mod._LOCAL.conn.close()
        except Exception:
            pass
        del db_mod._LOCAL.conn
    db_mod._INITIALIZED = False


@pytest.fixture(scope="session")
def dev_keypair():
    """Generate an Ed25519 keypair and patch the verifier to use the public half.

    Returns the private key so tests can sign their own payloads. The session
    scope is intentional: ``load_public_key`` is wrapped in ``lru_cache``, so we
    monkey-patch the *function* (not the cached return value) once and leave it
    in place for the whole run.
    """
    from wasender.core import crypto as crypto_mod

    private_key = Ed25519PrivateKey.generate()
    public_key: Ed25519PublicKey = private_key.public_key()

    # Drop any prior cached value, then replace the loader.
    try:
        crypto_mod.load_public_key.cache_clear()
    except AttributeError:
        pass

    original_loader = crypto_mod.load_public_key
    crypto_mod.load_public_key = lambda: public_key  # type: ignore[assignment]

    yield private_key

    crypto_mod.load_public_key = original_loader  # type: ignore[assignment]
