using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NCKH.VR.Clinic
{
    public enum PatientActorState
    {
        Inactive,
        FadingIn,
        Active,
        FadingOut
    }

    [DisallowMultipleComponent]
    public sealed class PatientActor : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Patient UI")]
        [SerializeField] private TMP_Text sicknessText;
        [SerializeField] private Button cureButton;
        [SerializeField] private CanvasGroup chatCanvasGroup;
        [SerializeField] private CanvasGroup cureCanvasGroup;

        [Header("Cure Button Orbit")]
        [SerializeField] private Transform cureOrbitPivot;
        [SerializeField] private Transform cureCanvas;
        [SerializeField, Min(0f)] private float orbitRadius = 1.1f;
        [SerializeField] private float orbitSideOffset = -1f;
        [SerializeField] private float orbitHeight;
        [SerializeField] private float canvasYawOffset = 180f;
        [SerializeField, Min(0f)] private float proximityRadius = 2f;
        [SerializeField, Min(0f)] private float proximityFadeDuration = 0.25f;

        [Header("Fade Targets")]
        [SerializeField] private Renderer[] fadeRenderers = Array.Empty<Renderer>();
        [SerializeField] private Canvas[] patientCanvases = Array.Empty<Canvas>();
        [SerializeField] private Collider[] patientColliders = Array.Empty<Collider>();

        [Header("Severity 4 Alert")]
        [SerializeField] private AudioClip dyingSfx;
        [SerializeField, Min(0.01f)] private float emergencyFlashInterval = 0.5f;
        [SerializeField] private Color emergencyFlashColor = Color.red;
        [SerializeField, Min(0f)] private float emergencyAudioMinDistance = 1f;
        [SerializeField, Min(0f)] private float emergencyAudioMaxDistance = 15f;

        private MaterialPropertyBlock propertyBlock;
        private Color[] rendererColors = Array.Empty<Color>();
        private AudioSource emergencyAudioSource;
        private Transform playerHead;
        private float fadeDuration;
        private float visualAlpha;
        private float proximityAlpha;
        private float treatmentDuration;
        private float emergencyFlashCountdown;
        private bool emergencyFlashRed;

        public event Action<PatientActor, PatientResolution> Resolved;
        public event Action<PatientActor> BecameInactive;

        public PatientActorState State { get; private set; } = PatientActorState.Inactive;
        public string SicknessName { get; private set; } = string.Empty;
        public int Severity { get; private set; }
        public float RemainingTreatmentTime { get; private set; }

        private void Awake()
        {
            EnsureRuntimeCache();
            SetColliderState(false);
            ApplyVisuals();
        }

        private void OnEnable()
        {
            if (cureButton != null)
                cureButton.onClick.AddListener(OnCureClicked);
        }

        private void OnDisable()
        {
            if (cureButton != null)
                cureButton.onClick.RemoveListener(OnCureClicked);

            StopEmergencyAlert();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            switch (State)
            {
                case PatientActorState.FadingIn:
                    visualAlpha = MoveAlpha(visualAlpha, 1f, deltaTime, fadeDuration);
                    if (visualAlpha >= 1f)
                    {
                        State = PatientActorState.Active;
                        RemainingTreatmentTime = treatmentDuration;
                        SetColliderState(true);
                    }
                    break;

                case PatientActorState.Active:
                    RemainingTreatmentTime = Mathf.Max(0f, RemainingTreatmentTime - deltaTime);
                    if (RemainingTreatmentTime <= 0f)
                        Resolve(PatientResolution.TimedOut);
                    break;

                case PatientActorState.FadingOut:
                    visualAlpha = MoveAlpha(visualAlpha, 0f, deltaTime, fadeDuration);
                    if (visualAlpha <= 0f)
                    {
                        CompleteFadeOut();
                        return;
                    }
                    break;
            }

            UpdateEmergencyFlash(deltaTime);
            UpdateProximity(deltaTime);
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            UpdatePatientUiFacing();
        }

        public void Activate(
            string sicknessName,
            int severity,
            float treatmentSeconds,
            Transform trackedHead,
            float transitionDuration,
            bool showSeverity)
        {
            SicknessName = sicknessName ?? string.Empty;
            Severity = Mathf.Clamp(severity, ClinicRoundRules.MinimumSeverity, ClinicRoundRules.MaximumSeverity);
            treatmentDuration = Mathf.Max(0f, treatmentSeconds);
            RemainingTreatmentTime = treatmentDuration;
            playerHead = trackedHead;
            fadeDuration = Mathf.Max(0f, transitionDuration);
            visualAlpha = 0f;
            proximityAlpha = 0f;
            State = PatientActorState.FadingIn;

            if (sicknessText != null)
            {
                sicknessText.text = showSeverity
                    ? $"Sickness: {SicknessName} (Severity {Severity})"
                    : $"Sickness: {SicknessName}";
            }

            SetColliderState(false);
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            if (!enabled)
                enabled = true;

            StartEmergencyAlert();
            UpdatePatientUiFacing();
            ApplyVisuals();
        }

        public void StopForRoundEnd()
        {
            if (State == PatientActorState.Inactive || State == PatientActorState.FadingOut)
                return;

            State = PatientActorState.FadingOut;
            StopEmergencyAlert();
            SetColliderState(false);
            SetButtonInteractable(false);
        }

        public void DeactivateImmediately()
        {
            State = PatientActorState.Inactive;
            visualAlpha = 0f;
            proximityAlpha = 0f;
            RemainingTreatmentTime = 0f;
            StopEmergencyAlert();
            SetColliderState(false);
            SetButtonInteractable(false);
            ApplyVisuals();
            enabled = false;
        }

        private void OnCureClicked()
        {
            if (State == PatientActorState.Active && cureButton != null && cureButton.interactable)
                Resolve(PatientResolution.Cured);
        }

        private void Resolve(PatientResolution resolution)
        {
            if (State != PatientActorState.Active)
                return;

            State = PatientActorState.FadingOut;
            StopEmergencyAlert();
            SetColliderState(false);
            SetButtonInteractable(false);
            Resolved?.Invoke(this, resolution);
        }

        private void CompleteFadeOut()
        {
            State = PatientActorState.Inactive;
            visualAlpha = 0f;
            proximityAlpha = 0f;
            RemainingTreatmentTime = 0f;
            StopEmergencyAlert();
            SetColliderState(false);
            SetButtonInteractable(false);
            ApplyVisuals();
            enabled = false;
            BecameInactive?.Invoke(this);
        }

        private void UpdateProximity(float deltaTime)
        {
            bool shouldShow = State == PatientActorState.Active && playerHead != null;
            if (shouldShow)
            {
                Vector3 offset = playerHead.position - transform.position;
                offset.y = 0f;
                shouldShow = offset.sqrMagnitude <= proximityRadius * proximityRadius;
            }

            float target = shouldShow ? 1f : 0f;
            proximityAlpha = MoveAlpha(proximityAlpha, target, deltaTime, proximityFadeDuration);
        }

        private void UpdatePatientUiFacing()
        {
            if (playerHead == null)
                return;

            Vector3 flatDirection = playerHead.position - transform.position;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.0001f)
                return;

            Quaternion worldRotation = Quaternion.LookRotation(flatDirection.normalized, Vector3.up);
            Quaternion canvasRotation = worldRotation * Quaternion.Euler(0f, canvasYawOffset, 0f);

            if (cureOrbitPivot != null && cureCanvas != null)
            {
                cureOrbitPivot.localPosition = Vector3.up * orbitHeight;
                cureOrbitPivot.rotation = worldRotation;
                cureCanvas.localPosition = new Vector3(orbitSideOffset, 0f, orbitRadius);
                cureCanvas.localRotation = Quaternion.Euler(0f, canvasYawOffset, 0f);
            }

            if (chatCanvasGroup != null)
                chatCanvasGroup.transform.rotation = canvasRotation;
        }

        private void StartEmergencyAlert()
        {
            StopEmergencyAlert();
            if (Severity != ClinicRoundRules.MaximumSeverity)
                return;

            emergencyFlashRed = true;
            emergencyFlashCountdown = Mathf.Max(0.01f, emergencyFlashInterval);

            if (dyingSfx == null)
                return;

            EnsureEmergencyAudioSource();
            emergencyAudioSource.clip = dyingSfx;
            emergencyAudioSource.Play();
        }

        private void StopEmergencyAlert()
        {
            emergencyFlashRed = false;
            emergencyFlashCountdown = 0f;

            if (emergencyAudioSource != null)
                emergencyAudioSource.Stop();
        }

        private void UpdateEmergencyFlash(float deltaTime)
        {
            if (Severity != ClinicRoundRules.MaximumSeverity
                || (State != PatientActorState.FadingIn && State != PatientActorState.Active))
            {
                emergencyFlashRed = false;
                return;
            }

            emergencyFlashCountdown -= deltaTime;
            float interval = Mathf.Max(0.01f, emergencyFlashInterval);
            while (emergencyFlashCountdown <= 0f)
            {
                emergencyFlashRed = !emergencyFlashRed;
                emergencyFlashCountdown += interval;
            }
        }

        private void ApplyVisuals()
        {
            EnsureRuntimeCache();

            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                Renderer targetRenderer = fadeRenderers[i];
                if (targetRenderer == null)
                    continue;

                targetRenderer.enabled = visualAlpha > 0f;
                Color color = i < rendererColors.Length ? rendererColors[i] : Color.white;
                if (emergencyFlashRed)
                {
                    float originalAlpha = color.a;
                    color = emergencyFlashColor;
                    color.a = originalAlpha;
                }
                color.a *= visualAlpha;
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            bool canvasesVisible = visualAlpha > 0f;
            for (int i = 0; i < patientCanvases.Length; i++)
            {
                if (patientCanvases[i] != null)
                    patientCanvases[i].enabled = canvasesVisible;
            }

            if (chatCanvasGroup != null)
                chatCanvasGroup.alpha = visualAlpha;

            if (cureCanvasGroup != null)
            {
                cureCanvasGroup.alpha = visualAlpha * proximityAlpha;
                cureCanvasGroup.blocksRaycasts = CanInteract();
                cureCanvasGroup.interactable = CanInteract();
            }

            SetButtonInteractable(CanInteract());
        }

        private bool CanInteract()
        {
            return State == PatientActorState.Active
                && playerHead != null
                && visualAlpha >= 0.999f
                && proximityAlpha >= 0.999f;
        }

        private void CacheRendererColors()
        {
            rendererColors = new Color[fadeRenderers.Length];
            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                Renderer targetRenderer = fadeRenderers[i];
                Material material = targetRenderer != null ? targetRenderer.sharedMaterial : null;
                if (material == null)
                {
                    rendererColors[i] = Color.white;
                }
                else if (material.HasProperty(BaseColorId))
                {
                    rendererColors[i] = material.GetColor(BaseColorId);
                }
                else if (material.HasProperty(ColorId))
                {
                    rendererColors[i] = material.GetColor(ColorId);
                }
                else
                {
                    rendererColors[i] = Color.white;
                }
            }
        }

        private void EnsureRuntimeCache()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            if (rendererColors == null || rendererColors.Length != fadeRenderers.Length)
                CacheRendererColors();
        }

        private void EnsureEmergencyAudioSource()
        {
            if (emergencyAudioSource == null)
                emergencyAudioSource = GetComponent<AudioSource>();
            if (emergencyAudioSource == null)
                emergencyAudioSource = gameObject.AddComponent<AudioSource>();

            emergencyAudioSource.playOnAwake = false;
            emergencyAudioSource.loop = true;
            emergencyAudioSource.spatialBlend = 1f;
            emergencyAudioSource.dopplerLevel = 0f;
            emergencyAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            emergencyAudioSource.minDistance = Mathf.Max(0f, emergencyAudioMinDistance);
            emergencyAudioSource.maxDistance = Mathf.Max(
                emergencyAudioSource.minDistance + 0.01f,
                emergencyAudioMaxDistance);
        }

        private void SetColliderState(bool enabled)
        {
            for (int i = 0; i < patientColliders.Length; i++)
            {
                if (patientColliders[i] != null)
                    patientColliders[i].enabled = enabled;
            }
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (cureButton != null)
                cureButton.interactable = interactable;
        }

        private static float MoveAlpha(float current, float target, float deltaTime, float duration)
        {
            float step = duration > 0f ? deltaTime / duration : 1f;
            return Mathf.MoveTowards(current, target, step);
        }
    }
}
