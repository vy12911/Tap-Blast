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
    private TargetView[] chainTargets;
    // Combo milestones get a short callout word over the arena.
    private static readonly int[] milestoneCombos = { 5, 10, 20, 30, 50 };
    private static readonly string[] milestoneWords = { "NICE!", "GREAT!", "AWESOME!", "AMAZING!", "LEGENDARY!" };
    private static readonly Color[] milestoneColors =
    {
        new Color32(120, 150, 255, 255), new Color32(90, 200, 255, 255), new Color32(170, 120, 255, 255),
        new Color32(255, 120, 190, 255), new Color32(255, 190, 60, 255)
    };

    private void Start()
    {
        // Hard 60 fps cap on every platform: VSync must be off or targetFrameRate is ignored
        // (desktop/editor), and 120 Hz phones would otherwise run at their panel rate.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        if (level == null) { Debug.LogError("Assign a LevelConfig to GameManager.", this); enabled = false; return; }
        if (levels == null || levels.Length == 0) levels = new[] { level };
        foreach (var config in levels) if (config != null && config.LevelId == ProgressStore.SelectedLevel) level = config;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Session = new RoundSession(level.Rules);
        if (hud == null || hud.TargetPrefab == null) { Debug.LogError("Assign the scene Canvas and Normal Target prefab to GameManager.", this); enabled = false; return; }
        hud.Initialize();
        feedback = gameObject.AddComponent<FeedbackPlayer>();
        feedback.Initialize();
        int capacity = level.MaxTargets;
        foreach (var config in levels) if (config != null) capacity = Mathf.Max(capacity, config.MaxTargets);
        pool = new TargetPool(hud.Arena, hud.TargetPrefab, capacity, OnTargetTapped, OnTargetExpired, OnChainImpact);
        chainTargets = new TargetView[capacity];
        hud.WorldEffects.Bind(pool);
        spawner = new TargetSpawner(pool, hud.Arena, level);
        hud.Play.onClick.AddListener(BeginCountdown);
        hud.Pause.onClick.AddListener(Pause);
        hud.Resume.onClick.AddListener(Resume);
        hud.Restart.onClick.AddListener(BeginCountdown);
        hud.Replay.onClick.AddListener(BeginCountdown);
        hud.Sound.onClick.AddListener(ToggleSound);
        hud.Motion.onClick.AddListener(ToggleMotion);
        hud.Next.onClick.AddListener(NextLevel);
        hud.Shield.onClick.AddListener(ActivateShield);
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
            if (hud.GetComponent<GuiNavigation>()?.TryCloseWallet() != true)
            {
                if (Session.State == RoundState.Paused) Resume(); else Pause();
            }
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
                RefreshHUD();
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
        if (Session.State != RoundState.Playing || !target.Active || target.BlastPending) return;
        if (target.Kind == TargetKind.Bomb)
        {
            bool protectedTap = Session.ShieldActive;
            Vector2 bombPosition = target.Position;
            pool.Release(target);
            int penalty = Session.Tap(TargetKind.Bomb);
            feedback.PlayHazard(protectedTap);
            hud.WorldEffects.Hazard(bombPosition, protectedTap);
            hud.Shake(protectedTap ? 6 : 16, protectedTap ? .2f : .34f, protectedTap ? .2f : World2Effects.BombFuseSeconds);
            if (protectedTap) hud.ShieldJuice();
            hud.HazardPop(bombPosition, protectedTap ? "BLOCKED!" : penalty < 0 ? "BOOM!  " + penalty : "BOOM!", protectedTap ? UIArt.Cyan : target.Tint,
                feedback.ReducedMotion, target, protectedTap);
            hud.HitJuice(Session.Combo);
            hud.SetHint(protectedTap ? "SHIELD BLOCKED THE BOMB" : "BOMB HIT! STREAK RESET",
                protectedTap ? (Session.GoalMet ? "Goal done! Keep scoring for more stars." : "Shield used. Leave the other Bombs alone.")
                : Session.BombObjectiveFailed ? "This goal needed zero Bomb taps. Replay to try again."
                : level.Objective == ObjectiveKind.Shield && Session.ShieldsRemaining > 0 ? "Press SHIELD first, then tap a Bomb."
                : "Leave red Bombs (X) alone. They fade safely.");
            RefreshHUD(); return;
        }
        int chainCount = pool.FindChain(target, chainTargets);
        if (chainCount >= 3)
        {
            int previousCombo = Session.Combo;
            Vector2 chainOrigin = target.Position;
            Color chainTint = target.Tint;
            int reactionStyle = hud.WorldEffects.Chain(chainTargets, chainCount);
            int total = 0;
            int blastCount = chainCount;
            for (int i = 0; i < blastCount; i++)
            {
                var member = chainTargets[i];
                int memberPoints = Session.Tap(member.Kind); total += memberPoints;
                hud.Pop(member.Position, memberPoints, member.Tint, feedback.ReducedMotion, i * TargetView.ChainStepSeconds,
                    chainCount >= 8 ? 1.65f : chainCount >= 5 ? 1.35f : 1.15f, reactionStyle, member);
                if (member.Kind == TargetKind.Time) hud.TimeJuice();
                member.BeginBlast(i * TargetView.ChainStepSeconds, feedback.ReducedMotion, i); chainTargets[i] = null;
            }
            total += Session.RecordChain(chainCount);
            hud.WorldEffects.Hit(chainOrigin, chainTint, previousCombo, Session.Combo);
            feedback.PlayTap(Session.Combo); feedback.PlayChain(chainCount); hud.HitJuice(Session.Combo);
            // The callout and shake land as the last link detonates.
            float finish = (chainCount - 1) * TargetView.ChainStepSeconds;
            hud.Callout(chainCount >= 8 ? "MEGA CHAIN!" : chainCount >= 5 ? "SUPER CHAIN!" : "CHAIN!",
                chainCount >= 8 ? (Color)new Color32(255, 190, 60, 255) : chainCount >= 5 ? (Color)new Color32(255, 140, 90, 255) : chainTint, finish + .06f);
            if (chainCount >= 5) hud.Shake(chainCount >= 8 ? 10 : 5, chainCount >= 8 ? .38f : .2f, finish);
            hud.SetHint((chainCount >= 8 ? "MEGA CHAIN! " : chainCount >= 5 ? "CHAIN 2! " : "CHAIN! ") + chainCount + " TARGETS  +" + total,
                level.GoalText);
            tutorialStarted = true; hud.HideTapHand(); RefreshHUD(); return;
        }
        Vector2 position = target.Position;
        Color color = target.Tint;
        float diameter = target.Diameter;

        TargetKind kind = target.Kind;
        bool destroyed = kind != TargetKind.Boss || target.Damage();
        if (destroyed) pool.Release(target);
        tutorialStarted = true;
        hud.HideTapHand();
        int priorCombo = Session.Combo;
        int points = Session.Tap(kind, destroyed);
        hud.WorldEffects.Hit(position, color, priorCombo, Session.Combo);
        hud.WorldEffects.Burst(position, color, diameter * .5f, kind);
        feedback.PlayTap(Session.Combo);
        hud.Pop(position, points, color, feedback.ReducedMotion, 0, 1, 0, target,
            kind == TargetKind.Golden || kind == TargetKind.Boss ? 1.3f : 1);
        hud.HitJuice(Session.Combo);
        if (kind == TargetKind.Boss)
        {
            hud.Shake(destroyed ? 12 : 5, destroyed ? .36f : .14f);
            if (destroyed) { hud.WorldEffects.Finale(position, UIArt.Gold); hud.Callout("CORE DESTROYED!", UIArt.Gold); }
        }
        else
            for (int i = 0; i < milestoneCombos.Length; i++)
                if (priorCombo < milestoneCombos[i] && Session.Combo >= milestoneCombos[i]) hud.Callout(milestoneWords[i], milestoneColors[i]);
        RefreshHUD();

        if (Session.State == RoundState.Results) { Finish(); return; }
        if (kind == TargetKind.Boss)
            hud.SetHint(destroyed ? "GOLDEN CORE DESTROYED!" : "CORE HEALTH " + target.Health + " / " + target.MaxHealth,
                destroyed ? "Keep scoring for mastery until time runs out" : "Every hit damages the Core and builds your combo");
        else if (kind == TargetKind.Golden)
            hud.SetHint("GOLDEN! +" + points, "Collected " + Session.GoldenHits + " Golden targets");
        else if (kind == TargetKind.Time)
        {
            hud.TimeJuice();
            hud.SetHint("TIME BUBBLE! +" + level.TimeBonus.ToString("0.#") + " SECONDS", "Collected " + Session.TimeHits + " Time Bubbles");
        }
        else if (level.FirstTapTutorial && Session.Hits < 3)
            hud.SetHint("NICE TAP! +" + points, "Each target adds points. Keep going!");
        else if (level.FirstTapTutorial && Session.Hits < 5)
            hud.SetHint("BUILD YOUR COMBO", "Tap targets before their rings run out");
        else
            hud.SetHint(Session.GoalMet ? "GOAL MET! KEEP SCORING" : level.GoalText,
                Session.GoalMet ? "Keep your streak until time runs out" : level.MechanicHint);
    }
    private void OnChainImpact(TargetView target)
    {
        if (!feedback.ReducedMotion) feedback.PlayChainImpact(target.BlastIndex);
    }
    private void OnTargetExpired(TargetView target)
    {
        pool.Release(target);
        if (Session.State != RoundState.Playing) return;
        if (target.Kind == TargetKind.Bomb) { hud.WorldEffects.Fizzle(target.Position); return; }
        Session.Miss();
        hud.MissJuice(target.Position);
        hud.SetHint("TARGET MISSED", "Your combo reset. Try the next one!");
        RefreshHUD();
    }

    public void Pause() { if (PauseRound()) hud.ShowPause(); }
    /// <summary>Pauses under an overlay (the wallet) without opening the pause menu; Resume restores play.</summary>
    public void PauseBehindOverlay() { PauseRound(); }
    private bool PauseRound()
    {
        if (Session == null || (Session.State != RoundState.Playing && Session.State != RoundState.Countdown)) return false;
        resumeState = Session.State; Session.Pause(); pool.SetInteractable(false); return true;
    }
    public void Resume()
    {
        if (Session.State != RoundState.Paused) return;
        Session.Resume(); pool.SetInteractable(true);
        if (resumeState == RoundState.Countdown) hud.ShowCountdown(); else hud.ShowPlaying();
        RefreshHUD();
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
    public LevelConfig[] GetAllLevels()
    {
        // Use the saved campaign references in both the Editor and player builds.
        return levels == null ? new LevelConfig[0] : (LevelConfig[])levels.Clone();
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
    public void ActivateShield()
    {
        if (Session == null || !Session.ActivateShield()) return;
        hud.ShieldJuice();
        hud.WorldEffects.ShieldActivate();
        hud.SetHint("SHIELD ON! NOW TAP A BOMB", "The Shield stops it: no points lost, streak kept.");
        RefreshHUD();
    }
    private void RefreshHUD() { displayedSecond = Mathf.CeilToInt(Session.RemainingTime); hud.Refresh(Session, level); }
    private void RefreshSettings() { hud.SetSettings(feedback.SoundEnabled, feedback.ReducedMotion); }
    private void ToggleSound() { feedback.ToggleSound(); RefreshSettings(); }
    private void ToggleMotion() { feedback.ToggleMotion(); pool.SetReducedMotion(feedback.ReducedMotion); RefreshSettings(); }
    private void OnApplicationPause(bool paused) { if (paused) Pause(); }
    private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }
    private void OnDestroy()
    {
        if (pool != null) pool.Dispose();
        Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }
}
