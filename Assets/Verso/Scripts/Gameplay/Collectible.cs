using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Cristal. Só pode ser coletado quando o mundo dele está ativo
    /// (coloque um WorldObject no mesmo GameObject para definir Dia/Noite/Ambos).
    /// Detecção por distância, sem depender de triggers.
    /// </summary>
    public class Collectible : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 0.8f;
        [SerializeField] private float spinSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private WorldObject worldObject;
        private Vector3 basePos;
        private bool collected;

        private void Awake()
        {
            worldObject = GetComponent<WorldObject>();
            basePos = transform.position;
        }

        private void Update()
        {
            if (collected) return;

            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

            if (worldObject != null && !worldObject.IsActiveInCurrentWorld) return;

            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 playerCenter = player.transform.TransformPoint(player.Controller.center);
            float reach = pickupRadius + player.Controller.radius;
            Vector3 closest = ClosestOnSegment(playerCenter, player.Controller.height * 0.5f, transform.position);
            if ((closest - transform.position).sqrMagnitude <= reach * reach)
                Collect();
        }

        private static Vector3 ClosestOnSegment(Vector3 center, float halfHeight, Vector3 point)
        {
            Vector3 a = center + Vector3.up * halfHeight;
            Vector3 b = center - Vector3.up * halfHeight;
            Vector3 ab = a - b;
            float t = Mathf.Clamp01(Vector3.Dot(point - b, ab) / ab.sqrMagnitude);
            return b + ab * t;
        }

        private void Collect()
        {
            collected = true;
            if (IslandManager.Instance != null) IslandManager.Instance.AddCrystal();
            gameObject.SetActive(false);
        }
    }
}
