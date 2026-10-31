"""WASender license keygen — developer GUI."""
from __future__ import annotations

import base64
import json
import secrets
import sys
import tkinter as tk
from datetime import date, datetime, timedelta
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric.ed25519 import Ed25519PrivateKey


def _runtime_dir() -> Path:
    """Folder tempat persistent files berada (private_key.pem, issued/, licenses.json).

    - Saat run as .py: folder script ini.
    - Saat run as PyInstaller .exe: folder .exe (BUKAN _MEI temp extract dir,
      karena private_key.pem disimpan terpisah dari .exe by design).
    """
    if getattr(sys, "frozen", False):
        return Path(sys.executable).resolve().parent
    return Path(__file__).resolve().parent


HERE = _runtime_dir()
PRIVATE_KEY_PATH = HERE / "private_key.pem"
ISSUED_DIR = HERE / "issued"
LICENSE_DB = HERE / "licenses.json"


def load_private_key() -> Ed25519PrivateKey:
    if not PRIVATE_KEY_PATH.exists():
        raise FileNotFoundError(
            f"Private key not found at {PRIVATE_KEY_PATH}. "
            "Run `python generate_keys.py` first."
        )
    pem = PRIVATE_KEY_PATH.read_bytes()
    key = serialization.load_pem_private_key(pem, password=None)
    if not isinstance(key, Ed25519PrivateKey):
        raise ValueError("Private key is not Ed25519")
    return key


def new_license_id() -> str:
    chunk = lambda: secrets.token_hex(2).upper()
    return f"WAS-{chunk()}-{chunk()}-{chunk()}"


def issue_license(
    hwid: str,
    customer: str,
    tier: str,
    features: list[str],
    expires_at: str | None,
) -> tuple[str, dict]:
    if not expires_at:  # "" dianggap lifetime, konsisten dgn is_expired()
        expires_at = None
    if expires_at is not None:
        try:
            date.fromisoformat(expires_at)
        except ValueError:
            raise ValueError("expires_at harus YYYY-MM-DD atau kosong")
    payload = {
        "license_id": new_license_id(),
        "hwid": hwid.strip().lower(),
        "customer": customer.strip(),
        "tier": tier,
        "features": features,
        "issued_at": date.today().isoformat(),
        "expires_at": expires_at,
        "version": 1,
    }
    payload_bytes = json.dumps(payload, separators=(",", ":"), sort_keys=True).encode()
    signature = load_private_key().sign(payload_bytes)
    blob = base64.b64encode(payload_bytes).decode() + "." + base64.b64encode(signature).decode()
    armored = (
        "-----BEGIN WASENDER LICENSE-----\n"
        + "\n".join(blob[i : i + 64] for i in range(0, len(blob), 64))
        + "\n-----END WASENDER LICENSE-----\n"
    )
    return armored, payload


def append_to_db(payload: dict) -> None:
    db = []
    if LICENSE_DB.exists():
        db = json.loads(LICENSE_DB.read_text(encoding="utf-8"))
    db.append({**payload, "issued_ts": datetime.now().isoformat()})
    LICENSE_DB.write_text(json.dumps(db, indent=2), encoding="utf-8")


class App(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title("WASender Keygen — DEVELOPER ONLY")
        self.geometry("520x540")
        self.resizable(False, False)
        self._build()

    def _build(self) -> None:
        pad = {"padx": 12, "pady": 6}
        ttk.Label(self, text="WASender License Generator", font=("Segoe UI", 14, "bold")).pack(pady=(14, 4))
        ttk.Label(self, text="Issue offline-signed license bound to customer HWID", foreground="#666").pack()

        frm = ttk.Frame(self)
        frm.pack(fill="x", padx=12, pady=10)

        ttk.Label(frm, text="Customer HWID (64 hex chars):").grid(row=0, column=0, sticky="w", **pad)
        self.hwid = ttk.Entry(frm, width=58)
        self.hwid.grid(row=1, column=0, **pad)

        ttk.Label(frm, text="Customer name / email:").grid(row=2, column=0, sticky="w", **pad)
        self.customer = ttk.Entry(frm, width=58)
        self.customer.grid(row=3, column=0, **pad)

        ttk.Label(frm, text="Tier:").grid(row=4, column=0, sticky="w", **pad)
        self.tier = ttk.Combobox(frm, values=["lifetime", "yearly", "monthly", "trial"], state="readonly", width=20)
        self.tier.set("lifetime")
        self.tier.grid(row=5, column=0, sticky="w", **pad)
        self.tier.bind("<<ComboboxSelected>>", self._on_tier_change)

        ttk.Label(frm, text="Expires (YYYY-MM-DD, kosong = no expiry):").grid(row=6, column=0, sticky="w", **pad)
        self.expires = ttk.Entry(frm, width=20)
        self.expires.grid(row=7, column=0, sticky="w", **pad)

        ttk.Label(frm, text="Features (comma-separated, '*' = all):").grid(row=8, column=0, sticky="w", **pad)
        self.features = ttk.Entry(frm, width=58)
        self.features.insert(0, "*")
        self.features.grid(row=9, column=0, **pad)

        ttk.Button(self, text="Generate & save .lic", command=self._generate).pack(pady=14)

        self.status = tk.Text(self, height=8, width=60, state="disabled", bg="#f6f6f6")
        self.status.pack(padx=12, pady=4)

    def _on_tier_change(self, _evt: object) -> None:
        tier = self.tier.get()
        if tier == "trial":
            self.expires.delete(0, "end")
            self.expires.insert(0, (date.today() + timedelta(days=7)).isoformat())
        elif tier == "monthly":
            self.expires.delete(0, "end")
            self.expires.insert(0, (date.today() + timedelta(days=30)).isoformat())
        elif tier == "yearly":
            self.expires.delete(0, "end")
            self.expires.insert(0, (date.today() + timedelta(days=365)).isoformat())
        else:
            self.expires.delete(0, "end")

    def _log(self, msg: str) -> None:
        self.status.configure(state="normal")
        self.status.insert("end", msg + "\n")
        self.status.see("end")
        self.status.configure(state="disabled")

    def _generate(self) -> None:
        hwid = self.hwid.get().strip()
        customer = self.customer.get().strip()
        tier = self.tier.get()
        expires = self.expires.get().strip() or None
        features_raw = self.features.get().strip() or "*"
        features = [f.strip() for f in features_raw.split(",") if f.strip()]

        if len(hwid) < 32:
            messagebox.showerror("Invalid", "HWID looks too short (expect 64 hex chars from customer's About panel)")
            return
        if not customer:
            messagebox.showerror("Invalid", "Customer name required")
            return
        if expires:
            try:
                date.fromisoformat(expires)
            except ValueError:
                messagebox.showerror("Invalid", "Expires must be YYYY-MM-DD")
                return

        try:
            armored, payload = issue_license(hwid, customer, tier, features, expires)
        except Exception as e:
            messagebox.showerror("Error", str(e))
            return

        ISSUED_DIR.mkdir(exist_ok=True)
        out = ISSUED_DIR / f"{payload['license_id']}.lic"
        out.write_text(armored, encoding="utf-8")
        append_to_db(payload)

        self._log(f"[OK] {payload['license_id']} → {out}")
        self._log(f"     customer: {customer}")
        self._log(f"     tier:     {tier}, expires: {expires or 'never'}")
        self._log(f"     features: {features}")
        self._log("")
        messagebox.showinfo("Generated", f"License saved:\n{out}\n\nKirim file ini ke customer.")


def main() -> int:
    if not PRIVATE_KEY_PATH.exists():
        messagebox.showerror(
            "Missing private key",
            f"Run `python generate_keys.py` first.\nExpected: {PRIVATE_KEY_PATH}",
        )
        return 1
    App().mainloop()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
