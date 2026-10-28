using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NCKH.VR.Clinic.Tests
{
    public sealed class PatientAppearanceTests
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 1.unity";

        [TestCase(0f, PatientGender.Female)]
        [TestCase(0.4999f, PatientGender.Female)]
        [TestCase(0.5f, PatientGender.Male)]
        [TestCase(1f, PatientGender.Male)]
        public void GenderSelectionUsesAnEvenSplit(float sample, PatientGender expected)
        {
            Assert.That(PatientAppearanceController.SelectGender(sample), Is.EqualTo(expected));
        }

        [Test]
        public void IndexSelectionCanReachEveryHairstyle()
        {
            HashSet<int> selected = new HashSet<int>();
            for (int i = 0; i < 8; i++)
                selected.Add(PatientAppearanceController.SelectIndex((i + 0.5f) / 8f, 8));

            Assert.That(selected, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(PatientAppearanceController.SelectIndex(1f, 8), Is.EqualTo(7));
            Assert.That(PatientAppearanceController.SelectIndex(0.5f, 0), Is.EqualTo(-1));
        }

        [Test]
        public void FacialHairIsMaleOnlyAndUsesConfiguredChance()
        {
            Assert.That(PatientAppearanceController.ShouldUseFacialHair(PatientGender.Female, 0f, 1f), Is.False);
            Assert.That(PatientAppearanceController.ShouldUseFacialHair(PatientGender.Male, 0.49f, 0.5f), Is.True);
            Assert.That(PatientAppearanceController.ShouldUseFacialHair(PatientGender.Male, 0.5f, 0.5f), Is.False);
        }

        [Test]
        public void MissingConfigurationFailsWithoutCreatingAPartialAppearance()
        {
            GameObject root = new GameObject("Patient");
            PatientAppearanceController controller = root.AddComponent<PatientAppearanceController>();
            LogAssert.Expect(LogType.Error, "PatientAppearanceController cần Appearance Root.");

            Assert.That(controller.RandomizeAppearance(false), Is.False);
            Assert.That(controller.ActiveRenderers, Is.Empty);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void GameOnePatientShellContainsCompleteHumanoidWiring()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject npcRoot = FindRoot(scene, "NPCs");
                Assert.That(npcRoot, Is.Not.Null);
                Transform patientTransform = npcRoot.transform.Find("npc");
                Assert.That(patientTransform, Is.Not.Null);
                Assert.That(patientTransform.GetComponent<MeshRenderer>(), Is.Null);
                Assert.That(patientTransform.GetComponent<MeshFilter>(), Is.Null);

                PatientActor actor = patientTransform.GetComponent<PatientActor>();
                PatientAppearanceController appearance = patientTransform.GetComponent<PatientAppearanceController>();
                Assert.That(actor, Is.Not.Null);
                Assert.That(appearance, Is.Not.Null);
                Assert.That(FindRoot(scene, "npc_csl_00_character_01f_02"), Is.Null,
                    "The standalone alignment prototype should not remain in Game 1.");

                SerializedObject serializedAppearance = new SerializedObject(appearance);
                Assert.That(serializedAppearance.FindProperty("appearanceRoot").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedAppearance.FindProperty("femaleCharacterPrefabs").arraySize, Is.EqualTo(6));
                Assert.That(serializedAppearance.FindProperty("maleCharacterPrefabs").arraySize, Is.EqualTo(6));
                Assert.That(serializedAppearance.FindProperty("hairstylePrefabs").arraySize, Is.EqualTo(8));
                Assert.That(serializedAppearance.FindProperty("facialHairPrefabs").arraySize, Is.EqualTo(4));
                Assert.That(serializedAppearance.FindProperty("maleFacialHairChance").floatValue, Is.EqualTo(0.5f));
                Assert.That(
                    AssetDatabase.GetAssetPath(serializedAppearance.FindProperty("lyingAnimatorController").objectReferenceValue),
                    Is.EqualTo("Assets/Animations/Clinic/Patient Lying.controller"));

                AnimatorController animatorController = (AnimatorController)serializedAppearance
                    .FindProperty("lyingAnimatorController").objectReferenceValue;
                Assert.That(animatorController.parameters, Has.Length.EqualTo(1));
                Assert.That(animatorController.parameters[0].name, Is.EqualTo("Emergency"));
                Assert.That(animatorController.layers, Has.Length.EqualTo(1));
                Assert.That(animatorController.layers[0].iKPass, Is.False);
                ChildAnimatorState[] states = animatorController.layers[0].stateMachine.states;
                Assert.That(states, Has.Length.EqualTo(1));
                Assert.That(states[0].state.name, Is.EqualTo("Frozen Supine"));
                Assert.That(states[0].state.transitions, Is.Empty);
                Assert.That(AssetDatabase.GetAssetPath(states[0].state.motion),
                    Is.EqualTo("Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx"));

                SerializedObject serializedActor = new SerializedObject(actor);
                Assert.That(serializedActor.FindProperty("appearanceController").objectReferenceValue, Is.SameAs(appearance));
                Assert.That(serializedActor.FindProperty("fadeRenderers").arraySize, Is.Zero);
                Assert.That(serializedActor.FindProperty("enableEmergencyAlert").boolValue, Is.True);
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
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
    }
}
