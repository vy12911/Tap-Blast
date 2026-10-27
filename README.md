# Tap Blast — Campaign (Levels 1–20)

Unity **6000.3.17f1**, URP, uGUI and Input System. Use a **9:16** Game view (1080 × 1920 reference frame).

The Canvas uses Scale With Screen Size / Expand and an authored 1080 × 1920 portrait frame. `ResponsiveCanvas` fits it inside phone and tablet safe areas while the background fills the screen. Edit UI manually in `Assets/Prefabs/UI/GameCanvas.prefab`; retain TMP text and serialized references. See [Canvas editing](Documentation/Canvas-Editing.md) for layout and device-preview guidance.

All UI text uses **TextMeshProUGUI** components and **`TMPro.TMP_Text`** script bindings. Always use TMP for new labels. Prefabs retain their saved scene references and use the shared Liberation Sans SDF font in `Assets/TextMesh Pro/`; no text UI is rebuilt at runtime.

## Play and campaign flow

Open **Tap Blast > Open Campaign**, enter Play Mode and select **Let's Play**. Completing a level unlocks the next one up to Level 20.

Flow: briefing and reward preview → three-second countdown → gameplay → score/star/reward results → replay, next level or level selection. First launch opens Level 1; later launches restore the selected unlocked level.

Level 1 starts with a non-expiring target and tap-hand tutorial. Its clock starts with the first tap and the round finishes on target 20. Levels 2–20 start the clock after countdown and continue until timeout, allowing mastery after the primary goal is met. Their briefings explain this. Expired targets reset the current streak; a previously reached best-streak objective remains earned. Empty-space taps are ignored.

## World 1 & World 2 objectives

These use the **suggested early-level objectives in the supplied GDD v1.0, pages 12–13**. Durations, exact sizes, pacing, star scores and rewards are implementation tuning values.

### World 1: Learn to Tap (Levels 1–10)
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

### World 2: Combo City (Levels 11–20)

| Level | Primary objective | Base time | Two-star score | Three-star streak |
|---|---|---|---|---|
| 11 — Chain Spark | Create 2 chains | 45s | 900 | 12 |
| 12 — Double Impact | Create 3 chains; five-target groups | 45s | 1,500 | 20 |
| 13 — Mega Burst | Create 1 Mega Chain | 45s | 2,500 | 24 |
| 14 — Drift Cluster | Score 3,000; moving clusters | 45s | 3,800 | 25 |
| 15 — Combo City | Reach a 20-hit streak | 45s | 2,200 | 30 |
| 16 — Danger Signs | Avoid all Bombs and tap 20 safe targets | 45s | 650 | 15 |
| 17 — Shield School | Block 1 Bomb using Shield | 45s | 700 | 15 |
| 18 — Safe Chain | Create 3 chains without an unprotected Bomb hit | 45s | 1,800 | 25 |
| 19 — Mega Mastery | Create 2 Mega Chains | 45s | 3,200 | 32 |
| 20 — Chain Master | Reach 30 best combo AND create 5 chains | 55s | 4,500 | 40 |

One star requires the complete primary objective at timeout. Two also require the configured score. Three additionally require zero expired valid targets, zero unprotected Bomb hits, and the listed best streak.

See [the World 1 guide](Assets/Design%20Doc/World1_Level_Objectives_and_Progression.md) and [the World 2 guide](Assets/Design%20Doc/World2_Level_Objectives_and_Progression.md) for rewards and tuning decisions.

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

## World 2 chains, Bombs and Shield

From level 11, ordinary colored targets appear gradually across the board. Tap one when at least three share its color to detonate every matching target, regardless of distance. Other colors and Bombs remain. Fewer than three matches give a single tap. The reaction proceeds one target at a time with colored bursts and explosion sounds. Five matches make Chain 2; eight make Mega. This user-requested rule replaces the earlier spatial LINK/area-blast interpretation. See the World 2 guide for spawning and tuning.

Red X/BOMB targets spawn at random safe positions and move in random directions. They bounce at arena edges and reverse before overlapping valid targets. Ignoring them is safe; tapping one breaks combo and subtracts up to 50 points. Chain blasts exclude Bombs. From level 17, the authored Shield button grants one protection per round; activation alone does not satisfy the Shield lesson until a Bomb is blocked. No-Bomb objectives remain at risk until time expires.

Level select shows all 20 levels in one scrolling list with no world pages, and both Editor and Android use the same 20 serialized configs. Next opens level 11 after level 10 like any other level; level 20 reports campaign completion without offering an unavailable level 21. The World 1/World 2 headings below are design groupings only.

## Progress, rewards and controls

Coins, XP, best scores and stars persist locally per level. Every 100 XP advances the player profile. Successful replays award the configured rewards again; failure and abandonment grant none. Starting a round saves a unique level-bound token; completion consumes it with the saved rewards to prevent duplicate payout. Existing progress is retained when updating this campaign, including historical World 2 records from earlier placeholder objectives. New attempts use the GDD objectives. Best combo is now saved per level alongside scores and stars.

Pause freezes targets, spawning, effects and time. Escape toggles pause; losing focus or backgrounding pauses the game. Restart/replay clear score, collections, boss state, targets, tutorial and feedback. Pause settings include sound, reduced motion and vibration.

## Architecture and tuning

`RoundSession.cs` owns scoring, collection counts, combo objectives, time bonuses and stars with no Unity dependencies. `GameManager.cs` coordinates views, round state and persistence. Level mechanics, timing, values, objective counts, star thresholds and rewards are serialized in `Assets/Data/Levels/`. Pool capacity is preallocated from the largest configured level (12 targets); no per-target Update, physics, coroutines or LINQ runs in gameplay loops.

Edit the authored Canvas and target prefab for presentation; see [Canvas editing](Documentation/Canvas-Editing.md). The detailed GDD campaign is implemented through level 20, including Chain at 11, Bombs at 16 and Shield at 17. Later power-ups, Perfect, Ultimate and other modes/meta systems remain outside this first-20-level scope. See the World 2 guide for tuning decisions and the distinction from the GDD's broader MVP recommendations.

## Validation

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Persistence
```

The CLI validates all 20 serialized scene references, compiles runtime/editor sources and runs pure C# rule checks. The Unity harness uses a disposable project and separate save identity. It simulates all 20 authored levels and verifies reachable three-star scores, navigation, board-wide color matching, Bomb spacing, Shield, pause/replay, UI bindings/text fit, records, rewards and duplicate protection. The persistence mode checks records in a fresh Unity process after a successful smoke run. Runners start Unity asynchronously: inspect the final report and process exit before starting another run.

CLI report: `Logs/Validation/round-rules.txt`. Unity reports and phone/tall-phone/tablet captures: `Temp/Campaign20Validation/`. Unity logs: `Logs/Validation/campaign20-*.log`. Android/iOS builds, hardware touch, vibration, performance and human difficulty still require device testing.
