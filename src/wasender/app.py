"""Main app entry — boots license check + main UI window."""
from __future__ import annotations

import sys
import traceback

import webview

from .api.bridge import Api
from .core import db
from .core.license import load_active_license
from .core.logger import logger, setup_logging
from .core.paths import ui_dir
from .wa import account_manager


def _start_main_window(api: Api, has_license: bool) -> None:
    entry = "index.html" if has_license else "login.html"
    url = (ui_dir() / entry).as_uri()
    try:
        webview.create_window(
            title="WASender",
            url=url,
            js_api=api,
            width=1180,
            height=820,
            min_size=(960, 640),
            resizable=True,
        )
        logger.info(f"Main window created: {entry}")
    except Exception as e:
        logger.error(f"Failed to create main window: {e}")
        raise


def main() -> int:
    try:
        setup_logging()
        logger.info("WASender v6.0.1 starting...")

        db._init_if_needed()
        logger.info("DB initialized")

        api = Api()
        lic = load_active_license()
        if lic:
            logger.info(f"Active license: {lic.license_id} ({lic.tier}, customer={lic.customer})")
        else:
            logger.warning("No valid license — boot to license entry screen")

        _start_main_window(api, has_license=bool(lic))

        try:
            webview.start(debug=False)
        except KeyboardInterrupt:
            logger.info("Interrupted by user")
        except Exception as e:
            logger.error(f"WebView error: {e}")
            return 1
        finally:
            logger.info("Shutdown — closing all WA sessions")
            account_manager.close_all()

        return 0

    except Exception as e:
        logger.error(f"Fatal startup error: {e}")
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
