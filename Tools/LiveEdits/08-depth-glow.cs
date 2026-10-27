// Live edit: depth & glow pass across the whole UI (idempotent).
//  - Pastel tiles (sprite "Tile" + child "Fill"): top-lit gradient fill, glossy top shine, soft outer glow in their edge colour.
//  - Gradient icon tiles / number badges (sprite "Squircle" + UIGradient): glossy top shine.
//  - Gold items (rule stars, coins, trophy, XP): warm halo. Primary gradient buttons: coloured glow beneath.
//  - Briefing best pill: warm gold-to-rose gradient with a soft gold glow.
System.Func<string, UnityEngine.Sprite> art = n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/" + n + ".png");
var tileSprite = art("Tile"); var squircle = art("Squircle"); var pillSprite = art("Pill"); var shadowSprite = art("CardShadow"); var glowSprite = art("Glow");
var mid = new UnityEngine.Vector2(.5f, .5f);
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);
int tiles = 0, shines = 0, halos = 0, glows = 0;

System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, bool, UnityEngine.UI.Image> image = (parent, name, sprite, sliced) =>
{
    var found = parent.Find(name);
    var go = found != null ? found.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    go.transform.SetParent(parent, false);
    var img = go.GetComponent<UnityEngine.UI.Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
    img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; return img;
};
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
// A sibling drawn just before `host`, matching its anchors, grown by `pad` and nudged down by `drop`.
System.Func<UnityEngine.RectTransform, string, UnityEngine.Sprite, bool, UnityEngine.Color, float, float, UnityEngine.UI.Image> behind = (host, suffix, sprite, sliced, color, pad, drop) =>
{
    var img = image(host.parent, host.name + " " + suffix, sprite, sliced); img.color = color;
    var r = img.rectTransform; r.SetSiblingIndex(host.GetSiblingIndex());
    r.anchorMin = host.anchorMin; r.anchorMax = host.anchorMax; r.pivot = host.pivot; r.localScale = UnityEngine.Vector3.one;
    r.sizeDelta = host.sizeDelta + new UnityEngine.Vector2(pad * 2, pad * 2); r.anchoredPosition = host.anchoredPosition + new UnityEngine.Vector2(0, -drop);
    return img;
};
// Glossy highlight across the top part of a shape, under its content.
System.Action<UnityEngine.Transform, UnityEngine.Sprite, float, float, int> shine = (host, sprite, inset, alpha, index) =>
{
    var img = image(host, "Shine", sprite, true); img.color = UnityEngine.Color.white;
    var r = img.rectTransform; r.anchorMin = new UnityEngine.Vector2(0, .5f); r.anchorMax = UnityEngine.Vector2.one; r.pivot = mid;
    r.offsetMin = new UnityEngine.Vector2(inset, 0); r.offsetMax = new UnityEngine.Vector2(-inset, -inset);
    gradient(img, new UnityEngine.Color(1, 1, 1, alpha), new UnityEngine.Color(1, 1, 1, 0), true);
    r.SetSiblingIndex(index); shines++;
};

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var portrait = canvas.transform.Find("Device safe area/Portrait frame");
    var images = portrait.GetComponentsInChildren<UnityEngine.UI.Image>(true);
    foreach (var host in images)
    {
        if (host == null || host.sprite == null) continue;
        var t = host.transform; var name = host.name;
        // Pastel tiles: host colour = edge, child Fill = face.
        if (host.sprite == tileSprite && t.Find("Fill") != null && name != "Tutorial hint plate" && name != "Round rewards" && !name.EndsWith(" glow"))
        {
            var fill = t.Find("Fill").GetComponent<UnityEngine.UI.Image>();
            if (fill.GetComponent<TapBlast.UIGradient>() == null)
            {
                var face = fill.color; fill.color = UnityEngine.Color.white;
                gradient(fill, UnityEngine.Color.Lerp(face, UnityEngine.Color.white, .6f), face, true);
            }
            shine(t, tileSprite, 6, .55f, 1);
            bool row = name.StartsWith("Select Level") || name == "Sound" || name == "Motion" || name == "Vibration" || name == "Restart";
            var edge = host.color; edge.a = row ? .22f : .4f;
            behind((UnityEngine.RectTransform)t, "glow", shadowSprite, true, edge, row ? 30 : 40, row ? 8 : 10);
            tiles++; glows++;
        }
        // Gradient squircles: icon tiles and level-number badges.
        else if (host.sprite == squircle && host.GetComponent<TapBlast.UIGradient>() != null && name != "Shine")
            shine(t, squircle, 4, .45f, 0);
    }
    // Warm halos behind gold items and the XP badge.
    var gold = C(255, 196, 60, .5f); var blue = C(100, 150, 255, .45f);
    string[][] haloTargets = {
        new[] { "Screens/Level briefing/Level briefing card/Reward/Reward coin", "g" }, new[] { "Screens/Level briefing/Level briefing card/Reward/Reward XP", "b" },
        new[] { "Screens/Level select/Level select card/Wallet/Selection coins left", "g" }, new[] { "Screens/Level select/Level select card/Wallet/Selection XP icon", "b" },
        new[] { "HUD/Header/Wallet shortcut/Coin", "g" }, new[] { "Screens/Level briefing/Level briefing card/Wallet/Best icon", "g" } };
    foreach (var h in haloTargets)
    {
        var t = (UnityEngine.RectTransform)portrait.Find(h[0]); if (t == null) continue;
        behind(t, "halo", glowSprite, false, h[1] == "g" ? gold : blue, t.sizeDelta.x * .45f, 0); halos++;
    }
    foreach (var star in portrait.Find("Screens/Level briefing/Level briefing card/Star rules").GetComponentsInChildren<UnityEngine.UI.Image>(true))
        if (star.name.StartsWith("Mastery star") && !star.name.EndsWith("halo")) { behind((UnityEngine.RectTransform)star.transform, "halo", glowSprite, false, gold, 22, 0); halos++; }
    // Primary gradient buttons glow in their own colour.
    var primaryGlow = C(91, 108, 255, .45f);
    foreach (var path in new[] { "Screens/Level briefing/Level briefing card/Actions/Start level", "Screens/Paused/Paused card/Actions/Resume", "Screens/Results/Results card/Actions/Result actions" })
    {
        var t = (UnityEngine.RectTransform)portrait.Find(path); if (t == null) continue;
        behind(t, "glow", shadowSprite, true, primaryGlow, 34, 14); glows++;
    }
    // Briefing best pill: warm gradient + gold glow.
    var bestChip = portrait.Find("Screens/Level briefing/Level briefing card/Wallet/Best chip").GetComponent<UnityEngine.UI.Image>();
    bestChip.color = UnityEngine.Color.white; gradient(bestChip, C(255, 244, 214, 1), C(255, 230, 239, 1), false);
    behind(bestChip.rectTransform, "glow", shadowSprite, true, C(255, 186, 80, .32f), 30, 8);
};

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
apply(ui);
string summary = tiles + " tiles, " + shines + " shines, " + halos + " halos, " + glows + " glows";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS depth pass on scene (saved) and prefab: " + summary;
