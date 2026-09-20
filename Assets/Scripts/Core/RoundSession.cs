using System;

namespace TapBlast
{
    public enum RoundState { Ready, Countdown, Playing, Paused, Results }
    // Keep the original serialized values for existing level assets.
    public enum ObjectiveKind { Taps, Score, Combo, Golden, Time, Boss }
    public enum TargetKind { Normal, Golden, Time, Boss }
    public readonly struct RoundRules
    {
        public readonly float Duration;
        public readonly int RequiredTaps, BasePoints, TwoStarScore, ThreeStarCombo;
        public readonly ObjectiveKind Objective;
        public readonly int TargetScore, LevelId;
        public readonly int ObjectiveCount, GoldenPoints;
        public readonly float TimeBonus;
        public RoundRules(float duration, int taps, int points, int twoStarScore, int threeStarCombo, ObjectiveKind objective = ObjectiveKind.Taps, int targetScore = 500, int levelId = 1, int objectiveCount = 1, int goldenPoints = 100, float timeBonus = 3)
        {
            Duration = Math.Max(1, duration); RequiredTaps = Math.Max(1, taps);
            BasePoints = Math.Max(1, points); TwoStarScore = Math.Max(1, twoStarScore);
            ThreeStarCombo = Math.Max(1, threeStarCombo);
            Objective = objective; TargetScore = Math.Max(1, targetScore); LevelId = Math.Max(1, levelId);
            ObjectiveCount = Math.Max(1, objectiveCount); GoldenPoints = Math.Max(1, goldenPoints); TimeBonus = Math.Max(0, timeBonus);
        }
    }
    /// <summary>Pure C# round rules with no Unity or scene dependencies.</summary>
    public sealed class RoundSession
    {
        private readonly RoundRules rules;
        private RoundState previousState;
        public RoundState State { get; private set; } = RoundState.Ready;
        public float RemainingTime { get; private set; }
        public int Score { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int GoldenHits { get; private set; }
        public int TimeHits { get; private set; }
        public int BossesDestroyed { get; private set; }
        public bool Won { get; private set; }
        public int LevelId => rules.LevelId;
        public int ObjectiveTarget => rules.Objective == ObjectiveKind.Score ? rules.TargetScore : rules.Objective == ObjectiveKind.Taps ? rules.RequiredTaps : rules.ObjectiveCount;
        public int ObjectiveProgress => rules.Objective switch
        {
            ObjectiveKind.Score => Score, ObjectiveKind.Combo => BestCombo,
            ObjectiveKind.Golden => GoldenHits, ObjectiveKind.Time => TimeHits,
            ObjectiveKind.Boss => BossesDestroyed, _ => Hits
        };
        public bool GoalMet => ObjectiveProgress >= ObjectiveTarget;
        public float Multiplier => Combo >= 50 ? 4f : Combo >= 30 ? 3f : Combo >= 20 ? 2f : Combo >= 10 ? 1.5f : Combo >= 5 ? 1.25f : 1f;
        public int Stars => !Won ? 0 : Score < rules.TwoStarScore ? 1 : Misses == 0 && BestCombo >= rules.ThreeStarCombo ? 3 : 2;
        public RoundSession(RoundRules rules) { this.rules = rules; RemainingTime = rules.Duration; }
        public void Reset()
        {
            Score = Hits = Misses = Combo = BestCombo = 0; Won = false;
            GoldenHits = TimeHits = BossesDestroyed = 0;
            RemainingTime = rules.Duration; State = RoundState.Countdown;
        }
        public void StartPlaying() { if (State == RoundState.Countdown) State = RoundState.Playing; }
        public int Tap(TargetKind kind = TargetKind.Normal, bool destroyed = true)
        {
            if (State != RoundState.Playing) return 0;
            Hits++; Combo++; BestCombo = Math.Max(BestCombo, Combo);
            if (kind == TargetKind.Golden) GoldenHits++;
            if (kind == TargetKind.Time) { TimeHits++; RemainingTime += rules.TimeBonus; }
            if (kind == TargetKind.Boss && destroyed) BossesDestroyed++;
            int points = (int)Math.Round((kind == TargetKind.Golden ? rules.GoldenPoints : rules.BasePoints) * Multiplier, MidpointRounding.AwayFromZero);
            Score += points;
            if (rules.Objective == ObjectiveKind.Taps && GoalMet) { Won = true; State = RoundState.Results; }
            return points;
        }
        public void Miss() { if (State == RoundState.Playing) { Misses++; Combo = 0; } }
        public void Tick(float delta)
        {
            if (State != RoundState.Playing || delta <= 0) return;
            RemainingTime = Math.Max(0, RemainingTime - delta);
            if (RemainingTime <= 0) { Won = GoalMet; State = RoundState.Results; }
        }
        public void Pause()
        {
            if (State != RoundState.Playing && State != RoundState.Countdown) return;
            previousState = State; State = RoundState.Paused;
        }
        public void Resume() { if (State == RoundState.Paused) State = previousState; }
    }
}
