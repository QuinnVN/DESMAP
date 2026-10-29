"""Adaptive dialogue versions. A session's frozen ``adaptiveDialogue.version``
selects its module for the whole conversation.

- No ``adaptiveDialogue``: legacy session, no adaptive module runs.
- ``lan-adaptive-v1``: ``app/sales_adaptive.py`` (label-unlocked actions).
- ``lan-adaptive-v2``: ``app/sales_adaptive_v2.py`` (model-chosen actions).
- A missing or unknown version is unsupported. It is never mapped to the
  current default, and no session is upgraded.
"""
from __future__ import annotations

from types import ModuleType
from typing import Any, Mapping

from app import sales_adaptive as v1
from app import sales_adaptive_v2 as v2

MODULES: dict[str, ModuleType] = {v1.ADAPTIVE_VERSION: v1, v2.ADAPTIVE_VERSION: v2}
DEFAULT_VERSION = v2.ADAPTIVE_VERSION


def configured_version(settings: Any) -> str:
    value = getattr(settings, "sales_adaptive_version", DEFAULT_VERSION)
    return value if value in MODULES else DEFAULT_VERSION


def frozen_config(settings: Any) -> dict:
    """Config frozen into a new session by the configured version's module."""
    return MODULES[configured_version(settings)].frozen_config(settings)


def module_for(session: Mapping) -> ModuleType | None:
    """The module for a session's frozen config, or None when unsupported."""
    config = session.get("adaptiveDialogue")
    if not isinstance(config, Mapping):
        return None
    module = MODULES.get(config.get("version"))
    return module if module is not None and module.supported(session) else None
