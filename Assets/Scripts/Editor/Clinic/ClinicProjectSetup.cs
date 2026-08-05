using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NCKH.VR.Clinic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NCKH.VR.Clinic.Editor
{
    public static class ClinicProjectSetup
    {
        private const string PatientPrefabPath = "Assets/Prefabs/npc.prefab";
        private const string BedPrefabPath = "Assets/Prefabs/Bed.prefab";
        private const string ScenePath = "Assets/Scenes/Main Scene.unity";
        private const string PatientMaterialPath = "Assets/Materials/Patient Fade.mat";

        [MenuItem("Tools/NCKH VR/Configure Clinic Gameplay")]
        public static void Configure()
        {
            Material patientMaterial = GetOrCreatePatientMaterial();
            ConfigurePatientPrefab(patientMaterial);
            ConfigureBedPrefab();
            ConfigureMainScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Clinic gameplay prefab and scene wiring completed.");
        }

        private static Material GetOrCreatePatientMaterial()
        {
            string folder = Path.GetDirectoryName(PatientMaterialPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "Materials");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(PatientMaterialPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("The URP Lit shader could not be found.");

            if (material == null)
            {
                material = new Material(shader) { name = "Patient Fade" };
                AssetDatabase.CreateAsset(material, PatientMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigurePatientPrefab(Material patientMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PatientPrefabPath);
            try
            {
                root.name = "npc";
                Transform cure = FindRequiredChild(root.transform, "Cure");
                Transform chatBox = FindRequiredChild(root.transform, "Chat box");
                Button cureButton = cure.GetComponentInChildren<Button>(true);
                TMP_Text sicknessText = chatBox.GetComponentInChildren<TMP_Text>(true);
                if (cureButton == null || sicknessText == null)
                    throw new InvalidOperationException("The npc prefab requires its existing Cure button and chat-box TMP text.");

                RemoveLegacyPatientUiComponents(root);

                Transform pivot = root.transform.Find("Cure Orbit Pivot");
                if (pivot == null)
                {
                    GameObject pivotObject = new GameObject("Cure Orbit Pivot");
                    pivot = pivotObject.transform;
                    pivot.SetParent(root.transform, false);
                }

                cure.SetParent(pivot, false);
                cure.localPosition = new Vector3(-1f, 0f, 1.1f);
                cure.localRotation = Quaternion.Euler(0f, 180f, 0f);

                CanvasGroup cureGroup = cureButton.GetComponent<CanvasGroup>();
                if (cureGroup == null)
                    cureGroup = cureButton.gameObject.AddComponent<CanvasGroup>();

                CanvasGroup chatGroup = chatBox.GetComponent<CanvasGroup>();
                if (chatGroup == null)
                    chatGroup = chatBox.gameObject.AddComponent<CanvasGroup>();

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterial = patientMaterial;

                PatientActor patient = root.GetComponent<PatientActor>();
                if (patient == null)
                    patient = root.AddComponent<PatientActor>();

                SerializedObject serializedPatient = new SerializedObject(patient);
                SetReference(serializedPatient, "sicknessText", sicknessText);
                SetReference(serializedPatient, "cureButton", cureButton);
                SetReference(serializedPatient, "chatCanvasGroup", chatGroup);
                SetReference(serializedPatient, "cureCanvasGroup", cureGroup);
                SetReference(serializedPatient, "cureOrbitPivot", pivot);
                SetReference(serializedPatient, "cureCanvas", cure);
                SetFloat(serializedPatient, "orbitRadius", 1.1f);
                SetFloat(serializedPatient, "orbitSideOffset", -1f);
                SetFloat(serializedPatient, "canvasYawOffset", 180f);
                SetObjectArray(serializedPatient, "fadeRenderers", renderers.Cast<UnityEngine.Object>().ToArray());
                SetObjectArray(serializedPatient, "patientCanvases", root.GetComponentsInChildren<Canvas>(true).Cast<UnityEngine.Object>().ToArray());
                SetObjectArray(serializedPatient, "patientColliders", root.GetComponentsInChildren<Collider>(true).Cast<UnityEngine.Object>().ToArray());
                serializedPatient.ApplyModifiedPropertiesWithoutUndo();

                sicknessText.text = "Sickness: [Placeholder] (Severity 1)";
                EditorUtility.SetDirty(sicknessText);
                PrefabUtility.SaveAsPrefabAsset(root, PatientPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureBedPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BedPrefabPath);
            try
            {
                ConfigureBedSlot(root);
                PrefabUtility.SaveAsPrefabAsset(root, BedPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMainScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject bedsRoot = FindRoot(scene, "Beds");
            if (bedsRoot == null)
                throw new InvalidOperationException("Main Scene requires its existing Beds root.");

            PatientBedSlot[] existingSlots = bedsRoot.GetComponentsInChildren<PatientBedSlot>(true);
            if (existingSlots.Length == 0)
            {
                Transform bed = bedsRoot.transform.Find("Bed");
                if (bed == null)
                    throw new InvalidOperationException("Main Scene requires at least one Bed under Beds.");

                ConfigureBedSlot(bed.gameObject);
            }

            PatientBedSlot[] bedSlots = bedsRoot.GetComponentsInChildren<PatientBedSlot>(true);
            for (int i = 0; i < bedSlots.Length; i++)
            {
                Transform prototypeCanvas = bedSlots[i].transform.Find("Canvas");
                if (prototypeCanvas != null)
                    prototypeCanvas.gameObject.SetActive(false);
            }

            GameObject prototypeNpcs = FindRoot(scene, "NPCs");
            if (prototypeNpcs != null)
            {
                Transform prototypeNpc = prototypeNpcs.transform.Find("npc");
                if (prototypeNpc != null)
                    prototypeNpc.gameObject.SetActive(false);
            }

            GameObject managerObject = FindRoot(scene, "Clinic Game Manager");
            if (managerObject == null)
                managerObject = new GameObject("Clinic Game Manager");

            ClinicGameManager manager = managerObject.GetComponent<ClinicGameManager>();
            if (manager == null)
                manager = managerObject.AddComponent<ClinicGameManager>();

            GameObject vrPlayer = FindRoot(scene, "VR Player");
            Camera trackedCamera = vrPlayer != null ? vrPlayer.GetComponentInChildren<Camera>(true) : null;
            PatientActor patientPrefab = AssetDatabase.LoadAssetAtPath<PatientActor>(PatientPrefabPath);
            if (trackedCamera == null || patientPrefab == null)
                throw new InvalidOperationException("Could not resolve the VR Player camera or npc patient prefab.");

            SerializedObject serializedManager = new SerializedObject(manager);
            SetReference(serializedManager, "patientPrefab", patientPrefab);
            SetReference(serializedManager, "playerHead", trackedCamera.transform);
            SetObjectArray(serializedManager, "bedSlots", bedSlots.Cast<UnityEngine.Object>().ToArray());
            SetFloat(serializedManager, "resolvedBedCooldown", 5f);
            SetFloat(serializedManager, "automaticStartReadinessTimeout", 10f);
            SetInteger(serializedManager, "automaticStartStableFrames", 2);
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            ConfigurePlayerHud(manager, vrPlayer, trackedCamera);

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePlayerHud(
            ClinicGameManager manager,
            GameObject vrPlayer,
            Camera trackedCamera)
        {
            Transform locomotion = FindRequiredChild(vrPlayer.transform, "Locomotion");
            RectTransform hudRoot = GetOrCreateRectChild(trackedCamera.transform, "Player HUD");
            Canvas canvas = GetOrAddComponent<Canvas>(hudRoot.gameObject);
            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(hudRoot.gameObject);
            ClinicRoundHud hud = GetOrAddComponent<ClinicRoundHud>(hudRoot.gameObject);

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = trackedCamera;
            canvas.planeDistance = 0.5f;
            canvas.sortingOrder = 100;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // A square design surface tracks the near-square per-eye XR viewport more
            // predictably than a desktop 16:9 reference while the Canvas remains driven
            // by the tracked camera's actual render target.
            scaler.referenceResolution = new Vector2(1440f, 1440f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform existingOverlay = hudRoot.Find("Preparation Overlay");
            RectTransform overlayRect = GetOrCreateRectChild(hudRoot, "Preparation Overlay");
            if (existingOverlay == null)
                StretchToParent(overlayRect);
            overlayRect.SetAsFirstSibling();
            CanvasGroup overlayGroup = GetOrAddComponent<CanvasGroup>(overlayRect.gameObject);
            overlayGroup.alpha = 0f;
            overlayGroup.interactable = false;
            overlayGroup.blocksRaycasts = false;

            Transform existingTint = overlayRect.Find("Tint");
            RectTransform tintRect = GetOrCreateRectChild(overlayRect, "Tint");
            if (existingTint == null)
                StretchToParent(tintRect);
            tintRect.SetAsFirstSibling();
            Image tint = GetOrAddComponent<Image>(tintRect.gameObject);
            tint.color = new Color(0f, 0f, 0f, 0.3f);
            tint.raycastTarget = false;

            Transform existingPreparationText = overlayRect.Find("Countdown");
            TextMeshProUGUI preparationText = GetOrCreateText(overlayRect, "Countdown");
            RectTransform preparationRect = preparationText.rectTransform;
            if (existingPreparationText == null)
                InsetWithinParent(preparationRect, 0.18f);
            preparationRect.SetAsLastSibling();
            preparationText.text = "2";
            preparationText.alignment = TextAlignmentOptions.Center;
            preparationText.fontSize = 200f;
            preparationText.fontStyle = FontStyles.Bold;
            preparationText.color = Color.white;
            preparationText.raycastTarget = false;

            Transform existingTotalScore = overlayRect.Find("Total Score");
            TextMeshProUGUI totalScoreText = GetOrCreateText(overlayRect, "Total Score");
            RectTransform totalScoreRect = totalScoreText.rectTransform;
            if (existingTotalScore == null)
                InsetWithinParent(totalScoreRect, 0.18f);
            totalScoreRect.SetAsLastSibling();
            totalScoreText.text = "TOTAL SCORE\n0";
            totalScoreText.alignment = TextAlignmentOptions.Center;
            totalScoreText.fontSize = 48f;
            totalScoreText.enableAutoSizing = true;
            totalScoreText.fontSizeMin = 32f;
            totalScoreText.fontSizeMax = 48f;
            totalScoreText.fontStyle = FontStyles.Bold;
            totalScoreText.color = Color.white;
            totalScoreText.raycastTarget = false;
            totalScoreText.gameObject.SetActive(false);

            Transform existingTimerBackdrop = hudRoot.Find("Round Timer Backdrop");
            RectTransform roundTimerBackdropRect = GetOrCreateRectChild(hudRoot, "Round Timer Backdrop");
            if (existingTimerBackdrop == null)
            {
                ConfigureRoundTimerRect(roundTimerBackdropRect);
            }
            else
            {
                // Keep designer-authored anchors and offsets when setup is rerun.
                roundTimerBackdropRect.sizeDelta = new Vector2(320f, 80f);
                roundTimerBackdropRect.pivot = new Vector2(0.5f, 0.5f);
            }
            roundTimerBackdropRect.SetAsLastSibling();
            Image roundTimerBackdrop = GetOrAddComponent<Image>(roundTimerBackdropRect.gameObject);
            roundTimerBackdrop.color = new Color(0f, 0f, 0f, 0.45f);
            roundTimerBackdrop.raycastTarget = false;

            Transform existingRoundTimer = hudRoot.Find("Round Timer");
            if (existingRoundTimer != null)
                existingRoundTimer.SetParent(roundTimerBackdropRect, false);

            TextMeshProUGUI roundTimerText = GetOrCreateText(roundTimerBackdropRect, "Round Timer");
            RectTransform roundTimerRect = roundTimerText.rectTransform;
            StretchToParent(roundTimerRect);
            roundTimerRect.SetAsLastSibling();
            roundTimerText.text = "1:00";
            roundTimerText.alignment = TextAlignmentOptions.Center;
            roundTimerText.fontSize = 46f;
            roundTimerText.fontStyle = FontStyles.Bold;
            roundTimerText.color = Color.white;
            roundTimerText.margin = new Vector4(12f, 6f, 12f, 6f);
            roundTimerText.raycastTarget = false;

            SerializedObject serializedHud = new SerializedObject(hud);
            SetReference(serializedHud, "gameManager", manager);
            SetReference(serializedHud, "locomotionRoot", locomotion.gameObject);
            SetReference(serializedHud, "roundTimerRoot", roundTimerBackdropRect.gameObject);
            SetReference(serializedHud, "roundTimerText", roundTimerText);
            SetReference(serializedHud, "preparationCountdownText", preparationText);
            SetReference(serializedHud, "totalScoreText", totalScoreText);
            SetReference(serializedHud, "preparationOverlay", overlayGroup);
            SetFloat(serializedHud, "preparationFadeDuration", 0.25f);
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            overlayRect.gameObject.SetActive(false);
            roundTimerBackdropRect.gameObject.SetActive(false);
            EditorUtility.SetDirty(canvas);
            EditorUtility.SetDirty(scaler);
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(overlayGroup);
            EditorUtility.SetDirty(tint);
            EditorUtility.SetDirty(preparationText);
            EditorUtility.SetDirty(totalScoreText);
            EditorUtility.SetDirty(roundTimerBackdrop);
            EditorUtility.SetDirty(roundTimerText);
        }

        private static PatientBedSlot ConfigureBedSlot(GameObject bed)
        {
            PatientBedSlot slot = bed.GetComponent<PatientBedSlot>();
            if (slot == null)
                slot = bed.AddComponent<PatientBedSlot>();

            Transform anchor = bed.transform.Find("Patient Anchor");
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject("Patient Anchor");
                anchor = anchorObject.transform;
                anchor.SetParent(bed.transform, false);
                anchor.localPosition = new Vector3(0f, 1.2f, 0f);
            }

            SerializedObject serializedSlot = new SerializedObject(slot);
            SetReference(serializedSlot, "patientAnchor", anchor);
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slot);
            return slot;
        }

        private static void RemoveLegacyPatientUiComponents(GameObject root)
        {
            List<MonoBehaviour> components = root.GetComponentsInChildren<MonoBehaviour>(true).ToList();
            for (int i = 0; i < components.Count; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                    continue;

                string typeName = component.GetType().Name;
                if (typeName == "FaceCamera" || typeName == "ProximityCanvasFade")
                    UnityEngine.Object.DestroyImmediate(component, true);
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                    return roots[i];
            }

            return null;
        }

        private static Transform FindRequiredChild(Transform root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                    return transforms[i];
            }

            throw new InvalidOperationException($"Required child '{name}' was not found under '{root.name}'.");
        }

        private static RectTransform GetOrCreateRectChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRect = existing as RectTransform;
                if (existingRect == null)
                    throw new InvalidOperationException($"Existing HUD object '{name}' requires a RectTransform.");
                return existingRect;
            }

            GameObject child = new GameObject(name, typeof(RectTransform));
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name)
        {
            RectTransform rect = GetOrCreateRectChild(parent, name);
            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(rect.gameObject);
            if (text.font == null && TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;
            return text;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void InsetWithinParent(RectTransform rect, float normalizedInset)
        {
            rect.anchorMin = new Vector2(normalizedInset, normalizedInset);
            rect.anchorMax = new Vector2(1f - normalizedInset, 1f - normalizedInset);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureRoundTimerRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.72f);
            rect.anchorMax = new Vector2(0.5f, 0.72f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(320f, 80f);
        }

        private static void SetReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.targetObject}.");
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.targetObject}.");

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.targetObject}.");
            property.floatValue = value;
        }

        private static void SetInteger(SerializedObject target, string propertyName, int value)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {target.targetObject}.");
            property.intValue = value;
        }
    }
}
