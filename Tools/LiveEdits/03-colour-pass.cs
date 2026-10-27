// Live edit: soft colour for modal cards, settings rows, wallet panel, combo and hint cards.
System.Func<byte, byte, byte, UnityEngine.Color> C = (r, g, b) => new UnityEngine.Color32(r, g, b, 255);
var card = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Card.png");
var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Tile.png");
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, vertical) =>
{
    var effect = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>();
    effect.Set(a, b, vertical);
};
System.Func<UnityEngine.Transform, string, UnityEngine.UI.Image> band = (parent, name) =>
{
    var existing = parent.Find(name);
    var go = existing != null ? existing.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    go.transform.SetParent(parent, false); go.transform.SetAsFirstSibling();
    var image = go.GetComponent<UnityEngine.UI.Image>(); image.sprite = card; image.type = UnityEngine.UI.Image.Type.Sliced;
    image.preserveAspect = true; image.raycastTarget = false; image.color = UnityEngine.Color.white; return image;
};
System.Action<UnityEngine.RectTransform, float, float, float, float> pin = (r, x, y, w, h) =>
{
    r.anchorMin = r.anchorMax = r.pivot = new UnityEngine.Vector2(.5f, .5f); r.anchoredPosition = new UnityEngine.Vector2(x, y);
    r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
};
var headerFrom = C(222, 230, 255); var headerTo = C(250, 226, 244);

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var screens = canvas.transform.Find("Device safe area/Portrait frame/Screens");
    var hud = canvas.transform.Find("Device safe area/Portrait frame/HUD");
    // Header bands behind the titles of the modal cards.
    var paused = screens.Find("Paused/Paused card"); var pb = band(paused, "Header band"); pin(pb.rectTransform, 0, 444, 888, 240); gradient(pb, headerFrom, headerTo, false);
    var wallet = screens.Find("Wallet information/Wallet card"); var wb = band(wallet, "Header band"); pin(wb.rectTransform, 0, 444, 888, 240); gradient(wb, headerFrom, headerTo, false);
    var count = screens.Find("Countdown/Countdown card"); var cb = band(count, "Header band"); pin(cb.rectTransform, 0, 274, 868, 180); gradient(cb, headerFrom, headerTo, false);
    // Settings rows tinted to match their icon; Restart gets a light lavender fill.
    string[] rows = { "Sound", "Motion", "Vibration" };
    UnityEngine.Color[] fills = { C(233, 242, 255), C(241, 235, 255), C(255, 241, 229) };
    UnityEngine.Color[] edges = { C(188, 212, 246), C(208, 194, 246), C(243, 205, 168) };
    for (int i = 0; i < 3; i++)
    {
        var row = paused.Find("Actions/" + rows[i]);
        row.GetComponent<UnityEngine.UI.Image>().color = edges[i];
        row.Find("Fill").GetComponent<UnityEngine.UI.Image>().color = fills[i];
    }
    var restart = paused.Find("Actions/Restart");
    restart.GetComponent<UnityEngine.UI.Image>().color = C(206, 200, 244); restart.Find("Fill").GetComponent<UnityEngine.UI.Image>().color = C(244, 242, 255);
    // Wallet details: warm-to-cool pastel panel, larger readable text.
    var plate = wallet.Find("Content/Details plate").GetComponent<UnityEngine.UI.Image>(); plate.sprite = tile; plate.color = UnityEngine.Color.white;
    gradient(plate, C(255, 238, 222), C(236, 228, 255), true);
    var details = wallet.Find("Content/Wallet details").GetComponent<TMPro.TMP_Text>(); details.fontSizeMax = details.fontSize = 40; details.lineSpacing = -4;
    // Countdown goal chip in soft lavender.
    var goalPlate = count.Find("Goal/Goal plate").GetComponent<UnityEngine.UI.Image>(); goalPlate.color = C(238, 234, 255);
    // Combo card: cool-to-warm gradient; hint card: faint lavender fill.
    var combo = hud.Find("Combo card").GetComponent<UnityEngine.UI.Image>(); gradient(combo, C(236, 241, 255), C(255, 240, 230), false);
    var hintFill = hud.Find("Tutorial hint/Tutorial hint plate/Fill"); if (hintFill != null) hintFill.GetComponent<UnityEngine.UI.Image>().color = C(247, 245, 255);
};

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
apply(ui);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS colour pass applied to scene (saved) and prefab";
