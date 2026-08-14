using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    [CreateAssetMenu(fileName = "Doctor Case", menuName = "NCKH VR/Doctor/Patient Case")]
    public sealed class DoctorCaseDefinition : ScriptableObject
    {
        [Header("Patient")]
        [SerializeField] private string patientName = "Placeholder Patient";
        [SerializeField, Min(0f)] private float selectionWeight = 1f;

        [Header("Opening Statement")]
        [SerializeField] private DoctorDialogue initialDialogue =
            new DoctorDialogue("Placeholder opening statement.", null, null);

        [Header("Question / Response Phases")]
        [SerializeField] private List<DoctorQuestionPhase> phases = new List<DoctorQuestionPhase>();

        public string PatientName => patientName;
        public float SelectionWeight => selectionWeight;
        public DoctorDialogue InitialDialogue => initialDialogue;
        public IReadOnlyList<DoctorQuestionPhase> Phases => phases;
        public bool IsSelectable => !string.IsNullOrWhiteSpace(patientName) && selectionWeight > 0f;

        public DoctorQuestionPhase GetPhase(string phaseId)
        {
            if (string.IsNullOrWhiteSpace(phaseId))
                return null;

            for (int i = 0; i < phases.Count; i++)
            {
                DoctorQuestionPhase phase = phases[i];
                if (phase != null && phase.Id == phaseId)
                    return phase;
            }

            return null;
        }

#if UNITY_EDITOR
        public void ConfigureInEditor(
            string name,
            float weight,
            DoctorDialogue opening,
            IEnumerable<DoctorQuestionPhase> configuredPhases)
        {
            patientName = name ?? string.Empty;
            selectionWeight = Mathf.Max(0f, weight);
            initialDialogue = opening ?? new DoctorDialogue(string.Empty, null, null);
            phases = configuredPhases != null
                ? new List<DoctorQuestionPhase>(configuredPhases)
                : new List<DoctorQuestionPhase>();
        }
#endif

        private void OnValidate() => selectionWeight = Mathf.Max(0f, selectionWeight);
    }
}
