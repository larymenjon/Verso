using UnityEngine;

namespace Verso
{
    /// <summary>Flutua um elemento de UI para cima e para baixo (logo, almas da tela final).</summary>
    public class UIFloat : MonoBehaviour
    {
        [SerializeField] private float amplitude = 10f;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private float phase = 0f;

        private RectTransform rt;
        private Vector2 basePos;

        public void Setup(float amp, float spd, float ph) { amplitude = amp; speed = spd; phase = ph; }

        private void Awake()
        {
            rt = transform as RectTransform;
            if (rt != null) basePos = rt.anchoredPosition;
        }

        private void Update()
        {
            if (rt == null) return;
            rt.anchoredPosition = basePos + Vector2.up * Mathf.Sin(Time.unscaledTime * speed + phase) * amplitude;
        }
    }
}
