# WASender — UI/UX Regression Test (2026-11-02)

## Otomatis (lolos)
- [x] MSBuild Release 0 error → `bin\Release\WASender.exe` fresh
- [x] MSBuild Debug 0 error → `bin\Debug\WASender.exe` fresh
- [x] `node --check` feature.js, app.js, router.js OK
- [x] Verifikasi sisipan `GridTheme.Apply` tepat setelah `InitializeComponent()` (script scan, 28 file)
- [x] Verifikasi kontrak Python utuh: `collectFeatureParams` key & `api.feature_run` tak berubah (diff hanya shell HTML); `bridge.py` untouched
- [x] Verifikasi 0 edit Designer/resx: `git status` tidak memuat keduanya

## Manual — BUTUH USER (asisten tidak bisa klik GUI)
- [ ] Buka app: sidebar navy tampil, menu aktif ikut tab, breadcrumb benar, tidak ada overlap di 1366×768
- [ ] Resize jendela: tabMain ikut melebar (anchor fix), sidebar tetap 216px
- [ ] Klik tiap item sidebar: Dashboard/Single/Group pindah tab; Sesi/Akun/Ringkasan/Pengaturan buka dialog yang benar
- [ ] Buka tiap dialog (~27 form): grid bertema gelap-putih, tidak ada error
- [ ] GMap: strip header + judul langkah tampil, Start/Stop/filter/export tetap jalan
- [ ] Kirim single + grup 1 pesan (life test): CampaignSummary muncul, kirim tetap jalan (anti-double utuh)
- [ ] Python UI (opsional, eksperimen): `python -m wasender`, cek sidebar navy + halaman feature + tabel hasil
- [ ] Screenshot tiap hasil → simpan ke `docs/screenshots/uiux-redesign/`

## Diketahui & diterima
- Header kartu NewUI (Dock=Top 22px) berpotensi menimpa 12px atas ring progress — kosmetik minor, konfirmasi via screenshot.
- `lblSection` putih di atas MaterialSkin toolbar — warna mengikuti tema gelap/terang? Dipantau via screenshot.
