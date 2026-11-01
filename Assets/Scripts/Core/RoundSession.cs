using System;

namespace TapBlast
{
    public enum RoundState { Ready, Countdown, Playing, Paused, Results }
    // Keep the original serialized values for existing level assets.
    public enum ObjectiveKind { Taps, Score, Combo, Golden, Time, Boss, Chains, MegaChains, NoBombs, Shield, ChainsNoBombs, ChainMaster }
    public enum TargetKind { Normal, Golden, Time, Boss, Chain, Bomb, Small, Priority, LinkedPair, Conduit, Multiplier }
    public enum GateKind { Taps, Small, Moving, Combo, Golden, Priority, Chains, MegaChains, Pairs, SafeTaps, Shield, ChainPairs }
    public enum HitSource { Direct, Chain, Ultimate }
    public static class LaneTiming
    {
        // Absolute simulation age avoids refresh-rate-dependent integration drift.
        public static float Fraction(float age, float warning, float transit, float cadence)
        {
            cadence = Math.Max(warning + transit, cadence);
            int cycle = (int)(Math.Max(0, age) / cadence);
            float local = Math.Max(0, age) - cycle * cadence;
            float t = Math.Max(0, Math.Min(1, (local - warning) / Math.Max(.001f, transit)));
            return cycle % 2 == 0 ? t : 1 - t;
        }
    }
    [Serializable] public sealed class WaveRules
    {
        public string title = "WAVE";
        public float duration = 15;
        public GateKind gate;
        public int quota = 1, pairs;
        public int coreHits, priorityOrder;
        public float coreWarning = .45f, coreOpen = 1.6f, coreClosed = 1.2f;
        public string GoalLabel => gate switch {
            GateKind.Small => "Small targets", GateKind.Moving => "Moving targets", GateKind.Combo => "Hits in a row",
            GateKind.Golden => "Golden targets", GateKind.Priority => "Priority !", GateKind.Chains or GateKind.ChainPairs => "Chains",
            GateKind.MegaChains => "Mega chains", GateKind.Pairs => "Ordered pairs", GateKind.SafeTaps => "Safe targets",
            GateKind.Shield => "Bombs blocked", _ => "Direct taps" };
    }
    [Serializable] public sealed class CampaignRules
    {
        public WaveRules[] waves = new WaveRules[0];
        public int pressureLimit = 4, maxCriticalMisses = 3, priorityMissLimit = 99;
        public int normalCharge = 3, goldenCharge = 6, chainCharge = 10;
        public bool noBombs, breakComboOnMiss = true;
    }
    public readonly struct RoundRules
    {
        public readonly float Duration;
        public readonly int RequiredTaps, BasePoints, TwoStarScore, ThreeStarCombo;
        public readonly ObjectiveKind Objective;
        public readonly int TargetScore, LevelId;
        public readonly int ObjectiveCount, GoldenPoints;
        public readonly float TimeBonus;
        public readonly int RequiredCombo, ShieldCharges, BombPenalty, ChainBonus;
        public readonly CampaignRules Campaign;
        public RoundRules(float duration, int taps, int points, int twoStarScore, int threeStarCombo, ObjectiveKind objective = ObjectiveKind.Taps, int targetScore = 500, int levelId = 1, int objectiveCount = 1, int goldenPoints = 100, float timeBonus = 3, int requiredCombo = 0, int shieldCharges = 0, int bombPenalty = 50, int chainBonus = 25, CampaignRules campaign = null)
        {
            Duration = Math.Max(1, duration); RequiredTaps = Math.Max(1, taps);
            BasePoints = Math.Max(1, points); TwoStarScore = Math.Max(1, twoStarScore);
            ThreeStarCombo = Math.Max(1, threeStarCombo);
            Objective = objective; TargetScore = Math.Max(1, targetScore); LevelId = Math.Max(1, levelId);
            ObjectiveCount = Math.Max(1, objectiveCount); GoldenPoints = Math.Max(1, goldenPoints); TimeBonus = Math.Max(0, timeBonus);
            RequiredCombo = Math.Max(0, requiredCombo); ShieldCharges = Math.Max(0, shieldCharges);
            BombPenalty = Math.Max(0, bombPenalty); ChainBonus = Math.Max(0, chainBonus);
            Campaign = campaign;
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
        public int Chains { get; private set; }
        public int MegaChains { get; private set; }
        public int LargestChain { get; private set; }
        public int BombHits { get; private set; }
        public int ShieldBlocks { get; private set; }
        public int ShieldsRemaining { get; private set; }
        public bool ShieldActive { get; private set; }
        public bool Won { get; private set; }
        public bool Scripted => rules.Campaign != null && rules.Campaign.waves.Length > 0;
        public bool Practice { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => Scripted ? rules.Campaign.waves.Length : 1;
        public WaveRules Wave => Scripted ? rules.Campaign.waves[Math.Min(WaveIndex, WaveCount - 1)] : null;
        public float WaveElapsed { get; private set; }
        public int WaveProgress { get; private set; }
        public int WavePairs { get; private set; }
        public int WaveCoreHits { get; private set; }
        public int WavePriorityOrder { get; private set; }
        public float BonusMultiplierRemaining { get; private set; }
        public bool SkillGateMet => Scripted && WaveProgress >= Wave.quota && WavePairs >= Wave.pairs && WavePriorityOrder >= Wave.priorityOrder;
        public bool CoreExposed => SkillGateMet && Wave.coreHits > WaveCoreHits && WaveElapsed >= Wave.coreWarning &&
            (WaveElapsed - Wave.coreWarning) % (Wave.coreOpen + Wave.coreClosed) < Wave.coreOpen;
        public int ClearedWaves { get; private set; }
        public int Pressure { get; private set; }
        public int PressureLimit => Scripted ? rules.Campaign.pressureLimit : 0;
        public int CriticalMisses { get; private set; }
        public int PriorityMisses { get; private set; }
        public int SuccessfulDirectTaps { get; private set; }
        public int EmptyTaps { get; private set; }
        public int InvalidTaps { get; private set; }
        public int HazardTaps { get; private set; }
        public int PairsCompleted { get; private set; }
        public int PriorityHits { get; private set; }
        public int SmallHits { get; private set; }
        public int MovingHits { get; private set; }
        public int UltimateCharge { get; private set; }
        public int UltimatesUsed { get; private set; }
        public float AddedTime { get; private set; }
        public float Accuracy => SuccessfulDirectTaps + EmptyTaps + InvalidTaps + HazardTaps + Misses == 0 ? 0 : (float)SuccessfulDirectTaps / (SuccessfulDirectTaps + EmptyTaps + InvalidTaps + HazardTaps + Misses);
        public string FailureReason { get; private set; } = "";
        public string LastMistake { get; private set; } = "";
        public int ActivePair { get; private set; } = -1;
        public float PairRemaining { get; private set; }
        public bool WaveGateMet => SkillGateMet && WaveCoreHits >= Wave.coreHits;
        public int LevelId => rules.LevelId;
        public int ObjectiveTarget => rules.Objective == ObjectiveKind.Score ? rules.TargetScore : rules.Objective == ObjectiveKind.Taps || rules.Objective == ObjectiveKind.NoBombs ? rules.RequiredTaps : rules.ObjectiveCount;
        public int ObjectiveProgress => rules.Objective switch
        {
            ObjectiveKind.Score => Score, ObjectiveKind.Combo => BestCombo,
            ObjectiveKind.Golden => GoldenHits, ObjectiveKind.Time => TimeHits,
            ObjectiveKind.Boss => BossesDestroyed,
            ObjectiveKind.Chains or ObjectiveKind.ChainsNoBombs or ObjectiveKind.ChainMaster => Chains,
            ObjectiveKind.MegaChains => MegaChains, ObjectiveKind.Shield => ShieldBlocks, _ => Hits
        };
        public bool BombObjectiveFailed => BombHits > 0 && (rules.Objective == ObjectiveKind.NoBombs || rules.Objective == ObjectiveKind.ChainsNoBombs);
        public bool GoalMet => Scripted ? ClearedWaves >= WaveCount && Pressure < PressureLimit : ObjectiveProgress >= ObjectiveTarget && !BombObjectiveFailed && BestCombo >= rules.RequiredCombo;
        public float Multiplier => Combo >= 50 ? 4f : Combo >= 30 ? 3f : Combo >= 20 ? 2f : Combo >= 10 ? 1.5f : Combo >= 5 ? 1.25f : 1f;
        public int Stars => !Won || Practice ? 0 : Scripted ? (BombHits == 0 && CriticalMisses <= rules.Campaign.maxCriticalMisses ? 3 : Score >= rules.TwoStarScore || Accuracy >= .85f ? 2 : 1) : Score < rules.TwoStarScore ? 1 : Misses == 0 && BombHits == 0 && BestCombo >= rules.ThreeStarCombo ? 3 : 2;
        public RoundSession(RoundRules rules) { this.rules = rules; RemainingTime = rules.Duration; ShieldsRemaining = rules.ShieldCharges; }
        public void Reset()
        {
            Score = Hits = Misses = Combo = BestCombo = 0; Won = false;
            GoldenHits = TimeHits = BossesDestroyed = 0;
            Chains = MegaChains = LargestChain = BombHits = ShieldBlocks = 0;
            ShieldActive = false; ShieldsRemaining = rules.ShieldCharges;
            RemainingTime = rules.Duration; State = RoundState.Countdown;
            WaveIndex = WaveProgress = WavePairs = ClearedWaves = Pressure = CriticalMisses = PriorityMisses = 0;
            SuccessfulDirectTaps = EmptyTaps = InvalidTaps = HazardTaps = PairsCompleted = PriorityHits = SmallHits = MovingHits = UltimateCharge = UltimatesUsed = 0;
            WaveElapsed = AddedTime = PairRemaining = 0; ActivePair = -1;
            WaveCoreHits = WavePriorityOrder = 0; BonusMultiplierRemaining = 0;
            FailureReason = LastMistake = "";
        }
        public void StartPlaying() { if (State == RoundState.Countdown) State = RoundState.Playing; }
        public int Tap(TargetKind kind = TargetKind.Normal, bool destroyed = true, HitSource source = HitSource.Direct, bool moving = false)
        {
            if (State != RoundState.Playing) return 0;
            if (Scripted && kind == TargetKind.Boss)
            {
                if (source != HitSource.Direct || !CoreExposed) { if (source == HitSource.Direct) InvalidTap("CORE SHIELDED"); return 0; }
                WaveCoreHits++;
            }
            if (kind == TargetKind.Bomb) return HitBomb();
            Hits++; if (!Scripted || source == HitSource.Direct) { Combo++; BestCombo = Math.Max(BestCombo, Combo); }
            if (Scripted && source == HitSource.Direct)
            {
                SuccessfulDirectTaps++;
                UltimateCharge = Math.Min(100, UltimateCharge + (kind == TargetKind.Golden ? Math.Max(0, rules.Campaign.goldenCharge) : Math.Max(0, rules.Campaign.normalCharge)));
                if (kind == TargetKind.Small) SmallHits++;
                if (moving) MovingHits++;
                if (kind == TargetKind.Priority) PriorityHits++;
                if (Wave.gate == GateKind.Taps || Wave.gate == GateKind.SafeTaps ||
                    Wave.gate == GateKind.Small && kind == TargetKind.Small ||
                    Wave.gate == GateKind.Moving && moving || Wave.gate == GateKind.Golden && kind == TargetKind.Golden ||
                    Wave.gate == GateKind.Priority && kind == TargetKind.Priority) WaveProgress++;
                if (Wave.gate == GateKind.Combo) WaveProgress = Math.Max(WaveProgress, Combo);
            }
            if (kind == TargetKind.Golden) GoldenHits++;
            if (kind == TargetKind.Time) { TimeHits++; float extra = Scripted ? Math.Min(rules.TimeBonus, Math.Max(0, 6 - AddedTime)) : rules.TimeBonus; RemainingTime += extra; AddedTime += extra; if (Scripted) WaveElapsed -= extra; }
            if (kind == TargetKind.Boss && destroyed) BossesDestroyed++;
            int points = (int)Math.Round((kind == TargetKind.Golden ? rules.GoldenPoints : kind == TargetKind.Small ? 15 : rules.BasePoints) * Multiplier * (BonusMultiplierRemaining > 0 ? 2 : 1), MidpointRounding.AwayFromZero);
            if (kind == TargetKind.Multiplier && source == HitSource.Direct) BonusMultiplierRemaining = 4;
            Score += points;
            if (!Scripted && rules.Objective == ObjectiveKind.Taps && GoalMet) { Won = true; State = RoundState.Results; }
            return points;
        }
        public bool ActivateShield()
        {
            if (State != RoundState.Playing || ShieldActive || ShieldsRemaining <= 0) return false;
            ShieldsRemaining--; ShieldActive = true; return true;
        }
        private int HitBomb()
        {
            if (Scripted) HazardTaps++;
            if (ShieldActive) { ShieldActive = false; ShieldBlocks++; if (Scripted && Wave.gate == GateKind.Shield) WaveProgress++; return 0; }
            BombHits++; Combo = 0;
            if (Scripted) { AddPressure("BOMB HIT"); if (rules.Campaign.noBombs) Fail("BOMB HIT — ZERO BOMBS REQUIRED"); }
            int penalty = Math.Min(Score, rules.BombPenalty); Score -= penalty; return -penalty;
        }
        // Called once after a connected group has resolved, never once per member.
        public int RecordChain(int targets)
        {
            if (State != RoundState.Playing || targets < 3) return 0;
            Chains++; LargestChain = Math.Max(LargestChain, targets);
            if (targets >= 8) MegaChains++;
            if (Scripted)
            {
                UltimateCharge = Math.Min(100, UltimateCharge + Math.Max(0, rules.Campaign.chainCharge));
                if (Wave.gate == GateKind.Chains || Wave.gate == GateKind.ChainPairs || Wave.gate == GateKind.MegaChains && targets >= 8) WaveProgress++;
            }
            int bonus = rules.ChainBonus * (targets >= 12 ? 16 : targets >= 8 ? 8 : targets >= 5 ? 3 : 1);
            Score += bonus; return bonus;
        }
        public void Miss(bool critical = false, bool priority = false, bool required = true)
        {
            if (State != RoundState.Playing) return;
            Misses++; if (!Scripted || required && rules.Campaign.breakComboOnMiss) Combo = 0;
            if (Scripted && critical) { CriticalMisses++; if (priority) PriorityMisses++; AddPressure(priority ? "PRIORITY MISSED" : "REQUIRED TARGET MISSED"); }
            if (Scripted && PriorityMisses > rules.Campaign.priorityMissLimit) Fail("TOO MANY PRIORITY MISSES");
        }
        public void EmptyTap() { if (State == RoundState.Playing && Scripted) { EmptyTaps++; LastMistake = "EMPTY TAP"; } }
        public void InvalidTap(string reason) { if (State == RoundState.Playing && Scripted) { InvalidTaps++; LastMistake = reason; } }
        public void AddPressure(string reason)
        {
            if (!Scripted || State != RoundState.Playing) return;
            Pressure++; LastMistake = reason;
            if (Pressure >= PressureLimit) Fail(reason + " — PRESSURE LIMIT");
        }
        public void Fail(string reason) { if (State != RoundState.Playing) return; Won = false; FailureReason = reason; State = RoundState.Results; }
        public void SetPractice(bool value) { if (State == RoundState.Ready || State == RoundState.Results || State == RoundState.Countdown) Practice = value; }
        // 0 wrong/duplicate, 1 first node, 2 completed pair. Wrong order resets this pair, not the round.
        public int PairTap(int pairId, int order)
        {
            if (!Scripted || State != RoundState.Playing) return 0;
            if (order == 1 && ActivePair == -1) { ActivePair = pairId; PairRemaining = 2.5f; Tap(TargetKind.LinkedPair); return 1; }
            if (order == 2 && ActivePair == pairId && PairRemaining > 0)
            {
                Tap(TargetKind.LinkedPair); PairsCompleted++; ActivePair = -1; PairRemaining = 0;
                if (Wave.gate == GateKind.Pairs) WaveProgress++;
                if (Wave.gate == GateKind.ChainPairs) WavePairs++;
                return 2;
            }
            ActivePair = -1; PairRemaining = 0; InvalidTap("WRONG ORDER — TAP 1 THEN 2"); return 0;
        }
        public void ResetPair() { ActivePair = -1; PairRemaining = 0; }
        public bool PriorityTap(int order)
        {
            if (!Scripted || State != RoundState.Playing) return false;
            if (Wave.priorityOrder > 0 && WavePriorityOrder < Wave.priorityOrder)
            {
                if (order != WavePriorityOrder + 1) { InvalidTap("TAP PRIORITY IN ORDER"); return false; }
                WavePriorityOrder++;
            }
            Tap(TargetKind.Priority); return true;
        }
        public bool UseUltimate()
        {
            if (!Scripted || State != RoundState.Playing || UltimateCharge < 100) return false;
            UltimateCharge = 0; UltimatesUsed++; return true;
        }
        public void Tick(float delta)
        {
            if (State != RoundState.Playing || delta <= 0) return;
            if (Scripted)
            {
                if (ActivePair >= 0) { PairRemaining = Math.Max(0, PairRemaining - delta); if (PairRemaining <= 0) { ActivePair = -1; LastMistake = "PAIR WINDOW MISSED"; } }
                float scaled = Practice ? delta * .7f : delta;
                BonusMultiplierRemaining = Math.Max(0, BonusMultiplierRemaining - scaled);
                WaveElapsed += scaled;
                RemainingTime = Math.Max(0, RemainingTime - scaled);
                while (WaveElapsed + .0001f >= Wave.duration)
                {
                    if (Wave.gate == GateKind.Shield) WaveProgress = ShieldBlocks;
                    if (!WaveGateMet) { Fail("WAVE " + (WaveIndex + 1) + ": " + Wave.GoalLabel + " " + WaveProgress + "/" + Wave.quota + (Wave.pairs > 0 ? " + pairs " + WavePairs + "/" + Wave.pairs : "") + (Wave.coreHits > 0 ? " + core " + WaveCoreHits + "/" + Wave.coreHits : "") + (Wave.priorityOrder > 0 ? " + order " + WavePriorityOrder + "/" + Wave.priorityOrder : "")); return; }
                    ClearedWaves++;
                    if (ClearedWaves == WaveCount) { Won = Pressure < PressureLimit; State = RoundState.Results; return; }
                    WaveElapsed = Math.Max(0, WaveElapsed - Wave.duration);
                    WaveIndex++; WaveProgress = WavePairs = WaveCoreHits = WavePriorityOrder = 0; ActivePair = -1; PairRemaining = 0;
                }
                if (RemainingTime <= 0 && WaveElapsed + .001f < Wave.duration) Fail("TIME UP — GATE INCOMPLETE");
                return;
            }
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
