using System;
using System.Linq;
using NCKH.VR.Clinic;
using NCKH.VR.Doctor;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace NCKH.VR.GameControl.Tests
{
    public sealed class GameControlArchitectureTests
    {
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";
        private const string ClinicScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string DoctorScenePath = "Assets/Scenes/Doctor/Game 2.unity";
        private const string LawyerOfficeScenePath = "Assets/Scenes/Lawyer/Office.unity";
        private const string LawyerCourtroomScenePath = "Assets/Scenes/Lawyer/Courtroom_Interior URP.unity";

        [Test]
        public void CombineUrlNormalizesOneSeparator()
        {
            Assert.That(GameControlApi.CombineUrl("http://server:8080/", "/api/test"),
                Is.EqualTo("http://server:8080/api/test"));
            Assert.That(GameControlApi.CombineUrl("", "/api/test"), Is.Empty);
        }

        [Test]
        public void CombineWebSocketUrlConvertsHttpSchemes()
        {
            Assert.That(GameControlApi.CombineWebSocketUrl("http://server:8080/", "/api/ws/ctrl"),
                Is.EqualTo("ws://server:8080/api/ws/ctrl"));
            Assert.That(GameControlApi.CombineWebSocketUrl("https://server/vr", "api/ws/ctrl"),
                Is.EqualTo("wss://server/vr/api/ws/ctrl"));
            Assert.That(GameControlApi.CombineWebSocketUrl("ftp://server", "/api/ws/ctrl"), Is.Empty);
        }

        [Test]
        public void ApiRoutesMatchBackendArchitecture()
        {
            Assert.That(GameControlApi.HealthRoute, Is.EqualTo("/api/health"));
            Assert.That(GameControlApi.TelemetryRoute, Is.EqualTo("/api/telemetry"));
            Assert.That(GameControlApi.ResultFragmentRoute, Is.EqualTo("/api/results/fragments"));
            Assert.That(GameControlApi.AssignParticipantCommand, Is.EqualTo("assign_participant"));
            Assert.That(GameControlApi.ClearParticipantCommand, Is.EqualTo("clear_participant"));
            Assert.That(GameControlApi.RunStartedFragmentKind, Is.EqualTo("run.started"));
            Assert.That(GameControlApi.RunAbortedFragmentKind, Is.EqualTo("run.aborted"));
            Assert.That(GameControlApi.ControlWebSocketRoute, Is.EqualTo("/ws/ctrl"));
            Assert.That(GameControlApi.TtsWebSocketRoute, Is.EqualTo("/ws/tts"));
            Assert.That(GameControlApi.SttWebSocketRoute, Is.EqualTo("/ws/stt"));
        }

        [TestCase("http://127.0.0.1:8000")]
        [TestCase("https://server.example.com/vr")]
        public void BackendUrlValidationAcceptsAbsoluteHttpUrls(string value)
        {
            Assert.That(GameController.TryValidateBackendBaseUrl(value, out string error), Is.True);
            Assert.That(error, Is.Empty);
        }

        [TestCase("")]
        [TestCase("server.local:8000")]
        [TestCase("ftp://server.local")]
        [TestCase("not a url")]
        public void BackendUrlValidationRejectsUnsupportedValues(string value)
        {
            Assert.That(GameController.TryValidateBackendBaseUrl(value, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void CommandDtoRoundTripsThroughJsonUtility()
        {
            RemoteCommandDto command = new RemoteCommandDto
            {
                commandId = "command-1",
                sequence = 7,
                type = GameControlApi.LoadSceneCommand,
                sceneId = "clinic",
                issuedAtUtc = "2026-08-17T12:00:00Z"
            };

            RemoteCommandDto restored = JsonUtility.FromJson<RemoteCommandDto>(JsonUtility.ToJson(command));

            Assert.That(restored.commandId, Is.EqualTo(command.commandId));
            Assert.That(restored.sequence, Is.EqualTo(command.sequence));
            Assert.That(restored.type, Is.EqualTo(command.type));
            Assert.That(restored.sceneId, Is.EqualTo(command.sceneId));
        }

        [Test]
        public void ParticipantCommandAndResultFragmentRoundTripIdentity()
        {
            RemoteCommandDto command = new RemoteCommandDto
            {
                commandId = "participant-1",
                type = GameControlApi.AssignParticipantCommand,
                participantName = "  Nguyễn   An  ",
                participantSessionId = "session-1"
            };
            RemoteCommandDto restored = JsonUtility.FromJson<RemoteCommandDto>(JsonUtility.ToJson(command));
            Assert.That(restored.type, Is.EqualTo(GameControlApi.AssignParticipantCommand));
            Assert.That(restored.participantName, Is.EqualTo(command.participantName));
            Assert.That(restored.participantSessionId, Is.EqualTo(command.participantSessionId));

            ResultFragmentDto fragment = new ResultFragmentDto
            {
                kind = "clinic.patient_resolved",
                fragmentId = "run:patient:1",
                runId = "run-1",
                gameId = "clinic",
                participantName = "Nguyễn An",
                participantSessionId = "session-1",
                occurredAtUtc = "2026-09-14T12:01:00Z",
                startedAtUtc = "2026-09-14T12:00:00Z",
                complete = false,
                requiredFields = Array.Empty<string>(),
                data = new GameplayTelemetryData
                {
                    participantName = "Nguyễn An",
                    participantSessionId = "session-1",
                    resolutionDurationSeconds = 2.5f
                }
            };
            ResultFragmentDto fragmentCopy = JsonUtility.FromJson<ResultFragmentDto>(JsonUtility.ToJson(fragment));
            Assert.That(fragmentCopy.fragmentId, Is.EqualTo(fragment.fragmentId));
            Assert.That(fragmentCopy.kind, Is.EqualTo("clinic.patient_resolved"));
            Assert.That(fragmentCopy.participantName, Is.EqualTo("Nguyễn An"));
            Assert.That(fragmentCopy.participantSessionId, Is.EqualTo("session-1"));
            Assert.That(fragmentCopy.occurredAtUtc, Is.EqualTo("2026-09-14T12:01:00Z"));
            Assert.That(fragmentCopy.data.participantSessionId, Is.EqualTo("session-1"));
            Assert.That(fragmentCopy.data.resolutionDurationSeconds, Is.EqualTo(2.5f));
        }

        [Test]
        public void LifecycleEnvelopeUsesBackendKindsAndTerminalRequirements()
        {
            ResultFragmentDto started = new ResultFragmentDto
            {
                kind = GameControlApi.RunStartedFragmentKind, runId = "run-1", gameId = "clinic",
                startedAtUtc = "2026-09-14T12:00:00Z", complete = true,
                requiredFields = Array.Empty<string>(), data = new GameplayTelemetryData()
            };
            ResultFragmentDto restored = JsonUtility.FromJson<ResultFragmentDto>(JsonUtility.ToJson(started));
            Assert.That(restored.kind, Is.EqualTo(GameControlApi.RunStartedFragmentKind));
            Assert.That(restored.complete, Is.True);
            Assert.That(restored.gameId, Is.EqualTo("clinic"));
        }

        [Test]
        public void ResetCommandDtoRoundTripsAllTarget()
        {
            RemoteCommandDto command = new RemoteCommandDto
            {
                commandId = "reset-1",
                sequence = 8,
                type = GameControlApi.ResetSceneCommand,
                sceneId = GameController.ResetAllTarget,
                issuedAtUtc = "2026-09-10T00:00:00Z"
            };

            RemoteCommandDto restored = JsonUtility.FromJson<RemoteCommandDto>(JsonUtility.ToJson(command));

            Assert.That(GameControlApi.ResetSceneCommand, Is.EqualTo("reset_scene"));
            Assert.That(restored.type, Is.EqualTo(GameControlApi.ResetSceneCommand));
            Assert.That(restored.sceneId, Is.EqualTo(GameController.ResetAllTarget));
        }

        [Test]
        public void CommandAcknowledgementRoundTripsThroughJsonUtility()
        {
            RemoteCommandAckDto acknowledgement = new RemoteCommandAckDto
            {
                commandId = "command-1",
                sequence = 7,
                status = "applied",
                sceneId = "clinic",
                phase = "ready",
                appliedAtUtc = "2026-08-20T12:00:00Z",
                errorCode = string.Empty,
                errorMessage = string.Empty
            };

            RemoteCommandAckDto restored = JsonUtility.FromJson<RemoteCommandAckDto>(
                JsonUtility.ToJson(acknowledgement));

            Assert.That(restored.commandId, Is.EqualTo(acknowledgement.commandId));
            Assert.That(restored.sequence, Is.EqualTo(acknowledgement.sequence));
            Assert.That(restored.status, Is.EqualTo(acknowledgement.status));
            Assert.That(restored.sceneId, Is.EqualTo(acknowledgement.sceneId));
            Assert.That(restored.phase, Is.EqualTo(acknowledgement.phase));
        }

        [Test]
        public void StandbyIsFirstEnabledBuildScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes, Has.Length.GreaterThanOrEqualTo(3));
            Assert.That(scenes[0].enabled, Is.True);
            Assert.That(scenes[0].path, Is.EqualTo(StandbyScenePath));
            Assert.That(scenes.Where(scene => scene.enabled).Select(scene => scene.path),
                Does.Contain(ClinicScenePath).And.Contain(DoctorScenePath).And.Contain("Assets/Scenes/Sale/Store.unity"));
        }

        [Test]
        public void StandbyContainsControllerXrRigAndStatusView()
        {
            WithScene(StandbyScenePath, scene =>
            {
                GameObject player = FindRoot(scene, "VR Player");
                Assert.That(player, Is.Not.Null);
                GameObject controllerObject = FindRoot(scene, "Game Controller");
                Assert.That(controllerObject, Is.Not.Null);
                GameController controller = controllerObject.GetComponent<GameController>();
                Assert.That(controller, Is.Not.Null);
                StandbyStatusView statusView = FindComponent<StandbyStatusView>(scene);
                Assert.That(statusView, Is.Not.Null);
                BackendServerConfigurationView configurationView =
                    FindComponent<BackendServerConfigurationView>(scene);
                Assert.That(configurationView, Is.Not.Null);

                Assert.That(FindRoot(scene, "Standby Environment"), Is.Null);
                Assert.That(FindRoot(scene, "Directional Light"), Is.Null);
                Assert.That(FindComponent<Light>(scene), Is.Null);

                Camera mainCamera = player.GetComponentInChildren<Camera>(true);
                Assert.That(mainCamera, Is.Not.Null);
                Assert.That(mainCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
                Assert.That(mainCamera.backgroundColor, Is.EqualTo(Color.black));

                Transform leftHand = FindChild(player.transform, "Left hand");
                Transform rightHand = FindChild(player.transform, "Right hand");
                Assert.That(leftHand, Is.Not.Null);
                Assert.That(rightHand, Is.Not.Null);
                Assert.That(leftHand.gameObject.activeSelf, Is.False);
                Assert.That(rightHand.gameObject.activeSelf, Is.False);

                Canvas canvas = statusView.GetComponent<Canvas>();
                Assert.That(canvas, Is.Not.Null);
                Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.WorldSpace));
                Assert.That(canvas.transform.parent, Is.EqualTo(mainCamera.transform));
                Assert.That(canvas.transform.localPosition, Is.EqualTo(new Vector3(0f, 0f, 2f)));
                Assert.That(canvas.transform.localRotation, Is.EqualTo(Quaternion.identity));
                Assert.That(canvas.transform.localScale, Is.EqualTo(Vector3.one * 0.0015f));
                Assert.That(FindChild(canvas.transform, "Title"), Is.Not.Null);
                Assert.That(FindChild(canvas.transform, "Instruction"), Is.Not.Null);
                Assert.That(FindChild(canvas.transform, "Connection Status"), Is.Not.Null);
                Assert.That(GetText(canvas.transform, "Title"), Is.EqualTo("NCKH VR"));
                Assert.That(GetText(canvas.transform, "Instruction"),
                    Is.EqualTo("Vui lòng chờ người hướng dẫn bắt đầu trò chơi"));
                Assert.That(GetText(canvas.transform, "Connection Status"),
                    Is.EqualTo("Chưa cấu hình máy chủ"));

                SerializedObject serializedConfiguration = new SerializedObject(configurationView);
                GameObject panel = serializedConfiguration.FindProperty("panelRoot").objectReferenceValue as GameObject;
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.activeSelf, Is.False);
                Assert.That(serializedConfiguration.FindProperty("urlInput").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedConfiguration.FindProperty("validationText").objectReferenceValue, Is.Not.Null);
                Assert.That(serializedConfiguration.FindProperty("saveButton").objectReferenceValue, Is.Not.Null);
                VrKeyboard keyboard = serializedConfiguration.FindProperty("keyboard").objectReferenceValue as VrKeyboard;
                Assert.That(keyboard, Is.Not.Null);
                Assert.That(VrKeyboard.ExpectedKeyCount, Is.EqualTo(59));
                Assert.That(canvas.GetComponent<TrackedDeviceGraphicRaycaster>(), Is.Not.Null);
                Assert.That(FindComponent<EventSystem>(scene), Is.Not.Null);
                Assert.That(FindComponent<XRUIInputModule>(scene), Is.Not.Null);
                Assert.That(FindComponent<XRInteractionManager>(scene), Is.Not.Null);

                SerializedObject serializedView = new SerializedObject(statusView);
                Assert.That(serializedView.FindProperty("statusText").objectReferenceValue, Is.Not.Null);

                SerializedObject serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("clientId"), Is.Null);
                Assert.That(serialized.FindProperty("healthRoute").stringValue,
                    Is.EqualTo(GameControlApi.HealthRoute));
                Assert.That(serialized.FindProperty("telemetryRoute").stringValue,
                    Is.EqualTo(GameControlApi.TelemetryRoute));
                Assert.That(serialized.FindProperty("controlWebSocketRoute").stringValue,
                    Is.EqualTo(GameControlApi.ControlWebSocketRoute));
                Assert.That(serialized.FindProperty("ttsWebSocketRoute").stringValue,
                    Is.EqualTo(GameControlApi.TtsWebSocketRoute));
                Assert.That(serialized.FindProperty("sttWebSocketRoute").stringValue,
                    Is.EqualTo(GameControlApi.SttWebSocketRoute));
                SerializedProperty catalog = serialized.FindProperty("scenes");
                Assert.That(catalog.arraySize, Is.EqualTo(5));
                string[] ids = Enumerable.Range(0, catalog.arraySize)
                    .Select(i => catalog.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue)
                    .ToArray();
                Assert.That(ids, Is.EquivalentTo(new[] { "standby", "clinic", "doctor", "lawyer", "sale" }));
            });
        }

        [Test]
        public void DoctorGameplayScenesUseRemoteAdaptersAndAutoStart()
        {
            WithScene(ClinicScenePath, scene =>
            {
                ClinicGameManager manager = FindComponent<ClinicGameManager>(scene);
                Assert.That(manager, Is.Not.Null);
                ClinicRemoteSceneAdapter adapter = manager.GetComponent<ClinicRemoteSceneAdapter>();
                Assert.That(adapter, Is.Not.Null);
                Assert.That(adapter.SceneId, Is.EqualTo("clinic"));
                SerializedObject serializedManager = new SerializedObject(manager);
                Assert.That(serializedManager.FindProperty("startAutomatically").boolValue, Is.True);
                ClinicRoundHud hud = FindComponent<ClinicRoundHud>(scene);
                Assert.That(hud, Is.Not.Null);
                GameObject locomotion = new SerializedObject(hud)
                    .FindProperty("locomotionRoot").objectReferenceValue as GameObject;
                Assert.That(locomotion, Is.Not.Null);
                Assert.That(locomotion.name, Is.EqualTo("Locomotion"));
                Assert.That(locomotion.transform.Find("Move").gameObject.activeSelf, Is.True);
                Assert.That(locomotion.transform.Find("Turn").gameObject.activeSelf, Is.True);
                Assert.That(locomotion.transform.Find("Jump").gameObject.activeSelf, Is.False);
            });

            WithScene(DoctorScenePath, scene =>
            {
                DoctorGameManager manager = FindComponent<DoctorGameManager>(scene);
                Assert.That(manager, Is.Not.Null);
                DoctorRemoteSceneAdapter adapter = manager.GetComponent<DoctorRemoteSceneAdapter>();
                Assert.That(adapter, Is.Not.Null);
                Assert.That(adapter.SceneId, Is.EqualTo("doctor"));
                SerializedObject serializedManager = new SerializedObject(manager);
                Assert.That(serializedManager.FindProperty("startAutomatically").boolValue, Is.True);
                GameObject locomotion = serializedManager.FindProperty("locomotionRoot")
                    .objectReferenceValue as GameObject;
                Assert.That(locomotion, Is.Not.Null);
                Assert.That(locomotion.name, Is.EqualTo("Locomotion"));
                Assert.That(serializedManager.FindProperty("moveProviderRoot")
                    .objectReferenceValue.name, Is.EqualTo("Move"));
                Assert.That(serializedManager.FindProperty("gravityProviderRoot")
                    .objectReferenceValue.name, Is.EqualTo("Gravity"));
                Assert.That(serializedManager.FindProperty("jumpProviderRoot")
                    .objectReferenceValue.name, Is.EqualTo("Jump"));
            });

            WithScene(LawyerOfficeScenePath, scene =>
            {
                LawyerRemoteSceneAdapter adapter = FindComponent<LawyerRemoteSceneAdapter>(scene);
                Assert.That(adapter, Is.Not.Null);
                Assert.That(adapter.SceneId, Is.EqualTo("lawyer"));
            });

            WithScene(LawyerCourtroomScenePath, scene =>
            {
                LawyerRemoteSceneAdapter adapter = FindComponent<LawyerRemoteSceneAdapter>(scene);
                Assert.That(adapter, Is.Not.Null);
                Assert.That(adapter.SceneId, Is.EqualTo("lawyer"));
            });
        }

        [Test]
        public void DoctorGameStartCommandsAcceptAlreadyStartingRounds()
        {
            GameObject doctorRoot = new GameObject("Doctor start command test");
            GameObject clinicRoot = new GameObject("Clinic start command test");
            try
            {
                DoctorGameManager doctorManager = doctorRoot.AddComponent<DoctorGameManager>();
                DoctorRemoteSceneAdapter doctorAdapter = doctorRoot.AddComponent<DoctorRemoteSceneAdapter>();
                SerializedObject serializedDoctorAdapter = new SerializedObject(doctorAdapter);
                serializedDoctorAdapter.FindProperty("gameManager").objectReferenceValue = doctorManager;
                serializedDoctorAdapter.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(doctorAdapter.TryStart(out string firstDoctorError), Is.True);
                Assert.That(firstDoctorError, Is.Empty);
                Assert.That(doctorManager.State, Is.EqualTo(DoctorGameState.Preparing));

                Assert.That(doctorAdapter.TryStart(out string repeatedDoctorError), Is.True);
                Assert.That(repeatedDoctorError, Is.Empty);
                Assert.That(doctorManager.State, Is.EqualTo(DoctorGameState.Preparing));

                ClinicGameManager clinicManager = clinicRoot.AddComponent<ClinicGameManager>();
                ClinicRemoteSceneAdapter clinicAdapter = clinicRoot.AddComponent<ClinicRemoteSceneAdapter>();
                SerializedObject serializedClinicAdapter = new SerializedObject(clinicAdapter);
                serializedClinicAdapter.FindProperty("gameManager").objectReferenceValue = clinicManager;
                serializedClinicAdapter.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(clinicAdapter.TryStart(out string firstClinicError), Is.True);
                Assert.That(firstClinicError, Is.Empty);
                Assert.That(clinicManager.RoundState, Is.EqualTo(ClinicRoundState.Preparing));

                Assert.That(clinicAdapter.TryStart(out string repeatedClinicError), Is.True);
                Assert.That(repeatedClinicError, Is.Empty);
                Assert.That(clinicManager.RoundState, Is.EqualTo(ClinicRoundState.Preparing));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(doctorRoot);
                UnityEngine.Object.DestroyImmediate(clinicRoot);
            }
        }

        [Test]
        public void ResetRegistrySkipsStandbyAndTargetsRequestedGameplayState()
        {
            ResetHandler standby = new ResetHandler("standby");
            ResetHandler lawyer = new ResetHandler("lawyer");
            SceneResetRegistry.Register(standby);
            SceneResetRegistry.Register(lawyer);
            try
            {
                Assert.That(SceneResetRegistry.TryReset("doctor", out string error), Is.True);
                Assert.That(error, Is.Empty);
                Assert.That(lawyer.ResetCount, Is.Zero);

                Assert.That(SceneResetRegistry.TryReset("lawyer", out error), Is.True);
                Assert.That(lawyer.ResetCount, Is.EqualTo(1));

                Assert.That(SceneResetRegistry.TryReset(GameController.ResetAllTarget, out error), Is.True);
                Assert.That(lawyer.ResetCount, Is.EqualTo(2));
                Assert.That(standby.ResetCount, Is.Zero);
            }
            finally
            {
                SceneResetRegistry.Unregister(standby);
                SceneResetRegistry.Unregister(lawyer);
            }
        }

        private sealed class ResetHandler : ISceneResetHandler
        {
            public string SceneId { get; }
            public int ResetCount { get; private set; }

            public ResetHandler(string sceneId) => SceneId = sceneId;

            public bool TryResetToInitialState(out string errorCode)
            {
                ResetCount++;
                errorCode = string.Empty;
                return true;
            }
        }

        private static void WithScene(string path, System.Action<Scene> assertion)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                assertion(scene);
            }
            finally
            {
                if (openedForTest)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }

        private static Transform FindChild(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == name);
        }

        private static string GetText(Transform root, string name)
        {
            Transform child = FindChild(root, name);
            Component textComponent = child == null ? null : child.GetComponent("TextMeshProUGUI");
            Assert.That(textComponent, Is.Not.Null, $"{name} has no TextMeshProUGUI component.");
            return new SerializedObject(textComponent).FindProperty("m_text").stringValue;
        }
    }
}
