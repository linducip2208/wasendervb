<#
.SYNOPSIS
    WASender — dev launcher.

.DESCRIPTION
    Bootstrap dev environment dan run aplikasi:
      1. Verify Python 3.11+
      2. Create .venv kalau belum ada
      3. Activate venv
      4. pip install -r requirements.txt
      5. playwright install chromium  (untuk Google Maps extractor)
      6. Set PYTHONPATH=src
      7. python -m wasender

.EXAMPLE
    .\run-dev.ps1
    .\run-dev.ps1 -SkipInstall    # skip pip install (faster restart)
    .\run-dev.ps1 -Recreate       # nuke .venv dan rebuild dari awal
#>

[CmdletBinding()]
param(
    [switch]$SkipInstall,
    [switch]$SkipPlaywright,
    [switch]$Recreate
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
function Write-Info  ($msg) { Write-Host "[run-dev] $msg" -ForegroundColor Cyan }
function Write-Ok    ($msg) { Write-Host "[run-dev] $msg" -ForegroundColor Green }
function Write-Warn2 ($msg) { Write-Host "[run-dev] $msg" -ForegroundColor Yellow }
function Write-Err   ($msg) { Write-Host "[run-dev] $msg" -ForegroundColor Red }

# ---------------------------------------------------------------------------
# Resolve paths (relative ke script dir, bukan cwd)
# ---------------------------------------------------------------------------
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$VenvDir     = Join-Path $ProjectRoot ".venv"
$VenvPython  = Join-Path $VenvDir "Scripts\python.exe"
$VenvActivate = Join-Path $VenvDir "Scripts\Activate.ps1"
$Requirements = Join-Path $ProjectRoot "requirements.txt"
$SrcDir       = Join-Path $ProjectRoot "src"

Set-Location $ProjectRoot

Write-Info "Project root : $ProjectRoot"

# ---------------------------------------------------------------------------
# Step 1: Verify Python 3.11+ (system-wide)
# ---------------------------------------------------------------------------
Write-Info "Checking system Python version..."
$pythonCmd = $null
foreach ($candidate in @('python', 'py -3.11', 'py -3.12', 'py -3.13', 'py -3')) {
    try {
        $verOutput = & cmd /c "$candidate --version 2>&1"
        if ($LASTEXITCODE -eq 0 -and $verOutput -match 'Python\s+(\d+)\.(\d+)') {
            $major = [int]$Matches[1]
            $minor = [int]$Matches[2]
            if ($major -gt 3 -or ($major -eq 3 -and $minor -ge 11)) {
                $pythonCmd = $candidate
                Write-Ok "Found: $candidate -> $verOutput"
                break
            } else {
                Write-Warn2 "Skip $candidate (version $major.$minor < 3.11)"
            }
        }
    } catch {
        # try next candidate
    }
}

if (-not $pythonCmd) {
    Write-Err "Python 3.11+ tidak ditemukan. Install dari https://python.org/downloads/"
    Write-Err "Pastikan 'Add Python to PATH' di-centang saat instalasi."
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: Create venv
# ---------------------------------------------------------------------------
if ($Recreate -and (Test-Path $VenvDir)) {
    Write-Warn2 "Removing existing .venv (--Recreate flag)..."
    Remove-Item -Recurse -Force $VenvDir
}

if (-not (Test-Path $VenvPython)) {
    Write-Info "Creating venv at .venv ..."
    & cmd /c "$pythonCmd -m venv `"$VenvDir`""
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Failed to create venv (exit $LASTEXITCODE)."
        exit $LASTEXITCODE
    }
    Write-Ok "Venv created."
} else {
    Write-Info "Venv already exists at .venv"
}

# ---------------------------------------------------------------------------
# Step 3: Activate venv
# ---------------------------------------------------------------------------
if (-not (Test-Path $VenvActivate)) {
    Write-Err "Activate script tidak ditemukan: $VenvActivate"
    exit 2
}

Write-Info "Activating venv..."
. $VenvActivate

# Sanity: pastikan python yang ke-resolve bener venv-nya
$resolvedPython = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $resolvedPython -or -not $resolvedPython.StartsWith($VenvDir, [StringComparison]::OrdinalIgnoreCase)) {
    Write-Warn2 "Resolved python = '$resolvedPython' (not from .venv)."
    Write-Warn2 "Fallback: pakai .venv\Scripts\python.exe langsung."
    $script:PythonExe = $VenvPython
} else {
    $script:PythonExe = $resolvedPython
    Write-Ok "Using python: $resolvedPython"
}

# ---------------------------------------------------------------------------
# Step 4: pip install -r requirements.txt
# ---------------------------------------------------------------------------
if ($SkipInstall) {
    Write-Warn2 "Skipping pip install (--SkipInstall)."
} else {
    if (-not (Test-Path $Requirements)) {
        Write-Err "requirements.txt tidak ditemukan: $Requirements"
        exit 3
    }
    Write-Info "Upgrading pip..."
    & $PythonExe -m pip install --upgrade pip
    if ($LASTEXITCODE -ne 0) {
        Write-Warn2 "pip upgrade gagal (exit $LASTEXITCODE) — lanjut tetap..."
    }

    Write-Info "Installing requirements..."
    & $PythonExe -m pip install -r $Requirements
    if ($LASTEXITCODE -ne 0) {
        Write-Err "pip install gagal (exit $LASTEXITCODE)."
        exit $LASTEXITCODE
    }
    Write-Ok "Dependencies installed."
}

# ---------------------------------------------------------------------------
# Step 5: playwright install chromium
# ---------------------------------------------------------------------------
if ($SkipPlaywright) {
    Write-Warn2 "Skipping playwright browser install (--SkipPlaywright)."
} else {
    Write-Info "Installing Playwright Chromium (needed for gmaps extractor)..."
    & $PythonExe -m playwright install chromium
    if ($LASTEXITCODE -ne 0) {
        Write-Warn2 "Playwright install gagal (exit $LASTEXITCODE) — gmaps extractor mungkin ga jalan."
        # Non-fatal: app inti tetap bisa run.
    } else {
        Write-Ok "Playwright Chromium ready."
    }
}

# ---------------------------------------------------------------------------
# Step 6: Set PYTHONPATH=src (so `python -m wasender` resolve dari src/)
# ---------------------------------------------------------------------------
$env:PYTHONPATH = $SrcDir
Write-Info "PYTHONPATH = $env:PYTHONPATH"

# Dev mode: bypass license check (WASENDER_DEV=1)
$env:WASENDER_DEV = "1"
Write-info "WASENDER_DEV = 1 (license bypass)"

# ---------------------------------------------------------------------------
# Step 7: Run app
# ---------------------------------------------------------------------------
Write-Info "Launching: python -m wasender"
Write-Host ""
& $PythonExe -m wasender
$exitCode = $LASTEXITCODE
Write-Host ""
if ($exitCode -eq 0) {
    Write-Ok "App exited cleanly (0)."
} else {
    Write-Err "App exited with code $exitCode."
}
exit $exitCode
