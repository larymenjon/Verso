using System.Collections;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Portal de saída com a chave da ilha.
    /// Trancado até a ilha ter todas as almas; aí a chave aparece girando no portal.
    /// O jogador pega a chave, comemora e vai pra próxima ilha.
    /// Se for o portal final, dispara a sequência do Eco em vez de trocar de cena direto.
    /// </summary>
    public class PortalExit : MonoBehaviour
    {
        [Tooltip("Cena seguinte. Vazio = próxima na ordem do SceneFlow.")]
        [SerializeField] private string nextScene = "";
        [SerializeField] private bool isFinalPortal = false;
        [SerializeField] private float triggerRadius = 1.2f;
        [SerializeField] private Renderer[] visuals;
        [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color unlockedColor = new Color(0.4f, 1f, 0.9f);
        [SerializeField] private float spinSpeed = 60f;

        [Header("Chave")]
        [Tooltip("Modelo da chave (filho do portal). Fica escondido até a ilha liberar.")]
        [SerializeField] private Transform keyVisual;
        [SerializeField] private float keySpin = 120f;
        [SerializeField] private float keyBob = 0.12f;
        [SerializeField] private float celebrateTime = 1.1f;

        private bool used;
        private WorldObject worldObject;
        private Vector3 keyBase;
        private Vector3 keyScale = Vector3.one;

        private void Awake()
        {
            worldObject = GetComponent<WorldObject>();
            if (visuals == null || visuals.Length == 0) visuals = GetComponentsInChildren<Renderer>();
            if (keyVisual != null)
            {
                keyBase = keyVisual.localPosition;
                keyScale = keyVisual.localScale;
                keyVisual.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            RefreshVisual();
            if (IslandManager.Instance != null) IslandManager.Instance.PortalOpened += OnOpened;
        }

        private void OnDestroy()
        {
            if (IslandManager.Instance != null) IslandManager.Instance.PortalOpened -= OnOpened;
        }

        private bool Unlocked => IslandManager.Instance == null || IslandManager.Instance.PortalUnlocked;

        private void OnOpened()
        {
            RefreshVisual();
            if (GameUI.Instance != null) GameUI.Instance.ShowMessage("", "A chave apareceu no portal!", 2.2f);
        }

        private void RefreshVisual()
        {
            ColorUtil.SetColor(visuals, Unlocked ? unlockedColor : lockedColor, Unlocked ? 1.5f : 0f);
            if (keyVisual != null && !used) keyVisual.gameObject.SetActive(Unlocked);
        }

        private void Update()
        {
            if (Unlocked && !used) transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            if (keyVisual != null && keyVisual.gameObject.activeSelf && !used)
            {
                // a chave gira no eixo do mundo, independente do giro do portal
                keyVisual.rotation = Quaternion.Euler(0f, Time.time * keySpin, 0f);
                keyVisual.localPosition = keyBase + Vector3.up * Mathf.Sin(Time.time * 2.2f) * keyBob;
            }

            if (used || !Unlocked) return;
            if (worldObject != null && !worldObject.IsActiveInCurrentWorld) return;

            var player = PlayerController.Instance;
            if (player == null) return;
            Vector3 d = player.transform.position - transform.position;
            d.y *= 0.5f;
            if (d.sqrMagnitude > triggerRadius * triggerRadius) return;

            used = true;
            StartCoroutine(CollectKey(player));
        }

        private IEnumerator CollectKey(PlayerController player)
        {
            player.Freeze();
            AudioManager.PlaySfx(SfxType.KeyPickup);
            var visual = player.GetComponent<CharacterVisual>();
            if (visual != null) visual.Celebrate(true);
            if (GameUI.Instance != null) GameUI.Instance.ShowMessage("", "Você pegou a chave!", 1.6f);

            // a chave voa até a cabeça do jogador e some
            if (keyVisual != null)
            {
                Vector3 from = keyVisual.position;
                float t = 0f;
                const float fly = 0.6f;
                while (t < fly)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / fly);
                    Vector3 to = player.transform.position + Vector3.up * 2.4f;
                    keyVisual.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                    keyVisual.rotation = Quaternion.Euler(0f, Time.time * keySpin * 3f, 0f);
                    keyVisual.localScale = keyScale * Mathf.Lerp(1f, 0.6f, k);
                    yield return null;
                }
                keyVisual.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(celebrateTime);
            AudioManager.PlaySfx(SfxType.PortalEnter);

            if (isFinalPortal)
            {
                if (visual != null) visual.Celebrate(false);
                var ending = FindAnyObjectByType<EndingSequence>();
                if (ending != null) { ending.Play(); yield break; }
            }

            if (GameUI.Instance != null)
                GameUI.Instance.FadeOut(0.6f, Go);
            else
                Go();
        }

        private void Go()
        {
            if (string.IsNullOrEmpty(nextScene)) SceneFlow.LoadNext();
            else SceneFlow.Load(nextScene);
        }
    }
}
