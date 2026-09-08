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
        [SerializeField] private Renderer cardRenderer;
        [SerializeField] private Transform cardVisual;
        [SerializeField] private BoxCollider cardCollider;
        [SerializeField] private BoxCollider diamondCollider;
        [SerializeField, Min(0.02f)] private float snapDuration = 0.1f;

        private XRGrabInteractable grabInteractable;
        private Rigidbody body;
        private Transform boardSurface;
        private LawyerBoardController boardController;
        private LawyerGameSession session;
        private Vector2 snapCenter;
        private Vector2 halfBounds;
        private float localDepth;
        private Vector2 lastValidBoardLocalPosition;
        private Coroutine snapRoutine;
        private Vector3 evidenceVisualScale = new Vector3(0.28f, 0.14f, 0.015f);
        private Vector3 evidencePinPosition = new Vector3(0.115f, 0.05f, 0.025f);

        public string ClueId { get; private set; } = string.Empty;
        public bool IsReasoningCard { get; private set; }
        public string CardId => ClueId;
        private bool initialized;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            grabInteractable = GetComponent<XRGrabInteractable>();
            body = GetComponent<Rigidbody>();
            grabInteractable.throwOnDetach = false;
            if (cardCollider == null)
                cardCollider = GetComponent<BoxCollider>();
            if (cardRenderer == null)
                cardRenderer = transform.Find("Card Visual")?.GetComponent<Renderer>();
            if (cardVisual == null)
                cardVisual = transform.Find("Card Visual");
            if (cardVisual != null)
                evidenceVisualScale = cardVisual.localScale;
            LawyerCluePin pin = GetComponentInChildren<LawyerCluePin>(true);
            if (pin != null)
                evidencePinPosition = pin.transform.localPosition;
            EnsureDiamondCollider();
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
            Vector2 configuredSnapCenter,
            Vector2 configuredHalfBounds,
            LawyerBoardController configuredBoardController,
            LawyerGameSession configuredSession)
        {
            Initialize();
            ClueId = clue != null ? clue.Id : string.Empty;
            boardSurface = configuredBoardSurface;
            snapCenter = configuredSnapCenter;
            halfBounds = configuredHalfBounds;
            boardController = configuredBoardController;
            session = configuredSession;
            IsReasoningCard = false;
            EnsureDiamondCollider();
            if (cardVisual != null)
            {
                cardVisual.localScale = evidenceVisualScale;
                cardVisual.localRotation = Quaternion.identity;
            }
            SetPinPosition(evidencePinPosition);
            SetColliderMode(false);
            if (titleLabel != null)
                titleLabel.text = clue != null ? clue.Title : string.Empty;
            if (bodyLabel != null)
                bodyLabel.text = clue != null ? clue.Description : string.Empty;
            ConfigureTextSizing();
            ResizeCardGeometry(false);
            localDepth = boardSurface != null
                ? boardSurface.InverseTransformPoint(transform.position).z
                : 0f;
        }

        public void ConfigureReasoning(
            LawyerReasoningCardDefinition card,
            Transform configuredBoardSurface,
            Vector2 configuredSnapCenter,
            Vector2 configuredHalfBounds,
            LawyerBoardController configuredBoardController,
            LawyerGameSession configuredSession,
            Material reasoningMaterial)
        {
            Initialize();
            IsReasoningCard = true;
            ClueId = card != null ? card.Id : string.Empty;
            boardSurface = configuredBoardSurface;
            snapCenter = configuredSnapCenter;
            halfBounds = configuredHalfBounds;
            boardController = configuredBoardController;
            session = configuredSession;
            EnsureDiamondCollider();
            if (titleLabel != null)
                titleLabel.text = card != null ? card.Title : string.Empty;
            if (bodyLabel != null)
                bodyLabel.text = card != null ? card.Description : string.Empty;
            if (cardRenderer == null)
                cardRenderer = transform.Find("Card Visual")?.GetComponent<Renderer>();
            if (cardVisual == null)
                cardVisual = transform.Find("Card Visual");
            if (cardRenderer != null && reasoningMaterial != null)
                cardRenderer.sharedMaterial = reasoningMaterial;
            if (cardVisual != null)
            {
                cardVisual.localScale = new Vector3(
                    Mathf.Max(evidenceVisualScale.x, evidenceVisualScale.y),
                    Mathf.Max(evidenceVisualScale.x, evidenceVisualScale.y),
                    evidenceVisualScale.z);
                cardVisual.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }
            SetPinPosition(new Vector3(
                Mathf.Max(evidenceVisualScale.x, evidenceVisualScale.y) * 0.36f,
                Mathf.Max(evidenceVisualScale.x, evidenceVisualScale.y) * 0.36f,
                evidencePinPosition.z));
            SetColliderMode(true);
            ConfigureTextSizing();
            ResizeCardGeometry(true);
            localDepth = boardSurface != null
                ? boardSurface.InverseTransformPoint(transform.position).z
                : 0f;
        }

        public void SetBoardLocalPosition(Vector2 localPosition)
        {
            if (boardSurface == null)
                return;
            Vector3 local = ResolvedBoardLocalPosition(localPosition, localPosition);
            transform.position = boardSurface.TransformPoint(local);
            transform.rotation = boardSurface.rotation;
            lastValidBoardLocalPosition = new Vector2(local.x, local.y);
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

        public Vector2 BoardLocalHalfExtents
        {
            get
            {
                BoxCollider active = IsReasoningCard ? diamondCollider : cardCollider;
                if (active == null || boardSurface == null)
                    return Vector2.zero;
                Bounds source = new Bounds(active.center, active.size);
                Vector3 sourceCenter = source.center;
                Vector3 extents = source.extents;
                Bounds local = new Bounds(
                    boardSurface.InverseTransformPoint(active.transform.TransformPoint(sourceCenter)),
                    Vector3.zero);
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    local.Encapsulate(boardSurface.InverseTransformPoint(
                        active.transform.TransformPoint(
                            sourceCenter + Vector3.Scale(extents, new Vector3(x, y, z)))));
                return new Vector2(local.extents.x, local.extents.y);
            }
        }

        private void ConstrainToBoard(bool save)
        {
            Vector3 local = ClampedBoardLocalPosition();
            transform.position = boardSurface.TransformPoint(local);
            transform.rotation = boardSurface.rotation;
            if (save && session != null)
                SetSessionPosition(new Vector2(local.x, local.y));
        }

        private void OnSelected(SelectEnterEventArgs _)
        {
            if (snapRoutine != null)
                StopCoroutine(snapRoutine);
            snapRoutine = null;
            lastValidBoardLocalPosition = BoardLocalPosition;
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
            Vector3 clamped = ClampedBoardLocalPosition();
            Vector3 local = ResolvedBoardLocalPosition(
                new Vector2(clamped.x, clamped.y), lastValidBoardLocalPosition);
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
            SetSessionPosition(new Vector2(local.x, local.y));
            lastValidBoardLocalPosition = new Vector2(local.x, local.y);
            snapRoutine = null;
        }

        private void SetSessionPosition(Vector2 position)
        {
            if (session == null)
                return;
            if (IsReasoningCard)
                session.SetReasoningPosition(ClueId, position);
            else
                session.SetCluePosition(ClueId, position);
        }

        private void ConfigureTextSizing()
        {
            if (titleLabel != null)
            {
                titleLabel.enableAutoSizing = true;
                titleLabel.fontSizeMin = 18f;
                titleLabel.fontSizeMax = 72f;
            }
            if (bodyLabel != null)
            {
                bodyLabel.enableAutoSizing = false;
                bodyLabel.fontSize = 14f;
                bodyLabel.fontSizeMin = 14f;
                bodyLabel.fontSizeMax = 14f;
                bodyLabel.ForceMeshUpdate(true, true);
                float width = IsReasoningCard ? 18f : 24f;
                Vector2 preferred = bodyLabel.GetPreferredValues(bodyLabel.text, width, 1000f);
                bodyLabel.rectTransform.sizeDelta = new Vector2(
                    width, Mathf.Max(3f, preferred.y + 0.5f));
                if (titleLabel != null)
                    titleLabel.rectTransform.sizeDelta = new Vector2(width, 4f);
            }
        }

        private void ResizeCardGeometry(bool diamond)
        {
            if (bodyLabel == null)
                return;
            float unitScale = Mathf.Abs(bodyLabel.transform.localScale.x);
            if (unitScale < 0.0001f)
                unitScale = 0.01f;
            Vector2 logical = bodyLabel.rectTransform.sizeDelta;
            float titleHeight = titleLabel != null
                ? titleLabel.rectTransform.sizeDelta.y
                : 4f;
            const float textGap = 1f;
            float totalLogicalHeight = titleHeight + textGap + logical.y;
            float width = logical.x * unitScale + 0.025f;
            float height = totalLogicalHeight * unitScale + 0.045f;
            // Scene templates may contain old manual offsets. Layout, collision and
            // attachment all use the card origin, regardless of those overrides.
            if (cardVisual != null)
                cardVisual.localPosition = Vector3.zero;
            if (cardCollider != null)
                cardCollider.center = Vector3.zero;
            if (titleLabel != null)
            {
                titleLabel.rectTransform.anchoredPosition = new Vector2(
                    0f, (logical.y + textGap) * 0.5f * unitScale);
                bodyLabel.rectTransform.anchoredPosition = new Vector2(
                    0f, -(titleHeight + textGap) * 0.5f * unitScale);
            }
            if (diamond)
            {
                float side = (width + height) / Mathf.Sqrt(2f);
                if (cardVisual != null)
                {
                    cardVisual.localScale = new Vector3(side, side, evidenceVisualScale.z);
                    cardVisual.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                if (diamondCollider != null)
                {
                    diamondCollider.transform.localPosition = Vector3.zero;
                    diamondCollider.center = Vector3.zero;
                    diamondCollider.transform.localScale = new Vector3(side, side, 1f);
                    diamondCollider.size = new Vector3(1f, 1f, evidenceVisualScale.z);
                    diamondCollider.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                }
                SetPinPosition(new Vector3(0f, side / Mathf.Sqrt(2f) - 0.018f,
                    evidenceVisualScale.z * 0.5f + 0.01f));
            }
            else
            {
                if (cardVisual != null)
                {
                    cardVisual.localScale = new Vector3(width, height, evidenceVisualScale.z);
                    cardVisual.localRotation = Quaternion.identity;
                }
                if (cardCollider != null)
                    cardCollider.size = new Vector3(width, height, 0.025f);
                SetPinPosition(new Vector3(0f, height * 0.5f - 0.012f,
                    evidenceVisualScale.z * 0.5f + 0.01f));
            }
            if (body != null)
                body.centerOfMass = Vector3.zero;
            if (grabInteractable != null && grabInteractable.attachTransform != null)
            {
                Vector3 attachment = grabInteractable.attachTransform.localPosition;
                grabInteractable.attachTransform.localPosition = new Vector3(0f, 0f, attachment.z);
            }
        }

        private void EnsureDiamondCollider()
        {
            if (diamondCollider == null)
            {
                Transform existing = transform.Find("Diamond Collider");
                GameObject colliderObject = existing != null
                    ? existing.gameObject
                    : new GameObject("Diamond Collider");
                colliderObject.transform.SetParent(transform, false);
                colliderObject.layer = gameObject.layer;
                colliderObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                colliderObject.transform.localScale = Vector3.one
                    * Mathf.Max(evidenceVisualScale.x, evidenceVisualScale.y);
                diamondCollider = colliderObject.GetComponent<BoxCollider>();
                if (diamondCollider == null)
                    diamondCollider = colliderObject.AddComponent<BoxCollider>();
                diamondCollider.size = new Vector3(1f, 1f, 0.12f);
            }
        }

        private void SetColliderMode(bool diamond)
        {
            if (cardCollider != null)
                cardCollider.enabled = !diamond;
            if (diamondCollider != null)
                diamondCollider.gameObject.SetActive(diamond);
            if (grabInteractable == null)
                return;
            grabInteractable.colliders.Clear();
            Collider activeCollider = diamond ? diamondCollider : cardCollider;
            if (activeCollider != null)
                grabInteractable.colliders.Add(activeCollider);
        }

        private void SetPinPosition(Vector3 localPosition)
        {
            LawyerCluePin pin = GetComponentInChildren<LawyerCluePin>(true);
            if (pin != null)
                pin.transform.localPosition = localPosition;
        }

        private Vector3 ResolvedBoardLocalPosition(Vector2 desiredPosition, Vector2 fallbackPosition)
        {
            Vector3 clamped = ClampedBoardLocalPosition(desiredPosition);
            Vector2 desired = new Vector2(clamped.x, clamped.y);
            if (boardController != null && boardController.TryResolveCluePosition(
                    this, desired, out Vector2 resolved))
                return new Vector3(resolved.x, resolved.y, localDepth);

            Vector3 fallback = ClampedBoardLocalPosition(fallbackPosition);
            return new Vector3(fallback.x, fallback.y, localDepth);
        }

        private Vector3 ClampedBoardLocalPosition()
        {
            Vector3 local = boardSurface.InverseTransformPoint(transform.position);
            return ClampedBoardLocalPosition(new Vector2(local.x, local.y));
        }

        private Vector3 ClampedBoardLocalPosition(Vector2 localPosition)
        {
            return new Vector3(
                Mathf.Clamp(localPosition.x,
                    snapCenter.x - halfBounds.x, snapCenter.x + halfBounds.x),
                Mathf.Clamp(localPosition.y,
                    snapCenter.y - halfBounds.y, snapCenter.y + halfBounds.y),
                localDepth);
        }
    }
}
