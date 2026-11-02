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
- [x] Sidebar navy permanen + versi + active-state + breadcrumb (`WaSenderForm.Sidebar.cs`, 0 edit Designer)
- [x] Anchor fix `tabMain` + `MinimumSize` (resize/DPI)
- [x] Dashboard WebView2 + hero + stat live + search + kartu berdeskripsi
- [x] Indikator online license.pandusolusi.com (pojok kanan atas)
- [ ] Rapikan top bar (kepadatan tombol) — BUTUH UJI VISUAL, tunggu screenshot user
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
- [ ] Riwayat campaign (butuh tabel DB baru — disain dulu, jangan asal tambah)

## Fase 6 — Modul-modul lain (satu per satu, terverifikasi build)
- [x] General Settings: seksi Anti-Ban + sembunyikan Chrome path mati
- [x] About: sisa hari + support WA klik + WebView2-missing fallback
- [x] Activate: jalur kode panel online + pesan error jelas
- [x] AddMessage/AddRule: caption spintax, preview, anti-crash
- [ ] Sisa ~20 form: header konsisten + empty/error state (bertahap)
- [ ] RunSingle/RunGroup: panel progres + ringkasan hasil (baca saja, jangan ubah alur kirim)

## Fase 7 — Verifikasi & Rilis
- [x] Build Release 0 error (MSBuild) — tiap perubahan
- [x] Installer Inno Setup + tes install senyap (42 DLL, tanpa profil/chromedriver)
- [ ] Tes runtime fitur ( BUTUH USER: kirim media, GMap, aktivasi) — log/error dikirim ke saya
- [ ] Screenshot aplikasi aktual — BUTUH USER (saya tidak bisa klik GUI)
- [ ] `docs/UIUX_FINAL_REPORT.md` + `docs/UIUX_BEFORE_AFTER.md` (tulis setelah visual final dikonfirmasi user)

## Ditunda dengan alasan (jujur, bukan diabaikan)
- Rewrite total WaSenderForm/Designer: risiko merusak 100+ event handler tanpa uji visual. Dilakukan bertahap per panel.
- Peta interaktif di dalam app: butuh API key berbayar / library peta; solusi sekarang (klik 2x = buka Google Maps) gratis & andal.
- Pagination grid live: risiko data race antarthread; diganti filter + auto-stop.
- Python track rewrite: BUKAN produk yang di-ship (installer = C#). Hanya diaudit, tidak ditulis ulang.
