using TapBlast;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Coordinates the round; rules, presentation, pooling and saving live separately.</summary>
[DisallowMultipleComponent]
public sealed class GameManager : MonoBehaviour
{
    [SerializeField] private LevelConfig level = null;
    [SerializeField] private LevelConfig[] levels = new LevelConfig[0];
    public LevelConfig CurrentLevel => level;
    public RoundSession Session { get; private set; }
    [SerializeField] private GameHUD hud;
    private TargetPool pool;
    private TargetSpawner spawner;
    private FeedbackPlayer feedback;
    private bool tutorialStarted;
    private float countdown;
    private int displayedSecond = -1;
    private RoundState resumeState;
    private string roundId;
    private bool roundFinalized;

    private void Start()
    {
        if (level == null) { Debug.LogError("Assign a LevelConfig to GameManager.", this); enabled = false; return; }
        if (levels == null || levels.Length == 0) levels = new[] { level };
        // Auto-advance to World 2 (Level 11) if all World 1 levels are completed
        if (ProgressStore.IsUnlocked(11) && ProgressStore.SelectedLevel <= 10)
        {
            ProgressStore.SelectLevel(11);
        }

        foreach (var config in levels) if (config != null && config.LevelId == ProgressStore.SelectedLevel) level = config;
#if UNITY_EDITOR
        if (level == null || level.LevelId != ProgressStore.SelectedLevel)
        {
            var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{ProgressStore.SelectedLevel:00}.asset");
            if (loaded != null) level = loaded;
        }
#endif
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Session = new RoundSession(level.Rules);
        if (hud == null || hud.TargetPrefab == null) { Debug.LogError("Assign the scene Canvas and Normal Target prefab to GameManager.", this); enabled = false; return; }
        hud.Initialize();
        feedback = gameObject.AddComponent<FeedbackPlayer>();
        feedback.Initialize();
        int capacity = level.MaxTargets;
        foreach (var config in levels) if (config != null) capacity = Mathf.Max(capacity, config.MaxTargets);
        pool = new TargetPool(hud.Arena, hud.TargetPrefab, capacity, OnTargetTapped, OnTargetExpired);
        spawner = new TargetSpawner(pool, hud.Arena, level);
        hud.Play.onClick.AddListener(BeginCountdown);
        hud.Pause.onClick.AddListener(Pause);
        hud.Resume.onClick.AddListener(Resume);
        hud.Restart.onClick.AddListener(BeginCountdown);
        hud.Replay.onClick.AddListener(BeginCountdown);
        hud.Sound.onClick.AddListener(ToggleSound);
        hud.Motion.onClick.AddListener(ToggleMotion);
        hud.Haptics.onClick.AddListener(ToggleHaptics);
        hud.Next.onClick.AddListener(NextLevel);
        hud.IntroMenu.onClick.AddListener(ShowLevels);
        hud.PauseMenu.onClick.AddListener(ShowLevels);
        hud.ResultMenu.onClick.AddListener(ShowLevels);
        RefreshSettings();
        ShowLevels();
        RefreshHUD();
    }

    private void Update()
    {
        if (Session == null) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Session.State == RoundState.Paused) Resume(); else Pause();
        }
        if (Session.State == RoundState.Countdown)
        {
            countdown -= Time.unscaledDeltaTime;
            hud.SetCountdown(Mathf.Max(1, Mathf.CeilToInt(countdown)));
            if (countdown <= 0)
            {
                Session.StartPlaying();
                hud.ShowPlaying();
                Canvas.ForceUpdateCanvases();
                spawner.SpawnTutorial(feedback.ReducedMotion);
                if (level.FirstTapTutorial)
                { hud.ShowTapHand(); hud.SetHint("TAP THE GLOWING TARGET", "Your first tap starts the clock"); }
                else
                { tutorialStarted = true; hud.SetHint(level.GoalText, level.MechanicHint); }
            }
            return;
        }
        if (Session.State != RoundState.Playing) return;
        if (tutorialStarted)
        {
            Session.Tick(Time.unscaledDeltaTime);
            if (Session.State == RoundState.Results) { Finish(); return; }
            spawner.Tick(Time.unscaledDeltaTime, feedback.ReducedMotion);
        }
        pool.Tick(Time.unscaledDeltaTime, feedback.ReducedMotion);
        if (Mathf.CeilToInt(Session.RemainingTime) != displayedSecond) RefreshHUD();
    }

    public void BeginCountdown()
    {
        pool.Clear(); hud.ClearFeedback(); spawner.Reset(); Session.Reset();
        roundId = ProgressStore.BeginRound(level.LevelId); roundFinalized = false;
        hud.HideTapHand();
        tutorialStarted = false; countdown = 3;
        hud.ShowCountdown(); hud.SetCountdown(3); RefreshHUD();
    }

    private void OnTargetTapped(TargetView target)
    {
        if (Session.State != RoundState.Playing) return;
        Vector2 position = target.Position;
        Color color = target.Tint;

        TargetKind kind = target.Kind;
        bool destroyed = kind != TargetKind.Boss || target.Damage();
        if (destroyed) pool.Release(target);
        tutorialStarted = true;
        hud.HideTapHand();
        int points = Session.Tap(kind, destroyed);
        feedback.PlayTap(Session.Combo);
        hud.Pop(position, points, color, feedback.ReducedMotion);
        hud.HitJuice(Session.Combo);
        RefreshHUD();

        if (Session.State == RoundState.Results) { Finish(); return; }
        if (kind == TargetKind.Boss)
            hud.SetHint(destroyed ? "GOLDEN CORE DESTROYED!" : "CORE HEALTH " + target.Health + " / " + target.MaxHealth,
                destroyed ? "Keep scoring for mastery until time runs out" : "Every hit damages the Core and builds your combo");
        else if (kind == TargetKind.Golden)
            hud.SetHint("GOLDEN! +" + points, "Collected " + Session.GoldenHits + " Golden targets");
        else if (kind == TargetKind.Time)
            hud.SetHint("TIME BUBBLE! +" + level.TimeBonus.ToString("0.#") + " SECONDS", "Collected " + Session.TimeHits + " Time Bubbles");
        else if (level.FirstTapTutorial && Session.Hits < 3)
            hud.SetHint("NICE TAP! +" + points, "Each target adds points. Keep going!");
        else if (level.FirstTapTutorial && Session.Hits < 5)
            hud.SetHint("BUILD YOUR COMBO", "Tap targets before their rings run out");
        else
            hud.SetHint(Session.GoalMet ? "GOAL MET! KEEP SCORING" : level.GoalText,
                Session.GoalMet ? "Keep your streak until time runs out" : level.MechanicHint);
    }
    private void OnTargetExpired(TargetView target)
    {
        pool.Release(target);
        if (Session.State != RoundState.Playing) return;
        Session.Miss();
        hud.MissJuice(target.Position);
        hud.SetHint("TARGET MISSED", "Your combo reset. Try the next one!");
        RefreshHUD();
    }

    public void Pause()
    {
        if (Session == null || (Session.State != RoundState.Playing && Session.State != RoundState.Countdown)) return;
        resumeState = Session.State; Session.Pause(); pool.SetInteractable(false); hud.ShowPause();
    }
    public void Resume()
    {
        if (Session.State != RoundState.Paused) return;
        Session.Resume(); pool.SetInteractable(true);
        if (resumeState == RoundState.Countdown) hud.ShowCountdown(); else hud.ShowPlaying();
    }
    private void Finish()
    {
        if (roundFinalized || Session.State != RoundState.Results) return;
        roundFinalized = true;
        pool.Clear();
        bool newBest = Session.Score > ProgressStore.GetBestScore(level.LevelId);
        ProgressStore.CompleteRound(roundId, Session, level.CoinReward, level.XPReward, out var receipt);
        feedback.PlayResult(Session.Won);
        hud.ShowResults(Session, newBest, ProgressStore.GetBestScore(level.LevelId), receipt); RefreshHUD();
    }
    public bool SelectLevel(int id)
    {
        LevelConfig selected = null;
        foreach (var config in levels) if (config != null && config.LevelId == id) selected = config;
#if UNITY_EDITOR
        if (selected == null)
        {
            selected = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{id:00}.asset");
        }
#endif
        if (selected == null || !ProgressStore.IsUnlocked(id)) return false;
        ProgressStore.AbandonRound(roundId); roundId = null; roundFinalized = false;
        pool.Clear(); hud.ClearFeedback(); hud.HideTapHand();
        level = selected; ProgressStore.SelectLevel(id);
        Session = new RoundSession(level.Rules); spawner = new TargetSpawner(pool, hud.Arena, level);
        tutorialStarted = false; hud.ShowIntro(level, ProgressStore.GetBestScore(id)); RefreshHUD(); return true;
    }
    public LevelConfig[] GetAllLevels()
    {
        var list = new System.Collections.Generic.List<LevelConfig>(levels ?? new LevelConfig[0]);
#if UNITY_EDITOR
        for (int i = 1; i <= 20; i++)
        {
            if (!list.Exists(c => c != null && c.LevelId == i))
            {
                var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfig>($"Assets/Data/Levels/Level{i:00}.asset");
                if (loaded != null) list.Add(loaded);
            }
        }
#endif
        return list.ToArray();
    }
    public void ShowLevels()
    {
        ProgressStore.AbandonRound(roundId); roundId = null; roundFinalized = false;
        pool.Clear(); hud.ClearFeedback(); hud.HideTapHand(); spawner.Reset();
        Session = new RoundSession(level.Rules); tutorialStarted = false; hud.ShowLevels(GetAllLevels(), SelectLevel); RefreshHUD();
    }
    public void NextLevel()
    {
        if (Session.State == RoundState.Results && Session.Won) SelectLevel(level.LevelId + 1);
    }
    private void RefreshHUD() { displayedSecond = Mathf.CeilToInt(Session.RemainingTime); hud.Refresh(Session, level); }
    private void RefreshSettings() { hud.SetSettings(feedback.SoundEnabled, feedback.ReducedMotion, feedback.HapticsEnabled); }
    private void ToggleSound() { feedback.ToggleSound(); RefreshSettings(); }
    private void ToggleMotion() { feedback.ToggleMotion(); RefreshSettings(); }
    private void ToggleHaptics() { feedback.ToggleHaptics(); RefreshSettings(); }
    private void OnApplicationPause(bool paused) { if (paused) Pause(); }
    private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
    private void OnDestroy()
    {
        if (pool != null) pool.Dispose();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }
}
