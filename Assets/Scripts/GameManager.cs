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
        foreach (var config in levels) if (config != null && config.LevelId == ProgressStore.SelectedLevel) level = config;
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
        hud.LevelOne.onClick.AddListener(() => SelectLevel(1));
        hud.LevelTwo.onClick.AddListener(() => SelectLevel(2));
        RefreshSettings();
        hud.ShowIntro(level, ProgressStore.GetBestScore(level.LevelId));
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
                { tutorialStarted = true; hud.SetHint("DIFFERENT SIZES, SAME POINTS", "Build a streak to reach " + level.TargetScore + " points"); }
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
        pool.Release(target);
        tutorialStarted = true;
        hud.HideTapHand();
        int points = Session.Tap();
        feedback.PlayTap(Session.Combo);
        hud.Pop(position, points, color, feedback.ReducedMotion);
        hud.HitJuice(Session.Combo);
        RefreshHUD();
        if (Session.State == RoundState.Results) { Finish(); return; }
        if (level.Objective == ObjectiveKind.Score)
            hud.SetHint(Session.GoalMet ? "GOAL MET! KEEP SCORING" : "DIFFERENT SIZES, SAME POINTS", Session.GoalMet ? "Keep your streak until time runs out" : "Reach " + level.TargetScore + " points before time runs out");
        else if (Session.Hits < 3) hud.SetHint("NICE TAP! +" + points, "Each target adds points. Keep going!");
        else if (Session.Hits < 5) hud.SetHint("BUILD YOUR COMBO", "Tap targets before their rings run out");
        else hud.SetHint(Session.Combo >= 5 ? "KEEP THE STREAK ALIVE" : "YOU'VE GOT THIS", "Tap " + (level.RequiredTaps - Session.Hits) + " more to finish");
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
        if (selected == null || !ProgressStore.IsUnlocked(id)) return false;
        ProgressStore.AbandonRound(roundId); roundId = null; roundFinalized = false;
        pool.Clear(); hud.ClearFeedback(); hud.HideTapHand();
        level = selected; ProgressStore.SelectLevel(id);
        Session = new RoundSession(level.Rules); spawner = new TargetSpawner(pool, hud.Arena, level);
        tutorialStarted = false; hud.ShowIntro(level, ProgressStore.GetBestScore(id)); RefreshHUD(); return true;
    }
    public void ShowLevels()
    {
        ProgressStore.AbandonRound(roundId); roundId = null; roundFinalized = false;
        pool.Clear(); hud.ClearFeedback(); hud.HideTapHand(); spawner.Reset();
        Session = new RoundSession(level.Rules); tutorialStarted = false; hud.ShowLevels(); RefreshHUD();
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
