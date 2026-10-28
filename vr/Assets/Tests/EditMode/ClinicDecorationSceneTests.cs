using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class ClinicDecorationSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 1.unity";

        [Test]
        public void GameOneContainsDynamicDecorationWiring()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject managerObject = FindRoot(scene, "Clinic Game Manager");
                GameObject decoRoot = FindRoot(scene, "Deco");
                Assert.That(managerObject, Is.Not.Null);
                Assert.That(decoRoot, Is.Not.Null);
                Assert.That(decoRoot.transform.childCount, Is.Zero,
                    "The Deco root should not contain permanent bedside decorations in Edit Mode.");

                ClinicGameManager manager = managerObject.GetComponent<ClinicGameManager>();
                Assert.That(manager, Is.Not.Null);
                SerializedObject serializedManager = new SerializedObject(manager);
                AssertAssetPath(serializedManager, "glucoseStandPrefab", "Assets/Prefabs/GlocoseStand Variant.prefab");
                AssertAssetPath(serializedManager, "ekgMachinePrefab", "Assets/Prefabs/ECGMachine.prefab");
                Assert.That(serializedManager.FindProperty("decorationRoot").objectReferenceValue,
                    Is.EqualTo(decoRoot.transform));
                Assert.That(serializedManager.FindProperty("glucoseStandScale").floatValue, Is.EqualTo(1.41f));
                Assert.That(serializedManager.FindProperty("ekgMachineScale").floatValue, Is.EqualTo(1.15f));

                PatientBedSlot[] beds = FindComponentsInScene<PatientBedSlot>(scene);
                Assert.That(beds, Has.Length.EqualTo(8));
                HashSet<Transform> anchors = new HashSet<Transform>();
                for (int i = 0; i < beds.Length; i++)
                {
                    Transform anchor = beds[i].DecorationAnchor;
                    Assert.That(anchor, Is.Not.Null, $"{beds[i].name} is missing its decoration anchor.");
                    Assert.That(anchor, Is.Not.EqualTo(beds[i].transform),
                        $"{beds[i].name} is falling back to the bed transform instead of a configured anchor.");
                    Assert.That(anchor.name, Is.EqualTo("Decoration Anchor"));
                    Assert.That(anchors.Add(anchor), Is.True, "Each bed must use a unique decoration anchor.");
                    Assert.That(Quaternion.Angle(anchor.rotation, beds[i].transform.rotation), Is.LessThan(0.001f));
                }
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertAssetPath(SerializedObject target, string propertyName, string expectedPath)
        {
            Object value = target.FindProperty(propertyName).objectReferenceValue;
            Assert.That(value, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(value), Is.EqualTo(expectedPath));
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].name == name)
                    return roots[i];
            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene) where T : Component
        {
            List<T> results = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return results.ToArray();
        }
    }
}
