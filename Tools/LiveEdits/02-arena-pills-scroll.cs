// Live edit: (1) true pill ends at any height, (2) richer arena backdrops so tap effects show,
// (3) white popup labels on the darker arena, (4) elastic level-list scrolling.
const string ArtDir = "Assets/Art/UI/Modern/";
System.Func<float, float, float> len = (x, y) => UnityEngine.Mathf.Sqrt(x * x + y * y);
System.Func<int, int, float> hash = (x, y) => { uint h = (uint)(x * 374761393 + y * 668265263); h = (h ^ (h >> 13)) * 1274126177; return (h & 0xffff) / 65535f; };
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);

// Deep, saturated gradient with soft light bokeh, a centre lift and a faint dot grid.
System.Action<string, UnityEngine.Color, UnityEngine.Color> backdrop = (name, top, bottom) =>
{
    const int w = 512, h = 768;
    var tex = new UnityEngine.Texture2D(w, h, UnityEngine.TextureFormat.RGBA32, false);
    var px = new UnityEngine.Color[w * h];
    var bx = new float[16]; var by = new float[16]; var br = new float[16]; var ba = new float[16];
    for (int i = 0; i < 16; i++) { bx[i] = hash(i, 1) * w; by[i] = hash(i, 2) * h; br[i] = 26 + hash(i, 3) * 80; ba[i] = .05f + hash(i, 4) * .08f; }
    for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
    {
        float x = i + .5f, y = h - (j + .5f);
        var c = UnityEngine.Color.Lerp(top, bottom, y / h);
        float lift = 1 - UnityEngine.Mathf.SmoothStep(0, 1, UnityEngine.Mathf.Clamp01(len(x - w / 2f, y - h * .42f) / (w * .7f)));
        c = UnityEngine.Color.Lerp(c, UnityEngine.Color.white, lift * .12f);
        for (int k = 0; k < 16; k++) { float a = (1 - UnityEngine.Mathf.SmoothStep(.5f, 1, len(x - bx[k], y - by[k]) / br[k])) * ba[k]; c = UnityEngine.Color.Lerp(c, UnityEngine.Color.white, a); }
        float gx = UnityEngine.Mathf.Repeat(x, 28) - 14, gy = UnityEngine.Mathf.Repeat(y, 28) - 14;
        float dot = UnityEngine.Mathf.Clamp01(.5f - (len(gx, gy) - 1.6f));
        c = UnityEngine.Color.Lerp(c, UnityEngine.Color.white, dot * .1f); c.a = 1;
        px[j * w + i] = c;
    }
    tex.SetPixels(px); tex.Apply();
    System.IO.File.WriteAllBytes(ArtDir + name + ".png", tex.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(tex);
    UnityEditor.AssetDatabase.ImportAsset(ArtDir + name + ".png", UnityEditor.ImportAssetOptions.ForceUpdate);
};
backdrop("Arena-Sky", C(58, 70, 168, 1), C(104, 66, 178, 1));     // indigo -> violet (levels 1, 11-20)
backdrop("Arena-Mint", C(22, 104, 122, 1), C(44, 78, 152, 1));    // teal -> blue (levels 2-10)
backdrop("Arena-Peach", C(150, 62, 112, 1), C(88, 58, 162, 1));   // plum -> violet (level 8)

int pills = 0;
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    // Pill sprite corners are 64 px; scale them to half the element's height so ends stay round.
    foreach (var image in canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true))
    {
        if (image.sprite == null || image.sprite.name != "Pill" || image.type != UnityEngine.UI.Image.Type.Sliced) continue;
        var rect = image.rectTransform.rect; float shortSide = UnityEngine.Mathf.Min(rect.width, rect.height);
        if (shortSide <= 0) continue;
        image.pixelsPerUnitMultiplier = UnityEngine.Mathf.Max(1, 128f / shortSide); pills++;
    }
    var portrait = canvas.transform.Find("Device safe area/Portrait frame");
    foreach (UnityEngine.Transform popup in portrait.Find("HUD/Arena/Pooled feedback"))
    {
        var points = popup.Find("Points"); if (points != null) points.GetComponent<TMPro.TMP_Text>().color = UnityEngine.Color.white;
    }
    var scroll = portrait.Find("Screens/Level select/Level select card/Level list/Level list viewport").GetComponent<UnityEngine.UI.ScrollRect>();
    scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic; scroll.elasticity = .12f;
};

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
apply(ui);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
int scenePills = pills; pills = 0;
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS backdrops regenerated; pill ends fixed on " + scenePills + " scene images; popup labels white; level list elastic; scene + prefab saved";
