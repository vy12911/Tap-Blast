// Live edit: target labels fit comfortably inside the ball for the wide Poppins font.
// GOLD / +3s / CORE auto-size within the middle 64% x 34% of the target (scales with each level's
// target diameter); BOMB gets a smaller box and size.
const string path = "Assets/Prefabs/Targets/NormalTarget.prefab";
var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
var sb = new System.Text.StringBuilder();
foreach (var t in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
{
    var r = t.rectTransform;
    if (t.name == "GOLD symbol" || t.name == "+3s symbol" || t.name == "CORE symbol")
    {
        r.anchorMin = new UnityEngine.Vector2(.18f, .33f); r.anchorMax = new UnityEngine.Vector2(.82f, .67f);
        r.offsetMin = r.offsetMax = UnityEngine.Vector2.zero;
        t.enableAutoSizing = true; t.fontSizeMax = 44; t.fontSizeMin = 12; t.fontSize = 44;
        t.characterSpacing = -2; t.alignment = TMPro.TextAlignmentOptions.Center; t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        sb.Append(t.name + " ");
    }
    else if (t.name == "Bomb warning")
    {
        r.sizeDelta = new UnityEngine.Vector2(92, 84);
        t.text = "<size=44>X</size>\nBOMB"; t.fontSizeMax = 20; t.fontSizeMin = 12; t.fontSize = 20; t.lineSpacing = -18;
        sb.Append("Bomb ");
    }
    else if (t.name == "Chain links")
    {
        r.sizeDelta = new UnityEngine.Vector2(92, 36); t.fontSizeMax = 22; t.fontSizeMin = 12; t.fontSize = 22;
        sb.Append("Link ");
    }
}
UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, path); UnityEditor.PrefabUtility.UnloadPrefabContents(root);
UnityEditor.AssetDatabase.SaveAssets();
return "PASS resized target labels: " + sb;
