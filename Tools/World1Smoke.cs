using TMPro;
// Runs only in the disposable validation project, never in production Assets.
using System;
using System.IO;
using System.Reflection;
using TapBlast;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class World1Smoke
{
    const string Key = "TapBlast.World1Smoke";
    const string Report = "world1-results.txt";
    static int checks, stage;
    static double began, next;
    static GameManager game;
    static GameHUD hud;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static World1Smoke()
    {
        if (SessionState.GetBool(Key, false))
        { began = EditorApplication.timeSinceStartup; EditorApplication.update += Step; Application.logMessageReceived += OnLog; }
    }
    public static void Run()
    {
        PlayerSettings.companyName = "TapBlastValidation";
        PlayerSettings.productName = "IsolatedWorld1Flow";
        File.WriteAllText(Report, "");
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
    static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Private).GetValue(o);
    static void Call(object o, string name) => o.GetType().GetMethod(name, Private).Invoke(o, null);
    static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; File.AppendAllText(Report, "PASS " + message + "\n"); }
    static void Finish(bool ok, string message)
    {
        SessionState.SetBool(Key, false); EditorApplication.update -= Step; Application.logMessageReceived -= OnLog;
        File.AppendAllText(Report, (ok ? "PASS " : "FAIL ") + message + "\n" + checks + " checks\n"); EditorApplication.Exit(ok ? 0 : 1);
    }
    static void OnLog(string message, string trace, LogType type)
    {
        if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + trace);
    }
    static void Begin()
    {
        game.BeginCountdown();
        typeof(GameManager).GetField("countdown", Private).SetValue(game, 0f); Call(game, "Update");
    }
    static TargetView[] Targets() => UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
    static void Tap(TargetView target) => target.OnPointerDown(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left });
    static void Complete()
    { game.Session.Tick(game.Session.RemainingTime + 1); Call(game, "Finish"); }

    static void VerifyCampaign()
    {
        // Product name is isolated above; these keys do not touch the player's save.
        PlayerPrefs.DeleteKey("TapBlast.v2.Progress");
        PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Best"); PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Stars");
        var configs = Field<LevelConfig[]>(game, "levels");
        var serializedHud = new SerializedObject(hud);
        var binding = serializedHud.GetIterator();
        int textBindings = 0;
        while (binding.Next(true))
        {
            if (binding.propertyType != SerializedPropertyType.ObjectReference || !binding.type.Contains("TMP_Text")) continue;
            Check(binding.objectReferenceValue is TMP_Text, "Scene TMP binding: " + binding.propertyPath);
            textBindings++;
        }
        Check(textBindings >= 40, "HUD and pooled popup TMP fields are wired in the scene");
        foreach (var component in hud.GetComponentsInChildren<Component>(true))
            Check(component == null || component.GetType().FullName != "UnityEngine.UI.Text", "No legacy Canvas text component");
        foreach (var label in hud.GetComponentsInChildren<TMP_Text>(true))
        {
            Check(label is TextMeshProUGUI && label.font != null && label.fontSharedMaterial != null,
                "TMP component has font and material: " + label.name);
            Check(!label.raycastTarget, "Decorative TMP label does not intercept taps: " + label.name);
        }
        Check(configs.Length == 10, "All ten levels are serialized into the scene without editor loading");
        for (int i = 0; i < 10; i++) Check(configs[i] != null && configs[i].LevelId == i + 1, "Scene level reference " + (i + 1));
        int count = hud.GetComponentsInChildren<Graphic>(true).Length;
        game.ShowLevels(); Canvas.ForceUpdateCanvases();
        var buttons = Field<Button[]>(hud, "levelButtons");
        var labels = Field<TMP_Text[]>(hud, "selectionTexts");
        Check(buttons.Length == 10 && labels.Length == 10, "Ten authored level buttons and progress labels");
        var fitting = hud.GetComponent<ResponsiveCanvas>();
        Check(fitting != null && fitting.SafeArea != null && fitting.Frame != null, "Authored responsive Canvas references");
        var frame = fitting.Frame;
        Check(frame.rect.size == new Vector2(1080, 1920), "Portrait frame is authored at 1080 by 1920");
        var scaler = hud.GetComponent<CanvasScaler>();
        Check(scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize && scaler.referenceResolution == new Vector2(1080, 1920) && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Expand, "Canvas Scaler fits phone and tablet resolutions");
        var corners = new Vector3[4];
        for (int i = 0; i < 10; i++)
        {
            Check(buttons[i] != null && labels[i] != null && buttons[i].interactable == (i == 0), "Authored level selection lock " + (i + 1));
            buttons[i].GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach (var corner in corners) Check(frame.rect.Contains((Vector2)frame.InverseTransformPoint(corner)), "Button inside portrait safe frame " + (i + 1));
        }
        Check(hud.GetComponentsInChildren<Graphic>(true).Length == count, "Level selection creates no UI objects");
        int coins = 0, xp = 0;
        for (int id = 1; id <= 10; id++)
        {
            Check(game.SelectLevel(id), "Unlocked campaign routing " + id);
            Begin(); var config = game.CurrentLevel;
            var pool = Field<TargetPool>(game, "pool"); var spawner = Field<TargetSpawner>(game, "spawner");
            if (id == 6) Check(Targets()[0].Kind == TargetKind.Golden, "Golden tutorial begins with a Golden target");
            if (id == 8) Check(Targets()[0].Kind == TargetKind.Time, "Time tutorial begins with a Time Bubble");
            if (id == 6 || id == 8 || id == 10)
            {
                var special = Targets()[0];
                Check(!Field<GameObject>(special, "normalSymbol").activeSelf && !Field<GameObject>(special, "normalOutline").activeSelf,
                    "Special label is not obscured by normal star artwork in level " + id);
                string symbol = id == 6 ? "goldenSymbol" : id == 8 ? "timeSymbol" : "bossSymbol";
                Check(Field<GameObject>(special, symbol).activeSelf, "Special target has a visible authored label in level " + id);
            }
            if (id == 10)
            {
                var boss = Targets()[0]; Check(boss.Kind == TargetKind.Boss && boss.Health == 5, "Five-hit boss spawns at round start");
                Tap(boss); Check(boss.Active && boss.Health == 4 && game.Session.BossesDestroyed == 0 && game.Session.Score == 10, "Boss damage scores but does not complete Core objective");
                Begin(); pool = Field<TargetPool>(game, "pool"); spawner = Field<TargetSpawner>(game, "spawner");
            }
            // Exercise every boundary with a large movement step, then restart cleanly.
            if (config.MovingTargets)
            {
                for (int n = 0; n < 20; n++) { spawner.Tick(.25f, false); pool.Tick(.25f, false); }
                foreach (var t in Targets()) Check(hud.Arena.rect.Contains(t.Position), "Moving target remains inside arena in level " + id);
                Begin(); pool = Field<TargetPool>(game, "pool"); spawner = Field<TargetSpawner>(game, "spawner");
            }
            if (id == 7) Check(config.SpawnIntervalAt(0) > config.SpawnIntervalAt(10) && Mathf.Approximately(config.SpawnIntervalAt(20), .5f), "Level 7 accelerates to 0.5 seconds");
            float paused = game.Session.RemainingTime;
            game.Pause(); foreach (var t in Targets()) Tap(t); game.Session.Tick(5);
            Check(game.Session.Score == 0 && game.Session.RemainingTime == paused, "Pause blocks clock and taps in level " + id); game.Resume();
            int timeHits = 0;
            for (int frameIndex = 0; frameIndex < 12000 && game.Session.State == RoundState.Playing; frameIndex++)
            {
                foreach (var t in Targets())
                {
                    if (t.Kind == TargetKind.Time) timeHits++;
                    int before = game.Session.Score;
                    float beforeTime = game.Session.RemainingTime;
                    var kind = t.Kind; Tap(t);
                    if (kind == TargetKind.Golden) Check(game.Session.Score - before >= config.GoldenPoints, "Golden score reaches session in level " + id);
                    if (kind == TargetKind.Time) Check(Mathf.Abs(game.Session.RemainingTime - beforeTime - config.TimeBonus) < .001f, "Time Bubble reaches clock in level " + id);
                }
                if (game.Session.State != RoundState.Playing) break;
                game.Session.Tick(1f / 60);
                if (game.Session.State != RoundState.Playing) break;
                spawner.Tick(1f / 60, false); pool.Tick(1f / 60, false);
            }
            if (game.Session.State == RoundState.Results) Call(game, "Finish");
            Check(game.Session.Won && game.Session.Stars == 3, "Authored level " + id + " is winnable with reachable three-star mastery");
            Check(timeHits <= config.MaxTimeTargets, "Time Bubble budget keeps level " + id + " finite");
            coins += config.CoinReward; xp += config.XPReward;
            Check(ProgressStore.Coins == coins && ProgressStore.XP == xp && ProgressStore.GetBestStars(id) == 3, "Level " + id + " persists records and pays once");
            Call(game, "Finish"); Check(ProgressStore.Coins == coins, "Duplicate completion cannot pay level " + id + " twice");
        }
        game.SelectLevel(10); Begin();
        for (int i = 0; i < 200; i++) game.Session.Tap();
        Complete(); Check(!game.Session.Won && ProgressStore.Coins == coins, "High score without Core destruction fails and earns no rewards");
        game.ShowLevels(); Check(hud.GetComponentsInChildren<Graphic>(true).Length == count, "All campaign navigation reuses authored UI");
    }
    static void VerifyDeviceLayouts(string screenName)
    {
        var sizes = new[] { new Vector2Int(1080, 1920), new Vector2Int(720, 1280), new Vector2Int(1080, 2400), new Vector2Int(1536, 2048), new Vector2Int(2048, 1536) };
        foreach (var size in sizes)
        {
            Rect safe = size.y == 2400 ? new Rect(0, 84, size.x, size.y - 204) : new Rect(0, 0, size.x, size.y);
            var fitting = hud.GetComponent<ResponsiveCanvas>();
            int objects = hud.GetComponentsInChildren<RectTransform>(true).Length;
            string device = size.x + "x" + size.y;
            UnitySmoke.Capture(hud, "responsive-" + screenName + "-" + device, size.x, size.y, safe, () =>
            {
                var corners = new Vector3[4];
                fitting.Frame.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    Vector2 p = RectTransformUtility.WorldToScreenPoint(Camera.main, corner);
                    Check(p.x >= safe.xMin - 1 && p.x <= safe.xMax + 1 && p.y >= safe.yMin - 1 && p.y <= safe.yMax + 1, screenName + " frame stays inside safe area " + device);
                }
                foreach (var button in hud.GetComponentsInChildren<Button>())
                {
                    button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector2 p = RectTransformUtility.WorldToScreenPoint(Camera.main, corner);
                        Check(p.x >= safe.xMin - 1 && p.x <= safe.xMax + 1 && p.y >= safe.yMin - 1 && p.y <= safe.yMax + 1, button.name + " is reachable in " + screenName + " " + device);
                    }
                }
                Check(Mathf.Approximately(fitting.Frame.lossyScale.x, fitting.Frame.lossyScale.y), "Uniform layout scale " + device);
                Check(fitting.Frame.rect.size == new Vector2(1080, 1920), "Device fitting preserves editable reference dimensions " + device);
                Check(hud.GetComponentsInChildren<RectTransform>(true).Length == objects, "Resizing does not create UI " + device);
            });
        }
    }
    static void Step()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup - began > 180) { Finish(false, "Timed out"); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (stage == 0)
            {
                game = UnityEngine.Object.FindFirstObjectByType<GameManager>(); hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                if (game == null || game.Session == null) return;
                VerifyCampaign(); stage++; next = EditorApplication.timeSinceStartup + 1;
            }
            else if (stage == 1) { UnitySmoke.Capture(hud, "world1-level-select"); VerifyDeviceLayouts("levels"); stage++; next = EditorApplication.timeSinceStartup + 1; }
            else if (stage == 2) { game.SelectLevel(6); stage++; next = EditorApplication.timeSinceStartup + 1; }
            else if (stage == 3) { UnitySmoke.Capture(hud, "world1-golden-briefing"); VerifyDeviceLayouts("briefing"); stage++; next = EditorApplication.timeSinceStartup + 1; }
            else if (stage == 4) { game.SelectLevel(10); Begin(); stage++; next = EditorApplication.timeSinceStartup + 1; }
            else if (stage == 5) { UnitySmoke.Capture(hud, "world1-core"); VerifyDeviceLayouts("gameplay"); game.Pause(); VerifyDeviceLayouts("pause"); game.Resume(); stage++; next = EditorApplication.timeSinceStartup + 1; }
            else if (stage == 6) { game.SelectLevel(8); Begin(); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 7) { UnitySmoke.Capture(hud, "world1-time"); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 8) { game.SelectLevel(6); Begin(); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else if (stage == 9) { UnitySmoke.Capture(hud, "world1-golden"); stage++; next = EditorApplication.timeSinceStartup + .3; }
            else Finish(true, "World 1 authored assets, objectives, mechanics, stars, progression and rewards verified");
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
}
