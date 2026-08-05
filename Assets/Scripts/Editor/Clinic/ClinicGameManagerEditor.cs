using NCKH.VR.Clinic;
using UnityEditor;
using UnityEngine;

namespace NCKH.VR.Clinic.Editor
{
    [CustomEditor(typeof(ClinicGameManager))]
    public sealed class ClinicGameManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty baseCureScore;
        private SerializedProperty generalCureMultiplier;
        private SerializedProperty emergencyCureMultiplier;
        private SerializedProperty[] timeoutPenalties;

        private void OnEnable()
        {
            baseCureScore = serializedObject.FindProperty("baseCureScore");
            generalCureMultiplier = serializedObject.FindProperty("generalCureMultiplier");
            emergencyCureMultiplier = serializedObject.FindProperty("emergencyCureMultiplier");
            timeoutPenalties = new[]
            {
                serializedObject.FindProperty("level1TimeoutPenalty"),
                serializedObject.FindProperty("level2TimeoutPenalty"),
                serializedObject.FindProperty("level3TimeoutPenalty"),
                serializedObject.FindProperty("level4TimeoutPenalty")
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scoring Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cure = Base Score × Severity × General Multiplier × Severity 4 Multiplier (severity 4 only). "
                + "Decimal results round to the nearest integer, with .5 rounding upward.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                for (int severity = ClinicRoundRules.MinimumSeverity;
                    severity <= ClinicRoundRules.MaximumSeverity;
                    severity++)
                {
                    int cureScore = ClinicRoundRules.CureScore(
                        severity,
                        baseCureScore.intValue,
                        generalCureMultiplier.floatValue,
                        emergencyCureMultiplier.floatValue);
                    int timeoutPenalty = Mathf.Max(0, timeoutPenalties[severity - 1].intValue);
                    string timeoutText = timeoutPenalty > 0 ? $"-{timeoutPenalty}" : "0";
                    EditorGUILayout.LabelField(
                        $"Severity {severity}",
                        $"Cure +{cureScore}    Timeout {timeoutText}");
                }
            }
        }
    }
}
