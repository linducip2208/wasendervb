"""Build automation untuk WASender desktop .exe.

Usage:
    python build.py                  # default: onefile, no obfuscation
    python build.py --onedir         # onedir (faster startup)
    python build.py --obfuscate      # PyArmor obfuscate dulu, lalu PyInstaller

Steps:
    1. Sanity-check keys (private_key.pem ada? public_key.pem bukan placeholder?)
    2. Clean dist/ + build/
    3. (optional) PyArmor obfuscate src/wasender/ -> build/obf/
    4. PyInstaller wasender.spec
    5. Print final exe path + size
"""
from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent
SRC_DIR = PROJECT_ROOT / "src"
PKG_DIR = SRC_DIR / "wasender"
DIST_DIR = PROJECT_ROOT / "dist"
BUILD_DIR = PROJECT_ROOT / "build"
SPEC_FILE = PROJECT_ROOT / "wasender.spec"

PRIVATE_KEY = PROJECT_ROOT / "keygen" / "private_key.pem"
PUBLIC_KEY = PKG_DIR / "core" / "public_key.pem"


# ---------------------------------------------------------------------------
# Pretty-print helpers
# ---------------------------------------------------------------------------
def _info(msg: str) -> None:
    print(f"[build] {msg}")


def _warn(msg: str) -> None:
    print(f"[build] WARN: {msg}", file=sys.stderr)


def _err(msg: str) -> None:
    print(f"[build] ERROR: {msg}", file=sys.stderr)


def _section(title: str) -> None:
    print()
    print("=" * 72)
    print(f"  {title}")
    print("=" * 72)


def _human_size(num_bytes: int) -> str:
    size = float(num_bytes)
    for unit in ("B", "KB", "MB", "GB"):
        if size < 1024.0:
            return f"{size:.2f} {unit}"
        size /= 1024.0
    return f"{size:.2f} TB"


# ---------------------------------------------------------------------------
# Pre-flight checks
# ---------------------------------------------------------------------------
def check_keys() -> None:
    """Verify keypair sudah generated. Public key wajib non-placeholder."""
    _section("Step 1/4: Verifying license keypair")

    if not PRIVATE_KEY.exists():
        _warn(f"private_key.pem TIDAK ADA di {PRIVATE_KEY}")
        _warn("Build akan tetap lanjut, tapi kamu ga bisa generate license.")
        _warn("Run: python keygen/generate_keys.py")
    else:
        _info(f"OK: private_key.pem found ({_human_size(PRIVATE_KEY.stat().st_size)})")

    if not PUBLIC_KEY.exists():
        _err(f"public_key.pem TIDAK ADA di {PUBLIC_KEY}")
        _err("Run: python keygen/generate_keys.py")
        sys.exit(2)

    pub_content = PUBLIC_KEY.read_text(encoding="utf-8", errors="replace")
    if "PLACEHOLDER" in pub_content.upper():
        _err(f"public_key.pem masih berisi PLACEHOLDER content!")
        _err(f"File: {PUBLIC_KEY}")
        _err("Run: python keygen/generate_keys.py")
        _err("(akan generate private_key.pem + overwrite public_key.pem)")
        sys.exit(2)

    _info(f"OK: public_key.pem looks valid ({_human_size(PUBLIC_KEY.stat().st_size)})")


def clean_artifacts() -> None:
    """Hapus dist/ dan build/ supaya build fresh."""
    _section("Step 2/4: Cleaning previous build artifacts")
    for target in (DIST_DIR, BUILD_DIR):
        if target.exists():
            _info(f"Removing {target}")
            shutil.rmtree(target, ignore_errors=True)
        else:
            _info(f"Skip (does not exist): {target}")
    _info("Clean done.")


# ---------------------------------------------------------------------------
# PyArmor obfuscation (optional)
# ---------------------------------------------------------------------------
def run_pyarmor() -> Path:
    """Obfuscate src/wasender/ ke build/obf/wasender/.

    Returns the path to obfuscated src root yang harus dipakai pyinstaller.
    """
    _section("Step 3a/4: PyArmor obfuscating wasender package")
    obf_root = BUILD_DIR / "obf"
    obf_root.mkdir(parents=True, exist_ok=True)

    cmd = [
        sys.executable,
        "-m",
        "pyarmor",
        "gen",
        "--recursive",
        "--output",
        str(obf_root),
        str(PKG_DIR),
    ]
    _info(f"$ {' '.join(cmd)}")
    subprocess.run(cmd, check=True, cwd=str(PROJECT_ROOT))

    obf_pkg = obf_root / "wasender"
    if not obf_pkg.exists():
        _err(f"PyArmor output ga ditemukan di {obf_pkg}")
        sys.exit(3)

    # Copy non-Python resources (PyArmor cuma proses .py) ke output dir
    # supaya datas di .spec masih valid kalau kita switch ke obf root.
    _info("Mirroring non-Python resources into obfuscated package...")
    for rel in ("ui", "wa/wapi.js", "wa/selectors.json", "core/public_key.pem"):
        src = PKG_DIR / rel
        dst = obf_pkg / rel
        if src.is_dir():
            shutil.copytree(src, dst, dirs_exist_ok=True)
        else:
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, dst)

    _info(f"Obfuscated package ready at: {obf_pkg}")
    return obf_root


# ---------------------------------------------------------------------------
# PyInstaller
# ---------------------------------------------------------------------------
def run_pyinstaller(onedir: bool, env_overrides: dict[str, str] | None = None) -> None:
    _section("Step 3/4: Running PyInstaller")

    if not SPEC_FILE.exists():
        _err(f"Spec file ga ditemukan: {SPEC_FILE}")
        sys.exit(4)

    env = os.environ.copy()
    env["BUILD_ONEDIR"] = "1" if onedir else "0"
    if env_overrides:
        env.update(env_overrides)

    cmd = [
        sys.executable,
        "-m",
        "PyInstaller",
        str(SPEC_FILE),
        "--clean",
        "--noconfirm",
    ]
    _info(f"BUILD_ONEDIR={env['BUILD_ONEDIR']}")
    _info(f"$ {' '.join(cmd)}")
    subprocess.run(cmd, check=True, cwd=str(PROJECT_ROOT), env=env)


# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------
def report_output(onedir: bool) -> None:
    _section("Step 4/4: Build artifacts")
    if onedir:
        out_dir = DIST_DIR / "WASender"
        exe = out_dir / "WASender.exe"
        if not exe.exists():
            _err(f"Expected onedir exe ga ditemukan: {exe}")
            sys.exit(5)
        total = sum(p.stat().st_size for p in out_dir.rglob("*") if p.is_file())
        _info(f"Mode      : onedir")
        _info(f"Folder    : {out_dir}")
        _info(f"Exe       : {exe}")
        _info(f"Exe size  : {_human_size(exe.stat().st_size)}")
        _info(f"Total size: {_human_size(total)}")
    else:
        exe = DIST_DIR / "WASender.exe"
        if not exe.exists():
            _err(f"Expected onefile exe ga ditemukan: {exe}")
            sys.exit(5)
        _info(f"Mode      : onefile")
        _info(f"Exe       : {exe}")
        _info(f"Size      : {_human_size(exe.stat().st_size)}")

    print()
    _info("Build OK. Distribusikan exe atau lanjut ke `iscc installer.iss`.")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build WASender desktop .exe via PyInstaller."
    )
    parser.add_argument(
        "--obfuscate",
        action="store_true",
        help="Jalankan PyArmor obfuscation sebelum PyInstaller (slower, lebih aman).",
    )
    parser.add_argument(
        "--onedir",
        action="store_true",
        help="Build sebagai folder onedir (faster startup) instead of onefile.",
    )
    parser.add_argument(
        "--skip-key-check",
        action="store_true",
        help="Skip sanity-check public/private key (NOT recommended).",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    print()
    print("#" * 72)
    print("#  WASender — production build")
    print(f"#  project root: {PROJECT_ROOT}")
    print(f"#  python      : {sys.version.split()[0]} ({sys.executable})")
    print(f"#  obfuscate   : {args.obfuscate}")
    print(f"#  onedir      : {args.onedir}")
    print("#" * 72)

    try:
        if not args.skip_key_check:
            check_keys()
        else:
            _warn("Skipping key sanity check (--skip-key-check).")

        clean_artifacts()

        env_overrides: dict[str, str] = {}
        if args.obfuscate:
            obf_root = run_pyarmor()
            # Saat obfuscate: kita harus arahkan PyInstaller ke obf source.
            # Spec saat ini hardcode path src/wasender — strategi paling
            # aman: replace src/wasender content via PYTHONPATH override.
            env_overrides["PYTHONPATH"] = (
                str(obf_root) + os.pathsep + str(SRC_DIR)
            )
            _warn(
                "Obfuscation aktif — pastikan wasender.spec membaca dari "
                f"{obf_root} (lihat env PYTHONPATH)."
            )

        run_pyinstaller(onedir=args.onedir, env_overrides=env_overrides or None)
        report_output(onedir=args.onedir)
        return 0

    except subprocess.CalledProcessError as e:
        _err(f"Subprocess failed (exit {e.returncode}): {e.cmd}")
        return e.returncode or 1
    except KeyboardInterrupt:
        _err("Build cancelled by user.")
        return 130


if __name__ == "__main__":
    sys.exit(main())
