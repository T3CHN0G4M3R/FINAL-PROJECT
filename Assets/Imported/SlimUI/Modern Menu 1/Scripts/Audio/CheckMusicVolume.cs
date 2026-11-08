using UnityEngine;

namespace SlimUI.ModernMenu
{
    [RequireComponent(typeof(AudioSource))]
    public class CheckMusicVolume : MonoBehaviour
    {
        private AudioSource _musicSource;

        private void Awake() => _musicSource = GetComponent<AudioSource>();

        private void OnEnable()
        {
            GameSettings.Changed -= UpdateVolume;
            GameSettings.Changed += UpdateVolume;
            UpdateVolume();
        }

        public void Start()
        {
            GameSettings.Changed -= UpdateVolume;
            GameSettings.Changed += UpdateVolume;
            UpdateVolume();
        }

        private void OnDisable() => GameSettings.Changed -= UpdateVolume;
        public void UpdateVolume() => _musicSource.volume = GameSettings.MusicVolume;
    }
}
