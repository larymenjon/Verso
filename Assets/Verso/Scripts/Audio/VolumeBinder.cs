using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Aplica o volume das configurações num AudioSource de cena (música do menu, da intro,
    /// do final, vozes). O volume salvo no AudioSource vira o "volume base".
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VolumeBinder : MonoBehaviour
    {
        public enum Kind { Music, Effects }

        [SerializeField] private Kind kind = Kind.Music;
        [SerializeField, Range(0f, 1f)] private float baseVolume = -1f;
        private AudioSource src;

        public void Setup(Kind k, float volume) { kind = k; baseVolume = volume; }

        private void Awake()
        {
            src = GetComponent<AudioSource>();
            if (baseVolume < 0f) baseVolume = src.volume;
            Apply();
        }

        private void OnEnable() { GameSettings.VolumeChanged += Apply; Apply(); }
        private void OnDisable() { GameSettings.VolumeChanged -= Apply; }

        /// <summary>Muda o volume base (ex.: fade) mantendo o multiplicador das opções.</summary>
        public void SetBase(float v) { baseVolume = v; Apply(); }

        private void Apply()
        {
            if (src == null) src = GetComponent<AudioSource>();
            if (src == null) return;
            float mul = kind == Kind.Music ? GameSettings.MusicVolume : GameSettings.SfxVolume;
            src.volume = Mathf.Max(0f, baseVolume) * mul;
        }
    }
}
