# WASender Keygen — DEVELOPER ONLY

**JANGAN distribusi folder ini ke customer. JANGAN commit `private_key.pem`.**

## First-time setup

```powershell
# Generate RSA keypair (sekali aja, ever)
python generate_keys.py
```

Output:
- `private_key.pem` — simpan AMAN di PC kamu, jangan share, jangan upload
- `../src/wasender/core/public_key.pem` — embed di client binary (otomatis)

## Issue license untuk customer

```powershell
python keygen.py
```

GUI akan muncul. Input:
- **HWID** — minta customer kirim HWID mereka via `WASender → About → HWID`
- **Customer name** — buat tracking
- **Tier** — lifetime / yearly / monthly / trial
- **Features** — `*` untuk all, atau list specific (`bulk_send,extractor`)
- **Expiry** — kalau bukan lifetime

Output: file `.lic` di `issued/<license_id>.lic`. Kirim ke customer.

## License key format

Internal: JSON payload + Ed25519 signature, base64-armored:

```
-----BEGIN WASENDER LICENSE-----
<base64 of payload + signature>
-----END WASENDER LICENSE-----
```

Payload contains: `license_id`, `hwid`, `customer`, `tier`, `features`, `issued_at`, `expires_at`.

Client validates signature offline pakai embedded public key. Tanpa internet pun bisa.

## Security notes

- Private key yang bocor = anyone bisa generate license. **Encrypt at rest minimum.** Pakai `cryptography.fernet` atau OS-level (Windows DPAPI) kalau mau extra.
- Keep backup di offline storage (USB encrypted, ya jangan di Google Drive plain).
- Kalau private key bocor: rotate. Generate baru, semua customer dapat license baru, ship updated `.exe` dengan public key baru.
