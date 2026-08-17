using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorNotebookUI : MonoBehaviour
    {
        [Header("Core HUD")]
        [SerializeField] private GameObject timerBadge;
        [SerializeField] private TMP_Text timerLabel;
        [SerializeField] private GameObject preparationOverlay;
        [SerializeField] private TMP_Text preparationLabel;
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text dialoguePatientName;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private TMP_Text hudHintLabel;
        [SerializeField] private GameObject questionPanel;
        [SerializeField] private TMP_Text questionCounterLabel;
        [SerializeField] private ScrollRect questionScroll;
        [SerializeField] private RectTransform questionArea;
        [SerializeField] private RectTransform questionContent;
        [SerializeField] private DoctorQuestionItemView questionItemTemplate;
        [SerializeField] private GameObject resultOverlay;
        [SerializeField] private TMP_Text resultScore;

        [Header("Question Layout")]
        [SerializeField, Min(1f)] private float questionPreferredPillHeight = 40f;
        [SerializeField, Min(0f)] private float questionSpacing = 8f;
        [SerializeField, Min(0f)] private float questionHorizontalPadding = 6f;
        [SerializeField, Min(0f)] private float questionVerticalPadding = 6f;
        [SerializeField, Min(1f)] private float questionMinimumFontSize = 5f;
        [SerializeField, Min(1f)] private float questionMaximumFontSize = 21f;

        [Header("Portrait Notebook")]
        [SerializeField] private GameObject notebookCanvasRoot;
        [SerializeField] private GameObject notebookOverlay;
        [SerializeField] private TMP_Text patientNameLabel;
        [SerializeField] private TMP_Text notebookHintLabel;
        [SerializeField] private RectTransform unfiledDrop;
        [SerializeField] private RectTransform unfiledContent;
        [SerializeField] private RectTransform timeDrop;
        [SerializeField] private RectTransform timeContent;
        [SerializeField] private RectTransform travelDrop;
        [SerializeField] private RectTransform travelContent;
        [SerializeField] private RectTransform historyDrop;
        [SerializeField] private RectTransform historyContent;
        [SerializeField] private RectTransform symptomsDrop;
        [SerializeField] private RectTransform symptomsContent;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private DoctorInfoCardView cardTemplate;

        [Header("Notebook Controls")]
        [SerializeField] private Button doneButton;
        [SerializeField] private Button unfiledDropButton;
        [SerializeField] private Button timeDropButton;
        [SerializeField] private Button travelDropButton;
        [SerializeField] private Button historyDropButton;
        [SerializeField] private Button symptomsDropButton;

        [Header("Configurable Controller Actions")]
        [SerializeField] private InputActionReference questionNavigateAction;
        [SerializeField] private InputActionReference questionSelectAction;
        [SerializeField] private InputActionReference notebookCloseAction;
        [SerializeField, Range(0.1f, 0.95f)] private float navigationDeadzone = 0.55f;
        [SerializeField, Min(0.05f)] private float navigationRepeatDelay = 0.35f;
        [SerializeField, Min(0.05f)] private float navigationRepeatRate = 0.16f;

        private readonly Dictionary<string, DoctorInfoCard> cardsById =
            new Dictionary<string, DoctorInfoCard>(StringComparer.Ordinal);
        private readonly Dictionary<string, DoctorInfoCardView> cardViews =
            new Dictionary<string, DoctorInfoCardView>(StringComparer.Ordinal);
        private readonly Dictionary<DoctorInfoCategory, RectTransform> dropZones =
            new Dictionary<DoctorInfoCategory, RectTransform>();
        private readonly Dictionary<DoctorInfoCategory, RectTransform> dropContents =
            new Dictionary<DoctorInfoCategory, RectTransform>();
#if UNITY_EDITOR
        [NonSerialized] private bool questionPreviewUpdateScheduled;
#endif
        private readonly List<DoctorQuestionPhase> visibleQuestions = new List<DoctorQuestionPhase>();
        private readonly List<DoctorQuestionItemView> questionViews = new List<DoctorQuestionItemView>();

        private IReadOnlyDictionary<string, DoctorInfoCategory?> placements;
        private string selectedCardId;
        private int selectedQuestionIndex;
        private Vector2 navigationValue;
        private float nextNavigationTime;
        private bool navigationLatched;
        private int displayedRoundSeconds = int.MinValue;
        private int displayedPreparationSeconds = int.MinValue;

        public event Action CloseRequested;
        public event Action DoneRequested;
        public event Action<string> QuestionRequested;
        public event Action<string, DoctorInfoCategory?> PlacementChanged;

        public bool IsNotebookOpen => notebookOverlay != null && notebookOverlay.activeInHierarchy;
        public bool IsQuestionHudOpen => questionPanel != null && questionPanel.activeInHierarchy;
        public int SelectedQuestionIndex => selectedQuestionIndex;

        private void Awake()
        {
            SetActive(notebookCanvasRoot, true);
            dropZones[DoctorInfoCategory.Time] = timeDrop;
            dropZones[DoctorInfoCategory.TravelHistory] = travelDrop;
            dropZones[DoctorInfoCategory.MedicalHistory] = historyDrop;
            dropZones[DoctorInfoCategory.Symptoms] = symptomsDrop;
            dropContents[DoctorInfoCategory.Time] = timeContent;
            dropContents[DoctorInfoCategory.TravelHistory] = travelContent;
            dropContents[DoctorInfoCategory.MedicalHistory] = historyContent;
            dropContents[DoctorInfoCategory.Symptoms] = symptomsContent;
            ConfigureQuestionViewport();
            HideAllPrimaryPanels();
        }

        private void OnEnable()
        {
            doneButton?.onClick.AddListener(HandleDone);
            unfiledDropButton?.onClick.AddListener(PlaceUnfiled);
            timeDropButton?.onClick.AddListener(PlaceTime);
            travelDropButton?.onClick.AddListener(PlaceTravel);
            historyDropButton?.onClick.AddListener(PlaceHistory);
            symptomsDropButton?.onClick.AddListener(PlaceSymptoms);
            Subscribe(questionNavigateAction, OnNavigatePerformed, OnNavigateCanceled);
            Subscribe(questionSelectAction, OnSelectQuestionPerformed, null);
            Subscribe(notebookCloseAction, OnCloseNotebookPerformed, null);
        }

        private void OnDisable()
        {
            doneButton?.onClick.RemoveListener(HandleDone);
            unfiledDropButton?.onClick.RemoveListener(PlaceUnfiled);
            timeDropButton?.onClick.RemoveListener(PlaceTime);
            travelDropButton?.onClick.RemoveListener(PlaceTravel);
            historyDropButton?.onClick.RemoveListener(PlaceHistory);
            symptomsDropButton?.onClick.RemoveListener(PlaceSymptoms);
            Unsubscribe(questionNavigateAction, OnNavigatePerformed, OnNavigateCanceled);
            Unsubscribe(questionSelectAction, OnSelectQuestionPerformed, null);
            Unsubscribe(notebookCloseAction, OnCloseNotebookPerformed, null);
        }

        private void Update()
        {
            if (!IsQuestionHudOpen || Mathf.Abs(navigationValue.y) < navigationDeadzone)
            {
                navigationLatched = false;
                return;
            }

            if (!navigationLatched || Time.unscaledTime >= nextNavigationTime)
            {
                CycleQuestion(navigationValue.y < 0f ? 1 : -1);
                nextNavigationTime = Time.unscaledTime + (navigationLatched ? navigationRepeatRate : navigationRepeatDelay);
                navigationLatched = true;
            }
        }

        public void ShowPreparation(float seconds)
        {
            HideAllPrimaryPanels();
            SetActive(preparationOverlay, true);
            int wholeSeconds = Mathf.Max(1, Mathf.CeilToInt(seconds));
            if (wholeSeconds != displayedPreparationSeconds)
            {
                displayedPreparationSeconds = wholeSeconds;
                preparationLabel.text = wholeSeconds.ToString();
            }
        }

        public void ShowRunning()
        {
            SetActive(preparationOverlay, false);
            SetActive(resultOverlay, false);
            SetActive(timerBadge, true);
        }

        public void SetRoundTime(float seconds)
        {
            int wholeSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            if (wholeSeconds == displayedRoundSeconds)
                return;
            displayedRoundSeconds = wholeSeconds;
            timerLabel.text = $"{wholeSeconds / 60:0}:{wholeSeconds % 60:00}";
        }

        public void ShowDialogue(string patientName, string transcript, string hint = "")
        {
            dialoguePatientName.text = patientName ?? string.Empty;
            dialogueText.text = transcript ?? string.Empty;
            hudHintLabel.text = hint ?? string.Empty;
            SetActive(dialoguePanel, true);
        }

        public void SetDialogueVisible(bool visible) => SetActive(dialoguePanel, visible);

        public void SetHudHint(string hint) => hudHintLabel.text = hint ?? string.Empty;
        public void SetNotebookHint(string hint) => notebookHintLabel.text = hint ?? string.Empty;

        public void OpenNotebook(
            string patientName,
            IReadOnlyList<DoctorInfoCard> cards,
            IReadOnlyDictionary<string, DoctorInfoCategory?> currentPlacements)
        {
            placements = currentPlacements;
            patientNameLabel.text = patientName ?? string.Empty;
            SetActive(notebookCanvasRoot, true);
            SetDialogueVisible(false);
            SetActive(questionPanel, false);
            SetActive(notebookOverlay, true);
            selectedCardId = null;
            CacheCards(cards);
            RebuildCards();
        }

        public void RefreshNotebook(
            IReadOnlyList<DoctorInfoCard> cards,
            IReadOnlyDictionary<string, DoctorInfoCategory?> currentPlacements)
        {
            placements = currentPlacements;
            CacheCards(cards);
            if (IsNotebookOpen)
                RebuildCards();
        }

        public void CloseNotebook() => SetActive(notebookOverlay, false);

        public void ShowQuestionHud(IReadOnlyList<DoctorQuestionPhase> questions, int preferredIndex)
        {
            visibleQuestions.Clear();
            if (questions != null)
                for (int i = 0; i < questions.Count; i++)
                    if (questions[i] != null)
                        visibleQuestions.Add(questions[i]);

            selectedQuestionIndex = visibleQuestions.Count == 0
                ? -1
                : Mathf.Clamp(preferredIndex, 0, visibleQuestions.Count - 1);
            SetActive(notebookOverlay, false);
            SetDialogueVisible(true);
            SetActive(questionPanel, visibleQuestions.Count > 0);
            RebuildQuestionList();
        }

        public void HideQuestionHud() => SetActive(questionPanel, false);

        public void ShowFinished(int score)
        {
            HideAllPrimaryPanels();
            SetActive(timerBadge, false);
            resultScore.text = $"TỔNG ĐIỂM\n{score}";
            SetActive(resultOverlay, true);
        }

        public void SelectCard(string cardId)
        {
            selectedCardId = cardId;
            UpdateSelectionStyles();
        }

        public void BeginCardDrag(DoctorInfoCardView view, PointerEventData eventData)
        {
            SelectCard(view.CardId);
            view.transform.SetParent(dragLayer, true);
            view.SetDragging(true);
            DragCard(view, eventData);
        }

        public void DragCard(DoctorInfoCardView view, PointerEventData eventData)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, eventData.position, eventData.pressEventCamera, out Vector2 point))
            {
                view.RectTransform.anchoredPosition = point;
            }
        }

        public void EndCardDrag(DoctorInfoCardView view, PointerEventData eventData)
        {
            view.SetDragging(false);
            foreach (KeyValuePair<DoctorInfoCategory, RectTransform> pair in dropZones)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(pair.Value, eventData.position, eventData.pressEventCamera))
                {
                    PlacementChanged?.Invoke(view.CardId, pair.Key);
                    return;
                }
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(unfiledDrop, eventData.position, eventData.pressEventCamera))
                PlacementChanged?.Invoke(view.CardId, null);
            else
                RebuildCards();
        }

        public void PlaceSelected(DoctorInfoCategory? category)
        {
            if (!string.IsNullOrEmpty(selectedCardId))
                PlacementChanged?.Invoke(selectedCardId, category);
        }

        private void CacheCards(IReadOnlyList<DoctorInfoCard> cards)
        {
            cardsById.Clear();
            if (cards == null)
                return;
            for (int i = 0; i < cards.Count; i++)
            {
                DoctorInfoCard card = cards[i];
                if (card != null && !string.IsNullOrWhiteSpace(card.Id) && !cardsById.ContainsKey(card.Id))
                    cardsById.Add(card.Id, card);
            }
        }

        private void RebuildCards()
        {
            foreach (DoctorInfoCardView existing in cardViews.Values)
                if (existing != null)
                    Destroy(existing.gameObject);
            cardViews.Clear();

            foreach (KeyValuePair<string, DoctorInfoCard> pair in cardsById)
            {
                DoctorInfoCategory? placement = null;
                placements?.TryGetValue(pair.Key, out placement);
                RectTransform parent = placement.HasValue ? dropContents[placement.Value] : unfiledContent;
                DoctorInfoCardView view = Instantiate(cardTemplate, parent);
                view.gameObject.SetActive(true);
                view.Initialize(this, pair.Key, pair.Value.Text);
                cardViews.Add(pair.Key, view);
            }
            UpdateCategoryHeight(timeDrop, timeContent);
            UpdateCategoryHeight(travelDrop, travelContent);
            UpdateCategoryHeight(historyDrop, historyContent);
            UpdateCategoryHeight(symptomsDrop, symptomsContent);
            UpdateSelectionStyles();
        }

        private static void UpdateCategoryHeight(RectTransform drop, RectTransform content)
        {
            if (drop == null || content == null || !drop.TryGetComponent(out LayoutElement layout))
                return;

            const float headerAndPadding = 68f;
            const float cardHeightAndSpacing = 90f;
            layout.preferredHeight = Mathf.Max(215f, headerAndPadding + content.childCount * cardHeightAndSpacing);
        }

        private void RebuildQuestionList()
        {
            for (int i = 0; i < questionViews.Count; i++)
                if (questionViews[i] != null)
                    Destroy(questionViews[i].gameObject);
            questionViews.Clear();
            NormalizeQuestionContentPosition();

            for (int i = 0; i < visibleQuestions.Count; i++)
            {
                DoctorQuestionItemView view = Instantiate(questionItemTemplate, questionContent);
                view.gameObject.SetActive(true);
                view.SetContent(visibleQuestions[i].Question, i == selectedQuestionIndex);
                questionViews.Add(view);
            }

            FitQuestionsToViewport();

            questionCounterLabel.text = visibleQuestions.Count == 0
                ? "Không còn câu hỏi"
                : $"{selectedQuestionIndex + 1}/{visibleQuestions.Count}  |  Cần trái: chọn  |  A: hỏi";
        }

        private void CycleQuestion(int direction)
        {
            selectedQuestionIndex = DoctorGameRules.CycleSelection(selectedQuestionIndex, direction, visibleQuestions.Count);
            for (int i = 0; i < questionViews.Count; i++)
                questionViews[i].SetContent(visibleQuestions[i].Question, i == selectedQuestionIndex);
            questionCounterLabel.text = $"{selectedQuestionIndex + 1}/{visibleQuestions.Count}  |  Cần trái: chọn  |  A: hỏi";
        }

        private void ConfigureQuestionViewport()
        {
            if (questionScroll == null)
                return;

            questionScroll.horizontal = false;
            questionScroll.vertical = false;
            if (questionScroll.verticalScrollbar != null)
                SetActive(questionScroll.verticalScrollbar.gameObject, false);
            questionScroll.enabled = false;
            ConfigureQuestionLayoutGeometry();
        }

        private void FitQuestionsToViewport()
        {
            RectTransform area = questionArea != null
                ? questionArea
                : questionScroll != null ? questionScroll.viewport : null;
            if (questionViews.Count == 0 || area == null || questionContent == null)
                return;

            VerticalLayoutGroup layout = questionContent != null
                ? questionContent.GetComponent<VerticalLayoutGroup>()
                : null;
            float spacing = Mathf.Max(0f, questionSpacing);
            float horizontalPadding = Mathf.Max(0f, questionHorizontalPadding);
            float verticalPadding = Mathf.Max(0f, questionVerticalPadding);
            if (layout != null)
            {
                layout.spacing = spacing;
                layout.padding = new RectOffset(
                    Mathf.RoundToInt(horizontalPadding),
                    Mathf.RoundToInt(horizontalPadding),
                    Mathf.RoundToInt(verticalPadding),
                    Mathf.RoundToInt(verticalPadding));
                layout.childAlignment = TextAnchor.LowerCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            Canvas.ForceUpdateCanvases();
            float itemHeight = DoctorGameRules.CalculateQuestionItemHeight(
                area.rect.height,
                questionViews.Count,
                verticalPadding,
                spacing,
                Mathf.Max(1f, questionPreferredPillHeight));
            float minimumFontSize = Mathf.Max(1f, questionMinimumFontSize);
            float maximumFontSize = Mathf.Max(minimumFontSize, questionMaximumFontSize);
            for (int i = 0; i < questionViews.Count; i++)
                questionViews[i].ConfigureLayout(itemHeight, minimumFontSize, maximumFontSize);

            float stackHeight = itemHeight * questionViews.Count
                + spacing * Mathf.Max(0, questionViews.Count - 1)
                + verticalPadding * 2f;
            Vector2 size = questionContent.sizeDelta;
            size.y = Mathf.Min(area.rect.height, stackHeight);
            questionContent.sizeDelta = size;
            LayoutRebuilder.ForceRebuildLayoutImmediate(questionContent);
            NormalizeQuestionContentPosition();
        }

        private void NormalizeQuestionContentPosition()
        {
            if (questionContent == null)
                return;

            questionContent.localScale = Vector3.one;
            questionContent.anchorMin = new Vector2(0f, 0f);
            questionContent.anchorMax = new Vector2(1f, 0f);
            questionContent.pivot = new Vector2(0.5f, 0f);
            questionContent.anchoredPosition = Vector2.zero;
            Vector2 size = questionContent.sizeDelta;
            size.x = 0f;
            questionContent.sizeDelta = size;
        }

        private void ConfigureQuestionLayoutGeometry()
        {
            if (questionArea == null && questionScroll != null)
                questionArea = questionScroll.viewport;

            if (questionContent != null && questionContent.TryGetComponent(out ContentSizeFitter fitter))
                fitter.enabled = false;
            NormalizeQuestionContentPosition();

            if (questionItemTemplate != null)
                questionItemTemplate.ConfigureLayout(
                    Mathf.Max(1f, questionPreferredPillHeight),
                    Mathf.Max(1f, questionMinimumFontSize),
                    Mathf.Max(questionMinimumFontSize, questionMaximumFontSize));
        }

        private void OnValidate()
        {
            questionPreferredPillHeight = Mathf.Max(1f, questionPreferredPillHeight);
            questionSpacing = Mathf.Max(0f, questionSpacing);
            questionHorizontalPadding = Mathf.Max(0f, questionHorizontalPadding);
            questionVerticalPadding = Mathf.Max(0f, questionVerticalPadding);
            questionMinimumFontSize = Mathf.Max(1f, questionMinimumFontSize);
            questionMaximumFontSize = Mathf.Max(questionMinimumFontSize, questionMaximumFontSize);

#if UNITY_EDITOR
            // RectTransform and layout changes are not legal during Unity's validation pass.
            // Defer the preview refresh so inspector edits still update the question template.
            if (!questionPreviewUpdateScheduled)
            {
                questionPreviewUpdateScheduled = true;
                UnityEditor.EditorApplication.delayCall += ApplyQuestionLayoutPreview;
            }
#endif
        }

#if UNITY_EDITOR
        private void ApplyQuestionLayoutPreview()
        {
            questionPreviewUpdateScheduled = false;
            if (this == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ConfigureQuestionLayoutGeometry();
        }
#endif

        private void UpdateSelectionStyles()
        {
            foreach (KeyValuePair<string, DoctorInfoCardView> pair in cardViews)
                pair.Value.SetSelected(pair.Key == selectedCardId);
        }

        private void HandleDone() => DoneRequested?.Invoke();
        private void PlaceUnfiled() => PlaceSelected(null);
        private void PlaceTime() => PlaceSelected(DoctorInfoCategory.Time);
        private void PlaceTravel() => PlaceSelected(DoctorInfoCategory.TravelHistory);
        private void PlaceHistory() => PlaceSelected(DoctorInfoCategory.MedicalHistory);
        private void PlaceSymptoms() => PlaceSelected(DoctorInfoCategory.Symptoms);

        private void OnNavigatePerformed(InputAction.CallbackContext context) => navigationValue = context.ReadValue<Vector2>();
        private void OnNavigateCanceled(InputAction.CallbackContext context) => navigationValue = Vector2.zero;

        private void OnSelectQuestionPerformed(InputAction.CallbackContext context)
        {
            if (IsQuestionHudOpen && selectedQuestionIndex >= 0 && selectedQuestionIndex < visibleQuestions.Count)
                QuestionRequested?.Invoke(visibleQuestions[selectedQuestionIndex].Id);
        }

        private void OnCloseNotebookPerformed(InputAction.CallbackContext context)
        {
            if (IsNotebookOpen)
                CloseRequested?.Invoke();
        }

        private static void Subscribe(
            InputActionReference reference,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null)
                return;
            if (performed != null)
                reference.action.performed += performed;
            if (canceled != null)
                reference.action.canceled += canceled;
            reference.action.Enable();
        }

        private static void Unsubscribe(
            InputActionReference reference,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null || reference.action == null)
                return;
            if (performed != null)
                reference.action.performed -= performed;
            if (canceled != null)
                reference.action.canceled -= canceled;
            reference.action.Disable();
        }

        private void HideAllPrimaryPanels()
        {
            SetActive(preparationOverlay, false);
            SetActive(dialoguePanel, false);
            SetActive(questionPanel, false);
            SetActive(notebookOverlay, false);
            SetActive(resultOverlay, false);
            SetActive(timerBadge, false);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }
    }
}
