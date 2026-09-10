using System;
using System.Collections.Generic;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    public enum LawyerSessionPhase
    {
        CaseBrief = 0,
        DefendantInterview = 1,
        EvidenceBoard = 2,
        EvidenceBoardPreview = EvidenceBoard,
        Resolved = 3,
        Failed = 4,
        Stopped = 5,
        DefensePreparation = 6,
        DefenseRecording = 7,
        DefenseUploading = 8,
        Completed = 9
    }

    public enum LawyerLinkAttemptResult
    {
        Invalid,
        Incorrect,
        Duplicate,
        Added,
        Solved
    }

    [DisallowMultipleComponent]
    public sealed class LawyerGameSession : MonoBehaviour, ISceneResetHandler
    {
        private readonly HashSet<string> unlockedClues = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> askedMainQuestions = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> selectedFollowUps =
            new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector2> cluePositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private readonly Dictionary<string, Vector2> reasoningPositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
        private readonly List<LawyerClueLink> clueLinks = new List<LawyerClueLink>();
        private readonly HashSet<string> unlockedReasoningCards = new HashSet<string>(StringComparer.Ordinal);

        public static LawyerGameSession Instance { get; private set; }
        public string SceneId => "lawyer";

        public event Action<LawyerSessionPhase> PhaseChanged;
        public event Action<string> ClueUnlocked;
        public event Action<string> MainQuestionAsked;
        public event Action<string, string> FollowUpAsked;
        public event Action<LawyerClueLink> LinkAdded;
        public event Action<LawyerClueLink> LinkRemoved;
        public event Action<string> ReasoningCardUnlocked;

        public LawyerCaseDefinition CaseDefinition { get; private set; }
        public LawyerSessionPhase Phase { get; private set; } = LawyerSessionPhase.CaseBrief;
        public string RoundId { get; private set; } = string.Empty;
        public bool BoardOpened { get; private set; }
        public int MainQuestionsAsked => askedMainQuestions.Count;
        public int MainQuestionsRemaining
        {
            get
            {
                int total = CaseDefinition != null && CaseDefinition.Questions != null
                    ? Mathf.Min(CaseDefinition.MainQuestionLimit, CaseDefinition.Questions.Count)
                    : 0;
                return Mathf.Max(0, total - askedMainQuestions.Count);
            }
        }

        public int FollowUpQuestionsRemaining
        {
            get
            {
                if (CaseDefinition == null || CaseDefinition.Questions == null)
                    return 0;

                int remaining = 0;
                for (int index = 0; index < CaseDefinition.Questions.Count; index++)
                {
                    LawyerInterviewQuestion question = CaseDefinition.Questions[index];
                    if (question == null || !askedMainQuestions.Contains(question.Id))
                        continue;
                    for (int followIndex = 0; followIndex < question.FollowUps.Count; followIndex++)
                    {
                        LawyerFollowUpQuestion followUp = question.FollowUps[followIndex];
                        if (followUp != null && !HasAskedFollowUp(question.Id, followUp.Id))
                            remaining++;
                    }
                }
                return remaining;
            }
        }

        public int QuestionsRemaining => MainQuestionsRemaining + FollowUpQuestionsRemaining;

        public string PendingKeyQuestionId
        {
            get
            {
                if (CaseDefinition == null || CaseDefinition.Questions == null)
                    return string.Empty;
                for (int index = 0; index < CaseDefinition.Questions.Count; index++)
                {
                    LawyerInterviewQuestion question = CaseDefinition.Questions[index];
                    if (question != null && question.IsKeyQuestion
                        && askedMainQuestions.Contains(question.Id)
                        && HasUnaskedFollowUps(question.Id))
                        return question.Id;
                }
                return string.Empty;
            }
        }
        public IReadOnlyCollection<string> UnlockedClueIds => unlockedClues;
        public IReadOnlyCollection<string> UnlockedReasoningCardIds => unlockedReasoningCards;
        public IReadOnlyList<LawyerClueLink> ClueLinks => clueLinks;
        public bool IsReasoningCardUnlocked(string cardId)
            => unlockedReasoningCards.Contains(cardId);
        public bool AllReasoningCardsUnlocked
        {
            get
            {
                LawyerCaseResolution resolution = CaseDefinition != null ? CaseDefinition.Resolution : null;
                IReadOnlyList<LawyerReasoningCardDefinition> cards = resolution?.GetReasoningCardsOrFallback();
                return cards != null && cards.Count > 0 && unlockedReasoningCards.Count >= cards.Count;
            }
        }

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
            SceneResetRegistry.Unregister(this);
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable() => SceneResetRegistry.Register(this);

        private void OnDisable() => SceneResetRegistry.Unregister(this);

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
            reasoningPositions.Clear();
            clueLinks.Clear();
            unlockedReasoningCards.Clear();
            RoundId = string.Empty;
            BoardOpened = false;
            SetPhase(LawyerSessionPhase.CaseBrief);

            IReadOnlyList<string> initialClues = caseDefinition != null ? caseDefinition.StartingClueIds : null;
            if (initialClues == null)
                return;
            for (int index = 0; index < initialClues.Count; index++)
                UnlockClue(initialClues[index]);
        }

        public bool TryResetToInitialState(out string errorCode)
        {
            ResetCase(CaseDefinition);
            errorCode = string.Empty;
            return true;
        }

        public void MarkBoardOpened() => BoardOpened = true;

        public void BeginRound() => RoundId = Guid.NewGuid().ToString("N");

        public bool TryBeginInterview()
        {
            if (Phase != LawyerSessionPhase.CaseBrief || !BoardOpened)
                return false;
            if (string.IsNullOrEmpty(RoundId))
                BeginRound();
            SetPhase(LawyerSessionPhase.DefendantInterview);
            return true;
        }

        public bool HasAskedMain(string questionId) => askedMainQuestions.Contains(questionId);

        public bool HasAskedFollowUp(string mainQuestionId, string followUpId)
        {
            return selectedFollowUps.TryGetValue(mainQuestionId, out HashSet<string> followUps)
                && followUps.Contains(followUpId);
        }

        public bool HasUnaskedFollowUps(string mainQuestionId)
        {
            LawyerInterviewQuestion mainQuestion = CaseDefinition != null
                ? CaseDefinition.GetQuestion(mainQuestionId)
                : null;
            if (mainQuestion == null)
                return false;
            for (int index = 0; index < mainQuestion.FollowUps.Count; index++)
            {
                LawyerFollowUpQuestion followUp = mainQuestion.FollowUps[index];
                if (followUp != null && !HasAskedFollowUp(mainQuestionId, followUp.Id))
                    return true;
            }
            return false;
        }

        public bool TryAskMain(string questionId, out LawyerInterviewQuestion question)
        {
            question = CaseDefinition != null ? CaseDefinition.GetQuestion(questionId) : null;
            if (Phase != LawyerSessionPhase.DefendantInterview || question == null
                || MainQuestionsRemaining <= 0
                || !askedMainQuestions.Add(questionId))
                return false;

            UnlockFrom(question.Response);
            MainQuestionAsked?.Invoke(question.Id);
            return true;
        }

        public bool TryAskFollowUp(string mainQuestionId, string followUpId, out LawyerFollowUpQuestion followUp)
        {
            followUp = null;
            if (Phase != LawyerSessionPhase.DefendantInterview
                || !askedMainQuestions.Contains(mainQuestionId))
                return false;

            LawyerInterviewQuestion mainQuestion = CaseDefinition != null
                ? CaseDefinition.GetQuestion(mainQuestionId)
                : null;
            followUp = mainQuestion?.GetFollowUp(followUpId);
            if (followUp == null)
                return false;

            if (!selectedFollowUps.TryGetValue(mainQuestionId, out HashSet<string> askedFollowUps))
            {
                askedFollowUps = new HashSet<string>(StringComparer.Ordinal);
                selectedFollowUps.Add(mainQuestionId, askedFollowUps);
            }
            if (!askedFollowUps.Add(followUpId))
            {
                followUp = null;
                return false;
            }
            UnlockFrom(followUp.Response);
            FollowUpAsked?.Invoke(mainQuestionId, followUpId);
            return true;
        }

        public bool CanCompleteInterview => Phase == LawyerSessionPhase.DefendantInterview
            && QuestionsRemaining == 0;

        public bool TryCompleteInterview()
        {
            if (!CanCompleteInterview)
                return false;
            SetPhase(LawyerSessionPhase.EvidenceBoard);
            return true;
        }

        public bool EvaluateEvidenceAvailability()
        {
            if (Phase != LawyerSessionPhase.EvidenceBoard
                && Phase != LawyerSessionPhase.DefendantInterview)
                return Phase != LawyerSessionPhase.Failed;

            LawyerCaseResolution resolution = CaseDefinition != null
                ? CaseDefinition.Resolution
                : null;
            IReadOnlyList<LawyerRequiredClueLink> requiredLinks = resolution?.RequiredLinks;
            if (requiredLinks == null || requiredLinks.Count == 0)
                return false;

            for (int index = 0; index < requiredLinks.Count; index++)
            {
                LawyerRequiredClueLink required = requiredLinks[index];
                if (required == null || !required.Link.IsValid
                    || !unlockedClues.Contains(required.Link.FirstId)
                    || !unlockedClues.Contains(required.Link.SecondId))
                {
                    return false;
                }
            }
            return true;
        }

        public bool TryProceedToEvidenceBoard()
        {
            if (Phase != LawyerSessionPhase.DefendantInterview
                || !EvaluateEvidenceAvailability())
                return false;
            SetPhase(LawyerSessionPhase.EvidenceBoard);
            return true;
        }

        public void Stop() => SetPhase(LawyerSessionPhase.Stopped);

        public bool TryBeginDefense()
        {
            if (Phase != LawyerSessionPhase.Resolved)
                return false;
            SetPhase(LawyerSessionPhase.DefensePreparation);
            return true;
        }

        public bool TryBeginDefenseRecording()
        {
            if (Phase != LawyerSessionPhase.DefensePreparation)
                return false;
            SetPhase(LawyerSessionPhase.DefenseRecording);
            return true;
        }

        public bool TryReturnToDefensePreparation()
        {
            if (Phase != LawyerSessionPhase.DefenseRecording)
                return false;
            SetPhase(LawyerSessionPhase.DefensePreparation);
            return true;
        }

        public bool TryBeginDefenseUpload()
        {
            if (Phase != LawyerSessionPhase.DefenseRecording)
                return false;
            SetPhase(LawyerSessionPhase.DefenseUploading);
            return true;
        }

        public bool TryCompleteDefense()
        {
            if (Phase != LawyerSessionPhase.DefenseUploading)
                return false;
            SetPhase(LawyerSessionPhase.Completed);
            return true;
        }

        public void SetCluePosition(string clueId, Vector2 localPosition)
        {
            if (!unlockedClues.Contains(clueId))
                return;
            cluePositions[clueId] = localPosition;
        }

        public bool TryGetCluePosition(string clueId, out Vector2 localPosition)
            => cluePositions.TryGetValue(clueId, out localPosition);

        public void SetReasoningPosition(string cardId, Vector2 localPosition)
        {
            if (!unlockedReasoningCards.Contains(cardId))
                return;
            reasoningPositions[cardId] = localPosition;
        }

        public bool TryGetReasoningPosition(string cardId, out Vector2 localPosition)
            => reasoningPositions.TryGetValue(cardId, out localPosition);

        public LawyerLinkAttemptResult TryAddEvidenceLink(string firstId, string secondId)
        {
            LawyerClueLink link = new LawyerClueLink(firstId, secondId);
            if (Phase != LawyerSessionPhase.EvidenceBoard || !link.IsValid
                || !unlockedClues.Contains(link.FirstId)
                || !unlockedClues.Contains(link.SecondId))
                return LawyerLinkAttemptResult.Invalid;
            LawyerCaseResolution resolution = CaseDefinition != null
                ? CaseDefinition.Resolution
                : null;
            if (resolution == null || !resolution.Contains(link))
                return LawyerLinkAttemptResult.Incorrect;
            if (clueLinks.Contains(link))
                return LawyerLinkAttemptResult.Duplicate;
            clueLinks.Add(link);
            LinkAdded?.Invoke(link);
            UnlockCompletedReasoningCards();
            if (!HasEveryRequiredLink(resolution.RequiredLinks) || !AllReasoningCardsUnlocked)
                return LawyerLinkAttemptResult.Added;
            SetPhase(LawyerSessionPhase.Resolved);
            return LawyerLinkAttemptResult.Solved;
        }

        public bool TryAddLink(string firstId, string secondId)
        {
            LawyerLinkAttemptResult result = TryAddEvidenceLink(firstId, secondId);
            return result == LawyerLinkAttemptResult.Added
                || result == LawyerLinkAttemptResult.Solved;
        }

        public bool TryUndoLastLink(out LawyerClueLink removed)
        {
            if (Phase != LawyerSessionPhase.EvidenceBoard || clueLinks.Count == 0)
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

        private bool HasEveryRequiredLink(IReadOnlyList<LawyerRequiredClueLink> requiredLinks)
        {
            for (int index = 0; index < requiredLinks.Count; index++)
            {
                LawyerRequiredClueLink required = requiredLinks[index];
                if (required == null || !clueLinks.Contains(required.Link))
                    return false;
            }
            return true;
        }

        private void UnlockCompletedReasoningCards()
        {
            LawyerCaseResolution resolution = CaseDefinition != null ? CaseDefinition.Resolution : null;
            IReadOnlyList<LawyerReasoningCardDefinition> cards = resolution?.GetReasoningCardsOrFallback();
            if (cards == null)
                return;
            for (int index = 0; index < cards.Count; index++)
            {
                LawyerReasoningCardDefinition card = cards[index];
                if (card == null || string.IsNullOrWhiteSpace(card.Id)
                    || unlockedReasoningCards.Contains(card.Id))
                    continue;
                bool complete = card.RequiredLinks != null && card.RequiredLinks.Count > 0;
                if (complete)
                {
                    for (int linkIndex = 0; linkIndex < card.RequiredLinks.Count; linkIndex++)
                    {
                        LawyerRequiredClueLink required = card.RequiredLinks[linkIndex];
                        if (required == null || !clueLinks.Contains(required.Link))
                        {
                            complete = false;
                            break;
                        }
                    }
                }
                if (!complete)
                    continue;
                unlockedReasoningCards.Add(card.Id);
                ReasoningCardUnlocked?.Invoke(card.Id);
            }
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
