using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NCKH.VR.Tutorial
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TutorialDraggableUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform dragBounds;
        [SerializeField] private RectTransform[] dropZones = System.Array.Empty<RectTransform>();
        [SerializeField] private string[] dropZoneLabels = System.Array.Empty<string>();
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(1f, 0.91f, 0.55f, 1f);
        [SerializeField] private Color draggingColor = new Color(1f, 0.75f, 0.25f, 1f);

        private RectTransform rectTransform;

        public void Configure(
            RectTransform configuredBounds,
            RectTransform[] configuredDropZones,
            string[] configuredDropZoneLabels,
            TMP_Text configuredStatusLabel,
            Image configuredBackground)
        {
            dragBounds = configuredBounds;
            dropZones = configuredDropZones ?? System.Array.Empty<RectTransform>();
            dropZoneLabels = configuredDropZoneLabels ?? System.Array.Empty<string>();
            statusLabel = configuredStatusLabel;
            background = configuredBackground;
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            if (background == null)
                background = GetComponent<Image>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            transform.SetAsLastSibling();
            if (background != null)
                background.color = draggingColor;
            SetStatus("Đang kéo thẻ thông tin...");
            MoveToPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            MoveToPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (background != null)
                background.color = normalColor;

            for (int index = 0; index < dropZones.Length; index++)
            {
                RectTransform zone = dropZones[index];
                if (zone == null || !RectTransformUtility.RectangleContainsScreenPoint(
                        zone, eventData.position, eventData.pressEventCamera))
                    continue;

                Vector3 worldCenter = zone.TransformPoint(zone.rect.center);
                rectTransform.anchoredPosition = dragBounds.InverseTransformPoint(worldCenter);
                string zoneLabel = index < dropZoneLabels.Length
                    ? dropZoneLabels[index]
                    : zone.name;
                SetStatus($"Đã xếp thẻ vào: {zoneLabel}");
                return;
            }

            SetStatus("Đã thả thẻ. Hãy kéo vào một vùng phân loại.");
        }

        private void MoveToPointer(PointerEventData eventData)
        {
            if (dragBounds == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragBounds, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
                return;

            Rect bounds = dragBounds.rect;
            Rect card = RectTransform.rect;
            float minX = bounds.xMin + card.width * RectTransform.pivot.x;
            float maxX = bounds.xMax - card.width * (1f - RectTransform.pivot.x);
            float minY = bounds.yMin + card.height * RectTransform.pivot.y;
            float maxY = bounds.yMax - card.height * (1f - RectTransform.pivot.y);
            rectTransform.anchoredPosition = new Vector2(
                Mathf.Clamp(localPoint.x, minX, maxX),
                Mathf.Clamp(localPoint.y, minY, maxY));
        }

        private RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = (RectTransform)transform;
                return rectTransform;
            }
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
