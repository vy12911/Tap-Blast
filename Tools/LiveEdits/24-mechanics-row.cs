// Live edit: Mechanics row (CHAINS / MEGA / BOMBS status + Shield and Ultimate pills).
// The pills were offset 30 px below the status line, their labels (280x60) were larger than the pills, and the status
// text rect ran underneath both buttons. Now: one 1000-wide row centred in the Mechanics band, status text on the left
// (right margin reserves the button area), Shield and Ultimate right-aligned, vertically centred, 64 tall with fully
// round ends, labels inset inside each pill and auto-sized on one line, Ultimate charge line inset along the bottom.
// Safe to re-run.
const float RowWidth = 1000, RowHeight = 64, PillHeight = 64, ShieldWidth = 220, UltimateWidth = 260, Gap = 14;
System.Func<UnityEngine.GameObject, string> apply = root =>
{
    var status = root.transform.Find("Device safe area/Portrait frame/HUD/Mechanics/Mechanics status") as UnityEngine.RectTransform;
    if (status == null) return "FAIL: Mechanics status not found";
    var shield = status.Find("Shield") as UnityEngine.RectTransform;
    var ultimate = status.Find("Ultimate control") as UnityEngine.RectTransform;
    if (shield == null || ultimate == null) return "FAIL: Shield / Ultimate control not found";

    // Row: centred, full card width, the band's centre line.
    status.anchorMin = status.anchorMax = new UnityEngine.Vector2(.5f, 1); status.pivot = new UnityEngine.Vector2(.5f, .5f);
    status.anchoredPosition = new UnityEngine.Vector2(0, status.anchoredPosition.y); status.sizeDelta = new UnityEngine.Vector2(RowWidth, RowHeight);
    var text = status.GetComponent<TMPro.TMP_Text>();
    text.margin = new UnityEngine.Vector4(8, 0, UltimateWidth + Gap + ShieldWidth + 20, 0);
    text.enableAutoSizing = true; text.fontSizeMax = 30; text.fontSizeMin = 20;
    text.alignment = TMPro.TextAlignmentOptions.MidlineLeft; text.textWrappingMode = TMPro.TextWrappingModes.Normal;

    // Pressure label/pips (scripted levels only) share the centre line instead of sitting below it.
    foreach (UnityEngine.Transform c in status)
        if (c.name.StartsWith("Pressure")) { var r = (UnityEngine.RectTransform)c; r.anchoredPosition = new UnityEngine.Vector2(r.anchoredPosition.x, 0); }

    System.Action<UnityEngine.RectTransform, float, float> pill = (r, width, x) =>
    {
        r.anchorMin = r.anchorMax = new UnityEngine.Vector2(1, .5f); r.pivot = new UnityEngine.Vector2(.5f, .5f);
        r.anchoredPosition = new UnityEngine.Vector2(x, 0); r.sizeDelta = new UnityEngine.Vector2(width, PillHeight); r.localScale = UnityEngine.Vector3.one;
        var img = r.GetComponent<UnityEngine.UI.Image>();
        if (img != null && img.sprite != null && img.type == UnityEngine.UI.Image.Type.Sliced)
            img.pixelsPerUnitMultiplier = (img.sprite.border.y + img.sprite.border.w) / PillHeight; // exact half-circle ends
    };
    pill(ultimate, UltimateWidth, -UltimateWidth / 2);
    pill(shield, ShieldWidth, -UltimateWidth - Gap - ShieldWidth / 2);

    System.Action<UnityEngine.RectTransform, float, float, float> label = (r, maxSize, bottom, top) =>
    {
        var t = r.GetComponentInChildren<TMPro.TMP_Text>(true); var lr = t.rectTransform;
        lr.anchorMin = UnityEngine.Vector2.zero; lr.anchorMax = UnityEngine.Vector2.one; lr.pivot = new UnityEngine.Vector2(.5f, .5f);
        lr.offsetMin = new UnityEngine.Vector2(18, bottom); lr.offsetMax = new UnityEngine.Vector2(-18, -top);
        t.margin = UnityEngine.Vector4.zero; t.enableAutoSizing = true; t.fontSizeMax = maxSize; t.fontSizeMin = 16;
        t.alignment = TMPro.TextAlignmentOptions.Center; t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        t.overflowMode = TMPro.TextOverflowModes.Overflow; t.raycastTarget = false;
    };
    label(shield, 26, 6, 6);
    label(ultimate, 26, 12, 4); // leaves room for the charge line

    var fill = ultimate.Find("Charge fill") as UnityEngine.RectTransform;
    if (fill != null)
    {
        fill.anchorMin = new UnityEngine.Vector2(0, 0); fill.anchorMax = new UnityEngine.Vector2(1, 0); fill.pivot = new UnityEngine.Vector2(.5f, 0);
        fill.offsetMin = new UnityEngine.Vector2(30, 8); fill.offsetMax = new UnityEngine.Vector2(-30, 13);
    }
    return "row " + status.sizeDelta + " shield " + shield.anchoredPosition + " ultimate " + ultimate.anchoredPosition;
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
