// Live edit: richer Results page and Wallet panel.
// Results: deep indigo-violet star stage with a slowly turning sunburst and gold glow (stage is its own nested
// Canvas + RectMask2D, so the spin re-batches 3 graphics only), gradient score, trophy best chip, three coloured
// stat tiles, coin / XP reward chips, and a tinted hint chip. Wallet: gradient header, coin and XP tiles, level bar.
const string Modern = "Assets/Art/UI/Modern/", Illustrated = "Assets/Art/GUI/03-Logo-and-Icons/Illustrated/";
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);
// Soft sunburst sprite: 16 tapered rays fading out before the edge, so it can spin inside any stage.
if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Modern + "Rays.png") == null)
{
    const int N = 512; var tex = new UnityEngine.Texture2D(N, N, UnityEngine.TextureFormat.RGBA32, false); var px = new UnityEngine.Color[N * N];
    for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
    {
        float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, r = UnityEngine.Mathf.Sqrt(x * x + y * y), a = UnityEngine.Mathf.Atan2(y, x);
        float ray = UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01((UnityEngine.Mathf.Cos(a * 16) - .1f) / .7f));
        float fade = (1 - UnityEngine.Mathf.SmoothStep(.2f, .98f, r)) * UnityEngine.Mathf.SmoothStep(.02f, .2f, r);
        px[j * N + i] = new UnityEngine.Color(1, 1, 1, ray * fade);
    }
    tex.SetPixels(px); tex.Apply(); System.IO.File.WriteAllBytes(Modern + "Rays.png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
    UnityEditor.AssetDatabase.ImportAsset(Modern + "Rays.png", UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(Modern + "Rays.png");
    imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.alphaIsTransparency = true;
    imp.mipmapEnabled = false; imp.wrapMode = UnityEngine.TextureWrapMode.Clamp; imp.textureCompression = UnityEditor.TextureImporterCompression.Compressed; imp.SaveAndReimport();
}
// First sprite at a path (handles sheets imported as Multiple).
System.Func<string, UnityEngine.Sprite> S = p => { foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(p)) if (o is UnityEngine.Sprite sp) return sp; return null; };
var rays = S(Modern + "Rays.png"); var glow = S(Modern + "Glow.png"); var tile = S(Modern + "Tile.png"); var pill = S(Modern + "Pill.png");
var close = S(Modern + "Icon-Close.png"); var coin = S(Illustrated + "coin.png"); var xpIcon = S(Illustrated + "xp.png");
var trophy = S(Illustrated + "trophy.png"); var flame = S(Illustrated + "flame.png"); var star = S("Assets/Art/GUI/09-Panels-and-Effects/star-gold.png");
System.Func<string, TMPro.TMP_FontAsset> F = n => UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Poppins/Poppins-" + n + " SDF.asset");
var fExtra = F("ExtraBold"); var fBold = F("Bold"); var fSemi = F("SemiBold"); var fMedium = F("Medium");
var mid = new UnityEngine.Vector2(.5f, .5f);

System.Func<UnityEngine.Transform, string, UnityEngine.GameObject> Node = (parent, name) =>
{
    var t = parent.Find(name);
    var go = t != null ? t.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform));
    go.transform.SetParent(parent, false); go.layer = parent.gameObject.layer; return go;
};
System.Action<UnityEngine.RectTransform, UnityEngine.Vector2, UnityEngine.Vector2, UnityEngine.Vector2> Place = (r, anchor, pos, size) =>
{
    r.anchorMin = r.anchorMax = anchor; r.pivot = mid; r.anchoredPosition = pos; r.sizeDelta = size; r.localScale = UnityEngine.Vector3.one; r.localRotation = UnityEngine.Quaternion.identity;
};
System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, bool, UnityEngine.UI.Image> Img = (parent, name, sprite, sliced) =>
{
    var go = Node(parent, name);
    var img = go.GetComponent<UnityEngine.UI.Image>(); if (img == null) img = go.AddComponent<UnityEngine.UI.Image>();
    img.sprite = sprite; img.type = sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
    img.preserveAspect = true; img.raycastTarget = false; img.color = UnityEngine.Color.white; return img;
};
System.Func<UnityEngine.Transform, string, TMPro.TMP_FontAsset, float, UnityEngine.Color, TMPro.TextMeshProUGUI> Txt = (parent, name, font, size, color) =>
{
    var go = Node(parent, name);
    var t = go.GetComponent<TMPro.TextMeshProUGUI>(); if (t == null) t = go.AddComponent<TMPro.TextMeshProUGUI>();
    t.font = font; t.fontSharedMaterial = font.material; t.fontSize = size; t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = UnityEngine.Mathf.Max(14, size * .55f);
    t.color = color; t.alignment = TMPro.TextAlignmentOptions.Center; t.raycastTarget = false; t.textWrappingMode = TMPro.TextWrappingModes.Normal; return t;
};
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> Grad = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>(); if (e == null) e = g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
System.Func<UnityEngine.Transform, string, UnityEngine.RectTransform> R = (root, path) => (UnityEngine.RectTransform)root.Find(path);

System.Func<UnityEngine.GameObject, string> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var screens = canvas.transform.Find("Device safe area/Portrait frame/Screens");
    var card = screens.Find("Results/Results card");
    var y0 = new UnityEngine.Vector2(.5f, 0);
    // ---- Header: bigger title (gradient is set per outcome in code) ----
    var title = R(card, "Header/Title").GetComponent<TMPro.TMP_Text>(); title.fontSize = 84; title.fontSizeMax = 84;
    // ---- Star stage ----
    var plate = card.Find("Score/Score plate");
    var band = R(plate, "Band"); band.anchorMin = new UnityEngine.Vector2(0, .42f); band.anchorMax = UnityEngine.Vector2.one;
    band.offsetMin = new UnityEngine.Vector2(14, 0); band.offsetMax = new UnityEngine.Vector2(-14, -14);
    var bandImg = band.GetComponent<UnityEngine.UI.Image>(); bandImg.color = UnityEngine.Color.white;
    Grad(bandImg, C(92, 106, 255, 1), C(160, 88, 236, 1), true);
    var stage = band.GetComponent<UnityEngine.Canvas>(); if (stage == null) stage = band.gameObject.AddComponent<UnityEngine.Canvas>();
    stage.overrideSorting = false; stage.additionalShaderChannels = canvas.GetComponent<UnityEngine.Canvas>().additionalShaderChannels;
    if (band.GetComponent<UnityEngine.UI.RectMask2D>() == null) band.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
    var raysImg = Img(band, "Sunburst", rays, false); raysImg.color = new UnityEngine.Color(1, 1, 1, .24f);
    Place(raysImg.rectTransform, new UnityEngine.Vector2(.5f, .5f), UnityEngine.Vector2.zero, new UnityEngine.Vector2(1150, 1150));
    var glowImg = Img(band, "Star glow", glow, false); glowImg.color = C(255, 196, 80, .6f);
    Place(glowImg.rectTransform, new UnityEngine.Vector2(.5f, .5f), new UnityEngine.Vector2(0, 10), new UnityEngine.Vector2(640, 640));
    for (int i = 1; i <= 3; i++)
    {
        foreach (var n in new[] { "Star " + i, "Star " + i + " slot" })
        {
            var r = R(plate, n); r.anchorMin = r.anchorMax = new UnityEngine.Vector2(.5f, .71f);
        }
        R(plate, "Star " + i + " slot").GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(1, 1, 1, .24f);
    }
    // ---- Score block ----
    var yScore = new UnityEngine.Vector2(.5f, .21f);
    var caption = R(plate, "Score caption"); Place(caption, yScore, new UnityEngine.Vector2(0, 104), new UnityEngine.Vector2(600, 40));
    var capText = caption.GetComponent<TMPro.TMP_Text>(); capText.text = "FINAL SCORE"; capText.characterSpacing = 8; capText.fontSize = 26; capText.fontSizeMax = 26;
    Place(R(plate, "Score"), yScore, new UnityEngine.Vector2(0, 18), new UnityEngine.Vector2(800, 128));
    Place(R(plate, "Best chip"), yScore, new UnityEngine.Vector2(0, -92), new UnityEngine.Vector2(560, 68));
    Place(R(plate, "Personal best"), yScore, new UnityEngine.Vector2(22, -92), new UnityEngine.Vector2(460, 46));
    var trophyImg = Img(plate, "Best trophy", trophy, false); Place(trophyImg.rectTransform, yScore, new UnityEngine.Vector2(-232, -92), new UnityEngine.Vector2(54, 54));
    // Chip, then trophy, then text (safe to re-run).
    trophyImg.transform.SetSiblingIndex(R(plate, "Best chip").GetSiblingIndex() + 1);
    R(plate, "Personal best").SetSiblingIndex(trophyImg.transform.GetSiblingIndex() + 1);
    // ---- Stat tiles ----
    var stats = card.Find("Stats");
    Place(R(stats, "Stats plate shadow"), y0, new UnityEngine.Vector2(0, 734), new UnityEngine.Vector2(1096, 346));
    Place(R(stats, "Stats plate"), y0, new UnityEngine.Vector2(0, 748), new UnityEngine.Vector2(1000, 250));
    var heading = R(stats, "Stats"); Place(heading, y0, new UnityEngine.Vector2(0, 850), new UnityEngine.Vector2(900, 36));
    var headText = heading.GetComponent<TMPro.TMP_Text>(); headText.font = fSemi; headText.fontSharedMaterial = fSemi.material; headText.fontSize = headText.fontSizeMax = 24;
    headText.color = C(107, 113, 144, 1); headText.alignment = TMPro.TextAlignmentOptions.Center; headText.characterSpacing = 6; headText.text = "This round";
    var tileFill = new[] { C(232, 239, 255, 1), C(255, 240, 225, 1), C(241, 238, 255, 1) };
    var tileEdge = new[] { C(198, 212, 255, 1), C(255, 214, 180, 1), C(218, 210, 250, 1) };
    var icons = new[] { star, flame, close };
    var tiles = data.FindProperty("resultStatTiles"); tiles.arraySize = 3;
    for (int i = 0; i < 3; i++)
    {
        var edge = Img(stats, "Stat tile " + (i + 1), tile, true); edge.color = tileEdge[i];
        Place(edge.rectTransform, y0, new UnityEngine.Vector2((i - 1) * 318, 730), new UnityEngine.Vector2(300, 172));
        var fill = Img(edge.transform, "Fill", tile, true); fill.color = tileFill[i];
        var fr = fill.rectTransform; fr.anchorMin = UnityEngine.Vector2.zero; fr.anchorMax = UnityEngine.Vector2.one; fr.offsetMin = new UnityEngine.Vector2(4, 4); fr.offsetMax = new UnityEngine.Vector2(-4, -4);
        var icon = Img(edge.transform, "Icon", icons[i], false); icon.color = i == 2 ? C(150, 136, 210, 1) : UnityEngine.Color.white;
        Place(icon.rectTransform, mid, new UnityEngine.Vector2(0, 50), new UnityEngine.Vector2(i == 2 ? 32 : 46, i == 2 ? 32 : 46));
        var label = Txt(edge.transform, "Value", fBold, 30, C(30, 34, 64, 1)); label.text = "0\nLabel";
        var lr = label.rectTransform; lr.anchorMin = UnityEngine.Vector2.zero; lr.anchorMax = UnityEngine.Vector2.one; lr.offsetMin = new UnityEngine.Vector2(12, 10); lr.offsetMax = new UnityEngine.Vector2(-12, -72);
        label.lineSpacing = -6;
        tiles.GetArrayElementAtIndex(i).objectReferenceValue = label;
    }
    // ---- Rewards ----
    var rewards = card.Find("Rewards");
    var box = R(rewards, "Round rewards"); Place(box, y0, new UnityEngine.Vector2(0, 500), new UnityEngine.Vector2(1000, 236));
    box.GetComponent<UnityEngine.UI.Image>().color = C(236, 224, 250, 1);
    var boxFill = box.Find("Fill").GetComponent<UnityEngine.UI.Image>(); boxFill.color = UnityEngine.Color.white; Grad(boxFill, C(255, 247, 230, 1), C(244, 238, 255, 1), false);
    var earned = R(box, "Earned"); Place(earned, mid, new UnityEngine.Vector2(0, 88), new UnityEngine.Vector2(920, 36));
    var earnedText = earned.GetComponent<TMPro.TMP_Text>(); earnedText.font = fSemi; earnedText.fontSharedMaterial = fSemi.material; earnedText.fontSize = earnedText.fontSizeMax = 26;
    earnedText.color = C(94, 100, 136, 1); earnedText.characterSpacing = 4;
    var chips = Node(box, "Reward chips"); Place((UnityEngine.RectTransform)chips.transform, mid, new UnityEngine.Vector2(0, 36), new UnityEngine.Vector2(600, 72));
    chips.transform.SetSiblingIndex(earned.GetSiblingIndex() + 1);
    System.Func<string, float, UnityEngine.Sprite, UnityEngine.Color, UnityEngine.Color, UnityEngine.Color, TMPro.TextMeshProUGUI> Chip = (name, x, icon, from, to, ink) =>
    {
        var c = Img(chips.transform, name, pill, true); c.pixelsPerUnitMultiplier = 128f / 70; Grad(c, from, to, false);
        Place(c.rectTransform, mid, new UnityEngine.Vector2(x, 0), new UnityEngine.Vector2(270, 70));
        var ic = Img(c.transform, "Icon", icon, false); Place(ic.rectTransform, mid, new UnityEngine.Vector2(-96, 0), new UnityEngine.Vector2(62, 62));
        var t = Txt(c.transform, "Amount", fExtra, 38, ink); Place(t.rectTransform, mid, new UnityEngine.Vector2(26, 1), new UnityEngine.Vector2(180, 56));
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; return t;
    };
    var coinText = Chip("Coin chip", -150, coin, C(255, 230, 150, 1), C(255, 198, 92, 1), C(112, 58, 0, 1)); coinText.text = "+50";
    var xpText = Chip("XP chip", 150, xpIcon, C(186, 166, 255, 1), C(138, 104, 250, 1), UnityEngine.Color.white); xpText.text = "+25 XP";
    Place(R(box, "Balance"), mid, new UnityEngine.Vector2(0, -16), new UnityEngine.Vector2(920, 34));
    var bal = R(box, "Balance").GetComponent<TMPro.TMP_Text>(); bal.fontSize = bal.fontSizeMax = 24;
    Place(R(box, "Player progress"), mid, new UnityEngine.Vector2(0, -54), new UnityEngine.Vector2(920, 34));
    var prog = R(box, "Player progress").GetComponent<TMPro.TMP_Text>(); prog.fontSize = prog.fontSizeMax = 26;
    var track = R(box, "XP track"); Place(track, mid, new UnityEngine.Vector2(0, -89), new UnityEngine.Vector2(860, 18));
    track.GetComponent<UnityEngine.UI.Image>().color = C(228, 222, 248, 1);
    Grad(track.Find("XP progress").GetComponent<UnityEngine.UI.Image>(), C(92, 106, 255, 1), C(170, 96, 240, 1), false);
    // ---- Hint chip ----
    var hintChip = Img(rewards, "Hint chip", tile, true); hintChip.color = C(222, 247, 231, 1);
    Place(hintChip.rectTransform, y0, new UnityEngine.Vector2(0, 344), new UnityEngine.Vector2(1000, 68));
    var hint = R(rewards, "Mastery hint"); hintChip.transform.SetAsLastSibling(); hint.SetAsLastSibling(); // Chip directly behind the text.
    Place(hint, y0, new UnityEngine.Vector2(0, 344), new UnityEngine.Vector2(960, 62));
    var hintText = hint.GetComponent<TMPro.TMP_Text>(); hintText.fontSize = hintText.fontSizeMax = 26; hintText.enableAutoSizing = true; hintText.fontSizeMin = 16; hintText.lineSpacing = -8;
    // ---- Bindings ----
    data.FindProperty("resultStageGradient").objectReferenceValue = band.GetComponent<TapBlast.UIGradient>();
    data.FindProperty("resultStageGlow").objectReferenceValue = glowImg;
    data.FindProperty("resultRays").objectReferenceValue = raysImg.rectTransform;
    data.FindProperty("rewardCoins").objectReferenceValue = coinText;
    data.FindProperty("rewardXP").objectReferenceValue = xpText;
    data.FindProperty("rewardChips").objectReferenceValue = chips;
    data.FindProperty("resultHintChip").objectReferenceValue = hintChip;
    data.ApplyModifiedPropertiesWithoutUndo();

    // ================= Wallet panel =================
    var nav = canvas.GetComponent<TapBlast.GuiNavigation>(); var navData = new UnityEditor.SerializedObject(nav);
    var wp = screens.Find("Wallet information");
    wp.GetComponent<UnityEngine.UI.Image>().color = C(28, 24, 70, .62f); // Dim the screen behind the modal.
    var wcard = R(wp, "Wallet card"); Place(wcard, mid, new UnityEngine.Vector2(0, -50), new UnityEngine.Vector2(920, 900));
    Place(R(wp, "Wallet card shadow"), mid, new UnityEngine.Vector2(0, -62), new UnityEngine.Vector2(1000, 980));
    var hb = R(wcard, "Header band"); Place(hb, mid, new UnityEngine.Vector2(0, 344), new UnityEngine.Vector2(888, 210));
    Grad(hb.GetComponent<UnityEngine.UI.Image>(), C(92, 106, 255, 1), C(160, 88, 236, 1), false);
    var wTitle = R(wcard, "Header/Wallet title"); Place(wTitle, mid, new UnityEngine.Vector2(0, 368), new UnityEngine.Vector2(700, 90));
    wTitle.GetComponent<TMPro.TMP_Text>().color = UnityEngine.Color.white;
    var wEye = R(wcard, "Header/Eyebrow"); Place(wEye, mid, new UnityEngine.Vector2(0, 306), new UnityEngine.Vector2(700, 40));
    var eyeText = wEye.GetComponent<TMPro.TMP_Text>(); eyeText.color = new UnityEngine.Color(1, 1, 1, .85f); eyeText.text = "PLAYER PROFILE"; eyeText.characterSpacing = 8; eyeText.fontSize = eyeText.fontSizeMax = 24;
    var closeBtn = R(wcard, "Actions/Close wallet"); Place(closeBtn, mid, new UnityEngine.Vector2(372, 388), new UnityEngine.Vector2(92, 92));
    closeBtn.GetComponent<UnityEngine.UI.Image>().color = new UnityEngine.Color(1, 1, 1, .95f);
    var content = wcard.Find("Content");
    System.Func<string, float, UnityEngine.Sprite, UnityEngine.Color, UnityEngine.Color, UnityEngine.Color, TMPro.TextMeshProUGUI> Tile = (name, x, icon, top, bottom, edge) =>
    {
        var e = Img(content, name, tile, true); e.color = edge; Place(e.rectTransform, mid, new UnityEngine.Vector2(x, 82), new UnityEngine.Vector2(400, 290));
        var f = Img(e.transform, "Fill", tile, true); Grad(f, top, bottom, true);
        var fr = f.rectTransform; fr.anchorMin = UnityEngine.Vector2.zero; fr.anchorMax = UnityEngine.Vector2.one; fr.offsetMin = new UnityEngine.Vector2(5, 5); fr.offsetMax = new UnityEngine.Vector2(-5, -5);
        var ic = Img(e.transform, "Icon", icon, false); Place(ic.rectTransform, mid, new UnityEngine.Vector2(0, 66), new UnityEngine.Vector2(116, 116));
        var t = Txt(e.transform, "Amount", fExtra, 58, C(30, 34, 64, 1)); Place(t.rectTransform, mid, new UnityEngine.Vector2(0, -66), new UnityEngine.Vector2(360, 130));
        t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.lineSpacing = -10; return t;
    };
    var wCoins = Tile("Coins tile", -212, coin, C(255, 246, 220, 1), C(255, 226, 164, 1), C(255, 214, 150, 1)); wCoins.text = "0\nCoins";
    var wXP = Tile("XP tile", 212, xpIcon, C(240, 235, 255, 1), C(214, 202, 255, 1), C(206, 192, 255, 1)); wXP.text = "0\nTotal XP";
    var plateImg = R(content, "Details plate").GetComponent<UnityEngine.UI.Image>();
    Place(plateImg.rectTransform, mid, new UnityEngine.Vector2(0, -166), new UnityEngine.Vector2(824, 168));
    plateImg.color = UnityEngine.Color.white; Grad(plateImg, C(248, 246, 255, 1), C(240, 237, 255, 1), true);
    var level = Txt(content, "Player level", fMedium, 34, C(30, 34, 64, 1)); Place(level.rectTransform, mid, new UnityEngine.Vector2(0, -130), new UnityEngine.Vector2(780, 48));
    level.textWrappingMode = TMPro.TextWrappingModes.NoWrap; level.text = "Player level 1";
    var wTrack = Img(content, "Level track", pill, true); wTrack.color = C(226, 220, 248, 1); wTrack.pixelsPerUnitMultiplier = 128f / 24;
    Place(wTrack.rectTransform, mid, new UnityEngine.Vector2(0, -196), new UnityEngine.Vector2(740, 24));
    var wFill = Img(wTrack.transform, "Level progress", pill, true); wFill.pixelsPerUnitMultiplier = 128f / 24; Grad(wFill, C(92, 106, 255, 1), C(170, 96, 240, 1), false);
    var wfr = wFill.rectTransform; wfr.anchorMin = UnityEngine.Vector2.zero; wfr.anchorMax = new UnityEngine.Vector2(.5f, 1); wfr.offsetMin = wfr.offsetMax = UnityEngine.Vector2.zero; wfr.pivot = mid;
    var details = R(content, "Wallet details"); details.SetAsLastSibling(); Place(details, mid, new UnityEngine.Vector2(0, -330), new UnityEngine.Vector2(780, 90));
    var dText = details.GetComponent<TMPro.TMP_Text>(); dText.fontSize = dText.fontSizeMax = 28; dText.enableAutoSizing = true; dText.fontSizeMin = 18;
    dText.color = C(107, 113, 144, 1); dText.alignment = TMPro.TextAlignmentOptions.Center;
    navData.FindProperty("walletCoins").objectReferenceValue = wCoins;
    navData.FindProperty("walletXP").objectReferenceValue = wXP;
    navData.FindProperty("walletLevel").objectReferenceValue = level;
    navData.FindProperty("walletXpFill").objectReferenceValue = wFill;
    navData.ApplyModifiedPropertiesWithoutUndo();
    return "results + wallet";
};
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
var sceneResult = apply(ui);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
var prefabResult = apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS scene " + sceneResult + "; prefab " + prefabResult;
