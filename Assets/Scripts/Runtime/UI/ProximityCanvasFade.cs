using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class ProximityCanvasFade : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually the XR Origin or player root transform.")]
    [SerializeField] private Transform player;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Proximity")]
    [Tooltip("Horizontal distance in world units at which the button becomes visible.")]
    [SerializeField, Min(0f)] private float detectionRadius = 2f;

    [Header("Fade")]
    [Tooltip("Seconds needed to fade fully in or out.")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    [SerializeField] private bool useUnscaledTime = true;

    private float targetAlpha;

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        EnsureCanvasGroup();
    }

    private void OnEnable()
    {
        EnsureCanvasGroup();
        UpdateTargetVisibility(true);
    }

    private void Update()
    {
        UpdateTargetVisibility(false);

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float fadeStep = fadeDuration > 0f ? deltaTime / fadeDuration : 1f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeStep);
    }

    private void EnsureCanvasGroup()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void UpdateTargetVisibility(bool applyImmediately)
    {
        bool isClose = false;

        if (player != null)
        {
            Vector3 offset = player.position - transform.position;
            float horizontalSqrDistance = offset.x * offset.x + offset.z * offset.z;
            isClose = horizontalSqrDistance <= detectionRadius * detectionRadius;
        }

        targetAlpha = isClose ? 1f : 0f;
        canvasGroup.interactable = isClose;
        canvasGroup.blocksRaycasts = isClose;

        if (applyImmediately)
            canvasGroup.alpha = targetAlpha;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);

        const int segmentCount = 48;
        Vector3 center = transform.position;
        Vector3 previousPoint = center + Vector3.forward * detectionRadius;

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = i * Mathf.PI * 2f / segmentCount;
            Vector3 nextPoint = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * detectionRadius;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
