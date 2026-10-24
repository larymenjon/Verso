using UnityEngine;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>Menu principal: Jogar e Sair. Enter/A também começa.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private string firstScene = SceneFlow.Island1;

        [Header("Créditos")]
        [SerializeField] private Button creditsButton;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private Button creditsClose;

        public void Bind(Button play, Button quit)
        {
            playButton = play;
            quitButton = quit;
        }

        public void BindCredits(Button open, GameObject panel, Button close)
        {
            creditsButton = open; creditsPanel = panel; creditsClose = close;
        }

        private void ShowCredits(bool show)
        {
            if (creditsPanel == null) return;
            creditsPanel.SetActive(show);
            if (show && creditsClose != null) creditsClose.Select();
            else if (!show && playButton != null) playButton.Select();
        }

        private void Update()
        {
            if (SettingsUI.IsOpen) return;
            if (creditsPanel != null && creditsPanel.activeSelf && GameInput.PausePressed) ShowCredits(false);
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            GameUI.IsPaused = false;
            GameInput.LockCursor(false);
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (quitButton != null) quitButton.onClick.AddListener(SceneFlow.Quit);
            if (creditsButton != null) creditsButton.onClick.AddListener(() => ShowCredits(true));
            if (creditsClose != null) creditsClose.onClick.AddListener(() => ShowCredits(false));
            if (creditsPanel != null) creditsPanel.SetActive(false);
#if UNITY_WEBGL
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        }

        private void Start()
        {
            if (playButton != null) playButton.Select();
        }

        private void Play()
        {
            Lives.Reset(); // jogo novo, 3 vidas
            IslandManager.ResetTips();
            // passa pela historinha (cena Intro) antes da primeira ilha, se ela estiver no Build
            bool hasIntro = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("Assets/Verso/Scenes/" + SceneFlow.Intro + ".unity") >= 0;
            string target = hasIntro ? SceneFlow.Intro : firstScene;
            if (!hasIntro) Debug.LogWarning("[Verso] Cena Intro não está no Build Settings; indo direto pra ilha.");
            SceneFlow.Load(target);
        }
    }
}
