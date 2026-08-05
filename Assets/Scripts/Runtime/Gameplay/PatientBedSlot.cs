using UnityEngine;

namespace NCKH.VR.Clinic
{
    [DisallowMultipleComponent]
    public sealed class PatientBedSlot : MonoBehaviour
    {
        [SerializeField] private Transform patientAnchor;

        public Transform PatientAnchor => patientAnchor != null ? patientAnchor : transform;
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
    }
}
