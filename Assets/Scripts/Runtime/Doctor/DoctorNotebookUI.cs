using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PanelRenderer))]
    public sealed class DoctorNotebookUI : MonoBehaviour
    {
        private const float DragThreshold = 6f;

        [SerializeField] private PanelRenderer panelRenderer;
        [SerializeField] private GameObject locomotionRoot;

        private readonly Dictionary<string, DoctorInfoCard> cardsById =
            new Dictionary<string, DoctorInfoCard>(StringComparer.Ordinal);
        private readonly Dictionary<DoctorInfoCategory, VisualElement> dropZones =
            new Dictionary<DoctorInfoCategory, VisualElement>();
        private readonly Dictionary<DoctorInfoCategory, VisualElement> dropContents =
            new Dictionary<DoctorInfoCategory, VisualElement>();
        private readonly Dictionary<string, VisualElement> cardElements =
            new Dictionary<string, VisualElement>(StringComparer.Ordinal);

        private VisualElement timerBadge;
        private Label timerLabel;
        private VisualElement preparationOverlay;
        private Label preparationLabel;
        private VisualElement notebookOverlay;
        private Label patientNameLabel;
        private Label questionCountLabel;
        private Label notebookHintLabel;
        private VisualElement unfiledDrop;
        private VisualElement unfiledContent;
        private VisualElement questionModal;
        private VisualElement conclusionOverlay;
        private Label conclusionTitle;
        private Label conclusionBody;
        private VisualElement resultOverlay;
        private Label resultScore;
        private VisualElement dragLayer;
        private Button askButton;
        private Button diagnoseButton;
        private Button closeButton;
        private Button questionTimeButton;
        private Button questionTravelButton;
        private Button questionSymptomsButton;
        private Button questionHistoryButton;
        private Button questionCancelButton;

        private IReadOnlyDictionary<string, DoctorInfoCategory?> placements;
        private DoctorQuestionTracker questionTracker;
        private string selectedCardId;
        private string draggedCardId;
        private VisualElement draggedElement;
        private Vector3 dragStartPosition;
        private int dragPointerId = -1;
        private bool dragInProgress;
        private bool initialized;
        private bool locomotionLocked;
        private bool locomotionWasActive;
        private int displayedRoundSeconds = int.MinValue;
        private int displayedPreparationSeconds = int.MinValue;
        private VisualElement rootVisualElement;
        private PanelRenderer.UIReloadCallback uiReloadCallback;

        public event Action CloseRequested;
        public event Action<DoctorInfoCategory> QuestionRequested;
        public event Action DiagnoseRequested;
        public event Action<string, DoctorInfoCategory?> PlacementChanged;

        public bool IsNotebookOpen => initialized
            && notebookOverlay.style.display == DisplayStyle.Flex;

        private void OnEnable()
        {
            if (panelRenderer == null)
                panelRenderer = GetComponent<PanelRenderer>();

            uiReloadCallback = OnUiReloaded;
            panelRenderer?.RegisterUIReloadCallback(uiReloadCallback);
        }

        private void OnDisable()
        {
            if (panelRenderer != null && uiReloadCallback != null)
                panelRenderer.UnregisterUIReloadCallback(uiReloadCallback);

            rootVisualElement = null;
            initialized = false;
            RestoreLocomotion();
        }

        public void ShowPreparation(float seconds)
        {
            if (!EnsureInitialized())
                return;
            HideAllPrimaryPanels();
            preparationOverlay.style.display = DisplayStyle.Flex;
            int wholeSeconds = Mathf.Max(1, Mathf.CeilToInt(seconds));
            if (wholeSeconds != displayedPreparationSeconds)
            {
                displayedPreparationSeconds = wholeSeconds;
                preparationLabel.text = wholeSeconds.ToString();
            }
            SetLocomotionLocked(true);
        }

        public void ShowRunning()
        {
            if (!EnsureInitialized())
                return;
            preparationOverlay.style.display = DisplayStyle.None;
            resultOverlay.style.display = DisplayStyle.None;
            conclusionOverlay.style.display = DisplayStyle.None;
            timerBadge.style.display = DisplayStyle.Flex;
            SetLocomotionLocked(false);
        }

        public void SetRoundTime(float seconds)
        {
            if (!EnsureInitialized())
                return;
            int wholeSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            if (wholeSeconds == displayedRoundSeconds)
                return;

            displayedRoundSeconds = wholeSeconds;
            timerLabel.text = $"{wholeSeconds / 60:0}:{wholeSeconds % 60:00}";
        }

        public void SetNotebookHint(string text)
        {
            if (!EnsureInitialized())
                return;
            notebookHintLabel.text = text ?? string.Empty;
        }

        public void OpenNotebook(
            string patientName,
            IReadOnlyList<DoctorInfoCard> cards,
            IReadOnlyDictionary<string, DoctorInfoCategory?> currentPlacements,
            DoctorQuestionTracker tracker)
        {
            if (!EnsureInitialized())
                return;
            placements = currentPlacements;
            questionTracker = tracker;
            patientNameLabel.text = patientName ?? string.Empty;
            questionModal.style.display = DisplayStyle.None;
            conclusionOverlay.style.display = DisplayStyle.None;
            notebookOverlay.style.display = DisplayStyle.Flex;
            selectedCardId = null;
            CacheCards(cards);
            RefreshQuestionControls();
            RebuildCards();
            SetLocomotionLocked(true);
        }

        public void RefreshNotebook(
            IReadOnlyList<DoctorInfoCard> cards,
            IReadOnlyDictionary<string, DoctorInfoCategory?> currentPlacements,
            DoctorQuestionTracker tracker)
        {
            if (!IsNotebookOpen)
                return;

            placements = currentPlacements;
            questionTracker = tracker;
            CacheCards(cards);
            RefreshQuestionControls();
            RebuildCards();
        }

        public void CloseNotebook()
        {
            if (!EnsureInitialized())
                return;
            CancelDrag();
            questionModal.style.display = DisplayStyle.None;
            notebookOverlay.style.display = DisplayStyle.None;
            selectedCardId = null;
            SetLocomotionLocked(false);
        }

        public void ShowConclusion(string conclusion, string treatment)
        {
            if (!EnsureInitialized())
                return;
            notebookOverlay.style.display = DisplayStyle.None;
            questionModal.style.display = DisplayStyle.None;
            conclusionTitle.text = conclusion ?? string.Empty;
            conclusionBody.text = treatment ?? string.Empty;
            conclusionOverlay.style.display = DisplayStyle.Flex;
            SetLocomotionLocked(true);
        }

        public void HideConclusion()
        {
            if (!EnsureInitialized())
                return;
            conclusionOverlay.style.display = DisplayStyle.None;
            SetLocomotionLocked(false);
        }

        public void ShowFinished(int score)
        {
            if (!EnsureInitialized())
                return;
            HideAllPrimaryPanels();
            timerBadge.style.display = DisplayStyle.None;
            resultScore.text = $"TOTAL SCORE\n{score}";
            resultOverlay.style.display = DisplayStyle.Flex;
            SetLocomotionLocked(true);
        }

        private void OnUiReloaded(PanelRenderer source, VisualElement rootElement)
        {
            if (source != panelRenderer || rootElement == null)
                return;

            rootVisualElement = rootElement;
            initialized = false;
            dropZones.Clear();
            dropContents.Clear();
            EnsureInitialized();
        }

        private bool EnsureInitialized()
        {
            if (initialized)
                return true;

            VisualElement root = rootVisualElement;
            if (root == null)
                return false;

            timerBadge = root.Q<VisualElement>("timer-badge");
            timerLabel = root.Q<Label>("timer-label");
            preparationOverlay = root.Q<VisualElement>("preparation-overlay");
            preparationLabel = root.Q<Label>("preparation-label");
            notebookOverlay = root.Q<VisualElement>("notebook-overlay");
            patientNameLabel = root.Q<Label>("patient-name-label");
            questionCountLabel = root.Q<Label>("question-count-label");
            notebookHintLabel = root.Q<Label>("notebook-hint-label");
            unfiledDrop = root.Q<VisualElement>("unfiled-drop");
            unfiledContent = root.Q<VisualElement>("unfiled-content");
            questionModal = root.Q<VisualElement>("question-modal");
            conclusionOverlay = root.Q<VisualElement>("conclusion-overlay");
            conclusionTitle = root.Q<Label>("conclusion-title");
            conclusionBody = root.Q<Label>("conclusion-body");
            resultOverlay = root.Q<VisualElement>("result-overlay");
            resultScore = root.Q<Label>("result-score");
            dragLayer = root.Q<VisualElement>("drag-layer");
            askButton = root.Q<Button>("ask-button");
            diagnoseButton = root.Q<Button>("diagnose-button");
            closeButton = root.Q<Button>("close-button");
            questionTimeButton = root.Q<Button>("question-time-button");
            questionTravelButton = root.Q<Button>("question-travel-button");
            questionSymptomsButton = root.Q<Button>("question-symptoms-button");
            questionHistoryButton = root.Q<Button>("question-history-button");
            questionCancelButton = root.Q<Button>("question-cancel-button");

            RegisterDropZone(root, DoctorInfoCategory.Time, "time-drop", "time-content");
            RegisterDropZone(root, DoctorInfoCategory.TravelHistory, "travel-drop", "travel-content");
            RegisterDropZone(root, DoctorInfoCategory.Symptoms, "symptoms-drop", "symptoms-content");
            RegisterDropZone(root, DoctorInfoCategory.MedicalHistory, "history-drop", "history-content");

            closeButton.clicked += () => CloseRequested?.Invoke();
            askButton.clicked += OpenQuestionModal;
            diagnoseButton.clicked += () => DiagnoseRequested?.Invoke();
            questionTimeButton.clicked += () => RequestQuestion(DoctorInfoCategory.Time);
            questionTravelButton.clicked += () => RequestQuestion(DoctorInfoCategory.TravelHistory);
            questionSymptomsButton.clicked += () => RequestQuestion(DoctorInfoCategory.Symptoms);
            questionHistoryButton.clicked += () => RequestQuestion(DoctorInfoCategory.MedicalHistory);
            questionCancelButton.clicked += () => questionModal.style.display = DisplayStyle.None;
            unfiledDrop.RegisterCallback<ClickEvent>(_ => PlaceSelectedCard(null));

            initialized = true;
            HideAllPrimaryPanels();
            timerBadge.style.display = DisplayStyle.None;
            return true;
        }

        private void RegisterDropZone(
            VisualElement root,
            DoctorInfoCategory category,
            string zoneName,
            string contentName)
        {
            VisualElement zone = root.Q<VisualElement>(zoneName);
            dropZones.Add(category, zone);
            dropContents.Add(category, root.Q<VisualElement>(contentName));
            zone.RegisterCallback<ClickEvent>(_ => PlaceSelectedCard(category));
        }

        private void CacheCards(IReadOnlyList<DoctorInfoCard> cards)
        {
            cardsById.Clear();
            if (cards == null)
                return;

            for (int i = 0; i < cards.Count; i++)
            {
                DoctorInfoCard card = cards[i];
                if (card == null || string.IsNullOrWhiteSpace(card.Id) || cardsById.ContainsKey(card.Id))
                    continue;
                cardsById.Add(card.Id, card);
            }
        }

        private void RebuildCards()
        {
            CancelDragWithoutRebuild();
            unfiledContent.Clear();
            foreach (VisualElement content in dropContents.Values)
                content.Clear();
            cardElements.Clear();

            foreach (KeyValuePair<string, DoctorInfoCard> pair in cardsById)
            {
                DoctorInfoCategory? placement = null;
                if (placements != null)
                    placements.TryGetValue(pair.Key, out placement);

                VisualElement card = CreateCard(pair.Value);
                cardElements.Add(pair.Key, card);
                if (placement.HasValue)
                    dropContents[placement.Value].Add(card);
                else
                    unfiledContent.Add(card);
            }

            UpdateSelectionStyles();
        }

        private VisualElement CreateCard(DoctorInfoCard cardData)
        {
            VisualElement card = new VisualElement();
            card.name = $"card-{cardData.Id}";
            card.AddToClassList("info-card");
            card.focusable = true;
            card.Add(new Label(cardData.Text));
            card.RegisterCallback<PointerDownEvent>(evt => OnCardPointerDown(evt, cardData.Id, card));
            card.RegisterCallback<PointerMoveEvent>(OnCardPointerMove);
            card.RegisterCallback<PointerUpEvent>(OnCardPointerUp);
            card.RegisterCallback<PointerCancelEvent>(OnCardPointerCancel);
            return card;
        }

        private void OnCardPointerDown(PointerDownEvent evt, string cardId, VisualElement card)
        {
            if (draggedElement != null)
                return;

            selectedCardId = cardId;
            draggedCardId = cardId;
            draggedElement = card;
            dragStartPosition = evt.position;
            dragPointerId = evt.pointerId;
            dragInProgress = false;
            card.CapturePointer(evt.pointerId);
            UpdateSelectionStyles();
            evt.StopPropagation();
        }

        private void OnCardPointerMove(PointerMoveEvent evt)
        {
            if (draggedElement == null || evt.pointerId != dragPointerId)
                return;

            if (!dragInProgress
                && Vector3.Distance(evt.position, dragStartPosition) < DragThreshold)
            {
                return;
            }

            if (!dragInProgress)
            {
                dragInProgress = true;
                float width = Mathf.Max(160f, draggedElement.resolvedStyle.width);
                draggedElement.style.width = width;
                draggedElement.style.position = Position.Absolute;
                draggedElement.AddToClassList("dragging");
                dragLayer.Add(draggedElement);
            }

            draggedElement.style.left = evt.position.x - draggedElement.resolvedStyle.width * 0.5f;
            draggedElement.style.top = evt.position.y - draggedElement.resolvedStyle.height * 0.5f;
            evt.StopPropagation();
        }

        private void OnCardPointerUp(PointerUpEvent evt)
        {
            if (draggedElement == null || evt.pointerId != dragPointerId)
                return;

            draggedElement.ReleasePointer(evt.pointerId);
            string cardId = draggedCardId;
            bool wasDragging = dragInProgress;
            Vector2 pointerPosition = evt.position;
            CancelDragWithoutRebuild();

            if (wasDragging && TryGetDropCategory(pointerPosition, out DoctorInfoCategory? category))
                PlacementChanged?.Invoke(cardId, category);
            else
                RebuildCards();

            evt.StopPropagation();
        }

        private void OnCardPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == dragPointerId)
                CancelDrag();
        }

        private bool TryGetDropCategory(Vector2 panelPosition, out DoctorInfoCategory? category)
        {
            foreach (KeyValuePair<DoctorInfoCategory, VisualElement> pair in dropZones)
            {
                if (pair.Value.worldBound.Contains(panelPosition))
                {
                    category = pair.Key;
                    return true;
                }
            }

            if (unfiledDrop.worldBound.Contains(panelPosition))
            {
                category = null;
                return true;
            }

            category = null;
            return false;
        }

        private void PlaceSelectedCard(DoctorInfoCategory? category)
        {
            if (string.IsNullOrEmpty(selectedCardId))
                return;

            PlacementChanged?.Invoke(selectedCardId, category);
        }

        private void OpenQuestionModal()
        {
            if (questionTracker == null || !questionTracker.CanAskAny)
                return;
            questionModal.style.display = DisplayStyle.Flex;
        }

        private void RequestQuestion(DoctorInfoCategory category)
        {
            questionModal.style.display = DisplayStyle.None;
            QuestionRequested?.Invoke(category);
        }

        private void RefreshQuestionControls()
        {
            int asked = questionTracker != null ? questionTracker.QuestionsAsked : 0;
            int maximum = questionTracker != null ? questionTracker.MaximumQuestions : 0;
            questionCountLabel.text = $"Questions: {asked}/{maximum}";
            askButton.SetEnabled(questionTracker != null && questionTracker.CanAskAny);
            SetQuestionButtonEnabled(questionTimeButton, DoctorInfoCategory.Time);
            SetQuestionButtonEnabled(questionTravelButton, DoctorInfoCategory.TravelHistory);
            SetQuestionButtonEnabled(questionSymptomsButton, DoctorInfoCategory.Symptoms);
            SetQuestionButtonEnabled(questionHistoryButton, DoctorInfoCategory.MedicalHistory);
        }

        private void SetQuestionButtonEnabled(Button button, DoctorInfoCategory category)
        {
            button.SetEnabled(questionTracker != null
                && questionTracker.CanAskAny
                && !questionTracker.HasAsked(category));
        }

        private void UpdateSelectionStyles()
        {
            foreach (KeyValuePair<string, VisualElement> pair in cardElements)
                pair.Value.EnableInClassList("selected", pair.Key == selectedCardId);
        }

        private void CancelDrag()
        {
            bool hadDrag = draggedElement != null;
            CancelDragWithoutRebuild();
            if (hadDrag)
                RebuildCards();
        }

        private void CancelDragWithoutRebuild()
        {
            if (draggedElement != null && dragPointerId >= 0 && draggedElement.HasPointerCapture(dragPointerId))
                draggedElement.ReleasePointer(dragPointerId);

            draggedElement = null;
            draggedCardId = null;
            dragPointerId = -1;
            dragInProgress = false;
        }

        private void HideAllPrimaryPanels()
        {
            if (!initialized)
                return;

            preparationOverlay.style.display = DisplayStyle.None;
            notebookOverlay.style.display = DisplayStyle.None;
            questionModal.style.display = DisplayStyle.None;
            conclusionOverlay.style.display = DisplayStyle.None;
            resultOverlay.style.display = DisplayStyle.None;
        }

        private void SetLocomotionLocked(bool locked)
        {
            if (locomotionRoot == null || locomotionLocked == locked)
                return;

            if (locked)
            {
                locomotionWasActive = locomotionRoot.activeSelf;
                locomotionLocked = true;
                if (locomotionWasActive)
                    locomotionRoot.SetActive(false);
                return;
            }

            RestoreLocomotion();
        }

        private void RestoreLocomotion()
        {
            if (!locomotionLocked)
                return;

            if (locomotionRoot != null && locomotionRoot.activeSelf != locomotionWasActive)
                locomotionRoot.SetActive(locomotionWasActive);
            locomotionLocked = false;
        }
    }
}
