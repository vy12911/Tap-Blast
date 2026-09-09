// Copied into an isolated project's Assets/Editor by the validation workflow.
// Never ships in the game.
using System;
using System.Collections.Generic;
using System.IO;
using TapBlast;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class UnitySmoke
{
    private const string Key = "TapBlast.Smoke.Active";
    private static int stage, previousHits, checks;
    private static double deadline, began;
    private static float frozenTime;
    private static GameManager manager;
    private static GameHUD hud;
    private static int coinsBefore, xpBefore;
    private static readonly List<RaycastResult> hits = new List<RaycastResult>();
    static UnitySmoke()
    {
        if (SessionState.GetBool(Key, false)) { began = EditorApplication.timeSinceStartup; EditorApplication.update += Step; Application.logMessageReceived += OnLog; }
    }
    public static void Run()
    {
        File.WriteAllText("smoke-results.txt", "");
        PlayerSettings.companyName = "TapBlastValidation";
        PlayerSettings.productName = "IsolatedLevelOneRewardsSmoke";
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }
    public static void VerifyPersistence()
    {
        try
        {
            PlayerSettings.companyName = "TapBlastValidation";
            PlayerSettings.productName = "IsolatedLevelOneRewardsSmoke";
            if (ProgressStore.Coins != 150 || ProgressStore.XP != 125 || ProgressStore.BestScore != 275 || ProgressStore.BestStars != 3)
                throw new Exception("Balances or best results did not survive editor restart");
            File.WriteAllText("persistence-results.txt", "PASS Coins, XP, best score and stars survive a fresh Unity process\n");
            string token = ProgressStore.BeginRound();
            var session = Completed(true);
            if (!ProgressStore.CompleteRound(token, session, 50, 25, out var receipt) || receipt.TotalCoins != 200 || receipt.TotalXP != 150)
                throw new Exception("New round after restart did not grant its own reward");
            File.AppendAllText("persistence-results.txt", "PASS A new successful round earns its own reward after restart\n");
            if (ProgressStore.CompleteRound(token, session, 50, 25, out _) || ProgressStore.Coins != 200)
                throw new Exception("Duplicate reward after restart");
            File.AppendAllText("persistence-results.txt", "PASS Duplicate protection remains effective after restart\n3 persistence checks passed.\n");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText("persistence-results.txt", "FAIL " + e); EditorApplication.Exit(1); }
    }
    private static void OnLog(string condition, string trace, LogType type)
    {
        // Unity's optional editor search index can fail during a cold batch import;
        // record that tooling issue without treating it as a gameplay exception.
        if (trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            File.AppendAllText("smoke-results.txt", "EDITOR TOOLING NOTE: " + condition + "\n"); return;
        }
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Fail(condition + "\n" + trace);
    }
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        checks++; File.AppendAllText("smoke-results.txt", "PASS " + name + "\n");
    }
    private static void Fail(string reason)
    {
        SessionState.SetBool(Key, false); EditorApplication.update -= Step; Application.logMessageReceived -= OnLog;
        File.AppendAllText("smoke-results.txt", "FAIL " + reason + "\n"); EditorApplication.Exit(1);
    }
    private static TargetView[] Targets() => UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsSortMode.None);
    private static RoundSession Completed(bool won)
    {
        var session = new RoundSession(new RoundRules(60, 20, 10, 240, 20));
        session.Reset(); session.StartPlaying();
        if (won) for (int i = 0; i < 20; i++) session.Tap(); else session.Tick(61);
        return session;
    }
    private static void CheckRewards()
    {
        // This project uses a separate product name; never reset production preferences.
        PlayerPrefs.DeleteKey("TapBlast.v2.Progress");
        PlayerPrefs.SetInt("TapBlast.v1.Level01.Best", 123);
        PlayerPrefs.SetInt("TapBlast.v1.Level01.Stars", 2);
        Check(ProgressStore.BestScore == 123 && ProgressStore.BestStars == 2, "Legacy score and stars remain available before migration");
        string token = ProgressStore.BeginRound();
        Check(ProgressStore.BestScore == 123 && ProgressStore.Coins == 0, "First write migrates legacy progress without inventing rewards");
        var won = Completed(true);
        Check(ProgressStore.CompleteRound(token, won, 50, 25, out var first) && first.Coins == 50 && first.XP == 25, "Winning round grants configured coins and XP");
        Check(!ProgressStore.CompleteRound(token, won, 50, 25, out _) && ProgressStore.Coins == 50 && ProgressStore.XP == 25, "Duplicate completion cannot grant rewards twice");
        string newer = ProgressStore.BeginRound();
        Check(!ProgressStore.CompleteRound(token, won, 50, 25, out _), "Old completion token rejected after another round starts");
        Check(ProgressStore.CompleteRound(newer, Completed(false), 50, 25, out var lost) && lost.Coins == 0 && lost.XP == 0 && ProgressStore.Coins == 50, "Failure consumes its token without paying rewards");
        var unfinished = new RoundSession(new RoundRules(60, 20, 10, 240, 20));
        token = ProgressStore.BeginRound();
        Check(!ProgressStore.CompleteRound(token, unfinished, 50, 25, out _), "Unfinished round cannot claim rewards");
        newer = ProgressStore.BeginRound();
        Check(!ProgressStore.CompleteRound(token, won, 50, 25, out _), "Restart invalidates the abandoned round token");
        Check(ProgressStore.CompleteRound(newer, won, 50, 75, out var leveled) && leveled.PlayerLevel == 2 && leveled.TotalXP == 100, "XP crossing 100 advances player level");
        Check(PlayerPrefs.GetString("TapBlast.v2.Progress").Contains("\"activeRound\":\"\"") && ProgressStore.Coins == 100 && ProgressStore.XP == 100, "Saved record contains balances and consumed token together");
        Check(ProgressStore.BestScore == 275 && ProgressStore.BestStars == 3, "Rewards preserve upgraded best score and stars");
        coinsBefore = ProgressStore.Coins; xpBefore = ProgressStore.XP;
    }
    private static void Click(GameObject target, bool pointerDown)
    {
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position), button = PointerEventData.InputButton.Left };
        hits.Clear(); EventSystem.current.RaycastAll(data, hits);
        Check(hits.Count > 0 && (hits[0].gameObject == target || hits[0].gameObject.transform.IsChildOf(target.transform)), "UI raycast reaches " + target.name);
        if (pointerDown) ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
        else ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
    }
    public static void RunCampaign() { CampaignSmoke.Run(); }
    public static void VerifyCampaignPersistence() { CampaignSmoke.VerifyPersistence(); }
    private static void Shot(string name) { Capture(hud, name); }
    public static void Capture(GameHUD hud, string name)
    {
        // Batch mode has no presented Game view; render the real UI through URP.
        var canvas = hud.GetComponent<Canvas>();
        var scaler = hud.GetComponent<CanvasScaler>();
        var camera = Camera.main;
        var portrait = (RectTransform)hud.transform.Find("Device safe area/Portrait frame");
        var oldScale = portrait.localScale;
        var oldFactor = canvas.scaleFactor;
        var oldTarget = camera.targetTexture;
        var texture = new RenderTexture(720, 1280, 24);
        var previous = RenderTexture.active;
        Texture2D pixels = null;
        try
        {
            texture.Create(); camera.targetTexture = texture;
            scaler.enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.scaleFactor = 1;
            portrait.localScale = Vector3.one; Canvas.ForceUpdateCanvases();
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = texture };
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture.active = texture;
            pixels = new Texture2D(720, 1280, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 720, 1280), 0, 0); pixels.Apply();
            File.WriteAllBytes(name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = oldTarget;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            canvas.scaleFactor = oldFactor; scaler.enabled = true; portrait.localScale = oldScale;
            Canvas.ForceUpdateCanvases();
            if (pixels != null) UnityEngine.Object.Destroy(pixels);
            texture.Release(); UnityEngine.Object.Destroy(texture);
        }
    }
    private static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 110) throw new Exception("Play-through timed out at stage " + stage);
            if (EditorApplication.timeSinceStartup < deadline) return;
            if (stage == 0)
            {
                manager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                if (manager == null || manager.Session == null) return;
                hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                CheckRewards();
                hud.ShowIntro(AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/Data/Levels/Level01.asset"), ProgressStore.BestScore);
                Check(hud != null && manager.Session.State == RoundState.Ready, "Scene loads into level briefing");
                Check(UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 4, "Exactly four targets prewarmed");
                Shot("01-briefing"); stage++; deadline = EditorApplication.timeSinceStartup + 1;
            }
            else if (stage == 1)
            {
                Click(hud.Play.gameObject, false); stage++; deadline = EditorApplication.timeSinceStartup + 3.5;
            }
            else if (stage == 2)
            {
                Check(manager.Session.State == RoundState.Playing && Targets().Length == 1, "Countdown spawns one tutorial target");
                Check(manager.Session.RemainingTime == 60, "Tutorial clock waits for first tap");
                Check(hud.TutorialHandVisible, "Tutorial hand appears at the first target");
                Shot("02-tutorial"); stage++; deadline = EditorApplication.timeSinceStartup + 1;
            }
            else if (stage == 3)
            {
                Click(Targets()[0].gameObject, true);
                Check(manager.Session.Hits == 1 && manager.Session.Score == 10, "Target pointer-down scores once");
                Check(!hud.TutorialHandVisible, "First target tap dismisses the hand");
                stage++; deadline = EditorApplication.timeSinceStartup + 1.3;
            }
            else if (stage == 4)
            {
                Click(hud.Pause.gameObject, false); frozenTime = manager.Session.RemainingTime;
                Shot("03-paused"); stage++; deadline = EditorApplication.timeSinceStartup + 1;
            }
            else if (stage == 5)
            {
                Check(manager.Session.State == RoundState.Paused && manager.Session.RemainingTime == frozenTime, "Pause freezes live round");
                Click(hud.Motion.gameObject, false);
                Check(ProgressStore.ReducedMotion, "Reduced-motion preference saves");
                Click(hud.Motion.gameObject, false);
                Click(hud.Resume.gameObject, false); stage++; deadline = EditorApplication.timeSinceStartup + .2;
            }
            else if (stage == 6)
            {
                Check(Targets().Length > 0, "Paused target survives resume");
                Targets()[0].Tick(10, true);
                Check(manager.Session.Misses == 1 && manager.Session.Combo == 0, "Target expiry releases target and resets combo");
                manager.BeginCountdown(); stage++; deadline = EditorApplication.timeSinceStartup + 3.5;
            }
            else if (stage == 7)
            {
                Check(manager.Session.Hits == 0 && manager.Session.Misses == 0, "Restart clears gameplay and tutorial state");
                Check(hud.TutorialHandVisible, "Restart restores the tutorial hand");
                stage++; previousHits = 0;
            }
            else if (stage == 8)
            {
                foreach (var target in Targets()) if (target.Active) Click(target.gameObject, true);
                if (manager.Session.Hits != previousHits)
                {
                    previousHits = manager.Session.Hits;
                    if (previousHits == 8) Shot("04-gameplay");
                }
                if (manager.Session.State != RoundState.Results) return;
                Check(manager.Session.Won && manager.Session.Score == 275 && manager.Session.Stars == 3, "Full 20-target play-through earns three stars");
                Check(Targets().Length == 0, "Completion clears active targets");
                Check(ProgressStore.BestScore == 275 && ProgressStore.BestStars == 3, "Best score and stars saved");
                Check(UnityEngine.Object.FindObjectsByType<TargetView>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 4, "Pool capacity unchanged after full round");
                Check(ProgressStore.Coins == coinsBefore + 50 && ProgressStore.XP == xpBefore + 25, "Gameplay win credits reward before animation");
                Check(!hud.ResultRevealComplete, "Result count-up and star sequence begins");
                typeof(GameManager).GetMethod("Finish", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(manager, null);
                Check(ProgressStore.Coins == coinsBefore + 50, "Repeated coordinator finish does not duplicate reward");
                Shot("05-results-start"); stage++; deadline = EditorApplication.timeSinceStartup + 2.5;
            }
            else if (stage == 9)
            {
                Check(hud.ResultRevealComplete, "Score and star animations settle");
                Shot("05-results");
                Click(hud.Replay.gameObject, false);
                Check(manager.Session.Score == 0 && manager.Session.State == RoundState.Countdown, "Result replay button starts clean round");
                stage++; deadline = EditorApplication.timeSinceStartup + 3.5;
            }
            else if (stage == 10)
            {
                Click(Targets()[0].gameObject, true);
                typeof(RoundSession).GetField("<RemainingTime>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(manager.Session, .05f);
                stage++; deadline = EditorApplication.timeSinceStartup + .3;
            }
            else if (stage == 11)
            {
                Check(!manager.Session.Won && manager.Session.Stars == 0 && manager.Session.State == RoundState.Results && Targets().Length == 0, "Unfinished round times out and clears arena");
                Check(ProgressStore.Coins == coinsBefore + 50 && ProgressStore.XP == xpBefore + 25, "Timeout adds no coins or XP");
                hud.SetSettings(true, true, false);
                stage++; deadline = EditorApplication.timeSinceStartup + .1;
            }
            else if (stage == 12)
            {
                Check(hud.ResultRevealComplete, "Reduced motion reveals final results immediately");
                Shot("06-timeout");
                hud.SetSettings(true, false, false);
                hud.ShowResults(Completed(true), true, ProgressStore.BestScore, new ProgressStore.RewardReceipt(0, 0, ProgressStore.Coins, ProgressStore.XP));
                Check(!hud.ResultRevealComplete, "A fresh result sequence can start after failure");
                Click(hud.Replay.gameObject, false);
                Check(hud.ResultRevealComplete && manager.Session.State == RoundState.Countdown, "Immediate replay cancels unfinished result animation");
                Check(ProgressStore.Coins == coinsBefore + 50, "Cancelling result presentation does not change rewards");
                SessionState.SetBool(Key, false); EditorApplication.update -= Step; Application.logMessageReceived -= OnLog;
                File.AppendAllText("smoke-results.txt", checks + " Unity smoke checks passed.\n"); EditorApplication.Exit(0);
            }
        }
        catch (Exception e) { Fail(e.ToString()); }
    }
}
