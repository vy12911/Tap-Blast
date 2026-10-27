// Live edit: level briefing page. A hero card that fills the free height (target on an indigo
// stage, goal below), then a bottom stack: star rules, rewards, personal-best pill, Let's Play.
System.Func<byte, byte, byte, UnityEngine.Color> C = (r, g, b) => new UnityEngine.Color32(r, g, b, 255);
var tile = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Tile.png");
var pill = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Pill.png");
var glow = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/UI/Modern/Glow.png");
var top = new UnityEngine.Vector2(.5f, 1); var bottom = new UnityEngine.Vector2(.5f, 0); var mid = new UnityEngine.Vector2(.5f, .5f);
System.Action<UnityEngine.RectTransform, UnityEngine.Vector2, float, float, float, float> pin = (r, a, x, y, w, h) =>
{
    r.anchorMin = r.anchorMax = a; r.pivot = mid; r.anchoredPosition = new UnityEngine.Vector2(x, y);
    r.sizeDelta = new UnityEngine.Vector2(w, h); r.localScale = UnityEngine.Vector3.one;
};
// Fixed width, stretched between a top inset and a bottom inset of its parent.
System.Action<UnityEngine.RectTransform, float, float, float> column = (r, w, t, b) =>
{
    r.anchorMin = new UnityEngine.Vector2(.5f, 0); r.anchorMax = new UnityEngine.Vector2(.5f, 1); r.pivot = mid; r.localScale = UnityEngine.Vector3.one;
    r.sizeDelta = new UnityEngine.Vector2(w, -(t + b)); r.anchoredPosition = new UnityEngine.Vector2(0, (b - t) / 2);
};
System.Func<UnityEngine.Transform, string, UnityEngine.Sprite, UnityEngine.UI.Image> image = (parent, name, sprite) =>
{
    var found = parent.Find(name);
    var go = found != null ? found.gameObject : new UnityEngine.GameObject(name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer), typeof(UnityEngine.UI.Image));
    go.transform.SetParent(parent, false);
    var img = go.GetComponent<UnityEngine.UI.Image>(); img.sprite = sprite; img.raycastTarget = false; img.preserveAspect = true;
    img.type = sprite == glow ? UnityEngine.UI.Image.Type.Simple : UnityEngine.UI.Image.Type.Sliced; img.color = UnityEngine.Color.white; return img;
};
System.Action<UnityEngine.UI.Graphic, UnityEngine.Color, UnityEngine.Color, bool> gradient = (g, a, b, v) =>
{
    var e = g.GetComponent<TapBlast.UIGradient>() ?? g.gameObject.AddComponent<TapBlast.UIGradient>(); e.Set(a, b, v);
};
System.Action<TMPro.TMP_Text, float, float> size = (t, max, min) => { t.fontSize = t.fontSizeMax = max; t.fontSizeMin = min; };

// Bottom stack, measured up from the screen bottom (reference px).
const float Cta = 40 + 64, BestY = 40 + 128 + 20 + 36, RewardY = 40 + 128 + 20 + 72 + 20 + 80, RulesY = 40 + 128 + 20 + 72 + 20 + 160 + 20 + 150;
const float HeroTop = 356, HeroBottom = 40 + 128 + 20 + 72 + 20 + 160 + 20 + 300 + 24;

System.Action<UnityEngine.GameObject> apply = canvas =>
{
    var page = canvas.transform.Find("Device safe area/Portrait frame/Screens/Level briefing/Level briefing card");
    // Hero card stretches into the free height; the target sits on an indigo stage inside it.
    var heroCard = (UnityEngine.RectTransform)page.Find("Hero/Hero card");
    column(heroCard, 1000, HeroTop, HeroBottom);
    var heroShadow = (UnityEngine.RectTransform)page.Find("Hero/Hero card shadow");
    if (heroShadow != null) { column(heroShadow, 1096, HeroTop - 48 + 14, HeroBottom - 48 - 14); }
    var stage = image(heroCard, "Stage", tile); stage.transform.SetAsFirstSibling();
    var sr = stage.rectTransform; sr.anchorMin = UnityEngine.Vector2.zero; sr.anchorMax = UnityEngine.Vector2.one; sr.pivot = mid;
    sr.offsetMin = new UnityEngine.Vector2(16, 184); sr.offsetMax = new UnityEngine.Vector2(-16, -16);
    gradient(stage, C(58, 70, 168), C(104, 66, 178), true);
    var halo = image(stage.transform, "Halo", glow); pin(halo.rectTransform, mid, 0, 0, 560, 560); halo.color = new UnityEngine.Color(1, 1, 1, .32f);
    var hero = (UnityEngine.RectTransform)page.Find("Hero/Hero target"); hero.SetParent(stage.transform, false); pin(hero, mid, 0, 0, 280, 280);
    var preview = (UnityEngine.RectTransform)page.Find("Hero/Target size preview"); preview.SetParent(stage.transform, false); pin(preview, mid, 0, 0, 810, 270);
    halo.transform.SetAsFirstSibling();
    // Goal and how-to sit on the white lower band of the hero card.
    var goal = (UnityEngine.RectTransform)page.Find("Goal/Goal"); pin(goal, bottom, 0, HeroBottom + 124, 920, 60);
    size(goal.GetComponent<TMPro.TMP_Text>(), 40, 26);
    var desc = (UnityEngine.RectTransform)page.Find("Goal/Description"); pin(desc, bottom, 0, HeroBottom + 56, 900, 76);
    size(desc.GetComponent<TMPro.TMP_Text>(), 28, 20);
    // Star rules: larger tiles and readable text.
    var rules = (UnityEngine.RectTransform)page.Find("Star rules/Mastery card"); pin(rules, bottom, 0, RulesY, 1000, 300);
    var rulesShadow = (UnityEngine.RectTransform)page.Find("Star rules/Mastery card shadow"); if (rulesShadow != null) pin(rulesShadow, bottom, 0, RulesY - 14, 1096, 396);
    string[] texts = { "Star rules", "Two star requirement", "Three star requirement" };
    string[][] stars = { new[] { "0-0" }, new[] { "1-0", "1-1" }, new[] { "2-0", "2-1", "2-2" } };
    for (int i = 0; i < 3; i++)
    {
        float x = -324 + i * 324;
        pin((UnityEngine.RectTransform)page.Find("Star rules/Rule tile " + (i + 1)), bottom, x, RulesY, 300, 260);
        var label = (UnityEngine.RectTransform)page.Find("Star rules/" + texts[i]); pin(label, bottom, x, RulesY - 44, 276, 140);
        size(label.GetComponent<TMPro.TMP_Text>(), 36, 22);
        for (int s = 0; s < stars[i].Length; s++)
            pin((UnityEngine.RectTransform)page.Find("Star rules/Mastery star " + stars[i][s]), bottom, x + (s - (stars[i].Length - 1) / 2f) * 66, RulesY + 72, 62, 62);
    }
    // Rewards: bigger icons and numbers.
    var reward = (UnityEngine.RectTransform)page.Find("Reward/Reward background"); pin(reward, bottom, 0, RewardY, 1000, 160);
    var rewardShadow = (UnityEngine.RectTransform)page.Find("Reward/Reward background shadow"); if (rewardShadow != null) pin(rewardShadow, bottom, 0, RewardY - 14, 1096, 256);
    pin((UnityEngine.RectTransform)page.Find("Reward/Coins chip"), bottom, -242, RewardY, 468, 120);
    pin((UnityEngine.RectTransform)page.Find("Reward/XP chip"), bottom, 242, RewardY, 468, 120);
    pin((UnityEngine.RectTransform)page.Find("Reward/Reward coin"), bottom, -410, RewardY, 84, 84);
    pin((UnityEngine.RectTransform)page.Find("Reward/Reward XP"), bottom, 74, RewardY, 88, 88);
    var coins = (UnityEngine.RectTransform)page.Find("Reward/Reward preview"); pin(coins, bottom, -188, RewardY, 320, 70); size(coins.GetComponent<TMPro.TMP_Text>(), 42, 26);
    var xp = (UnityEngine.RectTransform)page.Find("Reward/XP reward"); pin(xp, bottom, 296, RewardY, 320, 70); size(xp.GetComponent<TMPro.TMP_Text>(), 42, 26);
    // Personal best as a pill, not a footnote.
    var walletSection = page.Find("Wallet");
    var bestChip = image(walletSection, "Best chip", pill); bestChip.transform.SetAsFirstSibling();
    pin(bestChip.rectTransform, bottom, 0, BestY, 700, 72); bestChip.color = new UnityEngine.Color32(255, 255, 255, 230);
    bestChip.pixelsPerUnitMultiplier = 128f / 72;
    var best = (UnityEngine.RectTransform)page.Find("Wallet/Best"); pin(best, bottom, 0, BestY, 660, 56);
    var bestText = best.GetComponent<TMPro.TMP_Text>(); size(bestText, 30, 20); bestText.color = new UnityEngine.Color32(107, 113, 144, 255);
    pin((UnityEngine.RectTransform)page.Find("Actions/Start level"), bottom, 0, Cta, 1000, 128);
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
return "PASS briefing page rebuilt in scene (saved) and prefab";
