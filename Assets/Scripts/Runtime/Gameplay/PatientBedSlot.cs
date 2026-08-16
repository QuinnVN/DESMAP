using UnityEngine;

namespace NCKH.VR.Clinic
{
    [DisallowMultipleComponent]
    public sealed class PatientBedSlot : MonoBehaviour
    {
        [SerializeField] private Transform patientAnchor;
        [SerializeField] private Transform decorationAnchor;

        private GameObject standardDecoration;
        private GameObject emergencyDecoration;

        public Transform PatientAnchor => patientAnchor != null ? patientAnchor : transform;
        public Transform DecorationAnchor => decorationAnchor != null ? decorationAnchor : transform;
        public GameObject ActiveDecoration { get; private set; }
        public PatientActor Patient { get; private set; }
        public bool IsOccupied => Patient != null && Patient.State != PatientActorState.Inactive;
        public float RemainingCooldown { get; private set; }
        public bool CanAcceptPatient => !IsOccupied && RemainingCooldown <= 0f;

        internal void AssignPatient(PatientActor patient)
        {
            Patient = patient;
        }

        internal void BeginCooldown(float duration)
        {
            RemainingCooldown = Mathf.Max(0f, duration);
        }

        internal bool TickCooldown(float deltaTime)
        {
            if (RemainingCooldown <= 0f)
                return false;

            RemainingCooldown = Mathf.Max(0f, RemainingCooldown - Mathf.Max(0f, deltaTime));
            return RemainingCooldown <= 0f;
        }

        internal void ResetCooldown()
        {
            RemainingCooldown = 0f;
        }

        internal void ShowDecoration(
            bool emergency,
            GameObject prefab,
            float absoluteScale,
            Transform runtimeParent)
        {
            HideDecoration();
            if (prefab == null)
                return;

            GameObject decoration = emergency ? emergencyDecoration : standardDecoration;
            if (decoration == null)
            {
                Transform anchor = DecorationAnchor;
                decoration = Instantiate(prefab, anchor.position, transform.rotation, runtimeParent);
                decoration.name = prefab.name;

                if (emergency)
                    emergencyDecoration = decoration;
                else
                    standardDecoration = decoration;
            }

            Transform decorationTransform = decoration.transform;
            decorationTransform.SetPositionAndRotation(DecorationAnchor.position, transform.rotation);
            decorationTransform.localScale = Vector3.one * Mathf.Max(0f, absoluteScale);
            decoration.SetActive(true);
            ActiveDecoration = decoration;
        }

        internal void HideDecoration()
        {
            if (standardDecoration != null)
                standardDecoration.SetActive(false);
            if (emergencyDecoration != null)
                emergencyDecoration.SetActive(false);

            ActiveDecoration = null;
        }

        private void OnDestroy()
        {
            if (standardDecoration != null)
                Destroy(standardDecoration);
            if (emergencyDecoration != null)
                Destroy(emergencyDecoration);
        }
    }
}
