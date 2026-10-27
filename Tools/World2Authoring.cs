// Editor-only authoring utility. Run in the disposable validation project;
// copy the reviewed saved prefabs back afterwards. Never runs during gameplay.
using TMPro;
using TapBlast;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class World2Authoring
{
    static void Bind(SerializedObject data, string name, Object value) => data.FindProperty(name).objectReferenceValue = value;
    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
    }
    static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TMP_Text>(); label.font = font; label.text = text;
        label.fontSize = fontSize; label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false;
        Place(label.rectTransform, position, size); return label;
    }
    static Button Button(Transform parent, string name, TMP_FontAsset font, string text, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false); Place((RectTransform)go.transform, position, size);
        go.GetComponent<Image>().color = new Color32(28, 67, 87, 255);
        var button = go.GetComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Label(go.transform, "Label", font, text, Vector2.zero, size - new Vector2(12, 6), 25);
        return button;
    }
    public static void Run()
    {
        const string targetPath = "Assets/Prefabs/Targets/NormalTarget.prefab";
        var targetRoot = PrefabUtility.LoadPrefabContents(targetPath);
        var target = targetRoot.GetComponent<TargetView>(); var data = new SerializedObject(target);
        var golden = (GameObject)data.FindProperty("goldenSymbol").objectReferenceValue;
        var font = golden.GetComponent<TMP_Text>().font;
        if (data.FindProperty("chainSymbol").objectReferenceValue == null)
        {
            var chain = Label(targetRoot.transform, "Chain links", font, "LINK", new Vector2(0, -22), new Vector2(112, 42), 26);
            var ring = ((Image)data.FindProperty("lifetimeRing").objectReferenceValue).sprite;
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Link ring " + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(chain.transform, false);
                Place((RectTransform)go.transform, new Vector2(i == 0 ? -13 : 13, 40), new Vector2(42, 30));
                var img = go.GetComponent<Image>(); img.sprite = ring; img.color = Color.white; img.raycastTarget = false;
            }
            Bind(data, "chainSymbol", chain.gameObject); chain.gameObject.SetActive(false);
        }
        if (data.FindProperty("bombSymbol").objectReferenceValue == null)
        {
            var bomb = Label(targetRoot.transform, "Bomb warning", font, "<size=48>X</size>\nBOMB", Vector2.zero, new Vector2(112, 106), 25);
            Bind(data, "bombSymbol", bomb.gameObject); bomb.gameObject.SetActive(false);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(targetRoot, targetPath); PrefabUtility.UnloadPrefabContents(targetRoot);

        const string canvasPath = "Assets/Prefabs/UI/GameCanvas.prefab";
        var root = PrefabUtility.LoadPrefabContents(canvasPath);
        var hud = root.GetComponent<GameHUD>(); data = new SerializedObject(hud);
        var frame = root.transform.Find("Device safe area/Portrait frame");
        var feedbackRoot = frame.Find("Pooled feedback") ?? frame.Find("HUD/Arena/Pooled feedback");
        if (feedbackRoot.GetComponent<RectMask2D>() == null) feedbackRoot.gameObject.AddComponent<RectMask2D>();
        font = ((TMP_Text)data.FindProperty("worldLabel").objectReferenceValue).font;
        var select = ((GameObject)data.FindProperty("levelSelect").objectReferenceValue).transform;
        foreach (var label in select.GetComponentsInChildren<TMP_Text>(true))
        {
            if (label.name == "World") Bind(data, "selectionWorld", label);
            if (label.name == "Title") Bind(data, "selectionTitle", label);
        }
        var intro = (GameObject)data.FindProperty("intro").objectReferenceValue;
        foreach (var label in intro.GetComponentsInChildren<TMP_Text>(true))
            if (label.name == "Eyebrow") Bind(data, "introWorld", label);
        foreach (string field in new[] { "introGoal", "description", "starRules", "countdownGoal", "introName", "selectionWallet", "hint", "subhint", "resultStats", "resultHint", "objective" })
        {
            var label = (TMP_Text)data.FindProperty(field).objectReferenceValue;
            label.enableAutoSizing = true; label.fontSizeMin = 20; label.fontSizeMax = label.fontSize;
        }
        ((TMP_Text)data.FindProperty("countdownGoal").objectReferenceValue).rectTransform.sizeDelta = new Vector2(525, 120);
        // World page buttons were retired; LevelListAuthoring saves one twenty-row list.
        if (data.FindProperty("<Shield>k__BackingField").objectReferenceValue == null)
        {
            var shield = Button(frame, "Shield", font, "SHIELD 1", new Vector2(340, 450), new Vector2(252, 64));
            Bind(data, "<Shield>k__BackingField", shield); Bind(data, "shieldLabel", shield.GetComponentInChildren<TMP_Text>());
            shield.gameObject.SetActive(false);
            var status = Label(frame, "Mechanics status", font, "CHAINS 0   MEGA 0   BOMBS 0", new Vector2(-110, 450), new Vector2(630, 48), 24);
            Bind(data, "mechanicStatus", status); status.gameObject.SetActive(false);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, canvasPath); PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        System.IO.File.WriteAllText("world2-authoring.txt", "Saved Chain/Bomb symbols, Shield and progress bindings.\n");
        EditorApplication.Exit(0);
    }
}
