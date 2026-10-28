using System;
using System.Linq;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerDefenseSceneTests
    {
        private const string OfficePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string CourtroomPath =
            "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";

        [Test]
        public void OfficeContainsTimedDefenseInstructionAndStartButton()
        {
            WithScene(OfficePath, scene =>
            {
                LawyerBoardController controller = FindComponent<LawyerBoardController>(scene);
                Assert.That(controller, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("truthDisplayDuration").floatValue,
                    Is.EqualTo(5f).Within(0.001f));
                TMP_Text instruction = serialized.FindProperty("defenseInstructionLabel")
                    .objectReferenceValue as TMP_Text;
                Button button = serialized.FindProperty("startDefenseButton")
                    .objectReferenceValue as Button;
                Assert.That(instruction, Is.Not.Null);
                Assert.That(instruction.text, Does.Contain("trình bày lời bào chữa"));
                Assert.That(button, Is.Not.Null);
                Assert.That(button.GetComponentInChildren<TMP_Text>(true).text,
                    Is.EqualTo("Bắt đầu bào chữa"));
                Assert.That(button.GetComponentInParent<Canvas>()
                    .GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null);
            });
        }

        [Test]
        public void CourtroomContainsDefenseModeHudAndRightHandStopAction()
        {
            WithScene(CourtroomPath, scene =>
            {
                LawyerDefenseController controller = FindComponent<LawyerDefenseController>(scene);
                Assert.That(controller, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("interviewHud").objectReferenceValue,
                    Is.Not.Null);
                GameObject defenseHud = serialized.FindProperty("defenseHud")
                    .objectReferenceValue as GameObject;
                Assert.That(defenseHud, Is.Not.Null);
                Assert.That(defenseHud.activeSelf, Is.False);
                Assert.That(serialized.FindProperty("preparationDuration").floatValue,
                    Is.EqualTo(5f).Within(0.001f));
                Assert.That(serialized.FindProperty("recordingDuration").floatValue,
                    Is.EqualTo(60f).Within(0.001f));
                Assert.That(serialized.FindProperty("uploadAttempts").intValue,
                    Is.EqualTo(5));

                InputActionAsset controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/Input/Lawyer/Lawyer Controls.inputactions");
                InputAction stop = controls.FindAction("Lawyer/StopDefense", true);
                Assert.That(stop.bindings.Any(binding =>
                    binding.path == "<XRController>{RightHand}/primaryButton"), Is.True);
            });
        }

        private static void WithScene(string path, Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                assertion(scene);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
