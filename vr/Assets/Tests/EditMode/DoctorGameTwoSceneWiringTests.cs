using System.Collections.Generic;
using NCKH.VR.Doctor;
using NCKH.VR.NPC;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace NCKH.VR.Doctor.Tests
{
    public sealed class DoctorGameTwoSceneWiringTests
    {
        private const string ScenePath = "Assets/Scenes/Doctor/Game 2.unity";

        [Test]
        public void MonitorOwnsThePatientNotesInteraction()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                Assert.That(FindRoot(scene, "Notebook"), Is.Null,
                    "The obsolete placeholder notebook should not remain in Game 2.");

                GameObject monitor = FindRoot(scene, "Monitor Variant");
                Assert.That(monitor, Is.Not.Null);

                DoctorNotebookInteractable[] interactables =
                    FindComponentsInScene<DoctorNotebookInteractable>(scene);
                Assert.That(interactables, Has.Length.EqualTo(1));
                DoctorNotebookInteractable interactable = interactables[0];
                Assert.That(interactable.gameObject, Is.EqualTo(monitor));

                Transform rayTarget = monitor.transform.Find("Monitor Ray Button");
                Assert.That(rayTarget, Is.Not.Null);
                Assert.That(rayTarget.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("DoctorHUD")));

                Canvas canvas = rayTarget.GetComponent<Canvas>();
                Button button = rayTarget.GetComponent<Button>();
                Image image = rayTarget.GetComponent<Image>();
                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
                Assert.That(canvas.worldCamera, Is.Not.Null);
                Assert.That(button, Is.Not.Null);
                Assert.That(image, Is.Not.Null);
                Assert.That(image.raycastTarget, Is.True);
                Assert.That(HasComponentNamed(rayTarget.gameObject, "TrackedDeviceGraphicRaycaster"), Is.True);

                SerializedObject serializedInteractable = new SerializedObject(interactable);
                Assert.That(serializedInteractable.FindProperty("activationButton").objectReferenceValue,
                    Is.EqualTo(button));

                GameObject managerObject = FindRoot(scene, "Doctor Game Manager");
                Assert.That(managerObject, Is.Not.Null);
                DoctorGameManager manager = managerObject.GetComponent<DoctorGameManager>();
                Assert.That(manager, Is.Not.Null);
                SerializedObject serializedManager = new SerializedObject(manager);
                Assert.That(serializedManager.FindProperty("notebookInteractable").objectReferenceValue,
                    Is.EqualTo(interactable));
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PatientUsesCasualNpcPrefabAndPreservesPresenterWiring()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                GameObject npc = FindRoot(scene, "NPC");
                Assert.That(npc, Is.Not.Null);
                Assert.That(npc.transform.Find("Capsule"), Is.Null,
                    "The placeholder capsule should not remain in Game 2.");
                Assert.That(npc.transform.Find("Canvas"), Is.Not.Null,
                    "The existing patient dialogue canvas should be preserved.");

                DoctorPatientPresenter presenter = npc.GetComponent<DoctorPatientPresenter>();
                AudioSource voiceSource = npc.GetComponent<AudioSource>();
                Assert.That(presenter, Is.Not.Null);
                Assert.That(voiceSource, Is.Not.Null);

                SerializedObject serializedPresenter = new SerializedObject(presenter);
                GameObject modelRoot = serializedPresenter.FindProperty("modelRoot").objectReferenceValue
                    as GameObject;
                Assert.That(modelRoot, Is.Not.Null);
                Assert.That(modelRoot.transform.parent, Is.EqualTo(npc.transform));
                Assert.That(modelRoot.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(Quaternion.Angle(modelRoot.transform.localRotation,
                    Quaternion.Euler(0f, 180f, 0f)), Is.LessThan(0.01f));
                Assert.That(modelRoot.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(modelRoot),
                    Is.EqualTo("Assets/Prefabs/Casual NPC.prefab"));
                Assert.That(modelRoot.GetComponent<NpcAppearanceRandomizer>(), Is.Not.Null);
                SerializedObject serializedAppearance = new SerializedObject(
                    modelRoot.GetComponent<NpcAppearanceRandomizer>());
                Assert.That(serializedAppearance.FindProperty("externalHairSurfaceOffset").floatValue,
                    Is.EqualTo(0.003f).Within(0.0001f));
                SerializedProperty maleHairstyles = serializedAppearance.FindProperty(
                    "maleHairstylePrefabs");
                Assert.That(maleHairstyles.arraySize, Is.EqualTo(2));
                Assert.That(maleHairstyles.GetArrayElementAtIndex(0).objectReferenceValue.name,
                    Is.EqualTo("npc_haircut_d_01"));
                Assert.That(maleHairstyles.GetArrayElementAtIndex(1).objectReferenceValue.name,
                    Is.EqualTo("npc_haircut_e_01"));
                NpcMotionPlayer motionPlayer = modelRoot.GetComponent<NpcMotionPlayer>();
                Assert.That(motionPlayer, Is.Not.Null);
                Assert.That(motionPlayer.Mode, Is.EqualTo(NpcMotionMode.StaticHumanoidPose));
                Assert.That(motionPlayer.StaticPose, Is.Not.Null);
                Assert.That(motionPlayer.StaticPose.IsComplete, Is.True);
                Assert.That(motionPlayer.TrackHead, Is.True);
                Assert.That(motionPlayer.LookTarget, Is.Not.Null);
                Assert.That(motionPlayer.LookTarget.CompareTag("MainCamera"), Is.True);
                Assert.That(serializedPresenter.FindProperty("voiceSource").objectReferenceValue,
                    Is.EqualTo(voiceSource));

                CapsuleCollider collider = modelRoot.GetComponent<CapsuleCollider>();
                Assert.That(collider, Is.Not.Null);
                Assert.That(collider.isTrigger, Is.False);
                Assert.That(collider.center.x, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(collider.center.y, Is.EqualTo(0.62f).Within(0.0001f));
                Assert.That(collider.center.z, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(collider.radius, Is.EqualTo(0.36f).Within(0.0001f));
                Assert.That(collider.height, Is.EqualTo(1.2f).Within(0.0001f));
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void RightRayIsAlwaysVisibleAndUsesLeftStickScrolling()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                XRRayInteractor ray = FindComponentsInScene<XRRayInteractor>(scene)[0];
                SerializedObject serializedRay = new SerializedObject(ray);
                InputActionReference scrollAction = serializedRay
                    .FindProperty("m_UIScrollInput.m_InputActionReference")
                    .objectReferenceValue as InputActionReference;
                Assert.That(scrollAction, Is.Not.Null);
                Assert.That(scrollAction.action.actionMap.name, Is.EqualTo("XRI Left Interaction"));
                Assert.That(scrollAction.action.name, Is.EqualTo("UI Scroll"));

                XRInteractorLineVisual lineVisual = ray.GetComponent<XRInteractorLineVisual>();
                Assert.That(lineVisual, Is.Not.Null);
                Assert.That(lineVisual.enabled, Is.True);
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool HasComponentNamed(GameObject gameObject, string typeName)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
                if (components[i] != null && components[i].GetType().Name == typeName)
                    return true;
            return false;
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
