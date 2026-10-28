using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NCKH.VR.Doctor
{
    public sealed class DoctorInfoCardView : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color normalColor = new Color(1f, 0.99f, 0.94f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.82f, 0.94f, 0.89f, 1f);

        private DoctorNotebookUI owner;
        private bool dragging;

        public string CardId { get; private set; }
        public RectTransform RectTransform => (RectTransform)transform;

        public void Initialize(DoctorNotebookUI controller, string cardId, string text)
        {
            owner = controller;
            CardId = cardId;
            label.text = text ?? string.Empty;
            SetDragging(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
        }

        public void SetDragging(bool value)
        {
            dragging = value;
            if (background != null)
                background.raycastTarget = !value;
            if (label != null)
                label.raycastTarget = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!dragging)
                owner?.SelectCard(CardId);
        }

        public void OnBeginDrag(PointerEventData eventData) => owner?.BeginCardDrag(this, eventData);
        public void OnDrag(PointerEventData eventData) => owner?.DragCard(this, eventData);
        public void OnEndDrag(PointerEventData eventData) => owner?.EndCardDrag(this, eventData);
    }
}
