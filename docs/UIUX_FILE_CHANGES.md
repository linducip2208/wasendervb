# WASender — UI/UX File Changes (diff summary)

## Baru
- `CodeHere/WASender/WaSenderForm.Sidebar.cs` — sidebar + breadcrumb + anchor fix
- `docs/UIUX_FILE_INVENTORY.md`, `docs/UIUX_BEFORE_AUDIT.md`, `docs/UIUX_REDESIGN_PLAN.md`
- `docs/UIUX_REDESIGN_FINAL.md`, `docs/UIUX_FILE_CHANGES.md` (file ini), `docs/UIUX_REGRESSION_TEST.md`

## Diubah — C# (1 baris `GridTheme.Apply` per constructor, kecuali dicatat)
- `WaSenderForm.cs` — hook `BuildEnterpriseSidebar()` di `Form1_Load` (+ `ConfirmCampaign` dari sesi lalu)
- `NewUI.cs` — `PolishWorkspace()` + `AddCardHeader()` di `NewUI_Load`
- `AddPoll.cs`, `BuiltInVariable.cs`, `BusinessExtractor.cs`, `GenerateNumbers.cs`, `GoogleCSVGenerator.cs`, `GroupFinder.cs`, `GroupLinkVerify.cs`, `GroupMemberAdder.cs`, `GroupsJoiner.cs`, `KeyMarker.cs`, `NumberFilter.cs`, `OldClassic.cs`, `QuickFilter.cs`, `RunGroupWarmer.cs`, `RunWarmer.cs`, `SingleLauncher.cs`, `SocialMediaExtractor.cs`, `Warmer.cs`, `WebScrapper.cs`
- `WASender.csproj` — compile include `WaSenderForm.Sidebar.cs`
- `TODO.md` — centang Fase 1–4

## Diubah — Python/WebView (kontrak bridge utuh)
- `src/wasender/ui/index.html` — brand + label sidebar
- `src/wasender/ui/assets/css/main.css` — blok ENTERPRISE di akhir file
- `src/wasender/ui/assets/js/pages/feature.js` — shell workspace + skeleton + renderDataTable

## TIDAK diubah (verifikasi: tidak ada di `git status`)
- `*.Designer.cs` (63 file), `*.resx` (64 file), seluruh engine (`SendEngine.cs`, `RunSingle.cs` logic, `Bridge/*`, `Browser.cs`), seluruh DB/schema, `src/wasender/api/bridge.py`, `src/wasender/core/*`, `installer.iss` (isi berubah via rebuild, script tetap)
