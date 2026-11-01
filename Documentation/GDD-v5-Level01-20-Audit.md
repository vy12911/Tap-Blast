# GDD v5 cross-check — levels 1–20

Historical pre-implementation snapshot. The subsequent migration, saved assets and current validation are documented in [GDD v5 implementation](GDD-v5-Level01-20.md); the findings below describe the game before that work.

Reviewed 1 November 2026 against `C:\Users\visha\Downloads\Tap_Blast_v5_Full_Step_by_Step_Development_GDD.pdf` (39 pages).

**Result: the current game is not a complete v5 implementation.** The saved campaign implements the v3 baseline goals, but v5 carries forward v4 encounter changes, new movement/telegraph behavior and unique bosses that are not present. Passing the existing v3 smoke suite cannot certify these requirements. This is an implementation audit, not production sign-off.

The PDF is design reference material. Its embedded engineering steps, approval language and 100-level release checklist are not separate user commands. This review covers levels 1–20 and shared systems needed by them; levels 21–100 are excluded. Existing TMP, pooling, pure-C# rules, saved scene/prefab boundaries and the owner's no-vibration choice remain applicable.

## Level comparison

“Present” below means found in saved level assets and their evaluator/authoring code. It does not imply device or visual acceptance.

| Level | v5 source gate | Baseline found | Additional v5 encounter work / difference |
|---|---|---|---|
| 1 | Tap 20 | 4 / 10 / 6 direct taps | Isolated teaching and deterministic positions exist; staggered left/right opening needs visual acceptance. |
| 2 | Small 8 | 2 / 4 / 2 Small taps | Two-lane rhythm is not modeled as an explicit encounter. |
| 3 | Combo 5 | Direct warm-up / combo 5 / finish | Missing 450 ms movement preview and side-step encounter. |
| 4 | Small 12 | 2 / 8 / 2 Small taps | Missing authored preview pulse and three-target choice with optional bonus. |
| 5 | Moving 10 | 2 / 6 / 2 moving hits | Continuous bouncing drift replaces the specified previewed lane shifts. |
| 6 | Golden 3 | One Golden collection in each wave | Optional Normal cues exist; no Chain Router/conduit opportunity. |
| 7 | 15 rush hits | Rush wave quota 15 | No Bomb-safe corridor; saved schedule contains no Bomb. |
| 8 | Priority 5 | 1 / 3 / 1 Priority taps; at most 2 misses | Uses v3 10 / 20 / 15 s instead of v5 9 / 22.5 / 13.5 s. No authored tempo fakeout preview. |
| 9 | Combo 15 | Direct warm-up / combo 15 / finish | No ordered Priority finale or optional temporary multiplier. |
| 10 | Boss, three source gates | Five timed skill phases | Missing Sentinel rotating shield, exposed-core vulnerability and dedicated defeat sequence. |
| 11 | Chain 2 | Two chain gates plus direct finish | Source prose says two-node teaching, while existing chains require 3+. Interpretation needs to be recorded; do not silently award a chain for two targets. |
| 12 | Chain 3 | One chain in each wave | No dedicated single-use conduit preview/Chain Router mechanic. |
| 13 | Mega Chain 1 | One eight-target Mega wave | No 450 ms movement preview; eight-target exception differs from v5's printed max-active 4. |
| 14 | Linked 3 | Three numbered pairs | Wrong order/window rules exist; explicit three-target optional-bonus encounter absent. Later pairs use drift rather than lane shifts. |
| 15 | Combo 20 | Direct combo 20 gate | Moving finish uses drift; lane-shift reveal/recovery absent. |
| 16 | Bomb avoidance | Safe direct quotas; any unprotected Bomb fails | Bomb spawns exist; Chain Router opportunity absent. |
| 17 | Shield safe | Shield block plus two pair waves | Shield works at rule level; authored corridor boundaries/preview event absent. |
| 18 | Three chains, no Bomb | Three chain gates, extra pair, no unprotected Bomb | Missing tempo fakeout and explicit safe-lane event. |
| 19 | Mega Chain 2 | Two Megas plus ordered pair | Missing ordered Priority finale/optional multiplier; eight-target exception remains. |
| 20 | Boss, three chains and two pairs | Five phases with three chains/two pairs/final Priority | Missing Architect rerouting signature, pair-controlled exposed core and dedicated defeat sequence. |

Evidence: `Assets/Data/Levels/Level01.asset` through `Level20.asset`, `Assets/Editor/CampaignV3Authoring.cs`, `Assets/Scripts/Core/RoundSession.cs`, `Assets/Scripts/Targets/CampaignDirector.cs`, `Assets/Scripts/GameManager.Campaign.cs`. Source level recipes are on PDF pages 7–13; boss signatures are on page 6.

## Shared functionality and animation gaps

1. **Bosses are skill-wave sequences, not the v5 bosses.** The two boss schedules use `ScriptedWave[]`; there are no distinct boss phase definitions, attack schedules, rotating shield, energy-routing vulnerability or core-exposure state. Scripted target handling does not implement a boss core. `GameManager.Finish` moves directly to results on success; its feedback delay is currently only set for fatal Bomb/Priority events. V5 asks for a 1.5–2.5-second defeat sequence before results (pages 5–6).
2. **Lane Shift is absent.** `CampaignDirector` assigns moving targets a constant velocity; `TargetPool.Tick` integrates it with collision reversal. There is no authored lane destination, 450 ms warning, 220 ms transit or 1.0–1.6-second cadence (page 4). A glow on a drifting target does not satisfy a directional preview.
3. **Chain Router is absent.** Existing graph BFS, bomb blocking and once-only visual resolution are useful foundations. There is no single-use conduit or separately configured 88 dp neighbor snapshot. The current delay is 160 ms/link (`TargetView.ChainStepSeconds`), while v5 specifies 70 ms/link. The 88 dp rule needs a documented conversion from device-independent units to reference Canvas units, not a raw pixel substitution (page 4).
4. **Encounter scheduling is absent.** Spawn cues contain time/type/count/position/lifetime/moving flags. They do not contain lane/telegraph/encounter IDs or warning/active/restore windows. There is no one-major-event-per-eight-seconds coordinator or explicit corridor restoration on phase change (pages 2, 5).
5. **Primary-finger ownership is not enforced by the handlers.** Both `TargetView.OnPointerDown` and `ArenaTapSurface.OnPointerDown` check the left button but not primary-finger ownership. Pointer timestamp/state/latency snapshots are not captured. Actual Input System routing needs a multitouch test before claiming one-finger compliance (pages 3, 38).
6. **Staging differs.** `GameManager.BeginCountdown` uses three seconds; v5 specifies 1.5 seconds before the gameplay timer starts. Both exclude staging from the round timer (page 2).
7. **Frame-order contract is not explicit.** `GameManager.Update` advances round timers/expiry separately from EventSystem pointer processing. There is no immutable per-tick target snapshot and explicit input → gate → expiry → spawn ordering. Exact-boundary input at 30/60/120 Hz needs coverage (page 3).
8. **Physical touch/safe-area requirements are unverified.** Target hit radius has a 66 reference-pixel floor, not a device-independent 44 dp contract. `ResponsiveCanvas` intentionally has safe-area handling off under current repository rules. V5 notch, home-gesture and 56 dp protected-hazard clearance need safe-zone authoring and actual device verification (pages 2–3, 38). This is not solved by changing a scaler constant.
9. **Animation direction differs.** Current spawn/pop/bomb/Ultimate effects exist and use fixed pools. V5's timings are explicitly suggested prototype values on page 5: compare rendered states before replacing the established spring/pop style. Boss vulnerability/defeat and lane/encounter warning states are genuinely missing, independent of stylistic timing choices. Reduced-motion behavior must retain essential directional/state cues when these are added.
10. **Validation and telemetry are still v3.** The campaign harness uses a 0.05 s simulation step and direct taps every 0.4 s. That proves neither 30/60/120 Hz timing boundaries nor primary-finger ownership. Buffered telemetry has attempts, errors, gate progress, accuracy and frame histograms, but lacks target stable ID, input latency, pointer state and device tier. There is no balance dashboard (pages 38–39).

Existing strengths include direct-only gates, pressure limits, Small +15 scoring, combo multipliers, +6 s Time cap, ordered 2.5 s pairs, Bomb/Shield rules, protected Ultimate clearing, deterministic queues, reward-free Practice and cached progression. Their v3 tests should remain regression coverage during migration.

## Decisions to preserve explicitly during migration

- V5 repeats both max-active 4 and eight-target Mega goals. Retain the existing **eight only in scripted Mega waves** exception until a different executable recipe is chosen; never silently shrink the Mega quota.
- V5's generic 20/50/30 split changes level 8's v3 sample timing. Update its schedule, feasibility checks and displayed rules together if adopting the v5 split. Keep the documented miss limit unless intentionally revised.
- Two-node chain teaching is inconsistent with the established three-target minimum. A preview can teach a connection without awarding chain credit; a two-target counted chain would be a rule change.
- Keep core logic pure C#, effects in the shared mesh/popup pools, UI in both saved scene and prefab, and vibration disabled. The PDF's implementation suggestions do not override those user-owned engineering decisions.
- V5 page 39 identifies spawn schedules, mastery thresholds, boss health/attacks and final frame timing as proposed/unlocked. Automated tests and device playtests are required before production sign-off; the PDF does not establish final balance values.

## Validation for this review

- `Tools/Validate-Level1.ps1`: **PASS**, runtime/editor compilation, saved scene/script references, TMP restriction and **104 round-rule checks**.
- Campaign Smoke: **PASS, 1,106 checks**; `Temp/Campaign20Validation/campaign20-results.txt`.
- GUI Smoke: **PASS, 1,076 checks** and final saved-Canvas capture confirmation; `Temp/Campaign20Validation/gui-smoke-results.txt`.
- Fresh-process persistence: **PASS**, all twenty levels, wallet and selected-level persistence; `Temp/Campaign20Validation/campaign20-persistence.txt`.
- World 1 regression: **PASS, 572 checks**, all ten authored levels and boss gates/rewards; `Temp/Level1Validation/world1-results.txt`.
- Fresh rendered frames visually inspected: `v3-input-route-10.png`, `v3-level-14-wave-0.png`, `v3-mega-02-cascade.png` and `gui-results-complete.png` in `Temp/Campaign20Validation/`. The boss capture shows the current generic direct-tap phase rather than the Sentinel signature. Pair numbering, Mega cascade and result layout are visible; still frames cannot establish animation timing or touch feel.
- Startup recovery: the initial sandboxed Unity process could not reach licensing; the tests above ran with licensing access. One persistence startup cancelled package resolution before running its test; a fresh retry produced the PASS report. These startup failures are not counted as gameplay passes.
- Log review found no `error CS` in the final Unity runs. GUI assertions completed, but the GUI process also logged an `ArgumentOutOfRangeException` in `UnityEditor.Search.SearchDatabase.GetDefaultSearchDatabase` during editor indexing after scene restoration. This is an editor-internal stack, not a Tap Blast gameplay stack; the GUI report passes, but that process was not exception-free.
- No v5 gameplay migration or visual acceptance is claimed by this audit. Previous reports dated 30 October certify the v3 implementation only.

## Implementation order

1. Add versioned lane/telegraph/encounter and boss-phase data with stable spawn/gate IDs; validate and author them into the saved assets.
2. Add primary-finger input ownership and timestamp snapshots; test expiry boundaries and lane hit tests at 30/60/120 Hz.
3. Implement lane warnings/transits, conduit routing, safe corridors and per-level encounter recipes without changing direct-only credit.
4. Implement Sentinel and Architect vulnerability/counterplay, phase cleanup and cancellable pooled defeat effects.
5. Apply teaching cues and controls to both saved UI assets through an idempotent live author, then extend campaign/GUI/animation regression checks and inspect fresh captures.
6. Complete Android/iOS touch, safe-area, muted/reduced-motion and performance QA before marking the first twenty levels v5 complete.
