"""Generation-only replay benchmark: adaptive v1 vs v2, Qwen thinking off vs on.

Four arms over the same cases and repeats (10 x 3 x 4 = 120 samples):
  v1_off / v1_on  lan-adaptive-v1 from the hash-pinned snapshot
                  scripts/benchmarks/sales_adaptive_v1.py: v1 prompt, v1
                  label-unlocked policy, v1 validator and v1 glue.
  v2_off / v2_on  lan-adaptive-v2 (app/sales_adaptive_v2.py): model-chosen
                  actions, assertion-aware guard, warnings that never repair,
                  and one language/coherence verifier request (reasoning off)
                  per locally valid candidate.
Every arm uses the same model, provider pin, temperature, max_tokens and
deadline. Within one version the generation payloads are identical except
``reasoning``; the v2 verifier payload is the same in both v2 arms.
Every HTTP request is recorded with its role (``generation`` or ``guard``);
tokens, latency and cost are reported in total and per role.
v1 and v2 differ in prompt, context shape, allowed actions and validator by
design; that comparison is descriptive, not a single-factor experiment.

Only Lan's response generation is timed: no STT, Jev/Luna or TTS.

Case inputs (``--source``):
  cached   (default) the exact v1 model contexts, v1 policies and legacy plan
           fields recorded by an earlier v1 run (``--cached-run``, under the
           ignored output/ folder), for when the private source session files
           are unavailable. Each cached context must hash to the recorded
           contextSha256 and to every live sample of that case; its dialogue
           prefix, current utterance and labels must equal the discovery
           manifest's prior dialogue and selected-turn labels; and no later
           text, selected reply, private ID or scoring field may appear. The v2
           context is rendered from the same base by
           ``sales_adaptive_v2.render_context``.
  rebuild  rebuild each case from its source session with
           scripts/sales_adaptive_replay.py (needs the recordings/backup files
           under --project-root, with matching SHA-256).

Modes:
  dry-run  network-free: SIMULATED local transport, no key, no cost.
  live     paid OpenRouter calls, serial. Requires --confirm-paid.
           --resume <run dir> refuses any source, prompt, config, case or
           schedule mismatch and skips finished samples.
"""
from __future__ import annotations

import argparse
import asyncio
import hashlib
import importlib.util
import json
import random
import sys
import time
from collections import Counter
from dataclasses import replace
from datetime import datetime, timezone
from pathlib import Path
from types import ModuleType
from typing import Mapping

import httpx

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from app import sales_adaptive_v2 as v2  # noqa: E402
from app.config import get_settings  # noqa: E402
from app.sales_adaptive import AdaptiveRequestConfig  # noqa: E402
from app.sales_openrouter import QWEN_MODEL  # noqa: E402
from scripts.benchmark_sales_adaptive import (RATE_CARD_REFERENCE, RecordingClient, _token_totals,  # noqa: E402
                                              payload_sha_without_reasoning, quantile, stats)

OUTPUT_ROOT = ROOT.parent / "output" / "sales-adaptive-benchmark"
CACHED_RUN = OUTPUT_ROOT / "runs" / "live-20261028T082844Z"
DISCOVERY_MANIFEST = OUTPUT_ROOT / "discovery" / "selection_manifest.json"
EFFECTIVE_MANIFEST = OUTPUT_ROOT / "effective_manifest.json"
SAMPLE_SCHEMA = "sales-adaptive-arms-sample-v2"
ARMS = {"v1_off": ("lan-adaptive-v1", "off"), "v1_on": ("lan-adaptive-v1", "on"),
        "v2_off": ("lan-adaptive-v2", "off"), "v2_on": ("lan-adaptive-v2", "on")}
# Immutable v1 baseline. Line endings are normalized so a CRLF or LF checkout
# gives the same digest; the prompt/schema digests are those recorded by the
# original live run (runs/live-20261028T082844Z/config.json).
V1_SNAPSHOT = ROOT / "scripts" / "benchmarks" / "sales_adaptive_v1.py"
V1_SNAPSHOT_SHA256_LF = "16102fdef74c0ea6a914fc9229d73f946dd4f0ee8babf71410b11744121f9d6c"
V1_PROMPT_SHA256 = "89b5d8833a88190e1c9f9c9f9382936fb05ad614d14ec0b66c0a4b92012d857d"
V1_SCHEMA_SHA256 = "6e43e3222a226818087618978264872aa811eef8626fd2bf7f91f8bf676316ae"
# Code that shapes any arm's request or verdict. Resume refuses changes.
SOURCE_FILES = ("scripts/benchmarks/sales_adaptive_v1.py", "app/sales_adaptive.py", "app/sales_adaptive_v2.py",
                "app/sales_adaptive_guard.py", "app/sales_adaptive_v2_original_prompt.py",
                "app/sales_openrouter.py", "app/sales_customer_lines.py",
                "scripts/benchmark_sales_adaptive_arms.py", "scripts/benchmark_sales_adaptive.py")
# Old artifacts recorded paths from the previous workspace; read-only rebase.
PATH_REBASE = {"D:\\NCKH\\NCKH-AI": str(ROOT), "D:\\NCKH\\output": str(ROOT.parent / "output")}


def sha256_bytes(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def sha256_text(value: str) -> str:
    return sha256_bytes(value.encode("utf-8"))


def sha256_lf(path: Path) -> str:
    return sha256_bytes(Path(path).read_bytes().replace(b"\r\n", b"\n"))


def rebase(path: str | Path) -> Path:
    text = str(path)
    for old, new in PATH_REBASE.items():
        if text.lower().startswith(old.lower()):
            return Path(new + text[len(old):])
    return Path(text)


# ---------------------------------------------------------------- v1 baseline

def load_v1_snapshot(path: Path = V1_SNAPSHOT) -> ModuleType:
    """Import the frozen v1 module after verifying its hash and prompt/schema digests."""
    digest = sha256_lf(path)
    if digest != V1_SNAPSHOT_SHA256_LF:
        raise RuntimeError(f"v1 snapshot hash {digest} != pinned {V1_SNAPSHOT_SHA256_LF}")
    name = "sales_adaptive_v1_snapshot"
    if name in sys.modules:
        return sys.modules[name]
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module  # dataclasses resolve their module through sys.modules
    spec.loader.exec_module(module)
    if (module.PROMPT_SHA256, module.SCHEMA_SHA256) != (V1_PROMPT_SHA256, V1_SCHEMA_SHA256):
        del sys.modules[name]
        raise RuntimeError("v1 snapshot prompt/schema digests differ from the recorded live run")
    return module


def source_hashes(root: Path = ROOT) -> dict:
    return {name: sha256_lf(root / name) for name in SOURCE_FILES}


# ---------------------------------------------------------------- cases

def plan_from_cache(case: Mapping) -> dict:
    """The writer-plan fields the v1/v2 validators and fallback read, from a cached case.

    knownFacts/allowedNewFacts/factTexts come from the exact facts the model
    saw; requiredFactIds, intent and fallbackText from the recorded legacy plan.
    For confirm_fact the recorded policy already merged confirmedFactIds."""
    context, policy, legacy = case["modelContext"], case["policy"], case["legacyPlan"]
    facts = context["facts"]
    known = sorted(facts["sharedFacts"])
    texts = {**facts["sharedFacts"], **facts["mayDiscloseNow"]}
    return {"intent": legacy["intent"], "knownFacts": known, "allowedNewFacts": list(facts["mayDiscloseNow"]),
            "factTexts": texts, "requiredFactIds": list(legacy["requiredFactIds"]),
            "confirmedFactIds": list(policy["requiredFactIds"]) if legacy["intent"] == "confirm_fact" else [],
            "requiredContent": list(legacy["requiredContent"]), "requiredPhrases": [],
            "avoidReplyTexts": list(context.get("recentLanReplies") or []), "fallbackText": legacy["fallbackText"],
            "allowedVolunteeredFacts": [], "hostile": bool((context.get("observations") or {}).get("hostile")),
            "ending": policy.get("deterministicEnding")}


def _historical_labels(assessment: Mapping) -> set[str]:
    from scripts.sales_adaptive_replay import labels_for
    labels, _ = labels_for({"turnAssessment": assessment, "policyViolations": assessment.get("policyViolations") or []},
                           None)
    return {name for name, value in labels.items() if value.get("status") == "true"}


def cached_checks(case_id: str, case: Mapping, effective: Mapping, discovery: Mapping | None,
                  live_hashes: set[str]) -> list[str]:
    """Errors proving a cached context is the recorded, prefix-only input."""
    errors = []
    context = case["modelContext"]
    digest = sha256_text(json.dumps(context, ensure_ascii=False, sort_keys=True))
    if digest != case.get("contextSha256"):
        errors.append("cached context does not hash to its recorded contextSha256")
    if not live_hashes:
        errors.append("cached case has no live sample context hash")
    elif live_hashes != {digest}:
        errors.append("cached context differs from the live run samples' contextSha256")
    index = int(effective["savedTurnIndex1Based"])
    if discovery is None:
        errors.append("cached case has no discovery reference for prefix and leakage checks")
    else:
        prior = discovery["replayInput"]["priorDialogue"]
        expected = [(item["salespersonTranscript"], item.get("customerText")) for item in prior[:index - 1]]
        actual = [(item.get("salesperson"), item.get("lan")) for item in context["conversation"] if "salesperson" in item]
        if actual != expected:
            errors.append("dialogue prefix differs from discovery priorDialogue")
        selected = (prior[index - 1]["salespersonTranscript"] if index - 1 < len(prior)
                    else discovery["replayInput"]["salespersonTranscript"])
        if context["currentSalespersonUtterance"] != selected[:1200]:
            errors.append("current utterance differs from the discovery transcript")
        if not context["conversation"] or set(context["conversation"][0]) != {"lan"}:
            errors.append("conversation does not start with Lan's opening complaint")
    assessment = (effective.get("historicalReference") or {}).get("turnAssessment")
    if isinstance(assessment, Mapping):
        hinted = set((context.get("observations") or {}).get("recognizedActs") or [])
        if hinted != _historical_labels(assessment):
            errors.append("recognized acts differ from the selected turn's own labels")
    else:
        errors.append("cached case has no selected-turn assessment reference")
    return errors


def leakage_findings(messages: list, case: Mapping, effective: Mapping, discovery: Mapping | None) -> list[str]:
    """Model input must not hold the selected reply, later turns, private IDs or scoring fields."""
    text = "\n".join(message["content"] for message in messages)
    context = case["modelContext"]
    prefix = "\n".join([item.get(key) or "" for item in context["conversation"] for key in ("lan", "salesperson")]
                       + [context["currentSalespersonUtterance"], *context["facts"]["sharedFacts"].values(),
                          *context["facts"]["mayDiscloseNow"].values()])
    findings = []
    future = [((effective.get("historicalReference") or {}).get("customerText"), "historical_selected_reply")]
    if discovery is not None:
        index = int(effective["savedTurnIndex1Based"])
        prior = discovery["replayInput"]["priorDialogue"]
        future += [(item.get("customerText"), "future_turn_customerText") for item in prior[index - 1:]]
        future += [(item.get("salespersonTranscript"), "future_turn_transcript") for item in prior[index:]]
        if discovery.get("turnId") != effective.get("turnId"):
            future += [(discovery["replayInput"].get("salespersonTranscript"), "future_turn_transcript"),
                       ((discovery.get("historicalReference") or {}).get("customerText"), "future_turn_customerText")]
    for value, code in future:
        # Lan sometimes repeated an earlier line; text already in the prefix is not future.
        if isinstance(value, str) and len(value) > 20 and value in text and value not in prefix:
            findings.append(code)
    for key in ("sessionId", "runId", "part1AttemptId", "participantName"):
        value = effective.get(key) or (discovery or {}).get(key)
        if isinstance(value, str) and value and value in text:
            findings.append("private_id_" + key)
    for marker in ('"score"', '"customerRating"', '"trustState"', '"rubricComponents"', '"turnQuality"', '"noul"',
                   '"evidenceLedger"'):
        if marker in text:
            findings.append("scoring_field_" + marker.strip('"'))
    return sorted(set(findings))


def load_cached_cases(cached_run: Path, effective_path: Path, discovery_path: Path | None,
                      case_filter: set[str] | None = None) -> tuple[list[dict], dict]:
    effective = {case["caseId"]: case for case in json.loads(effective_path.read_text(encoding="utf-8"))["cases"]}
    discovery = ({case["caseId"]: case for case in json.loads(discovery_path.read_text(encoding="utf-8"))["cases"]}
                 if discovery_path and discovery_path.exists() else {})
    live_hashes: dict[str, set[str]] = {}
    samples_path = cached_run / "samples.jsonl"
    if samples_path.exists():
        for line in samples_path.read_text(encoding="utf-8").splitlines():
            if line.strip():
                sample = json.loads(line)
                if sample.get("contextSha256"):
                    live_hashes.setdefault(sample["caseId"], set()).add(sample["contextSha256"])
    checks = {"source": "cached", "cachedRun": str(cached_run), "errors": [], "cases": {}}
    items = []
    for path in sorted((cached_run / "cases").glob("*.json")):
        case_id = path.stem
        if case_filter and case_id not in case_filter:
            continue
        case = json.loads(path.read_text(encoding="utf-8"))
        if case_id not in effective:
            checks["errors"].append(f"{case_id}: not in effective manifest")
            continue
        errors = cached_checks(case_id, case, effective[case_id], discovery.get(case_id), live_hashes.get(case_id, set()))
        if case["modelContext"]["currentSalespersonUtterance"] != case["modelContext"]["currentSalespersonUtterance"][:1200]:
            errors.append("utterance truncated")
        checks["cases"][case_id] = {"caseFileSha256": sha256_bytes(path.read_bytes()),
                                    "contextSha256": case["contextSha256"],
                                    "liveDistinctContextHashes": len(live_hashes.get(case_id, ())),
                                    "discoveryCrossChecked": case_id in discovery, "errors": errors}
        checks["errors"] += [f"{case_id}: {error}" for error in errors]
        items.append({"caseId": case_id, "effective": effective[case_id], "discovery": discovery.get(case_id),
                      "v1Policy": case["policy"], "v1Context": case["modelContext"], "plan": plan_from_cache(case),
                      "transcript": case["modelContext"]["currentSalespersonUtterance"],
                      "provenance": {"source": "cached replay artifact", "caseFile": path.relative_to(cached_run.parent.parent).as_posix(),
                                     "caseFileSha256": checks["cases"][case_id]["caseFileSha256"],
                                     "replayProvenance": case.get("provenance")},
                      "cachedCase": case})
    return items, checks


def load_rebuilt_cases(manifest_path: Path, project_root: Path, v1: ModuleType,
                       case_filter: set[str] | None = None) -> tuple[list[dict], dict]:
    from scripts.sales_adaptive_replay import effective_manifest, reconstruct_case, sha256_file
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    checks = {"source": "rebuild", "errors": [], "cases": {}}
    for case in manifest["cases"]:
        path = project_root / case["sourceRelativePath"]
        if not path.exists() or sha256_file(path) != case["sourceSha256"]:
            checks["errors"].append(f"{case['caseId']}: source missing or hash differs")
    if checks["errors"]:
        return [], checks
    effective = effective_manifest(manifest, project_root)
    items = []
    for case in effective["cases"]:
        if case_filter and case["caseId"] not in case_filter:
            continue
        rebuilt = reconstruct_case(case, project_root)
        args = (rebuilt["before"], rebuilt["candidate"], rebuilt["decision"], rebuilt["plan"], rebuilt["labels"])
        v1_policy = v1.turn_policy(*args)
        v1_context = v1.model_context(*args, rebuilt["transcript"], v1_policy)
        if v2.turn_policy(*args) != v2.policy_from_limits(v2.limits_from_v1_policy(v1_policy)):
            checks["errors"].append(f"{case['caseId']}: v2 policy differs from its v1-derived limits")
        cached = {"modelContext": v1_context, "contextSha256": sha256_text(json.dumps(v1_context, ensure_ascii=False,
                                                                                       sort_keys=True))}
        items.append({"caseId": case["caseId"], "effective": case, "discovery": None, "v1Policy": v1_policy,
                      "v1Context": v1_context, "plan": rebuilt["plan"], "transcript": rebuilt["transcript"],
                      "provenance": {"source": "rebuilt from session file", "replayProvenance": rebuilt["provenance"]},
                      "cachedCase": cached})
    for case in manifest["cases"]:
        if sha256_file(project_root / case["sourceRelativePath"]) != case["sourceSha256"]:
            checks["errors"].append(f"{case['caseId']}: source hash changed during replay")
    return items, checks


def prepare(items: list[dict], v1: ModuleType, checks: dict) -> list[dict]:
    """Per-version policy, context and messages, plus leakage findings."""
    for item in items:
        v2_policy = v2.policy_from_limits(v2.limits_from_v1_policy(item["v1Policy"]))
        v2_context = v2.render_context(item["v1Context"], v2_policy)
        item["versions"] = {
            "lan-adaptive-v1": {"policy": item["v1Policy"], "context": item["v1Context"],
                                "messages": v1.build_messages(item["v1Context"])},
            "lan-adaptive-v2": {"policy": v2_policy, "context": v2_context, "messages": v2.build_messages(v2_context)},
        }
        for version, data in item["versions"].items():
            data["contextSha256"] = sha256_text(data["messages"][1]["content"])
            findings = leakage_findings(data["messages"], item["cachedCase"], item["effective"], item["discovery"])
            checks.setdefault("leakage", {}).setdefault(item["caseId"], {})[version] = findings
            if findings:
                checks["errors"].append(f"{item['caseId']} {version}: model input leaks {findings}")
        if item["versions"]["lan-adaptive-v1"]["contextSha256"] != item["cachedCase"]["contextSha256"]:
            checks["errors"].append(f"{item['caseId']}: v1 messages differ from the recorded context")
    sessions = {item["effective"].get("sessionId") for item in items}
    checks["caseCount"], checks["distinctSessions"] = len(items), len(sessions)
    return items


# ---------------------------------------------------------------- arms and schedule

def arm_configs(args) -> dict[str, AdaptiveRequestConfig]:
    provider = None if args.no_provider_pin else {"order": [args.provider], "allow_fallbacks": False,
                                                   "require_parameters": True}
    base = AdaptiveRequestConfig(max_tokens=args.max_tokens, reasoning_max_tokens=args.reasoning_budget or None,
                                 timeout_seconds=args.timeout, temperature=args.temperature, provider=provider)
    selected = set(getattr(args, "arms", ",".join(ARMS)).split(","))
    return {arm: replace(base, version=version, reasoning=reasoning)
            for arm, (version, reasoning) in ARMS.items() if arm in selected}


def schedule(case_ids: list[str], repeats: int, seed: int, arms=None) -> list[dict]:
    """Serial and interleaved. Each case runs its four arms back to back in a
    seeded order that is rotated every repeat (each arm takes a new position);
    case order is reshuffled per repeat."""
    rng = random.Random(seed)
    selected = tuple(ARMS if arms is None else arms)
    bases = {}
    for case_id in case_ids:
        base = list(selected)
        rng.shuffle(base)
        bases[case_id] = base
    plan, seq = [], 0
    for repeat in range(1, repeats + 1):
        order = list(case_ids)
        rng.shuffle(order)
        for case_id in order:
            shift = (repeat - 1) % len(selected)
            arms = bases[case_id][shift:] + bases[case_id][:shift]
            for position, arm in enumerate(arms):
                seq += 1
                plan.append({"seq": seq, "repeat": repeat, "caseId": case_id, "arm": arm,
                             "version": ARMS[arm][0], "reasoning": ARMS[arm][1], "positionInBlock": position})
    return plan


def payload_checks(items: list[dict], configs: Mapping, settings, v1: ModuleType) -> dict:
    """Within a version only ``reasoning`` differs; across versions the request
    settings other than version/reasoning are equal."""
    result = {"identicalExceptReasoningWithinVersion": True, "sharedRequestSettingsAcrossArms": True}
    for item in items:
        for version in ("lan-adaptive-v1", "lan-adaptive-v2"):
            selected = [arm for arm in configs if ARMS[arm][0] == version]
            hashes = {payload_sha_without_reasoning(_payload(item, arm, configs[arm], settings, v1)) for arm in selected}
            if len(hashes) > 1:
                result["identicalExceptReasoningWithinVersion"] = False
    shared = [{key: value for key, value in config.redacted().items() if key not in ("version", "reasoning")}
              for config in configs.values()]
    result["sharedRequestSettingsAcrossArms"] = all(item == shared[0] for item in shared)
    return result


def _module(arm: str, v1: ModuleType) -> ModuleType:
    return v1 if ARMS[arm][0] == "lan-adaptive-v1" else v2


def _payload(item: Mapping, arm: str, config, settings, v1: ModuleType) -> dict:
    module = _module(arm, v1)
    responder = module.AdaptiveLanResponder(settings, config=config)
    return module.build_payload(item["versions"][ARMS[arm][0]]["messages"], config, responder.provider_preferences())


# ---------------------------------------------------------------- transport

class CandidateRecordingClient(RecordingClient):
    """RecordingClient plus each request's role and, by role, the parsed
    generation candidate (redacted, bounded) or the verifier's verdict codes.
    Reasoning text, headers, rationales and raw bodies are never kept."""

    def __init__(self, client, key: str = ""):
        super().__init__(client)
        self.key = key

    async def post(self, url, **kwargs):
        role = v2.request_role(kwargs.get("json") or {})
        count = len(self.records)
        try:
            response = await super().post(url, **kwargs)
        finally:
            if len(self.records) > count:
                self.records[-1]["role"] = role
        try:
            body = response.json()
            choice = (body.get("choices") or [{}])[0]
            content = (choice.get("message") or {}).get("content")
        except (ValueError, AttributeError, IndexError, TypeError):
            content = None
        if role == "guard":
            prompt = ((kwargs.get("json") or {}).get("messages") or [{}])[0].get("content")
            revision = v2.DIALOGUE_REVISION if prompt == v2.LANGUAGE_GUARD_PROMPT else None
            verdict = v2.parse_verdict(content, revision=revision)
            self.records[-1].update(verdict=verdict["verdict"] if verdict else None,
                                    verdictCodes=verdict["codes"] if verdict else [])
        else:
            self.records[-1]["candidate"] = v2.candidate_record(content, None, key=self.key)
        return response


# A made-up phrase used only by the dry-run simulator; production code holds no phrase list.
SIMULATED_NONSENSE = "vặn xoáy pha"


def simulated_transport() -> httpx.MockTransport:
    """Local stand-in for dry-run: plausible JSON and synthetic usage. Never model output.

    Deterministic per request. Some utterances (the same ones for v1 and v2)
    get a first reply that breaks policy, so the repair path, repair usage and
    diagnostics run too. Others get a first reply holding a SIMULATED
    meaningless phrase that the simulated verifier rejects, so the v2
    regeneration path and verifier accounting run (v1 has no verifier)."""

    def handler(request: httpx.Request) -> httpx.Response:
        payload = json.loads(request.content)
        reasoning = (payload.get("reasoning") or {}).get("enabled")
        user = next(message["content"] for message in payload["messages"] if message["role"] == "user")
        if v2.request_role(payload) == "guard":
            line = json.loads(user)["lanLine"]
            verdict = ({"verdict": "reject", "codes": ["incoherent_text"]} if SIMULATED_NONSENSE in line
                       else {"verdict": "pass", "codes": []})
            if "salespersonClarity" in payload["messages"][0]["content"]:
                verdict["salespersonClarity"] = "clear"
            body = {"id": "sim-" + sha256_bytes(request.content)[:12], "model": payload["model"], "provider": "Simulated",
                    "choices": [{"finish_reason": "stop", "message": {"content": json.dumps(verdict)}}],
                    "usage": {"prompt_tokens": len(request.content) // 4, "completion_tokens": 12,
                              "total_tokens": len(request.content) // 4 + 12,
                              "completion_tokens_details": {"reasoning_tokens": 0},
                              "prompt_tokens_details": {"cached_tokens": 0}, "cost": None}}
            return httpx.Response(200, json=body)
        repair = len(payload["messages"]) > 2
        context = json.loads(user)
        facts = context["facts"]
        texts = {**facts["sharedFacts"], **facts["mayDiscloseNow"]}
        must = [fact for fact in facts["mustAnswerFactIds"] if fact in texts]
        prefix = " ".join(texts[fact] for fact in must)
        allowed = context["allowedActions"]
        digest = int(sha256_text(context["currentSalespersonUtterance"])[:8], 16)
        if context["limits"]["deterministicEnding"]:
            content = {"action": "close_conversation", "decision": "close", "closeReason": "deterministic_ending",
                       "customerText": (prefix + " Chị dừng ở đây, chuyện này vẫn chưa giải quyết được.").strip()}
        elif digest % 3 == 0 and not repair:
            content = {"action": "request_next_step", "decision": "continue", "closeReason": None,
                       "customerText": (prefix + " Cửa hàng đã đồng ý hoàn tiền cho chị rồi mà.").strip()}
        else:
            action = "request_next_step" if "request_next_step" in allowed else allowed[0]
            text = (f" Em {SIMULATED_NONSENSE} giúp chị đôi này được không?" if digest % 3 == 1 and not repair
                    else " Em nói cụ thể giúp chị bên mình sẽ kiểm tra độ vừa thế nào được không?")
            content = {"action": action, "decision": "close" if action == "close_conversation" else "continue",
                       "closeReason": "needs_time" if action == "close_conversation" else None,
                       "customerText": (prefix + text).strip()}
        content["factIds"] = must
        completion = 60 + (120 if reasoning else 0)
        body = {"id": "sim-" + sha256_bytes(request.content)[:12], "model": payload["model"], "provider": "Simulated",
                "choices": [{"finish_reason": "stop", "message": {"content": json.dumps(content, ensure_ascii=False)}}],
                "usage": {"prompt_tokens": len(request.content) // 4, "completion_tokens": completion,
                          "total_tokens": len(request.content) // 4 + completion,
                          "completion_tokens_details": {"reasoning_tokens": 120 if reasoning else 0},
                          "prompt_tokens_details": {"cached_tokens": 0}, "cost": None}}
        return httpx.Response(200, json=body)

    return httpx.MockTransport(handler)


# ---------------------------------------------------------------- samples

async def v1_turn(v1: ModuleType, messages: list, policy: Mapping, plan: Mapping, transcript: str, *, responder,
                  config) -> dict:
    """``v1.run_adaptive_turn`` from its response step on: the v1 validator,
    deadline, late re-check and fallback, with the context already built."""
    validate = lambda output: v1.output_rejections(output, policy, plan, transcript)
    try:
        metadata = await asyncio.wait_for(responder.respond(messages, validate), config.timeout_seconds + .25)
        output = metadata.get("output")
        if output is not None:
            late = validate(output)
            if late:
                metadata = dict(metadata, output=None, errorCode="adaptive_guard_rejected", rejectionCodes=late)
                output = None
    except Exception as exc:
        metadata = {"output": None, "attempts": [], "errorCode": "adaptive_timeout" if isinstance(exc, TimeoutError)
                    else "adaptive_failed", "costUsd": None}
        output = None
    source = "model" if output is not None else "fallback"
    return {"source": source, "final": dict(output) if output is not None else v1.fallback_output(plan, policy),
            "metadata": metadata}


def _role(record: Mapping) -> str:
    return record.get("role") or "generation"


def _role_cost(records: list[dict]) -> float | None:
    """Cost of one role's requests: 0 when there were none, null when any is unknown."""
    costs = [record["usage"].get("costUsd") for record in records]
    return None if any(cost is None for cost in costs) else float(sum(costs))


def _finish(sample: dict, records: list[dict], wall: float) -> dict:
    costs = [record["usage"].get("costUsd") for record in records]
    missing = sum(1 for cost in costs if cost is None)
    generation = [record for record in records if _role(record) == "generation"]
    guards = [record for record in records if _role(record) == "guard"]
    kinds = [item.get("kind") for item in sample.get("responderAttempts") or []]
    sample.update(httpAttempts=records, attemptCount=len(records), generationAttempts=len(generation),
                  guardAttempts=len(guards), repairAttempts=kinds.count("repair"),
                  regenerateAttempts=kinds.count("regenerate"), retryAttempts=kinds.count("retry"),
                  failedAttempts=sum(1 for record in records if record.get("errorCode")),
                  firstAttemptLatencySeconds=generation[0]["latencySeconds"] if generation else None,
                  requestLatencySeconds=[record["latencySeconds"] for record in records],
                  generationLatencySeconds=[record["latencySeconds"] for record in generation],
                  guardLatencySeconds=[record["latencySeconds"] for record in guards],
                  guardVerdicts=[record.get("verdict") for record in guards],
                  wallClockReadySeconds=round(wall, 4), tokens=_token_totals(records),
                  tokensByRole={"generation": _token_totals(generation), "guard": _token_totals(guards)},
                  costUsd=sum(costs) if costs and not missing else None, costMissingAttempts=missing,
                  costUsdByRole={"generation": _role_cost(generation), "guard": _role_cost(guards)},
                  servedProviders=sorted({record.get("provider") for record in records if record.get("provider")}),
                  servedModels=sorted({record.get("servedModel") for record in records if record.get("servedModel")}),
                  finishReasons=[record.get("finishReason") for record in records])
    return sample


async def run_sample(item: Mapping, slot: Mapping, config, settings, http_client, v1: ModuleType,
                     fingerprint: str, simulated: bool) -> dict:
    arm, version = slot["arm"], slot["version"]
    data = item["versions"][version]
    module = _module(arm, v1)
    recorder = CandidateRecordingClient(http_client, settings.openrouter_api_key or "")
    responder = module.AdaptiveLanResponder(settings, client=recorder, config=config)
    payload = module.build_payload(data["messages"], config, responder.provider_preferences())
    sample = {"schemaVersion": SAMPLE_SCHEMA, "runFingerprint": fingerprint, "simulated": simulated,
              **{key: slot[key] for key in ("seq", "repeat", "caseId", "arm", "version", "reasoning", "positionInBlock")},
              "startedAtUtc": datetime.now(timezone.utc).isoformat(), "requestConfig": config.redacted(),
              "promptSha256": module.PROMPT_SHA256, "schemaSha256": module.SCHEMA_SHA256,
              "contextSha256": data["contextSha256"], "payloadSha256ExcludingReasoning": payload_sha_without_reasoning(payload),
              "deterministic": {key: data["policy"][key] for key in ("deterministicEnding", "closeAllowed",
                                                                     "mustVoiceTrustDoubt", "requiredFactIds",
                                                                     "allowedActions")},
              "legacyIntent": item["plan"]["intent"]}
    started = time.monotonic()
    try:
        if version == "lan-adaptive-v1":
            result = await v1_turn(v1, data["messages"], data["policy"], item["plan"], item["transcript"],
                                   responder=responder, config=config)
        else:
            result = await v2.respond_with_fallback(data["messages"], data["policy"], item["plan"], item["transcript"],
                                                    responder=responder, config=config,
                                                    guard_context=v2.language_guard_context(data["context"]))
    except Exception as exc:  # harness failure, not a gameplay fallback
        sample.update(outcome="harness_error", harnessError=type(exc).__name__)
        return _finish(sample, recorder.records, time.monotonic() - started)
    wall = time.monotonic() - started
    metadata, final = result["metadata"], result["final"]
    policy = data["policy"]
    sample.update(outcome=result["source"], fallbackReason=None if result["source"] == "model" else metadata.get("errorCode"),
                  rejectionCodes=list(metadata.get("rejectionCodes") or []) if result["source"] == "fallback" else [],
                  warningCodes=list(metadata.get("warningCodes") or []) if result["source"] == "model" else [],
                  responderAttempts=[{key: attempt.get(key) for key in ("index", "role", "kind", "forAttempt", "errorCode",
                                                                        "finishReason", "rejectionCodes", "warningCodes",
                                                                        "latencySeconds")}
                                     for attempt in metadata.get("attempts") or []],
                  output={key: final.get(key) for key in ("action", "decision", "closeReason", "factIds", "customerText")},
                  softClose=result["source"] == "model" and final["decision"] == "close"
                  and not policy["deterministicEnding"])
    return _finish(sample, recorder.records, wall)


# ---------------------------------------------------------------- summary

def summarize(samples: list[dict], meta: Mapping) -> dict:
    arms = {}
    for arm in [arm for arm in ARMS if any(sample["arm"] == arm for sample in samples)]:
        group = [sample for sample in samples if sample["arm"] == arm]
        records = [record for sample in group for record in sample.get("httpAttempts", [])]
        costs = [sample.get("costUsd") for sample in group]
        known = [cost for cost in costs if cost is not None]
        model = [sample for sample in group if sample.get("outcome") == "model"]
        role_costs = {role: [(sample.get("costUsdByRole") or {}).get(role) for sample in group]
                      for role in ("generation", "guard")}
        responder = [attempt for sample in group for attempt in sample.get("responderAttempts") or []]
        arms[arm] = {
            "version": ARMS[arm][0], "reasoning": ARMS[arm][1], "samples": len(group),
            "outcomes": dict(Counter(sample.get("outcome") for sample in group)),
            "fallbackRate": sum(1 for sample in group if sample.get("outcome") == "fallback") / len(group),
            "fallbackReasons": dict(Counter(sample.get("fallbackReason") for sample in group if sample.get("fallbackReason"))),
            "httpAttempts": len(records),
            "generationHttpAttempts": sum(1 for record in records if _role(record) == "generation"),
            "guardHttpAttempts": sum(1 for record in records if _role(record) == "guard"),
            "repairAttempts": sum(sample.get("repairAttempts", 0) for sample in group),
            "regenerateAttempts": sum(sample.get("regenerateAttempts", 0) for sample in group),
            "retryAttempts": sum(sample.get("retryAttempts", 0) for sample in group),
            "failedAttempts": sum(sample.get("failedAttempts", 0) for sample in group),
            "wallClockReadySeconds": stats([sample.get("wallClockReadySeconds") for sample in group]),
            "firstAttemptLatencySeconds": stats([sample.get("firstAttemptLatencySeconds") for sample in group]),
            "requestLatencySeconds": stats([record.get("latencySeconds") for record in records]),
            "generationLatencySeconds": stats([record.get("latencySeconds") for record in records
                                               if _role(record) == "generation"]),
            "guardLatencySeconds": stats([record.get("latencySeconds") for record in records if _role(record) == "guard"]),
            "tokens": _token_totals(records) if records else {},
            "tokensByRole": {role: _token_totals([record for record in records if _role(record) == role])
                             for role in ("generation", "guard")},
            "costUsdByRole": {role: (sum(values) if values and all(value is not None for value in values) else None)
                              for role, values in role_costs.items()},
            "guardVerdicts": dict(Counter(str(verdict) for sample in group for verdict in sample.get("guardVerdicts") or [])),
            "costUsdTotal": sum(known) if known and len(known) == len(costs) else None,
            "costUsdKnownTotal": sum(known) if known else None,
            "costUsdPerSample": sum(known) / len(known) if known else None,
            "costUsdPerModelReply": (sum(known) / len(model)) if known and len(known) == len(costs) and model else None,
            "samplesMissingCost": len(costs) - len(known),
            "attemptRejectionCodes": dict(Counter(code for attempt in responder if attempt.get("role") != "guard"
                                                  for code in attempt.get("rejectionCodes") or [])),
            "guardRejectionCodes": dict(Counter(code for attempt in responder if attempt.get("role") == "guard"
                                                for code in attempt.get("rejectionCodes") or [])),
            "acceptedWarningCodes": dict(Counter(code for sample in group for code in sample.get("warningCodes") or [])),
            "actions": dict(Counter((sample.get("output") or {}).get("action") for sample in group)),
            "decisions": dict(Counter((sample.get("output") or {}).get("decision") for sample in group)),
            "softCloses": sum(1 for sample in group if sample.get("softClose")),
            "servedProviders": dict(Counter(provider for sample in group for provider in sample.get("servedProviders", []))),
            "servedModels": dict(Counter(model for sample in group for model in sample.get("servedModels", []))),
            "finishReasons": dict(Counter(reason for sample in group for reason in sample.get("finishReasons", []))),
        }
    by_case = {}
    for case_id in sorted({sample["caseId"] for sample in samples}):
        row = {}
        for arm in ARMS:
            group = [sample for sample in samples if sample["caseId"] == case_id and sample["arm"] == arm]
            if group:
                row[arm] = {"wallMedian": quantile([sample.get("wallClockReadySeconds") for sample in group], .5),
                            "outcomes": [sample.get("outcome") for sample in group],
                            "actions": [(sample.get("output") or {}).get("action") for sample in group],
                            "decisions": [(sample.get("output") or {}).get("decision") for sample in group],
                            "closed": sum(1 for sample in group if (sample.get("output") or {}).get("decision") == "close")}
        by_case[case_id] = row
    return {"schemaVersion": "sales-adaptive-arms-summary-v2", **meta, "arms": arms, "byCase": by_case,
            "notes": ["Generation-only fixed-prefix replay; excludes STT, Jev/Luna classification and TTS.",
                      "Within one version only reasoning differs (payload hashes excluding reasoning are equal).",
                      "v1 and v2 differ in prompt, context shape, allowed actions and validator; descriptive comparison.",
                      "costUsd is provider-reported usage.cost over every HTTP attempt, including repairs, "
                      "regenerations, verifier requests and rejected or failed attempts; null means unknown (never "
                      "zero-filled). costUsdByRole/tokensByRole split generation from the v2 verifier.",
                      "v2 arms make one reasoning-off verifier request per locally valid candidate; v1 arms make none.",
                      "Passing the guard is not evidence of dialogue quality; read accepted and rejected candidates.",
                      "Ten cases are qualitative pilot coverage, not a powered quality estimate."]}


def _fmt(value, digits=3):
    if value is None:
        return "null"
    if isinstance(value, float):
        return f"{value:.{digits}f}" if abs(value) >= 0.001 or value == 0 else f"{value:.3e}"
    return str(value)


def render_markdown(summary: Mapping, samples: list[dict]) -> str:
    lines = [f"# Adaptive Lan v1/v2 benchmark ({summary.get('mode')})", ""]
    if summary.get("simulated"):
        lines += ["> SIMULATED dry-run: local mock transport. Texts, latencies, tokens and costs are NOT model results.", ""]
    lines += [f"- Run `{summary.get('runId')}`, model `{summary.get('model')}`, seed {summary.get('seed')}, "
              f"repeats {summary.get('repeats')}, source `{summary.get('caseSource')}`",
              f"- Prompt SHA-256: v1 `{summary['promptSha256']['lan-adaptive-v1']}`, v2 `{summary['promptSha256']['lan-adaptive-v2']}`",
              f"- Payload checks: {summary.get('payloadChecks')}", "",
              "## Arms", "",
              "| arm | n | model/fallback/error | repairs | regenerations | retries | verifier calls | wall median s | "
              "wall p90 s | cost total USD | verifier cost USD | cost per model reply | missing-cost samples | "
              "soft closes | warnings | providers |",
              "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"]
    for arm, data in summary["arms"].items():
        outcomes = data["outcomes"]
        lines.append(f"| {arm} | {data['samples']} | {outcomes.get('model', 0)}/{outcomes.get('fallback', 0)}/"
                     f"{outcomes.get('harness_error', 0)} | {data['repairAttempts']} | {data['regenerateAttempts']} "
                     f"| {data['retryAttempts']} | {data['guardHttpAttempts']} "
                     f"| {_fmt(data['wallClockReadySeconds']['median'])} | {_fmt(data['wallClockReadySeconds']['p90'])} "
                     f"| {_fmt(data['costUsdTotal'], 6)} | {_fmt(data['costUsdByRole']['guard'], 6)} "
                     f"| {_fmt(data['costUsdPerModelReply'], 6)} "
                     f"| {data['samplesMissingCost']} | {data['softCloses']} | {data['acceptedWarningCodes']} "
                     f"| {data['servedProviders']} |")
    lines += ["", "## Closures by case", "", "| case | " + " | ".join(ARMS) + " |", "|---|" + "---|" * len(ARMS)]
    for case_id, row in summary["byCase"].items():
        lines.append(f"| {case_id} | " + " | ".join(f"{row[arm]['closed']}/{len(row[arm]['outcomes'])}" if arm in row
                                                    else "-" for arm in ARMS) + " |")
    lines += ["", "## Samples", "", "| seq | case | arm | rep | outcome | action | decision | wall s | Lan |",
              "|---|---|---|---|---|---|---|---|---|"]
    for sample in sorted(samples, key=lambda item: item["seq"]):
        output = sample.get("output") or {}
        text = str(output.get("customerText") or "").replace("|", "/")
        lines.append(f"| {sample['seq']} | {sample['caseId']} | {sample['arm']} | {sample['repeat']} | {sample.get('outcome')} "
                     f"| {output.get('action')} | {output.get('decision')} | {_fmt(sample.get('wallClockReadySeconds'))} | {text} |")
    lines += ["", "## Notes", ""] + [f"- {note}" for note in summary["notes"]]
    lines += ["- Rejected candidates are in samples.jsonl (httpAttempts[].candidate, redacted).",
              "- Historical Lan replies are in grader_reference.json (blinded reference, never model input)."]
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- main

def _write_json(path: Path, value) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")


def parse_args(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("mode", choices=("dry-run", "live"))
    parser.add_argument("--source", choices=("cached", "rebuild"), default="cached")
    parser.add_argument("--cached-run", type=Path, default=CACHED_RUN)
    parser.add_argument("--effective-manifest", type=Path, default=EFFECTIVE_MANIFEST)
    parser.add_argument("--discovery-manifest", type=Path, default=DISCOVERY_MANIFEST,
                        help="selection manifest; used for prefix/leakage cross-checks and --source rebuild")
    parser.add_argument("--project-root", type=Path, default=ROOT)
    parser.add_argument("--out-root", type=Path, default=OUTPUT_ROOT)
    parser.add_argument("--run-dir", type=Path, help="explicit new output directory")
    parser.add_argument("--resume", type=Path, help="existing run directory to continue")
    parser.add_argument("--cases", help="comma-separated case IDs (default: all)")
    parser.add_argument("--arms", default=",".join(ARMS), help="comma-separated arms (default: all four)")
    parser.add_argument("--repeats", type=int, default=3)
    parser.add_argument("--seed", type=int, default=20261029)
    parser.add_argument("--max-tokens", type=int, default=4096)
    parser.add_argument("--reasoning-budget", type=int, default=1024, help="0 = no reasoning.max_tokens")
    parser.add_argument("--timeout", type=float, default=45.0)
    parser.add_argument("--temperature", type=float, default=0.4)
    parser.add_argument("--provider", default="alibaba")
    parser.add_argument("--no-provider-pin", action="store_true")
    parser.add_argument("--confirm-paid", action="store_true", help="required for live mode")
    args = parser.parse_args(argv)
    selected = args.arms.split(",")
    if not selected or any(arm not in ARMS for arm in selected) or len(selected) != len(set(selected)):
        parser.error("--arms must name distinct supported arms: " + ",".join(ARMS))
    return args


def run_fingerprint(meta: Mapping) -> str:
    keys = ("mode", "caseSource", "model", "seed", "repeats", "cases", "armConfigs", "promptSha256", "schemaSha256",
            "guardRequest", "sourceHashes", "caseInputs", "scheduleSha256")
    return sha256_text(json.dumps({key: meta.get(key) for key in keys}, ensure_ascii=False, sort_keys=True))


def resume_mismatches(previous: Mapping, meta: Mapping) -> list[str]:
    keys = ("mode", "caseSource", "model", "seed", "repeats", "cases", "armConfigs", "promptSha256", "schemaSha256",
            "guardRequest", "sourceHashes", "caseInputs", "scheduleSha256", "runFingerprint")
    return [key for key in keys if previous.get(key) != meta.get(key)]


async def main_async(args) -> int:
    simulated = args.mode == "dry-run"
    if not simulated and not args.confirm_paid:
        print("live mode makes paid OpenRouter calls; pass --confirm-paid", file=sys.stderr)
        return 2
    v1 = load_v1_snapshot()
    case_filter = set(args.cases.split(",")) if args.cases else None
    if args.source == "cached":
        items, checks = load_cached_cases(args.cached_run, args.effective_manifest, args.discovery_manifest, case_filter)
    else:
        items, checks = load_rebuilt_cases(args.discovery_manifest, args.project_root, v1, case_filter)
    items = prepare(items, v1, checks)
    if not case_filter and len(items) != 10:
        checks["errors"].append(f"expected 10 cases, found {len(items)}")
    if checks.get("distinctSessions") != len(items):
        checks["errors"].append("cases do not cover distinct sessions one-to-one")
    if checks["errors"]:
        print("case checks failed: " + "; ".join(checks["errors"]), file=sys.stderr)
        return 3
    settings = get_settings()
    if simulated:
        settings = replace(settings, openrouter_api_key="dry-run-no-key")
    elif not settings.openrouter_api_key:
        print("OPENROUTER_API_KEY is not configured", file=sys.stderr)
        return 2
    configs = arm_configs(args)
    payload = payload_checks(items, configs, settings, v1)
    if not all(payload.values()):
        print(f"payload checks failed: {payload}", file=sys.stderr)
        return 3
    by_id = {item["caseId"]: item for item in items}
    slots = schedule(sorted(by_id), args.repeats, args.seed, arms=list(configs))
    meta = {"mode": args.mode, "simulated": simulated, "model": QWEN_MODEL, "seed": args.seed, "repeats": args.repeats,
            "caseSource": args.source, "cases": sorted(by_id),
            "armConfigs": {arm: {**config.redacted(), "providerPreferences":
                                 _module(arm, v1).AdaptiveLanResponder(settings, config=config).provider_preferences()}
                           for arm, config in configs.items()},
            "promptSha256": {"lan-adaptive-v1": v1.PROMPT_SHA256, "lan-adaptive-v2": v2.PROMPT_SHA256},
            "schemaSha256": {"lan-adaptive-v1": v1.SCHEMA_SHA256, "lan-adaptive-v2": v2.SCHEMA_SHA256},
            # The v2 verifier: fixed settings and prompt digest (v1 has none).
            "guardRequest": {"lan-adaptive-v2": dict(v2.GUARD_REQUEST)},
            "v1Snapshot": {"path": V1_SNAPSHOT.relative_to(ROOT).as_posix(), "sha256Lf": V1_SNAPSHOT_SHA256_LF,
                           "promptSha256MatchesLiveV1Run": v1.PROMPT_SHA256 == V1_PROMPT_SHA256},
            "sourceHashes": source_hashes(),
            "caseInputs": {case_id: {version: by_id[case_id]["versions"][version]["contextSha256"]
                                     for version in ("lan-adaptive-v1", "lan-adaptive-v2")} for case_id in sorted(by_id)},
            "scheduleSha256": sha256_text(json.dumps(slots, sort_keys=True)), "payloadChecks": payload,
            "plannedSamples": len(slots),
            # v2: up to MAX_ATTEMPTS generation requests, each followed by at most one verifier request.
            "maxRequests": sum(v2.MAX_ATTEMPTS * (2 if slot["version"] == v2.ADAPTIVE_VERSION else 1) for slot in slots),
            "rateCardReference": RATE_CARD_REFERENCE}
    # Validator policy and fallback plans affect verdicts even when model inputs
    # are identical. Include them and the source checks in resume identity.
    for case_id, item in by_id.items():
        meta["caseInputs"][case_id]["evaluationSha256"] = sha256_text(json.dumps(
            {"plan": item["plan"], "policies": {version: data["policy"] for version, data in item["versions"].items()},
             "effective": item["effective"], "discovery": item["discovery"], "provenance": item["provenance"]},
            ensure_ascii=False, sort_keys=True))
    meta["runFingerprint"] = run_fingerprint(meta)
    if args.resume:
        run_dir = args.resume
        previous_path = run_dir / "config.json"
        if not previous_path.exists():
            print(f"--resume: {previous_path} not found", file=sys.stderr)
            return 4
        previous = json.loads(previous_path.read_text(encoding="utf-8"))
        mismatched = resume_mismatches(previous, meta)
        if mismatched:
            print("--resume refused; changed: " + ", ".join(mismatched), file=sys.stderr)
            return 4
    else:
        stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
        run_dir = args.run_dir or args.out_root / "runs" / f"arms-{args.mode}-{stamp}"
        if run_dir.exists() and any(run_dir.iterdir()):
            print(f"{run_dir} is not empty; use --resume or a new --run-dir", file=sys.stderr)
            return 4
    meta["runId"] = run_dir.name
    run_dir.mkdir(parents=True, exist_ok=True)
    if not args.resume:
        _write_json(run_dir / "config.json", {**meta, "checks": checks, "schedule": slots, "pathRebase": PATH_REBASE})
        for case_id, item in by_id.items():
            _write_json(run_dir / "cases" / f"{case_id}.json", {
                "caseId": case_id, "provenance": item["provenance"],
                "plan": {key: item["plan"].get(key) for key in ("intent", "knownFacts", "allowedNewFacts", "requiredFactIds",
                                                                 "requiredContent", "fallbackText")},
                **{version: {"policy": data["policy"], "contextSha256": data["contextSha256"], "modelContext": data["context"]}
                   for version, data in item["versions"].items()}})
        _write_json(run_dir / "grader_reference.json", {
            case_id: {"historicalCustomerText": (item["effective"].get("historicalReference") or {}).get("customerText"),
                      "caseRationale": item["effective"].get("caseRationale")} for case_id, item in by_id.items()})
    samples_path = run_dir / "samples.jsonl"
    done = {}
    if samples_path.exists():
        for line in samples_path.read_text(encoding="utf-8").splitlines():
            if line.strip():
                sample = json.loads(line)
                if sample.get("runFingerprint") != meta["runFingerprint"]:
                    print("--resume refused; a stored sample has another run fingerprint", file=sys.stderr)
                    return 4
                # An error may follow a paid request. Retain its accounting and
                # never silently charge that saved slot again during resume.
                done[(sample["caseId"], sample["arm"], sample["repeat"])] = sample
    transport = simulated_transport() if simulated else None
    async with httpx.AsyncClient(transport=transport, follow_redirects=False) as http_client:
        with samples_path.open("a", encoding="utf-8") as output:
            for slot in slots:
                key = (slot["caseId"], slot["arm"], slot["repeat"])
                if key in done:
                    continue
                sample = await run_sample(by_id[slot["caseId"]], slot, configs[slot["arm"]], settings, http_client, v1,
                                          meta["runFingerprint"], simulated)
                done[key] = sample
                output.write(json.dumps(sample, ensure_ascii=False) + "\n")
                output.flush()
                print(f"[{slot['seq']}/{len(slots)}] {slot['caseId']} {slot['arm']} r{slot['repeat']} "
                      f"{sample.get('outcome')} {sample.get('wallClockReadySeconds')}s", flush=True)
    samples = sorted(done.values(), key=lambda sample: sample["seq"])
    summary = summarize(samples, meta)
    _write_json(run_dir / "summary.json", summary)
    (run_dir / "summary.md").write_text(render_markdown(summary, samples), encoding="utf-8")
    print(f"wrote {run_dir}")
    return 0


def main(argv=None) -> int:
    return asyncio.run(main_async(parse_args(argv)))


if __name__ == "__main__":
    raise SystemExit(main())
