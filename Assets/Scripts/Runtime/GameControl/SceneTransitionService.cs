using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NCKH.VR.GameControl
{
    /// <summary>
    /// Keeps a headset-safe loading overlay alive while a heavy scene is replaced.
    /// Single-scene loading is intentional: the lawyer scenes are too large to keep
    /// resident together on the target headset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneTransitionService : MonoBehaviour
    {
        private const float FadeDuration = 0.25f;
        // Keep the transition camera isolated from every project-authored render layer.
        private const int OverlayLayer = 31;

        private static SceneTransitionService instance;

        private Canvas fadeCanvas;
        private CanvasGroup fadeGroup;
        private Camera fadeCamera;
        private bool isTransitioning;

        public static bool IsTransitioning => instance != null && instance.isTransitioning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static bool BeginLoad(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                Debug.LogError("Không thể tải cảnh vì đường dẫn cảnh bị trống.");
                return false;
            }

            SceneTransitionService service = Ensure();
            if (service.isTransitioning)
                return false;

            service.StartCoroutine(service.LoadRoutine(scenePath));
            return true;
        }

        public static IEnumerator LoadScene(string scenePath)
        {
            if (!BeginLoad(scenePath))
                yield break;

            SceneTransitionService service = instance;
            while (service != null && service.isTransitioning)
                yield return null;
        }

        private static SceneTransitionService Ensure()
        {
            if (instance != null)
                return instance;

            GameObject root = new GameObject("Scene Transition Service");
            instance = root.AddComponent<SceneTransitionService>();
            return instance;
        }

        private IEnumerator LoadRoutine(string scenePath)
        {
            isTransitioning = true;
            SetOverlayActive(true);
            yield return FadeTo(1f);

            ThreadPriority previousPriority = Application.backgroundLoadingPriority;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            AsyncOperation operation = null;
            bool sceneLoaded = false;
            try
            {
                // Give the XR compositor one fully black frame before beginning disk IO.
                yield return null;
                operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
                if (operation == null)
                {
                    Debug.LogError($"Không thể bắt đầu tải cảnh '{scenePath}'.");
                }
                else
                {
                    while (!operation.isDone)
                        yield return null;

                    // Let Awake/OnEnable finish before revealing the destination scene.
                    yield return null;
                    sceneLoaded = true;
                }
            }
            finally
            {
                Application.backgroundLoadingPriority = previousPriority;
            }

            yield return FadeTo(0f);
            SetOverlayActive(false);
            isTransitioning = false;
            if (!sceneLoaded)
                yield break;
        }

        private void CreateOverlay()
        {
            GameObject cameraObject = new GameObject("Transition Camera");
            cameraObject.layer = OverlayLayer;
            cameraObject.transform.SetParent(transform, false);
            fadeCamera = cameraObject.AddComponent<Camera>();
            fadeCamera.clearFlags = CameraClearFlags.Depth;
            fadeCamera.cullingMask = 1 << OverlayLayer;
            fadeCamera.depth = 10000f;
            fadeCamera.nearClipPlane = 0.01f;
            fadeCamera.farClipPlane = 10f;
            fadeCamera.allowHDR = false;
            fadeCamera.allowMSAA = false;

            GameObject canvasObject = new GameObject("Transition Overlay");
            canvasObject.layer = OverlayLayer;
            canvasObject.transform.SetParent(transform, false);
            fadeCanvas = canvasObject.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            fadeCanvas.worldCamera = fadeCamera;
            fadeCanvas.planeDistance = 1f;
            fadeCanvas.sortingOrder = short.MaxValue;
            canvasObject.AddComponent<GraphicRaycaster>();
            fadeGroup = canvasObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = true;
            fadeGroup.blocksRaycasts = true;

            GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(Image));
            imageObject.layer = OverlayLayer;
            imageObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            imageObject.GetComponent<Image>().color = Color.black;

            SetOverlayActive(false);
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / FadeDuration));
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
        }

        private void SetOverlayActive(bool active)
        {
            fadeCamera.enabled = active;
            fadeCanvas.enabled = active;
            fadeGroup.blocksRaycasts = active;
        }
    }
}
