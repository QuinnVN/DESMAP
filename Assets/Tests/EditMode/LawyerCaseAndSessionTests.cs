using System.Collections.Generic;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerCaseAndSessionTests
    {
        private const string CasePath = "Assets/Data/Lawyer/Placeholder Lawyer Case.asset";
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
        public void PlaceholderCaseMatchesRequiredQuestionShape()
        {
            List<string> errors = new List<string>();
            caseDefinition.GetValidationErrors(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(caseDefinition.Questions, Has.Count.EqualTo(5));

            int keyCount = 0;
            for (int index = 0; index < caseDefinition.Questions.Count; index++)
            {
                LawyerInterviewQuestion question = caseDefinition.Questions[index];
                if (!question.IsKeyQuestion)
                    continue;
                keyCount++;
                Assert.That(question.FollowUps, Has.Count.EqualTo(3));
            }
            Assert.That(keyCount, Is.EqualTo(2));
            Assert.That(caseDefinition.Resolution, Is.Not.Null);
            Assert.That(caseDefinition.Resolution.RequiredLinks, Has.Count.EqualTo(2));
            Assert.That(caseDefinition.Resolution.ReasoningCards, Has.Count.EqualTo(2));
            Assert.That(caseDefinition.Resolution.TruthConclusion,
                Does.Contain("chưa đủ căn cứ quy trách nhiệm"));
        }

        [Test]
        public void ReasoningCardsUnlockPerChainAndCompletionWaitsForAllCards()
        {
            CompleteSuccessfulInterview();
            Assert.That(session.TryAddEvidenceLink(
                    "testimony-time", "contradiction-camera"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.UnlockedReasoningCardIds, Has.Count.EqualTo(1));
            Assert.That(session.AllReasoningCardsUnlocked, Is.False);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));

            Assert.That(session.TryAddEvidenceLink(
                    "testimony-phone", "contradiction-call"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));
            Assert.That(session.UnlockedReasoningCardIds, Has.Count.EqualTo(2));
            Assert.That(session.AllReasoningCardsUnlocked, Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.Resolved));
        }

        [Test]
        public void InterviewRequiresBoardAndAllowsEveryMainQuestion()
        {
            Assert.That(session.TryBeginInterview(), Is.False);
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);

            Assert.That(session.TryAskMain("main-hobby", out _), Is.True);
            Assert.That(session.TryAskMain("main-hobby", out _), Is.False);
            Assert.That(session.TryAskMain("main-food", out _), Is.True);
            Assert.That(session.MainQuestionsRemaining, Is.EqualTo(3));
            Assert.That(session.TryAskMain("main-weather", out _), Is.True);
            Assert.That(session.TryAskMain("main-time", out _), Is.True);
            Assert.That(session.TryAskMain("main-phone", out _), Is.True);
            Assert.That(session.MainQuestionsRemaining, Is.Zero);
            Assert.That(session.FollowUpQuestionsRemaining, Is.EqualTo(6));
            Assert.That(session.CanCompleteInterview, Is.False);
        }

        [Test]
        public void KeyQuestionAllowsAllFollowUpsWithoutBlockingOtherMainQuestions()
        {
            session.MarkBoardOpened();
            session.TryBeginInterview();
            Assert.That(session.TryAskMain("main-time", out LawyerInterviewQuestion question), Is.True);
            Assert.That(question.IsKeyQuestion, Is.True);
            Assert.That(session.PendingKeyQuestionId, Is.EqualTo("main-time"));
            Assert.That(session.TryAskMain("main-phone", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-route", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-camera", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-receipt", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-camera", out _), Is.False);
            Assert.That(session.PendingKeyQuestionId, Is.EqualTo("main-phone"));
        }

        [Test]
        public void RequiredLinksRejectIncorrectPairsAndResolveExactlyOnce()
        {
            CompleteSuccessfulInterview();
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);
            Assert.That(session.TryAddEvidenceLink("testimony-time", "testimony-phone"),
                Is.EqualTo(LawyerLinkAttemptResult.Incorrect));
            Assert.That(session.ClueLinks, Is.Empty);

            Assert.That(session.TryAddEvidenceLink(
                    "contradiction-camera", "testimony-time"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink(
                    "testimony-time", "contradiction-camera"),
                Is.EqualTo(LawyerLinkAttemptResult.Duplicate));
            Assert.That(session.TryAddEvidenceLink(
                    "testimony-phone", "contradiction-call"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.Resolved));
            Assert.That(session.ClueLinks, Has.Count.EqualTo(2));
            Assert.That(session.TryAddEvidenceLink("testimony-time", "contradiction-camera"),
                Is.EqualTo(LawyerLinkAttemptResult.Invalid));
            Assert.That(session.TryUndoLastLink(out _), Is.False);

            session.ResetCase(caseDefinition);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.CaseBrief));
            Assert.That(session.ClueLinks, Is.Empty);
        }

        [Test]
        public void DefensePhasesRequireResolutionAndCompleteInOrder()
        {
            Assert.That(session.TryBeginDefense(), Is.False);
            CompleteSuccessfulInterview();
            Assert.That(session.TryAddEvidenceLink(
                "testimony-time", "contradiction-camera"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink(
                "testimony-phone", "contradiction-call"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));

            string roundId = session.RoundId;
            Assert.That(roundId, Is.Not.Empty);
            Assert.That(session.TryBeginDefense(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefensePreparation));
            Assert.That(session.TryBeginDefenseUpload(), Is.False);
            Assert.That(session.TryBeginDefenseRecording(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefenseRecording));
            Assert.That(session.TryBeginDefenseUpload(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefenseUploading));
            Assert.That(session.TryCompleteDefense(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.Completed));
            Assert.That(session.RoundId, Is.EqualTo(roundId));
            Assert.That(session.TryCompleteDefense(), Is.False);
        }

        [Test]
        public void InterruptedRecordingCanReturnToPreparation()
        {
            CompleteSuccessfulInterview();
            session.TryAddEvidenceLink("testimony-time", "contradiction-camera");
            session.TryAddEvidenceLink("testimony-phone", "contradiction-call");
            session.TryBeginDefense();
            session.TryBeginDefenseRecording();

            Assert.That(session.TryReturnToDefensePreparation(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefensePreparation));
            Assert.That(session.TryReturnToDefensePreparation(), Is.False);
        }

        [Test]
        public void MissingInterviewEvidenceKeepsInterviewOpen()
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            Assert.That(session.TryAskMain("main-time", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-camera", out _), Is.True);
            Assert.That(session.TryAskMain("main-hobby", out _), Is.True);
            Assert.That(session.TryCompleteInterview(), Is.False);
            Assert.That(session.EvaluateEvidenceAvailability(), Is.False);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefendantInterview));
            Assert.That(session.TryProceedToEvidenceBoard(), Is.False);
            Assert.That(session.TryAddEvidenceLink("testimony-time", "contradiction-camera"),
                Is.EqualTo(LawyerLinkAttemptResult.Invalid));
        }

        [Test]
        public void SufficientInterviewEvidenceCanProceedBeforeAllQuestions()
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            Assert.That(session.TryAskMain("main-time", out _), Is.True);
            Assert.That(session.TryAskMain("main-phone", out _), Is.True);
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);
            Assert.That(session.TryProceedToEvidenceBoard(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
            Assert.That(session.TryAskMain("main-hobby", out _), Is.False);
        }

        [Test]
        public void ResolutionValidationRejectsInvalidDuplicateAndMissingClues()
        {
            LawyerCaseDefinition invalid = ScriptableObject.CreateInstance<LawyerCaseDefinition>();
            try
            {
                invalid.ConfigureInEditor(
                    caseDefinition.CaseId,
                    caseDefinition.CaseTitle,
                    caseDefinition.CaseSummary,
                    caseDefinition.InvestigationObjective,
                    caseDefinition.Timeline,
                    caseDefinition.Witnesses,
                    caseDefinition.CaseDetails,
                    caseDefinition.Clues,
                    caseDefinition.StartingClueIds,
                    caseDefinition.DefendantName,
                    caseDefinition.Questions,
                    new LawyerCaseResolution(
                        new[]
                        {
                            new LawyerRequiredClueLink("testimony-time", "contradiction-camera"),
                            new LawyerRequiredClueLink("contradiction-camera", "testimony-time"),
                            new LawyerRequiredClueLink("brief-time", "brief-time"),
                            new LawyerRequiredClueLink("brief-time", "missing")
                        },
                        "Kết luận mẫu.",
                        "Thông báo thiếu chứng cứ mẫu."));
                List<string> errors = new List<string>();
                invalid.GetValidationErrors(errors);
                Assert.That(errors, Has.Some.Contains("bị trùng"));
                Assert.That(errors, Has.Some.Contains("không hợp lệ"));
                Assert.That(errors, Has.Some.Contains("không tồn tại"));
            }
            finally
            {
                Object.DestroyImmediate(invalid);
            }
        }

        private void CompleteSuccessfulInterview()
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            Assert.That(session.TryAskMain("main-time", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-route", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-camera", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-receipt", out _), Is.True);
            Assert.That(session.TryAskMain("main-phone", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-phone", "phone-call", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-phone", "phone-owner", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-phone", "phone-location", out _), Is.True);
            Assert.That(session.TryAskMain("main-hobby", out _), Is.True);
            Assert.That(session.TryAskMain("main-food", out _), Is.True);
            Assert.That(session.TryAskMain("main-weather", out _), Is.True);
            Assert.That(session.TryCompleteInterview(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
        }
    }
}
