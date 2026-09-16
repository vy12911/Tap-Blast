# Tap Blast — Levels 1 and 2

Unity **6000.3.17f1**, using the existing URP project, uGUI and Input System packages.

## Play

1. Let Unity finish importing and compiling the new assets.
2. Choose **Tap Blast > Open Campaign**, then press **Play**.
3. Select **Let's Play**. Complete Level 1, then select **Next Level** for Level 2.

`Assets/Scenes/Level01.unity` is the shared campaign entry scene and the only enabled build scene. It references both level configurations, reusing the UI and pool across levels. The editor assigns it as the Play Mode start scene when none is assigned. **Open Level 1** remains an alias for the shared scene.

First launch opens Level 1's briefing. Later launches restore the last selected unlocked level. The briefing's back arrow, Pause and Results open level selection. Exiting a paused round abandons the attempt without rewards. Level 2 unlocks when Level 1 earns at least one star; existing completed Level 1 saves automatically unlock it. Level 2 offers replay and level selection, without an unavailable Level 3 button.

The scene contains the camera, 2D light, configured GameManager and an editable **Tap Blast UI** Canvas prefab instance. HUD, menus, input, tutorial hand and pooled effects are saved in the scene/prefab hierarchy and are visible outside Play Mode. Scripts bind references, update game values and animate these elements; they do not construct the UI. The four-target pool is prewarmed from the saved NormalTarget prefab at startup.

Use a **9:16** Game view for a phone preview. The design uses a 720 × 1280 reference frame, uniformly fitted into the screen's safe area. Wider/taller displays retain comfortable margins; mobile orientation is portrait.

## Level 1 rules

- Three-second countdown, then one non-expiring tutorial target.
- An animated tap hand points to the first target without intercepting input. It hides after the first tap and returns on restart/replay; reduced-motion mode uses a static hand.
- The 60-second clock and regular spawning begin with the first successful tap.
- Tap 20 normal targets to complete immediately.
- Normal targets live for 4.5 seconds. An expired target resets the streak, but does not directly end Level 1.
- Empty-space taps are ignored in this introductory level.
- Normal colors are decorative and share identical scoring and behavior.
- Score multipliers apply to the streak after the current hit; fractional scores round to the nearest integer, with halves rounded up.
- A clean 20-target round scores 275 points.

| Stars | Requirement |
|---|---|
| 1 | Complete the level |
| 2 | Complete and score at least 240 |
| 3 | Meet the two-star goal, miss no targets, and reach a 20-hit streak |

Pause freezes target lifetimes, spawning, feedback and the clock. Escape toggles pause on desktop. Backgrounding the app pauses it; return through Resume. Restart and Replay clear targets, effects, score, timer and tutorial progress. Results persist the highest score and highest star count independently.

## Level 2 rules

The GDD introduces **target sizes** with a **500-point objective**. The authored level is **Size It Up**:

- Three-second countdown, then the 45-second clock starts immediately.
- Normal targets cycle through 144, 100 and 112 reference-pixel diameters. All give 10 base points. The separate Small target type remains reserved for a later level.
- Spawns every 0.75 seconds, 4.5-second lifetimes, maximum four active targets. Spacing accounts for the largest target and its pulse.
- Reach 500 before timeout. Play continues after reaching the goal to allow score mastery; results appear at timeout.
- 1 star: 500 points. 2 stars: 750. 3 stars: 750, no expired targets, and a best streak of at least 20.
- Below 500 at timeout fails. Replay retains Level 2's rules and resets the attempt.

The GDD specifies the size mechanic and 500-point primary goal. Duration, diameters, pacing and mastery thresholds are tunable implementation defaults. Scores and stars are separate for each level; wallet, XP and player level are shared.

## Rewards and result presentation

Successful Level 1 rounds award **50 coins and 25 XP**; Level 2 awards **75 coins and 35 XP**, including successful replays. Failure and abandoned rounds grant neither. Rewards are tunable defaults, not fixed GDD values. Every 100 total XP advances the player level without affecting scoring. Coins are saved for future collection/shop features; this slice has no spending screen.

The briefing previews the completion reward and existing wallet. Results count the score up, reveal the personal-best message and stars in sequence, then fade in earned coins/XP, wallet totals, player level and XP progress. Reduced-motion mode shows the final state immediately. Replay stays usable during the animation.

Rewards are committed before any result animation. Starting a round saves a unique active token; completion consumes it together with balances, score and stars in one JSON PlayerPrefs record. Repeated completion, stale tokens after restart and incomplete sessions cannot pay again. Interrupting the reveal cannot duplicate or lose an already-saved award. This is local persistence, not server-authoritative or tamper-proof currency. Previous Level 1 best scores/stars migrate automatically without retroactively granting coins or XP.

## Tuning

For visual changes, edit `Assets/Prefabs/UI/GameCanvas.prefab` or its scene instance, and `Assets/Prefabs/Targets/NormalTarget.prefab`. **Tap Blast > Select Game Canvas** selects the shared Canvas. See [Canvas editing guide](Documentation/Canvas-Editing.md) for the hierarchy, feedback controls and GDD mechanic boundaries.

The gameplay presentation now includes a layered neon city backdrop, star-emblem normal targets, pooled tap sparks/shockwaves, score/combo punch, GDD Fever threshold labels and sounds, MISS feedback and completion confetti. Reduced motion suppresses decorative movement; pause and replay preserve/reset effects with the round.

Select **Tap Blast > Select Level 1 Settings** / **Select Level 2 Settings**, or the corresponding asset in `Assets/Data/Levels/`.

Duration, objective, base score, spawn interval, target lifetime, capacity, size, spacing and star goals are serialized in this ScriptableObject. Adjust them outside Play Mode. Changes to capacity take effect on the next scene start because the pool is prewarmed once.

These two levels leave out Bombs, Golden targets, moving targets, power-ups and Ultimate, consistent with the early campaign. Level IDs and progression routing currently support Levels 1 and 2. Later levels require extending the supported progression records and adding configs to the scene's level list.

## Structure

| Location | Responsibility |
|---|---|
| `Assets/Scripts/GameManager.cs` | Round lifecycle and coordination |
| `Assets/Scripts/Core/RoundSession.cs` | Pure C# scoring, combo, timer, completion and stars |
| `Assets/Scripts/Data/` | Level configuration and versioned local progress/settings |
| `Assets/Scripts/Targets/` | Bounded spawning, circular pointer input and fixed target pool |
| `Assets/Scripts/UI/` | Canvas bindings, safe-area fitting, result and pooled-feedback animation |
| `Assets/Scripts/Feedback/` | Synthesized tap/result sounds, motion preference, completion vibration |
| `Assets/Editor/` | Editor-only scene and configuration shortcuts |
| `Assets/Data/Levels/` | Authored level assets |
| `Tests/` | Executable rule checks |
| `Tools/` | Local compilation checks and isolated Unity smoke harness |

Runtime and editor scripts use separate assembly definitions. Editor utilities are excluded from player builds.

## Performance choices

- Four preallocated targets and ten preallocated feedback objects, shared across both levels without scene reloads. Pool capacity is chosen once from the largest configured capacity; there is no runtime pool growth.
- One coordinator ticks all target lifetimes and animations. No per-target Update methods, physics simulation, coroutines or LINQ in gameplay loops.
- Spawn positions use bounded attempts and minimum spacing; a saturated arena skips a spawn rather than forcing overlap.
- Static HUD, targets and feedback use separate canvas boundaries to contain frequent UI geometry rebuilds.
- Gameplay text is refreshed for events and timer-second changes. The brief result count-up updates text only when the displayed integer changes. Small strings can allocate on these events; this is not a zero-allocation claim.
- Five small shared sprite assets are stored in Assets/Art/UI and referenced by Canvas Images. Four short mono audio clips are generated once and released on teardown. No runtime texture generation or post-processing bloom is required.
- Decorative graphics do not receive raycasts. Targets use circular hit testing.
- No global time-scale changes. Pausing is explicit round state.
- Local saves occur on round start, results and settings changes, not during every tap or frame.
- A 60 FPS cap is configured; actual device performance must still be measured on hardware.

Sound, reduced motion and vibration can be toggled from Pause. Vibration uses the device's basic vibration on successful completion and major combo milestones; it is inactive in the editor. Audio is synthesized placeholder feedback, with no music track or commercial assets.

## Validation

From the project root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
```

The script uses Unity's installed compiler and .NET runtime, compiles runtime and editor sources against locally installed Unity/package assemblies, then runs 24 pure C# round-rule checks covering both levels. It writes results to `Logs/Validation/round-rules.txt`. This is a compile/rules check, not a substitute for Unity play testing or mobile profiling.

`Tools/UnitySmoke.cs` is an editor harness intended for a disposable project copy. It plays through UI raycasts, tutorial, pause, expiry, restart, win, saving, replay and timeout, and requests screenshots. It changes the disposable project's product name to isolate test saves. Do not place it in the production Assets folder.

`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnitySmoke.ps1` prepares and launches that copy using the existing local package cache. The process runs asynchronously; results appear in `Temp/Level1Validation/smoke-results.txt`, and the editor log is `Logs/Validation/unity-rewards.log`. The test saves are isolated under a separate product name.

For the two-level campaign use `Tools/Run-UnitySmoke.ps1 -Method RunCampaign`. This verifies unlocks, level selection, the full timed Level 2 round, size variants, pause/replay/exit, separate records and rewards. Its report is `Temp/Level1Validation/campaign-results.txt`; screenshots are captured alongside it. After it exits, use `-Method VerifyCampaignPersistence` to check the saved campaign and Level 2 startup in a new Unity process.

After the Canvas migration: runtime/editor compilation, **24 rule checks**, **564 Canvas/campaign checks**, **68 tutorial/replay smoke checks**, and **2 campaign restart checks** passed. The Canvas checks include persisted sprite/reference validation, retaining a designer text edit across Play Mode, no runtime UI reconstruction, pooled burst movement, paused effects and reduced-motion suppression. The campaign test plays through Level 1 and a full timed Level 2 round, including locking/unlocking, size variants, bounds, pool reuse, scoring, stars, pause, replay, abandonment, separate saves and level-bound reward protection. Restart verification checks both saved progress and the actual restored Level 2 briefing. The new level-selection, briefing, gameplay and result screens were visually reviewed. Captures are in `Documentation/Previews/`; reports are in `Logs/Validation/`.

For the persistence pass, run `Tools/Run-UnitySmoke.ps1 -Method VerifyPersistence` after a successful full smoke run; it expects the deterministic test balances from that run. Unity's optional editor search index threw an unrelated exception during isolated batch startup; the harness recorded it separately.

This change has not been validated in an Android/iOS build or on a physical device. Device vibration, touch feel, background/resume behavior on hardware and on-device profiling remain unverified. These require a device test pass; editor checks do not establish mobile performance.
