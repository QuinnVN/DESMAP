using System;
using System.Collections;
using NCKH.VR.GameControl;
using NCKH.VR.Lawyer;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace NCKH.VR.Sales
{
    public enum SalesMissionStage
    {
        Waiting,
        Dialogue,
        Transition,
        Choosing,
        PersuasionObjection,
        PersuasionPreparation,
        PersuasionRecording,
        PersuasionEncoding,
        PersuasionUploading,
        PersuasionMicrophoneFailed,
        PersuasionUploadFailed,
        Completed
    }

    public interface ISalesMicrophone
    {
        string[] Devices { get; }
        AudioClip Start(string deviceName, bool loop, int lengthSeconds, int frequency);
        int GetPosition(string deviceName);
        void End(string deviceName);
    }

    public sealed class UnitySalesMicrophone : ISalesMicrophone
    {
        public string[] Devices => Microphone.devices;
        public AudioClip Start(string deviceName, bool loop, int lengthSeconds, int frequency)
            => Microphone.Start(deviceName, loop, lengthSeconds, frequency);
        public int GetPosition(string deviceName) => Microphone.GetPosition(deviceName);
        public void End(string deviceName) => Microphone.End(deviceName);
    }

    [Serializable]
    public sealed class SalesPersuasionShoe
    {
        public string shoeId;
        public string name;
        public int price;
        public string details;
    }

    [Serializable]
    public sealed class SalesPersuasionContext
    {
        public string customerName;
        public string selectedShoe;
        public string bestFitShoe;
        public string customerNeeds;
        public string objective;
        public string objection;
        public SalesPersuasionShoe[] availableShoes;
    }

    public static class SalesAudioResampler
    {
        public static float[] ResampleMono(float[] source, int sourceRate, int targetRate)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sourceRate <= 0) throw new ArgumentOutOfRangeException(nameof(sourceRate));
            if (targetRate <= 0) throw new ArgumentOutOfRangeException(nameof(targetRate));
            if (source.Length == 0 || sourceRate == targetRate) return source;
            int targetLength = Mathf.Max(1, Mathf.RoundToInt(source.Length * (targetRate / (float)sourceRate)));
            float[] result = new float[targetLength];
            float scale = sourceRate / (float)targetRate;
            for (int index = 0; index < targetLength; index++)
            {
                float sourcePosition = index * scale;
                int first = Mathf.Clamp(Mathf.FloorToInt(sourcePosition), 0, source.Length - 1);
                int second = Mathf.Min(first + 1, source.Length - 1);
                result[index] = Mathf.Lerp(source[first], source[second], sourcePosition - first);
            }
            return result;
        }
    }

    public sealed class SalesMissionController : MonoBehaviour
    {
        private const string TelemetryEventType = "sales.persuasion_recording";
        private const int RecordingSampleRateHz = 16000;

        public SalesMissionData data;
        public Camera playerCamera;
        public Transform customer;
        public AudioSource voice;
        public Transform dialogueRoot;
        public Transform displayRoot;
        public GameObject choicesRoot;
        public GameObject resultRoot;

        public GameObject persuasionRoot;
        public GameObject recordingIndicator;
        public TMP_Text dialogueText;
        public TMP_Text speakerText;
        public TMP_Text nextText;
        public TMP_Text objectiveText;
        public TMP_Text resultText;
        public TMP_Text persuasionTitleText;
        public TMP_Text persuasionCountdownText;
        public TMP_Text persuasionInstructionText;
        public TMP_Text persuasionStatusText;
        public SalesActionTarget retryUploadTarget;
        public SalesActionTarget restartTarget;
        public GameObject customerMarker;
        public LawyerBoardFocusLighting focus;
        public Behaviour[] locomotion = Array.Empty<Behaviour>();
        public float conversationDistance = 2.7f;
        public float transitionDuration = .4f;
        public float preparationDuration = 10f;
        public float recordingDuration = 60f;
        public int uploadAttempts = 5;

        public float microphoneStartupTimeout = 2f;
        public float recordingBlinkInterval = .45f;

        // Tests can supply deterministic recording and upload services without hardware/network.
        public ISalesMicrophone microphoneProvider;
        public Action<string, GameplayTelemetryData, int, Action<TelemetryDeliveryResult>>
            reliableTelemetryPublisher;

        public SalesMissionStage Stage { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public int DialogueIndex { get; private set; }
        public int UploadStartCount { get; private set; }
        public int RecordingSubmitCount { get; private set; }
        public string AttemptId => pendingTelemetry?.roundId ?? string.Empty;
        public string SalesSessionId { get; private set; } = string.Empty;
        public byte[] PendingWavBytes => pendingTelemetry?.audio == null
            || string.IsNullOrEmpty(pendingTelemetry.audio.dataBase64)
            ? null : Convert.FromBase64String(pendingTelemetry.audio.dataBase64);

        private bool[] previousLocomotion;
        private bool locomotionLocked;
        private Coroutine transition;
        private Coroutine persuasionRoutine;
        private float nextActionTime;
        private bool shoeSelectionLocked;
        private bool initialized;
        private bool microphoneRecording;
        private string microphoneDevice;
        private AudioClip recordingClip;
        private GameplayTelemetryData pendingTelemetry;
        private InputAction advanceDialogueAction;
        private int attemptGeneration;
        private string[] devices = Array.Empty<string>();

        public InputAction AdvanceDialogueAction => advanceDialogueAction;
        public event Action<string> SalesSessionStarted;
        public event Action<string> Part1RecordingUploaded;

        private void OnEnable()
        {
            if (advanceDialogueAction == null)
                advanceDialogueAction = new InputAction("Advance Dialogue", InputActionType.Button,
                    "<XRController>{RightHand}/primaryButton");
            advanceDialogueAction.performed += OnAdvanceDialogue;
            advanceDialogueAction.Enable();
            if (!initialized) return;
            if (focus != null) focus.enabled = true;
            ResetMission();
        }

        private void Start()
        {
            if (data == null || !data.IsValid || playerCamera == null || customer == null)
            {
                Debug.LogError("Sales mission requires valid data, customer and player camera.", this);
                enabled = false;
                return;
            }
            previousLocomotion = new bool[locomotion.Length];
            if (microphoneProvider == null) microphoneProvider = new UnitySalesMicrophone();
            initialized = true;
            ResetMission();
        }

        private void Update()
        {
            if (Stage != SalesMissionStage.Waiting || playerCamera == null || customer == null) return;
            Vector3 delta = playerCamera.transform.position - customer.position;
            delta.y = 0;
            if (delta.sqrMagnitude <= conversationDistance * conversationDistance) BeginDialogue();
        }

        private void LateUpdate()
        {
            if (playerCamera == null || dialogueRoot == null) return;
            // Only the presentation follows the head. The tracked camera is never moved.
            dialogueRoot.SetPositionAndRotation(playerCamera.transform.position +
                playerCamera.transform.rotation * new Vector3(0, -.34f, 1.35f), playerCamera.transform.rotation);
        }

        public void BeginDialogue()
        {
            if (Stage != SalesMissionStage.Waiting) return;
            Stage = SalesMissionStage.Dialogue;
            DialogueIndex = 0;
            SetLocomotionLocked(true);
            SetObjective("LẮNG NGHE KHÁCH HÀNG");
            if (customerMarker != null) customerMarker.SetActive(false);
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(true);
            ShowLine();
        }

        private void ShowLine()
        {
            if (speakerText != null) speakerText.text = data.customerName + "  ·  KHÁCH HÀNG";
            if (dialogueText != null) dialogueText.text = data.dialogue[DialogueIndex].text;
            if (nextText != null)
                nextText.text = DialogueIndex + 1 < data.dialogue.Length
                    ? "NHẤN A ĐỂ TIẾP TỤC" : "NHẤN A ĐỂ XEM BA ĐÔI GIÀY";
            ReplayAudio();
        }

        private void OnAdvanceDialogue(InputAction.CallbackContext _) => HandleAction(-1);

        public void HandleAction(int action)
        {
            if (Time.unscaledTime < nextActionTime) return;
            if (action == -2 && Stage == SalesMissionStage.Dialogue) { ReplayAudio(); return; }
            if ((action == -3 || action == -1) && Stage == SalesMissionStage.Completed) { ResetMission(); return; }
            if (action == -4 && (Stage == SalesMissionStage.PersuasionUploadFailed
                || Stage == SalesMissionStage.PersuasionMicrophoneFailed))
            {
                if (Stage == SalesMissionStage.PersuasionUploadFailed) BeginUploadCycle();
                else BeginPersuasionPreparation();
                return;
            }
            if (action == -1 && Stage == SalesMissionStage.Dialogue)
            {
                nextActionTime = Time.unscaledTime + .2f;
                if (voice != null) voice.Stop();
                if (++DialogueIndex < data.dialogue.Length) ShowLine();
                else transition = StartCoroutine(OpenChoices());
            }
            else if (action == -1 && Stage == SalesMissionStage.PersuasionRecording)
                FinishRecording(true);
            else if (action == -1 && (Stage == SalesMissionStage.PersuasionUploadFailed
                || Stage == SalesMissionStage.PersuasionMicrophoneFailed)) HandleAction(-4);
            else if (action >= 0 && action < data.shoes.Length && Stage == SalesMissionStage.Choosing)
                SelectShoe(action);
        }

        public void ReplayAudio()
        {
            if (Stage != SalesMissionStage.Dialogue || voice == null) return;
            voice.Stop();
            voice.clip = data.dialogue[DialogueIndex].audioClip;
            if (voice.clip != null) voice.Play();
        }

        private IEnumerator OpenChoices()
        {
            Stage = SalesMissionStage.Transition;
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            SetObjective("SO SÁNH VÀ CHỌN GIÀY");
            if (focus != null) focus.SetFocused(true);
            yield return new WaitForSecondsRealtime(transitionDuration);
            if (playerCamera != null && displayRoot != null)
            {
                Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
                if (forward.sqrMagnitude < .1f) forward = Vector3.forward;
                displayRoot.SetPositionAndRotation(playerCamera.transform.position + forward * 2.0f,
                    Quaternion.LookRotation(forward, Vector3.up));
                displayRoot.gameObject.SetActive(true);
            }
            if (choicesRoot != null) choicesRoot.SetActive(true);
            if (resultRoot != null) resultRoot.SetActive(false);
            if (focus != null) focus.SetOverlayActive(true);
            Stage = SalesMissionStage.Choosing;
            transition = null;
        }

        private void SelectShoe(int index)
        {
            if (shoeSelectionLocked || data == null || index < 0 || index >= data.shoes.Length) return;
            shoeSelectionLocked = true;
            SelectedIndex = index;
            if (choicesRoot != null) choicesRoot.SetActive(false);
            if (focus != null) focus.SetOverlayActive(false);
            if (objectiveText != null) objectiveText.gameObject.SetActive(true);
            if (displayRoot != null) displayRoot.gameObject.SetActive(true);
            SalesShoeData shoe = data.shoes[index];
            SetObjective(shoe.bestFit ? "THUYẾT PHỤC KHÁCH HÀNG"
                : "THAY ĐỔI LỰA CHỌN HOẶC THUYẾT PHỤC KHÁCH HÀNG");
            Stage = SalesMissionStage.PersuasionObjection;
            ShowObjection(shoe);
            BeginPersuasionPreparation();
        }

        private void ShowObjection(SalesShoeData shoe)
        {
            if (persuasionRoot == null) persuasionRoot = resultRoot;
            if (persuasionRoot != null) persuasionRoot.SetActive(false);
            SetRetryButtons(false, false);
            ShowCustomerMessage(shoe.objection, "BẮT ĐẦU GHI ÂM SAU " + Mathf.CeilToInt(preparationDuration) + " GIÂY");
        }

        private void ShowCustomerMessage(string message, string footer)
        {
            if (speakerText != null) speakerText.text = data.customerName + "  ·  KHÁCH HÀNG";
            if (dialogueText != null) dialogueText.text = message;
            if (nextText != null) nextText.text = footer;
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(true);
        }


        private void BeginPersuasionPreparation()
        {
            if (SelectedIndex < 0 || data == null || SelectedIndex >= data.shoes.Length) return;
            StopActivePersuasionRoutine();
            Stage = SalesMissionStage.PersuasionPreparation;
            persuasionRoutine = StartCoroutine(PrepareAndRecord());
        }

        private IEnumerator PrepareAndRecord()
        {
            if (persuasionRoot == null) persuasionRoot = resultRoot;
            if (persuasionRoot != null) persuasionRoot.SetActive(false);
            SetRetryButtons(false, false);
            yield return RequestMicrophonePermission();
            if (Stage == SalesMissionStage.PersuasionMicrophoneFailed) yield break;
            float endTime = Time.realtimeSinceStartup + Mathf.Max(0f, preparationDuration);
            int lastSecond = -1;
            while (Time.realtimeSinceStartup < endTime && Stage == SalesMissionStage.PersuasionPreparation)
            {
                int seconds = Mathf.Max(1, Mathf.CeilToInt(endTime - Time.realtimeSinceStartup));
                if (seconds != lastSecond)
                {
                    if (nextText != null) nextText.text = "BẮT ĐẦU GHI ÂM SAU " + seconds + " GIÂY";
                    lastSecond = seconds;
                }
                yield return null;
            }
            if (Stage == SalesMissionStage.PersuasionPreparation) yield return StartMicrophoneAndRecord();
        }

        private IEnumerator RequestMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone)) yield break;

            bool resolved = false;
            bool granted = false;
            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { granted = true; resolved = true; };
            callbacks.PermissionDenied += _ => resolved = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => resolved = true;
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
            while (!resolved) yield return null;
            if (!granted)
                ShowMicrophoneFailure("Chưa được cấp quyền dùng micrô. Hãy cấp quyền rồi nhấn A để thử lại.");
#else
            yield return null;
#endif
        }

private IEnumerator StartMicrophoneAndRecord()
        {
            devices = microphoneProvider != null ? microphoneProvider.Devices : null;
            if (devices == null || devices.Length == 0)
            {
                ShowMicrophoneFailure("Không tìm thấy micrô. Hãy kiểm tra thiết bị rồi nhấn A để thử lại.");
                yield break;
            }
            Stage = SalesMissionStage.PersuasionEncoding;
            SetRecordingIndicator(false);
            SetPersuasionUi("CHUẨN BỊ GHI ÂM", 0f, "Đang khởi động micrô...", string.Empty);
            microphoneDevice = devices[0];
            recordingClip = microphoneProvider.Start(microphoneDevice, false,
                Mathf.CeilToInt(Mathf.Max(1f, recordingDuration)) + 1, RecordingSampleRateHz);
            if (recordingClip == null)
            {
                StopAndReleaseMicrophone();
                ShowMicrophoneFailure("Không thể khởi động micrô. Nhấn A để thử lại.");
                yield break;
            }
            float startupDeadline = Time.realtimeSinceStartup + Mathf.Max(.1f, microphoneStartupTimeout);
            while (microphoneProvider.GetPosition(microphoneDevice) <= 0
                && Time.realtimeSinceStartup < startupDeadline) yield return null;
            if (microphoneProvider.GetPosition(microphoneDevice) <= 0)
            {
                StopAndReleaseMicrophone();
                ShowMicrophoneFailure("Micrô không nhận được âm thanh. Nhấn A để thử lại.");
                yield break;
            }

            microphoneRecording = true;
            Stage = SalesMissionStage.PersuasionRecording;
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            if (persuasionRoot == null) persuasionRoot = resultRoot;
            if (persuasionRoot != null) persuasionRoot.SetActive(false);
            SetRetryButtons(false, false);

            float startedAt = Time.realtimeSinceStartup;
            float endTime = startedAt + Mathf.Max(0f, recordingDuration);
            float blinkInterval = Mathf.Max(.1f, recordingBlinkInterval);
            SetRecordingIndicator(true);
            while (Stage == SalesMissionStage.PersuasionRecording && Time.realtimeSinceStartup < endTime)
            {
                int blinkStep = Mathf.FloorToInt((Time.realtimeSinceStartup - startedAt) / blinkInterval);
                SetRecordingIndicator((blinkStep & 1) == 0);
                yield return null;
            }
            if (Stage == SalesMissionStage.PersuasionRecording) FinishRecording(false);
        }

private void FinishRecording(bool endedEarly)
        {
            if (Stage != SalesMissionStage.PersuasionRecording || recordingClip == null
                || microphoneProvider == null || string.IsNullOrEmpty(microphoneDevice)) return;
            SetRecordingIndicator(false);
            Stage = SalesMissionStage.PersuasionEncoding;
            RecordingSubmitCount++;
            int frameCount = Mathf.Min(Mathf.Max(0, microphoneProvider.GetPosition(microphoneDevice)), recordingClip.samples);
            microphoneProvider.End(microphoneDevice);
            microphoneRecording = false;
            microphoneDevice = null;
            if (frameCount <= 0)
            {
                StopAndReleaseMicrophone();
                ShowMicrophoneFailure("Không ghi được âm thanh. Nhấn A để thử lại.");
                return;
            }
            int sourceChannels = Mathf.Max(1, recordingClip.channels);
            float[] interleaved = new float[frameCount * sourceChannels];
            recordingClip.GetData(interleaved, 0);
            int actualSampleRate = recordingClip.frequency;
            UnityEngine.Object.Destroy(recordingClip);
            recordingClip = null;
            microphoneDevice = null;
            float[] mono = Pcm16WavEncoder.DownmixToMono(interleaved, sourceChannels);
            float[] normalized = SalesAudioResampler.ResampleMono(mono, actualSampleRate, RecordingSampleRateHz);
            byte[] wav = Pcm16WavEncoder.Encode(normalized, 1, RecordingSampleRateHz);
            SalesShoeData shoe = data.shoes[SelectedIndex];
            string attemptId = "sales-persuasion-" + Guid.NewGuid().ToString("N");
            pendingTelemetry = new GameplayTelemetryData
            {
                salesSessionId = SalesSessionId,
                roundId = attemptId,
                caseId = data.customerName,
                questionId = "sales-part-1-persuasion",
                cardId = shoe.label,
                category = SalesMissionStage.PersuasionUploading.ToString(),
                resolution = JsonUtility.ToJson(new SalesPersuasionContext
                {
                    customerName = data.customerName,
                    selectedShoe = shoe.label,
                    bestFitShoe = GetBestFitShoeLabel(),
                    customerNeeds = GetCustomerNeeds(),
                    objective = shoe.bestFit ? "Thuyết phục khách hàng"
                        : "Thay đổi lựa chọn hoặc thuyết phục khách hàng",
                    objection = shoe.objection,
                    availableShoes = GetShoeContexts()
                }),
                audio = new AudioTelemetryData
                {
                    fileName = attemptId + ".wav",
                    mimeType = "audio/wav",
                    encoding = "pcm_s16le",
                    sampleRateHz = RecordingSampleRateHz,
                    channels = 1,
                    durationSeconds = normalized.Length / (float)RecordingSampleRateHz,
                    endedEarly = endedEarly,
                    dataBase64 = Convert.ToBase64String(wav)
                }
            };
            BeginUploadCycle();
        }

        private SalesPersuasionShoe[] GetShoeContexts()
        {
            SalesPersuasionShoe[] shoes = new SalesPersuasionShoe[data.shoes.Length];
            for (int index = 0; index < shoes.Length; index++)
            {
                SalesShoeData source = data.shoes[index];
                shoes[index] = new SalesPersuasionShoe
                {
                    shoeId = source.label,
                    name = source.label,
                    price = source.price,
                    details = source.properties
                };
            }
            return shoes;
        }

        private string GetBestFitShoeLabel()
        {
            for (int index = 0; index < data.shoes.Length; index++)
                if (data.shoes[index].bestFit) return data.shoes[index].label;
            return string.Empty;
        }

        private string GetCustomerNeeds()
        {
            string needs = string.Empty;
            for (int index = 0; index < data.dialogue.Length; index++)
            {
                if (index > 0) needs += "\n";
                needs += data.dialogue[index].text;
            }
            return needs;
        }

        private void BeginUploadCycle()
        {
            if (pendingTelemetry?.audio == null || Stage == SalesMissionStage.PersuasionUploading) return;
            Stage = SalesMissionStage.PersuasionUploading;
            UploadStartCount++;
            SetPersuasionUi("ĐANG GỬI BẢN GHI", 0f, "Đang gửi câu trả lời của bạn...", string.Empty);
            int callbackGeneration = attemptGeneration;
            Action<TelemetryDeliveryResult> callback = result =>
            {
                if (callbackGeneration == attemptGeneration) OnUploadCompleted(result);
            };
            if (reliableTelemetryPublisher != null)
            {
                reliableTelemetryPublisher(TelemetryEventType, pendingTelemetry, uploadAttempts, callback);
                return;
            }
            GameController controller = GameController.Instance;
            if (controller == null)
            {
                ShowUploadFailure("Không tìm thấy kết nối máy chủ. Nhấn A để thử lại.");
                return;
            }
            controller.PublishTelemetryReliable(TelemetryEventType, pendingTelemetry, uploadAttempts, callback);
        }

        private void OnUploadCompleted(TelemetryDeliveryResult result)
        {
            if (Stage != SalesMissionStage.PersuasionUploading) return;
            if (!result.Success)
            {
                ShowUploadFailure("Không thể gửi bản ghi. Nhấn A để thử lại.");
                return;
            }
            string completedAttemptId = pendingTelemetry != null ? pendingTelemetry.roundId : string.Empty;
            if (pendingTelemetry?.audio != null) pendingTelemetry.audio.dataBase64 = string.Empty;
            pendingTelemetry = null;
            Stage = SalesMissionStage.Completed;
            SetLocomotionLocked(false);
            if (choicesRoot != null) choicesRoot.SetActive(false);
            if (focus != null) { focus.SetOverlayActive(false); focus.SetFocused(false); }
            if (persuasionRoot != null) persuasionRoot.SetActive(false);
            SetRetryButtons(false, false);
            SetObjective("HOÀN THÀNH THUYẾT PHỤC");
            ShowCustomerMessage(data.closingReply, "NHẤN A ĐỂ CHƠI LẠI");
            Part1RecordingUploaded?.Invoke(completedAttemptId);
            // Notify the Part 2 bridge only after Part 1 has finished its own
            // cleanup, so the bridge can safely take over the HUD and locomotion.
        }

        private void ShowMicrophoneFailure(string message)
        {
            Stage = SalesMissionStage.PersuasionMicrophoneFailed;
            SetPersuasionUi("LỖI MICRÔ", 0f, message, "Nhấn A để thử lại");
            SetRetryButtons(true, false);
            if (retryUploadTarget != null && retryUploadTarget.caption != null)
                retryUploadTarget.caption.text = "THỬ LẠI";
        }

        private void ShowUploadFailure(string message)
        {
            Stage = SalesMissionStage.PersuasionUploadFailed;
            SetPersuasionUi("CHƯA GỬI ĐƯỢC", 0f, message, "Nhấn A để gửi lại đúng bản ghi");
            SetRetryButtons(true, false);
            if (retryUploadTarget != null && retryUploadTarget.caption != null)
                retryUploadTarget.caption.text = "GỬI LẠI";
        }

        private void SetPersuasionUi(string title, float countdown, string instruction, string status)
        {
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            if (persuasionRoot == null) persuasionRoot = resultRoot;
            if (persuasionRoot != null) persuasionRoot.SetActive(true);
            if (persuasionTitleText != null) persuasionTitleText.text = title;
            if (persuasionInstructionText != null) persuasionInstructionText.text = instruction;
            if (persuasionStatusText != null) persuasionStatusText.text = status;
            if (countdown > 0f) SetCountdown(Mathf.CeilToInt(countdown));
        }

        private void SetCountdown(int seconds)
        {
            if (persuasionCountdownText != null) persuasionCountdownText.text = seconds.ToString();
        }

private void SetRecordingIndicator(bool visible)
        {
            if (recordingIndicator != null && recordingIndicator.activeSelf != visible)
                recordingIndicator.SetActive(visible);
        }


        private void SetRetryButtons(bool uploadRetryVisible, bool restartVisible)
        {
            if (retryUploadTarget != null) retryUploadTarget.gameObject.SetActive(uploadRetryVisible);
            if (restartTarget != null) restartTarget.gameObject.SetActive(restartVisible);
        }

        public void ResetMission()
        {
            attemptGeneration++;
            StopActivePersuasionRoutine();
            StopAndReleaseMicrophone();
            pendingTelemetry = null;
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (voice != null) { voice.Stop(); voice.clip = null; }
            if (focus != null) { focus.SetOverlayActive(false); focus.SetFocused(false); }
            if (displayRoot != null) displayRoot.gameObject.SetActive(false);
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            SetLocomotionLocked(false);
            SelectedIndex = -1;
            shoeSelectionLocked = false;
            nextActionTime = 0f;
            DialogueIndex = 0;
            SalesSessionId = Guid.NewGuid().ToString("N");
            SalesSessionStarted?.Invoke(SalesSessionId);
            Stage = SalesMissionStage.Waiting;
            if (objectiveText != null)
            {
                objectiveText.gameObject.SetActive(true);
                objectiveText.text = "ĐẾN GẶP KHÁCH Ở CỬA CHÍNH\nTrò chơi dùng micrô và lưu tạm bản ghi để chẩn đoán kỹ thuật.";
            }
            if (customerMarker != null) customerMarker.SetActive(true);
            SetRetryButtons(false, false);
        }

        private void SetObjective(string text)
        {
            if (objectiveText != null) objectiveText.text = text;
        }

        private void SetLocomotionLocked(bool value)
        {
            if (locomotionLocked == value || previousLocomotion == null) return;
            for (int i = 0; i < locomotion.Length; i++)
            {
                if (locomotion[i] == null) continue;
                if (value) { previousLocomotion[i] = locomotion[i].enabled; locomotion[i].enabled = false; }
                else locomotion[i].enabled = previousLocomotion[i];
            }
            locomotionLocked = value;
        }

        private void StopActivePersuasionRoutine()
        {
            if (persuasionRoutine == null) return;
            StopCoroutine(persuasionRoutine);
            persuasionRoutine = null;
        }

private void StopAndReleaseMicrophone()
        {
            SetRecordingIndicator(false);
            if (microphoneProvider != null && !string.IsNullOrEmpty(microphoneDevice))
                microphoneProvider.End(microphoneDevice);
            microphoneRecording = false;
            if (recordingClip != null)
            {
                UnityEngine.Object.Destroy(recordingClip);
                recordingClip = null;
            }
            microphoneDevice = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused || (Stage != SalesMissionStage.PersuasionPreparation
                && Stage != SalesMissionStage.PersuasionRecording
                && Stage != SalesMissionStage.PersuasionEncoding)) return;
            StopActivePersuasionRoutine();
            StopAndReleaseMicrophone();
            ShowMicrophoneFailure("Ghi âm bị gián đoạn khi kính tạm dừng. Nhấn A để thử lại.");
        }

        private void OnDisable()
        {
            attemptGeneration++;
            if (advanceDialogueAction != null)
            {
                advanceDialogueAction.performed -= OnAdvanceDialogue;
                advanceDialogueAction.Disable();
            }
            StopActivePersuasionRoutine();
            StopAndReleaseMicrophone();
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (voice != null) voice.Stop();
            if (focus != null) focus.enabled = false;
            if (displayRoot != null) displayRoot.gameObject.SetActive(false);
            if (dialogueRoot != null) dialogueRoot.gameObject.SetActive(false);
            SetLocomotionLocked(false);
        }
    }
}
