using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Authored Image whose fill keeps two round caps at every value.</summary>
    public sealed class RoundedProgressImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float width = rect.width * Mathf.Clamp01(fillAmount);
            if (width <= 0 || rect.height <= 0) return;
            float radius = Mathf.Min(rect.height, width) * .5f;
            float middle = rect.center.y;
            // ProgressFill's central pixels are opaque white; tint comes from Image.color.
            Vector2 uv = sprite == null ? Vector2.zero : new Vector2(
                sprite.textureRect.center.x / sprite.texture.width,
                sprite.textureRect.center.y / sprite.texture.height);
            mesh.AddVert(new Vector3(rect.xMin + width * .5f, middle), color, uv);
            const int segments = 12;
            for (int side = 0; side < 2; side++)
            {
                float center = side == 0 ? rect.xMin + width - radius : rect.xMin + radius;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (-90 + side * 180 + i * 180f / segments) * Mathf.Deg2Rad;
                    mesh.AddVert(new Vector3(center + Mathf.Cos(angle) * radius, middle + Mathf.Sin(angle) * radius), color, uv);
                }
            }
            int perimeter = (segments + 1) * 2;
            for (int i = 0; i < perimeter; i++) mesh.AddTriangle(0, i + 1, (i + 1) % perimeter + 1);
        }
    }
}
