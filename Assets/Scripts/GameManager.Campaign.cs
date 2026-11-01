using TapBlast;
using UnityEngine;

public sealed partial class GameManager
{
    private CampaignDirector director;
    private int shownWave = -1;
    private bool practiceRequested;
    private float resultDelay, ultimateSlowRemaining, lowFxSeconds;
    private bool bossVictoryStarted;
    private int encounterShown = -1;
    private void TickV5Presentation()
    {
        var e = director.CurrentEncounter(Session);
        hud.WorldEffects.SetEncounter(e, e == null ? 0 : Session.WaveElapsed - e.at);
        if (e != null && encounterShown != Session.WaveIndex * 1000 + Mathf.RoundToInt(e.at * 10))
        {
            encounterShown = Session.WaveIndex * 1000 + Mathf.RoundToInt(e.at * 10);
            string cue = e.kind == ArenaEventKind.SafeCorridor ? "SAFE LANE | LEAVE X ALONE" : e.kind == ArenaEventKind.TempoFakeout ? "PREVIEW | WAIT FOR !" : e.kind == ArenaEventKind.PulsePreview ? "PULSE | CHOOSE YOUR TARGET" : e.kind == ArenaEventKind.SafeOpening ? "TAP THE GLOWING TARGET" : e.kind == ArenaEventKind.TwoLaneRhythm ? "LEFT | RIGHT | KEEP THE RHYTHM" : e.kind == ArenaEventKind.ConduitPreview ? "R ROUTER | TAP TO LINK" : e.kind == ArenaEventKind.PriorityFinale ? "CLEAR THE GATE | WATCH FOR !" : "ARROW | LANE SHIFT NEXT";
            hud.SetHint(cue, level.MechanicHint);
        }
        if (level.V5Boss)
        {
            var phase = level.BossPhases[Session.WaveIndex];
            hud.WorldEffects.SetBoss(phase.signature, true, Session.CoreExposed, Session.WaveElapsed, phase.corePosition);
            if (Session.SkillGateMet && Session.WaveCoreHits < Session.Wave.coreHits)
                hud.SetHint(Session.CoreExposed ? "CORE OPEN | TAP GOLD CORE" : "SHIELD CLOSED | WAIT", phase.teachingHint);
        }
    }
    private void TickResultDelay(float delta)
    {
        resultDelay = Mathf.Max(0, resultDelay - delta);
        if (resultDelay <= 0) Finish();
    }
    private void TrackExit(string name)
    {
        if (Session == null || !Session.Scripted || string.IsNullOrEmpty(roundId) || Session.State == RoundState.Ready || Session.State == RoundState.Results) return;
        CampaignTelemetry.Add(name, level.LevelId, Session.WaveIndex, Session.Practice ? 1 : 0);
        CampaignTelemetry.Flush();
    }
    private void BeginCampaignPlay()
    {
        director.Reset(); shownWave = -1; encounterShown = -1; bossVictoryStarted = false; tutorialStarted = true;
        hud.SetHint(level.Waves[0].rules.title, level.MechanicHint);
        CampaignTelemetry.Add("wave_start", level.LevelId, 0, 0);
        if (level.LevelId == 1) hud.ShowTapHand();
        TickCampaign(0);
    }
    private void TickCampaign(float delta)
    {
        if (Session.State == RoundState.Results) { if (resultDelay > 0) TickResultDelay(delta); else Finish(); return; }
        if (Session.State != RoundState.Playing) return;
        CampaignTelemetry.SampleFrame(delta);
        lowFxSeconds = delta > .04f ? 1 : Mathf.Max(0, lowFxSeconds - delta);
        hud.WorldEffects.SetPerformanceBudget(lowFxSeconds > 0);
        if (ultimateSlowRemaining > 0)
        {
            float part = Mathf.Min(delta, ultimateSlowRemaining); ultimateSlowRemaining -= part; delta -= part * .65f;
        }
        int previous = Session.WaveIndex, previousPair = Session.ActivePair;
        Session.Tick(delta);
        if (Session.State == RoundState.Results) { Finish(); return; }
        if (previousPair >= 0 && Session.ActivePair < 0 && previous == Session.WaveIndex && Session.LastMistake == "PAIR WINDOW MISSED")
        {
            hud.MessagePop(Vector2.zero, "PAIR RESET", UIArt.Warning, feedback.ReducedMotion);
            hud.SetHint("PAIR WINDOW MISSED", "Pair reset. Tap 1, then 2 within 2.5 seconds.");
            feedback.PlaySequence(false); CampaignTelemetry.Add("error_reason", level.LevelId, Session.WaveIndex, 1, "PAIR WINDOW MISSED");
        }
        if (previous != Session.WaveIndex)
        {
            CampaignTelemetry.Add("wave_pass", level.LevelId, previous, Session.Pressure);
            CampaignTelemetry.Add("wave_start", level.LevelId, Session.WaveIndex, Session.Pressure);
            hud.Callout("WAVE " + (Session.WaveIndex + 1), UIArt.Cyan);
            hud.SetHint(level.Waves[Session.WaveIndex].rules.title, level.MechanicHint);
        }
        if (previous != Session.WaveIndex) pool.Clear();
        pool.Tick(delta, feedback.ReducedMotion);
        director.Tick(Session, delta, feedback.ReducedMotion);
        if (level.DesignVersion >= 5) TickV5Presentation();
        for (int i = 0; i < pool.Capacity; i++)
        {
            var t = pool.At(i);
            if (t.Active && t.Kind == TargetKind.LinkedPair) t.ShowPairState(Session.ActivePair == t.SequenceId, Mathf.Min(Session.PairRemaining, Session.Wave.duration - Session.WaveElapsed));
        }
        if (shownWave != Session.WaveIndex || Mathf.CeilToInt(Session.RemainingTime) != displayedSecond)
        { shownWave = Session.WaveIndex; RefreshHUD(); }
        if (Session.State == RoundState.Results) Finish();
    }
    private void OnCampaignTarget(TargetView target)
    {
        CampaignTelemetry.InputResult(target.Kind == TargetKind.Bomb ? (Session.ShieldActive ? "SHIELD_BLOCK" : "BOMB_HIT") : "DIRECT_HIT");
        if (target.Kind == TargetKind.Boss)
        {
            bool exposed = Session.CoreExposed;
            int points = Session.Tap(TargetKind.Boss);
            if (!exposed) { CampaignTelemetry.InputResult("CORE_SHIELDED"); hud.MessagePop(target.Position, "SHIELDED", UIArt.Cyan, feedback.ReducedMotion); feedback.PlaySequence(false); return; }
            hud.Pop(target.Position, points, UIArt.Gold, feedback.ReducedMotion, 0, 1.2f, 0, target, 1.3f);
            hud.WorldEffects.Burst(target.Position, UIArt.Gold, target.Radius, TargetKind.Boss);
            hud.WorldEffects.CoreCrack(target.Position); hud.Shake(5, .14f); feedback.PlayTarget(TargetKind.Boss, Session.Combo);
            hud.SetHint("CORE HIT | PHASE CLEARED", "The next phase starts when this phase timer ends.");
            pool.Release(target); RefreshHUD(); return;
        }
        int priorCombo = Session.Combo; bool chainEvent = false;
        hud.HideTapHand();
        CampaignTelemetry.Add("target_tap", level.LevelId, Session.WaveIndex, (int)target.Kind);
        if (target.Kind == TargetKind.Bomb)
        {
            bool blocked = Session.ShieldActive; Vector2 p = target.Position;
            int penalty = Session.Tap(TargetKind.Bomb);
            hud.HazardPop(p, blocked ? "BLOCKED!" : "BOOM! " + penalty, blocked ? UIArt.Cyan : target.Tint, feedback.ReducedMotion, target, blocked);
            hud.WorldEffects.Hazard(p, blocked); hud.Shake(blocked ? 6 : 16, .3f, blocked ? 0 : World2Effects.BombFuseSeconds);
            feedback.PlayHazard(blocked); if (blocked) hud.ShieldJuice(); pool.Release(target);
            if (Session.State == RoundState.Results) resultDelay = .45f;
            hud.SetHint(blocked ? "SHIELD BLOCKED THE BOMB" : "BOMB HIT", blocked ? "The gate is safe. Continue the wave." : "Pressure " + Session.Pressure + "/" + Session.PressureLimit);
            CampaignTelemetry.Add(blocked ? "shield_block" : "error_reason", level.LevelId, Session.WaveIndex, Session.Pressure);
        }
        else if (target.Kind == TargetKind.LinkedPair)
        {
            int score = Session.Score;
            int result = Session.PairTap(target.SequenceId, target.Order);
            if (result == 0)
            {
                CampaignTelemetry.InputResult("WRONG_PAIR_ORDER");
                CampaignTelemetry.Add("error_reason", level.LevelId, Session.WaveIndex, 1, "WRONG ORDER");
                feedback.PlaySequence(false); hud.MessagePop(target.Position, "WRONG ORDER", UIArt.Warning, feedback.ReducedMotion);
                hud.SetHint("WRONG ORDER", "Pair reset. Tap 1, then 2 within 2.5 seconds.");
            }
            else
            {
                Vector2 firstPosition = target.Position, secondPosition = target.Position;
                for (int i = 0; i < pool.Capacity; i++)
                { var node = pool.At(i); if (node.Active && node.Kind == TargetKind.LinkedPair && node.SequenceId == target.SequenceId) { if (result == 1) node.HoldPairWindow(2.5f); if (node.Order == 1) firstPosition = node.Position; else secondPosition = node.Position; } }
                hud.WorldEffects.PairLink(firstPosition, secondPosition, result == 2);
                feedback.PlayTap(Session.Combo); feedback.PlaySequence(true); hud.WorldEffects.Burst(target.Position, UIArt.Gold, target.Radius, TargetKind.Time);
                hud.MessagePop(target.Position, result == 1 ? "NOW TAP 2" : "PAIR COMPLETE!", UIArt.Gold, feedback.ReducedMotion);
                if (result == 2)
                {
                    int id = target.SequenceId;
                    for (int i = 0; i < pool.Capacity; i++)
                    {
                        var member = pool.At(i); if (!member.Active || member.Kind != TargetKind.LinkedPair || member.SequenceId != id) continue;
                        hud.WorldEffects.Burst(member.Position, member.Tint, member.Radius, member.Kind); pool.Release(member);
                    }
                }
                hud.SetHint(result == 1 ? "PAIR 1 OF 2" : "PAIR COMPLETE", result == 1 ? "Tap the glowing 2 before the ring empties." : "Keep clearing this wave's gate.");
            }
        }
        else
        {
            int chain = pool.FindChain(target, chainTargets);
            if (chain >= 3)
            {
                chainEvent = true;
                hud.WorldEffects.Chain(chainTargets, chain);
                for (int i = 0; i < chain; i++)
                {
                    var member = chainTargets[i];
                    int points = Session.Tap(member.Kind, true, i == 0 ? HitSource.Direct : HitSource.Chain, member.Moving);
                    hud.Pop(member.Position, points, member.Tint, feedback.ReducedMotion, i * TargetView.ChainStepSeconds, chain >= 8 ? 1.5f : 1.15f, 0, member);
                    member.BeginBlast(i * TargetView.ChainStepSeconds, feedback.ReducedMotion, i); chainTargets[i] = null;
                }
                Session.RecordChain(chain); feedback.PlayTap(Session.Combo); feedback.PlayChain(chain);
                float finish = (chain - 1) * TargetView.ChainStepSeconds;
                hud.Callout(chain >= 12 ? "ULTRA CHAIN!" : chain >= 8 ? "MEGA CHAIN!" : "CHAIN!", chain >= 8 ? UIArt.Gold : UIArt.Cyan, finish + .1f);
                if (chain >= 5) hud.Shake(chain >= 8 ? 10 : 5, .3f, finish);
                CampaignTelemetry.Add("chain_created", level.LevelId, Session.WaveIndex, chain);
            }
            else
            {
                int before = Session.Score;
                if (target.Kind == TargetKind.Priority && !Session.PriorityTap(target.Order))
                { CampaignTelemetry.InputResult("WRONG_PRIORITY_ORDER"); hud.MessagePop(target.Position, "TAP ! IN ORDER", UIArt.Warning, feedback.ReducedMotion); feedback.PlaySequence(false); RefreshHUD(); return; }
                int points = target.Kind == TargetKind.Priority ? Session.Score - before : Session.Tap(target.Kind, true, HitSource.Direct, target.Moving);
                hud.Pop(target.Position, points, target.Tint, feedback.ReducedMotion, 0, 1, 0, target, target.Kind == TargetKind.Golden ? 1.3f : 1);
                hud.WorldEffects.Burst(target.Position, target.Tint, target.Radius, target.Kind);
                if (target.Kind == TargetKind.Time) hud.TimeJuice();
                feedback.PlayTarget(target.Kind, Session.Combo); pool.Release(target);
                if (target.Kind == TargetKind.Multiplier) hud.SetHint("x2 SCORE FOR 4 SECONDS", "Optional reward. Skill gates still need direct taps.");
            }
        }
        if (Session.Combo > priorCombo)
        {
            hud.WorldEffects.Hit(target.Position, target.Tint, priorCombo, Session.Combo);
            if (!chainEvent)
                for (int i = 0; i < milestoneCombos.Length; i++)
                    if (priorCombo < milestoneCombos[i] && Session.Combo >= milestoneCombos[i]) hud.Callout(milestoneWords[i], milestoneColors[i]);
        }
        hud.HitJuice(Session.Combo); RefreshHUD();
        if (Session.State == RoundState.Results) Finish();
    }
    private void OnCampaignExpired(TargetView target)
    {
        if (target.Kind == TargetKind.Bomb) { hud.WorldEffects.Fizzle(target.Position); return; }
        if (target.Kind == TargetKind.LinkedPair)
        {
            int id = target.SequenceId;
            for (int i = 0; i < pool.Capacity; i++)
            { var t = pool.At(i); if (t.Active && t.Kind == TargetKind.LinkedPair && t.SequenceId == id) pool.Release(t); }
            Session.ResetPair();
            hud.SetHint("PAIR EXPIRED", "Wait for the next pair, then tap 1 followed by 2.");
        }
        Session.Miss(target.Critical, target.Kind == TargetKind.Priority, target.Required);
        if (Session.State == RoundState.Results) resultDelay = .35f;
        hud.MissJuice(target.Position);
        if (target.Critical) { feedback.PlayPressure(); hud.SetHint("PRIORITY MISSED", "Pressure " + Session.Pressure + "/" + Session.PressureLimit); }
        CampaignTelemetry.Add("target_miss", level.LevelId, Session.WaveIndex, (int)target.Kind, target.Critical ? "PRIORITY MISSED" : "TARGET MISSED");
        RefreshHUD();
    }
    public void RecordEmptyTap()
    {
        if (!level.Scripted || Session.State != RoundState.Playing) return;
        Session.EmptyTap(); CampaignTelemetry.Add("empty_tap", level.LevelId, Session.WaveIndex, 0); RefreshHUD();
    }
    public void ActivateUltimate()
    {
        if (!level.AllowUltimate || !Session.UseUltimate()) return;
        ultimateSlowRemaining = feedback.ReducedMotion ? 0 : .18f;
        hud.WorldEffects.Finale(Vector2.zero, UIArt.Gold); hud.Callout("ULTIMATE!", UIArt.Gold); hud.Shake(12, .35f);
        for (int i = 0; i < pool.Capacity; i++)
        {
            var target = pool.At(i);
            // Direct, ordered and required chain gates remain visible and never receive blast credit.
            if (!level.CanUltimateClear(target.Kind) || !target.Active || target.Required || target.BlastPending || target.Kind == TargetKind.Bomb || target.Kind == TargetKind.Priority || target.Kind == TargetKind.LinkedPair) continue;
            int points = Session.Tap(target.Kind, true, HitSource.Ultimate);
            hud.Pop(target.Position, points, target.Tint, feedback.ReducedMotion, 0, 1.4f, 0, target); pool.Release(target);
        }
        feedback.PlayChain(12); CampaignTelemetry.Add("ultimate_used", level.LevelId, Session.WaveIndex, 0); RefreshHUD();
    }
    public void StartPractice() { practiceRequested = true; BeginCountdown(); }
}
