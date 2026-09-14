using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    public enum DoctorGameState
    {
        Idle,
        Preparing,
        Listening,
        Arranging,
        QuestionSelecting,
        Transitioning,
        Finished
    }

    [DisallowMultipleComponent]
    public sealed class DoctorGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DoctorCaseDatabase caseDatabase;
        [SerializeField] private DoctorPatientPresenter patientPresenter;
        [SerializeField] private DoctorNotebookInteractable notebookInteractable;
        [SerializeField] private DoctorNotebookUI notebookUI;
        [SerializeField] private GameObject locomotionRoot;
        [SerializeField] private GameObject moveProviderRoot;
        [SerializeField] private GameObject gravityProviderRoot;
        [SerializeField] private GameObject jumpProviderRoot;

        [Header("Round")]
        [SerializeField, Min(0f)] private float preparationDuration = 10f;
        [SerializeField, Min(0f)] private float roundDuration = 300f;
        [SerializeField] private bool startAutomatically = true;

        [Header("Patient Flow")]
        [SerializeField, Min(0.01f)] private float dialogueFallbackDuration = 4f;
        [SerializeField, Min(0f)] private float patientTransitionDelay = 1f;

        [Header("Scoring")]
        [SerializeField, Min(0)] private int pointsPerCorrectInfo = 1;

        private readonly List<DoctorCaseDefinition> caseQueue = new List<DoctorCaseDefinition>();
        private readonly List<DoctorInfoCard> revealedCards = new List<DoctorInfoCard>();
        private readonly HashSet<string> revealedCardIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, DoctorInfoCategory?> placements =
            new Dictionary<string, DoctorInfoCategory?>(StringComparer.Ordinal);
        private readonly List<DoctorQuestionPhase> remainingQuestions = new List<DoctorQuestionPhase>();
        private readonly DoctorQuestionTracker questionTracker = new DoctorQuestionTracker();

        private DoctorCaseDefinition currentCase;
        private DoctorCaseDefinition lastCase;
        private int caseQueueIndex;
        private int preferredQuestionIndex;
        private float stateCountdown;
        private float preparationRemaining;
        private bool moveWasActive;
        private bool gravityWasActive;
        private bool jumpWasActive;
        private bool locomotionWasActive;
        private bool locomotionStateCaptured;

        public event Action<int> RoundEnded;
        public event Action RoundStarted;
        public event Action<DoctorCaseDefinition> CasePresented;
        public event Action<string> QuestionAsked;
        public event Action<string, DoctorInfoCategory?> CardPlacementChanged;
        public event Action<DoctorCaseDefinition, int, int> CaseSubmitted;

        public DoctorGameState State { get; private set; } = DoctorGameState.Idle;
        public float RemainingRoundTime { get; private set; }
        public float RemainingPreparationTime => preparationRemaining;
        public int Score { get; private set; }
        public DoctorCaseDefinition CurrentCase => currentCase;
        public IReadOnlyList<DoctorInfoCard> RevealedCards => revealedCards;
        public IReadOnlyDictionary<string, DoctorInfoCategory?> Placements => placements;

        private void OnEnable()
        {
            if (notebookInteractable != null)
                notebookInteractable.Activated += OnNotebookActivated;

            if (notebookUI != null)
            {
                notebookUI.CloseRequested += OnNotebookCloseRequested;
                notebookUI.DoneRequested += OnDoneRequested;
                notebookUI.QuestionRequested += OnQuestionRequested;
                notebookUI.PlacementChanged += OnPlacementChanged;
            }
        }

        private void OnDisable()
        {
            if (notebookInteractable != null)
                notebookInteractable.Activated -= OnNotebookActivated;

            if (notebookUI != null)
            {
                notebookUI.CloseRequested -= OnNotebookCloseRequested;
                notebookUI.DoneRequested -= OnDoneRequested;
                notebookUI.QuestionRequested -= OnQuestionRequested;
                notebookUI.PlacementChanged -= OnPlacementChanged;
            }

            SetNotebookInteractionEnabled(false);
            RestoreLocomotion();
        }

        private IEnumerator Start()
        {
            yield return null;
            if (startAutomatically && State == DoctorGameState.Idle)
                StartRound();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (State == DoctorGameState.Preparing)
            {
                preparationRemaining = Mathf.Max(0f, preparationRemaining - deltaTime);
                notebookUI?.ShowPreparation(preparationRemaining);
                if (preparationRemaining <= 0f)
                    BeginTimedRound();
                return;
            }

            if (State == DoctorGameState.Idle || State == DoctorGameState.Finished)
                return;

            RemainingRoundTime = Mathf.Max(0f, RemainingRoundTime - deltaTime);
            notebookUI?.SetRoundTime(RemainingRoundTime);
            if (RemainingRoundTime <= 0f)
            {
                FinishRound();
                return;
            }

            if (State != DoctorGameState.Listening && State != DoctorGameState.Transitioning)
                return;

            stateCountdown = Mathf.Max(0f, stateCountdown - deltaTime);
            if (stateCountdown > 0f)
                return;

            if (State == DoctorGameState.Listening)
                EnterArrangingState();
            else
                BeginNextCase();
        }

        public void StartRound()
        {
            CaptureAndDisableLocomotion();
            Score = 0;
            RemainingRoundTime = Mathf.Max(0f, roundDuration);
            preparationRemaining = Mathf.Max(0f, preparationDuration);
            currentCase = null;
            lastCase = null;
            caseQueue.Clear();
            caseQueueIndex = 0;
            ClearPatientNotes();
            patientPresenter?.SetVisible(false);
            SetNotebookInteractionEnabled(false);
            RoundStarted?.Invoke();

            if (preparationRemaining > 0f)
            {
                State = DoctorGameState.Preparing;
                notebookUI?.ShowPreparation(preparationRemaining);
            }
            else
            {
                BeginTimedRound();
            }
        }

        public void StopRound()
        {
            if (State != DoctorGameState.Idle && State != DoctorGameState.Finished)
                FinishRound();
        }

        private void BeginTimedRound()
        {
            EnableTurningOnly();
            RemainingRoundTime = Mathf.Max(0f, roundDuration);
            notebookUI?.ShowRunning();
            notebookUI?.SetRoundTime(RemainingRoundTime);
            BeginNextCase();
        }

        private void BeginNextCase()
        {
            currentCase = GetNextCase();
            if (currentCase == null)
            {
                Debug.LogError("DoctorGameManager requires at least one selectable case.", this);
                FinishRound();
                return;
            }

            ClearPatientNotes();
            AddRevealedCards(currentCase.InitialDialogue);
            SetNotebookInteractionEnabled(false);
            notebookUI?.CloseNotebook();
            notebookUI?.HideQuestionHud();
            notebookUI?.ShowDialogue(currentCase.PatientName, currentCase.InitialDialogue?.Transcript,
                "Hãy lắng nghe bệnh nhân. Sổ ghi chú sẽ tự động mở sau đó.");
            stateCountdown = patientPresenter != null
                ? patientPresenter.PresentDialogue(currentCase.InitialDialogue, dialogueFallbackDuration)
                : Mathf.Max(0.01f, dialogueFallbackDuration);
            State = DoctorGameState.Listening;
            CasePresented?.Invoke(currentCase);
        }

        private void EnterArrangingState()
        {
            State = DoctorGameState.Arranging;
            SetNotebookInteractionEnabled(true);
            notebookUI?.SetHudHint("B: đóng sổ ghi chú và xem câu hỏi");
            notebookUI?.SetNotebookHint("Kéo các chi tiết vào những mục bên trái, hoặc chọn một thẻ rồi chọn mục.");
            notebookUI?.OpenNotebook(currentCase.PatientName, revealedCards, placements);
        }

        private void OnNotebookActivated()
        {
            if ((State != DoctorGameState.Arranging && State != DoctorGameState.QuestionSelecting)
                || currentCase == null || notebookUI == null || notebookUI.IsNotebookOpen)
                return;

            if (State == DoctorGameState.QuestionSelecting)
                preferredQuestionIndex = Mathf.Max(0, notebookUI.SelectedQuestionIndex);
            State = DoctorGameState.Arranging;
            notebookUI.HideQuestionHud();
            notebookUI.OpenNotebook(currentCase.PatientName, revealedCards, placements);
        }

        private void OnNotebookCloseRequested()
        {
            if (State != DoctorGameState.Arranging || currentCase == null)
                return;

            notebookUI?.CloseNotebook();
            notebookUI?.SetDialogueVisible(true);
            RefreshRemainingQuestions();
            if (remainingQuestions.Count > 0)
            {
                notebookUI?.ShowQuestionHud(remainingQuestions, preferredQuestionIndex);
                notebookUI?.SetHudHint("Cần trái: chọn câu hỏi. A: hỏi. Dùng tia chọn màn hình để mở lại ghi chú.");
                State = DoctorGameState.QuestionSelecting;
            }
            else
            {
                notebookUI?.SetNotebookHint("Không còn câu hỏi. Hãy phân loại chi tiết rồi nhấn Hoàn tất.");
                notebookUI?.SetHudHint("Không còn câu hỏi. Dùng tia chọn màn hình để mở lại ghi chú, rồi nhấn Hoàn tất.");
            }
        }

        private void OnQuestionRequested(string questionId)
        {
            if (State != DoctorGameState.QuestionSelecting || currentCase == null || !questionTracker.TryAsk(questionId))
                return;

            preferredQuestionIndex = Mathf.Max(0, notebookUI != null ? notebookUI.SelectedQuestionIndex : 0);
            DoctorQuestionPhase phase = currentCase.GetPhase(questionId);
            if (phase == null)
                return;

            AddRevealedCards(phase.Response);
            RefreshRemainingQuestions();
            preferredQuestionIndex = Mathf.Clamp(preferredQuestionIndex, 0, Mathf.Max(0, remainingQuestions.Count - 1));
            notebookUI?.HideQuestionHud();
            SetNotebookInteractionEnabled(false);
            notebookUI?.ShowDialogue(currentCase.PatientName, phase.Response?.Transcript,
                "Hãy lắng nghe câu trả lời. Sổ ghi chú sẽ tự động mở sau đó.");
            stateCountdown = patientPresenter != null
                ? patientPresenter.PresentDialogue(phase.Response, dialogueFallbackDuration)
                : Mathf.Max(0.01f, dialogueFallbackDuration);
            State = DoctorGameState.Listening;
            QuestionAsked?.Invoke(questionId);
        }

        private void OnDoneRequested()
        {
            if (State != DoctorGameState.Arranging || currentCase == null)
                return;

            int scoreDelta = DoctorGameRules.CountCorrectNotes(revealedCards, placements)
                * Mathf.Max(0, pointsPerCorrectInfo);
            Score += scoreDelta;
            CaseSubmitted?.Invoke(currentCase, scoreDelta, Score);
            SetNotebookInteractionEnabled(false);
            notebookUI?.CloseNotebook();
            notebookUI?.HideQuestionHud();
            patientPresenter?.SetVisible(false);
            stateCountdown = Mathf.Max(0f, patientTransitionDelay);
            State = DoctorGameState.Transitioning;
            if (stateCountdown <= 0f)
                BeginNextCase();
        }

        private void OnPlacementChanged(string cardId, DoctorInfoCategory? category)
        {
            if (State != DoctorGameState.Arranging || string.IsNullOrWhiteSpace(cardId) || !revealedCardIds.Contains(cardId))
                return;

            placements[cardId] = category;
            CardPlacementChanged?.Invoke(cardId, category);
            notebookUI?.RefreshNotebook(revealedCards, placements);
        }

        private void FinishRound()
        {
            if (State == DoctorGameState.Finished)
                return;

            State = DoctorGameState.Finished;
            RemainingRoundTime = 0f;
            SetNotebookInteractionEnabled(false);
            patientPresenter?.SetVisible(false);
            notebookUI?.ShowFinished(Score);
            RoundEnded?.Invoke(Score);
        }

        private void ClearPatientNotes()
        {
            revealedCards.Clear();
            revealedCardIds.Clear();
            placements.Clear();
            remainingQuestions.Clear();
            questionTracker.Reset();
            preferredQuestionIndex = 0;
        }

        private void RefreshRemainingQuestions()
        {
            questionTracker.GetRemaining(currentCase?.Phases, remainingQuestions);
        }

        private void AddRevealedCards(DoctorDialogue dialogue)
        {
            IReadOnlyList<DoctorInfoCard> cards = dialogue?.SelectableCards;
            if (cards == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                DoctorInfoCard card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id) || !revealedCardIds.Add(card.Id))
                    continue;
                revealedCards.Add(card);
                placements.Add(card.Id, null);
            }
        }

        private DoctorCaseDefinition GetNextCase()
        {
            if (caseQueueIndex >= caseQueue.Count)
                RebuildCaseQueue();
            if (caseQueue.Count == 0)
                return null;
            DoctorCaseDefinition selected = caseQueue[caseQueueIndex++];
            lastCase = selected;
            return selected;
        }

        private void RebuildCaseQueue()
        {
            caseQueue.Clear();
            caseQueueIndex = 0;
            IReadOnlyList<DoctorCaseDefinition> configuredCases = caseDatabase?.Cases;
            if (configuredCases == null)
                return;

            List<DoctorCaseDefinition> candidates = new List<DoctorCaseDefinition>();
            for (int i = 0; i < configuredCases.Count; i++)
                if (configuredCases[i] != null && configuredCases[i].IsSelectable)
                    candidates.Add(configuredCases[i]);

            while (candidates.Count > 0)
            {
                float totalWeight = 0f;
                for (int i = 0; i < candidates.Count; i++)
                    totalWeight += candidates[i].SelectionWeight;
                float roll = UnityEngine.Random.value * totalWeight;
                int chosenIndex = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= candidates[i].SelectionWeight;
                    if (roll <= 0f) { chosenIndex = i; break; }
                }
                caseQueue.Add(candidates[chosenIndex]);
                candidates.RemoveAt(chosenIndex);
            }

            if (caseQueue.Count > 1 && caseQueue[0] == lastCase)
                (caseQueue[0], caseQueue[1]) = (caseQueue[1], caseQueue[0]);
        }

        private void CaptureAndDisableLocomotion()
        {
            if (!locomotionStateCaptured)
            {
                locomotionWasActive = locomotionRoot != null && locomotionRoot.activeSelf;
                moveWasActive = moveProviderRoot != null && moveProviderRoot.activeSelf;
                gravityWasActive = gravityProviderRoot != null && gravityProviderRoot.activeSelf;
                jumpWasActive = jumpProviderRoot != null && jumpProviderRoot.activeSelf;
                locomotionStateCaptured = true;
            }

            if (locomotionRoot != null)
            {
                locomotionRoot.SetActive(false);
                return;
            }

            moveProviderRoot?.SetActive(false);
            gravityProviderRoot?.SetActive(false);
            jumpProviderRoot?.SetActive(false);
        }

        private void EnableTurningOnly()
        {
            if (!locomotionStateCaptured)
                return;

            if (locomotionRoot != null)
                locomotionRoot.SetActive(locomotionWasActive);
            moveProviderRoot?.SetActive(false);
            gravityProviderRoot?.SetActive(false);
            jumpProviderRoot?.SetActive(false);
        }

        private void SetNotebookInteractionEnabled(bool enabled)
        {
            notebookInteractable?.SetInteractionEnabled(enabled);
        }

        private void RestoreLocomotion()
        {
            if (!locomotionStateCaptured)
                return;

            if (locomotionRoot != null)
                locomotionRoot.SetActive(locomotionWasActive);
            if (moveProviderRoot != null)
                moveProviderRoot.SetActive(moveWasActive);
            if (gravityProviderRoot != null)
                gravityProviderRoot.SetActive(gravityWasActive);
            if (jumpProviderRoot != null)
                jumpProviderRoot.SetActive(jumpWasActive);
            locomotionStateCaptured = false;
        }

        private void OnValidate()
        {
            preparationDuration = Mathf.Max(0f, preparationDuration);
            roundDuration = Mathf.Max(0f, roundDuration);
            dialogueFallbackDuration = Mathf.Max(0.01f, dialogueFallbackDuration);
            patientTransitionDelay = Mathf.Max(0f, patientTransitionDelay);
            pointsPerCorrectInfo = Mathf.Max(0, pointsPerCorrectInfo);
        }
    }
}
