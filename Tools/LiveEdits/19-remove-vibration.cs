// Live edit: vibration removed. Deletes the Pause card's Vibration row (and its glow), moves "Exit to levels"
// up into the gap and shortens the card by the same amount, keeping its top edge in place.
const float Row = 116;
System.Func<UnityEngine.GameObject, string> apply = canvas =>
{
    var paused = canvas.transform.Find("Device safe area/Portrait frame/Screens/Paused");
    var card = (UnityEngine.RectTransform)paused.Find("Paused card");
    var actions = card.Find("Actions");
    var row = actions.Find("Vibration");
    if (row == null) return "already removed";
    var glow = actions.Find("Vibration glow");
    UnityEngine.Object.DestroyImmediate(row.gameObject);
    if (glow != null) UnityEngine.Object.DestroyImmediate(glow.gameObject);
    var exit = (UnityEngine.RectTransform)actions.Find("Exit to levels");
    exit.anchoredPosition += new UnityEngine.Vector2(0, Row);
    // Shorter card, same top: the centre moves up by half a row, so children move down by half a row in card space.
    card.sizeDelta -= new UnityEngine.Vector2(0, Row); card.anchoredPosition += new UnityEngine.Vector2(0, Row / 2);
    foreach (UnityEngine.RectTransform child in card)
    {
        if (child.anchorMin == UnityEngine.Vector2.zero && child.anchorMax == UnityEngine.Vector2.one)
            foreach (UnityEngine.RectTransform item in child) item.anchoredPosition -= new UnityEngine.Vector2(0, Row / 2);
        else child.anchoredPosition -= new UnityEngine.Vector2(0, Row / 2);
    }
    var shadow = (UnityEngine.RectTransform)paused.Find("Paused card shadow");
    shadow.sizeDelta -= new UnityEngine.Vector2(0, Row); shadow.anchoredPosition += new UnityEngine.Vector2(0, Row / 2);
    return "Vibration row removed, card " + card.sizeDelta.y;
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
return "PASS scene: " + sceneResult + "; prefab: " + prefabResult;
