using System;
using System.Collections.Generic;
using NCKH.VR.Clinic;
using NCKH.VR.Doctor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.GameControl.Editor
{
    public static class GameControlProjectSetup
    {
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";
        private const string ClinicScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string DoctorScenePath = "Assets/Scenes/Doctor/Game 2.unity";
        private const string LawyerOfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string VrPlayerPrefabPath = "Assets/Prefabs/VR Player.prefab";

        [MenuItem("NCKH/Game Control/Apply Project Setup")]
        public static void ApplyProjectSetup()
        {
            CreateStandbyScene();
            ConfigureClinicScene();
            ConfigureDoctorScene();
            ConfigureBuildSettings();
            PlayerSettings.Android.forceInternetPermission = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Game control project setup completed.");
        }

        [MenuItem("NCKH/Game Control/Apply Standby Backend Configuration")]
        public static void ApplyStandbyBackendConfiguration()
        {
            Scene scene = SceneManager.GetSceneByPath(StandbyScenePath);
            bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
            if (openedForSetup)
                scene = EditorSceneManager.OpenScene(StandbyScenePath, OpenSceneMode.Additive);

            try
            {
                StandbyStatusView statusView = FindComponentInScene<StandbyStatusView>(scene);
                if (statusView == null)
                    throw new InvalidOperationException("Standby scene has no StandbyStatusView.");

                Canvas canvas = statusView.GetComponent<Canvas>();
                if (canvas == null)
                    throw new InvalidOperationException("Standby status view has no Canvas.");

                Camera camera = FindComponentInScene<Camera>(scene);
                if (camera == null || camera.transform.parent == null)
                    throw new InvalidOperationException("Standby scene has no XR camera hierarchy.");

                TMP_Text statusText = new SerializedObject(statusView)
                    .FindProperty("statusText").objectReferenceValue as TMP_Text;
                CreateBackendConfiguration(scene, canvas, camera.transform.parent,
                    statusText == null ? null : statusText.font);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Standby backend configuration UI applied.");
            }
            finally
            {
                if (openedForSetup)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateStandbyScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Standby";

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VrPlayerPrefabPath);
            if (playerPrefab == null)
                throw new InvalidOperationException($"Missing VR Player prefab at {VrPlayerPrefabPath}.");

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.name = "VR Player";
            player.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            DisableNamedChildren(
                player.transform,
                "Locomotion",
                "Move",
                "Turn",
                "Jump",
                "Gravity",
                "Left hand",
                "Right hand");

            Camera mainCamera = player.GetComponentInChildren<Camera>(true);
            if (mainCamera == null)
                throw new InvalidOperationException("VR Player prefab has no camera.");
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.black;

            GameObject controllerObject = new GameObject("Game Controller");
            GameController controller = controllerObject.AddComponent<GameController>();
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("backendBaseUrl").stringValue = string.Empty;
            serializedController.FindProperty("healthRoute").stringValue = GameControlApi.HealthRoute;
            serializedController.FindProperty("telemetryRoute").stringValue = GameControlApi.TelemetryRoute;
            serializedController.FindProperty("controlWebSocketRoute").stringValue = GameControlApi.ControlWebSocketRoute;
            serializedController.FindProperty("ttsWebSocketRoute").stringValue = GameControlApi.TtsWebSocketRoute;
            serializedController.FindProperty("sttWebSocketRoute").stringValue = GameControlApi.SttWebSocketRoute;
            SerializedProperty scenes = serializedController.FindProperty("scenes");
            scenes.arraySize = 4;
            SetCatalogEntry(scenes.GetArrayElementAtIndex(0), "standby", StandbyScenePath);
            SetCatalogEntry(scenes.GetArrayElementAtIndex(1), "clinic", ClinicScenePath);
            SetCatalogEntry(scenes.GetArrayElementAtIndex(2), "doctor", DoctorScenePath);
            SetCatalogEntry(scenes.GetArrayElementAtIndex(3), "lawyer-office", LawyerOfficeScenePath);
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            CreateStatusCanvas(controller, mainCamera.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, StandbyScenePath);
        }

        private static void CreateStatusCanvas(GameController controller, Transform cameraTransform)
        {
            GameObject canvasObject = new GameObject(
                "Backend Status",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.SetParent(cameraTransform, false);
            canvasRect.localPosition = new Vector3(0f, 0f, 2f);
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.sizeDelta = new Vector2(900f, 360f);
            canvasRect.localScale = Vector3.one * 0.0015f;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            GameObject titleObject = CreateText("Title", canvasRect, "NCKH VR", 72f, new Vector2(0f, 80f));
            titleObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            GameObject instructionObject = CreateText(
                "Instruction",
                canvasRect,
                "Vui lòng chờ người hướng dẫn bắt đầu trò chơi",
                42f,
                new Vector2(0f, 0f));
            instructionObject.GetComponent<TextMeshProUGUI>().color = new Color(0.88f, 0.96f, 0.94f);
            GameObject statusObject = CreateText(
                "Connection Status",
                canvasRect,
                "Chưa cấu hình máy chủ",
                34f,
                new Vector2(0f, -85f));

            StandbyStatusView view = canvasObject.AddComponent<StandbyStatusView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("statusText").objectReferenceValue = statusObject.GetComponent<TMP_Text>();
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            CreateBackendConfiguration(
                canvasObject.scene,
                canvas,
                cameraTransform.parent,
                statusObject.GetComponent<TMP_Text>().font);
            Selection.activeObject = controller;
        }

        private static void CreateBackendConfiguration(
            Scene scene,
            Canvas canvas,
            Transform cameraOffset,
            TMP_FontAsset font)
        {
            BackendServerConfigurationView existingView = canvas.GetComponent<BackendServerConfigurationView>();
            if (existingView != null)
                UnityEngine.Object.DestroyImmediate(existingView);
            Transform existingPanel = canvas.transform.Find("Cấu hình máy chủ");
            if (existingPanel != null)
                UnityEngine.Object.DestroyImmediate(existingPanel.gameObject);

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1200f, 920f);
            EnsureXrEventSystem(scene);
            EnsureXrInteractionManager(scene);

            GameObject panelObject = CreateUiObject(
                "Cấu hình máy chủ",
                canvas.transform,
                new Vector2(1180f, 900f),
                Vector2.zero);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.035f, 0.055f, 0.07f, 0.98f);

            CreateLabel("Tiêu đề cấu hình", panelObject.transform, "CẤU HÌNH MÁY CHỦ",
                50f, new Vector2(1100f, 80f), new Vector2(0f, 385f), font, FontStyles.Bold);
            CreateLabel("Nhãn URL", panelObject.transform, "URL máy chủ backend",
                30f, new Vector2(1080f, 45f), new Vector2(0f, 320f), font, FontStyles.Normal);

            TMP_InputField input = CreateInputField(
                panelObject.transform,
                font,
                new Vector2(1080f, 74f),
                new Vector2(0f, 260f));
            TMP_Text validation = CreateLabel(
                "Lỗi cấu hình",
                panelObject.transform,
                string.Empty,
                25f,
                new Vector2(1080f, 52f),
                new Vector2(0f, 198f),
                font,
                FontStyles.Normal);
            validation.color = new Color(1f, 0.42f, 0.36f, 1f);

            GameObject keyboardObject = CreateUiObject(
                "Bàn phím đầy đủ",
                panelObject.transform,
                new Vector2(1120f, 410f),
                new Vector2(0f, -35f));
            VerticalLayoutGroup rowsLayout = keyboardObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 6f;
            rowsLayout.childAlignment = TextAnchor.MiddleCenter;
            rowsLayout.childControlHeight = true;
            rowsLayout.childControlWidth = true;
            rowsLayout.childForceExpandHeight = false;
            rowsLayout.childForceExpandWidth = true;

            Button keyTemplate = CreateButton(
                "Mẫu phím",
                keyboardObject.transform,
                "A",
                font,
                new Vector2(70f, 66f),
                Vector2.zero,
                28f);
            keyTemplate.gameObject.SetActive(false);

            VrKeyboard keyboard = keyboardObject.AddComponent<VrKeyboard>();
            SerializedObject serializedKeyboard = new SerializedObject(keyboard);
            serializedKeyboard.FindProperty("target").objectReferenceValue = input;
            serializedKeyboard.FindProperty("rowsRoot").objectReferenceValue = keyboardObject.GetComponent<RectTransform>();
            serializedKeyboard.FindProperty("keyTemplate").objectReferenceValue = keyTemplate;
            serializedKeyboard.ApplyModifiedPropertiesWithoutUndo();

            Button save = CreateButton(
                "Lưu cấu hình",
                panelObject.transform,
                "Lưu",
                font,
                new Vector2(240f, 72f),
                new Vector2(0f, -398f),
                34f);

            Transform leftHand = cameraOffset == null ? null : cameraOffset.Find("Left hand");
            Transform rightHand = cameraOffset == null ? null : cameraOffset.Find("Right hand");
            if (leftHand == null || rightHand == null)
                throw new InvalidOperationException("Standby XR rig is missing left or right hand objects.");
            Transform leftHandVisual = leftHand.Find("Left Hand Model");
            Transform rightHandVisual = rightHand.Find("Right Hand Model");
            if (leftHandVisual == null || rightHandVisual == null)
                throw new InvalidOperationException("Standby XR rig is missing hand visual objects.");

            BackendServerConfigurationView view = canvas.gameObject.AddComponent<BackendServerConfigurationView>();
            SerializedObject serializedView = new SerializedObject(view);
            serializedView.FindProperty("panelRoot").objectReferenceValue = panelObject;
            serializedView.FindProperty("urlInput").objectReferenceValue = input;
            serializedView.FindProperty("validationText").objectReferenceValue = validation;
            serializedView.FindProperty("saveButton").objectReferenceValue = save;
            serializedView.FindProperty("keyboard").objectReferenceValue = keyboard;
            serializedView.FindProperty("leftHand").objectReferenceValue = leftHand.gameObject;
            serializedView.FindProperty("rightHand").objectReferenceValue = rightHand.gameObject;
            serializedView.FindProperty("leftHandVisual").objectReferenceValue = leftHandVisual.gameObject;
            serializedView.FindProperty("rightHandVisual").objectReferenceValue = rightHandVisual.gameObject;
            serializedView.FindProperty("chordHoldSeconds").floatValue = 0.5f;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            panelObject.SetActive(false);
        }

        private static void EnsureXrEventSystem(Scene scene)
        {
            EventSystem eventSystem = FindComponentInScene<EventSystem>(scene);
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventSystemObject, scene);
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
        }

        private static void EnsureXrInteractionManager(Scene scene)
        {
            if (FindComponentInScene<XRInteractionManager>(scene) != null)
                return;

            GameObject managerObject = new GameObject(
                "XR Interaction Manager",
                typeof(XRInteractionManager));
            SceneManager.MoveGameObjectToScene(managerObject, scene);
        }

        private static TMP_InputField CreateInputField(
            Transform parent,
            TMP_FontAsset font,
            Vector2 size,
            Vector2 position)
        {
            GameObject fieldObject = CreateUiObject("Nhập URL máy chủ", parent, size, position);
            Image background = fieldObject.AddComponent<Image>();
            background.color = new Color(0.11f, 0.15f, 0.18f, 1f);

            GameObject viewportObject = CreateUiObject(
                "Vùng văn bản",
                fieldObject.transform,
                new Vector2(size.x - 36f, size.y - 14f),
                Vector2.zero);
            viewportObject.AddComponent<RectMask2D>();
            TMP_Text text = CreateLabel(
                "Văn bản URL",
                viewportObject.transform,
                string.Empty,
                30f,
                new Vector2(size.x - 48f, size.y - 18f),
                Vector2.zero,
                font,
                FontStyles.Normal);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            TMP_Text placeholder = CreateLabel(
                "Gợi ý URL",
                viewportObject.transform,
                "Ví dụ: http://192.168.1.10:8000",
                28f,
                new Vector2(size.x - 48f, size.y - 18f),
                Vector2.zero,
                font,
                FontStyles.Italic);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            placeholder.color = new Color(0.62f, 0.68f, 0.71f, 1f);
            placeholder.raycastTarget = false;

            TMP_InputField input = fieldObject.AddComponent<TMP_InputField>();
            input.textViewport = viewportObject.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.shouldHideMobileInput = true;
            input.shouldHideSoftKeyboard = true;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.2f, 0.6f, 1f, 0.45f);
            return input;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            string labelText,
            TMP_FontAsset font,
            Vector2 size,
            Vector2 position,
            float fontSize)
        {
            GameObject buttonObject = CreateUiObject(name, parent, size, position);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.52f, 0.72f, 1f);
            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.58f, 0.83f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            TMP_Text label = CreateLabel(
                "Nhãn",
                buttonObject.transform,
                labelText,
                fontSize,
                size,
                Vector2.zero,
                font,
                FontStyles.Bold);
            label.raycastTarget = false;
            return button;
        }

        private static GameObject CreateUiObject(
            string name,
            Transform parent,
            Vector2 size,
            Vector2 position)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            RectTransform rect = result.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return result;
        }

        private static TMP_Text CreateLabel(
            string name,
            Transform parent,
            string text,
            float fontSize,
            Vector2 size,
            Vector2 position,
            TMP_FontAsset font,
            FontStyles fontStyle)
        {
            GameObject labelObject = CreateUiObject(name, parent, size, position);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
                label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static GameObject CreateText(
            string name,
            Transform parent,
            string text,
            float fontSize,
            Vector2 anchoredPosition)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(850f, 100f);
            rect.anchoredPosition = anchoredPosition;
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            return textObject;
        }

        private static void ConfigureClinicScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ClinicScenePath, OpenSceneMode.Single);
            ClinicGameManager manager = FindComponentInScene<ClinicGameManager>(scene);
            if (manager == null)
                throw new InvalidOperationException("Clinic scene has no ClinicGameManager.");

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("startAutomatically").boolValue = false;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            GameObject timerBackdrop = FindGameObjectInScene(scene, "Round Timer Backdrop");
            if (timerBackdrop != null)
                timerBackdrop.SetActive(false);
            ClinicRemoteSceneAdapter adapter = manager.GetComponent<ClinicRemoteSceneAdapter>();
            if (adapter == null)
                adapter = manager.gameObject.AddComponent<ClinicRemoteSceneAdapter>();
            SerializedObject serializedAdapter = new SerializedObject(adapter);
            serializedAdapter.FindProperty("gameManager").objectReferenceValue = manager;
            serializedAdapter.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureDoctorScene()
        {
            Scene scene = EditorSceneManager.OpenScene(DoctorScenePath, OpenSceneMode.Single);
            DoctorGameManager manager = FindComponentInScene<DoctorGameManager>(scene);
            if (manager == null)
                throw new InvalidOperationException("Doctor scene has no DoctorGameManager.");

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("startAutomatically").boolValue = false;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            DoctorRemoteSceneAdapter adapter = manager.GetComponent<DoctorRemoteSceneAdapter>();
            if (adapter == null)
                adapter = manager.gameObject.AddComponent<DoctorRemoteSceneAdapter>();
            SerializedObject serializedAdapter = new SerializedObject(adapter);
            serializedAdapter.FindProperty("gameManager").objectReferenceValue = manager;
            serializedAdapter.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(StandbyScenePath, true),
                new EditorBuildSettingsScene(ClinicScenePath, true),
                new EditorBuildSettingsScene(DoctorScenePath, true)
            };
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T component = roots[i].GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static GameObject FindGameObjectInScene(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                    if (transforms[j].name == name)
                        return transforms[j].gameObject;
            }
            return null;
        }

        private static void SetCatalogEntry(SerializedProperty property, string id, string scenePath)
        {
            property.FindPropertyRelative("id").stringValue = id;
            property.FindPropertyRelative("scenePath").stringValue = scenePath;
        }

        private static void DisableNamedChildren(Transform root, params string[] names)
        {
            HashSet<string> disabledNames = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
                if (children[i] != root && disabledNames.Contains(children[i].name))
                    children[i].gameObject.SetActive(false);
        }

    }
}
