# WASender v6.1 — Catatan Rilis (1 Nov 2026)

Installer: `dist\WASender-Setup-6.1.exe` (±11 MB). Dibangun dari source C# (`CodeHere\WASender`, .NET Framework 4.8).

## Yang baru di versi ini

**Lisensi online (license.pandusolusi.com)**
- Aktivasi pakai kode `XXXX-XXXX-XXXX-XXXX` dari panel web (menu Activate).
- 1 lisensi = 1 PC (kunci HWID otomatis saat aktivasi pertama).
- About menampilkan sisa hari (merah ≤7, oranye ≤30, hijau), tanggal mulai aktif, "Unlimited" untuk lifetime.
- Indikator Online/Offline ke server di kanan atas (klik untuk cek ulang).
- Tanpa internet tetap jalan (grace offline); lisensi yang dihapus di server ditanyakan ulang saat aplikasi dibuka.
- Kontak support di About: Pandusolusi.com — WA 6281296052010.

**Pulihkan laporan kirim & anti-double**
- Pesan terkirim tidak lagi dilaporkan Failed (verifikasi = pesan masuk chat, bukan harus sudah dibaca HP).
- Satu pesan tidak akan terkirim 2x (tiap lapisan berhenti begitu API WA menerima).

**Kirim media**
- Foto/video/dokumen dikirim sebagai data-URL valid + tipe eksplisit, file besar dipecah otomatis, timeout menyesuaikan ukuran. Berlaku single, grup, broadcast.

**Spintax**
- Format `{opsi1|opsi2|...}` + nested, berlaku di semua pesan. Ada keterangan + contoh di bawah kolom pesan dan halaman Tutorial.

**GMap unlimited + export kuat**
- Scroll + panen otomatis, tanpa batas 20, berhenti sendiri saat halaman habis.
- Export Excel tahan semua kasus (template hilang, path beda, data kosong).

**Dashboard & tampilan**
- Dashboard WebView2 (bukan IE lagi), ikon offline, strip status live (lisensi/terkirim/antrian/kesehatan/sesi/update), deskripsi tiap fitur, box Tutorial + DailySummary (form terpisah).
- General Settings: seksi Anti-Ban (limit harian + jeda), tanpa tombol Chrome Driver legacy.

## Syarat PC customer

1. Windows 10 (1903+) / 11 64-bit.
2. .NET Framework 4.8 (installer mengecek otomatis, diarahkan download bila belum ada).
3. Microsoft Edge WebView2 Runtime (installer mengecek otomatis; kebanyakan Win10/11 sudah bawaan).

## Cara aktivasi untuk customer

1. Install → buka WASender → form Activate muncul.
2. Salin "Your Activation Code" (fingerprint PC) — tidak wajib dikirim dulu.
3. Minta kode lisensi ke admin → paste di kolom kunci → Activate Now.
4. Cek About untuk sisa masa aktif. Pindah PC: minta admin revoke HWID lama di panel, aktivasi ulang.
