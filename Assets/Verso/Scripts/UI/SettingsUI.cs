using UnityEngine;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>
    /// Caixa de configurações (engrenagem): idioma (English/Português) e volume da música e dos efeitos.
    /// Usada no menu e na pausa. Esc ou "Voltar" fecha.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        /// <summary>Frame em que fechou (pra o mesmo Esc não fechar a pausa junto).</summary>
        public static int ClosedFrame { get; private set; } = -1;

        [SerializeField] private GameObject panel;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button englishButton;
        [SerializeField] private Button portugueseButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite normalSprite;

        private Selectable returnFocus;

        public void Bind(GameObject p, Button open, Button close, Button en, Button pt, Slider music, Slider sfx, Sprite selected, Sprite normal)
        {
            panel = p; openButton = open; closeButton = close; englishButton = en; portugueseButton = pt;
            musicSlider = music; sfxSlider = sfx; selectedSprite = selected; normalSprite = normal;
        }

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (englishButton != null) englishButton.onClick.AddListener(() => Pick(Language.English));
            if (portugueseButton != null) portugueseButton.onClick.AddListener(() => Pick(Language.Portugues));
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                musicSlider.onValueChanged.AddListener(GameSettings.SetMusicVolume);
            }
            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
                sfxSlider.onValueChanged.AddListener(v =>
                {
                    GameSettings.SetSfxVolume(v);
                });
            }
            RefreshLanguage();
        }

        private void OnDestroy()
        {
            IsOpen = false;
        }

        private void Update()
        {
            if (IsOpen && GameInput.PausePressed) Close();
        }

        public void Open()
        {
            if (panel == null) return;
            returnFocus = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null
                ? UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() : null;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            IsOpen = true;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            RefreshLanguage();
            if (closeButton != null) closeButton.Select();
        }

        public void Close()
        {
            if (panel == null || !panel.activeSelf) return;
            panel.SetActive(false);
            IsOpen = false;
            ClosedFrame = Time.frameCount;
            if (returnFocus != null) returnFocus.Select();
        }

        private void Pick(Language lang)
        {
            GameSettings.SetLanguage(lang);
            RefreshLanguage();
        }

        private void RefreshLanguage()
        {
            SetSelected(englishButton, GameSettings.Language == Language.English);
            SetSelected(portugueseButton, GameSettings.Language == Language.Portugues);
        }

        private void SetSelected(Button b, bool on)
        {
            if (b == null) return;
            var img = b.targetGraphic as Image;
            if (img != null && selectedSprite != null && normalSprite != null) img.sprite = on ? selectedSprite : normalSprite;
        }
    }
}
