# WASender — UI/UX Redesign Plan (UI-only, fitur & logic 100% dipertahankan)

## Prinsip eksekusi
1. **Nol sentuh Designer.cs** — semua perubahan shell via injeksi kode runtime (partial class), fail-soft try/catch. Alasan: 176 Controls.Add + 80 event wiring di `WaSenderForm.Designer.cs`; edit manual berisiko merusak layout tanpa uji klik.
2. **Event handler tidak diubah** — sidebar/topbar hanya *memanggil* handler yang sudah ada (`materialButton31_Click_1`, `materialButton32_Click`, `materialButton34_Click`, `materialButton35_Click`, `tabMain.SelectedIndex`).
3. **Kontrak tidak berubah**: bridge Python, DB schema, API lisensi, engine kirim/scrape.
4. **Dead code tidak di-redesign**: `GMapExtractor.cs`/Designer + `AccountSessionControl` (bukti mati di inventory). Alasan jujur: me-redesign form yang tidak bisa dibuka user = mockup tanpa fungsi.
5. **Python track**: design tokens (CSS variables) di `main.css` + layout per-modul di `feature.js` TANPA ubah nama method/payload bridge.

## Tahap
- [x] T0 Audit + inventory + before + plan (file ini)
- [ ] T1 Shell: sidebar navy permanen + breadcrumb + anchor fix (`WaSenderForm.Sidebar.cs` baru, 0 edit Designer)
- [ ] T2 `FormStyler` runtime: header + padding standar semua dialog (tanpa sentuh Designer)
- [ ] T3 `NewUI` workspace 3-area (header breadcrumb + kartu Cari/Proses/Hasil)
- [ ] T4 Python: design tokens `main.css` + layout per-modul `feature.js`
- [ ] T5 Build Debug + Release + installer + regression doc + final report
- [ ] T6 Screenshot aplikasi aktual — BUTUH USER (asisten tidak bisa klik GUI)

## Verifikasi tiap tahap
MSBuild Release 0 error → commit → push. Tidak ada klaim tanpa build.
