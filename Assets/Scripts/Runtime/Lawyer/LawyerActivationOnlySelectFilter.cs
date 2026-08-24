using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerActivationOnlySelectFilter : MonoBehaviour, IXRSelectFilter
    {
        private XRBaseInteractable interactable;

        public bool canProcess => isActiveAndEnabled;

        private void Awake() => interactable = GetComponent<XRBaseInteractable>();

        private void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRBaseInteractable>();
            interactable?.selectFilters.Add(this);
        }

        private void OnDisable() => interactable?.selectFilters.Remove(this);

        public bool Process(IXRSelectInteractor _, IXRSelectInteractable __) => false;
    }
}
