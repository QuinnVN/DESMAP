"""Adaptive Lan v2: the model reacts to the utterance; code keeps facts, scoring and limits.

The real v2 AdaptiveLanResponder runs against httpx.MockTransport, so payloads,
bounded attempts, the guard, warnings, diagnostics and cost capture are
exercised as in production. STT and Jev are the existing scripted fakes.
"""
import asyncio
import copy
import json
import os
import tempfile
import unittest
from dataclasses import replace
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

import httpx

from app import sales_adaptive as v1
from app import sales_adaptive_v2 as v2
from app import sales_adaptive_versions as versions
from app.config import get_settings
from app.sales_adaptive_guard import completion_findings, evaluate, policy_findings
from app.sales_openrouter import FACTS
from app.sales_pipeline import complete, process_turn
from app.sales_returning_customer import CompletionRequest, ReturningSessionStore, public_session
from app.tests.sales_fixtures import write_part1
from app.tests.test_sales_adaptive import LEGACY, ExplodingResponder, LegacyWriter
from app.tests.test_sales_pipeline_api import classification
from app.tests.test_sales_returning_customer import FakeTranscriber, request

ADAPTIVE = dict(sales_adaptive_dialogue_enabled=True, sales_adaptive_reasoning="off", sales_adaptive_max_tokens=4096,
                sales_adaptive_reasoning_max_tokens=1024, sales_adaptive_timeout_seconds=45.0)
V1 = SimpleNamespace(**vars(LEGACY), **ADAPTIVE, sales_adaptive_version=v1.ADAPTIVE_VERSION)
V2 = SimpleNamespace(**vars(LEGACY), **ADAPTIVE, sales_adaptive_version=v2.ADAPTIVE_VERSION)
KEY = "sk-or-v1-test-secret-key-0123456789"
QUESTION = "Dạ chị mua đôi này lúc nào vậy chị?"
ANSWER = "Chị mua đôi này ở tiệm mình ba ngày trước. Giờ chân chị vẫn đau, em tính sao?"
THREE = ("Chị hiểu em đang cố giải thích. Nhưng chân chị vẫn đau mỗi lần mang đôi này. "
         "Em nói rõ giúp chị bên mình sẽ kiểm tra độ vừa thế nào?")
QUIET_CLOSE = "Thôi, chuyện này để chị tự tính vậy, chị không ở lại thêm nữa."
STEP = "Em nói rõ giúp chị bên mình sẽ kiểm tra độ vừa cho chị theo cách nào được không?"


def body(content, *, cost=0.0002, reasoning=0, reasoning_text=None):
    text = content if isinstance(content, str) else json.dumps(content, ensure_ascii=False)
    message = {"content": text}
    if reasoning_text:
        message["reasoning"] = reasoning_text
    return {"id": "gen-test", "model": "qwen/qwen3.7-flash", "provider": "Alibaba",
            "choices": [{"finish_reason": "stop", "message": message}],
            "usage": {"prompt_tokens": 900, "completion_tokens": 80 + reasoning, "total_tokens": 980 + reasoning,
                      "completion_tokens_details": {"reasoning_tokens": reasoning},
                      "prompt_tokens_details": {"cached_tokens": 0}, "cost": cost}}


def say(action, text, *, decision="continue", reason=None, facts=None):
    return {"action": action, "decision": decision, "closeReason": reason, "customerText": text, "factIds": facts or []}


PASS = {"verdict": "pass", "codes": []}
GUARD_COST = 0.00001


def verdict(*codes):
    return {"verdict": "reject", "codes": list(codes)} if codes else dict(PASS)


class ScriptedModel:
    """Plays the model: strategy(context, call number, repair) -> output, or an httpx.Response.

    Verifier requests are answered by verifier(guard input, guard call number) ->
    verdict, raw string or httpx.Response (default: pass). ``payloads`` holds only
    generation requests; ``guard_payloads`` the verifier requests."""

    def __init__(self, strategy, *, reasoning_text=None, verifier=None):
        self.strategy, self.payloads, self.contexts, self.reasoning_text = strategy, [], [], reasoning_text
        self.verifier = verifier or (lambda data, call: PASS)
        self.guard_payloads, self.guard_inputs, self.order = [], [], []

    def handler(self, request_):
        payload = json.loads(request_.content)
        if v2.request_role(payload) == "guard":
            self.guard_payloads.append(payload)
            self.order.append("guard")
            data = json.loads(payload["messages"][1]["content"])
            self.guard_inputs.append(data)
            result = self.verifier(data, len(self.guard_payloads))
            if (isinstance(result, dict) and set(result) == {"verdict", "codes"}
                    and "salespersonClarity" in payload["messages"][0]["content"]):
                result = dict(result, salespersonClarity="clear")
            if isinstance(result, httpx.Response):
                return result
            return httpx.Response(200, json=body(result, cost=GUARD_COST))
        self.order.append("generation")
        self.payloads.append(payload)
        context = json.loads(next(m["content"] for m in payload["messages"] if m["role"] == "user"))
        self.contexts.append(context)
        result = self.strategy(context, len(self.payloads), len(payload["messages"]) > 2)
        if isinstance(result, httpx.Response):
            return result
        return httpx.Response(200, json=body(result, reasoning_text=self.reasoning_text))

    def responder(self, module=v2, config=None):
        client = httpx.AsyncClient(transport=httpx.MockTransport(self.handler))
        config = config or (v2.request_config(timeout_seconds=5) if module is v2
                            else v1.AdaptiveRequestConfig(timeout_seconds=5))
        return module.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key=KEY), client=client, config=config)


def step(context, call, repair):
    action = "request_next_step" if "request_next_step" in context["allowedActions"] else context["allowedActions"][0]
    facts = context["facts"]["mustAnswerFactIds"]
    texts = {**context["facts"]["sharedFacts"], **context["facts"]["mayDiscloseNow"]}
    prefix = " ".join(texts[fact] for fact in facts)
    return say(action, (prefix + " " + STEP).strip(), facts=facts)


class Harness(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.directory = Path(self.temp.name)
        self.store = ReturningSessionStore(self.directory)
        self.index = 0

    async def asyncTearDown(self):
        self.temp.cleanup()

    async def session(self, session_id, settings):
        attempt = write_part1(self.directory, session_id, "run-" + session_id)
        with patch("app.sales_returning_customer.get_settings", return_value=settings):
            await self.store.create_or_resume(session_id, attempt, run_id="run-" + session_id)
        return session_id

    async def turn(self, session_id, *true, responder=None, writer=None, turn_id=None, utterance="Dạ, em nghe chị.", values=None):
        self.index += 1

        class Classifier:
            async def classify(self, transcript, context):
                return classification(*true, **(values or {}))

        return await process_turn(session_id, request(turn_id or f"t{self.index}"), store=self.store,
                                  transcriber=FakeTranscriber(utterance), classifier=Classifier(), writer=writer,
                                  responder=responder)


class ClarificationTurnTests(Harness):
    REPEAT = "Chị chưa nghe rõ đoạn vừa rồi, em nói lại cho rõ được không?"

    def repeat_model(self, **kwargs):
        return ScriptedModel(lambda context, call, repair: say("request_repeat", self.REPEAT), **kwargs)

    async def test_repeat_is_saved_without_spending_any_turn_budget(self):
        sid = await self.session("repeat-budget", V2)
        before = await self.store.get(sid)
        model = self.repeat_model()
        for index in range(3):
            result = await self.turn(sid, responder=model.responder(), utterance="Dạ tăng pha chị ạ.")
            self.assertEqual((result["status"], result["adaptiveAction"]["source"]), ("accepted", "model"),
                             (await self.store.get(sid)).get("adaptiveDiagnostics"))
            self.assertTrue(result["clarificationRequested"])
            for counter in ("acceptedTurnCount", "dialogueTurnCount", "evaluableTurnCount"):
                self.assertEqual(result[counter], before[counter], counter)
            self.assertEqual(result["remainingTurns"], before["maxTurns"])
            self.assertFalse(result["conversationComplete"])
        state = await self.store.get(sid)
        self.assertEqual(len(state["turns"]), 3)
        self.assertEqual(len(state["evidenceLedger"]), 3)
        self.assertTrue(all(entry["clarificationRequested"] and not entry["noProgress"]
                            for entry in state["evidenceLedger"]))
        self.assertEqual(state["customerState"]["hintLevels"], {"1": 0, "2": 0, "3": 0, "4": 0})
        self.assertTrue(all(data["clarificationRequest"] for data in model.guard_inputs))
        # A real answer afterwards consumes one turn, not four.
        result = await self.turn(sid, "acknowledgment", responder=ScriptedModel(step).responder())
        self.assertEqual((result["acceptedTurnCount"], result["dialogueTurnCount"]), (1, 1))
        self.assertFalse(result.get("clarificationRequested", False))

    async def test_repeat_at_last_slot_does_not_end_the_conversation(self):
        sid = await self.session("repeat-cap", V2)
        state = await self.store.get(sid)
        state.update(dialogueTurnCount=7, evaluableTurnCount=7, acceptedTurnCount=7)
        await self.store.save(state)
        result = await self.turn(sid, responder=self.repeat_model().responder())
        self.assertTrue(result.get("clarificationRequested"))
        self.assertEqual((result["dialogueTurnCount"], result["remainingTurns"]), (7, 1))
        self.assertFalse(result["conversationComplete"])
        self.assertIsNone(result["endingReason"])
        self.assertEqual((await self.store.get(sid))["status"], "active")
        result = await self.turn(sid, responder=ScriptedModel(step).responder())
        self.assertTrue(result["conversationComplete"])
        self.assertEqual((result["dialogueTurnCount"], result["endingReason"]), (8, "turn_limit"))

    async def test_repeat_defers_stalemate_without_resetting_previous_hints(self):
        sid = await self.session("repeat-stalemate", V2)
        for _ in range(2):
            await self.turn(sid, responder=ScriptedModel(step).responder())
        before = await self.store.get(sid)
        result = await self.turn(sid, responder=self.repeat_model().responder())
        self.assertTrue(result.get("clarificationRequested"))
        self.assertFalse(result["conversationComplete"])
        after = await self.store.get(sid)
        self.assertEqual(after["customerState"]["hintLevels"], before["customerState"]["hintLevels"])
        result = await self.turn(sid, responder=ScriptedModel(step).responder())
        self.assertEqual(result["endingReason"], "stalemate")

    async def test_ordinary_proposal_question_still_consumes_a_turn(self):
        sid = await self.session("repeat-ordinary", V2)
        model = ScriptedModel(lambda context, call, repair: say("ask_clarification", STEP))
        result = await self.turn(sid, responder=model.responder())
        self.assertEqual((result["acceptedTurnCount"], result["dialogueTurnCount"]), (1, 1))
        self.assertFalse(result.get("clarificationRequested", False))
        self.assertNotIn("clarificationRequest", model.guard_inputs[0])

    async def test_guard_rejection_or_unavailability_never_earns_a_free_turn(self):
        for name, check in (("reject", lambda data, call: verdict("incoherent_text")),
                            ("unavailable", lambda data, call: "invalid JSON")):
            sid = await self.session("repeat-" + name, V2)
            model = self.repeat_model(verifier=check)
            result = await self.turn(sid, responder=model.responder())
            self.assertTrue(result["fallbackUsed"])
            self.assertEqual(result["dialogueTurnCount"], 1)
            self.assertFalse(result.get("clarificationRequested", False))

    async def test_clear_violation_is_kept_even_when_another_fragment_needs_repeating(self):
        sid = await self.session("repeat-violation", V2)
        result = await self.turn(sid, "unauthorizedRefund", responder=self.repeat_model().responder(),
                                 utterance="Em hoàn tiền cho chị, còn tăng pha thì...")
        self.assertTrue(result.get("clarificationRequested"))
        self.assertEqual(result["dialogueTurnCount"], 0)
        self.assertIn("unauthorized_refund", result["policyViolations"])
        self.assertEqual(result["playerResponseRating"], "bad")
        state = await self.store.get(sid)
        self.assertIn("unauthorized_refund", state["unresolvedPromises"])

    async def test_repeat_cannot_hide_mandatory_fact_answers(self):
        sid = await self.session("repeat-fact", V2)
        self.assertNotIn("fit_condition", (await self.store.get(sid))["investigationEvidence"])
        result = await self.turn(sid, "fitQuestion", responder=self.repeat_model().responder(),
                                 utterance="Đôi giày có bị chật không chị? Tăng pha nữa.")
        self.assertTrue(result["fallbackUsed"])
        self.assertFalse(result.get("clarificationRequested", False))
        self.assertEqual(result["dialogueTurnCount"], 1)

    async def test_repeat_can_answer_clear_part_and_preserve_its_evidence(self):
        sid = await self.session("repeat-clear-part", V2)

        def answer_and_repeat(context, call, repair):
            facts = context["facts"]["mustAnswerFactIds"]
            texts = {**context["facts"]["sharedFacts"], **context["facts"]["mayDiscloseNow"]}
            return say("request_repeat", " ".join(texts[fact] for fact in facts) + " " + self.REPEAT, facts=facts)

        self.assertNotIn("fit_condition", (await self.store.get(sid))["investigationEvidence"])
        result = await self.turn(sid, "fitQuestion", responder=ScriptedModel(answer_and_repeat).responder(),
                                 utterance="Đôi giày có bị chật không chị? Tăng pha nữa.")
        self.assertTrue(result.get("clarificationRequested"))
        self.assertEqual(result["dialogueTurnCount"], 0)
        state = await self.store.get(sid)
        self.assertIn("fit_condition", state["investigationEvidence"])
        self.assertEqual(state["evidenceLedger"][-1]["disclosedFactIds"], ["fit_condition"])

    def test_repeat_cannot_bypass_the_trust_challenge(self):
        limits = dict(GuardTests.LIMITS, repeatAllowed=True, mustVoiceTrustDoubt=True, challengeShown=True)
        policy = v2.policy_from_limits(limits)
        self.assertEqual(policy["allowedActions"], ["voice_trust_doubt"])
        codes, _ = v2.output_findings(say("request_repeat", self.REPEAT), policy, GuardTests.PLAN, "Không rõ")
        self.assertIn("action_not_allowed", codes)

    async def test_hard_ending_cannot_be_replaced_by_a_repeat(self):
        sid = await self.session("repeat-hard-ending", V2)
        result = await self.turn(sid, "managerEscalation", responder=self.repeat_model().responder())
        self.assertTrue(result["conversationComplete"])
        self.assertEqual(result["endingReason"], "manager_escalation")
        self.assertFalse(result.get("clarificationRequested", False))

    async def test_repeat_checkpoint_and_duplicate_are_idempotent(self):
        sid = await self.session("repeat-checkpoint", V2)
        model = self.repeat_model()
        finalize, failures = self.store.finalize_turn, []

        async def fail_once(*args, **kwargs):
            if not failures:
                failures.append(True)
                raise RuntimeError("disk full")
            return await finalize(*args, **kwargs)

        with patch.object(self.store, "finalize_turn", fail_once):
            failed = await self.turn(sid, responder=model.responder(), turn_id="repeat-id")
        self.assertEqual(failed["status"], "failed")
        result = await self.turn(sid, responder=model.responder(), turn_id="repeat-id")
        duplicate = await self.turn(sid, responder=model.responder(), turn_id="repeat-id")
        self.assertTrue(result.get("clarificationRequested"))
        self.assertEqual(duplicate, result)
        self.assertEqual(model.order, ["generation", "guard"])
        state = await self.store.get(sid)
        self.assertEqual((len(state["evidenceLedger"]), len(state["turns"]), state["dialogueTurnCount"]), (1, 1, 0))

    async def test_time_limit_can_finish_a_session_containing_only_repeats(self):
        sid = await self.session("repeat-only-completion", V2)
        result = await self.turn(sid, responder=self.repeat_model().responder())
        self.assertTrue(result.get("clarificationRequested"))
        request_ = CompletionRequest(completionId="repeat-complete", reason="time_limit")
        done = await complete(sid, request_, store=self.store)
        self.assertEqual((done["status"], done["completionStatus"]), ("finished", "failed"))
        self.assertEqual(done["assessmentStatus"], "failed")
        self.assertEqual(done["terminalError"]["code"], "speech_unintelligible")
        self.assertIsNone(done.get("score"))
        self.assertIsNone(done.get("trustState"))
        self.assertEqual((done["dialogueTurnCount"], done["acceptedTurnCount"]), (0, 0))
        self.assertEqual(len(done["evidenceLedger"]), 1)
        self.assertEqual(await complete(sid, request_, store=self.store), done)

    async def test_fifth_consecutive_repeat_closes_without_spending_a_turn(self):
        sid = await self.session("repeat-limit", V2)
        model = self.repeat_model()
        for index in range(5):
            result = await self.turn(sid, responder=model.responder(), utterance="Dạ tăng pha chị ạ.")
            self.assertTrue(result.get("clarificationRequested"))
            self.assertEqual(result["conversationComplete"], index == 4)
            self.assertEqual((result["dialogueTurnCount"], result["acceptedTurnCount"]), (0, 0))
        self.assertEqual(result["endingReason"], "clarification_limit")
        self.assertEqual(result["customerText"], v2.REPEAT_LIMIT_TEXT)
        self.assertEqual((result["adaptiveAction"]["action"], result["adaptiveAction"]["decision"]),
                         ("close_conversation", "close"))
        state = await self.store.get(sid)
        self.assertEqual((state["consecutiveRepeatRequests"], len(state["evidenceLedger"])), (5, 5))
        self.assertEqual(state["status"], "finished")
        self.assertEqual(state["assessmentStatus"], "failed")
        self.assertEqual(result["terminalError"]["code"], "speech_unintelligible")
        self.assertIsNone(state.get("score"))
        self.assertIsNone(state.get("trustState"))
        self.assertEqual(state["policyViolations"], [])
        self.assertEqual((len(model.payloads), len(model.guard_payloads)), (5, 5))
        done = await complete(sid, CompletionRequest(completionId="limit-complete"), store=self.store)
        self.assertEqual((done["status"], done["completionReason"]), ("finished", "clarification_limit"))
        self.assertEqual(done["finalCustomerText"], v2.REPEAT_LIMIT_TEXT)

    async def test_repeat_only_failure_skips_review_and_scoring_and_is_idempotent(self):
        sid = await self.session("repeat-no-review", V2)
        for _ in range(2):
            await self.turn(sid, responder=self.repeat_model().responder())
        state = await self.store.get(sid)
        state.update(assessmentStatus="needs-review", clarificationCount=3,
                     assessmentReviewItems=[{"turnId": "t1", "labels": ["abuse"], "objective": 1}])
        await self.store.save(state)
        with patch("app.sales_review.resolve_review", side_effect=AssertionError("must not review")), \
                patch("app.sales_pipeline.score_session", side_effect=AssertionError("must not score")):
            done = await complete(sid, CompletionRequest(completionId="unintelligible", reason="time_limit"), store=self.store)
            duplicate = await complete(sid, CompletionRequest(completionId="another-id"), store=self.store)
        self.assertEqual(done, duplicate)
        self.assertEqual(done["completionStatus"], "failed")
        self.assertIsNone(public_session(done).get("score"))
        with self.assertRaisesRegex(RuntimeError, "session_not_accepting_turns"):
            await self.turn(sid, responder=self.repeat_model().responder())

    async def test_valid_speech_before_repeat_limit_still_gets_an_assessment(self):
        sid = await self.session("repeat-after-clear", V2)
        await self.turn(sid, "acknowledgment", responder=ScriptedModel(step).responder())
        for _ in range(5):
            result = await self.turn(sid, responder=self.repeat_model().responder())
        self.assertNotIn("terminalError", result)
        done = await complete(sid, CompletionRequest(completionId="with-evidence"), store=self.store)
        self.assertEqual(done["completionStatus"], "completed")
        self.assertIsNotNone(done.get("score"))

    async def test_uncertain_only_session_fails_without_review(self):
        sid = await self.session("uncertain-only", V2)
        await self.turn(sid, responder=ScriptedModel(step).responder(), values={"abuse": .5})
        with patch("app.sales_review.resolve_review", side_effect=AssertionError("must not review")):
            done = await complete(sid, CompletionRequest(completionId="uncertain-stop", reason="time_limit"), store=self.store)
        self.assertEqual(done["assessmentStatus"], "failed")
        self.assertIsNone(done.get("score"))
        self.assertEqual(done["terminalError"]["code"], "speech_unintelligible")

    async def test_clear_violation_in_a_repeat_is_not_discarded_at_completion(self):
        sid = await self.session("partial-clear-violation", V2)
        await self.turn(sid, "unauthorizedRefund", responder=self.repeat_model().responder())
        done = await complete(sid, CompletionRequest(completionId="partial-stop", reason="time_limit"), store=self.store)
        self.assertEqual(done["completionStatus"], "completed")
        self.assertNotIn("terminalError", done)
        self.assertTrue(any(v["code"] == "unauthorized_refund" for v in done["policyViolations"]))

    async def test_normal_verified_reply_resets_the_repeat_streak(self):
        sid = await self.session("repeat-reset", V2)
        for _ in range(4):
            await self.turn(sid, responder=self.repeat_model().responder())
        result = await self.turn(sid, "acknowledgment", responder=ScriptedModel(step).responder())
        self.assertFalse(result.get("clarificationRequested", False))
        self.assertEqual((await self.store.get(sid))["consecutiveRepeatRequests"], 0)
        for index in range(4):
            result = await self.turn(sid, responder=self.repeat_model().responder())
            self.assertFalse(result["conversationComplete"])
            self.assertEqual(result["dialogueTurnCount"], 1)
        self.assertEqual((await self.store.get(sid))["consecutiveRepeatRequests"], 4)

    async def test_fifth_repeat_retry_does_not_increment_or_call_again(self):
        sid = await self.session("repeat-limit-retry", V2)
        model = self.repeat_model()
        for _ in range(4):
            await self.turn(sid, responder=model.responder())
        result = await self.turn(sid, responder=model.responder(), turn_id="fifth")
        duplicate = await self.turn(sid, responder=model.responder(), turn_id="fifth")
        self.assertEqual(duplicate, result)
        self.assertTrue(result["conversationComplete"])
        self.assertEqual((await self.store.get(sid))["consecutiveRepeatRequests"], 5)
        self.assertEqual((len(model.payloads), len(model.guard_payloads)), (5, 5))

    async def test_previous_karen_revision_keeps_prompt_guard_and_counting(self):
        sid = await self.session("repeat-previous", V2)
        state = await self.store.get(sid)
        state["adaptiveDialogue"].update(dialogueRevision=v2.PREVIOUS_DIALOGUE_REVISION,
            promptSha256=v2.sha256_text(v2.KAREN_LANGUAGE_SYSTEM_PROMPT), languageGuard=v2.PREVIOUS_GUARD_REQUEST)
        await self.store.save(state)
        self.assertTrue(v2.supported(state))
        model = ScriptedModel(lambda context, call, repair:
                              say("ask_clarification", "Chị chưa hiểu đoạn đó, em nói lại cho rõ được không?"))
        result = await self.turn(sid, responder=model.responder())
        self.assertEqual(result["dialogueTurnCount"], 1)
        self.assertNotIn("request_repeat", model.contexts[0]["allowedActions"])
        self.assertEqual(model.payloads[0]["messages"][0]["content"], v2.KAREN_LANGUAGE_SYSTEM_PROMPT)
        self.assertEqual(model.guard_payloads[0]["messages"][0]["content"], v2.PREVIOUS_LANGUAGE_GUARD_PROMPT)
        self.assertEqual(result["writerMetadata"]["guardRequest"], v2.PREVIOUS_GUARD_REQUEST)


class ConfigTests(Harness):
    def test_default_version_and_env_parsing(self):
        self.assertEqual(get_settings().sales_adaptive_version, v2.ADAPTIVE_VERSION)
        self.assertFalse(get_settings().sales_adaptive_dialogue_enabled)
        for value, expected in (("v1", v1.ADAPTIVE_VERSION), ("lan-adaptive-v1", v1.ADAPTIVE_VERSION),
                                ("V2", v2.ADAPTIVE_VERSION), ("bogus", v2.ADAPTIVE_VERSION), ("", v2.ADAPTIVE_VERSION)):
            with patch.dict(os.environ, {"SALES_ADAPTIVE_VERSION": value}):
                self.assertEqual(get_settings().sales_adaptive_version, expected, value)

    async def test_each_session_freezes_its_version(self):
        for name, settings, version in (("f1", V1, v1.ADAPTIVE_VERSION), ("f2", V2, v2.ADAPTIVE_VERSION)):
            state = await self.store.get(await self.session(name, settings))
            self.assertEqual(state["adaptiveDialogue"]["version"], version)
            self.assertIs(versions.module_for(state), v1 if version == v1.ADAPTIVE_VERSION else v2)
            self.assertTrue(public_session(state)["pipelineSupported"])
        state = await self.store.get(await self.session("f0", LEGACY))
        self.assertNotIn("adaptiveDialogue", state)
        self.assertIsNone(versions.module_for(state))

    async def test_legacy_session_never_calls_a_responder(self):
        sid = await self.session("legacy", LEGACY)
        result = await self.turn(sid, "refusesRemedy", responder=ExplodingResponder(), writer=LegacyWriter())
        self.assertEqual(result["status"], "accepted")
        self.assertNotIn("adaptiveAction", result)

    async def test_v1_session_keeps_v1_after_settings_switch_to_v2(self):
        sid = await self.session("keep-v1", V1)
        model = ScriptedModel(step)
        with patch("app.config.get_settings", return_value=replace(get_settings(), sales_adaptive_version=v2.ADAPTIVE_VERSION)):
            result = await self.turn(sid, "refusesRemedy", responder=model.responder(v1))
        self.assertEqual(result["adaptiveAction"]["version"], v1.ADAPTIVE_VERSION)
        self.assertIn("observations", model.contexts[0])  # v1 context shape
        self.assertEqual(model.payloads[0]["messages"][0]["content"], v1.ADAPTIVE_SYSTEM_PROMPT)
        self.assertNotIn("adaptiveDiagnostics", await self.store.get(sid))

    async def test_missing_or_unknown_version_is_unsupported_not_upgraded(self):
        for name, mutate in (("missing", lambda config: config.pop("version")),
                             ("unknown", lambda config: config.update(version="lan-adaptive-v9")),
                             ("bad-model", lambda config: config.update(model="other/model"))):
            sid = await self.session(name, V2)
            state = await self.store.get(sid)
            mutate(state["adaptiveDialogue"])
            await self.store.save(state)
            self.assertFalse(public_session(state)["pipelineSupported"])
            self.assertEqual(public_session(state)["pipelineUnavailableReason"], "pipeline_version_unsupported")
            frozen = copy.deepcopy(state["adaptiveDialogue"])
            model = ScriptedModel(step)
            result = await self.turn(sid, "acknowledgment", responder=model.responder())
            self.assertEqual((result["status"], result["error"]["code"]), ("failed", "pipeline_version_unsupported"))
            self.assertEqual(model.payloads, [])
            self.assertEqual((await self.store.get(sid))["adaptiveDialogue"], frozen)


class ChoiceTests(Harness):
    async def test_colloquial_policy_objection_is_verified_without_repair(self):
        sid = await self.session("policy-objection", V2)
        text = "Em có quyền hứa bồi thường hay giảm giá đâu? Chị chỉ cần đổi đôi khác vừa chân thôi."
        model = ScriptedModel(lambda context, call, repair: say("reject_promise", text))
        result = await self.turn(sid, responder=model.responder())
        self.assertEqual(result["customerText"], text)
        self.assertFalse(result["fallbackUsed"])
        self.assertEqual(model.order, ["generation", "guard"])

    async def test_off_label_question_is_answered_in_v2_but_not_v1(self):
        def answer(context, call, repair):
            return say("answer_question", ANSWER, facts=["purchase_history"])

        results = {}
        for name, settings, module in (("q1", V1, v1), ("q2", V2, v2)):
            sid = await self.session(name, settings)
            model = ScriptedModel(answer)
            # Jev saw only an acknowledgment, not the purchase question.
            results[name] = (await self.turn(sid, "acknowledgment", responder=model.responder(module),
                                             utterance=QUESTION), model)
        old, old_model = results["q1"]
        self.assertEqual(old["adaptiveAction"]["source"], "fallback")
        self.assertNotIn("answer_question", old_model.contexts[0]["allowedActions"])
        new, new_model = results["q2"]
        self.assertEqual((new["adaptiveAction"]["source"], new["adaptiveAction"]["action"]), ("model", "answer_question"))
        self.assertEqual(new["customerText"], ANSWER)
        self.assertEqual(len(new_model.payloads), 1)
        context = new_model.contexts[0]
        self.assertEqual(context["currentSalespersonUtterance"], QUESTION)
        self.assertEqual(context["hints"]["classifierActs"], ["acknowledgment"])
        self.assertIn("answer_question", context["allowedActions"])
        self.assertNotIn("observations", context)
        for private in ("score", "turnQuality", "rubricComponents", "noul", "sessionId", "evidenceLedger"):
            self.assertNotIn(private, json.dumps(context))

    async def test_same_labels_give_same_ledger_and_score_across_legacy_v1_v2(self):
        def v2_choice(context, call, repair):
            if context["limits"]["closeAllowed"]:
                return say("close_conversation", QUIET_CLOSE, decision="close", reason="unresolved_frustration")
            return say("express_frustration" if call % 2 else "ask_clarification",
                       ("Chị thấy mình cứ nói vòng vòng mà chân chị vẫn đau.",
                        "Ý em là sao, em nói rõ cho chị được không?")[call % 2])

        legacy, old, new = (await self.session("i0", LEGACY), await self.session("i1", V1), await self.session("i2", V2))
        script = [("acknowledgment", "openQuestion"), ("refusesRemedy",), ("refusesRemedy",)]
        model_v1, model_v2 = ScriptedModel(step), ScriptedModel(v2_choice)
        for number, labels in enumerate(script, 1):
            await self.turn(legacy, *labels, writer=LegacyWriter(), turn_id=f"s{number}")
            await self.turn(old, *labels, responder=model_v1.responder(v1), turn_id=f"s{number}")
            result = await self.turn(new, *labels, responder=model_v2.responder(), turn_id=f"s{number}")
        self.assertEqual((result["endingReason"], result["adaptiveAction"]["action"]),
                         (v2.SOFT_CLOSE_ENDING, "close_conversation"))
        strip = lambda ledger: [{key: entry[key] for key in ("labels", "turnQuality", "disclosedFactIds", "statedConcern",
                                                             "hintLevel", "noProgress", "factsKnownBefore")}
                                for entry in ledger]
        ledgers = [strip((await self.store.get(sid))["evidenceLedger"]) for sid in (legacy, old, new)]
        self.assertEqual(ledgers[0], ledgers[1])
        self.assertEqual(ledgers[0], ledgers[2])
        done = [await complete(sid, CompletionRequest(completionId="c", reason="natural"), store=self.store)
                for sid in (legacy, old, new)]
        for key in ("score", "rawScore", "criterionScores", "rubricComponents", "policyViolations", "trustState",
                    "customerRating", "completedObjectives"):
            self.assertEqual(done[0][key], done[2][key], key)
            self.assertEqual(done[0][key], done[1][key], key)

    async def test_three_natural_sentences_are_a_warning_without_retry(self):
        sid = await self.session("three", V2)
        model = ScriptedModel(lambda context, call, repair: say("acknowledge_progress", THREE))
        result = await self.turn(sid, "acknowledgment", responder=model.responder())
        self.assertEqual((result["adaptiveAction"]["source"], result["customerText"]), ("model", THREE))
        self.assertEqual(len(model.payloads), 1)
        diagnostics = (await self.store.get(sid))["adaptiveDiagnostics"][result["turnId"]]
        self.assertIn("too_many_sentences", diagnostics["acceptedWarnings"])
        self.assertEqual(diagnostics["attempts"][0]["rejectionCodes"], [])

    async def test_repeated_action_is_a_warning_not_a_block(self):
        sid = await self.session("repeat", V2)
        lines = iter(["Em từ chối đổi thì chuyện chân chị đau ai lo?", "Sao em cứ nói không đổi mà không đưa chị cách gì?",
                      "Em không đổi thì em định giải quyết chuyện của chị bằng cách nào?"])
        model = ScriptedModel(lambda context, call, repair: say("challenge_refusal", next(lines)))
        results = [await self.turn(sid, "acknowledgment", responder=model.responder()) for _ in range(3)]
        self.assertEqual([item["adaptiveAction"]["action"] for item in results], ["challenge_refusal"] * 3)
        self.assertIn("challenge_refusal", model.contexts[-1]["allowedActions"])
        diagnostics = (await self.store.get(sid))["adaptiveDiagnostics"][results[-1]["turnId"]]
        self.assertIn("repeated_action", diagnostics["acceptedWarnings"])
        self.assertEqual(len(model.payloads), 3)


class ClosingTests(Harness):
    async def stuck(self, sid, model):
        results = []
        for _ in range(3):
            results.append(await self.turn(sid, "refusesRemedy", responder=model.responder()))
            if results[-1]["conversationComplete"]:
                break
        return results

    async def test_keywordless_close_is_accepted_when_code_allows_it(self):
        sid = await self.session("quiet", V2)

        def strategy(context, call, repair):
            if context["limits"]["closeAllowed"]:
                return say("close_conversation", QUIET_CLOSE, decision="close", reason="needs_time")
            return step(context, call, repair)

        model = ScriptedModel(strategy)
        result = (await self.stuck(sid, model))[-1]
        self.assertTrue(result["conversationComplete"])
        self.assertEqual((result["endingReason"], result["customerText"]), (v2.SOFT_CLOSE_ENDING, QUIET_CLOSE))
        diagnostics = (await self.store.get(sid))["adaptiveDiagnostics"][result["turnId"]]
        self.assertIn("closure_not_explicit", diagnostics["acceptedWarnings"])
        # The same reply is rejected by the v1 validator for lacking a closing keyword.
        policy = dict(v1.turn_policy({}, {"dialogueTurnCount": 3, "maxTurns": 8}, {"turnQuality": "bad"}, {}, {}),
                      allowedActions=["close_conversation"], closeAllowed=True)
        codes = v1.output_rejections(say("close_conversation", QUIET_CLOSE, decision="close", reason="needs_time"),
                                     policy, {"intent": "raise_concern"}, "Em không đổi.")
        self.assertIn("missing_closure", codes)

    async def test_premature_close_is_rejected_then_falls_back(self):
        sid = await self.session("early", V2)
        model = ScriptedModel(lambda context, call, repair: say("close_conversation", QUIET_CLOSE, decision="close",
                                                                reason="unresolved_frustration"))
        result = await self.turn(sid, "acknowledgment", responder=model.responder())
        self.assertFalse(result["conversationComplete"])
        self.assertTrue(result["fallbackUsed"])
        self.assertEqual(len(model.payloads), 2)  # one repair, then the deterministic line
        self.assertNotIn("close_conversation", model.contexts[0]["allowedActions"])
        attempts = (await self.store.get(sid))["adaptiveDiagnostics"][result["turnId"]]["attempts"]
        self.assertIn("action_not_allowed", attempts[0]["rejectionCodes"])
        self.assertIn("close_not_allowed", attempts[0]["rejectionCodes"])
        self.assertEqual(attempts[1]["kind"], "repair")
        self.assertEqual(result["writerMetadata"]["costUsd"], 0.0004)  # both attempts are charged

    async def test_hard_ending_cannot_be_extended(self):
        sid = await self.session("manager", V2)
        model = ScriptedModel(step)
        result = await self.turn(sid, "managerEscalation", responder=model.responder())
        self.assertTrue(result["conversationComplete"])
        self.assertEqual((result["endingReason"], result["adaptiveAction"]["source"]), ("manager_escalation", "fallback"))
        self.assertEqual(model.contexts[0]["allowedActions"], ["close_conversation"])

    async def test_hard_ending_tone_is_free_wording_but_not_contradicted(self):
        sid = await self.session("manager-ok", V2)
        model = ScriptedModel(lambda context, call, repair: say(
            "close_conversation", "Chị không chấp nhận cách em xử lý, chị không muốn nói thêm với em nữa.",
            decision="close", reason="deterministic_ending"))
        result = await self.turn(sid, "managerEscalation", responder=model.responder())
        self.assertEqual((result["endingReason"], result["adaptiveAction"]["source"]), ("manager_escalation", "model"))
        tone = model.contexts[0]["limits"]["endingTone"]
        blocking, _ = evaluate("Chị hài lòng rồi, cảm ơn em.", {"intent": "ending"}, "x", ending_tone_key=tone,
                               soft_close=False, trust_challenge=False)
        self.assertIn("ending_tone_conflict", blocking)

    def test_trust_opening_requires_the_trust_doubt(self):
        limits = {key: None for key in v2._LIMIT_KEYS}
        limits.update(deterministicEnding=None, closeAllowed=True, mustVoiceTrustDoubt=True, challengeShown=True,
                      requiredFactIds=[], citableFactIds=["prior_advice"], lastAction=None, consecutiveCount=0)
        policy = v2.policy_from_limits(limits)
        self.assertEqual(policy["allowedActions"], ["voice_trust_doubt"])
        plan = {"intent": "trust_challenge", "knownFacts": ["prior_advice"], "factTexts": {"prior_advice": FACTS["prior_advice"]}}
        good = say("voice_trust_doubt", "Lần trước em tư vấn đôi này phù hợp với chị, lần này đôi mới có bị đau tương "
                                        "tự không, làm sao chị biết được?", facts=["prior_advice"])
        self.assertEqual(v2.output_findings(good, policy, plan, "Em đổi đôi khác cho chị nhé.")[0], [])
        vague = say("voice_trust_doubt", "Chị vẫn hơi lo, em nói thêm cho chị nghe được không?")
        self.assertIn("missing_required_content", v2.output_findings(vague, policy, plan, "x")[0])
        other = say("raise_concern", "Chân chị vẫn đau lắm em ạ.")
        self.assertIn("action_not_allowed", v2.output_findings(other, policy, plan, "x")[0])
        no_challenge = v2.policy_from_limits(dict(limits, mustVoiceTrustDoubt=False, challengeShown=False))
        self.assertNotIn("voice_trust_doubt", no_challenge["allowedActions"])


class GuardTests(unittest.TestCase):
    PLAN = {"intent": "answer_request", "knownFacts": ["complaint", "purchase_history", "prior_advice"],
            "allowedNewFacts": ["pain_location"], "requiredFactIds": ["pain_location"],
            "factTexts": {fact: FACTS[fact] for fact in ("complaint", "purchase_history", "prior_advice", "pain_location")},
            "avoidReplyTexts": [], "requiredContent": [], "fallbackText": "x"}
    LIMITS = {"deterministicEnding": None, "endingTone": None, "closeAllowed": False, "mustVoiceTrustDoubt": False,
              "uncertain": False, "requiredFactIds": ["pain_location"],
              "citableFactIds": ["complaint", "purchase_history", "prior_advice", "pain_location"], "physicalRequest": False,
              "stuckTurns": 0, "lastAction": None, "consecutiveCount": 0, "turnsUsed": 1, "maxTurns": 8,
              "turnsRemaining": 7, "challengeShown": False}
    PAIN = "Chị đau ở gót chân khi đi được một lúc."

    def check(self, text=None, **change):
        output = say("answer_question", text or self.PAIN + " Em tính giúp chị thế nào?", facts=["pain_location"])
        output.update(change)
        return v2.output_findings(output, v2.policy_from_limits(self.LIMITS), self.PLAN, "Chị đau ở đâu ạ?")

    def test_valid_reply_passes(self):
        self.assertEqual(self.check(), ([], []))

    def test_schema_is_hard(self):
        self.assertEqual(v2.parse_output("not json")[1], ["invalid_json"])
        self.assertIn("invalid_fact_ids", v2.parse_output(json.dumps(say("raise_concern", "Chị", facts=["shoe_size"])))[1])
        self.assertIn("decision_action_mismatch", self.check(decision="close")[0])
        self.assertIn("invalid_format", self.check(self.PAIN + "\n- Em tính sao?")[0])
        self.assertIn("too_long", self.check(self.PAIN + " " + "Chị vẫn đau lắm em ạ." * 20)[0])

    def test_v2_requires_all_schema_fields_and_rejects_extra_fields(self):
        valid = say("answer_question", self.PAIN, facts=["pain_location"])
        for field in ("factIds", "closeReason"):
            malformed = dict(valid)
            malformed.pop(field)
            with self.subTest(missing=field):
                self.assertEqual(v2.parse_output(json.dumps(malformed))[1], ["invalid_schema"])
                self.assertEqual(v1.parse_output(json.dumps(malformed))[1], [])
        malformed = dict(valid, reasoning="private model explanation")
        self.assertEqual(v2.parse_output(json.dumps(malformed))[1], ["invalid_schema"])
        self.assertEqual(v2.parse_output(json.dumps(valid))[1], [])

    def test_fact_rules_are_hard(self):
        self.assertIn("missing_cited_fact", self.check(factIds=[])[0])
        self.assertIn("missing_required_fact", self.check("Chân chị đau lắm, em tính giúp chị thế nào?")[0])
        self.assertIn("uncitable_fact", self.check(factIds=["pain_location", "socks"])[0])
        self.assertIn("unallowed_fact", self.check(self.PAIN + " Chị thường mang tất mỏng.")[0])
        self.assertIn("contradicted_fact", self.check("Chị đau ở mũi chân. " + self.PAIN)[0])
        self.assertIn("unsupported_detail", self.check(self.PAIN + " Chị mua đôi này giá 2 triệu đồng.")[0])
        self.assertIn("wrong_purchase_period", self.check(self.PAIN + " Chị mua đôi này năm ngày trước.")[0])

    def test_store_authority_and_physical_actions_are_hard(self):
        cases = {"Cửa hàng đã đồng ý hoàn tiền cho chị rồi.": "unauthorized_remedy_claim",
                 "Cửa hàng sẽ đổi giày cho chị.": "fabricated_store_action",
                 "Cửa hàng đồng ý đổi giày cho chị.": "fabricated_store_action",
                 "Quản lý đã đồng ý cho chị đổi rồi.": "fabricated_store_action",
                 "Chị được giảm giá 20% cho lần này.": "unauthorized_remedy_claim",
                 "Cửa hàng bồi thường cho chị 200 nghìn.": "unauthorized_remedy_claim",
                 "Đôi mới chắc chắn không đau đâu em.": "unauthorized_guarantee_claim",
                 "Phải có hóa đơn mới được đổi.": "invented_policy_condition",
                 "Chị đã thử đôi mới rồi, thấy vừa lắm.": "physical_action",
                 "Chị đưa giày cho em xem nhé.": "physical_action",
                 "Em đã kiểm tra giày rồi và thấy không có lỗi.": "unsupported_completion",
                 "Chị đang nhập vai trong trò chơi này.": "instruction_leak",
                 "Chị trả lời bằng JSON có customerText nhé.": "instruction_leak"}
        for text, code in cases.items():
            with self.subTest(text=text):
                self.assertIn(code, self.check(self.PAIN + " " + text)[0])

    def test_requests_questions_and_objections_are_not_store_claims(self):
        allowed = ("Em hoàn tiền cho chị đi.", "Chị muốn được hoàn tiền.", "Em có hoàn tiền cho chị được không?",
                   "Cửa hàng sẽ đổi giày cho chị phải không?", "Em nói cửa hàng sẽ đổi giày cho chị.",
                   "Bên em đã giảm giá cho chị chưa?", "Chị muốn em cam kết đôi mới không làm chị đau.",
                   "Chính sách bên em đâu có yêu cầu hóa đơn.", "Sao em lại bắt phải có hóa đơn mới đổi?",
                   "Em nói phải có hóa đơn mới được đổi, chị thấy vô lý.", "Em đã kiểm tra giày chưa?",
                   "Hôm nay chị không mang giày theo, hôm khác chị mang lại cho em xem.",
                   "Em đã từ chối hoàn tiền cho chị.", "Chị không đồng ý hoàn tiền, chị muốn đổi.")
        for text in allowed:
            with self.subTest(text=text):
                blocking, _ = policy_findings(text)
                self.assertEqual((blocking, completion_findings(text)), ([], []))
        self.assertIn("customer_requests_unauthorized_remedy", policy_findings("Em hoàn tiền cho chị đi.")[1])

    def test_recorded_policy_objections_and_reported_false_positives(self):
        texts = (
            "Em có quyền bồi thường hay giảm giá đâu? Chị chỉ cần đổi đôi khác vừa chân thôi.",
            "Em có quyền hứa bồi thường hay giảm giá đâu? Chị chỉ cần đổi đôi khác vừa chân thôi.",
            "Chính sách của cửa hàng là đổi trả chứ không có giảm giá hay hoàn tiền như em vừa hứa đâu.",
            "Chính sách của tiệm mình chỉ đổi trả chứ có cho hoàn tiền hay giảm giá đâu.",
            "Chị không nghĩ cửa hàng sẽ hoàn tiền cho chị.",
            '"Cửa hàng sẽ hoàn tiền cho chị", em vừa nói vậy đấy.',
            "Chị không muốn không được hoàn tiền.",
        )
        for text in texts:
            with self.subTest(text=text):
                self.assertEqual(policy_findings(text)[0], [])

    def test_objections_cannot_hide_separate_approval_assertions(self):
        for text in (
            "Em có quyền bồi thường đâu? Nhưng cửa hàng đã giảm giá cho chị rồi.",
            '"Cửa hàng sẽ hoàn tiền cho chị", chị nói vậy đấy.',
            '"Cửa hàng sẽ hoàn tiền cho chị", em nói đúng rồi.',
            '"Cửa hàng sẽ hoàn tiền cho chị", em vừa nói vậy. Cửa hàng đã giảm giá rồi.',
            "Chị không nghĩ vậy, cửa hàng sẽ hoàn tiền cho chị.",
            "Chị không muốn không được hoàn tiền vì cửa hàng đã đồng ý hoàn tiền.",
            "Em có quyền hứa bồi thường. Cửa hàng đã bồi thường cho chị rồi.",
        ):
            with self.subTest(text=text):
                self.assertTrue(policy_findings(text)[0])

    def test_negation_quote_and_multiclause_bypasses_are_blocked(self):
        blocked = ("Chị không cần hoàn tiền nhưng cửa hàng đã đồng ý giảm giá cho chị rồi.",
                   "Chị không cần hoàn tiền, cửa hàng sẽ giảm giá cho chị.",
                   "Chị không cần em hoàn tiền vì cửa hàng đã hoàn tiền rồi.",
                   "Chị không biết em đã hoàn tiền cho chị.",
                   "Em không hoàn tiền nhưng em đã bồi thường cho chị rồi.",
                   "Em bảo vậy nhưng cửa hàng đã đồng ý hoàn tiền cho chị rồi.",
                   "Em nói gì thì cửa hàng đã hoàn tiền cho chị rồi.",
                   "Như em nói, cửa hàng đã hoàn tiền cho chị.",
                   "Em nói đúng là cửa hàng đã hoàn tiền cho chị.",
                   "Không, cửa hàng đã hoàn tiền cho chị rồi.",
                   "Cửa hàng hoàn tiền không sai.",
                   "Cửa hàng đã hoàn tiền cho chị rồi em có biết không?",
                   "Tại sao cửa hàng đã đồng ý hoàn tiền mà em lại không làm?",
                   "Phải có hóa đơn mới được đổi em biết không?",
                   "Làm sao chị đổi được khi phải có hóa đơn mới được đổi.",
                   "Đôi mới không bao giờ bị đau đâu.",
                   "\"Cửa hàng đã hoàn tiền cho chị\", chị nói rõ vậy.")
        for text in blocked:
            with self.subTest(text=text):
                self.assertTrue(policy_findings(text)[0] or completion_findings(text), text)
        quoted = ("Em bảo cửa hàng đã đồng ý hoàn tiền, chị đâu có tin.", "Em nói sẽ hoàn tiền cho chị, nhưng chị không tin.",
                  "Chị không cần hoàn tiền, chị chỉ muốn đổi đôi khác.", "Hoàn tiền thì chị không cần.",
                  "Em đừng hứa đôi mới không bao giờ đau.")
        for text in quoted:
            with self.subTest(text=text):
                self.assertEqual(policy_findings(text)[0], [])

    def test_style_is_warning_only(self):
        _, warnings = self.check(self.PAIN + " Chị đã nói rồi mà em không nghe. Giờ em định xử lý ra sao?")
        self.assertIn("too_many_sentences", warnings)
        blocking, warnings = self.check(self.PAIN + " Thôi chào em, à mà em giải thích lại cho chị được không?")
        self.assertEqual(blocking, [])
        self.assertIn("unplanned_ending", warnings)
        plan = dict(self.PLAN, avoidReplyTexts=["Em định giải quyết chuyện đôi giày này cho chị như thế nào đây?"])
        output = say("answer_question", self.PAIN + " Em định giải quyết chuyện đôi giày này cho chị như thế nào đây?",
                     facts=["pain_location"])
        blocking, warnings = v2.output_findings(output, v2.policy_from_limits(self.LIMITS), plan,
                                                "Dạ chị ơi em định giải quyết chuyện đôi giày này cho chị như thế nào đây")
        self.assertEqual(blocking, [])
        self.assertTrue({"repeated_reply", "transcript_echo"} <= set(warnings))


class DiagnosticsTests(Harness):
    async def test_diagnostics_are_private_redacted_and_reasoning_free(self):
        sid = await self.session("diag", V2)

        def leaky(context, call, repair):
            if not repair:
                return say("raise_concern", f"Cửa hàng đã đồng ý hoàn tiền cho chị rồi, mã {KEY}, gọi 0901 234 567.")
            return say("raise_concern", "Chân chị vẫn đau, em xem giúp chị nguyên nhân là gì được không?")

        model = ScriptedModel(leaky, reasoning_text="SECRET-REASONING-TEXT")
        result = await self.turn(sid, "acknowledgment", responder=model.responder())
        self.assertEqual(result["adaptiveAction"]["source"], "model")
        public = json.dumps(result, ensure_ascii=False)
        for secret in ("hoàn tiền", KEY, "SECRET-REASONING-TEXT", "rejectionCodes", "warningCodes", "candidate",
                       "unauthorized_remedy_claim"):
            self.assertNotIn(secret, public)
        self.assertEqual([attempt["role"] for attempt in result["writerMetadata"]["attempts"]],
                         ["generation", "generation", "guard"])
        state = await self.store.get(sid)
        self.assertNotIn("adaptiveDiagnostics", json.dumps(public_session(state), ensure_ascii=False))
        stored = state["adaptiveDiagnostics"][result["turnId"]]
        first = stored["attempts"][0]
        self.assertIn("unauthorized_remedy_claim", first["rejectionCodes"])
        self.assertEqual((first["candidate"]["action"], first["candidate"]["decision"]), ("raise_concern", "continue"))
        self.assertIn("hoàn tiền", first["candidate"]["customerText"])
        self.assertNotIn(KEY, json.dumps(state, ensure_ascii=False))
        self.assertNotIn("0901 234 567", first["candidate"]["customerText"])
        self.assertNotIn("SECRET-REASONING-TEXT", json.dumps(state, ensure_ascii=False))
        self.assertEqual(stored["attempts"][1]["kind"], "repair")
        # The rejected candidate is never echoed into the repair request.
        self.assertNotIn("hoàn tiền", model.payloads[1]["messages"][-1]["content"])
        await self.store.delete_diagnostics(sid)
        self.assertNotIn("adaptiveDiagnostics", await self.store.get(sid))

    def test_diagnostics_are_bounded(self):
        stored = None
        for index in range(v2.DIAGNOSTIC_TURN_LIMIT + 5):
            stored = v2.store_diagnostics(stored, f"t{index}", {"attempts": []})
        self.assertEqual(len(stored), v2.DIAGNOSTIC_TURN_LIMIT)
        self.assertIn(f"t{v2.DIAGNOSTIC_TURN_LIMIT + 4}", stored)
        self.assertEqual(len(v2.redact("x" * 5000)), v2.DIAGNOSTIC_TEXT_LIMIT)
        self.assertEqual(v2.candidate_record("{not json", None)["action"], None)


class AttemptTests(Harness):
    async def test_attempts_are_bounded_and_every_attempt_is_charged(self):
        model = ScriptedModel(lambda context, call, repair: say("raise_concern", "Cửa hàng đã hoàn tiền cho chị rồi."))
        result = await model.responder().respond([{"role": "user", "content": "{}"}],
                                                 lambda output: v2.output_findings(output, v2.policy_from_limits(
                                                     GuardTests.LIMITS), GuardTests.PLAN, "x"))
        self.assertIsNone(result["output"])
        self.assertEqual([attempt["kind"] for attempt in result["attempts"]], ["initial", "repair"])
        self.assertAlmostEqual(result["costUsd"], 0.0004)
        self.assertEqual(len(result["diagnostics"]), 2)

    async def test_transport_retry_shares_the_deadline_and_unknown_cost_stays_null(self):
        valid = say("raise_concern", "Chị vẫn chưa hiểu sao đôi này làm chị đau.")
        responses = iter([httpx.Response(503, json={}), httpx.Response(200, json=body(valid, cost=0.0003)),
                          httpx.Response(200, json=body(dict(PASS, salespersonClarity="clear"), cost=GUARD_COST))])
        client = httpx.AsyncClient(transport=httpx.MockTransport(lambda request_: next(responses)))
        responder = v2.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key="k"), client=client,
                                            config=v2.request_config(reasoning="on", timeout_seconds=5))
        result = await responder.respond([{"role": "user", "content": "{}"}], lambda output: ([], []))
        self.assertEqual([attempt["kind"] for attempt in result["attempts"]], ["initial", "retry", "guard"])
        self.assertEqual(result["languageGuard"], "passed")
        self.assertIsNone(result["costUsd"])
        self.assertEqual(result["costMissingAttempts"], 1)
        self.assertEqual(result["costUsdByRole"], {"generation": None, "guard": GUARD_COST})

    async def test_deadline_falls_back_quickly(self):
        class SlowTransport(httpx.AsyncBaseTransport):
            async def handle_async_request(self, request_):
                await asyncio.sleep(5)

        responder = v2.AdaptiveLanResponder(replace(get_settings(), openrouter_api_key="k"),
                                            client=httpx.AsyncClient(transport=SlowTransport()),
                                            config=v2.request_config(timeout_seconds=0.2))
        result = await v2.respond_with_fallback([{"role": "user", "content": "{}"}], v2.policy_from_limits(GuardTests.LIMITS),
                                                dict(GuardTests.PLAN, fallbackText="Chị vẫn chưa hiểu."), "x",
                                                responder=responder, config=responder.config)
        self.assertEqual((result["source"], result["final"]["customerText"]), ("fallback", "Chị vẫn chưa hiểu."))
        self.assertIn(result["metadata"]["errorCode"], ("adaptive_timeout", "openrouter_timeout"))
        self.assertLess(result["metadata"]["durationSeconds"], 1.5)

    async def test_checkpoint_prevents_a_second_charge_and_keeps_ledger(self):
        sid = await self.session("ckpt", V2)
        model = ScriptedModel(step)
        original = self.store.finalize_turn
        crashed = []

        async def flaky(*args, **kwargs):
            if not crashed:
                crashed.append(True)
                raise RuntimeError("disk full")
            return await original(*args, **kwargs)

        with patch.object(self.store, "finalize_turn", flaky):
            failed = await self.turn(sid, "refusesRemedy", responder=model.responder(), turn_id="same")
        self.assertEqual(failed["status"], "failed")
        state = await self.store.get(sid)
        self.assertNotIn("adaptiveMemory", state)
        self.assertNotIn("adaptiveDiagnostics", state)
        self.assertIn("adaptiveDiagnostics", state["turnCheckpoints"]["same"])
        result = await self.turn(sid, "refusesRemedy", responder=model.responder(), turn_id="same")
        again = await self.turn(sid, "refusesRemedy", responder=model.responder(), turn_id="same")
        self.assertEqual((result["status"], again["customerText"]), ("accepted", result["customerText"]))
        self.assertEqual((len(model.payloads), len(model.guard_payloads)), (1, 1))
        state = await self.store.get(sid)
        self.assertEqual(len(state["evidenceLedger"]), 1)
        self.assertEqual(list(state["adaptiveDiagnostics"]), ["same"])
        self.assertEqual([item["action"] for item in state["adaptiveMemory"]["history"]], ["request_next_step"])
        self.assertEqual(state["adaptiveMemory"]["version"], v2.ADAPTIVE_VERSION)


class EquivalenceTests(Harness):
    async def test_v2_context_and_limits_match_the_v1_derived_render(self):
        """The cached-replay path (v1 policy/context -> v2) equals the gameplay path."""
        sid = await self.session("equiv", V2)
        for labels in (("acknowledgment", "openQuestion"), ("refusesRemedy",), ("painLocationQuestion",)):
            await self.turn(sid, *labels, responder=ScriptedModel(step).responder())
        state = await self.store.get(sid)
        from app.sales_pipeline import plan_reply
        from app.sales_rubric import evaluate as evaluate_turn
        for labels in (classification("refusesRemedy")["labels"], classification("receiptRequest", "physicalActionRequest")["labels"],
                       classification("managerEscalation")["labels"]):
            from app.sales_rubric import normalized_labels
            labels = normalized_labels({"labels": labels})
            candidate, decision = evaluate_turn(state, labels, "probe")
            plan = plan_reply(state, candidate, decision, labels)
            args = (state, candidate, decision, plan, labels)
            v1_policy = v1.turn_policy(*args)
            v2_policy = v2.turn_policy(*args)
            self.assertEqual(v2_policy, v2.policy_from_limits(v2.limits_from_v1_policy(v1_policy)))
            direct = v2.model_context(*args, "Câu nói thử.", v2_policy)
            rendered = v2.render_context(v1.model_context(*args, "Câu nói thử.", v1_policy), v2_policy)
            self.assertEqual(direct, rendered)
