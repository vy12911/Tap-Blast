// Live edit: objective card proportions — slim progress line, even padding, compact rating stars.
// Card 1000x132 (centre y -458, 16 px below the stats card); label row and bar each get 22-24 px padding.
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var objective = canvas.transform.Find("Device safe area/Portrait frame/HUD/Objective");
    var labelSection = objective.Find("Objective label");
    var track = objective.Find("Objective track");
    const float y = -458;
    var top = new UnityEngine.Vector2(.5f, 1);
    System.Action<UnityEngine.RectTransform, UnityEngine.Vector2, float, float, float, float> pin = (r, a, x, py, w, h) =>
    {
        r.anchorMin = r.anchorMax = a; r.pivot = new UnityEngine.Vector2(.5f, .5f);
        r.anchoredPosition = new UnityEngine.Vector2(x, py); r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
    };
    var card = (UnityEngine.RectTransform)objective.Find("Objective card");
    pin(card, top, 0, y, 1000, 132);
    var shadow = (UnityEngine.RectTransform)objective.Find("Objective card shadow");
    if (shadow != null) pin(shadow, top, 0, y - 14, 1000 + 96, 132 + 96);
    var label = (UnityEngine.RectTransform)labelSection.Find("Objective");
    pin(label, top, -130, y + 30, 680, 40);
    var text = label.GetComponent<TMPro.TMP_Text>(); text.fontSize = text.fontSizeMax = 28; text.fontSizeMin = 20;
    for (int i = 1; i <= 3; i++)
        pin((UnityEngine.RectTransform)labelSection.Find("Objective star " + i), top, 372 + (i - 1) * 42, y + 30, 36, 36);
    pin((UnityEngine.RectTransform)track, top, 0, y - 28, 920, 26);
    pin((UnityEngine.RectTransform)track.Find("Progress"), new UnityEngine.Vector2(.5f, .5f), 0, 0, 912, 20);
};

var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found in " + scene.path;
apply(ui);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS objective card proportions updated in " + scene.path + " (saved) and " + prefabPath;
