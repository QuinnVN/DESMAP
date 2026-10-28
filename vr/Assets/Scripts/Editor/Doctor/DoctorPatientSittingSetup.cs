using System;
using NCKH.VR.Clinic.Editor;
using NCKH.VR.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Doctor.Editor
{
    public static class DoctorPatientSittingSetup
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 2.unity";
        private const string CasualNpcPath = "Assets/Prefabs/Casual NPC.prefab";
        private const string PoseFolder = "Assets/Animations/NPC";
        private const string PosePath = PoseFolder + "/Doctor Patient Sitting.asset";

        [MenuItem("Tools/NCKH VR/Doctor/Configure Game 2 Sitting Patient")]
        public static void Configure()
        {
            NpcPoseAuthoring.EnsureFolder(PoseFolder);
            NpcHumanoidPose pose = CreateOrUpdateSittingPose();
            NpcPoseAuthoring.EnsureAuthoringRigAsset();
            EnsureMotionPlayerOnCasualNpcPrefab();
            ConfigureGameTwoScene(pose);
            AssetDatabase.SaveAssets();
            Debug.Log("Đã cấu hình bệnh nhân ngồi và hệ thống tư thế NPC cho Game 2.");
        }

        private static NpcHumanoidPose CreateOrUpdateSittingPose()
        {
            NpcHumanoidPose pose = AssetDatabase.LoadAssetAtPath<NpcHumanoidPose>(PosePath);
            if (pose == null)
            {
                pose = ScriptableObject.CreateInstance<NpcHumanoidPose>();
                AssetDatabase.CreateAsset(pose, PosePath);
            }

            float[] muscles = new float[HumanTrait.MuscleCount];
            SetMuscle(muscles, "Spine Front-Back", -0.15f);
            SetMuscle(muscles, "Chest Front-Back", 0.08f);
            SetMuscle(muscles, "UpperChest Front-Back", 0.05f);

            SetMuscle(muscles, "Left Upper Leg Front-Back", -0.65f);
            SetMuscle(muscles, "Right Upper Leg Front-Back", -0.65f);
            SetMuscle(muscles, "Left Upper Leg In-Out", -0.08f);
            SetMuscle(muscles, "Right Upper Leg In-Out", -0.08f);
            SetMuscle(muscles, "Left Lower Leg Stretch", -0.15f);
            SetMuscle(muscles, "Right Lower Leg Stretch", -0.15f);
            SetMuscle(muscles, "Left Foot Up-Down", 0.2f);
            SetMuscle(muscles, "Right Foot Up-Down", 0.2f);

            SetMuscle(muscles, "Left Shoulder Front-Back", 0.1f);
            SetMuscle(muscles, "Right Shoulder Front-Back", 0.1f);
            SetMuscle(muscles, "Left Shoulder Down-Up", -0.5f);
            SetMuscle(muscles, "Right Shoulder Down-Up", -0.5f);
            SetMuscle(muscles, "Left Arm Down-Up", -0.6f);
            SetMuscle(muscles, "Right Arm Down-Up", -0.6f);
            SetMuscle(muscles, "Left Arm Front-Back", 0.2f);
            SetMuscle(muscles, "Right Arm Front-Back", 0.2f);
            SetMuscle(muscles, "Left Forearm Stretch", 0.1f);
            SetMuscle(muscles, "Right Forearm Stretch", 0.1f);
            SetMuscle(muscles, "Left Forearm Twist In-Out", 0.5f);
            SetMuscle(muscles, "Right Forearm Twist In-Out", 0.5f);
            SetMuscle(muscles, "Left Hand Down-Up", 0.1f);
            SetMuscle(muscles, "Right Hand Down-Up", 0.1f);

            pose.Configure(new Vector3(0f, 0.62f, -0.02f), Quaternion.identity, muscles);
            EditorUtility.SetDirty(pose);
            return pose;
        }

        private static void EnsureMotionPlayerOnCasualNpcPrefab()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(CasualNpcPath);
            try
            {
                NpcMotionPlayer player = contents.GetComponent<NpcMotionPlayer>();
                if (player == null)
                    player = contents.AddComponent<NpcMotionPlayer>();

                SerializedObject serialized = new SerializedObject(player);
                serialized.FindProperty("mode").enumValueIndex = (int)NpcMotionMode.None;
                serialized.FindProperty("staticPose").objectReferenceValue = null;
                serialized.FindProperty("animationClip").objectReferenceValue = null;
                serialized.FindProperty("modelLocalPosition").vector3Value = Vector3.zero;
                serialized.FindProperty("modelLocalEulerAngles").vector3Value = Vector3.zero;
                serialized.FindProperty("trackHead").boolValue = false;
                serialized.FindProperty("lookTarget").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, CasualNpcPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ConfigureGameTwoScene(NpcHumanoidPose pose)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject npc = FindRoot(scene, "NPC")
                ?? throw new InvalidOperationException("Game 2 cần GameObject gốc 'NPC'.");
            NpcAppearanceRandomizer randomizer = npc.GetComponentInChildren<NpcAppearanceRandomizer>(true)
                ?? throw new InvalidOperationException("NPC Game 2 cần một Casual NPC đã kết nối prefab.");
            GameObject modelRoot = randomizer.gameObject;
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(modelRoot);
            if (prefabPath != CasualNpcPath)
                throw new InvalidOperationException("Mô hình bệnh nhân Game 2 không còn kết nối Casual NPC.prefab.");

            NpcMotionPlayer player = modelRoot.GetComponent<NpcMotionPlayer>()
                ?? throw new InvalidOperationException("Casual NPC.prefab chưa có NpcMotionPlayer.");
            Camera xrCamera = FindMainCamera(scene)
                ?? throw new InvalidOperationException("Game 2 cần camera có tag MainCamera.");

            modelRoot.transform.SetLocalPositionAndRotation(
                Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
            modelRoot.transform.localScale = Vector3.one;

            SerializedObject serialized = new SerializedObject(player);
            serialized.FindProperty("mode").enumValueIndex = (int)NpcMotionMode.StaticHumanoidPose;
            serialized.FindProperty("staticPose").objectReferenceValue = pose;
            serialized.FindProperty("animationClip").objectReferenceValue = null;
            serialized.FindProperty("playbackSpeed").floatValue = 1f;
            serialized.FindProperty("loopAnimation").boolValue = true;
            serialized.FindProperty("modelLocalPosition").vector3Value = Vector3.zero;
            serialized.FindProperty("modelLocalEulerAngles").vector3Value = Vector3.zero;
            serialized.FindProperty("trackHead").boolValue = true;
            serialized.FindProperty("lookTarget").objectReferenceValue = xrCamera.transform;
            serialized.FindProperty("maximumYaw").floatValue = 50f;
            serialized.FindProperty("maximumPitch").floatValue = 25f;
            serialized.FindProperty("headTurnSpeed").floatValue = 120f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedAppearance = new SerializedObject(randomizer);
            serializedAppearance.FindProperty("externalHairSurfaceOffset").floatValue = 0.003f;
            SerializedProperty allHairstyles = serializedAppearance.FindProperty("hairstylePrefabs");
            if (allHairstyles.arraySize < 8)
                throw new InvalidOperationException("Casual NPC cần đủ kiểu tóc để chọn tóc tương thích cho NPC nam.");
            SerializedProperty maleHairstyles = serializedAppearance.FindProperty("maleHairstylePrefabs");
            maleHairstyles.arraySize = 2;
            maleHairstyles.GetArrayElementAtIndex(0).objectReferenceValue =
                allHairstyles.GetArrayElementAtIndex(6).objectReferenceValue;
            maleHairstyles.GetArrayElementAtIndex(1).objectReferenceValue =
                allHairstyles.GetArrayElementAtIndex(7).objectReferenceValue;
            serializedAppearance.ApplyModifiedPropertiesWithoutUndo();

            CapsuleCollider collider = modelRoot.GetComponent<CapsuleCollider>();
            if (collider == null)
                collider = modelRoot.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.62f, 0f);
            collider.radius = 0.36f;
            collider.height = 1.2f;
            collider.direction = 1;
            collider.isTrigger = false;

            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(collider);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetMuscle(float[] muscles, string muscleName, float value)
        {
            string[] names = HumanTrait.MuscleName;
            for (int index = 0; index < names.Length; index++)
            {
                if (names[index] != muscleName)
                    continue;
                muscles[index] = value;
                return;
            }

            throw new InvalidOperationException($"Không tìm thấy muscle Humanoid '{muscleName}'.");
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
                if (roots[index].name == name)
                    return roots[index];
            return null;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Camera[] cameras = roots[rootIndex].GetComponentsInChildren<Camera>(true);
                for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
                    if (cameras[cameraIndex].CompareTag("MainCamera"))
                        return cameras[cameraIndex];
            }
            return null;
        }
    }
}
