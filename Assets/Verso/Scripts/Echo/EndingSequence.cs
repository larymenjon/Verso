using System.Collections;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Final (GDD 5): ao entrar no último portal, o Eco se aproxima,
    /// os dois se fundem e a cena final é carregada.
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [SerializeField] private float approachDuration = 2.2f;
        [SerializeField] private float mergeDuration = 1.2f;
        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private string endScene = SceneFlow.End;

        private bool playing;

        public void Play()
        {
            if (playing) return;
            playing = true;
            StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            var player = PlayerController.Instance;
            var echo = EchoGhost.Instance;
            if (player != null) player.Freeze();
            if (GameUI.Instance != null) GameUI.Instance.SetHudVisible(false);
            AudioManager.PlaySfx(SfxType.Ending);

            if (player != null && echo != null)
            {
                echo.StopAll();
                Vector3 start = echo.transform.position;
                Vector3 end = player.transform.position + player.transform.forward * 1.2f;
                float t = 0f;
                while (t < approachDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / approachDuration);
                    echo.transform.position = Vector3.Lerp(start, end, k);
                    Vector3 look = player.transform.position - echo.transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.001f) echo.transform.rotation = Quaternion.LookRotation(look);
                    yield return null;
                }

                if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.05f, mergeDuration);

                Vector3 meet = Vector3.Lerp(player.transform.position, echo.transform.position, 0.5f);
                Vector3 pStart = player.transform.position;
                Vector3 eStart = echo.transform.position;
                Vector3 eScale = echo.transform.localScale;
                t = 0f;
                while (t < mergeDuration)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / mergeDuration);
                    player.Teleport(Vector3.Lerp(pStart, meet, k), player.transform.rotation);
                    echo.transform.position = Vector3.Lerp(eStart, meet, k);
                    echo.transform.localScale = Vector3.Lerp(eScale, eScale * 0.2f, k);
                    yield return null;
                }
                echo.gameObject.SetActive(false);
            }

            bool done = false;
            if (GameUI.Instance != null) GameUI.Instance.FadeOut(fadeDuration, () => done = true, Color.white);
            else done = true;
            while (!done) yield return null;

            SceneFlow.Load(endScene);
        }
    }
}
