// Live edit: stronger glows and halos from the depth pass (idempotent: sets absolute values).
int glows = 0, halos = 0;
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    foreach (var img in canvas.GetComponentsInChildren<UnityEngine.UI.Image>(true))
    {
        if (img.name.EndsWith(" glow"))
        {
            var c = img.color; bool row = img.name.StartsWith("Select Level");
            bool button = img.name.StartsWith("Start level") || img.name.StartsWith("Resume") || img.name.StartsWith("Result actions");
            c.a = button ? .7f : row ? .38f : .62f; img.color = c; glows++;
        }
        else if (img.name.EndsWith(" halo"))
        {
            var c = img.color; c.a = .85f; img.color = c;
            var host = img.transform.parent.Find(img.name.Substring(0, img.name.Length - 5)) as UnityEngine.RectTransform;
            if (host != null) img.rectTransform.sizeDelta = host.sizeDelta * 2.1f;
            halos++;
        }
    }
};
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null;
foreach (var g in scene.GetRootGameObjects()) if (g.name == "Tap Blast UI") ui = g;
if (ui == null) return "FAIL: Tap Blast UI not found";
apply(ui);
string summary = glows + " glows, " + halos + " halos";
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
return "PASS boosted " + summary + " in scene (saved) and prefab";
