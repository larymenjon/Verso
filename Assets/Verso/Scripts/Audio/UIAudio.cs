using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Sons dos botões (clique e hover), em qualquer cena, inclusive no menu (que não tem AudioManager).
    /// Os clips ficam em Assets/Verso/Resources/UI. Volume segue "Efeitos" das configurações.
    /// </summary>
    public static class UIAudio
    {
        private static AudioSource source;
        private static AudioClip click, hover;
        private static float lastHover = -1f;

        private static AudioSource Source
        {
            get
            {
                if (source != null) return source;
                var go = new GameObject("UIAudio");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.ignoreListenerPause = true; // toca mesmo com o jogo pausado
                click = Resources.Load<AudioClip>("UI/ui_click");
                hover = Resources.Load<AudioClip>("UI/ui_hover");
                return source;
            }
        }

        public static void Click()
        {
            var s = Source;
            if (click != null) s.PlayOneShot(click, 0.9f * GameSettings.SfxVolume);
        }

        public static void Hover()
        {
            // evita metralhar o som quando o mouse passa rápido por vários botões
            if (Time.unscaledTime - lastHover < 0.06f) return;
            lastHover = Time.unscaledTime;
            var s = Source;
            if (hover != null) s.PlayOneShot(hover, 0.6f * GameSettings.SfxVolume);
        }
    }
}
