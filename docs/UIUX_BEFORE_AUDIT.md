# WASender — UI/UX Before Audit (kondisi sebelum redesign, 2026-11-02)

## Shell utama (`WaSenderForm`, 1771×945)
- Navigasi = tombol absolut di top bar (`materialButton31/32/34/35`, `comboBox1` sesi, `chkDarkMode`) + `tabMain` 3 tab (Contact Sender, Group Sender, Tools/Dashboard). **Tidak ada sidebar permanen.**
- `tabMain` posisi absolut (3,64, 1765×878), tanpa Anchor → tidak ikut resize.
- `lblSection` (breadcrumb) ada di (220,30) tapi tidak pernah diisi teks (0 assignment) → mati.
- Duplikasi 10× (`_browser1..10`, grid ×10) — sumber bug + berat visual.

## Dialog & form fitur (62 form)
- Semua memakai MaterialSkin global (hijau) — konsisten dasar, tapi: warna aksen/hardcode tersebar, header tanpa pola (judul + deskripsi + aksi tidak seragam), grid polos sebelum `GridTheme` (16 tabel sudah diperbaiki).
- Ukuran ClientSize bervariasi 393–1377 px → di layar 1366 sebagian form besar (GroupLinkVerify, NumberFilter, GroupsJoiner) mepet; tidak ada auto-scale check.

## Google Maps Extractor
- Jalur aktif `NewUI`: fungsional (unlimited, filter, rating, detail, export) tapi panel tersusun vertikal seadanya, tanpa header breadcrumb, tanpa kartu area (cari/proses/hasil campur).
- `GMapExtractor.cs` lama (Selenium): mati total, UI usang — tidak disentuh (alasan: dead code, lihat inventory).

## Python/WebView (`src/wasender`)
- `main.css` 11 KB: tanpa CSS variables, tanpa token; gaya tersebar per halaman.
- `feature.js` 21 KB: satu renderer generik untuk semua modul (form generik seragam) — masalah yang dikeluhkan brief.
- Bridge `api/bridge.py`: kontrak JS↔Python — tidak diubah.

## Dashboard C# (`HelpDocs/Dashboard.html`)
- Sudah modern (hero + stat live + search + kartu deskriptif, offline). Sidebar belum ada di level aplikasi — navigasi antar modul hanya lewat kartu dashboard & top bar.
