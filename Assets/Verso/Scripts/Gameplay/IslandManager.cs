using System;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Controla uma ilha: contagem de cristais, liberação do portal e mensagem inicial.
    /// </summary>
    public class IslandManager : MonoBehaviour
    {
        public static IslandManager Instance { get; private set; }

        [Tooltip("0 = usa a quantidade de cristais na cena.")]
        [SerializeField] private int crystalsRequired = 0;
        [TextArea]
        [SerializeField] private string introMessage = "";
        [SerializeField] private float introDuration = 4f;
        [SerializeField] private string islandTitle = "";

        [Header("Dicas da Alminha (início da ilha)")]
        [TextArea] [SerializeField] private string[] tipsPt = new string[0];
        [TextArea] [SerializeField] private string[] tipsEn = new string[0];

        public int Collected { get; private set; }
        public int Required { get; private set; }
        public bool PortalUnlocked => Collected >= Required;
        public string IslandTitle => islandTitle;

        public event Action<int, int> CrystalsChanged;
        public event Action PortalOpened;

        private void Awake()
        {
            Instance = this;
            int inScene = FindObjectsByType<Collectible>().Length;
            Required = crystalsRequired > 0 ? Mathf.Min(crystalsRequired, inScene) : inScene;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            CrystalsChanged?.Invoke(Collected, Required);

            if (GameUI.Instance != null)
            {
                GameUI.Instance.SetCrystals(Collected, Required);
                // dicas só na primeira vez que a ilha abre na partida (reiniciar com R não repete)
                string scene = gameObject.scene.name;
                bool hasTips = tipsPt != null && tipsPt.Length > 0 && !TipsSeen.Contains(scene);
                if (hasTips)
                {
                    TipsSeen.Add(scene);
                    GameUI.Instance.ShowTips(tipsPt, tipsEn, ShowTitle);
                }
                else ShowTitle();
            }
        }

        private static readonly System.Collections.Generic.HashSet<string> TipsSeen = new System.Collections.Generic.HashSet<string>();

        /// <summary>Esquece as dicas já vistas (jogo novo).</summary>
        public static void ResetTips() => TipsSeen.Clear();

        private void ShowTitle()
        {
            if (GameUI.Instance == null) return;
            if (!string.IsNullOrEmpty(introMessage)) GameUI.Instance.ShowMessage(Loc.Tr(islandTitle).ToUpperInvariant(), Loc.Tr(introMessage), introDuration);
            else if (!string.IsNullOrEmpty(islandTitle)) GameUI.Instance.ShowMessage("", Loc.Tr(islandTitle), 2.5f);
        }

        public void AddCrystal()
        {
            bool wasUnlocked = PortalUnlocked;
            Collected++;
            CrystalsChanged?.Invoke(Collected, Required);
            if (GameUI.Instance != null) GameUI.Instance.SetCrystals(Collected, Required);
            AudioManager.PlaySfx(SfxType.Crystal);

            if (!wasUnlocked && PortalUnlocked)
            {
                PortalOpened?.Invoke();
                AudioManager.PlaySfx(SfxType.PortalOpen);
            }
        }
    }
}
