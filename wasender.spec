# -*- mode: python ; coding: utf-8 -*-
"""PyInstaller spec for WASender desktop app.

Build via: `python build.py` (preferred) or `pyinstaller wasender.spec --clean --noconfirm`.

Onefile + windowed (no console). Bundles UI assets, WhatsApp Web injection
script, selectors, and license public key.
"""
from __future__ import annotations

from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
# SPECPATH dipasang otomatis oleh PyInstaller saat baca spec — pakai itu agar
# build bisa jalan dari working dir mana saja.
PROJECT_ROOT = Path(SPECPATH).resolve()
SRC_ROOT = PROJECT_ROOT / "src"
PKG_ROOT = SRC_ROOT / "wasender"
ENTRY = PKG_ROOT / "__main__.py"

# Optional icon — kalau ga ada, build tetap jalan tanpa icon kustom.
ICON_PATH = PROJECT_ROOT / "assets" / "icon.ico"
ICON_ARG = str(ICON_PATH) if ICON_PATH.exists() else None
# NOTE: kalau mau pakai icon, taruh di assets/icon.ico (256x256 multi-size).

# ---------------------------------------------------------------------------
# Datas — (source, dest_inside_bundle)
# ---------------------------------------------------------------------------
# IMPORTANT: paths.py.resource_dir() di-frozen mode return sys._MEIPASS
# (root extraction dir), bukan sub-path. Jadi bundle ke ui/, wa/, core/
# di ROOT bundle — bukan di-nested di bawah wasender/.
#   ui_dir()        -> _MEIPASS/ui
#   wapi_js_path()  -> _MEIPASS/wa/wapi.js
#   selectors_path()-> _MEIPASS/wa/selectors.json
#   public_key_path -> _MEIPASS/core/public_key.pem
datas = [
    (str(PKG_ROOT / "ui"), "ui"),
    (str(PKG_ROOT / "wa" / "wapi.js"), "wa"),
    (str(PKG_ROOT / "wa" / "selectors.json"), "wa"),
    (str(PKG_ROOT / "core" / "public_key.pem"), "core"),
]

# ---------------------------------------------------------------------------
# Hidden imports — paket yang di-import secara dinamis / lazy
# ---------------------------------------------------------------------------
hiddenimports = [
    # Feature subpackages — di-load via dispatch dynamic dari bridge
    "wasender.features",
    "wasender.features.automations",
    "wasender.features.automations.group_warmer",
    "wasender.features.automations.number_filter",
    "wasender.features.automations.responder_bot",
    "wasender.features.automations.warmer",
    "wasender.features.data_extractors",
    "wasender.features.data_extractors.active_members",
    "wasender.features.data_extractors.chat_list",
    "wasender.features.data_extractors.contact_list",
    "wasender.features.data_extractors.group_member",
    "wasender.features.data_extractors.poll_results",
    "wasender.features.group_tools",
    "wasender.features.group_tools.auto_joiner",
    "wasender.features.group_tools.bulk_add",
    "wasender.features.group_tools.bulk_generator",
    "wasender.features.group_tools.grab_links",
    "wasender.features.group_tools.group_finder",
    "wasender.features.lead_gen",
    "wasender.features.lead_gen.gcontacts_csv",
    "wasender.features.lead_gen.gmaps_extractor",
    "wasender.features.lead_gen.social_extractor",
    "wasender.features.lead_gen.website_extractor",
    "wasender.features.messaging",
    "wasender.features.messaging.bulk_sender",
    # Crypto — Ed25519 di-resolve via string name di license.py
    "cryptography.hazmat.primitives.asymmetric.ed25519",
    # APScheduler — trigger & executor di-load by name
    "apscheduler.triggers.cron",
    "apscheduler.triggers.date",
    "apscheduler.triggers.interval",
    "apscheduler.executors.pool",
    "apscheduler.jobstores.memory",
    # Misc lazy imports
    "pkg_resources.py2_warn",
    "loguru._defaults",
]

# ---------------------------------------------------------------------------
# Excludes — kurangi ukuran binary
# ---------------------------------------------------------------------------
# tkinter & friends ga dipakai di app utama (cuma di keygen — yang di-build
# terpisah lewat build_keygen.py).
excludes = [
    "tkinter",
    "_tkinter",
    "Tkinter",
    "matplotlib",
    "scipy",
    "IPython",
    "jupyter",
    "notebook",
    "pytest",
    "sphinx",
]

# ---------------------------------------------------------------------------
# Analysis
# ---------------------------------------------------------------------------
a = Analysis(
    [str(ENTRY)],
    pathex=[str(SRC_ROOT)],
    binaries=[],
    datas=datas,
    hiddenimports=hiddenimports,
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=excludes,
    noarchive=False,
    optimize=0,
)

pyz = PYZ(a.pure)

# ---------------------------------------------------------------------------
# UPX exclusions — DLL ini kalau di-compress sering bikin AV false-positive
# atau crash saat runtime di Windows tertentu.
# ---------------------------------------------------------------------------
upx_exclude = [
    "vcruntime140.dll",
    "vcruntime140_1.dll",
    "python3.dll",
    "python311.dll",
    "python312.dll",
    "python313.dll",
]

# ---------------------------------------------------------------------------
# Build mode — onefile (default) atau onedir (kalau env BUILD_ONEDIR=1)
# ---------------------------------------------------------------------------
import os as _os

_ONEDIR = _os.environ.get("BUILD_ONEDIR", "0") == "1"

if _ONEDIR:
    # Onedir: faster startup (no extraction to temp), distributable as folder.
    exe = EXE(
        pyz,
        a.scripts,
        [],
        exclude_binaries=True,
        name="WASender",
        debug=False,
        bootloader_ignore_signals=False,
        strip=False,
        upx=True,
        upx_exclude=upx_exclude,
        console=False,
        disable_windowed_traceback=False,
        argv_emulation=False,
        target_arch=None,
        codesign_identity=None,
        entitlements_file=None,
        icon=ICON_ARG,
    )
    coll = COLLECT(
        exe,
        a.binaries,
        a.datas,
        strip=False,
        upx=True,
        upx_exclude=upx_exclude,
        name="WASender",
    )
else:
    # Onefile: single .exe, slower first startup tapi praktis untuk distribusi.
    exe = EXE(
        pyz,
        a.scripts,
        a.binaries,
        a.datas,
        [],
        name="WASender",
        debug=False,
        bootloader_ignore_signals=False,
        strip=False,
        upx=True,
        upx_exclude=upx_exclude,
        runtime_tmpdir=None,
        console=False,
        disable_windowed_traceback=False,
        argv_emulation=False,
        target_arch=None,
        codesign_identity=None,
        entitlements_file=None,
        icon=ICON_ARG,
    )
