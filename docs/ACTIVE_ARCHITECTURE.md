# WASender — Active Architecture (fakta lapangan)

## Produk yang di-ship
- **Satu-satunya biner produksi**: `CodeHere/WASender/bin/Release/WASender.exe` (.NET Framework 4.8, WinForms + MaterialSkin).
- **Entry**: `Program.Main` → single-instance mutex → `RunApp` → `WaSenderForm`.
- **Installer**: `installer.iss` (Inno Setup 6) memaketkan isi `bin/Release` (minus `*.pdb/*.xml/chromedriver/WebView2-profile/Backup/log/DB-runtime).

## Python track (`src/wasender`) — BUKAN bagian produk
- Entry terpisah: `python -m wasender` → `app.py:main()` → jendela pywebview (`index.html`/`login.html`), bridge `api/bridge.py`, lisensi Ed25519 sendiri (`core/license.py`), UI HTML sendiri (`ui/`).
- Tidak ada pemanggilan silang C# ↔ Python di runtime mana pun (tidak ada `Process.Start(python`, tidak ada HTTP lokal di antara keduanya).
- `build.py`/`wasender.spec` menghasilkan `dist/WASender.exe` (PyInstaller) — artefak eksperimen, TIDAK dipaket installer.
- Kesimpulan: arsipkan/pertahankan sebagai eksperimen; JANGAN rewrite sebagai bagian redesign C#. (Lihat TODO Fase 4.)

## C# runtime map
```
Program.Main (mutex single-instance)
└── WaSenderForm (shell: tab Single | Group | Tools-dashboard[WebView2] + top bar + net indicator)
    ├── Single/Group: txtMsg1..5 + gridTargets/gridStatus → RunSingle/RunGroup (kirim via WPPHelper)
    │   └── SendEngine (3 lapis: WPP API → FMARK → DOM) + SendGovernor (kuota/harian+skor) + HumanDelay
    ├── WaSenderBrowser → MainUC (1 WebView2 per sesi WA) ; Utils.waSenderBrowser singleton
    ├── Tools dashboard: HelpDocs/Dashboard.html (WebView2) ⇄ postMessage(data-id) ⇄ dashboardMessageReceived
    ├── GMap: GmExtractorForm → NewUI (aktif) / OldClassic (opsi) ; Browser.cs (WebView2 scraper + XHR hook wapi_gmaps.js)
    │   └── GMapExtractor.cs (lama, Selenium): MATI — hanya referensi komentar. Kandidat arsip.
    ├── Auto-reply: WaAutoReplyForm + AddRule/AddMessage/AddCaption/AddButton/AddPoll (+AiResponder BYOK + AiHandover)
    ├── Tools: ContactGrabber, GetGroupMember, GrabChatList, GrabGroupActiveMembers, GrabGroupLinks,
    │          GroupFinder/Generator/Joiner/LinkVerify/MemberAdder, NumberFilter, QuickFilter,
    │          SocialMediaExtractor, WebScrapper, GoogleCSVGenerator, ManageAccounts, AllSchedules
    ├── Settings: GeneralSettings (+BrowserPolicy paksa WebView2) ; SendLimits (send_limits.json)
    └── Lisensi: Activate.cs (kode panel XXXX-… ATAU blob KeySecurity lama)
        → LicenseServer.cs (verify ke https://license.pandusolusi.com/api/v1/licenses/verify)
        → Config.IsProductActive() = KeySecurity OK || online-lokal valid (hook OnlineActiveCheck)
        → About.cs (sisa hari + tanggal mulai + support WA link)
```

## Data & file penting (runtime, %ProgramData%\WaSender)
| File | Isi | Dibuat otomatis? |
|---|---|---|
| `Activation.txt` | lisensi legacy terenkripsi | saat aktivasi lama |
| `online_license.json` | `{code,hwid,customer,issued_on,expires_on}` | saat aktivasi panel |
| `wa_monitor.db` | `send_quota(date,account,n)`, `health(ts,score)` | ya (CREATE TABLE IF NOT EXISTS) |
| `ai_cache.db` | cache/override/history/stats AI | ya |
| `send_limits.json` | DailyPerAccount, Min/MaxDelay, MinScore | ya (default) |
| `ai_settings.json` | BaseUrl/ApiKey/Model/persona | template saat dibuka |
| `crash.log` / `single_send.log` | log & crash global | ya |

## Kontrak eksternal (jangan diubah tanpa uji dampak)
- `POST {panel}/api/v1/licenses/verify {code,hwid[,label,app_version]}` → `{valid,message,status,product,customer,issued_on,expires_on,activations}` (selalu HTTP 200 utk hasil bisnis).
- `POST {panel}/api/v1/licenses/release` (Bearer Sanctum) → cabut aktivasi HWID.
- `wppconnect-wa.js` (bundel, versi terkunci di repo) + `wapi.js`/`wapi_gmaps.js` (legacy).
- Struktur JSON settings (`GeneralSettingsModel`, dll Models.dll) — format file user yg sudah ada.
