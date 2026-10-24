using UnityEngine;

namespace Verso
{
    /// <summary>Créditos subindo devagar, em loop. Começa de baixo toda vez que abre.</summary>
    public class CreditsRoll : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private float speed = 70f;

        private float y;

        public void Bind(RectTransform c, RectTransform v) { content = c; viewport = v; }

        private void OnEnable()
        {
            if (content == null || viewport == null) return;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            y = -viewport.rect.height;   // começa logo abaixo da área visível
            Apply();
        }

        private void Update()
        {
            if (content == null || viewport == null) return;
            y += speed * Time.unscaledDeltaTime;
            if (y > content.rect.height) y = -viewport.rect.height; // recomeça
            Apply();
        }

        private void Apply() => content.anchoredPosition = new Vector2(0f, y);
    }
}
