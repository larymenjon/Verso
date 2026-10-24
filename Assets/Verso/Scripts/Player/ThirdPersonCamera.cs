using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Câmera orbital simples (substitui Cinemachine no blockout).
    /// Mouse/analógico direito giram, colisão só com o mundo ativo, e tremor para feedback.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float distance = 7f;
        [SerializeField] private float minPitch = -25f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float startPitch = 20f;
        [SerializeField] private float followSharpness = 15f;
        [SerializeField] private float collisionRadius = 0.25f;

        private float yaw;
        private float pitch;
        private Vector3 smoothedPivot;
        private float shakeTime;
        private float shakeDuration;
        private float shakeAmplitude;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            WorldManager.WorldChanged += OnWorldChanged;
            WorldManager.FlipBlocked += OnFlipBlocked;
        }

        private void OnDisable()
        {
            WorldManager.WorldChanged -= OnWorldChanged;
            WorldManager.FlipBlocked -= OnFlipBlocked;
        }

        private void Start()
        {
            if (target == null && PlayerController.Instance != null) target = PlayerController.Instance.transform;
            if (target != null)
            {
                yaw = target.eulerAngles.y;
                smoothedPivot = target.position + targetOffset;
            }
            pitch = startPitch;
            GameInput.LockCursor(true);
        }

        private void OnWorldChanged(WorldType w)
        {
            if (Time.timeSinceLevelLoad < 0.1f) return; // ignora o evento inicial
            Shake(0.12f, 0.25f);
        }

        private void OnFlipBlocked()
        {
            Shake(0.08f, 0.2f);
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmplitude = amplitude;
            shakeDuration = duration;
            shakeTime = duration;
        }

        public void SetTarget(Transform t) { target = t; }

        private void LateUpdate()
        {
            if (target == null) return;

            if (!GameUI.IsPaused)
            {
                Vector2 look = GameInput.LookDelta;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
            }

            Vector3 pivot = target.position + targetOffset;
            smoothedPivot = Vector3.Lerp(smoothedPivot, pivot, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;
            float dist = distance;

            int mask = WorldLayers.SolidMaskFor(WorldManager.CurrentWorld);
            if (Physics.SphereCast(smoothedPivot, collisionRadius, dir, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.5f, hit.distance);

            Vector3 pos = smoothedPivot + dir * dist;

            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                float k = shakeDuration > 0f ? shakeTime / shakeDuration : 0f;
                pos += Random.insideUnitSphere * shakeAmplitude * k;
            }

            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
