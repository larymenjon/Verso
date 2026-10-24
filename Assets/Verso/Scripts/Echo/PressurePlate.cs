using System;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Placa de pressão. Pertence a um mundo (WorldObject no mesmo objeto).
    /// É pressionada por quem existe nesse mundo: o jogador quando o mundo está ativo,
    /// ou o Eco quando o mundo é o oposto.
    /// Use "ghost" no WorldObject para o jogador enxergar a placa do outro lado.
    /// </summary>
    public class PressurePlate : MonoBehaviour
    {
        [SerializeField] private float margin = 0.15f;
        [SerializeField] private float heightTolerance = 0.9f;
        [SerializeField] private Color idleColor = new Color(0.9f, 0.4f, 0.3f);
        [SerializeField] private Color pressedColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] private float pressDepth = 0.06f;

        public bool IsPressed { get; private set; }
        public event Action<bool> Changed;

        private WorldObject worldObject;
        private Renderer rend;
        private Vector3 restLocalPos;

        public WorldType World => worldObject != null ? worldObject.World : WorldType.Both;

        private void Awake()
        {
            worldObject = GetComponent<WorldObject>();
            rend = GetComponent<Renderer>();
            restLocalPos = transform.localPosition;
        }

        private void Start()
        {
            ColorUtil.SetColor(rend, idleColor, 0.3f);
        }

        private void Update()
        {
            bool pressed = false;

            var player = PlayerController.Instance;
            if (player != null && player.IsGrounded && ExistsIn(WorldManager.CurrentWorld) && IsOver(player.transform.position))
                pressed = true;

            var echo = EchoGhost.Instance;
            if (!pressed && echo != null && echo.Replaying && ExistsIn(echo.World) && IsOver(echo.transform.position))
                pressed = true;

            if (pressed != IsPressed)
            {
                IsPressed = pressed;
                transform.localPosition = restLocalPos - Vector3.up * (pressed ? pressDepth : 0f);
                ColorUtil.SetColor(rend, pressed ? pressedColor : idleColor, pressed ? 1f : 0.3f);
                AudioManager.PlaySfx(SfxType.Plate);
                Changed?.Invoke(pressed);
            }
        }

        private bool ExistsIn(WorldType bodyWorld)
        {
            return World == WorldType.Both || World == bodyWorld;
        }

        private bool IsOver(Vector3 feet)
        {
            Vector3 local = transform.InverseTransformPoint(feet);
            Vector3 s = transform.lossyScale;
            float mx = s.x != 0f ? margin / Mathf.Abs(s.x) : 0f;
            float mz = s.z != 0f ? margin / Mathf.Abs(s.z) : 0f;
            if (Mathf.Abs(local.x) > 0.5f + mx || Mathf.Abs(local.z) > 0.5f + mz) return false;

            float top = transform.position.y + Mathf.Abs(s.y) * 0.5f;
            float dy = feet.y - top;
            return dy > -0.4f && dy < heightTolerance;
        }
    }
}
