using UnityEngine;

namespace Verso
{
    /// <summary>Troca cor de renderers sem duplicar material (URP usa _BaseColor).</summary>
    public static class ColorUtil
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private static MaterialPropertyBlock block;

        public static void SetColor(Renderer r, Color c, float emission = 0f)
        {
            if (r == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, c);
            block.SetColor(ColorId, c);
            if (emission > 0f) block.SetColor(Emission, c * emission);
            r.SetPropertyBlock(block);
        }

        public static void SetColor(Renderer[] rs, Color c, float emission = 0f)
        {
            if (rs == null) return;
            foreach (var r in rs) SetColor(r, c, emission);
        }
    }
}
