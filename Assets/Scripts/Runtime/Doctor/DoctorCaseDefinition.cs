using UnityEngine;

namespace NCKH.VR.Doctor
{
    [CreateAssetMenu(fileName = "Doctor Case", menuName = "NCKH VR/Doctor/Patient Case")]
    public sealed class DoctorCaseDefinition : ScriptableObject
    {
        [Header("Patient")]
        [SerializeField] private string patientName = "Placeholder Patient";
        [SerializeField, Min(0f)] private float selectionWeight = 1f;

        [Header("Initial Conversation")]
        [SerializeField] private DoctorDialogue initialDialogue =
            new DoctorDialogue("Placeholder initial conversation.", null, null);

        [Header("Follow-up Answers")]
        [SerializeField] private DoctorDialogue timeFollowUp =
            new DoctorDialogue("Placeholder time answer.", null, null);
        [SerializeField] private DoctorDialogue travelHistoryFollowUp =
            new DoctorDialogue("Placeholder travel answer.", null, null);
        [SerializeField] private DoctorDialogue symptomsFollowUp =
            new DoctorDialogue("Placeholder symptoms answer.", null, null);
        [SerializeField] private DoctorDialogue medicalHistoryFollowUp =
            new DoctorDialogue("Placeholder medical history answer.", null, null);

        [Header("Conclusion")]
        [SerializeField, TextArea(2, 5)] private string conclusion = "Placeholder conclusion";
        [SerializeField, TextArea(2, 5)] private string treatment = "Send the patient to placeholder treatment.";

        public string PatientName => patientName;
        public float SelectionWeight => selectionWeight;
        public DoctorDialogue InitialDialogue => initialDialogue;
        public string Conclusion => conclusion;
        public string Treatment => treatment;
        public bool IsSelectable => !string.IsNullOrWhiteSpace(patientName) && selectionWeight > 0f;

        public DoctorDialogue GetFollowUp(DoctorInfoCategory category)
        {
            switch (category)
            {
                case DoctorInfoCategory.Time:
                    return timeFollowUp;
                case DoctorInfoCategory.TravelHistory:
                    return travelHistoryFollowUp;
                case DoctorInfoCategory.Symptoms:
                    return symptomsFollowUp;
                default:
                    return medicalHistoryFollowUp;
            }
        }

#if UNITY_EDITOR
        public void ConfigureInEditor(
            string name,
            float weight,
            DoctorDialogue initial,
            DoctorDialogue time,
            DoctorDialogue travel,
            DoctorDialogue symptoms,
            DoctorDialogue medicalHistory,
            string diagnosisConclusion,
            string treatmentInstruction)
        {
            patientName = name ?? string.Empty;
            selectionWeight = Mathf.Max(0f, weight);
            initialDialogue = initial;
            timeFollowUp = time;
            travelHistoryFollowUp = travel;
            symptomsFollowUp = symptoms;
            medicalHistoryFollowUp = medicalHistory;
            conclusion = diagnosisConclusion ?? string.Empty;
            treatment = treatmentInstruction ?? string.Empty;
        }
#endif

        private void OnValidate()
        {
            selectionWeight = Mathf.Max(0f, selectionWeight);
        }
    }
}
