using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorNotebookInteractable : MonoBehaviour
    {
        [SerializeField] private XRSimpleInteractable interactable;

        public event Action Activated;

        private void OnEnable()
        {
            if (interactable != null)
                interactable.selectEntered.AddListener(OnSelected);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnSelected);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (interactable != null)
                interactable.enabled = enabled;
        }

        private void OnSelected(SelectEnterEventArgs args)
        {
            Activated?.Invoke();
        }
    }
}
