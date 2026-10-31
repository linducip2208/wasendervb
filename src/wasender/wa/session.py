"""Single WhatsApp Web session — wraps a pywebview window pointing at web.whatsapp.com."""
from __future__ import annotations

import json
import threading
import time
from pathlib import Path
from typing import Any, Callable

import webview

from ..core import db
from ..core.logger import logger
from ..core.paths import wa_sessions_dir, wapi_js_path

WA_URL = "https://web.whatsapp.com/"
_WAPI_SCRIPT: str | None = None


def _get_wapi_script() -> str:
    global _WAPI_SCRIPT
    if _WAPI_SCRIPT is None:
        _WAPI_SCRIPT = Path(wapi_js_path()).read_text(encoding="utf-8")
    return _WAPI_SCRIPT


class WhatsAppSession:
    """Thin wrapper over a pywebview window that hosts WhatsApp Web for one account."""

    def __init__(self, account_id: int, label: str) -> None:
        self.account_id = account_id
        self.label = label
        self.profile_dir = wa_sessions_dir() / f"acc_{account_id}"
        self.profile_dir.mkdir(parents=True, exist_ok=True)
        self.window: webview.Window | None = None
        self._wapi_loaded = False
        self._wapi_retries = 0
        self._on_incoming: Callable[[dict], None] | None = None

    def open_window(self, hidden: bool = False) -> webview.Window:
        if self.window is not None:
            return self.window
        try:
            self.window = webview.create_window(
                title=f"WhatsApp — {self.label}",
                url=WA_URL,
                width=1100,
                height=750,
                resizable=True,
                js_api=None,
            )
        except TypeError:
            self.window = webview.create_window(
                title=f"WhatsApp — {self.label}",
                url=WA_URL,
                width=1100,
                height=750,
                resizable=True,
                js_api=None,
                hidden=hidden,
            )
        try:
            self.window.events.loaded += self._on_loaded
        except AttributeError:
            pass
        return self.window

    def _on_loaded(self) -> None:
        logger.info(f"WA window loaded for account {self.account_id} ({self.label})")
        self._schedule_wapi_injection()

    def _schedule_wapi_injection(self) -> None:
        if not self.window:
            return
        js = _get_wapi_script()
        self.window.evaluate_js(
            "(function(){"
            "  var check = function(){"
            "    if(document.readyState === 'complete' || document.readyState === 'interactive'){"
            f"      try {{ ({js})(); }} catch(e) {{ console.error('[WAPI] inject error:', e); }}"
            "    } else { setTimeout(check, 500); }"
            "  };"
            "  check();"
            "})()"
        )
        self._wapi_loaded = True
        self._wapi_retries = 0

    def _inject_wapi(self) -> None:
        """Inject WAPI synchronously (for use after page navigation)."""
        if not self.window:
            return
        js = _get_wapi_script()
        try:
            self.window.evaluate_js(f"({js})();")
            self._wapi_loaded = True
        except Exception as e:
            logger.error(f"WAPI inject failed: {e}")

    def eval_js(self, js: str, timeout: float = 30.0) -> Any:
        if not self.window:
            raise RuntimeError("Window not opened yet")
        try:
            return self.window.evaluate_js(js)
        except Exception as e:
            logger.warning(f"eval_js failed: {e}")
            raise

    def wait_for_wapi(self, timeout: float = 10.0) -> bool:
        end = time.time() + timeout
        while time.time() < end:
            try:
                ready = self.eval_js(
                    "window.WAPI && window.WAPI.__ready ? 1 : 0"
                )
                if ready == 1 or ready == "1" or ready is True:
                    return True
            except Exception:
                pass
            time.sleep(0.5)
        return False

    def get_my_jid(self) -> str | None:
        raw = self.eval_js("WAPI.getMyJid()")
        return raw if raw and raw != "null" else None

    def get_all_chats(self) -> list[dict]:
        raw = self.eval_js("JSON.stringify(WAPI.getAllChats())")
        return json.loads(raw) if raw and raw != "null" else []

    def get_all_contacts(self) -> list[dict]:
        raw = self.eval_js("JSON.stringify(WAPI.getAllContacts())")
        return json.loads(raw) if raw and raw != "null" else []

    def get_group_participants(self, group_id: str) -> list[dict]:
        raw = self.eval_js(
            f"WAPI.getGroupParticipants({json.dumps(group_id)})"
            ".then(r => JSON.stringify(r)).catch(e => JSON.stringify([]))"
        )
        return json.loads(raw) if raw and raw != "null" else []

    def get_messages(self, chat_id: str, count: int = 100) -> list[dict]:
        raw = self.eval_js(
            f"JSON.stringify(WAPI.getMessages({json.dumps(chat_id)}, {int(count)}))"
        )
        return json.loads(raw) if raw and raw != "null" else []

    def check_number(self, phone: str) -> dict:
        raw = self.eval_js(
            f"WAPI.checkNumberStatus({json.dumps(phone)})"
            ".then(r => JSON.stringify(r)).catch(e => JSON.stringify({{phone:{json.dumps(phone)}, exists: null, error: String(e)}}))"
        )
        if not raw or raw == "null":
            return {"phone": phone, "exists": None}
        return json.loads(raw)

    def send_message_by_phone(self, phone: str, text: str) -> dict:
        raw_phone = "".join(c for c in str(phone) if c.isdigit())
        send_url = f"https://web.whatsapp.com/send?phone={raw_phone}"

        logger.info(f"SEND START phone={raw_phone} len={len(text)}")
        self.window.load_url(send_url)
        time.sleep(6)

        self._inject_wapi()
        time.sleep(2)

        step1 = self.eval_js("try { JSON.stringify({title: document.title, url: window.location.href, ready: true}) } catch(e) { JSON.stringify({error: String(e)}) }")
        logger.info(f"SEND step1={step1}")

        js = (
            "WAPI._sendViaDOM(" + json.dumps(str(text)) + ")"
            ".then(r => JSON.stringify(r))"
            ".catch(e => JSON.stringify({error: 'sendViaDOM_fail: ' + String(e)}))"
        )
        raw = self.eval_js(js)
        logger.info(f"SEND raw={raw[:200] if raw else 'None'}")
        try:
            result = json.loads(raw) if raw else {"error": "no_response"}
        except Exception:
            result = {"error": "parse_error", "raw": str(raw)[:100]}

        if result.get("ok"):
            logger.info(f"SEND OK method={result.get('method', '?')}")
            self.window.load_url(WA_URL)
            return result

        logger.warning(f"SEND FAIL attempt1: {result.get('error', '?')}")
        self.window.load_url(WA_URL)
        time.sleep(4)
        self._inject_wapi()
        time.sleep(2)

        raw = self.eval_js(
            "WAPI.sendMessageByPhone(" + json.dumps(phone) + ", " + json.dumps(str(text)) + ")"
            ".then(r => JSON.stringify(r))"
            ".catch(e => JSON.stringify({error: 'sendByPhone_fail: ' + String(e)}))"
        )
        logger.info(f"SEND attempt2 raw={raw[:200] if raw else 'None'}")
        try:
            result = json.loads(raw) if raw else {"error": "no_response"}
        except Exception:
            result = {"error": "parse_error"}

        self.window.load_url(WA_URL)
        return result

    def send_media_by_phone(self, phone: str, media_path: str, caption: str = "") -> dict:
        import base64 as _b64
        import mimetypes as _mt
        from pathlib import Path as _P

        p = _P(media_path)
        if not p.exists():
            return {"error": f"file_not_found:{media_path}"}
        data = p.read_bytes()
        b64 = _b64.b64encode(data).decode()
        mime, _ = _mt.guess_type(p.name)
        mime = mime or "application/octet-stream"
        js_args = ", ".join([
            json.dumps(phone),
            json.dumps(b64),
            json.dumps(p.name),
            json.dumps(mime),
            json.dumps(caption),
        ])
        raw = self.eval_js(
            f"WAPI.sendMediaByPhone({js_args})"
            ".then(r => JSON.stringify(r)).catch(e => JSON.stringify({error: String(e)}))"
        )
        return json.loads(raw) if raw else {"error": "no_response"}

    def get_poll_results(self, chat_id: str, msg_id: str) -> dict | None:
        raw = self.eval_js(
            f"JSON.stringify(WAPI.getPollResults({json.dumps(chat_id)}, {json.dumps(msg_id)}))"
        )
        return json.loads(raw) if raw and raw != "null" else None

    def register_incoming_handler(self, callback: Callable[[dict], None]) -> None:
        self._on_incoming = callback
        self.eval_js(
            "WAPI.onIncomingMessage(function(m){"
            "  try { window.pywebview.api._on_wa_incoming("
            f"   JSON.stringify({{accountId: {self.account_id}, msg: m}}));"
            "  } catch(e) { console.error('[WAPI] bridge error:', e); }"
            "})"
        )

    def close(self) -> None:
        if self.window:
            try:
                self.window.destroy()
            except Exception:
                pass
            self.window = None
