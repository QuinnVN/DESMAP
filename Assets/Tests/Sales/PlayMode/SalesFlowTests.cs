using System.Collections;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace NCKH.VR.Sales.Tests
{
    public sealed class SalesFlowTests
    {
        private GameObject root;
        private SalesMissionData data;
        private SalesMissionController mission;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Sales test fixture");
            root.SetActive(false);
            mission = root.AddComponent<SalesMissionController>();
            mission.playerCamera = Child("Camera").AddComponent<Camera>();
            mission.customer = Child("Customer").transform;
            mission.customer.position = Vector3.forward * 100;
            mission.voice = Child("Voice").AddComponent<AudioSource>();
            mission.dialogueRoot = Child("Dialogue").transform;
            mission.displayRoot = Child("Display").transform;
            mission.choicesRoot = Child("Choices");
            mission.resultRoot = Child("Result");
            mission.dialogueText = Label(); mission.speakerText = Label(); mission.nextText = Label();
            mission.objectiveText = Label(); mission.resultText = Label();
            mission.focus = root.AddComponent<LawyerBoardFocusLighting>();
            data = ScriptableObject.CreateInstance<SalesMissionData>();
            data.dialogue = new[] {new SalesDialogueLine {text = "Mình cần giày chạy bộ."}};
            data.shoes = new[] {Shoe("1A"), Shoe("3B"), Shoe("8A")};
            mission.data = data;
            root.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root); Object.Destroy(data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ProximityDialogueTransitionAndSingleSelection()
        {
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Waiting));
            mission.HandleAction(0);
            Assert.That(mission.SelectedIndex, Is.EqualTo(-1));
            mission.customer.position = Vector3.forward;
            yield return null;
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Dialogue));
            Assert.That(mission.dialogueText.text, Is.EqualTo(data.dialogue[0].text));
            Assert.That(mission.voice.isPlaying, Is.False);
            mission.HandleAction(-1);
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Transition));
            mission.HandleAction(1);
            Assert.That(mission.SelectedIndex, Is.EqualTo(-1));
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Choosing));
            Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.True);
            mission.HandleAction(1); mission.HandleAction(2);
            Assert.That(mission.SelectedIndex, Is.EqualTo(1));
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Completed));
            Assert.That(mission.resultText.text, Does.Contain("3B"));
            mission.enabled = false;
            Assert.That(LawyerBoardFocusLighting.CurrentOverlayActive, Is.False);
            Assert.That(LawyerBoardFocusLighting.CurrentScreenDimAmount, Is.Zero);
            mission.enabled = true;
            Assert.That(mission.focus.enabled, Is.True);
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Waiting));
        }

        [UnityTest]
        public IEnumerator OptionalAudioClipReplaysAndStopsOnAdvance()
        {
            var clip = AudioClip.Create("Test dialogue", 44100, 1, 44100, false);
            data.dialogue[0].audioClip = clip;
            mission.BeginDialogue();
            Assert.That(mission.voice.clip, Is.SameAs(clip));
            mission.ReplayAudio();
            Assert.That(mission.voice.clip, Is.SameAs(clip));
            mission.HandleAction(-1);
            Assert.That(mission.voice.isPlaying, Is.False);
            yield return null;
            Object.Destroy(clip);
        }

        [UnityTest]
        public IEnumerator RightControllerAAdvancesExactlyOneDialogueLine()
        {
            XRSimulatedController rightController = null;
            try
            {
                data.dialogue = new[]
                {
                    new SalesDialogueLine {text = "Dòng một"},
                    new SalesDialogueLine {text = "Dòng hai"},
                    new SalesDialogueLine {text = "Dòng ba"}
                };
                mission.ResetMission();
                mission.BeginDialogue();
                rightController = InputSystem.AddDevice<XRSimulatedController>();
                InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);

                using (StateEvent.From(rightController, out InputEventPtr eventPointer))
                {
                    rightController.primaryButton.WriteValueIntoEvent(1f, eventPointer);
                    InputSystem.QueueEvent(eventPointer);
                }
                InputSystem.Update();
                yield return null;

                Assert.That(mission.DialogueIndex, Is.EqualTo(1));
                Assert.That(mission.dialogueText.text, Is.EqualTo("Dòng hai"));
                Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Dialogue));
            }
            finally
            {
                if (rightController != null && rightController.added)
                    InputSystem.RemoveDevice(rightController);
            }
        }

        [UnityTest]
        public IEnumerator ShoeModelRotatesWithoutDesktopInput()
        {
            var go = Child("Shoe target");
            var target = go.AddComponent<SalesActionTarget>();
            target.idleSpeed = 20;
            target.spinningModel = Child("Visual").transform;
            Assert.That(go.GetComponent<XRSimpleInteractable>().selectMode,
                Is.EqualTo(InteractableSelectMode.Single));
            Assert.That(target.Hovered, Is.False);
            Assert.That(target.CurrentSpeed, Is.EqualTo(20));
            Quaternion start = target.spinningModel.localRotation;
            yield return null;
            Assert.That(target.spinningModel.localRotation, Is.Not.EqualTo(start));
            target.enabled = false;
            Assert.That(target.Hovered, Is.False);
            Assert.That(target.CurrentSpeed, Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator ControllerSelectInvokesOnceWhenActivateAlsoFires()
        {
            data.dialogue = new[]
            {
                new SalesDialogueLine {text = "Dòng một"},
                new SalesDialogueLine {text = "Dòng hai"}
            };
            mission.ResetMission();
            mission.BeginDialogue();
            var go = Child("Dialogue button");
            var target = go.AddComponent<SalesActionTarget>();
            target.mission = mission;
            target.action = -1;
            yield return null;

            XRSimpleInteractable interactable = go.GetComponent<XRSimpleInteractable>();
            interactable.selectEntered.Invoke(new SelectEnterEventArgs());
            interactable.activated.Invoke(new ActivateEventArgs());

            Assert.That(mission.DialogueIndex, Is.EqualTo(1));
            Assert.That(mission.Stage, Is.EqualTo(SalesMissionStage.Dialogue));
        }

        private GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform); return go; }
        private TMP_Text Label() => Child("Label").AddComponent<TextMeshPro>();
        private SalesShoeData Shoe(string label) => new SalesShoeData {label = label, prefab = root, feedback = "Phản hồi"};
    }
}
