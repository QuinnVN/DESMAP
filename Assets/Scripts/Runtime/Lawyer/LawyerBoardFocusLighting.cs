using System.Collections;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerBoardFocusLighting : MonoBehaviour
    {
        [SerializeField] private Light[] officeLights = System.Array.Empty<Light>();
        [SerializeField] private Light boardSpotlight;
        [SerializeField, Range(0f, 1f)] private float dimMultiplier = 0.2f;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.3f;

        private float[] originalIntensities = System.Array.Empty<float>();
        private float boardIntensity;
        private Coroutine fadeRoutine;

        public bool IsFocused { get; private set; }
        public Light BoardSpotlight => boardSpotlight;

#if UNITY_EDITOR
        public void ConfigureInEditor(Light[] lights, Light spotlight)
        {
            officeLights = lights ?? System.Array.Empty<Light>();
            boardSpotlight = spotlight;
        }
#endif

        private void Awake()
        {
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

        private IEnumerator Fade(bool focused)
        {
            float[] starts = new float[officeLights.Length];
            for (int index = 0; index < officeLights.Length; index++)
                starts[index] = officeLights[index] != null ? officeLights[index].intensity : 0f;
            float spotlightStart = boardSpotlight != null ? boardSpotlight.intensity : 0f;
            if (focused && boardSpotlight != null)
                boardSpotlight.enabled = true;

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
                yield return null;
            }

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
            IsFocused = false;
            fadeRoutine = null;
        }
    }
}
