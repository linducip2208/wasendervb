# WASender — UI/UX Redesign Final (2026-11-02)

## Yang diubah (UI-ONLY, fitur & logic 100% sama)

### C# WinForms
1. **Shell enterprise (`WaSenderForm.Sidebar.cs` BARU, 0 edit Designer)** — sidebar navy permanen 216px (logo + versi + 2 seksi: KIRIM PESAN / APLIKASI), active-state hijau mengikuti tab, breadcrumb `lblSection` dihidupkan ("Single Sender / ...", "Dashboard / ..."), `tabMain` anchor penuh (resize fix), `MinimumSize` 1100×700. Semua tombol hanya memanggil handler yang sudah ada.
2. **GridTheme ke 27 form** — `GridTheme.Apply` 1 baris per constructor (AddPoll, BusinessExtractor, GroupLinkVerify, GroupMemberAdder, GroupsJoiner, NumberFilter, RunWarmer, RunGroupWarmer, OldClassic, QuickFilter, WebScrapper, Warmer, SocialMediaExtractor, SingleLauncher, KeyMarker, BuiltInVariable, GroupFinder, GoogleCSVGenerator, GenerateNumbers + 7 yang sudah ada). Murni kosmetik (cek `GridTheme.cs`: hanya warna/font, tanpa ubah SelectionMode/ReadOnly).
3. **NewUI workspace 3-area** — strip header navy + header langkah per kartu (`PolishWorkspace`, fail-soft). Struktur kartu sudah ada (browser / parameter+proses / hasil), tinggal penegasan visual.

### Python/WebView (tidak di-ship, eksperimen)
4. **`index.html`** — sidebar: brand WA + label menu (SVG tetap, `data-page` & router tak berubah).
5. **`main.css`** — design tokens (`--navy`, `--accent`, `--radius`, `--shadow`), sidebar navy + collapsed rail, `.ws-grid` workspace, `.empty-state`, `.skeleton`, `.table-wrap` sticky header, `.pager`. Append-only; selektor lama utuh.
6. **`feature.js`** — shell halaman jadi breadcrumb + workspace 3-langkah (ID `f-*`, `collectFeatureParams`, `api.feature_run` TAK BERUBAH), skeleton saat running, `renderDataTable` generik (array-of-objects aktual, maks 8 kolom × 200 baris, tanpa dummy).

### Dokumen
- `docs/UIUX_FILE_INVENTORY.md`, `docs/UIUX_BEFORE_AUDIT.md`, `docs/UIUX_REDESIGN_PLAN.md` (audit)
- `docs/UIUX_REDESIGN_FINAL.md`, `docs/UIUX_FILE_CHANGES.md`, `docs/UIUX_REGRESSION_TEST.md` (file ini)

## Yang SENGAJA tidak diubah (alasan jujur)
- `GMapExtractor.cs`/Designer (mati — 0 referensi selain komentar): me-redesign form yang tak bisa dibuka = mockup tanpa fungsi.
- Nol edit `*.Designer.cs`: 80 event wiring `WaSenderForm` rawan rusak tanpa uji klik.
- Bridge Python, DB schema, API lisensi, engine kirim/scrape: untouched (verifikasi: `git diff --stat` hanya file UI + docs).

## Build & test
- MSBuild Release → `bin\Release\WASender.exe` 0 error. Debug → `bin\Debug\WASender.exe` 0 error.
- `node --check` semua JS yang disentuh: OK.
- Regression perilaku: butuh user (runtime GUI). Lihat `docs/UIUX_REGRESSION_TEST.md`.
