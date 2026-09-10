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
        public GameObject prefab;
        [TextArea(3, 8)] public string properties;
        [TextArea(2, 5)] public string feedback;
        public bool bestFit;
        [Min(1)] public float rotationSpeed = 18;
    }

    [CreateAssetMenu(menuName = "NCKH/Sales/Mission Data")]
    public sealed class SalesMissionData : ScriptableObject
    {
        public string customerName = "Minh";
        public SalesDialogueLine[] dialogue = Array.Empty<SalesDialogueLine>();
        public SalesShoeData[] shoes = Array.Empty<SalesShoeData>();

        public bool IsValid => dialogue != null && dialogue.Length > 0 && shoes != null
            && shoes.Length == 3 && Array.TrueForAll(dialogue, l => l != null && !string.IsNullOrWhiteSpace(l.text))
            && Array.TrueForAll(shoes, s => s != null && s.prefab != null && !string.IsNullOrWhiteSpace(s.label));
    }
}
