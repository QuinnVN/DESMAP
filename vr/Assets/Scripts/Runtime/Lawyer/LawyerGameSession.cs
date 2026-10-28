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

    public enum LawyerLinkAttemptResult { Invalid, Incorrect, Duplicate, Added, Solved }

    [DisallowMultipleComponent]
    public sealed class LawyerGameSession : MonoBehaviour, ISceneResetHandler
    {
        private readonly HashSet<string> unlockedClues = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> selectedQuestionIds = new List<string>();
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
        public int InterviewRestartCount { get; private set; }
        public int CurrentWaveIndex => selectedQuestionIds.Count;
        public int TotalWaves => CaseDefinition?.InterviewWaves?.Count ?? 0;
        public int MainQuestionsAsked => selectedQuestionIds.Count;
        public int MainQuestionsRemaining => Mathf.Max(0, TotalWaves - CurrentWaveIndex);
        public int FollowUpQuestionsRemaining => 0;
        public int QuestionsRemaining => MainQuestionsRemaining;
        public string PendingKeyQuestionId => string.Empty;
        public IReadOnlyList<string> SelectedQuestionIds => selectedQuestionIds;
        public IReadOnlyCollection<string> UnlockedClueIds => unlockedClues;
        public IReadOnlyCollection<string> UnlockedReasoningCardIds => unlockedReasoningCards;
        public IReadOnlyList<LawyerClueLink> ClueLinks => clueLinks;
        public bool IsReasoningCardUnlocked(string cardId) => unlockedReasoningCards.Contains(cardId);
        public LawyerInterviewWave CurrentWave
        {
            get
            {
                IReadOnlyList<LawyerInterviewWave> waves = CaseDefinition?.InterviewWaves;
                return waves != null && CurrentWaveIndex < waves.Count ? waves[CurrentWaveIndex] : null;
            }
        }
        public bool AllReasoningCardsUnlocked
        {
            get
            {
                IReadOnlyList<LawyerClueLink> required = GetRequiredEvidenceLinks();
                return required.Count > 0 && unlockedReasoningCards.Count >= required.Count;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy()
        {
            SceneResetRegistry.Unregister(this);
            if (Instance == this) Instance = null;
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
            if (Instance.CaseDefinition != caseDefinition) Instance.ResetCase(caseDefinition);
            return Instance;
        }

        public void ResetCase(LawyerCaseDefinition caseDefinition)
        {
            CaseDefinition = caseDefinition;
            InterviewRestartCount = 0;
            RoundId = string.Empty;
            BoardOpened = false;
            ClearAttemptProgress();
            SetPhase(LawyerSessionPhase.CaseBrief);
            RestoreStartingClues(false);
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
            if (Phase != LawyerSessionPhase.CaseBrief || !BoardOpened) return false;
            if (string.IsNullOrEmpty(RoundId)) BeginRound();
            SetPhase(LawyerSessionPhase.DefendantInterview);
            return true;
        }

        public bool HasAskedMain(string questionId) => selectedQuestionIds.Contains(questionId);
        public bool HasAskedFollowUp(string mainQuestionId, string followUpId) => false;
        public bool HasUnaskedFollowUps(string mainQuestionId) => false;

        public bool TryAskMain(string questionId, out LawyerInterviewQuestion question)
        {
            question = null;
            LawyerInterviewWave wave = CurrentWave;
            if (Phase != LawyerSessionPhase.DefendantInterview || wave == null) return false;
            for (int index = 0; index < wave.Questions.Count; index++)
            {
                LawyerInterviewQuestion candidate = wave.Questions[index];
                if (candidate != null && candidate.Id == questionId) { question = candidate; break; }
            }
            if (question == null || selectedQuestionIds.Contains(questionId)) return false;
            selectedQuestionIds.Add(questionId);
            UnlockFrom(question.Response);
            MainQuestionAsked?.Invoke(question.Id);
            return true;
        }
        public bool TryAskFollowUp(string mainQuestionId, string followUpId, out LawyerFollowUpQuestion followUp)
        {
            followUp = null;
            return false;
        }

        public bool CanCompleteInterview => Phase == LawyerSessionPhase.DefendantInterview
            && TotalWaves > 0 && CurrentWaveIndex == TotalWaves;
        public bool TryCompleteInterview()
        {
            if (!CanCompleteInterview || !EvaluateEvidenceAvailability()) return false;
            SetPhase(LawyerSessionPhase.EvidenceBoard);
            return true;
        }
        public bool EvaluateEvidenceAvailability()
        {
            if (!CanCompleteInterview) return false;
            int strong = 0;
            int weak = 0;
            int noise = 0;
            for (int index = 0; index < selectedQuestionIds.Count; index++)
            {
                LawyerInterviewQuestion question = CaseDefinition.GetQuestion(selectedQuestionIds[index]);
                if (question == null) continue;
                if (question.Strength == LawyerClueStrength.Strong) strong++;
                else if (question.Strength == LawyerClueStrength.Weak) weak++;
                else noise++;
            }
            return noise == 0 && (strong >= 4 || (strong == 3 && weak >= 1));
        }
        public bool TryProceedToEvidenceBoard() => TryCompleteInterview();
        public bool TryRestartInterview()
        {
            if (Phase != LawyerSessionPhase.DefendantInterview || !CanCompleteInterview) return false;
            InterviewRestartCount++;
            ClearAttemptProgress();
            RestoreStartingClues(false);
            SetPhase(LawyerSessionPhase.DefendantInterview);
            return true;
        }

        public IReadOnlyList<string> GetOrderedEvidenceClueIds()
        {
            List<string> ordered = new List<string>();
            IReadOnlyList<LawyerEvidenceChainSlot> slots = CaseDefinition?.Resolution?.EvidenceChainSlots;
            if (slots == null) return ordered;
            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                LawyerEvidenceChainSlot slot = slots[slotIndex];
                for (int questionIndex = 0; questionIndex < selectedQuestionIds.Count; questionIndex++)
                {
                    LawyerInterviewQuestion question = CaseDefinition.GetQuestion(selectedQuestionIds[questionIndex]);
                    IReadOnlyList<string> revealed = question?.Response?.RevealedClueIds;
                    if (revealed != null && revealed.Count == 1 && slot.Accepts(revealed[0]))
                    {
                        ordered.Add(revealed[0]);
                        break;
                    }
                }
            }
            return ordered;
        }
        public LawyerClueStrength GetSelectedClueStrength(string clueId)
        {
            for (int index = 0; index < selectedQuestionIds.Count; index++)
            {
                LawyerInterviewQuestion question = CaseDefinition.GetQuestion(selectedQuestionIds[index]);
                IReadOnlyList<string> revealed = question?.Response?.RevealedClueIds;
                if (revealed == null) continue;
                for (int clueIndex = 0; clueIndex < revealed.Count; clueIndex++)
                    if (revealed[clueIndex] == clueId) return question.Strength;
            }
            return LawyerClueStrength.Weak;
        }
        public IReadOnlyList<LawyerClueLink> GetRequiredEvidenceLinks()
        {
            IReadOnlyList<string> ordered = GetOrderedEvidenceClueIds();
            List<LawyerClueLink> required = new List<LawyerClueLink>();
            for (int index = 0; index + 1 < ordered.Count; index++)
                required.Add(new LawyerClueLink(ordered[index], ordered[index + 1]));
            return required;
        }
        public bool TryGetEvidenceLinkForReasoning(string reasoningId, out LawyerClueLink link)
        {
            IReadOnlyList<LawyerReasoningCardDefinition> cards = CaseDefinition?.Resolution?.ReasoningCards;
            IReadOnlyList<LawyerClueLink> required = GetRequiredEvidenceLinks();
            if (cards != null)
            {
                for (int index = 0; index < cards.Count && index < required.Count; index++)
                    if (cards[index] != null && cards[index].Id == reasoningId)
                    { link = required[index]; return true; }
            }
            link = default;
            return false;
        }

        public void Stop() => SetPhase(LawyerSessionPhase.Stopped);
        public bool TryBeginDefense()
        {
            if (Phase != LawyerSessionPhase.Resolved) return false;
            SetPhase(LawyerSessionPhase.DefensePreparation); return true;
        }
        public bool TryBeginDefenseRecording()
        {
            if (Phase != LawyerSessionPhase.DefensePreparation) return false;
            SetPhase(LawyerSessionPhase.DefenseRecording); return true;
        }
        public bool TryReturnToDefensePreparation()
        {
            if (Phase != LawyerSessionPhase.DefenseRecording) return false;
            SetPhase(LawyerSessionPhase.DefensePreparation); return true;
        }
        public bool TryBeginDefenseUpload()
        {
            if (Phase != LawyerSessionPhase.DefenseRecording) return false;
            SetPhase(LawyerSessionPhase.DefenseUploading); return true;
        }
        public bool TryCompleteDefense()
        {
            if (Phase != LawyerSessionPhase.DefenseUploading) return false;
            SetPhase(LawyerSessionPhase.Completed); return true;
        }

        public void SetCluePosition(string clueId, Vector2 localPosition)
        { if (unlockedClues.Contains(clueId)) cluePositions[clueId] = localPosition; }
        public bool TryGetCluePosition(string clueId, out Vector2 localPosition) => cluePositions.TryGetValue(clueId, out localPosition);
        public void SetReasoningPosition(string cardId, Vector2 localPosition)
        { if (unlockedReasoningCards.Contains(cardId)) reasoningPositions[cardId] = localPosition; }
        public bool TryGetReasoningPosition(string cardId, out Vector2 localPosition) => reasoningPositions.TryGetValue(cardId, out localPosition);

        public LawyerLinkAttemptResult TryAddEvidenceLink(string firstId, string secondId)
        {
            LawyerClueLink link = new LawyerClueLink(firstId, secondId);
            if (Phase != LawyerSessionPhase.EvidenceBoard || !link.IsValid
                || !unlockedClues.Contains(link.FirstId) || !unlockedClues.Contains(link.SecondId))
                return LawyerLinkAttemptResult.Invalid;
            IReadOnlyList<LawyerClueLink> required = GetRequiredEvidenceLinks();
            if (!Contains(required, link)) return LawyerLinkAttemptResult.Incorrect;
            if (clueLinks.Contains(link)) return LawyerLinkAttemptResult.Duplicate;
            clueLinks.Add(link);
            LinkAdded?.Invoke(link);
            RebuildReasoningCards();
            if (!HasEveryRequiredLink(required) || !AllReasoningCardsUnlocked) return LawyerLinkAttemptResult.Added;
            SetPhase(LawyerSessionPhase.Resolved);
            return LawyerLinkAttemptResult.Solved;
        }
        public bool TryAddLink(string firstId, string secondId)
        {
            LawyerLinkAttemptResult result = TryAddEvidenceLink(firstId, secondId);
            return result == LawyerLinkAttemptResult.Added || result == LawyerLinkAttemptResult.Solved;
        }
        public bool TryUndoLastLink(out LawyerClueLink removed)
        {
            if (Phase != LawyerSessionPhase.EvidenceBoard || clueLinks.Count == 0)
            { removed = default; return false; }
            int index = clueLinks.Count - 1;
            removed = clueLinks[index];
            clueLinks.RemoveAt(index);
            RebuildReasoningCards();
            LinkRemoved?.Invoke(removed);
            return true;
        }

        private void RebuildReasoningCards()
        {
            HashSet<string> previous = new HashSet<string>(unlockedReasoningCards, StringComparer.Ordinal);
            unlockedReasoningCards.Clear();
            IReadOnlyList<LawyerReasoningCardDefinition> cards = CaseDefinition?.Resolution?.ReasoningCards;
            IReadOnlyList<LawyerClueLink> required = GetRequiredEvidenceLinks();
            if (cards == null) return;
            for (int index = 0; index < cards.Count && index < required.Count; index++)
            {
                LawyerReasoningCardDefinition card = cards[index];
                if (card == null || !clueLinks.Contains(required[index])) continue;
                unlockedReasoningCards.Add(card.Id);
                if (!previous.Contains(card.Id)) ReasoningCardUnlocked?.Invoke(card.Id);
            }
        }
        private bool HasEveryRequiredLink(IReadOnlyList<LawyerClueLink> required)
        {
            if (required == null || required.Count == 0) return false;
            for (int index = 0; index < required.Count; index++) if (!clueLinks.Contains(required[index])) return false;
            return true;
        }
        private static bool Contains(IReadOnlyList<LawyerClueLink> links, LawyerClueLink target)
        {
            for (int index = 0; index < links.Count; index++) if (links[index].Equals(target)) return true;
            return false;
        }
        private void ClearAttemptProgress()
        {
            unlockedClues.Clear();
            selectedQuestionIds.Clear();
            cluePositions.Clear();
            reasoningPositions.Clear();
            clueLinks.Clear();
            unlockedReasoningCards.Clear();
        }
        private void RestoreStartingClues(bool notify)
        {
            IReadOnlyList<string> initial = CaseDefinition?.StartingClueIds;
            if (initial == null) return;
            for (int index = 0; index < initial.Count; index++)
            {
                string clueId = initial[index];
                if (string.IsNullOrWhiteSpace(clueId) || !unlockedClues.Add(clueId)) continue;
                if (notify) ClueUnlocked?.Invoke(clueId);
            }
        }
        private void UnlockFrom(LawyerTestimony testimony)
        {
            IReadOnlyList<string> ids = testimony?.RevealedClueIds;
            if (ids == null) return;
            for (int index = 0; index < ids.Count; index++) UnlockClue(ids[index]);
        }
        private void UnlockClue(string clueId)
        {
            if (string.IsNullOrWhiteSpace(clueId) || !unlockedClues.Add(clueId)) return;
            ClueUnlocked?.Invoke(clueId);
        }
        private void SetPhase(LawyerSessionPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
