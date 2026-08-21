using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
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
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int SrcBlendAlphaId = Shader.PropertyToID("_SrcBlendAlpha");
        private static readonly int DstBlendAlphaId = Shader.PropertyToID("_DstBlendAlpha");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
        private static readonly Dictionary<Material, Material> TransparentMaterialCache =
            new Dictionary<Material, Material>(96);

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

        [Header("Humanoid Appearance")]
        [SerializeField] private PatientAppearanceController appearanceController;

        [Header("Severity 4 Alert")]
        [SerializeField] private bool enableEmergencyAlert = true;
        [SerializeField] private AudioClip dyingSfx;
        [SerializeField, Min(0.01f)] private float emergencyFlashInterval = 0.5f;
        [SerializeField] private Color emergencyFlashColor = Color.red;
        [SerializeField, Min(0f)] private float emergencyAudioMinDistance = 1f;
        [SerializeField, Min(0f)] private float emergencyAudioMaxDistance = 15f;

        private MaterialPropertyBlock propertyBlock;
        private RendererVisualState[] rendererVisualStates = Array.Empty<RendererVisualState>();
        private AudioSource emergencyAudioSource;
        private Transform playerHead;
        private float fadeDuration;
        private float visualAlpha;
        private float proximityAlpha;
        private float treatmentDuration;
        private float emergencyFlashCountdown;
        private bool emergencyFlashRed;
        private bool emergencyAlertRunning;
        private Renderer[] activeRenderers = Array.Empty<Renderer>();
        private bool appearanceRenderersActive;

        public event Action<PatientActor, PatientResolution> Resolved;
        public event Action<PatientActor> BecameInactive;

        public PatientActorState State { get; private set; } = PatientActorState.Inactive;
        public string SicknessName { get; private set; } = string.Empty;
        public int Severity { get; private set; }
        public float RemainingTreatmentTime { get; private set; }
        public bool EmergencyAlertEnabled => enableEmergencyAlert;

        private void Awake()
        {
            activeRenderers = fadeRenderers;
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
                        SetAppearanceFadeMaterials(false);
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

            bool emergency = Severity == ClinicRoundRules.MaximumSeverity;
            if (appearanceController != null)
            {
                if (!appearanceController.RandomizeAppearance(emergency))
                {
                    DeactivateImmediately();
                    return;
                }

                IReadOnlyList<Renderer> appearanceRenderers = appearanceController.ActiveRenderers;
                activeRenderers = new Renderer[appearanceRenderers.Count];
                for (int i = 0; i < appearanceRenderers.Count; i++)
                    activeRenderers[i] = appearanceRenderers[i];

                appearanceRenderersActive = true;
                CacheRendererColors();
                SetAppearanceFadeMaterials(true);
            }
            else
            {
                activeRenderers = fadeRenderers;
                appearanceRenderersActive = false;
            }

            if (sicknessText != null)
            {
                sicknessText.text = showSeverity
                    ? $"Bệnh: {SicknessName} (Mức độ {Severity})"
                    : $"Bệnh: {SicknessName}";
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
            SetAppearanceFadeMaterials(true);
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
            ReleaseAppearance();
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
            SetAppearanceFadeMaterials(true);
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
            ReleaseAppearance();
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
            if (!ShouldRunEmergencyAlert())
                return;

            emergencyAlertRunning = true;
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
            emergencyAlertRunning = false;
            emergencyFlashRed = false;
            emergencyFlashCountdown = 0f;

            if (emergencyAudioSource != null)
                emergencyAudioSource.Stop();
        }

        private void UpdateEmergencyFlash(float deltaTime)
        {
            if (!ShouldRunEmergencyAlert())
            {
                if (emergencyAlertRunning)
                    StopEmergencyAlert();
                else
                    emergencyFlashRed = false;
                return;
            }

            if (!emergencyAlertRunning)
            {
                StartEmergencyAlert();
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

        private bool ShouldRunEmergencyAlert()
        {
            return enableEmergencyAlert
                && Severity == ClinicRoundRules.MaximumSeverity
                && (State == PatientActorState.FadingIn || State == PatientActorState.Active);
        }

        private void ApplyVisuals()
        {
            EnsureRuntimeCache();

            for (int i = 0; i < activeRenderers.Length; i++)
            {
                Renderer targetRenderer = activeRenderers[i];
                if (targetRenderer == null)
                    continue;

                if (appearanceRenderersActive)
                    targetRenderer.forceRenderingOff = visualAlpha <= 0f;
                else
                    targetRenderer.enabled = visualAlpha > 0f;
                if (i >= rendererVisualStates.Length)
                    continue;

                RendererVisualState visualState = rendererVisualStates[i];
                for (int materialIndex = 0; materialIndex < visualState.MaterialColors.Length; materialIndex++)
                {
                    Color color = visualState.MaterialColors[materialIndex];
                    if (emergencyFlashRed)
                    {
                        float originalAlpha = color.a;
                        color = emergencyFlashColor;
                        color.a = originalAlpha;
                    }

                    color.a *= visualAlpha;
                    propertyBlock.Clear();
                    targetRenderer.GetPropertyBlock(propertyBlock, materialIndex);
                    if (visualState.HasBaseColor[materialIndex])
                        propertyBlock.SetColor(BaseColorId, color);
                    if (visualState.HasColor[materialIndex])
                        propertyBlock.SetColor(ColorId, color);
                    targetRenderer.SetPropertyBlock(propertyBlock, materialIndex);
                }
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
            rendererVisualStates = new RendererVisualState[activeRenderers.Length];
            for (int i = 0; i < activeRenderers.Length; i++)
            {
                Renderer targetRenderer = activeRenderers[i];
                Material[] materials = targetRenderer != null
                    ? targetRenderer.sharedMaterials
                    : Array.Empty<Material>();
                RendererVisualState visualState = new RendererVisualState(materials.Length);
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    visualState.OriginalMaterials[materialIndex] = material;
                    visualState.FadeMaterials[materialIndex] = GetTransparentFadeMaterial(material);
                    bool hasBaseColor = material != null && material.HasProperty(BaseColorId);
                    bool hasColor = material != null && material.HasProperty(ColorId);
                    visualState.HasBaseColor[materialIndex] = hasBaseColor;
                    visualState.HasColor[materialIndex] = hasColor;
                    visualState.MaterialColors[materialIndex] = hasBaseColor
                        ? material.GetColor(BaseColorId)
                        : hasColor
                            ? material.GetColor(ColorId)
                            : Color.white;
                }

                rendererVisualStates[i] = visualState;
            }
        }

        private void SetAppearanceFadeMaterials(bool useFadeMaterials)
        {
            if (!appearanceRenderersActive)
                return;

            for (int i = 0; i < rendererVisualStates.Length; i++)
            {
                RendererVisualState visualState = rendererVisualStates[i];
                Renderer targetRenderer = i < activeRenderers.Length ? activeRenderers[i] : null;
                if (targetRenderer == null)
                    continue;

                targetRenderer.sharedMaterials = useFadeMaterials
                    ? visualState.FadeMaterials
                    : visualState.OriginalMaterials;
            }
        }

        private void ReleaseAppearance()
        {
            if (appearanceController != null)
                appearanceController.ClearAppearance();

            activeRenderers = Array.Empty<Renderer>();
            rendererVisualStates = Array.Empty<RendererVisualState>();
            appearanceRenderersActive = false;
        }

        private static Material GetTransparentFadeMaterial(Material source)
        {
            if (source == null || !source.HasProperty(SurfaceId))
                return source;
            if (TransparentMaterialCache.TryGetValue(source, out Material cached) && cached != null)
                return cached;

            Material fadeMaterial = new Material(source)
            {
                name = source.name + " (Patient Fade)",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Transparent
            };
            fadeMaterial.SetOverrideTag("RenderType", "Transparent");
            fadeMaterial.SetFloat(SurfaceId, 1f);
            if (fadeMaterial.HasProperty(BlendId))
                fadeMaterial.SetFloat(BlendId, 0f);
            if (fadeMaterial.HasProperty(SrcBlendId))
                fadeMaterial.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            if (fadeMaterial.HasProperty(DstBlendId))
                fadeMaterial.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            if (fadeMaterial.HasProperty(SrcBlendAlphaId))
                fadeMaterial.SetFloat(SrcBlendAlphaId, (float)BlendMode.One);
            if (fadeMaterial.HasProperty(DstBlendAlphaId))
                fadeMaterial.SetFloat(DstBlendAlphaId, (float)BlendMode.OneMinusSrcAlpha);
            if (fadeMaterial.HasProperty(ZWriteId))
                fadeMaterial.SetFloat(ZWriteId, 0f);
            if (fadeMaterial.HasProperty(AlphaClipId))
                fadeMaterial.SetFloat(AlphaClipId, 0f);
            fadeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fadeMaterial.DisableKeyword("_ALPHATEST_ON");
            TransparentMaterialCache[source] = fadeMaterial;
            return fadeMaterial;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransparentMaterialCache()
        {
            TransparentMaterialCache.Clear();
        }

        private void EnsureRuntimeCache()
        {
            if (propertyBlock == null)
                propertyBlock = new MaterialPropertyBlock();

            if (activeRenderers == null)
                activeRenderers = fadeRenderers;

            if (rendererVisualStates == null || rendererVisualStates.Length != activeRenderers.Length)
                CacheRendererColors();
        }

        private sealed class RendererVisualState
        {
            public RendererVisualState(int materialCount)
            {
                MaterialColors = new Color[materialCount];
                HasBaseColor = new bool[materialCount];
                HasColor = new bool[materialCount];
                OriginalMaterials = new Material[materialCount];
                FadeMaterials = new Material[materialCount];
            }

            public Color[] MaterialColors { get; }
            public bool[] HasBaseColor { get; }
            public bool[] HasColor { get; }
            public Material[] OriginalMaterials { get; }
            public Material[] FadeMaterials { get; }
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
