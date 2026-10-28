using System.Linq;
using NCKH.VR.Doctor;
using NUnit.Framework;
using UnityEditor;

namespace NCKH.VR.Doctor.Tests
{
    public sealed class DoctorVietnameseCaseDataTests
    {
        private const string DatabasePath =
            "Assets/Data/Doctor/Doctor Game 2 Vietnamese Case Database.asset";

        [Test]
        public void DatabaseContainsTwentyCasesWithRequestedQuestionDistribution()
        {
            DoctorCaseDatabase database =
                AssetDatabase.LoadAssetAtPath<DoctorCaseDatabase>(DatabasePath);

            Assert.That(database, Is.Not.Null);
            Assert.That(database.Cases, Has.Count.EqualTo(20));
            Assert.That(database.Cases.Count(item => item.Phases.Count == 2), Is.EqualTo(2));
            Assert.That(database.Cases.Count(item => item.Phases.Count == 3), Is.EqualTo(12));
            Assert.That(database.Cases.Count(item => item.Phases.Count == 4), Is.EqualTo(6));
            Assert.That(database.Cases.All(item => item.IsSelectable), Is.True);
            Assert.That(database.Cases.All(item => item.InitialDialogue.SelectableCards.Count > 0), Is.True);
            Assert.That(database.Cases.SelectMany(item => item.Phases).All(phase => phase.IsValid), Is.True);
        }
    }
}
