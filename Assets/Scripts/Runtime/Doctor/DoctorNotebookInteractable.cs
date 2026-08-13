using System;
using UnityEngine;
using UnityEngine.UI;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorNotebookInteractable : MonoBehaviour
    {
        [SerializeField] private Button activationButton;

        public event Action Activated;

        private void OnEnable()
        {
            activationButton?.onClick.AddListener(OnPressed);
        }

        private void OnDisable()
        {
            activationButton?.onClick.RemoveListener(OnPressed);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            if (activationButton != null)
                activationButton.interactable = enabled;
        }

        private void OnPressed()
        {
            Activated?.Invoke();
        }
    }
}
