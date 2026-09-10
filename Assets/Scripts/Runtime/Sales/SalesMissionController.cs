using System.Collections;
using NCKH.VR.Lawyer;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NCKH.VR.Sales
{
    public enum SalesMissionStage { Waiting, Dialogue, Transition, Choosing, Completed }

    public sealed class SalesMissionController : MonoBehaviour
    {
        public SalesMissionData data;
        public Camera playerCamera;
        public Transform customer;
        public AudioSource voice;
        public Transform dialogueRoot;
        public Transform displayRoot;
        public GameObject choicesRoot;
        public GameObject resultRoot;
        public TMP_Text dialogueText;
        public TMP_Text speakerText;
        public TMP_Text nextText;
        public TMP_Text objectiveText;
        public TMP_Text resultText;
        public GameObject customerMarker;
        public LawyerBoardFocusLighting focus;
        public Behaviour[] locomotion = System.Array.Empty<Behaviour>();
        public float conversationDistance = 2.7f;
        public float transitionDuration = .4f;
        public SalesMissionStage Stage { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public int DialogueIndex { get; private set; }
        private bool[] previousLocomotion;
        private bool locomotionLocked;
        private bool previousOcclusion;
        private SalesActionTarget hovered;
        private Coroutine transition;
        private float nextActionTime;
        private bool initialized;

        private void OnEnable()
        {
            if (!initialized) return;
            focus.enabled = true;
            ResetMission();
        }

        private void Start()
        {
            if (data == null || !data.IsValid || playerCamera == null || customer == null)
            {
                Debug.LogError("Sales mission requires valid data, customer and player camera.", this);
                enabled = false;
                return;
            }
            previousOcclusion = playerCamera.useOcclusionCulling;
            previousLocomotion = new bool[locomotion.Length];
            initialized = true;
            ResetMission();
        }

        private void Update()
        {
            if (Stage == SalesMissionStage.Waiting)
            {
                Vector3 delta = playerCamera.transform.position - customer.position;
                delta.y = 0;
                if (delta.sqrMagnitude <= conversationDistance * conversationDistance) BeginDialogue();
            }
            UpdateDesktopPointer();
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame) HandleAction(-1);
            if (keyboard.rKey.wasPressedThisFrame) HandleAction(-2);
            if (Stage == SalesMissionStage.Choosing)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) HandleAction(0);
                else if (keyboard.digit2Key.wasPressedThisFrame) HandleAction(1);
                else if (keyboard.digit3Key.wasPressedThisFrame) HandleAction(2);
            }
        }

        private void LateUpdate()
        {
            if (playerCamera == null || dialogueRoot == null) return;
            // Only the presentation follows the head. The tracked camera is never moved.
            dialogueRoot.SetPositionAndRotation(playerCamera.transform.position +
                playerCamera.transform.rotation * new Vector3(0, -.34f, 1.35f), playerCamera.transform.rotation);
        }

        private void UpdateDesktopPointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || playerCamera == null) return;
            Ray ray = playerCamera.ScreenPointToRay(mouse.position.ReadValue());
            SalesActionTarget target = null;
            // Focus interactions deliberately ignore store geometry, like the focus render pass.
            if (Physics.Raycast(ray, out RaycastHit hit, 8f, 1 << 9, QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<SalesActionTarget>();
            if (hovered != target)
            {
                if (hovered != null) hovered.SetDesktopHover(false);
                hovered = target;
                if (hovered != null) hovered.SetDesktopHover(true);
            }
            if (target != null && mouse.leftButton.wasPressedThisFrame) target.Invoke();
        }

        public void BeginDialogue()
        {
            if (Stage != SalesMissionStage.Waiting) return;
            Stage = SalesMissionStage.Dialogue;
            DialogueIndex = 0;
            SetLocomotionLocked(true);
            objectiveText.text = "LẮNG NGHE KHÁCH HÀNG";
            if (customerMarker != null) customerMarker.SetActive(false);
            dialogueRoot.gameObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            speakerText.text = data.customerName + "  ·  KHÁCH HÀNG";
            dialogueText.text = data.dialogue[DialogueIndex].text;
            nextText.text = DialogueIndex + 1 < data.dialogue.Length ? "Tiếp tục  →" : "Xem ba đôi giày  →";
            ReplayAudio();
        }

        public void HandleAction(int action)
        {
            if (Time.unscaledTime < nextActionTime) return;
            if (action == -2 && Stage == SalesMissionStage.Dialogue) { ReplayAudio(); return; }
            if (action == -3 && Stage == SalesMissionStage.Completed) { ResetMission(); return; }
            if (action == -1 && Stage == SalesMissionStage.Dialogue)
            {
                nextActionTime = Time.unscaledTime + .2f;
                voice.Stop();
                if (++DialogueIndex < data.dialogue.Length) ShowLine();
                else transition = StartCoroutine(OpenChoices());
            }
            else if (action >= 0 && action < data.shoes.Length && Stage == SalesMissionStage.Choosing)
                SelectShoe(action);
        }

        public void ReplayAudio()
        {
            if (Stage != SalesMissionStage.Dialogue || voice == null) return;
            voice.Stop();
            voice.clip = data.dialogue[DialogueIndex].audioClip;
            if (voice.clip != null) voice.Play();
        }

        private IEnumerator OpenChoices()
        {
            Stage = SalesMissionStage.Transition;
            dialogueRoot.gameObject.SetActive(false);
            objectiveText.text = "SO SÁNH VÀ CHỌN GIÀY";
            objectiveText.gameObject.SetActive(false);
            focus.SetFocused(true);
            yield return new WaitForSecondsRealtime(transitionDuration);
            Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
            displayRoot.SetPositionAndRotation(playerCamera.transform.position + forward * 2.0f,
                Quaternion.LookRotation(forward, Vector3.up));
            displayRoot.gameObject.SetActive(true);
            choicesRoot.SetActive(true);
            resultRoot.SetActive(false);
            playerCamera.useOcclusionCulling = false;
            focus.SetOverlayActive(true);
            Stage = SalesMissionStage.Choosing;
            transition = null;
        }

        private void SelectShoe(int index)
        {
            SelectedIndex = index;
            Stage = SalesMissionStage.Completed;
            choicesRoot.SetActive(false);
            resultRoot.SetActive(true);
            SalesShoeData shoe = data.shoes[index];
            resultText.text = "ĐÃ CHỌN GIÀY " + shoe.label + "\n\n" + shoe.feedback
                + "\n\nBạn đã hoàn thành phần tìm hiểu nhu cầu và chọn giày.";
            objectiveText.text = "HOÀN THÀNH PHẦN 1";
            SetLocomotionLocked(false);
        }

        public void ResetMission()
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (voice != null) { voice.Stop(); voice.clip = null; }
            focus.SetOverlayActive(false);
            focus.SetFocused(false);
            displayRoot.gameObject.SetActive(false);
            dialogueRoot.gameObject.SetActive(false);
            playerCamera.useOcclusionCulling = previousOcclusion;
            SetLocomotionLocked(false);
            SelectedIndex = -1;
            nextActionTime = 0f;
            DialogueIndex = 0;
            Stage = SalesMissionStage.Waiting;
            objectiveText.gameObject.SetActive(true);
            if (customerMarker != null) customerMarker.SetActive(true);
            objectiveText.text = "ĐẾN GẶP KHÁCH Ở CỬA CHÍNH";
        }

        private void SetLocomotionLocked(bool value)
        {
            if (locomotionLocked == value || previousLocomotion == null) return;
            for (int i = 0; i < locomotion.Length; i++)
            {
                if (locomotion[i] == null) continue;
                if (value) { previousLocomotion[i] = locomotion[i].enabled; locomotion[i].enabled = false; }
                else locomotion[i].enabled = previousLocomotion[i];
            }
            locomotionLocked = value;
        }

        private void OnDisable()
        {
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (voice != null) voice.Stop();
            if (focus != null) focus.enabled = false;
            if (displayRoot != null) displayRoot.gameObject.SetActive(false);
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            if (playerCamera != null) playerCamera.useOcclusionCulling = previousOcclusion;
            SetLocomotionLocked(false);
            if (hovered != null) hovered.SetDesktopHover(false);
        }
    }
}
