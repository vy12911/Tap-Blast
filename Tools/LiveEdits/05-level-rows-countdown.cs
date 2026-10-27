// Live edit: colourful level rows (pastel tile per badge colour) and a lively countdown card
// (indigo stage, glowing white bubble with a gradient number).
System.Func<byte, byte, byte, UnityEngine.Color> C = (r, g, b) => new UnityEngine.Color32(r, g, b, 255);
var glow = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Glow.png");
var ringSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Ring.png");
var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Tile.png");
var mid = new UnityEngine.Vector2(.5f, .5f);
System.Action<UnityEngine.RectTransform, float, float, float, float> pin = (r, x, y, w, h) =>
{
    r.anchorMin = r.anchorMax = r.pivot = mid; r.anchoredPosition = new UnityEngine.Vector2(x, y);
    r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
};
System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, UnityEngine.UI.Image> image = (parent, name, sprite) =>
{
    var found = parent.Find(name);
    var go = found != null ? found.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    go.transform.SetParent(parent, false);
    var img = go.GetComponent<UnityEngine.UI.Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
    img.type = UnityEngine.UI.Image.Type.Simple; img.color = UnityEngine.Color.white; return img;
};
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
// Badge cycle: blue, green, purple, peach, pink — row tiles use the matching pastel fill and edge.
UnityEngine.Color[] fills = { C(226, 238, 255), C(225, 246, 231), C(238, 231, 255), C(255, 238, 222), C(255, 231, 238) };
UnityEngine.Color[] edges = { C(176, 205, 245), C(168, 222, 183), C(201, 184, 245), C(243, 200, 160), C(242, 180, 198) };

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>();
    var rows = new UnityEditor.SerializedObject(hud).FindProperty("levelButtons");
    for (int i = 0; i < rows.arraySize; i++)
    {
        var row = ((UnityEngine.UI.Button)rows.GetArrayElementAtIndex(i).objectReferenceValue).transform;
        row.GetComponent<UnityEngine.UI.Image>().color = edges[i % 5];
        var fill = row.Find("Fill").GetComponent<UnityEngine.UI.Image>(); fill.color = fills[i % 5];
        var title = row.Find("Label").GetComponent<TMPro.TMP_Text>(); title.fontSize = title.fontSizeMax = 38;
    }
    // Countdown: the header band becomes an indigo stage holding the title and the number.
    var card = canvas.transform.Find("Device safe area/Portrait frame/Screens/Countdown/Countdown card");
    var stage = card.Find("Header band").GetComponent<UnityEngine.UI.Image>(); stage.sprite = tile; stage.type = UnityEngine.UI.Image.Type.Sliced;
    pin(stage.rectTransform, 0, 64, 868, 600); gradient(stage, C(58, 70, 168), C(104, 66, 178), true);
    var caption = card.Find("Header/Caption").GetComponent<TMPro.TMP_Text>(); caption.color = UnityEngine.Color.white;
    pin(caption.rectTransform, 0, 286, 800, 96);
    var countSection = card.Find("Count");
    var halo = image(countSection, "Halo", glow); halo.color = new UnityEngine.Color(1, 1, 1, .34f); pin(halo.rectTransform, 0, 20, 560, 560); halo.transform.SetAsFirstSibling();
    var ring = image(countSection, "Ring", ringSprite); ring.color = new UnityEngine.Color(1, 1, 1, .55f); pin(ring.rectTransform, 0, 20, 350, 350); ring.transform.SetSiblingIndex(1);
    var bubble = countSection.Find("Countdown bubble").GetComponent<UnityEngine.UI.Image>();
    var bubbleGradient = bubble.GetComponent<TapBlast.UIGradient>(); if (bubbleGradient != null) UnityEngine.Object.DestroyImmediate(bubbleGradient);
    bubble.color = UnityEngine.Color.white; pin(bubble.rectTransform, 0, 20, 280, 280);
    var count = countSection.Find("Count").GetComponent<TMPro.TMP_Text>();
    count.color = UnityEngine.Color.white; count.enableVertexGradient = true;
    count.colorGradient = new TMPro.VertexGradient(C(79, 124, 255), C(79, 124, 255), C(155, 107, 255), C(155, 107, 255));
    pin(count.rectTransform, 0, 22, 280, 240);
    pin(card.Find("Goal/Goal plate").GetComponent<UnityEngine.RectTransform>(), 0, -306, 820, 96);
    pin(card.Find("Goal/Goal").GetComponent<UnityEngine.RectTransform>(), 0, -306, 760, 72);
    var cardRect = (UnityEngine.RectTransform)card; cardRect.sizeDelta = new UnityEngine.Vector2(900, 800);
    var shadow = card.parent.Find("Countdown card shadow"); if (shadow != null) ((UnityEngine.RectTransform)shadow).sizeDelta = new UnityEngine.Vector2(996, 896);
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
return "PASS level rows and countdown updated in scene (saved) and prefab";
