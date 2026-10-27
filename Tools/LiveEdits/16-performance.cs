// Live edit: performance. The scrolling level list gets its own nested Canvas (+ raycaster), so scrolling
// re-batches only the rows instead of the whole UI. The camera and URP asset drop HDR: the UI is a Screen
// Space Overlay canvas with no post-processing, and an HDR target costs a full-screen 64-bit buffer per frame.
System.Func<UnityEngine.GameObject, string> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>();
    var scroll = canvas.transform.Find("Device safe area/Portrait frame/Screens/Level select").GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
    var content = scroll.content.gameObject;
    var sub = content.GetComponent<UnityEngine.Canvas>(); if (sub == null) sub = content.AddComponent<UnityEngine.Canvas>();
    sub.overrideSorting = false; sub.pixelPerfect = false;
    // The root canvas passes these to TMP and gradients; a nested canvas needs them too.
    sub.additionalShaderChannels = canvas.GetComponent<UnityEngine.Canvas>().additionalShaderChannels;
    if (content.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null) content.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    return "list canvas on " + content.name;
};
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
UnityEngine.GameObject ui = null; UnityEngine.Camera cam = null;
foreach (var g in scene.GetRootGameObjects()) { if (g.name == "Tap Blast UI") ui = g; if (g.GetComponent<UnityEngine.Camera>()) cam = g.GetComponent<UnityEngine.Camera>(); }
if (ui == null || cam == null) return "FAIL: UI or camera not found";
var sceneResult = apply(ui);
cam.allowHDR = false; cam.allowMSAA = false;
UnityEditor.EditorUtility.SetDirty(cam);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
const string prefabPath = "Assets/Prefabs/UI/GameCanvas.prefab";
var prefab = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
var prefabResult = apply(prefab); UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); UnityEditor.PrefabUtility.UnloadPrefabContents(prefab);
var urp = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
urp.supportsHDR = false; UnityEditor.EditorUtility.SetDirty(urp); UnityEditor.AssetDatabase.SaveAssets();
return "PASS scene: " + sceneResult + "; prefab: " + prefabResult + "; camera HDR " + cam.allowHDR + "; URP HDR " + urp.supportsHDR;
