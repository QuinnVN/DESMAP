using TMPro;
using UnityEngine;

namespace NCKH.VR.Clinic
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class ClinicRoundHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ClinicGameManager gameManager;
        [SerializeField] private GameObject locomotionRoot;
        [SerializeField] private GameObject roundTimerRoot;
        [SerializeField] private TMP_Text roundTimerText;
        [SerializeField] private TMP_Text preparationCountdownText;
        [SerializeField] private TMP_Text totalScoreText;
        [SerializeField] private CanvasGroup preparationOverlay;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float preparationFadeDuration = 0.25f;

        private ClinicRoundState observedState = (ClinicRoundState)(-1);
        private int displayedRoundSeconds = int.MinValue;
        private int displayedPreparationSeconds = int.MinValue;
        private int displayedScore = int.MinValue;
        private bool fadePreparationOverlay;
        private bool locomotionFrozen;
        private bool locomotionWasActive;

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.RoundStarted += OnRoundStarted;
                gameManager.RoundEnded += OnRoundEnded;
            }
            observedState = (ClinicRoundState)(-1);
            displayedRoundSeconds = int.MinValue;
            displayedPreparationSeconds = int.MinValue;
            displayedScore = int.MinValue;
            SynchronizeImmediately();
        }

        private void Start()
        {
            // Synchronize before default-order locomotion components process input.
            SynchronizeImmediately();
        }

        private void Update()
        {
            SynchronizeImmediately();

            if (fadePreparationOverlay)
                UpdatePreparationFade();
        }

        private void SynchronizeImmediately()
        {
            if (gameManager == null)
            {
                RestoreLocomotion();
                HidePreparationOverlay();
                return;
            }

            ClinicRoundState state = gameManager.RoundState;
            if (state != observedState)
                HandleStateChanged(state);

            UpdateRoundTimer(state);

            if (state == ClinicRoundState.Preparing)
                UpdatePreparationCountdown();
            else if (state == ClinicRoundState.Finished)
                UpdateTotalScore();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.RoundStarted -= OnRoundStarted;
                gameManager.RoundEnded -= OnRoundEnded;
            }
            RestoreLocomotion();
            HidePreparationOverlay();
        }

        private void OnRoundStarted() => SynchronizeImmediately();

        private void OnRoundEnded(int _) => SynchronizeImmediately();

        private void HandleStateChanged(ClinicRoundState state)
        {
            ClinicRoundState previousState = observedState;
            observedState = state;

            if (state == ClinicRoundState.Preparing)
            {
                displayedPreparationSeconds = int.MinValue;
                fadePreparationOverlay = false;
                ShowPreparationOverlay();
                FreezeLocomotion();
                return;
            }

            if (state == ClinicRoundState.Finished)
            {
                displayedScore = int.MinValue;
                fadePreparationOverlay = false;
                ShowFinishedOverlay();
                FreezeLocomotion();
                return;
            }

            if (state == ClinicRoundState.Idle)
            {
                fadePreparationOverlay = false;
                RestoreLocomotion();
                ShowWaitingOverlay();
                return;
            }

            RestoreLocomotion();

            if (state == ClinicRoundState.Running && previousState == ClinicRoundState.Preparing)
            {
                fadePreparationOverlay = preparationOverlay != null && preparationOverlay.alpha > 0f;
                if (!fadePreparationOverlay)
                    HidePreparationOverlay();
            }
            else
            {
                HidePreparationOverlay();
            }
        }

        private void UpdateRoundTimer(ClinicRoundState state)
        {
            if (roundTimerText == null)
                return;

            bool shouldShow = state != ClinicRoundState.Idle;
            GameObject timerRoot = roundTimerRoot != null ? roundTimerRoot : roundTimerText.gameObject;
            if (timerRoot.activeSelf != shouldShow)
                timerRoot.SetActive(shouldShow);

            if (!shouldShow)
                return;

            if (roundTimerText.gameObject != timerRoot && !roundTimerText.gameObject.activeSelf)
                roundTimerText.gameObject.SetActive(true);

            int wholeSeconds = state == ClinicRoundState.Finished
                ? 0
                : Mathf.Max(0, Mathf.CeilToInt(gameManager.RemainingRoundTime));
            if (wholeSeconds == displayedRoundSeconds)
                return;

            displayedRoundSeconds = wholeSeconds;
            int minutes = wholeSeconds / 60;
            int seconds = wholeSeconds % 60;
            roundTimerText.SetText("{0:0}:{1:00}", minutes, seconds);
        }

        private void UpdatePreparationCountdown()
        {
            if (preparationCountdownText == null)
                return;

            int wholeSeconds = Mathf.Max(1, Mathf.CeilToInt(gameManager.RemainingPreparationTime));
            if (wholeSeconds == displayedPreparationSeconds)
                return;

            displayedPreparationSeconds = wholeSeconds;
            preparationCountdownText.SetText("{0:0}", wholeSeconds);
        }

        private void UpdateTotalScore()
        {
            if (totalScoreText == null || displayedScore == gameManager.Score)
                return;

            displayedScore = gameManager.Score;
            totalScoreText.SetText("TỔNG ĐIỂM\n{0:0}", displayedScore);
        }

        private void UpdatePreparationFade()
        {
            if (preparationOverlay == null)
            {
                fadePreparationOverlay = false;
                return;
            }

            float fadeStep = preparationFadeDuration <= 0f
                ? 1f
                : Time.deltaTime / preparationFadeDuration;
            preparationOverlay.alpha = Mathf.MoveTowards(preparationOverlay.alpha, 0f, fadeStep);

            if (preparationOverlay.alpha > 0f)
                return;

            fadePreparationOverlay = false;
            preparationOverlay.gameObject.SetActive(false);
        }

        private void ShowPreparationOverlay()
        {
            if (preparationOverlay == null)
                return;

            SetTextObjectActive(preparationCountdownText, true);
            SetTextObjectActive(totalScoreText, false);
            preparationOverlay.gameObject.SetActive(true);
            preparationOverlay.alpha = 1f;
            preparationOverlay.interactable = false;
            preparationOverlay.blocksRaycasts = false;
        }

        private void ShowWaitingOverlay()
        {
            if (preparationOverlay == null)
                return;

            SetTextObjectActive(preparationCountdownText, true);
            SetTextObjectActive(totalScoreText, false);
            if (preparationCountdownText != null)
                preparationCountdownText.text = "ĐANG CHỜ BẮT ĐẦU";
            preparationOverlay.gameObject.SetActive(true);
            preparationOverlay.alpha = 1f;
            preparationOverlay.interactable = false;
            preparationOverlay.blocksRaycasts = false;
        }

        private void ShowFinishedOverlay()
        {
            if (preparationOverlay == null)
                return;

            SetTextObjectActive(preparationCountdownText, false);
            SetTextObjectActive(totalScoreText, true);
            preparationOverlay.gameObject.SetActive(true);
            preparationOverlay.alpha = 1f;
            preparationOverlay.interactable = false;
            preparationOverlay.blocksRaycasts = false;
        }

        private void HidePreparationOverlay()
        {
            fadePreparationOverlay = false;
            if (preparationOverlay == null)
                return;

            preparationOverlay.alpha = 0f;
            preparationOverlay.interactable = false;
            preparationOverlay.blocksRaycasts = false;
            preparationOverlay.gameObject.SetActive(false);
        }

        private static void SetTextObjectActive(TMP_Text text, bool isActive)
        {
            if (text != null && text.gameObject.activeSelf != isActive)
                text.gameObject.SetActive(isActive);
        }

        private void FreezeLocomotion()
        {
            if (locomotionRoot == null || locomotionFrozen)
                return;

            locomotionWasActive = locomotionRoot.activeSelf;
            locomotionFrozen = true;
            if (locomotionWasActive)
                locomotionRoot.SetActive(false);
        }

        private void RestoreLocomotion()
        {
            if (!locomotionFrozen)
                return;

            if (locomotionRoot != null && locomotionRoot.activeSelf != locomotionWasActive)
                locomotionRoot.SetActive(locomotionWasActive);

            locomotionFrozen = false;
        }

        private void OnValidate()
        {
            preparationFadeDuration = Mathf.Max(0f, preparationFadeDuration);
        }
    }
}
