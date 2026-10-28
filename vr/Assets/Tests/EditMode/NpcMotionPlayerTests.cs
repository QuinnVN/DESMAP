using NCKH.VR.NPC;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class NpcMotionPlayerTests
    {
        private const string CasualNpcPath = "Assets/Prefabs/Casual NPC.prefab";
        private const string SittingPosePath = "Assets/Animations/NPC/Doctor Patient Sitting.asset";

        [Test]
        public void SittingPoseContainsEveryHumanoidMuscle()
        {
            NpcHumanoidPose pose = AssetDatabase.LoadAssetAtPath<NpcHumanoidPose>(SittingPosePath);
            Assert.That(pose, Is.Not.Null);
            Assert.That(pose.IsComplete, Is.True);
            Assert.That(pose.MuscleCount, Is.EqualTo(HumanTrait.MuscleCount));
        }

        [Test]
        public void CasualNpcProvidesInactiveMotionPlayerByDefault()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CasualNpcPath);
            Assert.That(prefab, Is.Not.Null);
            NpcMotionPlayer player = prefab.GetComponent<NpcMotionPlayer>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Mode, Is.EqualTo(NpcMotionMode.None));
            Assert.That(player.StaticPose, Is.Null);
            Assert.That(player.AnimationClip, Is.Null);
            Assert.That(player.TrackHead, Is.False);
        }

        [Test]
        public void EveryCasualNpcModelHasValidHumanoidHead()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CasualNpcPath);
            NpcAppearanceRandomizer randomizer = prefab.GetComponent<NpcAppearanceRandomizer>();
            SerializedObject serialized = new SerializedObject(randomizer);
            AssertModelArray(serialized.FindProperty("femaleModelPrefabs"));
            AssertModelArray(serialized.FindProperty("maleModelPrefabs"));
        }

        private static void AssertModelArray(SerializedProperty models)
        {
            Assert.That(models.arraySize, Is.EqualTo(6));
            for (int index = 0; index < models.arraySize; index++)
            {
                GameObject model = models.GetArrayElementAtIndex(index).objectReferenceValue as GameObject;
                Assert.That(model, Is.Not.Null);
                Animator animator = model.GetComponentInChildren<Animator>(true);
                Assert.That(animator, Is.Not.Null, model.name);
                Assert.That(animator.avatar, Is.Not.Null, model.name);
                Assert.That(animator.avatar.isHuman, Is.True, model.name);
                Assert.That(animator.GetBoneTransform(HumanBodyBones.Head), Is.Not.Null, model.name);
            }
        }
    }
}
