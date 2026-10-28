using System;
using NCKH.VR.Clinic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Clinic.Editor
{
    public static class ClinicHumanoidPatientSetup
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string AnimationFolder = "Assets/Animations/Clinic";
        private const string CalmClipPath = AnimationFolder + "/Patient Calm Supine.anim";
        private const string EmergencyClipPath = AnimationFolder + "/Patient Emergency Supine.anim";
        private const string ControllerPath = AnimationFolder + "/Patient Lying.controller";
        private const string FrozenPoseClipPath =
            "Assets/ThirdParty/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx";
        private const string FrozenPoseClipName = "HumanF@Idle01";
        private const string PrototypeName = "npc_csl_00_character_01f_02";

        private static readonly string[] FemaleCharacters =
        {
            "npc_csl_00_character_01f_01", "npc_csl_00_character_01f_02", "npc_csl_00_character_01f_03",
            "npc_csl_00_character_02f_01", "npc_csl_00_character_02f_02", "npc_csl_00_character_02f_03"
        };

        private static readonly string[] MaleCharacters =
        {
            "npc_csl_00_character_01m_01", "npc_csl_00_character_01m_02", "npc_csl_00_character_01m_03",
            "npc_csl_00_character_02m_01", "npc_csl_00_character_02m_02", "npc_csl_00_character_02m_03"
        };

        private static readonly string[] Hairstyles =
        {
            "npc_haircut_a_01", "npc_haircut_a_02", "npc_haircut_a_03", "npc_haircut_c_01",
            "npc_haircut_c_02", "npc_haircut_c_03", "npc_haircut_d_01", "npc_haircut_e_01"
        };

        private static readonly string[] FacialHair =
        {
            "npc_hairfacial_a_01", "npc_hairfacial_b_01", "npc_hairfacial_c_01", "npc_hairfacial_d_01"
        };

        [MenuItem("Tools/NCKH VR/Configure Game 1 Humanoid Patients")]
        public static void Configure()
        {
            EnsureFolder(AnimationFolder);
            AssetDatabase.DeleteAsset(CalmClipPath);
            AssetDatabase.DeleteAsset(EmergencyClipPath);
            AnimationClip frozenPose = LoadAnimationClip(FrozenPoseClipPath, FrozenPoseClipName);
            AnimatorController controller = CreateController(frozenPose);
            ConfigureScene(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Game 1 humanoid patients configured.");
        }

        private static AnimationClip LoadAnimationClip(string path, string clipName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            throw new InvalidOperationException($"Could not load animation clip '{clipName}' from {path}.");
        }

        private static AnimatorController CreateController(AnimationClip frozenPose)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Emergency", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorControllerLayer layer = controller.layers[0];
            layer.iKPass = false;
            controller.layers = new[] { layer };
            AnimatorState frozenState = stateMachine.AddState("Frozen Supine");
            frozenState.motion = frozenPose;
            stateMachine.defaultState = frozenState;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void ConfigureScene(RuntimeAnimatorController controller)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject npcRoot = FindRoot(scene, "NPCs");
            Transform patientTransform = npcRoot != null ? npcRoot.transform.Find("npc") : null;
            if (patientTransform == null)
                throw new InvalidOperationException("Game 1 requires the existing NPCs/npc patient shell.");

            PatientActor patient = patientTransform.GetComponent<PatientActor>();
            if (patient == null)
                throw new InvalidOperationException("NPCs/npc requires PatientActor.");

            Transform appearanceRoot = patientTransform.Find("Appearance Root");
            if (appearanceRoot == null)
            {
                GameObject rootObject = new GameObject("Appearance Root");
                appearanceRoot = rootObject.transform;
                appearanceRoot.SetParent(patientTransform, false);
            }

            GameObject prototype = FindRoot(scene, PrototypeName);
            CalibrateAppearanceRoot(scene, patientTransform, appearanceRoot, prototype);

            MeshRenderer oldRenderer = patientTransform.GetComponent<MeshRenderer>();
            MeshFilter oldFilter = patientTransform.GetComponent<MeshFilter>();
            if (oldRenderer != null)
                UnityEngine.Object.DestroyImmediate(oldRenderer);
            if (oldFilter != null)
                UnityEngine.Object.DestroyImmediate(oldFilter);

            PatientAppearanceController appearance = patientTransform.GetComponent<PatientAppearanceController>();
            if (appearance == null)
                appearance = patientTransform.gameObject.AddComponent<PatientAppearanceController>();

            SerializedObject serializedAppearance = new SerializedObject(appearance);
            SetReference(serializedAppearance, "appearanceRoot", appearanceRoot);
            SetObjectArray(serializedAppearance, "femaleCharacterPrefabs", LoadPrefabs(FemaleCharacters));
            SetObjectArray(serializedAppearance, "maleCharacterPrefabs", LoadPrefabs(MaleCharacters));
            SetObjectArray(serializedAppearance, "hairstylePrefabs", LoadPrefabs(Hairstyles));
            SetObjectArray(serializedAppearance, "facialHairPrefabs", LoadPrefabs(FacialHair));
            SetFloat(serializedAppearance, "maleFacialHairChance", 0.5f);
            SetReference(serializedAppearance, "lyingAnimatorController", controller);
            serializedAppearance.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedPatient = new SerializedObject(patient);
            SetReference(serializedPatient, "appearanceController", appearance);
            SetObjectArray(serializedPatient, "fadeRenderers", Array.Empty<UnityEngine.Object>());
            SetBool(serializedPatient, "enableEmergencyAlert", true);
            serializedPatient.ApplyModifiedPropertiesWithoutUndo();

            if (prototype != null)
                UnityEngine.Object.DestroyImmediate(prototype);

            EditorUtility.SetDirty(appearance);
            EditorUtility.SetDirty(patient);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CalibrateAppearanceRoot(
            Scene scene,
            Transform patientTemplate,
            Transform appearanceRoot,
            GameObject prototype)
        {
            GameObject bedsRoot = FindRoot(scene, "Beds");
            PatientBedSlot firstBed = bedsRoot != null ? bedsRoot.GetComponentInChildren<PatientBedSlot>(true) : null;
            Transform anchor = firstBed != null ? firstBed.PatientAnchor : null;

            if (prototype != null && anchor != null)
            {
                Vector3 patientScale = patientTemplate.lossyScale;
                Matrix4x4 patientAtAnchor = Matrix4x4.TRS(anchor.position, anchor.rotation, patientScale);
                appearanceRoot.localPosition = patientAtAnchor.inverse.MultiplyPoint3x4(prototype.transform.position);
                appearanceRoot.localRotation = Quaternion.Inverse(anchor.rotation) * prototype.transform.rotation;
                Vector3 prototypeScale = prototype.transform.lossyScale;
                appearanceRoot.localScale = new Vector3(
                    SafeRatio(prototypeScale.x, patientScale.x),
                    SafeRatio(prototypeScale.y, patientScale.y),
                    SafeRatio(prototypeScale.z, patientScale.z));
            }
            else if (appearanceRoot.localPosition == Vector3.zero)
            {
                appearanceRoot.localPosition = new Vector3(0f, -0.4f, 0.6f);
                appearanceRoot.localRotation = Quaternion.Euler(-90f, 0f, 90f);
                appearanceRoot.localScale = Vector3.one;
            }
        }

        private static float SafeRatio(float numerator, float denominator)
        {
            return Mathf.Abs(denominator) > 0.0001f ? numerator / denominator : numerator;
        }

        private static GameObject[] LoadPrefabs(string[] names)
        {
            GameObject[] prefabs = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path = $"Assets/ThirdParty/npc_casual_set_00/Prefabs/{names[i]}.prefab";
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabs[i] == null)
                    throw new InvalidOperationException($"Could not load NPC prefab at {path}.");
            }

            return prefabs;
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

        private static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private static void SetReference(SerializedObject target, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedObject target, string propertyName, UnityEngine.Object[] values)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetFloat(SerializedObject target, string propertyName, float value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.floatValue = value;
        }

        private static void SetBool(SerializedObject target, string propertyName, bool value)
        {
            SerializedProperty property = target.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property '{propertyName}'.");
            property.boolValue = value;
        }
    }
}
