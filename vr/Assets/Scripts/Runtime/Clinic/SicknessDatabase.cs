using System;
using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Clinic
{
    [Serializable]
    public sealed class SicknessEntry
    {
        [SerializeField] private string sicknessName = string.Empty;
        [SerializeField, Range(ClinicRoundRules.MinimumSeverity, ClinicRoundRules.MaximumSeverity)]
        private int severity = ClinicRoundRules.MinimumSeverity;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;

        public string SicknessName => sicknessName;
        public int Severity => severity;
        public float SelectionWeight => selectionWeight;

        public SicknessEntry()
        {
        }

        public SicknessEntry(string sicknessName, int severity, float selectionWeight)
        {
            this.sicknessName = sicknessName;
            this.severity = Mathf.Clamp(
                severity,
                ClinicRoundRules.MinimumSeverity,
                ClinicRoundRules.MaximumSeverity);
            this.selectionWeight = Mathf.Max(0f, selectionWeight);
        }

        internal bool IsSelectable(int? requiredSeverity)
        {
            return !string.IsNullOrWhiteSpace(sicknessName)
                && selectionWeight > 0f
                && severity >= ClinicRoundRules.MinimumSeverity
                && severity <= ClinicRoundRules.MaximumSeverity
                && (!requiredSeverity.HasValue || severity == requiredSeverity.Value);
        }

        internal void Validate()
        {
            severity = Mathf.Clamp(
                severity,
                ClinicRoundRules.MinimumSeverity,
                ClinicRoundRules.MaximumSeverity);
            selectionWeight = Mathf.Max(0f, selectionWeight);
        }
    }

    [DisallowMultipleComponent]
    public sealed class SicknessDatabase : MonoBehaviour
    {
        [SerializeField] private List<SicknessEntry> entries = new List<SicknessEntry>();

        public IReadOnlyList<SicknessEntry> Entries => entries;

        public bool TrySelectRandom(out SicknessEntry entry)
        {
            return TrySelect(UnityEngine.Random.value, null, out entry);
        }

        public bool TrySelectRandom(int requiredSeverity, out SicknessEntry entry)
        {
            requiredSeverity = Mathf.Clamp(
                requiredSeverity,
                ClinicRoundRules.MinimumSeverity,
                ClinicRoundRules.MaximumSeverity);
            return TrySelect(UnityEngine.Random.value, requiredSeverity, out entry);
        }

        internal bool TrySelect(float normalizedRoll, int? requiredSeverity, out SicknessEntry entry)
        {
            entry = null;
            if (entries == null)
                return false;

            float totalWeight = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                SicknessEntry candidate = entries[i];
                if (candidate != null && candidate.IsSelectable(requiredSeverity))
                    totalWeight += candidate.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return false;

            float weightedRoll = Mathf.Clamp01(normalizedRoll) * totalWeight;
            SicknessEntry lastSelectable = null;
            float cumulativeWeight = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                SicknessEntry candidate = entries[i];
                if (candidate == null || !candidate.IsSelectable(requiredSeverity))
                    continue;

                lastSelectable = candidate;
                cumulativeWeight += candidate.SelectionWeight;
                if (weightedRoll < cumulativeWeight)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = lastSelectable;
            return entry != null;
        }

        private void OnValidate()
        {
            if (entries == null)
            {
                entries = new List<SicknessEntry>();
                return;
            }

            for (int i = 0; i < entries.Count; i++)
                entries[i]?.Validate();
        }
    }
}
