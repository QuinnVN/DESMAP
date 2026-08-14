using UnityEngine;

namespace NCKH.VR.Doctor
{
    [DisallowMultipleComponent]
    public sealed class DoctorPatientPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject modelRoot;
        [SerializeField] private AudioSource voiceSource;

        public float PresentDialogue(DoctorDialogue dialogue, float fallbackDuration)
        {
            SetVisible(true);
            AudioClip clip = dialogue != null ? dialogue.AudioClip : null;
            if (voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = clip;
                if (clip != null)
                    voiceSource.Play();
            }

            return clip != null ? Mathf.Max(0.01f, clip.length) : Mathf.Max(0.01f, fallbackDuration);
        }

        public void SetVisible(bool visible)
        {
            if (modelRoot != null && modelRoot.activeSelf != visible)
                modelRoot.SetActive(visible);

            if (!visible && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = null;
            }
        }
    }
}
