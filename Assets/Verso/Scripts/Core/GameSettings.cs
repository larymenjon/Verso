using UnityEngine;

namespace Verso
{
    public enum Language { English, Portugues }

    /// <summary>
    /// Opções do jogador (engrenagem): idioma, volume da música e dos efeitos.
    /// Salvas em PlayerPrefs. O jogo começa em inglês.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyLang = "verso.lang";
        private const string KeyMusic = "verso.music";
        private const string KeySfx = "verso.sfx";

        public static Language Language { get; private set; } = Language.English;
        public static float MusicVolume { get; private set; } = 0.8f;
        public static float SfxVolume { get; private set; } = 1f;

        public static bool IsEnglish => Language == Language.English;

        public static event System.Action LanguageChanged;
        public static event System.Action VolumeChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Language = (Language)PlayerPrefs.GetInt(KeyLang, (int)Language.English);
            MusicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfx, 1f);
        }

        public static void SetLanguage(Language lang)
        {
            if (Language == lang) return;
            Language = lang;
            PlayerPrefs.SetInt(KeyLang, (int)lang);
            PlayerPrefs.Save();
            LanguageChanged?.Invoke();
        }

        public static void SetMusicVolume(float v)
        {
            MusicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeyMusic, MusicVolume);
            VolumeChanged?.Invoke();
        }

        public static void SetSfxVolume(float v)
        {
            SfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeySfx, SfxVolume);
            VolumeChanged?.Invoke();
        }
    }
}
