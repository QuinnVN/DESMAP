using System.Collections;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerBoardFocusLighting : MonoBehaviour
    {
        public const string GlobalDimProperty = "_LawyerFocusDimAmount";
        private static readonly int GlobalDimId = Shader.PropertyToID(GlobalDimProperty);

        [SerializeField] private Light[] officeLights = System.Array.Empty<Light>();
        [SerializeField] private Light boardSpotlight;
        [SerializeField] private Renderer focusMaskRenderer;
        [SerializeField, Range(0f, 1f)] private float dimMultiplier = 0.45f;
        [SerializeField, Range(0f, 1f)] private float screenDimAmount = 0.55f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.3f;

        private float[] originalIntensities = System.Array.Empty<float>();
        private float boardIntensity;
        private Coroutine fadeRoutine;

        public bool IsFocused { get; private set; }
        public Light BoardSpotlight => boardSpotlight;
        public Renderer FocusMaskRenderer => focusMaskRenderer;
        public float DimMultiplier => dimMultiplier;
        public float ScreenDimAmount => screenDimAmount;
        public static float CurrentScreenDimAmount => Shader.GetGlobalFloat(GlobalDimId);
        public static bool CurrentOverlayActive { get; private set; }

#if UNITY_EDITOR
        public void ConfigureInEditor(
            Light[] lights, Light spotlight, Renderer maskRenderer)
        {
            officeLights = lights ?? System.Array.Empty<Light>();
            boardSpotlight = spotlight;
            focusMaskRenderer = maskRenderer;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetGlobalState()
        {
            Shader.SetGlobalFloat(GlobalDimId, 0f);
            CurrentOverlayActive = false;
        }

        private void Awake()
        {
            Shader.SetGlobalFloat(GlobalDimId, 0f);
            CurrentOverlayActive = false;
            if (focusMaskRenderer != null)
                focusMaskRenderer.enabled = false;
            originalIntensities = new float[officeLights.Length];
            for (int index = 0; index < officeLights.Length; index++)
                if (officeLights[index] != null)
                    originalIntensities[index] = officeLights[index].intensity;
            if (boardSpotlight != null)
            {
                boardIntensity = boardSpotlight.intensity;
                boardSpotlight.intensity = 0f;
                boardSpotlight.enabled = false;
            }
        }

        private void OnDisable()
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            RestoreImmediate();
        }

        public void SetFocused(bool focused)
        {
            if (IsFocused == focused && fadeRoutine == null)
                return;
            IsFocused = focused;
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(focused));
        }

        public void SetOverlayActive(bool active)
        {
            CurrentOverlayActive = active;
            if (focusMaskRenderer != null)
                focusMaskRenderer.enabled = active;
        }

        private IEnumerator Fade(bool focused)
        {
            float[] starts = new float[officeLights.Length];
            for (int index = 0; index < officeLights.Length; index++)
                starts[index] = officeLights[index] != null ? officeLights[index].intensity : 0f;
            float spotlightStart = boardSpotlight != null ? boardSpotlight.intensity : 0f;
            float dimStart = CurrentScreenDimAmount;
            if (focused)
            {
                if (boardSpotlight != null)
                    boardSpotlight.enabled = true;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fadeDuration));
                for (int index = 0; index < officeLights.Length; index++)
                {
                    Light light = officeLights[index];
                    if (light == null)
                        continue;
                    float target = focused ? originalIntensities[index] * dimMultiplier : originalIntensities[index];
                    light.intensity = Mathf.Lerp(starts[index], target, t);
                }
                if (boardSpotlight != null)
                    boardSpotlight.intensity = Mathf.Lerp(spotlightStart, focused ? boardIntensity : 0f, t);
                Shader.SetGlobalFloat(
                    GlobalDimId, Mathf.Lerp(dimStart, focused ? screenDimAmount : 0f, t));
                yield return null;
            }

            Shader.SetGlobalFloat(GlobalDimId, focused ? screenDimAmount : 0f);
            if (!focused && boardSpotlight != null)
                boardSpotlight.enabled = false;
            fadeRoutine = null;
        }

        private void RestoreImmediate()
        {
            for (int index = 0; index < officeLights.Length; index++)
                if (officeLights[index] != null && index < originalIntensities.Length)
                    officeLights[index].intensity = originalIntensities[index];
            if (boardSpotlight != null)
            {
                boardSpotlight.intensity = 0f;
                boardSpotlight.enabled = false;
            }
            Shader.SetGlobalFloat(GlobalDimId, 0f);
            SetOverlayActive(false);
            IsFocused = false;
            fadeRoutine = null;
        }
    }
}
