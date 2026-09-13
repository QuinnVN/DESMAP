using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Sales
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class SalesPart2ActionTarget : MonoBehaviour
    {
        public SalesPart2Controller controller;
        public int action = -1; // -1 submits/retries, -3 resets.
        public TMP_Text caption;

        private XRSimpleInteractable interactable;
        private int lastInvokeFrame = -1;

        public bool Hovered => interactable != null && interactable.isHovered;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        private void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectMode = InteractableSelectMode.Single;
            interactable.selectEntered.AddListener(OnSelected);
            interactable.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            if (interactable == null)
                return;
            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.activated.RemoveListener(OnActivated);
        }

        private void OnSelected(SelectEnterEventArgs _) => Invoke();
        private void OnActivated(ActivateEventArgs _) => Invoke();

        public void Invoke()
        {
            if (lastInvokeFrame == Time.frameCount)
                return;
            lastInvokeFrame = Time.frameCount;
            if (controller != null && isActiveAndEnabled)
                controller.HandleAction(action);
        }

        private void Update()
        {
            if (caption != null)
                caption.color = Hovered ? new Color(.35f, 1f, .78f) : Color.white;
        }
    }
}
