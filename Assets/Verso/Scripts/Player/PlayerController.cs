using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Personagem em terceira pessoa com CharacterController.
    /// Pivô nos pés. Movimento relativo à câmera, pulo com coyote time e buffer,
    /// flip, reinício de ilha e respawn ao cair ou tocar perigo.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movimento")]
        [SerializeField] private float moveSpeed = 5.5f;
        [SerializeField] private float acceleration = 40f;
        [SerializeField] private float airControl = 0.6f;
        [SerializeField] private float turnSpeed = 720f;

        [Header("Pulo")]
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float fallGravityMultiplier = 1.6f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBuffer = 0.12f;

        [Header("Respawn")]
        [Tooltip("Abaixo desse Y o jogador volta ao último ponto seguro.")]
        [SerializeField] private float killY = -15f;

        public CharacterController Controller { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public bool IsGrounded => Controller.isGrounded;
        public Vector3 Velocity => velocity;

        public event System.Action Jumped;
        public event System.Action Respawned;

        private Vector3 velocity;
        private float lastGroundedTime = -10f;
        private float lastJumpPressedTime = -10f;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Transform cam;

        private void Awake()
        {
            Instance = this;
            Controller = GetComponent<CharacterController>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            int playerLayer = WorldLayers.Player;
            if (playerLayer >= 0) gameObject.layer = playerLayer;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (Camera.main != null) cam = Camera.main.transform;
        }

        public void SetSpawn(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        private void Update()
        {
            if (GameUI.IsPaused || !Controller.enabled) return;

            bool canAct = InputEnabled;

            if (canAct && GameInput.RestartPressed)
            {
                SceneFlow.ReloadCurrent();
                return;
            }

            if (canAct && GameInput.FlipPressed && WorldManager.Instance != null)
                WorldManager.Instance.TryFlip();

            Vector2 input = canAct ? GameInput.Move : Vector2.zero;
            Vector3 desired = CameraRelative(input) * moveSpeed;

            bool grounded = Controller.isGrounded;
            if (grounded) lastGroundedTime = Time.time;

            float accel = acceleration * (grounded ? 1f : airControl);
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(horizontal, desired, accel * Time.deltaTime);
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;

            if (desired.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(desired.x, 0f, desired.z));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }

            // Pulo
            if (canAct && GameInput.JumpPressed) lastJumpPressedTime = Time.time;
            bool coyote = Time.time - lastGroundedTime <= coyoteTime;
            bool buffered = Time.time - lastJumpPressedTime <= jumpBuffer;
            if (coyote && buffered)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                lastJumpPressedTime = -10f;
                lastGroundedTime = -10f;
                Jumped?.Invoke();
                AudioManager.PlaySfx(SfxType.Jump);
            }

            // Gravidade (cai mais rápido, e pulo curto se soltar o botão)
            if (grounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }
            else
            {
                float g = gravity;
                if (velocity.y < 0f || !(canAct && GameInput.JumpHeld)) g *= fallGravityMultiplier;
                velocity.y += g * Time.deltaTime;
            }

            CollisionFlags flags = Controller.Move(velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f) velocity.y = 0f;

            if (transform.position.y < killY) Respawn();
        }

        private Vector3 CameraRelative(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return Vector3.zero;
            Vector3 forward = cam != null ? cam.forward : Vector3.forward;
            Vector3 right = cam != null ? cam.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();
            return forward * input.y + right * input.x;
        }

        /// <summary>
        /// Perdeu a rodada (caiu ou encostou num perigo): gasta uma vida e volta ao spawn
        /// mantendo as almas coletadas. Sem vidas, abre a tela de fim de jogo.
        /// </summary>
        public void Respawn()
        {
            if (Lives.IsGameOver) return;
            bool gameOver = Lives.LoseLife();
            if (gameOver)
            {
                Freeze();
                Controller.enabled = false; // para de cair enquanto a tela de fim de jogo aparece
                AudioManager.PlaySfx(SfxType.Respawn);
                if (GameUI.Instance != null) GameUI.Instance.ShowGameOver();
                return;
            }
            if (GameUI.Instance != null) GameUI.Instance.OnLifeLost(Lives.Current);
            Teleport(spawnPosition, spawnRotation);
            velocity = Vector3.zero;
            AudioManager.PlaySfx(SfxType.Respawn);
            Respawned?.Invoke();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            Controller.enabled = true;
            velocity = Vector3.zero;
        }

        public void Freeze()
        {
            InputEnabled = false;
            velocity = new Vector3(0f, velocity.y, 0f);
        }
    }
}
