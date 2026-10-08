using TMPro;
// Editor-only verification, copied into a disposable project by Run-UnitySmoke.ps1.
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
public static class CampaignSmoke
{
    private const string Key = "TapBlast.CampaignSmoke";
    private static GameManager game;
    private static GameHUD hud;
    private static int stage, checks;
    private static double began, deadline;
    private static float pausedTime;
    private static bool checkedGoal, checkedFever;
    private static int authoredGraphicCount;
    private static RectTransform frozenSpark;
    private static Vector2 frozenSparkPosition;
    private static readonly HashSet<int> poolIds = new HashSet<int>();
    private static readonly List<RaycastResult> raycasts = new List<RaycastResult>();
    static CampaignSmoke()
    {
        if (SessionState.GetBool(Key, false))
        { stage = SessionState.GetBool(Key + ".Restore", false) ? 900 : 0; began = EditorApplication.timeSinceStartup; EditorApplication.update += Step; Application.logMessageReceived += OnLog; }
    }
    private static void SetProduct()
    { PlayerSettings.companyName = "TapBlastValidation"; PlayerSettings.productName = "IsolatedCampaignSmoke"; }
    public static void Run()
    {
        SetProduct(); File.WriteAllText("campaign-results.txt", "");
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        var canvas = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
        Check(canvas != null && PrefabUtility.IsPartOfPrefabInstance(canvas), "Editable Canvas prefab exists in the saved scene before Play");
        Check(canvas.TargetPrefab != null && AssetDatabase.Contains(canvas.TargetPrefab), "Target pool uses a saved target prefab");
        var serialized = new SerializedObject(canvas); var property = serialized.GetIterator();
        while (property.Next(true))
            if (property.propertyType == SerializedPropertyType.ObjectReference && !property.propertyPath.StartsWith("m_"))
                Check(property.objectReferenceValue != null, "Serialized Canvas reference: " + property.propertyPath);
        authoredGraphicCount = canvas.GetComponentsInChildren<Graphic>(true).Length;
        SessionState.SetInt(Key + ".Graphics", authoredGraphicCount);
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
            if (img.sprite != null) Check(AssetDatabase.Contains(img.sprite), "Canvas sprite is persisted: " + img.name);
        var wordmark = canvas.transform.Find("Device safe area/Portrait frame/Wordmark").GetComponent<TMP_Text>();
        SessionState.SetString(Key + ".Wordmark", wordmark.text); wordmark.text = "CANVAS EDIT CHECK";
        PrefabUtility.RecordPrefabInstancePropertyModifications(wordmark);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene); EditorSceneManager.SaveScene(canvas.gameObject.scene);
        SessionState.SetInt(Key + ".Checks", checks);
        SessionState.SetBool(Key + ".Restore", false); SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
    private static void Check(bool condition, string label)
    { if (!condition) throw new Exception(label); checks++; File.AppendAllText("campaign-results.txt", "PASS " + label + "\n"); }
    private static void OnLog(string condition, string trace, LogType type)
    {
        if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, condition + "\n" + trace);
    }
    private static void Finish(bool success, string message)
    {
        SessionState.SetBool(Key, false); EditorApplication.update -= Step; Application.logMessageReceived -= OnLog;
        File.AppendAllText("campaign-results.txt", (success ? "PASS " : "FAIL ") + message + "\n"); EditorApplication.Exit(success ? 0 : 1);
    }
    private static TargetView[] Targets(bool inactive = false) => UnityEngine.Object.FindObjectsByType<TargetView>(inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    private static void Click(GameObject target, bool tap = false)
    {
        Canvas.ForceUpdateCanvases();
        var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position), button = PointerEventData.InputButton.Left };
        raycasts.Clear(); EventSystem.current.RaycastAll(data, raycasts);
        Check(raycasts.Count > 0 && (raycasts[0].gameObject == target || raycasts[0].gameObject.transform.IsChildOf(target.transform)), "Raycast reaches " + target.name);
        if (tap) ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler); else ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
    }
    private static void TapAll() { foreach (var target in Targets()) Click(target.gameObject, true); }
    private static void Advance(int next, double delay = 0) { stage = next; deadline = EditorApplication.timeSinceStartup + delay; }
    private static void Shot(string name) { UnitySmoke.Capture(hud, name); }
    private static void Step()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            if (EditorApplication.timeSinceStartup - began > 180) throw new Exception("Campaign timeout, stage " + stage);
            if (EditorApplication.timeSinceStartup < deadline) return;
            switch (stage)
            {
                case 900:
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    if (game == null || game.Session == null) return;
                    Check(game.CurrentLevel.LevelId == 2 && game.Session.State == RoundState.Ready && game.Session.RemainingTime == 45, "Fresh launch restores the Level 2 briefing and rules");
                    File.AppendAllText("campaign-persistence.txt", "PASS Fresh launch restores Level 2's briefing and 45-second rules.\n");
                    SessionState.SetBool(Key + ".Restore", false); Finish(true, "Campaign restart verified."); break;
                case 0:
                    game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                    hud = UnityEngine.Object.FindFirstObjectByType<GameHUD>();
                    if (game == null || game.Session == null || hud == null) return;
                    checks = SessionState.GetInt(Key + ".Checks", 0);
                    var wordmark = hud.transform.Find("Device safe area/Portrait frame/Wordmark").GetComponent<TMP_Text>();
                    Check(wordmark.text == "CANVAS EDIT CHECK", "Designer Canvas text edit survives entering Play");
                    wordmark.text = SessionState.GetString(Key + ".Wordmark", "TAP BLAST");
                    Check(hud.GetComponentsInChildren<Graphic>(true).Length == SessionState.GetInt(Key + ".Graphics", 0) + 12 * hud.TargetPrefab.GetComponentsInChildren<Graphic>(true).Length, "Play only prewarms target prefab; it does not rebuild Canvas elements");
                    PlayerPrefs.SetString("TapBlast.v2.Progress", "{\"bestScore\":275,\"bestStars\":3,\"coins\":180,\"xp\":90,\"activeRound\":\"\"}");
                    Check(ProgressStore.IsUnlocked(2) && ProgressStore.GetBestScore(1) == 275 && ProgressStore.Coins == 180 && ProgressStore.XP == 90, "Existing Level 1 save unlocks Level 2 without losing wallet");
                    Check(ProgressStore.GetBestScore(2) == 0 && ProgressStore.GetBestStars(2) == 0, "Migration does not copy Level 1 records into Level 2");
                    Check(ProgressStore.SelectLevel(2) && ProgressStore.SelectedLevel == 2, "Migrated save can select unlocked Level 2");
                    PlayerPrefs.DeleteKey("TapBlast.v2.Progress"); PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Best"); PlayerPrefs.DeleteKey("TapBlast.v1.Level01.Stars");
                    Check(!ProgressStore.IsUnlocked(2) && !game.SelectLevel(2), "Fresh player cannot enter locked Level 2");
                    Check(game.SelectLevel(1), "Fresh player can select Level 1");
                    foreach (var target in Targets(true)) poolIds.Add(target.GetInstanceID());
                    Check(poolIds.Count == 12, "Campaign prewarms one shared twelve-target pool");
                    Click(hud.IntroMenu.gameObject);
                    Check(!hud.LevelTwo.interactable, "Level select visibly disables locked Level 2");
                    Shot("07-level-select-locked"); Advance(1, .2); break;
                case 1:
                    Click(hud.LevelOne.gameObject); Advance(101, .2); break;
                case 101:
                    Click(hud.Play.gameObject); Advance(2, 3.4); break;
                case 2:
                    Check(hud.TutorialHandVisible && game.Session.RemainingTime == 60, "Level 1 retains first-tap tutorial timing");
                    Click(Targets()[0].gameObject, true); Advance(201, .12); break;
                case 201:
                    frozenSpark = hud.transform.Find("Device safe area/Portrait frame/Pooled feedback/Tap feedback 0/Spark 0") as RectTransform;
                    Check(frozenSpark.gameObject.activeInHierarchy && frozenSpark.anchoredPosition.sqrMagnitude > 500, "Tap emits pooled outward sparks");
                    Shot("15-canvas-tap-burst"); Click(hud.Pause.gameObject); frozenSparkPosition = frozenSpark.anchoredPosition;
                    Advance(202, .3); break;
                case 202:
                    Check(frozenSpark.anchoredPosition == frozenSparkPosition, "Pause freezes tap burst animation");
                    hud.SetSettings(true, true, false);
                    Check(!frozenSpark.gameObject.activeSelf, "Reduced motion suppresses in-flight particles");
                    hud.SetSettings(true, false, false); Click(hud.Resume.gameObject);
                    Advance(3); break;
                case 3:
                    TapAll();
                    if (game.Session.Combo == 10 && !checkedFever)
                    {
                        checkedFever = true;
                        Check(hud.transform.Find("Device safe area/Portrait frame/Combo card/Fever state").GetComponent<TMP_Text>().text == "COMBO FEVER", "Ten-hit streak enters GDD Combo Fever presentation");
                        Shot("16-canvas-combo-fever");
                    }
                    if (game.Session.State != RoundState.Results) break;
                    Check(game.Session.Won && ProgressStore.IsUnlocked(2), "Completing Level 1 unlocks Level 2");
                    Check(ProgressStore.Coins == 50 && ProgressStore.XP == 25, "Level 1 grants only its configured reward");
                    Check(hud.Next.gameObject.activeSelf, "Level 1 success offers Next Level");
                    Check(hud.transform.Find("Device safe area/Portrait frame/Results/Completion confetti").gameObject.activeSelf, "Successful completion starts pooled confetti");
                    Advance(4, 2.4); break;
                case 4:
                    Shot("08-level1-next"); Click(hud.Next.gameObject);
                    Advance(104, .2); break;
                case 104:
                    Check(game.CurrentLevel.LevelId == 2 && game.Session.State == RoundState.Ready, "Next opens Level 2 briefing");
                    Check(ProgressStore.SelectedLevel == 2 && game.Session.RemainingTime == 45, "Level 2 selection and duration applied");
                    foreach (var target in Targets(true)) Check(poolIds.Contains(target.GetInstanceID()), "Transition reuses prewarmed target");
                    Shot("09-level2-briefing"); Click(hud.Play.gameObject); Advance(5, 4.8); break;
                case 5:
                    Check(game.Session.State == RoundState.Playing && game.Session.RemainingTime < 45 && !hud.TutorialHandVisible, "Level 2 clock starts at countdown end without repeating tap tutorial");
                    var sizes = new HashSet<int>();
                    foreach (var target in Targets())
                    {
                        var rect = (RectTransform)target.transform; sizes.Add(Mathf.RoundToInt(rect.rect.width));
                        float radius = rect.rect.width * .513f;
                        Check(hud.Arena.rect.Contains(target.Position + Vector2.one * radius) && hud.Arena.rect.Contains(target.Position - Vector2.one * radius), "Size variant stays inside arena");
                    }
                    Check(sizes.Contains(150) && sizes.Contains(168) && sizes.Contains(216), "Level 2 spawns all three normal-target sizes");
                    Shot("10-level2-sizes"); Click(hud.Pause.gameObject); pausedTime = game.Session.RemainingTime; Advance(6, 1); break;
                case 6:
                    Check(game.Session.RemainingTime == pausedTime && game.Session.State == RoundState.Paused, "Level 2 pause freezes clock");
                    Click(hud.Resume.gameObject); TapAll();
                    Check(game.Session.Hits == 3 && game.Session.Score == 30, "All three sizes award equal ten-point base score");
                    Advance(7); break;
                case 7:
                    TapAll();
                    if (game.Session.GoalMet && !checkedGoal)
                    {
                        Check(game.Session.State == RoundState.Playing, "Reaching 500 continues play toward mastery stars");
                        Shot("11-level2-goal-met"); checkedGoal = true;
                    }
                    if (game.Session.State != RoundState.Results) break;
                    Check(game.Session.Won && game.Session.Stars == 3 && game.Session.Misses == 0, "Full timed Level 2 round earns three stars");
                    Check(ProgressStore.GetBestScore(1) == 275 && ProgressStore.GetBestStars(1) == 3, "Level 2 preserves Level 1 records");
                    Check(ProgressStore.GetBestScore(2) == game.Session.Score && ProgressStore.GetBestStars(2) == 3, "Level 2 saves separate score and stars");
                    Check(ProgressStore.Coins == 125 && ProgressStore.XP == 60, "Level 2 adds its 75-coin and 35-XP reward once");
                    Check(hud.Next.gameObject.activeSelf && ProgressStore.IsUnlocked(3), "Level 2 unlocks the authored Level 3");
                    var token = (string)typeof(GameManager).GetField("roundId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(game);
                    Check(!ProgressStore.CompleteRound(token, game.Session, 75, 35, out _), "Level 2 result cannot be rewarded twice");
                    string wrongToken = ProgressStore.BeginRound(1);
                    Check(!ProgressStore.CompleteRound(wrongToken, game.Session, 75, 35, out _), "Level 1 token cannot claim a Level 2 result");
                    ProgressStore.AbandonRound(wrongToken);
                    Advance(8, 2.4); break;
                case 8:
                    Check(hud.ResultRevealComplete, "Level 2 result animation completes"); Shot("12-level2-results");
                    Click(hud.Replay.gameObject); Check(game.CurrentLevel.LevelId == 2 && game.Session.Score == 0, "Replay stays in Level 2 and clears score");
                    Advance(9, 3.5); break;
                case 9:
                    typeof(RoundSession).GetField("<RemainingTime>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(game.Session, .05f);
                    Advance(10, .3); break;
                case 10:
                    Check(game.Session.State == RoundState.Results && !game.Session.Won && game.Session.Stars == 0, "Level 2 fails when timer ends below 500");
                    Check(ProgressStore.Coins == 125 && ProgressStore.XP == 60, "Failed Level 2 replay pays no reward");
                    Check(!hud.Next.gameObject.activeSelf, "Failed round offers no next-level shortcut");
                    Check(!hud.transform.Find("Device safe area/Portrait frame/Results/Completion confetti").gameObject.activeSelf, "Failure does not play success confetti");
                    Advance(11, 2.4); break;
                case 11:
                    Shot("13-level2-failure"); Click(hud.ResultMenu.gameObject);
                    Advance(111, .2); break;
                case 111:
                    Check(hud.LevelTwo.interactable, "Completed Level 2 remains available in level select");
                    Shot("14-level-select-completed"); Click(hud.LevelOne.gameObject);
                    Advance(112, .2); break;
                case 112:
                    Check(game.CurrentLevel.LevelId == 1 && game.Session.RemainingTime == 60, "Player can return to Level 1 with its original rules");
                    Click(hud.Play.gameObject); Advance(12, 3.5); break;
                case 12:
                    Click(hud.Pause.gameObject); Advance(120, .2); break;
                case 120:
                    Click(hud.PauseMenu.gameObject); Advance(121, .2); break;
                case 121:
                    Check(Targets().Length == 0 && game.Session.State == RoundState.Ready && ProgressStore.Coins == 125, "Exiting paused round clears targets without awarding currency");
                    Click(hud.LevelTwo.gameObject);
                    Advance(122, .2); break;
                case 122:
                    Check(ProgressStore.SelectedLevel == 2, "Last selected level is saved");
                    Finish(true, checks + " campaign checks passed."); break;
            }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }
    public static void VerifyPersistence()
    {
        SetProduct();
        bool passed = ProgressStore.SelectedLevel == 2 && ProgressStore.IsUnlocked(2) && ProgressStore.Coins == 125 && ProgressStore.XP == 60 && ProgressStore.GetBestScore(1) == 275 && ProgressStore.GetBestStars(1) == 3 && ProgressStore.GetBestScore(2) >= 750 && ProgressStore.GetBestStars(2) == 3;
        File.WriteAllText("campaign-persistence.txt", passed ? "PASS Unlock, selection, separate level records and shared wallet survive a fresh Unity process.\n" : "FAIL Campaign progress did not survive restart.\n");
        if (!passed) { EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/Level01.unity");
        SessionState.SetBool(Key + ".Restore", true); SessionState.SetBool(Key, true); EditorApplication.EnterPlaymode();
    }
}
