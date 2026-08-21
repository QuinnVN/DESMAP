using NCKH.VR.NPC;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class NpcAppearanceRandomizerTests
    {
        [TestCase(0f, NpcGender.Female)]
        [TestCase(0.4999f, NpcGender.Female)]
        [TestCase(0.5f, NpcGender.Male)]
        [TestCase(1f, NpcGender.Male)]
        public void GenderSelectionUsesAnEvenSplit(float sample, NpcGender expected)
        {
            Assert.That(NpcAppearanceRandomizer.SelectGender(sample), Is.EqualTo(expected));
        }

        [Test]
        public void IndexSelectionIsBoundedAndCanReachEveryEntry()
        {
            for (int index = 0; index < 24; index++)
            {
                float sample = (index + 0.5f) / 24f;
                Assert.That(NpcAppearanceRandomizer.SelectIndex(sample, 24), Is.EqualTo(index));
            }

            Assert.That(NpcAppearanceRandomizer.SelectIndex(-1f, 24), Is.Zero);
            Assert.That(NpcAppearanceRandomizer.SelectIndex(1f, 24), Is.EqualTo(23));
            Assert.That(NpcAppearanceRandomizer.SelectIndex(0.5f, 0), Is.EqualTo(-1));
        }

        [TestCase("shirt_lod0", 0)]
        [TestCase("shirt_LOD4", 4)]
        [TestCase("hair_lod12", 12)]
        [TestCase("shirt", -1)]
        [TestCase("shirt_lod", -1)]
        public void LodIndexParsingHandlesPackNames(string objectName, int expected)
        {
            Assert.That(NpcAppearanceRandomizer.ParseLodIndex(objectName), Is.EqualTo(expected));
        }

        [Test]
        public void MissingConfigurationFailsWithoutCreatingAnAppearance()
        {
            GameObject root = new GameObject("NPC");
            NpcAppearanceRandomizer randomizer = root.AddComponent<NpcAppearanceRandomizer>();
            LogAssert.Expect(LogType.Error, "NpcAppearanceRandomizer cần Appearance Root.");

            Assert.That(randomizer.RandomizeAppearance(), Is.False);
            Assert.That(randomizer.CurrentModelPrefab, Is.Null);
            Assert.That(randomizer.CurrentHairObject, Is.Null);
            Object.DestroyImmediate(root);
        }

        [TestCase("Assets/Prefabs/Casual NPC.prefab", 6, 6, 8, 12, 0)]
        [TestCase("Assets/Prefabs/Special NPC.prefab", 24, 24, 0, 0, 1)]
        public void PrefabsContainCompleteRandomizationPools(
            string prefabPath,
            int femaleCount,
            int maleCount,
            int hairCount,
            int hairMaterialCount,
            int expectedHairMode)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.name, Is.EqualTo(System.IO.Path.GetFileNameWithoutExtension(prefabPath)));

            NpcAppearanceRandomizer randomizer = prefab.GetComponent<NpcAppearanceRandomizer>();
            Assert.That(randomizer, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(randomizer);
            Assert.That(serialized.FindProperty("appearanceRoot").objectReferenceValue, Is.Not.Null);
            Assert.That(serialized.FindProperty("femaleModelPrefabs").arraySize, Is.EqualTo(femaleCount));
            Assert.That(serialized.FindProperty("maleModelPrefabs").arraySize, Is.EqualTo(maleCount));
            Assert.That(serialized.FindProperty("hairstylePrefabs").arraySize, Is.EqualTo(hairCount));
            Assert.That(serialized.FindProperty("hairMaterials").arraySize, Is.EqualTo(hairMaterialCount));
            Assert.That(serialized.FindProperty("hairMode").enumValueIndex, Is.EqualTo(expectedHairMode));
            Assert.That(serialized.FindProperty("randomizeOnAwake").boolValue, Is.True);
        }
    }
}
