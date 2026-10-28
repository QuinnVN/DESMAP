using System;
using UnityEngine;

namespace NCKH.VR.Clinic
{
    public enum ClinicRoundState
    {
        Idle,
        Preparing,
        Running,
        Finished
    }

    [Flags]
    public enum ClinicClockTransition
    {
        None = 0,
        RoundStarted = 1,
        RoundFinished = 2
    }

    public enum PatientResolution
    {
        Cured,
        TimedOut
    }

    public sealed class ClinicRoundClock
    {
        private float preparationRemaining;
        private float roundDuration;

        public ClinicRoundState State { get; private set; } = ClinicRoundState.Idle;
        public float RemainingRoundTime { get; private set; }
        public float RemainingPreparationTime => preparationRemaining;

        public void Begin(float preparationDuration, float duration)
        {
            preparationRemaining = Mathf.Max(0f, preparationDuration);
            roundDuration = Mathf.Max(0f, duration);
            RemainingRoundTime = roundDuration;
            State = preparationRemaining > 0f ? ClinicRoundState.Preparing : ClinicRoundState.Running;
        }

        public ClinicClockTransition Tick(float deltaTime)
        {
            if (deltaTime <= 0f || State == ClinicRoundState.Idle || State == ClinicRoundState.Finished)
                return ClinicClockTransition.None;

            ClinicClockTransition transition = ClinicClockTransition.None;

            if (State == ClinicRoundState.Preparing)
            {
                preparationRemaining -= deltaTime;
                if (preparationRemaining > 0f)
                    return transition;

                float overshoot = -preparationRemaining;
                preparationRemaining = 0f;
                State = ClinicRoundState.Running;
                transition |= ClinicClockTransition.RoundStarted;
                deltaTime = overshoot;
            }

            if (State == ClinicRoundState.Running && deltaTime > 0f)
            {
                RemainingRoundTime = Mathf.Max(0f, RemainingRoundTime - deltaTime);
                if (RemainingRoundTime <= 0f)
                {
                    State = ClinicRoundState.Finished;
                    transition |= ClinicClockTransition.RoundFinished;
                }
            }

            return transition;
        }

        public void Stop()
        {
            preparationRemaining = 0f;
            State = ClinicRoundState.Finished;
        }
    }

    public static class ClinicRoundRules
    {
        public const int MinimumSeverity = 1;
        public const int MaximumSeverity = 4;

        public static int SelectSeverity(
            float normalizedRoll,
            float level1Weight,
            float level2Weight,
            float level3Weight,
            float level4Weight)
        {
            level1Weight = Mathf.Max(0f, level1Weight);
            level2Weight = Mathf.Max(0f, level2Weight);
            level3Weight = Mathf.Max(0f, level3Weight);
            level4Weight = Mathf.Max(0f, level4Weight);

            float total = level1Weight + level2Weight + level3Weight + level4Weight;
            if (total <= Mathf.Epsilon)
                return MinimumSeverity;

            float weightedRoll = Mathf.Clamp01(normalizedRoll) * total;
            if (weightedRoll < level1Weight)
                return 1;
            if (weightedRoll < level1Weight + level2Weight)
                return 2;
            if (weightedRoll < level1Weight + level2Weight + level3Weight)
                return 3;
            return 4;
        }

        public static float TreatmentDuration(int severity, float standardDuration, float emergencyDuration)
        {
            return severity == MaximumSeverity
                ? Mathf.Max(0f, emergencyDuration)
                : Mathf.Max(0f, standardDuration);
        }

        public static int CureScore(
            int severity,
            int baseCureScore,
            float generalCureMultiplier,
            float emergencyCureMultiplier)
        {
            severity = Mathf.Clamp(severity, MinimumSeverity, MaximumSeverity);
            float multiplier = Mathf.Max(0f, generalCureMultiplier);
            if (severity == MaximumSeverity)
                multiplier *= Mathf.Max(0f, emergencyCureMultiplier);

            float rawScore = Mathf.Max(0, baseCureScore) * severity * multiplier;
            return Mathf.Max(0, Mathf.FloorToInt(rawScore + 0.5f));
        }

        public static int TimeoutPenalty(
            int severity,
            int level1Penalty,
            int level2Penalty,
            int level3Penalty,
            int level4Penalty)
        {
            switch (Mathf.Clamp(severity, MinimumSeverity, MaximumSeverity))
            {
                case 1:
                    return Mathf.Max(0, level1Penalty);
                case 2:
                    return Mathf.Max(0, level2Penalty);
                case 3:
                    return Mathf.Max(0, level3Penalty);
                default:
                    return Mathf.Max(0, level4Penalty);
            }
        }

        public static int ScoreDelta(
            PatientResolution resolution,
            int severity,
            int baseCureScore,
            float generalCureMultiplier,
            float emergencyCureMultiplier,
            int level1TimeoutPenalty,
            int level2TimeoutPenalty,
            int level3TimeoutPenalty,
            int level4TimeoutPenalty)
        {
            if (resolution == PatientResolution.Cured)
            {
                return CureScore(
                    severity,
                    baseCureScore,
                    generalCureMultiplier,
                    emergencyCureMultiplier);
            }

            return -TimeoutPenalty(
                severity,
                level1TimeoutPenalty,
                level2TimeoutPenalty,
                level3TimeoutPenalty,
                level4TimeoutPenalty);
        }
    }
}
