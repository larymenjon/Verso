using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Verso
{
    /// <summary>
    /// Piloto automático da demo (vídeo de gameplay + screenshots).
    /// Liga sozinho quando o menu Verso > Gravar demo entra no Play (PlayerPrefs "verso.demo" = 1).
    /// Joga um trecho de cada ilha mostrando as mecânicas, tira screenshots e grava
    /// a linha do tempo das seções em Recordings/demo_timeline.json (pra encaixar a narração).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class DemoDirector : MonoBehaviour, GameInput.IDemoInput
    {
        public const string PrefKey = "verso.demo";
        public static string OutFolder => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Recordings");

        // ---- input injetado
        private Vector2 move, look;
        private int jumpFrame = -1, flipFrame = -1, pauseFrame = -1;
        private float jumpHeldUntil;
        public Vector2 Move => move;
        public Vector2 Look => look;
        public bool Jump => Time.frameCount == jumpFrame;
        public bool JumpHeld => Time.unscaledTime < jumpHeldUntil;
        public bool Flip => Time.frameCount == flipFrame;
        public bool Pause => Time.frameCount == pauseFrame;

        private float t0;
        private readonly List<string> timeline = new List<string>();
        private Language previousLanguage;
        private int shot;
        private bool steerCamera = true;
        private DemoCapture capture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) != 1) return;
            PlayerPrefs.SetInt(PrefKey, 0);
            var go = new GameObject("DemoDirector");
            DontDestroyOnLoad(go);
            go.AddComponent<DemoDirector>();
        }

        private void Awake()
        {
            GameInput.Demo = this;
            Directory.CreateDirectory(OutFolder);
            previousLanguage = GameSettings.Language;
            GameSettings.SetLanguage(Language.English);
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(GameInput.Demo, this)) GameInput.Demo = null;
            GameSettings.SetLanguage(previousLanguage);
        }

        private void Start()
        {
            t0 = Time.unscaledTime;
            capture = gameObject.AddComponent<DemoCapture>();
            capture.Begin(t0);
            StartCoroutine(Run());
        }

        // ================================================================ helpers

        private void Mark(string id)
        {
            float t = Time.unscaledTime - t0;
            timeline.Add($"  {{ \"id\": \"{id}\", \"t\": {t.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} }}");
            Debug.Log($"[Verso Demo] {t:0.0}s  {id}");
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            shot++;
            string path = Path.Combine(OutFolder, $"screenshot_{shot:00}_{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Verso Demo] screenshot {path}");
        }

        private void PressJump(float hold = 0.25f) { jumpFrame = Time.frameCount + 1; jumpHeldUntil = Time.unscaledTime + hold; }
        private void PressFlip() { flipFrame = Time.frameCount + 1; }
        private void PressPause() { pauseFrame = Time.frameCount + 1; }

        private static IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        private IEnumerator LoadScene(string name)
        {
            SceneFlow.Load(name);
            yield return null;
            while (SceneManager.GetActiveScene().name != name) yield return null;
            yield return Wait(0.3f);
        }

        /// <summary>Espera as dicas da Alminha aparecerem e passa uma por uma.</summary>
        private IEnumerator SkipTips(float perLine)
        {
            float wait = 0f;
            while ((GameUI.Instance == null || !GameUI.Instance.TipsShowing) && wait < 1.5f) { wait += Time.unscaledDeltaTime; yield return null; }
            while (GameUI.Instance != null && GameUI.Instance.TipsShowing)
            {
                yield return Wait(perLine);
                PressJump(0f);
                yield return null;
            }
            yield return Wait(0.3f);
        }

        private Vector3 PlayerPos => PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;

        /// <summary>Converte uma direção do mundo no input relativo à câmera.</summary>
        private Vector2 ToInput(Vector3 worldDir)
        {
            var cam = Camera.main;
            if (cam == null) return new Vector2(worldDir.x, worldDir.z);
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(worldDir, r), Vector3.Dot(worldDir, f)), 1f);
        }

        private void LateUpdate()
        {
            // câmera segue suave por trás da direção em que o jogador anda
            look = Vector2.zero;
            var cam = Camera.main;
            var p = PlayerController.Instance;
            if (!steerCamera || cam == null || p == null) return;
            Vector3 v = p.Velocity; v.y = 0f;
            if (v.sqrMagnitude < 1f) return;
            float desired = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            float delta = Mathf.DeltaAngle(cam.transform.eulerAngles.y, desired);
            look = new Vector2(Mathf.Clamp(delta * 2.2f * Time.unscaledDeltaTime, -3f, 3f), 0f);
        }

        /// <summary>Anda até (x, z). Opcional: pula quando passar de jumpAt e vira o mundo no ar.</summary>
        private IEnumerator MoveTo(float x, float z, float timeout = 8f, float stopDist = 0.35f, bool stopAtEnd = true)
        {
            float t = 0f;
            while (t < timeout)
            {
                Vector3 d = new Vector3(x, 0f, z) - new Vector3(PlayerPos.x, 0f, PlayerPos.z);
                if (d.magnitude < stopDist) break;
                move = stopAtEnd ? ToInput(d.normalized * Mathf.Clamp01(d.magnitude / 0.8f + 0.35f)) : ToInput(d.normalized);
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (stopAtEnd) move = Vector2.zero;
        }

        /// <summary>Corre pra (x, z), pula ao chegar perto do ponto de pulo e vira o mundo no ar.</summary>
        private IEnumerator JumpFlipTo(float jumpX, float jumpZ, float landX, float landZ, float flipDelay = 0.18f, bool flip = true)
        {
            yield return MoveTo(jumpX, jumpZ, 6f, 0.45f, false); // chega correndo, sem frear
            Vector3 dir = new Vector3(landX - PlayerPos.x, 0f, landZ - PlayerPos.z).normalized;
            move = ToInput(dir);
            PressJump(0.4f);
            yield return Wait(flipDelay);
            if (flip) PressFlip();
            yield return MoveTo(landX, landZ, 3f, 0.4f);
            yield return Wait(0.2f);
        }

        // ================================================================ roteiro

        private IEnumerator Run()
        {
            // ---- Menu
            yield return Wait(0.5f);
            if (SceneManager.GetActiveScene().name != SceneFlow.MainMenu) yield return LoadScene(SceneFlow.MainMenu);
            Mark("menu");
            yield return Wait(1.2f);
            yield return Shot("menu");
            yield return Wait(2.8f);

            // ---- Intro (duas falas e pula)
            Lives.Reset();
            IslandManager.ResetTips();
            yield return LoadScene(SceneFlow.Intro);
            Mark("intro");
            yield return Wait(4.2f); PressJump(0f);
            yield return Wait(0.4f); PressJump(0f);
            yield return Wait(3.4f);
            yield return Shot("intro");
            yield return Wait(1.5f); PressJump(0f);
            yield return Wait(3.0f);
            PressPause(); // Esc pula o resto
            while (SceneManager.GetActiveScene().name != SceneFlow.Island1) yield return null;
            yield return Wait(0.3f);

            // ---- Ilha 1: flip, almas, chave
            Mark("island1_tips");
            yield return SkipTips(2.6f);
            Mark("island1_flip");
            yield return Wait(1.0f);
            yield return Shot("ilha1_vivos");
            PressFlip();                         // vira pro Além: o caminho aparece
            yield return Wait(0.9f);
            yield return Shot("ilha1_alem");
            yield return MoveTo(0f, 23f);        // alma 1
            Mark("island1_soul");
            yield return Wait(0.4f);
            PressFlip();                         // Vivos: ponte aparece
            yield return Wait(0.7f);
            yield return MoveTo(9f, 23f);
            yield return MoveTo(16.5f, 21.5f);   // alma 2 (Vivos)
            yield return MoveTo(18f, 24.5f);
            Mark("island1_wall");
            PressFlip();                         // Além: parede some
            yield return Wait(0.8f);
            yield return MoveTo(18f, 31f);       // alma 3 → chave
            Mark("island1_key");
            yield return Wait(1.0f);
            yield return Shot("ilha1_chave");
            yield return MoveTo(18f, 34f);       // pega a chave → próxima ilha
            Mark("island1_portal");
            float wait = 0f;
            while (SceneManager.GetActiveScene().name != SceneFlow.Island2 && wait < 6f) { wait += Time.unscaledDeltaTime; yield return null; }
            if (SceneManager.GetActiveScene().name != SceneFlow.Island2) yield return LoadScene(SceneFlow.Island2);
            yield return Wait(0.3f);

            // ---- Ilha 2: degraus que trocam de lado
            Mark("island2_tips");
            yield return SkipTips(2.6f);
            Mark("island2_steps");
            yield return MoveTo(0f, 8f);
            yield return JumpFlipTo(0f, 9.2f, 0f, 13f);
            yield return Shot("ilha2_degraus");
            yield return JumpFlipTo(0f, 14.2f, 0f, 18f);
            yield return JumpFlipTo(0f, 19.2f, 0f, 23f);
            yield return Wait(0.6f);

            // ---- Ilha 3: virar no ar
            yield return LoadScene(SceneFlow.Island3);
            Mark("island3_tips");
            yield return SkipTips(2.8f);
            Mark("island3_blocked");
            yield return MoveTo(0f, 12.5f);
            yield return Wait(0.4f);
            PressFlip();                         // recusado: tem um bloco do Além aqui
            yield return Wait(1.2f);
            Mark("island3_midair");
            PressJump(0.4f);
            yield return Wait(0.2f);
            PressFlip();                         // no ar: o bloco aparece embaixo
            yield return Wait(1.0f);
            yield return Shot("ilha3_salto");
            yield return MoveTo(0f, 22f);
            yield return Wait(0.5f);

            // ---- Ilha 4: ritmo e espinhos
            yield return LoadScene(SceneFlow.Island4);
            Mark("island4_tips");
            yield return SkipTips(2.6f);
            Mark("island4_steps");
            yield return MoveTo(1.2f, 6.3f);
            yield return JumpFlipTo(0.4f, 7.3f, -1.2f, 9.7f);
            yield return JumpFlipTo(-0.6f, 10.4f, 1.2f, 12.9f);
            yield return Shot("ilha4_ritmo");
            yield return Wait(0.8f);

            // ---- Ilha 5: o Eco
            yield return LoadScene(SceneFlow.Island5);
            Mark("island5_tips");
            yield return SkipTips(2.8f);
            Mark("island5_echo");
            yield return MoveTo(-3f, 0f);
            yield return MoveTo(-3f, 4f);
            yield return MoveTo(2f, 4f);
            PressFlip();
            yield return Wait(1.6f);
            yield return Shot("ilha5_eco");
            yield return MoveTo(2f, 0f);
            yield return Wait(1.5f);

            // ---- Final
            yield return LoadScene(SceneFlow.End);
            Mark("end");
            yield return Wait(5.5f);
            yield return Shot("final");
            yield return Wait(2f);
            Mark("done");

            capture.End();
            File.WriteAllText(Path.Combine(OutFolder, "demo_timeline.json"), "[\n" + string.Join(",\n", timeline) + "\n]\n");
            Debug.Log($"[Verso Demo] Pronto. Arquivos em {OutFolder}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
