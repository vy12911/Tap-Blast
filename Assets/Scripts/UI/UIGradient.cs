using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Two-colour gradient across a Graphic's rect, multiplied with its vertex colour.
    /// Applied only when the mesh rebuilds; no per-frame work.</summary>
    [DisallowMultipleComponent]
    public sealed class UIGradient : BaseMeshEffect
    {
        [SerializeField] private Color from = Color.white, to = Color.white;
        [SerializeField] private bool vertical;

        public void Set(Color start, Color end, bool isVertical)
        {
            from = start; to = end; vertical = isVertical;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            Rect rect = graphic.rectTransform.rect;
            var vertex = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = vertical ? Mathf.InverseLerp(rect.yMax, rect.yMin, vertex.position.y) : Mathf.InverseLerp(rect.xMin, rect.xMax, vertex.position.x);
                vertex.color = (Color)vertex.color * Color.Lerp(from, to, t);
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
