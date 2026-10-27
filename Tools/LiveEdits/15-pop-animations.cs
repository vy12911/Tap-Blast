// Live edit: pop animation pool. 16 authored "Tap feedback" popups (chains of up to 12 plus callouts never
// recycle a live burst), single-line labels so callout words never wrap, and the callout glow sprite.
const int Count = 16;
var glow = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Glow.png");
System.Func<UnityEngine.Transform, UnityEngine.Transform, string> rel = (root, t) =>
{
    if (t == root) return ""; string s = t.name;
    for (var p = t.parent; p != root; p = p.parent) s = p.name + "/" + s; return s;
};
System.Func<UnityEngine.GameObject, string> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var list = data.FindProperty("popups");
    var template = list.GetArrayElementAtIndex(0);
    var root0 = (UnityEngine.RectTransform)template.FindPropertyRelative("Root").objectReferenceValue;
    var holder = root0.parent;
    int had = list.arraySize;
    for (int i = had; i < Count; i++)
    {
        var name = "Tap feedback " + i;
        var existing = holder.Find(name);
        var clone = existing != null ? existing.gameObject : UnityEngine.Object.Instantiate(root0.gameObject, holder, false);
        clone.name = name; clone.SetActive(false);
        clone.transform.SetSiblingIndex(holder.Find("Tap feedback " + (i - 1)).GetSiblingIndex() + 1);
        list.arraySize = i + 1;
        var element = list.GetArrayElementAtIndex(i);
        // Map every object reference in the template element to the same relative path in the clone.
        foreach (var field in new[] { "Root", "RingRect", "Label", "Ring", "Core", "Alpha" })
        {
            var source = template.FindPropertyRelative(field).objectReferenceValue as UnityEngine.Component;
            var target = clone.transform; var path = rel(root0, source.transform);
            if (path.Length > 0) target = clone.transform.Find(path);
            element.FindPropertyRelative(field).objectReferenceValue = target.GetComponent(source.GetType());
        }
        var sparks0 = template.FindPropertyRelative("Sparks"); var sparks = element.FindPropertyRelative("Sparks");
        sparks.arraySize = sparks0.arraySize;
        for (int j = 0; j < sparks0.arraySize; j++)
            sparks.GetArrayElementAtIndex(j).objectReferenceValue = clone.transform.Find(rel(root0, ((UnityEngine.Component)sparks0.GetArrayElementAtIndex(j).objectReferenceValue).transform));
        element.FindPropertyRelative("Active").boolValue = false;
    }
    data.FindProperty("calloutGlow").objectReferenceValue = glow;
    // Star face on the squashing ball, so a popped target keeps its look until it bursts.
    var targetPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/Prefabs/Targets/NormalTarget.prefab");
    var starFace = targetPrefab.transform.Find("Star face").GetComponent<UnityEngine.UI.Image>().sprite;
    for (int i = 0; i < Count; i++)
    {
        var element = list.GetArrayElementAtIndex(i);
        var core = (UnityEngine.UI.Image)element.FindPropertyRelative("Core").objectReferenceValue;
        var faceT = core.transform.Find("Face");
        var faceGo = faceT != null ? faceT.gameObject : new UnityEngine.GameObject("Face", typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
        faceGo.transform.SetParent(core.transform, false);
        var face = faceGo.GetComponent<UnityEngine.UI.Image>(); face.sprite = starFace; face.raycastTarget = false; face.preserveAspect = true; face.enabled = false;
        var fr = face.rectTransform; fr.anchorMin = new UnityEngine.Vector2(.2f, .2f); fr.anchorMax = new UnityEngine.Vector2(.8f, .8f);
        fr.offsetMin = fr.offsetMax = UnityEngine.Vector2.zero; fr.localScale = UnityEngine.Vector3.one;
        element.FindPropertyRelative("Face").objectReferenceValue = face;
    }
    data.ApplyModifiedPropertiesWithoutUndo();
    int labels = 0;
    for (int i = 0; i < Count; i++)
    {
        var label = (TMPro.TMP_Text)list.GetArrayElementAtIndex(i).FindPropertyRelative("Label").objectReferenceValue;
        label.textWrappingMode = TMPro.TextWrappingModes.NoWrap; label.overflowMode = TMPro.TextOverflowModes.Overflow; labels++;
    }
    return had + "->" + list.arraySize + " popups, " + labels + " labels";
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
return "PASS scene " + sceneResult + "; prefab " + prefabResult;
