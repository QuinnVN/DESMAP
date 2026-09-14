using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NCKH.VR.GameControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(LawyerActivationOnlySelectFilter))]
    public sealed class LawyerBoardController : MonoBehaviour
    {
        public const string CourtroomScenePath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";

        [Header("Data")]
        [SerializeField] private LawyerCaseDefinition caseDefinition;
        [SerializeField] private InputActionAsset controls;

        [Header("Board")]
        [SerializeField] private Transform boardSurface;
        [SerializeField] private Transform clueContainer;
        [SerializeField] private LawyerBoardClue clueTemplate;
        [SerializeField] private LawyerClueLinkSystem linkSystem;
        [SerializeField] private LawyerBoardFocusLighting focusLighting;
        [SerializeField] private Renderer evidenceSurfaceRenderer;
        [SerializeField] private Material reasoningCardMaterial;
        [SerializeField, Min(0.5f)] private float expandedDistance = 0.70f;
        [SerializeField] private float expandedVerticalOffset = 0.04f;
        [SerializeField, Min(0.01f)] private float animationDuration = 0.3f;

        [Header("Focus Placement")]
        [SerializeField] private Transform[] focusObstacleRoots = Array.Empty<Transform>();
        [SerializeField, Min(0f)] private float focusClearance = 0.03f;
        [SerializeField, Min(0f)] private float maximumFocusLift = 0.30f;
        [SerializeField, Min(0f)] private float maximumFocusPull = 0.15f;
        [SerializeField, Min(0.01f)] private float focusSearchStep = 0.05f;
        [SerializeField, Min(0f)] private float raisedArcLift = 0.25f;
        [SerializeField, Range(4, 32)] private int pathSamples = 12;

        [Header("Physical Briefing")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private TMP_Text timelineLabel;
        [SerializeField] private TMP_Text witnessesLabel;
        [SerializeField] private TMP_Text detailsLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text boardDefenseBriefLabel;
        [SerializeField] private GameObject boardDefenseBriefPanel;
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField, Min(0.1f)] private float toastDuration = 1.5f;
        [SerializeField] private LawyerPhysicalButton continueButton;
        [SerializeField] private LawyerPhysicalButton undoLinkButton;

        [Header("Evidence Finale")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleLabel;
        [SerializeField] private TMP_Text resultBodyLabel;
        [SerializeField] private TMP_Text defenseInstructionLabel;
        [SerializeField] private Button startDefenseButton;
        [SerializeField, Min(0f)] private float truthDisplayDuration = 15f;
        [SerializeField] private Vector2 truthFinalPosition = new Vector2(0f, 115f);
        [SerializeField] private Vector2 instructionFinalPosition = Vector2.zero;
        [SerializeField] private CanvasGroup introTitleGroup;
        [SerializeField] private TMP_Text introTitleLabel;
        [SerializeField, Min(0f)] private float introDisplayDuration = 2f;
        [SerializeField, Min(0.01f)] private float introFadeDuration = 0.75f;

        [Header("Modal Interaction")]
        [SerializeField] private XRRayInteractor rightRayInteractor;
        [SerializeField] private XRBaseInteractor[] modalInteractors = Array.Empty<XRBaseInteractor>();
        [SerializeField] private InteractionLayerMask boardInteractionMask;
        [SerializeField] private LayerMask boardPhysicsMask;

        private readonly List<InteractorState> savedInteractorStates = new List<InteractorState>();
        private readonly List<LawyerBoardClue> clueViews = new List<LawyerBoardClue>();
        private readonly List<LawyerBoardClue> reasoningViews = new List<LawyerBoardClue>();
        private readonly List<LawyerBoardClue> allPhysicalCards = new List<LawyerBoardClue>();
        private readonly List<float> clueCandidateXCoordinates = new List<float>();
        private readonly List<float> clueCandidateYCoordinates = new List<float>();
        private readonly List<Bounds> focusObstacleBounds = new List<Bounds>();
        private XRSimpleInteractable boardInteractable;
        private BoxCollider boardCollider;
        private LawyerGameSession session;
        private InputAction closeBoardAction;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private Vector3 originalScale;
        private Coroutine animationRoutine;
        private bool boardOpen;
        private bool animating;
        private LayerMask savedRaycastMask;
        private bool raycastMaskSaved;
        private bool contentInteractionEnabled;
        private bool usesRaisedFocusPath;
        private Vector3 focusArcControlPoint;
        private bool focusFinalRequiresOverlay;
        private bool focusPathRequiresOverlay;
        private Coroutine introTitleRoutine;
        private Coroutine finaleRoutine;
        private Coroutine statusFeedbackRoutine;
        private bool introTitleShown;
        private bool finaleSequenceStarted;
        private bool defenseStartRequested;

        private readonly struct InteractorState
        {
            public XRBaseInteractor Interactor { get; }
            public InteractionLayerMask InteractionLayers { get; }
            public InteractorState(XRBaseInteractor interactor)
            {
                Interactor = interactor;
                InteractionLayers = interactor != null ? interactor.interactionLayers : default;
            }
        }

        public event Action BoardOpened;
        public event Action CourtroomRequested;
        public event Action DefenseRequested;
        public bool IsOpen => boardOpen;
        public LawyerGameSession Session => session;

#if UNITY_EDITOR
        public void ConfigureAssetReferencesInEditor(
            LawyerCaseDefinition definition,
            InputActionAsset inputControls,
            LawyerBoardClue cardTemplate)
        {
            caseDefinition = definition;
            controls = inputControls;
            clueTemplate = cardTemplate;
        }
#endif

        private void Awake()
        {
            boardInteractable = GetComponent<XRSimpleInteractable>();
            boardCollider = GetComponent<BoxCollider>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            session = LawyerGameSession.Ensure(caseDefinition);
            linkSystem?.Configure(session);
            ResolveBoardDefenseBriefReferences();
            ConfigureBriefingText();
            if (resultPanel != null)
                resultPanel.SetActive(false);
            SetBoardDefenseBriefActive(false);
            if (startDefenseButton != null)
                startDefenseButton.gameObject.SetActive(false);
            if (introTitleGroup != null)
            {
                introTitleGroup.alpha = 0f;
                introTitleGroup.gameObject.SetActive(false);
            }
            closeBoardAction = controls != null ? controls.FindAction("CloseBoard", false) : null;
        }

        private void OnEnable()
        {
            if (boardInteractable == null)
                boardInteractable = GetComponent<XRSimpleInteractable>();
            boardInteractable.activated.AddListener(OnBoardActivated);
            if (continueButton != null)
                continueButton.Pressed += OnContinuePressed;
            if (undoLinkButton != null)
                undoLinkButton.Pressed += UndoLastLink;
            if (startDefenseButton != null)
                startDefenseButton.onClick.AddListener(StartDefense);
            if (session != null)
            {
                session.PhaseChanged += OnSessionPhaseChanged;
                session.ReasoningCardUnlocked += OnReasoningCardUnlocked;
            }
            if (linkSystem != null)
                linkSystem.LinkAttempted += OnLinkAttempted;
            if (closeBoardAction != null)
            {
                closeBoardAction.performed += OnCloseBoardPerformed;
                closeBoardAction.Enable();
            }
        }

        private void Start()
        {
            RebuildClues();
            SetContentInteraction(false);
            RefreshFinalePresentation();
            RefreshStatus();
            if (session != null && (session.Phase == LawyerSessionPhase.EvidenceBoard
                || session.Phase == LawyerSessionPhase.Resolved))
                StartCoroutine(OpenAfterSceneStart());
        }

        private void OnDisable()
        {
            if (boardInteractable != null)
                boardInteractable.activated.RemoveListener(OnBoardActivated);
            if (continueButton != null)
                continueButton.Pressed -= OnContinuePressed;
            if (undoLinkButton != null)
                undoLinkButton.Pressed -= UndoLastLink;
            if (startDefenseButton != null)
                startDefenseButton.onClick.RemoveListener(StartDefense);
            if (session != null)
            {
                session.PhaseChanged -= OnSessionPhaseChanged;
                session.ReasoningCardUnlocked -= OnReasoningCardUnlocked;
            }
            if (linkSystem != null)
                linkSystem.LinkAttempted -= OnLinkAttempted;
            if (closeBoardAction != null)
            {
                closeBoardAction.performed -= OnCloseBoardPerformed;
                closeBoardAction.Disable();
            }
            RestoreModalInteraction();
            SetContentInteraction(false);
            focusLighting?.SetOverlayActive(false);
            if (gameObject.activeInHierarchy && focusLighting != null
                && focusLighting.isActiveAndEnabled)
                focusLighting.SetFocused(false);
        }

        public void OpenBoard()
        {
            if (boardOpen || animating)
                return;
            session?.MarkBoardOpened();
            SetContentInteraction(session != null && (session.Phase == LawyerSessionPhase.CaseBrief
                || session.Phase == LawyerSessionPhase.EvidenceBoard));
            RestrictModalInteraction();
            focusLighting?.SetFocused(true);
            Camera camera = Camera.main;
            LawyerBoardFocusPlacementResult placement = CalculateFocusPlacement(camera);
            usesRaisedFocusPath = placement.UsesRaisedArc;
            focusArcControlPoint = placement.ArcControlPoint;
            focusFinalRequiresOverlay = placement.FinalOverlapVolume > 0.000001f;
            focusPathRequiresOverlay = placement.PathOverlapVolume > 0.000001f;
            focusLighting?.SetOverlayActive(
                focusFinalRequiresOverlay || focusPathRequiresOverlay);
            if (placement.OverlapVolume > 0.000001f)
                Debug.LogWarning("Không tìm thấy vị trí bảng hoàn toàn trống; đang dùng vị trí ít giao nhau nhất.", this);
            animationRoutine = StartCoroutine(AnimateBoard(
                placement.Position, placement.Rotation, originalScale, true,
                usesRaisedFocusPath, focusArcControlPoint));
            BoardOpened?.Invoke();
            RefreshStatus();
        }

        public void CloseBoard()
        {
            if (!boardOpen || animating || finaleSequenceStarted)
                return;
            SetContentInteraction(false);
            focusLighting?.SetOverlayActive(
                focusFinalRequiresOverlay || focusPathRequiresOverlay);
            focusLighting?.SetFocused(false);
            animationRoutine = StartCoroutine(AnimateBoard(
                originalPosition, originalRotation, originalScale, false,
                usesRaisedFocusPath, focusArcControlPoint));
        }

        public void ContinueToCourtroom()
        {
            if (session == null || !session.TryBeginInterview())
                return;
            CourtroomRequested?.Invoke();
            StartCoroutine(SceneTransitionService.LoadScene(CourtroomScenePath));
        }

        public void UndoLastLink() => linkSystem?.UndoLastLink();

        public void StartDefense()
        {
            if (defenseStartRequested || session == null || !session.TryBeginDefense())
                return;
            defenseStartRequested = true;
            if (startDefenseButton != null)
                startDefenseButton.interactable = false;
            DefenseRequested?.Invoke();
            StartCoroutine(SceneTransitionService.LoadScene(CourtroomScenePath));
        }

        private void OnContinuePressed()
        {
            ContinueToCourtroom();
        }

        private IEnumerator OpenAfterSceneStart()
        {
            yield return null;
            OpenBoard();
        }

        private void OnBoardActivated(ActivateEventArgs _) => OpenBoard();

        private void OnCloseBoardPerformed(InputAction.CallbackContext _)
        {
            if (boardOpen)
                CloseBoard();
        }

        private IEnumerator AnimateBoard(
            Vector3 targetPosition,
            Quaternion targetRotation,
            Vector3 targetScale,
            bool opening,
            bool raisedPath,
            Vector3 arcControlPoint)
        {
            animating = true;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animationDuration));
                transform.position = raisedPath
                    ? LawyerBoardFocusPlacement.QuadraticPoint(
                        startPosition, arcControlPoint, targetPosition, t)
                    : Vector3.Lerp(startPosition, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                transform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            transform.SetPositionAndRotation(targetPosition, targetRotation);
            transform.localScale = targetScale;
            boardOpen = opening;
            animating = false;
            animationRoutine = null;
            if (boardInteractable != null)
                boardInteractable.enabled = !opening;
            focusLighting?.SetOverlayActive(opening && focusFinalRequiresOverlay);
            RefreshStatus();
            if (opening && session != null && session.Phase == LawyerSessionPhase.EvidenceBoard)
                ShowIntroTitleOnce();
            if (!opening)
            {
                RestoreModalInteraction();
            }
        }

        private LawyerBoardFocusPlacementResult CalculateFocusPlacement(Camera camera)
        {
            Vector3 cameraPosition = camera != null ? camera.transform.position : transform.position;
            Vector3 cameraForward = camera != null ? camera.transform.forward : transform.forward;
            CollectFocusObstacleBounds();
            Vector3 colliderSize = boardCollider != null
                ? boardCollider.size
                : new Vector3(1.2f, 0.72f, 0.08f);
            Vector3 scale = new Vector3(
                Mathf.Abs(originalScale.x), Mathf.Abs(originalScale.y), Mathf.Abs(originalScale.z));
            Vector3 halfExtents = Vector3.Scale(colliderSize * 0.5f, scale);
            return LawyerBoardFocusPlacement.Calculate(
                cameraPosition,
                cameraForward,
                originalPosition,
                originalRotation,
                halfExtents,
                focusObstacleBounds,
                expandedDistance,
                expandedVerticalOffset,
                focusClearance,
                maximumFocusLift,
                maximumFocusPull,
                focusSearchStep,
                raisedArcLift,
                pathSamples);
        }

        private void CollectFocusObstacleBounds()
        {
            focusObstacleBounds.Clear();
            for (int rootIndex = 0; rootIndex < focusObstacleRoots.Length; rootIndex++)
            {
                Transform root = focusObstacleRoots[rootIndex];
                if (root == null)
                    continue;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                        focusObstacleBounds.Add(renderer.bounds);
                }
            }
        }

        private void ConfigureBriefingText()
        {
            if (caseDefinition == null)
                return;
            if (titleLabel != null)
                titleLabel.text = caseDefinition.CaseTitle;
            if (objectiveLabel != null)
                objectiveLabel.text = string.Empty;
            if (timelineLabel != null)
                timelineLabel.text = string.Empty;
            if (witnessesLabel != null)
                witnessesLabel.text = string.Empty;
            if (detailsLabel != null)
                detailsLabel.text = string.Empty;
        }

        private void RebuildClues()
        {
            if (clueContainer == null || clueTemplate == null || caseDefinition == null || session == null)
                return;
            if (!TryGetClueSnapArea(
                    out Vector2 snapCenter,
                    out Vector2 snapHalfBounds,
                    out _))
            {
                Debug.LogError("Không thể xác định vùng đặt manh mối trên bề mặt chứng cứ.", this);
                return;
            }
            clueViews.Clear();
            reasoningViews.Clear();
            for (int index = clueContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = clueContainer.GetChild(index);
                if (child == clueTemplate.transform)
                    continue;
                GameObject existing = child.gameObject;
                existing.SetActive(false);
                Destroy(existing);
            }

            bool keepEvidence = true;
            int displayedIndex = 0;
            IReadOnlyList<string> orderedEvidence = session.GetOrderedEvidenceClueIds();
            bool showOrderedEvidence = session.Phase >= LawyerSessionPhase.EvidenceBoard
                && orderedEvidence.Count > 0;
            int displayedCount = keepEvidence
                ? (showOrderedEvidence ? orderedEvidence.Count : session.UnlockedClueIds.Count)
                : 0;
            if (keepEvidence)
            {
                IEnumerable<string> displayedClues = showOrderedEvidence
                    ? orderedEvidence
                    : session.UnlockedClueIds;
                foreach (string clueId in displayedClues)
                {
                    LawyerClueDefinition clue = caseDefinition.GetClue(clueId);
                    if (clue == null)
                        continue;
                    LawyerBoardClue view = Instantiate(clueTemplate, clueContainer);
                    view.name = $"Manh mối - {clue.Id}";
                    Vector2 position = session.TryGetCluePosition(clue.Id, out Vector2 savedPosition)
                        ? savedPosition
                        : DefaultCluePosition(
                            displayedIndex++, displayedCount, snapCenter, snapHalfBounds);
                    view.Configure(
                        clue, boardSurface, snapCenter, snapHalfBounds, this, session);
                    view.SetBoardLocalPosition(position);
                    view.SetInteractionEnabled(false);
                    LawyerCluePin pin = view.GetComponentInChildren<LawyerCluePin>(true);
                    pin?.Configure(view, linkSystem);
                    view.gameObject.SetActive(true);
                    clueViews.Add(view);
                }
            }

            IReadOnlyList<LawyerReasoningCardDefinition> reasoningCards =
                caseDefinition.Resolution?.GetReasoningCardsOrFallback();
            int reasoningIndex = 0;
            int reasoningCount = session.UnlockedReasoningCardIds.Count;
            if (reasoningCards != null)
            {
                for (int index = 0; index < reasoningCards.Count; index++)
                {
                    LawyerReasoningCardDefinition card = reasoningCards[index];
                    if (card == null || !session.IsReasoningCardUnlocked(card.Id))
                        continue;
                    LawyerBoardClue view = Instantiate(clueTemplate, clueContainer);
                    view.name = $"Suy luận - {card.Id}";
                    Vector2 position = session.TryGetReasoningPosition(card.Id, out Vector2 savedPosition)
                        ? savedPosition
                        : DefaultCluePosition(
                            reasoningIndex++, reasoningCount, snapCenter, snapHalfBounds);
                    view.ConfigureReasoning(
                        card, boardSurface, snapCenter, snapHalfBounds, this, session,
                        reasoningCardMaterial);
                    view.SetBoardLocalPosition(position);
                    view.SetInteractionEnabled(false);
                    LawyerCluePin pin = view.GetComponentInChildren<LawyerCluePin>(true);
                    pin?.Configure(view, linkSystem);
                    view.gameObject.SetActive(true);
                    reasoningViews.Add(view);
                }
            }
            linkSystem?.RestoreCommittedLinks();
            AddTerminalReasoningLinks(reasoningCards);
            SetContentInteraction(contentInteractionEnabled && boardOpen);
        }

        private void AddTerminalReasoningLinks(
            IReadOnlyList<LawyerReasoningCardDefinition> reasoningCards)
        {
            if (linkSystem == null || reasoningCards == null)
                return;
            for (int index = 0; index < reasoningViews.Count; index++)
            {
                LawyerBoardClue reasoningView = reasoningViews[index];
                LawyerReasoningCardDefinition reasoning = null;
                for (int cardIndex = 0; cardIndex < reasoningCards.Count; cardIndex++)
                {
                    LawyerReasoningCardDefinition candidate = reasoningCards[cardIndex];
                    if (candidate != null && candidate.Id == reasoningView.CardId)
                    {
                        reasoning = candidate;
                        break;
                    }
                }
                if (reasoning == null || session == null
                    || !session.TryGetEvidenceLinkForReasoning(reasoning.Id, out LawyerClueLink evidenceLink))
                    continue;

                // A chain becomes a physical argument when its final evidence card is
                // connected to the revealed reasoning card. Keep the link terminal so it
                // survives undo and card movement while evidence remains on the board.
                LawyerCluePin evidencePin = FindCluePin(evidenceLink.SecondId);
                string evidenceId = evidenceLink.SecondId;
                if (evidencePin == null)
                {
                    evidencePin = FindCluePin(evidenceLink.FirstId);
                    evidenceId = evidenceLink.FirstId;
                }
                LawyerClueLink terminalLink = new LawyerClueLink(evidenceId, reasoning.Id);
                LawyerCluePin reasoningPin = FindReasoningPin(reasoning.Id);
                if (evidencePin != null && reasoningPin != null)
                    linkSystem.AddTerminalLink(terminalLink, evidencePin, reasoningPin);
            }
        }

        private LawyerCluePin FindCluePin(string clueId)
        {
            for (int index = 0; index < clueViews.Count; index++)
            {
                LawyerBoardClue view = clueViews[index];
                if (view != null && view.CardId == clueId)
                    return view.GetComponentInChildren<LawyerCluePin>(true);
            }
            return null;
        }

        private LawyerCluePin FindReasoningPin(string cardId)
        {
            for (int index = 0; index < reasoningViews.Count; index++)
            {
                LawyerBoardClue view = reasoningViews[index];
                if (view != null && view.CardId == cardId)
                    return view.GetComponentInChildren<LawyerCluePin>(true);
            }
            return null;
        }

        internal bool TryResolveCluePosition(
            LawyerBoardClue movingClue, Vector2 desiredPosition, out Vector2 resolvedPosition)
        {
            if (!TryGetClueSnapArea(
                    out Vector2 center,
                    out Vector2 safeHalfBounds,
                    out Vector2 clueHalfExtents))
            {
                resolvedPosition = desiredPosition;
                return false;
            }
            if (movingClue != null)
            {
                Vector2 actualHalfExtents = movingClue.BoardLocalHalfExtents;
                if (actualHalfExtents.sqrMagnitude > 0.000001f)
                {
                    safeHalfBounds -= new Vector2(
                        Mathf.Max(0f, actualHalfExtents.x - clueHalfExtents.x),
                        Mathf.Max(0f, actualHalfExtents.y - clueHalfExtents.y));
                    clueHalfExtents = actualHalfExtents;
                }
            }

            Vector2 minimum = center - safeHalfBounds;
            Vector2 maximum = center + safeHalfBounds;
            Vector2 desired = new Vector2(
                Mathf.Clamp(desiredPosition.x, minimum.x, maximum.x),
                Mathf.Clamp(desiredPosition.y, minimum.y, maximum.y));
            if (!OverlapsAnotherClue(movingClue, desired, clueHalfExtents))
            {
                resolvedPosition = desired;
                return true;
            }

            clueCandidateXCoordinates.Clear();
            AddCandidateCoordinate(clueCandidateXCoordinates, desired.x);
            AddCandidateCoordinate(clueCandidateXCoordinates, minimum.x);
            AddCandidateCoordinate(clueCandidateXCoordinates, maximum.x);
            for (int index = 0; index < AllPhysicalCards.Count; index++)
            {
                LawyerBoardClue other = AllPhysicalCards[index];
                if (other == null || other == movingClue || !other.gameObject.activeInHierarchy)
                    continue;
                Vector2 otherPosition = other.BoardLocalPosition;
                float separation = clueHalfExtents.x + other.BoardLocalHalfExtents.x;
                AddCandidateCoordinate(clueCandidateXCoordinates, Mathf.Clamp(
                    otherPosition.x - separation, minimum.x, maximum.x));
                AddCandidateCoordinate(clueCandidateXCoordinates, Mathf.Clamp(
                    otherPosition.x + separation, minimum.x, maximum.x));
            }

            clueCandidateYCoordinates.Clear();
            AddCandidateCoordinate(clueCandidateYCoordinates, desired.y);
            AddCandidateCoordinate(clueCandidateYCoordinates, minimum.y);
            AddCandidateCoordinate(clueCandidateYCoordinates, maximum.y);
            for (int index = 0; index < AllPhysicalCards.Count; index++)
            {
                LawyerBoardClue other = AllPhysicalCards[index];
                if (other == null || other == movingClue || !other.gameObject.activeInHierarchy)
                    continue;
                Vector2 otherPosition = other.BoardLocalPosition;
                float separation = clueHalfExtents.y + other.BoardLocalHalfExtents.y;
                AddCandidateCoordinate(clueCandidateYCoordinates, Mathf.Clamp(
                    otherPosition.y - separation, minimum.y, maximum.y));
                AddCandidateCoordinate(clueCandidateYCoordinates, Mathf.Clamp(
                    otherPosition.y + separation, minimum.y, maximum.y));
            }

            bool found = false;
            float bestDistance = float.PositiveInfinity;
            resolvedPosition = desired;
            for (int xIndex = 0; xIndex < clueCandidateXCoordinates.Count; xIndex++)
            for (int yIndex = 0; yIndex < clueCandidateYCoordinates.Count; yIndex++)
            {
                Vector2 candidate = new Vector2(
                    clueCandidateXCoordinates[xIndex], clueCandidateYCoordinates[yIndex]);
                if (OverlapsAnotherClue(movingClue, candidate, clueHalfExtents))
                    continue;
                float distance = (candidate - desired).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                found = true;
                bestDistance = distance;
                resolvedPosition = candidate;
            }
            return found;
        }

        private bool OverlapsAnotherClue(
            LawyerBoardClue movingClue, Vector2 position, Vector2 clueHalfExtents)
        {
            const float ContactTolerance = 0.0001f;
            for (int index = 0; index < AllPhysicalCards.Count; index++)
            {
                LawyerBoardClue other = AllPhysicalCards[index];
                if (other == null || other == movingClue || !other.gameObject.activeInHierarchy)
                    continue;
                Vector2 delta = other.BoardLocalPosition - position;
                Vector2 separation = clueHalfExtents + other.BoardLocalHalfExtents;
                if (Mathf.Abs(delta.x) + ContactTolerance < separation.x
                    && Mathf.Abs(delta.y) + ContactTolerance < separation.y)
                    return true;
            }
            return false;
        }

        private IReadOnlyList<LawyerBoardClue> AllPhysicalCards
        {
            get
            {
                allPhysicalCards.Clear();
                allPhysicalCards.AddRange(clueViews);
                allPhysicalCards.AddRange(reasoningViews);
                return allPhysicalCards;
            }
        }

        private static void AddCandidateCoordinate(List<float> coordinates, float value)
        {
            const float DuplicateTolerance = 0.0001f;
            for (int index = 0; index < coordinates.Count; index++)
                if (Mathf.Abs(coordinates[index] - value) < DuplicateTolerance)
                    return;
            coordinates.Add(value);
        }

        private bool TryGetClueSnapArea(
            out Vector2 center, out Vector2 halfBounds, out Vector2 clueHalfExtents)
        {
            center = Vector2.zero;
            halfBounds = Vector2.zero;
            clueHalfExtents = Vector2.zero;
            if (boardSurface == null || clueTemplate == null)
                return false;

            BoxCollider clueCollider = clueTemplate.GetComponent<BoxCollider>();
            if (clueCollider == null)
                return false;

            Bounds evidenceBounds;
            if (boardCollider != null)
            {
                evidenceBounds = BoundsInLocalSpace(
                    new Bounds(boardCollider.center, boardCollider.size),
                    transform, boardSurface);
            }
            else if (evidenceSurfaceRenderer != null)
            {
                MeshFilter evidenceMeshFilter = evidenceSurfaceRenderer.GetComponent<MeshFilter>();
                if (evidenceMeshFilter == null || evidenceMeshFilter.sharedMesh == null)
                    return false;
                evidenceBounds = BoundsInLocalSpace(
                    evidenceMeshFilter.sharedMesh.bounds,
                    evidenceSurfaceRenderer.transform,
                    boardSurface);
            }
            else
            {
                return false;
            }
            Bounds clueBounds = BoundsInLocalSpace(
                new Bounds(clueCollider.center, clueCollider.size),
                clueTemplate.transform,
                boardSurface);
            Transform templateVisual = clueTemplate.transform.Find("Card Visual");
            if (templateVisual != null)
            {
                float squareSize = Mathf.Max(
                    Mathf.Abs(templateVisual.localScale.x),
                    Mathf.Abs(templateVisual.localScale.y));
                Bounds diamondBounds = BoundsInLocalSpace(
                    new Bounds(Vector3.zero, new Vector3(squareSize, squareSize, 0.025f)),
                    clueTemplate.transform,
                    boardSurface);
                clueBounds.Encapsulate(diamondBounds.min);
                clueBounds.Encapsulate(diamondBounds.max);
            }
            center = new Vector2(evidenceBounds.center.x, evidenceBounds.center.y);
            clueHalfExtents = new Vector2(clueBounds.extents.x, clueBounds.extents.y);
            halfBounds = new Vector2(
                evidenceBounds.extents.x - clueHalfExtents.x,
                evidenceBounds.extents.y - clueHalfExtents.y);
            return halfBounds.x >= 0f && halfBounds.y >= 0f;
        }

        private static Bounds BoundsInLocalSpace(
            Bounds sourceBounds, Transform sourceSpace, Transform targetSpace)
        {
            Vector3 sourceCenter = sourceBounds.center;
            Vector3 sourceExtents = sourceBounds.extents;
            Bounds result = new Bounds(
                targetSpace.InverseTransformPoint(sourceSpace.TransformPoint(
                    sourceCenter - sourceExtents)),
                Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = sourceCenter + Vector3.Scale(
                    sourceExtents, new Vector3(x, y, z));
                result.Encapsulate(targetSpace.InverseTransformPoint(
                    sourceSpace.TransformPoint(corner)));
            }
            return result;
        }

        private void SetContentInteraction(bool value)
        {
            value = value && session != null && (session.Phase == LawyerSessionPhase.CaseBrief
                || session.Phase == LawyerSessionPhase.EvidenceBoard
                || session.Phase == LawyerSessionPhase.Resolved);
            contentInteractionEnabled = value;
            if (!value)
                linkSystem?.CancelActiveLink();
            for (int index = 0; index < clueViews.Count; index++)
                if (clueViews[index] != null)
                    clueViews[index].SetInteractionEnabled(value);
            for (int index = 0; index < reasoningViews.Count; index++)
                if (reasoningViews[index] != null)
                    reasoningViews[index].SetInteractionEnabled(value);
            if (undoLinkButton != null)
                undoLinkButton.SetInteractable(value
                    && session.Phase == LawyerSessionPhase.EvidenceBoard
                    && session.ClueLinks.Count > 0);
            RefreshStatus();
        }

        private static Vector2 DefaultCluePosition(
            int index, int count, Vector2 center, Vector2 halfBounds)
        {
            int columns = Mathf.Min(4, Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * 1.5f))));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            int column = index % columns;
            int row = index / columns;
            float x = columns == 1
                ? center.x
                : Mathf.Lerp(center.x - halfBounds.x, center.x + halfBounds.x,
                    column / (float)(columns - 1));
            float y = rows == 1
                ? center.y
                : Mathf.Lerp(center.y + halfBounds.y, center.y - halfBounds.y,
                    row / (float)(rows - 1));
            return new Vector2(x, y);
        }

        private void RefreshStatus()
        {
            if (continueButton != null)
            {
                bool caseBrief = session != null && session.Phase == LawyerSessionPhase.CaseBrief;
                continueButton.gameObject.SetActive(caseBrief);
                continueButton.SetLabel("Đến phòng xử án");
                continueButton.SetInteractable(boardOpen && !animating && caseBrief
                    && contentInteractionEnabled && session.BoardOpened);
            }
            if (undoLinkButton != null)
            {
                bool evidence = session != null && session.Phase == LawyerSessionPhase.EvidenceBoard;
                undoLinkButton.gameObject.SetActive(evidence);
                undoLinkButton.SetInteractable(evidence && contentInteractionEnabled
                    && session.ClueLinks.Count > 0);
            }
        }

        private void OnSessionPhaseChanged(LawyerSessionPhase phase)
        {
            if (phase != LawyerSessionPhase.Resolved)
                finaleSequenceStarted = false;
            if (phase == LawyerSessionPhase.Resolved)
            {
                if (boardOpen)
                    SetContentInteraction(true);
                StartCoroutine(RefreshReasoningAfterUnlock());
            }
            RefreshFinalePresentation();
            RefreshStatus();
        }

        private void OnReasoningCardUnlocked(string _)
        {
            if (isActiveAndEnabled)
                StartCoroutine(RefreshReasoningAfterUnlock());
        }

        private IEnumerator RefreshReasoningAfterUnlock()
        {
            // Let the link system finish committing the evidence line before adding the
            // revealed card. Keep the live evidence objects in place so a held pin or an
            // undo immediately after a valid link never points at a destroyed object.
            yield return null;
            if (session != null && !session.AllReasoningCardsUnlocked)
                AddUnlockedReasoningCardsWithoutRebuildingEvidence();
            else
                RebuildClues();
            if (session != null && session.AllReasoningCardsUnlocked)
                ShowTemporaryStatus("Đã hoàn tất các thẻ suy luận.");
        }

        private void AddUnlockedReasoningCardsWithoutRebuildingEvidence()
        {
            if (clueContainer == null || clueTemplate == null || caseDefinition == null
                || session == null || !TryGetClueSnapArea(
                    out Vector2 snapCenter, out Vector2 snapHalfBounds, out _))
                return;
            IReadOnlyList<LawyerReasoningCardDefinition> cards =
                caseDefinition.Resolution?.GetReasoningCardsOrFallback();
            if (cards == null)
                return;
            int nextPositionIndex = reasoningViews.Count;
            int unlockedCount = session.UnlockedReasoningCardIds.Count;
            for (int index = 0; index < cards.Count; index++)
            {
                LawyerReasoningCardDefinition card = cards[index];
                if (card == null || !session.IsReasoningCardUnlocked(card.Id)
                    || FindReasoningView(card.Id) != null)
                    continue;
                LawyerBoardClue view = Instantiate(clueTemplate, clueContainer);
                view.name = $"Suy luận - {card.Id}";
                Vector2 position = session.TryGetReasoningPosition(card.Id, out Vector2 savedPosition)
                    ? savedPosition
                    : DefaultCluePosition(
                        nextPositionIndex++, unlockedCount, snapCenter, snapHalfBounds);
                view.ConfigureReasoning(
                    card, boardSurface, snapCenter, snapHalfBounds, this, session,
                    reasoningCardMaterial);
                view.SetBoardLocalPosition(position);
                view.SetInteractionEnabled(false);
                LawyerCluePin pin = view.GetComponentInChildren<LawyerCluePin>(true);
                pin?.Configure(view, linkSystem);
                view.gameObject.SetActive(true);
                reasoningViews.Add(view);
            }
            linkSystem?.RestoreCommittedLinks();
            AddTerminalReasoningLinks(cards);
            SetContentInteraction(contentInteractionEnabled && boardOpen);
        }

        private LawyerBoardClue FindReasoningView(string cardId)
        {
            for (int index = 0; index < reasoningViews.Count; index++)
            {
                LawyerBoardClue view = reasoningViews[index];
                if (view != null && view.CardId == cardId)
                    return view;
            }
            return null;
        }

        private void OnLinkAttempted(LawyerLinkAttemptResult result)
        {
            if (result == LawyerLinkAttemptResult.Incorrect)
                ShowTemporaryStatus("Hai manh mối này không tạo thành mối liên hệ đúng.");
            else if (result == LawyerLinkAttemptResult.Duplicate)
                ShowTemporaryStatus("Hai manh mối này đã được liên kết.");
            else if (result == LawyerLinkAttemptResult.Added)
                RefreshStatus();
        }

        private void ShowTemporaryStatus(string message)
        {
            if (statusFeedbackRoutine != null)
                StopCoroutine(statusFeedbackRoutine);
            if (toastGroup != null)
                toastGroup.gameObject.SetActive(true);
            statusFeedbackRoutine = StartCoroutine(TemporaryStatusRoutine(message));
        }

        private IEnumerator TemporaryStatusRoutine(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            yield return new WaitForSecondsRealtime(toastDuration);
            statusFeedbackRoutine = null;
            if (statusLabel != null)
                statusLabel.text = string.Empty;
            if (toastGroup != null)
                toastGroup.gameObject.SetActive(false);
        }

        private void RefreshFinalePresentation()
        {
            bool resolved = session != null && session.Phase == LawyerSessionPhase.Resolved;
            if (resultPanel != null)
                resultPanel.SetActive(resolved);
            SetBoardDefenseBriefActive(false);
            if (!resolved)
                return;
            if (resultTitleLabel != null)
                resultTitleLabel.text = "SỰ THẬT";
            if (resultBodyLabel != null)
                resultBodyLabel.text = GetDefenseTruth();
            if (defenseInstructionLabel != null)
                defenseInstructionLabel.text = "Tự động chuyển đến phòng xử án sau ít giây...";
            if (!finaleSequenceStarted && isActiveAndEnabled)
            {
                finaleSequenceStarted = true;
                finaleRoutine = StartCoroutine(FinaleSequence());
            }
        }

        private void SetBoardDefenseBriefActive(bool value)
        {
            ResolveBoardDefenseBriefReferences();
            if (boardDefenseBriefLabel != null)
                boardDefenseBriefLabel.gameObject.SetActive(false);
            if (boardDefenseBriefPanel != null)
                boardDefenseBriefPanel.SetActive(false);
        }

        private void ResolveBoardDefenseBriefReferences()
        {
            if (boardDefenseBriefLabel == null)
                boardDefenseBriefLabel = transform.Find(
                    "Lawyer Board Content/Board Defense Brief Text")?.GetComponent<TMP_Text>();
            if (boardDefenseBriefPanel == null)
                boardDefenseBriefPanel = transform.Find(
                    "Lawyer Board Content/Board Defense Brief")?.gameObject;
        }

        private IEnumerator FinaleSequence()
        {
            if (startDefenseButton != null)
            {
                startDefenseButton.interactable = false;
                startDefenseButton.gameObject.SetActive(false);
            }

            while (!boardOpen || animating)
                yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, truthDisplayDuration));
            StartDefense();
            finaleRoutine = null;
        }

        private string GetDefenseTruth()
        {
            return caseDefinition != null && caseDefinition.Resolution != null
                ? caseDefinition.Resolution.TruthConclusion
                : string.Empty;
        }

        private void ShowIntroTitleOnce()
        {
            if (introTitleShown || introTitleGroup == null)
                return;
            introTitleShown = true;
            if (introTitleLabel != null)
                introTitleLabel.text = "Liên kết các cặp manh mối có quan hệ trực tiếp";
            if (introTitleRoutine != null)
                StopCoroutine(introTitleRoutine);
            introTitleRoutine = StartCoroutine(IntroTitleRoutine());
        }

        private IEnumerator IntroTitleRoutine()
        {
            introTitleGroup.gameObject.SetActive(true);
            introTitleGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(introDisplayDuration);
            float elapsed = 0f;
            while (elapsed < introFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                introTitleGroup.alpha = 1f - Mathf.Clamp01(elapsed / introFadeDuration);
                yield return null;
            }
            introTitleGroup.alpha = 0f;
            introTitleGroup.gameObject.SetActive(false);
            introTitleRoutine = null;
        }

        private void RestrictModalInteraction()
        {
            savedInteractorStates.Clear();
            for (int index = 0; index < modalInteractors.Length; index++)
            {
                XRBaseInteractor candidate = modalInteractors[index];
                if (candidate == null)
                    continue;
                savedInteractorStates.Add(new InteractorState(candidate));
                candidate.interactionLayers = candidate is XRPokeInteractor
                    ? default
                    : boardInteractionMask;
            }
            if (rightRayInteractor != null)
            {
                savedRaycastMask = rightRayInteractor.raycastMask;
                raycastMaskSaved = true;
                rightRayInteractor.raycastMask = boardPhysicsMask;
            }
        }

        private void RestoreModalInteraction()
        {
            for (int index = 0; index < savedInteractorStates.Count; index++)
            {
                InteractorState state = savedInteractorStates[index];
                if (state.Interactor != null)
                    state.Interactor.interactionLayers = state.InteractionLayers;
            }
            savedInteractorStates.Clear();
            if (raycastMaskSaved && rightRayInteractor != null)
                rightRayInteractor.raycastMask = savedRaycastMask;
            raycastMaskSaved = false;
        }

    }
}
