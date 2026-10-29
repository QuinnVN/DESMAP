using System;
using System.Collections;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace NCKH.VR.Sales
{
    public enum SalesPart2Stage
    {
        Waiting,
        Transition,
        CustomerApproach,
        CustomerReply,
        StartingRecording,
        Recording,
        Transcribing,
        CustomerResponding,
        FinalAnalysis,
        Incomplete,
        Completed,
        Stopped,
        Failed
    }

    /// <summary>
    /// Runtime state machine for the returning-customer conversation. All network
    /// work is delegated to ISalesPart2RequestService so Edit and Play Mode tests
    /// can use deterministic substitutes without a microphone or server.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalesPart2Controller : MonoBehaviour
    {
        private const int RecordingSampleRateHz = 16000;
        private const float DefaultTurnSeconds = 45f;
        private const float DefaultActiveMinutes = 5f;
        private const int DefaultMaximumAcceptedTurns = 8;
        private const string VietnameseOnlyRepeat =
            "Em có thể nói lại bằng tiếng Việt giúp chị được không?";

        [Header("Scenario")]
        public SalesPart2ScenarioData scenario;

        [Header("XR and authored route")]
        public Transform xrOrigin;
        public Transform playerAnchor;
        public Transform customer;
        public Transform customerStart;
        public Transform[] customerWaypoints = Array.Empty<Transform>();
        public Transform customerStop;
        public Animator customerAnimator;
        public AnimationClip walkClip;
        public AnimationClip idleClip;
        public Transform headTarget;
        public float customerWalkSpeed = 1.1f;
        public float customerStoppingDistance = .02f;

        [Header("HUD")]
        public CanvasGroup fadeCanvas;
        public TMP_Text customerText;
        public TMP_Text objectiveText;
        public TMP_Text policyText;
        public TMP_Text statusText;
        public TMP_Text timerText;
        public TMP_Text trustStateText;
        public TMP_Text resultText;
        public GameObject recordingIndicator;
        public GameObject incompleteRoot;
        public SalesPart2ActionTarget retryTarget;

        [Header("Input and comfort")]
        public Behaviour[] locomotion = Array.Empty<Behaviour>();
        [Min(0f)] public float fadeDuration = .4f;
        [Min(0f)] public float customerReplyDisplaySeconds = 1f;
        public float endingDisplaySeconds = 5f;
        public float transitionLabelSeconds = 2f;
        public GameObject hudRoot;
        [Min(0f)] public float turnDurationSeconds = DefaultTurnSeconds;
        [Min(0f)] public float activeTimeLimitSeconds = DefaultActiveMinutes * 60f;
        [Min(1)] public int maximumAcceptedTurns = DefaultMaximumAcceptedTurns;
        [Min(.1f)] public float microphoneStartupTimeout = 2f;
        [Min(.1f)] public float requestTimeoutSeconds = 20f;

        [Header("Services")]
        public ISalesMicrophone microphoneProvider;
        public ISalesPart2RequestService requestService;

        public SalesPart2Stage Stage { get; private set; } = SalesPart2Stage.Waiting;
        public SalesPart2Objective CurrentObjective { get; private set; } = SalesPart2Objective.ListeningAndClarifying;
        public SalesPart2TrustState TrustState { get; private set; } = SalesPart2TrustState.Unknown;
        public string SalesSessionId { get; private set; } = string.Empty;
        public string CurrentTurnId { get; private set; } = string.Empty;
        public string Part1AttemptId { get; private set; } = string.Empty;
        public int AcceptedTurnCount { get; private set; }
        public int SilenceCount { get; private set; }
        public float ActiveTimeSeconds => activeTimeSeconds;
        public bool RequestInFlight => requestInFlight;
        public bool IsCompleted => Stage == SalesPart2Stage.Completed;
        public bool IsFinished => IsCompleted || Stage == SalesPart2Stage.Failed;

        public event Action<string, GameplayTelemetryData> TelemetryRaised;
        public event Action<SalesPart2TrustState> TrustResolved;
        public event Action Part2Finished;

        private enum RequestKind
        {
            None,
            StartSession,
            Turn,
            Analysis,
            ReadState
        }

        private bool[] previousLocomotion;
        private bool locomotionLocked;
        private bool activeTimerRunning;
        private bool requestInFlight;
        private bool challengeShown;
        private bool finalCompletionRequested;
        private bool initialized;
        private bool microphoneRecording;
        private string microphoneDevice;
        private AudioClip recordingClip;
        private SalesPart2TurnRequest pendingTurn;
        private Coroutine requestRoutine;
        private Coroutine presentationRoutine;
        private Coroutine recordingRoutine;
        private Coroutine replyRoutine;
        private Coroutine finalRoutine;
        private int callbackGeneration;
        private float activeTimeSeconds;
        private float requestStartedAt;
        private string completionReason = string.Empty;
        private string lastTranscript = string.Empty;
        private string lastCustomerReply = string.Empty;
        private bool emotionalHandling;
        private bool causeIdentification;
        private bool solutionSuitability;
        private bool trustRebuilding;
        private RequestKind requestKind;
        private RequestKind failedRequestKind;
        private string completionId;
        private string llmProvider = "llama.cpp", llmModel = "unavailable", llmVersion = "unavailable";
        private string sttProvider = "whisper.cpp", sttModel = "unavailable", sttVersion = "unavailable";
        [Serializable]
        private sealed class DiagnosticData
        {
            public string salesSessionId, turnId, clientVersion, sttProvider, sttModel, sttVersion, llmProvider, llmModel, llmVersion, trustState;
            public int activeObjective, retryCount, acceptedTurnCount, silenceCount;
            public float activeDurationSeconds, processingDurationSeconds;
        }
        private InputAction submitAction;
        private PlayableGraph animationGraph;
        private AnimationClipPlayable animationPlayable;
        private Transform headBone;

        public InputAction SubmitAction => submitAction;

        private void Awake()
        {
            previousLocomotion = new bool[locomotion != null ? locomotion.Length : 0];
            if (microphoneProvider == null)
                microphoneProvider = new UnitySalesMicrophone();
        }

        private void OnEnable()
        {
            if (submitAction == null)
                submitAction = new InputAction("Submit Sales Part 2 Turn", InputActionType.Button,
                    "<XRController>{RightHand}/primaryButton");
            submitAction.performed += OnSubmitPerformed;
            submitAction.Enable();
            if (initialized && Stage == SalesPart2Stage.Stopped)
                ResetPart2();
        }

        private void Start()
        {
            initialized = true;
            if (Stage == SalesPart2Stage.Waiting) HideResultUi();
            if (scenario == null || !scenario.IsValid)
                Debug.LogWarning("Part 2 Sales cần một SalesPart2ScenarioData hợp lệ.", this);
        }

        private void Update()
        {
            if (requestInFlight && Time.realtimeSinceStartup - requestStartedAt >=
                Mathf.Max(.1f, requestTimeoutSeconds))
            {
                HandleRequestTimeout();
                return;
            }

            if (activeTimerRunning && !requestInFlight
                && (Stage == SalesPart2Stage.CustomerReply || Stage == SalesPart2Stage.CustomerResponding || Stage == SalesPart2Stage.Recording
                    || Stage == SalesPart2Stage.StartingRecording))
            {
                activeTimeSeconds += Time.unscaledDeltaTime;
                UpdateTimerUi();
                if (activeTimeSeconds >= Mathf.Max(.1f, activeTimeLimitSeconds))
                    RequestEndForLimit("time_limit");
            }

            if (Stage == SalesPart2Stage.Recording && recordingClip != null
                && !string.IsNullOrEmpty(microphoneDevice) && microphoneProvider != null
                && Time.realtimeSinceStartup >= recordingDeadline)
                FinishRecording(false);
        }

        private float recordingDeadline;

        private void LateUpdate()
        {
            if (animationGraph.IsValid() && animationPlayable.IsValid()) { var clip = animationPlayable.GetAnimationClip(); if (clip != null && clip.length > 0 && animationPlayable.GetTime() >= clip.length) animationPlayable.SetTime(animationPlayable.GetTime() % clip.length); }
            if (headTarget == null || headBone == null)
                return;
            Vector3 direction = headTarget.position - headBone.position;
            if (direction.sqrMagnitude < .0001f)
                return;
            headBone.rotation = Quaternion.Slerp(headBone.rotation,
                Quaternion.LookRotation(direction.normalized, Vector3.up),
                Mathf.Clamp01(Time.unscaledDeltaTime * 5f));
        }

        private void OnSubmitPerformed(InputAction.CallbackContext _) => HandleAction(-1);

        /// <summary>Starts Part 2 with the Sales session created by Part 1.</summary>
        public void BeginFromPart1(string salesSessionId, string part1AttemptId)
        {
            BeginPart2(salesSessionId, part1AttemptId);
        }

        public void StartPart2(string salesSessionId, string part1AttemptId)
        {
            BeginPart2(salesSessionId, part1AttemptId);
        }

        public void BeginPart2(string salesSessionId, string part1AttemptId)
        {
            if (Stage != SalesPart2Stage.Waiting && Stage != SalesPart2Stage.Incomplete)
                return;
            if (string.IsNullOrWhiteSpace(salesSessionId))
            {
                ShowIncomplete("Không thể bắt đầu tình huống vì thiếu mã phiên.");
                return;
            }

            enabled = true;
            if (requestService == null) requestService = GetComponent<SalesPart2RequestService>();
            if (hudRoot != null) hudRoot.SetActive(true);
            if (customer != null) customer.gameObject.SetActive(true);
            CancelActiveRequest();
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref recordingRoutine);
            StopRoutine(ref replyRoutine);
            StopRoutine(ref finalRoutine);
            StopAndReleaseMicrophone();
            SalesSessionId = salesSessionId.Trim();
            Part1AttemptId = part1AttemptId == null ? string.Empty : part1AttemptId.Trim();
            CurrentObjective = SalesPart2Objective.ListeningAndClarifying;
            TrustState = SalesPart2TrustState.Unknown;
            AcceptedTurnCount = 0;
            SilenceCount = 0;
            activeTimeSeconds = 0f;
            activeTimerRunning = false;
            challengeShown = false;
            finalCompletionRequested = false;
            completionReason = string.Empty;
            completionId = Guid.NewGuid().ToString("N");
            pendingTurn = null;
            CurrentTurnId = string.Empty;
            if (incompleteRoot != null) incompleteRoot.SetActive(false);
            SetPolicyUi();
            SetLocomotionLocked(true);
            Stage = SalesPart2Stage.Transition;
            Emit("sales.part2.started");

            if (requestService == null)
            {
                ShowIncomplete("Không tìm thấy kết nối máy chủ. Nhấn A để thử lại.");
                return;
            }

            SalesPart2StartSessionRequest request = new SalesPart2StartSessionRequest
            {
                salesSessionId = SalesSessionId,
                part1AttemptId = Part1AttemptId
            };
            BeginRequest(RequestKind.StartSession);
            int token = callbackGeneration;
            requestRoutine = StartCoroutine(requestService.StartOrResumeSession(request,
                response => OnSessionStarted(token, response)));
        }

        private void OnSessionStarted(int token, SalesPart2SessionResponse response)
        {
            if (!IsCurrentCallback(token, RequestKind.StartSession))
                return;
            EndRequest();
            if (response?.terminalError != null) { ShowTerminalError(response.terminalError); return; }
            if (response == null || (!response.success && string.IsNullOrWhiteSpace(response.sessionId)
                && string.IsNullOrWhiteSpace(response.salesSessionId)))
            {
                ShowIncomplete(response != null && !string.IsNullOrWhiteSpace(response.errorMessage)
                    ? response.errorMessage : "Không thể kết nối máy chủ. Hãy thử lại.");
                return;
            }
            string returnedId = !string.IsNullOrWhiteSpace(response.sessionId)
                ? response.sessionId : response.salesSessionId;
            if (!string.IsNullOrWhiteSpace(returnedId) && !string.Equals(returnedId, SalesSessionId,
                StringComparison.Ordinal))
                SalesSessionId = returnedId;
            presentationRoutine = StartCoroutine(PresentAfterSession());
        }

        private IEnumerator PresentAfterSession()
        {
            Stage = SalesPart2Stage.Transition;
            yield return FadeTo(1f);
            if (xrOrigin != null && playerAnchor != null)
                xrOrigin.SetPositionAndRotation(playerAnchor.position, playerAnchor.rotation);
            if (customer != null && customerStart != null)
                customer.SetPositionAndRotation(customerStart.position, customerStart.rotation);
            SetStatus("Ba ngày sau");
            yield return FadeTo(0f);
            yield return new WaitForSecondsRealtime(transitionLabelSeconds);
            SetStatus(string.Empty);

            Stage = SalesPart2Stage.CustomerApproach;
            SetAnimation(walkClip);
            if (customer != null)
            {
                Transform[] route = customerWaypoints ?? Array.Empty<Transform>();
                for (int index = 0; index < route.Length; index++)
                    yield return WalkTo(route[index]);
                if (customerStop != null)
                    yield return WalkTo(customerStop);
            }
            SetAnimation(idleClip);
            ShowOpeningComplaint();
            presentationRoutine = null;
        }

        private IEnumerator WalkTo(Transform target)
        {
            if (target == null || customer == null)
                yield break;
            while (Stage == SalesPart2Stage.CustomerApproach
                && Vector3.Distance(customer.position, target.position) > Mathf.Max(0f, customerStoppingDistance))
            {
                Vector3 delta = target.position - customer.position;
                if (delta.sqrMagnitude > .0001f)
                    customer.rotation = Quaternion.Slerp(customer.rotation,
                        Quaternion.LookRotation(Vector3.ProjectOnPlane(delta, Vector3.up), Vector3.up),
                        Mathf.Clamp01(Time.unscaledDeltaTime * 8f));
                customer.position = Vector3.MoveTowards(customer.position, target.position,
                    Mathf.Max(0f, customerWalkSpeed) * Time.unscaledDeltaTime);
                yield return null;
            }
            if (target != null && customer != null)
                customer.SetPositionAndRotation(target.position, target.rotation);
        }

        private void ShowOpeningComplaint()
        {
            if (scenario == null)
            {
                ShowIncomplete("Không thể tải tình huống khách hàng.");
                return;
            }
            Stage = SalesPart2Stage.CustomerReply;
            activeTimerRunning = true;
            activeTimeSeconds = 0f;
            ShowCustomerText(scenario.openingComplaint);
            SetObjective(CurrentObjective);
            SetStatus(string.Empty);
            Emit("sales.part2.opening_shown");
            BeginNewRecordingTurn();
        }

        private void BeginNewRecordingTurn()
        {
            failedRequestKind = RequestKind.None;
            if (finalCompletionRequested || AcceptedTurnCount >= Mathf.Max(1, maximumAcceptedTurns))
            {
                RequestFinalAnalysis(completionReason.Length == 0 ? "turn_limit" : completionReason);
                return;
            }
            activeTimerRunning = true;
            SetObjective(CurrentObjective);
            pendingTurn = null;
            CurrentTurnId = Guid.NewGuid().ToString("N");
            recordingRoutine = StartCoroutine(PrepareAndRecord());
        }

        private IEnumerator PrepareAndRecord()
        {
            StopAndReleaseMicrophone();
            Stage = SalesPart2Stage.StartingRecording;
            SetStatus("ĐANG GHI ÂM");
            SetRecordingIndicator(false);
            yield return RequestMicrophonePermission();
            if (Stage != SalesPart2Stage.StartingRecording)
                yield break;

            string[] devices = microphoneProvider != null ? microphoneProvider.Devices : null;
            if (devices == null || devices.Length == 0)
            {
                ShowIncomplete("Không tìm thấy micrô. Hãy kiểm tra thiết bị rồi thử lại.");
                yield break;
            }

            microphoneDevice = devices[0];
            recordingClip = microphoneProvider.Start(microphoneDevice, false,
                Mathf.CeilToInt(Mathf.Max(1f, turnDurationSeconds)) + 1, RecordingSampleRateHz);
            if (recordingClip == null)
            {
                ShowIncomplete("Không thể khởi động micrô. Hãy thử lại.");
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + Mathf.Max(.1f, microphoneStartupTimeout);
            while (microphoneProvider.GetPosition(microphoneDevice) <= 0
                && Time.realtimeSinceStartup < deadline && Stage == SalesPart2Stage.StartingRecording)
                yield return null;
            if (Stage != SalesPart2Stage.StartingRecording)
                yield break;
            if (microphoneProvider.GetPosition(microphoneDevice) <= 0)
            {
                StopAndReleaseMicrophone();
                ShowIncomplete("Micrô không nhận được âm thanh. Hãy thử lại.");
                yield break;
            }

            Stage = SalesPart2Stage.Recording;
            recordingDeadline = Time.realtimeSinceStartup + Mathf.Max(.1f, turnDurationSeconds);
            SetStatus("ĐANG GHI ÂM");
            SetRecordingIndicator(true);
            recordingRoutine = null;
        }

        private IEnumerator RequestMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                yield break;
            bool resolved = false;
            bool granted = false;
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { granted = true; resolved = true; };
            callbacks.PermissionDenied += _ => resolved = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => resolved = true;
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
            while (!resolved)
                yield return null;
            if (!granted)
                ShowIncomplete("Chưa được cấp quyền dùng micrô. Hãy cấp quyền rồi thử lại.");
#else
            yield return null;
#endif
        }

        public void HandleAction(int action)
        {
            if (action == -1 && Stage == SalesPart2Stage.Recording)
            {
                FinishRecording(true);
                return;
            }
            if ((action == -1 || action == -4) && Stage == SalesPart2Stage.Incomplete)
            {
                RetryCurrentOperation();
                return;
            }

        }

        public void SubmitEarly()
        {
            if (Stage == SalesPart2Stage.Recording)
                FinishRecording(true);
        }

        public void RetryCurrentOperation()
        {
            if (Stage != SalesPart2Stage.Incomplete)
                return;
            if (incompleteRoot != null) incompleteRoot.SetActive(false);
            if (retryTarget != null) retryTarget.gameObject.SetActive(false);
            if (failedRequestKind == RequestKind.Analysis) { RequestFinalAnalysis(completionReason); return; }
            if (failedRequestKind == RequestKind.ReadState) { ResumeSession(SalesSessionId); return; }
            if (pendingTurn != null)
            {
                pendingTurn.retryCount++;
                SubmitPendingTurn();
                return;
            }
            if (string.IsNullOrWhiteSpace(SalesSessionId))
                return;
            if (AcceptedTurnCount == 0 && activeTimeSeconds <= 0f)
            {
                BeginPart2(SalesSessionId, Part1AttemptId);
                return;
            }
            BeginNewRecordingTurn();
        }

        private void FinishRecording(bool endedEarly)
        {
            if (Stage != SalesPart2Stage.Recording || recordingClip == null
                || microphoneProvider == null || string.IsNullOrEmpty(microphoneDevice))
                return;
            SetRecordingIndicator(false);
            Stage = SalesPart2Stage.Transcribing;
            int frameCount = Mathf.Min(Mathf.Max(0, microphoneProvider.GetPosition(microphoneDevice)),
                recordingClip.samples);
            microphoneProvider.End(microphoneDevice);
            microphoneRecording = false;
            microphoneDevice = null;
            if (frameCount <= 0)
            {
                StopAndReleaseMicrophone();
                ShowIncomplete("Không ghi được âm thanh. Hãy thử lại.");
                return;
            }

            int channels = Mathf.Max(1, recordingClip.channels);
            float[] interleaved = new float[frameCount * channels];
            recordingClip.GetData(interleaved, 0);
            int sourceRate = recordingClip.frequency;
            UnityEngine.Object.Destroy(recordingClip);
            recordingClip = null;
            float[] mono = Pcm16WavEncoder.DownmixToMono(interleaved, channels);
            float[] normalized = SalesAudioResampler.ResampleMono(mono, sourceRate, RecordingSampleRateHz);
            byte[] wav = Pcm16WavEncoder.Encode(normalized, 1, RecordingSampleRateHz);

            pendingTurn = new SalesPart2TurnRequest
            {
                salesSessionId = SalesSessionId,
                turnId = CurrentTurnId,
                clientVersion = Application.version,
                retryCount = 0,
                audio = new SalesPart2AudioPayload
                {
                    mimeType = "audio/wav",
                    encoding = "pcm_s16le",
                    sampleRateHz = RecordingSampleRateHz,
                    channels = 1,
                    durationSeconds = normalized.Length / (float)RecordingSampleRateHz,
                    endedEarly = endedEarly,
                    dataBase64 = Convert.ToBase64String(wav)
                }
            };
            Emit("sales.part2.turn_submitted");
            SubmitPendingTurn();
        }

        private void SubmitPendingTurn()
        {
            if (pendingTurn == null || requestService == null)
            {
                ShowIncomplete("Không thể gửi bản ghi. Hãy thử lại.");
                return;
            }
            Stage = SalesPart2Stage.Transcribing;
            if (pendingTurn.retryCount > 0)
                Emit("sales.part2.turn_retry");
            SetStatus("ĐANG NHẬN DẠNG LỜI NÓI");
            BeginRequest(RequestKind.Turn);
            int token = callbackGeneration;
            requestRoutine = StartCoroutine(requestService.SubmitTurn(pendingTurn,
                response => OnTurnResponse(token, response)));
        }

        private void OnTurnResponse(int token, SalesPart2TurnResponse response)
        {
            if (!IsCurrentCallback(token, RequestKind.Turn))
                return;
            EndRequest();
            if (response != null)
            {
                sttProvider = SafeMetadata(response.sttProvider);
                sttModel = SafeMetadata(response.sttModel);
                sttVersion = SafeMetadata(response.sttVersion);
                llmProvider = SafeMetadata(response.llmProvider);
                llmModel = SafeMetadata(response.llmModel);
                llmVersion = SafeMetadata(response.llmVersion);
            }
            if (response?.terminalError != null) { ShowTerminalError(response.terminalError); return; }
            if (response == null || response.IsFailure)
            {
                EmitError("turn_processing_failed", response != null ? response.FailureMessage : string.Empty);
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
                return;
            }

            lastTranscript = response.transcript ?? string.Empty;
            lastCustomerReply = response.customerText ?? string.Empty;

            if (response.silent || string.Equals(response.status, "silent", StringComparison.Ordinal))
            {
                pendingTurn = null;
                SilenceCount = Mathf.Max(SilenceCount + 1, response.silenceCount);
                Emit("sales.part2.silent_turn");
                if (SilenceCount >= 2 || !string.IsNullOrWhiteSpace(response.deterministicEnding))
                {
                    ResolveDeterministicLost("second_silence");
                    return;
                }
                ShowCustomerResponse(string.IsNullOrWhiteSpace(response.customerText)
                    ? "Chị chưa nghe rõ em, em có thể nói lại bằng tiếng Việt được không?"
                    : response.customerText);
                return;
            }

            if (response.nonVietnamese
                || string.Equals(response.status, "nonVietnamese", StringComparison.OrdinalIgnoreCase))
            {
                pendingTurn = null;
                Emit("sales.part2.non_vietnamese");
                ShowCustomerResponse(string.IsNullOrWhiteSpace(response.customerText)
                    ? VietnameseOnlyRepeat : response.customerText);
                return;
            }

            if (!response.accepted && !string.Equals(response.status, "accepted", StringComparison.OrdinalIgnoreCase))
            {
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
                return;
            }

            SalesPart2Objective previousObjective = CurrentObjective;
            int returnedObjective = response.activeObjective;
            if (returnedObjective < 1 || returnedObjective > 4
                || returnedObjective < (int)previousObjective
                || returnedObjective > (int)previousObjective + 1
                || (returnedObjective == (int)previousObjective + 1 && !response.objectiveCompleted))
            {
                EmitError("invalid_phase_progression", string.Empty);
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
                return;
            }

            if (string.IsNullOrWhiteSpace(response.customerText)) { ShowIncomplete("Máy chủ trả về dữ liệu không hợp lệ. Nhấn A để thử lại."); return; }
            pendingTurn = null;
            AcceptedTurnCount = Mathf.Max(AcceptedTurnCount + (response.clarificationRequested ? 0 : 1),
                response.acceptedTurnCount);
            CurrentObjective = (SalesPart2Objective)returnedObjective;
            if (previousObjective == SalesPart2Objective.ProposeSuitableSolution
                && CurrentObjective == SalesPart2Objective.RebuildTrust && !challengeShown)
            {
                challengeShown = true;
                response.customerText = scenario != null ? scenario.mandatoryTrustChallenge : response.customerText;
            }
            Emit("sales.part2.turn_accepted");
            if (!string.IsNullOrWhiteSpace(response.deterministicEnding))
            {
                ResolveDeterministicLost(response.deterministicEnding);
                return;
            }

            if (AcceptedTurnCount >= Mathf.Max(1, maximumAcceptedTurns))
                completionReason = "turn_limit";
            if (response.conversationComplete)
                finalCompletionRequested = true;
            ShowCustomerResponse(response.customerText);
        }

        private void ShowCustomerResponse(string text)
        {
            Stage = SalesPart2Stage.CustomerResponding;
            SetStatus("KHÁCH HÀNG ĐANG TRẢ LỜI");
            ShowCustomerText(text);
            SetObjective(CurrentObjective);
            StopRoutine(ref replyRoutine);
            replyRoutine = StartCoroutine(ContinueAfterCustomerResponse());
        }

        private IEnumerator ContinueAfterCustomerResponse()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, customerReplyDisplaySeconds));
            replyRoutine = null;
            if (finalCompletionRequested || completionReason.Length > 0)
            {
                RequestFinalAnalysis(completionReason.Length == 0 ? "natural" : completionReason);
                yield break;
            }
            BeginNewRecordingTurn();
        }

        private void RequestEndForLimit(string reason)
        {
            if (finalCompletionRequested || Stage == SalesPart2Stage.FinalAnalysis
                || Stage == SalesPart2Stage.Completed || Stage == SalesPart2Stage.Incomplete)
                return;
            completionReason = reason;
            finalCompletionRequested = true;
            if (Stage == SalesPart2Stage.Recording)
                FinishRecording(false);
            else
            {
                StopRoutine(ref recordingRoutine);
                StopRoutine(ref replyRoutine);
                StopAndReleaseMicrophone();
                RequestFinalAnalysis(reason);
            }
        }

        private void RequestFinalAnalysis(string reason)
        {
            if (Stage == SalesPart2Stage.FinalAnalysis || Stage == SalesPart2Stage.Completed
                || Stage == SalesPart2Stage.Stopped)
                return;
            completionReason = string.IsNullOrWhiteSpace(reason) ? "natural" : reason;
            activeTimerRunning = false;
            SetRecordingIndicator(false);
            StopAndReleaseMicrophone();
            Stage = SalesPart2Stage.FinalAnalysis;
            SetStatus("KHÁCH HÀNG ĐANG TRẢ LỜI");
            Emit("sales.part2.analysis_started");
            if (requestService == null)
            {
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
                return;
            }
            SalesPart2AnalysisRequest request = new SalesPart2AnalysisRequest
            {
                salesSessionId = SalesSessionId,
                completionId = completionId,
                reason = completionReason,
                acceptedTurnCount = AcceptedTurnCount,
                activeDurationSeconds = activeTimeSeconds
            };
            BeginRequest(RequestKind.Analysis);
            int token = callbackGeneration;
            requestRoutine = StartCoroutine(requestService.CompleteAnalysis(request,
                response => OnAnalysisResponse(token, response)));
        }

        private void OnAnalysisResponse(int token, SalesPart2AnalysisResponse response)
        {
            if (!IsCurrentCallback(token, RequestKind.Analysis))
                return;
            EndRequest();
            if (response?.terminalError != null) { ShowTerminalError(response.terminalError); return; }
            failedRequestKind = RequestKind.Analysis;
            if (response == null || response.ParsedTrustState == SalesPart2TrustState.Unknown)
            {
                EmitError("analysis_failed", response != null ? response.errorMessage : string.Empty);
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
                return;
            }
            emotionalHandling = response.emotionalHandling;
            causeIdentification = response.causeIdentification;
            solutionSuitability = response.solutionSuitability;
            trustRebuilding = response.trustRebuilding;
            ResolveTrust(response.ParsedTrustState);
        }

        private void ResolveDeterministicLost(string reason)
        {
            completionReason = string.IsNullOrWhiteSpace(reason) ? "deterministic_lost" : reason;
            ResolveTrust(SalesPart2TrustState.Lost);
        }

        private void ResolveTrust(SalesPart2TrustState state)
        {
            if (state == SalesPart2TrustState.Unknown || Stage == SalesPart2Stage.Completed)
                return;
            TrustState = state;
            if (state == SalesPart2TrustState.Lost)
            {
                emotionalHandling = false;
                causeIdentification = false;
                solutionSuitability = false;
                trustRebuilding = false;
            }
            activeTimerRunning = false;
            finalCompletionRequested = true;
            StopAndReleaseMicrophone();
            StopRoutine(ref recordingRoutine);
            SetRecordingIndicator(false);
            Stage = SalesPart2Stage.CustomerReply;
            ShowCustomerText(scenario != null ? scenario.Ending(state) : string.Empty);
            SetStatus(string.Empty);
            SetObjective(SalesPart2Objective.RebuildTrust);
            TrustResolved?.Invoke(state);
            Emit("sales.part2.authored_ending");
            StopRoutine(ref finalRoutine);
            finalRoutine = StartCoroutine(ShowFinalHudAfterEnding());
        }

        private IEnumerator ShowFinalHudAfterEnding()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, endingDisplaySeconds));
            yield return FadeTo(1f);
            if (customerText != null) customerText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;
            if (timerText != null) timerText.text = string.Empty;
            if (objectiveText != null) objectiveText.text = string.Empty;
            string label = TrustLabel(TrustState);
            if (trustStateText != null) trustStateText.text = label;
            if (resultText != null) resultText.text = label;
            yield return FadeTo(0f);
            Stage = SalesPart2Stage.Completed;
            Emit("sales.part2.completed");
            Part2Finished?.Invoke();
            finalRoutine = null;
        }

        public void ResumeSession(string salesSessionId)
        {
            if (requestService == null || string.IsNullOrWhiteSpace(salesSessionId))
                return;
            CancelActiveRequest();
            StopAndReleaseMicrophone();
            StopRoutine(ref recordingRoutine);
            enabled = true;
            if (hudRoot != null) hudRoot.SetActive(true);
            SetLocomotionLocked(true);
            SalesSessionId = salesSessionId.Trim();
            if (string.IsNullOrEmpty(completionId)) completionId = Guid.NewGuid().ToString("N");
            BeginRequest(RequestKind.ReadState);
            int token = callbackGeneration;
            requestRoutine = StartCoroutine(requestService.ReadSessionState(
                new SalesPart2SessionStateRequest { salesSessionId = SalesSessionId },
                response => OnSessionState(token, response)));
        }

        private void OnSessionState(int token, SalesPart2SessionStateResponse response)
        {
            if (!IsCurrentCallback(token, RequestKind.ReadState))
                return;
            EndRequest();
            if (response?.terminalError != null) { ShowTerminalError(response.terminalError); return; }
            if (response == null || (!response.success && string.IsNullOrEmpty(response.sessionId)))
            {
                ShowIncomplete("Không thể khôi phục tình huống. Hãy thử lại.");
                return;
            }
            if (!string.IsNullOrEmpty(response.trustState)) { var final = new SalesPart2AnalysisResponse { trustState = response.trustState }; if (final.ParsedTrustState != SalesPart2TrustState.Unknown) { ResolveTrust(final.ParsedTrustState); return; } }
            if (response.status == "awaitingCompletion") { RequestFinalAnalysis("natural"); return; }
            AcceptedTurnCount = Mathf.Max(0, response.acceptedTurnCount);
            SilenceCount = Mathf.Max(0, response.silenceCount);
            if (response.activeObjective >= 1 && response.activeObjective <= 4)
                CurrentObjective = (SalesPart2Objective)response.activeObjective;
            if (!string.IsNullOrWhiteSpace(response.lastCustomerText))
                ShowCustomerResponse(response.lastCustomerText);
            else
                ShowOpeningComplaint();
        }

        private void HandleRequestTimeout()
        {
            RequestKind timedOut = requestKind;
            failedRequestKind = requestKind;
            CancelActiveRequest();
            EmitError("request_timeout", string.Empty);
            if (timedOut == RequestKind.Turn && pendingTurn != null)
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
            else
                ShowIncomplete("Không thể hoàn thành tình huống. Hãy thử lại.");
        }

        private void ShowTerminalError(SalesPart2Error error)
        {
            callbackGeneration++;
            CancelActiveRequest();
            activeTimerRunning = false;
            finalCompletionRequested = true;
            pendingTurn = null;
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref recordingRoutine);
            StopRoutine(ref replyRoutine);
            StopRoutine(ref finalRoutine);
            SetRecordingIndicator(false);
            StopAndReleaseMicrophone();
            TrustState = SalesPart2TrustState.Unknown;
            Stage = SalesPart2Stage.Failed;
            if (incompleteRoot != null) incompleteRoot.SetActive(true);
            if (retryTarget != null) retryTarget.gameObject.SetActive(false);
            SetStatus("PHIÊN ĐÃ KẾT THÚC");
            if (customerText != null) customerText.text = string.Empty;
            if (trustStateText != null) trustStateText.text = string.Empty;
            if (timerText != null) timerText.text = string.Empty;
            if (objectiveText != null) objectiveText.text = string.Empty;
            if (resultText != null) resultText.text = string.IsNullOrWhiteSpace(error.message)
                ? "Không thể đánh giá phiên vì lời nói không rõ. Vui lòng kiểm tra mic và bắt đầu phiên mới."
                : error.message;
            SetLocomotionLocked(false);
            EmitError(error.code, error.message);
            Emit("sales.part2.failed");
        }

        private void ShowIncomplete(string message)
        {
            activeTimerRunning = false;
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref recordingRoutine);
            StopRoutine(ref replyRoutine);
            SetRecordingIndicator(false);
            StopAndReleaseMicrophone();
            Stage = SalesPart2Stage.Incomplete;
            if (incompleteRoot != null) incompleteRoot.SetActive(true);
            SetStatus("KHÔNG THỂ HOÀN THÀNH TÌNH HUỐNG\nNhấn A để thử lại");
            if (customerText != null) customerText.text = string.Empty;
            if (resultText != null) resultText.text = message ?? string.Empty;
            if (retryTarget != null) retryTarget.gameObject.SetActive(true);
            Emit("sales.part2.incomplete");
        }

        public void StopPart2()
        {
            callbackGeneration++;
            CancelActiveRequest();
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref recordingRoutine);
            StopRoutine(ref replyRoutine);
            StopRoutine(ref finalRoutine);
            StopAndReleaseMicrophone();
            activeTimerRunning = false;
            Stage = SalesPart2Stage.Stopped;
            pendingTurn = null;
            if (animationGraph.IsValid()) animationGraph.Destroy();
            if (fadeCanvas != null) fadeCanvas.alpha = 0f;
            SetRecordingIndicator(false);
            SetLocomotionLocked(false);
            Emit("sales.part2.stopped");
        }

        public void ResetPart2()
        {
            StopPart2();
            Stage = SalesPart2Stage.Waiting;
            SalesSessionId = string.Empty;
            Part1AttemptId = string.Empty;
            CurrentTurnId = string.Empty;
            AcceptedTurnCount = 0;
            SilenceCount = 0;
            lastTranscript = string.Empty;
            lastCustomerReply = string.Empty;
            emotionalHandling = false;
            causeIdentification = false;
            solutionSuitability = false;
            trustRebuilding = false;
            TrustState = SalesPart2TrustState.Unknown;
            CurrentObjective = SalesPart2Objective.ListeningAndClarifying;
            activeTimeSeconds = 0f;
            completionReason = string.Empty;
            finalCompletionRequested = false;
            challengeShown = false;
            if (incompleteRoot != null) incompleteRoot.SetActive(false);
            if (retryTarget != null) retryTarget.gameObject.SetActive(false);
            HideResultUi();
            if (hudRoot != null) hudRoot.SetActive(false);
            if (customer != null) customer.gameObject.SetActive(false);
        }

        private void BeginRequest(RequestKind kind)
        {
            requestInFlight = true;
            requestKind = kind;
            failedRequestKind = kind;
            requestStartedAt = Time.realtimeSinceStartup;
            callbackGeneration++;
        }

        private bool IsCurrentCallback(int token, RequestKind kind)
        {
            return requestInFlight && callbackGeneration == token && requestKind == kind;
        }

        private void EndRequest()
        {
            requestInFlight = false;
            requestKind = RequestKind.None;
            requestRoutine = null;
        }

        private void CancelActiveRequest()
        {
            callbackGeneration++;
            if (requestRoutine != null) StopCoroutine(requestRoutine);
            if (requestService is ISalesPart2RequestCancellation cancellation) cancellation.CancelAll();
            requestRoutine = null;
            requestInFlight = false;
            requestKind = RequestKind.None;
        }

        private void ShowCustomerText(string text)
        {
            if (customerText != null)
                customerText.text = text ?? string.Empty;
        }

        private void SetObjective(SalesPart2Objective objective)
        {
            if (objectiveText != null)
                objectiveText.text = scenario != null ? scenario.ObjectiveText(objective) : objective.ToString();
        }

        private void SetPolicyUi()
        {
            if (policyText != null)
                policyText.text = scenario != null && scenario.storePolicy != null
                    ? scenario.storePolicy.DisplayText : string.Empty;
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text ?? string.Empty;
        }

        private void UpdateTimerUi()
        {
            if (timerText != null)
                timerText.text = Stage == SalesPart2Stage.Recording
                    ? "Lượt: " + Mathf.CeilToInt(Mathf.Max(0f, recordingDeadline - Time.realtimeSinceStartup)) + " giây"
                    : "Còn: " + Mathf.CeilToInt(Mathf.Max(0f, activeTimeLimitSeconds - activeTimeSeconds)) + " giây";
        }

        private void SetRecordingIndicator(bool value)
        {
            if (recordingIndicator != null && recordingIndicator.activeSelf != value)
                recordingIndicator.SetActive(value);
        }

        private void HideResultUi()
        {
            if (trustStateText != null) trustStateText.text = string.Empty;
            if (resultText != null) resultText.text = string.Empty;
            if (recordingIndicator != null) recordingIndicator.SetActive(false);
            if (incompleteRoot != null) incompleteRoot.SetActive(false);
        }

        private static string TrustLabel(SalesPart2TrustState state)
        {
            switch (state)
            {
                case SalesPart2TrustState.Restored: return "ĐÃ KHÔI PHỤC NIỀM TIN";
                case SalesPart2TrustState.PartiallyRestored:
                    return "ĐÃ GIẢI QUYẾT, NHƯNG CHƯA KHÔI PHỤC HOÀN TOÀN NIỀM TIN";
                case SalesPart2TrustState.Lost: return "ĐÃ MẤT NIỀM TIN";
                default: return string.Empty;
            }
        }

        private IEnumerator FadeTo(float target)
        {
            if (fadeCanvas == null)
                yield break;
            float start = fadeCanvas.alpha;
            float duration = Mathf.Max(0f, fadeDuration);
            if (duration <= 0f)
            {
                fadeCanvas.alpha = target;
                yield break;
            }
            float elapsed = 0f;
            fadeCanvas.blocksRaycasts = true;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvas.alpha = Mathf.Lerp(start, target,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            fadeCanvas.alpha = target;
            fadeCanvas.blocksRaycasts = target > .001f;
        }

        private void SetLocomotionLocked(bool value)
        {
            if (locomotionLocked == value)
                return;
            if (previousLocomotion == null || previousLocomotion.Length != (locomotion != null ? locomotion.Length : 0))
                previousLocomotion = new bool[locomotion != null ? locomotion.Length : 0];
            if (locomotion != null)
            {
                for (int index = 0; index < locomotion.Length; index++)
                {
                    if (locomotion[index] == null) continue;
                    if (value)
                    {
                        previousLocomotion[index] = locomotion[index].enabled;
                        locomotion[index].enabled = false;
                    }
                    else locomotion[index].enabled = previousLocomotion[index];
                }
            }
            locomotionLocked = value;
        }

        private void StopAndReleaseMicrophone()
        {
            SetRecordingIndicator(false);
            if (microphoneProvider != null && !string.IsNullOrEmpty(microphoneDevice))
                microphoneProvider.End(microphoneDevice);
            microphoneRecording = false;
            microphoneDevice = null;
            if (recordingClip != null)
            {
                UnityEngine.Object.Destroy(recordingClip);
                recordingClip = null;
            }
        }

        private void StopRoutine(ref Coroutine routine)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
        }

        private void SetAnimation(AnimationClip clip)
        {
            if (customerAnimator == null || !customerAnimator.isHuman || clip == null)
                return;
            if (animationGraph.IsValid())
                animationGraph.Destroy();
            customerAnimator.applyRootMotion = false;
            animationGraph = PlayableGraph.Create("Sales Part 2 Customer Motion");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            animationPlayable = AnimationClipPlayable.Create(animationGraph, clip);
            animationPlayable.SetApplyFootIK(false);
            animationPlayable.SetApplyPlayableIK(false);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Customer Animation",
                customerAnimator);
            output.SetSourcePlayable(animationPlayable);
            animationGraph.Play();
            headBone = customerAnimator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void Emit(string eventType)
        {
            TelemetryRaised?.Invoke(eventType, CurrentTelemetry());
        }

        private void EmitError(string errorCode, string message)
        {
            GameplayTelemetryData data = CurrentTelemetry();
            data.errorCode = errorCode ?? "processing_failed";
            data.errorMessage = "Xử lý yêu cầu thất bại.";
            TelemetryRaised?.Invoke("sales.part2.error", data);
        }

        private static string SafeMetadata(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 100 || value.IndexOfAny(new[] { '/', '\\', ':', '\n', '\r' }) >= 0) return "unavailable";
            return value;
        }

        private GameplayTelemetryData CurrentTelemetry()
        {
            return new GameplayTelemetryData
            {
                salesSessionId = SalesSessionId,
                roundId = CurrentTurnId,
                caseId = scenario != null && scenario.customerProfile != null
                    ? scenario.customerProfile.name : "Lan",
                category = Stage.ToString(),
                trustState = TrustState == SalesPart2TrustState.Restored ? "restored" : TrustState == SalesPart2TrustState.PartiallyRestored ? "partially_restored" : TrustState == SalesPart2TrustState.Lost ? "lost" : string.Empty,
                emotionalHandling = emotionalHandling,
                causeIdentification = causeIdentification,
                solutionSuitability = solutionSuitability,
                trustRebuilding = trustRebuilding,
                completionReason = completionReason,
                transcript = lastTranscript,
                customerReply = lastCustomerReply,
                objective = CurrentObjective.ToString(),
                acceptedTurnCount = AcceptedTurnCount,
                silenceCount = SilenceCount,
                questionId = ((int)CurrentObjective).ToString(),
                resolution = JsonUtility.ToJson(new DiagnosticData
                {
                    salesSessionId = SalesSessionId, turnId = CurrentTurnId,
                    clientVersion = Application.version + " / Unity " + Application.unityVersion,
                    activeObjective = (int)CurrentObjective, retryCount = pendingTurn != null ? pendingTurn.retryCount : 0,
                    acceptedTurnCount = AcceptedTurnCount, silenceCount = SilenceCount,
                    activeDurationSeconds = activeTimeSeconds, processingDurationSeconds = requestInFlight ? Time.realtimeSinceStartup - requestStartedAt : 0,
                    sttProvider = sttProvider, sttModel = sttModel, sttVersion = sttVersion,
                    llmProvider = llmProvider, llmModel = llmModel, llmVersion = llmVersion,
                    trustState = TrustState == SalesPart2TrustState.Restored ? "restored" : TrustState == SalesPart2TrustState.PartiallyRestored ? "partially_restored" : TrustState == SalesPart2TrustState.Lost ? "lost" : string.Empty
                }),
                remainingTime = Mathf.Max(0f, activeTimeLimitSeconds - activeTimeSeconds)
            };
        }

        private void OnApplicationFocus(bool focused)
        {
#if !UNITY_EDITOR
            if (!focused) OnApplicationPause(true);
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                return;
            if (Stage == SalesPart2Stage.Waiting || Stage == SalesPart2Stage.Stopped || IsFinished) return;
            if (requestInFlight) failedRequestKind = requestKind;
            CancelActiveRequest();
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref replyRoutine);
            StopRoutine(ref finalRoutine);
            ShowIncomplete("Tình huống bị gián đoạn khi kính tạm dừng. Nhấn A để thử lại.");
        }

        private void OnDisable()
        {
            callbackGeneration++;
            CancelActiveRequest();
            StopRoutine(ref presentationRoutine);
            StopRoutine(ref recordingRoutine);
            StopRoutine(ref replyRoutine);
            StopRoutine(ref finalRoutine);
            StopAndReleaseMicrophone();
            if (submitAction != null)
            {
                submitAction.performed -= OnSubmitPerformed;
                submitAction.Disable();
            }
            if (animationGraph.IsValid())
                animationGraph.Destroy();
            SetLocomotionLocked(false);
        }

        private void OnDestroy()
        {
            if (animationGraph.IsValid())
                animationGraph.Destroy();
        }
    }
}
