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
        FollowUpListening,
        Diagnosing,
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

        [Header("Round")]
        [SerializeField, Min(0f)] private float preparationDuration = 10f;
        [SerializeField, Min(0f)] private float roundDuration = 300f;
        [SerializeField] private bool startAutomatically = true;

        [Header("Patient Flow")]
        [SerializeField, Min(0.01f)] private float dialogueFallbackDuration = 4f;
        [SerializeField, Min(0f)] private float conclusionDisplayDuration = 3f;
        [SerializeField, Min(0f)] private float patientTransitionDelay = 1f;
        [SerializeField, Min(0)] private int maximumQuestionsPerPatient = 2;

        [Header("Scoring")]
        [SerializeField, Min(0)] private int pointsPerCorrectInfo = 1;

        private readonly List<DoctorCaseDefinition> caseQueue = new List<DoctorCaseDefinition>();
        private readonly List<DoctorInfoCard> revealedCards = new List<DoctorInfoCard>();
        private readonly HashSet<string> revealedCardIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, DoctorInfoCategory?> placements =
            new Dictionary<string, DoctorInfoCategory?>(StringComparer.Ordinal);

        private DoctorQuestionTracker questionTracker;
        private DoctorCaseDefinition currentCase;
        private DoctorCaseDefinition lastCase;
        private int caseQueueIndex;
        private float stateCountdown;
        private float preparationRemaining;

        public event Action<int> RoundEnded;

        public DoctorGameState State { get; private set; } = DoctorGameState.Idle;
        public float RemainingRoundTime { get; private set; }
        public float RemainingPreparationTime => preparationRemaining;
        public int Score { get; private set; }
        public DoctorCaseDefinition CurrentCase => currentCase;
        public IReadOnlyList<DoctorInfoCard> RevealedCards => revealedCards;
        public IReadOnlyDictionary<string, DoctorInfoCategory?> Placements => placements;

        private void Awake()
        {
            questionTracker = new DoctorQuestionTracker(maximumQuestionsPerPatient);
        }

        private void OnEnable()
        {
            if (notebookInteractable != null)
                notebookInteractable.Activated += OnNotebookActivated;

            if (notebookUI != null)
            {
                notebookUI.CloseRequested += OnNotebookCloseRequested;
                notebookUI.QuestionRequested += OnQuestionRequested;
                notebookUI.DiagnoseRequested += OnDiagnoseRequested;
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
                notebookUI.QuestionRequested -= OnQuestionRequested;
                notebookUI.DiagnoseRequested -= OnDiagnoseRequested;
                notebookUI.PlacementChanged -= OnPlacementChanged;
            }
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

            if (State != DoctorGameState.Listening
                && State != DoctorGameState.FollowUpListening
                && State != DoctorGameState.Diagnosing
                && State != DoctorGameState.Transitioning)
            {
                return;
            }

            stateCountdown = Mathf.Max(0f, stateCountdown - deltaTime);
            if (stateCountdown > 0f)
                return;

            switch (State)
            {
                case DoctorGameState.Listening:
                case DoctorGameState.FollowUpListening:
                    EnterArrangingState();
                    break;
                case DoctorGameState.Diagnosing:
                    BeginTransition();
                    break;
                case DoctorGameState.Transitioning:
                    BeginNextCase();
                    break;
            }
        }

        public void StartRound()
        {
            Score = 0;
            RemainingRoundTime = Mathf.Max(0f, roundDuration);
            preparationRemaining = Mathf.Max(0f, preparationDuration);
            currentCase = null;
            revealedCards.Clear();
            revealedCardIds.Clear();
            placements.Clear();
            caseQueue.Clear();
            caseQueueIndex = 0;
            patientPresenter?.SetVisible(false);
            notebookInteractable?.SetInteractionEnabled(false);

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
                Debug.LogError(
                    "DoctorGameManager requires at least one named case with a positive selection weight.",
                    this);
                FinishRound();
                return;
            }

            revealedCards.Clear();
            revealedCardIds.Clear();
            placements.Clear();
            questionTracker.Reset(maximumQuestionsPerPatient);
            AddRevealedCards(currentCase.InitialDialogue);
            notebookInteractable?.SetInteractionEnabled(false);
            notebookUI?.CloseNotebook();
            notebookUI?.SetNotebookHint("Listen to the patient, then select the notebook.");
            stateCountdown = patientPresenter != null
                ? patientPresenter.PresentDialogue(
                    currentCase.PatientName,
                    currentCase.InitialDialogue,
                    dialogueFallbackDuration)
                : Mathf.Max(0.01f, dialogueFallbackDuration);
            State = DoctorGameState.Listening;
        }

        private void EnterArrangingState()
        {
            State = DoctorGameState.Arranging;
            notebookInteractable?.SetInteractionEnabled(true);
            notebookUI?.SetNotebookHint("Select the notebook to arrange the patient notes.");
        }

        private void OnNotebookActivated()
        {
            if (State != DoctorGameState.Arranging || currentCase == null || notebookUI == null)
                return;

            patientPresenter?.SetDialogueVisible(false);
            notebookUI.OpenNotebook(
                currentCase.PatientName,
                revealedCards,
                placements,
                questionTracker);
        }

        private void OnNotebookCloseRequested()
        {
            if (State == DoctorGameState.Arranging)
            {
                notebookUI?.CloseNotebook();
                patientPresenter?.SetDialogueVisible(true);
            }
        }

        private void OnQuestionRequested(DoctorInfoCategory category)
        {
            if (State != DoctorGameState.Arranging
                || currentCase == null
                || !questionTracker.TryAsk(category))
            {
                return;
            }

            DoctorDialogue response = currentCase.GetFollowUp(category);
            AddRevealedCards(response);
            notebookUI?.CloseNotebook();
            notebookInteractable?.SetInteractionEnabled(false);
            stateCountdown = patientPresenter != null
                ? patientPresenter.PresentDialogue(
                    currentCase.PatientName,
                    response,
                    dialogueFallbackDuration)
                : Mathf.Max(0.01f, dialogueFallbackDuration);
            State = DoctorGameState.FollowUpListening;
        }

        private void OnDiagnoseRequested()
        {
            if (State != DoctorGameState.Arranging || currentCase == null)
                return;

            int correctNotes = DoctorGameRules.CountCorrectNotes(revealedCards, placements);
            Score += correctNotes * Mathf.Max(0, pointsPerCorrectInfo);
            notebookInteractable?.SetInteractionEnabled(false);
            notebookUI?.CloseNotebook();
            patientPresenter?.SetDialogueVisible(false);
            notebookUI?.ShowConclusion(currentCase.Conclusion, currentCase.Treatment);
            patientPresenter?.ShowConclusion(currentCase.Conclusion);
            stateCountdown = Mathf.Max(0f, conclusionDisplayDuration);
            State = DoctorGameState.Diagnosing;

            if (stateCountdown <= 0f)
                BeginTransition();
        }

        private void OnPlacementChanged(string cardId, DoctorInfoCategory? category)
        {
            if (State != DoctorGameState.Arranging
                || string.IsNullOrWhiteSpace(cardId)
                || !revealedCardIds.Contains(cardId))
            {
                return;
            }

            placements[cardId] = category;
            notebookUI?.RefreshNotebook(revealedCards, placements, questionTracker);
        }

        private void BeginTransition()
        {
            notebookUI?.HideConclusion();
            patientPresenter?.SetVisible(false);
            stateCountdown = Mathf.Max(0f, patientTransitionDelay);
            State = DoctorGameState.Transitioning;

            if (stateCountdown <= 0f)
                BeginNextCase();
        }

        private void FinishRound()
        {
            if (State == DoctorGameState.Finished)
                return;

            State = DoctorGameState.Finished;
            RemainingRoundTime = 0f;
            notebookInteractable?.SetInteractionEnabled(false);
            patientPresenter?.SetVisible(false);
            notebookUI?.ShowFinished(Score);
            RoundEnded?.Invoke(Score);
        }

        private void AddRevealedCards(DoctorDialogue dialogue)
        {
            if (dialogue == null || dialogue.SelectableCards == null)
                return;

            IReadOnlyList<DoctorInfoCard> cards = dialogue.SelectableCards;
            for (int i = 0; i < cards.Count; i++)
            {
                DoctorInfoCard card = cards[i];
                if (card == null
                    || string.IsNullOrWhiteSpace(card.Id)
                    || !revealedCardIds.Add(card.Id))
                {
                    continue;
                }

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

            if (caseDatabase == null || caseDatabase.Cases == null)
                return;

            List<DoctorCaseDefinition> candidates = new List<DoctorCaseDefinition>();
            IReadOnlyList<DoctorCaseDefinition> configuredCases = caseDatabase.Cases;
            for (int i = 0; i < configuredCases.Count; i++)
            {
                DoctorCaseDefinition candidate = configuredCases[i];
                if (candidate != null && candidate.IsSelectable)
                    candidates.Add(candidate);
            }

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
                    if (roll <= 0f)
                    {
                        chosenIndex = i;
                        break;
                    }
                }

                caseQueue.Add(candidates[chosenIndex]);
                candidates.RemoveAt(chosenIndex);
            }

            if (caseQueue.Count > 1 && caseQueue[0] == lastCase)
            {
                DoctorCaseDefinition swap = caseQueue[0];
                caseQueue[0] = caseQueue[1];
                caseQueue[1] = swap;
            }
        }

        private void OnValidate()
        {
            preparationDuration = Mathf.Max(0f, preparationDuration);
            roundDuration = Mathf.Max(0f, roundDuration);
            dialogueFallbackDuration = Mathf.Max(0.01f, dialogueFallbackDuration);
            conclusionDisplayDuration = Mathf.Max(0f, conclusionDisplayDuration);
            patientTransitionDelay = Mathf.Max(0f, patientTransitionDelay);
            maximumQuestionsPerPatient = Mathf.Max(0, maximumQuestionsPerPatient);
            pointsPerCorrectInfo = Mathf.Max(0, pointsPerCorrectInfo);
        }
    }
}
