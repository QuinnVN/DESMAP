using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;

namespace NCKH.VR.Clinic
{
    [DisallowMultipleComponent]
    public sealed class ClinicGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PatientActor patientPrefab;
        [SerializeField] private Transform playerHead;
        [SerializeField] private PatientBedSlot[] bedSlots = Array.Empty<PatientBedSlot>();

        [Header("Round")]
        [SerializeField, Min(0f)] private float initialPreparationDelay = 2f;
        [SerializeField, Min(0f)] private float roundDuration = 60f;
        [SerializeField, Min(0.01f)] private float arrivalInterval = 5f;
        [SerializeField] private bool startAutomatically = true;
        [SerializeField, Min(0f)] private float automaticStartReadinessTimeout = 10f;
        [SerializeField, Min(0)] private int automaticStartStableFrames = 2;

        [Header("Patient Timing")]
        [SerializeField, Min(0f)] private float standardTreatmentDuration = 30f;
        [SerializeField, Min(0f)] private float emergencyTreatmentDuration = 5f;
        [SerializeField, Min(0f)] private float patientFadeDuration = 0.25f;
        [FormerlySerializedAs("curedBedCooldown")]
        [SerializeField, Min(0f)] private float resolvedBedCooldown = 5f;

        [Header("Severity Weights")]
        [SerializeField, Min(0f)] private float level1Weight = 40f;
        [SerializeField, Min(0f)] private float level2Weight = 30f;
        [SerializeField, Min(0f)] private float level3Weight = 20f;
        [SerializeField, Min(0f)] private float level4Weight = 10f;

        [Header("Scoring - Cure Rewards")]
        [FormerlySerializedAs("cureReward")]
        [SerializeField, Min(0)] private int baseCureScore = 1;
        [SerializeField, Min(0f)] private float generalCureMultiplier = 1f;
        [SerializeField, Min(0f)] private float emergencyCureMultiplier = 2f;

        [Header("Scoring - Timeout Penalties")]
        [SerializeField, Min(0)] private int level1TimeoutPenalty;
        [SerializeField, Min(0)] private int level2TimeoutPenalty;
        [SerializeField, Min(0)] private int level3TimeoutPenalty;
        [FormerlySerializedAs("missedEmergencyPenalty")]
        [SerializeField, Min(0)] private int level4TimeoutPenalty = 2;

        [Header("Patient Display")]
        [SerializeField] private string sicknessPlaceholder = "Sickness: [Placeholder]";

        private readonly ClinicRoundClock clock = new ClinicRoundClock();
        private readonly List<XRDisplaySubsystem> xrDisplays = new List<XRDisplaySubsystem>(1);
        private float arrivalCountdown;
        private bool roundEndRaised;

        public event Action<int> ScoreChanged;
        public event Action<int> RoundEnded;

        public int Score { get; private set; }
        public float RemainingRoundTime => clock.RemainingRoundTime;
        public float RemainingPreparationTime => clock.RemainingPreparationTime;
        public ClinicRoundState RoundState => clock.State;
        public bool IsRoundRunning => clock.State == ClinicRoundState.Running;

        private void Awake()
        {
            PrewarmPatients();
        }

        private IEnumerator Start()
        {
            if (!startAutomatically)
                yield break;

            float deadline = Time.realtimeSinceStartup + automaticStartReadinessTimeout;
            bool displayReady = IsAnyXrDisplayRunning();
            while (!displayReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                displayReady = IsAnyXrDisplayRunning();
            }

            if (!displayReady)
            {
                Debug.LogWarning(
                    $"XR display did not become ready within {automaticStartReadinessTimeout:0.##} seconds. "
                    + "Starting the clinic round using the desktop fallback.",
                    this);
            }

            for (int i = 0; i < automaticStartStableFrames; i++)
                yield return new WaitForEndOfFrame();

            if (clock.State == ClinicRoundState.Idle)
                StartRound();
        }

        private bool IsAnyXrDisplayRunning()
        {
            xrDisplays.Clear();
            SubsystemManager.GetSubsystems(xrDisplays);
            for (int i = 0; i < xrDisplays.Count; i++)
            {
                XRDisplaySubsystem display = xrDisplays[i];
                if (display != null && display.running)
                    return true;
            }

            return false;
        }

        private void Update()
        {
            ClinicRoundState stateBeforeTick = clock.State;
            ClinicClockTransition transition = clock.Tick(Time.deltaTime);

            if ((transition & ClinicClockTransition.RoundStarted) != 0
                || (stateBeforeTick == ClinicRoundState.Idle && clock.State == ClinicRoundState.Running))
            {
                BeginPatientArrivals();
            }

            if (clock.State == ClinicRoundState.Running
                && (transition & ClinicClockTransition.RoundStarted) == 0)
            {
                UpdateBedCooldowns(Time.deltaTime);
                arrivalCountdown -= Time.deltaTime;
                if (arrivalCountdown <= 0f)
                {
                    SpawnPatientInRandomFreeBed();
                    arrivalCountdown = Mathf.Max(0.01f, arrivalInterval);
                }
            }

            if ((transition & ClinicClockTransition.RoundFinished) != 0)
                FinishRound();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientActor patient = bedSlots[i] != null ? bedSlots[i].Patient : null;
                if (patient == null)
                    continue;

                patient.Resolved -= OnPatientResolved;
                patient.BecameInactive -= OnPatientBecameInactive;
            }
        }

        public void StartRound()
        {
            EnsurePatientsArePrewarmed();

            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientBedSlot slot = bedSlots[i];
                if (slot != null)
                    slot.ResetCooldown();

                PatientActor patient = slot != null ? slot.Patient : null;
                if (patient != null)
                    patient.DeactivateImmediately();
            }

            Score = 0;
            ScoreChanged?.Invoke(Score);
            roundEndRaised = false;
            clock.Begin(initialPreparationDelay, roundDuration);

            if (clock.State == ClinicRoundState.Running)
                BeginPatientArrivals();
        }

        public void StopRound()
        {
            if (clock.State == ClinicRoundState.Idle || clock.State == ClinicRoundState.Finished)
                return;

            clock.Stop();
            FinishRound();
        }

        private void PrewarmPatients()
        {
            if (patientPrefab == null)
            {
                Debug.LogError("ClinicGameManager requires a patient prefab.", this);
                return;
            }

            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientBedSlot slot = bedSlots[i];
                if (slot == null || slot.Patient != null)
                    continue;

                Transform anchor = slot.PatientAnchor;
                PatientActor patient = Instantiate(patientPrefab, anchor.position, anchor.rotation, transform);
                patient.name = patientPrefab.name;
                patient.Resolved += OnPatientResolved;
                patient.BecameInactive += OnPatientBecameInactive;
                patient.DeactivateImmediately();
                slot.AssignPatient(patient);
            }
        }

        private void EnsurePatientsArePrewarmed()
        {
            for (int i = 0; i < bedSlots.Length; i++)
            {
                if (bedSlots[i] != null && bedSlots[i].Patient == null)
                {
                    PrewarmPatients();
                    return;
                }
            }
        }

        private void BeginPatientArrivals()
        {
            SpawnPatientInRandomFreeBed();
            arrivalCountdown = Mathf.Max(0.01f, arrivalInterval);
        }

        private void SpawnPatientInRandomFreeBed()
        {
            if (clock.State != ClinicRoundState.Running)
                return;

            int chosenIndex = -1;
            int availableCount = 0;

            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientBedSlot slot = bedSlots[i];
                if (slot == null || slot.Patient == null || !slot.CanAcceptPatient)
                    continue;

                availableCount++;
                if (UnityEngine.Random.Range(0, availableCount) == 0)
                    chosenIndex = i;
            }

            if (chosenIndex >= 0)
                ActivatePatient(bedSlots[chosenIndex].Patient);
        }

        private void ActivatePatient(PatientActor patient)
        {
            int severity = ClinicRoundRules.SelectSeverity(
                UnityEngine.Random.value,
                level1Weight,
                level2Weight,
                level3Weight,
                level4Weight);

            float treatmentDuration = ClinicRoundRules.TreatmentDuration(
                severity,
                standardTreatmentDuration,
                emergencyTreatmentDuration);

            patient.Activate(
                severity,
                treatmentDuration,
                sicknessPlaceholder,
                playerHead,
                patientFadeDuration);
        }

        private void OnPatientResolved(PatientActor patient, PatientResolution resolution)
        {
            int slotIndex = FindSlotIndex(patient);
            if (slotIndex < 0 || clock.State != ClinicRoundState.Running)
                return;

            int delta = ClinicRoundRules.ScoreDelta(
                resolution,
                patient.Severity,
                baseCureScore,
                generalCureMultiplier,
                emergencyCureMultiplier,
                level1TimeoutPenalty,
                level2TimeoutPenalty,
                level3TimeoutPenalty,
                level4TimeoutPenalty);

            if (delta != 0)
            {
                Score += delta;
                ScoreChanged?.Invoke(Score);
            }

            PatientBedSlot slot = bedSlots[slotIndex];
            slot.BeginCooldown(resolvedBedCooldown);
        }

        private void OnPatientBecameInactive(PatientActor patient)
        {
            int slotIndex = FindSlotIndex(patient);
            if (slotIndex < 0)
                return;

            PatientBedSlot slot = bedSlots[slotIndex];
            bool shouldReplace = clock.State == ClinicRoundState.Running
                && slot.RemainingCooldown <= 0f;

            if (shouldReplace)
                ActivatePatient(patient);
        }

        private void UpdateBedCooldowns(float deltaTime)
        {
            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientBedSlot slot = bedSlots[i];
                if (slot == null || !slot.TickCooldown(deltaTime))
                    continue;

                if (slot.CanAcceptPatient && slot.Patient != null)
                    ActivatePatient(slot.Patient);
            }
        }

        private int FindSlotIndex(PatientActor patient)
        {
            for (int i = 0; i < bedSlots.Length; i++)
            {
                if (bedSlots[i] != null && bedSlots[i].Patient == patient)
                    return i;
            }

            return -1;
        }

        private void FinishRound()
        {
            for (int i = 0; i < bedSlots.Length; i++)
            {
                PatientBedSlot slot = bedSlots[i];
                if (slot != null)
                    slot.ResetCooldown();

                PatientActor patient = slot != null ? slot.Patient : null;
                if (patient != null)
                    patient.StopForRoundEnd();
            }

            if (roundEndRaised)
                return;

            roundEndRaised = true;
            RoundEnded?.Invoke(Score);
        }

        private void OnValidate()
        {
            initialPreparationDelay = Mathf.Max(0f, initialPreparationDelay);
            roundDuration = Mathf.Max(0f, roundDuration);
            arrivalInterval = Mathf.Max(0.01f, arrivalInterval);
            standardTreatmentDuration = Mathf.Max(0f, standardTreatmentDuration);
            emergencyTreatmentDuration = Mathf.Max(0f, emergencyTreatmentDuration);
            patientFadeDuration = Mathf.Max(0f, patientFadeDuration);
            resolvedBedCooldown = Mathf.Max(0f, resolvedBedCooldown);
            automaticStartReadinessTimeout = Mathf.Max(0f, automaticStartReadinessTimeout);
            automaticStartStableFrames = Mathf.Max(0, automaticStartStableFrames);
            baseCureScore = Mathf.Max(0, baseCureScore);
            generalCureMultiplier = Mathf.Max(0f, generalCureMultiplier);
            emergencyCureMultiplier = Mathf.Max(0f, emergencyCureMultiplier);
            level1TimeoutPenalty = Mathf.Max(0, level1TimeoutPenalty);
            level2TimeoutPenalty = Mathf.Max(0, level2TimeoutPenalty);
            level3TimeoutPenalty = Mathf.Max(0, level3TimeoutPenalty);
            level4TimeoutPenalty = Mathf.Max(0, level4TimeoutPenalty);
        }
    }
}
