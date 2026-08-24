using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NCKH.VR.GameControl
{
    [DisallowMultipleComponent]
    public sealed class BackendServerConfigurationView : MonoBehaviour
    {
        private const string RightPrimaryPath = "<XRController>{RightHand}/primaryButton";
        private const string RightSecondaryPath = "<XRController>{RightHand}/secondaryButton";
        private const string LeftPrimaryPath = "<XRController>{LeftHand}/primaryButton";
        private const string LeftSecondaryPath = "<XRController>{LeftHand}/secondaryButton";

        [Header("Window")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_InputField urlInput;
        [SerializeField] private TMP_Text validationText;
        [SerializeField] private Button saveButton;
        [SerializeField] private VrKeyboard keyboard;

        [Header("XR Interaction")]
        [SerializeField] private GameObject leftHand;
        [SerializeField] private GameObject rightHand;
        [SerializeField] private GameObject leftHandVisual;
        [SerializeField] private GameObject rightHandVisual;
        [SerializeField, Min(0.1f)] private float chordHoldSeconds = 0.5f;

        private InputAction rightPrimaryAction;
        private InputAction rightSecondaryAction;
        private InputAction leftPrimaryAction;
        private InputAction leftSecondaryAction;
        private float chordStartedAt = -1f;
        private bool chordArmed = true;
        private bool rightPrimaryHeld;
        private bool rightSecondaryHeld;
        private bool leftPrimaryHeld;
        private bool leftSecondaryHeld;
        private bool previousLeftHandActive;
        private bool previousRightHandActive;
        private bool previousLeftHandVisualActive;
        private bool previousRightHandVisualActive;
        private bool handStatesCaptured;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void OnEnable()
        {
            CreateAndEnableActions();
            if (saveButton != null)
                saveButton.onClick.AddListener(Save);
            if (keyboard != null)
                keyboard.EnterPressed += Save;

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnDisable()
        {
            if (saveButton != null)
                saveButton.onClick.RemoveListener(Save);
            if (keyboard != null)
                keyboard.EnterPressed -= Save;

            ClosePanel();
            DisableAndDisposeActions();
        }

        private void Update()
        {
            RefreshHeldStates();
            bool anyHeld = rightPrimaryHeld || rightSecondaryHeld || leftPrimaryHeld || leftSecondaryHeld;
            bool allHeld = rightPrimaryHeld && rightSecondaryHeld && leftPrimaryHeld && leftSecondaryHeld;

            if (!anyHeld)
            {
                chordArmed = true;
                chordStartedAt = -1f;
                return;
            }

            if (!allHeld)
            {
                chordStartedAt = -1f;
                return;
            }

            if (!chordArmed || IsOpen)
                return;

            if (chordStartedAt < 0f)
            {
                chordStartedAt = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - chordStartedAt >= chordHoldSeconds)
            {
                chordArmed = false;
                OpenPanel();
            }
        }

        private void RefreshHeldStates()
        {
            rightPrimaryHeld = rightPrimaryAction != null && rightPrimaryAction.IsPressed();
            rightSecondaryHeld = rightSecondaryAction != null && rightSecondaryAction.IsPressed();
            leftPrimaryHeld = leftPrimaryAction != null && leftPrimaryAction.IsPressed();
            leftSecondaryHeld = leftSecondaryAction != null && leftSecondaryAction.IsPressed();
        }

        public void OpenPanel()
        {
            if (panelRoot == null || urlInput == null)
                return;

            GameController controller = GameController.Instance;
            urlInput.SetTextWithoutNotify(controller == null ? string.Empty : controller.BackendBaseUrl);
            int end = urlInput.text.Length;
            urlInput.selectionStringAnchorPosition = end;
            urlInput.selectionStringFocusPosition = end;
            SetValidation(string.Empty);

            CaptureAndEnableHands();
            panelRoot.SetActive(true);
            if (keyboard != null)
                keyboard.SetTarget(urlInput);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(urlInput.gameObject);
        }

        public void Save()
        {
            GameController controller = GameController.Instance;
            if (controller == null)
            {
                SetValidation("Không tìm thấy bộ điều khiển trò chơi.");
                return;
            }

            if (!controller.TrySetBackendBaseUrl(urlInput == null ? string.Empty : urlInput.text, out string error))
            {
                SetValidation(error);
                return;
            }

            ClosePanel();
        }

        public void ClosePanel()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            RestoreHandStates();
        }

        private void CaptureAndEnableHands()
        {
            if (!handStatesCaptured)
            {
                previousLeftHandActive = leftHand != null && leftHand.activeSelf;
                previousRightHandActive = rightHand != null && rightHand.activeSelf;
                previousLeftHandVisualActive = leftHandVisual != null && leftHandVisual.activeSelf;
                previousRightHandVisualActive = rightHandVisual != null && rightHandVisual.activeSelf;
                handStatesCaptured = true;
            }

            if (leftHand != null)
                leftHand.SetActive(true);
            if (rightHand != null)
                rightHand.SetActive(true);
            if (leftHandVisual != null)
                leftHandVisual.SetActive(false);
            if (rightHandVisual != null)
                rightHandVisual.SetActive(false);
        }

        private void RestoreHandStates()
        {
            if (!handStatesCaptured)
                return;

            if (leftHand != null)
                leftHand.SetActive(previousLeftHandActive);
            if (rightHand != null)
                rightHand.SetActive(previousRightHandActive);
            if (leftHandVisual != null)
                leftHandVisual.SetActive(previousLeftHandVisualActive);
            if (rightHandVisual != null)
                rightHandVisual.SetActive(previousRightHandVisualActive);
            handStatesCaptured = false;
        }

        private void SetValidation(string value)
        {
            if (validationText != null)
                validationText.text = value ?? string.Empty;
        }

        private void CreateAndEnableActions()
        {
            rightPrimaryAction = CreateButtonAction("A", RightPrimaryPath, OnRightPrimaryPerformed, OnRightPrimaryCanceled);
            rightSecondaryAction = CreateButtonAction("B", RightSecondaryPath, OnRightSecondaryPerformed, OnRightSecondaryCanceled);
            leftPrimaryAction = CreateButtonAction("X", LeftPrimaryPath, OnLeftPrimaryPerformed, OnLeftPrimaryCanceled);
            leftSecondaryAction = CreateButtonAction("Y", LeftSecondaryPath, OnLeftSecondaryPerformed, OnLeftSecondaryCanceled);
        }

        private static InputAction CreateButtonAction(
            string name,
            string binding,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            InputAction action = new InputAction(name, InputActionType.Button, binding);
            action.performed += performed;
            action.canceled += canceled;
            action.Enable();
            return action;
        }

        private void DisableAndDisposeActions()
        {
            DisposeAction(ref rightPrimaryAction, OnRightPrimaryPerformed, OnRightPrimaryCanceled);
            DisposeAction(ref rightSecondaryAction, OnRightSecondaryPerformed, OnRightSecondaryCanceled);
            DisposeAction(ref leftPrimaryAction, OnLeftPrimaryPerformed, OnLeftPrimaryCanceled);
            DisposeAction(ref leftSecondaryAction, OnLeftSecondaryPerformed, OnLeftSecondaryCanceled);
            rightPrimaryHeld = false;
            rightSecondaryHeld = false;
            leftPrimaryHeld = false;
            leftSecondaryHeld = false;
            chordStartedAt = -1f;
            chordArmed = true;
        }

        private static void DisposeAction(
            ref InputAction action,
            System.Action<InputAction.CallbackContext> performed,
            System.Action<InputAction.CallbackContext> canceled)
        {
            if (action == null)
                return;
            action.performed -= performed;
            action.canceled -= canceled;
            action.Disable();
            action.Dispose();
            action = null;
        }

        private void OnRightPrimaryPerformed(InputAction.CallbackContext _) => rightPrimaryHeld = true;
        private void OnRightPrimaryCanceled(InputAction.CallbackContext _) => rightPrimaryHeld = false;
        private void OnRightSecondaryPerformed(InputAction.CallbackContext _) => rightSecondaryHeld = true;
        private void OnRightSecondaryCanceled(InputAction.CallbackContext _) => rightSecondaryHeld = false;
        private void OnLeftPrimaryPerformed(InputAction.CallbackContext _) => leftPrimaryHeld = true;
        private void OnLeftPrimaryCanceled(InputAction.CallbackContext _) => leftPrimaryHeld = false;
        private void OnLeftSecondaryPerformed(InputAction.CallbackContext _) => leftSecondaryHeld = true;
        private void OnLeftSecondaryCanceled(InputAction.CallbackContext _) => leftSecondaryHeld = false;
    }
}
