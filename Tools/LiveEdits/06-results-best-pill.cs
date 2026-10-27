// Live edit: results page (outcome chip, score card filling the free height with a gradient band,
// personal-best chip, readable stats and rewards) and the briefing best pill (trophy + 3 stars).
System.Func<byte, byte, byte, UnityEngine.Color> C = (r, g, b) => new UnityEngine.Color32(r, g, b, 255);
System.Func<string, UnityEngine.Sprite> art = n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/" + n + ".png");
var pill = art("Pill"); var tile = art("Tile"); var squircle = art("Squircle"); var trophy = art("Icon-Trophy");
var top = new UnityEngine.Vector2(.5f, 1); var bottom = new UnityEngine.Vector2(.5f, 0); var mid = new UnityEngine.Vector2(.5f, .5f);
System.Action<UnityEngine.RectTransform, UnityEngine.Vector2, float, float, float, float> pin = (r, a, x, y, w, h) =>
{
    r.anchorMin = r.anchorMax = a; r.pivot = mid; r.anchoredPosition = new UnityEngine.Vector2(x, y);
    r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
};
System.Action<UnityEngine.RectTransform, float, float, float> column = (r, w, t, b) =>
{
    r.anchorMin = new UnityEngine.Vector2(.5f, 0); r.anchorMax = new UnityEngine.Vector2(.5f, 1); r.pivot = mid; r.localScale = UnityEngine.Vector3.one;
    r.sizeDelta = new UnityEngine.Vector2(w, -(t + b)); r.anchoredPosition = new UnityEngine.Vector2(0, (b - t) / 2);
};
System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, bool, UnityEngine.UI.Image> image = (parent, name, sprite, sliced) =>
{
    var found = parent.Find(name);
    var go = found != null ? found.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    go.transform.SetParent(parent, false);
    var img = go.GetComponent<UnityEngine.UI.Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
    img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; img.color = UnityEngine.Color.white; return img;
};
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
System.Func<UnityEngine.Transform, string, TMPro.TMP_Text> text = (root, path) => root.Find(path).GetComponent<TMPro.TMP_Text>();
System.Action<TMPro.TMP_Text, float, float> size = (t, max, min) => { t.fontSize = t.fontSizeMax = max; t.fontSizeMin = min; };

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var page = canvas.transform.Find("Device safe area/Portrait frame/Screens/Results/Results card");
    // Header: outcome chip, then the title.
    var outcome = image(page.Find("Header"), "Outcome chip", pill, true); outcome.transform.SetAsFirstSibling();
    pin(outcome.rectTransform, top, -230, -66, 540, 64); outcome.pixelsPerUnitMultiplier = 2;
    var eyebrow = (UnityEngine.RectTransform)page.Find("Header/Eyebrow"); pin(eyebrow, top, -230, -66, 500, 44);
    var eyebrowText = eyebrow.GetComponent<TMPro.TMP_Text>(); size(eyebrowText, 28, 20); eyebrowText.alignment = TMPro.TextAlignmentOptions.Center;
    pin((UnityEngine.RectTransform)page.Find("Header/Title"), top, 0, -166, 1000, 104);
    data.FindProperty("resultOutcomeChip").objectReferenceValue = outcome;
    // Bottom stack (from the screen bottom): level select 96, actions 232, hint 346, rewards 501, stats 745.
    pin((UnityEngine.RectTransform)page.Find("Rewards/Mastery hint"), bottom, 0, 346, 1000, 60);
    var rewards = (UnityEngine.RectTransform)page.Find("Rewards/Round rewards"); pin(rewards, bottom, 0, 501, 1000, 210);
    rewards.GetComponent<UnityEngine.UI.Image>().color = C(220, 214, 250); rewards.Find("Fill").GetComponent<UnityEngine.UI.Image>().color = C(250, 247, 255);
    var stats = (UnityEngine.RectTransform)page.Find("Stats/Stats plate"); pin(stats, bottom, 0, 745, 1000, 230);
    var statsShadow = page.Find("Stats/Stats plate shadow"); if (statsShadow != null) pin((UnityEngine.RectTransform)statsShadow, bottom, 0, 731, 1096, 326);
    pin((UnityEngine.RectTransform)page.Find("Stats/Stats"), bottom, 0, 745, 880, 190);
    // Score card fills the free height; stars sit on a soft gradient band in its upper half.
    var score = (UnityEngine.RectTransform)page.Find("Score/Score plate"); column(score, 1000, 232, 884);
    var scoreShadow = page.Find("Score/Score plate shadow"); if (scoreShadow != null) column((UnityEngine.RectTransform)scoreShadow, 1096, 232 - 48 + 14, 884 - 48 - 14);
    var band = image(score, "Band", tile, true); band.transform.SetAsFirstSibling();
    var br = band.rectTransform; br.anchorMin = new UnityEngine.Vector2(0, .46f); br.anchorMax = UnityEngine.Vector2.one; br.pivot = mid;
    br.offsetMin = new UnityEngine.Vector2(16, 0); br.offsetMax = new UnityEngine.Vector2(-16, -16);
    gradient(band, C(226, 232, 255), C(250, 226, 244), false);
    for (int i = 1; i <= 3; i++)
    {
        var star = (UnityEngine.RectTransform)(page.Find("Star rating/Star " + i) ?? score.Find("Star " + i));
        star.SetParent(score, false); pin(star, mid, (i - 2) * 200, i == 2 ? 196 : 172, 160, 160);
    }
    foreach (var n in new[] { "Score caption", "Score", "Personal best" })
    { var t = page.Find("Score/" + n) ?? score.Find(n); t.SetParent(score, false); }
    pin((UnityEngine.RectTransform)score.Find("Score caption"), mid, 0, 34, 600, 40);
    pin((UnityEngine.RectTransform)score.Find("Score"), mid, 0, -56, 800, 124); size(score.Find("Score").GetComponent<TMPro.TMP_Text>(), 110, 48);
    var bestChip = image(score, "Best chip", pill, true); pin(bestChip.rectTransform, mid, 0, -160, 520, 64); bestChip.pixelsPerUnitMultiplier = 2;
    bestChip.transform.SetSiblingIndex(score.Find("Personal best").GetSiblingIndex());
    pin((UnityEngine.RectTransform)score.Find("Personal best"), mid, 0, -160, 480, 46);
    size(score.Find("Personal best").GetComponent<TMPro.TMP_Text>(), 28, 20);
    data.FindProperty("resultBestChip").objectReferenceValue = bestChip;

    // Briefing best pill: trophy tile, best score, three star icons for the earned rating.
    var brief = canvas.transform.Find("Device safe area/Portrait frame/Screens/Level briefing/Level briefing card/Wallet");
    const float BestY = 224;
    var chip = brief.Find("Best chip").GetComponent<UnityEngine.UI.Image>(); pin(chip.rectTransform, bottom, 0, BestY, 760, 84);
    chip.pixelsPerUnitMultiplier = 128f / 84; chip.color = UnityEngine.Color.white;
    var icon = image(brief, "Best icon", squircle, true); pin(icon.rectTransform, bottom, -326, BestY, 60, 60);
    icon.transform.SetSiblingIndex(chip.transform.GetSiblingIndex() + 1);
    gradient(icon, C(255, 181, 71), C(255, 138, 31), true);
    var glyph = image(icon.transform, "Glyph", trophy, false); pin(glyph.rectTransform, mid, 0, 0, 34, 34);
    var best = (UnityEngine.RectTransform)brief.Find("Best"); pin(best, bottom, -46, BestY, 470, 56);
    var bestText = best.GetComponent<TMPro.TMP_Text>(); bestText.alignment = TMPro.TextAlignmentOptions.MidlineLeft; size(bestText, 30, 20);
    var starSprite = page.parent.parent.Find("Level select/Level select card/Level list/Level list viewport/Level list content/Select Level 1/Earned star 1").GetComponent<UnityEngine.UI.Image>().sprite;
    var stars = data.FindProperty("introBestStars"); stars.arraySize = 3;
    for (int i = 0; i < 3; i++)
    {
        var s = image(brief, "Best star " + (i + 1), starSprite, false); pin(s.rectTransform, bottom, 232 + i * 46, BestY, 42, 42);
        s.color = new UnityEngine.Color(1, 1, 1, .28f); stars.GetArrayElementAtIndex(i).objectReferenceValue = s;
    }
    data.ApplyModifiedPropertiesWithoutUndo();
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
return "PASS results page and briefing best pill updated in scene (saved) and prefab";
