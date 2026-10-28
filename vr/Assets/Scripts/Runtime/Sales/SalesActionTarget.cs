using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Sales
{
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class SalesActionTarget : MonoBehaviour
    {
        public SalesMissionController mission;
        public int action = -1; // -1 advance, -2 replay, -3 restart, 0..2 shoe
        public Transform spinningModel;
        public TMP_Text caption;
        public float idleSpeed = 18;
        public float hoverMultiplier = 3;
        private XRSimpleInteractable interactable;
        private int lastInvokeFrame = -1;
        public bool Hovered => interactable != null && interactable.isHovered;
        public float CurrentSpeed => idleSpeed * (Hovered ? hoverMultiplier : 1);

        private void Awake() => interactable = GetComponent<XRSimpleInteractable>();
        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<XRSimpleInteractable>();
            interactable.selectMode = InteractableSelectMode.Single;
            interactable.selectEntered.AddListener(OnSelected);
            interactable.activated.AddListener(OnActivated);
        }
        private void OnDisable()
        {
            if (interactable == null) return;
            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.activated.RemoveListener(OnActivated);
        }
        private void OnSelected(SelectEnterEventArgs _) => Invoke();
        private void OnActivated(ActivateEventArgs _) => Invoke();
        public void Invoke()
        {
            if (lastInvokeFrame == Time.frameCount) return;
            lastInvokeFrame = Time.frameCount;
            if (mission != null && isActiveAndEnabled) mission.HandleAction(action);
        }
        private void Update()
        {
            if (spinningModel != null)
                spinningModel.Rotate(Vector3.up, CurrentSpeed * Time.unscaledDeltaTime, Space.Self);
            if (caption != null) caption.color = Hovered ? new Color(0.35f, 1f, 0.78f) : Color.white;
        }
    }
}
