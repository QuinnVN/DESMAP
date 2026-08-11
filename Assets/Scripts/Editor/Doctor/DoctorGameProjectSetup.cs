using System;
using System.Collections.Generic;
using System.Linq;
using NCKH.VR.Doctor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Doctor.Editor
{
    public static class DoctorGameProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 2.unity";
        private const string GameOneScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string UiDocumentPath = "Assets/UI/Doctor/DoctorNotebook.uxml";
        private const string PanelSettingsPath = "Assets/UI/Doctor/DoctorPanelSettings.asset";
        private const string DataFolder = "Assets/Data/Doctor";
        private const string DatabasePath = DataFolder + "/Placeholder Doctor Case Database.asset";

        [MenuItem("Tools/NCKH VR/Setup Doctor Minigame")]
        public static void SetupDoctorMinigame()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(DataFolder);

            DoctorCaseDefinition[] cases = CreatePlaceholderCases();
            DoctorCaseDatabase database = CreateDatabase(cases);
            ConfigureScene(database);
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Doctor minigame setup completed.");
        }

        private static DoctorCaseDefinition[] CreatePlaceholderCases()
        {
            return new[]
            {
                CreateCaseIfMissing(
                    DataFolder + "/Placeholder Case A.asset",
                    "Alex Morgan",
                    Initial(
                        "I started feeling unwell yesterday evening after my usual long commute. "
                        + "I have a dry cough and a mild fever. I also watched a movie last night.",
                        Card("a-time", "Started yesterday evening", DoctorInfoCategory.Time),
                        Card("a-symptom-cough", "Dry cough", DoctorInfoCategory.Symptoms),
                        Card("a-symptom-fever", "Mild fever", DoctorInfoCategory.Symptoms),
                        Noise("a-movie", "Watched a movie last night")),
                    Reply("It began around seven in the evening, just after dinner.",
                        Noise("a-time-extra", "Had dinner before symptoms began")),
                    Reply("I have not travelled recently, not even out of the city.",
                        Card("a-travel", "No recent travel", DoctorInfoCategory.TravelHistory),
                        Noise("a-city", "Stayed in the city")),
                    Reply("The cough is dry and the fever has stayed mild. My favourite tea has not helped.",
                        Noise("a-tea", "Tried drinking tea")),
                    Reply("I have a history of asthma, although it has been controlled for years.",
                        Card("a-history", "History of asthma", DoctorInfoCategory.MedicalHistory),
                        Noise("a-years", "Asthma has been controlled for years")),
                    "Placeholder viral respiratory illness",
                    "Send the patient to the respiratory treatment area for assessment."),

                CreateCaseIfMissing(
                    DataFolder + "/Placeholder Case B.asset",
                    "Jordan Lee",
                    Initial(
                        "My stomach started hurting this morning. I feel nauseous and have vomited twice. "
                        + "Yesterday I bought a new umbrella.",
                        Card("b-time", "Started this morning", DoctorInfoCategory.Time),
                        Card("b-symptom-pain", "Stomach pain", DoctorInfoCategory.Symptoms),
                        Card("b-symptom-nausea", "Nausea", DoctorInfoCategory.Symptoms),
                        Card("b-symptom-vomit", "Vomited twice", DoctorInfoCategory.Symptoms),
                        Noise("b-umbrella", "Bought a new umbrella")),
                    Reply("The pain began shortly after breakfast this morning.",
                        Noise("b-breakfast", "Ate breakfast before the pain")),
                    Reply("I returned from a coastal trip two days ago and spent most of it reading.",
                        Card("b-travel", "Returned from a coastal trip two days ago", DoctorInfoCategory.TravelHistory),
                        Noise("b-reading", "Read during the trip")),
                    Reply("No other symptoms, although I am tired because I stayed up late.",
                        Noise("b-late", "Stayed up late")),
                    Reply("I have no significant medical history and take no regular medicine.",
                        Card("b-history", "No significant medical history", DoctorInfoCategory.MedicalHistory),
                        Noise("b-medicine", "Takes no regular medicine")),
                    "Placeholder gastrointestinal illness",
                    "Send the patient to the general treatment area for hydration and assessment."),

                CreateCaseIfMissing(
                    DataFolder + "/Placeholder Case C.asset",
                    "Taylor Nguyen",
                    Initial(
                        "I became short of breath after cleaning a dusty room about an hour ago. "
                        + "There is tightness in my chest, and my neighbour recently painted their door blue.",
                        Card("c-time", "Started about one hour ago", DoctorInfoCategory.Time),
                        Card("c-symptom-breath", "Shortness of breath", DoctorInfoCategory.Symptoms),
                        Card("c-symptom-tight", "Chest tightness", DoctorInfoCategory.Symptoms),
                        Noise("c-door", "Neighbour painted a door blue")),
                    Reply("It started roughly one hour ago while I was cleaning.",
                        Noise("c-cleaning", "Was cleaning when it began")),
                    Reply("I have not travelled outside the region in the last year.",
                        Card("c-travel", "No travel outside the region in the last year", DoctorInfoCategory.TravelHistory)),
                    Reply("I am wheezing as well, especially when I breathe out.",
                        Card("c-symptom-wheeze", "Wheezing", DoctorInfoCategory.Symptoms)),
                    Reply("I had childhood asthma, but I have not used an inhaler recently.",
                        Card("c-history", "History of childhood asthma", DoctorInfoCategory.MedicalHistory),
                        Noise("c-inhaler", "Has not used an inhaler recently")),
                    "Placeholder airway irritation",
                    "Send the patient to the respiratory treatment area for immediate assessment.")
            };
        }

        private static DoctorCaseDefinition CreateCaseIfMissing(
            string path,
            string patientName,
            DoctorDialogue initial,
            DoctorDialogue time,
            DoctorDialogue travel,
            DoctorDialogue symptoms,
            DoctorDialogue medicalHistory,
            string conclusion,
            string treatment)
        {
            DoctorCaseDefinition existing = AssetDatabase.LoadAssetAtPath<DoctorCaseDefinition>(path);
            if (existing != null)
                return existing;

            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                AssetDatabase.DeleteAsset(path);

            DoctorCaseDefinition created = ScriptableObject.CreateInstance<DoctorCaseDefinition>();
            created.ConfigureInEditor(
                patientName,
                1f,
                initial,
                time,
                travel,
                symptoms,
                medicalHistory,
                conclusion,
                treatment);
            AssetDatabase.CreateAsset(created, path);
            EditorUtility.SetDirty(created);
            return created;
        }

        private static DoctorCaseDatabase CreateDatabase(IEnumerable<DoctorCaseDefinition> cases)
        {
            DoctorCaseDatabase existing = AssetDatabase.LoadAssetAtPath<DoctorCaseDatabase>(DatabasePath);
            if (existing != null)
                return existing;

            if (AssetDatabase.LoadMainAssetAtPath(DatabasePath) != null)
                AssetDatabase.DeleteAsset(DatabasePath);

            DoctorCaseDatabase database = ScriptableObject.CreateInstance<DoctorCaseDatabase>();
            database.ConfigureInEditor(cases);
            AssetDatabase.CreateAsset(database, DatabasePath);
            EditorUtility.SetDirty(database);
            return database;
        }

        private static void ConfigureScene(DoctorCaseDatabase database)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject npc = FindRoot(scene, "NPC") ?? CreatePlaceholderNpc();
            GameObject notebook = FindRoot(scene, "Notebook") ?? CreatePlaceholderNotebook();
            GameObject vrPlayer = FindRequiredRoot(scene, "VR Player");
            EnsureEventSystem(scene);

            Transform model = FindRequiredDescendant(npc.transform, "Capsule");
            Transform canvasTransform = FindRequiredDescendant(npc.transform, "Canvas");
            Transform dialogueTransform = FindRequiredDescendant(npc.transform, "Dialogue");
            Transform nameTransform = FindRequiredDescendant(npc.transform, "Name");

            CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(canvasTransform.gameObject);
            AudioSource voiceSource = GetOrAdd<AudioSource>(npc);
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 1f;
            voiceSource.minDistance = 1f;
            voiceSource.maxDistance = 15f;

            DoctorPatientPresenter presenter = GetOrAdd<DoctorPatientPresenter>(npc);
            SerializedObject presenterObject = new SerializedObject(presenter);
            SetReference(presenterObject, "modelRoot", model.gameObject);
            SetReference(presenterObject, "dialogueCanvas", canvasGroup);
            SetReference(presenterObject, "patientNameText", nameTransform.GetComponent<TMP_Text>());
            SetReference(presenterObject, "dialogueText", dialogueTransform.GetComponent<TMP_Text>());
            SetReference(presenterObject, "voiceSource", voiceSource);
            presenterObject.ApplyModifiedPropertiesWithoutUndo();

            Transform notebookColliderTransform = FindRequiredDescendant(notebook.transform, "Cube");
            XRSimpleInteractable xrInteractable = GetOrAdd<XRSimpleInteractable>(notebookColliderTransform.gameObject);
            DoctorNotebookInteractable notebookActivation = GetOrAdd<DoctorNotebookInteractable>(notebook);
            SerializedObject notebookObject = new SerializedObject(notebookActivation);
            SetReference(notebookObject, "interactable", xrInteractable);
            notebookObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject uiObject = FindRoot(scene, "Doctor UI") ?? new GameObject("Doctor UI");
            PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            panelSettings.sortingOrder = 200;
            EditorUtility.SetDirty(panelSettings);

            PanelRenderer panelRenderer = GetOrAdd<PanelRenderer>(uiObject);
            panelRenderer.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiDocumentPath);
            panelRenderer.panelSettings = panelSettings;

            UIDocument legacyDocument = uiObject.GetComponent<UIDocument>();
            if (legacyDocument != null)
                UnityEngine.Object.DestroyImmediate(legacyDocument);

            DoctorNotebookUI notebookUi = GetOrAdd<DoctorNotebookUI>(uiObject);
            SerializedObject uiObjectSerialized = new SerializedObject(notebookUi);
            SetReference(uiObjectSerialized, "panelRenderer", panelRenderer);
            SetReference(
                uiObjectSerialized,
                "locomotionRoot",
                FindRequiredDescendant(vrPlayer.transform, "Locomotion").gameObject);
            uiObjectSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject managerObject = FindRoot(scene, "Doctor Game Manager")
                ?? new GameObject("Doctor Game Manager");
            DoctorGameManager manager = GetOrAdd<DoctorGameManager>(managerObject);
            SerializedObject managerSerialized = new SerializedObject(manager);
            SetReference(managerSerialized, "caseDatabase", database);
            SetReference(managerSerialized, "patientPresenter", presenter);
            SetReference(managerSerialized, "notebookInteractable", notebookActivation);
            SetReference(managerSerialized, "notebookUI", notebookUi);
            SetFloat(managerSerialized, "preparationDuration", 10f);
            SetFloat(managerSerialized, "roundDuration", 300f);
            SetFloat(managerSerialized, "dialogueFallbackDuration", 4f);
            SetFloat(managerSerialized, "conclusionDisplayDuration", 3f);
            SetFloat(managerSerialized, "patientTransitionDelay", 1f);
            SetInteger(managerSerialized, "maximumQuestionsPerPatient", 2);
            SetInteger(managerSerialized, "pointsPerCorrectInfo", 1);
            managerSerialized.FindProperty("startAutomatically").boolValue = true;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(canvasGroup);
            EditorUtility.SetDirty(voiceSource);
            EditorUtility.SetDirty(presenter);
            EditorUtility.SetDirty(xrInteractable);
            EditorUtility.SetDirty(notebookActivation);
            EditorUtility.SetDirty(panelRenderer);
            EditorUtility.SetDirty(notebookUi);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameOneScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static GameObject CreatePlaceholderNpc()
        {
            GameObject npc = new GameObject("NPC");
            npc.transform.position = new Vector3(-1.15398f, 0f, 1.42966f);

            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            model.name = "Capsule";
            model.transform.SetParent(npc.transform, false);
            model.transform.localPosition = new Vector3(0f, 0.8511f, 0f);
            model.transform.localScale = new Vector3(0.64645f, 0.8258585f, 0.64645f);

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.SetParent(npc.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            RectTransform panel = CreateRectChild(canvasRect, "Panel");
            panel.anchorMin = new Vector2(0.16f, 0.04f);
            panel.anchorMax = new Vector2(0.84f, 0.30f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            UnityEngine.UI.Image panelImage = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0.04f, 0.08f, 0.1f, 0.88f);
            panelImage.raycastTarget = false;

            TextMeshProUGUI name = CreateText(panel, "Name");
            name.rectTransform.anchorMin = new Vector2(0f, 0.72f);
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(24f, 0f);
            name.rectTransform.offsetMax = new Vector2(-24f, -8f);
            name.text = "Placeholder Patient";
            name.fontSize = 30f;
            name.fontStyle = FontStyles.Bold;
            name.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI dialogue = CreateText(panel, "Dialogue");
            dialogue.rectTransform.anchorMin = Vector2.zero;
            dialogue.rectTransform.anchorMax = new Vector2(1f, 0.72f);
            dialogue.rectTransform.offsetMin = new Vector2(28f, 18f);
            dialogue.rectTransform.offsetMax = new Vector2(-28f, -4f);
            dialogue.text = "Placeholder dialogue";
            dialogue.enableAutoSizing = true;
            dialogue.fontSizeMin = 20f;
            dialogue.fontSizeMax = 38f;
            dialogue.alignment = TextAlignmentOptions.Center;
            return npc;
        }

        private static GameObject CreatePlaceholderNotebook()
        {
            GameObject notebook = new GameObject("Notebook");
            notebook.transform.position = new Vector3(0.6918f, 0.88832f, 1.44175f);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.SetParent(notebook.transform, false);
            cube.transform.localPosition = new Vector3(-0.6491f, 0.04368f, -0.37355f);
            cube.transform.localScale = new Vector3(0.3999873f, 0.0430649f, 0.5704442f);
            return notebook;
        }

        private static void EnsureEventSystem(Scene scene)
        {
            GameObject eventSystemObject = FindRoot(scene, "EventSystem") ?? new GameObject("EventSystem");
            GetOrAdd<EventSystem>(eventSystemObject);

            BaseInputModule[] inputModules = eventSystemObject.GetComponents<BaseInputModule>();
            for (int i = 0; i < inputModules.Length; i++)
            {
                if (!(inputModules[i] is XRUIInputModule))
                    UnityEngine.Object.DestroyImmediate(inputModules[i]);
            }

            GetOrAdd<XRUIInputModule>(eventSystemObject);
        }

        private static RectTransform CreateRectChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            RectTransform rect = CreateRectChild(parent, name);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.color = Color.white;
            text.raycastTarget = false;
            if (text.font == null && TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static DoctorDialogue Initial(string transcript, params DoctorInfoCard[] cards)
        {
            return new DoctorDialogue(transcript, null, cards);
        }

        private static DoctorDialogue Reply(string transcript, params DoctorInfoCard[] cards)
        {
            return new DoctorDialogue(transcript, null, cards);
        }

        private static DoctorInfoCard Card(
            string id,
            string text,
            DoctorInfoCategory category)
        {
            return new DoctorInfoCard(id, text, category, true);
        }

        private static DoctorInfoCard Noise(string id, string text)
        {
            return new DoctorInfoCard(id, text, DoctorInfoCategory.Time, false);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string name = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject FindRequiredRoot(Scene scene, string name)
        {
            return FindRoot(scene, name)
                ?? throw new InvalidOperationException($"Game 2 requires the existing root '{name}'.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(candidate => candidate.name == name);
        }

        private static Transform FindRequiredDescendant(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i];
            }

            throw new InvalidOperationException($"'{root.name}' requires the existing child '{name}'.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }

        private static void SetReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing property '{propertyName}' on {target.targetObject}.");
            property.objectReferenceValue = value;
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing property '{propertyName}' on {target.targetObject}.");
            property.floatValue = value;
        }

        private static void SetInteger(SerializedObject target, string propertyName, int value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing property '{propertyName}' on {target.targetObject}.");
            property.intValue = value;
        }
    }
}
