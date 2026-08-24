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
        }

        [Test]
        public void InterviewRequiresBoardAndLimitsMainQuestionsToTwo()
        {
            Assert.That(session.TryBeginInterview(), Is.False);
            session.MarkBoardOpened();
            Assert.That(session.TryBeginInterview(), Is.True);

            Assert.That(session.TryAskMain("main-hobby", out _), Is.True);
            Assert.That(session.TryAskMain("main-hobby", out _), Is.False);
            Assert.That(session.TryAskMain("main-food", out _), Is.True);
            Assert.That(session.MainQuestionsRemaining, Is.Zero);
            Assert.That(session.TryAskMain("main-weather", out _), Is.False);
            Assert.That(session.CanCompleteInterview, Is.True);
        }

        [Test]
        public void KeyQuestionImmediatelyRequiresExactlyOneFollowUp()
        {
            session.MarkBoardOpened();
            session.TryBeginInterview();
            Assert.That(session.TryAskMain("main-time", out LawyerInterviewQuestion question), Is.True);
            Assert.That(question.IsKeyQuestion, Is.True);
            Assert.That(session.PendingKeyQuestionId, Is.EqualTo("main-time"));
            Assert.That(session.TryAskMain("main-phone", out _), Is.False);
            Assert.That(session.TryAskFollowUp("main-time", "time-route", out _), Is.True);
            Assert.That(session.TryAskFollowUp("main-time", "time-camera", out _), Is.False);
            Assert.That(session.PendingKeyQuestionId, Is.Empty);
        }

        [Test]
        public void CluesAndLinksAreDeduplicatedAndUndoable()
        {
            Assert.That(session.TryAddLink("brief-time", "brief-witness"), Is.True);
            Assert.That(session.TryAddLink("brief-witness", "brief-time"), Is.False);
            Assert.That(session.TryAddLink("brief-time", "brief-time"), Is.False);
            Assert.That(session.TryAddLink("brief-time", "missing"), Is.False);
            Assert.That(session.TryUndoLastLink(out LawyerClueLink removed), Is.True);
            Assert.That(removed, Is.EqualTo(new LawyerClueLink("brief-time", "brief-witness")));
            Assert.That(session.ClueLinks, Is.Empty);
        }
    }
}
