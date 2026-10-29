using System;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Sales
{
    /// <summary>
    /// Bridges the two Sales parts to remote start/restart/stop commands. Part 2
    /// is enabled only after Part 1 has persisted its recording successfully.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalesRemoteSceneAdapter : MonoBehaviour, IGameSceneAdapter
    {
        public SalesMissionController part1;
        public SalesPart2Controller part2;
        [SerializeField] private string sceneId = "sale";

        private bool started;
        private bool stopped;
        private string salesSessionId = string.Empty;
        private string part1AttemptId = string.Empty;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private void Start() { if (part2 != null && part2.xrOrigin != null) { initialPosition = part2.xrOrigin.position; initialRotation = part2.xrOrigin.rotation; } }

        public string SceneId => string.IsNullOrWhiteSpace(sceneId) ? "sale" : sceneId.Trim();
        public event Action<string, GameplayTelemetryData> TelemetryRaised;

        public RemoteGamePhase Phase
        {
            get
            {
                if (stopped)
                    return RemoteGamePhase.Finished;
                if (!started || (part1 != null && part1.enabled && part1.Stage == SalesMissionStage.Waiting))
                    return RemoteGamePhase.Ready;
                if (part2 != null && part2.IsFinished)
                    return RemoteGamePhase.Finished;
                if (part1 != null && part1.Stage == SalesMissionStage.Completed
                    && (part2 == null || part2.Stage == SalesPart2Stage.Waiting))
                    return RemoteGamePhase.Running;
                return RemoteGamePhase.Running;
            }
        }

        private void OnEnable()
        {
            if (part1 != null)
            {
                part1.SalesSessionStarted += OnSalesSessionStarted;
                part1.Part1RecordingUploaded += OnPart1RecordingUploaded;
            }
            if (part2 != null)
                part2.TelemetryRaised += OnPart2Telemetry;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (part2 != null && part2.requestService is ISalesPart2RequestCancellation cancel) cancel.CancelAll();
            if (part1 != null)
            {
                part1.SalesSessionStarted -= OnSalesSessionStarted;
                part1.Part1RecordingUploaded -= OnPart1RecordingUploaded;
            }
            if (part2 != null)
                part2.TelemetryRaised -= OnPart2Telemetry;
        }

        public bool TryStart(out string errorCode)
        {
            errorCode = string.Empty;
            if (part1 == null)
            {
                errorCode = "missing_part1";
                return false;
            }
            if (Phase == RemoteGamePhase.Running && !stopped)
            {
                errorCode = "invalid_phase";
                return false;
            }
            stopped = false;
            started = true;
            if (part1.enabled) part1.ResetMission();
            else part1.enabled = true;
            salesSessionId = part1.SalesSessionId;
            part1AttemptId = string.Empty;
            part1.BeginDialogue();
            Emit("sales.started", CurrentData());
            return true;
        }

        public bool TryRestart(out string errorCode)
        {
            errorCode = string.Empty;
            stopped = false;
            started = true;
            if (part2 != null)
            {
                part2.StopPart2();
                part2.ResetPart2();
                part2.enabled = false;
            }
            if (part1 == null)
            {
                errorCode = "missing_part1";
                return false;
            }
            if (part2 != null && part2.xrOrigin != null) part2.xrOrigin.SetPositionAndRotation(initialPosition, initialRotation);
            if (part1.customer != null) part1.customer.gameObject.SetActive(true);
            if (part1.enabled) part1.ResetMission();
            else part1.enabled = true;
            salesSessionId = part1.SalesSessionId;
            part1AttemptId = string.Empty;
            Emit("game.restarted", CurrentData());
            return true;
        }

        public bool TryStop(out string errorCode)
        {
            errorCode = string.Empty;
            if (!started || stopped)
            {
                errorCode = "invalid_phase";
                return false;
            }
            StopAllCoroutines();
            if (part2 != null)
                part2.StopPart2();
            if (part1 != null) part1.enabled = false;
            stopped = true;
            Emit("game.stopped", CurrentData());
            return true;
        }

        private void OnSalesSessionStarted(string id)
        {
            salesSessionId = id ?? string.Empty;
            started = true;
            if (part2 != null && part2.requestService == null) part2.requestService = part2.GetComponent<SalesPart2RequestService>();
            if (part2 != null && part2.requestService != null) StartCoroutine(part2.requestService.StartOrResumeSession(new SalesPart2StartSessionRequest { salesSessionId = salesSessionId }, _ => { }));
            Emit("sales.session_started", CurrentData());
        }

        private void OnPart1RecordingUploaded(string attemptId)
        {
            part1AttemptId = attemptId ?? string.Empty;
            if (part1 != null && string.IsNullOrWhiteSpace(salesSessionId))
                salesSessionId = part1.SalesSessionId;
            Emit("sales.part1_recording_uploaded", CurrentData());
            if (part2 == null)
                return;
            part1.enabled = false;
            if (part1.customer != null) part1.customer.gameObject.SetActive(false);
            if (part1.objectiveText != null) part1.objectiveText.gameObject.SetActive(false);
            part2.enabled = true;
            part2.BeginFromPart1(salesSessionId, part1AttemptId);
        }

        private void OnPart2Telemetry(string eventType, GameplayTelemetryData data)
        {
            Emit(eventType, data);
        }

        private void Emit(string eventType, GameplayTelemetryData data)
        {
            TelemetryRaised?.Invoke(eventType, data ?? CurrentData());
        }

        private GameplayTelemetryData CurrentData()
        {
            string selected = string.Empty;
            string bestFit = string.Empty;
            if (part1 != null && part1.data != null && part1.SelectedIndex >= 0
                && part1.SelectedIndex < part1.data.shoes.Length)
            {
                selected = part1.data.shoes[part1.SelectedIndex].label;
                for (int i = 0; i < part1.data.shoes.Length; i++)
                    if (part1.data.shoes[i].bestFit) bestFit = part1.data.shoes[i].label;
            }
            return new GameplayTelemetryData
            {
                salesSessionId = salesSessionId,
                roundId = part1AttemptId,
                caseId = part2 != null && part2.scenario != null
                    && part2.scenario.customerProfile != null
                    ? part2.scenario.customerProfile.name : "Lan",
                category = Phase.ToString(),
                selectedShoeId = selected,
                bestFitShoeId = bestFit,
                fragmentId = !string.IsNullOrWhiteSpace(part1AttemptId)
                    ? salesSessionId + ":part1:" + part1AttemptId : string.Empty
            };
        }
    }
}
