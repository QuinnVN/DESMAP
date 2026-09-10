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
        private bool desktopHover;
        public bool Hovered => desktopHover || (interactable != null && interactable.isHovered);
        public float CurrentSpeed => idleSpeed * (Hovered ? hoverMultiplier : 1);

        private void Awake() => interactable = GetComponent<XRSimpleInteractable>();
        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<XRSimpleInteractable>();
            interactable.activated.AddListener(OnActivated);
        }
        private void OnDisable()
        {
            if (interactable != null) interactable.activated.RemoveListener(OnActivated);
            desktopHover = false;
        }
        private void OnActivated(ActivateEventArgs _) => Invoke();
        public void SetDesktopHover(bool value) => desktopHover = value;
        public void Invoke()
        {
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
