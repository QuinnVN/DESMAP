using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NCKH.VR.Doctor
{
    public sealed class DoctorQuestionItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private Color normalColor = new Color(0.08f, 0.15f, 0.17f, 0.92f);
        [SerializeField] private Color selectedColor = new Color(0.19f, 0.53f, 0.48f, 1f);

        public void SetContent(string text, bool selected)
        {
            if (label != null)
                label.text = text ?? string.Empty;
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
        }

        public void ConfigureLayout(float height, float minimumFontSize, float maximumFontSize)
        {
            height = Mathf.Max(0f, height);
            if (layoutElement == null)
                layoutElement = GetComponent<LayoutElement>();

            if (layoutElement != null)
            {
                layoutElement.minHeight = height;
                layoutElement.preferredHeight = height;
                layoutElement.flexibleHeight = 0f;
            }

            if (label != null)
            {
                RectTransform labelRect = label.rectTransform;
                float labelHeightRatio = Mathf.Abs(labelRect.anchorMax.y - labelRect.anchorMin.y);
                float innerHeight = Mathf.Max(1f, height * labelHeightRatio - Mathf.Abs(labelRect.sizeDelta.y));
                float effectiveMinimum = Mathf.Min(Mathf.Max(1f, minimumFontSize), innerHeight);
                float effectiveMaximum = Mathf.Max(effectiveMinimum, Mathf.Min(maximumFontSize, innerHeight));
                label.enableAutoSizing = true;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.alignment = TextAlignmentOptions.Center;
                label.fontSizeMin = effectiveMinimum;
                label.fontSizeMax = effectiveMaximum;
            }
        }
    }
}
