"""Ed25519 signature verification (client-side). Public key embedded; private key is dev-only."""
from __future__ import annotations

import base64
from functools import lru_cache
from pathlib import Path

from cryptography.exceptions import InvalidSignature
from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PublicKey

from .paths import public_key_path


@lru_cache(maxsize=1)
def load_public_key() -> Ed25519PublicKey:
    pem = Path(public_key_path()).read_bytes()
    key = serialization.load_pem_public_key(pem)
    if not isinstance(key, Ed25519PublicKey):
        raise ValueError("Embedded public key is not Ed25519")
    return key


def verify_signature(payload: bytes, signature_b64: str) -> bool:
    try:
        sig = base64.b64decode(signature_b64, validate=True)
        load_public_key().verify(sig, payload)
        return True
    except InvalidSignature:
        return False
    except Exception as e:
        from .logger import logger
        logger.warning(f"Signature verify error ({type(e).__name__}): {e}")
        return False
