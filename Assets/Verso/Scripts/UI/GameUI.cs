using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>
    /// HUD (almas coletadas + mundo atual), mensagens de ilha, menu de pausa e fade.
    /// Visual definido no Figma (arquivo "Verso · UI"). As referências são ligadas
    /// pelo menu Verso > Aplicar UI no prefab GameUI.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }
        public static bool IsPaused { get; set; }

        [Serializable]
        public class Refs
        {
            [Header("HUD")]
            public GameObject hudRoot;
            public Text crystalText;
            public Image worldBadge;
            public Image worldBadgeIcon;
            public Text worldText;
            public Image blockedRing;
            public Button pauseHudButton;

            [Header("Sprites do mundo")]
            public Sprite badgeDay;
            public Sprite badgeNight;
            public Sprite iconDay;
            public Sprite iconNight;

            [Header("Mensagem")]
            public CanvasGroup messageGroup;
            public Text messageTitle;
            public Text messageText;

            [Header("Pausa")]
            public GameObject pausePanel;
            public Button resumeButton;
            public Button restartButton;
            public Button menuButton;

            [Header("Vidas")]
            public Image[] lifeIcons;
            public Sprite lifeFull;
            public Sprite lifeEmpty;

            [Header("Fim de jogo")]
            public GameObject gameOverPanel;
            public Button retryButton;
            public Button gameOverMenuButton;

            [Header("Dicas da Alminha")]
            public CanvasGroup tipGroup;
            public Text tipText;
            public GameObject tipHint;

            [Header("Fade")]
            public Image fadeImage;
        }

        [SerializeField] private Refs ui = new Refs();
        [SerializeField] private string dayLabel = "VIVOS";
        [SerializeField] private string nightLabel = "ALÉM";
        [SerializeField] private Color dayLabelColor = new Color(1f, 0.70f, 0.28f);
        [SerializeField] private Color nightLabelColor = new Color(0.49f, 0.98f, 0.91f);
        [SerializeField] private float fadeInDuration = 0.6f;

        private Coroutine messageRoutine;
        private Coroutine fadeRoutine;
        private Coroutine blockedRoutine;
        private Coroutine popRoutine;

        public void Bind(Refs refs) { ui = refs; }

        public bool IsGameOverShown { get; private set; }

        private void Awake()
        {
            Instance = this;
            IsPaused = false;
            Time.timeScale = 1f;

            if (ui.pausePanel != null) ui.pausePanel.SetActive(false);
            if (ui.messageGroup != null) ui.messageGroup.alpha = 0f;
            if (ui.blockedRing != null) SetAlpha(ui.blockedRing, 0f);
            if (ui.resumeButton != null) ui.resumeButton.onClick.AddListener(() => SetPaused(false));
            if (ui.pauseHudButton != null) ui.pauseHudButton.onClick.AddListener(() => SetPaused(true));
            if (ui.restartButton != null) ui.restartButton.onClick.AddListener(() => SceneFlow.ReloadCurrent());
            if (ui.menuButton != null) ui.menuButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.MainMenu));
            if (ui.gameOverPanel != null) ui.gameOverPanel.SetActive(false);
            // fim de jogo: recomeça da Ilha 1 com 3 vidas
            if (ui.retryButton != null) ui.retryButton.onClick.AddListener(() => { Lives.Reset(); SceneFlow.Load(SceneFlow.Island1); });
            if (ui.tipGroup != null) { ui.tipGroup.alpha = 0f; ui.tipGroup.gameObject.SetActive(false); }
            if (ui.gameOverMenuButton != null) ui.gameOverMenuButton.onClick.AddListener(() => { Lives.Reset(); SceneFlow.Load(SceneFlow.MainMenu); });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldManager.WorldChanged += OnWorldChanged;
            WorldManager.FlipBlocked += OnFlipBlocked;
            GameSettings.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged() => ApplyWorld(WorldManager.CurrentWorld);

        private void OnDisable()
        {
            WorldManager.WorldChanged -= OnWorldChanged;
            WorldManager.FlipBlocked -= OnFlipBlocked;
            GameSettings.LanguageChanged -= OnLanguageChanged;
        }

        private void Start()
        {
            ApplyWorld(WorldManager.CurrentWorld);
            RefreshLives(Lives.Current);
            if (ui.fadeImage != null)
            {
                ui.fadeImage.color = new Color(0.06f, 0.04f, 0.1f, 1f);
                fadeRoutine = StartCoroutine(FadeRoutine(1f, 0f, fadeInDuration, null));
            }
        }

        private void Update()
        {
            if (IsGameOverShown || TipsShowing) return;
            // Esc com as configurações abertas só fecha as configurações
            if (SettingsUI.IsOpen || SettingsUI.ClosedFrame == Time.frameCount) return;
            if (GameInput.PausePressed) SetPaused(!IsPaused);

            // Clique na tela devolve o cursor ao jogo (útil no WebGL).
            if (!IsPaused && Cursor.lockState != CursorLockMode.Locked && GameInput.ClickPressed && PlayerController.Instance != null)
                GameInput.LockCursor(true);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (ui.pausePanel != null) ui.pausePanel.SetActive(paused);
            GameInput.LockCursor(!paused);
            if (paused && ui.resumeButton != null) ui.resumeButton.Select();
        }

        // ------------------------------------------------------------ HUD

        public void SetCrystals(int collected, int required)
        {
            if (ui.crystalText == null) return;
            ui.crystalText.text = $"{collected} / {required}";
            if (collected > 0)
            {
                if (popRoutine != null) StopCoroutine(popRoutine);
                popRoutine = StartCoroutine(Pop(ui.crystalText.transform.parent as RectTransform));
            }
        }

        // ------------------------------------------------------------ Dicas da Alminha (início da ilha)

        public bool TipsShowing { get; private set; }

        /// <summary>Mostra as dicas uma por uma; o jogador fica parado até terminar.</summary>
        public void ShowTips(string[] pt, string[] en, Action onDone)
        {
            string[] lines = GameSettings.IsEnglish && en != null && en.Length > 0 ? en : pt;
            if (ui.tipGroup == null || ui.tipText == null || lines == null || lines.Length == 0) { onDone?.Invoke(); return; }
            StartCoroutine(TipsRoutine(lines, onDone));
        }

        private static bool AdvancePressed =>
            GameInput.JumpPressed || GameInput.ClickPressed ||
            (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame);

        private IEnumerator TipsRoutine(string[] lines, Action onDone)
        {
            TipsShowing = true;
            var player = PlayerController.Instance;
            if (player != null) player.InputEnabled = false;
            GameInput.LockCursor(false);

            ui.tipGroup.gameObject.SetActive(true);
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { ui.tipGroup.alpha = t / 0.3f; yield return null; }
            ui.tipGroup.alpha = 1f;

            foreach (string line in lines)
            {
                if (ui.tipHint != null) ui.tipHint.SetActive(false);
                ui.tipText.text = "";
                float shown = 0f;
                yield return null;
                while (shown < line.Length)
                {
                    if (AdvancePressed) break;
                    shown += 55f * Time.unscaledDeltaTime;
                    ui.tipText.text = line.Substring(0, Mathf.Min(line.Length, Mathf.FloorToInt(shown)));
                    yield return null;
                }
                ui.tipText.text = line;
                if (ui.tipHint != null) ui.tipHint.SetActive(true);
                yield return null;
                while (!AdvancePressed) yield return null;
                UIAudio.Click();
            }

            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime) { ui.tipGroup.alpha = 1f - t / 0.25f; yield return null; }
            ui.tipGroup.alpha = 0f;
            ui.tipGroup.gameObject.SetActive(false);
            yield return null; // o mesmo Espaço não vira pulo
            if (player != null) player.InputEnabled = true;
            TipsShowing = false;
            onDone?.Invoke();
        }

        // ------------------------------------------------------------ Vidas e fim de jogo

        private void RefreshLives(int current)
        {
            if (ui.lifeIcons == null) return;
            for (int i = 0; i < ui.lifeIcons.Length; i++)
            {
                if (ui.lifeIcons[i] == null) continue;
                ui.lifeIcons[i].sprite = i < current ? ui.lifeFull : ui.lifeEmpty;
            }
        }

        /// <summary>Chamado pelo jogador ao perder uma vida (ainda sobrou alguma).</summary>
        public void OnLifeLost(int remaining)
        {
            RefreshLives(remaining);
            int idx = Mathf.Clamp(remaining, 0, (ui.lifeIcons?.Length ?? 1) - 1);
            if (ui.lifeIcons != null && ui.lifeIcons.Length > 0 && ui.lifeIcons[idx] != null)
            {
                if (popRoutine != null) StopCoroutine(popRoutine);
                popRoutine = StartCoroutine(Pop(ui.lifeIcons[idx].rectTransform));
            }
            string msg = remaining == 1 ? Loc.Tr("Cuidado! Só resta 1 vida.") : Loc.Format("Ops! Restam {0} vidas.", remaining);
            ShowMessage("", msg, 1.4f);
        }

        public void ShowGameOver()
        {
            if (IsGameOverShown) return;
            IsGameOverShown = true;
            RefreshLives(0);
            if (ui.pausePanel != null) ui.pausePanel.SetActive(false);
            if (ui.messageGroup != null) ui.messageGroup.alpha = 0f;
            IsPaused = true;
            Time.timeScale = 0f;
            GameInput.LockCursor(false);
            if (ui.gameOverPanel != null) ui.gameOverPanel.SetActive(true);
            AudioManager.PlayGameOver();
            if (ui.retryButton != null) ui.retryButton.Select();
        }

        public void SetHudVisible(bool visible)
        {
            if (ui.hudRoot != null) ui.hudRoot.SetActive(visible);
        }

        private void OnWorldChanged(WorldType w)
        {
            ApplyWorld(w);
            if (Time.timeSinceLevelLoad > 0.1f && ui.worldBadge != null)
            {
                if (popRoutine != null) StopCoroutine(popRoutine);
                popRoutine = StartCoroutine(Pop(ui.worldBadge.rectTransform));
            }
        }

        private void ApplyWorld(WorldType w)
        {
            bool day = w == WorldType.Day;
            if (ui.worldBadge != null)
            {
                Sprite s = day ? ui.badgeDay : ui.badgeNight;
                if (s != null) ui.worldBadge.sprite = s;
            }
            if (ui.worldBadgeIcon != null)
            {
                Sprite s = day ? ui.iconDay : ui.iconNight;
                if (s != null) ui.worldBadgeIcon.sprite = s;
            }
            if (ui.worldText != null)
            {
                ui.worldText.text = Loc.Tr(day ? dayLabel : nightLabel);
                ui.worldText.color = day ? dayLabelColor : nightLabelColor;
            }
        }

        private void OnFlipBlocked()
        {
            if (ui.worldBadge == null) return;
            if (blockedRoutine != null) StopCoroutine(blockedRoutine);
            blockedRoutine = StartCoroutine(BlockedRoutine());
        }

        private IEnumerator BlockedRoutine()
        {
            RectTransform rt = ui.worldBadge.rectTransform;
            Vector2 basePos = rt.anchoredPosition;
            const float d = 0.35f;
            float t = 0f;
            while (t < d)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / d;
                if (ui.blockedRing != null) SetAlpha(ui.blockedRing, k);
                rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 80f) * 8f * k, 0f);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            if (ui.blockedRing != null) SetAlpha(ui.blockedRing, 0f);
        }

        private static IEnumerator Pop(RectTransform rt)
        {
            if (rt == null) yield break;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI);
                rt.localScale = Vector3.one * (1f + 0.15f * k);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private static void SetAlpha(Graphic g, float a)
        {
            Color c = g.color; c.a = a; g.color = c;
        }

        // ------------------------------------------------------------ Mensagens

        public void ShowMessage(string text, float duration)
        {
            ShowMessage("", text, duration);
        }

        public void ShowMessage(string title, string text, float duration)
        {
            if (ui.messageText == null || ui.messageGroup == null) return;
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(title, text, duration));
        }

        private IEnumerator MessageRoutine(string title, string text, float duration)
        {
            ui.messageText.text = Loc.Tr(text);
            if (ui.messageTitle != null)
            {
                ui.messageTitle.text = Loc.Tr(title);
                ui.messageTitle.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            float t = 0f;
            while (t < 0.5f) { t += Time.unscaledDeltaTime; ui.messageGroup.alpha = t / 0.5f; yield return null; }
            ui.messageGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(duration);
            t = 0f;
            while (t < 0.8f) { t += Time.unscaledDeltaTime; ui.messageGroup.alpha = 1f - t / 0.8f; yield return null; }
            ui.messageGroup.alpha = 0f;
        }

        // ------------------------------------------------------------ Fade

        public void FadeOut(float duration, Action onDone)
        {
            FadeOut(duration, onDone, new Color(0.06f, 0.04f, 0.1f));
        }

        public void FadeOut(float duration, Action onDone, Color color)
        {
            if (ui.fadeImage == null) { onDone?.Invoke(); return; }
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            Color c = color; c.a = ui.fadeImage.color.a;
            ui.fadeImage.color = c;
            fadeRoutine = StartCoroutine(FadeRoutine(ui.fadeImage.color.a, 1f, duration, onDone));
        }

        private IEnumerator FadeRoutine(float from, float to, float duration, Action onDone)
        {
            ui.fadeImage.raycastTarget = to > 0.5f;
            float t = 0f;
            Color c = ui.fadeImage.color;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                ui.fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            ui.fadeImage.color = c;
            onDone?.Invoke();
        }
    }
}
