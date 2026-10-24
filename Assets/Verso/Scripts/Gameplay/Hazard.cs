using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Espinho/perigo. Mata (respawn) se o jogador encostar enquanto o mundo dele estiver ativo.
    /// Usa os bounds do Collider (pode ser trigger) ou do Renderer.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [SerializeField] private float padding = 0.05f;

        private WorldObject worldObject;
        private Collider col;
        private Renderer rend;

        private void Awake()
        {
            worldObject = GetComponent<WorldObject>();
            col = GetComponent<Collider>();
            rend = GetComponent<Renderer>();
        }

        private void Update()
        {
            if (worldObject != null && !worldObject.IsActiveInCurrentWorld) return;
            var player = PlayerController.Instance;
            if (player == null || !player.InputEnabled) return;

            Bounds b = col != null ? col.bounds : (rend != null ? rend.bounds : new Bounds(transform.position, Vector3.one));
            b.Expand(padding);
            if (b.Intersects(player.Controller.bounds))
                player.Respawn();
        }
    }
}
