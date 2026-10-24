using UnityEngine;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>
    /// Troca o texto de um Text quando muda o idioma. Guarda o original em português
    /// (o que foi escrito no builder) e usa Loc.Tr pra mostrar a versão certa.
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [TextArea] [SerializeField] private string sourcePt;
        private Text text;

        public void SetSource(string pt)
        {
            sourcePt = pt;
            Apply();
        }

        private void Awake()
        {
            text = GetComponent<Text>();
            if (string.IsNullOrEmpty(sourcePt)) sourcePt = text.text;
        }

        private void OnEnable()
        {
            GameSettings.LanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameSettings.LanguageChanged -= Apply;
        }

        private void Apply()
        {
            if (text == null) text = GetComponent<Text>();
            if (text != null) text.text = Loc.Tr(sourcePt);
        }
    }
}
