// Live edit: refined objective bar — calmer glow, smaller head, inset track, warm fill, bigger stars.
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var objective = canvas.transform.Find("Device safe area/Portrait frame/HUD/Objective");
    var track = objective.Find("Objective track");
    // Inset track: slightly darker at the top edge.
    var trackImg = track.GetComponent<UnityEngine.UI.Image>(); trackImg.color = UnityEngine.Color.white;
    gradient(trackImg, C(220, 214, 248, 1), C(238, 235, 253, 1), true);
    // Warm, calmer fill.
    gradient(track.Find("Progress").GetComponent<UnityEngine.UI.Image>(), C(255, 196, 70, 1), C(255, 122, 92, 1), false);
    // Glow: tighter and about half the strength.
    var glow = track.Find("Progress glow").GetComponent<UnityEngine.UI.Image>(); glow.color = C(255, 150, 90, .38f);
    glow.rectTransform.offsetMin = new UnityEngine.Vector2(-16, -16); glow.rectTransform.offsetMax = new UnityEngine.Vector2(16, 16);
    glow.pixelsPerUnitMultiplier = 3.6f;
    // Shine: a faint streak.
    var shine = track.Find("Progress shine").GetComponent<TapBlast.UIGradient>();
    shine.Set(new UnityEngine.Color(1, 1, 1, .5f), new UnityEngine.Color(1, 1, 1, .05f), true);
    // Head: smaller, softer spark.
    var head = (UnityEngine.RectTransform)track.Find("Progress head"); head.sizeDelta = new UnityEngine.Vector2(44, 44);
    head.GetComponent<UnityEngine.UI.Image>().color = C(255, 236, 196, .8f);
    ((UnityEngine.RectTransform)head.Find("Core")).sizeDelta = new UnityEngine.Vector2(14, 14);
    // Stars: larger, on a deeper lavender slot.
    var label = objective.Find("Objective label");
    for (int i = 1; i <= 3; i++)
    {
        var star = (UnityEngine.RectTransform)label.Find("Objective star " + i); var slot = (UnityEngine.RectTransform)label.Find("Objective star " + i + " slot");
        var pos = new UnityEngine.Vector2(352 + (i - 1) * 50, star.anchoredPosition.y);
        star.anchoredPosition = pos; star.sizeDelta = new UnityEngine.Vector2(44, 44);
        slot.anchoredPosition = pos; slot.sizeDelta = new UnityEngine.Vector2(36, 36); slot.GetComponent<UnityEngine.UI.Image>().color = C(168, 160, 230, 1);
    }
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
return "PASS objective bar refined in scene (saved) and prefab";
