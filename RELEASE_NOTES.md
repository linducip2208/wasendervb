# WASender v6.1 — Catatan Rilis (2 Nov 2026)

Installer: `dist\WASender-Setup-6.1.exe` (±11 MB). Dibangun dari source C# (`CodeHere\WASender`, .NET Framework 4.8).

## Yang baru di versi ini

**Lisensi online (license.pandusolusi.com) + panel web (repo terpisah: github.com/linducip2208/license)**
- Aktivasi pakai kode `XXXX-XXXX-XXXX-XXXX` dari panel web (menu Activate). Panel: kelola lisensi (CRUD, renew, suspend, regenerate, bulk, CSV/Excel), batas aktivasi per kode, verifikasi HWID via REST API, token Sanctum, log aktivitas, role admin/reseller, checkout Tripay + QRIS manual.
- 1 lisensi = 1 PC (kunci HWID otomatis saat aktivasi pertama); aktivasi pindah PC = revoke HWID lama di panel.
- About menampilkan sisa hari (merah ≤7, oranye ≤30, hijau), tanggal mulai aktif, "Unlimited" untuk lifetime.
- Indikator Online/Offline ke server di kanan atas (klik untuk cek ulang; cek otomatis hanya saat aplikasi dibuka).
- Tanpa internet tetap jalan (grace offline); lisensi yang dihapus/ditangguhkan di server ditanyakan ulang saat aplikasi dibuka.
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
- Dashboard WebView2 premium: hero + sapaan + tanggal + pencarian fitur, stat live (lisensi/terkirim/antrian/kesehatan/sesi/update), quick action, feed aktivitas, 22 kartu kategori dgn ikon offline.
- Grid konsisten: header emerald + baris belang di 16 tabel (kontak, status kirim, GMap, auto-reply, dsb).
- Form Ringkasan Harian terpisah (stat + AI + update + log + riwayat 7 hari), Tutorial offline terpisah.
- General Settings: seksi Anti-Ban (limit harian + jeda Min/Max), tanpa tombol Chrome Driver legacy, MinimumSize jendela.
- GMap Extractor: filter Cari, batas Maks auto-stop, filter rating, elapsed timer, ring progres, bar detail, klik 2x = buka lokasi di Google Maps (tanpa API key).
- Composer pesan: toolbar B/I/emoji/variabel + hitung karakter & segmen otomatis di semua tab pesan; dialog Preview mockup chat WA + uji acak spintax; keterangan format spintax di tiap form tulis pesan.
- Jaring pengaman global: error tak terduga jadi pesan ramah + tercatat di crash.log (bukan dialog JIT).

## Syarat PC customer

1. Windows 10 (1903+) / 11 64-bit.
2. .NET Framework 4.8 (installer mengecek otomatis, diarahkan download bila belum ada).
3. Microsoft Edge WebView2 Runtime (installer mengecek otomatis; kebanyakan Win10/11 sudah bawaan).

## Cara aktivasi untuk customer

1. Install → buka WASender → form Activate muncul.
2. Salin "Your Activation Code" (fingerprint PC) — tidak wajib dikirim dulu.
3. Minta kode lisensi ke admin → paste di kolom kunci → Activate Now.
4. Cek About untuk sisa masa aktif. Pindah PC: minta admin revoke HWID lama di panel, aktivasi ulang.
