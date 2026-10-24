using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>Tela final: frase do twist aparecendo devagar e botão para o menu.</summary>
    public class EndScreenUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup textGroup;
        [SerializeField] private CanvasGroup buttonGroup;
        [SerializeField] private Button menuButton;
        [SerializeField] private float textDelay = 0.8f;
        [SerializeField] private float textFade = 2.5f;
        [SerializeField] private float buttonDelay = 2f;

        public void Bind(CanvasGroup text, CanvasGroup button, Button menu)
        {
            textGroup = text;
            buttonGroup = button;
            menuButton = menu;
        }

        private void Awake()
        {
            Time.timeScale = 1f;
            GameInput.LockCursor(false);
            if (textGroup != null) textGroup.alpha = 0f;
            if (buttonGroup != null) { buttonGroup.alpha = 0f; buttonGroup.interactable = false; }
            if (menuButton != null) menuButton.onClick.AddListener(() => SceneFlow.Load(SceneFlow.MainMenu));
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(textDelay);
            yield return Fade(textGroup, textFade);
            yield return new WaitForSeconds(buttonDelay);
            yield return Fade(buttonGroup, 0.8f);
            if (buttonGroup != null) buttonGroup.interactable = true;
            if (menuButton != null) menuButton.Select();
        }

        private static IEnumerator Fade(CanvasGroup g, float d)
        {
            if (g == null) yield break;
            float t = 0f;
            while (t < d) { t += Time.deltaTime; g.alpha = t / d; yield return null; }
            g.alpha = 1f;
        }
    }
}
