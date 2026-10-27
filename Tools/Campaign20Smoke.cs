// Runs only in Temp/Campaign20Validation, with an isolated save identity.
using System;
using System.IO;
using System.Reflection;
using TapBlast;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Campaign20Smoke
{
    const string Key = "TapBlast.Campaign20Smoke";
    const string Report = "campaign20-results.txt";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static GameManager game;
    static GameHUD hud;
    static int checks, stage;
    static int inputLevel = 11;
    static double started, next;
    static Campaign20Smoke()
    {
        if (!SessionState.GetBool(Key, false)) return;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update += Step;
        Application.logMessageReceived += OnLog;
    }
    public static void Run()
    {
        PlayerSettings.companyName = "TapBlastValidation";
        PlayerSettings.productName = "IsolatedCampaign20";
        File.WriteAllText(Report, "");
        PlayerPrefs.DeleteKey("TapBlast.v2.Progress");
        PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Best");
        PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Stars"); ProgressStore.Invalidate();
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    public static void VerifyPersistence()
    {
        try
        {
            PlayerSettings.companyName = "TapBlastValidation";
            PlayerSettings.productName = "IsolatedCampaign20";
            int coins = 0, xp = 0;
            for (int id = 1; id <= 20; id++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset");
                coins += config.CoinReward; xp += config.XPReward;
                Check(ProgressStore.GetBestStars(id) == 3 && ProgressStore.GetBestCombo(id) >= config.ThreeStarCombo && ProgressStore.GetBestScore(id) >= config.TwoStarScore,
                    "Fresh process restores level " + id + " stars, score and combo");
            }
            Check(ProgressStore.Coins == coins && ProgressStore.XP == xp && ProgressStore.IsUnlocked(20), "Fresh process restores wallet, XP and campaign unlocks");
            Check(ProgressStore.SelectedLevel == 13 && ProgressStore.SelectLevel(1) && ProgressStore.SelectedLevel == 1, "Saved selection restores and World 1 can be selected after World 2");
            ProgressStore.SelectLevel(13);
            File.WriteAllText("campaign20-persistence.txt", "PASS All 20 records, wallet, XP, unlocks and selected level survive a fresh Unity process.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.WriteAllText("campaign20-persistence.txt", "FAIL " + e);
            EditorApplication.Exit(1);
        }
    }
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; File.AppendAllText(Report, "PASS " + message + "\n");
    }
    static void Finish(bool ok, string message)
    {
        SessionState.SetBool(Key, false); EditorApplication.update -= Step;
        Application.logMessageReceived -= OnLog;
        File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n" + checks + " checks\n");
        EditorApplication.Exit(ok ? 0 : 1);
    }
    static void OnLog(string message, string trace, LogType type)
    {
        if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + trace);
    }
    static TargetView[] Targets() => UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
    static TargetView ChainTarget()
    {
        var buffer = new TargetView[12]; var pool = Field<TargetPool>(game, "pool");
        TargetView best = null; int count = -1;
        foreach (var t in Targets()) if (t.Kind == TargetKind.Chain && !t.BlastPending)
        { int matches = pool.FindChain(t, buffer); if (matches > count) { best = t; count = matches; } }
        return best ?? throw new Exception("Missing color target");
    }
    static void Tap(TargetView target) => target.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
    static void Begin(bool populateColors = true)
    {
        game.BeginCountdown(); typeof(GameManager).GetField("countdown", Private).SetValue(game, 0f);
        Call(game, "Update"); Canvas.ForceUpdateCanvases();
        if (populateColors && game.CurrentLevel.LevelId > 10)
        {
            var pool = Field<TargetPool>(game, "pool"); var spawner = Field<TargetSpawner>(game, "spawner");
            for (int i = 0; i < 60 && pool.ActiveCount - pool.BombCount < game.CurrentLevel.ClusterSize + 2; i++)
            { spawner.Tick(.05f, false); pool.Tick(.05f, false); game.Session.Tick(.05f); }
        }
    }
    static void Complete() { game.Session.Tick(game.Session.RemainingTime + 1); Call(game, "Finish"); }
    const string HudPath = "Device safe area/Portrait frame/HUD/";
    // Section CanvasGroups hide whole HUD areas; multiply alpha up the hierarchy.
    static float VisibleAlpha(Transform t)
    {
        float alpha = 1;
        for (; t != null; t = t.parent)
        {
            var group = t.GetComponent<CanvasGroup>();
            if (group == null) continue;
            alpha *= group.alpha;
            if (group.ignoreParentGroups) break;
        }
        return alpha;
    }
    static Rect FrameRect(RectTransform rect)
    {
        var frame = (RectTransform)hud.transform.Find("Device safe area/Portrait frame"); var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Vector2 min = frame.InverseTransformPoint(corners[0]), max = frame.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    static void TextFits(string field)
    {
        var label = Field<TMP_Text>(hud, field); label.ForceMeshUpdate(true);
        Check(!label.isTextOverflowing, "Text fits: " + field + " in level " + game.CurrentLevel.LevelId);
    }
    static void VerifyBindings()
    {
        var configs = Field<LevelConfig[]>(game, "levels");
        Check(configs.Length == 20, "All 20 configs are saved scene references without Editor fallback");
        for (int i = 0; i < 20; i++) Check(configs[i] != null && configs[i].LevelId == i + 1, "Build reference for level " + (i + 1));
        var expected = new[] { ObjectiveKind.Chains, ObjectiveKind.Chains, ObjectiveKind.MegaChains, ObjectiveKind.Score, ObjectiveKind.Combo,
            ObjectiveKind.NoBombs, ObjectiveKind.Shield, ObjectiveKind.ChainsNoBombs, ObjectiveKind.MegaChains, ObjectiveKind.ChainMaster };
        var counts = new[] { 2, 3, 1, 1, 20, 1, 1, 3, 2, 5 };
        for (int i = 0; i < 10; i++)
        {
            var c = configs[i + 10];
            Check(c.Objective == expected[i] && c.ObjectiveCount == counts[i], "Level " + c.LevelId + " matches GDD objective");
            Check(c.ClusterSize >= 3 && c.MaxTargets >= c.ClusterSize + (c.BombChance > 0 ? 1 : 0) + 2, "Pool fits complete cluster and safe Bomb in level " + c.LevelId);
        }
        for (int i = 16; i < 20; i++) Check(configs[i].BombChance > configs[i - 1].BombChance, "Bomb probability increases at level " + (i + 1));
        Check(configs[13].TargetScore == 3000 && configs[19].RequiredCombo == 30, "GDD level 14 score and Chain Master combo requirements");
        var serialized = new SerializedObject(hud); var binding = serialized.GetIterator();
        while (binding.Next(true))
            if (binding.propertyType == SerializedPropertyType.ObjectReference && !binding.propertyPath.StartsWith("m_"))
                Check(binding.objectReferenceValue != null, "Saved HUD binding " + binding.propertyPath);
        foreach (var text in hud.GetComponentsInChildren<TMP_Text>(true))
            Check(text.font != null && !text.raycastTarget, "TMP font and nonblocking label: " + text.name);
        Check(Field<GameObject>(hud.TargetPrefab, "chainSymbol") != null && Field<GameObject>(hud.TargetPrefab, "bombSymbol") != null, "Target prefab contains distinct LINK and X BOMB symbols");
        Check(hud.transform.Find(HudPath + "Arena/Pooled feedback").GetComponent<RectMask2D>() != null, "Saved feedback mask keeps Mega rings inside the arena");
        Check(!game.SelectLevel(11), "Level 11 is locked on a fresh save");
        game.ShowLevels();
        var rows = Field<Button[]>(hud, "levelButtons");
        Check(rows.Length == 20 && rows[0].interactable && !rows[10].interactable, "Single level list shows level 11 locked on a fresh save");
    }
    static void VerifyCampaign()
    {
        int objectCount = hud.GetComponentsInChildren<RectTransform>(true).Length;
        int coins = 0, xp = 0;
        for (int id = 1; id <= 20; id++)
        {
            Check(game.SelectLevel(id), "Unlocked routing to level " + id);
            TextFits("introGoal"); TextFits("description"); TextFits("starRules"); TextFits("introName");
            foreach (var rule in new[] { "oneStar", "twoStars", "threeStars" })
            {
                var ruleText = Field<TMP_Text>(hud.GetComponent<GuiBriefing>(), rule); ruleText.ForceMeshUpdate(true);
                Check(!ruleText.isTextOverflowing, "Star rule fits: " + rule + " in level " + id);
            }
            Check(Field<TMP_Text>(hud, "introGoal").GetParsedText() == game.CurrentLevel.GoalText && Field<TMP_Text>(hud, "description").text.Contains(game.CurrentLevel.WinRule),
                "Briefing states the exact goal and win rule for level " + id);
            if (id == 1 || id == 11 || id == 16 || id == 17) { Canvas.ForceUpdateCanvases(); UnitySmoke.Capture(hud, "briefing-" + id.ToString("00")); }
            Begin(); if (id == 17) { Canvas.ForceUpdateCanvases(); UnitySmoke.Capture(hud, "playing-17"); }
            TextFits("countdownGoal"); TextFits("hint"); TextFits("subhint"); TextFits("objective"); TextFits("worldLabel"); TextFits("levelChip");
            if(id>10) {
                TextFits("mechanicStatus");
                var status=FrameRect(Field<TMP_Text>(hud,"mechanicStatus").rectTransform);
                var fill=FrameRect(Field<Image>(hud,"progressFill").rectTransform);
                var rim=FrameRect((RectTransform)hud.transform.Find(HudPath+"Arena/Arena rim"));
                Check(status.yMin>rim.yMax && status.yMax<fill.yMin,"World 2 status stays between progress track and arena rim");
            }
            Check(Field<TMP_Text>(hud, "mechanicStatus").gameObject.activeSelf == (id > 10) && hud.Shield.gameObject.activeSelf == (id >= 17), "Relevant mechanics are visible immediately when level " + id + " starts");
            var config = game.CurrentLevel;
            var pool = Field<TargetPool>(game, "pool"); var spawner = Field<TargetSpawner>(game, "spawner");
            float time = game.Session.RemainingTime;
            game.Pause(); foreach (var t in Targets()) Tap(t); game.Session.Tick(5); game.ActivateShield();
            Check(!Field<TMP_Text>(hud,"mechanicStatus").gameObject.activeSelf && !hud.Shield.gameObject.activeSelf && VisibleAlpha(hud.Arena)==0,"Pause hides gameplay graphics immediately in level "+id);
            Check(game.Session.Score == 0 && game.Session.RemainingTime == time && !game.Session.ShieldActive, "Pause blocks taps, clock and Shield in level " + id);
            game.Resume();
            bool boundsOk = true, bombsSafe = true;
            for (int frame = 0; frame < 6000 && game.Session.State == RoundState.Playing; frame++)
            {
                var active = Targets();
                bool ready = id <= 10;
                var matches = new TargetView[12];
                foreach (var candidate in active)
                    if (pool.FindChain(candidate, matches) >= config.ClusterSize) ready = true;
                foreach (var t in active)
                {
                    if (!t.Active) continue;
                    boundsOk &= hud.Arena.rect.Contains(t.Position + Vector2.one * t.Radius) && hud.Arena.rect.Contains(t.Position - Vector2.one * t.Radius);
                    if (t.Kind == TargetKind.Bomb)
                    {
                        foreach (var other in active)
                            if (other.Active && other.Kind != TargetKind.Bomb)
                                bombsSafe &= (other.Position - t.Position).magnitude >= other.Radius + t.Radius + 15;
                        if (id == 17 && game.Session.ShieldBlocks == 0)
                        { hud.Shield.onClick.Invoke(); Tap(t); }
                        continue;
                    }
                    if (ready) Tap(t);
                }
                if (game.Session.State != RoundState.Playing) break;
                game.Session.Tick(.05f);
                if (game.Session.State != RoundState.Playing) break;
                spawner.Tick(.05f, false); pool.Tick(.05f, false);
            }
            if (game.Session.State == RoundState.Results) Call(game, "Finish");
            TextFits("resultStats"); TextFits("resultHint");
            foreach (var statTile in Field<TMP_Text[]>(hud, "resultStatTiles"))
            { statTile.ForceMeshUpdate(true); Check(!statTile.isTextOverflowing, "Stat tile fits: " + statTile.GetParsedText().Replace('\n', ' ') + " in level " + game.CurrentLevel.LevelId); }
            Check(boundsOk && bombsSafe, "All targets stay in bounds and Bombs remain separated in level " + id);
            Check(game.Session.Won && game.Session.Stars == 3, "Level " + id + " has attainable objective and three stars (score " + game.Session.Score + ", chains " + game.Session.Chains + ")");
            Check(game.Session.TimeHits <= config.MaxTimeTargets, "Time bonus budget keeps level " + id + " finite");
            coins += config.CoinReward; xp += config.XPReward;
            Check(ProgressStore.Coins == coins && ProgressStore.XP == xp && ProgressStore.GetBestStars(id) == 3 && ProgressStore.GetBestCombo(id) == game.Session.BestCombo,
                "Level " + id + " saves score, stars, best combo and rewards");
            Call(game, "Finish"); Check(ProgressStore.Coins == coins, "Duplicate result cannot pay level " + id + " twice");
            if (id < 20)
            {
                Check(hud.Next.gameObject.activeSelf && ProgressStore.IsUnlocked(id + 1), "Next level unlock " + (id + 1));
                hud.Next.onClick.Invoke();
                Check(game.CurrentLevel.LevelId == id + 1 && game.Session.State == RoundState.Ready && ProgressStore.SelectedLevel == id + 1, "Next button opens briefing " + id + " -> " + (id + 1));
            }
            else Check(!hud.Next.gameObject.activeSelf, "Chain Master results do not offer unavailable level 21");
        }
        game.ShowLevels(); Canvas.ForceUpdateCanvases();
        TextFits("selectionWallet");
        Check(UIArt.Compact(5030) == 5030.ToString("N0") && UIArt.Compact(99999) == 99999.ToString("N0") && UIArt.Compact(100000) == "100K"
            && UIArt.Compact(999999) == "999K" && UIArt.Compact(1239999) == 1.23m.ToString("0.##") + "M" && UIArt.Compact(int.MaxValue) == 2.14m.ToString("0.##") + "B",
            "Compact coin amounts keep full digits to 99,999 then truncate to K/M/B");
        var balance = Field<TMP_Text>(hud.GetComponent<GuiNavigation>(), "headerBalance");
        foreach (var amount in new[] { 99999, 888888, 88888888, int.MaxValue })
        {
            balance.text = UIArt.Compact(amount); balance.ForceMeshUpdate(true);
            Check(!balance.isTextOverflowing && balance.textBounds.size.x <= balance.rectTransform.rect.width, "Header coin pill fits " + amount.ToString("N0"));
        }
        balance.text = UIArt.Compact(ProgressStore.Coins);
        Field<TMP_Text>(hud, "selectionTitle").ForceMeshUpdate(true);
        Check(Field<TMP_Text>(hud, "selectionWorld").text == "Tap Blast" && Field<TMP_Text>(hud, "selectionTitle").GetParsedText() == "Choose a Level", "Selection header names one list without worlds");
        Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objectCount, "Campaign navigation reuses authored UI and preallocated targets");
        var rows = Field<Button[]>(hud, "levelButtons");
        for (int i = 0; i < 20; i++)
            Check(rows[i].interactable && Field<TMP_Text>(rows[i].GetComponent<GuiLevelRow>(), "number").text == (i + 1).ToString("00"), "Completed campaign lists level " + (i + 1) + " on the single page");
        var scroll = Field<ScrollRect>(hud, "selectionScroll"); var corners = new Vector3[4];
        ((RectTransform)rows[19].transform).GetWorldCorners(corners);
        bool visible = true;
        foreach (var corner in corners) visible &= scroll.viewport.rect.Contains(scroll.viewport.InverseTransformPoint(corner));
        Check(scroll.content.anchoredPosition.y > 0 && visible, "Level list opens scrolled to the current level 20 row");
    }
    static void VerifyMechanics()
    {
        game.SelectLevel(11); Begin(false);
        var spawningPool = Field<TargetPool>(game, "pool");
        Color initial = ChainTarget().Tint;
        Check(spawningPool.ActiveCount == 1, "Color lesson starts with one target rather than a fixed LINK group");
        Field<TargetSpawner>(game, "spawner").Tick(.2f, false);
        bool different = false; foreach (var t in Targets()) different |= t.Tint != initial;
        Check(spawningPool.ActiveCount == 2 && different, "Colored targets appear gradually with mixed colors");
        game.SelectLevel(13); Begin();
        var pool = Field<TargetPool>(game, "pool"); var buffer = new TargetView[12];
        var origin = ChainTarget();
        Check(pool.FindChain(origin, buffer) == 8 && pool.ActiveCount == 10, "Mega lesson offers eight of one color and two other colors");
        Check(!Field<GameObject>(origin, "chainSymbol").activeSelf && Field<GameObject>(origin, "normalSymbol").activeSelf,
            "Color-chain targets use the original target artwork without LINK labels");
        Tap(origin);
        Check(pool.ActiveCount == 9 && game.Session.Hits == 8 && game.Session.MegaChains == 1, "Only matching colors commit to the chain");
        int score = game.Session.Score;
        foreach (var t in Targets()) if (t.BlastPending) Tap(t);
        Check(game.Session.Score == score, "Pending color targets reject duplicate input");
        game.Pause(); Call(game, "Update"); Check(pool.ActiveCount == 9, "Pause freezes the color cascade");
        game.Resume(); foreach (var t in Targets()) if (t.BlastPending) Tap(t);
        Check(game.Session.Score == score, "Resume keeps committed colors noninteractive");
        pool.Tick(.08f, false);
        Check(Field<Image>(buffer[1], "lifetimeRing").color != buffer[1].Tint && Field<Image>(buffer[7], "lifetimeRing").color == buffer[7].Tint,
            "Only the next matching target heats to white");
        pool.Tick(.1f, false); Check(pool.ActiveCount == 8, "Color targets detonate individually");
        pool.Tick(1.2f, false);
        Check(pool.ActiveCount == 2 && game.Session.Misses == 0, "Other colors remain after the cascade");

        Begin(); pool.Clear();
        pool.Spawn(new Vector2(-350, -300), 150, .01f, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(350, 300), 150, .01f, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(-350, 300), 150, .01f, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(Vector2.zero, 150, 4, Color.green, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(177, 0), 150, 4, Color.cyan, false, false, TargetKind.Golden);
        pool.Spawn(new Vector2(-177, 0), 150, 4, Color.cyan, false, false, TargetKind.Time);
        pool.Spawn(new Vector2(0, -177), 150, 4, Color.cyan, false, false, TargetKind.Bomb);
        origin = ChainTarget();
        Check(pool.FindChain(origin, buffer) == 3, "Three matching colors chain across the entire board regardless of spacing");
        Tap(origin); pool.Tick(.5f, false);
        Check(pool.ActiveCount == 4 && game.Session.Hits == 3 && game.Session.Chains == 1 && game.Session.Misses == 0 && game.Session.BombHits == 0 && game.Session.GoldenHits == 0 && game.Session.TimeHits == 0,
            "Color cascade excludes other colors and special targets and survives original expiry");
        Begin(); pool.Clear();
        pool.Spawn(Vector2.zero, 150, 4, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(177, 0), 150, 4, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(-177, 0), 150, 4, Color.green, false, false, TargetKind.Chain);
        Check(pool.FindChain(ChainTarget(), buffer) == 0, "Two matching colors plus a different color cannot form a chain");
        Tap(ChainTarget()); Check(game.Session.Hits == 1 && game.Session.Chains == 0 && pool.ActiveCount == 2, "Fewer than three matching colors give an individual tap");
        Begin(); pool.Clear();
        for (int i = 0; i < 12; i++) pool.Spawn(new Vector2((i % 4 - 1.5f) * 180, (i / 4 - 1) * 180), 150, 4, Color.cyan, false, false, TargetKind.Chain);
        Check(pool.FindChain(ChainTarget(), buffer) == 12, "Every matching target participates up to full pool capacity");
        Tap(ChainTarget()); pool.Tick(0, true);
        Check(pool.ActiveCount == 0 && game.Session.Hits == 12 && game.Session.LargestChain == 12, "Full-board color chain resolves exactly once in reduced motion");
        Begin(); Tap(ChainTarget()); game.BeginCountdown(); pool.Tick(2, false);
        Check(pool.ActiveCount == 0 && game.Session.Score == 0, "Restart cancels pending color explosions");

        game.SelectLevel(18); Begin(); var member = ChainTarget(); Vector2 before = member.Position;
        pool.Tick(.5f, true);
        Check(member.Position != before && pool.FindChain(member, buffer) == 5, "Moving targets retain their color match in reduced motion");
        game.SelectLevel(16); Begin();
        foreach (var t in Targets()) if (t.Kind != TargetKind.Bomb) Tap(t);
        pool.Tick(.5f, false); Field<TargetSpawner>(game, "spawner").Tick(2.5f, false);
        Check(pool.ActiveCount == 2 && pool.BombCount == 1, "A new color begins spawning while an untouched Bomb remains");
        game.SelectLevel(17); Begin(); hud.Shield.onClick.Invoke();
        foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) { Tap(t); break; }
        Check(game.Session.ShieldBlocks == 1 && game.Session.BombHits == 0, "Shield still protects the tutorial Bomb");
        game.SelectLevel(16); Begin(); int coins = ProgressStore.Coins;
        foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) { Tap(t); break; }
        Complete(); Check(!game.Session.Won && ProgressStore.Coins == coins && !hud.Next.gameObject.activeSelf, "Bomb objective failure cannot grant rewards or show Next");
    }
    static void VerifyInputRouting()
    {
            int id = game.CurrentLevel.LevelId; var target = ChainTarget();
            var canvas = hud.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var pointer = new PointerEventData(EventSystem.current) {
                position = RectTransformUtility.WorldToScreenPoint(camera, target.transform.position),
                button = PointerEventData.InputButton.Left
            };
            var hits = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            var graphic = target.GetComponent<Image>();
            Check(hits.Count > 0, "Level " + id + " target produces a UI raycast hit (screen " + Screen.width + "x" + Screen.height + ", point " + pointer.position + ", depth " + graphic.depth + ", culled " + graphic.canvasRenderer.cull + ")");
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(game.Session.Chains == 1, "Level " + id + " chain receives input through the visible Canvas raycast hierarchy");
    }
    static void VerifyMovingBombs()
    {
        var pool = Field<TargetPool>(game, "pool");
        Vector2 previousPosition = default, previousVelocity = default;
        bool variedPosition = false, variedDirection = false;
        for (int id = 16; id <= 20; id++)
        {
            game.SelectLevel(id); Begin(); TargetView bomb = null;
            foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) bomb = t;
            Check(bomb != null && bomb.Velocity.magnitude >= 60, "Level " + id + " spawns a moving Bomb");
            Vector2 initial = bomb.Position;
            if (id > 16) { variedPosition |= (initial - previousPosition).sqrMagnitude > 1; variedDirection |= (bomb.Velocity.normalized - previousVelocity.normalized).sqrMagnitude > .01f; }
            previousPosition = initial; previousVelocity = bomb.Velocity;
            game.Pause(); Call(game, "Update"); Check(bomb.Position == initial, "Pause freezes the Bomb in level " + id); game.Resume();
            bool safe = true, moved = false; float minimumGap = float.MaxValue;
            for (int frame = 0; frame < 60; frame++)
            {
                pool.Tick(1f / 60, true);
                if (!bomb.Active) break;
                moved |= (bomb.Position - initial).sqrMagnitude > .0001f;
                safe &= hud.Arena.rect.Contains(bomb.Position + Vector2.one * bomb.Radius) && hud.Arena.rect.Contains(bomb.Position - Vector2.one * bomb.Radius);
                foreach (var t in Targets()) if (t != bomb && t.Active)
                {
                    float gap = (t.Position - bomb.Position).magnitude - t.Radius - bomb.Radius;
                    minimumGap = Mathf.Min(minimumGap, gap); safe &= gap >= 15;
                }
            }
            Check(moved && safe, "Level " + id + " Bomb moves in reduced motion while remaining in bounds and separated from targets (moved " + moved + ", safe " + safe + ", minimum gap " + minimumGap + ")");
        }
        Check(variedPosition && variedDirection, "Bomb positions and movement directions vary across rounds");
        Begin(); pool.Clear();
        pool.Spawn(Vector2.zero, 150, 4, Color.cyan, false, false, TargetKind.Chain);
        pool.Spawn(new Vector2(-230, 0), 150, 4, Color.red, false, false, TargetKind.Bomb, new Vector2(250, 0));
        TargetView moving = null; foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) moving = t;
        for (int frame = 0; frame < 30; frame++) pool.Tick(1f / 60, false);
        Check(moving.Velocity.x < 0 && moving.Position.x < -173, "Bomb reverses before crossing a valid target");
    }
    static void Capture(string name)
    {
        UnitySmoke.Capture(hud, name, 1080, 1920);
        UnitySmoke.Capture(hud, name + "-tall", 1080, 2400, new Rect(0, 84, 1080, 2196));
        UnitySmoke.Capture(hud, name + "-tablet", 1536, 2048);
    }
    static void TickHUD(float delta) => typeof(GameHUD).GetMethod("TickFeedback", Private).Invoke(hud, new object[] { delta });
    static T PopupPart<T>(object popup, string field) => (T)popup.GetType().GetField(field).GetValue(popup);
    static void VerifyAnimations()
    {
        var feedback = Field<FeedbackPlayer>(game, "feedback");
        if (feedback.ReducedMotion) hud.Motion.onClick.Invoke();
        hud.SetSettings(true, false);
        int objects = hud.GetComponentsInChildren<RectTransform>(true).Length;
        game.SelectLevel(6); Begin();
        var pool = Field<TargetPool>(game, "pool"); var golden = Targets()[0];
        var goldenRing = Field<Image>(golden, "lifetimeRing");
        pool.Tick(.25f, false); float goldenScale = goldenRing.rectTransform.localScale.x;
        UnitySmoke.Capture(hud, "animation-golden-expanded");
        pool.Tick(.5f, false);
        Check(goldenScale > goldenRing.rectTransform.localScale.x + .05f && goldenRing.fillAmount < 1, "Golden ring radiates while lifetime remains accurate");
        UnitySmoke.Capture(hud, "animation-golden-rest");
        game.Pause(); Vector3 pausedScale = goldenRing.rectTransform.localScale;
        Call(game, "Update"); TickHUD(.5f);
        Check(goldenRing.rectTransform.localScale == pausedScale, "Pause freezes target cosmetics");
        hud.Motion.onClick.Invoke();
        Check(goldenRing.rectTransform.localScale == Vector3.one && Field<Image>(golden, "glow").rectTransform.localScale == Vector3.one, "Reduced motion immediately clears target pulses while paused");
        hud.Motion.onClick.Invoke(); game.Resume();
        game.SelectLevel(16); Begin(false); TargetView bomb = null;
        foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) bomb = t;
        var bombGlow = Field<Image>(bomb, "glow"); var bombSymbol = Field<GameObject>(bomb, "bombSymbol");
        pool.Tick(.2f, false); float bombScale = bombGlow.rectTransform.localScale.x;
        UnitySmoke.Capture(hud, "animation-bomb-warning");
        pool.Tick(.33f, false);
        Check(bombScale > bombGlow.rectTransform.localScale.x + .1f && bombSymbol.activeSelf && bombSymbol.transform.localScale == Vector3.one && bomb.transform.localScale == Vector3.one, "Bomb warning pulses its glow while X BOMB and tap radius stay steady");
        pool.SetReducedMotion(true); pool.Tick(.1f, true);
        Check(bombGlow.rectTransform.localScale == Vector3.one && bombSymbol.activeSelf, "Reduced motion retains static Bomb warning identity");
        game.SelectLevel(10); Begin(); var boss = Targets()[0];
        Tap(boss); var bossRing = Field<Image>(boss, "lifetimeRing");
        Check(bossRing.color != boss.Tint && Mathf.Approximately(bossRing.fillAmount, .8f), "Core hit animates an impact and preserves exact health ring");
        pool.Tick(.3f, false);
        Check(bossRing.color == boss.Tint && boss.Active, "Core impact settles without hiding an undestroyed boss");
        game.SelectLevel(8); Begin(); Tap(Targets()[0]); TickHUD(.1f);
        Check(Field<TMP_Text>(hud, "timer").rectTransform.localScale.x > 1, "Time Bubble collection punches the timer");
        game.SelectLevel(17); Begin(); game.ActivateShield(); TickHUD(.1f);
        Check(hud.Shield.transform.localScale.x > 1, "Shield activation animates its saved button");
        game.Pause(); Vector3 shieldScale = hud.Shield.transform.localScale; TickHUD(1);
        Check(hud.Shield.transform.localScale == shieldScale, "Pause freezes Shield pulse");
        game.Resume(); foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) { Tap(t); break; }
        TickHUD(.1f);
        Check(hud.Shield.transform.localScale.x > 1 && !game.Session.ShieldActive && game.Session.ShieldBlocks == 1, "Shield block produces a one-shot confirmation");
        game.SelectLevel(13); Begin(); Tap(ChainTarget());
        var popups = Field<Array>(hud, "popups"); var first = popups.GetValue(0); var last = popups.GetValue(7);
        Check(!PopupPart<Image>(first, "Ring").enabled && PopupPart<Image>(first, "Core").enabled, "Tap starts with the shrinking core before the ring burst");
        TickHUD(.1f); pool.Tick(.1f, false);
        Check(PopupPart<Image>(first, "Ring").enabled && PopupPart<CanvasGroup>(last, "Alpha").alpha == 0, "Chain blast starts at the tapped target before distant members");
        UnitySmoke.Capture(hud, "animation-chain-start");
        game.Pause(); float delayedAge = PopupPart<float>(last, "Age"); TickHUD(.5f);
        Check(PopupPart<float>(last, "Age") == delayedAge, "Pause freezes delayed chain waves");
        game.Resume(); TickHUD(1.15f); pool.Tick(1.15f, false);
        Check(PopupPart<Image>(last, "Ring").enabled && PopupPart<CanvasGroup>(last, "Alpha").alpha > 0, "Chain wave reaches later members in sequence");
        UnitySmoke.Capture(hud, "animation-chain-wave");
        TickHUD(.8f); Check(!PopupPart<bool>(first, "Active") && !PopupPart<bool>(last, "Active"), "Chain feedback releases all pooled bursts");
        Begin(); Tap(ChainTarget()); hud.SetSettings(true, true);
        Check(PopupPart<CanvasGroup>(last, "Alpha").alpha > 0 && !PopupPart<Image>(last, "Ring").enabled && !PopupPart<Image>(last, "Core").enabled, "Reduced motion reveals pending chain score text without animated effects");
        Check(hud.Shield.transform.localScale == Vector3.one && Field<TMP_Text>(hud, "timer").rectTransform.localScale == Vector3.one, "Reduced motion settles all new HUD punches");
        hud.SetSettings(true, false);
        game.SelectLevel(20); Begin();
        var result = new RoundSession(new RoundRules(45, 20, 10, 240, 20)); result.Reset(); result.StartPlaying();
        for (int i = 0; i < 20; i++) result.Tap();
        hud.ShowResults(result, true, result.Score, new ProgressStore.RewardReceipt(0, 0, ProgressStore.Coins, ProgressStore.XP));
        TickHUD(.3f);
        Check(!hud.ResultRevealComplete && Field<CanvasGroup>(hud, "celebration").gameObject.activeSelf && Field<CanvasGroup>(hud, "rewardGroup").alpha == 0, "Results begin with confetti and score count-up before rewards");
        UnitySmoke.Capture(hud, "animation-results-start");
        TickHUD(1);
        Check(Field<Image[]>(hud, "resultStars")[0].rectTransform.localScale.x > 1 && Field<TMP_Text>(hud, "resultBest").enabled, "Personal best appears and earned stars bounce in sequence");
        UnitySmoke.Capture(hud, "animation-results-stars");
        TickHUD(1.2f);
        Check(hud.ResultRevealComplete && Field<CanvasGroup>(hud, "rewardGroup").alpha == 1, "Result animation ends with fully visible rewards");
        hud.SetSettings(true, true); hud.ShowResults(result, true, result.Score, new ProgressStore.RewardReceipt(0, 0, ProgressStore.Coins, ProgressStore.XP));
        Check(hud.ResultRevealComplete && !Field<CanvasGroup>(hud, "celebration").gameObject.activeSelf, "Reduced motion shows final results immediately without confetti");
        hud.SetSettings(true, false); game.SelectLevel(13); Begin();
        Check(Field<TMP_Text>(hud, "timer").rectTransform.localScale == Vector3.one && hud.Shield.transform.localScale == Vector3.one, "Replay resets animation transforms");
        Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objects, "Animation playback creates no new Canvas or target objects");
    }
    static void VerifyWorldEffects()
    {
        var fx = hud.WorldEffects;
        Check(fx != null && !fx.raycastTarget && fx.GetComponentInParent<RectMask2D>() != null,
            "World 2 effect graphic is authored, clipped and cannot intercept taps");
        var pool = Field<TargetPool>(game, "pool");
        int objects = hud.GetComponentsInChildren<RectTransform>(true).Length;
        for (int id = 11; id <= 20; id++)
        {
            game.SelectLevel(id); Begin();
            Check(fx.ActiveEffectCount == 0, "Level " + id + " starts without queued effects");
            if (id == 17)
            {
                game.ActivateShield();
                TickHUD(.18f);
                UnitySmoke.Capture(hud, "vfx-17-shield-active");
                foreach (var t in Targets()) if (t.Kind == TargetKind.Bomb) { Tap(t); break; }
            }
            else
            {
                if (id == 15) for (int i = 0; i < 9; i++) game.Session.Tap();
                Tap(ChainTarget());
            }
            Check(fx.ActiveEffectCount > 0, "Level " + id + " emits themed feedback");
            TickHUD(.08f); pool.Tick(.08f, false);
            UnitySmoke.Capture(hud, "vfx-" + id + "-impact");
            game.Pause(); float before = Field<float>(fx, "clock"); TickHUD(.5f);
            Check(Field<float>(fx, "clock") == before, "Level " + id + " freezes effects on pause");
            game.Resume(); TickHUD(.4f); pool.Tick(.4f, false);
            UnitySmoke.Capture(hud, "vfx-" + id + "-cascade");
            TickHUD(1); pool.Tick(1, false);
            if (id == 13 || id == 19 || id == 20) UnitySmoke.Capture(hud, "vfx-" + id + "-finale");
            TickHUD(4); pool.Tick(4, false); TickHUD(1); // Expiry puffs from leftover targets drain too.
            Check(fx.ActiveEffectCount == 0, "Level " + id + " releases all transient effects");
        }
        game.SelectLevel(15); Begin(); fx.Clear();
        fx.Hit(Vector2.zero, UIArt.Cyan, 9, 12);
        Check(fx.ActiveEffectCount > 1, "Crossing a combo milestone within a chain launches the skyline");
        fx.Clear(); fx.Hit(Vector2.zero, UIArt.Cyan, 12, 15);
        Check(fx.ActiveEffectCount == 1, "Hits within the same combo tier do not repeat the skyline");
        game.SelectLevel(13); Begin(); Tap(ChainTarget());
        hud.SetSettings(true, true);
        Check(fx.ActiveEffectCount == 0, "Reduced motion clears queued travelling links and explosions immediately");
        Begin(); Tap(ChainTarget());
        Check(fx.ActiveEffectCount == 0, "Reduced motion does not enqueue new animated effects");
        hud.SetSettings(true, false);
        Begin(); Tap(ChainTarget()); game.BeginCountdown();
        Check(fx.ActiveEffectCount == 0, "Replay cancels queued World 2 feedback");
        Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objects,
            "All ten World 2 effects reuse the authored Canvas without creating objects");
        // Pop accents, callouts and arena shake run on every level and always settle.
        game.SelectLevel(1); Begin(); fx.Clear();
        var card = (RectTransform)hud.Arena.parent; Vector2 home = card.anchoredPosition;
        Tap(Targets()[0]);
        Check(fx.ActiveEffectCount > 0, "World 1 taps emit pop accents");
        TickHUD(.07f); UnitySmoke.Capture(hud, "pop-squash");
        TickHUD(.1f); UnitySmoke.Capture(hud, "pop-burst");
        TickHUD(.15f); UnitySmoke.Capture(hud, "pop-sparks");
        hud.Callout("GREAT!", new Color32(90, 200, 255, 255)); TickHUD(.3f); UnitySmoke.Capture(hud, "pop-callout");
        hud.Shake(16, .3f); TickHUD(.05f);
        Check(card.anchoredPosition != home, "Shake offsets the arena card");
        TickHUD(1.2f); pool.Tick(1.2f, false);
        Check(card.anchoredPosition == home && fx.ActiveEffectCount == 0, "Shake returns the arena card home and pop accents drain");
        hud.Shake(16, .3f); TickHUD(.05f); game.BeginCountdown();
        Check(card.anchoredPosition == home, "Replay cancels a running shake");
        hud.SetSettings(true, true); hud.Shake(16, .3f); TickHUD(.05f);
        Check(card.anchoredPosition == home, "Reduced motion never shakes the arena");
        hud.SetSettings(true, false);
        Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objects, "Pop accents and callouts reuse the authored pool");
        // Bomb: swell, layered explosion (level 16) and shield implosion (level 17) play and drain.
        foreach (int bombLevel in new[] { 16, 17 })
        {
            game.SelectLevel(bombLevel); Begin();
            var spawner = Field<TargetSpawner>(game, "spawner"); TargetView bomb = null;
            for (int i = 0; i < 400 && bomb == null; i++)
            {
                foreach (var t in Targets()) if (t.Active && t.Kind == TargetKind.Bomb) bomb = t;
                if (bomb == null) { spawner.Tick(.05f, false); pool.Tick(.05f, false); }
            }
            Check(bomb != null, "Level " + bombLevel + " spawns a Bomb for the explosion check");
            if (bombLevel == 17) game.ActivateShield();
            fx.Clear(); Tap(bomb);
            string tag = bombLevel == 17 ? "bomb-blocked" : "bomb";
            Check(fx.ActiveEffectCount >= (bombLevel == 17 ? 5 : 8), "Bomb " + (bombLevel == 17 ? "block" : "explosion") + " layers its effects");
            TickHUD(.05f); UnitySmoke.Capture(hud, tag + "-1-swell");
            TickHUD(.07f); UnitySmoke.Capture(hud, tag + "-2-boom");
            TickHUD(.15f); UnitySmoke.Capture(hud, tag + "-3-fire");
            TickHUD(.35f); UnitySmoke.Capture(hud, tag + "-4-smoke");
            TickHUD(2);
            Check(fx.ActiveEffectCount == 0, "Bomb " + (bombLevel == 17 ? "block" : "explosion") + " effects drain");
        }
        game.SelectLevel(13);
    }
    static void VerifyReactionVariety()
    {
        game.SelectLevel(13); Begin();
        var fx = hud.WorldEffects;
        var pool = Field<TargetPool>(game, "pool");
        var members = new TargetView[pool.Capacity];
        int count = pool.FindChain(ChainTarget(), members);
        int score = game.Session.Score, hits = game.Session.Hits;
        int objects = hud.GetComponentsInChildren<RectTransform>(true).Length;
        for (int style = 0; style < 3; style++)
        {
            int chosen = fx.Chain(members, count);
            Check(chosen == style, "Chain reactions cycle without repeats: style " + style);
            TickHUD(.08f);
            UnitySmoke.Capture(hud, "reaction-" + style + "-travel");
            game.Pause(); float time = Field<float>(fx, "clock"); TickHUD(.5f);
            Check(time == Field<float>(fx, "clock"), "Reaction style " + style + " freezes on pause");
            game.Resume(); TickHUD(.25f);
            UnitySmoke.Capture(hud, "reaction-" + style + "-burst");
            TickHUD(3);
            Check(fx.ActiveEffectCount == 0, "Reaction style " + style + " drains its fixed pool");
        }
        Check(fx.Chain(members, count) == 0, "Fourth chain wraps to the electric reaction");
        Check(game.Session.Score == score && game.Session.Hits == hits,
            "Presentation variations cannot grant score or extra hits");
        hud.SetSettings(true, true);
        fx.Chain(members, count);
        Check(fx.ActiveEffectCount == 0, "Alternate reactions obey reduced motion");
        hud.SetSettings(true, false);
        game.BeginCountdown();
        Check(fx.LastReactionStyle == 0 && fx.ActiveEffectCount == 0, "Replay resets reaction rotation and effects");
        Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objects, "Reaction variety creates no UI objects");
        for (int style = 0; style < 3; style++)
        {
            Begin(); count = pool.FindChain(ChainTarget(), members);
            for (int prime = 0; prime < style; prime++) { fx.Chain(members, count); fx.Tick(3); }
            Tap(ChainTarget());
            Check(fx.LastReactionStyle == style && game.Session.Hits == count && game.Session.Chains == 1,
                "Real tap resolves the same chain once with reaction style " + style);
            TickHUD(.08f); pool.Tick(.08f, false);
            UnitySmoke.Capture(hud, "reaction-" + style + "-travel");
            TickHUD(.25f); pool.Tick(.25f, false);
            UnitySmoke.Capture(hud, "reaction-" + style + "-burst");
            var popup = Field<Array>(hud, "popups").GetValue(0);
            Check(PopupPart<Image>(popup, "Ring").enabled == (style == 0),
                "Reaction style " + style + " uses its own impact silhouette");
        }

        game.SelectLevel(13);
    }
    static void Step()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup - started > 300) { Finish(false, "Timed out"); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (stage == 0)
            {
                game = UnityEngine.Object.FindFirstObjectByType<GameManager>(); hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                if (game == null || game.Session == null || hud == null) return;
                VerifyBindings(); VerifyCampaign(); VerifyMechanics(); VerifyMovingBombs();
                game.SelectLevel(inputLevel); Begin(); stage = 10; next = EditorApplication.timeSinceStartup + .1;
            }
            else if (stage == 10)
            {
                VerifyInputRouting(); inputLevel++;
                if (inputLevel <= 20) { game.SelectLevel(inputLevel); Begin(); }
                else { VerifyAnimations(); VerifyWorldEffects(); VerifyReactionVariety(); game.SelectLevel(20); stage = 1; }
                next = EditorApplication.timeSinceStartup + .1;
            }
            else if (stage == 1) { Capture("campaign20-briefing"); game.SelectLevel(17); Begin(); game.ActivateShield(); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 2) { Capture("campaign20-shield"); game.SelectLevel(13); Begin(); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 3) { Capture("campaign20-mega"); game.Pause(); Capture("campaign20-pause"); game.ShowLevels(); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 4) { Capture("campaign20-world2");
                game.SelectLevel(8); Begin(); Capture("gui-03-waterfall");
                game.SelectLevel(4); Begin();
                var guiPool=Field<TargetPool>(game,"pool"); var guiSpawner=Field<TargetSpawner>(game,"spawner");
                for(int i=0;i<80 && guiPool.ActiveCount<4;i++) guiSpawner.Tick(.1f,false);
                Capture("gui-08-active-four-targets");
                ProgressStore.SelectLevel(13);
                Finish(true, "All 20 authored levels, transitions, mechanics, mastery and rewards verified"); }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
}
