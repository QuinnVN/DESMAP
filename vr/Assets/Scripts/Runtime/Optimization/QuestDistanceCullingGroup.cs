using UnityEngine;

namespace NCKH.VR.Optimization
{
    /// <summary>
    /// Spreads distance checks across frames and disables small decorative renderers
    /// that are too far away to contribute useful detail on a standalone headset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QuestDistanceCullingGroup : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(1f)] private float maxDistance = 12f;
        [SerializeField, Min(16)] private int checksPerFrame = 256;

        private Renderer[] renderers;
        private bool[] initiallyEnabled;
        private int nextRenderer;

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            initiallyEnabled = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                initiallyEnabled[i] = renderers[i] != null && renderers[i].enabled;
        }

        private void Update()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null || renderers == null || renderers.Length == 0)
                return;

            Vector3 cameraPosition = targetCamera.transform.position;
            float disableDistanceSqr = maxDistance * maxDistance;
            float enableDistance = maxDistance * 0.9f;
            float enableDistanceSqr = enableDistance * enableDistance;
            int count = Mathf.Min(checksPerFrame, renderers.Length);

            for (int processed = 0; processed < count; processed++)
            {
                int index = nextRenderer++;
                if (nextRenderer >= renderers.Length)
                    nextRenderer = 0;

                Renderer item = renderers[index];
                if (item == null || !initiallyEnabled[index])
                    continue;

                float distanceSqr = item.bounds.SqrDistance(cameraPosition);
                if (item.enabled)
                {
                    if (distanceSqr > disableDistanceSqr)
                        item.enabled = false;
                }
                else if (distanceSqr < enableDistanceSqr)
                {
                    item.enabled = true;
                }
            }
        }

        private void OnDisable()
        {
            if (renderers == null || initiallyEnabled == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].enabled = initiallyEnabled[i];
        }
    }
}
