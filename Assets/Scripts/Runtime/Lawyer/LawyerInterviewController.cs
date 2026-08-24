using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerInterviewController : MonoBehaviour
    {
        public const string OfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";

        private enum InterviewView
        {
            MainQuestions,
            MainResponse,
            FollowUps,
            FollowUpResponse,
            Loading
        }

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
        [SerializeField, Min(0.1f)] private float navigationRepeatDelay = 0.3f;
        [SerializeField, Min(0.2f)] private float toastDuration = 2f;

        private LawyerGameSession session;
        private InputAction navigateAction;
        private InputAction confirmAction;
        private LawyerInterviewQuestion activeMainQuestion;
        private InterviewView view;
        private Vector2 navigationValue;
        private float nextNavigationTime;
        private int selectedMainIndex;
        private int selectedFollowUpIndex;
        private Coroutine toastRoutine;

        public event Action<string> QuestionPresented;
        public event Action<string, string> FollowUpPresented;
        public event Action InterviewCompleted;
        public LawyerGameSession Session => session;

#if UNITY_EDITOR
        public void ConfigureAssetReferencesInEditor(
            LawyerCaseDefinition definition,
            InputActionAsset inputControls)
        {
            caseDefinition = definition;
            controls = inputControls;
        }
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
            if (defendantNameLabel != null && caseDefinition != null)
                defendantNameLabel.text = caseDefinition.DefendantName;
            ConfigureButtons();
        }

        private void OnEnable()
        {
            session.ClueUnlocked += OnClueUnlocked;
            if (navigateAction != null)
            {
                navigateAction.performed += OnNavigatePerformed;
                navigateAction.canceled += OnNavigateCanceled;
                navigateAction.Enable();
            }
            if (confirmAction != null)
            {
                confirmAction.performed += OnConfirmPerformed;
                confirmAction.Enable();
            }
            if (responseContinueButton != null)
                responseContinueButton.onClick.AddListener(AdvanceResponse);
        }

        private void Start() => ShowMainQuestions();

        private void OnDisable()
        {
            if (session != null)
                session.ClueUnlocked -= OnClueUnlocked;
            if (navigateAction != null)
            {
                navigateAction.performed -= OnNavigatePerformed;
                navigateAction.canceled -= OnNavigateCanceled;
                navigateAction.Disable();
            }
            if (confirmAction != null)
            {
                confirmAction.performed -= OnConfirmPerformed;
                confirmAction.Disable();
            }
            if (responseContinueButton != null)
                responseContinueButton.onClick.RemoveListener(AdvanceResponse);
            StopVoice();
        }

        private void Update()
        {
            if ((view != InterviewView.MainQuestions && view != InterviewView.FollowUps)
                || Mathf.Abs(navigationValue.y) < navigationDeadzone || Time.unscaledTime < nextNavigationTime)
                return;
            CycleSelection(navigationValue.y < 0f ? 1 : -1);
            nextNavigationTime = Time.unscaledTime + navigationRepeatDelay;
        }

        private void ConfigureButtons()
        {
            for (int index = 0; index < mainQuestionButtons.Length; index++)
            {
                int capturedIndex = index;
                mainQuestionButtons[index]?.onClick.AddListener(() => AskMain(capturedIndex));
            }
            for (int index = 0; index < followUpButtons.Length; index++)
            {
                int capturedIndex = index;
                followUpButtons[index]?.onClick.AddListener(() => AskFollowUp(capturedIndex));
            }
        }

        private void ShowMainQuestions()
        {
            if (session != null && session.CanCompleteInterview)
            {
                CompleteInterview();
                return;
            }
            view = InterviewView.MainQuestions;
            SetActive(mainQuestionPanel, true);
            SetActive(responsePanel, false);
            SetActive(followUpPanel, false);
            IReadOnlyList<LawyerInterviewQuestion> questions = caseDefinition != null
                ? caseDefinition.Questions
                : Array.Empty<LawyerInterviewQuestion>();
            for (int index = 0; index < mainQuestionButtons.Length; index++)
            {
                Button button = mainQuestionButtons[index];
                bool exists = index < questions.Count && questions[index] != null;
                if (button == null)
                    continue;
                button.gameObject.SetActive(exists);
                if (!exists)
                    continue;
                bool asked = session.HasAskedMain(questions[index].Id);
                button.interactable = !asked && session.MainQuestionsRemaining > 0;
                SetButtonText(button, asked ? $"Đã hỏi: {questions[index].Prompt}" : questions[index].Prompt);
            }
            selectedMainIndex = FindNextAvailableMain(selectedMainIndex, 1);
            UpdateSelectionColors();
            if (questionCounterLabel != null)
                questionCounterLabel.text = $"Còn {session.MainQuestionsRemaining}/2 lượt hỏi  |  Cần trái: chọn  |  A: hỏi";
        }

        private void AskMain(int index)
        {
            if (view != InterviewView.MainQuestions || caseDefinition == null
                || index < 0 || index >= caseDefinition.Questions.Count)
                return;
            LawyerInterviewQuestion question = caseDefinition.Questions[index];
            if (!session.TryAskMain(question.Id, out LawyerInterviewQuestion accepted))
                return;
            activeMainQuestion = accepted;
            selectedMainIndex = index;
            view = InterviewView.MainResponse;
            QuestionPresented?.Invoke(accepted.Id);
            ShowResponse(accepted.Response, accepted.IsKeyQuestion
                ? "A hoặc Tiếp tục: chọn một câu hỏi tiếp theo"
                : "A hoặc Tiếp tục: quay lại danh sách câu hỏi");
        }

        private void ShowFollowUps()
        {
            view = InterviewView.FollowUps;
            SetActive(mainQuestionPanel, false);
            SetActive(responsePanel, false);
            SetActive(followUpPanel, true);
            for (int index = 0; index < followUpButtons.Length; index++)
            {
                Button button = followUpButtons[index];
                bool exists = activeMainQuestion != null && index < activeMainQuestion.FollowUps.Count;
                if (button == null)
                    continue;
                button.gameObject.SetActive(exists);
                button.interactable = exists;
                if (exists)
                    SetButtonText(button, activeMainQuestion.FollowUps[index].Prompt);
            }
            selectedFollowUpIndex = 0;
            UpdateSelectionColors();
        }

        private void AskFollowUp(int index)
        {
            if (view != InterviewView.FollowUps || activeMainQuestion == null
                || index < 0 || index >= activeMainQuestion.FollowUps.Count)
                return;
            LawyerFollowUpQuestion requested = activeMainQuestion.FollowUps[index];
            if (!session.TryAskFollowUp(activeMainQuestion.Id, requested.Id, out LawyerFollowUpQuestion accepted))
                return;
            selectedFollowUpIndex = index;
            view = InterviewView.FollowUpResponse;
            FollowUpPresented?.Invoke(activeMainQuestion.Id, accepted.Id);
            ShowResponse(accepted.Response, "A hoặc Tiếp tục: trở lại danh sách câu hỏi");
        }

        public void AdvanceResponse()
        {
            StopVoice();
            if (view == InterviewView.MainResponse)
            {
                if (activeMainQuestion != null && activeMainQuestion.IsKeyQuestion)
                    ShowFollowUps();
                else
                    ShowMainQuestions();
            }
            else if (view == InterviewView.FollowUpResponse)
            {
                activeMainQuestion = null;
                ShowMainQuestions();
            }
        }

        private void ShowResponse(LawyerTestimony testimony, string hint)
        {
            SetActive(mainQuestionPanel, false);
            SetActive(followUpPanel, false);
            SetActive(responsePanel, true);
            if (responseLabel != null)
                responseLabel.text = testimony != null ? testimony.Transcript : string.Empty;
            if (responseHintLabel != null)
                responseHintLabel.text = hint;
            StopVoice();
            if (defendantVoice != null && testimony != null && testimony.AudioClip != null)
            {
                defendantVoice.clip = testimony.AudioClip;
                defendantVoice.Play();
            }
        }

        private void CompleteInterview()
        {
            if (view == InterviewView.Loading || !session.TryCompleteInterview())
                return;
            view = InterviewView.Loading;
            InterviewCompleted?.Invoke();
            StartCoroutine(LoadOffice());
        }

        private IEnumerator LoadOffice()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(OfficeScenePath, LoadSceneMode.Single);
            if (operation == null)
                yield break;
            while (!operation.isDone)
                yield return null;
        }

        private void OnClueUnlocked(string clueId)
        {
            LawyerClueDefinition clue = caseDefinition != null ? caseDefinition.GetClue(clueId) : null;
            if (clue == null || toastLabel == null || toastPanel == null)
                return;
            if (toastRoutine != null)
                StopCoroutine(toastRoutine);
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

        private void OnNavigatePerformed(InputAction.CallbackContext context)
        {
            navigationValue = context.ReadValue<Vector2>();
            nextNavigationTime = 0f;
        }

        private void OnNavigateCanceled(InputAction.CallbackContext _) => navigationValue = Vector2.zero;

        private void OnConfirmPerformed(InputAction.CallbackContext _)
        {
            if (view == InterviewView.MainQuestions)
                AskMain(selectedMainIndex);
            else if (view == InterviewView.FollowUps)
                AskFollowUp(selectedFollowUpIndex);
            else if (view == InterviewView.MainResponse || view == InterviewView.FollowUpResponse)
                AdvanceResponse();
        }

        private void CycleSelection(int direction)
        {
            if (view == InterviewView.MainQuestions)
                selectedMainIndex = FindNextAvailableMain(selectedMainIndex + direction, direction);
            else if (view == InterviewView.FollowUps && activeMainQuestion != null)
                selectedFollowUpIndex = Wrap(selectedFollowUpIndex + direction, activeMainQuestion.FollowUps.Count);
            UpdateSelectionColors();
        }

        private int FindNextAvailableMain(int start, int direction)
        {
            int count = caseDefinition != null ? caseDefinition.Questions.Count : 0;
            if (count == 0)
                return -1;
            int candidate = Wrap(start, count);
            for (int attempts = 0; attempts < count; attempts++)
            {
                LawyerInterviewQuestion question = caseDefinition.Questions[candidate];
                if (question != null && !session.HasAskedMain(question.Id))
                    return candidate;
                candidate = Wrap(candidate + Math.Sign(direction == 0 ? 1 : direction), count);
            }
            return -1;
        }

        private void UpdateSelectionColors()
        {
            for (int index = 0; index < mainQuestionButtons.Length; index++)
                SetButtonColor(mainQuestionButtons[index], view == InterviewView.MainQuestions && index == selectedMainIndex);
            for (int index = 0; index < followUpButtons.Length; index++)
                SetButtonColor(followUpButtons[index], view == InterviewView.FollowUps && index == selectedFollowUpIndex);
        }

        private void SetButtonColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = selected ? selectedColor : normalColor;
        }

        private static void SetButtonText(Button button, string text)
        {
            TMP_Text label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            if (label != null)
                label.text = text ?? string.Empty;
        }

        private void StopVoice()
        {
            if (defendantVoice == null)
                return;
            defendantVoice.Stop();
            defendantVoice.clip = null;
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0)
                return -1;
            value %= count;
            return value < 0 ? value + count : value;
        }

        private static void SetActive(GameObject target, bool value)
        {
            if (target != null)
                target.SetActive(value);
        }
    }
}
