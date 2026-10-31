WASender 6.1 — WhatsApp marketing & automation (Python + WebView2)
==============================================================================

MULAI DARI SINI:
1. User/pemakai .exe      -> baca exe/README.txt (aktivasi license .lic)
2. Developer (run source)  -> jalankan .\run-dev.ps1  (butuh Python 3.11+)
3. Build .exe + installer  -> python build.py, lalu iscc installer.iss
4. Dokumentasi teknis      -> README.md (root) + docs/architecture.md + docs/license-model.md

CATATAN:
- Folder Documentation/, AppHere/, update/, exe/WASender.exe adalah peninggalan
  era .NET/CodeCanyon dan TIDAK dipakai aplikasi Python 6.x. Jangan jadikan acuan.
- samples/ berisi contoh CSV yang kompatibel dengan parser aplikasi.
- keygen/ hanya untuk DEVELOPER. private_key.pem tidak boleh di-commit (lihat .gitignore).
