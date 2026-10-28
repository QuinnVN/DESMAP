using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NCKH.VR.GameControl
{
    public enum VrKeyboardCommand
    {
        Character,
        Tab,
        CapsLock,
        Shift,
        Backspace,
        Delete,
        Home,
        End,
        Left,
        Right,
        Space,
        Enter
    }

    [DisallowMultipleComponent]
    public sealed class VrKeyboard : MonoBehaviour
    {
        private readonly struct KeySpec
        {
            public KeySpec(string normal, string shifted = null, float width = 1f)
            {
                Normal = normal;
                Shifted = shifted ?? normal;
                Width = width;
                Command = VrKeyboardCommand.Character;
            }

            public KeySpec(VrKeyboardCommand command, string label, float width = 1f)
            {
                Normal = label;
                Shifted = label;
                Width = width;
                Command = command;
            }

            public string Normal { get; }
            public string Shifted { get; }
            public float Width { get; }
            public VrKeyboardCommand Command { get; }
        }

        public const int ExpectedKeyCount = 59;

        [SerializeField] private TMP_InputField target;
        [SerializeField] private RectTransform rowsRoot;
        [SerializeField] private Button keyTemplate;
        [SerializeField] private float keyHeight = 66f;
        [SerializeField] private float keySpacing = 6f;

        private readonly List<VrKeyboardKeyButton> keys = new List<VrKeyboardKeyButton>(ExpectedKeyCount);
        private bool built;
        private bool shift;
        private bool capsLock;

        public event Action EnterPressed;
        public int GeneratedKeyCount => keys.Count;

        private void Awake()
        {
            BuildKeyboard();
        }

        public void SetTarget(TMP_InputField inputField)
        {
            target = inputField;
            MoveCaretTo(target == null ? 0 : target.text.Length);
        }

        public void HandleKey(VrKeyboardKeyButton key)
        {
            if (key == null || target == null)
                return;

            switch (key.Command)
            {
                case VrKeyboardCommand.Character:
                    ReplaceSelection(key.GetOutput(shift, capsLock));
                    ConsumeShift();
                    break;
                case VrKeyboardCommand.Tab:
                    ReplaceSelection("\t");
                    break;
                case VrKeyboardCommand.CapsLock:
                    capsLock = !capsLock;
                    RefreshLabels();
                    break;
                case VrKeyboardCommand.Shift:
                    shift = !shift;
                    RefreshLabels();
                    break;
                case VrKeyboardCommand.Backspace:
                    Backspace();
                    break;
                case VrKeyboardCommand.Delete:
                    Delete();
                    break;
                case VrKeyboardCommand.Home:
                    MoveCaretTo(0);
                    break;
                case VrKeyboardCommand.End:
                    MoveCaretTo(target.text.Length);
                    break;
                case VrKeyboardCommand.Left:
                    MoveCaret(-1);
                    break;
                case VrKeyboardCommand.Right:
                    MoveCaret(1);
                    break;
                case VrKeyboardCommand.Space:
                    ReplaceSelection(" ");
                    break;
                case VrKeyboardCommand.Enter:
                    EnterPressed?.Invoke();
                    break;
            }
        }

        private void BuildKeyboard()
        {
            if (built || rowsRoot == null || keyTemplate == null)
                return;

            built = true;
            KeySpec[][] layout = CreateLayout();
            for (int rowIndex = 0; rowIndex < layout.Length; rowIndex++)
            {
                GameObject rowObject = new GameObject(
                    "Hàng phím " + (rowIndex + 1),
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup),
                    typeof(LayoutElement));
                rowObject.transform.SetParent(rowsRoot, false);
                HorizontalLayoutGroup row = rowObject.GetComponent<HorizontalLayoutGroup>();
                row.spacing = keySpacing;
                row.childAlignment = TextAnchor.MiddleCenter;
                row.childControlHeight = true;
                row.childControlWidth = true;
                row.childForceExpandHeight = true;
                row.childForceExpandWidth = true;
                rowObject.GetComponent<LayoutElement>().preferredHeight = keyHeight;

                KeySpec[] specs = layout[rowIndex];
                for (int keyIndex = 0; keyIndex < specs.Length; keyIndex++)
                    CreateKey(rowObject.transform, specs[keyIndex]);
            }

            keyTemplate.gameObject.SetActive(false);
            RefreshLabels();
        }

        private void CreateKey(Transform parent, KeySpec spec)
        {
            Button button = Instantiate(keyTemplate, parent, false);
            button.gameObject.name = "Phím " + spec.Normal;
            button.gameObject.SetActive(true);
            LayoutElement layout = button.GetComponent<LayoutElement>();
            if (layout == null)
                layout = button.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = spec.Width;
            layout.preferredHeight = keyHeight;

            VrKeyboardKeyButton key = button.GetComponent<VrKeyboardKeyButton>();
            if (key == null)
                key = button.gameObject.AddComponent<VrKeyboardKeyButton>();
            key.Configure(this, button.GetComponentInChildren<TMP_Text>(true), spec.Command,
                spec.Normal, spec.Shifted);
            keys.Add(key);
        }

        private static KeySpec[][] CreateLayout()
        {
            return new[]
            {
                new[]
                {
                    new KeySpec("`", "~"), new KeySpec("1", "!"), new KeySpec("2", "@"),
                    new KeySpec("3", "#"), new KeySpec("4", "$"), new KeySpec("5", "%"),
                    new KeySpec("6", "^"), new KeySpec("7", "&"), new KeySpec("8", "*"),
                    new KeySpec("9", "("), new KeySpec("0", ")"), new KeySpec("-", "_"),
                    new KeySpec("=", "+"), new KeySpec(VrKeyboardCommand.Backspace, "⌫", 1.8f)
                },
                new[]
                {
                    new KeySpec(VrKeyboardCommand.Tab, "Tab", 1.4f),
                    new KeySpec("q", "Q"), new KeySpec("w", "W"), new KeySpec("e", "E"),
                    new KeySpec("r", "R"), new KeySpec("t", "T"), new KeySpec("y", "Y"),
                    new KeySpec("u", "U"), new KeySpec("i", "I"), new KeySpec("o", "O"),
                    new KeySpec("p", "P"), new KeySpec("[", "{"), new KeySpec("]", "}"),
                    new KeySpec("\\", "|")
                },
                new[]
                {
                    new KeySpec(VrKeyboardCommand.CapsLock, "⇪", 1.7f),
                    new KeySpec("a", "A"), new KeySpec("s", "S"), new KeySpec("d", "D"),
                    new KeySpec("f", "F"), new KeySpec("g", "G"), new KeySpec("h", "H"),
                    new KeySpec("j", "J"), new KeySpec("k", "K"), new KeySpec("l", "L"),
                    new KeySpec(";", ":"), new KeySpec("'", "\""),
                    new KeySpec(VrKeyboardCommand.Enter, "Nhập", 1.9f)
                },
                new[]
                {
                    new KeySpec(VrKeyboardCommand.Shift, "⇧", 2.1f),
                    new KeySpec("z", "Z"), new KeySpec("x", "X"), new KeySpec("c", "C"),
                    new KeySpec("v", "V"), new KeySpec("b", "B"), new KeySpec("n", "N"),
                    new KeySpec("m", "M"), new KeySpec(",", "<"), new KeySpec(".", ">"),
                    new KeySpec("/", "?"), new KeySpec(VrKeyboardCommand.Shift, "⇧", 2.1f)
                },
                new[]
                {
                    new KeySpec(VrKeyboardCommand.Home, "Đầu", 1.3f),
                    new KeySpec(VrKeyboardCommand.End, "Cuối", 1.3f),
                    new KeySpec(VrKeyboardCommand.Left, "←", 1.1f),
                    new KeySpec(VrKeyboardCommand.Space, "Dấu cách", 5f),
                    new KeySpec(VrKeyboardCommand.Right, "→", 1.1f),
                    new KeySpec(VrKeyboardCommand.Delete, "Xóa", 1.3f)
                }
            };
        }

        private void ReplaceSelection(string insertion)
        {
            GetSelection(out int start, out int end);
            string value = target.text ?? string.Empty;
            string updated = value.Remove(start, end - start).Insert(start, insertion);
            target.SetTextWithoutNotify(updated);
            MoveCaretTo(start + insertion.Length);
        }

        private void Backspace()
        {
            GetSelection(out int start, out int end);
            if (start != end)
            {
                RemoveRange(start, end);
                return;
            }
            if (start > 0)
                RemoveRange(start - 1, start);
        }

        private void Delete()
        {
            GetSelection(out int start, out int end);
            if (start != end)
            {
                RemoveRange(start, end);
                return;
            }
            if (end < target.text.Length)
                RemoveRange(end, end + 1);
        }

        private void RemoveRange(int start, int end)
        {
            target.SetTextWithoutNotify(target.text.Remove(start, end - start));
            MoveCaretTo(start);
        }

        private void MoveCaret(int delta)
        {
            GetSelection(out int start, out int end);
            int position = delta < 0 ? start : end;
            if (start == end)
                position += delta;
            MoveCaretTo(position);
        }

        private void MoveCaretTo(int position)
        {
            if (target == null)
                return;
            position = Mathf.Clamp(position, 0, target.text == null ? 0 : target.text.Length);
            target.selectionStringAnchorPosition = position;
            target.selectionStringFocusPosition = position;
            target.ForceLabelUpdate();
        }

        private void GetSelection(out int start, out int end)
        {
            int length = target.text == null ? 0 : target.text.Length;
            int anchor = Mathf.Clamp(target.selectionStringAnchorPosition, 0, length);
            int focus = Mathf.Clamp(target.selectionStringFocusPosition, 0, length);
            start = Mathf.Min(anchor, focus);
            end = Mathf.Max(anchor, focus);
        }

        private void ConsumeShift()
        {
            if (!shift)
                return;
            shift = false;
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            for (int i = 0; i < keys.Count; i++)
                keys[i].RefreshLabel(shift, capsLock);
        }
    }

    [RequireComponent(typeof(Button))]
    public sealed class VrKeyboardKeyButton : MonoBehaviour
    {
        [SerializeField] private VrKeyboard keyboard;
        [SerializeField] private TMP_Text label;
        [SerializeField] private VrKeyboardCommand command;
        [SerializeField] private string normalValue;
        [SerializeField] private string shiftedValue;

        private Button button;

        public VrKeyboardCommand Command => command;

        private void OnEnable()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(Press);
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(Press);
        }

        public void Configure(
            VrKeyboard owner,
            TMP_Text keyLabel,
            VrKeyboardCommand keyCommand,
            string normal,
            string shifted)
        {
            keyboard = owner;
            label = keyLabel;
            command = keyCommand;
            normalValue = normal;
            shiftedValue = shifted;
        }

        public string GetOutput(bool shift, bool capsLock)
        {
            bool isLetter = normalValue != null && normalValue.Length == 1 && char.IsLetter(normalValue[0]);
            bool useShifted = isLetter ? shift ^ capsLock : shift;
            return useShifted ? shiftedValue : normalValue;
        }

        public void RefreshLabel(bool shift, bool capsLock)
        {
            if (label == null || command != VrKeyboardCommand.Character)
                return;
            label.text = GetOutput(shift, capsLock);
        }

        private void Press()
        {
            keyboard?.HandleKey(this);
        }
    }
}
