using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(LawyerActivationOnlySelectFilter))]
    public sealed class LawyerPhysicalButton : MonoBehaviour
    {
        [SerializeField] private Transform pressVisual;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color normalColor = new Color(0.10f, 0.22f, 0.20f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.18f, 0.55f, 0.48f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField, Min(0.001f)] private float pressDepth = 0.012f;
        [SerializeField, Min(0.02f)] private float pressDuration = 0.08f;
        [SerializeField, Min(0.05f)] private float inputCooldown = 0.25f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock propertyBlock;
        private XRSimpleInteractable interactable;
        private Vector3 restPosition;
        private Coroutine pressRoutine;
        private float nextAllowedPressTime;
        private bool hovered;
        private bool interactableState = true;

        public event Action Pressed;
        public bool IsPressed { get; private set; }
        public bool Interactable => interactableState;

#if UNITY_EDITOR
        public void ConfigureInEditor(Transform visual, Renderer renderer, TMP_Text configuredLabel)
        {
            pressVisual = visual;
            targetRenderer = renderer;
            label = configuredLabel;
        }
#endif

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (pressVisual == null)
                pressVisual = transform;
            propertyBlock = new MaterialPropertyBlock();
            restPosition = pressVisual.localPosition;
            ApplyColor();
        }

        private void OnEnable()
        {
            if (interactable == null)
                interactable = GetComponent<XRSimpleInteractable>();
            interactable.activated.AddListener(OnActivated);
            interactable.hoverEntered.AddListener(OnHoverEntered);
            interactable.hoverExited.AddListener(OnHoverExited);
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.activated.RemoveListener(OnActivated);
                interactable.hoverEntered.RemoveListener(OnHoverEntered);
                interactable.hoverExited.RemoveListener(OnHoverExited);
            }
            hovered = false;
            IsPressed = false;
            if (pressVisual != null)
                pressVisual.localPosition = restPosition;
        }

        public void SetInteractable(bool value)
        {
            interactableState = value;
            if (interactable != null)
                interactable.enabled = value;
            hovered = false;
            ApplyColor();
        }

        public void SetLabel(string value)
        {
            if (label != null)
                label.text = value ?? string.Empty;
        }

        private void OnActivated(ActivateEventArgs _)
        {
            if (!interactableState || Time.unscaledTime < nextAllowedPressTime)
                return;
            nextAllowedPressTime = Time.unscaledTime + inputCooldown;
            if (pressRoutine != null)
                StopCoroutine(pressRoutine);
            pressRoutine = StartCoroutine(AnimatePress());
            Pressed?.Invoke();
        }

        private void OnHoverEntered(HoverEnterEventArgs _)
        {
            hovered = true;
            ApplyColor();
        }

        private void OnHoverExited(HoverExitEventArgs _)
        {
            hovered = false;
            ApplyColor();
        }

        private IEnumerator AnimatePress()
        {
            IsPressed = true;
            Vector3 pressedPosition = restPosition + Vector3.back * pressDepth;
            float halfDuration = pressDuration * 0.5f;
            yield return MoveVisual(restPosition, pressedPosition, halfDuration);
            yield return MoveVisual(pressedPosition, restPosition, halfDuration);
            IsPressed = false;
            pressRoutine = null;
        }

        private IEnumerator MoveVisual(Vector3 from, Vector3 to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                pressVisual.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            pressVisual.localPosition = to;
        }

        private void ApplyColor()
        {
            if (targetRenderer == null)
                return;
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();
            Color color = !interactableState ? disabledColor : hovered ? hoverColor : normalColor;
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
