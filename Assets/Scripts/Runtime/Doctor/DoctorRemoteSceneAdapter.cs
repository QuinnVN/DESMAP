using System;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorRemoteSceneAdapter : MonoBehaviour, IGameSceneAdapter
    {
        [SerializeField] private DoctorGameManager gameManager;
        private string roundId = string.Empty;

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
            TelemetryRaised?.Invoke("doctor.round_started", CurrentRoundData());
        }

        private void OnCasePresented(DoctorCaseDefinition caseDefinition)
        {
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
            TelemetryRaised?.Invoke("doctor.case_submitted", new GameplayTelemetryData
            {
                roundId = roundId,
                caseId = caseDefinition != null ? caseDefinition.name : string.Empty,
                scoreDelta = scoreDelta,
                scoreTotal = scoreTotal,
                remainingTime = gameManager.RemainingRoundTime
            });
        }

        private void OnRoundEnded(int score)
        {
            GameplayTelemetryData data = CurrentRoundData();
            data.scoreTotal = score;
            TelemetryRaised?.Invoke("doctor.round_finished", data);
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
