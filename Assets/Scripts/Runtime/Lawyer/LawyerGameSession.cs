using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    public enum LawyerSessionPhase
    {
        CaseBrief,
        DefendantInterview,
        EvidenceBoardPreview,
        Stopped
    }

    [DisallowMultipleComponent]
    public sealed class LawyerGameSession : MonoBehaviour
    {
        private readonly HashSet<string> unlockedClues = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> askedMainQuestions = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> selectedFollowUps = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector2> cluePositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private readonly List<LawyerClueLink> clueLinks = new List<LawyerClueLink>();

        public static LawyerGameSession Instance { get; private set; }

        public event Action<LawyerSessionPhase> PhaseChanged;
        public event Action<string> ClueUnlocked;
        public event Action<string> MainQuestionAsked;
        public event Action<string, string> FollowUpAsked;
        public event Action<LawyerClueLink> LinkAdded;
        public event Action<LawyerClueLink> LinkRemoved;

        public LawyerCaseDefinition CaseDefinition { get; private set; }
        public LawyerSessionPhase Phase { get; private set; } = LawyerSessionPhase.CaseBrief;
        public bool BoardOpened { get; private set; }
        public int MainQuestionsAsked => askedMainQuestions.Count;
        public int MainQuestionsRemaining => Mathf.Max(0, 2 - askedMainQuestions.Count);
        public string PendingKeyQuestionId { get; private set; } = string.Empty;
        public IReadOnlyCollection<string> UnlockedClueIds => unlockedClues;
        public IReadOnlyList<LawyerClueLink> ClueLinks => clueLinks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static LawyerGameSession Ensure(LawyerCaseDefinition caseDefinition)
        {
            if (Instance == null)
            {
                GameObject root = new GameObject("Lawyer Game Session");
                Instance = root.AddComponent<LawyerGameSession>();
            }

            if (Instance.CaseDefinition != caseDefinition)
                Instance.ResetCase(caseDefinition);
            return Instance;
        }

        public void ResetCase(LawyerCaseDefinition caseDefinition)
        {
            CaseDefinition = caseDefinition;
            unlockedClues.Clear();
            askedMainQuestions.Clear();
            selectedFollowUps.Clear();
            cluePositions.Clear();
            clueLinks.Clear();
            BoardOpened = false;
            PendingKeyQuestionId = string.Empty;
            SetPhase(LawyerSessionPhase.CaseBrief);

            IReadOnlyList<string> initialClues = caseDefinition != null ? caseDefinition.StartingClueIds : null;
            if (initialClues == null)
                return;
            for (int index = 0; index < initialClues.Count; index++)
                UnlockClue(initialClues[index]);
        }

        public void MarkBoardOpened() => BoardOpened = true;

        public bool TryBeginInterview()
        {
            if (Phase != LawyerSessionPhase.CaseBrief || !BoardOpened)
                return false;
            SetPhase(LawyerSessionPhase.DefendantInterview);
            return true;
        }

        public bool HasAskedMain(string questionId) => askedMainQuestions.Contains(questionId);

        public bool TryAskMain(string questionId, out LawyerInterviewQuestion question)
        {
            question = CaseDefinition != null ? CaseDefinition.GetQuestion(questionId) : null;
            if (Phase != LawyerSessionPhase.DefendantInterview || question == null
                || MainQuestionsRemaining <= 0 || !string.IsNullOrEmpty(PendingKeyQuestionId)
                || !askedMainQuestions.Add(questionId))
                return false;

            if (question.IsKeyQuestion)
                PendingKeyQuestionId = question.Id;
            UnlockFrom(question.Response);
            MainQuestionAsked?.Invoke(question.Id);
            return true;
        }

        public bool TryAskFollowUp(string mainQuestionId, string followUpId, out LawyerFollowUpQuestion followUp)
        {
            followUp = null;
            if (Phase != LawyerSessionPhase.DefendantInterview
                || PendingKeyQuestionId != mainQuestionId || selectedFollowUps.ContainsKey(mainQuestionId))
                return false;

            LawyerInterviewQuestion mainQuestion = CaseDefinition != null
                ? CaseDefinition.GetQuestion(mainQuestionId)
                : null;
            followUp = mainQuestion?.GetFollowUp(followUpId);
            if (followUp == null)
                return false;

            selectedFollowUps.Add(mainQuestionId, followUpId);
            PendingKeyQuestionId = string.Empty;
            UnlockFrom(followUp.Response);
            FollowUpAsked?.Invoke(mainQuestionId, followUpId);
            return true;
        }

        public bool CanCompleteInterview => Phase == LawyerSessionPhase.DefendantInterview
            && MainQuestionsRemaining == 0 && string.IsNullOrEmpty(PendingKeyQuestionId);

        public bool TryCompleteInterview()
        {
            if (!CanCompleteInterview)
                return false;
            SetPhase(LawyerSessionPhase.EvidenceBoardPreview);
            return true;
        }

        public void Stop() => SetPhase(LawyerSessionPhase.Stopped);

        public void SetCluePosition(string clueId, Vector2 localPosition)
        {
            if (!unlockedClues.Contains(clueId))
                return;
            cluePositions[clueId] = localPosition;
        }

        public bool TryGetCluePosition(string clueId, out Vector2 localPosition)
            => cluePositions.TryGetValue(clueId, out localPosition);

        public bool TryAddLink(string firstId, string secondId)
        {
            LawyerClueLink link = new LawyerClueLink(firstId, secondId);
            if (!link.IsValid || !unlockedClues.Contains(link.FirstId)
                || !unlockedClues.Contains(link.SecondId) || clueLinks.Contains(link))
                return false;
            clueLinks.Add(link);
            LinkAdded?.Invoke(link);
            return true;
        }

        public bool TryUndoLastLink(out LawyerClueLink removed)
        {
            if (clueLinks.Count == 0)
            {
                removed = default;
                return false;
            }
            int index = clueLinks.Count - 1;
            removed = clueLinks[index];
            clueLinks.RemoveAt(index);
            LinkRemoved?.Invoke(removed);
            return true;
        }

        private void UnlockFrom(LawyerTestimony testimony)
        {
            IReadOnlyList<string> ids = testimony?.RevealedClueIds;
            if (ids == null)
                return;
            for (int index = 0; index < ids.Count; index++)
                UnlockClue(ids[index]);
        }

        private void UnlockClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !unlockedClues.Add(clueId))
                return;
            ClueUnlocked?.Invoke(clueId);
        }

        private void SetPhase(LawyerSessionPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
