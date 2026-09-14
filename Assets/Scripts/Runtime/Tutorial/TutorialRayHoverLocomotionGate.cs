using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialRayHoverLocomotionGate : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private ContinuousMoveProvider moveProvider;

        private bool rayHovering;
        private bool restoreMoveProvider;

        public bool IsRayHovering => rayHovering;

        public void Configure(ContinuousMoveProvider configuredMoveProvider)
        {
            moveProvider = configuredMoveProvider;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsRayPointer(eventData) || rayHovering)
                return;

            rayHovering = true;
            if (moveProvider == null)
                return;

            restoreMoveProvider = moveProvider.enabled;
            moveProvider.enabled = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!IsRayPointer(eventData) || !rayHovering)
                return;

            RestoreLocomotion();
        }

        private void OnDisable()
        {
            RestoreLocomotion();
        }

        private void RestoreLocomotion()
        {
            if (!rayHovering)
                return;

            rayHovering = false;
            if (moveProvider != null)
                moveProvider.enabled = restoreMoveProvider;
            restoreMoveProvider = false;
        }

        private static bool IsRayPointer(PointerEventData eventData)
        {
            return eventData is TrackedDeviceEventData trackedEvent &&
                trackedEvent.interactor is XRRayInteractor;
        }
    }
}
