using System.Collections.Generic;
using UnityEngine;

namespace Verso
{
    /// <summary>
    /// Marca um objeto como pertencente ao Dia, à Noite ou aos dois.
    /// O objeto fica sempre ativo; o que muda é o layer (colisão via matriz de física)
    /// e a renderização. Assim o WorldManager consegue testar overlap no mundo de destino.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldObject : MonoBehaviour
    {
        public static readonly List<WorldObject> All = new List<WorldObject>();

        [SerializeField] private WorldType world = WorldType.Day;

        [Tooltip("Quando o mundo não está ativo, mostra uma versão fantasma em vez de esconder.")]
        [SerializeField] private bool ghostWhenInactive = false;

        [Tooltip("Material usado no modo fantasma. Se vazio, usa o padrão do WorldManager.")]
        [SerializeField] private Material ghostMaterialOverride;

        private Renderer[] renderers;
        private Material[][] originalMaterials;
        private bool? lastActive;

        public WorldType World => world;
        public bool IsActiveInCurrentWorld => WorldManager.IsWorldActive(world);

        public void Configure(WorldType newWorld, bool ghost)
        {
            world = newWorld;
            ghostWhenInactive = ghost;
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterials;

            int layer = WorldLayers.LayerFor(world);
            if (world != WorldType.Both && layer < 0)
                Debug.LogError("[Verso] Layers WorldDay/WorldNight não existem. Rode o menu Verso > Gerar Projeto.");
            else if (world != WorldType.Both)
                SetLayerRecursive(transform, layer);
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            lastActive = null;
            Apply(WorldManager.CurrentWorld);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        public void Apply(WorldType current)
        {
            bool active = world == WorldType.Both || world == current;
            if (lastActive.HasValue && lastActive.Value == active) return;
            lastActive = active;

            Material ghost = ghostMaterialOverride != null ? ghostMaterialOverride :
                (WorldManager.Instance != null ? WorldManager.Instance.GhostMaterial : null);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null) continue;
                if (active)
                {
                    r.enabled = true;
                    r.sharedMaterials = originalMaterials[i];
                }
                else if (ghostWhenInactive && ghost != null)
                {
                    r.enabled = true;
                    var mats = new Material[originalMaterials[i].Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = ghost;
                    r.sharedMaterials = mats;
                }
                else
                {
                    r.enabled = false;
                }
            }
        }

        private static void SetLayerRecursive(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
        }
    }
}
