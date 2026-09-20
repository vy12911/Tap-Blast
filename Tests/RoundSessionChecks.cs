using System;
using TapBlast;

// Dependency-free executable checks. Run with Tools/Validate-Level1.ps1.
internal static class RoundSessionChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++; Console.WriteLine("PASS " + message);
    }
    private static RoundSession NewRound()
    {
        var session = new RoundSession(new RoundRules(60, 20, 10, 240, 20));
        session.Reset(); session.StartPlaying(); return session;
    }
    public static int Main()
    {
        try
        {
            var s = new RoundSession(new RoundRules(60, 20, 10, 240, 20));
            Check(s.Tap() == 0 && s.Hits == 0, "Ready state rejects taps");
            s.Reset(); s.Tick(8); Check(s.RemainingTime == 60 && s.Tap() == 0, "Countdown does not consume time or accept taps");
            s.Pause(); s.Resume(); Check(s.State == RoundState.Countdown, "Countdown pause resumes into countdown");
            s.StartPlaying(); s.Tap(); s.Pause(); s.Tick(10); s.Tap(); s.Miss();
            Check(s.RemainingTime == 60 && s.Hits == 1 && s.Misses == 0, "Pause freezes time, taps and misses");
            s.Resume(); s.Tick(1); Check(s.State == RoundState.Playing && s.RemainingTime == 59, "Resume restores play");
            s = NewRound(); for (int i = 0; i < 4; i++) s.Tap();
            Check(s.Score == 40 && s.Multiplier == 1, "First four targets score ten each");
            Check(s.Tap() == 13 && s.Multiplier == 1.25f, "Fifth hit applies multiplier with explicit rounding");
            s.Miss(); Check(s.Combo == 0 && s.BestCombo == 5 && s.Misses == 1, "Miss resets current streak and preserves best");
            Check(s.Tap() == 10, "Score multiplier resets after a miss");
            s = NewRound(); for (int i = 0; i < 20; i++) s.Tap();
            Check(s.State == RoundState.Results && s.Won && s.Score == 275 && s.Stars == 3, "Twenty clean taps complete with 275 points and three stars");
            s.Tap(); s.Miss(); s.Tick(100); s.Pause();
            Check(s.Hits == 20 && s.Misses == 0 && s.Score == 275 && s.Won, "Completed results cannot change or complete twice");
            s.Reset(); Check(s.Hits == 0 && s.Score == 0 && s.Misses == 0 && s.Combo == 0 && s.BestCombo == 0 && s.Stars == 0 && !s.Won && s.RemainingTime == 60, "Replay clears all round state");
            s = NewRound(); s.Tick(60); Check(!s.Won && s.State == RoundState.Results && s.Stars == 0, "Time expiry fails unfinished objective");
            Check(s.Tap() == 0 && s.Hits == 0, "Late tap cannot rescue expired round");
            s = NewRound(); for (int i = 0; i < 20; i++) { s.Tap(); s.Miss(); }
            Check(s.Won && s.Score == 200 && s.Stars == 1, "Completion alone awards one star");
            s = NewRound(); s.Tap(); s.Miss(); for (int i = 1; i < 20; i++) s.Tap();
            Check(s.Won && s.Score == 265 && s.Stars == 2, "Score goal with a miss awards two stars");
            s = NewRound(); s.Tick(-1); Check(s.RemainingTime == 60, "Negative delta cannot add time");
            var levelTwo = new RoundRules(45, 20, 10, 750, 20, ObjectiveKind.Score, 500, 2);
            s = new RoundSession(levelTwo); s.Reset(); s.StartPlaying();
            for (int i = 0; i < 20; i++) s.Tap();
            Check(s.LevelId == 2 && s.State == RoundState.Playing && !s.GoalMet, "Level 2 does not end after twenty taps");
            while (!s.GoalMet) s.Tap();
            Check(s.Score >= 500 && s.State == RoundState.Playing && !s.Won, "Reaching 500 leaves time to pursue mastery");
            s.Tick(45); Check(s.Won && s.Stars == 1, "Level 2 score goal awards one star at timeout");
            s = new RoundSession(levelTwo); s.Reset(); s.StartPlaying();
            while (s.Score < 750) s.Tap();
            s.Tick(45); Check(s.Won && s.Stars == 3, "750 plus clean twenty-hit streak earns three stars");
            s = new RoundSession(levelTwo); s.Reset(); s.StartPlaying(); s.Miss();
            while (s.Score < 750) s.Tap();
            s.Tick(45); Check(s.Won && s.Stars == 2, "Missed target prevents Level 2 mastery star");
            s = new RoundSession(levelTwo); s.Reset(); s.StartPlaying(); s.Tap(); s.Tick(45);
            Check(!s.Won && s.Stars == 0, "Level 2 fails below 500 points");
            s.Reset(); Check(s.LevelId == 2 && s.Score == 0 && s.RemainingTime == 45, "Level 2 replay resets state and retains its rules");
            var level10 = new RoundRules(50, 20, 10, 3000, 25, ObjectiveKind.Boss, 3000, 10);
            s = new RoundSession(level10); s.Reset(); s.StartPlaying();
            Check(s.LevelId == 10 && s.State == RoundState.Playing, "Level 10 initializes correctly");
            for (int i = 0; i < 150; i++) s.Tap();
            Check(!s.GoalMet, "High score cannot replace destroying the Core");
            for (int i = 0; i < 4; i++) s.Tap(TargetKind.Boss, false);
            Check(s.BossesDestroyed == 0 && !s.GoalMet, "Partial Core damage does not satisfy objective");
            s.Tap(TargetKind.Boss, true);
            Check(s.BossesDestroyed == 1 && s.GoalMet && s.State == RoundState.Playing, "Core destruction earns goal and leaves time for mastery");
            s.Tick(50); Check(s.Won, "Level 10 completed successfully");
            s.Reset(); Check(s.BossesDestroyed == 0 && !s.GoalMet, "Replay clears Core objective");
            foreach (int goal in new[] { 5, 10, 15 })
            {
                s = new RoundSession(new RoundRules(45, 20, 10, 600, 20, ObjectiveKind.Combo, 500, 3, goal));
                s.Reset(); s.StartPlaying();
                for (int i = 0; i < 40; i++) { s.Tap(); s.Miss(); }
                Check(!s.GoalMet, "Total taps cannot replace a " + goal + "-hit streak");
                for (int i = 0; i < goal; i++) s.Tap();
                Check(s.GoalMet && s.State == RoundState.Playing, "Streak goal " + goal + " allows timed mastery");
                s.Miss(); s.Tick(45);
                Check(s.Won && s.Stars < 3, "Reached streak remains earned but expiry prevents mastery");
            }
            s = new RoundSession(new RoundRules(45, 20, 10, 1800, 15, ObjectiveKind.Golden, 500, 6, 3));
            s.Reset(); s.StartPlaying();
            Check(s.Tap(TargetKind.Golden) == 100 && s.Score == 100 && s.GoldenHits == 1, "Golden score and collection commit together");
            s.Tap(); s.Tap(); s.Tap();
            Check(s.Tap(TargetKind.Golden) == 125 && s.Score == 255, "Golden value uses the current combo multiplier");
            Check(!s.GoalMet, "Normal taps do not count as Golden collection");
            s.Tap(TargetKind.Golden); s.Tick(45); Check(s.Won && s.Stars == 1, "Three Golden targets satisfy collection without forcing two stars");
            s.Reset(); Check(s.GoldenHits == 0 && s.TimeHits == 0, "Replay resets special collection counters");
            s = new RoundSession(new RoundRules(35, 20, 10, 1400, 20, ObjectiveKind.Time, 500, 8, 3));
            s.Reset(); s.StartPlaying(); s.Tick(5); s.Tap(TargetKind.Time);
            Check(s.RemainingTime == 33 && s.TimeHits == 1 && s.Score == 10, "Time Bubble adds three seconds and normal tap points");
            s.Pause(); s.Tap(TargetKind.Time); s.Tick(5);
            Check(s.RemainingTime == 33 && s.TimeHits == 1, "Pause rejects special rewards and clock changes");
            s.Resume(); s.Tap(TargetKind.Time); s.Tap(TargetKind.Time); s.Tick(100);
            Check(s.Won && s.TimeHits == 3, "Time collection goal completes at timeout");
            float finishedTime = s.RemainingTime; int finishedScore = s.Score;
            s.Tap(TargetKind.Time); s.Tap(TargetKind.Golden);
            Check(s.RemainingTime == finishedTime && s.Score == finishedScore && s.TimeHits == 3, "Results reject late special taps");
            Console.WriteLine(checks + " round-rule checks passed."); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
