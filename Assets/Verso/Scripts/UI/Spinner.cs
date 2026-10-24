using UnityEngine;

namespace Verso
{
    /// <summary>Gira o objeto continuamente (decoração do menu).</summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 20f, 0f);

        public void SetSpeed(Vector3 dps) { degreesPerSecond = dps; }

        private void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
