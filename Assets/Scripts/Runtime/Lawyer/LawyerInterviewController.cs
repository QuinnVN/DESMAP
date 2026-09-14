using System;
using System.Collections;
using NCKH.VR.GameControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerInterviewController : MonoBehaviour
    {
        public const string OfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";
        private enum InterviewView { Questions, Response, InsufficientEvidence, Loading }

        [Header("Data")]
        [SerializeField] private LawyerCaseDefinition caseDefinition;
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private AudioSource defendantVoice;
        [Header("Panels")]
        [SerializeField] private GameObject mainQuestionPanel;
        [SerializeField] private GameObject responsePanel;
        [SerializeField] private GameObject followUpPanel;
        [SerializeField] private GameObject toastPanel;
        [Header("Text")]
        [SerializeField] private TMP_Text defendantNameLabel;
        [SerializeField] private TMP_Text questionCounterLabel;
        [SerializeField] private TMP_Text responseLabel;
        [SerializeField] private TMP_Text responseHintLabel;
        [SerializeField] private TMP_Text toastLabel;
        [Header("Controls")]
        [SerializeField] private Button[] mainQuestionButtons = Array.Empty<Button>();
        [SerializeField] private Button[] followUpButtons = Array.Empty<Button>();
        [SerializeField] private Button responseContinueButton;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.15f, 0.17f, 0.94f);
        [SerializeField] private Color selectedColor = new Color(0.19f, 0.53f, 0.48f, 1f);
        [Header("Navigation")]
        [SerializeField, Range(0.1f, 0.95f)] private float navigationDeadzone = 0.65f;
        [SerializeField, Min(0.1f)] private float navigationRepeatDelay = 0.6f;
        [SerializeField, Min(0.05f)] private float navigationRepeatRate = 0.35f;
        [SerializeField, Min(0.2f)] private float toastDuration = 2f;

        private LawyerGameSession session;
        private InputAction navigateAction;
        private InputAction confirmAction;
        private InputAction closeBoardAction;
        private InterviewView view;
        private Vector2 navigationValue;
        private float nextNavigationTime;
        private bool navigationLatched;
        private int selectedMainIndex;
        private Coroutine toastRoutine;
        private bool transitionRequested;

        public event Action<string> QuestionPresented;
        public event Action<string, string> FollowUpPresented;
        public event Action InterviewCompleted;
        public LawyerGameSession Session => session;

#if UNITY_EDITOR
        public void ConfigureAssetReferencesInEditor(LawyerCaseDefinition definition, InputActionAsset inputControls)
        { caseDefinition = definition; controls = inputControls; }
#endif

        private void Awake()
        {
            session = LawyerGameSession.Ensure(caseDefinition);
            if (session.Phase == LawyerSessionPhase.CaseBrief)
            {
                session.MarkBoardOpened();
                session.TryBeginInterview();
            }
            navigateAction = controls != null ? controls.FindAction("NavigateQuestions", false) : null;
            confirmAction = controls != null ? controls.FindAction("Confirm", false) : null;
            closeBoardAction = controls != null ? controls.FindAction("CloseBoard", false) : null;
            if (defendantNameLabel != null && caseDefinition != null) defendantNameLabel.text = caseDefinition.DefendantName;
            for (int index = 0; index < mainQuestionButtons.Length; index++)
            {
                int captured = index;
                mainQuestionButtons[index]?.onClick.AddListener(() => AskQuestion(captured));
            }
            for (int index = 0; index < followUpButtons.Length; index++)
                if (followUpButtons[index] != null) followUpButtons[index].gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            session.ClueUnlocked += OnClueUnlocked;
            if (navigateAction != null) { navigateAction.performed += OnNavigate; navigateAction.canceled += OnNavigateCanceled; navigateAction.Enable(); }
            if (confirmAction != null) { confirmAction.performed += OnConfirm; confirmAction.Enable(); }
            if (closeBoardAction != null) { closeBoardAction.performed += OnClose; closeBoardAction.Enable(); }
            responseContinueButton?.onClick.AddListener(Advance);
        }
        private void Start() => ShowQuestions();
        private void OnDisable()
        {
            if (session != null) session.ClueUnlocked -= OnClueUnlocked;
            if (navigateAction != null) { navigateAction.performed -= OnNavigate; navigateAction.canceled -= OnNavigateCanceled; navigateAction.Disable(); }
            if (confirmAction != null) { confirmAction.performed -= OnConfirm; confirmAction.Disable(); }
            if (closeBoardAction != null) { closeBoardAction.performed -= OnClose; closeBoardAction.Disable(); }
            responseContinueButton?.onClick.RemoveListener(Advance);
            StopVoice();
        }
        private void Update()
        {
            if (view != InterviewView.Questions || Mathf.Abs(navigationValue.y) < navigationDeadzone)
            { navigationLatched = false; return; }
            if (navigationLatched && Time.unscaledTime < nextNavigationTime) return;
            selectedMainIndex = Wrap(selectedMainIndex + (navigationValue.y < 0f ? 1 : -1), CurrentQuestionCount());
            UpdateSelection();
            nextNavigationTime = Time.unscaledTime + (navigationLatched ? navigationRepeatRate : navigationRepeatDelay);
            navigationLatched = true;
        }

        private void ShowQuestions()
        {
            if (session.CanCompleteInterview) { CompleteInterview(); return; }
            view = InterviewView.Questions;
            SetActive(mainQuestionPanel, true);
            SetActive(responsePanel, false);
            SetActive(followUpPanel, false);
            LawyerInterviewWave wave = session.CurrentWave;
            int count = wave?.Questions?.Count ?? 0;
            for (int index = 0; index < mainQuestionButtons.Length; index++)
            {
                Button button = mainQuestionButtons[index];
                if (button == null) continue;
                bool exists = index < count && wave.Questions[index] != null;
                button.gameObject.SetActive(exists);
                button.interactable = exists;
                if (exists) SetButtonText(button, wave.Questions[index].Prompt);
            }
            selectedMainIndex = Wrap(selectedMainIndex, count);
            UpdateSelection();
            if (questionCounterLabel != null)
                questionCounterLabel.text = $"Đợt {session.CurrentWaveIndex + 1}/{session.TotalWaves} | Cần trái: chọn | A: hỏi";
        }

        private void AskQuestion(int index)
        {
            LawyerInterviewWave wave = session.CurrentWave;
            if (view != InterviewView.Questions || wave == null || index < 0 || index >= wave.Questions.Count) return;
            LawyerInterviewQuestion requested = wave.Questions[index];
            if (requested == null || !session.TryAskMain(requested.Id, out LawyerInterviewQuestion accepted)) return;
            selectedMainIndex = index;
            view = InterviewView.Response;
            QuestionPresented?.Invoke(accepted.Id);
            ShowResponse(accepted.Response);
        }

        public void Advance()
        {
            StopVoice();
            if (view == InterviewView.InsufficientEvidence) RestartInterview();
            else if (view == InterviewView.Response) ShowQuestions();
        }
        public void AdvanceResponse() => Advance();

        private void ShowResponse(LawyerTestimony testimony)
        {
            SetActive(mainQuestionPanel, false);
            SetActive(followUpPanel, false);
            SetActive(responsePanel, true);
            if (responseLabel != null) responseLabel.text = testimony?.Transcript ?? string.Empty;
            if (responseHintLabel != null) responseHintLabel.text = session.CanCompleteInterview
                ? "A/Tiếp tục: kiểm tra chứng cứ" : "A/Tiếp tục: sang đợt hỏi tiếp theo";
            SetButtonText(responseContinueButton, "Tiếp tục");
            if (responseContinueButton != null) responseContinueButton.interactable = true;
            StopVoice();
            if (defendantVoice != null && testimony?.AudioClip != null)
            { defendantVoice.clip = testimony.AudioClip; defendantVoice.Play(); }
        }

        private void CompleteInterview()
        {
            if (transitionRequested || view == InterviewView.Loading || session == null) return;
            if (!session.EvaluateEvidenceAvailability()) { ShowInsufficientEvidence(); return; }
            if (!session.TryCompleteInterview()) return;
            transitionRequested = true;
            view = InterviewView.Loading;
            InterviewCompleted?.Invoke();
            StartCoroutine(LoadOffice());
        }
        private void ShowInsufficientEvidence()
        {
            view = InterviewView.InsufficientEvidence;
            StopVoice();
            if (toastRoutine != null) { StopCoroutine(toastRoutine); toastRoutine = null; }
            SetActive(mainQuestionPanel, false);
            SetActive(responsePanel, true);
            SetActive(followUpPanel, false);
            SetActive(toastPanel, false);
            string message = caseDefinition?.Resolution?.InsufficientEvidenceMessage ?? string.Empty;
            if (responseLabel != null) responseLabel.text = $"<size=42><b>CHƯA ĐỦ CHỨNG CỨ</b></size>\n\n{message}";
            if (responseHintLabel != null) responseHintLabel.text = "A: phỏng vấn lại từ đợt 1";
            SetButtonText(responseContinueButton, "Phỏng vấn lại");
            if (responseContinueButton != null) responseContinueButton.interactable = true;
        }
        private void RestartInterview()
        {
            if (view != InterviewView.InsufficientEvidence || !session.TryRestartInterview()) return;
            selectedMainIndex = 0;
            ShowQuestions();
        }
        private IEnumerator LoadOffice() { yield return SceneTransitionService.LoadScene(OfficeScenePath); }

        private void OnClueUnlocked(string clueId)
        {
            LawyerClueDefinition clue = caseDefinition?.GetClue(clueId);
            if (clue == null || toastLabel == null || toastPanel == null) return;
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ShowToast($"Đã phát hiện manh mối: {clue.Title}"));
        }
        private IEnumerator ShowToast(string message)
        {
            toastLabel.text = message;
            toastPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(toastDuration);
            toastPanel.SetActive(false);
            toastRoutine = null;
        }
        private void OnNavigate(InputAction.CallbackContext context) => navigationValue = context.ReadValue<Vector2>();
        private void OnNavigateCanceled(InputAction.CallbackContext _) { navigationValue = Vector2.zero; navigationLatched = false; }
        private void OnConfirm(InputAction.CallbackContext _)
        {
            if (view == InterviewView.Questions) AskQuestion(selectedMainIndex);
            else Advance();
        }
        private void OnClose(InputAction.CallbackContext _) { }
        private int CurrentQuestionCount() => session?.CurrentWave?.Questions?.Count ?? 0;
        private void UpdateSelection()
        {
            for (int index = 0; index < mainQuestionButtons.Length; index++)
            {
                Button button = mainQuestionButtons[index];
                if (button?.targetGraphic != null) button.targetGraphic.color =
                    view == InterviewView.Questions && index == selectedMainIndex ? selectedColor : normalColor;
            }
        }
        private void AskMain(int index) => AskQuestion(index);
        private void StopVoice()
        {
            if (defendantVoice == null) return;
            defendantVoice.Stop();
            defendantVoice.clip = null;
        }
        private static void SetButtonText(Button button, string text)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null) label.text = text ?? string.Empty;
        }
        private static int Wrap(int value, int count)
        {
            if (count <= 0) return -1;
            value %= count;
            return value < 0 ? value + count : value;
        }
        private static void SetActive(GameObject target, bool active) { if (target != null) target.SetActive(active); }
    }
}
