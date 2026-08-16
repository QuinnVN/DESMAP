using System.Collections.Generic;
using NCKH.VR.Doctor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
