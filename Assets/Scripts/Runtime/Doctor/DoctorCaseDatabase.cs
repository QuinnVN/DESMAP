using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    [CreateAssetMenu(fileName = "Doctor Case Database", menuName = "NCKH VR/Doctor/Case Database")]
    public sealed class DoctorCaseDatabase : ScriptableObject
    {
        [SerializeField] private List<DoctorCaseDefinition> cases = new List<DoctorCaseDefinition>();

        public IReadOnlyList<DoctorCaseDefinition> Cases => cases;

#if UNITY_EDITOR
        public void ConfigureInEditor(IEnumerable<DoctorCaseDefinition> configuredCases)
        {
            cases = configuredCases != null
                ? new List<DoctorCaseDefinition>(configuredCases)
                : new List<DoctorCaseDefinition>();
        }
#endif
    }
}
