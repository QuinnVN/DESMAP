using UnityEngine;

namespace NCKH.VR.GameControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SceneBgmPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip bgmClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField] private bool playOnStart = true;

        private AudioSource audioSource;

        public AudioClip Clip => bgmClip;
        public float Volume => volume;

        private void Awake() => ConfigureAudioSource();

        private void Start()
        {
            if (playOnStart)
                PlayBgm();
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            if (TryGetComponent(out AudioSource source))
                ConfigureAudioSource(source);
        }

        public void PlayBgm()
        {
            ConfigureAudioSource();
            if (bgmClip == null)
                return;

            if (audioSource.clip != bgmClip)
                audioSource.clip = bgmClip;

            audioSource.Play();
        }

        public void SetVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            ConfigureAudioSource();
        }

        public void StopBgm()
        {
            if (audioSource != null)
                audioSource.Stop();
        }

        private void ConfigureAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            ConfigureAudioSource(audioSource);
        }

        private void ConfigureAudioSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = volume;
        }
    }
}
