using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using NCKH.VR.GameControl;

namespace NCKH.VR.Sales
{
    public interface ISalesPart2RequestCancellation
    {
        void CancelAll();
    }

    /// <summary>
    /// Request-response HTTP transport for Part 2. The gameplay controller owns
    /// retry and turn state; this class only serializes requests and responses.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalesPart2RequestService : MonoBehaviour, ISalesPart2RequestService,
        ISalesPart2RequestCancellation
    {
        [Header("Backend")]
        [SerializeField] private string backendBaseUrl = string.Empty;
        [SerializeField, Min(1f)] private float timeoutSeconds = 20f;

        [Header("Routes")]
        [SerializeField] private string startSessionRoute = "/api/sales/sessions";
        [SerializeField] private string submitTurnRoute = "/api/sales/sessions/{sessionId}/turns";
        [SerializeField] private string completeAnalysisRoute = "/api/sales/sessions/{sessionId}/complete";
        [SerializeField] private string sessionStateRoute = "/api/sales/sessions/{sessionId}";

        public string BearerToken { private get; set; }
        private readonly HashSet<UnityWebRequest> requests = new HashSet<UnityWebRequest>();

        public string BackendBaseUrl
        {
            get => backendBaseUrl;
            set => backendBaseUrl = value == null ? string.Empty : value.Trim();
        }

        public IEnumerator StartOrResumeSession(SalesPart2StartSessionRequest request,
            Action<SalesPart2SessionResponse> completed)
        {
            object wire = request == null ? null : string.IsNullOrEmpty(request.part1AttemptId)
                ? (object)new SessionStartWire { sessionId = request.salesSessionId }
                : new SessionWire { sessionId = request.salesSessionId, part1AttemptId = request.part1AttemptId };
            yield return SendJson(startSessionRoute, wire, completed);
        }

        public IEnumerator AssociatePart1(SalesPart2Part1AssociationRequest request,
            Action<SalesPart2SessionResponse> completed)
        {
            yield return SendJson(startSessionRoute, request == null ? null : new SessionWire
            {
                sessionId = request.salesSessionId,
                part1AttemptId = request.part1AttemptId
            }, completed);
        }

        public IEnumerator SubmitTurn(SalesPart2TurnRequest request,
            Action<SalesPart2TurnResponse> completed)
        {
            yield return SendJson(WithSession(submitTurnRoute, request?.salesSessionId),
                request == null ? null : new TurnWire
                {
                    turnId = request.turnId,
                    audio = request.audio == null ? null : new AudioWire
                    {
                        mimeType = request.audio.mimeType,
                        encoding = request.audio.encoding,
                        sampleRateHz = request.audio.sampleRateHz,
                        channels = request.audio.channels,
                        dataBase64 = request.audio.dataBase64
                    },
                    clientVersion = request.clientVersion,
                    retryCount = request.retryCount
                }, completed);
        }

        public IEnumerator CompleteAnalysis(SalesPart2AnalysisRequest request,
            Action<SalesPart2AnalysisResponse> completed)
        {
            yield return SendJson(WithSession(completeAnalysisRoute, request?.salesSessionId),
                request == null ? null : new CompletionWire
                {
                    completionId = request.completionId,
                    reason = request.reason
                }, completed);
        }

        public IEnumerator ReadSessionState(SalesPart2SessionStateRequest request,
            Action<SalesPart2SessionStateResponse> completed)
        {
            yield return SendGet(WithSession(sessionStateRoute, request?.salesSessionId), completed);
        }

        public void CancelAll()
        {
            UnityWebRequest[] active = new UnityWebRequest[requests.Count];
            requests.CopyTo(active);
            requests.Clear();
            for (int index = 0; index < active.Length; index++)
                if (active[index] != null) { active[index].Abort(); active[index].Dispose(); }
        }

        private string WithSession(string route, string sessionId)
        {
            return (route ?? string.Empty).Replace("{sessionId}", Uri.EscapeDataString(sessionId ?? string.Empty));
        }

        [Serializable]
        private sealed class SessionStartWire { public string sessionId; }

        [Serializable]
        private sealed class SessionWire
        {
            public string sessionId;
            public string part1AttemptId;
        }

        [Serializable]
        private sealed class TurnWire
        {
            public string turnId;
            public AudioWire audio;
            public string clientVersion;
            public int retryCount;
        }

        [Serializable]
        private sealed class AudioWire
        {
            public string mimeType;
            public string encoding;
            public int sampleRateHz;
            public int channels;
            public string dataBase64;
        }

        [Serializable]
        private sealed class CompletionWire
        {
            public string completionId;
            public string reason;
        }

        private IEnumerator SendJson<TResponse>(string route, object payload,
            Action<TResponse> completed) where TResponse : class
        {
            string baseUrl = string.IsNullOrWhiteSpace(backendBaseUrl) && GameController.Instance != null ? GameController.Instance.BackendBaseUrl : backendBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                completed?.Invoke(CreateFailure<TResponse>("backend_not_configured", "Không tìm thấy kết nối máy chủ."));
                yield break;
            }

            string url = GameControlApi.CombineUrl(baseUrl, route);
            string body = JsonUtility.ToJson(payload ?? new object());
            using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                requests.Add(request);
                try
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrWhiteSpace(BearerToken)) request.SetRequestHeader("Authorization", "Bearer " + BearerToken);
                    request.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success
                        || request.responseCode < 200 || request.responseCode >= 300)
                    {
                        string category = request.result == UnityWebRequest.Result.ConnectionError
                            ? "network" : "http";
                        completed?.Invoke(CreateFailure<TResponse>(category,
                            "Không thể kết nối máy chủ. Hãy thử lại."));
                        yield break;
                    }

                    TResponse response = null;
                    try
                    {
                        response = JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
                    }
                    catch (Exception)
                    {
                        completed?.Invoke(CreateFailure<TResponse>("invalid_response",
                            "Máy chủ trả về dữ liệu không hợp lệ."));
                        yield break;
                    }
                    if (response == null)
                        response = CreateFailure<TResponse>("invalid_response",
                            "Máy chủ trả về dữ liệu trống.");
                    NormalizeResponse(response);
                    completed?.Invoke(response);
                }
                finally
                {
                    requests.Remove(request);
                }
            }
        }

        private IEnumerator SendGet<TResponse>(string route,
            Action<TResponse> completed) where TResponse : class
        {
            string baseUrl = string.IsNullOrWhiteSpace(backendBaseUrl) && GameController.Instance != null ? GameController.Instance.BackendBaseUrl : backendBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                completed?.Invoke(CreateFailure<TResponse>("backend_not_configured", "Không tìm thấy kết nối máy chủ."));
                yield break;
            }
            using (UnityWebRequest request = UnityWebRequest.Get(GameControlApi.CombineUrl(baseUrl, route)))
            {
                requests.Add(request);
                try
                {
                    if (!string.IsNullOrWhiteSpace(BearerToken)) request.SetRequestHeader("Authorization", "Bearer " + BearerToken);
                    request.timeout = Mathf.Max(1, Mathf.CeilToInt(timeoutSeconds));
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success
                        || request.responseCode < 200 || request.responseCode >= 300)
                    {
                        completed?.Invoke(CreateFailure<TResponse>("network", "Không thể kết nối máy chủ. Hãy thử lại."));
                        yield break;
                    }
                    TResponse response = null;
                    try { response = JsonUtility.FromJson<TResponse>(request.downloadHandler.text); }
                    catch (Exception) { }
                    if (response == null)
                        response = CreateFailure<TResponse>("invalid_response",
                            "Máy chủ trả về dữ liệu không hợp lệ.");
                    NormalizeResponse(response);
                    completed?.Invoke(response);
                }
                finally { requests.Remove(request); }
            }
        }

        private static TResponse CreateFailure<TResponse>(string category, string message)
            where TResponse : class
        {
            TResponse result = Activator.CreateInstance<TResponse>();
            if (result is SalesPart2SessionResponse session)
            {
                session.success = false;
                session.failureCategory = category;
                session.errorMessage = message;
            }
            else if (result is SalesPart2TurnResponse turn)
            {
                turn.success = false;
                turn.technicalFailure = true;
                turn.failureCategory = category;
                turn.errorMessage = message;
            }
            else if (result is SalesPart2AnalysisResponse analysis)
            {
                analysis.success = false;
                analysis.failureCategory = category;
                analysis.errorMessage = message;
            }
            else if (result is SalesPart2SessionStateResponse state)
            {
                state.success = false;
                state.failureCategory = category;
                state.errorMessage = message;
            }
            return result;
        }

        private static void NormalizeResponse<TResponse>(TResponse value) where TResponse : class
        {
            if (value is SalesPart2SessionResponse session)
            {
                if (string.IsNullOrWhiteSpace(session.sessionId))
                    session.sessionId = session.salesSessionId;
                if (string.IsNullOrWhiteSpace(session.salesSessionId))
                    session.salesSessionId = session.sessionId;
                session.success = !string.IsNullOrWhiteSpace(session.sessionId)
                    && string.IsNullOrWhiteSpace(session.failureCategory);
            }
            else if (value is SalesPart2TurnResponse turn)
            {
                turn.success = !string.Equals(turn.status, "failed", StringComparison.OrdinalIgnoreCase)
                    && turn.error == null
                    && (turn.accepted || turn.silent || turn.nonVietnamese
                        || string.Equals(turn.status, "silent", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(turn.status, "nonVietnamese", StringComparison.OrdinalIgnoreCase));
                turn.technicalFailure = !turn.success && !turn.silent && !turn.nonVietnamese;
                if (turn.error != null)
                {
                    turn.failureCategory = turn.error.code;
                    turn.errorMessage = turn.error.message;
                }
            }
            else if (value is SalesPart2AnalysisResponse analysis)
                analysis.success = analysis.ParsedTrustState != SalesPart2TrustState.Unknown;
            else if (value is SalesPart2SessionStateResponse state)
                state.success = !string.IsNullOrWhiteSpace(state.salesSessionId);
        }

        private void OnDisable() => CancelAll();
        private void OnDestroy() => CancelAll();
    }
}
