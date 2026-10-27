// Live edit: balance the results score card. Band = upper half with the stars centred in it;
// caption, score and best chip centred in the lower half. Anchored proportionally for any height.
var mid = new UnityEngine.Vector2(.5f, .5f);
System.Action<UnityEngine.RectTransform, float, float, float, float, float> at = (r, anchorY, x, y, w, h) =>
{
    r.anchorMin = r.anchorMax = new UnityEngine.Vector2(.5f, anchorY); r.pivot = mid;
    r.anchoredPosition = new UnityEngine.Vector2(x, y); r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
};
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var score = canvas.transform.Find("Device safe area/Portrait frame/Screens/Results/Results card/Score/Score plate");
    var band = (UnityEngine.RectTransform)score.Find("Band");
    band.anchorMin = new UnityEngine.Vector2(0, .5f); band.anchorMax = UnityEngine.Vector2.one;
    band.offsetMin = new UnityEngine.Vector2(16, 0); band.offsetMax = new UnityEngine.Vector2(-16, -16);
    for (int i = 1; i <= 3; i++) at((UnityEngine.RectTransform)score.Find("Star " + i), .75f, (i - 2) * 200, i == 2 ? 14 : -10, 160, 160);
    at((UnityEngine.RectTransform)score.Find("Score caption"), .25f, 0, 96, 600, 40);
    at((UnityEngine.RectTransform)score.Find("Score"), .25f, 0, 14, 800, 124);
    at((UnityEngine.RectTransform)score.Find("Best chip"), .25f, 0, -92, 520, 64);
    at((UnityEngine.RectTransform)score.Find("Personal best"), .25f, 0, -92, 480, 46);
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
return "PASS results score card balanced in scene (saved) and prefab";
