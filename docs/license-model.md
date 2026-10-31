# License Model

WASender ships with an **offline, hardware-bound license system** based on Ed25519 signatures. This document explains the choices, the trade-offs, and the operational flow.

## Why Ed25519 (not RSA)

| Aspect | Ed25519 | RSA-2048 |
|---|---|---|
| Signature size | 64 bytes | 256 bytes |
| Verify speed | ~70k ops/s | ~5–10k ops/s |
| Key size | 32 bytes priv / 32 pub | ~1.2 KB priv / ~256 B pub |
| Determinism | Deterministic (RFC 8032) | Randomised (PSS) — needs CSPRNG |
| Side-channel | Constant-time by design | Implementation-dependent |
| Maturity | Stable, widely adopted | Older, broader compatibility |

For a desktop app that verifies a single small payload at startup, Ed25519 wins on every axis we care about: tiny `.lic` files, instant verification, and a public key that fits in a 64-character PEM. RSA's compatibility advantage is irrelevant here because we control both ends (keygen + verifier).

## Why HWID-bind

Without HWID binding, one paying customer can post their `.lic` file to a forum and the entire userbase converts. Binding to a stable hardware fingerprint (BIOS UUID + motherboard serial + disk serial + MAC + CPU arch, hashed with SHA-256) means a leaked file is useless on any other machine. Customers who legitimately replace hardware just request a re-issue — the friction is small for honest users, large for pirates.

## Why offline verification (no phone-home)

- **Works without internet.** Sales agents in the field, on flight wifi, or at a kiosk: the app must boot and send messages.
- **No central failure point.** A blocked DNS, an expired TLS cert, or a downed license server cannot brick paying customers.
- **No customer telemetry.** Nothing about customer usage leaves the machine. This is a meaningful trust signal in a privacy-aware market.

The trade-off: revocation is harder. Once a `.lic` is in the wild, we cannot remotely disable it. Mitigations are short-dated tiers (monthly/yearly), HWID binding, and (if needed) a future blocklist shipped with app updates.

## Limitations & failure modes

- **If `private_key.pem` leaks**, anyone can mint valid licenses. Recovery requires:
  1. Generate a fresh keypair (`keygen/generate_keys.py`).
  2. Rotate the embedded `src/wasender/core/public_key.pem` to the new public key.
  3. Ship a new app version. Old installs with the old public key keep accepting old licenses (degraded, but not broken).
  4. Re-issue licenses to legitimate customers from the new keygen.
- **If a customer's hardware fails**, their HWID changes. Process: customer reports new HWID via About panel → vendor re-issues `.lic` against the new HWID. Old file is silently invalidated by the HWID check.
- **System clock manipulation.** A determined customer can roll the clock back to evade `expires_at`. We accept this — it's a low-impact attack and detecting it requires phone-home, which we deliberately rejected.

## Customer activation flow

1. **Install** the WASender installer.
2. On first launch the **Activation screen** appears. The user opens **About** → copies their **HWID** (64-char hex; or the friendly 16-char short form for emails/chats).
3. Customer sends HWID + payment proof to the vendor (via WhatsApp, email, or web form).
4. **Vendor runs `keygen/keygen.py`** on the developer machine, pastes the HWID, picks tier + expiry, hits **Generate**. A `.lic` file is written to `keygen/issued/`.
5. Vendor emails/sends the `.lic` file back to the customer.
6. Customer **pastes the file contents** into the activation textarea (or browses to the file). The app verifies signature + HWID, persists the file at `%APPDATA%\WASender\license.lic`, and unlocks the main UI.
7. Subsequent launches reload from disk silently.

## Backup strategy for the private key

The private key is the entire kingdom. Treat it accordingly.

- **Never** commit `keygen/private_key.pem` to git. The repo's `.gitignore` covers `keygen/private_key.pem`, `keygen/issued/`, and `keygen/licenses.json`.
- Store the canonical copy on an **encrypted USB drive** kept offline (BitLocker To Go, VeraCrypt, or hardware-encrypted drive).
- Keep a second copy on a separate encrypted drive in a different physical location.
- The keygen machine should be a developer workstation that does not auto-sync the `keygen/` folder to any cloud drive (OneDrive, Dropbox, iCloud).
- Optionally: import the key into a hardware token (YubiKey FIDO2 or PGP-capable smartcard) so it never sits on a hot disk at all.
- Maintain `keygen/licenses.json` (auto-appended by the keygen) as the source-of-truth ledger of who got what — back this up alongside the key.
