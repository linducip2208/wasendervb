# WASender — UI/UX Audit (hasil inspeksi kode, bukan tebakan)

Tanggal: 2026-11-02. Metode: baca langsung seluruh source (C# + Python + HTML/JS/CSS).

## 1. Arsitektur aktif (fakta)

| Lapisan | Realita |
|---|---|
| Produk yang di-ship | **C# WinForms, .NET Framework 4.8**, entry `CodeHere/WASender/Program.cs` → `WaSenderForm`. Build: MSBuild Release. Installer: `installer.iss` (Inno Setup) memaketkan `CodeHere/WASender/bin/Release` |
| UI framework | WinForms + **MaterialSkin.2** (tema hijau), `System.Windows.Forms.DataGridView` polos di ~30 form (sebelum GridTheme) |
| Browser modern | **WebView2** (`Microsoft.Web.WebView2` 1.0.3967.48): dashboard (`webBrowser11`), `Browser.cs` (scraper/GMap), `MainUC.webView21` (sesi WA per tab di `WaSenderBrowser`) |
| Browser legacy (mati) | `System.Windows.Forms.WebBrowser` (IE) masih dipakai sbg preview HTML statis (`webBrowser1..10` + `setBrowserDefaultHtml`); Selenium/`WebDriver.dll` + `chromedriver.exe` sudah **non-aktif runtime** (`browserType` dipaksa `2` = WebView2, `Utils.SetDriver()` = no-op) |
| Bridge WA aktif | `WPPHelper` parsial (`Bridge/WppBridge*.cs`) → `ExecuteScriptAsync` + `wppconnect-wa.js` (wa-js yg masih dirawat). `WapiBridge.cs` (~12rb baris) = shim legacy di atas API baru |
| Python track | `src/wasender` = aplikasi desktop **terpisah** (pywebview + lisensi Ed25519 sendiri + UI HTML sendiri di `src/wasender/ui`). **Tidak di-ship** (installer hanya bawa C#), **tidak terintegrasi** dgn C#. Status: eksperimen rewrite, bukan produk aktif |
| Lisensi | Ganda: (a) `KeySecurity.dll` tertutup + `Config.IsProductActive` (offline, butuh server vendor saat generate — 403 saat dites), (b) panel online `license.pandusolusi.com` via `LicenseServer.cs` (verify/HWID/kuota/expiry + grace offline) |
| Database | SQLite file-based di `%ProgramData%\WASender`: `wa_monitor.db` (`send_quota`, `health`), `ai_cache.db` (`ai_cache/ai_history/ai_stats/ai_override`), `db.db` (template ikut installer). EF6 + `System.Data.SQLite` |
| Session WA | Singleton `Utils.waSenderBrowser`; tiap tab `WaSenderBrowser` berisi `MainUC` (1 WebView2 per sesi) |
| Scheduler | `AllSchedules` + `ScheduleSingle` + `schedulesModel` (jadwal tersimpan, dieksekusi via `runSchedule`) |
| Updater | `AppUpdater` (cek GitHub releases, catat log saja) + `checkForInternalUpdate` (unzip `update.zip` utk file WAPI/JS) |

## 2. Peta form: aktif vs mati

Aktif (dipakai user): `WaSenderForm` (shell + tab Single/Group/Tools), `RunSingle`, `RunGroup`, `WaSenderBrowser`+`MainUC`, `NewUI` (+`OldClassic` sbg opsi lama) via `GmExtractorForm`, `WaAutoReplyForm`+`AddRule`+`AddMessage`+`AddCaption`+`AddButton`+`AddPoll`, `ContactGrabber`, `NumberFilter`, `GetGroupMember`, `GrabChatList`, `GrabGroupActiveMembers`, `GrabGroupLinks`, `GroupFinder`, `GroupGenerator`, `GroupMemberAdder`, `GroupsJoiner`, `GroupLinkVerify`, `GroupMemberAdder`, `NumberFilter`, `QuickFilter`, `GeneralSettings`, `ManageAccounts`, `AllSchedules`, `Browser`, `SocialMediaExtractor`, `WebScrapper`, `GMapExtractor`? **MATI** (cek §3).

**File mati (hanya referensi komentar / tidak pernah di-instantiate):**
- `GMapExtractor.cs` (+ Designer, ±2400 baris): hanya muncul di baris komentar `//GMapExtractor form = new GMapExtractor(this)` di `WaSenderForm.cs:1096`. Jalur aktif = `GmExtractorForm` → `NewUI` (default) / `OldClassic`. Kandidat arsip, BUKAN hapus diam-diam.
- `AccountSessionControl.cs` (buatan sendiri, eksperimen pecah tab — belum dipakai; aman dihapus/arsipkan bila tidak dilanjutkan).
- `Utils.SetDriver` + seluruh cabang `browserType == 1` (±100 titik): mati runtime (dipaksa 2), kode masih ada sebagai fallback.

## 3. Masalah UI/UX terkonfirmasi (bukan opini)

1. **Dashboard lama = IE + CDN**: sebelum refactor me-load FontAwesome + popper dari CDN (broken offline/tofu boxes), selektor `a#pnnext` usang.
2. **Duplikasi 10x** di `WaSenderForm` (`_browser1..10`, `pollModelList1..10`, grid ×10). Sumber bug, bukan sekadar jelek.
3. **Grid polos** di ~30 form (header abu default, tanpa zebra/seleksi konsisten). → diperbaiki via `GridTheme.cs`.
4. **Tidak ada umpan balik kirim yg jujur**: `ack>=1` wajib → false Failed padahal terkirim; tiap lapisan kirim ulang → double-send. → diperbaiki (`ConfirmDelivered` FOUND=terkirim + `sentAccepted` stop-lanjut).
5. **Media gagal diam-diam**: base64 mentah tanpa prefix, tanpa `type`, payload raksasa 1x Eval. → diperbaiki (data-URL + tipe + chunked + timeout skala ukuran).
6. **Export crash**: path template relatif + `new Uri("")` + tanpa try/catch. → diperbaiki (resolver + guard + pesan error).
7. **AddRule crash**: `Single()` utk hapus duplikat, `SelectedItems[0]` tanpa guard, edit tak sinkron ke model. → diperbaiki.
8. **About crash (NullReferenceException)**: label baru tanpa `new`. → diperbaiki + jaring pengaman global (`Program.cs` → crash.log + pesan ramah).
9. **GMap stagnan di ~20 + loading selamanya**: paginator usang + rem hanya lihat scrollTop. → NewUI: scroll bertahap + panen tiap putaran + stop 12 putaran buntu + batas maks + filter rating + auto-stop + buka-peta.
10. **Form-layer campur aduk**: warna hardcode, teks campur ID/EN, tombol duplikat fungsi.

## 4. Yang SUDAH diperbaiki (ringkas; detail di git log)
GridTheme (16 tabel) • Dashboard WebView2 + hero + stat live + search + deskripsi (bebas IE/CDN) • DailySummaryForm • Tutorial offline + box dashboard • MessagePreviewForm + toolbar composer (B/I/emoji/variabel/karakter) • keterangan spintax • spintax engine `{a|b}` nested • anti-double kirim • verify longgar • RemGovernor di jalur kirim • lisensi online penuh (server + About + About-tanggal-mulai + indikator Online + tanya-kunci-saat-dibuka + grace offline) • General Settings Anti-Ban • About sisa hari + link support • AppUpdater badge • `BrowserPolicy` WebView2-only • `SendQueue` + `HumanDelay` + `AccountState` + `ScraperWebView2` + `GMapWebView2Extractor` + `BrowserPolicy` (fondasi) • installer ramping + cek prasyarat • RELEASE_NOTES + DEPLOY.md.

## 5. Yang SENGAJA tidak dilakukan (dengan alasan)
- **Rewrite total WaSenderForm/Designer**: 100+ event handler terikat layout; tanpa uji klik visual = risiko merusak aplikasi kirim. Dipoles bertahap (toolbar composer, GridTheme, net indicator).
- **Peta interaktif di dalam app**: butuh API key berbayar; solusi klik-2x = buka Google Maps gratis & andal.
- **Pagination grid live**: risiko data race antar-thread; diganti filter + auto-stop + batas maks.
- **Rewrite Python track (`feature.js`/`main.css`)**: BUKAN produk yg di-ship; rewrite membabi buta = buang waktu + nol dampak ke user.
- **Campaign Manager / Message Templates / Settings tab ala mockup**: butuh tabel DB + API baru — scope disetujui dulu sebelum eksekusi (lihat TODO Fase 5).
