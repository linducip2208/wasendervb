# WASender — Total UI/UX Redesign + Full Engineering Refactor (TODO)

Target: aplikasi desktop kelas enterprise, semua fungsi inti tetap jalan.
Aturan main: tidak ada tombol/menu palsu, tidak ada data dummy, tiap langkah terverifikasi build.

## Fase 0 — Audit & Arsitektur (WAJIB DULU)
- [x] Identifikasi entry point, framework, dan peran C# vs Python
- [x] Tulis `docs/UIUX_AUDIT.md`
- [x] Tulis `docs/ACTIVE_ARCHITECTURE.md`
- [ ] Verifikasi modul aktif vs file mati (dead code)

## Fase 1 — Design System
- [x] `Theme.cs` (token warna/typography) — sudah ada, dipakai parsial
- [x] `GridTheme.cs` — dipakai SEMUA 28 form ber-grid (bulk 1-baris per constructor, murni kosmetik)
- [x] Python `main.css`: tokens `--navy/--accent/--radius/--shadow` + sidebar navy + workspace + states
- [ ] Header standar semua form (ditunda: butuh edit Designer ×62, risiko tanpa uji klik)

## Fase 2 — Shell Aplikasi (WaSenderForm.cs + .Designer.cs)
- [x] Sidebar terang permanen (bg putih + teks navy + ikon emoji unik) + versi + active-state + breadcrumb (`WaSenderForm.Sidebar.cs`, 0 edit Designer)
- [x] Hamburger drawer bawaan dimatikan (satu-satunya navigasi = sidebar)
- [x] Brand pindah tengah atas title bar + top padding menu
- [x] Tema hijau → indigo (title, tombol, tab, grid)
- [x] Anchor fix `tabMain` + `MinimumSize` (resize/DPI)
- [x] Dashboard WebView2 + hero + stat live + search + kartu (ikon emoji, tanpa tofu FA)
- [x] Indikator online license.pandusolusi.com (pojok kanan atas)
- [x] Anti-jendela-ganda lintas sidebar+dashboard (`ToolWindows`, by Type)
- [x] Navigasi antar modul konsisten (sidebar memanggil handler existing)

## Fase 3 — Google Maps Extractor (jalur AKTIF: NewUI + Browser.cs)
- [x] Workspace 3-area berjudul (strip header + LANGKAH 1/2/3, UI-only)
- [x] Unlimited + auto-scroll + auto-panen + stop anti-macet
- [x] Filter cari, batas maks + auto-stop, filter rating, elapsed, ring progres, bar detail, klik 2x = buka peta
- [x] Export diperkuat (template resolver, try/catch, guard kosong)
- [ ] Audit thread/cancellation/logging engine (bagian 2)
- [ ] Pagination hasil (ditunda: risiko data race, filter+search sudah mencakup)
- [ ] Panel info/select-all di grid (opsional)

## Fase 4 — GMapExtractor.cs lama + Python track
- [x] Status diputuskan: `GMapExtractor.cs`/Designer MATI (bukti di inventory) — diarsipkan via dokumen, tidak dihapus, tidak di-redesign (mockup tanpa fungsi = dilarang)
- [x] `src/wasender` (Python): peran = eksperimen terpisah, tidak di-ship — didokumentasikan, TIDAK di-rewrite
- [x] `feature.js` + `main.css` + `index.html`: workspace 3-langkah + skeleton + tabel hasil data aktual + sidebar navy (kontrak bridge utuh)

## Fase 5 — Bulk Sender Workspace
- [x] Preview mockup WA + hitung segmen (dialog, dari AddMessage)
- [x] Toolbar composer (B/I/emoji/variabel/karakter) semua tab pesan
- [x] Keterangan spintax
- [x] Panel "Ringkasan Campaign" sebelum Start (total, estimasi waktu, batas harian) — `CampaignSummaryForm`, fail-open, dari `ValidateControls`/`ValidateControlsGroup`
- [x] Riwayat Pengiriman read-only dari `wa_monitor.db` (`CampaignHistoryForm`, tanpa ubah schema) — padanan jujur Campaign Manager
- [ ] Riwayat campaign per-nama (butuh tabel DB baru — disain dulu, jangan asal tambah)
- [ ] Template Pesan (butuh storage + integrasi composer — scope fitur baru, tunggu persetujuan)

## Fase 6 — Modul-modul lain (satu per satu, terverifikasi build)
- [x] General Settings: seksi Anti-Ban + sembunyikan Chrome path mati
- [x] About: sisa hari + support WA klik + WebView2-missing fallback
- [x] Activate: jalur kode panel online + pesan error jelas
- [x] AddMessage/AddRule: caption spintax, preview, anti-crash
- [ ] Sisa ~20 form: header konsisten + empty/error state (bertahap)
- [ ] RunSingle/RunGroup: panel progres + ringkasan hasil (baca saja, jangan ubah alur kirim)

## Fase 7 — Verifikasi & Rilis
- [x] Build Release 0 error (MSBuild) — tiap perubahan
- [x] Build Debug 0 error — terverifikasi ulang
- [x] Installer Inno Setup + tes install senyap (tanpa profil/chromedriver)
- [x] Audit wiring: 0 tombol mati di 63 form (semua ada handler Click)
- [x] Smoke test: app start, jendela utama tampil, crash.log tidak nambah
- [ ] Tes runtime fitur ( BUTUH USER: kirim media/video, GMap, init WA, multi-akun) — log/error dikirim ke saya
- [ ] Screenshot aplikasi aktual — BUTUH USER (saya tidak bisa klik GUI)
- [ ] `docs/UIUX_FINAL_REPORT.md` + `docs/UIUX_BEFORE_AFTER.md` (tulis setelah visual final dikonfirmasi user)

## Fase 8 — Stabilitas engine & init (sesi berjalan)
- [x] Init WA gagal bersuara + sekali per form + tanpa freeze UI (`GetGroupMember`)
- [x] Auto-init saat form dibuka (tanpa klik bila sesi sehat)
- [x] `isListReady` benar-benar tunggu sinkron chat (dulu return instan palsu)
- [x] Auto scroll-sync grup bila list kosong (tanpa buka chat manual)
- [x] Probe diagnostik WPP (`ProbeGroupsApi`) + pesan penyebab kosong
- [x] Retry baca grup 2x anti sinkronisasi lambat
- [x] WebScrapper: User-Agent browser + hitung gagal + URL tanpa skema otomatis https + anti-null href + timeout 15 dtk
- [x] SocialMediaExtractor: port WebView2 + parser SERP tanpa class usang + consent + CAPTCHA flow (butuh uji live Google)
- [x] Singleton browser anti-disposed + sesi Load tersinkron (`ManageAccounts`)
- [x] Video tanpa nama file diperbaiki 3 titik (`RunSingle`)
- [ ] Hasil uji live user untuk semua di atas (menunggu laporan)

## Ditunda dengan alasan (jujur, bukan diabaikan)
- Rewrite total WaSenderForm/Designer: risiko merusak 100+ event handler tanpa uji visual. Dilakukan bertahap per panel.
- Peta interaktif di dalam app: butuh API key berbayar / library peta; solusi sekarang (klik 2x = buka Google Maps) gratis & andal.
- Pagination grid live: risiko data race antarthread; diganti filter + auto-stop.
- Python track rewrite: BUKAN produk yang di-ship (installer = C#). Hanya diaudit, tidak ditulis ulang.
