"""Feature registry & dispatch."""
from __future__ import annotations

from typing import Any, Callable

_REGISTRY: dict[str, dict] = {}


def register(key: str, *, label: str, category: str, description: str = "", requires_account: bool = True):
    def deco(fn: Callable[[dict], Any]):
        _REGISTRY[key] = {
            "key": key,
            "label": label,
            "category": category,
            "description": description,
            "requires_account": requires_account,
            "fn": fn,
        }
        return fn
    return deco


def run(key: str, params: dict) -> Any:
    entry = _REGISTRY.get(key)
    if not entry:
        raise KeyError(f"Unknown feature: {key}")
    return entry["fn"](params or {})


def catalog() -> list[dict]:
    return [
        {k: v for k, v in entry.items() if k != "fn"}
        for entry in _REGISTRY.values()
    ]


# Import all feature modules to populate registry
from .data_extractors import (  # noqa: E402, F401
    group_member,
    contact_list,
    active_members,
    chat_list,
    poll_results,
)
from .group_tools import (  # noqa: E402, F401
    grab_links,
    auto_joiner,
    bulk_add,
    group_finder,
    bulk_generator,
)
from .automations import (  # noqa: E402, F401
    responder_bot,
    number_filter,
    warmer,
    group_warmer,
)
from .lead_gen import (  # noqa: E402, F401
    gmaps_extractor,
    website_extractor,
    social_extractor,
    gcontacts_csv,
)
from .messaging import bulk_sender  # noqa: E402, F401

# Register handlers for scheduler
from ..core import scheduler  # noqa: E402

for entry in _REGISTRY.values():
    scheduler.register_feature_handler(entry["key"], entry["fn"])
