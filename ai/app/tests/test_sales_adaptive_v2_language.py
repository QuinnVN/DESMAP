"""Adaptive Lan v2 language/coherence verifier (offline, mocked provider).

The verifier's judgement is played by httpx.MockTransport, so these tests prove
the plumbing: which requests are made, what they contain, how verdicts are
parsed, bounded, charged and kept private. They do not show how well the live
model detects mixed-language or meaningless Vietnamese.
"""
import asyncio
import json
import unicodedata
import unittest
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

import httpx

from app import sales_adaptive as v1
from app import sales_adaptive_v2 as v2
from app import sales_adaptive_versions as versions
from app.config import get_settings
from app.sales_returning_customer import public_session
from app.tests.test_sales_adaptive_v2 import (GUARD_COST, KEY, PASS, V1, V2, GuardTests, Harness, ScriptedModel, body,
                                              say, step, verdict)

UTTERANCE = "DẠ CHỊ ĐỢI CHÚT ĐỂ EM TĂNG PHA GIÀY CHO CHỊ NHÉ"
NONSENSE = "Em tăng pha giúp chị xem có ổn không?"
CLARIFY = "Tăng pha là cái gì? Em nói rõ ra xem nào, chị không có thời gian đoán đâu."
KAREN = ("Xin lỗi suông thì giải quyết được gì hả em? Chân chị đau mà em cứ vòng vo mãi, nói thẳng xem em tính "
         "làm sao đi!")
LOANWORDS = "Em check lại form giày giúp chị đi, mang vào là đau."
CHINESE = "Chị không cần tiền补偿, chị chỉ muốn đổi đôi khác."
CLEAN = "Chị không cần tiền, chị chỉ muốn đổi đôi khác cho vừa chân."
LIMITS = dict(GuardTests.LIMITS, requiredFactIds=[])
PLAN = dict(GuardTests.PLAN, requiredFactIds=[], intent="raise_concern", fallbackText="Chân chị vẫn đau, em tính sao?")
GUARD_INPUT_KEYS = {"salespersonCurrentWords", "lanPreviousLine", "lanLine"}


def lines(*texts):
    """Generation strategy: the n-th request gets the n-th text (the last one repeats)."""
    return lambda context, call, repair: say("raise_concern", texts[min(call, len(texts)) - 1])


def verdicts(*results):
    return lambda data, call: results[min(call, len(results)) - 1]


async def run(model, *, config=None, utterance=UTTERANCE):
    responder = model.responder(config=config)
    messages = [{"role": "system", "content": v2.ADAPTIVE_SYSTEM_PROMPT},
                {"role": "user", "content": json.dumps({"currentSalespersonUtterance": utterance}, ensure_ascii=False)}]
    return await v2.respond_with_fallback(messages, v2.policy_from_limits(LIMITS), PLAN, utterance, responder=responder,
                                          config=responder.config,
                                          guard_context={"salespersonCurrentWords": utterance,
                                                         "lanPreviousLine": "Chân chị đau lắm em ạ."})


def roles(result):
    return [attempt["role"] for attempt in result["metadata"]["attempts"]]


def kinds(result):
    return [attempt["kind"] for attempt in result["metadata"]["attempts"]]


class FrozenRevisionTests(Harness):
    async def test_new_session_freezes_prompt_and_verifier_revision(self):
        state = await self.store.get(await self.session("revision-new", V2))
        frozen = state["adaptiveDialogue"]
        self.assertEqual(frozen["dialogueRevision"], v2.DIALOGUE_REVISION)
        self.assertEqual(frozen["promptSha256"], v2.PROMPT_SHA256)
        self.assertEqual(frozen["languageGuard"], v2.GUARD_REQUEST)
        for field, value in (("dialogueRevision", "future"), ("promptSha256", "changed"),
                             ("languageGuard", {}), ("dialogueRevision", None)):
            with self.subTest(field=field, value=value):
                altered = dict(state, adaptiveDialogue=dict(frozen, **{field: value}))
                self.assertIsNone(versions.module_for(altered))

    async def test_existing_v2_keeps_original_prompt_and_generation_only(self):
        sid = await self.session("revision-old", V2)
        state = await self.store.get(sid)
        for field in ("dialogueRevision", "promptSha256", "languageGuard"):
            state["adaptiveDialogue"].pop(field)
        frozen = dict(state["adaptiveDialogue"])
        await self.store.save(state)
        self.assertIs(versions.module_for(state), v2)
        model = ScriptedModel(step)
        responder = model.responder()
        responder.language_guard_enabled = False
        # Exercise the production constructor selection, rather than supplying
        # an already chosen responder to the turn.
        with patch.object(v2, "AdaptiveLanResponder", return_value=responder) as constructor:
            result = await self.turn(sid, "acknowledgment")
        self.assertFalse(constructor.call_args.kwargs["language_guard_enabled"])
        self.assertEqual(result["adaptiveAction"]["source"], "model")
        self.assertEqual(model.order, ["generation"])
        self.assertEqual(model.payloads[0]["messages"][0]["content"], v2.ORIGINAL_SYSTEM_PROMPT)
        self.assertEqual((await self.store.get(sid))["adaptiveDialogue"], frozen)
        # Hash from the original v2 benchmark, before the personality change.
        self.assertEqual(v2.sha256_text(v2.ORIGINAL_SYSTEM_PROMPT),
                         "cf847a802c7095c4c7e9010266761f129eac28799e1f7f62ebc0fe5a827ee466")
        self.assertEqual(result["writerMetadata"]["promptSha256"],
                         v2.sha256_text(v2.ORIGINAL_SYSTEM_PROMPT))


class LocalLanguageTests(unittest.TestCase):
    def test_all_cjk_ranges_are_rejected_in_customer_text(self):
        samples = {"BMP Han": "补偿", "extension A": "㐀", "extension B": "\U00020bb7", "extension H": "\U00031350",
                   "compatibility": "豈", "compatibility supplement": "\U0002f800", "radical": "⼈",
                   "hiragana": "ひらがな", "katakana": "カタカナ", "hangul": "한국어", "ideographic comma": "、",
                   "full-width comma": "，", "full-width letters": "ｓｉｚｅ"}
        for name, value in samples.items():
            with self.subTest(name):
                self.assertEqual(v2.language_findings(f"Chị không cần {value} đâu em."), ["non_vietnamese_text"])
        output = say("raise_concern", CHINESE)
        self.assertIn("non_vietnamese_text", v2.output_findings(output, v2.policy_from_limits(LIMITS), PLAN, "x")[0])

    def test_vietnamese_names_and_loanwords_are_not_flagged_locally(self):
        for text in ("Ừ, đổi đôi khác thì còn nghe được, nhưng ơ kìa, ăn nói cẩn thận vào!", LOANWORDS, KAREN,
                     "Đôi Biti's Hunter size 38 này form chật quá, shop xem lại đi… “ok” chưa?",
                     unicodedata.normalize("NFD", "Chị đau gót lắm, em tính sao?")):
            with self.subTest(text=text):
                self.assertEqual(v2.language_findings(text), [])

    def test_verdict_schema_is_strict(self):
        self.assertEqual(v2.parse_verdict(json.dumps(PASS)), PASS)
        self.assertEqual(v2.parse_verdict('```json\n{"verdict": "reject", "codes": ["incoherent_text"]}\n```'),
                         verdict("incoherent_text"))
        for bad in ("not json", None, "[]", json.dumps({"verdict": "pass"}), json.dumps(dict(PASS, reason="fine")),
                    json.dumps({"verdict": "pass", "codes": ["incoherent_text"]}),
                    json.dumps({"verdict": "reject", "codes": []}), json.dumps({"verdict": "PASS", "codes": []}),
                    json.dumps({"verdict": "reject", "codes": ["rude_tone"]}),
                    json.dumps({"verdict": "reject", "codes": "incoherent_text"}),
                    json.dumps({"verdict": "reject", "codes": [["incoherent_text"]]})):
            with self.subTest(bad=bad):
                self.assertIsNone(v2.parse_verdict(bad))

    def test_verifier_judges_language_only_and_has_no_phrase_list(self):
        self.assertEqual(v2.LANGUAGE_CODES, ("non_vietnamese_text", "incoherent_text"))
        self.assertIn("Do not judge politeness, tone", v2.LANGUAGE_GUARD_PROMPT)
        self.assertIn("untrusted data", v2.LANGUAGE_GUARD_PROMPT)
        root = Path(v2.__file__).parent
        for path in (root / "sales_adaptive_v2.py", root / "sales_adaptive_guard.py"):
            source = unicodedata.normalize("NFC", path.read_text(encoding="utf-8")).lower()
            self.assertNotIn("tăng pha", source, path.name)
            self.assertNotIn("tang pha", source, path.name)

    def test_generation_prompt_sets_the_personality_and_language(self):
        prompt = v2.ADAPTIVE_SYSTEM_PROMPT
        for phrase in ("cọc cằn, khó tính", "Karen", "grudgingly", "Ordinary distrust", "Vietnamese only",
                       "speech recognition", "never repeat such a word as if you understood it"):
            self.assertIn(phrase, prompt)
        self.assertNotIn("without insults", prompt)
        self.assertEqual(v2.PROMPT_SHA256, v2.sha256_text(prompt))
        self.assertNotEqual(v2.PROMPT_SHA256, v1.PROMPT_SHA256)


class GuardFlowTests(unittest.IsolatedAsyncioTestCase):
    async def test_chinese_mixed_text_is_regenerated_cleanly(self):
        model = ScriptedModel(lines(CHINESE, CLEAN))
        result = await run(model)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("model", CLEAN))
        # The Han candidate is rejected locally and never sent to the verifier.
        self.assertEqual(model.order, ["generation", "generation", "guard"])
        self.assertEqual([data["lanLine"] for data in model.guard_inputs], [CLEAN])
        regenerate = model.payloads[1]["messages"]
        self.assertIn("non_vietnamese_text", regenerate[-1]["content"])
        self.assertIn("only in Vietnamese", regenerate[-1]["content"])
        self.assertNotIn("补偿", json.dumps(regenerate, ensure_ascii=False))
        self.assertEqual(regenerate[:-1], model.payloads[0]["messages"])

    async def test_model_guard_rejects_nonsense_and_the_fresh_reply_is_rechecked(self):
        model = ScriptedModel(lines(NONSENSE, CLARIFY), verifier=verdicts(verdict("incoherent_text"), PASS))
        result = await run(model)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("model", CLARIFY))
        self.assertEqual(kinds(result), ["initial", "guard", "regenerate", "guard"])
        self.assertEqual([data["lanLine"] for data in model.guard_inputs], [NONSENSE, CLARIFY])
        regenerate = model.payloads[1]["messages"]
        self.assertEqual(regenerate[:-1], model.payloads[0]["messages"])  # the original context, unchanged
        self.assertNotIn("assistant", [message["role"] for message in regenerate])
        self.assertNotIn(NONSENSE, json.dumps(regenerate, ensure_ascii=False))
        self.assertIn("incoherent_text", regenerate[-1]["content"])
        self.assertIn("never repeat it as if you understood it", regenerate[-1]["content"])
        guard_diagnostics = [item for item in result["metadata"]["diagnostics"] if item["role"] == "guard"]
        self.assertEqual([(item["verdict"], item["codes"]) for item in guard_diagnostics],
                         [("reject", ["incoherent_text"]), ("pass", [])])

    async def test_nonsense_variants_are_judged_by_the_model_not_a_phrase_list(self):
        variants = (NONSENSE, "Em xoay pha đế giúp chị nhé?", "Em bóp mềm lớp ga giày giúp chị được không?")
        for text in variants:
            with self.subTest(text=text):
                self.assertEqual(v2.output_findings(say("raise_concern", text), v2.policy_from_limits(LIMITS), PLAN,
                                                    UTTERANCE), ([], []))
                rejected = await run(ScriptedModel(lines(text), verifier=verdicts(verdict("incoherent_text"))))
                self.assertEqual((rejected["source"], rejected["metadata"]["errorCode"]),
                                 ("fallback", "adaptive_guard_rejected"))
                self.assertEqual(rejected["metadata"]["rejectionCodes"], ["incoherent_text"])
                self.assertEqual(roles(rejected), ["generation", "guard", "generation", "guard"])
                # The same text passes when the verifier passes it: code holds no denylist.
                accepted = await run(ScriptedModel(lines(text)))
                self.assertEqual((accepted["source"], accepted["final"]["customerText"]), ("model", text))

    async def test_rough_karen_reply_is_accepted_without_a_politeness_retry(self):
        model = ScriptedModel(lines(KAREN))
        result = await run(model)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("model", KAREN))
        self.assertEqual(model.order, ["generation", "guard"])
        self.assertEqual(model.guard_payloads[0]["messages"][0]["content"], v2.LANGUAGE_GUARD_PROMPT)

    async def test_names_and_loanwords_reach_the_verifier_unchanged(self):
        model = ScriptedModel(lines(LOANWORDS))
        result = await run(model)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("model", LOANWORDS))
        self.assertEqual(model.guard_inputs[0]["lanLine"], LOANWORDS)

    async def test_unusable_verdict_never_passes_the_candidate_and_a_fresh_one_is_checked(self):
        for bad in ("not json", json.dumps(dict(PASS, reason="RATIONALE-TEXT looks fine")),
                    json.dumps({"verdict": "maybe", "codes": []}), httpx.Response(503, json={}),
                    httpx.Response(429, json={}), httpx.Response(200, json={"error": {"message": "RAW-PROVIDER"}})):
            with self.subTest(bad=bad):
                model = ScriptedModel(lines(NONSENSE, CLARIFY), verifier=verdicts(bad, PASS))
                result = await run(model)
                self.assertEqual((result["source"], result["final"]["customerText"]), ("model", CLARIFY))
                self.assertEqual(kinds(result), ["initial", "guard", "regenerate", "guard"])
                # Nothing was wrong with the text itself: a new draft from the same request.
                self.assertEqual(model.payloads[1]["messages"], model.payloads[0]["messages"])
                first = next(item for item in result["metadata"]["diagnostics"] if item["role"] == "guard")
                self.assertIsNone(first["verdict"])
                self.assertIn(first["errorCode"], ("guard_invalid_verdict", "openrouter_http_503", "openrouter_http_429",
                                                   "invalid_openrouter_response"))
                stored = json.dumps(result, ensure_ascii=False)
                self.assertNotIn("RATIONALE-TEXT", stored)
                self.assertNotIn("RAW-PROVIDER", stored)

    async def test_persistently_unusable_verifier_falls_back(self):
        model = ScriptedModel(lines(KAREN), verifier=verdicts("not json"))
        result = await run(model)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("fallback", PLAN["fallbackText"]))
        self.assertEqual((result["metadata"]["errorCode"], result["metadata"]["rejectionCodes"]),
                         ("adaptive_guard_unavailable", ["language_unverified"]))
        self.assertEqual(model.order, ["generation", "guard", "generation", "guard"])

    async def test_fatal_verifier_error_stops_without_regenerating(self):
        model = ScriptedModel(lines(KAREN), verifier=verdicts(httpx.Response(401, json={})))
        result = await run(model)
        self.assertEqual((result["source"], result["metadata"]["errorCode"]), ("fallback", "adaptive_guard_unavailable"))
        self.assertEqual(model.order, ["generation", "guard"])

    async def test_injected_output_without_a_passed_verdict_is_not_used(self):
        class Unverified:
            async def respond(self, messages, validate, guard_context=None):
                return {"output": say("raise_concern", KAREN), "attempts": [], "costUsd": None}

        result = await v2.respond_with_fallback([], v2.policy_from_limits(LIMITS), PLAN, UTTERANCE,
                                                responder=Unverified(), config=v2.request_config(timeout_seconds=1))
        self.assertEqual((result["source"], result["metadata"]["errorCode"]), ("fallback", "adaptive_guard_unavailable"))


class SlowGuardTransport(httpx.AsyncBaseTransport):
    """Generation answers at once; the verifier takes ``delay`` seconds."""

    def __init__(self, delay):
        self.delay, self.order = delay, []

    async def handle_async_request(self, request_):
        payload = json.loads(request_.content)
        self.order.append(v2.request_role(payload))
        if self.order[-1] == "guard":
            await asyncio.sleep(self.delay)
            return httpx.Response(200, json=body(PASS, cost=GUARD_COST))
        return httpx.Response(200, json=body(say("raise_concern", KAREN)))


class DeadlineAndCostTests(unittest.IsolatedAsyncioTestCase):
    async def run_slow(self, delay, timeout):
        transport = SlowGuardTransport(delay)
        responder = v2.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key=KEY),
                                            client=httpx.AsyncClient(transport=transport),
                                            config=v2.request_config(timeout_seconds=timeout))
        result = await v2.respond_with_fallback([{"role": "user", "content": "{}"}], v2.policy_from_limits(LIMITS), PLAN,
                                                UTTERANCE, responder=responder, config=responder.config)
        return result, transport

    async def test_each_verifier_call_has_a_short_timeout(self):
        with patch.object(v2, "GUARD_TIMEOUT_SECONDS", 0.1):
            result, transport = await self.run_slow(2.0, 5)
        self.assertEqual((result["source"], result["metadata"]["errorCode"]), ("fallback", "adaptive_guard_unavailable"))
        self.assertEqual(transport.order, ["generation", "guard", "generation", "guard"])
        guards = [item for item in result["metadata"]["attempts"] if item["role"] == "guard"]
        self.assertEqual([item["errorCode"] for item in guards], ["guard_timeout", "guard_timeout"])
        self.assertLess(result["metadata"]["durationSeconds"], 1.0)

    async def test_verifier_shares_the_turn_deadline(self):
        result, transport = await self.run_slow(2.0, 0.4)
        self.assertEqual((result["source"], result["metadata"]["errorCode"]), ("fallback", "adaptive_timeout"))
        self.assertEqual(transport.order, ["generation", "guard"])
        last = result["metadata"]["attempts"][-1]
        self.assertEqual((last["role"], last["kind"], last["errorCode"]), ("guard", "guard", "openrouter_timeout"))
        self.assertLess(result["metadata"]["durationSeconds"], 1.0)

    async def test_every_generation_and_verifier_attempt_is_charged_by_role(self):
        model = ScriptedModel(lines(NONSENSE, CLARIFY), verifier=verdicts(verdict("incoherent_text")))
        metadata = (await run(model))["metadata"]
        self.assertEqual(roles({"metadata": metadata}), ["generation", "guard", "generation", "guard"])
        self.assertEqual((metadata["generationAttempts"], metadata["guardAttempts"]), (2, 2))
        self.assertAlmostEqual(metadata["costUsd"], 2 * 0.0002 + 2 * GUARD_COST)
        self.assertAlmostEqual(metadata["costUsdByRole"]["generation"], 0.0004)
        self.assertAlmostEqual(metadata["costUsdByRole"]["guard"], 2 * GUARD_COST)
        self.assertEqual([attempt["forAttempt"] for attempt in metadata["attempts"] if attempt["role"] == "guard"],
                         [0, 2])
        unknown = httpx.Response(200, json=dict(body(dict(PASS, salespersonClarity="clear")), usage={"prompt_tokens": 80}))
        metadata = (await run(ScriptedModel(lines(KAREN), verifier=verdicts(unknown))))["metadata"]
        self.assertIsNone(metadata["costUsd"])
        self.assertIsNone(metadata["costUsdByRole"]["guard"])
        self.assertEqual(metadata["costMissingAttempts"], 1)

    async def test_verifier_request_is_minimal_and_reasoning_off(self):
        model = ScriptedModel(lines(KAREN))
        await run(model, config=v2.request_config(reasoning="on", timeout_seconds=5))
        generation, check = model.payloads[0], model.guard_payloads[0]
        self.assertEqual(generation["reasoning"], {"enabled": True, "max_tokens": 1024})
        self.assertEqual(check["reasoning"], {"enabled": False})
        self.assertEqual((check["max_tokens"], check["temperature"]), (v2.GUARD_MAX_TOKENS, v2.GUARD_TEMPERATURE))
        for key in ("model", "provider", "response_format", "usage", "stream"):
            self.assertEqual(check[key], generation[key], key)
        self.assertEqual(set(model.guard_inputs[0]), GUARD_INPUT_KEYS)
        self.assertNotIn(KEY, json.dumps(check, ensure_ascii=False))
        data = check["messages"][1]["content"]
        for private in ("allowedActions", "facts", "hints", "limits", "unknownToLan", "storePolicy", "purchasedShoe"):
            self.assertNotIn(private, data)


class PipelineGuardTests(Harness):
    async def test_turn_regenerates_privately_and_a_retried_turn_is_not_charged_again(self):
        sid = await self.session("guard-turn", V2)
        model = ScriptedModel(lines(NONSENSE, CLARIFY), verifier=verdicts(verdict("incoherent_text"), PASS))
        original, crashed = self.store.finalize_turn, []

        async def flaky(*args, **kwargs):
            if not crashed:
                crashed.append(True)
                raise RuntimeError("disk full")
            return await original(*args, **kwargs)

        with patch.object(self.store, "finalize_turn", flaky):
            failed = await self.turn(sid, "acknowledgment", responder=model.responder(), turn_id="g1",
                                     utterance=UTTERANCE)
        self.assertEqual(failed["status"], "failed")
        result = await self.turn(sid, "acknowledgment", responder=model.responder(), turn_id="g1", utterance=UTTERANCE)
        self.assertEqual((result["status"], result["adaptiveAction"]["source"], result["customerText"]),
                         ("accepted", "model", CLARIFY))
        self.assertEqual((len(model.payloads), len(model.guard_payloads)), (2, 2))  # no second charge
        metadata = result["writerMetadata"]
        self.assertAlmostEqual(metadata["costUsd"], 2 * 0.0002 + 2 * GUARD_COST)
        self.assertEqual((metadata["generationAttempts"], metadata["guardAttempts"]), (2, 2))
        public = json.dumps(result, ensure_ascii=False)
        for private in (NONSENSE, "incoherent_text", "verdict", "lanLine", "languageGuard", "rejectionCodes",
                        "language_unverified"):
            self.assertNotIn(private, public)
        state = await self.store.get(sid)
        stored = state["adaptiveDiagnostics"]["g1"]
        self.assertEqual(stored["languageGuard"], "passed")
        self.assertEqual([(item["role"], item["kind"]) for item in stored["attempts"]],
                         [("generation", "initial"), ("guard", "guard"), ("generation", "regenerate"),
                          ("guard", "guard")])
        self.assertEqual(stored["attempts"][1]["codes"], ["incoherent_text"])
        self.assertEqual(stored["attempts"][0]["candidate"]["customerText"], NONSENSE)
        guard_input = model.guard_inputs[0]
        self.assertEqual(set(guard_input), GUARD_INPUT_KEYS)
        self.assertEqual(guard_input["salespersonCurrentWords"], UTTERANCE)
        self.assertEqual(guard_input["lanPreviousLine"], model.contexts[0]["conversation"][-1]["lan"])
        self.assertNotIn("adaptiveDiagnostics", json.dumps(public_session(state), ensure_ascii=False))
        self.assertEqual(len(state["evidenceLedger"]), 1)

    async def test_v1_session_never_calls_the_verifier(self):
        sid = await self.session("v1-unaffected", V1)
        model = ScriptedModel(step)
        result = await self.turn(sid, "refusesRemedy", responder=model.responder(v1))
        self.assertEqual(result["adaptiveAction"]["version"], v1.ADAPTIVE_VERSION)
        self.assertEqual((model.order, model.guard_payloads), (["generation"], []))
        self.assertNotIn("guardAttempts", result["writerMetadata"])


if __name__ == "__main__":
    unittest.main()
