# Tap Blast — World 1 (Levels 1–10)

Unity **6000.3.17f1**, URP, uGUI and Input System. Use a **9:16** Game view (1080 × 1920 reference frame).

The Canvas uses Scale With Screen Size / Expand and an authored 1080 × 1920 portrait frame. `ResponsiveCanvas` fits it inside phone and tablet safe areas while the background fills the screen. Edit UI manually in `Assets/Prefabs/UI/GameCanvas.prefab`; retain TMP text and serialized references. See [Canvas editing](Documentation/Canvas-Editing.md) for layout and device-preview guidance.

All UI text uses **TextMeshProUGUI** components and **`TMPro.TMP_Text`** script bindings. Always use TMP for new labels. Prefabs retain their saved scene references and use the shared Liberation Sans SDF font in `Assets/TextMesh Pro/`; no text UI is rebuilt at runtime.

## Play and campaign flow

Open **Tap Blast > Open Campaign**, enter Play Mode and select **Let's Play**. The shared `Assets/Scenes/Level01.unity` scene references all ten level configurations directly, including in player builds. Completing a level unlocks the next one. Level selection contains ten authored buttons in `Assets/Prefabs/UI/GameCanvas.prefab`; scripts bind and update them without constructing UI.

Flow: briefing and reward preview → three-second countdown → gameplay → score/star/reward results → replay, next level or level selection. First launch opens Level 1; later launches restore the selected unlocked level. Level 10 has replay and level selection, without a nonexistent Level 11 button.

Level 1 starts with a non-expiring target and tap-hand tutorial. Its clock starts with the first tap and the round finishes on target 20. Levels 2–10 start the clock after countdown and continue until timeout, allowing mastery after the primary goal is met. Their briefings explain this. Expired targets reset the current streak; a previously reached best-streak objective remains earned. Empty-space taps are ignored.

## World 1 objectives

These use the **suggested early-level objectives in the supplied GDD v1.0, page 12**. The GDD's “5x / 10x / 15x” combo objectives mean consecutive hits, not the separate score multiplier. Durations, exact sizes, pacing, star scores and rewards are implementation tuning values.

| Level | Primary objective | Base time | Two-star score | Three-star streak |
|---|---|---|---|---|
| 1 — First Spark | Tap 20 targets | 60s | 240 | 20 |
| 2 — Size It Up | Score 500; cycling sizes | 45s | 750 | 20 |
| 3 — Streak Starter | Reach a 5-hit streak | 45s | 600 | 15 |
| 4 — Precision Push | Score 1,000; small targets | 45s | 1,250 | 15 |
| 5 — On the Move | Reach a 10-hit streak; moving targets | 45s | 800 | 15 |
| 6 — Golden Opportunity | Tap 3 Golden targets | 45s | 1,800 | 15 |
| 7 — Fast & Furious | Score 2,000; accelerating spawns | 40s | 2,500 | 20 |
| 8 — Tick Tock | Collect 3 Time Bubbles | 35s | 1,400 | 20 |
| 9 — Master Streak | Reach a 15-hit streak | 45s | 1,600 | 25 |
| 10 — Golden Core | Destroy the 5-hit Core | 50s | 3,000 | 25 |

One star requires the primary objective. Two stars also require the configured score. Three stars additionally require zero expired targets and the listed best streak. Score alone cannot replace collection, combo or Core objectives.

See [the World 1 guide](Assets/Design%20Doc/World1_Level_Objectives_and_Progression.md) for rewards and tuning decisions.

## Targets and scoring

- Normal targets award 10 base points; Level 4's small targets award 15.
- Golden targets award **100 base points**, using the low end of the GDD's 100–1,000 range. They update actual score and collection progress, not only feedback text. `goldenPoints` is tunable per level.
- Time Bubbles award normal tap points and add **3 seconds** through the pure round rules. A maximum of six Time Bubbles can spawn per round, limiting added time to 18 seconds with the default tuning.
- Golden and Time lessons start with the relevant target and guarantee another opportunity every fourth spawn, subject to the Time Bubble budget. Additional specials follow the configured probabilities.
- Moving targets bounce inside the arena on every pooled tick, including after resizing. Movement continues in reduced-motion mode because it is a gameplay mechanic.
- The Core spawns at round start, is larger than normal targets, never expires, and displays remaining health in its ring. Every hit scores and builds combo; only the fifth hit records destruction. Other targets continue spawning for score mastery.
- Level 7 interpolates from 0.75s to 0.5s spawn intervals during its first 20 seconds.
- Special targets have authored GOLD, +3s and CORE labels so color is not their only identifier.

Multipliers apply after the current hit: 1–4 = 1x, 5–9 = 1.25x, 10–19 = 1.5x, 20–29 = 2x, 30–49 = 3x, 50+ = 4x. Fractional scores round to the nearest integer, halves up. A clean Level 1 round scores 275.

## Progress, rewards and controls

Coins, XP, best scores and stars persist locally per level. Every 100 XP advances the player profile. Successful replays award the configured rewards again; failure and abandonment grant none. Starting a round saves a unique level-bound token; completion consumes it with the saved rewards to prevent duplicate payout. Existing progress is retained when updating this campaign.

Pause freezes targets, spawning, effects and time. Escape toggles pause; losing focus or backgrounding pauses the game. Restart/replay clear score, collections, boss state, targets, tutorial and feedback. Pause settings include sound, reduced motion and vibration.

## Architecture and tuning

`RoundSession.cs` owns scoring, collection counts, combo objectives, time bonuses and stars with no Unity dependencies. `GameManager.cs` coordinates views, round state and persistence. Level mechanics, timing, values, objective counts, star thresholds and rewards are serialized in `Assets/Data/Levels/`. Pool capacity is preallocated from the largest configured level (six targets); no per-target Update, physics, coroutines or LINQ runs in gameplay loops.

Edit the authored Canvas and target prefab for presentation; see [Canvas editing](Documentation/Canvas-Editing.md). The GDD's detailed campaign places Chain and Bomb introductions at Levels 11 and 16. They, power-ups, Perfect and Ultimate remain outside this World 1 implementation, despite the broader MVP wish list elsewhere in the GDD.

## Validation

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnitySmoke.ps1 -Method RunWorld1
```

The CLI compiles runtime/editor sources and runs pure C# checks. The World 1 Unity harness uses a disposable project and a separate save identity. It checks serialized campaign references, authored selection UI, objective routing, special targets, movement, acceleration, attainable mastery, rewards and duplicate protection. It advances the actual pooled gameplay logic in deterministic time steps while tapping available targets; it is not a human difficulty or touch-feel test.

CLI reports: `Logs/Validation/round-rules.txt`. Unity report/captures: `Temp/Level1Validation/world1-*`. Unity log: `Logs/Validation/unity-world1.log`. Android/iOS builds, hardware touch, vibration, performance and difficulty still require device testing.
