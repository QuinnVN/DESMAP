using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NCKH.VR.GameControl;
using NUnit.Framework;
using TMPro;
using Unity.XR.Oculus.Input;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NCKH.VR.GameControl.Tests
{
    public sealed class BackendConfigurationPlayModeTests
    {
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";

        private bool hadSavedUrl;
        private string savedUrl;
        private OculusTouchController leftController;
        private OculusTouchController rightController;

        [SetUp]
        public void Setup()
        {
            hadSavedUrl = PlayerPrefs.HasKey(GameController.BackendBaseUrlPlayerPrefsKey);
            savedUrl = PlayerPrefs.GetString(GameController.BackendBaseUrlPlayerPrefsKey, string.Empty);
            PlayerPrefs.DeleteKey(GameController.BackendBaseUrlPlayerPrefsKey);
        }

        [TearDown]
        public void TearDown()
        {
            Scene standby = SceneManager.GetSceneByPath(StandbyScenePath);
            if (standby.IsValid() && standby.isLoaded)
            {
                GameObject[] roots = standby.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                    roots[i].SetActive(false);
            }
            if (GameController.Instance != null)
                GameController.Instance.gameObject.SetActive(false);
            if (leftController != null && leftController.added)
                InputSystem.RemoveDevice(leftController);
            if (rightController != null && rightController.added)
                InputSystem.RemoveDevice(rightController);
            leftController = null;
            rightController = null;

            if (hadSavedUrl)
                PlayerPrefs.SetString(GameController.BackendBaseUrlPlayerPrefsKey, savedUrl);
            else
                PlayerPrefs.DeleteKey(GameController.BackendBaseUrlPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator FourButtonHoldOpensWindowOnceAndBuildsFullKeyboard()
        {
            yield return LoadStandby();
            BackendServerConfigurationView view =
                Object.FindAnyObjectByType<BackendServerConfigurationView>();
            Assert.That(view, Is.Not.Null);

            leftController = InputSystem.AddDevice<OculusTouchController>();
            rightController = InputSystem.AddDevice<OculusTouchController>();
            InputSystem.SetDeviceUsage(leftController, CommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);

            SetFaceButtons(leftController, true, true);
            SetFaceButtons(rightController, true, true);
            yield return null;
            Assert.That(leftController.primaryButton.isPressed, Is.True, "X control was not pressed.");
            Assert.That(leftController.secondaryButton.isPressed, Is.True, "Y control was not pressed.");
            Assert.That(rightController.primaryButton.isPressed, Is.True, "A control was not pressed.");
            Assert.That(rightController.secondaryButton.isPressed, Is.True, "B control was not pressed.");
            Assert.That(GetAction(view, "leftPrimaryAction").IsPressed(), Is.True, "X action did not resolve.");
            Assert.That(GetAction(view, "leftSecondaryAction").IsPressed(), Is.True, "Y action did not resolve.");
            Assert.That(GetAction(view, "rightPrimaryAction").IsPressed(), Is.True, "A action did not resolve.");
            Assert.That(GetAction(view, "rightSecondaryAction").IsPressed(), Is.True, "B action did not resolve.");
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(view.IsOpen, Is.False);
            yield return new WaitForSecondsRealtime(0.35f);
            yield return null;
            Assert.That(view.IsOpen, Is.True);

            VrKeyboard keyboard = view.GetComponentInChildren<VrKeyboard>(true);
            Assert.That(keyboard, Is.Not.Null);
            Assert.That(keyboard.GeneratedKeyCount, Is.EqualTo(VrKeyboard.ExpectedKeyCount));

            SetFaceButtons(leftController, false, false);
            SetFaceButtons(rightController, false, false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KeyboardEditsTextAndSaveValidatesThenCloses()
        {
            yield return LoadStandby();
            BackendServerConfigurationView view =
                Object.FindAnyObjectByType<BackendServerConfigurationView>();
            view.OpenPanel();
            yield return null;

            TMP_InputField input = view.GetComponentInChildren<TMP_InputField>(true);
            VrKeyboard keyboard = view.GetComponentInChildren<VrKeyboard>(true);
            VrKeyboardKeyButton[] keys = view.GetComponentsInChildren<VrKeyboardKeyButton>(true);
            Assert.That(input, Is.Not.Null);
            Assert.That(keys, Has.Length.EqualTo(VrKeyboard.ExpectedKeyCount));

            input.SetTextWithoutNotify(string.Empty);
            input.selectionStringAnchorPosition = 0;
            input.selectionStringFocusPosition = 0;
            VrKeyboardKeyButton letterA = keys.First(key => key.gameObject.name == "Phím a");
            keyboard.HandleKey(letterA);
            Assert.That(input.text, Is.EqualTo("a"));

            input.SetTextWithoutNotify("invalid");
            view.Save();
            Assert.That(view.IsOpen, Is.True);
            TMP_Text validation = view.GetComponentsInChildren<TMP_Text>(true)
                .First(text => text.gameObject.name == "Lỗi cấu hình");
            Assert.That(validation.text, Is.Not.Empty);

            input.SetTextWithoutNotify("http://127.0.0.1:8000");
            view.Save();
            Assert.That(view.IsOpen, Is.False);
            Assert.That(PlayerPrefs.GetString(GameController.BackendBaseUrlPlayerPrefsKey),
                Is.EqualTo("http://127.0.0.1:8000"));
        }

        [UnityTest]
        public IEnumerator PersistedUrlOverridesSerializedStandbyDefault()
        {
            const string persisted = "https://backend.example.com/vr";
            PlayerPrefs.SetString(GameController.BackendBaseUrlPlayerPrefsKey, persisted);
            PlayerPrefs.Save();
            LogAssert.Expect(LogType.Error, new Regex("Network error: .*"));

            yield return LoadStandby();

            Assert.That(GameController.Instance, Is.Not.Null);
            Assert.That(GameController.Instance.BackendBaseUrl, Is.EqualTo(persisted));
        }

        private static IEnumerator LoadStandby()
        {
            if (GameController.Instance != null)
                Object.Destroy(GameController.Instance.gameObject);
            yield return null;

            AsyncOperation load = SceneManager.LoadSceneAsync(StandbyScenePath, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null);
            while (!load.isDone)
                yield return null;
            yield return null;
        }

        private static InputAction GetAction(BackendServerConfigurationView view, string fieldName)
        {
            FieldInfo field = typeof(BackendServerConfigurationView).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            InputAction action = field.GetValue(view) as InputAction;
            Assert.That(action, Is.Not.Null);
            return action;
        }

        private static void SetFaceButtons(
            OculusTouchController controller,
            bool primaryPressed,
            bool secondaryPressed)
        {
            using (StateEvent.From(controller, out InputEventPtr eventPointer))
            {
                controller.primaryButton.WriteValueIntoEvent(primaryPressed ? 1f : 0f, eventPointer);
                controller.secondaryButton.WriteValueIntoEvent(secondaryPressed ? 1f : 0f, eventPointer);
                InputSystem.QueueEvent(eventPointer);
            }
            InputSystem.Update();
        }
    }
}
