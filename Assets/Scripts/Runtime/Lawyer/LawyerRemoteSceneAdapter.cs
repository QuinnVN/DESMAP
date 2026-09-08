using System;
using NCKH.VR.GameControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerRemoteSceneAdapter : MonoBehaviour, IGameSceneAdapter
    {
        [SerializeField] private LawyerCaseDefinition caseDefinition;
        [SerializeField] private LawyerBoardController boardController;
        [SerializeField] private LawyerInterviewController interviewController;

        private LawyerGameSession session;
        public event Action<string, GameplayTelemetryData> TelemetryRaised;

#if UNITY_EDITOR
        public void ConfigureInEditor(
            LawyerCaseDefinition definition,
            LawyerBoardController board,
            LawyerInterviewController interview)
        {
            caseDefinition = definition;
            boardController = board;
            interviewController = interview;
        }
#endif

        public RemoteGamePhase Phase
        {
            get
            {
                if (session == null || session.Phase == LawyerSessionPhase.CaseBrief)
                    return RemoteGamePhase.Ready;
                return session.Phase == LawyerSessionPhase.Stopped
                    || session.Phase == LawyerSessionPhase.Completed
                    || session.Phase == LawyerSessionPhase.Failed
                    ? RemoteGamePhase.Finished
                    : RemoteGamePhase.Running;
            }
        }

        private void OnEnable()
        {
            session = LawyerGameSession.Ensure(caseDefinition);
            Subscribe();
            if (boardController != null)
            {
                boardController.BoardOpened += OnBoardOpened;
                boardController.CourtroomRequested += OnCourtroomRequested;
                boardController.DefenseRequested += OnDefenseRequested;
            }
            if (interviewController != null)
                interviewController.InterviewCompleted += OnInterviewCompleted;
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (boardController != null)
            {
                boardController.BoardOpened -= OnBoardOpened;
                boardController.CourtroomRequested -= OnCourtroomRequested;
                boardController.DefenseRequested -= OnDefenseRequested;
            }
            if (interviewController != null)
                interviewController.InterviewCompleted -= OnInterviewCompleted;
        }

        public bool TryStart(out string errorCode)
        {
            errorCode = string.Empty;
            if (session == null)
                session = LawyerGameSession.Ensure(caseDefinition);
            if (session.Phase != LawyerSessionPhase.CaseBrief)
            {
                errorCode = "invalid_phase";
                return false;
            }
            session.BeginRound();
            TelemetryRaised?.Invoke("lawyer.round_started", CurrentData());
            return true;
        }

        public bool TryRestart(out string errorCode)
        {
            errorCode = string.Empty;
            session = LawyerGameSession.Ensure(caseDefinition);
            session.ResetCase(caseDefinition);
            session.BeginRound();
            if (SceneManager.GetActiveScene().path != LawyerInterviewController.OfficeScenePath)
            {
                if (!SceneTransitionService.BeginLoad(LawyerInterviewController.OfficeScenePath))
                {
                    errorCode = "scene_loading";
                    return false;
                }
            }
            TelemetryRaised?.Invoke("game.restarted", CurrentData());
            return true;
        }

        public bool TryStop(out string errorCode)
        {
            errorCode = string.Empty;
            if (session == null)
            {
                errorCode = "not_ready";
                return false;
            }
            session.Stop();
            TelemetryRaised?.Invoke("game.stopped", CurrentData());
            return true;
        }

        private void Subscribe()
        {
            if (session == null)
                return;
            session.ClueUnlocked += OnClueUnlocked;
            session.MainQuestionAsked += OnMainQuestionAsked;
            session.FollowUpAsked += OnFollowUpAsked;
            session.LinkAdded += OnLinkAdded;
            session.PhaseChanged += OnPhaseChanged;
        }

        private void Unsubscribe()
        {
            if (session == null)
                return;
            session.ClueUnlocked -= OnClueUnlocked;
            session.MainQuestionAsked -= OnMainQuestionAsked;
            session.FollowUpAsked -= OnFollowUpAsked;
            session.LinkAdded -= OnLinkAdded;
            session.PhaseChanged -= OnPhaseChanged;
        }

        private void OnBoardOpened() => TelemetryRaised?.Invoke("lawyer.case_brief_opened", CurrentData());
        private void OnCourtroomRequested() => TelemetryRaised?.Invoke("lawyer.interview_started", CurrentData());
        private void OnDefenseRequested() => TelemetryRaised?.Invoke("lawyer.defense_started", CurrentData());
        private void OnInterviewCompleted() => TelemetryRaised?.Invoke("lawyer.interview_finished", CurrentData());

        private void OnClueUnlocked(string clueId)
        {
            GameplayTelemetryData data = CurrentData();
            data.cardId = clueId;
            TelemetryRaised?.Invoke("lawyer.clue_revealed", data);
        }

        private void OnMainQuestionAsked(string questionId)
        {
            GameplayTelemetryData data = CurrentData();
            data.questionId = questionId;
            TelemetryRaised?.Invoke("lawyer.question_asked", data);
        }

        private void OnFollowUpAsked(string mainId, string followUpId)
        {
            GameplayTelemetryData data = CurrentData();
            data.questionId = followUpId;
            data.resolution = mainId;
            TelemetryRaised?.Invoke("lawyer.followup_asked", data);
        }

        private void OnLinkAdded(LawyerClueLink link)
        {
            GameplayTelemetryData data = CurrentData();
            data.cardId = link.FirstId;
            data.resolution = link.SecondId;
            TelemetryRaised?.Invoke("lawyer.clue_linked", data);
        }

        private void OnPhaseChanged(LawyerSessionPhase phase)
        {
            if (phase == LawyerSessionPhase.Failed)
                TelemetryRaised?.Invoke("lawyer.evidence_failed", CurrentData());
            else if (phase == LawyerSessionPhase.Resolved)
                TelemetryRaised?.Invoke("lawyer.case_resolved", CurrentData());
        }

        private GameplayTelemetryData CurrentData()
        {
            return new GameplayTelemetryData
            {
                roundId = session != null ? session.RoundId : string.Empty,
                caseId = caseDefinition != null ? caseDefinition.CaseId : string.Empty,
                category = session != null ? session.Phase.ToString() : string.Empty
            };
        }
    }
}
