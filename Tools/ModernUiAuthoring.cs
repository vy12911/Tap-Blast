// Editor-only authoring utility. Run in the disposable validation project; copy the reviewed
// prefab, generated art (Assets/Art/UI/Modern) and fonts (Assets/Fonts/Poppins) back afterwards.
// Re-skins GameCanvas in a soft modern style: lavender gradient background, white rounded cards
// with soft shadows, navy Poppins headings, blue-violet gradient pills and pastel tiles.
// Every scripted binding is kept; only unreferenced decoration is removed. Layout fills the
// screen: headers pin to the top, action bars to the bottom, arena and lists stretch between.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using TapBlast;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ModernUiAuthoring
{
    const string CanvasPath = "Assets/Prefabs/UI/GameCanvas.prefab";
    const string ArtDir = "Assets/Art/UI/Modern/";
    const string FontDir = "Assets/Fonts/Poppins/";
    const float Top = 32, Bottom = 40, Width = 1000;
    static readonly StringBuilder log = new StringBuilder();
    static GameObject root;
    static Transform frame;

    // ---- Palette ------------------------------------------------------------------------------
    static Color C(byte r, byte g, byte b, float a = 1) => new Color(r / 255f, g / 255f, b / 255f, a);
    static readonly Color Ink = C(30, 34, 64), Sub = C(107, 113, 144), Line = C(222, 226, 245), Track = C(236, 233, 249);
    static readonly Color Primary = C(79, 124, 255), Accent = C(155, 107, 255), Dim = C(30, 34, 64, .38f);
    static readonly Color[] TileFill = { C(221, 235, 255), C(255, 235, 217), C(236, 228, 255), C(221, 246, 227), C(255, 227, 234), C(255, 244, 210) };
    static readonly Color[] TileEdge = { C(169, 201, 245), C(243, 197, 154), C(198, 180, 245), C(165, 221, 178), C(242, 175, 192), C(240, 210, 120) };
    static readonly Color[][] IconGrad = {
        new[] { C(61, 165, 245), C(47, 123, 234) }, new[] { C(255, 181, 71), C(255, 138, 31) }, new[] { C(165, 123, 255), C(122, 85, 240) },
        new[] { C(76, 208, 125), C(34, 169, 90) }, new[] { C(255, 107, 142), C(233, 68, 106) } };
    const int Blue = 0, Peach = 1, Purple = 2, Green = 3, Pink = 4, Yellow = 5;

    // ---- Procedural art -----------------------------------------------------------------------
    delegate float Cov(float x, float y); // y-down pixel space
    static float Aa(float d) => Mathf.Clamp01(.5f - d);
    static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);
    static float SdSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
        float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
        return Len(pax - bax * h, pay - bay * h);
    }
    static float SdRoundRect(float px, float py, float cx, float cy, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(px - cx) - (hw - r), qy = Mathf.Abs(py - cy) - (hh - r);
        return Len(Mathf.Max(qx, 0), Mathf.Max(qy, 0)) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }
    static Cov Capsule(float ax, float ay, float bx, float by, float r) => (x, y) => Aa(SdSegment(x, y, ax, ay, bx, by) - r);
    static Cov Disc(float cx, float cy, float r) => (x, y) => Aa(Len(x - cx, y - cy) - r);
    static Cov Ring(float cx, float cy, float r, float w) => (x, y) => Aa(Mathf.Abs(Len(x - cx, y - cy) - r) - w / 2);
    static Cov Arc(float cx, float cy, float r, float w, float from, float to) => (x, y) =>
    {
        float a = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg; if (a < 0) a += 360;
        return a >= from && a <= to ? Aa(Mathf.Abs(Len(x - cx, y - cy) - r) - w / 2) : 0;
    };
    static Cov RRect(float cx, float cy, float hw, float hh, float r) => (x, y) => Aa(SdRoundRect(x, y, cx, cy, hw, hh, r));
    static Cov RRectRing(float cx, float cy, float hw, float hh, float r, float w) => (x, y) => Aa(Mathf.Abs(SdRoundRect(x, y, cx, cy, hw, hh, r) + w / 2) - w / 2);
    static Cov Poly(params float[] xy) => (x, y) =>
    {
        int inside = 0;
        for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)
        {
            float px = x - .5f + (sx + .5f) / 4, py = y - .5f + (sy + .5f) / 4; bool hit = false;
            for (int i = 0, j = xy.Length / 2 - 1; i < xy.Length / 2; j = i++)
            {
                float xi = xy[i * 2], yi = xy[i * 2 + 1], xj = xy[j * 2], yj = xy[j * 2 + 1];
                if ((yi > py) != (yj > py) && px < (xj - xi) * (py - yi) / (yj - yi) + xi) hit = !hit;
            }
            if (hit) inside++;
        }
        return inside / 16f;
    };
    static Cov Union(params Cov[] parts) => (x, y) => parts.Max(p => p(x, y));

    static Sprite Bake(string name, int w, int h, Func<float, float, Color> pixel, Vector4 border, bool force = true)
    {
        string path = ArtDir + name + ".png";
        if (!force && File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Directory.CreateDirectory(ArtDir);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) pixels[j * w + i] = pixel(i + .5f, h - (j + .5f));
        tex.SetPixels(pixels); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.spritePixelsPerUnit = 100;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static Sprite Shape(string name, int size, Cov cov, float border = 0) =>
        Bake(name, size, size, (x, y) => new Color(1, 1, 1, cov(x, y)), Vector4.one * border);
    static Sprite Layers(string name, int w, int h, params (Cov cov, Color color)[] layers) => Bake(name, w, h, (x, y) =>
    {
        Color result = Color.clear;
        foreach (var (cov, color) in layers)
        {
            float a = cov(x, y) * color.a; if (a <= 0) continue;
            float outA = a + result.a * (1 - a);
            result = new Color((color.r * a + result.r * result.a * (1 - a)) / outA, (color.g * a + result.g * result.a * (1 - a)) / outA, (color.b * a + result.b * result.a * (1 - a)) / outA, outA);
        }
        return result;
    }, Vector4.zero);

    static Sprite sCard, sShadow, sTile, sSquircle, sPill, sCircle, sRing, sBackground, sGlow, sMotif;
    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    static void BakeArt()
    {
        sCard = Shape("Card", 128, RRect(64, 64, 63.5f, 63.5f, 56), 60);
        sTile = Shape("Tile", 96, RRect(48, 48, 47.5f, 47.5f, 40), 44);
        sSquircle = Shape("Squircle", 72, RRect(36, 36, 35.5f, 35.5f, 26), 32);
        sPill = Shape("Pill", 128, Disc(64, 64, 63.5f), 64);
        sCircle = Shape("Circle", 256, Disc(128, 128, 127));
        sRing = Shape("Ring", 256, Ring(128, 128, 114, 22));
        sShadow = Bake("CardShadow", 256, 256, (x, y) =>
        {
            float d = SdRoundRect(x, y, 128, 128, 80, 80, 56), a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-6, 46, d));
            return new Color(1, 1, 1, a * a);
        }, Vector4.one * 104);
        Color top = C(228, 223, 252), mid = C(237, 238, 252), low = C(246, 247, 252);
        sBackground = Bake("Background", 8, 512, (x, y) => y < 220 ? Color.Lerp(top, mid, y / 220) : Color.Lerp(mid, low, (y - 220) / 292), Vector4.zero);
        sGlow = Bake("Glow", 256, 256, (x, y) => { float d = Mathf.Clamp01(Len(x - 128, y - 128) / 127); float a = 1 - Mathf.SmoothStep(0, 1, d); return new Color(1, 1, 1, a * a); }, Vector4.zero);
        // Soft line-and-node motif for the top-right corner (echoes connected tap targets).
        Color blue = C(126, 178, 240, .7f), yellow = C(252, 214, 110, .75f), pink = C(240, 160, 196, .7f), white = Color.white;
        sMotif = Layers("Motif", 640, 480,
            (Capsule(60, 150, 330, 150, 13), blue), (Capsule(330, 150, 470, 300, 13), blue), (Capsule(470, 300, 640, 300, 13), blue),
            (Capsule(330, 0, 330, 420, 13), yellow), (Capsule(330, 150, 470, 70, 13), yellow),
            (Capsule(470, 300, 470, 480, 13), pink), (Capsule(560, 300, 560, 480, 13), pink), (Capsule(470, 300, 560, 300, 13), pink),
            (Disc(60, 150, 26), white), (Ring(60, 150, 22, 10), blue), (Disc(330, 150, 26), white), (Ring(330, 150, 22, 10), yellow),
            (Disc(330, 420, 26), white), (Ring(330, 420, 22, 10), yellow), (Disc(470, 300, 26), white), (Ring(470, 300, 22, 10), pink),
            (Disc(560, 300, 26), white), (Ring(560, 300, 22, 10), pink), (Disc(470, 70, 26), white), (Ring(470, 70, 22, 10), yellow));
        // Flat glyphs, white, recoloured per use.
        icons["pause"] = Shape("Icon-Pause", 128, Union(Capsule(46, 34, 46, 94, 12), Capsule(82, 34, 82, 94, 12)));
        icons["play"] = Shape("Icon-Play", 128, Poly(44, 28, 44, 100, 104, 64));
        icons["arrow-right"] = Shape("Icon-ArrowRight", 128, Union(Capsule(26, 64, 100, 64, 9), Capsule(100, 64, 70, 34, 9), Capsule(100, 64, 70, 94, 9)));
        icons["arrow-left"] = Shape("Icon-ArrowLeft", 128, Union(Capsule(28, 64, 102, 64, 9), Capsule(28, 64, 58, 34, 9), Capsule(28, 64, 58, 94, 9)));
        icons["close"] = Shape("Icon-Close", 128, Union(Capsule(38, 38, 90, 90, 9), Capsule(38, 90, 90, 38, 9)));
        icons["restart"] = Shape("Icon-Restart", 128, Union(Arc(64, 66, 34, 14, 40, 320), Poly(104, 28, 104, 66, 70, 52)));
        icons["sound"] = Shape("Icon-Sound", 128, Union(Poly(20, 50, 42, 50, 68, 26, 68, 102, 42, 78, 20, 78), Arc(72, 64, 20, 9, 0, 50), Arc(72, 64, 20, 9, 310, 360), Arc(72, 64, 38, 9, 0, 48), Arc(72, 64, 38, 9, 312, 360)));
        icons["motion"] = Shape("Icon-Motion", 128, Union(Capsule(22, 40, 84, 40, 8), Capsule(40, 64, 106, 64, 8), Capsule(22, 88, 76, 88, 8)));
        icons["vibrate"] = Shape("Icon-Vibrate", 128, Union(RRectRing(64, 64, 22, 36, 9, 9), Capsule(22, 48, 22, 80, 6), Capsule(106, 48, 106, 80, 6)));
        icons["trophy"] = Shape("Icon-Trophy", 128, Union(RRect(64, 46, 28, 24, 6), Disc(64, 62, 28), RRect(64, 92, 7, 12, 2), RRect(64, 108, 26, 7, 4), Arc(36, 48, 14, 8, 90, 270), Arc(92, 48, 14, 8, 270, 360), Arc(92, 48, 14, 8, 0, 90)));
        icons["grid"] = Shape("Icon-Grid", 128, Union(RRect(42, 42, 18, 18, 6), RRect(86, 42, 18, 18, 6), RRect(42, 86, 18, 18, 6), RRect(86, 86, 18, 18, 6)));
        icons["clock"] = Shape("Icon-Clock", 128, Union(Ring(64, 64, 42, 11), Capsule(64, 64, 64, 38, 7), Capsule(64, 64, 84, 74, 7)));
        icons["plus"] = Shape("Icon-Plus", 128, Union(Capsule(64, 30, 64, 98, 11), Capsule(30, 64, 98, 64, 11)));
    }

    static TMP_FontAsset fExtra, fBold, fSemi, fMedium;
    static TMP_FontAsset Font(string weight)
    {
        string path = FontDir + "Poppins-" + weight + " SDF.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (asset != null) return asset;
        var source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(FontDir + "Poppins-" + weight + ".ttf");
        if (source == null) throw new Exception("Missing font " + weight);
        asset = TMP_FontAsset.CreateFontAsset(source, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024);
        asset.name = "Poppins-" + weight + " SDF";
        var chars = new StringBuilder(); for (int c = 32; c <= 126; c++) chars.Append((char)c);
        chars.Append(" ·×–—‘’“”…•");
        if (!asset.TryAddCharacters(chars.ToString(), out string missing) && missing.Trim().Length > 0) log.AppendLine("Poppins " + weight + " lacks: " + missing);
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(asset, path);
        foreach (var atlas in asset.atlasTextures) { atlas.name = asset.name + " Atlas"; AssetDatabase.AddObjectToAsset(atlas, asset); }
        asset.material.name = asset.name + " Material"; AssetDatabase.AddObjectToAsset(asset.material, asset);
        EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
        if (asset.atlasTextures.Length != 1) log.AppendLine("Poppins " + weight + " uses " + asset.atlasTextures.Length + " atlases.");
        return asset;
    }

    // ---- Canvas helpers -----------------------------------------------------------------------
    static readonly Vector2 Mid = new Vector2(.5f, .5f), TopEdge = new Vector2(.5f, 1), BottomEdge = new Vector2(.5f, 0);
    static Transform T(string path) => frame.Find(path) ?? throw new Exception("Missing Portrait frame/" + path);
    static RectTransform R(string path) => (RectTransform)T(path);
    static RectTransform Pin(RectTransform r, Vector2 anchor, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = Mid; r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
        r.localScale = Vector3.one; r.localRotation = Quaternion.identity; return r;
    }
    static RectTransform Pin(string path, Vector2 anchor, float x, float y, float w, float h) => Pin(R(path), anchor, x, y, w, h);
    static RectTransform Fill(RectTransform r, float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = Mid; r.localScale = Vector3.one; r.localRotation = Quaternion.identity;
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top); return r;
    }
    // Full-height column: fixed width, stretched between top and bottom insets.
    static RectTransform Column(RectTransform r, float width, float top, float bottom)
    {
        r.anchorMin = new Vector2(.5f, 0); r.anchorMax = new Vector2(.5f, 1); r.pivot = Mid; r.localScale = Vector3.one;
        r.sizeDelta = new Vector2(width, -(top + bottom)); r.anchoredPosition = new Vector2(0, (bottom - top) / 2); return r;
    }
    static T Ensure<T>(GameObject go) where T : Component => go.GetComponent<T>() ?? go.AddComponent<T>();
    static Image Skin(Image image, Sprite sprite, Color color, bool sliced = true)
    {
        image.sprite = sprite; image.color = color; image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1; image.preserveAspect = true; image.fillCenter = true; return image;
    }
    static Image Skin(string path, Sprite sprite, Color color, bool sliced = true) => Skin(Ensure<Image>(T(path).gameObject), sprite, color, sliced);
    static Image NewImage(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = Skin(go.GetComponent<Image>(), sprite, color, sliced); image.raycastTarget = false; return image;
    }
    static void Gradient(Graphic g, Color from, Color to, bool vertical = false) => Ensure<UIGradient>(g.gameObject).Set(from, to, vertical);
    static TMP_Text Text(TMP_Text t, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, float min = 20)
    {
        t.font = font; t.fontSharedMaterial = font.material; t.color = color; t.enableVertexGradient = false;
        t.fontStyle = FontStyles.Normal; t.characterSpacing = 0; t.extraPadding = false; t.raycastTarget = false;
        t.enableAutoSizing = true; t.fontSize = t.fontSizeMax = size; t.fontSizeMin = Mathf.Min(min, size); t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 0; return t;
    }
    static TMP_Text Text(string path, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center, float min = 20) =>
        Text(T(path).GetComponent<TMP_Text>(), font, size, color, align, min);
    static Image Glyph(Transform parent, string icon, float size, Color color, float x = 0, float y = 0)
    {
        var image = NewImage(parent, "Glyph", icons[icon], color, false);
        Pin(image.rectTransform, Mid, x, y, size, size); image.transform.SetAsLastSibling(); return image;
    }
    // White rounded card with a soft lavender shadow drawn as the previous sibling.
    static Image Card(RectTransform r, bool shadow = true, Sprite sprite = null)
    {
        var image = Skin(Ensure<Image>(r.gameObject), sprite ?? sCard, Color.white);
        if (!shadow) return image;
        var s = NewImage(r.parent, r.name + " shadow", sShadow, C(90, 80, 190, .22f));
        s.transform.SetSiblingIndex(r.GetSiblingIndex());
        var sr = s.rectTransform; sr.anchorMin = r.anchorMin; sr.anchorMax = r.anchorMax; sr.pivot = r.pivot; sr.localScale = Vector3.one;
        sr.anchoredPosition = r.anchoredPosition + new Vector2(0, -14); sr.sizeDelta = r.sizeDelta + new Vector2(96, 96);
        return image;
    }
    // Pastel tile: edge colour on the host, inset fill with a thicker bottom lip.
    static Image Tile(RectTransform host, int palette)
    {
        var edge = Skin(Ensure<Image>(host.gameObject), sTile, TileEdge[palette]);
        var fill = NewImage(host, "Fill", sTile, TileFill[palette]); fill.transform.SetAsFirstSibling(); Fill(fill.rectTransform, 3, 3, 3, 8);
        return edge;
    }
    static Image Outlined(RectTransform host, Color fillColor)
    {
        var edge = Skin(Ensure<Image>(host.gameObject), sTile, Line);
        var fill = NewImage(host, "Fill", sTile, fillColor); fill.transform.SetAsFirstSibling(); Fill(fill.rectTransform, 3, 3, 3, 6);
        return fill;
    }
    static Image IconTile(Transform parent, string name, int palette, string icon, float x, float y, float size)
    {
        var tile = NewImage(parent, name, sSquircle, Color.white); Pin(tile.rectTransform, Mid, x, y, size, size);
        Gradient(tile, IconGrad[palette][0], IconGrad[palette][1], true);
        if (icon != null) Glyph(tile.transform, icon, size * .52f, Color.white);
        return tile;
    }
    static void ButtonColors(Button b, bool tintGraphic = true)
    {
        var c = b.colors; c.normalColor = c.highlightedColor = c.selectedColor = Color.white;
        c.pressedColor = new Color(.86f, .88f, 1); c.disabledColor = new Color(.86f, .87f, .92f, .7f); c.fadeDuration = .08f; b.colors = c;
    }
    // Gradient pill CTA with a white knob carrying the glyph (the screenshot's primary button).
    static void PrimaryPill(RectTransform button, string labelText, string knobIcon, Color from, Color to)
    {
        var image = Skin(Ensure<Image>(button.gameObject), sPill, Color.white); Gradient(image, from, to);
        ButtonColors(button.GetComponent<Button>());
        float w = button.sizeDelta.x, h = button.sizeDelta.y;
        var label = button.GetComponentsInChildren<TMP_Text>(true).First();
        Pin(label.rectTransform, Mid, knobIcon != null ? -h * .3f : 0, 2, w - (knobIcon != null ? h * 2.2f : 64), h * .62f);
        Text(label, fBold, Mathf.Round(h * .36f), Color.white, TextAlignmentOptions.Center, 28);
        if (labelText != null) label.text = labelText;
        var icon = button.Find("Button icon");
        if (knobIcon == null) { if (icon != null) icon.gameObject.SetActive(false); return; }
        var knob = icon != null ? Skin(icon.GetComponent<Image>(), sCircle, Color.white, false) : NewImage(button, "Button icon", sCircle, Color.white, false);
        knob.gameObject.SetActive(true); Pin(knob.rectTransform, Mid, w / 2 - h / 2, 0, h - 24, h - 24);
        Glyph(knob.transform, knobIcon, (h - 24) * .5f, to);
    }
    static void SolidPill(RectTransform button, string labelText, Color fill, Color ink, string icon, bool outline = false)
    {
        var image = outline ? Outlined(button, Color.white) : Skin(Ensure<Image>(button.gameObject), sPill, fill);
        if (outline) { var host = button.GetComponent<Image>(); host.sprite = sPill; image.sprite = sPill; button.Find("Fill").GetComponent<Image>().sprite = sPill; }
        ButtonColors(button.GetComponent<Button>());
        float w = button.sizeDelta.x, h = button.sizeDelta.y;
        var label = button.GetComponentsInChildren<TMP_Text>(true).First();
        Pin(label.rectTransform, Mid, icon != null ? 24 : 0, 2, w - 200, h * .62f);
        Text(label, fBold, Mathf.Round(h * .36f), ink, TextAlignmentOptions.Center, 26);
        if (labelText != null) label.text = labelText;
        var iconT = button.Find("Button icon");
        if (iconT == null) return;
        if (icon == null) { iconT.gameObject.SetActive(false); return; }
        var img = Skin(iconT.GetComponent<Image>(), icons[icon], ink, false); img.gameObject.SetActive(true);
        Pin(img.rectTransform, Mid, -w / 2 + 70, 0, h * .42f, h * .42f);
    }
    static void RoundButton(RectTransform button, string icon, Color background, Color ink)
    {
        Skin(Ensure<Image>(button.gameObject), sCircle, background, false);
        foreach (Transform child in button) if (child.name != "Glyph") child.gameObject.SetActive(false);
        Glyph(button, icon, button.sizeDelta.x * .42f, ink);
        var b = button.GetComponent<Button>(); if (b != null) ButtonColors(b);
    }

    // Unreferenced decoration is deleted; anything a script references is hidden instead.
    static bool Referenced(GameObject candidate)
    {
        var components = new HashSet<Object>(candidate.GetComponentsInChildren<Component>(true));
        var targets = new HashSet<Object>(components.Concat(candidate.GetComponentsInChildren<Transform>(true).Select(t => (Object)t.gameObject)));
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component == null || components.Contains(component)) continue;
            var p = new SerializedObject(component).GetIterator();
            while (p.Next(true))
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null && p.propertyPath != "m_GameObject" &&
                    p.propertyPath != "m_Father" && !p.propertyPath.StartsWith("m_Children") && targets.Contains(p.objectReferenceValue)) return true;
        }
        return false;
    }
    static int dropped, hidden;
    static void Drop(params string[] paths)
    {
        foreach (var path in paths)
        {
            var t = frame.Find(path); if (t == null) continue;
            if (Referenced(t.gameObject)) { t.gameObject.SetActive(false); hidden++; log.AppendLine("Hid referenced " + path); }
            else { Object.DestroyImmediate(t.gameObject); dropped++; }
        }
    }
    static void FillSections(Transform parent, params string[] names) { foreach (var n in names) { var t = parent.Find(n); if (t != null) Fill((RectTransform)t); } }

    // ---- Background ---------------------------------------------------------------------------
    static void Background()
    {
        var bg = root.transform.Find("Background");
        var image = Skin(bg.GetComponent<Image>(), sBackground, Color.white, false); image.preserveAspect = false;
        var glowBlue = NewImage(bg, "Glow blue", sGlow, C(170, 196, 255, .55f), false); Pin(glowBlue.rectTransform, new Vector2(0, 1), 80, -140, 1000, 1000);
        var glowPink = NewImage(bg, "Glow pink", sGlow, C(250, 196, 232, .5f), false); Pin(glowPink.rectTransform, new Vector2(1, 1), -60, -300, 900, 900);
        var motif = NewImage(bg, "Motif", sMotif, C(255, 255, 255, .9f), false); Pin(motif.rectTransform, new Vector2(1, 1), -290, -250, 640, 480);
    }

    // ---- Gameplay HUD -------------------------------------------------------------------------
    // Vertical stack from the top (reference px): margin 32, header 120, 24, stats 200, 16,
    // objective 120, 16, mechanics 96, 16 -> arena. From the bottom: margin 40, combo 150, 16, hint 112, 16.
    const float ArenaTop = Top + 120 + 24 + 200 + 16 + 120 + 16 + 96 + 16, ArenaBottom = Bottom + 150 + 16 + 112 + 16;
    static void Hud()
    {
        const string H = "HUD/";
        foreach (Transform section in T("HUD")) if (section.name != "Combo card") Fill((RectTransform)section);
        FillSections(T(H + "Header"), "Level title"); FillSections(T(H + "Objective"), "Objective label");
        Drop(H + "Header/Illustrated logo", H + "Header/Level title/Level title plate", H + "Objective/Objective label/Objective plate",
            H + "Objective/Objective track/Track color", H + "Combo card/Combo divider");

        // Header: round pause, two-line level title, wallet pill.
        float headerY = -(Top + 60);
        RoundButton(Pin(H + "Header/Pause button", TopEdge, -440, headerY, 120, 120), "pause", Color.white, Ink);
        Pin(H + "Header/Level title/Level title", TopEdge, -70, headerY, 540, 116);
        var title = Text(H + "Header/Level title/Level title", fExtra, 44, Ink, TextAlignmentOptions.MidlineLeft, 30); title.lineSpacing = -12;
        var wallet = Pin(H + "Header/Wallet shortcut", TopEdge, 360, headerY, 260, 96);
        Skin(wallet.GetComponent<Image>(), sPill, Color.white); ButtonColors(wallet.GetComponent<Button>());
        Pin((RectTransform)wallet.Find("Coin"), Mid, -86, 0, 64, 64);
        Pin((RectTransform)wallet.Find("Balance"), Mid, 4, 2, 110, 56); Text(wallet.Find("Balance").GetComponent<TMP_Text>(), fBold, 36, Ink, TextAlignmentOptions.Center, 24);
        var plus = Skin((Image)wallet.Find("Plus").GetComponent<Image>(), sCircle, Color.white, false); Pin(plus.rectTransform, Mid, 86, 0, 64, 64);
        Gradient(plus, Primary, Accent); foreach (Transform c in plus.transform) c.gameObject.SetActive(false); Glyph(plus.transform, "plus", 30, Color.white);

        // Stats: one white card with three pastel chips.
        float statsY = -(Top + 120 + 24 + 100);
        var statsBg = NewImage(T(H + "Stats"), "Stats card", sCard, Color.white); statsBg.transform.SetAsFirstSibling();
        Pin(statsBg.rectTransform, TopEdge, 0, statsY, Width, 200); Card(statsBg.rectTransform);
        string[] chips = { "Score card", "Level card", "Timer card" }; int[] palette = { Peach, Blue, Purple };
        for (int i = 0; i < 3; i++)
        {
            var chip = Pin(H + "Stats/" + chips[i], TopEdge, -324 + i * 324, statsY, 300, 164); Tile(chip, palette[i]);
            foreach (var label in chip.GetComponentsInChildren<TMP_Text>(true))
            {
                bool value = label.name == "Value" || label.name == "Time";
                Pin(label.rectTransform, Mid, 50, value ? -16 : 34, 170, value ? 64 : 36);
                Text(label, value ? fExtra : fSemi, value ? 46 : 26, value ? Ink : Sub, TextAlignmentOptions.MidlineLeft, value ? 28 : 18);
            }
        }
        T(H + "Stats/Score card/Caption").GetComponent<TMP_Text>().text = "Score";
        T(H + "Stats/Level card/Caption").GetComponent<TMP_Text>().text = "Level";
        T(H + "Stats/Timer card/Caption").GetComponent<TMP_Text>().text = "Seconds";
        var trophy = Skin(H + "Stats/Score card/Trophy", sSquircle, Color.white); Pin(trophy.rectTransform, Mid, -92, 4, 88, 88);
        Gradient(trophy, IconGrad[Peach][0], IconGrad[Peach][1], true); Glyph(trophy.transform, "trophy", 48, Color.white);
        IconTile(T(H + "Stats/Level card"), "Icon", Blue, "grid", -92, 4, 88);
        var ringTrack = Skin(H + "Stats/Timer card/Track", sRing, C(214, 204, 250), false); Pin(ringTrack.rectTransform, Mid, -92, 4, 92, 92);
        var ring = Skin(H + "Stats/Timer card/Countdown ring", sRing, Primary, false); Pin(ring.rectTransform, Mid, -92, 4, 92, 92);
        ring.type = Image.Type.Filled; ring.fillMethod = Image.FillMethod.Radial360; ring.fillOrigin = (int)Image.Origin360.Top;
        Glyph(T(H + "Stats/Timer card/Track"), "clock", 40, Accent);

        // Objective card: label over a rounded progress track with the three star markers.
        float objectiveY = -(Top + 120 + 24 + 200 + 16 + 60);
        var objectiveBg = NewImage(T(H + "Objective"), "Objective card", sCard, Color.white); objectiveBg.transform.SetAsFirstSibling();
        Pin(objectiveBg.rectTransform, TopEdge, 0, objectiveY, Width, 120); Card(objectiveBg.rectTransform);
        Pin(H + "Objective/Objective label/Objective", TopEdge, -150, objectiveY + 28, 640, 44);
        Text(H + "Objective/Objective label/Objective", fSemi, 30, Ink, TextAlignmentOptions.MidlineLeft, 22);
        var track = Skin(H + "Objective/Objective track", sPill, Track); Pin(track.rectTransform, TopEdge, 0, objectiveY - 24, 920, 38);
        var progress = (Image)T(H + "Objective/Objective track/Progress").GetComponent<Image>();
        Pin(progress.rectTransform, Mid, 0, 0, 912, 30); progress.color = Color.white; Gradient(progress, C(255, 200, 40), C(255, 128, 31));
        for (int i = 1; i <= 3; i++)
        {
            var star = R(H + "Objective/Objective track/Objective star " + i);
            Pin(star, Mid, star.anchoredPosition.x, 0, 48, 48);
        }

        // Mechanics band (levels 11+): status text and a full-size Shield pill.
        float mechanicsY = -(Top + 120 + 24 + 200 + 16 + 120 + 16 + 48);
        Pin(H + "Mechanics/Mechanics status", TopEdge, -170, mechanicsY, 640, 56);
        Text(H + "Mechanics/Mechanics status", fSemi, 30, Ink, TextAlignmentOptions.MidlineLeft, 22);
        var shield = Pin(H + "Mechanics/Shield", TopEdge, 330, mechanicsY, 320, 96);
        Skin(shield.GetComponent<Image>(), sPill, Primary); ButtonColors(shield.GetComponent<Button>());
        var shieldLabel = shield.GetComponentInChildren<TMP_Text>(true); Pin(shieldLabel.rectTransform, Mid, 0, 2, 280, 60);
        Text(shieldLabel, fBold, 34, Color.white, TextAlignmentOptions.Center, 22);

        // Arena: white card filling the remaining height; scenery is a soft tint inside.
        var arenaSection = T(H + "Arena");
        var rim = Column(R(H + "Arena/Arena rim"), Width, ArenaTop, ArenaBottom); rim.SetAsFirstSibling();
        var rimImage = Card(rim); rimImage.raycastTarget = false;
        foreach (string inner in new[] { "Target arena", "Pooled feedback" }) Column(R(H + "Arena/" + inner), Width - 20, ArenaTop + 10, ArenaBottom + 10);
        var mask = Skin(H + "Arena/Target arena", sCard, Color.white); mask.preserveAspect = false;
        var scenery = (Image)T(H + "Arena/Target arena/Arena scenery").GetComponent<Image>(); Fill(scenery.rectTransform); scenery.color = new Color(1, 1, 1, .32f); scenery.preserveAspect = false;
        foreach (Transform popup in T(H + "Arena/Pooled feedback"))
        {
            var points = popup.Find("Points"); if (points == null) continue;
            var label = points.GetComponent<TMP_Text>(); label.font = fExtra; label.fontSharedMaterial = fExtra.material; label.color = Color.white; label.extraPadding = false;
        }

        // Bottom stack: hint card above the combo card.
        float hintY = Bottom + 150 + 16 + 56;
        var hintPlate = R(H + "Tutorial hint/Tutorial hint plate"); Pin(hintPlate, BottomEdge, 0, hintY, Width, 112); Outlined(hintPlate, Color.white);
        Pin(H + "Tutorial hint/Tutorial hint", BottomEdge, 0, hintY + 22, 940, 48); Text(H + "Tutorial hint/Tutorial hint", fBold, 36, Ink, TextAlignmentOptions.Center, 24);
        Pin(H + "Tutorial hint/Tutorial detail", BottomEdge, 0, hintY - 24, 940, 38); Text(H + "Tutorial hint/Tutorial detail", fMedium, 28, Sub, TextAlignmentOptions.Center, 20);
        var combo = Pin(H + "Combo card", BottomEdge, 0, Bottom + 75, Width, 150); Card(combo);
        Pin(H + "Combo card/Combo caption", Mid, -400, 34, 160, 34); Text(H + "Combo card/Combo caption", fSemi, 26, Sub).text = "Combo";
        Pin(H + "Combo card/Combo value", Mid, -400, -14, 160, 72); Text(H + "Combo card/Combo value", fExtra, 60, Ink);
        Pin(H + "Combo card/Fever state", Mid, 20, 0, 470, 60); Text(H + "Combo card/Fever state", fBold, 34, Primary, TextAlignmentOptions.Center, 22);
        Pin(H + "Combo card/Combo flame", Mid, 296, 0, 76, 92);
        var multiplierChip = NewImage(T(H + "Combo card"), "Multiplier chip", sPill, C(255, 241, 199)); Pin(multiplierChip.rectTransform, Mid, 410, 0, 140, 84);
        multiplierChip.transform.SetSiblingIndex(T(H + "Combo card/Multiplier").GetSiblingIndex());
        Pin(H + "Combo card/Multiplier", Mid, 410, 0, 126, 68); Text(H + "Combo card/Multiplier", fExtra, 46, C(196, 104, 0), TextAlignmentOptions.Center, 28);
        var feverLine = T(H + "Combo card/Fever glow line"); feverLine.gameObject.SetActive(false);
    }

    // ---- Full-screen pages --------------------------------------------------------------------
    // Shared page header: round back button, grey eyebrow, bold title (the screenshot pattern).
    const float EyebrowY = -(Top + 120 + 24 + 22), TitleY = -(Top + 120 + 24 + 44 + 8 + 52), BodyTop = Top + 120 + 24 + 44 + 8 + 104 + 24;
    static void PageHeader(string back, string eyebrow, string title, bool hasBack = true, float shift = 0)
    {
        if (back != null) { var b = Pin(back, TopEdge, -440, -(Top + 60), 120, 120); RoundButton(b, "arrow-left", Color.white, Ink); }
        Pin(eyebrow, TopEdge, -150, EyebrowY + shift, 700, 44); Text(eyebrow, fSemi, 34, Sub, TextAlignmentOptions.MidlineLeft, 24);
        Pin(title, TopEdge, 0, TitleY + shift, Width, 104); Text(title, fExtra, 80, Ink, TextAlignmentOptions.MidlineLeft, 44);
    }
    static RectTransform Page(string overlay, string card)
    {
        T(overlay).GetComponent<Image>().color = Color.clear;
        var page = Fill(R(overlay + "/" + card)); var image = page.GetComponent<Image>(); image.color = Color.clear; image.raycastTarget = false;
        foreach (Transform section in page) if (section.GetComponent<Graphic>() == null) Fill((RectTransform)section);
        return page;
    }
    static float Center(float topOffset, float height) => -(topOffset + height / 2);

    static void LevelSelect()
    {
        const string P = "Screens/Level select/Level select card/";
        Drop(P + "Frame", P + "Header/Selection logo", P + "Header/Title plate", P + "Header/Selection star left", P + "Header/Selection star right",
            P + "Header/Eyebrow plate", P + "Header/Description", P + "Wallet/Wallet plate", P + "Wallet/Selection coins right", P + "Wallet/Selection coins top",
            P + "Wallet/Selection wallet divider", P + "Wallet/Footer");
        Page("Screens/Level select", "Level select card");
        PageHeader("Screens/Level select/Close level selection", P + "Header/Eyebrow", P + "Header/Title");
        // Wallet card: coins and XP chips.
        float walletY = Center(BodyTop, 150);
        var walletCard = NewImage(T(P + "Wallet"), "Wallet card", sCard, Color.white); walletCard.transform.SetAsFirstSibling();
        Pin(walletCard.rectTransform, TopEdge, 0, walletY, Width, 150); Card(walletCard.rectTransform);
        var coins = NewImage(T(P + "Wallet"), "Coins chip", sTile, Color.white); Pin(coins.rectTransform, TopEdge, -242, walletY, 468, 110); Tile(coins.rectTransform, Peach);
        coins.transform.SetSiblingIndex(walletCard.transform.GetSiblingIndex() + 1);
        var xp = NewImage(T(P + "Wallet"), "XP chip", sTile, Color.white); Pin(xp.rectTransform, TopEdge, 242, walletY, 468, 110); Tile(xp.rectTransform, Purple);
        xp.transform.SetSiblingIndex(coins.transform.GetSiblingIndex() + 1);
        Pin(P + "Wallet/Selection coins left", TopEdge, -416, walletY, 72, 72);
        Pin(P + "Wallet/Wallet", TopEdge, -200, walletY, 300, 96); Text(P + "Wallet/Wallet", fExtra, 40, Ink, TextAlignmentOptions.MidlineLeft, 24).lineSpacing = -14;
        Pin(P + "Wallet/Selection XP icon", TopEdge, 68, walletY, 80, 80);
        Pin(P + "Wallet/Selection experience", TopEdge, 286, walletY, 300, 96); Text(P + "Wallet/Selection experience", fExtra, 40, Ink, TextAlignmentOptions.MidlineLeft, 24).lineSpacing = -14;
        // Level list fills the rest of the screen.
        float listTop = BodyTop + 150 + 24;
        var viewport = Column(R(P + "Level list/Level list viewport"), Width + 20, listTop, Bottom);
        var listScroll = viewport.GetComponent<ScrollRect>(); listScroll.movementType = ScrollRect.MovementType.Elastic; listScroll.elasticity = .12f;
        var bar = R(P + "Level list/Level list scrollbar"); bar.anchorMin = new Vector2(.5f, 0); bar.anchorMax = new Vector2(.5f, 1);
        bar.sizeDelta = new Vector2(6, -(listTop + 16 + Bottom + 16)); bar.anchoredPosition = new Vector2(Width / 2 + 16, (Bottom - listTop) / 2);
        Skin(bar.GetComponent<Image>(), sPill, C(91, 108, 255, .18f)); Skin(bar.Find("Scroll handle").GetComponent<Image>(), sPill, C(91, 108, 255, .7f));
        var content = (RectTransform)viewport.Find("Level list content");
        var buttons = root.GetComponent<GameHUD>(); var rows = new SerializedObject(buttons).FindProperty("levelButtons");
        int[] cycle = { Blue, Green, Purple, Peach, Pink };
        for (int i = 0; i < rows.arraySize; i++)
        {
            var row = (RectTransform)((Button)rows.GetArrayElementAtIndex(i).objectReferenceValue).transform;
            row.anchorMin = row.anchorMax = TopEdge; row.sizeDelta = new Vector2(Width, 144);
            var fill = Outlined(row, Color.white); var button = row.GetComponent<Button>(); button.targetGraphic = fill; ButtonColors(button);
            var badge = Skin(row.Find("Number badge").GetComponent<Image>(), sSquircle, Color.white); Pin(badge.rectTransform, Mid, -406, 0, 104, 104);
            Gradient(badge, IconGrad[cycle[i % 5]][0], IconGrad[cycle[i % 5]][1], true);
            foreach (Transform c in badge.transform) c.gameObject.SetActive(false);
            Pin((RectTransform)row.Find("Level number"), Mid, -406, 2, 104, 80); Text(row.Find("Level number").GetComponent<TMP_Text>(), fExtra, 46, Color.white, TextAlignmentOptions.Center, 30);
            var landscape = row.Find("Level landscape"); Pin((RectTransform)landscape, Mid, -372, 0, 56, 56); landscape.gameObject.SetActive(false);
            var title = row.Find("Label").GetComponent<TMP_Text>(); Pin(title.rectTransform, Mid, -46, 26, 560, 48);
            Text(title, fBold, 36, Ink, TextAlignmentOptions.MidlineLeft, 32).textWrappingMode = TextWrappingModes.NoWrap;
            var status = row.Find("Level " + (i + 1) + " progress").GetComponent<TMP_Text>(); Pin(status.rectTransform, Mid, -46, -30, 560, 40);
            Text(status, fMedium, 27, Sub, TextAlignmentOptions.MidlineLeft, 24).textWrappingMode = TextWrappingModes.NoWrap;
            var plate = row.Find("Level " + (i + 1) + " progress plate"); if (plate != null) Object.DestroyImmediate(plate.gameObject);
            for (int s = 1; s <= 3; s++) Pin((RectTransform)row.Find("Earned star " + s), Mid, 300 + (s - 1) * 58, 0, 52, 52);
        }
    }

    static void Briefing()
    {
        const string P = "Screens/Level briefing/Level briefing card/";
        Drop(P + "Frame", P + "Header/Briefing logo", P + "Header/Eyebrow plate", P + "Header/Title star left", P + "Header/Title star right",
            P + "Hero/Hero star left", P + "Hero/Hero star low", P + "Hero/Hero star right", P + "Goal/Goal plate",
            P + "Star rules/Mastery divider 0", P + "Star rules/Mastery divider 1", P + "Reward/Reward coin two", P + "Reward/Reward coin three",
            P + "Reward/Reward caption", P + "Reward/Reward divider", P + "Wallet/Best plate", P + "Wallet/Wallet coin", P + "Wallet/Wallet XP");
        Page("Screens/Level briefing", "Level briefing card");
        PageHeader(P + "Header/Back to levels", P + "Header/Eyebrow", P + "Header/Name");
        // Hero card: target visual, goal and how-to.
        float heroY = Center(BodyTop, 500);
        var hero = NewImage(T(P + "Hero"), "Hero card", sCard, Color.white); hero.transform.SetAsFirstSibling(); Pin(hero.rectTransform, TopEdge, 0, heroY, Width, 500); Card(hero.rectTransform);
        Pin(P + "Hero/Hero target", TopEdge, 0, heroY + 110, 220, 220);
        var preview = R(P + "Hero/Target size preview"); Pin(preview, TopEdge, 0, heroY + 110, 810, 270);
        Pin(P + "Goal/Goal", TopEdge, 0, heroY - 100, 920, 60); Text(P + "Goal/Goal", fBold, 40, Ink, TextAlignmentOptions.Center, 26);
        Pin(P + "Goal/Description", TopEdge, 0, heroY - 182, 900, 84); Text(P + "Goal/Description", fMedium, 30, Sub, TextAlignmentOptions.Center, 20);
        // Star rules: three pastel tiles inside a card.
        float rulesY = Center(BodyTop + 500 + 24, 270);
        var rules = R(P + "Star rules/Mastery card"); rules.SetAsFirstSibling(); Pin(rules, TopEdge, 0, rulesY, Width, 270); Card(rules);
        string[] texts = { "Star rules", "Two star requirement", "Three star requirement" }; int[] tones = { Yellow, Peach, Purple };
        for (int i = 0; i < 3; i++)
        {
            float x = -324 + i * 324;
            var tile = NewImage(T(P + "Star rules"), "Rule tile " + (i + 1), sTile, Color.white); Pin(tile.rectTransform, TopEdge, x, rulesY, 300, 232); Tile(tile.rectTransform, tones[i]);
            tile.transform.SetSiblingIndex(rules.GetSiblingIndex() + 1 + i);
            Pin(P + "Star rules/" + texts[i], TopEdge, x, rulesY - 40, 280, 120); Text(P + "Star rules/" + texts[i], fBold, 32, Ink, TextAlignmentOptions.Center, 20);
        }
        string[][] stars = { new[] { "0-0" }, new[] { "1-0", "1-1" }, new[] { "2-0", "2-1", "2-2" } };
        for (int i = 0; i < 3; i++) for (int s = 0; s < stars[i].Length; s++)
            Pin(P + "Star rules/Mastery star " + stars[i][s], TopEdge, -324 + i * 324 + (s - (stars[i].Length - 1) / 2f) * 58, rulesY + 62, 54, 54);
        // Rewards: coin and XP chips.
        float rewardY = Center(BodyTop + 500 + 24 + 270 + 24, 150);
        var reward = R(P + "Reward/Reward background"); reward.SetAsFirstSibling(); Pin(reward, TopEdge, 0, rewardY, Width, 150); Card(reward);
        var coinChip = NewImage(T(P + "Reward"), "Coins chip", sTile, Color.white); Pin(coinChip.rectTransform, TopEdge, -242, rewardY, 468, 112); Tile(coinChip.rectTransform, Peach);
        coinChip.transform.SetSiblingIndex(reward.GetSiblingIndex() + 1);
        var xpChip = NewImage(T(P + "Reward"), "XP chip", sTile, Color.white); Pin(xpChip.rectTransform, TopEdge, 242, rewardY, 468, 112); Tile(xpChip.rectTransform, Purple);
        xpChip.transform.SetSiblingIndex(coinChip.transform.GetSiblingIndex() + 1);
        Pin(P + "Reward/Reward coin", TopEdge, -416, rewardY, 72, 72);
        Pin(P + "Reward/Reward preview", TopEdge, -196, rewardY, 320, 64); Text(P + "Reward/Reward preview", fExtra, 38, Ink, TextAlignmentOptions.MidlineLeft, 24);
        Pin(P + "Reward/Reward XP", TopEdge, 68, rewardY, 80, 80);
        Pin(P + "Reward/XP reward", TopEdge, 292, rewardY, 320, 64); Text(P + "Reward/XP reward", fExtra, 38, Ink, TextAlignmentOptions.MidlineLeft, 24);
        // Bottom: best line and the primary call to action.
        PrimaryPill(Pin(P + "Actions/Start level", BottomEdge, 0, Bottom + 64, Width, 128), "Let's Play", "arrow-right", Primary, Accent);
        Pin(P + "Wallet/Best", BottomEdge, 0, Bottom + 128 + 20 + 22, Width, 44); Text(P + "Wallet/Best", fMedium, 30, Sub, TextAlignmentOptions.Center, 20);
        foreach (string w in new[] { "Wallet coins", "Wallet experience" }) T(P + "Wallet/" + w).gameObject.SetActive(false);
    }

    static void Results()
    {
        const string P = "Screens/Results/Results card/";
        Drop(P + "Frame", P + "Header/Eyebrow plate", P + "Header/Title star left", P + "Header/Title star right");
        Page("Screens/Results", "Results card");
        const float shift = 120 + 24; // no back button: lift the header into its place
        PageHeader(null, P + "Header/Eyebrow", P + "Header/Title", false, shift);
        float bodyTop = BodyTop - shift;
        float scoreY = Center(bodyTop, 470);
        var scoreCard = R(P + "Score/Score plate"); Pin(scoreCard, TopEdge, 0, scoreY, Width, 470); Card(scoreCard);
        R(P + "Star rating").SetSiblingIndex(R(P + "Score").GetSiblingIndex() + 1);
        for (int i = 1; i <= 3; i++) Pin(P + "Star rating/Star " + i, TopEdge, (i - 2) * 190, scoreY + (i == 2 ? 140 : 118), 140, 140);
        Pin(P + "Score/Score caption", TopEdge, 0, scoreY + 6, 600, 40); Text(P + "Score/Score caption", fSemi, 30, Sub).text = "Final score";
        Pin(P + "Score/Score", TopEdge, 0, scoreY - 78, 800, 120); Text(P + "Score/Score", fExtra, 100, Ink, TextAlignmentOptions.Center, 48);
        Pin(P + "Score/Personal best", TopEdge, 0, scoreY - 168, 800, 44); Text(P + "Score/Personal best", fSemi, 30, Primary, TextAlignmentOptions.Center, 20);
        float statsY = Center(bodyTop + 470 + 24, 230);
        var statsCard = R(P + "Stats/Stats plate"); Pin(statsCard, TopEdge, 0, statsY, Width, 230); Card(statsCard);
        Pin(P + "Stats/Stats", TopEdge, 0, statsY, 880, 190); Text(P + "Stats/Stats", fMedium, 34, Ink, TextAlignmentOptions.MidlineLeft, 22);
        float rewardsY = Center(bodyTop + 470 + 24 + 230 + 24, 210);
        var rewards = R(P + "Rewards/Round rewards"); Pin(rewards, TopEdge, 0, rewardsY, Width, 210); Outlined(rewards, Color.white);
        Pin((RectTransform)rewards.Find("Earned"), Mid, 0, 58, 920, 50); Text(rewards.Find("Earned").GetComponent<TMP_Text>(), fBold, 40, Ink);
        Pin((RectTransform)rewards.Find("Balance"), Mid, 0, 10, 920, 40); Text(rewards.Find("Balance").GetComponent<TMP_Text>(), fMedium, 28, Sub);
        Pin((RectTransform)rewards.Find("Player progress"), Mid, 0, -32, 920, 40); Text(rewards.Find("Player progress").GetComponent<TMP_Text>(), fSemi, 28, Ink);
        var xpTrack = Skin(rewards.Find("XP track").GetComponent<Image>(), sPill, Track); Pin(xpTrack.rectTransform, Mid, 0, -74, 880, 20);
        var xpFill = Skin(rewards.Find("XP track/XP progress").GetComponent<Image>(), sPill, Color.white); Gradient(xpFill, Primary, Accent);
        Pin(P + "Rewards/Mastery hint", TopEdge, 0, -(bodyTop + 470 + 24 + 230 + 24 + 210 + 16 + 36), Width, 72); Text(P + "Rewards/Mastery hint", fSemi, 30, Sub, TextAlignmentOptions.Center, 20);
        // Actions: primary row above a dark secondary pill.
        var levels = Pin(P + "Actions/Result level select", BottomEdge, 0, Bottom + 56, Width, 112);
        SolidPill(levels, "Level select", Ink, Color.white, null);
        var actions = Pin(P + "Actions/Result actions", BottomEdge, 0, Bottom + 112 + 16 + 64, Width, 128);
        var layout = actions.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) { layout.spacing = 24; layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = layout.childForceExpandHeight = true; layout.padding = new RectOffset(); }
        foreach (Transform button in actions)
        {
            var r = (RectTransform)button; r.sizeDelta = new Vector2(r.sizeDelta.x, 128);
            bool next = button.name == "Next level";
            var image = Skin(Ensure<Image>(button.gameObject), sPill, Color.white);
            Gradient(image, next ? C(52, 211, 120) : Primary, next ? C(22, 163, 74) : Accent);
            ButtonColors(button.GetComponent<Button>());
            var label = button.GetComponentInChildren<TMP_Text>(true); Fill(label.rectTransform, 24, 24, 16, 16); Text(label, fBold, 44, Color.white, TextAlignmentOptions.Center, 26);
            if (next) label.text = "Next level";
        }
    }

    // ---- Modal cards over the HUD -------------------------------------------------------------
    const float ModalY = -90;
    static RectTransform Modal(string overlay, string card, float w, float h, float dim = .38f)
    {
        T(overlay).GetComponent<Image>().color = new Color(Dim.r, Dim.g, Dim.b, dim);
        var r = Pin(overlay + "/" + card, Mid, 0, ModalY, w, h); Card(r).raycastTarget = true;
        foreach (Transform section in r) if (section.GetComponent<Graphic>() == null && !section.name.EndsWith(" shadow")) Fill((RectTransform)section);
        return r;
    }
    static void Countdown()
    {
        const string P = "Screens/Countdown/Countdown card/";
        Drop(P + "Frame", P + "Header/Countdown star left", P + "Header/Countdown star right", P + "Count/Countdown star middle left",
            P + "Count/Countdown star middle right", P + "Count/Countdown star low");
        Modal("Screens/Countdown", "Countdown card", 900, 760, .3f);
        Pin(P + "Header/Caption", Mid, 0, 280, 800, 100); Text(P + "Header/Caption", fExtra, 76, Ink).text = "Get Ready!";
        var bubble = Skin(P + "Count/Countdown bubble", sCircle, Color.white, false); Pin(bubble.rectTransform, Mid, 0, 20, 300, 300); Gradient(bubble, Primary, Accent, true);
        Pin(P + "Count/Count", Mid, 0, 22, 300, 260); Text(P + "Count/Count", fExtra, 170, Color.white, TextAlignmentOptions.Center, 80);
        var goalPlate = NewImage(T(P + "Goal"), "Goal plate", sPill, Track); goalPlate.transform.SetAsFirstSibling(); Pin(goalPlate.rectTransform, Mid, 0, -268, 820, 104);
        Pin(P + "Goal/Goal", Mid, 0, -268, 760, 76); Text(P + "Goal/Goal", fBold, 34, Ink, TextAlignmentOptions.Center, 22);
    }
    static void Paused()
    {
        const string P = "Screens/Paused/Paused card/";
        Drop(P + "Frame", P + "Header/Pause leaves left", P + "Header/Pause leaves right", P + "Header/Pause star left", P + "Header/Pause star right",
            P + "Header/Eyebrow plate", P + "Header/Detail plate");
        Modal("Screens/Paused", "Paused card", 920, 1160);
        Pin(P + "Header/Title", Mid, 0, 480, 820, 96); Text(P + "Header/Title", fExtra, 76, Ink).text = "Paused";
        Pin(P + "Header/Eyebrow", Mid, 0, 412, 820, 44); Text(P + "Header/Eyebrow", fSemi, 32, Sub).text = "Take a breather";
        Pin(P + "Header/Detail", Mid, 0, 362, 820, 40); Text(P + "Header/Detail", fMedium, 28, Sub, TextAlignmentOptions.Center, 20);
        PrimaryPill(Pin(P + "Actions/Resume", Mid, 0, 250, 840, 128), "Keep playing", "play", Primary, Accent);
        SolidPill(Pin(P + "Actions/Restart", Mid, 0, 102, 840, 112), "Restart level", Color.white, Ink, "restart", outline: true);
        string[] settings = { "Sound", "Motion", "Vibration" }; string[] glyphs = { "sound", "motion", "vibrate" }; int[] tones = { Blue, Purple, Peach };
        for (int i = 0; i < 3; i++)
        {
            var row = Pin(P + "Actions/" + settings[i], Mid, 0, -30 - i * 116, 840, 104);
            var fill = Outlined(row, Color.white); var button = row.GetComponent<Button>(); button.targetGraphic = fill; ButtonColors(button);
            var icon = row.Find("Button icon"); if (icon != null) { var img = Skin(icon.GetComponent<Image>(), sSquircle, Color.white); Pin(img.rectTransform, Mid, -360, 0, 72, 72); Gradient(img, IconGrad[tones[i]][0], IconGrad[tones[i]][1], true); Glyph(icon, glyphs[i], 40, Color.white); }
            var label = row.GetComponentsInChildren<TMP_Text>(true).First(); Pin(label.rectTransform, Mid, 40, 2, 680, 60); Text(label, fSemi, 34, Ink, TextAlignmentOptions.MidlineLeft, 24);
        }
        SolidPill(Pin(P + "Actions/Exit to levels", Mid, 0, -432, 840, 112), "Exit to level select", Ink, Color.white, "arrow-left");
    }
    static void Wallet()
    {
        const string P = "Screens/Wallet information/Wallet card/";
        Drop(P + "Frame", P + "Header/Title star left", P + "Header/Title star right", P + "Header/Eyebrow plate");
        Modal("Screens/Wallet information", "Wallet card", 900, 900, .45f);
        Pin(P + "Header/Wallet title", Mid, 0, 330, 700, 90); Text(P + "Header/Wallet title", fExtra, 70, Ink).text = "Your Wallet";
        Pin(P + "Header/Eyebrow", Mid, 0, 264, 700, 44); Text(P + "Header/Eyebrow", fSemi, 32, Sub).text = "Player profile";
        var plate = Skin(P + "Content/Details plate", sTile, C(243, 241, 253)); Pin(plate.rectTransform, Mid, 0, -40, 800, 500);
        Pin(P + "Content/Wallet details", Mid, 0, -40, 740, 460); Text(P + "Content/Wallet details", fMedium, 36, Ink, TextAlignmentOptions.Center, 22);
        RoundButton(Pin(P + "Actions/Close wallet", Mid, 360, 360, 104, 104), "close", C(240, 240, 250), Ink);
    }

    // Pill corners are 64 sprite px; scale them to half each element's short side so ends stay round.
    static void PillEnds(GameObject canvas)
    {
        foreach (var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite == null || image.sprite.name != "Pill" || image.type != Image.Type.Sliced) continue;
            float shortSide = Mathf.Min(image.rectTransform.rect.width, image.rectTransform.rect.height);
            if (shortSide > 0) image.pixelsPerUnitMultiplier = Mathf.Max(1, 128f / shortSide);
        }
    }

    // Any label not styled above still moves to Poppins (SemiBold) so the canvas uses one family.
    static void SweepFonts()
    {
        int moved = 0;
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.font != null && label.font.name.StartsWith("Poppins")) continue;
            label.font = fSemi; label.fontSharedMaterial = fSemi.material; label.extraPadding = false; moved++;
        }
        log.AppendLine("Moved " + moved + " remaining labels to Poppins SemiBold.");
    }

    // Follow-up fixes applied to both the prefab template and the unpacked scene copy:
    // a fainter, smaller background motif and a wallet card matching the Paused card it covers.
    static void PolishCanvas(GameObject canvas)
    {
        var motif = (RectTransform)canvas.transform.Find("Background/Motif");
        Pin(motif, new Vector2(1, 1), -250, -215, 560, 420); motif.GetComponent<Image>().color = new Color(1, 1, 1, .5f);
        var screens = canvas.transform.Find("Device safe area/Portrait frame/Screens/Wallet information");
        var card = (RectTransform)screens.Find("Wallet card");
        Pin(card, Mid, 0, ModalY, 920, 1160);
        var shadow = (RectTransform)screens.Find("Wallet card shadow"); Pin(shadow, Mid, 0, ModalY - 14, 920 + 96, 1160 + 96);
        Pin((RectTransform)card.Find("Header/Wallet title"), Mid, 0, 470, 700, 96);
        Pin((RectTransform)card.Find("Header/Eyebrow"), Mid, 0, 402, 700, 44);
        Pin((RectTransform)card.Find("Content/Details plate"), Mid, 0, -40, 820, 680);
        Pin((RectTransform)card.Find("Content/Wallet details"), Mid, 0, -40, 760, 620);
        Pin((RectTransform)card.Find("Actions/Close wallet"), Mid, 370, 490, 104, 104);
    }
    public static void Polish()
    {
        try
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasPath);
            PolishCanvas(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, CanvasPath); PrefabUtility.UnloadPrefabContents(prefab);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
            var ui = scene.GetRootGameObjects().First(g => g.name == "Tap Blast UI");
            if (PrefabUtility.IsPartOfPrefabInstance(ui)) throw new Exception("Expected the scene UI to be unpacked.");
            PolishCanvas(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("modern-ui-authoring.txt", "PASS Polish applied to prefab and unpacked scene.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("modern-ui-authoring.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    // ---- Objective card: rating stars are a separate indicator, not milestones on the bar ----
    static void ApplyObjective(GameObject canvas)
    {
        // Card 1000x132 (16 px below the stats card): label row with compact rating stars, then a slim progress line.
        var objective = canvas.transform.Find("Device safe area/Portrait frame/HUD/Objective");
        var labelSection = objective.Find("Objective label"); var track = objective.Find("Objective track");
        const float y = -458;
        Pin((RectTransform)objective.Find("Objective card"), TopEdge, 0, y, Width, 132);
        var shadow = objective.Find("Objective card shadow"); if (shadow != null) Pin((RectTransform)shadow, TopEdge, 0, y - 14, Width + 96, 132 + 96);
        var label = Pin((RectTransform)labelSection.Find("Objective"), TopEdge, -130, y + 30, 680, 40).GetComponent<TMP_Text>();
        label.fontSize = label.fontSizeMax = 28; label.fontSizeMin = 20;
        for (int i = 1; i <= 3; i++)
        {
            var star = (RectTransform)(track.Find("Objective star " + i) ?? labelSection.Find("Objective star " + i));
            star.SetParent(labelSection, false); star.SetAsLastSibling();
            Pin(star, TopEdge, 372 + (i - 1) * 42, y + 30, 36, 36);
            star.GetComponent<Image>().color = UIArt.Unearned; // Runtime tints earned stars white.
        }
        track.GetComponent<Image>().color = C(224, 220, 246);
        Pin((RectTransform)track, TopEdge, 0, y - 28, 920, 26);
        Pin((RectTransform)track.Find("Progress"), Mid, 0, 0, 912, 20);
    }
    public static void Objective()
    {
        try
        {
            var prefab = PrefabUtility.LoadPrefabContents(CanvasPath);
            ApplyObjective(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, CanvasPath); PrefabUtility.UnloadPrefabContents(prefab);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
            ApplyObjective(scene.GetRootGameObjects().First(g => g.name == "Tap Blast UI"));
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            File.WriteAllText("modern-ui-authoring.txt", "PASS Objective card applied to prefab and unpacked scene.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("modern-ui-authoring.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    // ---- Backgrounds pass: richer page backdrop, clean per-theme arena backdrops, lighter scrims ----
    static float Hash(int x, int y) { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h & 0xffff) / 65535f; }
    // Pastel arena: vertical gradient, soft centre glow, light bokeh and a faint dot grid.
    static Sprite ArenaBackdrop(string name, Color top, Color bottom, Color bokeh, Color dots)
    {
        const int w = 512, h = 768;
        var bubbles = new List<(float x, float y, float r, float a)>();
        for (int i = 0; i < 14; i++) bubbles.Add((Hash(i, 1) * w, Hash(i, 2) * h, 30 + Hash(i, 3) * 90, .18f + Hash(i, 4) * .22f));
        return Bake(name, w, h, (x, y) =>
        {
            Color c = Color.Lerp(top, bottom, y / h);
            float glow = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(Len(x - w / 2f, y - h * .38f) / (w * .62f)));
            c = Color.Lerp(c, Color.white, glow * .12f);
            foreach (var b in bubbles) { float a = (1 - Mathf.SmoothStep(.55f, 1, Len(x - b.x, y - b.y) / b.r)) * b.a * bokeh.a; c = Color.Lerp(c, bokeh, a); }
            float gx = Mathf.Repeat(x, 28) - 14, gy = Mathf.Repeat(y, 28) - 14; float dot = Aa(Len(gx, gy) - 1.6f);
            c = Color.Lerp(c, dots, dot * dots.a); c.a = 1; return c;
        }, Vector4.zero);
    }
    static Sprite arenaSky, arenaMint, arenaPeach;
    static void BakeBackgrounds()
    {
        Color top = C(216, 224, 255), mid = C(236, 231, 253), low = C(247, 248, 253);
        sBackground = Bake("Background", 8, 512, (x, y) => y < 190 ? Color.Lerp(top, mid, y / 190) : Color.Lerp(mid, low, Mathf.SmoothStep(0, 1, (y - 190) / 322)), Vector4.zero);
        // Deep, saturated stages so white sparks, coloured bursts and white point labels stay visible.
        arenaSky = ArenaBackdrop("Arena-Sky", C(58, 70, 168), C(104, 66, 178), C(255, 255, 255, .55f), C(255, 255, 255, .1f));
        arenaMint = ArenaBackdrop("Arena-Mint", C(22, 104, 122), C(44, 78, 152), C(255, 255, 255, .55f), C(255, 255, 255, .1f));
        arenaPeach = ArenaBackdrop("Arena-Peach", C(150, 62, 112), C(88, 58, 162), C(255, 255, 255, .55f), C(255, 255, 255, .1f));
        sGlow = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "Glow.png");
        sShadow = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "CardShadow.png");
    }
    static void ApplyBackgrounds(GameObject canvas)
    {
        var bg = canvas.transform.Find("Background");
        var image = bg.GetComponent<Image>(); image.sprite = sBackground; image.color = Color.white;
        Pin(bg.Find("Glow blue").GetComponent<RectTransform>(), new Vector2(0, 1), 60, -120, 1200, 1200); bg.Find("Glow blue").GetComponent<Image>().color = C(150, 182, 255, .55f);
        Pin(bg.Find("Glow pink").GetComponent<RectTransform>(), new Vector2(1, 1), -40, -360, 1000, 1000); bg.Find("Glow pink").GetComponent<Image>().color = C(250, 186, 228, .5f);
        var mint = NewImage(bg, "Glow mint", sGlow, C(170, 232, 214, .4f), false); Pin(mint.rectTransform, new Vector2(0, 0), 120, 260, 1000, 1000);
        mint.transform.SetSiblingIndex(bg.Find("Motif").GetSiblingIndex());
        var theme = new SerializedObject(canvas.GetComponent<GuiArenaTheme>());
        theme.FindProperty("city").objectReferenceValue = arenaSky; theme.FindProperty("valley").objectReferenceValue = arenaMint;
        theme.FindProperty("waterfall").objectReferenceValue = arenaPeach; theme.FindProperty("sceneryTint").colorValue = Color.white;
        theme.ApplyModifiedPropertiesWithoutUndo();
        var portrait = canvas.transform.Find("Device safe area/Portrait frame");
        var scenery = portrait.Find("HUD/Arena/Target arena/Arena scenery").GetComponent<Image>(); scenery.sprite = arenaSky; scenery.color = Color.white;
        // Lighter, cooler scrims keep the pastel HUD clean behind modal cards.
        foreach (var (overlay, alpha) in new[] { ("Countdown", .22f), ("Paused", .26f), ("Wallet information", .32f) })
            portrait.Find("Screens/" + overlay).GetComponent<Image>().color = C(40, 44, 110, alpha);
        foreach (var shadow in portrait.GetComponentsInChildren<Image>(true).Where(i => i.name.EndsWith(" shadow")))
            shadow.color = C(80, 70, 190, shadow.name.StartsWith("Countdown card") || shadow.name.StartsWith("Paused card") || shadow.name.StartsWith("Wallet card") ? .38f : .26f);
    }
    public static void Backgrounds()
    {
        try
        {
            AssetDatabase.Refresh(); BakeBackgrounds();
            var prefab = PrefabUtility.LoadPrefabContents(CanvasPath);
            ApplyBackgrounds(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, CanvasPath); PrefabUtility.UnloadPrefabContents(prefab);
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
            var ui = scene.GetRootGameObjects().First(g => g.name == "Tap Blast UI");
            ApplyBackgrounds(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("modern-ui-authoring.txt", "PASS Backgrounds applied to prefab and unpacked scene.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("modern-ui-authoring.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    // Target labels (GOLD, +3s, CORE, LINK, BOMB) move to Poppins ExtraBold, keeping their colours.
    public static void RunTargetFonts()
    {
        try
        {
            const string path = "Assets/Prefabs/Targets/NormalTarget.prefab";
            fExtra = Font("ExtraBold");
            var target = PrefabUtility.LoadPrefabContents(path); int labels = 0;
            foreach (var label in target.GetComponentsInChildren<TMP_Text>(true)) { label.font = fExtra; label.fontSharedMaterial = fExtra.material; label.extraPadding = false; labels++; }
            PrefabUtility.SaveAsPrefabAsset(target, path); PrefabUtility.UnloadPrefabContents(target); AssetDatabase.SaveAssets();
            File.WriteAllText("modern-ui-authoring.txt", "PASS " + labels + " target labels use Poppins ExtraBold.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("modern-ui-authoring.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }

    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();
            BakeArt();
            fExtra = Font("ExtraBold"); fBold = Font("Bold"); fSemi = Font("SemiBold"); fMedium = Font("Medium");
            root = PrefabUtility.LoadPrefabContents(CanvasPath);
            frame = root.transform.Find("Device safe area/Portrait frame");
            if (frame.Find("HUD") == null) throw new Exception("Run LayoutAuthor first: the modern skin expects the HUD / Screens hierarchy.");
            var fitting = new SerializedObject(root.GetComponent<ResponsiveCanvas>());
            fitting.FindProperty("respectSafeArea").boolValue = false; fitting.FindProperty("fillScreen").boolValue = true; fitting.ApplyModifiedPropertiesWithoutUndo();
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Fill((RectTransform)frame); Fill((RectTransform)frame.parent);
            Background(); Hud(); LevelSelect(); Briefing(); Results(); Countdown(); Paused(); Wallet(); SweepFonts(); PillEnds(root);
            log.AppendLine("Removed " + dropped + " unreferenced decorations; hid " + hidden + " referenced ones.");
            PrefabUtility.SaveAsPrefabAsset(root, CanvasPath); PrefabUtility.UnloadPrefabContents(root); AssetDatabase.SaveAssets();
            File.WriteAllText("modern-ui-authoring.txt", "PASS Modern UI applied.\n" + log);
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); File.WriteAllText("modern-ui-authoring.txt", "FAIL " + e + "\n" + log); EditorApplication.Exit(1); }
    }
}
