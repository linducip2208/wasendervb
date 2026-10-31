"""Build WASenderKeygen.exe — developer-side license generator.

Output: dist/keygen/WASenderKeygen.exe (onefile, windowed).

PENTING: keygen exe ini cuma untuk DEVELOPER (kamu). JANGAN distribusi ke
end user. private_key.pem & public_key.pem TIDAK di-bundle — keygen harus
baca private_key.pem dari folder yang sama saat dijalankan.
"""
from __future__ import annotations

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent
KEYGEN_DIR = PROJECT_ROOT / "keygen"
KEYGEN_ENTRY = KEYGEN_DIR / "keygen.py"
GEN_KEYS_SCRIPT = KEYGEN_DIR / "generate_keys.py"

OUT_BASE = PROJECT_ROOT / "dist" / "keygen"
WORK_DIR = PROJECT_ROOT / "build" / "keygen"
SPEC_NAME = "WASenderKeygen"


def _info(msg: str) -> None:
    print(f"[build-keygen] {msg}")


def _err(msg: str) -> None:
    print(f"[build-keygen] ERROR: {msg}", file=sys.stderr)


def _human_size(num_bytes: int) -> str:
    size = float(num_bytes)
    for unit in ("B", "KB", "MB", "GB"):
        if size < 1024.0:
            return f"{size:.2f} {unit}"
        size /= 1024.0
    return f"{size:.2f} TB"


def clean() -> None:
    _info("Cleaning previous keygen build artifacts...")
    for target in (OUT_BASE, WORK_DIR):
        if target.exists():
            shutil.rmtree(target, ignore_errors=True)


def verify_inputs() -> None:
    if not KEYGEN_ENTRY.exists():
        _err(f"Entry script ga ditemukan: {KEYGEN_ENTRY}")
        sys.exit(2)
    if not GEN_KEYS_SCRIPT.exists():
        _err(f"generate_keys.py ga ditemukan: {GEN_KEYS_SCRIPT}")
        sys.exit(2)
    _info(f"Entry: {KEYGEN_ENTRY}")
    _info(f"Helper bundled: {GEN_KEYS_SCRIPT}")


def run_pyinstaller(onefile: bool = True) -> None:
    """Invoke PyInstaller without a spec — semua arg langsung di CLI.

    Bundling rules:
      - generate_keys.py masuk sebagai data file (so user bisa rotate keys
        kalau perlu lewat keygen UI nanti).
      - private_key.pem & public_key.pem JANGAN ikut di-bundle (defense:
        kalau exe bocor, attacker dapet private key = bencana).
    """
    cmd = [
        sys.executable,
        "-m",
        "PyInstaller",
        "--clean",
        "--noconfirm",
        "--name",
        SPEC_NAME,
        "--windowed",
        "--distpath",
        str(OUT_BASE),
        "--workpath",
        str(WORK_DIR),
        "--specpath",
        str(WORK_DIR),
    ]
    if onefile:
        cmd.append("--onefile")

    # Bundle generate_keys.py sebagai data — keygen UI bisa shell out atau
    # import lewat runtime extraction kalau ditambah fitur "rotate keys".
    # Path separator buat --add-data: di Windows pakai ';'.
    cmd.extend(["--add-data", f"{GEN_KEYS_SCRIPT}{';'}."])

    # Hidden imports yang dibutuhin keygen (cryptography lazy resolve).
    for imp in (
        "cryptography.hazmat.primitives.asymmetric.ed25519",
        "cryptography.hazmat.primitives.serialization",
    ):
        cmd.extend(["--hidden-import", imp])

    # Excludes — keygen butuh tkinter (GUI), jadi JANGAN exclude tkinter di sini.
    for exc in ("matplotlib", "scipy", "IPython", "jupyter", "pandas", "numpy"):
        cmd.extend(["--exclude-module", exc])

    cmd.append(str(KEYGEN_ENTRY))

    _info(f"$ {' '.join(cmd)}")
    subprocess.run(cmd, check=True, cwd=str(PROJECT_ROOT))


def report() -> None:
    exe = OUT_BASE / f"{SPEC_NAME}.exe"
    if not exe.exists():
        _err(f"Expected output ga ditemukan: {exe}")
        sys.exit(3)
    _info(f"Built: {exe}")
    _info(f"Size : {_human_size(exe.stat().st_size)}")
    print()
    _info("REMINDER: ini exe internal developer. JANGAN distribusi ke end user.")
    _info("Pastikan private_key.pem ada di folder yang sama saat menjalankan.")


def parse_args() -> argparse.Namespace:
    p = argparse.ArgumentParser(
        description="Build WASenderKeygen.exe (developer-only license generator)."
    )
    p.add_argument(
        "--onedir",
        action="store_true",
        help="Build sebagai folder onedir (default: onefile).",
    )
    return p.parse_args()


def main() -> int:
    args = parse_args()
    print("#" * 72)
    print("#  WASenderKeygen — developer-side build")
    print(f"#  python: {sys.version.split()[0]}")
    print(f"#  mode  : {'onedir' if args.onedir else 'onefile'}")
    print("#" * 72)

    try:
        verify_inputs()
        clean()
        run_pyinstaller(onefile=not args.onedir)
        report()
        return 0
    except subprocess.CalledProcessError as e:
        _err(f"Subprocess failed (exit {e.returncode}): {e.cmd}")
        return e.returncode or 1
    except KeyboardInterrupt:
        _err("Build cancelled by user.")
        return 130


if __name__ == "__main__":
    sys.exit(main())
