using System.Collections.Generic;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// O Eco (GDD 4.5): repete posição e rotação do jogador com atraso
    /// e existe sempre no mundo oposto ao do jogador.
    /// Não tem colisão; placas de pressão checam a posição dele diretamente.
    /// </summary>
    public class EchoGhost : MonoBehaviour
    {
        public static EchoGhost Instance { get; private set; }

        [SerializeField] private float delay = 1.5f;
        [SerializeField] private Renderer[] visuals;
        [SerializeField] private Color dayColor = new Color(1f, 0.75f, 0.35f);
        [SerializeField] private Color nightColor = new Color(0.45f, 0.8f, 1f);

        private struct Sample
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
        }

        private readonly Queue<Sample> samples = new Queue<Sample>();
        public bool Recording { get; private set; } = true;
        public bool Replaying { get; private set; } = true;
        public float Delay => delay;

        /// <summary>O Eco está sempre do outro lado.</summary>
        public WorldType World => WorldLayers.Opposite(WorldManager.CurrentWorld);

        private void Awake()
        {
            Instance = this;
            if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable() { WorldManager.WorldChanged += OnWorldChanged; }
        private void OnDisable() { WorldManager.WorldChanged -= OnWorldChanged; }

        private void Start()
        {
            var p = PlayerController.Instance;
            if (p != null) transform.SetPositionAndRotation(p.transform.position, p.transform.rotation);
            OnWorldChanged(WorldManager.CurrentWorld);
        }

        private void OnWorldChanged(WorldType playerWorld)
        {
            // Eco do lado oposto: se o jogador está no Dia, o Eco tem cor de Noite.
            ColorUtil.SetColor(visuals, playerWorld == WorldType.Day ? nightColor : dayColor, 1.2f);
        }

        private void Update()
        {
            var p = PlayerController.Instance;
            if (Recording && p != null)
            {
                samples.Enqueue(new Sample
                {
                    time = Time.time,
                    position = p.transform.position,
                    rotation = p.transform.rotation
                });
            }

            if (!Replaying) return;

            float playTime = Time.time - delay;
            bool moved = false;
            Sample last = default;
            while (samples.Count > 0 && samples.Peek().time <= playTime)
            {
                last = samples.Dequeue();
                moved = true;
            }
            if (moved) transform.SetPositionAndRotation(last.position, last.rotation);
        }

        /// <summary>Para de gravar e de reproduzir (usado no final).</summary>
        public void StopAll()
        {
            Recording = false;
            Replaying = false;
            samples.Clear();
        }

        /// <summary>Apaga o histórico e reposiciona o Eco (ex.: depois de um respawn).</summary>
        public void ResetTo(Vector3 position, Quaternion rotation)
        {
            samples.Clear();
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
