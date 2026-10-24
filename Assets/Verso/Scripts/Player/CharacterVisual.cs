using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Liga o modelo 3D (Dia ou Noite) ao jogador ou ao Eco e alimenta o Animator.
    /// Jogador: mostra o personagem do mundo atual. Eco: mostra o do mundo oposto.
    /// Parâmetros do Animator: Speed (0..1), Grounded, VSpeed, Jump, Flip, Celebrate.
    /// </summary>
    public class CharacterVisual : MonoBehaviour
    {
        public enum Mode { Player, Echo }

        [SerializeField] private Mode mode = Mode.Player;
        [SerializeField] private GameObject dayModel;
        [SerializeField] private GameObject nightModel;
        [Tooltip("Velocidade horizontal que conta como corrida cheia.")]
        [SerializeField] private float runSpeed = 5.5f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int VSpeedId = Animator.StringToHash("VSpeed");
        private static readonly int JumpId = Animator.StringToHash("Jump");
        private static readonly int FlipId = Animator.StringToHash("Flip");
        private static readonly int CelebrateId = Animator.StringToHash("Celebrate");

        private PlayerController player;
        private Animator dayAnim, nightAnim, active;
        private Vector3 lastPos;
        private float smoothSpeed, echoVSpeed;
        private bool celebrating;

        public Animator Active => active;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            if (dayModel != null) dayAnim = dayModel.GetComponentInChildren<Animator>(true);
            if (nightModel != null) nightAnim = nightModel.GetComponentInChildren<Animator>(true);
            lastPos = transform.position;
        }

        private void OnEnable()
        {
            WorldManager.WorldChanged += OnWorldChanged;
            if (player != null) player.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            WorldManager.WorldChanged -= OnWorldChanged;
            if (player != null) player.Jumped -= OnJumped;
        }

        private void Start()
        {
            Show(WorldManager.CurrentWorld, false);
        }

        private void OnWorldChanged(WorldType world) => Show(world, true);

        private void Show(WorldType world, bool flip)
        {
            WorldType shown = mode == Mode.Player ? world : WorldLayers.Opposite(world);
            bool day = shown != WorldType.Night;
            if (dayModel != null) dayModel.SetActive(day);
            if (nightModel != null) nightModel.SetActive(!day);
            active = day ? dayAnim : nightAnim;
            if (active == null) return;
            active.SetBool(CelebrateId, celebrating);
            if (flip) active.SetTrigger(FlipId);
        }

        private void OnJumped()
        {
            if (active != null) active.SetTrigger(JumpId);
        }

        /// <summary>Liga/desliga a animação de comemoração (cristal, final).</summary>
        public void Celebrate(bool on)
        {
            celebrating = on;
            if (active != null) active.SetBool(CelebrateId, on);
        }

        private void Update()
        {
            if (active == null) return;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            float horizontal;
            float vSpeed;
            bool grounded;

            if (player != null && mode == Mode.Player)
            {
                Vector3 v = player.Velocity;
                horizontal = new Vector2(v.x, v.z).magnitude;
                vSpeed = v.y;
                grounded = player.IsGrounded;
            }
            else
            {
                // Eco: deduz o movimento pela posição reproduzida
                Vector3 delta = (transform.position - lastPos) / dt;
                horizontal = new Vector2(delta.x, delta.z).magnitude;
                echoVSpeed = Mathf.Lerp(echoVSpeed, delta.y, 12f * dt);
                vSpeed = echoVSpeed;
                grounded = Mathf.Abs(vSpeed) < 1.5f;
                if (horizontal > runSpeed * 3f) horizontal = 0f; // teleporte/respawn
            }
            lastPos = transform.position;

            smoothSpeed = Mathf.Lerp(smoothSpeed, Mathf.Clamp01(horizontal / runSpeed), 12f * dt);
            active.SetFloat(SpeedId, smoothSpeed);
            active.SetFloat(VSpeedId, vSpeed);
            active.SetBool(GroundedId, grounded);
        }
    }
}
