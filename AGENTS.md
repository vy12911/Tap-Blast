# AGENTS.md — Agent Guide for Tap Blast

> **MANDATORY FOR ALL AI CODING AGENTS**: Read this document before making any code modifications, inspecting issues, or adding features to this codebase.

---

## 1. Game Overview

**Tap Blast** is a fast-paced 2D mobile arcade tap game built with **Unity 6000.3.17f1** using Universal Render Pipeline (URP), uGUI, and the Unity Input System. Designed for mobile portrait orientation (**9:16 aspect ratio**, 1080 × 1920 reference resolution).

### Core Mechanics & Gameplay Loop
- **Target Spawning & Tapping**: Glowing circular targets spawn bounded inside an arena. Players tap targets before their lifetime timers expire (default lifetime: 4.5 seconds).
- **Combo & Multiplier System**: Successive hits build a combo streak, applying score multipliers:
  - 5–9 hits: **1.25x** (Great Streak)
  - 10–19 hits: **1.5x** (Combo Fever)
  - 20–29 hits: **2.0x** (Super Fever)
  - 30–49 hits: **3.0x** (Ultra Fever)
  - 50+ hits: **4.0x** (Master Combo)
  - **Miss / Expiry**: Resets the combo streak to 0.
- **Level Campaign & Objectives**:
  - **World 1 (Levels 1–10 Fully Implemented)**:
    - **Level 1 ("FIRST SPARK")**: Tap 20 targets within 60s (First-tap tutorial hand).
    - **Level 2 ("SIZE IT UP")**: Score 500 points within 45s (Cycling target diameters).
    - **Level 3 ("STREAK STARTER")**: Hit 5 targets in a row; timed mastery play.
    - **Level 4 ("PRECISION PUSH")**: Score 1,000 points (Small target sizes 127.5–142.5px).
    - **Level 5 ("ON THE MOVE")**: Hit 10 targets in a row (drifting/moving targets).
    - **Level 6 ("GOLDEN OPPORTUNITY")**: Tap 3 Golden targets (100 base points each before combo multiplier).
    - **Level 7 ("FAST & FURIOUS")**: Score 2,000 points (0.5s spawn acceleration).
    - **Level 8 ("TICK TOCK")**: Collect 3 Time Bubbles (+3s each; at most six spawn per round).
    - **Level 9 ("MASTER STREAK")**: Hit 15 targets in a row (advanced combo challenge).
    - **Level 10 ("GOLDEN CORE")**: Destroy the 5-hit Golden Core; continue timed play for score mastery.
  - **World 2 (Levels 11–20 Implemented)**: 11: two chains; 12: three chains; 13: one Mega Chain; 14: moving clusters / 3,000 score; 15: 20-hit streak; 16: avoid Bombs / 20 safe targets; 17: block one Bomb with Shield; 18: three chains without Bomb damage; 19: two Mega Chains; 20: Chain Master / 30 best combo plus five chains. See the World 2 guide for tuning and rules.
- **Star Rating (1–3 Stars per Level)**:
  - 1 Star: Complete level / meet primary goal.
  - 2 Stars: Reach higher score threshold.
  - 3 Stars: Meet 2-star threshold + zero target misses + zero unprotected Bomb hits + streak goal.
- **Player Economy & Progression**:
  - Unlocked levels, earned coins, and XP persist locally.
  - Every 100 XP levels up the player profile.

---

## 2. Game Design Documents (GDD) Status

- **Full GDD File**: [`Assets/Design Doc/Tap_Blast_FULL_GDD_v1.0.pdf`](Assets/Design%20Doc/Tap_Blast_FULL_GDD_v1.0.pdf) — Production Design document outlining the 100-level campaign, target types (Normal, Golden, Time, Bomb, Perfect, Rainbow, Chain), power-ups, Ultimate meter, daily challenges, cosmetics, economy, and technical architecture.
- **World 1 Level & XP Guide**: [`Assets/Design Doc/World1_Level_Objectives_and_Progression.md`](Assets/Design%20Doc/World1_Level_Objectives_and_Progression.md) — Detailed table of level requirements, star thresholds, target mechanics, and player XP leveling formulas.
- **Authoritative Implementation References**:
  1. [`README.md`](README.md) — Active rules, scoring formulas, rewards, performance standards, and tuning instructions.
  2. [`Documentation/Canvas-Editing.md`](Documentation/Canvas-Editing.md) — Canvas object hierarchy, visual system, and UI prefab/scene boundaries.
  3. [`Documentation/Animations-Level01-20.md`](Documentation/Animations-Level01-20.md) — Every gameplay animation, when it plays, and how it is verified.
- **Future GDD Features (Reserved for Higher Levels)**:
  - Targets reserved for later worlds: Perfect, Rainbow and Decoy. Chain (L11) and Bombs (L16) are implemented.
  - Systems reserved for later worlds: other power-ups, Perfect bonuses and Ultimate meter. Shield (L17) is implemented.

---

## 3. Architecture & Codebase Map

| Directory / File | Responsibility |
|---|---|
| [`Assets/Scripts/GameManager.cs`](Assets/Scripts/GameManager.cs) | Round manager: UI events, pooling, spawner coordination, pause/focus, and **which feedback each gameplay event triggers**. Hard 60 fps cap in `Start()`. |
| [`Assets/Scripts/Core/RoundSession.cs`](Assets/Scripts/Core/RoundSession.cs) | Pure C# logic (zero Unity dependencies). Scoring, combo tracking, countdowns, star calculation. |
| [`Assets/Scripts/Data/LevelConfig.cs`](Assets/Scripts/Data/LevelConfig.cs) | `ScriptableObject` defining level parameters (duration, objectives, target sizes, star limits, rewards). |
| [`Assets/Scripts/Data/ProgressStore.cs`](Assets/Scripts/Data/ProgressStore.cs) | Versioned `PlayerPrefs` wrapper with an in-memory cache. Level unlocks, coins, XP, round tokens, settings. `LevelCount` is the campaign size. |
| [`Assets/Scripts/Targets/`](Assets/Scripts/Targets/) | `TargetPool` (fixed preallocated pool), `TargetSpawner` (positioning/spacing), `TargetView` (view, hit testing, spawn spring, chain charge). |
| [`Assets/Scripts/UI/`](Assets/Scripts/UI/) | `GameHUD.cs` (bindings, pooled popups, callouts, results), `GameHUD.Juice.cs` (HUD punches, arena shake), `ResultReveal.cs`, `TapHandHint.cs`, `GuiNavigation.cs` (wallet), `UIArt.cs` (colour tokens, `Compact` number format), `UIGradient.cs`. |
| [`Assets/Scripts/Feedback/World2Effects.cs`](Assets/Scripts/Feedback/World2Effects.cs) | Arena effects for **every level** in one procedural mesh: pop flash, candy shards, rings, chain reactions, bomb explosion, shield implosion, smoke, finale. Fixed 192-slot ring buffer. |
| [`Assets/Scripts/Feedback/FeedbackPlayer.cs`](Assets/Scripts/Feedback/FeedbackPlayer.cs) | Synthesised audio (tap, chain, explosion, shield) and the sound / reduced-motion preferences. The game has no vibration. |
| [`Assets/Data/Levels/`](Assets/Data/Levels/) | Authored level configs (`Level01.asset` through `Level20.asset`). |
| [`Tools/`](Tools/) | PowerShell validation scripts, Unity smoke harnesses, editor authoring utilities. |
| [`Tools/LiveEdits/`](Tools/LiveEdits/) | Numbered, re-runnable editor scripts that authored the current UI and art into the open editor (see its README). |

---

## 4. Key Rules & Coding Standards

1. **Performance & Allocation Constraints**:
   - **No per-target `Update()` calls**, `Coroutines`, physics simulation, or `LINQ` in gameplay execution loops.
   - Use fixed preallocated pools (`TargetPool`, the 16 pooled popups, the `World2Effects` slot buffer). Do not instantiate/destroy objects during gameplay.
2. **UI Architecture Rules**:
   - **Always use `TMPro.TMP_Text` for UI text fields, parameters, arrays, and component lookups.** Author Canvas labels as `TextMeshProUGUI`, assign a Poppins TMP font asset, and wire the saved references. Do not introduce `UnityEngine.UI.Text` or construct replacement text objects at runtime.
   - **Do NOT reconstruct UI elements via script at runtime.** The UI lives in `Assets/Scenes/Level01.unity` as the **unpacked** root **Tap Blast UI** (the owner's choice; keep it unpacked). `Assets/Prefabs/UI/GameCanvas.prefab` is a matching template. **Every UI change must be applied to both.** Runtime scripts only bind fields, update text/colours, and toggle visibility.
   - Editor-time authoring scripts (in `Tools/` and `Tools/LiveEdits/`) may create and arrange saved objects. Write them to be idempotent (find-or-create by name, explicit sibling order).
   - Canvas Scaler: Scale With Screen Size, 1080 × 1920, Expand. `ResponsiveCanvas` fills the screen; `respectSafeArea` is **off** by default. Pin elements to the edge they belong to so taller phones gain space in the arena and lists.
3. **Pure C# Logic Decoupling**:
   - Core round rules and scoring reside in [`RoundSession.cs`](Assets/Scripts/Core/RoundSession.cs). Keep it free of `UnityEngine` so the CLI tests can run it.
4. **Local Progress Security**:
   - Round tokens and session rewards are validated and saved in [`ProgressStore.cs`](Assets/Scripts/Data/ProgressStore.cs) on round start and completion to prevent duplication or incomplete session grants.
   - All writes go through `ProgressStore` (it refreshes its cache on save). Code or tests that write the progress `PlayerPrefs` key directly **must call `ProgressStore.Invalidate()`** afterwards.

---

## 5. Gameplay Feedback & Animation Rules (every level, including future ones)

These rules keep the game feeling consistent as levels are added. They are mandatory for new levels, new target kinds and new mechanics.

1. **Feedback follows the event, never the level number.** Effects are chosen by target kind and event (tap, chain, bomb, miss, combo milestone), so any new level automatically gets the full set. Do not gate core feedback on `LevelId`. Per-level themed flourishes (the `level == N` extras in `World2Effects.Chain` / `Hit` / `DrawBoard`) are optional decoration on top.
2. **Required feedback per event** (wired in `GameManager`):

   | Event | Required feedback |
   |---|---|
   | Target spawns | Spring-in scale in `TargetView` (0.2 s, ~4% overshoot). |
   | Any target tapped | `hud.WorldEffects.Burst(position, tint, radius, kind)` + `hud.Pop(..., source: target)`: ball squash-and-burst with its star face, lightened ring, golden star sparkles on gravity arcs, bouncy score that floats up. Golden/Core labels use `labelScale` 1.3. |
   | Golden / Time / Core | `Burst` kind extras: gold shards + star shower; double ripple; gold shards. Core hit `Shake(5, .14)`; Core destroyed `Finale` + `Shake(12, .36)` + "CORE DESTROYED!" callout. |
   | Chain (3+ same colour) | `WorldEffects.Chain` (links, per-member white flash, rotating reaction style) and one `Pop` per member delayed by `i * TargetView.ChainStepSeconds` with `source: member`. Callout at the last link: CHAIN! / SUPER CHAIN! (5+) / MEGA CHAIN! (8+). Shake 5 (5+) or 10 (8+). Mega adds `Finale`. |
   | Bomb tapped | `HazardPop` (bomb swells for `World2Effects.BombFuseSeconds`), `WorldEffects.Hazard` layered explosion (fireball, shockwave, debris, embers, smoke, scorch, red arena wash), `Shake(16, .34)` delayed by the fuse, explosion sound, "BOOM! -N" text. |
   | Bomb blocked by Shield | Implosion into a cyan hexagon, "BLOCKED!", `Shake(6, .2)`, shield sound. |
   | Bomb expires untouched | `WorldEffects.Fizzle` (small smoke puff), no penalty. |
   | Target expires (miss) | "MISS" `MessagePop` + grey `WorldEffects.Puff`. Misses must read, never celebrate. |
   | Combo milestone 5/10/20/30/50 | Callout NICE! / GREAT! / AWESOME! / AMAZING! / LEGENDARY!, HUD fever label, milestone sound. Keep the thresholds identical in `GameManager.milestoneCombos`, `FeedbackPlayer.PlayTap` and `GameHUD.HitJuice`. |
   | Results | Star stage + turning sunburst, score count-up, stat tiles, reward chips, tinted hint chip, confetti on a win. |

3. **Restraint (not overwhelming).** Each effect lasts about 1 s or less (smoke/scorch up to 1.4 s). Shake only for big moments (Bomb, Mega, Core, 5+ chains, Shield block), never on ordinary taps. One callout per event. Arena-wide flashes only for Mega and Bomb, at ≤ 0.32 alpha. Ring/spark colours lean toward white so they read on an arena of the same hue.
4. **How to build an effect.** Add it to `World2Effects` (a new `Emit` kind drawn in the shared mesh) or to the pooled popups (`GameHUD.Pop` / `MessagePop` / `HazardPop` / `Callout`). Never spawn GameObjects, particle systems or materials at runtime. Shake only through `hud.Shake`.
5. **Every effect must**: do nothing under reduced motion (`Emit` already returns; popups fall back to static text), freeze on pause (driven by the HUD feedback clock), clear on replay/navigation (`ClearFeedback`), and drain to zero (finite life).
6. **Colours**: use `UIArt` tokens. Gold for rewards, green for success, rose/red/orange for hazards and misses, cyan for Shield, indigo→violet for brand gradients.
7. **Prove it.** Add checks to `Tools/Campaign20Smoke.cs` (effects emitted, drained to 0, nothing under reduced motion, Canvas object count unchanged) and capture frames with `UnitySmoke.Capture` to review them.

### Adding a new target kind or mechanic
Add the `TargetKind`, its authored symbol on `NormalTarget.prefab` (toggled in `TargetView.Spawn`), its `Burst` extras (or a `Hazard`-style sequence if harmful), its hint tone in `GameHUD.SetHint` (`successHints` / `missHints`), and tests.

---

### Player instructions (every level)

Players must always know exactly what clears a level. Each level shows three parts, all from `LevelConfig`:

1. **Goal** — `Goal` (no time) / `GoalText` (with "in N seconds"). Exact numbers and nouns the player can count: "Make 3 chains", "Hit 15 targets in a row", "Block 1 Bomb with your Shield". Never vague words like "streak", "combo" or "mastery" without the count.
2. **How to do it** — the level asset's `mechanicHint`: one concrete sentence, under ~80 characters, naming the button or target to tap and what resets progress. No YAML-breaking `": "` inside it.
3. **Win rule** — `WinRule`, generated per objective kind (immediate finish, timed, or "one Bomb tap fails the goal").

Star tiles use `OneStarRule` / `TwoStarRule` / `ThreeStarRule` ("no misses", plus "or Bomb hits" on Bomb levels). Multi-step mechanics also give live step hints during play (Shield: USE SHIELD → SHIELD ON + "NOW TAP A BOMB" → "SHIELD BLOCKED THE BOMB"; tapping a Bomb with Shield off says "Press SHIELD first"). A new `ObjectiveKind` must add a line to `Goal`, `ObjectiveLabel` and `WinRule`. `Tools/Campaign20Smoke.cs` checks on every level that the briefing shows `GoalText` and `WinRule` and that all instruction and star texts fit.

---

## 6. Adding Levels Beyond 20

Generic feedback needs no changes. Do the following:

1. Create `LevelNN.asset` (`LevelConfig`) in `Assets/Data/Levels/` and add it to `GameManager.levels` in the scene. Write its `mechanicHint` as one exact "how to" sentence (see "Player instructions" above) and add its goal to the README tables.
2. Raise `ProgressStore.LevelCount` (saved arrays grow automatically on load; "Next level" and "all levels complete" follow it).
3. Add a level-select row: `Tools/Run-Campaign20.ps1 -Mode ListAuthor`, or duplicate a row in the scene **and** the prefab; extend `GameHUD.levelButtons` / `selectionTexts` and the list content height.
4. Pick the arena art in `GuiArenaTheme` (levels above 10 use the city scenery).
5. Results show chain stat tiles for `LevelId > 10`; adjust `GameHUD.ShowResults` if a new mechanic needs its own stats.
6. Optional: a themed flourish in `World2Effects` (keep it additive).
7. Extend the smoke tests (they assume 20 levels), README and this file's level list.

---

## 7. UI, Visual and Performance Rules

- **Visual system:** lavender background, white rounded cards with soft shadows, navy Poppins text (ExtraBold titles/numbers, Bold buttons, SemiBold labels, Medium body), indigo→violet gradient pills for primary actions, pastel tiles. Gradients via `UIGradient`; colours set from code via `UIArt`. Coin amounts in small slots use `UIArt.Compact` (full digits to 99,999, then K/M/B); detail views show exact `N0`.
- **Images:** non-stretched Images keep `preserveAspect` on (GuiSmoke enforces it); decoration has `raycastTarget` off.
- **Live editing:** the owner keeps Unity open. Edit through the Unity CLI (`unity command script recompile`, then `script eval_file --file Tools/LiveEdits/NN-name.cs`), apply to the scene and the prefab, save both, and add the script to `Tools/LiveEdits/README.md`. In editor scripts compare Unity objects with `== null` (not `??`).
- **Performance:**
  - Never read `PlayerPrefs` or parse JSON per frame; use `ProgressStore` (cached).
  - Do not write transforms or colours every frame when the value is unchanged (see `SetScale` in `GameHUD.Juice.cs`). A transform change re-batches the whole Canvas.
  - Anything that moves continuously (scroll content, the results sunburst) sits on its own nested Canvas.
  - The camera and URP asset have HDR off. Avoid adding more full-screen translucent layers.
  - Frame rate is hard-capped at 60.

---

## 8. Verification & Testing

Before concluding any work, run:

```powershell
# Pure C# round rules, scene references, runtime + editor compile:
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
# Full campaign + animation checks, then GUI layout checks (run one at a time; each starts Unity asynchronously):
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode GuiSmoke
# After Smoke: progress survives a fresh process. World 1 regression:
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Persistence
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnitySmoke.ps1 -Method RunWorld1
```

Results land in `Temp/Campaign20Validation/*.txt` and `Temp/Level1Validation/world1-results.txt`, with captures (`gui-*.png`, `vfx-*.png`, `pop-*.png`, `bomb-*.png`) beside them. If an editor test file fails to compile, Unity silently runs the previous build: check the logs in `Logs/Validation/` for `error CS` and confirm new checks appear in the results.
