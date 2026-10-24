using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Porta ligada a placas de pressão. Desce para dentro do chão quando aberta.
    /// latch = fica aberta para sempre depois da primeira ativação.
    /// Não fecha enquanto o jogador estiver no vão.
    /// </summary>
    public class Door : MonoBehaviour
    {
        [SerializeField] private PressurePlate[] plates;
        [SerializeField] private bool requireAll = false;
        [SerializeField] private bool latch = false;
        [Tooltip("Tempo que a porta segura aberta depois que a placa é solta.")]
        [SerializeField] private float closeDelay = 0.6f;
        [SerializeField] private float speed = 6f;
        [Tooltip("0 = altura do próprio objeto.")]
        [SerializeField] private float openDepth = 0f;

        public bool IsOpen { get; private set; }

        private Vector3 closedPos;
        private Vector3 openPos;
        private Bounds closedBounds;
        private float lastActiveTime = -100f;

        public void Setup(PressurePlate[] newPlates, bool all, bool latchOpen, float delay)
        {
            plates = newPlates;
            requireAll = all;
            latch = latchOpen;
            closeDelay = delay;
        }

        private void Start()
        {
            closedPos = transform.position;
            float depth = openDepth > 0f ? openDepth : transform.lossyScale.y + 0.05f;
            openPos = closedPos - Vector3.up * depth;
            var col = GetComponent<Collider>();
            closedBounds = col != null ? col.bounds : new Bounds(closedPos, transform.lossyScale);
        }

        private bool PlatesActive()
        {
            if (plates == null || plates.Length == 0) return false;
            bool any = false, all = true;
            foreach (var p in plates)
            {
                if (p == null) continue;
                if (p.IsPressed) any = true; else all = false;
            }
            return requireAll ? all : any;
        }

        private void Update()
        {
            bool active = PlatesActive();
            if (active) lastActiveTime = Time.time;

            bool wantOpen = IsOpen;
            if (active) wantOpen = true;
            else if (!latch && Time.time - lastActiveTime > closeDelay) wantOpen = false;

            if (IsOpen && !wantOpen && PlayerInDoorway()) wantOpen = true;

            if (wantOpen != IsOpen)
            {
                IsOpen = wantOpen;
                AudioManager.PlaySfx(IsOpen ? SfxType.DoorOpen : SfxType.DoorClose);
            }

            Vector3 target = IsOpen ? openPos : closedPos;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }

        private bool PlayerInDoorway()
        {
            var p = PlayerController.Instance;
            if (p == null) return false;
            Bounds b = closedBounds;
            b.Expand(0.2f);
            return b.Intersects(p.Controller.bounds);
        }
    }
}
