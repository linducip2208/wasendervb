# WASender Architecture

## Process model

WASender runs as a single Python process that owns multiple `pywebview` (WebView2) windows:

- **One main window** hosts the application UI (HTML/CSS/JS shipped under `src/wasender/ui/`). This is where the user manages accounts, contacts, schedules, settings, and licensing.
- **One window per WhatsApp account** is spawned on demand by `wa.account_manager`. Each loads `https://web.whatsapp.com` with a dedicated user-data directory under `%APPDATA%\WASender\wa_sessions\<account_id>\`, giving every account its own cookies, local storage, and login state — exactly like running multiple Chrome profiles. After WA Web finishes loading, `wa/wapi.js` is injected to expose a stable JS surface (`window.WAPI.*`) on top of WhatsApp's webpack modules.

Background jobs (scheduler, auto-responder polling) live in regular Python threads inside the main process. There is no separate worker daemon.

## Data flow

```
[ Browser UI (HTML/JS) ]
        │  pywebview.api.<method>(...)
        ▼
[ api/bridge.py ]              <-- single typed entry point
        │
        ├── DB read/write ──────────────► core/db.py (sqlite)
        ├── Feature dispatch ───────────► features/<module>  (registered via @register)
        └── WA action ─────────────────► wa.account_manager.session.evaluate_js(...)
                                                          │
                                                          ▼
                                                  WAPI hook in WA window
```

Feature modules are registered through `features/__init__.py` with a `@register("feature_key", ...)` decorator. The bridge picks the right handler purely by key — UI never hard-codes module paths, which keeps the surface uniform across all 19 features.

## License flow

1. Developer runs `keygen/keygen.py` (Tk GUI) on a private machine. It signs the JSON payload with the **Ed25519 private key** kept off-repo (`keygen/private_key.pem`, gitignored).
2. The signed payload is base64-encoded, joined to its signature with `.`, wrapped in `-----BEGIN/END WASENDER LICENSE-----` armor, and written to a `.lic` file.
3. Customer pastes the file contents into the activation screen. `core/license.parse_license_text` strips the armor, splits on `.`, verifies the signature against the **embedded public key** (`src/wasender/core/public_key.pem`), then enforces `hwid == get_hwid()` and the optional `expires_at`.
4. Valid licenses are persisted at `%APPDATA%\WASender\license.lic` and reloaded on every launch.

## Anti-ban

`core/ratelimit.py` enforces two layers per account:

- **Random delay** between sends (`delay_min_seconds`..`delay_max_seconds`, defaults 3–15s).
- **Daily quota** (`daily_quota`, default 200/day). Counters reset automatically when `last_reset != today`.

Send paths must call `acquire_send_slot(account_id)` before invoking WAPI. Outside of this, features can sequence work however they like.

## DOM-change resilience

WhatsApp Web ships frequent UI rewrites. Two defenses:

- `wa/selectors.json` — every CSS/XPath selector lives in a single JSON file, hot-swappable without touching code.
- `wa/wapi.js` — hooks WhatsApp's webpack module loader and pulls stable internal modules (Store, Wap, etc.) by signature, not by class name. This survives most cosmetic releases.

If WhatsApp ships a major rewrite, an auto-updater (planned) will let us push a new `selectors.json` + `wapi.js` without rebuilding the whole installer.

## Measurement

Every send is recorded in `message_log` with three orthogonal axes:

- `direction`: `'out'` (bulk/warmer/responder) vs `'in'` (incoming DMs, logged by `responder_bot.on_message`, groups skipped).
- `kind`: `'bulk'` / `'warmer'` / `'inbox'`.
- `campaign`: free-text label from bulk-sender params (`NULL` = uncategorized).

Reply-rate per campaign = distinct `in`-phones that previously received an `out`-message in the same campaign (`o.sent_at <= m.sent_at`, phones normalized to digits). Dashboard aggregates (`bridge.send_heatmap`, `account_health`, `campaigns`, `alerts`) read only — no extra writes.
