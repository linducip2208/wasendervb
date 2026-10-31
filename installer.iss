; ===========================================================================
;  WASender — Inno Setup installer script
; ---------------------------------------------------------------------------
;  Build via:
;      iscc installer.iss
;  Prereq:
;      - dist\WASender.exe sudah di-build via `python build.py`
;      - Inno Setup 6+ terpasang (https://jrsoftware.org/isdl.php)
;  Output:
;      dist\installer\WASender-Setup-6.1.exe
; ===========================================================================

#define MyAppName       "WASender"
#define MyAppVersion    "6.1"
#define MyAppPublisher  "WASender"
#define MyAppExeName    "WASender.exe"
#define MyAppURL        "https://wasender.local"

[Setup]
AppId={{8D7A3E2C-9B1F-4E5A-8C42-7F1E0D3B9A1C}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=dist\installer
OutputBaseFilename=WASender-Setup-{#MyAppVersion}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
; SetupIconFile=assets\icon.ico
; ^ Uncomment kalau assets/icon.ico tersedia.
MinVersion=10.0.17134

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; \
    GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; Main exe (hasil onefile build dari PyInstaller)
Source: "dist\WASender.exe"; DestDir: "{app}"; Flags: ignoreversion
; Optional README — copy ke install dir kalau ada
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; \
    Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; \
    Flags: nowait postinstall skipifsilent

; ===========================================================================
;  Custom Code — WebView2 runtime detection
; ---------------------------------------------------------------------------
;  Win10 1803+ dan Win11 biasanya udah pre-installed Edge WebView2 runtime.
;  Tapi beberapa SKU (LTSC / N edition / fresh install) ga ada — kita check
;  registry & prompt user kalau missing.
; ===========================================================================
[Code]
const
  WV2_CLIENT_64 = 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WV2_CLIENT_32 = 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WV2_CLIENT_HKCU = 'Software\Microsoft\EdgeUpdate\ClientState\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WV2_DOWNLOAD_URL = 'https://developer.microsoft.com/microsoft-edge/webview2/';

function IsWebView2Installed(): Boolean;
var
  Version: string;
begin
  Result := False;

  // Cek HKLM 64-bit (per-machine, x64 host)
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, WV2_CLIENT_64, 'pv', Version) then
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := True;
      Exit;
    end;

  // Cek HKLM 32-bit fallback
  if RegQueryStringValue(HKEY_LOCAL_MACHINE, WV2_CLIENT_32, 'pv', Version) then
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := True;
      Exit;
    end;

  // Cek HKCU (per-user install)
  if RegQueryStringValue(HKEY_CURRENT_USER, WV2_CLIENT_HKCU, 'pv', Version) then
    if (Version <> '') and (Version <> '0.0.0.0') then
    begin
      Result := True;
      Exit;
    end;
end;

function InitializeSetup(): Boolean;
var
  ResponseCode: Integer;
  ErrorCode: Integer;
begin
  Result := True;

  if IsWebView2Installed() then
    Exit;

  ResponseCode := MsgBox(
    'WASender membutuhkan Microsoft Edge WebView2 Runtime, tapi runtime tersebut belum terinstall di sistem ini.' + #13#10 + #13#10 +
    'Klik [Yes] untuk membuka halaman download WebView2 dari microsoft.com.' + #13#10 +
    'Klik [No] untuk membatalkan installer (silakan install WebView2 dulu, lalu jalankan installer ini lagi).' + #13#10 + #13#10 +
    'Catatan: WebView2 sudah pre-installed di Windows 11 dan kebanyakan Windows 10 modern, tapi beberapa edisi (LTSC / N) tidak menyertakannya.',
    mbConfirmation,
    MB_YESNO or MB_DEFBUTTON1
  );

  if ResponseCode = IDYES then
  begin
    ShellExec('open', WV2_DOWNLOAD_URL, '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;

  // Either way: cancel installer, biar user install WebView2 dulu.
  Result := False;
end;
