"""Revision gates shared by dialogue, arbitration and the ledger scorer."""
from collections.abc import Mapping

REVISION = "karen-reliability-v1"


def enabled(session: Mapping) -> bool:
    config = session.get("adaptiveDialogue") or {}
    return config.get("version") == "lan-adaptive-v2" and config.get("dialogueRevision") == REVISION
