// Live edit: header level chip ("LEVEL 05" lavender pill) above the bold level name; coin balance sized for compact amounts.
System.Func<byte, byte, byte, float, UnityEngine.Color> C = (r, g, b, a) => new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a);
var bold = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/Poppins/Poppins-Bold SDF.asset");
var pill = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Pill.png");
var topLeft = new UnityEngine.Vector2(.5f, 1); var leftMid = new UnityEngine.Vector2(0, .5f); var mid = new UnityEngine.Vector2(.5f, .5f);
System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var hud = canvas.GetComponent<TapBlast.GameHUD>(); var data = new UnityEditor.SerializedObject(hud);
    var group = canvas.transform.Find("Device safe area/Portrait frame/HUD/Header/Level title");
    // Chip: lavender pill at the left edge of the title column.
    var chipT = group.Find("Level chip");
    var chipGo = chipT != null ? chipT.gameObject : new UnityEngine.GameObject("Level chip", typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    chipGo.transform.SetParent(group, false); chipGo.transform.SetSiblingIndex(0);
    var chip = chipGo.GetComponent<UnityEngine.UI.Image>(); chip.sprite = pill; chip.type = UnityEngine.UI.Image.Type.Sliced; chip.raycastTarget = false; chip.preserveAspect = true;
    chip.pixelsPerUnitMultiplier = 128f / 40; chip.color = UnityEngine.Color.white;
    var grad = chipGo.GetComponent<TapBlast.UIGradient>() ?? chipGo.AddComponent<TapBlast.UIGradient>(); grad.Set(C(240, 236, 255, 1), C(226, 219, 255, 1), true);
    var cr = (UnityEngine.RectTransform)chipGo.transform; cr.anchorMin = cr.anchorMax = topLeft; cr.pivot = leftMid;
    cr.anchoredPosition = new UnityEngine.Vector2(-340, -62); cr.sizeDelta = new UnityEngine.Vector2(140, 40); cr.localScale = UnityEngine.Vector3.one;
    var labelT = chipGo.transform.Find("Label");
    var labelGo = labelT != null ? labelT.gameObject : new UnityEngine.GameObject("Label", typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
    labelGo.transform.SetParent(chipGo.transform, false);
    var label = labelGo.GetComponent<TMPro.TextMeshProUGUI>(); label.font = bold; label.fontSharedMaterial = bold.material;
    label.text = "LEVEL 01"; label.fontSize = 22; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 22; label.characterSpacing = 6;
    label.color = C(96, 70, 214, 1); label.alignment = TMPro.TextAlignmentOptions.Center; label.textWrappingMode = TMPro.TextWrappingModes.NoWrap; label.raycastTarget = false;
    var lr = label.rectTransform; lr.anchorMin = UnityEngine.Vector2.zero; lr.anchorMax = UnityEngine.Vector2.one; lr.pivot = mid;
    lr.offsetMin = new UnityEngine.Vector2(12, 0); lr.offsetMax = new UnityEngine.Vector2(-12, 2);
    // Name: one bold line under the chip; shrinks for long names.
    var name = group.Find("Level title").GetComponent<TMPro.TMP_Text>(); name.text = "First Spark";
    name.fontSize = 48; name.enableAutoSizing = true; name.fontSizeMin = 30; name.fontSizeMax = 48; name.lineSpacing = 0; name.characterSpacing = -0.5f;
    name.alignment = TMPro.TextAlignmentOptions.MidlineLeft; name.textWrappingMode = TMPro.TextWrappingModes.NoWrap; name.overflowMode = TMPro.TextOverflowModes.Ellipsis;
    var nr = name.rectTransform; nr.anchorMin = nr.anchorMax = topLeft; nr.pivot = leftMid;
    nr.anchoredPosition = new UnityEngine.Vector2(-340, -116); nr.sizeDelta = new UnityEngine.Vector2(540, 62);
    data.FindProperty("levelChip").objectReferenceValue = label;
    data.ApplyModifiedPropertiesWithoutUndo();
    // Coin balance: one line that shrinks before it ever overflows the pill (amounts are compact K/M/B past 99,999).
    // Pill 280 wide (right margin stays 40, matching the pause button); coin and plus move out to give the amount room.
    var wallet = (UnityEngine.RectTransform)canvas.transform.Find("Device safe area/Portrait frame/HUD/Header/Wallet shortcut");
    wallet.anchoredPosition = new UnityEngine.Vector2(360, wallet.anchoredPosition.y); wallet.sizeDelta = new UnityEngine.Vector2(280, 96);
    foreach (var n in new[] { "Coin halo", "Coin" }) { var r = (UnityEngine.RectTransform)wallet.Find(n); r.anchoredPosition = new UnityEngine.Vector2(-90, 0); }
    ((UnityEngine.RectTransform)wallet.Find("Plus")).anchoredPosition = new UnityEngine.Vector2(90, 0);
    var balance = wallet.Find("Balance").GetComponent<TMPro.TMP_Text>();
    balance.textWrappingMode = TMPro.TextWrappingModes.NoWrap; balance.enableAutoSizing = true; balance.fontSizeMin = 22; balance.fontSizeMax = 34;
    balance.rectTransform.anchoredPosition = new UnityEngine.Vector2(0, 2); balance.rectTransform.sizeDelta = new UnityEngine.Vector2(108, 56);
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
return "PASS level chip + coin pill in scene (saved) and prefab";
