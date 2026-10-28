using System.Collections;
using NCKH.VR.NPC;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class NpcAppearanceRandomizerPlayModeTests
    {
        [UnityTest]
        public IEnumerator CasualNpcSpawnsRebindsHairStripsLodsAndRerolls()
        {
            GameObject instance = InstantiatePrefab("Assets/Prefabs/Casual NPC.prefab");
            yield return null;

            NpcAppearanceRandomizer randomizer = instance.GetComponent<NpcAppearanceRandomizer>();
            Assert.That(randomizer.CurrentModelPrefab, Is.Not.Null);
            Assert.That(randomizer.CurrentHairObject, Is.Not.Null);
            AssertNoLods(instance);

            Transform modelRoot = instance.transform.Find("Appearance Root/NPC Model");
            Assert.That(modelRoot, Is.Not.Null);
            AssertSharedMaterialsPreserved(randomizer.CurrentModelPrefab, modelRoot);
            AssertRenderersUseHumanoidSkeleton(modelRoot);
            AssertHairUsesModelSkeleton(randomizer, modelRoot);
            AssertHairFollowsHead(randomizer, modelRoot);

            GameObject previousModel = modelRoot.gameObject;
            Assert.That(randomizer.RandomizeAppearance(), Is.True);
            Assert.That(previousModel.activeInHierarchy, Is.False);
            yield return null;
            Assert.That(instance.transform.Find("Appearance Root/NPC Model").gameObject,
                Is.Not.SameAs(previousModel));
            AssertNoLods(instance);
            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator CasualNpcDisablesModelHaircutWhenUsingExternalHair()
        {
            GameObject instance = InstantiatePrefab("Assets/Prefabs/Casual NPC.prefab");
            NpcAppearanceRandomizer randomizer = instance.GetComponent<NpcAppearanceRandomizer>();
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/npc_casual_set_00/Prefabs/npc_csl_00_character_01f_01.prefab");
            GameObject hairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ThirdParty/npc_casual_set_00/Prefabs/npc_haircut_d_01.prefab");
            Material hairMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/ThirdParty/npc_casual_set_00/Materials/mtl_npc_hair_04.mat");
            Assert.That(modelPrefab, Is.Not.Null);
            Assert.That(hairPrefab, Is.Not.Null);
            Assert.That(hairMaterial, Is.Not.Null);

            ConfigureSingleExternalAppearance(randomizer, modelPrefab, hairPrefab, hairMaterial);
            Assert.That(randomizer.RandomizeAppearance(), Is.True);
            yield return null;

            Transform modelRoot = instance.transform.Find("Appearance Root/NPC Model");
            Assert.That(modelRoot, Is.Not.Null);
            int modelHaircutCount = 0;
            SkinnedMeshRenderer[] modelRenderers = modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < modelRenderers.Length; index++)
            {
                SkinnedMeshRenderer renderer = modelRenderers[index];
                if (!renderer.name.StartsWith("npc_haircut_", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                modelHaircutCount++;
                Assert.That(renderer.gameObject.activeInHierarchy, Is.False,
                    $"Tóc có sẵn '{renderer.name}' vẫn đang hiển thị cùng tóc ngoài.");
            }

            Assert.That(modelHaircutCount, Is.GreaterThan(0));
            Assert.That(randomizer.CurrentHairObject, Is.Not.Null);
            Assert.That(randomizer.CurrentHairObject.activeInHierarchy, Is.True);
            AssertHairUsesModelSkeleton(randomizer, modelRoot);
            AssertHairUsesMaterial(randomizer, hairMaterial);
            AssertHairFollowsHead(randomizer, modelRoot);
            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator MotionPlayerAppliesSittingPoseAndRebindsAfterRerollAndVisibilityChange()
        {
            GameObject instance = InstantiatePrefab("Assets/Prefabs/Casual NPC.prefab");
            NpcAppearanceRandomizer randomizer = instance.GetComponent<NpcAppearanceRandomizer>();
            NpcMotionPlayer motionPlayer = instance.GetComponent<NpcMotionPlayer>();
            NpcHumanoidPose pose = AssetDatabase.LoadAssetAtPath<NpcHumanoidPose>(
                "Assets/Animations/NPC/Doctor Patient Sitting.asset");
            Assert.That(pose, Is.Not.Null);

            SerializedObject serializedPlayer = new SerializedObject(motionPlayer);
            serializedPlayer.FindProperty("mode").enumValueIndex = (int)NpcMotionMode.StaticHumanoidPose;
            serializedPlayer.FindProperty("staticPose").objectReferenceValue = pose;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(randomizer.RandomizeAppearance(), Is.True);
            yield return null;
            Assert.That(motionPlayer.BoundModel, Is.SameAs(randomizer.CurrentModelInstance));
            Animator firstAnimator = randomizer.CurrentModelInstance.GetComponentInChildren<Animator>(true);
            Assert.That(firstAnimator.GetBoneTransform(HumanBodyBones.Head), Is.Not.Null);

            GameObject firstModel = randomizer.CurrentModelInstance;
            Assert.That(randomizer.RandomizeAppearance(), Is.True);
            yield return null;
            Assert.That(firstModel == null || !firstModel.activeInHierarchy, Is.True);
            Assert.That(motionPlayer.BoundModel, Is.SameAs(randomizer.CurrentModelInstance));

            instance.SetActive(false);
            Assert.That(motionPlayer.BoundModel, Is.Null);
            instance.SetActive(true);
            yield return null;
            Assert.That(motionPlayer.BoundModel, Is.SameAs(randomizer.CurrentModelInstance));
            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator SpecialNpcUsesOneHairHidesAccessoriesAndPreservesPackComponents()
        {
            GameObject instance = InstantiatePrefab("Assets/Prefabs/Special NPC.prefab");
            yield return null;

            NpcAppearanceRandomizer randomizer = instance.GetComponent<NpcAppearanceRandomizer>();
            Assert.That(randomizer.CurrentModelPrefab, Is.Not.Null);
            Assert.That(randomizer.CurrentHairObject, Is.Not.Null);
            Transform modelRoot = instance.transform.Find("Appearance Root/NPC Model");
            Assert.That(modelRoot, Is.Not.Null);
            AssertSharedMaterialsPreserved(randomizer.CurrentModelPrefab, modelRoot);

            int activeHairCount = 0;
            SkinnedMeshRenderer[] renderers = modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                SkinnedMeshRenderer renderer = renderers[index];
                if (renderer.name.StartsWith("Hairs", System.StringComparison.OrdinalIgnoreCase)
                    && renderer.gameObject.activeInHierarchy)
                {
                    activeHairCount++;
                }
            }

            Assert.That(activeHairCount, Is.EqualTo(1));
            Assert.That(randomizer.CurrentHairObject.activeInHierarchy, Is.True);
            AssertExcludedVisualsAreHidden(modelRoot);
            AssertNoLods(instance);

            MonoBehaviour[] packBehaviours = modelRoot.GetComponentsInChildren<MonoBehaviour>(true);
            Assert.That(packBehaviours.Length, Is.GreaterThan(0));
            for (int index = 0; index < packBehaviours.Length; index++)
                Assert.That(packBehaviours[index].enabled, Is.False);

            Animator animator = modelRoot.GetComponent<Animator>();
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Rigidbody[] rigidbodies = modelRoot.GetComponentsInChildren<Rigidbody>(true);
            Assert.That(rigidbodies.Length, Is.GreaterThan(0));
            for (int index = 0; index < rigidbodies.Length; index++)
                Assert.That(rigidbodies[index].isKinematic, Is.True);
            Assert.That(modelRoot.GetComponentsInChildren<Collider>(true).Length, Is.GreaterThan(0));
            Assert.That(modelRoot.GetComponentsInChildren<Joint>(true).Length, Is.GreaterThan(0));

            GameObject previousModel = modelRoot.gameObject;
            Assert.That(randomizer.RandomizeAppearance(), Is.True);
            Assert.That(previousModel.activeInHierarchy, Is.False);
            yield return null;
            Assert.That(instance.transform.Find("Appearance Root/NPC Model").gameObject,
                Is.Not.SameAs(previousModel));
            AssertExcludedVisualsAreHidden(instance.transform.Find("Appearance Root/NPC Model"));
            Object.Destroy(instance);
        }

        private static GameObject InstantiatePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null);
            return Object.Instantiate(prefab);
        }

        private static void ConfigureSingleExternalAppearance(
            NpcAppearanceRandomizer randomizer,
            GameObject modelPrefab,
            GameObject hairPrefab,
            Material hairMaterial)
        {
            SerializedObject serialized = new SerializedObject(randomizer);
            SerializedProperty femaleModels = serialized.FindProperty("femaleModelPrefabs");
            SerializedProperty maleModels = serialized.FindProperty("maleModelPrefabs");
            SerializedProperty hairstyles = serialized.FindProperty("hairstylePrefabs");
            SerializedProperty maleHairstyles = serialized.FindProperty("maleHairstylePrefabs");

            femaleModels.arraySize = 1;
            femaleModels.GetArrayElementAtIndex(0).objectReferenceValue = modelPrefab;
            maleModels.arraySize = 1;
            maleModels.GetArrayElementAtIndex(0).objectReferenceValue = modelPrefab;
            hairstyles.arraySize = 1;
            hairstyles.GetArrayElementAtIndex(0).objectReferenceValue = hairPrefab;
            maleHairstyles.arraySize = 0;
            SerializedProperty hairMaterials = serialized.FindProperty("hairMaterials");
            hairMaterials.arraySize = 1;
            hairMaterials.GetArrayElementAtIndex(0).objectReferenceValue = hairMaterial;
            serialized.FindProperty("hairMode").enumValueIndex = (int)NpcHairMode.ExternalPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertNoLods(GameObject root)
        {
            Assert.That(root.GetComponentsInChildren<LODGroup>(true), Is.Empty);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                Assert.That(NpcAppearanceRandomizer.ParseLodIndex(renderers[index].name),
                    Is.LessThanOrEqualTo(0));
            }
        }

        private static void AssertSharedMaterialsPreserved(GameObject sourcePrefab, Transform instanceRoot)
        {
            Renderer[] sourceRenderers = sourcePrefab.GetComponentsInChildren<Renderer>(true);
            Renderer[] instanceRenderers = instanceRoot.GetComponentsInChildren<Renderer>(true);
            int comparedRenderers = 0;
            for (int instanceIndex = 0; instanceIndex < instanceRenderers.Length; instanceIndex++)
            {
                Renderer instanceRenderer = instanceRenderers[instanceIndex];
                Renderer sourceRenderer = null;
                for (int sourceIndex = 0; sourceIndex < sourceRenderers.Length; sourceIndex++)
                {
                    if (sourceRenderers[sourceIndex].name == instanceRenderer.name)
                    {
                        sourceRenderer = sourceRenderers[sourceIndex];
                        break;
                    }
                }

                Assert.That(sourceRenderer, Is.Not.Null,
                    $"Không tìm thấy Renderer nguồn '{instanceRenderer.name}'.");
                Assert.That(instanceRenderer.sharedMaterials, Is.EqualTo(sourceRenderer.sharedMaterials));
                comparedRenderers++;
            }

            Assert.That(comparedRenderers, Is.GreaterThan(0));
        }

        private static void AssertRenderersUseHumanoidSkeleton(Transform modelRoot)
        {
            Animator animator = modelRoot.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Assert.That(hips, Is.Not.Null);

            Transform skeletonRoot = hips.parent != null ? hips.parent : hips;
            System.Collections.Generic.Dictionary<string, Transform> canonicalBones =
                new System.Collections.Generic.Dictionary<string, Transform>();
            Transform[] skeleton = skeletonRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < skeleton.Length; index++)
                if (!canonicalBones.ContainsKey(skeleton[index].name))
                    canonicalBones.Add(skeleton[index].name, skeleton[index]);
            for (int index = 0; index < (int)HumanBodyBones.LastBone; index++)
            {
                Transform humanBone = animator.GetBoneTransform((HumanBodyBones)index);
                if (humanBone != null)
                    canonicalBones[humanBone.name] = humanBone;
            }

            SkinnedMeshRenderer[] renderers = modelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Transform[] bones = renderers[rendererIndex].bones;
                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                {
                    Transform bone = bones[boneIndex];
                    Assert.That(bone, Is.Not.Null, renderers[rendererIndex].name);
                    Assert.That(canonicalBones[bone.name], Is.SameAs(bone),
                        $"{renderers[rendererIndex].name} dùng sai bộ xương tại '{bone.name}'.");
                }
            }
        }

        private static void AssertExcludedVisualsAreHidden(Transform modelRoot)
        {
            Transform[] transforms = modelRoot.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform candidate = transforms[index];
                string candidateName = candidate.name;
                if (candidateName.Equals("Hat", System.StringComparison.OrdinalIgnoreCase)
                    || candidateName.Equals("Glasses", System.StringComparison.OrdinalIgnoreCase)
                    || candidateName.StartsWith("npc_hairfacial_", System.StringComparison.OrdinalIgnoreCase))
                {
                    Assert.That(candidate.gameObject.activeSelf, Is.False);
                }
            }
        }

        private static void AssertHairFollowsHead(NpcAppearanceRandomizer randomizer, Transform modelRoot)
        {
            Animator animator = modelRoot.GetComponentInChildren<Animator>(true);
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Assert.That(head, Is.Not.Null);

            SkinnedMeshRenderer[] renderers = randomizer.CurrentHairObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
            {
                if (!renderers[index].enabled || renderers[index].forceRenderingOff)
                    continue;

                Assert.That(renderers[index].bounds.SqrDistance(head.position), Is.LessThan(0.01f),
                    $"Tóc '{renderers[index].name}' không còn bám theo đầu NPC.");
            }
        }

        private static void AssertHairUsesModelSkeleton(
            NpcAppearanceRandomizer randomizer,
            Transform modelRoot)
        {
            SkinnedMeshRenderer[] renderers = randomizer.CurrentHairObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));

            int activeRendererCount = 0;
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                SkinnedMeshRenderer renderer = renderers[rendererIndex];
                if (renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy)
                    activeRendererCount++;

                Transform[] bones = renderer.bones;
                for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
                    Assert.That(bones[boneIndex].IsChildOf(modelRoot), Is.True);
            }

            Assert.That(activeRendererCount, Is.GreaterThan(0));
        }

        private static void AssertHairUsesMaterial(
            NpcAppearanceRandomizer randomizer,
            Material expectedMaterial)
        {
            SkinnedMeshRenderer[] renderers = randomizer.CurrentHairObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int index = 0; index < renderers.Length; index++)
                Assert.That(renderers[index].sharedMaterial, Is.SameAs(expectedMaterial));
        }
    }
}
