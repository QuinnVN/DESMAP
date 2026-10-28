using System.Collections.Generic;
using NCKH.VR.Doctor;
using NUnit.Framework;

namespace NCKH.VR.Doctor.Tests
{
    public sealed partial class DoctorGameRulesTests
    {
        [Test]
        public void EarlySubmissionScoresOnlyCurrentlyRevealedCards()
        {
            DoctorInfoCard openingCard = new DoctorInfoCard(
                "opening", "Fever", DoctorInfoCategory.Symptoms, true);
            DoctorInfoCard unrevealedCard = new DoctorInfoCard(
                "later", "Travelled recently", DoctorInfoCategory.TravelHistory, true);
            Dictionary<string, DoctorInfoCategory?> placements = new Dictionary<string, DoctorInfoCategory?>
            {
                ["opening"] = DoctorInfoCategory.Symptoms,
                ["later"] = DoctorInfoCategory.TravelHistory
            };

            Assert.That(
                DoctorGameRules.CountCorrectNotes(new[] { openingCard }, placements),
                Is.EqualTo(1));
            Assert.That(
                DoctorGameRules.CountCorrectNotes(new[] { openingCard, unrevealedCard }, placements),
                Is.EqualTo(2));
        }
    }
}
