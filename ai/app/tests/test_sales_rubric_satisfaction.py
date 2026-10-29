import unittest

from app.sales_arbitration import select_arbitration_labels
from app.sales_concerns import MISMATCH_LABEL, score_ledger
from app.sales_openrouter import QUESTION_TEXT

FACTS = ["complaint", "lighter_preference", "walking_routine"]


def entry(turn_id, *true, uncertain=(), false=()):
    labels = {name: {"status": "true"} for name in true}
    labels.update({name: {"status": "uncertain"} for name in uncertain})
    labels.update({name: {"status": "false"} for name in false})
    return {"turnId": turn_id, "labels": labels, "factsKnownBefore": FACTS, "challengeShownBefore": False}


def quick_exchange(*remedy_labels, uncertain=()):
    """The recorded two-turn exchange: cause and remedy, then policy and trial."""
    second = entry("t2", "exchangeOffer", "exchangeConditions", "fitOrWalkTrial", "walkingRecall", *remedy_labels,
                   uncertain=uncertain)
    second["confirmedFactIds"] = ["walking_routine"]
    return [entry("t1", "causeStatement", "lightweightForWalking"), second]


class SatisfactionScoringTests(unittest.TestCase):
    def test_quick_matching_exchange_earns_full_satisfaction_and_efficiency(self):
        scored = score_ledger(quick_exchange())
        self.assertEqual(scored["endingReason"], "objectives_completed")
        self.assertEqual(scored["outcome"]["criterionScores"],
                         {"apologyAndPolicyRemedy": 45, "adaptabilityAndDeescalation": 40})
        self.assertEqual(scored["outcome"]["score"], 85)
        # Without an apology trust is only partially restored.
        self.assertEqual(scored["outcome"]["trustState"], "partially_restored")

    def test_size_remedy_contradicting_fit_is_accepted_with_reservations(self):
        ledger = quick_exchange(MISMATCH_LABEL)
        scored = score_ledger(ledger)
        self.assertTrue(scored["remedyMismatch"])
        self.assertEqual(scored["endingReason"], "objectives_completed")
        self.assertEqual(scored["outcome"]["score"], 80)
        self.assertIn("lightweight", scored["rubricComponents"])

    def test_uncertain_mismatch_costs_nothing(self):
        ledger = quick_exchange(uncertain=(MISMATCH_LABEL,))
        scored = score_ledger(ledger)
        self.assertFalse(scored["remedyMismatch"])
        self.assertEqual(scored["outcome"]["score"], 85)

    def test_a_later_matching_remedy_clears_the_mismatch(self):
        ledger = [entry("t1", "causeStatement", "lightweightForWalking", "exchangeOffer", MISMATCH_LABEL),
                  entry("t2", "exchangeOffer", "exchangeConditions", "fitOrWalkTrial", "lightweightForWalking",
                        false=(MISMATCH_LABEL,))]
        self.assertFalse(score_ledger(ledger)["remedyMismatch"])

    def test_mismatch_blocks_restored_trust(self):
        ledger = quick_exchange(MISMATCH_LABEL)
        ledger[0]["labels"]["acknowledgment"] = {"status": "true"}
        ledger[0]["labels"]["originalSaleResponsibility"] = {"status": "true"}
        outcome = score_ledger(ledger)["outcome"]
        self.assertGreaterEqual(outcome["score"], 70)
        self.assertEqual(outcome["trustState"], "partially_restored")

    def test_efficiency_falls_with_each_turn_after_five(self):
        filler = [entry(f"f{index}", name) for index, name in
                  enumerate(("acknowledgment", "openQuestion", "painLocationQuestion", "painTimingQuestion"))]
        quick = score_ledger(quick_exchange())["outcome"]["criterionScores"]["adaptabilityAndDeescalation"]
        slow = score_ledger(filler + quick_exchange())["outcome"]["criterionScores"]["adaptabilityAndDeescalation"]
        # Six turns: 15 * (8 - 6) // 3 = 10 efficiency points instead of 15.
        self.assertEqual(quick - slow, 5)

    def test_no_satisfaction_or_efficiency_without_an_accepted_remedy(self):
        scored = score_ledger([entry("t1", "acknowledgment", "openQuestion")], ending="stopped_early")
        self.assertEqual(scored["outcome"]["criterionScores"],
                         {"apologyAndPolicyRemedy": 5, "adaptabilityAndDeescalation": 10})


class MismatchArbitrationTests(unittest.TestCase):
    def test_uncertain_mismatch_is_verified_while_the_remedy_is_open(self):
        labels = {name: {"status": "false"} for name in QUESTION_TEXT}
        labels[MISMATCH_LABEL] = {"status": "uncertain"}
        self.assertIn(MISMATCH_LABEL, select_arbitration_labels({"evidenceLedger": []}, labels))


if __name__ == "__main__":
    unittest.main()
