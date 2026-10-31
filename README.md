# WASender

WhatsApp marketing & automation suite untuk Windows 10/11.

Dibangun dengan **Python + WebView2** sebagai single-file `.exe` desktop app.
License system offline (Ed25519 signed) — keygen hanya di developer's PC.

## Features

### Data Extractors
- Get Group Member — extract list anggota dari group
- Contact List Grabber — grab semua kontak di akun
- Grab Active Group Members — filter member yg aktif chat
- Grab Chat List — grab list chat (private + group)
- Get Poll Results — extract hasil polling

### Group Tools
- Grab WhatsApp Group Links from web — scrape link group dari URL
- Auto Group Joiner — auto-join dari list link
- Bulk Add Group Members — add banyak member sekaligus
- Group Finder — cari group via Google dorking
- Bulk Group Generator — bikin banyak group sekaligus

### Automations & Filtration
- WhatsApp Auto Responder Bot — auto reply by keyword
- WhatsApp Number Filter — cek nomor aktif/tidak di WA
- WhatsApp Warmer — warmup akun (anti-ban)
- WhatsApp Group Warmer — warmup di group
- All Schedules — manage scheduled tasks

### Lead Generation
- Google Map Data Extractor
- Website Email/Mobile Extractor
- Social Media Data Extractor
- Google Contacts CSV Generator

### Messaging
- Bulk Sender — mass message dengan template variabel + media + delay random

## System Requirements

- Windows 10 (1803+) atau Windows 11
- WebView2 Runtime (sudah pre-installed di Win10 1803+ dan Win11)
- Internet connection untuk WhatsApp Web

## Build dari source

```powershell
# 1. Install Python 3.11+ dari python.org
# 2. Install dependencies
pip install -r requirements.txt

# 3. (Developer only — sekali aja) Generate keypair untuk license
python keygen/generate_keys.py
# → private_key.pem (simpan, JANGAN distribusi)
# → src/wasender/core/public_key.pem (embed di binary)

# 4. Run dev mode
python -m wasender

# 5. Build .exe
python build.py
# → dist/WASender.exe
```

## License Model

- **Offline RSA/Ed25519:** client validate signature pakai public key embedded
- **HWID-bound:** 1 license = 1 PC (CPU + Motherboard + Disk + MAC fingerprint)
- **Keygen at developer side only** — `keygen/keygen.py` dengan private key
- **Optional revocation server:** belum included di v1, tinggal extend kalau perlu

## Anti-Ban Best Practice

WhatsApp aktif banned akun yang dipakai bulk messaging. Built-in mitigations:

- Random delay 3-15 detik antar pesan (configurable)
- Daily message cap (default 200/akun)
- Mandatory warmup mode untuk akun baru
- Multi-akun rotation
- Typing simulation

**Disclaimer:** Penggunaan tool ini untuk spam tanpa consent melanggar ToS WhatsApp. User bertanggung jawab atas penggunaan.

## Disclaimer

This tool is for legitimate marketing & business communication. Misuse may result in WhatsApp account ban. The developer is not responsible for any account suspension or legal consequences.
