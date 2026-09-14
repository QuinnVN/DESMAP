using System;
using NCKH.VR.Tutorial;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.EditorTools.Tutorial
{
    public static class TutorialSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/Tutorial.unity";
        private static readonly Color PanelColor = new Color(0.055f, 0.105f, 0.11f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.08f, 0.15f, 0.17f, 0.96f);

        [MenuItem("Tools/NCKH VR/Setup Tutorial Interactions")]
        public static void Setup()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DestroyRoot(scene, "Tutorial Interaction Canvas");
            DestroyRoot(scene, "TUTORIAL - Thẻ manh mối từ xa");

            Canvas canvas = CreateCanvas(scene);
            CreateDragArea(canvas.transform);
            CreateQuestionList(canvas.transform, scene);
            CreateRemoteClue(scene);
            ConfigureRightRay(scene);
            DisableJump(scene);

            SetLayerRecursively(canvas.gameObject, LayerMask.NameToLayer("UI"));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Đã tạo hướng dẫn kéo-thả, danh sách cuộn và vật thể nhặt bằng tia.");
        }

        private static Canvas CreateCanvas(Scene scene)
        {
            GameObject root = new GameObject(
                "Tutorial Interaction Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(TrackedDeviceGraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(1100f, 700f);
            rect.position = new Vector3(-0.38f, 1.43f, 1.45f);
            rect.rotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.001f;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            RectTransform background = CreateImage(
                root.transform, "Nền hướng dẫn", Vector2.zero,
                new Vector2(1100f, 700f), new Color(0.025f, 0.055f, 0.065f, 0.96f));
            Stretch(background);
            CreateText(root.transform, "Tiêu đề", "THỰC HÀNH TƯƠNG TÁC VR",
                new Vector2(0f, 315f), new Vector2(1020f, 55f), 34f,
                TextAlignmentOptions.Center);
            return canvas;
        }

        private static void CreateDragArea(Transform canvas)
        {
            RectTransform panel = CreateImage(
                canvas, "Khu vực kéo thả", new Vector2(-270f, -18f),
                new Vector2(500f, 570f), PanelColor);
            CreateText(panel, "Hướng dẫn kéo",
                "Trỏ tia, giữ cò và kéo thẻ vào đúng nhóm",
                new Vector2(0f, 245f), new Vector2(455f, 48f), 22f,
                TextAlignmentOptions.Center);

            RectTransform symptomZone = CreateImage(
                panel, "Triệu chứng", new Vector2(-118f, -125f),
                new Vector2(210f, 160f), new Color(0.13f, 0.28f, 0.27f, 0.95f));
            CreateText(symptomZone, "Nhãn", "TRIỆU CHỨNG",
                new Vector2(0f, -54f), new Vector2(190f, 34f), 18f,
                TextAlignmentOptions.Center);

            RectTransform historyZone = CreateImage(
                panel, "Tiền sử", new Vector2(118f, -125f),
                new Vector2(210f, 160f), new Color(0.18f, 0.22f, 0.31f, 0.95f));
            CreateText(historyZone, "Nhãn", "TIỀN SỬ",
                new Vector2(0f, -54f), new Vector2(190f, 34f), 18f,
                TextAlignmentOptions.Center);

            TMP_Text status = CreateText(panel, "Trạng thái kéo",
                "Hãy kéo thẻ thông tin", new Vector2(0f, -252f),
                new Vector2(450f, 48f), 19f, TextAlignmentOptions.Center);

            RectTransform card = CreateImage(
                panel, "Thẻ thông tin có thể kéo", new Vector2(0f, 82f),
                new Vector2(300f, 105f), new Color(1f, 0.91f, 0.55f, 1f));
            TMP_Text label = CreateText(card, "Nội dung",
                "Đau đầu kéo dài hai ngày", Vector2.zero,
                new Vector2(270f, 82f), 23f, TextAlignmentOptions.Center);
            label.color = new Color(0.08f, 0.07f, 0.04f, 1f);

            TutorialDraggableUI draggable = card.gameObject.AddComponent<TutorialDraggableUI>();
            draggable.Configure(
                panel,
                new[] { symptomZone, historyZone },
                new[] { "Triệu chứng", "Tiền sử" },
                status,
                card.GetComponent<Image>());
        }

        private static void CreateQuestionList(Transform canvas, Scene scene)
        {
            RectTransform panel = CreateImage(
                canvas, "Danh sách câu hỏi", new Vector2(275f, -18f),
                new Vector2(510f, 570f), PanelColor);
            TutorialRayHoverLocomotionGate locomotionGate =
                panel.gameObject.AddComponent<TutorialRayHoverLocomotionGate>();
            locomotionGate.Configure(FindComponentInScene<ContinuousMoveProvider>(scene));
            CreateText(panel, "Hướng dẫn danh sách", "Chọn câu hỏi phỏng vấn",
                new Vector2(0f, 245f), new Vector2(460f, 42f), 24f,
                TextAlignmentOptions.Center);

            RectTransform scrollRoot = CreateImage(
                panel, "Danh sách cuộn", new Vector2(-8f, 2f),
                new Vector2(470f, 420f), new Color(0.025f, 0.055f, 0.06f, 0.98f));
            RectTransform viewport = CreateImage(
                scrollRoot, "Khung nhìn", new Vector2(-10f, 0f),
                new Vector2(430f, 410f), new Color(1f, 1f, 1f, 0.015f));
            viewport.gameObject.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject(
                "Nội dung", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            RectTransform content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            string[] questions =
            {
                "Bạn có thể kể lại sự việc từ đầu không?",
                "Bạn ở đâu khi sự việc xảy ra?",
                "Ai đã nhìn thấy bạn tại thời điểm đó?",
                "Bạn có mâu thuẫn với người liên quan không?",
                "Bạn nhận ra vật chứng này chứ?",
                "Tại sao lời khai của hai người khác nhau?",
                "Có chi tiết nào bạn chưa nói với tôi không?",
                "Bạn có thể xác nhận mốc thời gian cuối cùng không?"
            };

            Button[] buttons = new Button[questions.Length];
            for (int index = 0; index < questions.Length; index++)
            {
                buttons[index] = CreateButton(
                    content, "Câu hỏi " + (index + 1), questions[index],
                    new Vector2(405f, 72f));
                LayoutElement itemLayout = buttons[index].gameObject.AddComponent<LayoutElement>();
                itemLayout.preferredWidth = 405f;
                itemLayout.preferredHeight = 72f;
            }

            RectTransform scrollbarRoot = CreateImage(
                scrollRoot, "Thanh cuộn", new Vector2(222f, 0f),
                new Vector2(18f, 400f), new Color(0.09f, 0.16f, 0.17f, 1f));
            Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            RectTransform handle = CreateImage(
                scrollbarRoot, "Nút kéo", Vector2.zero,
                new Vector2(16f, 110f), new Color(0.2f, 0.67f, 0.61f, 1f));
            handle.anchorMin = new Vector2(0f, 0f);
            handle.anchorMax = new Vector2(1f, 0.3f);
            handle.offsetMin = Vector2.zero;
            handle.offsetMax = Vector2.zero;
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.GetComponent<Image>();
            scrollbar.interactable = false;

            ScrollRect scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = false;
            scroll.enabled = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            TMP_Text status = CreateText(panel, "Trạng thái danh sách",
                "1/8 | Cần trái: chọn | A: xác nhận",
                new Vector2(0f, -252f), new Vector2(470f, 46f), 19f,
                TextAlignmentOptions.Center);
            InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Input/Lawyer/Lawyer Controls.inputactions");
            TutorialScrollableQuestionList controller =
                panel.gameObject.AddComponent<TutorialScrollableQuestionList>();
            controller.Configure(controls, scroll, buttons, status, locomotionGate);
        }

        private static void CreateRemoteClue(Scene scene)
        {
            GameObject root = new GameObject("TUTORIAL - Thẻ manh mối từ xa");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(0.52f, 1.12f, 1.18f);

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.34f, 0.20f, 0.03f);
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            XRGrabInteractable grab = root.AddComponent<XRGrabInteractable>();
            grab.colliders.Clear();
            grab.colliders.Add(collider);
            grab.useDynamicAttach = true;
            grab.throwOnDetach = false;
            SerializedObject serializedGrab = new SerializedObject(grab);
            serializedGrab.FindProperty("m_InteractionLayers")
                .FindPropertyRelative("m_Bits").intValue = 1 << 1;
            serializedGrab.FindProperty("m_MovementType").enumValueIndex = 1;
            serializedGrab.ApplyModifiedPropertiesWithoutUndo();

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Mặt thẻ";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(0.34f, 0.20f, 0.025f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/Lawyer/Lawyer Clue Card.mat");
            if (material != null)
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;

            TMP_Text title = CreateWorldText(root.transform, "Tiêu đề", "MANH MỐI",
                new Vector3(0f, 0.054f, -0.017f), new Vector2(30f, 4f), 13f,
                TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            CreateWorldText(root.transform, "Nội dung",
                "Biên nhận lúc 20:15\nDùng tia phải để nhặt từ xa",
                new Vector3(0f, -0.020f, -0.017f), new Vector2(30f, 11f), 9f,
                TextAlignmentOptions.Center);
        }

        private static void ConfigureRightRay(Scene scene)
        {
            XRRayInteractor ray = FindComponentInScene<XRRayInteractor>(scene);
            XRDirectInteractor rightDirect = null;
            XRDirectInteractor[] directInteractors =
                FindComponentsInScene<XRDirectInteractor>(scene);
            for (int index = 0; index < directInteractors.Length; index++)
            {
                if (directInteractors[index].name.IndexOf(
                        "Right", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                rightDirect = directInteractors[index];
                break;
            }

            if (ray == null || rightDirect == null)
                throw new InvalidOperationException(
                    "Không tìm thấy Right Ray Interactor hoặc Right Direct Interactor.");

            SerializedObject source = new SerializedObject(rightDirect);
            SerializedObject target = new SerializedObject(ray);
            CopyInputReader(source, "m_SelectInput", target, "m_SelectInput");
            SerializedProperty bits = target.FindProperty("m_InteractionLayers")
                .FindPropertyRelative("m_Bits");
            bits.intValue |= 1 << 1;
            target.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ray);
        }

        private static void CopyInputReader(
            SerializedObject source, string sourcePropertyName,
            SerializedObject target, string targetPropertyName)
        {
            string[] relativeProperties =
            {
                "m_InputSourceMode",
                "m_InputActionReferencePerformed",
                "m_InputActionReferenceValue",
                "m_ObjectReferenceObject"
            };
            SerializedProperty sourceReader = source.FindProperty(sourcePropertyName);
            SerializedProperty targetReader = target.FindProperty(targetPropertyName);
            for (int index = 0; index < relativeProperties.Length; index++)
            {
                SerializedProperty from =
                    sourceReader.FindPropertyRelative(relativeProperties[index]);
                SerializedProperty to =
                    targetReader.FindPropertyRelative(relativeProperties[index]);
                if (from == null || to == null)
                    continue;
                if (from.propertyType == SerializedPropertyType.ObjectReference)
                    to.objectReferenceValue = from.objectReferenceValue;
                else if (from.propertyType == SerializedPropertyType.Enum)
                    to.enumValueIndex = from.enumValueIndex;
                else
                    to.intValue = from.intValue;
            }
        }

        private static RectTransform CreateImage(
            Transform parent, string name, Vector2 position,
            Vector2 size, Color color)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text CreateText(
            Transform parent, string name, string value, Vector2 position,
            Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(
            Transform parent, string name, string label, Vector2 size)
        {
            RectTransform root = CreateImage(
                parent, name, Vector2.zero, size, ButtonColor);
            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = root.GetComponent<Image>();
            CreateText(root, "Nhãn", label, Vector2.zero,
                size - new Vector2(26f, 12f), 22f,
                TextAlignmentOptions.MidlineLeft);
            return button;
        }

        private static TMP_Text CreateWorldText(
            Transform parent, string name, string value, Vector3 localPosition,
            Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.localPosition = localPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.01f;
            rect.sizeDelta = size;
            TextMeshPro text = go.GetComponent<TextMeshPro>();
            text.text = value;
            text.fontSize = fontSize;
            text.color = new Color(0.08f, 0.07f, 0.04f, 1f);
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
                transforms[index].gameObject.layer = layer;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            System.Collections.Generic.List<T> results =
                new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static void DestroyRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != name)
                    continue;
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }
        }

        private static void DisableJump(Scene scene)
        {
            JumpProvider jumpProvider = FindComponentInScene<JumpProvider>(scene);
            if (jumpProvider == null)
                return;

            jumpProvider.enabled = false;
            EditorUtility.SetDirty(jumpProvider);
        }
    }
}
