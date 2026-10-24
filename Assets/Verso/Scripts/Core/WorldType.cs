using UnityEngine;

namespace Verso
{
    public enum WorldType
    {
        Day,
        Night,
        Both
    }

    /// <summary>Nomes de layers e helpers usados pelo flip.</summary>
    public static class WorldLayers
    {
        public const string PlayerLayerName = "Player";
        public const string DayLayerName = "WorldDay";
        public const string NightLayerName = "WorldNight";

        public static int Player => LayerMask.NameToLayer(PlayerLayerName);
        public static int Day => LayerMask.NameToLayer(DayLayerName);
        public static int Night => LayerMask.NameToLayer(NightLayerName);

        public static int LayerFor(WorldType world)
        {
            switch (world)
            {
                case WorldType.Day: return Day;
                case WorldType.Night: return Night;
                default: return 0; // Default
            }
        }

        public static WorldType Opposite(WorldType world)
        {
            return world == WorldType.Day ? WorldType.Night : WorldType.Day;
        }

        /// <summary>Máscara de tudo que é sólido no mundo informado (Default + layer do mundo).</summary>
        public static int SolidMaskFor(WorldType world)
        {
            int mask = 1 << 0;
            int layer = LayerFor(world);
            if (layer > 0) mask |= 1 << layer;
            return mask;
        }

        public static bool AreValid()
        {
            return Player >= 0 && Day >= 0 && Night >= 0;
        }
    }
}
