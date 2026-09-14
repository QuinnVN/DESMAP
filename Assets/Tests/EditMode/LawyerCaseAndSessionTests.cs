using System.Collections.Generic;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerCaseAndSessionTests
    {
        private const string CasePath = "Assets/Data/Lawyer/Club Room Fire Case.asset";
        private LawyerCaseDefinition caseDefinition;
        private LawyerGameSession session;

        [SetUp]
        public void SetUp()
        {
            caseDefinition = AssetDatabase.LoadAssetAtPath<LawyerCaseDefinition>(CasePath);
            Assert.That(caseDefinition, Is.Not.Null);
            session = LawyerGameSession.Ensure(caseDefinition);
            session.ResetCase(caseDefinition);
        }

        [TearDown]
        public void TearDown()
        {
            if (LawyerGameSession.Instance != null)
                Object.DestroyImmediate(LawyerGameSession.Instance.gameObject);
        }

        [Test]
        public void ClubRoomFireCaseHasFourWavesAndBackendSamples()
        {
            List<string> errors = new List<string>();
            caseDefinition.GetValidationErrors(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(caseDefinition.InterviewWaves, Has.Count.EqualTo(4));
            for (int index = 0; index < caseDefinition.InterviewWaves.Count; index++)
                Assert.That(caseDefinition.InterviewWaves[index].Questions, Has.Count.EqualTo(5));
            Assert.That(caseDefinition.StartingClueIds, Has.Count.EqualTo(6));
            Assert.That(caseDefinition.Clues, Has.Count.EqualTo(26));
            Assert.That(caseDefinition.Resolution.EvidenceChainSlots, Has.Count.EqualTo(4));
            Assert.That(caseDefinition.Resolution.ReasoningCards, Has.Count.EqualTo(3));
            Assert.That(caseDefinition.SampleAnswers, Has.Count.EqualTo(5));
        }

        [Test]
        public void OneQuestionPerWaveAdvancesPermanently()
        {
            BeginInterview();
            Assert.That(session.CurrentWaveIndex, Is.Zero);
            Assert.That(session.TryAskMain("w2-other", out _), Is.False);
            Assert.That(session.TryAskMain("w1-door", out _), Is.True);
            Assert.That(session.CurrentWaveIndex, Is.EqualTo(1));
            Assert.That(session.TryAskMain("w1-charger", out _), Is.False);
            Assert.That(session.TryAskMain("w2-other", out _), Is.True);
            Assert.That(session.TryAskMain("w3-description", out _), Is.True);
            Assert.That(session.TryAskMain("w4-smell", out _), Is.True);
            Assert.That(session.CanCompleteInterview, Is.True);
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);
        }

        [Test]
        public void ThreeStrongAndOneWeakPassesButNoiseFails()
        {
            BeginInterview();
            Ask("w1-laptop", "w2-other", "w3-description", "w4-smell");
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);

            session.ResetCase(caseDefinition);
            BeginInterview();
            Ask("w1-door", "w2-other", "w3-description", "w4-window");
            Assert.That(session.EvaluateEvidenceAvailability(), Is.False);
            Assert.That(session.TryCompleteInterview(), Is.False);
        }

        [Test]
        public void InterviewRestartKeepsRoundAndInitialCluesButClearsAttemptProgress()
        {
            BeginInterview();
            string roundId = session.RoundId;
            Ask("w1-hurry", "w2-items", "w3-forgotten", "w4-window");
            Assert.That(session.UnlockedClueIds, Does.Contain("clue-10"));
            Assert.That(session.TryRestartInterview(), Is.True);
            Assert.That(session.RoundId, Is.EqualTo(roundId));
            Assert.That(session.InterviewRestartCount, Is.EqualTo(1));
            Assert.That(session.CurrentWaveIndex, Is.Zero);
            Assert.That(session.UnlockedClueIds, Has.Count.EqualTo(6));
            Assert.That(session.UnlockedClueIds, Does.Not.Contain("clue-10"));
            Assert.That(session.ClueLinks, Is.Empty);
        }

        [Test]
        public void DynamicChainAcceptsAnyActionOrderAndSerializesWaveOrder()
        {
            CompleteSuccessfulInterview();
            CollectionAssert.AreEqual(new[] { "clue-08", "clue-12", "clue-17", "clue-23" },
                session.GetOrderedEvidenceClueIds());
            Assert.That(session.TryAddEvidenceLink("clue-08", "clue-17"),
                Is.EqualTo(LawyerLinkAttemptResult.Incorrect));
            Assert.That(session.TryAddEvidenceLink("clue-17", "clue-23"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink("clue-08", "clue-12"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink("clue-12", "clue-17"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));
            Assert.That(session.AllReasoningCardsUnlocked, Is.True);
        }

        [Test]
        public void AssessmentContextContainsOnlyOrderedSelectedEvidence()
        {
            CompleteSuccessfulInterview();
            string json = LawyerAssessmentContextPayload.BuildJson(caseDefinition, session);
            Assert.That(json, Does.Contain("\"evidenceId\":\"clue-08\""));
            Assert.That(json.IndexOf("clue-08"), Is.LessThan(json.IndexOf("clue-12")));
            Assert.That(json.IndexOf("clue-12"), Is.LessThan(json.IndexOf("clue-17")));
            Assert.That(json.IndexOf("clue-17"), Is.LessThan(json.IndexOf("clue-23")));
            Assert.That(json, Does.Not.Contain("clue-01"));
            Assert.That(json, Does.Contain("\"sampleAnswers\""));
        }

        [Test]
        public void DefensePhasesCompleteInOrderAndKeepRound()
        {
            CompleteSuccessfulInterview();
            session.TryAddEvidenceLink("clue-08", "clue-12");
            session.TryAddEvidenceLink("clue-12", "clue-17");
            session.TryAddEvidenceLink("clue-17", "clue-23");
            string roundId = session.RoundId;
            Assert.That(session.TryBeginDefense(), Is.True);
            Assert.That(session.TryBeginDefenseRecording(), Is.True);
            Assert.That(session.TryBeginDefenseUpload(), Is.True);
            Assert.That(session.TryCompleteDefense(), Is.True);
            Assert.That(session.RoundId, Is.EqualTo(roundId));
        }

        private void BeginInterview()
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
        }

        private void CompleteSuccessfulInterview()
        {
            BeginInterview();
            Ask("w1-door", "w2-other", "w3-description", "w4-smell");
            Assert.That(session.TryCompleteInterview(), Is.True);
        }

        private void Ask(params string[] ids)
        {
            for (int index = 0; index < ids.Length; index++)
                Assert.That(session.TryAskMain(ids[index], out _), Is.True, ids[index]);
        }
    }
}
