"""Synthetic regressions for ambiguous speech, money promises and receipt handling."""
import copy
import json
import unittest
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

from app import sales_adaptive_v2 as v2
from app.sales_adaptive_guard import evaluate as guard
from app.sales_arbitration import select_arbitration_labels
from app.sales_pipeline import complete
from app.sales_returning_customer import CompletionRequest
from app.sales_openrouter import FACTS
from app.tests.test_sales_adaptive_v2 import Harness, ScriptedModel, V2, say, step
from app.tests.test_sales_pipeline_api import classification


class ReliabilityTests(Harness):
    async def test_garbled_speech_cannot_spend_budget_by_choosing_frustration(self):
        sid = await self.session("garbled-frustration", V2)
        model = ScriptedModel(lambda context, call, repair:
            say("express_frustration", "Em nói gì vậy? Chị không hiểu nổi, em nói lại rõ xem."),
            verifier=lambda data, call: {"verdict": "pass", "codes": [], "salespersonClarity": "unclear"})
        for index in range(5):
            result = await self.turn(sid, responder=model.responder(),
                utterance="Tăng pha bảy nhánh mềm, gót cái mai xanh, ờ chữ chạy chín.")
            self.assertTrue(result.get("clarificationRequested"))
            self.assertEqual(result["dialogueTurnCount"], 0)
        self.assertTrue(result["conversationComplete"])
        self.assertEqual(result["terminalError"]["code"], "speech_unintelligible")
        self.assertNotIn("score", result)
        self.assertEqual(len((await self.store.get(sid))["evidenceLedger"]), 5)
        with patch("app.sales_review.resolve_review", side_effect=AssertionError("must not review")):
            done = await complete(sid, CompletionRequest(completionId="done"), store=self.store)
        self.assertEqual(done["assessmentStatus"], "failed")
        self.assertNotIn("score", done)

    async def test_unclear_input_resolves_spurious_uncertain_labels_without_review(self):
        sid = await self.session("unclear-ambiguous", V2)
        model = ScriptedModel(lambda context, call, repair: say("express_frustration", "Chị chưa hiểu em muốn nói gì."),
            verifier=lambda data, call: {"verdict": "pass", "codes": [], "salespersonClarity": "unclear"})
        result = await self.turn(sid, values={"openQuestion": .5}, responder=model.responder())
        self.assertTrue(result["clarificationRequested"])
        self.assertEqual(result["evaluableTurnCount"], 0)
        state = await self.store.get(sid)
        self.assertFalse(state.get("assessmentReviewItems"))
        self.assertEqual(state["evidenceLedger"][-1]["labels"]["openQuestion"]["status"], "uncertain")

    async def test_unclear_verdict_cannot_discard_a_clear_violation(self):
        sid = await self.session("unclear-with-violation", V2)
        model = ScriptedModel(step, verifier=lambda data, call:
            {"verdict": "pass", "codes": [], "salespersonClarity": "unclear"})
        result = await self.turn(sid, "unauthorizedRefund", responder=model.responder())
        self.assertEqual(result["dialogueTurnCount"], 1)
        self.assertIn("unauthorized_refund", result["policyViolations"])
        self.assertFalse(result.get("clarificationRequested"))

    async def test_meaningful_off_topic_speech_and_mixed_speech_still_count(self):
        for clarity in ("clear", "mixed"):
            sid = await self.session("meaningful-" + clarity, V2)
            model = ScriptedModel(step, verifier=lambda data, call:
                {"verdict": "pass", "codes": [], "salespersonClarity": clarity})
            result = await self.turn(sid, responder=model.responder(), utterance="Em muốn kể chuyện ngoài lề một chút.")
            self.assertEqual(result["dialogueTurnCount"], 1)
            self.assertFalse(result.get("clarificationRequested"))

    async def test_previous_revision_keeps_prompt_guard_and_counting(self):
        sid = await self.session("frozen-clarification", V2)
        state = await self.store.get(sid)
        state["adaptiveDialogue"].update(dialogueRevision=v2.CLARIFICATION_REVISION,
            promptSha256=v2.sha256_text(v2.CLARIFICATION_SYSTEM_PROMPT), languageGuard=v2.CLARIFICATION_GUARD_REQUEST)
        await self.store.save(state)
        self.assertTrue(v2.supported(state))
        model = ScriptedModel(lambda context, call, repair: say("express_frustration", "Chị không hiểu em muốn nói gì."))
        result = await self.turn(sid, responder=model.responder())
        self.assertEqual(result["dialogueTurnCount"], 1)
        self.assertEqual(model.payloads[0]["messages"][0]["content"], v2.CLARIFICATION_SYSTEM_PROMPT)
        self.assertEqual(v2.sha256_text(v2.CLARIFICATION_SYSTEM_PROMPT),
                         "2c7e8e37a6b36f7dd56c6e99dccbcc53243c2f2df76d6d8a852b8178fcdb2d5a")
        self.assertEqual(v2.CLARIFICATION_GUARD_REQUEST["promptSha256"],
                         "799ee5083eed9b7a44661bae4e762549332fbd0a772184fd54c07d57379359fd")

    async def test_confident_refund_and_compensation_are_arbitrated_together(self):
        sid = await self.session("money-conflict", V2)
        selected = select_arbitration_labels(await self.store.get(sid),
            classification("unauthorizedRefund", "unauthorizedCompensation")["labels"])
        self.assertIn("unauthorizedRefund", selected)
        self.assertIn("unauthorizedCompensation", selected)

    async def test_verified_compensation_is_one_penalty_and_failure_withholds_both(self):
        for resolved in (True, False):
            sid = await self.session("money-" + str(resolved), V2)
            state = await self.store.get(sid)
            state["lunaArbitrationEnabled"] = True
            await self.store.save(state)
            labels = {"unauthorizedRefund": {"status": "false", "evidence": ""},
                      "unauthorizedCompensation": {"status": "true", "evidence": "Em bù chị hai trăm nghìn"}} if resolved else {}
            arb = SimpleNamespace(adjudicate=AsyncMock(return_value={"labels": labels, "metadata": {}}))
            with patch("app.sales_arbitration.LunaSalesArbitrator", return_value=arb):
                result = await self.turn(sid, "acknowledgment", "unauthorizedRefund", "unauthorizedCompensation",
                    responder=ScriptedModel(step).responder(), utterance="Em bù chị hai trăm nghìn")
            self.assertEqual(result["policyViolations"], ["unauthorized_compensation"] if resolved else [])
            if not resolved:
                self.assertTrue(result["assessmentUncertain"])
            self.assertEqual(arb.adjudicate.await_count, 1)

    async def test_receipt_question_cannot_finish_before_customer_answers_objection(self):
        sid = await self.session("receipt-pending", V2)
        model = ScriptedModel(step)
        await self.turn(sid, "acknowledgment", "openQuestion", "painLocationQuestion", responder=model.responder())
        await self.turn(sid, "causeStatement", "fitQuestion", "originalSaleResponsibility", responder=model.responder())
        result = await self.turn(sid, "exchangeOffer", "exchangeConditions", "lightweightForWalking",
            "fitOrWalkTrial", "routineMatchExplanation", "receiptRequest", responder=model.responder())
        self.assertFalse(result["conversationComplete"])
        self.assertIn("receipt_status", result["disclosedFactIds"])
        # The player addresses the missing receipt without inventing a rule.
        followup = await self.turn(sid, "exchangeConditions", responder=model.responder(),
            utterance="Không bắt buộc hóa đơn chị ạ, trong bảy ngày và còn nguyên là mình hỗ trợ đổi.")
        self.assertTrue(followup["conversationComplete"])

    async def test_invented_mandatory_receipt_does_not_earn_policy_credit(self):
        sid = await self.session("mandatory-receipt", V2)
        state = await self.store.get(sid)
        state["lunaArbitrationEnabled"] = True
        await self.store.save(state)
        arb = SimpleNamespace(adjudicate=AsyncMock(return_value={"labels": {
            "exchangeConditions": {"status": "false", "evidence": "phải có hóa đơn"}}, "metadata": {}}))
        with patch("app.sales_arbitration.LunaSalesArbitrator", return_value=arb):
            await self.turn(sid, "exchangeOffer", "exchangeConditions", responder=ScriptedModel(step).responder(),
                utterance="Em đổi trong bảy ngày, còn nguyên và phải có hóa đơn.")
        self.assertNotIn("policy", (await self.store.get(sid))["rubricComponents"])


class NaturalFactTests(unittest.TestCase):
    def test_correct_size_paraphrase_does_not_require_exact_words(self):
        plan = {"intent": "answer_request", "knownFacts": [], "allowedNewFacts": ["fit_condition"],
                "requiredFactIds": ["fit_condition"], "requiredContent": [],
                "naturalFactParaphrases": True,
                "factTexts": {"fit_condition": FACTS["fit_condition"]}}
        text = "Chị không thấy chật hay trượt gót gì cả. Đôi này chị mua đúng size và vẫn còn nguyên vẹn."
        self.assertEqual(guard(text, plan, "Chị thấy chật hay bị trượt gót không?",
            ending_tone_key=None, soft_close=False, trust_challenge=False)[0], [])
        for false in ("Chị mua sai size, giày quá chật và vẫn còn nguyên vẹn.",
                      "Chị mua đúng size nhưng đôi này đã bị rách."):
            with self.subTest(false=false):
                self.assertTrue(guard(false, plan, "Chị thấy thế nào?", ending_tone_key=None,
                    soft_close=False, trust_challenge=False)[0])

    def test_new_clarity_schema_is_strict_and_old_schema_stays_supported(self):
        old = {"verdict": "pass", "codes": []}
        self.assertEqual(v2.parse_verdict(json.dumps(old)), old)
        self.assertIsNone(v2.parse_verdict(json.dumps(old), revision=v2.DIALOGUE_REVISION))
        good = dict(old, salespersonClarity="unclear")
        self.assertEqual(v2.parse_verdict(json.dumps(good), revision=v2.DIALOGUE_REVISION), good)
        for bad in (dict(good, explanation="reason"), dict(good, salespersonClarity="maybe")):
            self.assertIsNone(v2.parse_verdict(json.dumps(bad), revision=v2.DIALOGUE_REVISION))
