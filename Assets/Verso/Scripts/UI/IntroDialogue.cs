using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Verso
{
    /// <summary>
    /// Historinha de introdução em forma de diálogo, antes da Ilha 1.
    /// Espaço, Enter, clique ou A avança (e completa a linha se ainda estiver escrevendo).
    /// Esc/Start ou o botão "Pular" vão direto pro jogo.
    /// As falas podem ser editadas no Inspector.
    /// </summary>
    public class IntroDialogue : MonoBehaviour
    {
        public enum Speaker { Lume, Alminha, Misterio }

        [System.Serializable]
        public class Line
        {
            public Speaker speaker;
            [TextArea(2, 4)] public string text;
            [TextArea(2, 4)] public string textEn;
            public Line() { }
            public Line(Speaker s, string t, string en) { speaker = s; text = t; textEn = en; }
            public string Current => GameSettings.IsEnglish && !string.IsNullOrEmpty(textEn) ? textEn : text;
        }

        [Header("Falas")]
        [SerializeField] private Line[] lines =
        {
            new Line(Speaker.Alminha, "Ei, você acordou! Achei que fosse dormir pra sempre.", "Hey, you're awake! I thought you'd sleep forever."),
            new Line(Speaker.Lume, "Onde eu tô? Por que tá tudo flutuando?", "Where am I? Why is everything floating?"),
            new Line(Speaker.Alminha, "Essas são as Ilhas do Verso. Cada uma existe duas vezes: no mundo dos Vivos e no Além.", "These are the Verso Islands. Each one exists twice: in the world of the Living and in the Beyond."),
            new Line(Speaker.Lume, "Duas vezes? No mesmo lugar?", "Twice? In the same place?"),
            new Line(Speaker.Alminha, "No mesmo lugar, mas nunca ao mesmo tempo. Uma ponte que existe aqui pode sumir do outro lado.", "Same place, but never at the same time. A bridge that's here might vanish on the other side."),
            new Line(Speaker.Alminha, "Aperte F e o mundo vira. O que estava escondido aparece.", "Press F and the world flips. Whatever was hidden shows up!"),
            new Line(Speaker.Lume, "E o que eu faço lá?", "And what do I do there?"),
            new Line(Speaker.Alminha, "Muitas almas como eu se perderam entre os dois lados. Junte 3 em cada ilha e o portal se abre.", "Lots of souls like me got lost between the two sides. Gather 3 on each island and the portal opens."),
            new Line(Speaker.Lume, "E se eu virar o mundo bem em cima de uma pedra?", "What if I flip the world right on top of a rock?"),
            new Line(Speaker.Alminha, "O mundo não deixa. Se tiver algo sólido do outro lado, ele treme e se recusa a virar.", "The world won't let you. If something solid is on the other side, it shakes and refuses to flip."),
            new Line(Speaker.Alminha, "Ah... e se sentir que alguém te segue do outro lado, não se assuste.", "Oh... and if you feel someone following you from the other side, don't be scared."),
            new Line(Speaker.Lume, "Alguém? Quem?", "Someone? Who?"),
            new Line(Speaker.Misterio, "...você vai descobrir.", "...you'll find out."),
            new Line(Speaker.Alminha, "Vamos! A primeira ilha tá esperando.", "Let's go! The first island is waiting."),
        };

        [Header("Nomes")]
        [SerializeField] private string lumeName = "Lume";
        [SerializeField] private string alminhaName = "Alminha";
        [SerializeField] private string misterioName = "???";

        [Header("Referências")]
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text nameText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Sprite alminhaSprite;
        [SerializeField] private Sprite misterioSprite;
        [SerializeField] private RectTransform nameTag;
        [SerializeField] private GameObject continueHint;
        [SerializeField] private Button skipButton;

        [Header("Áudio")]
        [Tooltip("Vozes (ElevenLabs), uma por fala, na mesma ordem das falas.")]
        [SerializeField] private AudioClip[] voices = new AudioClip[0];
        [Tooltip("Vozes em inglês, mesma ordem.")]
        [SerializeField] private AudioClip[] voicesEn = new AudioClip[0];
        [SerializeField] private AudioSource voiceSource;
        [Tooltip("Música de fundo do diálogo. Arraste o clip no AudioSource 'Music' (fica bem baixinha).")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.12f;

        [Header("Ritmo")]
        [SerializeField] private float charsPerSecond = 45f;
        [SerializeField] private float fadeTime = 0.6f;
        [SerializeField] private string nextScene = SceneFlow.Island1;

        private static readonly Color Dim = new Color(0.42f, 0.38f, 0.55f, 1f);
        private static readonly Color Shadow = new Color(0.05f, 0.04f, 0.12f, 1f);
        private bool leaving;

        public void Bind(CanvasGroup g, Text name, Text body, Image left, Image right, Sprite alma, Sprite misterio,
            RectTransform tag, GameObject hint, Button skip)
        {
            group = g; nameText = name; bodyText = body; leftPortrait = left; rightPortrait = right;
            alminhaSprite = alma; misterioSprite = misterio; nameTag = tag; continueHint = hint; skipButton = skip;
        }

        public void BindAudio(AudioSource voice, AudioSource music, AudioClip[] clips, AudioClip[] clipsEn)
        {
            voiceSource = voice; musicSource = music; voices = clips; voicesEn = clipsEn;
        }

        private void Awake()
        {
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * GameSettings.MusicVolume;
                musicSource.loop = true;
                if (musicSource.clip != null && !musicSource.isPlaying) musicSource.Play();
            }
            Time.timeScale = 1f;
            GameInput.LockCursor(false);
            if (skipButton != null) skipButton.onClick.AddListener(Leave);
        }

        private void Start()
        {
            if (bodyText == null || nameText == null)
                Debug.LogWarning("[Verso] IntroDialogue sem referências de texto. Rode Verso > Criar Intro (diálogo).");
            StartCoroutine(Run());
        }

        private static bool AdvancePressed =>
            GameInput.JumpPressed || GameInput.ClickPressed ||
            (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame));

        private void Update()
        {
            if (!leaving && GameInput.PausePressed) Leave();
        }

        private IEnumerator Run()
        {
            if (group != null)
            {
                group.alpha = 0f;
                for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
                {
                    group.alpha = t / fadeTime;
                    yield return null;
                }
                group.alpha = 1f;
            }

            foreach (var line in lines)
            {
                if (leaving) yield break;
                ShowSpeaker(line.speaker);
                int idx = System.Array.IndexOf(lines, line);
                if (voiceSource != null)
                {
                    voiceSource.Stop();
                    var set = GameSettings.IsEnglish ? voicesEn : voices;
                    if (set != null && idx < set.Length && set[idx] != null)
                    {
                        voiceSource.volume = GameSettings.SfxVolume;
                        voiceSource.clip = set[idx];
                        voiceSource.Play();
                    }
                }
                if (continueHint != null) continueHint.SetActive(false);

                // efeito máquina de escrever
                bodyText.text = "";
                float shown = 0f;
                string lineText = line.Current;
                int total = lineText.Length;
                yield return null;
                while (shown < total)
                {
                    if (leaving) yield break;
                    if (AdvancePressed) { shown = total; break; }
                    shown += charsPerSecond * Time.unscaledDeltaTime;
                    bodyText.text = lineText.Substring(0, Mathf.Min(total, Mathf.FloorToInt(shown)));
                    yield return null;
                }
                bodyText.text = lineText;
                if (continueHint != null) continueHint.SetActive(true);

                yield return null;
                while (!AdvancePressed)
                {
                    if (leaving) yield break;
                    yield return null;
                }
                UIAudio.Click();
            }
            Leave();
        }

        private void ShowSpeaker(Speaker s)
        {
            bool left = s == Speaker.Lume;
            nameText.text = Loc.Tr(s == Speaker.Lume ? lumeName : s == Speaker.Alminha ? alminhaName : misterioName);

            if (rightPortrait != null)
            {
                rightPortrait.sprite = s == Speaker.Misterio ? misterioSprite : alminhaSprite;
                rightPortrait.color = s == Speaker.Misterio ? Shadow : (left ? Dim : Color.white);
                rightPortrait.rectTransform.localScale = Vector3.one * (left ? 0.92f : 1f);
            }
            if (leftPortrait != null)
            {
                leftPortrait.color = left ? Color.white : Dim;
                leftPortrait.rectTransform.localScale = Vector3.one * (left ? 1f : 0.92f);
            }
            if (nameTag != null)
            {
                // etiqueta do nome fica do lado de quem fala
                Vector2 a = left ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
                nameTag.anchorMin = nameTag.anchorMax = a;
                nameTag.pivot = new Vector2(left ? 0f : 1f, 0.5f);
                nameTag.anchoredPosition = new Vector2(left ? 120f : -120f, 0f);
            }
        }

        private void Leave()
        {
            if (leaving) return;
            leaving = true;
            StopAllCoroutines();
            StartCoroutine(FadeOutAndLoad());
        }

        private IEnumerator FadeOutAndLoad()
        {
            if (voiceSource != null) voiceSource.Stop();
            float m0 = musicSource != null ? musicSource.volume : 0f;
            if (group != null)
            {
                float start = group.alpha;
                for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
                {
                    group.alpha = Mathf.Lerp(start, 0f, t / fadeTime);
                    if (musicSource != null) musicSource.volume = Mathf.Lerp(m0, 0f, t / fadeTime);
                    yield return null;
                }
            }
            SceneFlow.Load(nextScene);
        }
    }
}
