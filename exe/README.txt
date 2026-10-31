WASender — Distribution Folder
================================

Folder ini berisi binary siap-distribusi.

WASender.exe
------------
- Aplikasi utama untuk customer
- Single-file, ~75 MB
- Pertama kali run: akan minta license activation
- Customer flow:
  1. Double-click WASender.exe -> license activation screen
  2. Copy HWID (16 char hex) dari layar
  3. Kirim HWID ke kamu via WhatsApp/email
  4. Kamu generate license pakai WASenderKeygen.exe (folder keygen-developer-only/)
  5. Kirim balik file .lic ke customer
  6. Customer paste isi .lic ke text area, klik Activate
  7. Restart aplikasi -> akun WhatsApp bisa di-add & fitur jalan

System requirements:
- Windows 10 (1803+) atau Windows 11
- WebView2 Runtime (sudah pre-installed di Win10/11 baru)
- Internet untuk WhatsApp Web

keygen-developer-only/
----------------------
JANGAN distribusi folder ini ke customer!

- WASenderKeygen.exe -> GUI untuk generate license
- private_key.pem    -> Ed25519 private key, RAHASIA
                       (kalau bocor: anyone bisa palsu license)

Cara pakai keygen:
1. Pastikan private_key.pem ada di folder yang sama dengan WASenderKeygen.exe
2. Run WASenderKeygen.exe
3. Input HWID customer + nama + tier + expiry
4. Output: file .lic di subfolder issued/
5. Kirim file .lic ke customer

Backup private_key.pem ke USB encrypted offline. JANGAN simpan di Google Drive
plain, JANGAN commit ke git, JANGAN share screenshot folder ini.

Kalau private_key.pem bocor:
- Generate keypair baru (run python keygen/generate_keys.py dari source)
- Re-build WASender.exe dengan public_key.pem yang baru
- Re-issue license untuk semua customer existing
- Kirim update .exe + .lic baru ke semua customer
