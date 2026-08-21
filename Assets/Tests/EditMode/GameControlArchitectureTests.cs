using System.Linq;
using NCKH.VR.Clinic;
using NCKH.VR.Doctor;
using NCKH.VR.GameControl;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.GameControl.Tests
{
    public sealed class GameControlArchitectureTests
    {
        private const string StandbyScenePath = "Assets/Scenes/Standby.unity";
        private const string ClinicScenePath = "Assets/Scenes/Doctor/Game 1.unity";
        private const string DoctorScenePath = "Assets/Scenes/Doctor/Game 2.unity";

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
            Assert.That(GameControlApi.ControlWebSocketRoute, Is.EqualTo("/api/ws/ctrl"));
            Assert.That(GameControlApi.TtsWebSocketRoute, Is.EqualTo("/api/ws/tts"));
            Assert.That(GameControlApi.SttWebSocketRoute, Is.EqualTo("/api/ws/stt"));
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
        public void CommandAcknowledgementRoundTripsThroughJsonUtility()
        {
            RemoteCommandAckDto acknowledgement = new RemoteCommandAckDto
            {
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
            Assert.That(scenes[1].path, Is.EqualTo(ClinicScenePath));
            Assert.That(scenes[2].path, Is.EqualTo(DoctorScenePath));
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
                Assert.That(catalog.arraySize, Is.EqualTo(3));
                string[] ids = Enumerable.Range(0, catalog.arraySize)
                    .Select(i => catalog.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue)
                    .ToArray();
                Assert.That(ids, Is.EquivalentTo(new[] { "standby", "clinic", "doctor" }));
            });
        }

        [Test]
        public void GameplayScenesUseRemoteAdaptersAndDoNotAutoStart()
        {
            WithScene(ClinicScenePath, scene =>
            {
                ClinicGameManager manager = FindComponent<ClinicGameManager>(scene);
                Assert.That(manager, Is.Not.Null);
                Assert.That(manager.GetComponent<ClinicRemoteSceneAdapter>(), Is.Not.Null);
                Assert.That(new SerializedObject(manager).FindProperty("startAutomatically").boolValue, Is.False);
            });

            WithScene(DoctorScenePath, scene =>
            {
                DoctorGameManager manager = FindComponent<DoctorGameManager>(scene);
                Assert.That(manager, Is.Not.Null);
                Assert.That(manager.GetComponent<DoctorRemoteSceneAdapter>(), Is.Not.Null);
                Assert.That(new SerializedObject(manager).FindProperty("startAutomatically").boolValue, Is.False);
            });
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
