using System;
using System.Collections.Generic;
using NCKH.VR.Clinic;
using NCKH.VR.Doctor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NCKH.VR.GameControl.Editor
{
    public static class GameControlProjectSetup
    {
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";
        private const string ClinicScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string DoctorScenePath = "Assets/Scenes/Doctor/Game 2.unity";
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
            scenes.arraySize = 3;
            SetCatalogEntry(scenes.GetArrayElementAtIndex(0), "standby", StandbyScenePath);
            SetCatalogEntry(scenes.GetArrayElementAtIndex(1), "clinic", ClinicScenePath);
            SetCatalogEntry(scenes.GetArrayElementAtIndex(2), "doctor", DoctorScenePath);
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
            Selection.activeObject = controller;
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
            label.enableWordWrapping = true;
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
