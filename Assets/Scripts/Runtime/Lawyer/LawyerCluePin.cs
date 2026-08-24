using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(LawyerActivationOnlySelectFilter))]
    public sealed class LawyerCluePin : MonoBehaviour
    {
        private XRSimpleInteractable interactable;
        private LawyerClueLinkSystem linkSystem;

        public LawyerBoardClue Clue { get; private set; }
        public Vector3 AnchorPosition => transform.position;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        private void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.activated.AddListener(OnActivated);
        }

        private void OnDisable()
        {
            if (interactable == null)
                return;
            interactable.activated.RemoveListener(OnActivated);
            linkSystem?.UnregisterPin(this);
        }

        public void Configure(LawyerBoardClue clue, LawyerClueLinkSystem configuredLinkSystem)
        {
            Clue = clue;
            linkSystem = configuredLinkSystem;
            linkSystem?.RegisterPin(this);
        }

        public void SetInteractionEnabled(bool value)
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.enabled = value;
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (args.interactorObject is XRRayInteractor ray)
                linkSystem?.BeginLink(this, ray);
        }
    }
}
