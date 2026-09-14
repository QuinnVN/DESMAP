using System;
using System.Collections;
using NCKH.VR.GameControl;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerDefenseController : MonoBehaviour
    {
        private const string TelemetryEventType = "lawyer.defense_recording";
        private const int RecordingSampleRateHz = 16000;
        private string[] devices;

        private enum DefenseView
        {
            Inactive,
            AwaitingPermission,
            Preparing,
            StartingMicrophone,
            Recording,
            Encoding,
            Uploading,
            Processing,
            MicrophoneFailed,
            UploadFailed,
            Completed
        }

        [Header("Data")]
        [SerializeField] private LawyerCaseDefinition caseDefinition;
        [SerializeField] private InputActionAsset controls;

        [Header("Courtroom Modes")]
        [SerializeField] private LawyerInterviewController interviewController;
        [SerializeField] private GameObject interviewHud;
        [SerializeField] private GameObject defenseHud;

        [Header("Defense HUD")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text countdownLabel;
        [SerializeField] private TMP_Text instructionLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text truthLabel;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float preparationDuration = 5f;
        [SerializeField, Min(1f)] private float recordingDuration = 60f;
        [SerializeField, Min(1)] private int uploadAttempts = 5;
        [SerializeField, Min(0.1f)] private float microphoneStartupTimeout = 2f;
        [SerializeField, Min(0.5f)] private float assessmentPollInterval = 2f;

        private LawyerGameSession session;
        private InputAction stopDefenseAction;
        private AudioClip recordingClip;
        private Coroutine activeRoutine;
        private DefenseView view;
        private GameplayTelemetryData pendingTelemetry;
        private bool defenseMode;
        private bool applicationPaused;

        public LawyerGameSession Session => session;
        public bool IsDefenseMode => defenseMode;

#if UNITY_EDITOR
        public void ConfigureInEditor(
            LawyerCaseDefinition definition,
            InputActionAsset inputControls,
            LawyerInterviewController interview,
            GameObject conversationHud,
            GameObject recordingHud)
        {
            caseDefinition = definition;
            controls = inputControls;
            interviewController = interview;
            interviewHud = conversationHud;
            defenseHud = recordingHud;
        }
#endif

        private void Awake()
        {
            session = LawyerGameSession.Ensure(caseDefinition);
            stopDefenseAction = controls != null
                ? controls.FindAction("StopDefense", false)
                : null;
            defenseMode = IsDefensePhase(session.Phase);
            if (defenseHud != null)
                defenseHud.SetActive(defenseMode);
            if (!defenseMode)
                return;

            if (interviewController != null)
                interviewController.enabled = false;
            if (interviewHud != null)
                interviewHud.SetActive(false);

            devices = Microphone.devices;
        }

        private void OnEnable()
        {
            if (!defenseMode || stopDefenseAction == null)
                return;
            stopDefenseAction.performed += OnStopDefensePerformed;
            stopDefenseAction.Enable();
        }

        private void Start()
        {
            if (!defenseMode)
                return;
            if (session.Phase == LawyerSessionPhase.Completed)
            {
                ShowCompleted();
                return;
            }
            if (session.Phase != LawyerSessionPhase.DefensePreparation)
                ShowMicrophoneFailure("Không thể khôi phục phần ghi âm. Nhấn A để chuẩn bị lại.");
            else
                BeginMicrophonePreparation();
        }

        private void OnDisable()
        {
            if (stopDefenseAction != null)
            {
                stopDefenseAction.performed -= OnStopDefensePerformed;
                stopDefenseAction.Disable();
            }
            StopActiveRoutine();
            StopAndReleaseMicrophone();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            if (!paused || !defenseMode)
                return;
            if (view != DefenseView.Preparing
                && view != DefenseView.StartingMicrophone
                && view != DefenseView.Recording)
                return;

            StopActiveRoutine();
            StopAndReleaseMicrophone();
            session.TryReturnToDefensePreparation();
            ShowMicrophoneFailure(
                "Ghi âm đã bị gián đoạn khi kính tạm dừng. Nhấn A để thử lại.");
        }

        private void BeginMicrophonePreparation()
        {
            if (applicationPaused || view == DefenseView.AwaitingPermission
                || view == DefenseView.Preparing || view == DefenseView.StartingMicrophone)
                return;
            if (session.Phase == LawyerSessionPhase.DefenseRecording)
                session.TryReturnToDefensePreparation();
            if (session.Phase != LawyerSessionPhase.DefensePreparation)
            {
                ShowMicrophoneFailure("Không thể bắt đầu chuẩn bị ghi âm. Nhấn A để thử lại.");
                return;
            }

            StopActiveRoutine();
            activeRoutine = StartCoroutine(PrepareAndRecord());
        }

        private IEnumerator PrepareAndRecord()
        {
            view = DefenseView.AwaitingPermission;
            SetHud("CHUẨN BỊ BÀO CHỮA", string.Empty,
                "Đang kiểm tra quyền sử dụng micrô...", string.Empty);
            yield return RequestMicrophonePermission();
            if (view == DefenseView.MicrophoneFailed)
                yield break;
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                ShowMicrophoneFailure(
                    "Không tìm thấy micrô của kính. Hãy kiểm tra thiết bị rồi nhấn A để thử lại.");
                yield break;
            }

            view = DefenseView.Preparing;
            SetHud("CHUẨN BỊ BÀO CHỮA", Mathf.CeilToInt(preparationDuration).ToString(),
                "Hãy chuẩn bị trình bày lời bào chữa cho bị cáo.", string.Empty);
            float endTime = Time.realtimeSinceStartup + preparationDuration;
            int lastSecond = -1;
            while (Time.realtimeSinceStartup < endTime)
            {
                int seconds = Mathf.Max(1, Mathf.CeilToInt(endTime - Time.realtimeSinceStartup));
                if (seconds != lastSecond)
                {
                    countdownLabel?.SetText("{0}", seconds);
                    lastSecond = seconds;
                }
                yield return null;
            }

            yield return StartMicrophoneAndRecord();
        }

        private IEnumerator RequestMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                yield break;

            bool resolved = false;
            bool granted = false;
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ =>
            {
                granted = true;
                resolved = true;
            };
            callbacks.PermissionDenied += _ => resolved = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => resolved = true;
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
            while (!resolved)
                yield return null;
            if (!granted)
                ShowMicrophoneFailure(
                    "Chưa được cấp quyền dùng micrô. Hãy cấp quyền rồi nhấn A để thử lại.");
#else
            yield return null;
#endif
        }

        private IEnumerator StartMicrophoneAndRecord()
        {
            view = DefenseView.StartingMicrophone;
            SetHud("CHUẨN BỊ BÀO CHỮA", string.Empty,
                "Đang khởi động micrô...", string.Empty);
            
            //temp
            foreach (string device in devices) Debug.Log($"Microphone: {device}");
            
            recordingClip = Microphone.Start(
                devices[0], false, Mathf.CeilToInt(recordingDuration) + 1,
                RecordingSampleRateHz);
            
            Microphone.GetDeviceCaps(devices[0], out int minFreq, out int maxFreq);
            Debug.Log($"Microphone: {devices[0]} (minFreq={minFreq}, maxFreq={maxFreq})");

            if (recordingClip == null)
            {
                ShowMicrophoneFailure(
                    "Không thể khởi động micrô. Nhấn A để thử lại.");
                yield break;
            }

            float startupDeadline = Time.realtimeSinceStartup + microphoneStartupTimeout;
            while (Microphone.GetPosition(devices[0]) <= 0
                && Time.realtimeSinceStartup < startupDeadline)
                yield return null;
            if (Microphone.GetPosition(devices[0]) <= 0)
            {
                StopAndReleaseMicrophone();
                ShowMicrophoneFailure(
                    "Micrô không nhận được âm thanh. Nhấn A để thử lại.");
                yield break;
            }
            if (!session.TryBeginDefenseRecording())
            {
                StopAndReleaseMicrophone();
                ShowMicrophoneFailure(
                    "Không thể bắt đầu phần bào chữa. Nhấn A để thử lại.");
                yield break;
            }

            view = DefenseView.Recording;
            SetHud("ĐANG GHI ÂM", Mathf.CeilToInt(recordingDuration).ToString(),
                "Hãy trình bày lời bào chữa cho bị cáo.",
                "Nhấn A để kết thúc sớm");
            float endTime = Time.realtimeSinceStartup + recordingDuration;
            int lastSecond = -1;
            while (view == DefenseView.Recording && Time.realtimeSinceStartup < endTime)
            {
                int seconds = Mathf.Max(0, Mathf.CeilToInt(endTime - Time.realtimeSinceStartup));
                if (seconds != lastSecond)
                {
                    countdownLabel?.SetText("{0}", seconds);
                    lastSecond = seconds;
                }
                yield return null;
            }
            if (view == DefenseView.Recording)
                FinishRecording(false);
        }

        private void FinishRecording(bool endedEarly)
        {
            if (view != DefenseView.Recording || recordingClip == null)
                return;
            view = DefenseView.Encoding;
            int frameCount = Microphone.GetPosition(devices[0]);
            Microphone.End(devices[0]);
            if (frameCount <= 0)
            {
                StopAndReleaseMicrophone();
                session.TryReturnToDefensePreparation();
                ShowMicrophoneFailure(
                    "Không ghi được âm thanh. Nhấn A để thử lại.");
                return;
            }

            int sourceChannels = Mathf.Max(1, recordingClip.channels);
            float[] interleaved = new float[frameCount * sourceChannels];
            recordingClip.GetData(interleaved, 0);
            int actualSampleRate = recordingClip.frequency;
            UnityEngine.Object.Destroy(recordingClip);
            recordingClip = null;

            float[] mono = Pcm16WavEncoder.DownmixToMono(interleaved, sourceChannels);
            byte[] wav = Pcm16WavEncoder.Encode(mono, 1, actualSampleRate);
            float actualDuration = frameCount / (float)actualSampleRate;
            pendingTelemetry = new GameplayTelemetryData
            {
                roundId = session.RoundId,
                caseId = caseDefinition != null ? caseDefinition.CaseId : string.Empty,
                category = LawyerSessionPhase.DefenseUploading.ToString(),
                interviewRestartCount = session.InterviewRestartCount,
                assessmentContextJson = LawyerAssessmentContextPayload.BuildJson(caseDefinition, session),
                audio = new AudioTelemetryData
                {
                    fileName = $"lawyer-defense-{session.RoundId}.wav",
                    mimeType = "audio/wav",
                    encoding = "pcm_s16le",
                    sampleRateHz = actualSampleRate,
                    channels = 1,
                    durationSeconds = actualDuration,
                    endedEarly = endedEarly,
                    dataBase64 = Convert.ToBase64String(wav)
                }
            };

            if (!session.TryBeginDefenseUpload())
            {
                pendingTelemetry = null;
                ShowUploadFailure("Không thể chuẩn bị gửi bài bào chữa. Nhấn A để thử lại.");
                return;
            }
            BeginUploadCycle();
        }

        private void BeginUploadCycle()
        {
            if (pendingTelemetry?.audio == null || view == DefenseView.Uploading)
                return;
            view = DefenseView.Uploading;
            SetHud("ĐANG GỬI", string.Empty,
                "Đang gửi bài bào chữa...", string.Empty);
            GameController controller = GameController.Instance;
            if (controller == null)
            {
                ShowUploadFailure(
                    "Không tìm thấy kết nối máy chủ. Nhấn A để thử lại.");
                return;
            }

            controller.PublishTelemetryReliable(
                TelemetryEventType, pendingTelemetry, uploadAttempts, OnUploadCompleted);
        }

        private void OnUploadCompleted(TelemetryDeliveryResult result)
        {
            if (view != DefenseView.Uploading)
                return;
            if (!result.Success)
            {
                ShowUploadFailure(
                    "Không thể gửi bài bào chữa. Nhấn A để thử lại.");
                return;
            }

            StopActiveRoutine();
            activeRoutine = StartCoroutine(PollAssessmentStatus());
        }

        private IEnumerator PollAssessmentStatus()
        {
            view = DefenseView.Processing;
            SetHud("ĐANG PHÂN TÍCH", string.Empty,
                "Máy chủ đang đánh giá bài bào chữa...", string.Empty);
            while (view == DefenseView.Processing)
            {
                GameController controller = GameController.Instance;
                if (controller == null)
                {
                    ShowUploadFailure("Không tìm thấy kết nối máy chủ. Nhấn A để thử lại.");
                    yield break;
                }
                string route = $"api/lawyer/defense-recordings/{session.RoundId}";
                using (UnityWebRequest request = UnityWebRequest.Get(
                    GameControlApi.CombineUrl(controller.BackendBaseUrl, route)))
                {
                    request.timeout = 15;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        ShowUploadFailure("Chưa nhận được kết quả đánh giá. Nhấn A để thử lại.");
                        yield break;
                    }
                    LawyerAssessmentStatusResponse status =
                        JsonUtility.FromJson<LawyerAssessmentStatusResponse>(request.downloadHandler.text);
                    if (status != null && status.assessmentStatus == "completed")
                    {
                        GameplayTelemetryData completedData = pendingTelemetry == null
                            ? new GameplayTelemetryData()
                            : JsonUtility.FromJson<GameplayTelemetryData>(
                                JsonUtility.ToJson(pendingTelemetry));
                        completedData.audio = null;
                        completedData.fragmentId = session.RoundId + ":defense";
                        controller.PublishResultFragmentReliable(
                            "lawyer.defense_completed", completedData, 3, null);
                        if (pendingTelemetry?.audio != null)
                            pendingTelemetry.audio.dataBase64 = string.Empty;
                        pendingTelemetry = null;
                        session.TryCompleteDefense();
                        ShowCompleted();
                        yield break;
                    }
                    if (status != null && status.assessmentStatus == "failed")
                    {
                        ShowUploadFailure("Máy chủ chưa đánh giá được bài bào chữa. Nhấn A để thử lại.");
                        yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(assessmentPollInterval);
            }
        }

        private void ShowMicrophoneFailure(string message)
        {
            view = DefenseView.MicrophoneFailed;
            SetHud("LỖI MICRÔ", string.Empty, message, "Nhấn A để thử lại");
        }

        private void ShowUploadFailure(string message)
        {
            view = DefenseView.UploadFailed;
            SetHud("CHƯA GỬI ĐƯỢC", string.Empty, message, "Nhấn A để thử lại");
        }

        private void ShowCompleted()
        {
            view = DefenseView.Completed;
            SetHud("HOÀN TẤT", string.Empty,
                "Đã gửi bài bào chữa.", string.Empty);
        }

        private void OnStopDefensePerformed(InputAction.CallbackContext _)
        {
            if (view == DefenseView.Recording)
                FinishRecording(true);
            else if (view == DefenseView.MicrophoneFailed)
                BeginMicrophonePreparation();
            else if (view == DefenseView.UploadFailed)
                BeginUploadCycle();
        }

        private void SetHud(string title, string countdown, string instruction, string status)
        {
            if (titleLabel != null)
                titleLabel.text = title;
            if (countdownLabel != null)
                countdownLabel.text = countdown;
            if (instructionLabel != null)
                instructionLabel.text = instruction;
            if (statusLabel != null)
                statusLabel.text = status;
            if (truthLabel != null)
            {
                truthLabel.gameObject.SetActive(view == DefenseView.Recording);
                truthLabel.text = view == DefenseView.Recording
                    ? GetDefenseTruth()
                    : string.Empty;
            }
        }

        private string GetDefenseTruth()
        {
            return caseDefinition != null && caseDefinition.Resolution != null
                ? caseDefinition.Resolution.TruthConclusion
                : string.Empty;
        }

        private void StopActiveRoutine()
        {
            if (activeRoutine == null)
                return;
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        private void StopAndReleaseMicrophone()
        {
            if (Microphone.IsRecording(null))
                Microphone.End(null);
            if (recordingClip == null)
                return;
            UnityEngine.Object.Destroy(recordingClip);
            recordingClip = null;
        }

        private static bool IsDefensePhase(LawyerSessionPhase phase)
        {
            return phase == LawyerSessionPhase.DefensePreparation
                || phase == LawyerSessionPhase.DefenseRecording
                || phase == LawyerSessionPhase.DefenseUploading
                || phase == LawyerSessionPhase.Completed;
        }

        [Serializable]
        private sealed class LawyerAssessmentStatusResponse
        {
            public string roundId;
            public string assessmentStatus;
            public bool retryable;
            public string errorCode;
        }
    }
}
