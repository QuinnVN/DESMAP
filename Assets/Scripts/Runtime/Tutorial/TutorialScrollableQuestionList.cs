using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NCKH.VR.Tutorial
{
    [DisallowMultipleComponent]
    public sealed class TutorialScrollableQuestionList : MonoBehaviour
    {
        [SerializeField] private InputActionAsset controls;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button[] questionButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TutorialRayHoverLocomotionGate rayHoverGate;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.15f, 0.17f, 0.96f);
        [SerializeField] private Color selectedColor = new Color(0.19f, 0.53f, 0.48f, 1f);
        [SerializeField, Range(0.1f, 0.95f)] private float navigationDeadzone = 0.65f;

        private InputAction navigateAction;
        private InputAction confirmAction;
        private Vector2 navigationValue;
        private bool navigationLatched;
        private int selectedIndex;
        private bool listenersBound;

        public void Configure(
            InputActionAsset configuredControls,
            ScrollRect configuredScrollRect,
            Button[] configuredButtons,
            TMP_Text configuredStatusLabel,
            TutorialRayHoverLocomotionGate configuredRayHoverGate)
        {
            controls = configuredControls;
            scrollRect = configuredScrollRect;
            questionButtons = configuredButtons ?? Array.Empty<Button>();
            statusLabel = configuredStatusLabel;
            rayHoverGate = configuredRayHoverGate;
        }

        private void Awake()
        {
            navigateAction = controls != null
                ? controls.FindAction("NavigateQuestions", false)
                : null;
            confirmAction = controls != null
                ? controls.FindAction("Confirm", false)
                : null;
            BindButtons();
            UpdateSelection(false);
        }

        private void OnEnable()
        {
            if (navigateAction == null && controls != null)
                navigateAction = controls.FindAction("NavigateQuestions", false);
            if (confirmAction == null && controls != null)
                confirmAction = controls.FindAction("Confirm", false);

            if (navigateAction != null)
            {
                navigateAction.performed += OnNavigate;
                navigateAction.canceled += OnNavigateCanceled;
                navigateAction.Enable();
            }

            if (confirmAction != null)
            {
                confirmAction.performed += OnConfirm;
                confirmAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (navigateAction != null)
            {
                navigateAction.performed -= OnNavigate;
                navigateAction.canceled -= OnNavigateCanceled;
                navigateAction.Disable();
            }

            if (confirmAction != null)
            {
                confirmAction.performed -= OnConfirm;
                confirmAction.Disable();
            }

            navigationValue = Vector2.zero;
            navigationLatched = false;
        }

        private void Update()
        {
            if (questionButtons.Length == 0)
                return;

            bool navigationIsNeutral =
                Mathf.Abs(navigationValue.y) < navigationDeadzone;
            if (rayHoverGate == null || !rayHoverGate.IsRayHovering)
            {
                navigationLatched = !navigationIsNeutral;
                return;
            }

            if (navigationIsNeutral)
            {
                navigationLatched = false;
                return;
            }

            if (navigationLatched)
                return;

            int direction = navigationValue.y < 0f ? 1 : -1;
            selectedIndex = Wrap(selectedIndex + direction, questionButtons.Length);
            UpdateSelection(false);
            navigationLatched = true;
        }

        private void BindButtons()
        {
            if (listenersBound)
                return;

            for (int index = 0; index < questionButtons.Length; index++)
            {
                int capturedIndex = index;
                if (questionButtons[index] != null)
                    questionButtons[index].onClick.AddListener(
                        () => SelectAndConfirm(capturedIndex));
            }

            listenersBound = true;
        }

        private void SelectAndConfirm(int index)
        {
            if (index < 0 || index >= questionButtons.Length)
                return;
            selectedIndex = index;
            UpdateSelection(true);
        }

        private void OnNavigate(InputAction.CallbackContext context)
        {
            navigationValue = context.ReadValue<Vector2>();
        }

        private void OnNavigateCanceled(InputAction.CallbackContext _)
        {
            navigationValue = Vector2.zero;
            navigationLatched = false;
        }

        private void OnConfirm(InputAction.CallbackContext _)
        {
            if (rayHoverGate == null || !rayHoverGate.IsRayHovering)
                return;

            ConfirmSelection();
        }

        private void ConfirmSelection()
        {
            if (selectedIndex < 0 || selectedIndex >= questionButtons.Length)
                return;

            TMP_Text label = questionButtons[selectedIndex] != null
                ? questionButtons[selectedIndex].GetComponentInChildren<TMP_Text>(true)
                : null;
            if (statusLabel != null)
                statusLabel.text = label != null
                    ? $"Đã chọn: {label.text}"
                    : $"Đã chọn mục {selectedIndex + 1}";
        }

        private void UpdateSelection(bool confirm)
        {
            selectedIndex = Wrap(selectedIndex, questionButtons.Length);
            for (int index = 0; index < questionButtons.Length; index++)
            {
                Button button = questionButtons[index];
                if (button != null && button.targetGraphic != null)
                    button.targetGraphic.color =
                        index == selectedIndex ? selectedColor : normalColor;
            }

            if (scrollRect != null && questionButtons.Length > 1)
                scrollRect.verticalNormalizedPosition =
                    1f - selectedIndex / (float)(questionButtons.Length - 1);

            if (statusLabel != null && !confirm)
                statusLabel.SetText("{0}/{1} | Cần trái: chọn | A: xác nhận",
                    selectedIndex + 1, questionButtons.Length);

            if (confirm)
                ConfirmSelection();
        }

        private static int Wrap(int value, int count)
        {
            if (count <= 0)
                return -1;
            value %= count;
            return value < 0 ? value + count : value;
        }
    }
}
