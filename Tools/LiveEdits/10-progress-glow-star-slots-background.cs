// Live edit: glowing objective progress, visible star slots, richer main background.
const string Dir = "Assets/Art/UI/Modern/";
System.Func<float, float, float> len = (x, y) => UnityEngine.Mathf.Sqrt(x * x + y * y);
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);
System.Action<string, int, int, System.Func<float, float, UnityEngine.Color>, UnityEngine.Vector4> bake = (name, w, h, px, border) =>
{
    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false); var p = new UnityEngine.Color[w * h];
    for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) p[j * w + i] = px(i + .5f, h - (j + .5f));
    tex.SetPixels(p); tex.Apply(); System.IO.File.WriteAllBytes(Dir + name + ".png", tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
    UnityEditor.AssetDatabase.ImportAsset(Dir + name + ".png", UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(Dir + name + ".png");
    imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; imp.spriteBorder = border;
    imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.wrapMode = UnityEngine.TextureWrapMode.Clamp;
    imp.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed; imp.npotScale = UnityEditor.TextureImporterNPOTScale.None; imp.SaveAndReimport();
};
// Flat five-point star (supersampled) used as the visible slot behind every rating star.
var starPts = new UnityEngine.Vector2[10];
for (int k = 0; k < 10; k++) { float a = -UnityEngine.Mathf.PI / 2 + k * UnityEngine.Mathf.PI / 5; float r = k % 2 == 0 ? 60 : 27; starPts[k] = new UnityEngine.Vector2(64 + r * UnityEngine.Mathf.Cos(a), 66 + r * UnityEngine.Mathf.Sin(a)); }
System.Func<float, float, bool> inStar = (x, y) => { bool hit = false; for (int i = 0, j = 9; i < 10; j = i++) { var a = starPts[i]; var b = starPts[j]; if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x) hit = !hit; } return hit; };
bake("StarSlot", 128, 128, (x, y) => { int n = 0; for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++) if (inStar(x - .5f + (sx + .5f) / 4, y - .5f + (sy + .5f) / 4)) n++; return new UnityEngine.Color(1, 1, 1, n / 16f); }, UnityEngine.Vector4.zero);
// Richer page background: periwinkle -> lilac -> rose.
UnityEngine.Color bgTop = C(186, 201, 255, 1), bgMid = C(221, 208, 255, 1), bgLow = C(255, 225, 241, 1);
bake("Background", 8, 512, (x, y) => y < 230 ? UnityEngine.Color.Lerp(bgTop, bgMid, y / 230) : UnityEngine.Color.Lerp(bgMid, bgLow, UnityEngine.Mathf.SmoothStep(0, 1, (y - 230) / 282)), UnityEngine.Vector4.zero);

System.Func<string, UnityEngine.Sprite> art = n => UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Dir + n + ".png");
var starSlot = art("StarSlot"); var shadow = art("CardShadow"); var pill = art("Pill"); var glow = art("Glow"); var circle = art("Circle");
var mid = new UnityEngine.Vector2(.5f, .5f);
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
int slots = 0;
System.Action<UnityEngine.UI.Image> slot = star =>
{
    var s = image(star.transform.parent, star.name + " slot", starSlot, false); s.color = C(192, 188, 236, 1);
    var r = s.rectTransform; var t = star.rectTransform; r.SetSiblingIndex(t.GetSiblingIndex());
    r.anchorMin = t.anchorMin; r.anchorMax = t.anchorMax; r.pivot = t.pivot; r.anchoredPosition = t.anchoredPosition; r.sizeDelta = t.sizeDelta * .8f; r.localScale = UnityEngine.Vector3.one;
    slots++;
};

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var portrait = canvas.transform.Find("Device safe area/Portrait frame");
    // ---- Glowing objective progress ----
    var track = portrait.Find("HUD/Objective/Objective track");
    track.GetComponent<UnityEngine.UI.Image>().color = C(222, 216, 248, 1);
    var fill = track.Find("Progress").GetComponent<UnityEngine.UI.Image>();
    gradient(fill, C(255, 210, 40, 1), C(255, 92, 150, 1), false);
    var glowImg = image(track, "Progress glow", shadow, true); glowImg.color = C(255, 140, 70, .8f); glowImg.pixelsPerUnitMultiplier = 2.4f;
    var gr = glowImg.rectTransform; gr.SetAsFirstSibling(); gr.anchorMin = UnityEngine.Vector2.zero; gr.anchorMax = new UnityEngine.Vector2(0, 1); gr.pivot = mid;
    gr.offsetMin = new UnityEngine.Vector2(-30, -30); gr.offsetMax = new UnityEngine.Vector2(30, 30);
    var shineImg = image(track, "Progress shine", pill, true); shineImg.color = UnityEngine.Color.white; shineImg.pixelsPerUnitMultiplier = 16;
    var sr = shineImg.rectTransform; sr.SetSiblingIndex(track.Find("Progress").GetSiblingIndex() + 1);
    sr.anchorMin = new UnityEngine.Vector2(0, .5f); sr.anchorMax = new UnityEngine.Vector2(0, 1); sr.pivot = mid;
    sr.offsetMin = new UnityEngine.Vector2(10, 0); sr.offsetMax = new UnityEngine.Vector2(-6, -5);
    gradient(shineImg, new UnityEngine.Color(1, 1, 1, .75f), new UnityEngine.Color(1, 1, 1, .1f), true);
    var head = image(track, "Progress head", glow, false); head.color = C(255, 232, 170, .95f);
    var hr = head.rectTransform; hr.SetAsLastSibling(); hr.anchorMin = hr.anchorMax = new UnityEngine.Vector2(0, .5f); hr.pivot = mid;
    hr.anchoredPosition = UnityEngine.Vector2.zero; hr.sizeDelta = new UnityEngine.Vector2(72, 72);
    var core = image(head.transform, "Core", circle, false); core.color = UnityEngine.Color.white;
    core.rectTransform.anchorMin = core.rectTransform.anchorMax = mid; core.rectTransform.sizeDelta = new UnityEngine.Vector2(18, 18); core.rectTransform.anchoredPosition = UnityEngine.Vector2.zero;
    glowImg.gameObject.SetActive(false); shineImg.gameObject.SetActive(false); head.gameObject.SetActive(false);
    var followers = data.FindProperty("progressFollowers"); followers.arraySize = 2;
    followers.GetArrayElementAtIndex(0).objectReferenceValue = gr; followers.GetArrayElementAtIndex(1).objectReferenceValue = sr;
    data.FindProperty("progressHead").objectReferenceValue = hr;
    // ---- Star slots behind every rating star ----
    foreach (var field in new[] { "objectiveStars", "resultStars", "introBestStars" })
    {
        var list = data.FindProperty(field);
        for (int i = 0; i < list.arraySize; i++) slot((UnityEngine.UI.Image)list.GetArrayElementAtIndex(i).objectReferenceValue);
    }
    foreach (var row in canvas.GetComponentsInChildren<TapBlast.GuiLevelRow>(true))
        for (int s = 1; s <= 3; s++) slot(row.transform.Find("Earned star " + s).GetComponent<UnityEngine.UI.Image>());
    data.ApplyModifiedPropertiesWithoutUndo();
    // ---- Richer background glows; eyebrows on the background stay 4.5:1 ----
    var bg = canvas.transform.Find("Background");
    bg.GetComponent<UnityEngine.UI.Image>().sprite = art("Background");
    bg.Find("Glow blue").GetComponent<UnityEngine.UI.Image>().color = C(125, 160, 255, .6f);
    bg.Find("Glow pink").GetComponent<UnityEngine.UI.Image>().color = C(255, 160, 210, .55f);
    bg.Find("Glow mint").GetComponent<UnityEngine.UI.Image>().color = C(140, 220, 200, .45f);
    foreach (var path in new[] { "Screens/Level briefing/Level briefing card/Header/Eyebrow", "Screens/Level select/Level select card/Header/Eyebrow" })
        portrait.Find(path).GetComponent<TMPro.TMP_Text>().color = C(78, 84, 120, 1);
};

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
apply(ui);
int sceneSlots = slots;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS progress glow + head bound, " + sceneSlots + " star slots, background recoloured; scene saved, prefab saved";
