using System;
using System.Collections;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Estado Dia/Noite da ilha e execução do flip (GDD 4.3).
    /// - Troca colisão na hora (matriz de layers) e interpola cor/luz em ~0,6 s.
    /// - Bloqueia o flip se o jogador ficaria dentro de um sólido do outro mundo.
    /// - Cooldown de 0,5 s.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }
        public static WorldType CurrentWorld { get; private set; } = WorldType.Day;

        /// <summary>Disparado no momento do flip (já com o novo mundo).</summary>
        public static event Action<WorldType> WorldChanged;
        /// <summary>Disparado quando o flip é recusado.</summary>
        public static event Action FlipBlocked;

        [Header("Regras")]
        [SerializeField] private WorldType startWorld = WorldType.Day;
        [SerializeField] private float transitionDuration = 0.6f;
        [SerializeField] private float cooldown = 0.5f;
        [Tooltip("Folga do teste de overlap para o jogador não ser bloqueado só por encostar.")]
        [SerializeField] private float overlapSkin = 0.06f;

        [Header("Ambiente Dia")]
        [SerializeField] private Color daySky = new Color(1f, 0.85f, 0.65f);
        [SerializeField] private Color dayAmbient = new Color(0.75f, 0.7f, 0.6f);
        [SerializeField] private Color dayLight = new Color(1f, 0.92f, 0.8f);
        [SerializeField] private float dayLightIntensity = 1.2f;
        [SerializeField] private Vector3 dayLightEuler = new Vector3(50f, -30f, 0f);
        [SerializeField] private Color dayFog = new Color(1f, 0.88f, 0.72f);

        [Header("Ambiente Noite")]
        [SerializeField] private Color nightSky = new Color(0.08f, 0.06f, 0.2f);
        [SerializeField] private Color nightAmbient = new Color(0.25f, 0.22f, 0.45f);
        [SerializeField] private Color nightLight = new Color(0.55f, 0.6f, 1f);
        [SerializeField] private float nightLightIntensity = 0.5f;
        [SerializeField] private Vector3 nightLightEuler = new Vector3(30f, 150f, 0f);
        [SerializeField] private Color nightFog = new Color(0.12f, 0.1f, 0.28f);

        [Header("Céu (skybox)")]
        [Tooltip("Sem material, usa a cor sólida de Ambiente Dia/Noite.")]
        [SerializeField] private Material daySkybox;
        [SerializeField] private Material nightSkybox;
        [Tooltip("Quanto o céu escurece no meio da virada, pra esconder a troca.")]
        [SerializeField, Range(0f, 1f)] private float skyDip = 0.75f;

        [Header("Referências (opcionais)")]
        [SerializeField] private Light sun;
        [SerializeField] private Material ghostMaterial;

        public Material GhostMaterial => ghostMaterial;
        public bool IsTransitioning { get; private set; }
        public float TransitionProgress { get; private set; } = 1f;

        private float lastFlipTime = -10f;
        private Material daySkyRt, nightSkyRt;
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private Coroutine transitionRoutine;

        public static bool IsWorldActive(WorldType world)
        {
            return world == WorldType.Both || world == CurrentWorld;
        }

        private void Awake()
        {
            Instance = this;
            CurrentWorld = startWorld;
            if (sun == null) sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { sun = l; break; }
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 25f;
            RenderSettings.fogEndDistance = 90f;
            ApplyPhysics();
            // cópias em tempo de jogo pra mexer na exposição sem alterar o material salvo
            if (daySkybox != null) daySkyRt = new Material(daySkybox);
            if (nightSkybox != null) nightSkyRt = new Material(nightSkybox);
        }

        private void Start()
        {
            ApplyAllObjects();
            ApplyEnvironment(CurrentWorld == WorldType.Day ? 0f : 1f);
            WorldChanged?.Invoke(CurrentWorld);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Tenta virar o mundo. Retorna true se virou.</summary>
        public bool TryFlip()
        {
            if (Time.time - lastFlipTime < cooldown) return false;

            WorldType target = WorldLayers.Opposite(CurrentWorld);
            if (!IsSafeToFlip(target))
            {
                lastFlipTime = Time.time - cooldown * 0.5f; // recusa também tem um mini cooldown
                FlipBlocked?.Invoke();
                return false;
            }

            lastFlipTime = Time.time;
            SetWorld(target, true);
            return true;
        }

        /// <summary>Vira só se der agora, sem tremor nem som de recusa (usado pelo buffer do F/clique).</summary>
        public bool TryFlipQuiet()
        {
            if (Time.time - lastFlipTime < cooldown) return false;
            WorldType target = WorldLayers.Opposite(CurrentWorld);
            if (!IsSafeToFlip(target)) return false;
            lastFlipTime = Time.time;
            SetWorld(target, true);
            return true;
        }

        /// <summary>Força um mundo (usado em reinícios/cutscenes).</summary>
        public void SetWorld(WorldType world, bool animate)
        {
            if (world == WorldType.Both) return;
            CurrentWorld = world;
            ApplyPhysics();
            ApplyAllObjects();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            if (animate) transitionRoutine = StartCoroutine(TransitionRoutine(world));
            else ApplyEnvironment(world == WorldType.Day ? 0f : 1f);

            WorldChanged?.Invoke(world);
        }

        /// <summary>Checagem de segurança: o jogador ficaria dentro de algo sólido no mundo alvo?</summary>
        public bool IsSafeToFlip(WorldType target)
        {
            var player = PlayerController.Instance;
            if (player == null) return true;
            CharacterController cc = player.Controller;

            int targetLayer = WorldLayers.LayerFor(target);
            if (targetLayer < 0) return true;
            int mask = 1 << targetLayer;

            Vector3 center = player.transform.TransformPoint(cc.center);
            float radius = Mathf.Max(0.05f, cc.radius - overlapSkin);
            float half = Mathf.Max(0f, cc.height * 0.5f - cc.radius);
            Vector3 up = player.transform.up;
            // Sobe um pouco o ponto inferior para não colidir com o chão em que está pisando.
            Vector3 p1 = center + up * half;
            Vector3 p2 = center - up * (half - overlapSkin);

            return !Physics.CheckCapsule(p1, p2, radius, mask, QueryTriggerInteraction.Ignore);
        }

        private void ApplyPhysics()
        {
            if (!WorldLayers.AreValid())
            {
                Debug.LogError("[Verso] Layers Player/WorldDay/WorldNight não encontrados. Rode Verso > Gerar Projeto.");
                return;
            }
            int player = WorldLayers.Player;
            bool day = CurrentWorld == WorldType.Day;
            Physics.IgnoreLayerCollision(player, WorldLayers.Day, !day);
            Physics.IgnoreLayerCollision(player, WorldLayers.Night, day);
        }

        private void ApplyAllObjects()
        {
            for (int i = WorldObject.All.Count - 1; i >= 0; i--)
            {
                var wo = WorldObject.All[i];
                if (wo != null) wo.Apply(CurrentWorld);
            }
        }

        private IEnumerator TransitionRoutine(WorldType target)
        {
            IsTransitioning = true;
            float from = Mathf.Clamp01(currentBlend);
            float to = target == WorldType.Day ? 0f : 1f;
            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                TransitionProgress = Mathf.Clamp01(t / transitionDuration);
                float k = Mathf.SmoothStep(0f, 1f, TransitionProgress);
                ApplyEnvironment(Mathf.Lerp(from, to, k));
                yield return null;
            }
            ApplyEnvironment(to);
            TransitionProgress = 1f;
            IsTransitioning = false;
            transitionRoutine = null;
        }

        private float currentBlend;

        /// <summary>0 = Dia, 1 = Noite.</summary>
        private void ApplyEnvironment(float blend)
        {
            currentBlend = blend;
            RenderSettings.ambientLight = Color.Lerp(dayAmbient, nightAmbient, blend);
            RenderSettings.fogColor = Color.Lerp(dayFog, nightFog, blend);

            Camera cam = Camera.main;
            bool useSky = daySkyRt != null && nightSkyRt != null;
            if (useSky)
            {
                // troca de céu no meio da virada, com uma escurecida pra não "pular"
                Material sky = blend < 0.5f ? daySkyRt : nightSkyRt;
                sky.SetFloat(ExposureId, 1f - skyDip * Mathf.Sin(Mathf.Clamp01(blend) * Mathf.PI));
                if (RenderSettings.skybox != sky) RenderSettings.skybox = sky;
            }
            if (cam != null)
            {
                cam.clearFlags = useSky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.Lerp(daySky, nightSky, blend);
            }

            if (sun != null)
            {
                sun.color = Color.Lerp(dayLight, nightLight, blend);
                sun.intensity = Mathf.Lerp(dayLightIntensity, nightLightIntensity, blend);
                sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(dayLightEuler), Quaternion.Euler(nightLightEuler), blend);
            }
        }
    }
}
