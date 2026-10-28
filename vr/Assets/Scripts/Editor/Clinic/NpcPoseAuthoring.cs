using System;
using NCKH.VR.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NCKH.VR.Clinic.Editor
{
    public static class NpcPoseAuthoring
    {
        public const string AuthoringRigPath = "Assets/Prefabs/Authoring/NPC Humanoid Pose Rig.prefab";
        private const string SourceModelPath =
            "Assets/ThirdParty/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_01.prefab";

        [MenuItem("Tools/NCKH VR/NPC/Create Base Humanoid Pose Rig")]
        public static void CreateAuthoringRigInScene()
        {
            GameObject rigPrefab = EnsureAuthoringRigAsset();
            GameObject instance = PrefabUtility.InstantiatePrefab(rigPrefab) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Không thể tạo rig Humanoid để chỉnh tư thế.");

            Undo.RegisterCreatedObjectUndo(instance, "Create NPC Humanoid Pose Rig");
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Selection.activeGameObject = instance;
            EditorGUIUtility.PingObject(instance);
        }

        [MenuItem("Tools/NCKH VR/NPC/Capture Selected Humanoid Pose...")]
        public static void CaptureSelectedPose()
        {
            Animator animator = FindSelectedAnimator();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
                throw new InvalidOperationException("Hãy chọn một rig có Animator Humanoid hợp lệ.");

            string path = EditorUtility.SaveFilePanelInProject(
                "Lưu tư thế Humanoid",
                "NPC Humanoid Pose",
                "asset",
                "Chọn vị trí lưu tư thế NPC.",
                "Assets/Animations/NPC");
            if (string.IsNullOrEmpty(path))
                return;

            NpcHumanoidPose poseAsset = AssetDatabase.LoadAssetAtPath<NpcHumanoidPose>(path);
            if (poseAsset == null)
            {
                poseAsset = ScriptableObject.CreateInstance<NpcHumanoidPose>();
                AssetDatabase.CreateAsset(poseAsset, path);
            }

            HumanPose pose = new HumanPose();
            using (HumanPoseHandler handler = new HumanPoseHandler(animator.avatar, animator.transform))
                handler.GetHumanPose(ref pose);

            poseAsset.Capture(pose, animator);
            EditorUtility.SetDirty(poseAsset);
            AssetDatabase.SaveAssets();
            Selection.activeObject = poseAsset;
            EditorGUIUtility.PingObject(poseAsset);
        }

        public static GameObject EnsureAuthoringRigAsset()
        {
            EnsureFolder("Assets/Prefabs/Authoring");
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AuthoringRigPath);
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath);
            if (source == null)
                throw new InvalidOperationException($"Không tìm thấy mô hình Humanoid tại '{SourceModelPath}'.");

            GameObject root = new GameObject("NPC Humanoid Pose Rig");
            try
            {
                GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
                if (model == null)
                    throw new InvalidOperationException("Không thể tạo mô hình cho rig chỉnh tư thế.");

                model.name = "Humanoid Model";
                model.transform.SetParent(root.transform, false);
                MonoBehaviour[] behaviours = model.GetComponentsInChildren<MonoBehaviour>(true);
                for (int index = 0; index < behaviours.Length; index++)
                    behaviours[index].enabled = false;

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, AuthoringRigPath);
                if (saved == null)
                    throw new InvalidOperationException("Không thể lưu prefab rig chỉnh tư thế.");
                return saved;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void EnsureFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static Animator FindSelectedAnimator()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return null;
            Animator animator = selected.GetComponent<Animator>();
            return animator != null ? animator : selected.GetComponentInChildren<Animator>(true);
        }
    }
}
