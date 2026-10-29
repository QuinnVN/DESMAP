"""Four-arm v1/v2 benchmark harness (offline: simulated transport, cached artifacts)."""
import asyncio
import copy
import json
import tempfile
import unittest
from collections import Counter
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

import httpx

from app import sales_adaptive as app_v1
from app import sales_adaptive_v2 as v2
from app.config import get_settings
from scripts import benchmark_sales_adaptive_arms as arms
from scripts.benchmark_sales_adaptive_arms import (ARMS, CACHED_RUN, DISCOVERY_MANIFEST, EFFECTIVE_MANIFEST, V1_SNAPSHOT,
                                                   cached_checks, leakage_findings, load_cached_cases, load_v1_snapshot,
                                                   plan_from_cache, prepare, rebase, schedule, summarize)

CASES = [f"SA-{index:02d}" for index in range(1, 11)]
HAVE_ARTIFACTS = CACHED_RUN.exists() and EFFECTIVE_MANIFEST.exists() and DISCOVERY_MANIFEST.exists()


class ScheduleTests(unittest.TestCase):
    def test_off_only_selection_has_no_thinking_requests(self):
        args = arms.parse_args(["dry-run", "--arms", "v1_off,v2_off", "--repeats", "1"])
        configs = arms.arm_configs(args)
        self.assertEqual(set(configs), {"v1_off", "v2_off"})
        self.assertTrue(all(config.reasoning == "off" for config in configs.values()))
        slots = schedule(CASES[:4], 1, args.seed, arms=list(configs))
        self.assertEqual(len(slots), 8)
        self.assertEqual(Counter(slot["arm"] for slot in slots), {"v1_off": 4, "v2_off": 4})
        self.assertTrue(all(slot["reasoning"] == "off" for slot in slots))

    def test_four_arms_ten_cases_three_repeats(self):
        slots = schedule(CASES, 3, 20261029)
        self.assertEqual(len(slots), 120)
        self.assertEqual(slots, schedule(CASES, 3, 20261029))
        self.assertNotEqual(slots, schedule(CASES, 3, 7))
        self.assertEqual(Counter(slot["arm"] for slot in slots), {arm: 30 for arm in ARMS})
        self.assertEqual([slot["seq"] for slot in slots], list(range(1, 121)))
        for start in range(0, 120, 4):
            block = slots[start:start + 4]
            self.assertEqual(len({slot["caseId"] for slot in block}), 1)  # interleaved per case
            self.assertEqual({slot["arm"] for slot in block}, set(ARMS))
        for case in CASES:
            positions = {}
            for slot in slots:
                if slot["caseId"] == case:
                    positions.setdefault(slot["arm"], []).append(slot["positionInBlock"])
            for arm, values in positions.items():
                self.assertEqual(len(set(values)), 3, (case, arm))  # rotated every repeat

    def test_arm_configs_share_everything_but_version_and_reasoning(self):
        configs = arms.arm_configs(arms.parse_args(["dry-run"]))
        shared = [{key: value for key, value in config.redacted().items() if key not in ("version", "reasoning")}
                  for config in configs.values()]
        self.assertTrue(all(item == shared[0] for item in shared))
        self.assertEqual({arm: (config.version, config.reasoning) for arm, config in configs.items()}, ARMS)
        self.assertEqual(shared[0]["provider"], {"order": ["alibaba"], "allow_fallbacks": False, "require_parameters": True})


class SnapshotTests(unittest.TestCase):
    def test_snapshot_is_pinned_and_matches_the_v1_module(self):
        module = load_v1_snapshot()
        self.assertEqual((module.PROMPT_SHA256, module.SCHEMA_SHA256), (arms.V1_PROMPT_SHA256, arms.V1_SCHEMA_SHA256))
        self.assertEqual((app_v1.PROMPT_SHA256, app_v1.ADAPTIVE_VERSION), (module.PROMPT_SHA256, module.ADAPTIVE_VERSION))
        self.assertEqual(arms.sha256_lf(V1_SNAPSHOT), arms.sha256_lf(Path(app_v1.__file__)))

    def test_tampered_snapshot_is_refused(self):
        with tempfile.TemporaryDirectory() as folder:
            copy_path = Path(folder) / "sales_adaptive_v1.py"
            copy_path.write_text(V1_SNAPSHOT.read_text(encoding="utf-8") + "\n# edited\n", encoding="utf-8")
            with self.assertRaises(RuntimeError):
                load_v1_snapshot(copy_path)

    def test_old_paths_rebase_read_only(self):
        self.assertEqual(rebase(r"D:\NCKH\NCKH-AI\recordings\x.json"), arms.ROOT / "recordings" / "x.json")
        self.assertEqual(rebase(r"D:\NCKH\output\sales-adaptive-benchmark"), arms.OUTPUT_ROOT)
        self.assertEqual(rebase(r"C:\other\file"), Path(r"C:\other\file"))


def synthetic_case():
    context = {"storePolicy": {}, "purchasedShoe": None,
               "facts": {"sharedFacts": {"complaint": "Chị mang đôi này bị đau chân nên muốn đổi."}, "mayDiscloseNow": {},
                         "mustAnswerFactIds": [], "unknownToLan": []},
               "conversation": [{"lan": "Em ơi, chị bị đau chân."},
                                {"salesperson": "PRIOR UTTERANCE ONE", "lan": "Chị vẫn đau lắm, em hiểu không?"}],
               "currentSalespersonUtterance": "CURRENT UTTERANCE",
               "observations": {"recognizedActs": ["acknowledgment"], "unclearActs": [], "openConcern": "", "missingPart": "",
                                "hintLevel": 0, "unresolvedPromises": [], "salespersonRequestedPhysicalAction": False,
                                "hostile": False},
               "limits": {}, "allowedActions": ["raise_concern"], "actionMemory": {"recent": []}, "recentLanReplies": []}
    case = {"modelContext": context, "contextSha256": arms.sha256_text(json.dumps(context, ensure_ascii=False, sort_keys=True)),
            "policy": {}, "legacyPlan": {}}
    effective = {"caseId": "X", "sessionId": "session-secret-id", "savedTurnIndex1Based": 2, "turnId": "t2",
                 "historicalReference": {"customerText": "SELECTED HISTORICAL LAN REPLY TEXT",
                                         "turnAssessment": {"acknowledgment": {"status": "true"},
                                                            "openQuestion": {"status": "false"}}}}
    discovery = {"caseId": "X", "turnId": "t3", "sessionId": "session-secret-id",
                 "replayInput": {"priorDialogue": [
                     {"salespersonTranscript": "PRIOR UTTERANCE ONE", "customerText": "Chị vẫn đau lắm, em hiểu không?"},
                     {"salespersonTranscript": "CURRENT UTTERANCE", "customerText": "SELECTED HISTORICAL LAN REPLY TEXT"}],
                     "salespersonTranscript": "LATER TURN THREE UTTERANCE TEXT"},
                 "historicalReference": {"customerText": "LATER TURN THREE LAN REPLY TEXT"}}
    return case, effective, discovery


class CachedCheckTests(unittest.TestCase):
    def test_clean_synthetic_case_passes(self):
        case, effective, discovery = synthetic_case()
        self.assertEqual(cached_checks("X", case, effective, discovery, {case["contextSha256"]}), [])
        messages = [{"role": "user", "content": json.dumps(case["modelContext"], ensure_ascii=False)}]
        self.assertEqual(leakage_findings(messages, case, effective, discovery), [])

    def test_missing_provenance_cannot_pass_cached_validation(self):
        case, effective, discovery = synthetic_case()
        self.assertIn("cached case has no live sample context hash",
                      cached_checks("X", case, effective, discovery, set()))
        self.assertIn("cached case has no discovery reference for prefix and leakage checks",
                      cached_checks("X", case, effective, None, {case["contextSha256"]}))
        effective["historicalReference"].pop("turnAssessment")
        self.assertIn("cached case has no selected-turn assessment reference",
                      cached_checks("X", case, effective, discovery, {case["contextSha256"]}))

    def test_hash_prefix_and_label_mismatches_are_errors(self):
        case, effective, discovery = synthetic_case()
        self.assertTrue(cached_checks("X", case, effective, discovery, {"other-live-hash"}))
        tampered = copy.deepcopy(case)
        tampered["modelContext"]["currentSalespersonUtterance"] = "EDITED"
        errors = cached_checks("X", tampered, effective, discovery, set())
        self.assertIn("cached context does not hash to its recorded contextSha256", errors)
        self.assertIn("current utterance differs from the discovery transcript", errors)
        future_label = copy.deepcopy(effective)
        future_label["historicalReference"]["turnAssessment"]["openQuestion"]["status"] = "true"
        self.assertIn("recognized acts differ from the selected turn's own labels",
                      cached_checks("X", case, future_label, discovery, set()))
        shorter = copy.deepcopy(discovery)
        shorter["replayInput"]["priorDialogue"][0]["customerText"] = "DIFFERENT"
        self.assertIn("dialogue prefix differs from discovery priorDialogue", cached_checks("X", case, effective, shorter, set()))

    def test_leakage_of_future_text_ids_and_scores_is_found(self):
        case, effective, discovery = synthetic_case()
        for injected, code in (("SELECTED HISTORICAL LAN REPLY TEXT", "historical_selected_reply"),
                               ("LATER TURN THREE UTTERANCE TEXT", "future_turn_transcript"),
                               ("LATER TURN THREE LAN REPLY TEXT", "future_turn_customerText"),
                               ("session-secret-id", "private_id_sessionId"), ('"score": 1', "scoring_field_score")):
            messages = [{"role": "user", "content": json.dumps(case["modelContext"], ensure_ascii=False) + injected}]
            self.assertIn(code, leakage_findings(messages, case, effective, discovery), injected)


class SummaryTests(unittest.TestCase):
    def test_costs_include_every_attempt_and_unknown_stays_null(self):
        def sample(arm, costs, outcome="model"):
            records = [{"latencySeconds": 1.0, "usage": {"promptTokens": 100, "completionTokens": 10, "reasoningTokens": 0,
                                                          "cachedTokens": 0, "totalTokens": 110, "costUsd": cost}}
                       for cost in costs]
            missing = sum(cost is None for cost in costs)
            return {"seq": 1, "caseId": "SA-01", "arm": arm, "outcome": outcome, "httpAttempts": records,
                    "wallClockReadySeconds": 1.0, "firstAttemptLatencySeconds": 1.0,
                    "costUsd": None if missing else sum(costs), "repairAttempts": len(costs) - 1, "retryAttempts": 0,
                    "responderAttempts": [{"kind": "initial", "rejectionCodes": ["unallowed_fact"] if len(costs) > 1 else []},
                                          *([{"kind": "repair", "rejectionCodes": []}] if len(costs) > 1 else [])],
                    "warningCodes": ["too_many_sentences"], "output": {"action": "raise_concern", "decision": "continue"}}

        summary = summarize([sample("v2_off", [0.0001, 0.0002]), sample("v2_off", [0.0003]),
                             sample("v2_on", [0.0004, None], "fallback")], {"mode": "test"})
        off, on = summary["arms"]["v2_off"], summary["arms"]["v2_on"]
        self.assertAlmostEqual(off["costUsdTotal"], 0.0006)
        self.assertAlmostEqual(off["costUsdPerModelReply"], 0.0003)
        self.assertEqual(off["repairAttempts"], 1)
        self.assertEqual(off["httpAttempts"], 3)
        self.assertEqual(off["attemptRejectionCodes"], {"unallowed_fact": 1})
        self.assertEqual(off["acceptedWarningCodes"], {"too_many_sentences": 2})
        self.assertIsNone(on["costUsdTotal"])
        self.assertEqual(on["samplesMissingCost"], 1)


class V1GlueTests(unittest.IsolatedAsyncioTestCase):
    async def test_harness_v1_turn_matches_v1_run_adaptive_turn(self):
        """The cached path reuses v1's response step: same verdict as v1.run_adaptive_turn."""
        from app.tests.test_sales_adaptive_v2 import say

        snapshot = load_v1_snapshot()
        policy = {"allowedActions": ["raise_concern", "request_next_step"], "deterministicEnding": None, "endingTone": None,
                  "closeAllowed": False, "mustVoiceTrustDoubt": False, "requiredFactIds": [], "citableFactIds": ["complaint"]}
        plan = {"intent": "raise_concern", "knownFacts": ["complaint"], "allowedNewFacts": [], "requiredFactIds": [],
                "factTexts": {}, "avoidReplyTexts": [], "requiredContent": [], "fallbackText": "Chân chị vẫn đau."}
        for text, expected in (("Chân chị vẫn đau, em tính sao?", "model"), ("Em hoàn tiền cho chị đi.", "fallback")):
            outputs = []
            for module in (snapshot, app_v1):
                responses = iter([httpx.Response(200, json={"choices": [{"finish_reason": "stop", "message": {
                    "content": json.dumps(say("raise_concern", text), ensure_ascii=False)}}], "usage": {"cost": 0.0001}})] * 2)
                client = httpx.AsyncClient(transport=httpx.MockTransport(lambda request_: next(responses)))
                responder = module.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key="k"), client=client,
                                                        config=module.AdaptiveRequestConfig(timeout_seconds=5))
                outputs.append(await arms.v1_turn(module, [{"role": "user", "content": "{}"}], policy, plan, "x",
                                                  responder=responder, config=responder.config))
            self.assertEqual(outputs[0]["source"], expected)
            self.assertEqual(outputs[0]["final"], outputs[1]["final"])
            validate = lambda output: app_v1.output_rejections(output, policy, plan, "x")
            self.assertEqual(bool(validate(say("raise_concern", text))), expected == "fallback")


class VerifierAccountingTests(unittest.IsolatedAsyncioTestCase):
    """v2 verifier requests are recorded as their own role, never read as generation output."""

    async def test_simulated_regeneration_is_recorded_by_role(self):
        from app.tests.test_sales_adaptive_v2_language import LIMITS, PLAN

        utterance = next(f"Câu nói thử {index}" for index in range(100)
                         if int(arms.sha256_text(f"Câu nói thử {index}")[:8], 16) % 3 == 1)
        context = {"facts": {"sharedFacts": {}, "mayDiscloseNow": {}, "mustAnswerFactIds": []},
                   "allowedActions": ["raise_concern", "request_next_step"], "limits": {"deterministicEnding": None},
                   "currentSalespersonUtterance": utterance, "conversation": [{"lan": "Em ơi, chị bị đau chân."}]}
        messages = v2.build_messages(context)
        async with httpx.AsyncClient(transport=arms.simulated_transport()) as client:
            recorder = arms.CandidateRecordingClient(client, "dry-run-no-key")
            responder = v2.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key="dry-run-no-key"),
                                                client=recorder, config=v2.request_config(timeout_seconds=5))
            result = await v2.respond_with_fallback(messages, v2.policy_from_limits(LIMITS), PLAN, utterance,
                                                    responder=responder, config=responder.config,
                                                    guard_context=v2.language_guard_context(context))
        records = recorder.records
        self.assertEqual(result["source"], "model")
        self.assertNotIn(arms.SIMULATED_NONSENSE, result["final"]["customerText"])
        self.assertEqual([record["role"] for record in records], ["generation", "guard", "generation", "guard"])
        self.assertEqual([record.get("verdict") for record in records if record["role"] == "guard"], ["reject", "pass"])
        self.assertTrue(all("candidate" in record for record in records if record["role"] == "generation"))
        self.assertTrue(all("candidate" not in record for record in records if record["role"] == "guard"))
        self.assertEqual([attempt["kind"] for attempt in result["metadata"]["attempts"]],
                         ["initial", "guard", "regenerate", "guard"])

    def test_finish_and_summary_split_cost_tokens_and_latency_by_role(self):
        def record(role, cost, latency, verdict=None):
            item = {"role": role, "latencySeconds": latency, "errorCode": None,
                    "usage": {"promptTokens": 100 if role == "generation" else 20, "completionTokens": 10,
                              "reasoningTokens": 0, "cachedTokens": 0, "totalTokens": 110, "costUsd": cost}}
            return dict(item, verdict=verdict) if role == "guard" else item

        records = [record("generation", 0.0002, 2.0), record("guard", 0.00001, 0.5, "reject"),
                   record("generation", 0.0002, 2.5), record("guard", 0.00001, 0.4, "pass")]
        responder = [{"kind": "initial", "role": "generation"}, {"kind": "guard", "role": "guard",
                                                                 "rejectionCodes": ["incoherent_text"]},
                     {"kind": "regenerate", "role": "generation"}, {"kind": "guard", "role": "guard"}]
        sample = arms._finish({"seq": 1, "caseId": "SA-01", "arm": "v2_off", "outcome": "model",
                               "responderAttempts": responder, "output": {}}, records, 6.0)
        self.assertEqual((sample["attemptCount"], sample["generationAttempts"], sample["guardAttempts"]), (4, 2, 2))
        self.assertEqual((sample["regenerateAttempts"], sample["repairAttempts"]), (1, 0))
        self.assertEqual(sample["firstAttemptLatencySeconds"], 2.0)
        self.assertEqual(sample["guardLatencySeconds"], [0.5, 0.4])
        self.assertAlmostEqual(sample["costUsd"], 0.00042)
        self.assertAlmostEqual(sample["costUsdByRole"]["guard"], 0.00002)
        self.assertEqual(sample["tokensByRole"]["guard"]["promptTokens"], 40)
        v1_sample = arms._finish({"seq": 2, "caseId": "SA-01", "arm": "v1_off", "outcome": "model",
                                  "responderAttempts": [{"kind": "initial"}], "output": {}},
                                 [{k: v for k, v in record("generation", 0.0001, 1.0).items() if k != "role"}], 1.0)
        self.assertEqual((v1_sample["guardAttempts"], v1_sample["costUsdByRole"]["guard"]), (0, 0.0))
        summary = summarize([sample, v1_sample], {"mode": "test"})
        data = summary["arms"]["v2_off"]
        self.assertEqual((data["httpAttempts"], data["generationHttpAttempts"], data["guardHttpAttempts"]), (4, 2, 2))
        self.assertAlmostEqual(data["costUsdByRole"]["guard"], 0.00002)
        self.assertEqual(data["guardVerdicts"], {"reject": 1, "pass": 1})
        self.assertEqual(data["guardRejectionCodes"], {"incoherent_text": 1})
        self.assertEqual(data["attemptRejectionCodes"], {})
        self.assertEqual(data["generationLatencySeconds"]["n"], 2)
        self.assertEqual(summary["arms"]["v1_off"]["guardHttpAttempts"], 0)


@unittest.skipUnless(HAVE_ARTIFACTS, "cached v1 run artifacts not present")
class CachedArtifactTests(unittest.TestCase):
    def test_cached_cases_reproduce_the_live_v1_inputs_without_leakage(self):
        snapshot = load_v1_snapshot()
        items, checks = load_cached_cases(CACHED_RUN, EFFECTIVE_MANIFEST, DISCOVERY_MANIFEST)
        items = prepare(items, snapshot, checks)
        self.assertEqual(checks["errors"], [])
        self.assertEqual((checks["caseCount"], checks["distinctSessions"]), (10, 10))
        for item in items:
            v1_data, v2_data = item["versions"]["lan-adaptive-v1"], item["versions"]["lan-adaptive-v2"]
            self.assertEqual(v1_data["contextSha256"], item["cachedCase"]["contextSha256"])
            self.assertEqual(v1_data["messages"][0]["content"], snapshot.ADAPTIVE_SYSTEM_PROMPT)
            self.assertEqual(v2_data["messages"][0]["content"], v2.ADAPTIVE_SYSTEM_PROMPT)
            for key in ("deterministicEnding", "closeAllowed", "mustVoiceTrustDoubt", "requiredFactIds", "citableFactIds"):
                self.assertEqual(v1_data["policy"][key], v2_data["policy"][key], key)
            self.assertTrue(set(v1_data["policy"]["allowedActions"]) <= set(v2_data["policy"]["allowedActions"])
                            or v1_data["policy"]["deterministicEnding"])
            self.assertEqual(checks["leakage"][item["caseId"]], {"lan-adaptive-v1": [], "lan-adaptive-v2": []})
        sa01 = next(item for item in items if item["caseId"] == "SA-01")
        self.assertEqual(sa01["effective"]["savedTurnIndex1Based"], 6)
        self.assertTrue(sa01["v1Policy"]["closeAllowed"])

    def test_plan_from_cache_reads_recorded_fields(self):
        case = json.loads((CACHED_RUN / "cases" / "SA-08.json").read_text(encoding="utf-8"))
        plan = plan_from_cache(case)
        self.assertEqual(plan["requiredFactIds"], case["legacyPlan"]["requiredFactIds"])
        self.assertEqual(plan["fallbackText"], case["legacyPlan"]["fallbackText"])
        self.assertEqual(set(plan["knownFacts"]), set(case["modelContext"]["facts"]["sharedFacts"]))


@unittest.skipUnless(HAVE_ARTIFACTS, "cached v1 run artifacts not present")
class DryRunResumeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.out = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def run_cli(self, *extra):
        return asyncio.run(arms.main_async(arms.parse_args(["dry-run", "--out-root", str(self.out), "--cases", "SA-01,SA-08",
                                                            "--repeats", "1", *extra])))

    def test_dry_run_resume_and_refusals(self):
        run_dir = self.out / "run"
        self.assertEqual(self.run_cli("--run-dir", str(run_dir)), 0)
        samples = [json.loads(line) for line in (run_dir / "samples.jsonl").read_text(encoding="utf-8").splitlines()]
        self.assertEqual(len(samples), 8)
        self.assertEqual(Counter(sample["arm"] for sample in samples), {arm: 2 for arm in ARMS})
        self.assertTrue(all(sample["simulated"] for sample in samples))
        for version in ("lan-adaptive-v1", "lan-adaptive-v2"):
            group = [sample for sample in samples if sample["version"] == version]
            self.assertEqual(len({(sample["caseId"], sample["payloadSha256ExcludingReasoning"]) for sample in group}), 2)
        config = json.loads((run_dir / "config.json").read_text(encoding="utf-8"))
        self.assertEqual(config["checks"]["errors"], [])
        self.assertTrue(config["v1Snapshot"]["promptSha256MatchesLiveV1Run"])
        self.assertEqual(config["guardRequest"], {"lan-adaptive-v2": v2.GUARD_REQUEST})
        self.assertEqual(config["maxRequests"], 4 * 2 + 4 * 4)  # v1: 2 per sample; v2: 2 generations + 2 verifier
        for sample in samples:
            records = sample["httpAttempts"]
            self.assertEqual(len(records), sample["generationAttempts"] + sample["guardAttempts"])
            if sample["version"] == "lan-adaptive-v1":
                self.assertEqual(sample["guardAttempts"], 0)
            else:
                self.assertGreaterEqual(sample["guardAttempts"], 1)
                self.assertTrue(all(record["reasoning"] == {"enabled": False} for record in records
                                    if record["role"] == "guard"))
                self.assertTrue(all("candidate" not in record for record in records if record["role"] == "guard"))
        # Resume with identical inputs adds nothing.
        self.assertEqual(self.run_cli("--resume", str(run_dir)), 0)
        self.assertEqual(len((run_dir / "samples.jsonl").read_text(encoding="utf-8").splitlines()), 8)
        # Any config, seed or source change is refused.
        self.assertEqual(self.run_cli("--resume", str(run_dir), "--seed", "1"), 4)
        self.assertEqual(self.run_cli("--resume", str(run_dir), "--max-tokens", "2048"), 4)
        original = (run_dir / "config.json").read_text(encoding="utf-8")
        for change in (lambda stored: stored["sourceHashes"].update({"app/sales_adaptive_guard.py": "0" * 64}),
                       lambda stored: stored["guardRequest"]["lan-adaptive-v2"].update(maxTokens=1),
                       lambda stored: stored.pop("guardRequest")):
            stored = json.loads(original)
            change(stored)
            (run_dir / "config.json").write_text(json.dumps(stored), encoding="utf-8")
            self.assertEqual(self.run_cli("--resume", str(run_dir)), 4)
        # A non-empty new run directory is never overwritten.
        self.assertEqual(self.run_cli("--run-dir", str(run_dir)), 4)

    def test_partial_run_resumes_only_missing_samples(self):
        run_dir = self.out / "partial"
        self.assertEqual(self.run_cli("--run-dir", str(run_dir)), 0)
        lines = (run_dir / "samples.jsonl").read_text(encoding="utf-8").splitlines()
        (run_dir / "samples.jsonl").write_text("\n".join(lines[:3]) + "\n", encoding="utf-8")
        self.assertEqual(self.run_cli("--resume", str(run_dir)), 0)
        resumed = [json.loads(line) for line in (run_dir / "samples.jsonl").read_text(encoding="utf-8").splitlines()]
        self.assertEqual(sorted(sample["seq"] for sample in resumed), list(range(1, 9)))

    def test_resume_refuses_changed_fallback_plan_with_identical_model_input(self):
        run_dir = self.out / "plan-change"
        self.assertEqual(self.run_cli("--run-dir", str(run_dir)), 0)
        items, checks = load_cached_cases(CACHED_RUN, EFFECTIVE_MANIFEST, DISCOVERY_MANIFEST, {"SA-01", "SA-08"})
        items[0]["plan"]["fallbackText"] += " Changed fallback."
        with patch.object(arms, "load_cached_cases", return_value=(items, checks)):
            self.assertEqual(self.run_cli("--resume", str(run_dir)), 4)
        self.assertEqual(len((run_dir / "samples.jsonl").read_text(encoding="utf-8").splitlines()), 8)

    def test_resume_keeps_saved_error_cost_without_reissuing_paid_slot(self):
        run_dir = self.out / "saved-error"
        self.assertEqual(self.run_cli("--run-dir", str(run_dir)), 0)
        path = run_dir / "samples.jsonl"
        samples = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]
        samples[0].update(outcome="harness_error", costUsd=0.0001)
        path.write_text("\n".join(json.dumps(sample) for sample in samples) + "\n", encoding="utf-8")
        with patch.object(arms, "run_sample", side_effect=AssertionError("saved slots must not call the provider")):
            self.assertEqual(self.run_cli("--resume", str(run_dir)), 0)
        summary = json.loads((run_dir / "summary.json").read_text(encoding="utf-8"))
        self.assertEqual(summary["arms"][samples[0]["arm"]]["outcomes"]["harness_error"], 1)
        self.assertEqual(summary["arms"][samples[0]["arm"]]["costUsdKnownTotal"], 0.0001)

    def test_live_requires_confirmation(self):
        self.assertEqual(asyncio.run(arms.main_async(arms.parse_args(["live", "--out-root", str(self.out)]))), 2)
        self.assertFalse((self.out / "runs").exists())
