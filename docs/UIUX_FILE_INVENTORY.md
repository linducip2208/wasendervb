# WASender — UI/UX File Inventory (hasil scan otomatis, 2026-11-02)

## C# WinForms — 63 Designer + 63 code-behind (+3 form tanpa Designer: CampaignSummaryForm, DailySummaryForm, MessagePreviewForm)

| Form | Baris Designer | Controls.Add | Event wiring | ClientSize |
|---|---|---|---|---|
| About | 280 | 14 | 4 | 527, 564 |
| AccountSettings | 339 | 12 | 3 | 569, 639 |
| Activate | 227 | 9 | 6 | 622, 552 |
| AddAccount | 133 | 3 | 2 | 440, 278 |
| AddButton | 212 | 7 | 4 | 505, 436 |
| AddCaption | 214 | 5 | 6 | 547, 560 |
| AddKeyMarker | 153 | 4 | 3 | 504, 428 |
| AddLocation | 267 | 7 | 2 | 439, 564 |
| AddMessage | 332 | 12 | 8 | 948, 689 |
| AddPoll | 242 | 8 | 3 | 538, 653 |
| AddRule | 429 | 17 | 12 | 642, 718 |
| AllSchedules | 208 | 3 | 3 | 1036, 538 |
| AttachContact | 165 | 4 | 2 | 509, 389 |
| BlockList | 115 | 3 | 2 | 393, 528 |
| Browser | 211 | 7 | 6 | 1040, 778 |
| BuiltInVariable | 112 | 2 | 3 | 445, 573 |
| BusinessExtractor | 458 | 21 | 3 | 1012, 764 |
| ButtonHolder | 289 | 10 | 5 | 453, 789 |
| ChooseGroup | 176 | 5 | 4 | 503, 718 |
| ContactGrabber | 276 | 12 | 5 | 1040, 738 |
| CountryCodeInput | 120 | 2 | 1 | 549, 184 |
| FriendlyNumbers | 176 | 5 | 4 | 465, 485 |
| GMapExtractor | 660 | 23 | 10 | 794, 773 |
| GeneralSettings | 929 | 36 | 14 | 891, 721 |
| GenerateNumbers | 236 | 8 | 3 | 403, 727 |
| GenerateRandomNames | 203 | 5 | 2 | 562, 418 |
| GetGroupMember | 259 | 11 | 5 | 1058, 785 |
| GetPollResults | 334 | 16 | 5 | 1058, 815 |
| GmExtractorForm | 72 | 1 | 3 | 1059, 800 |
| GoogleCSVGenerator | 188 | 5 | 5 | 883, 674 |
| GrabCHatList | 286 | 13 | 5 | 1058, 785 |
| GrabGroupActiveMembers | 357 | 17 | 6 | 1058, 785 |
| GrabGroupLinks | 234 | 10 | 4 | 1058, 785 |
| GroupFinder | 337 | 13 | 6 | 784, 772 |
| GroupGenerator | 842 | 41 | 5 | 1096, 713 |
| GroupLauncher | 219 | 7 | 4 | 578, 393 |
| GroupLinkVerify | 724 | 32 | 12 | 1377, 811 |
| GroupMemberAdder | 787 | 41 | 7 | 1060, 687 |
| GroupsJoiner | 742 | 39 | 10 | 1370, 764 |
| InfoWindow | 110 | 3 | 2 | 542, 627 |
| InputDialog | 176 | 4 | 3 | 492, 491 |
| KeyMarker | 127 | 3 | 4 | 665, 616 |
| MainUC | 66 | 1 | 1 | UserControl |
| ManageAccounts | 229 | 4 | 6 | 813, 538 |
| NewUI | 659 | 28 | 9 | UserControl |
| NumberFilter | 785 | 35 | 16 | 1377, 811 |
| OldClassic | 620 | 22 | 7 | UserControl |
| PasteNumber | 113 | 3 | 2 | 423, 688 |
| Progressbar | 82 | 2 | 1 | 477, 188 |
| QuickFilter | 179 | 7 | 3 | 550, 782 |
| RunGroup | 487 | 24 | 6 | 1059, 811 |
| RunGroupWarmer | 264 | 9 | 5 | 856, 639 |
| RunSingle | 487 | 24 | 6 | 1059, 804 |
| RunWarmer | 266 | 9 | 5 | 856, 639 |
| ScheduleSingle | 222 | 10 | 2 | 570, 471 |
| SelectLabel | 139 | 3 | 2 | 526, 563 |
| SingleLauncher | 399 | 14 | 10 | 617, 758 |
| SocialMediaExtractor | 648 | 28 | 13 | 1283, 903 |
| WaAutoReplyForm | 352 | 16 | 7 | 1182, 758 |
| WaSenderBrowser | 142 | 2 | 2 | 1238, 699 |
| WaSenderForm | 4171 | 176 | 80 | 1771, 945 |
| Warmer | 351 | 13 | 13 | 975, 608 |
| WebScrapper | 581 | 23 | 12 | 1259, 720 |

## UserControl aktif
- `MainUC` (1 WebView2 sesi WA per tab, dipakai `WaSenderBrowser`)
- `NewUI` (GMap aktif, default) + `OldClassic` (GMap opsi lama) — dipakai `GmExtractorForm`
- `AccountSessionControl` — MATI (tidak pernah di-instantiate)

## Form MATI (dead code, bukti: nol `new X(` di luar komentar)
- `GMapExtractor` (+Designer 660 baris): satu-satunya referensi = baris komentar `//GMapExtractor form = ...` di `WaSenderForm.cs:1096`. Jalur aktif = `GmExtractorForm` → `NewUI`.
- `AccountSessionControl`: 0 referensi.

## Python/WebView (`src/wasender`, TIDAK di-ship installer)
- `ui/assets/css/main.css` (11 KB) — satu stylesheet global
- `ui/assets/js/pages/`: `feature.js` (21 KB), `dashboard.js` (13 KB), `settings.js` (7 KB), `schedules.js` (6 KB), `logs.js` (5 KB), `accounts.js` (4 KB), `contacts.js` (3 KB)
- `ui/*.html`: `index.html`, `login.html`
- Bridge: `api/bridge.py` (kontrak JS↔Python — DILARANG ubah signature)

## Help/HTML lokal (C#, di-ship)
- `HelpDocs/Dashboard.html` + `HelpDocs/Tutorial.html` (WebView2, offline, tanpa CDN)

## Resource/icon
- `Resources/` + `*.resx` (64 file) — icon/logo form. Tidak ada penambahan file gambar; sidebar memakai glyph teks agar tidak menambah resource biner.
