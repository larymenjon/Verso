using System;
using System.Collections;
using UnityEngine;

namespace Verso
{
    public enum SfxType
    {
        Jump,
        Flip,
        FlipBlocked,
        Crystal,
        PortalOpen,
        PortalEnter,
        Plate,
        DoorOpen,
        DoorClose,
        Respawn,
        UIClick,
        Ending,
        KeyPickup
    }

    /// <summary>
    /// Música Dia/Noite com crossfade no flip e SFX por tipo (GDD 8).
    /// Todos os clips são opcionais: sem clip, nada toca e nada quebra.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Serializable]
        public struct SfxEntry
        {
            public SfxType type;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
        }

        [Header("Música")]
        [SerializeField] private AudioClip musicDay;
        [SerializeField] private AudioClip musicNight;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;
        [SerializeField] private float crossfade = 0.6f;
        [Tooltip("Ligado: a mesma música continua ao virar o mundo (usa só Music Day).")]
        [SerializeField] private bool keepMusicOnFlip = true;
        [Tooltip("Toca uma vez quando as 3 vidas acabam.")]
        [SerializeField] private AudioClip musicGameOver;

        [Header("SFX")]
        [SerializeField] private SfxEntry[] sfx = new SfxEntry[0];

        private AudioSource daySource;
        private AudioSource nightSource;
        private AudioSource sfxSource;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            Instance = this;
            daySource = CreateSource("MusicDay", true);
            nightSource = CreateSource("MusicNight", true);
            sfxSource = CreateSource("SFX", false);
            daySource.clip = musicDay;
            nightSource.clip = musicNight;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private float MusicVol => musicVolume * GameSettings.MusicVolume;

        private void OnVolumeChanged()
        {
            if (fadeRoutine != null) return; // o crossfade termina no volume certo
            bool day = keepMusicOnFlip || WorldManager.CurrentWorld == WorldType.Day;
            if (daySource.clip == musicGameOver && musicGameOver != null) { daySource.volume = MusicVol; return; }
            daySource.volume = day ? MusicVol : 0f;
            nightSource.volume = day ? 0f : MusicVol;
        }

        private void OnEnable()
        {
            GameSettings.VolumeChanged += OnVolumeChanged;
            WorldManager.WorldChanged += OnWorldChanged;
            WorldManager.FlipBlocked += OnFlipBlocked;
        }

        private void OnDisable()
        {
            GameSettings.VolumeChanged -= OnVolumeChanged;
            WorldManager.WorldChanged -= OnWorldChanged;
            WorldManager.FlipBlocked -= OnFlipBlocked;
        }

        private void Start()
        {
            bool day = keepMusicOnFlip || WorldManager.CurrentWorld == WorldType.Day;
            daySource.volume = day ? MusicVol : 0f;
            nightSource.volume = day ? 0f : MusicVol;
            if (daySource.clip != null) daySource.Play();
            if (nightSource.clip != null && !keepMusicOnFlip) nightSource.Play();
        }

        private AudioSource CreateSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        private void OnWorldChanged(WorldType w)
        {
            if (Time.timeSinceLevelLoad > 0.1f) Play(SfxType.Flip);
            if (keepMusicOnFlip) return; // mesma música nos dois mundos
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Crossfade(w == WorldType.Day));
        }

        private void OnFlipBlocked()
        {
            Play(SfxType.FlipBlocked);
        }

        private IEnumerator Crossfade(bool toDay)
        {
            float t = 0f;
            float d0 = daySource.volume, n0 = nightSource.volume;
            float d1 = toDay ? MusicVol : 0f, n1 = toDay ? 0f : MusicVol;
            while (t < crossfade)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / crossfade);
                daySource.volume = Mathf.Lerp(d0, d1, k);
                nightSource.volume = Mathf.Lerp(n0, n1, k);
                yield return null;
            }
            daySource.volume = d1;
            nightSource.volume = n1;
            fadeRoutine = null;
        }

        public void Play(SfxType type)
        {
            for (int i = 0; i < sfx.Length; i++)
            {
                if (sfx[i].type != type || sfx[i].clip == null) continue;
                sfxSource.PlayOneShot(sfx[i].clip, (sfx[i].volume <= 0f ? 1f : sfx[i].volume) * GameSettings.SfxVolume);
                return;
            }
        }

        /// <summary>Fim de jogo: abaixa a música da ilha e toca a do fim de jogo.</summary>
        public static void PlayGameOver()
        {
            if (Instance == null) return;
            if (Instance.fadeRoutine != null) Instance.StopCoroutine(Instance.fadeRoutine);
            Instance.daySource.Stop();
            Instance.nightSource.Stop();
            if (Instance.musicGameOver != null)
            {
                Instance.daySource.clip = Instance.musicGameOver;
                Instance.daySource.loop = false;
                Instance.daySource.volume = Instance.MusicVol;
                Instance.daySource.Play();
            }
        }

        public static void PlaySfx(SfxType type)
        {
            if (Instance != null) Instance.Play(type);
        }
    }
}
