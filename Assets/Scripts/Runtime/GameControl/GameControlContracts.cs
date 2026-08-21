using System;
using UnityEngine;

namespace NCKH.VR.GameControl
{
    public enum RemoteGamePhase
    {
        Standby,
        Ready,
        Running,
        Finished
    }

    [Serializable]
    public sealed class RemoteSceneCatalogEntry
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string scenePath = string.Empty;

        public string Id => id?.Trim() ?? string.Empty;
        public string ScenePath => scenePath?.Trim() ?? string.Empty;

        public RemoteSceneCatalogEntry()
        {
        }

        public RemoteSceneCatalogEntry(string id, string scenePath)
        {
            this.id = id;
            this.scenePath = scenePath;
        }
    }

    public interface IGameSceneAdapter
    {
        RemoteGamePhase Phase { get; }
        event Action<string, GameplayTelemetryData> TelemetryRaised;
        bool TryStart(out string errorCode);
        bool TryRestart(out string errorCode);
        bool TryStop(out string errorCode);
    }

    [Serializable]
    public sealed class RemoteCommandDto
    {
        public string commandId;
        public long sequence;
        public string type;
        public string sceneId;
        public string issuedAtUtc;
    }

    [Serializable]
    public sealed class RemoteCommandAckDto
    {
        public long sequence;
        public string status;
        public string sceneId;
        public string phase;
        public string appliedAtUtc;
        public string errorCode;
        public string errorMessage;
    }

    [Serializable]
    public sealed class GameplayTelemetryData
    {
        public string roundId;
        public string caseId;
        public string questionId;
        public string cardId;
        public string category;
        public string patientId;
        public string sicknessName;
        public string resolution;
        public string commandId;
        public string commandType;
        public string commandStatus;
        public string errorCode;
        public string errorMessage;
        public int severity;
        public int bedIndex = -1;
        public int scoreDelta;
        public int scoreTotal;
        public float remainingTime;
    }

    [Serializable]
    public sealed class TelemetryEventDto
    {
        public int schemaVersion = 1;
        public string eventId;
        public string sessionId;
        public string occurredAtUtc;
        public string sceneId;
        public string phase;
        public string eventType;
        public GameplayTelemetryData payload;
    }

    public static class GameControlApi
    {
        public const string LoadSceneCommand = "load_scene";
        public const string StartSceneCommand = "start_scene";
        public const string RestartSceneCommand = "restart_scene";
        public const string StopSceneCommand = "stop_scene";
        public const string HealthRoute = "/api/health";
        public const string TelemetryRoute = "/api/telemetry";
        public const string ControlWebSocketRoute = "/api/ws/ctrl";
        public const string TtsWebSocketRoute = "/api/ws/tts";
        public const string SttWebSocketRoute = "/api/ws/stt";

        public static string CombineUrl(string baseUrl, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return string.Empty;

            return baseUrl.TrimEnd('/') + "/" + (relativePath ?? string.Empty).TrimStart('/');
        }

        public static string CombineWebSocketUrl(string baseUrl, string relativePath)
        {
            string httpUrl = CombineUrl(baseUrl, relativePath);
            if (!Uri.TryCreate(httpUrl, UriKind.Absolute, out Uri uri))
                return string.Empty;

            string webSocketScheme;
            if (uri.Scheme == Uri.UriSchemeHttp)
                webSocketScheme = "ws";
            else if (uri.Scheme == Uri.UriSchemeHttps)
                webSocketScheme = "wss";
            else
                return string.Empty;

            UriBuilder builder = new UriBuilder(uri)
            {
                Scheme = webSocketScheme,
                Port = uri.IsDefaultPort ? -1 : uri.Port
            };
            return builder.Uri.AbsoluteUri;
        }

        public static string UtcNow()
        {
            return DateTime.UtcNow.ToString("O");
        }
    }
}
