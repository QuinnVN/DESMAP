using System;
using UnityEngine;

namespace NCKH.VR.Sales
{
    [Serializable]
    public sealed class SalesDialogueLine
    {
        [TextArea(3, 8)] public string text;
        public AudioClip audioClip;
    }

    [Serializable]
    public sealed class SalesShoeData
    {
        public string label;
        [Min(0)] public int price;
        public GameObject prefab;
        [TextArea(3, 8)] public string properties;
        [TextArea(2, 5)] public string feedback;
        [TextArea(3, 8)] public string objection;
        [Min(-1)] public int comparisonAlternativeIndex = -1;
        public bool bestFit;
        [Min(1)] public float rotationSpeed = 18;
    }

    [CreateAssetMenu(menuName = "NCKH/Sales/Mission Data")]
    public sealed class SalesMissionData : ScriptableObject
    {
        public string customerName = "Minh";
        [TextArea(3, 8)] public string closingReply = "Cảm ơn bạn nhé! Mình hiểu vì sao đôi giày này phù hợp với nhu cầu của mình hơn rồi.";
        public SalesDialogueLine[] dialogue = Array.Empty<SalesDialogueLine>();
        public SalesShoeData[] shoes = Array.Empty<SalesShoeData>();

        public bool IsValid => dialogue != null && dialogue.Length > 0 && shoes != null
            && shoes.Length == 3 && Array.TrueForAll(dialogue, l => l != null && !string.IsNullOrWhiteSpace(l.text))
            && !string.IsNullOrWhiteSpace(closingReply)
            && Array.TrueForAll(shoes, s => s != null && s.prefab != null && !string.IsNullOrWhiteSpace(s.label)
                && s.price > 0 && !string.IsNullOrWhiteSpace(s.properties) && !string.IsNullOrWhiteSpace(s.objection))
            && CountBestFits() == 1 && ObjectionsReferenceEarlierShoes();

        private int CountBestFits()
        {
            int count = 0;
            if (shoes == null) return count;
            for (int index = 0; index < shoes.Length; index++)
                if (shoes[index] != null && shoes[index].bestFit) count++;
            return count;
        }

        private bool ObjectionsReferenceEarlierShoes()
        {
            if (shoes == null) return false;
            for (int index = 0; index < shoes.Length; index++)
            {
                SalesShoeData shoe = shoes[index];
                if (shoe == null || shoe.comparisonAlternativeIndex < 0
                    || shoe.comparisonAlternativeIndex >= shoes.Length
                    || shoe.comparisonAlternativeIndex == index)
                    return false;
                SalesShoeData alternative = shoes[shoe.comparisonAlternativeIndex];
                if (alternative == null || string.IsNullOrWhiteSpace(alternative.label)
                    || shoe.objection.IndexOf(alternative.label, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }
    }
}
