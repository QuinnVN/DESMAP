using System.Collections.Generic;
using NCKH.VR.Doctor;
using NUnit.Framework;

namespace NCKH.VR.Doctor.Tests
{
    public sealed partial class DoctorGameRulesTests
    {
        [Test]
        public void CorrectEssentialNotesScoreOncePerUniqueId()
        {
            DoctorInfoCard[] cards =
            {
                new DoctorInfoCard("time", "Started yesterday", DoctorInfoCategory.Time, true),
                new DoctorInfoCard("time", "Duplicate wording", DoctorInfoCategory.Time, true),
                new DoctorInfoCard("symptom", "Dry cough", DoctorInfoCategory.Symptoms, true)
            };
            Dictionary<string, DoctorInfoCategory?> placements = new Dictionary<string, DoctorInfoCategory?>
            {
                ["time"] = DoctorInfoCategory.Time,
                ["symptom"] = DoctorInfoCategory.Symptoms
            };

            Assert.That(DoctorGameRules.CountCorrectNotes(cards, placements), Is.EqualTo(2));
        }

        [Test]
        public void DistractorsIncorrectCategoriesAndUnfiledCardsDoNotScore()
        {
            DoctorInfoCard[] cards =
            {
                new DoctorInfoCard("noise", "Bought an umbrella", DoctorInfoCategory.Time, false),
                new DoctorInfoCard("travel", "Recent trip", DoctorInfoCategory.TravelHistory, true),
                new DoctorInfoCard("history", "History of asthma", DoctorInfoCategory.MedicalHistory, true)
            };
            Dictionary<string, DoctorInfoCategory?> placements = new Dictionary<string, DoctorInfoCategory?>
            {
                ["noise"] = DoctorInfoCategory.Time,
                ["travel"] = DoctorInfoCategory.Symptoms,
                ["history"] = null
            };

            Assert.That(DoctorGameRules.CountCorrectNotes(cards, placements), Is.Zero);
        }

        [Test]
        public void QuestionTrackerPreservesAuthoredOrderAndPreventsDuplicateIds()
        {
            DoctorQuestionPhase first = Phase("first");
            DoctorQuestionPhase duplicate = Phase("first");
            DoctorQuestionPhase second = Phase("second");
            DoctorQuestionTracker tracker = new DoctorQuestionTracker();
            List<DoctorQuestionPhase> remaining = new List<DoctorQuestionPhase>();

            tracker.GetRemaining(new[] { first, duplicate, second }, remaining);
            Assert.That(remaining, Is.EqualTo(new[] { first, second }));
            Assert.That(tracker.TryAsk("first"), Is.True);
            Assert.That(tracker.TryAsk("first"), Is.False);
            tracker.GetRemaining(new[] { first, second }, remaining);
            Assert.That(remaining, Is.EqualTo(new[] { second }));
        }

        [Test]
        public void SelectionCyclingWrapsInBothDirections()
        {
            Assert.That(DoctorGameRules.CycleSelection(0, -1, 3), Is.EqualTo(2));
            Assert.That(DoctorGameRules.CycleSelection(2, 1, 3), Is.Zero);
            Assert.That(DoctorGameRules.CycleSelection(0, 1, 0), Is.EqualTo(-1));
        }

        [TestCase(1, 40f)]
        [TestCase(2, 40f)]
        [TestCase(4, 22f)]
        [TestCase(6, 12f)]
        public void QuestionItemsUsePreferredHeightOrShrinkToFit(int itemCount, float expectedHeight)
        {
            float itemHeight = DoctorGameRules.CalculateQuestionItemHeight(
                questionAreaHeight: 124f,
                itemCount: itemCount,
                verticalPadding: 6f,
                spacing: 8f,
                preferredHeight: 40f);

            Assert.That(itemHeight, Is.EqualTo(expectedHeight).Within(0.001f));
            float occupiedHeight = itemHeight * itemCount + 12f + 8f * (itemCount - 1);
            Assert.That(occupiedHeight, Is.LessThanOrEqualTo(124f));
        }

        [TestCase(0f, 2, 40f)]
        [TestCase(100f, 0, 40f)]
        [TestCase(100f, 2, 0f)]
        [TestCase(-10f, 2, 40f)]
        public void QuestionItemHeightRejectsInvalidDimensions(float areaHeight, int count, float preferredHeight)
        {
            Assert.That(
                DoctorGameRules.CalculateQuestionItemHeight(areaHeight, count, 6f, 8f, preferredHeight),
                Is.Zero);
        }

        private static DoctorQuestionPhase Phase(string id)
        {
            return new DoctorQuestionPhase(id, $"Question {id}?", new DoctorDialogue("Response", null, null));
        }
    }
}
