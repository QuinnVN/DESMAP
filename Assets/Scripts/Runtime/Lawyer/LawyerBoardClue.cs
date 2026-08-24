using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class LawyerBoardClue : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField, Min(0.02f)] private float snapDuration = 0.1f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody body;
        private Transform boardSurface;
        private LawyerGameSession session;
        private Vector2 halfBounds;
        private float localDepth;
        private Coroutine snapRoutine;

        public string ClueId { get; private set; } = string.Empty;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            grabInteractable.throwOnDetach = false;
        }

        private void OnEnable()
        {
            grabInteractable.selectEntered.AddListener(OnSelected);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDisable()
        {
            grabInteractable.selectEntered.RemoveListener(OnSelected);
            grabInteractable.selectExited.RemoveListener(OnReleased);
            if (snapRoutine != null)
                StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        private void LateUpdate()
        {
            if (boardSurface == null || grabInteractable == null || !grabInteractable.isSelected)
                return;
            ConstrainToBoard(false);
        }

        public void Configure(
            LawyerClueDefinition clue,
            Transform configuredBoardSurface,
            Vector2 configuredHalfBounds,
            LawyerGameSession configuredSession)
        {
            ClueId = clue != null ? clue.Id : string.Empty;
            boardSurface = configuredBoardSurface;
            halfBounds = configuredHalfBounds;
            session = configuredSession;
            if (titleLabel != null)
                titleLabel.text = clue != null ? clue.Title : string.Empty;
            if (bodyLabel != null)
                bodyLabel.text = clue != null ? clue.Description : string.Empty;
            localDepth = boardSurface != null
                ? boardSurface.InverseTransformPoint(transform.position).z
                : 0f;
        }

        public void SetBoardLocalPosition(Vector2 localPosition)
        {
            if (boardSurface == null)
                return;
            Vector3 local = new Vector3(localPosition.x, localPosition.y, localDepth);
            transform.position = boardSurface.TransformPoint(local);
            transform.rotation = boardSurface.rotation;
        }

        public void SetInteractionEnabled(bool value)
        {
            if (grabInteractable == null)
                grabInteractable = GetComponent<XRGrabInteractable>();
            grabInteractable.enabled = value;
            LawyerCluePin pin = GetComponentInChildren<LawyerCluePin>(true);
            pin?.SetInteractionEnabled(value);
        }

        public Vector2 BoardLocalPosition
        {
            get
            {
                Vector3 local = boardSurface != null
                    ? boardSurface.InverseTransformPoint(transform.position)
                    : Vector3.zero;
                return new Vector2(local.x, local.y);
            }
        }

        private void ConstrainToBoard(bool save)
        {
            Vector3 local = ClampedBoardLocalPosition();
            transform.position = boardSurface.TransformPoint(local);
            transform.rotation = boardSurface.rotation;
            if (save && session != null)
                session.SetCluePosition(ClueId, new Vector2(local.x, local.y));
        }

        private void OnSelected(SelectEnterEventArgs _)
        {
            if (snapRoutine != null)
                StopCoroutine(snapRoutine);
            snapRoutine = null;
        }

        private void OnReleased(SelectExitEventArgs _)
        {
            if (boardSurface == null)
                return;
            if (snapRoutine != null)
                StopCoroutine(snapRoutine);
            snapRoutine = StartCoroutine(SnapToBoard());
        }

        private IEnumerator SnapToBoard()
        {
            Vector3 local = ClampedBoardLocalPosition();
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            while (elapsed < snapDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / snapDuration));
                transform.position = Vector3.Lerp(
                    startPosition, boardSurface.TransformPoint(local), t);
                transform.rotation = Quaternion.Slerp(
                    startRotation, boardSurface.rotation, t);
                yield return null;
            }

            transform.SetPositionAndRotation(
                boardSurface.TransformPoint(local), boardSurface.rotation);
            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            session?.SetCluePosition(ClueId, new Vector2(local.x, local.y));
            snapRoutine = null;
        }

        private Vector3 ClampedBoardLocalPosition()
        {
            Vector3 local = boardSurface.InverseTransformPoint(transform.position);
            local.x = Mathf.Clamp(local.x, -halfBounds.x, halfBounds.x);
            local.y = Mathf.Clamp(local.y, -halfBounds.y, halfBounds.y);
            local.z = localDepth;
            return local;
        }
    }
}
