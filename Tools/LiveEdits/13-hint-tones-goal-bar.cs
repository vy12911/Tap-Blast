// Live edit: bind semantic hint tint and goal-met bar references; darker hint detail for contrast on tints.
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var hudRoot = canvas.transform.Find("Device safe area/Portrait frame/HUD");
    var track = hudRoot.Find("Objective/Objective track");
    data.FindProperty("progressFillGradient").objectReferenceValue = track.Find("Progress").GetComponent<TapBlast.UIGradient>();
    data.FindProperty("progressGlow").objectReferenceValue = track.Find("Progress glow").GetComponent<UnityEngine.UI.Image>();
    var plate = hudRoot.Find("Tutorial hint/Tutorial hint plate");
    data.FindProperty("hintEdge").objectReferenceValue = plate.GetComponent<UnityEngine.UI.Image>();
    data.FindProperty("hintFill").objectReferenceValue = plate.Find("Fill").GetComponent<UnityEngine.UI.Image>();
    data.ApplyModifiedPropertiesWithoutUndo();
    hudRoot.Find("Tutorial hint/Tutorial detail").GetComponent<TMPro.TMP_Text>().color = new UnityEngine.Color32(84, 90, 122, 255);
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
return "PASS semantic hint + goal-met bar bound in scene (saved) and prefab";
