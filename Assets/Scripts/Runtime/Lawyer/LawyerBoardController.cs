using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
        [SerializeField] private GameObject focusBackdrop;
        [SerializeField] private LawyerBoardFocusLighting focusLighting;
        [SerializeField] private Vector2 clueHalfBounds = new Vector2(0.48f, 0.28f);
        [SerializeField, Min(0.5f)] private float expandedDistance = 0.85f;
        [SerializeField] private float expandedVerticalOffset = -0.12f;
        [SerializeField, Min(0.01f)] private float animationDuration = 0.3f;

        [Header("Physical Briefing")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text objectiveLabel;
        [SerializeField] private TMP_Text timelineLabel;
        [SerializeField] private TMP_Text witnessesLabel;
        [SerializeField] private TMP_Text detailsLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private LawyerPhysicalButton continueButton;
        [SerializeField] private LawyerPhysicalButton undoLinkButton;

        [Header("Modal Interaction")]
        [SerializeField] private XRRayInteractor rightRayInteractor;
        [SerializeField] private XRBaseInteractor[] modalInteractors = Array.Empty<XRBaseInteractor>();
        [SerializeField] private InteractionLayerMask boardInteractionMask;
        [SerializeField] private LayerMask boardPhysicsMask;

        private readonly List<InteractorState> savedInteractorStates = new List<InteractorState>();
        private readonly List<LawyerBoardClue> clueViews = new List<LawyerBoardClue>();
        private XRSimpleInteractable boardInteractable;
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
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            session = LawyerGameSession.Ensure(caseDefinition);
            linkSystem?.Configure(session);
            ConfigureBriefingText();
            if (focusBackdrop != null)
                focusBackdrop.SetActive(false);
            closeBoardAction = controls != null ? controls.FindAction("CloseBoard", false) : null;
        }

        private void OnEnable()
        {
            if (boardInteractable == null)
                boardInteractable = GetComponent<XRSimpleInteractable>();
            boardInteractable.activated.AddListener(OnBoardActivated);
            if (continueButton != null)
                continueButton.Pressed += ContinueToCourtroom;
            if (undoLinkButton != null)
                undoLinkButton.Pressed += UndoLastLink;
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
            RefreshStatus();
            if (session != null && session.Phase == LawyerSessionPhase.EvidenceBoardPreview)
                StartCoroutine(OpenAfterSceneStart());
        }

        private void OnDisable()
        {
            if (boardInteractable != null)
                boardInteractable.activated.RemoveListener(OnBoardActivated);
            if (continueButton != null)
                continueButton.Pressed -= ContinueToCourtroom;
            if (undoLinkButton != null)
                undoLinkButton.Pressed -= UndoLastLink;
            if (closeBoardAction != null)
            {
                closeBoardAction.performed -= OnCloseBoardPerformed;
                closeBoardAction.Disable();
            }
            RestoreModalInteraction();
            SetContentInteraction(false);
            if (gameObject.activeInHierarchy && focusLighting != null
                && focusLighting.isActiveAndEnabled)
                focusLighting.SetFocused(false);
        }

        public void OpenBoard()
        {
            if (boardOpen || animating)
                return;
            session?.MarkBoardOpened();
            SetContentInteraction(true);
            RestrictModalInteraction();
            if (focusBackdrop != null)
                focusBackdrop.SetActive(true);
            focusLighting?.SetFocused(true);
            Vector3 horizontalForward = CameraForwardOnFloor();
            Camera camera = Camera.main;
            Vector3 cameraPosition = camera != null ? camera.transform.position : transform.position;
            Vector3 targetPosition = cameraPosition
                + horizontalForward * expandedDistance + Vector3.up * expandedVerticalOffset;
            Quaternion targetRotation = Quaternion.LookRotation(
                cameraPosition - targetPosition, Vector3.up);
            animationRoutine = StartCoroutine(AnimateBoard(
                targetPosition, targetRotation, originalScale, true));
            BoardOpened?.Invoke();
            RefreshStatus();
        }

        public void CloseBoard()
        {
            if (!boardOpen || animating)
                return;
            SetContentInteraction(false);
            focusLighting?.SetFocused(false);
            animationRoutine = StartCoroutine(AnimateBoard(
                originalPosition, originalRotation, originalScale, false));
        }

        public void ContinueToCourtroom()
        {
            if (session == null || !session.TryBeginInterview())
                return;
            CourtroomRequested?.Invoke();
            StartCoroutine(LoadScene(CourtroomScenePath));
        }

        public void UndoLastLink() => linkSystem?.UndoLastLink();

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
            bool opening)
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
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
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
            if (!opening)
            {
                if (focusBackdrop != null)
                    focusBackdrop.SetActive(false);
                RestoreModalInteraction();
            }
        }

        private void ConfigureBriefingText()
        {
            if (caseDefinition == null)
                return;
            if (titleLabel != null)
                titleLabel.text = caseDefinition.CaseTitle;
            if (objectiveLabel != null)
                objectiveLabel.text = "MỤC TIÊU ĐIỀU TRA\n" + caseDefinition.InvestigationObjective;
            if (timelineLabel != null)
                timelineLabel.text = BuildList("DÒNG THỜI GIAN", caseDefinition.Timeline);
            if (witnessesLabel != null)
                witnessesLabel.text = BuildList("NHÂN CHỨNG", caseDefinition.Witnesses);
            if (detailsLabel != null)
                detailsLabel.text = BuildList("CHI TIẾT VỤ ÁN", caseDefinition.CaseDetails);
        }

        private static string BuildList(string heading, IReadOnlyList<string> items)
        {
            StringBuilder builder = new StringBuilder(256);
            builder.AppendLine(heading);
            for (int index = 0; index < items.Count; index++)
                builder.Append("• ").AppendLine(items[index]);
            return builder.ToString();
        }

        private void RebuildClues()
        {
            if (clueContainer == null || clueTemplate == null || caseDefinition == null || session == null)
                return;
            clueViews.Clear();
            for (int index = clueContainer.childCount - 1; index >= 0; index--)
            {
                Transform child = clueContainer.GetChild(index);
                if (child == clueTemplate.transform)
                    continue;
                GameObject existing = child.gameObject;
                existing.SetActive(false);
                Destroy(existing);
            }

            int displayedIndex = 0;
            foreach (string clueId in session.UnlockedClueIds)
            {
                LawyerClueDefinition clue = caseDefinition.GetClue(clueId);
                if (clue == null)
                    continue;
                LawyerBoardClue view = Instantiate(clueTemplate, clueContainer);
                view.name = $"Manh mối - {clue.Id}";
                Vector2 position = session.TryGetCluePosition(clue.Id, out Vector2 savedPosition)
                    ? savedPosition
                    : DefaultCluePosition(displayedIndex++);
                view.Configure(clue, boardSurface, clueHalfBounds, session);
                view.SetBoardLocalPosition(position);
                view.SetInteractionEnabled(false);
                LawyerCluePin pin = view.GetComponentInChildren<LawyerCluePin>(true);
                pin?.Configure(view, linkSystem);
                view.gameObject.SetActive(true);
                clueViews.Add(view);
            }
            linkSystem?.RestoreCommittedLinks();
        }

        private void SetContentInteraction(bool value)
        {
            contentInteractionEnabled = value;
            if (!value)
                linkSystem?.CancelActiveLink();
            for (int index = 0; index < clueViews.Count; index++)
                if (clueViews[index] != null)
                    clueViews[index].SetInteractionEnabled(value);
            if (undoLinkButton != null)
                undoLinkButton.SetInteractable(value);
            RefreshStatus();
        }

        private static Vector2 DefaultCluePosition(int index)
        {
            int column = index % 2;
            int row = index / 2;
            return new Vector2(0.16f + column * 0.26f, 0.16f - row * 0.16f);
        }

        private void RefreshStatus()
        {
            bool preview = session != null && session.Phase == LawyerSessionPhase.EvidenceBoardPreview;
            if (statusLabel != null)
            {
                statusLabel.text = preview
                    ? "Giai đoạn 3 sẽ được phát triển sau. Bạn có thể sắp xếp và nối các manh mối."
                    : "Kéo thẻ để sắp xếp. Kéo chốt đỏ sang chốt khác để nối dây. Nhấn B để đóng bảng.";
            }
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(!preview);
                continueButton.SetInteractable(contentInteractionEnabled && session != null && session.BoardOpened
                    && session.Phase == LawyerSessionPhase.CaseBrief);
            }
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

        private static Vector3 CameraForwardOnFloor()
        {
            Camera camera = Camera.main;
            if (camera == null)
                return Vector3.forward;
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        private static IEnumerator LoadScene(string scenePath)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null)
                yield break;
            while (!operation.isDone)
                yield return null;
        }
    }
}
