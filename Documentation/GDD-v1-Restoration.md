# GDD v1 restoration through level 20

Latest request: 21 November 2026. Source: the supplied Downloads/Tap_Blast_FULL_GDD_v1.0.pdf, 16 pages. The detailed early campaign table on pages 12-13 defines this implementation. Document recommendations are design evidence, not independent instructions to change the owner's current UI or to build the entire 100-level product.

## Level requirements

| Level | Mechanic | Completion requirement |
|---|---|---|
| 1 | Normal circles, first-tap hand | Tap 20; first tap starts the clock |
| 2 | Cycling sizes | Score 500 |
| 3 | Combo | Best streak 5 |
| 4 | Small circles, 15 base points | Score 1,000 |
| 5 | Moving targets | Best streak 10 |
| 6 | Golden | Directly collect 3 Golden targets |
| 7 | Accelerating spawns | Score 2,000 |
| 8 | Time Bubbles | Collect 3; each grants 3 seconds |
| 9 | Combo challenge | Best streak 15 |
| 10 | Golden Core | Destroy the five-hit Core |
| 11 | Connected spatial chains | Create 2 chains |
| 12 | Five-member larger blasts | Create 3 chains |
| 13 | Eight-member Mega | Create 1 Mega Chain |
| 14 | Moving clusters | Score 3,000 |
| 15 | Combo mastery | Best streak 20 |
| 16 | Bomb avoidance | 20 safe hits and zero unprotected Bomb hits |
| 17 | Shield lesson | Activate Shield and block a Bomb |
| 18 | Chains with hazards | Create 3 chains without an unprotected Bomb hit |
| 19 | Mega mastery | Create 2 Mega Chains |
| 20 | Chain Master | Best streak 30 and 5 chains |

Level 1 ends immediately on the twentieth hit. Other goals remain at risk or allow score mastery until their timers end. Timings, spawn budgets, score/star thresholds and rewards restore the previous v1 assets, with explicit designVersion 1 and scripted=false. Existing saved GUIDs are preserved. V3/v5 boss phase assets remain on disk but are not referenced by the active levels.

## Shared mechanics and constraints

Scoring, combos, misses, collection counters, stars, Shield, Ultimate and objective evaluation live in pure C# RoundSession. The fixed target/feedback pools, stable input roots, primary-touch filtering, safe-area setup and existing feedback/art remain. No vibration is added, preserving the established project preference.

Spatial chains traverse connected same-color circles once using the preallocated result queue. Tier thresholds are 3/5/8/12. Each tier adds score, sequential impacts and a progressively larger area blast against nearby Normal/Small/Golden/Time targets. Area blasts exclude Bombs, Core and other chain groups. Cluster layouts provide connected opportunities without lucky random placement; moving clusters start moving when assembled. Bomb placement reserves the cluster slots. V1 Bombs remain stationary so they cannot enter a required cluster while it assembles; moving target groups still avoid them. Moving Bomb support remains tested for later campaigns.

The existing Ultimate meter/button is reused. Direct valid taps grant 3 charge, Golden grants 6, chains grant 10 (Ultra 25); charge caps at 100. Ultimate awards score only, cannot advance tap/collection/combo gates or damage Core, and cannot refill itself. Pause freezes its short time stretch. Reduced motion skips the time stretch and animated effects.

All level instructions and results bind the original objective fields. V3/v5 pressure, Priority, pair, conduit, wave and shielded-core rules are inactive for this campaign. Practice remains reward-free. Reward tokens protect against duplicate completion grants, with records and wallet persisted through ProgressStore.

## Explicit interpretations and scope

The GDD leaves exact graph reach, area radii, activation gesture, early-level timings/rewards and three-star thresholds to tuning. One tap activates a connected group. Ordinary chain members advance combo; Ultimate does not. Level 16 retains the established 20-safe-hit participation requirement in addition to the document's zero-Bomb goal. Level 20 is the table's Chain Master hybrid objective, not a second giant health-bar boss.

The requested boundary is campaign functionality through level 20 while preserving the current UI/assets. The GDD schedules Decoys/Dark Mode for 21-30, most power-ups for 31-40, Perfect for 41-50, and treasure mechanics for 71-80. Those are outside this boundary. Separate modes (Daily Challenge, Endless, Frenzy Rush, Treasure Rush, Master Trial), shops, collections, daily missions, achievements, ads, IAP and a backend are product-wide sections rather than mechanics of levels 1-20; this restoration does not claim to implement them. GDD art/UI redesign and haptics are superseded by the user's current-asset constraint and existing no-vibration rule.

## Verification

Run Validate-Level1, Campaign20 Smoke, GuiSmoke, Persistence and World1 regression sequentially. Inspect logs for compiler errors and final reports rather than relying on process launch. Tests cover objective attainability, UI text, input, pause/replay, effects, reduced motion, pool stability, rewards and persistence. Physical mobile-device performance, touch latency and human difficulty remain separate production QA.

Validation results on 21 November 2026:

- Runtime and editor compilation passed; 126 pure round-rule checks passed.
- Campaign smoke: 1,503 checks passed across all 20 levels, including three-star attainability, saved input routing, spatial chain and area behavior, Ultimate safeguards, effects, pool stability and duplicate reward protection.
- GUI smoke: 1,068 checks passed, with reference/tall/tablet captures from the saved Canvas.
- Fresh-process persistence passed for all 20 records, wallet, XP, unlocks and saved level selection.
- Main open editor recompiled without errors and imported/verified all 20 v1 assets through LiveEdits/22.
- The scene, UI template, target prefabs and art assets have no working-tree changes.
- The supplied PDF was copied byte-for-byte into the design folder; PDF files are marked binary to prevent line-ending conversion.
- World 1 regression: 5,293 checks passed, including phone/tablet/landscape layouts with the saved safe-area preference.

Animation follow-up (21 November 2026): Reduced Motion was enabled in the main editor player preferences. It is now off at the user's request. Golden rotation, tap shrink, Core recoil, dedicated Ultimate waves/staggered clears and a 360 ms final-tap stage were added without UI/art asset changes. See Animations-Level01-20.md for the GDD mapping and rendered capture evidence. Campaign smoke now passes 1,512 checks.
