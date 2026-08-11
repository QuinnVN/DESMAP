using System.Collections.Generic;
using NCKH.VR.Doctor;
using NUnit.Framework;

namespace NCKH.VR.Doctor.Tests
{
    public sealed class DoctorGameRulesTests
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
        public void QuestionTrackerAllowsTwoUniqueCategoriesOnly()
        {
            DoctorQuestionTracker tracker = new DoctorQuestionTracker(2);

            Assert.That(tracker.TryAsk(DoctorInfoCategory.Time), Is.True);
            Assert.That(tracker.TryAsk(DoctorInfoCategory.Time), Is.False);
            Assert.That(tracker.TryAsk(DoctorInfoCategory.Symptoms), Is.True);
            Assert.That(tracker.TryAsk(DoctorInfoCategory.TravelHistory), Is.False);
            Assert.That(tracker.QuestionsAsked, Is.EqualTo(2));
        }
    }
}
