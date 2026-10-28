using System;
using System.Collections;
using NCKH.VR.GameControl;
using UnityEngine;

namespace NCKH.VR.Sales
{
    [Serializable]
    public sealed class SalesPart2StartSessionRequest
    {
        public string salesSessionId;
        public string part1AttemptId;
    }

    [Serializable]
    public sealed class SalesPart2Part1AssociationRequest
    {
        public string salesSessionId;
        public string part1AttemptId;
    }

    [Serializable]
    public sealed class SalesPart2TurnRequest
    {
        public string salesSessionId;
        public string turnId;
        public SalesPart2AudioPayload audio;
        public string clientVersion;
        public int retryCount;
    }

    [Serializable]
    public sealed class SalesPart2AudioPayload
    {
        public string mimeType = "audio/wav";
        public string encoding = "pcm_s16le";
        public int sampleRateHz = 16000;
        public int channels = 1;
        public float durationSeconds;
        public bool endedEarly;
        public string dataBase64;
    }

    [Serializable]
    public sealed class SalesPart2CompletionRequest
    {
        public string salesSessionId;
        public string reason;
        public int acceptedTurnCount;
        public float activeDurationSeconds;
        public string completionId;
    }

    [Serializable]
    public sealed class SalesPart2SessionStateRequest
    {
        public string salesSessionId;
    }

    [Serializable]
    public sealed class SalesPart2SessionResponse
    {
        public bool success;
        public string sessionId;
        public string salesSessionId;
        public string failureCategory;
        public string errorMessage;
    }

    /// <summary>
    /// Structured response from one idempotent Part 2 turn. The backend owns the
    /// conversation history and returns only state needed to drive the HUD.
    /// </summary>
    [Serializable]
    public sealed class SalesPart2TurnResponse
    {
        public bool success;
        public string status;
        public bool accepted;
        public bool silent;
        public bool technicalFailure;
        public bool nonVietnamese;
        public string language = "vi";
        public string transcript;
        public string customerText;
        public int activeObjective;
        public string objective;
        public bool objectiveCompleted;
        public string[] disclosedFactIds = Array.Empty<string>();
        public bool conversationComplete;
        public string deterministicEnding;
        public string failureCategory;
        public string errorMessage;
        public int acceptedTurnCount;
        public int silenceCount;
        public string sttProvider;
        public string sttModel;
        public string sttVersion;
        public string llmProvider;
        public string llmModel;
        public string llmVersion;
        public string customerModel;
        public string customerModelVersion;
        public SalesPart2Error error;

        public bool HasUsableCustomerText => !string.IsNullOrWhiteSpace(customerText);
        public bool PreviousObjectiveCompleted => objectiveCompleted;
        public bool IsFailure => technicalFailure || string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase)
            || error != null;
        public string FailureMessage => error != null ? error.message : errorMessage;
    }

    [Serializable]
    public sealed class SalesPart2Error
    {
        public string code;
        public string message;
    }

    [Serializable]
    public sealed class SalesPart2SessionStateResponse
    {
        public string sessionId;
        public string trustState;
        public string status;
        public bool success;
        public string salesSessionId;
        public int activeObjective;
        public int acceptedTurnCount;
        public int silenceCount;
        public bool conversationComplete;
        public string deterministicEnding;
        public string lastCustomerText;
        public string failureCategory;
        public string errorMessage;
    }

    [Serializable]
    public sealed class SalesPart2AnalysisRequest
    {
        public string salesSessionId;
        public string reason;
        public int acceptedTurnCount;
        public float activeDurationSeconds;
        public string completionId;
    }

    [Serializable]
    public sealed class SalesPart2AnalysisResponse
    {
        public bool success;
        public string trustState;
        public bool emotionalHandling;
        public bool causeIdentification;
        public bool solutionSuitability;
        public bool trustRebuilding;
        public string failureCategory;
        public string errorMessage;

        public SalesPart2TrustState ParsedTrustState
        {
            get
            {
                if (string.Equals(trustState, "restored", StringComparison.OrdinalIgnoreCase))
                    return SalesPart2TrustState.Restored;
                if (string.Equals(trustState, "partially_restored", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(trustState, "partiallyRestored", StringComparison.OrdinalIgnoreCase))
                    return SalesPart2TrustState.PartiallyRestored;
                if (string.Equals(trustState, "lost", StringComparison.OrdinalIgnoreCase))
                    return SalesPart2TrustState.Lost;
                return SalesPart2TrustState.Unknown;
            }
        }
    }

    /// <summary>
    /// Coroutine transport seam. Implementations must invoke each callback once,
    /// including technical failures. Requests are idempotent by session/turn ID.
    /// </summary>
    public interface ISalesPart2RequestService
    {
        IEnumerator StartOrResumeSession(SalesPart2StartSessionRequest request,
            Action<SalesPart2SessionResponse> completed);
        IEnumerator AssociatePart1(SalesPart2Part1AssociationRequest request,
            Action<SalesPart2SessionResponse> completed);
        IEnumerator SubmitTurn(SalesPart2TurnRequest request,
            Action<SalesPart2TurnResponse> completed);
        IEnumerator CompleteAnalysis(SalesPart2AnalysisRequest request,
            Action<SalesPart2AnalysisResponse> completed);
        IEnumerator ReadSessionState(SalesPart2SessionStateRequest request,
            Action<SalesPart2SessionStateResponse> completed);
    }
}
