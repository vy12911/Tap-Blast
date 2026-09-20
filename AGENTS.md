# AGENTS.md — Agent Guide for Tap Blast

> **MANDATORY FOR ALL AI CODING AGENTS**: Read this document before making any code modifications, inspecting issues, or adding features to this codebase.

---

## 1. Game Overview

**Tap Blast** is a fast-paced 2D mobile arcade tap game built with **Unity 6000.3.17f1** using Universal Render Pipeline (URP), uGUI, and the Unity Input System. Designed for mobile portrait orientation (**9:16 aspect ratio**, 720 × 1280 reference resolution).

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
    - **Level 3 ("STREAK STARTER")**: Reach a 5-hit streak; timed mastery play.
    - **Level 4 ("PRECISION PUSH")**: Score 1,000 points (Small target sizes 85–95px).
    - **Level 5 ("ON THE MOVE")**: Reach a 10-hit streak (drifting/moving targets).
    - **Level 6 ("GOLDEN OPPORTUNITY")**: Tap 3 Golden targets (100 base points each before combo multiplier).
    - **Level 7 ("FAST & FURIOUS")**: Score 2,000 points (0.5s spawn acceleration).
    - **Level 8 ("TICK TOCK")**: Collect 3 Time Bubbles (+3s each; at most six spawn per round).
    - **Level 9 ("MASTER STREAK")**: Reach a 15-hit streak (advanced combo challenge).
    - **Level 10 ("GOLDEN CORE")**: Destroy the 5-hit Golden Core; continue timed play for score mastery.
- **Star Rating (1–3 Stars per Level)**:
  - 1 Star: Complete level / meet primary goal.
  - 2 Stars: Reach higher score threshold.
  - 3 Stars: Meet 2-star threshold + zero target misses + streak goal.
- **Player Economy & Progression**:
  - Unlocked levels, earned coins, and XP persist locally.
  - Every 100 XP levels up the player profile.

---

## 2. Game Design Documents (GDD) Status

- **Full GDD File**: [`Assets/Design Doc/Tap_Blast_FULL_GDD_v1.0.pdf`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Design%20Doc/Tap_Blast_FULL_GDD_v1.0.pdf) — Production Design document outlining the 100-level campaign, target types (Normal, Golden, Time, Bomb, Perfect, Rainbow, Chain), power-ups, Ultimate meter, daily challenges, cosmetics, economy, and technical architecture.
- **World 1 Level & XP Guide**: [`Assets/Design Doc/World1_Level_Objectives_and_Progression.md`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Design%20Doc/World1_Level_Objectives_and_Progression.md) — Detailed table of level requirements, star thresholds, target mechanics, and player XP leveling formulas.
- **Authoritative Implementation References**:
  1. [`README.md`](file:///d:/own/tap%20blast/Tap-Blast/README.md) — World 1 active rules, scoring formulas, rewards, performance standards, and tuning instructions.
  2. [`Documentation/Canvas-Editing.md`](file:///d:/own/tap%20blast/Tap-Blast/Documentation/Canvas-Editing.md) — Canvas object hierarchy, visual feedback rules, and UI prefab boundaries.
- **Future GDD Features (Reserved for Higher Levels)**:
  - Targets: Chain targets (L11), Bombs (L16), and later Perfect/Rainbow target types.
  - Systems: Power-ups, Perfect bonuses, Ultimate meter.

---

## 3. Architecture & Codebase Map

| Directory / File | Responsibility |
|---|---|
| [`Assets/Scripts/GameManager.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/GameManager.cs) | High-level round manager, UI events, pooling, spawner coordination, application pause/focus. |
| [`Assets/Scripts/Core/RoundSession.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Core/RoundSession.cs) | Pure C# logic (zero Unity dependencies). Scoring, combo tracking, countdowns, star calculation. |
| [`Assets/Scripts/Data/LevelConfig.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Data/LevelConfig.cs) | `ScriptableObject` defining level parameters (duration, objectives, target sizes, star limits, rewards). |
| [`Assets/Scripts/Data/ProgressStore.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Data/ProgressStore.cs) | Versioned `PlayerPrefs` wrapper. Handles level unlocks, coins, XP, round tokens, settings. |
| [`Assets/Scripts/Targets/`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Targets/) | `TargetPool` (fixed preallocated target pool), `TargetSpawner` (positioning/spacing), `TargetView` (view & hit testing). |
| [`Assets/Scripts/UI/`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/UI/) | `GameHUD.cs`, `GameHUD.Juice.cs`, `ResultReveal.cs`, `TapHandHint.cs` — Canvas bindings and animation. |
| [`Assets/Scripts/Feedback/FeedbackPlayer.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Feedback/FeedbackPlayer.cs) | Audio synthesis, screen shake, haptics/vibration, reduced-motion preferences. |
| [`Assets/Data/Levels/`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Data/Levels/) | Authored level configs (`Level01.asset` through `Level10.asset`). |
| [`Tools/`](file:///d:/own/tap%20blast/Tap-Blast/Tools/) | PowerShell validation scripts and automated Unity editor smoke test harness. |

---

## 4. Key Rules & Coding Standards

1. **Performance & Allocation Constraints**:
   - **No per-target `Update()` calls**, `Coroutines`, physics simulation, or `LINQ` in gameplay execution loops.
   - Use fixed preallocated pools (`TargetPool`, pooled feedback particles). Do not instantiate/destroy targets dynamically during gameplay.
2. **UI Architecture Rules**:
   - **Always use `TMPro.TMP_Text` for UI text fields, parameters, arrays, and component lookups.** Author Canvas labels as `TextMeshProUGUI`, assign a TMP font asset, and wire the saved prefab/scene references. Do not introduce `UnityEngine.UI.Text` or construct replacement text objects at runtime.
   - **Do NOT reconstruct UI elements via script**. The UI is built visually in `Assets/Prefabs/UI/GameCanvas.prefab` (`Assets/Scenes/Level01.unity`). Scripts only bind fields, update text, and toggle panel visibility.
   - Dynamic screens fit inside the 720 × 1280 reference safe area frame.
3. **Pure C# Logic Decoupling**:
   - Core round rules and scoring reside in [`RoundSession.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Core/RoundSession.cs). Maintain pure C# separation without `UnityEngine` dependencies for core logic to allow standalone CLI testing.
4. **Local Progress Security**:
   - Round tokens and session rewards are validated and saved in [`ProgressStore.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Data/ProgressStore.cs) on round start and completion to prevent duplication or incomplete session grants.

---

## 5. Verification & Testing

Before concluding any work, verify your changes by running the CLI validation suite from PowerShell:

```powershell
# Run pure C# round-rules validation (48 rule checks):
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
```

---
