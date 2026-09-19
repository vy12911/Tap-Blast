# World 1 Level Objectives & Progression Guide

This document defines the complete level requirements, star criteria, rewards, and player XP leveling formulas for **World 1 (Levels 1 to 10)** in Tap Blast.

---

## 🎯 World 1 Level Objectives & Requirements

| Level | Name | Objective Type | ⏱️ Time | Primary Requirement (1 Star) | ⭐⭐ 2 Stars Goal | ⭐⭐⭐ 3 Stars Goal (Mastery) | Coins | XP |
|---|---|---|---|---|---|---|---|---|
| **Level 01** | **FIRST SPARK** | `Taps (0)` | 60s | Tap **20 targets** within 60s | Score **240+** | Score **240+** + 0 misses + 20x combo | 50 | 25 |
| **Level 02** | **SIZE IT UP** | `Score (1)` | 45s | Score **500 points** within 45s | Score **750+** | Score **750+** + 0 misses + 20x combo | 75 | 35 |
| **Level 03** | **STREAK STARTER** | `Taps (0)` | 45s | Tap **25 targets** with combo focus within 45s | Score **320+** | Score **320+** + 0 misses + 15x combo | 85 | 40 |
| **Level 04** | **PRECISION PUSH** | `Score (1)` | 45s | Score **1,000 points** (small target sizes 85–95px) within 45s | Score **750+** | Score **750+** + 0 misses + 15x combo | 100 | 45 |
| **Level 05** | **ON THE MOVE** | `Taps (0)` | 45s | Tap **30 moving targets** (drifting velocity) within 45s | Score **700+** | Score **700+** + 0 misses + 15x combo | 110 | 50 |
| **Level 06** | **GOLDEN OPPORTUNITY** | `Score (1)` | 45s | Score **1,500 points** (Golden bonus targets +50 pts) within 45s | Score **1,000+** | Score **1,000+** + 0 misses + 15x combo | 125 | 60 |
| **Level 07** | **FAST & FURIOUS** | `Score (1)` | 40s | Score **2,000 points** (0.5s spawn acceleration) within 40s | Score **950+** | Score **950+** + 0 misses + 20x combo | 140 | 70 |
| **Level 08** | **TICK TOCK** | `Taps (0)` | 35s | Tap **30 targets** (+3s Time Bubbles) within 35s | Score **1,100+** | Score **1,100+** + 0 misses + 20x combo | 150 | 75 |
| **Level 09** | **MASTER STREAK** | `Taps (0)` | 45s | Tap **35 targets** without missing within 45s | Score **1,000+** | Score **1,000+** + 0 misses + 25x combo | 175 | 85 |
| **Level 10** | **GOLDEN CORE** | `Score (1)` | 50s | Score **3,000 points** & Destroy 5-hit Boss target within 50s | Score **1,250+** | Score **1,250+** + 0 misses + 25x combo | 250 | 100 |

---

## ⚡ Level Mechanics Breakdown

- **Level 1 ("FIRST SPARK")**: Introduces the animated first-tap tutorial hand. Clock starts on the first successful tap.
- **Level 2 ("SIZE IT UP")**: Target diameters cycle between 144px, 100px, and 112px to test accuracy.
- **Level 3 ("STREAK STARTER")**: Focuses on building combo streaks up to 15x without letting target rings expire.
- **Level 4 ("PRECISION PUSH")**: Small target diameters (85px and 95px) awarding 15 base points.
- **Level 5 ("ON THE MOVE")**: Targets drift inside the arena, bouncing off screen boundaries.
- **Level 6 ("GOLDEN OPPORTUNITY")**: Spawns rare Golden targets awarding **+50 bonus points** on hit.
- **Level 7 ("FAST & FURIOUS")**: Spawn rate accelerates to 0.5 seconds with up to 6 active targets simultaneously.
- **Level 8 ("TICK TOCK")**: Spawns Time Bubbles that add **+3 seconds** back onto the timer clock when tapped.
- **Level 9 ("MASTER STREAK")**: Advanced speed and streak challenge requiring a 25x combo for 3 stars.
- **Level 10 ("GOLDEN CORE")**: World 1 finale featuring a giant **5-hit Mini-Boss target** with a health ring indicator.

---

## 📈 XP & Player Leveling System

### 1. How XP is Earned
- **Winning Rounds**: Completing any level round successfully grants the level's configured XP payout:
  - **Level 01**: 25 XP
  - **Level 02**: 35 XP
  - **Level 03**: 40 XP
  - **Level 04**: 45 XP
  - **Level 05**: 50 XP
  - **Level 06**: 60 XP
  - **Level 07**: 70 XP
  - **Level 08**: 75 XP
  - **Level 09**: 85 XP
  - **Level 10**: 100 XP
- **Replays Included**: Replaying completed levels continues to award full Coins and XP.
- **Failed / Abandoned Rounds**: If a round expires before meeting the objective or is exited early via Pause menu, **0 XP and 0 Coins** are granted.

---

### 2. Step-by-Step: How the Player Level Increases

1. **Starting State**: All new players begin at **Player Level 1** with `0 Total XP`.
2. **XP Accumulation**: Earned XP accumulates across all rounds into a single persistent counter (`TotalXP`) saved in `PlayerPrefs` via [`ProgressStore.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/Data/ProgressStore.cs).
3. **Mathematical Formula**:
   $$\text{Player Level} = 1 + \left\lfloor \frac{\text{Total XP}}{100} \right\rfloor$$
   *(In C# code: `1 + TotalXP / 100`)*
4. **Milestone Level Thresholds**:
   - `0 – 99 Total XP` $\rightarrow$ **Player Level 1**
   - `100 – 199 Total XP` $\rightarrow$ **Player Level 2** *(Crossed at 100 XP)*
   - `200 – 299 Total XP` $\rightarrow$ **Player Level 3** *(Crossed at 200 XP)*
   - `300 – 399 Total XP` $\rightarrow$ **Player Level 4** *(Crossed at 300 XP)*
   - `400 – 499 Total XP` $\rightarrow$ **Player Level 5** *(Crossed at 400 XP)*
   - `500 – 599 Total XP` $\rightarrow$ **Player Level 6** *(Crossed at 500 XP)*
   - `1,000+ Total XP` $\rightarrow$ **Player Level 11+**

5. **Level-Up UI Detection & Banner**:
   - At the end of each round, [`GameHUD.cs`](file:///d:/own/tap%20blast/Tap-Blast/Assets/Scripts/UI/GameHUD.cs) checks if a 100 XP boundary was crossed during that specific round:
     ```csharp
     bool leveledUp = receipt.TotalXP / 100 > (receipt.TotalXP - receipt.XP) / 100;
     ```
   - If `leveledUp == true`, the UI displays a gold **"LEVEL UP! PLAYER LEVEL X"** banner, animates the XP bar fill, and updates the player level tag across all menus.

