"""One-time keypair generation. Run sekali aja, simpan private_key.pem aman."""
from __future__ import annotations
import sys
from pathlib import Path

from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey


def _runtime_dir() -> Path:
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent


def main() -> int:
    here = _runtime_dir()
    priv_path = here / "private_key.pem"
    # Saat frozen: ga ada folder src/wasender/core/, taruh public_key di sebelah exe
    if getattr(sys, "frozen", False):
        pub_path = here / "public_key.pem"
    else:
        pub_path = here.parent / "src" / "wasender" / "core" / "public_key.pem"

    if priv_path.exists():
        print(f"[!] {priv_path} already exists. Aborting to avoid overwriting.")
        print("    Delete manually if you really want to rotate keys.")
        return 1

    private_key = Ed25519PrivateKey.generate()
    public_key = private_key.public_key()

    priv_pem = private_key.private_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PrivateFormat.PKCS8,
        encryption_algorithm=serialization.NoEncryption(),
    )
    pub_pem = public_key.public_bytes(
        encoding=serialization.Encoding.PEM,
        format=serialization.PublicFormat.SubjectPublicKeyInfo,
    )

    priv_path.write_bytes(priv_pem)
    pub_path.parent.mkdir(parents=True, exist_ok=True)
    pub_path.write_bytes(pub_pem)

    print(f"[OK] Private key  -> {priv_path}")
    print(f"[OK] Public  key  -> {pub_path}")
    print()
    print("WARNING: private_key.pem must NEVER be committed or distributed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
