# World 2: Combo City (Levels 11–20)

Implemented against GDD v1.0, pages 11–13 (chain thresholds and detailed early levels) and page 8 (contextual tutorials). The World 2 finale is **Chain Master**, not another Golden Core. World 1 retains its Level 10 Golden Core.

## Level specifications

| Level | Name | Primary objective | Time | Two-star score | Three-star best combo | Coins | XP |
|---|---|---|---|---|---|---|---|
| 11 | Chain Spark | Make 2 chains | 45s | 900 | 12 | 200 | 110 |
| 12 | Double Impact | Make 3 chains (five-target clusters) | 45s | 1,500 | 20 | 225 | 120 |
| 13 | Mega Burst | Make 1 Mega Chain | 45s | 2,500 | 24 | 250 | 130 |
| 14 | Drift Cluster | Score 3,000 points (moving clusters) | 45s | 3,800 | 25 | 275 | 140 |
| 15 | Combo City | Hit 20 targets in a row | 45s | 2,200 | 30 | 300 | 150 |
| 16 | Danger Signs | Tap 20 targets without tapping a Bomb | 45s | 650 | 15 | 325 | 160 |
| 17 | Shield School | Block 1 Bomb with your Shield | 45s | 700 | 15 | 350 | 170 |
| 18 | Safe Chain | Make 3 chains without tapping a Bomb | 45s | 1,800 | 25 | 375 | 180 |
| 19 | Mega Mastery | Make 2 Mega Chains | 45s | 3,200 | 32 | 400 | 190 |
| 20 | Chain Master | Hit 30 in a row and make 5 chains | 55s | 4,500 | 40 | 500 | 200 |

The objective column is the exact goal text shown to the player (`LevelConfig.Goal`). Level 17 is taught in two steps: press **USE SHIELD**, then tap a red Bomb while the button reads **SHIELD ON**; pressing Shield alone does not count. See the README section "What the player is told".

Objectives follow the detailed GDD table. Names, durations, rewards, scores, cluster spacing, movement speeds and bonuses are implementation tuning choices. Level 16's 20-safe-target minimum prevents completing the avoidance lesson by doing nothing. The GDD describes “2-chain/3-chain” lessons without a precise interaction algorithm; this implementation teaches its explicit 3/5/8 linked-target thresholds in levels 11/12/13.

## Chain Blast — updated user rule

The user's requested color-matching rule supersedes the earlier spatial LINK implementation and its collateral area blasts.

- Targets use the original colored target artwork, with no LINK label. Starting at level 11, targets appear individually every 0.2 seconds at scattered, non-overlapping positions.
- If at least three live targets share the tapped target's color anywhere on the board, all targets of that color join the reaction. Adjacency is not required. One or two matches behave as individual taps.
- Other colors, Golden targets, Time Bubbles, bosses and Bombs stay untouched. Matching compares the original spawn color, not the temporary detonation flash.
- Three matches form a basic chain; five form Chain 2; eight form Mega; twelve form Ultra. Every matched target scores once and advances combo. Tier bonuses remain 25 / 75 / 200 / 400.
- Each authored wave offers 3/5/8 of its main color plus two targets of other colors. The main color rotates. This preserves attainable campaign objectives while making color selection meaningful. Tap the other colors individually to clear the board; waiting for eight of one color enables Mega objectives.
- The tapped target detonates first; remaining matches follow nearest-first at 160 ms intervals, with a color-to-white charge, colored burst and rising explosion sound. Pending targets reject repeat input and do not expire.
- Moving boards retain motion and safe Bomb separation. Pause freezes the reaction, replay clears it, and reduced motion keeps matching/scoring while removing the animated delay.
- `clusterSize` is retained as the serialized tuning field for the offered same-color count; it no longer defines a spatial cluster.

## Bombs and Shield

- Levels 16–20 introduce red **X / BOMB** targets at random safe positions with random headings and speeds (60–100 reference pixels/s at level 16, rising by 10 per level). Ignore it until it expires. Expired Bombs cause no miss or combo penalty.
- An unprotected Bomb tap awards no hit or combo, resets the current streak, and subtracts up to 50 points without allowing negative score. It invalidates the no-Bomb objectives in levels 16 and 18, including after their count requirement was met.
- The opening wave guarantees a Bomb for its lesson. Later waves use `bombChance`, rising from 20% at level 16 to 60% at level 20 in 10-point increments. At most one Bomb is active. An untouched Bomb does not delay the next cluster; it stays separated from valid targets while it moves. Bombs bounce at arena edges and reverse before entering another target tap area. Movement uses bounded substeps and swept clearance checks; frame hitches do not let Bombs cross through targets.
- Level 17 introduces the saved Shield button. One activation protects against one Bomb until consumed. It preserves score and combo, records a Shield block, and cannot stack. One charge is granted per round and restored on replay; levels 18–20 retain this allowance.
- Activating Shield alone does not complete level 17: the player must block a Bomb. Absorbed hits do not count as unprotected Bomb mistakes.
- Pause/countdown/results reject Shield use. Pause freezes target movement, lifetime, feedback and the round clock.

## Completion, saves and navigation

All World 2 levels remain timed. One star requires the complete primary objective at timeout; two also require the configured score; three also require zero expired valid targets, zero unprotected Bomb hits, and the configured best streak. Level 20 checks both chain count and best streak.

The build scene serializes all 20 configs; Editor and player builds use the same list. World buttons and Shield controls are saved Canvas elements. Next opens level 11 after a successful level 10; level 20 displays World 2 completion with no unavailable Next button. World 1 remains accessible after unlocking World 2.

Existing wallet, XP, stars, selected level and unlocks are retained. Historical World 2 records from placeholder objectives remain records; future attempts use the GDD objectives. Best combo is now saved per level. Rewards still require the level-bound round token and are consumed once.

## Verification

Run `Tools/Validate-Level1.ps1` for runtime/editor compilation, pure rule checks and serialized campaign references. Run `Tools/Run-Campaign20.ps1 -Mode Smoke` for isolated Unity simulation and captures. The report is `Temp/Campaign20Validation/campaign20-results.txt`; logs are under `Logs/Validation/`. The runner starts Unity asynchronously: inspect the final report and process exit before another run. Use `-Mode Persistence` after a successful smoke run to verify records in a fresh Unity process.

The simulation covers all 20 authored objectives and attainable three-star thresholds, transitions, pause/replay, safe hazard spacing, connectivity, Shield, text fit and duplicate rewards. It does not establish human difficulty, Android hardware performance or touch feel; those require a rebuilt APK and device playtesting.

## Scope after level 20

The GDD's campaign introduces other power-ups in World 4 and Perfect Tap in World 5. Shield is implemented early because level 17 explicitly requires it. Perfect, Frenzy, Ultimate, Decoys, daily modes, shops, cosmetics, monetization and levels 21–100 are outside this first-20-level campaign implementation. This follows the detailed progression table where the document's broader MVP recommendations differ.
