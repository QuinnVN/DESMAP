using TMPro;
using UnityEngine;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorPatientPresenter : MonoBehaviour
    {
        [Header("Patient Visuals")]
        [SerializeField] private GameObject modelRoot;
        [SerializeField] private CanvasGroup dialogueCanvas;
        [SerializeField] private TMP_Text patientNameText;
        [SerializeField] private TMP_Text dialogueText;

        [Header("Voice")]
        [SerializeField] private AudioSource voiceSource;

        public float PresentDialogue(
            string patientName,
            DoctorDialogue dialogue,
            float fallbackDuration)
        {
            SetVisible(true);

            if (patientNameText != null)
                patientNameText.text = patientName ?? string.Empty;
            if (dialogueText != null)
                dialogueText.text = dialogue != null ? dialogue.Transcript : string.Empty;

            AudioClip clip = dialogue != null ? dialogue.AudioClip : null;
            if (voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                if (clip != null)
                    voiceSource.Play();
            }

            return clip != null
                ? Mathf.Max(0.01f, clip.length)
                : Mathf.Max(0.01f, fallbackDuration);
        }

        public void ShowConclusion(string conclusion)
        {
            if (dialogueText != null)
                dialogueText.text = conclusion ?? string.Empty;
        }

        public void SetDialogueVisible(bool visible)
        {
            if (dialogueCanvas == null)
                return;

            dialogueCanvas.alpha = visible ? 1f : 0f;
            dialogueCanvas.interactable = false;
            dialogueCanvas.blocksRaycasts = false;
        }

        public void SetVisible(bool visible)
        {
            if (modelRoot != null && modelRoot.activeSelf != visible)
                modelRoot.SetActive(visible);

            if (dialogueCanvas != null)
            {
                if (dialogueCanvas.gameObject.activeSelf != visible)
                    dialogueCanvas.gameObject.SetActive(visible);
                SetDialogueVisible(visible);
            }

            if (!visible && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = null;
            }
        }
    }
}
