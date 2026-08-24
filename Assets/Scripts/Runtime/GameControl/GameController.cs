using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace NCKH.VR.GameControl
{
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        public const string BackendBaseUrlPlayerPrefsKey = "nckh.backendBaseUrl.v1";

        private const int MaximumConcurrentTelemetryRequests = 4;
        private const float HeartbeatIntervalSeconds = 15f;

        [Header("Backend")]
        [SerializeField] private string backendBaseUrl = string.Empty;
        [SerializeField, Min(1f)] private float requestTimeoutSeconds = 10f;
        [SerializeField, Min(0f)] private float disconnectGraceSeconds = 10f;
        [SerializeField, Min(1f)] private float maximumReconnectDelaySeconds = 10f;

        [Header("API Routes")]
        [SerializeField] private string healthRoute = GameControlApi.HealthRoute;
        [SerializeField] private string telemetryRoute = GameControlApi.TelemetryRoute;
        [SerializeField] private string controlWebSocketRoute = GameControlApi.ControlWebSocketRoute;
        [SerializeField] private string ttsWebSocketRoute = GameControlApi.TtsWebSocketRoute;
        [SerializeField] private string sttWebSocketRoute = GameControlApi.SttWebSocketRoute;

        [Header("Scene Catalog")]
        [SerializeField] private List<RemoteSceneCatalogEntry> scenes = new List<RemoteSceneCatalogEntry>();

        private readonly Dictionary<string, RemoteSceneCatalogEntry> scenesById =
            new Dictionary<string, RemoteSceneCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, RemoteCommandAckDto> acknowledgementCache =
            new Dictionary<string, RemoteCommandAckDto>(StringComparer.Ordinal);
        private readonly Queue<RemoteCommandDto> pendingCommands = new Queue<RemoteCommandDto>();

        private IGameSceneAdapter activeAdapter;
        private Coroutine controlLoop;
        private WebSocket controlWebSocket;
        private RemoteCommandAckDto pendingAcknowledgement;
        private string pendingAcknowledgementCommandId = string.Empty;
        private string controlSocketError = string.Empty;
        private string sessionId;
        private string currentSceneId = string.Empty;
        private string configurationError;
        private float disconnectedSince = -1f;
        private float nextHeartbeatTime;
        private int consecutiveFailures;
        private int telemetryRequestsInFlight;
        private bool backendHealthy;
        private bool controlSocketOpen;
        private bool controlSocketClosed;
        private bool stoppedForDisconnect;
        private bool warnedTelemetrySaturation;

        public static GameController Instance { get; private set; }
        public event Action<string> StatusChanged;

        public string CurrentSceneId => currentSceneId;
        public RemoteGamePhase CurrentPhase => activeAdapter != null
            ? activeAdapter.Phase
            : RemoteGamePhase.Standby;
        public string SessionId => sessionId;
        public bool IsConfigured => string.IsNullOrEmpty(configurationError);
        public string BackendBaseUrl => backendBaseUrl;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            sessionId = Guid.NewGuid().ToString("N");
            LoadPersistedBackendBaseUrl();
            RebuildSceneCatalog();
        }

        public bool TrySetBackendBaseUrl(string value, out string error)
        {
            string normalized = value == null ? string.Empty : value.Trim();
            if (!TryValidateBackendBaseUrl(normalized, out error))
                return false;

            backendBaseUrl = normalized;
            PlayerPrefs.SetString(BackendBaseUrlPlayerPrefsKey, backendBaseUrl);
            PlayerPrefs.Save();
            RebuildSceneCatalog();
            RestartBackendConnection();
            error = string.Empty;
            return true;
        }

        public static bool TryValidateBackendBaseUrl(string value, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Vui lòng nhập URL máy chủ.";
                return false;
            }

            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "URL máy chủ phải là địa chỉ HTTP hoặc HTTPS tuyệt đối.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private void OnEnable()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            BindLoadedScene(SceneManager.GetActiveScene());
            controlLoop = StartCoroutine(ControlLoop());
        }

        private void OnDisable()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnbindAdapter();
            if (controlLoop != null)
                StopCoroutine(controlLoop);
            controlLoop = null;
            ReleaseControlSocket(true);
            pendingCommands.Clear();
            pendingAcknowledgement = null;
            pendingAcknowledgementCommandId = string.Empty;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private IEnumerator ControlLoop()
        {
            PublishTelemetry("client.started", null);

            while (enabled)
            {
                if (!IsConfigured)
                {
                    SetStatus(configurationError);
                    yield return new WaitForSecondsRealtime(1f);
                    continue;
                }

                if (!backendHealthy)
                {
                    yield return CheckBackendHealth();
                    if (!backendHealthy)
                    {
                        yield return new WaitForSecondsRealtime(ReconnectDelay());
                        continue;
                    }
                }

                yield return ConnectControlSocket();
                if (!controlSocketOpen)
                {
                    ReleaseControlSocket(false);
                    yield return new WaitForSecondsRealtime(ReconnectDelay());
                    continue;
                }

                while (enabled && controlSocketOpen && string.IsNullOrEmpty(controlSocketError))
                {
                    PublishHeartbeatIfDue();

                    if (pendingAcknowledgement != null)
                    {
                        yield return SendPendingAcknowledgement();
                        if (pendingAcknowledgement != null)
                            break;
                    }
                    else if (pendingCommands.Count > 0)
                    {
                        yield return ProcessCommand(pendingCommands.Dequeue());
                    }
                    else
                    {
                        yield return null;
                    }
                }

                if (enabled)
                {
                    string error = string.IsNullOrEmpty(controlSocketError)
                        ? "Kênh điều khiển đã đóng."
                        : controlSocketError;
                    HandleNetworkFailure(error);
                }

                ReleaseControlSocket(false);
                if (enabled)
                    yield return new WaitForSecondsRealtime(ReconnectDelay());
            }
        }

        private void LoadPersistedBackendBaseUrl()
        {
            if (PlayerPrefs.HasKey(BackendBaseUrlPlayerPrefsKey))
                backendBaseUrl = PlayerPrefs.GetString(BackendBaseUrlPlayerPrefsKey, backendBaseUrl).Trim();
        }

        private void RestartBackendConnection()
        {
            if (controlLoop != null)
                StopCoroutine(controlLoop);
            controlLoop = null;

            ReleaseControlSocket(true);
            pendingCommands.Clear();
            acknowledgementCache.Clear();
            pendingAcknowledgement = null;
            pendingAcknowledgementCommandId = string.Empty;
            controlSocketError = string.Empty;
            backendHealthy = false;
            controlSocketOpen = false;
            controlSocketClosed = false;
            disconnectedSince = -1f;
            nextHeartbeatTime = 0f;
            consecutiveFailures = 0;
            stoppedForDisconnect = false;

            if (isActiveAndEnabled && Instance == this)
                controlLoop = StartCoroutine(ControlLoop());
        }

        private IEnumerator CheckBackendHealth()
        {
            SetStatus("Đang kiểm tra kết nối máy chủ...");
            using UnityWebRequest request = UnityWebRequest.Get(
                GameControlApi.CombineUrl(backendBaseUrl, healthRoute));
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success
                && request.responseCode >= 200 && request.responseCode < 300)
            {
                backendHealthy = true;
                SetStatus("Máy chủ sẵn sàng, đang mở kênh điều khiển...");
                yield break;
            }

            string error = request.result == UnityWebRequest.Result.Success
                ? $"Health endpoint returned HTTP {request.responseCode}."
                : request.error;
            HandleNetworkFailure(error);
        }

        private IEnumerator ConnectControlSocket()
        {
            ReleaseControlSocket(false);
            controlSocketError = string.Empty;
            controlSocketOpen = false;
            controlSocketClosed = false;

            string url = GameControlApi.CombineWebSocketUrl(backendBaseUrl, controlWebSocketRoute);
            Debug.Log($"Connecting to control WebSocket at {url}...", this);
            if (string.IsNullOrEmpty(url))
            {
                HandleNetworkFailure("Không thể tạo URL WebSocket điều khiển.");
                yield break;
            }

            SetStatus("Đang kết nối kênh điều khiển...");
            controlWebSocket = new WebSocket(url);
            controlWebSocket.OnOpen += OnControlSocketOpened;
            controlWebSocket.OnMessage += OnControlSocketMessage;
            controlWebSocket.OnError += OnControlSocketError;
            controlWebSocket.OnClose += OnControlSocketClosed;

            try
            {
                _ = controlWebSocket.Connect();
            }
            catch (Exception exception)
            {
                controlSocketError = exception.Message;
                controlSocketClosed = true;
            }

            float deadline = Time.realtimeSinceStartup + requestTimeoutSeconds;
            while (enabled && !controlSocketOpen && !controlSocketClosed
                && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!enabled)
                yield break;

            if (!controlSocketOpen)
            {
                if (!controlSocketClosed)
                {
                    controlSocketError = "Hết thời gian kết nối WebSocket điều khiển.";
                    controlWebSocket.CancelConnection();
                }

                HandleNetworkFailure(string.IsNullOrEmpty(controlSocketError)
                    ? "Không thể kết nối WebSocket điều khiển."
                    : controlSocketError);
            }
        }

        private void OnControlSocketOpened()
        {
            controlSocketOpen = true;
            controlSocketClosed = false;
            controlSocketError = string.Empty;
            HandleNetworkSuccess();
        }

        private void OnControlSocketMessage(byte[] data)
        {
            RemoteCommandDto command;
            try
            {
                command = JsonUtility.FromJson<RemoteCommandDto>(System.Text.Encoding.UTF8.GetString(data));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Invalid command JSON: {exception.Message}", this);
                return;
            }

            if (command == null || string.IsNullOrWhiteSpace(command.commandId))
            {
                Debug.LogWarning("Backend command is missing commandId.", this);
                return;
            }

            pendingCommands.Enqueue(command);
        }

        private void OnControlSocketError(string error)
        {
            controlSocketError = string.IsNullOrWhiteSpace(error)
                ? "WebSocket điều khiển gặp lỗi."
                : error;
        }

        private void OnControlSocketClosed(WebSocketCloseCode closeCode)
        {
            controlSocketOpen = false;
            controlSocketClosed = true;
            if (string.IsNullOrEmpty(controlSocketError))
                controlSocketError = $"WebSocket điều khiển đã đóng ({closeCode}).";
        }

        private IEnumerator ProcessCommand(RemoteCommandDto command)
        {
            if (acknowledgementCache.TryGetValue(command.commandId, out RemoteCommandAckDto cached))
            {
                SetPendingAcknowledgement(command.commandId, cached);
                yield break;
            }

            PublishTelemetry("command.received", new GameplayTelemetryData
            {
                commandId = command.commandId,
                commandType = command.type
            });

            string status = "applied";
            string errorCode = string.Empty;
            string errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(command.sceneId)
                || !scenesById.TryGetValue(command.sceneId.Trim(), out RemoteSceneCatalogEntry targetScene))
            {
                status = "rejected";
                errorCode = "unknown_scene";
                errorMessage = $"Unknown scene ID '{command.sceneId}'.";
            }
            else
            {
                switch (command.type?.Trim())
                {
                    case GameControlApi.LoadSceneCommand:
                        yield return LoadCatalogScene(targetScene);
                        if (!string.Equals(currentSceneId, targetScene.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            status = "failed";
                            errorCode = "scene_load_failed";
                            errorMessage = $"Scene '{targetScene.Id}' did not become active.";
                        }
                        break;
                    case GameControlApi.StartSceneCommand:
                        ApplyAdapterCommand(targetScene,
                            activeAdapter == null ? null : new AdapterOperation(activeAdapter.TryStart),
                            ref status, ref errorCode, ref errorMessage);
                        break;
                    case GameControlApi.RestartSceneCommand:
                        ApplyAdapterCommand(targetScene,
                            activeAdapter == null ? null : new AdapterOperation(activeAdapter.TryRestart),
                            ref status, ref errorCode, ref errorMessage);
                        break;
                    case GameControlApi.StopSceneCommand:
                        ApplyAdapterCommand(targetScene,
                            activeAdapter == null ? null : new AdapterOperation(activeAdapter.TryStop),
                            ref status, ref errorCode, ref errorMessage);
                        break;
                    default:
                        status = "rejected";
                        errorCode = "unknown_command";
                        errorMessage = $"Unknown command type '{command.type}'.";
                        break;
                }
            }

            RemoteCommandAckDto acknowledgement = new RemoteCommandAckDto
            {
                commandId = command.commandId,
                sequence = command.sequence,
                status = status,
                sceneId = currentSceneId,
                phase = PhaseName(CurrentPhase),
                appliedAtUtc = GameControlApi.UtcNow(),
                errorCode = errorCode,
                errorMessage = errorMessage
            };
            acknowledgementCache[command.commandId] = acknowledgement;

            PublishTelemetry("command." + status, new GameplayTelemetryData
            {
                commandId = command.commandId,
                commandType = command.type,
                commandStatus = status,
                errorCode = errorCode,
                errorMessage = errorMessage
            });

            SetPendingAcknowledgement(command.commandId, acknowledgement);
        }

        private delegate bool AdapterOperation(out string errorCode);

        private void ApplyAdapterCommand(
            RemoteSceneCatalogEntry targetScene,
            AdapterOperation operation,
            ref string status,
            ref string errorCode,
            ref string errorMessage)
        {
            if (!string.Equals(currentSceneId, targetScene.Id, StringComparison.OrdinalIgnoreCase))
            {
                status = "rejected";
                errorCode = "scene_mismatch";
                errorMessage = $"Command targets '{targetScene.Id}' while '{currentSceneId}' is active.";
                return;
            }

            if (operation == null)
            {
                status = "rejected";
                errorCode = "scene_not_controllable";
                errorMessage = "The active scene has no gameplay adapter.";
                return;
            }

            if (!operation(out errorCode))
            {
                status = "rejected";
                errorMessage = $"The active scene rejected the command ({errorCode}).";
            }
        }

        private IEnumerator LoadCatalogScene(RemoteSceneCatalogEntry entry)
        {
            if (string.Equals(currentSceneId, entry.Id, StringComparison.OrdinalIgnoreCase))
                yield break;

            SetStatus("Đang tải cảnh...");
            PublishTelemetry("scene.load_started", null);
            AsyncOperation load = SceneManager.LoadSceneAsync(entry.ScenePath, LoadSceneMode.Single);
            if (load == null)
                yield break;

            while (!load.isDone)
                yield return null;

            BindLoadedScene(SceneManager.GetActiveScene());
            PublishTelemetry("scene.ready", null);
        }

        private void SetPendingAcknowledgement(string commandId, RemoteCommandAckDto acknowledgement)
        {
            pendingAcknowledgementCommandId = commandId;
            pendingAcknowledgement = acknowledgement;
        }

        private IEnumerator SendPendingAcknowledgement()
        {
            if (pendingAcknowledgement == null || controlWebSocket == null
                || controlWebSocket.State != WebSocketState.Open)
                yield break;

            string commandId = pendingAcknowledgementCommandId;
            string json = JsonUtility.ToJson(pendingAcknowledgement);
            Task sendTask;
            try
            {
                sendTask = controlWebSocket.SendText(json);
            }
            catch (Exception exception)
            {
                controlSocketError = exception.Message;
                yield break;
            }

            while (!sendTask.IsCompleted && controlSocketOpen)
                yield return null;

            if (sendTask.IsFaulted)
            {
                controlSocketError = sendTask.Exception?.GetBaseException().Message
                    ?? "Không thể gửi xác nhận lệnh.";
                yield break;
            }

            if (!controlSocketOpen || controlWebSocket.State != WebSocketState.Open)
                yield break;

            if (string.Equals(commandId, pendingAcknowledgementCommandId, StringComparison.Ordinal))
            {
                pendingAcknowledgement = null;
                pendingAcknowledgementCommandId = string.Empty;
            }
        }

        public void PublishTelemetry(string eventType, GameplayTelemetryData payload)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(eventType))
                return;

            if (telemetryRequestsInFlight >= MaximumConcurrentTelemetryRequests)
            {
                if (!warnedTelemetrySaturation)
                {
                    warnedTelemetrySaturation = true;
                    Debug.LogWarning("Telemetry request limit reached; excess best-effort events are being dropped.", this);
                }
                return;
            }

            StartCoroutine(SendTelemetry(eventType, payload));
        }

        private IEnumerator SendTelemetry(string eventType, GameplayTelemetryData payload)
        {
            telemetryRequestsInFlight++;
            TelemetryEventDto telemetry = new TelemetryEventDto
            {
                eventId = Guid.NewGuid().ToString("N"),
                sessionId = sessionId,
                occurredAtUtc = GameControlApi.UtcNow(),
                sceneId = currentSceneId,
                phase = PhaseName(CurrentPhase),
                eventType = eventType,
                payload = payload
            };
            using UnityWebRequest request = CreateJsonPost(
                GameControlApi.CombineUrl(backendBaseUrl, telemetryRoute),
                JsonUtility.ToJson(telemetry));
            yield return request.SendWebRequest();

            telemetryRequestsInFlight--;
            warnedTelemetrySaturation = false;
        }

        private UnityWebRequest CreateJsonPost(string url, string json)
        {
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = Mathf.CeilToInt(requestTimeoutSeconds);
            return request;
        }

        private void ReleaseControlSocket(bool closeConnection)
        {
            WebSocket socket = controlWebSocket;
            if (socket == null)
                return;

            socket.OnOpen -= OnControlSocketOpened;
            socket.OnMessage -= OnControlSocketMessage;
            socket.OnError -= OnControlSocketError;
            socket.OnClose -= OnControlSocketClosed;
            controlWebSocket = null;
            controlSocketOpen = false;
            controlSocketClosed = true;

            if (!closeConnection)
            {
                socket.CancelConnection();
                return;
            }

            if (socket.State == WebSocketState.Open)
                _ = socket.Close(WebSocketCloseCode.Normal, "Unity client stopped");
            else if (socket.State == WebSocketState.Connecting)
                socket.CancelConnection();
        }

        private void HandleNetworkSuccess()
        {
            bool wasDisconnected = disconnectedSince >= 0f;
            backendHealthy = true;
            consecutiveFailures = 0;
            disconnectedSince = -1f;
            stoppedForDisconnect = false;
            SetStatus("Đã kết nối với máy chủ");
            if (wasDisconnected)
                PublishTelemetry("backend.connected", null);

            PublishHeartbeatIfDue();
        }

        private void PublishHeartbeatIfDue()
        {
            if (Time.realtimeSinceStartup >= nextHeartbeatTime)
            {
                nextHeartbeatTime = Time.realtimeSinceStartup + HeartbeatIntervalSeconds;
                PublishTelemetry("client.heartbeat", null);
            }
        }

        private void HandleNetworkFailure(string error)
        {
            Debug.LogError($"Network error: {error}", this);
            backendHealthy = false;
            consecutiveFailures++;
            if (disconnectedSince < 0f)
            {
                disconnectedSince = Time.realtimeSinceStartup;
                PublishTelemetry("backend.disconnected", new GameplayTelemetryData { errorMessage = error });
            }

            SetStatus("Mất kết nối - đang thử kết nối lại...");
            if (!stoppedForDisconnect
                && activeAdapter != null
                && activeAdapter.Phase == RemoteGamePhase.Running
                && Time.realtimeSinceStartup - disconnectedSince >= disconnectGraceSeconds)
            {
                stoppedForDisconnect = activeAdapter.TryStop(out _);
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindLoadedScene(scene);
        }

        private void BindLoadedScene(Scene scene)
        {
            UnbindAdapter();
            currentSceneId = FindSceneId(scene.path);
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && activeAdapter == null; i++)
            {
                MonoBehaviour[] behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (int j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is IGameSceneAdapter adapter)
                    {
                        activeAdapter = adapter;
                        break;
                    }
                }
            }

            if (activeAdapter != null)
                activeAdapter.TelemetryRaised += OnGameplayTelemetry;
            SetStatus("Sẵn sàng");
        }

        private void UnbindAdapter()
        {
            if (activeAdapter != null)
                activeAdapter.TelemetryRaised -= OnGameplayTelemetry;
            activeAdapter = null;
        }

        private void OnGameplayTelemetry(string eventType, GameplayTelemetryData payload)
        {
            PublishTelemetry(eventType, payload);
        }

        private void RebuildSceneCatalog()
        {
            scenesById.Clear();
            configurationError = string.Empty;

            if (!TryValidateBackendBaseUrl(backendBaseUrl, out string backendError))
                configurationError = backendError;

            ValidateRoute(healthRoute, "kiểm tra sức khỏe");
            ValidateRoute(telemetryRoute, "telemetry");
            ValidateRoute(controlWebSocketRoute, "WebSocket điều khiển");
            ValidateRoute(ttsWebSocketRoute, "WebSocket TTS");
            ValidateRoute(sttWebSocketRoute, "WebSocket STT");

            for (int i = 0; i < scenes.Count; i++)
            {
                RemoteSceneCatalogEntry entry = scenes[i];
                if (entry == null || string.IsNullOrEmpty(entry.Id) || string.IsNullOrEmpty(entry.ScenePath))
                {
                    configurationError = $"Mục {i} trong danh mục cảnh chưa đầy đủ.";
                    continue;
                }

                if (scenesById.ContainsKey(entry.Id))
                    configurationError = $"Mã cảnh '{entry.Id}' bị trùng lặp.";
                else
                    scenesById.Add(entry.Id, entry);
            }
        }

        private void ValidateRoute(string route, string displayName)
        {
            if (string.IsNullOrWhiteSpace(route))
                configurationError = $"Chưa cấu hình tuyến API {displayName}.";
        }

        private string FindSceneId(string scenePath)
        {
            foreach (RemoteSceneCatalogEntry entry in scenesById.Values)
                if (string.Equals(entry.ScenePath, scenePath, StringComparison.OrdinalIgnoreCase))
                    return entry.Id;
            return string.IsNullOrEmpty(scenePath) ? "unknown" : SceneNameFromPath(scenePath);
        }

        private static string SceneNameFromPath(string scenePath)
        {
            int slash = scenePath.LastIndexOf('/');
            string name = slash >= 0 ? scenePath.Substring(slash + 1) : scenePath;
            return name.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - 6)
                : name;
        }

        private static string PhaseName(RemoteGamePhase phase)
        {
            return phase.ToString().ToLowerInvariant();
        }

        private float ReconnectDelay()
        {
            return Mathf.Max(1f, Mathf.Min(
                maximumReconnectDelaySeconds,
                Mathf.Pow(2f, Mathf.Max(0, consecutiveFailures - 1))));
        }

        private void SetStatus(string status)
        {
            StatusChanged?.Invoke(status);
        }

        private void OnValidate()
        {
            requestTimeoutSeconds = Mathf.Max(1f, requestTimeoutSeconds);
            disconnectGraceSeconds = Mathf.Max(0f, disconnectGraceSeconds);
            maximumReconnectDelaySeconds = Mathf.Max(1f, maximumReconnectDelaySeconds);
            RebuildSceneCatalog();
        }
    }
}
