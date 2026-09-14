using System;
using System.Collections.Generic;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Clinic
{
    [DisallowMultipleComponent]
    public sealed class ClinicRemoteSceneAdapter : MonoBehaviour, IGameSceneAdapter
    {
        [SerializeField] private ClinicGameManager gameManager;

        private readonly Dictionary<PatientActor, string> patientIds = new Dictionary<PatientActor, string>();
        private string roundId = string.Empty;
        private int patientSequence;

        public event Action<string, GameplayTelemetryData> TelemetryRaised;

        public string SceneId => "clinic";

        public RemoteGamePhase Phase
        {
            get
            {
                if (gameManager == null || gameManager.RoundState == ClinicRoundState.Idle)
                    return RemoteGamePhase.Ready;
                if (gameManager.RoundState == ClinicRoundState.Finished)
                    return RemoteGamePhase.Finished;
                return RemoteGamePhase.Running;
            }
        }

        private void OnEnable()
        {
            if (gameManager == null)
                return;

            gameManager.RoundStarted += OnRoundStarted;
            gameManager.PatientSpawned += OnPatientSpawned;
            gameManager.PatientResolvedTelemetry += OnPatientResolved;
            gameManager.RoundEnded += OnRoundEnded;
        }

        private void OnDisable()
        {
            if (gameManager == null)
                return;

            gameManager.RoundStarted -= OnRoundStarted;
            gameManager.PatientSpawned -= OnPatientSpawned;
            gameManager.PatientResolvedTelemetry -= OnPatientResolved;
            gameManager.RoundEnded -= OnRoundEnded;
        }

        public bool TryStart(out string errorCode)
        {
            if (gameManager == null)
            {
                errorCode = "missing_manager";
                return false;
            }
            if (gameManager.RoundState != ClinicRoundState.Idle)
            {
                // Gameplay scenes auto-start after loading. Treat the backend's
                // follow-up start command as idempotent while that round is active.
                if (gameManager.RoundState != ClinicRoundState.Finished)
                {
                    errorCode = string.Empty;
                    return true;
                }
                errorCode = "invalid_phase";
                return false;
            }

            errorCode = string.Empty;
            gameManager.StartRound();
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
            if (gameManager.RoundState == ClinicRoundState.Idle
                || gameManager.RoundState == ClinicRoundState.Finished)
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
            patientSequence = 0;
            patientIds.Clear();
            TelemetryRaised?.Invoke("clinic.round_started", CurrentRoundData());
        }

        private void OnPatientSpawned(PatientActor patient, int bedIndex)
        {
            string patientId = (++patientSequence).ToString();
            patientIds[patient] = patientId;
            TelemetryRaised?.Invoke("clinic.patient_spawned", new GameplayTelemetryData
            {
                roundId = roundId,
                patientId = patientId,
                sicknessName = patient.SicknessName,
                severity = patient.Severity,
                bedIndex = bedIndex,
                scoreTotal = gameManager.Score,
                remainingTime = gameManager.RemainingRoundTime
            });
        }

        private void OnPatientResolved(PatientActor patient, PatientResolution resolution, int scoreDelta, int scoreTotal)
        {
            patientIds.TryGetValue(patient, out string patientId);
            TelemetryRaised?.Invoke("clinic.patient_resolved", new GameplayTelemetryData
            {
                roundId = roundId,
                patientId = patientId,
                sicknessName = patient.SicknessName,
                severity = patient.Severity,
                resolution = resolution.ToString().ToLowerInvariant(),
                scoreDelta = scoreDelta,
                scoreTotal = scoreTotal,
                remainingTime = gameManager.RemainingRoundTime
            });
        }

        private void OnRoundEnded(int score)
        {
            GameplayTelemetryData data = CurrentRoundData();
            data.scoreTotal = score;
            TelemetryRaised?.Invoke("clinic.round_finished", data);
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
