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
        public void ClubRoomFireCaseMatchesRequiredQuestionShape()
        {
            List<string> errors = new List<string>();
            caseDefinition.GetValidationErrors(errors);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
            Assert.That(caseDefinition.Questions, Has.Count.EqualTo(5));
            Assert.That(caseDefinition.MainQuestionLimit, Is.EqualTo(2));

            int keyCount = 0;
            for (int index = 0; index < caseDefinition.Questions.Count; index++)
            {
                LawyerInterviewQuestion question = caseDefinition.Questions[index];
                if (question.IsKeyQuestion)
                    keyCount++;
                Assert.That(question.FollowUps, Has.Count.EqualTo(3));
            }
            Assert.That(keyCount, Is.EqualTo(2));
            Assert.That(caseDefinition.Clues, Has.Count.EqualTo(18));
            Assert.That(caseDefinition.StartingClueIds, Has.Count.EqualTo(8));
            Assert.That(caseDefinition.Resolution, Is.Not.Null);
            Assert.That(caseDefinition.Resolution.RequiredLinks, Has.Count.EqualTo(3));
            Assert.That(caseDefinition.Resolution.ReasoningCards, Has.Count.EqualTo(3));
            Assert.That(caseDefinition.Resolution.TruthConclusion,
                Does.Contain("chưa đủ để kết luận Minh"));
        }

        [Test]
        public void ReasoningCardsUnlockPerChainAndCompletionWaitsForAllCards()
        {
            CompleteSuccessfulInterview();
            Assert.That(session.TryAddEvidenceLink(
                    "clue-01", "clue-09"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.UnlockedReasoningCardIds, Has.Count.EqualTo(1));
            Assert.That(session.AllReasoningCardsUnlocked, Is.False);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));

            Assert.That(session.TryAddEvidenceLink(
                    "clue-11", "clue-15"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink(
                    "clue-04", "clue-10"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));
            Assert.That(session.UnlockedReasoningCardIds, Has.Count.EqualTo(3));
            Assert.That(session.AllReasoningCardsUnlocked, Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.Resolved));
        }

        [Test]
        public void InterviewRequiresBoardAndAllowsOnlyTwoMainQuestions()
        {
            Assert.That(session.TryBeginInterview(), Is.False);
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);

            Assert.That(session.TryAskMain("main-return", out _), Is.True);
            Assert.That(session.TryAskMain("main-return", out _), Is.False);
            Assert.That(session.TryAskMain("main-lan-observation", out _), Is.True);
            Assert.That(session.MainQuestionsRemaining, Is.Zero);
            Assert.That(session.TryAskMain("main-full-timeline", out _), Is.False);
            Assert.That(session.TryAskMain("main-outlet", out _), Is.False);
            Assert.That(session.TryAskMain("main-other-person", out _), Is.False);
            Assert.That(session.FollowUpQuestionsRemaining, Is.EqualTo(6));
            Assert.That(session.CanCompleteInterview, Is.False);
        }

        [Test]
        public void KeyQuestionAllowsAllFollowUpsWithoutBlockingOtherMainQuestions()
        {
            session.MarkBoardOpened();
            session.TryBeginInterview();
            Assert.That(session.TryAskMain("main-outlet", out LawyerInterviewQuestion question), Is.True);
            Assert.That(question.IsKeyQuestion, Is.True);
            Assert.That(session.PendingKeyQuestionId, Is.EqualTo("main-outlet"));
            Assert.That(session.TryAskMain("main-other-person", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-charger", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-other-device", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-warning", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-charger", out _), Is.False);
            Assert.That(session.PendingKeyQuestionId, Is.EqualTo("main-other-person"));
        }

        [Test]
        public void RequiredLinksRejectIncorrectPairsAndResolveExactlyOnce()
        {
            CompleteSuccessfulInterview();
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);
            Assert.That(session.TryAddEvidenceLink("clue-01", "clue-03"),
                Is.EqualTo(LawyerLinkAttemptResult.Incorrect));
            Assert.That(session.ClueLinks, Is.Empty);

            Assert.That(session.TryAddEvidenceLink(
                    "clue-09", "clue-01"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink(
                    "clue-01", "clue-09"),
                Is.EqualTo(LawyerLinkAttemptResult.Duplicate));
            Assert.That(session.TryAddEvidenceLink(
                    "clue-11", "clue-15"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink("clue-04", "clue-10"),
                Is.EqualTo(LawyerLinkAttemptResult.Solved));
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.Resolved));
            Assert.That(session.ClueLinks, Has.Count.EqualTo(3));
            Assert.That(session.TryAddEvidenceLink("clue-01", "clue-09"),
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
                "clue-01", "clue-09"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink(
                "clue-11", "clue-15"),
                Is.EqualTo(LawyerLinkAttemptResult.Added));
            Assert.That(session.TryAddEvidenceLink("clue-04", "clue-10"),
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
            session.TryAddEvidenceLink("clue-01", "clue-09");
            session.TryAddEvidenceLink("clue-11", "clue-15");
            session.TryAddEvidenceLink("clue-04", "clue-10");
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
            Assert.That(session.TryAskMain("main-outlet", out _), Is.True);
            Assert.That(session.TryCompleteInterview(), Is.False);
            Assert.That(session.EvaluateEvidenceAvailability(), Is.False);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.DefendantInterview));
            Assert.That(session.TryProceedToEvidenceBoard(), Is.False);
            Assert.That(session.TryAddEvidenceLink("clue-01", "clue-03"),
                Is.EqualTo(LawyerLinkAttemptResult.Invalid));
        }

        [Test]
        public void SufficientInterviewEvidenceCanProceedBeforeAllQuestions()
        {
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);
            Assert.That(session.TryAskMain("main-outlet", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-charger", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-outlet", "outlet-warning", out _), Is.True);
            Assert.That(session.TryAskMain("main-other-person", out _), Is.True);
            Assert.That(session.EvaluateEvidenceAvailability(), Is.True);
            Assert.That(session.TryProceedToEvidenceBoard(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
            Assert.That(session.TryAskMain("main-return", out _), Is.False);
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
                            new LawyerRequiredClueLink("clue-01", "clue-06"),
                            new LawyerRequiredClueLink("clue-06", "clue-01"),
                            new LawyerRequiredClueLink("clue-01", "clue-01"),
                            new LawyerRequiredClueLink("clue-01", "missing")
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
            int asked = 0;
            for (int questionIndex = 0; questionIndex < caseDefinition.Questions.Count; questionIndex++)
            {
                LawyerInterviewQuestion question = caseDefinition.Questions[questionIndex];
                if (!question.IsKeyQuestion)
                    continue;
                Assert.That(session.TryAskMain(question.Id, out _), Is.True);
                asked++;
                for (int followUpIndex = 0; followUpIndex < question.FollowUps.Count; followUpIndex++)
                {
                    Assert.That(session.TryAskFollowUp(
                        question.Id, question.FollowUps[followUpIndex].Id, out _), Is.True);
                }
            }
            Assert.That(asked, Is.EqualTo(caseDefinition.MainQuestionLimit));
            Assert.That(session.TryCompleteInterview(), Is.True);
            Assert.That(session.Phase, Is.EqualTo(LawyerSessionPhase.EvidenceBoard));
        }
    }
}
