using System;
using System.Collections.Generic;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorRemoteSceneAdapter : MonoBehaviour, IGameSceneAdapter
    {
        [SerializeField] private DoctorGameManager gameManager;
        private string roundId = string.Empty;
        private string roundStartedAtUtc = string.Empty;
        private string roundCompletedAtUtc = string.Empty;
        private readonly List<SelectedQuestionSnapshot> selectedQuestions = new List<SelectedQuestionSnapshot>();

        [Serializable]
        private sealed class SelectedQuestionSnapshot
        {
            public string questionId;
            public string text;
            public int selectionOrder;
        }

        [Serializable]
        private sealed class NoteSnapshot
        {
            public string cardId;
            public string text;
            public bool isEssential;
            public string selectedCategory;
            public string correctCategory;
            public bool isCorrect;
        }

        [Serializable]
        private sealed class SnapshotList<T>
        {
            public T[] items;
            public SnapshotList(List<T> values) => items = values.ToArray();
        }

        public event Action<string, GameplayTelemetryData> TelemetryRaised;

        public string SceneId => "doctor";

        public RemoteGamePhase Phase
        {
            get
            {
                if (gameManager == null || gameManager.State == DoctorGameState.Idle)
                    return RemoteGamePhase.Ready;
                if (gameManager.State == DoctorGameState.Finished)
                    return RemoteGamePhase.Finished;
                return RemoteGamePhase.Running;
            }
        }

        private void OnEnable()
        {
            if (gameManager == null)
                return;

            gameManager.RoundStarted += OnRoundStarted;
            gameManager.CasePresented += OnCasePresented;
            gameManager.QuestionAsked += OnQuestionAsked;
            gameManager.CardPlacementChanged += OnCardPlacementChanged;
            gameManager.CaseSubmitted += OnCaseSubmitted;
            gameManager.RoundEnded += OnRoundEnded;
        }

        private void OnDisable()
        {
            if (gameManager == null)
                return;

            gameManager.RoundStarted -= OnRoundStarted;
            gameManager.CasePresented -= OnCasePresented;
            gameManager.QuestionAsked -= OnQuestionAsked;
            gameManager.CardPlacementChanged -= OnCardPlacementChanged;
            gameManager.CaseSubmitted -= OnCaseSubmitted;
            gameManager.RoundEnded -= OnRoundEnded;
        }

        public bool TryStart(out string errorCode)
        {
            if (gameManager == null)
            {
                errorCode = "missing_manager";
                return false;
            }
            if (gameManager.State != DoctorGameState.Idle)
            {
                // Gameplay scenes auto-start after loading. Treat the backend's
                // follow-up start command as idempotent while that round is active.
                if (gameManager.State != DoctorGameState.Finished)
                {
                    errorCode = string.Empty;
                    return true;
                }
                errorCode = "invalid_phase";
                return false;
            }

            gameManager.StartRound();
            errorCode = string.Empty;
            return true;
        }

        public bool TryRestart(out string errorCode)
        {
            if (gameManager == null)
            {
                errorCode = "missing_manager";
                return false;
            }

            gameManager.StopRound();
            gameManager.StartRound();
            errorCode = string.Empty;
            TelemetryRaised?.Invoke("game.restarted", CurrentRoundData());
            return true;
        }

        public bool TryStop(out string errorCode)
        {
            if (gameManager == null)
            {
                errorCode = "missing_manager";
                return false;
            }
            if (gameManager.State == DoctorGameState.Idle || gameManager.State == DoctorGameState.Finished)
            {
                errorCode = "invalid_phase";
                return false;
            }

            gameManager.StopRound();
            errorCode = string.Empty;
            TelemetryRaised?.Invoke("game.stopped", CurrentRoundData());
            return true;
        }

        private void OnRoundStarted()
        {
            roundId = Guid.NewGuid().ToString("N");
            roundStartedAtUtc = GameControlApi.UtcNow();
            roundCompletedAtUtc = string.Empty;
            selectedQuestions.Clear();
            TelemetryRaised?.Invoke("doctor.round_started", CurrentRoundData());
        }

        private void OnCasePresented(DoctorCaseDefinition caseDefinition)
        {
            selectedQuestions.Clear();
            TelemetryRaised?.Invoke("doctor.case_presented", new GameplayTelemetryData
            {
                roundId = roundId,
                caseId = caseDefinition != null ? caseDefinition.name : string.Empty,
                scoreTotal = gameManager.Score,
                remainingTime = gameManager.RemainingRoundTime
            });
        }

        private void OnQuestionAsked(string questionId)
        {
            GameplayTelemetryData data = CurrentCaseData();
            data.questionId = questionId;
            DoctorQuestionPhase phase = gameManager != null && gameManager.CurrentCase != null
                ? gameManager.CurrentCase.GetPhase(questionId) : null;
            selectedQuestions.Add(new SelectedQuestionSnapshot
            {
                questionId = questionId ?? string.Empty,
                text = phase != null ? phase.Question : string.Empty,
                selectionOrder = selectedQuestions.Count + 1
            });
            data.selectedQuestionsJson = JsonUtility.ToJson(new SnapshotList<SelectedQuestionSnapshot>(selectedQuestions));
            TelemetryRaised?.Invoke("doctor.question_asked", data);
        }

        private void OnCardPlacementChanged(string cardId, DoctorInfoCategory? category)
        {
            GameplayTelemetryData data = CurrentCaseData();
            data.cardId = cardId;
            data.category = category.HasValue ? category.Value.ToString().ToLowerInvariant() : "unassigned";
            TelemetryRaised?.Invoke("doctor.card_placement_changed", data);
        }

        private void OnCaseSubmitted(DoctorCaseDefinition caseDefinition, int scoreDelta, int scoreTotal)
        {
            List<NoteSnapshot> notes = new List<NoteSnapshot>();
            int correct = 0, incorrect = 0, unassigned = 0;
            int essentialCorrect = 0, essentialIncorrect = 0, essentialUnassigned = 0;
            if (gameManager != null && gameManager.RevealedCards != null)
            {
                for (int i = 0; i < gameManager.RevealedCards.Count; i++)
                {
                    DoctorInfoCard card = gameManager.RevealedCards[i];
                    if (card == null) continue;
                    gameManager.Placements.TryGetValue(card.Id, out DoctorInfoCategory? selected);
                    bool isCorrect = selected.HasValue && selected.Value == card.CorrectCategory;
                    if (!selected.HasValue) unassigned++; else if (isCorrect) correct++; else incorrect++;
                    if (card.IsEssential)
                    {
                        if (!selected.HasValue) essentialUnassigned++;
                        else if (isCorrect) essentialCorrect++;
                        else essentialIncorrect++;
                    }
                    notes.Add(new NoteSnapshot
                    {
                        cardId = card.Id, text = card.Text, isEssential = card.IsEssential,
                        selectedCategory = selected.HasValue ? selected.Value.ToString().ToLowerInvariant() : null,
                        correctCategory = card.CorrectCategory.ToString().ToLowerInvariant(), isCorrect = isCorrect
                    });
                }
            }
            GameplayTelemetryData data = new GameplayTelemetryData
            {
                roundId = roundId,
                caseId = caseDefinition != null ? caseDefinition.name : string.Empty,
                scoreDelta = scoreDelta,
                scoreTotal = scoreTotal,
                remainingTime = gameManager.RemainingRoundTime,
                patientName = caseDefinition != null ? caseDefinition.PatientName : string.Empty,
                selectedQuestionsJson = JsonUtility.ToJson(new SnapshotList<SelectedQuestionSnapshot>(selectedQuestions)),
                notesJson = JsonUtility.ToJson(new SnapshotList<NoteSnapshot>(notes)),
                correctCount = correct, incorrectCount = incorrect, unassignedCount = unassigned,
                essentialCorrectCount = essentialCorrect, essentialIncorrectCount = essentialIncorrect,
                essentialUnassignedCount = essentialUnassigned,
                categorizationAccuracyPercent = Accuracy(correct, incorrect, unassigned),
                essentialCategorizationAccuracyPercent = Accuracy(essentialCorrect, essentialIncorrect, essentialUnassigned),
                caseSubmittedAtUtc = GameControlApi.UtcNow(),
                fragmentId = roundId + ":case:" + (caseDefinition != null ? caseDefinition.name : string.Empty)
            };
            TelemetryRaised?.Invoke("doctor.case_submitted", data);
        }

        private void OnRoundEnded(int score)
        {
            GameplayTelemetryData data = CurrentRoundData();
            data.scoreTotal = score;
            roundCompletedAtUtc = GameControlApi.UtcNow();
            data.roundStartedAtUtc = roundStartedAtUtc;
            data.roundCompletedAtUtc = roundCompletedAtUtc;
            data.fragmentId = roundId + ":round";
            TelemetryRaised?.Invoke("doctor.round_finished", data);
        }

        private static string Accuracy(int correct, int incorrect, int unassigned)
        {
            int total = correct + incorrect + unassigned;
            return total == 0 ? "null" : (correct * 100f / total).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }

        private GameplayTelemetryData CurrentCaseData()
        {
            GameplayTelemetryData data = CurrentRoundData();
            data.caseId = gameManager != null && gameManager.CurrentCase != null
                ? gameManager.CurrentCase.name
                : string.Empty;
            return data;
        }

        private GameplayTelemetryData CurrentRoundData()
        {
            return new GameplayTelemetryData
            {
                roundId = roundId,
                scoreTotal = gameManager != null ? gameManager.Score : 0,
                remainingTime = gameManager != null ? gameManager.RemainingRoundTime : 0f
            };
        }
    }
}
