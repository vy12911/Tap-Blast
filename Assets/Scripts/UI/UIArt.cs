using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Small shared procedural UI atlas pieces; generated once, no external art dependency.</summary>
    public sealed class UIArt
    {
        public readonly Sprite Panel, Disc, Ring, Glow, Star;
        public readonly Font Font;
        public static readonly Color Ink = new Color32(10, 22, 35, 255);
        public static readonly Color Card = new Color32(22, 44, 60, 255);
        public static readonly Color Cyan = new Color32(64, 232, 226, 255);
        public static readonly Color Muted = new Color32(145, 174, 189, 255);
        public static readonly Color Gold = new Color32(255, 206, 88, 255);
        public UIArt()
        {
            Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Panel = Make(64, 0); Disc = Make(128, 1); Ring = Make(128, 2);
            Glow = Make(128, 3); Star = Make(64, 4);
        }
        private static Sprite Make(int size, int shape)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "TapBlast UI " + shape, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1);
                float r = p.magnitude;
                float a;
                if (shape == 0)
                {
                    Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - Vector2.one * .62f;
                    float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - .35f;
                    a = Mathf.Clamp01(-d * size / 2);
                }
                else if (shape == 1) a = Mathf.Clamp01((.94f - r) * size / 2);
                else if (shape == 2) a = Mathf.Clamp01((.94f - r) * size / 2) * Mathf.Clamp01((r - .81f) * size / 2);
                else if (shape == 3) a = Mathf.Pow(Mathf.Clamp01(1 - r), 2) * .8f;
                else
                {
                    // Alternating-radius 10-vertex polygon, with one tip facing up.
                    bool inside = false;
                    for (int i = 0, j = 9; i < 10; j = i++)
                    {
                        float ai = Mathf.PI * .5f + i * Mathf.PI / 5, aj = Mathf.PI * .5f + j * Mathf.PI / 5;
                        Vector2 vi = new Vector2(Mathf.Cos(ai), Mathf.Sin(ai)) * (i % 2 == 0 ? .92f : .43f);
                        Vector2 vj = new Vector2(Mathf.Cos(aj), Mathf.Sin(aj)) * (j % 2 == 0 ? .92f : .43f);
                        if ((vi.y > p.y) != (vj.y > p.y) && p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x) inside = !inside;
                    }
                    a = inside ? 1 : 0;
                }
                pixels[y * size + x] = new Color(1, 1, 1, a);
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, shape == 0 ? Vector4.one * 24 : Vector4.zero);
        }
        public void Dispose()
        {
            Dispose(Panel); Dispose(Disc); Dispose(Ring); Dispose(Glow); Dispose(Star);
        }
        private static void Dispose(Sprite sprite) { Object.Destroy(sprite.texture); Object.Destroy(sprite); }
        public RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; return rect;
        }
        public RectTransform Fixed(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, Vector2.one * .5f, Vector2.one * .5f, Vector2.zero, Vector2.zero);
            rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
        }
        public Image Image(RectTransform rect, Color color, Sprite sprite = null)
        {
            var image = rect.gameObject.AddComponent<Image>(); image.color = color;
            image.sprite = sprite; image.raycastTarget = false;
            if (sprite == Panel) image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        public Text Label(RectTransform rect, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter, bool bold = false)
        {
            var text = rect.gameObject.AddComponent<Text>(); text.font = Font; text.text = content;
            text.fontSize = size; text.color = color; text.alignment = alignment;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate; return text;
        }
        public Button Button(RectTransform rect, string title, Color background, Color foreground)
        {
            var image = Image(rect, background, Panel); image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.highlightedColor = new Color(.9f, 1, 1); colors.pressedColor = new Color(.7f, .8f, .8f); button.colors = colors;
            Label(Rect("Label", rect, Vector2.zero, Vector2.one, new Vector2(12, 0), new Vector2(-12, 0)), title, 22, foreground, TextAnchor.MiddleCenter, true);
            return button;
        }
    }
}
