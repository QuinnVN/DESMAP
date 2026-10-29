using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace NCKH.VR.Sales.Tests
{
    public sealed class SalesPart2FlowTests
    {
        private GameObject root;
        private SalesPart2Controller controller;
        private SalesPart2ScenarioData scenario;
        private DeterministicMicrophone microphone;
        private DeterministicRequestService requests;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("Sales Part 2 test fixture");
            root.SetActive(false);
            controller = root.AddComponent<SalesPart2Controller>();
            scenario = ScriptableObject.CreateInstance<SalesPart2ScenarioData>();
            microphone = new DeterministicMicrophone();
            requests = new DeterministicRequestService();

            controller.scenario = scenario;
            controller.microphoneProvider = microphone;
            controller.requestService = requests;
            controller.customerReplyDisplaySeconds = 0f;
            controller.endingDisplaySeconds = 0f;
            controller.fadeDuration = 0f;
            controller.requestTimeoutSeconds = 5f;
            controller.turnDurationSeconds = 5f;
            controller.activeTimeLimitSeconds = 60f;
            controller.maximumAcceptedTurns = 8;
            controller.hudRoot = Child("HUD");
            controller.recordingIndicator = Child("Recording");
            controller.recordingIndicator.SetActive(false);
            controller.incompleteRoot = Child("Incomplete");
            controller.incompleteRoot.SetActive(false);
            controller.customerText = Label("Customer");
            controller.objectiveText = Label("Objective");
            controller.policyText = Label("Policy");
            controller.statusText = Label("Status");
            controller.timerText = Label("Timer");
            controller.trustStateText = Label("Trust");
            controller.resultText = Label("Result");
            root.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UnityEngine.Object.Destroy(root);
            UnityEngine.Object.Destroy(scenario);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AcceptedResponsesAdvanceObjectivesInOrderAndFinalALeavesHudVisible()
        {
            requests.TurnResponses.Enqueue(Accepted(2, true, "Chị sẽ nói rõ hơn."));
            requests.TurnResponses.Enqueue(Accepted(3, true, "Chị đi bộ cả ngày."));
            requests.TurnResponses.Enqueue(Accepted(4, true, "Chị muốn thử đôi nhẹ hơn."));
            requests.TurnResponses.Enqueue(Accepted(4, true, "Cảm ơn em.", true));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse
            {
                success = true,
                trustState = "restored"
            });

            controller.BeginPart2("session-ordered", "part1-ordered");
            yield return WaitForStage(SalesPart2Stage.Recording);

            yield return SubmitAndWaitForRecording(SalesPart2Objective.FindingRootCause);
            yield return SubmitAndWaitForRecording(SalesPart2Objective.ProposeSuitableSolution);
            yield return SubmitAndWaitForRecording(SalesPart2Objective.RebuildTrust);
            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Completed);

            Assert.That(controller.AcceptedTurnCount, Is.EqualTo(4));
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(4));
            Assert.That(controller.TrustState, Is.EqualTo(SalesPart2TrustState.Restored));
            Assert.That(controller.trustStateText.text, Is.EqualTo("ĐÃ KHÔI PHỤC NIỀM TIN"));
            Assert.That(controller.hudRoot.activeSelf, Is.True);

            string visibleResult = controller.resultText.text;
            controller.HandleAction(-1);
            yield return null;
            Assert.That(controller.Stage, Is.EqualTo(SalesPart2Stage.Completed));
            Assert.That(controller.hudRoot.activeSelf, Is.True);
            Assert.That(controller.resultText.text, Is.EqualTo(visibleResult));
        }

        [UnityTest]
        public IEnumerator TurnDeadlineSubmitsWithoutAButtonPress()
        {
            controller.turnDurationSeconds = .08f;
            requests.HoldTurnRequests = true;
            controller.BeginPart2("session-auto-stop", string.Empty);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (requests.SubmittedTurns.Count == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));
            Assert.That(microphone.EndCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator AcceptedTurnLimitRequestsFinalAnalysis()
        {
            for (int i = 0; i < 8; i++) requests.TurnResponses.Enqueue(Accepted(1, false, "Chị cần em hỏi thêm."));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse { trustState = "lost" });
            controller.BeginPart2("session-eight-turns", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);
            for (int i = 0; i < 7; i++)
            {
                controller.HandleAction(-1);
                yield return WaitForStage(SalesPart2Stage.Recording);
            }
            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Completed);
            Assert.That(controller.AcceptedTurnCount, Is.EqualTo(8));
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ClarificationAtLastTurnKeepsRecordingAndNextAnswerConsumesLastTurn()
        {
            controller.maximumAcceptedTurns = 1;
            SalesPart2TurnResponse clarification = Accepted(1, false, "Chị chưa nghe rõ, em nói lại đi.");
            clarification.clarificationRequested = true;
            clarification.acceptedTurnCount = 0;
            requests.TurnResponses.Enqueue(clarification);
            requests.TurnResponses.Enqueue(Accepted(1, false, "Chị cần em giải thích rõ hơn."));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse { trustState = "lost" });
            controller.BeginPart2("session-clarification", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Recording);
            Assert.That(controller.AcceptedTurnCount, Is.Zero);
            Assert.That(requests.AnalysisRequests.Count, Is.Zero);
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Completed);
            Assert.That(controller.AcceptedTurnCount, Is.EqualTo(1));
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(1));
            Assert.That(requests.SubmittedTurns[0].turnId, Is.Not.EqualTo(requests.SubmittedTurns[1].turnId));
        }

        [UnityTest]
        public IEnumerator ClarificationOnlyErrorEndsWithoutAssessmentOrRetry()
        {
            SalesPart2TurnResponse clarification = Accepted(1, false,
                "Chị vẫn chưa hiểu rõ lời em nói. Chị dừng trao đổi ở đây đã.", true);
            clarification.clarificationRequested = true;
            clarification.acceptedTurnCount = 0;
            clarification.terminalError = UnintelligibleError();
            requests.TurnResponses.Enqueue(clarification);
            controller.BeginPart2("session-clarification-limit", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Failed);
            Assert.That(controller.AcceptedTurnCount, Is.Zero);
            Assert.That(requests.AnalysisRequests.Count, Is.Zero);
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));
            Assert.That(controller.TrustState, Is.EqualTo(SalesPart2TrustState.Unknown));
            Assert.That(controller.statusText.text, Is.EqualTo("PHIÊN ĐÃ KẾT THÚC"));
            Assert.That(controller.resultText.text, Does.Contain("lời nói không rõ"));
            controller.RetryCurrentOperation();
            controller.HandleAction(-1);
            yield return null;
            Assert.That(controller.Stage, Is.EqualTo(SalesPart2Stage.Failed));
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));
            Assert.That(requests.AnalysisRequests.Count, Is.Zero);
        }

        [UnityTest]
        public IEnumerator UnintelligibleSessionResumeKeepsTerminalErrorWithoutRetry()
        {
            requests.SessionState = new SalesPart2SessionStateResponse
            { sessionId = "unclear-resume", status = "finished", terminalError = UnintelligibleError() };
            controller.ResumeSession("unclear-resume");
            yield return WaitForStage(SalesPart2Stage.Failed);
            controller.RetryCurrentOperation();
            controller.HandleAction(-1);
            yield return null;
            Assert.That(controller.IsFinished, Is.True);
            Assert.That(controller.IsCompleted, Is.False);
            Assert.That(requests.SubmittedTurns.Count, Is.Zero);
            Assert.That(requests.AnalysisRequests.Count, Is.Zero);
            Assert.That(controller.TrustState, Is.EqualTo(SalesPart2TrustState.Unknown));
        }

        [UnityTest]
        public IEnumerator TimeLimitUnintelligibleErrorStopsWithoutTrustVerdict()
        {
            controller.activeTimeLimitSeconds = .08f;
            requests.TurnResponses.Enqueue(Accepted(1, false, "Chị chưa hiểu, em nói lại đi."));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse { terminalError = UnintelligibleError() });
            controller.BeginPart2("unclear-time-limit", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Failed);
            controller.RetryCurrentOperation();
            yield return null;
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(1));
            Assert.That(controller.TrustState, Is.EqualTo(SalesPart2TrustState.Unknown));
        }

        private static SalesPart2Error UnintelligibleError() => new SalesPart2Error
        { code = "speech_unintelligible", message = "Không thể đánh giá phiên vì lời nói không rõ." };

        [UnityTest]
        public IEnumerator ActiveDeadlineSubmitsCurrentRecordingThenRequestsFinalAnalysis()
        {
            controller.activeTimeLimitSeconds = .08f;
            requests.TurnResponses.Enqueue(Accepted(1, false, "Chị đã nghe em nói."));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse { trustState = "lost" });
            controller.BeginPart2("session-active-limit", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Completed);
            Assert.That(controller.AcceptedTurnCount, Is.EqualTo(1));
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InvalidPhasePreservesPendingAudioAndTurnIdForRetry()
        {
            requests.TurnResponses.Enqueue(Accepted(3, true, "Sai pha"));
            requests.TurnResponses.Enqueue(Accepted(1, false, "Chị cần em lắng nghe."));
            controller.BeginPart2("session-invalid-phase", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Incomplete);
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));
            TurnSnapshot first = requests.SubmittedTurns[0];
            Assert.That(controller.CurrentTurnId, Is.EqualTo(first.turnId));

            controller.RetryCurrentOperation();
            yield return WaitForStage(SalesPart2Stage.Recording);
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(2));
            TurnSnapshot retry = requests.SubmittedTurns[1];
            Assert.That(retry.turnId, Is.EqualTo(first.turnId));
            Assert.That(retry.audioBase64, Is.EqualTo(first.audioBase64));
            Assert.That(retry.retryCount, Is.EqualTo(1));
            Assert.That(controller.AcceptedTurnCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SilentAndTechnicalTurnsDoNotInflateAcceptedTurnCount()
        {
            requests.TurnResponses.Enqueue(new SalesPart2TurnResponse
            {
                success = true,
                status = "silent",
                silent = true,
                silenceCount = 1,
                customerText = "Chị chưa nghe rõ em."
            });
            requests.TurnResponses.Enqueue(new SalesPart2TurnResponse
            {
                success = false,
                status = "failed",
                technicalFailure = true,
                errorMessage = "network"
            });
            controller.BeginPart2("session-counts", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Recording);
            Assert.That(controller.SilenceCount, Is.EqualTo(1));
            Assert.That(controller.AcceptedTurnCount, Is.Zero);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Incomplete);
            Assert.That(controller.SilenceCount, Is.EqualTo(1));
            Assert.That(controller.AcceptedTurnCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AnalysisRetryUsesTheSameCompletionId()
        {
            requests.TurnResponses.Enqueue(Accepted(1, false, "Chị hiểu rồi.", true));
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse
            {
                success = false,
                errorMessage = "analysis unavailable"
            });
            requests.AnalysisResponses.Enqueue(new SalesPart2AnalysisResponse
            {
                success = true,
                trustState = "partially_restored"
            });
            controller.BeginPart2("session-analysis-retry", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Incomplete);
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(1));

            controller.RetryCurrentOperation();
            yield return WaitForStage(SalesPart2Stage.Completed);
            Assert.That(requests.AnalysisRequests.Count, Is.EqualTo(2));
            Assert.That(requests.AnalysisRequests[1].completionId,
                Is.EqualTo(requests.AnalysisRequests[0].completionId));
            Assert.That(controller.TrustState, Is.EqualTo(SalesPart2TrustState.PartiallyRestored));
        }

        [UnityTest]
        public IEnumerator RequestTimeoutRetainsCurrentTurnId()
        {
            requests.HoldTurnRequests = true;
            controller.requestTimeoutSeconds = .03f;
            controller.BeginPart2("session-timeout", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);
            string turnId = controller.CurrentTurnId;

            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Incomplete);
            Assert.That(requests.SubmittedTurns.Count, Is.EqualTo(1));
            Assert.That(requests.SubmittedTurns[0].turnId, Is.EqualTo(turnId));
            Assert.That(controller.CurrentTurnId, Is.EqualTo(turnId));
            Assert.That(requests.CancelCount, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator ActiveTimerDoesNotAdvanceWhileTurnProcessingIsInFlight()
        {
            requests.HoldTurnRequests = true;
            controller.BeginPart2("session-timer", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);
            yield return new WaitForSecondsRealtime(.05f);
            controller.HandleAction(-1);
            float processingStart = controller.ActiveTimeSeconds;
            yield return new WaitForSecondsRealtime(.12f);

            Assert.That(controller.RequestInFlight, Is.True);
            Assert.That(controller.ActiveTimeSeconds, Is.EqualTo(processingStart).Within(.02f));
            controller.StopPart2();
        }

        [UnityTest]
        public IEnumerator PauseAndStopCancelRequestsReleaseMicrophoneAndRestoreLocomotion()
        {
            LocomotionProbe locomotion = root.AddComponent<LocomotionProbe>();
            controller.locomotion = new Behaviour[] { locomotion };
            controller.BeginPart2("session-pause", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);
            Assert.That(locomotion.enabled, Is.False);

            int microphoneEndsBeforeStop = microphone.EndCount;
            int cancelsBeforeStop = requests.CancelCount;
            controller.StopPart2();
            Assert.That(controller.Stage, Is.EqualTo(SalesPart2Stage.Stopped));
            Assert.That(microphone.EndCount, Is.GreaterThan(microphoneEndsBeforeStop));
            Assert.That(requests.CancelCount, Is.GreaterThan(cancelsBeforeStop));
            Assert.That(locomotion.enabled, Is.True);

            controller.ResetPart2();
            controller.BeginPart2("session-pause-retry", string.Empty);
            yield return WaitForStage(SalesPart2Stage.Recording);
            int microphoneEndsBeforePause = microphone.EndCount;

            root.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
            yield return null;
            Assert.That(controller.Stage, Is.EqualTo(SalesPart2Stage.Incomplete));
            Assert.That(microphone.EndCount, Is.GreaterThan(microphoneEndsBeforePause));
            Assert.That(requests.CancelCount, Is.GreaterThan(0));
            Assert.That(locomotion.enabled, Is.False);
        }

        private IEnumerator SubmitAndWaitForRecording(SalesPart2Objective expectedObjective)
        {
            controller.HandleAction(-1);
            yield return WaitForStage(SalesPart2Stage.Recording);
            Assert.That(controller.CurrentObjective, Is.EqualTo(expectedObjective));
        }

        private IEnumerator WaitForStage(SalesPart2Stage expected)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (controller.Stage != expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(controller.Stage, Is.EqualTo(expected));
        }

        private GameObject Child(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        private TMP_Text Label(string name) => Child(name).AddComponent<TextMeshPro>();

        private static SalesPart2TurnResponse Accepted(int objective, bool completed, string customerText,
            bool conversationComplete = false)
        {
            return new SalesPart2TurnResponse
            {
                success = true,
                status = "accepted",
                accepted = true,
                activeObjective = objective,
                objectiveCompleted = completed,
                customerText = customerText,
                conversationComplete = conversationComplete
            };
        }

        private sealed class LocomotionProbe : MonoBehaviour
        {
        }

        private sealed class DeterministicMicrophone : ISalesMicrophone
        {
            public int EndCount { get; private set; }
            public string[] Devices => new[] { "Test microphone" };

            public AudioClip Start(string deviceName, bool loop, int lengthSeconds, int frequency)
            {
                return AudioClip.Create("Sales Part 2 recording", 1600, 1, 16000, false);
            }

            public int GetPosition(string deviceName) => 800;
            public void End(string deviceName) => EndCount++;
        }

        private sealed class TurnSnapshot
        {
            public string turnId;
            public string audioBase64;
            public int retryCount;
        }

        private sealed class AnalysisSnapshot
        {
            public string completionId;
        }

        private sealed class DeterministicRequestService : ISalesPart2RequestService,
            ISalesPart2RequestCancellation
        {
            public readonly Queue<SalesPart2TurnResponse> TurnResponses = new Queue<SalesPart2TurnResponse>();
            public readonly Queue<SalesPart2AnalysisResponse> AnalysisResponses = new Queue<SalesPart2AnalysisResponse>();
            public readonly List<TurnSnapshot> SubmittedTurns = new List<TurnSnapshot>();
            public readonly List<AnalysisSnapshot> AnalysisRequests = new List<AnalysisSnapshot>();
            public bool HoldTurnRequests;
            public SalesPart2SessionStateResponse SessionState;
            public int CancelCount { get; private set; }

            public IEnumerator StartOrResumeSession(SalesPart2StartSessionRequest request,
                Action<SalesPart2SessionResponse> completed)
            {
                completed(new SalesPart2SessionResponse { success = true, sessionId = request.salesSessionId });
                yield break;
            }

            public IEnumerator AssociatePart1(SalesPart2Part1AssociationRequest request,
                Action<SalesPart2SessionResponse> completed)
            {
                completed(new SalesPart2SessionResponse { success = true, sessionId = request.salesSessionId });
                yield break;
            }

            public IEnumerator SubmitTurn(SalesPart2TurnRequest request,
                Action<SalesPart2TurnResponse> completed)
            {
                SubmittedTurns.Add(new TurnSnapshot
                {
                    turnId = request.turnId,
                    audioBase64 = request.audio != null ? request.audio.dataBase64 : string.Empty,
                    retryCount = request.retryCount
                });
                if (HoldTurnRequests)
                {
                    int cancellationVersion = CancelCount;
                    while (cancellationVersion == CancelCount)
                        yield return null;
                    yield break;
                }
                completed(TurnResponses.Count > 0 ? TurnResponses.Dequeue()
                    : new SalesPart2TurnResponse { success = false, technicalFailure = true });
            }

            public IEnumerator CompleteAnalysis(SalesPart2AnalysisRequest request,
                Action<SalesPart2AnalysisResponse> completed)
            {
                AnalysisRequests.Add(new AnalysisSnapshot { completionId = request.completionId });
                completed(AnalysisResponses.Count > 0 ? AnalysisResponses.Dequeue()
                    : new SalesPart2AnalysisResponse { success = false });
                yield break;
            }

            public IEnumerator ReadSessionState(SalesPart2SessionStateRequest request,
                Action<SalesPart2SessionStateResponse> completed)
            {
                completed(SessionState ?? new SalesPart2SessionStateResponse
                {
                    success = true,
                    sessionId = request.salesSessionId
                });
                yield break;
            }

            public void CancelAll()
            {
                CancelCount++;
            }
        }
    }
}
