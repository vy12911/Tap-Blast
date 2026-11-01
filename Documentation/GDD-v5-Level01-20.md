# GDD v5 implementation — levels 1–20

Design reference: [Tap Blast v5 step-by-step GDD](../Assets/Design%20Doc/Tap_Blast_v5_Full_Step_by_Step_Development_GDD.pdf), pages 2–13 and 38–39. This milestone covers the first twenty levels and their shared systems. Later-world target families and levels 21–100 remain outside this milestone.

## Authored content

All twenty level assets have `designVersion: 5`, stable spawn/gate IDs, replayable schedules, lane timing and arena-event data. Normal rounds use 9 / 22.5 / 13.5 second waves; bosses retain 10 / 12 / 15 / 15 / 8 second phases. Staging lasts 1.5 seconds and does not consume round time. The existing pressure, score, stars, progression and reward-free Practice rules remain in effect.

| Level | Playable recipe |
|---|---|
| 1 | Twenty direct taps; isolated opening and staggered left/right positions. |
| 2 | Eight Small taps in an alternating two-lane rhythm. |
| 3 | Five-hit direct streak, then previewed side-step targets. |
| 4 | Twelve Small taps; pulsed introduction, two Small targets and an optional Golden choice. |
| 5 | Ten moving hits with directional lane warnings and timed lane transits. |
| 6 | Three Golden collections; safe optional R conduit and linked circles in the teaching wave. |
| 7 | Fifteen rush hits; Bomb-safe lane boundaries and separated hazard positions. |
| 8 | Five Priority hits; tempo preview; at most two Priority misses; v5 wave timing. |
| 9 | Fifteen-hit streak, ordered !1/!2 finale and optional four-second x2 score reward. |
| 10 | Sentinel: five phase assets, rotating shield, per-phase skill counter, exposed gold core, hazard phase and two-second defeat sequence. |
| 11 | Two graph chains plus direct finish; readable isolated chain introduction. |
| 12 | Three conduit-routed graph chains, one in each wave. |
| 13 | One eight-target Mega plus direct waves; directional/route preview language. |
| 14 | Three ordered pairs, optional Golden choice and lane-shifting later pairs. |
| 15 | Twenty-hit direct streak plus previewed moving finish. |
| 16 | Safe hits with zero unprotected Bomb taps; optional conduit route. |
| 17 | Ordered pair, Shield interception and ordered finish; visible safe corridor. |
| 18 | Three chains, one pair and zero unprotected Bomb taps; tempo/safe-lane cues. |
| 19 | Ordered pair, two Mega gates, ordered !1/!2 finale and optional x2 score reward. |
| 20 | Architect: five phase assets, chain/pair routing, three chains and two pairs, hazard phase, exposed cores, final Priority check and two-second defeat sequence. |

Ordered Priority finales include explicitly scheduled replacement !1/!2 opportunities. A blast cannot clear these targets or award ordered credit. Wrong order leaves the target visible and reports the required order. Ordinary pair resets and expiry retain the existing 2.5-second retry rules.

Expired safe targets now reduce accuracy as specified in v5; they do not add pressure. Direct streak resets retain the explicitly configured campaign behavior. Untouched Bomb expiry remains penalty-free.

## Runtime and feedback

`RoundSession` remains pure C#. It owns core exposure windows, once-only direct core credit, ordered Priority credit and the temporary score multiplier, as well as the existing gates/pressure/accuracy/combo/pair/Ultimate rules. A phase cannot pass until its skill counter, pair/order requirements and exposed-core hit are all satisfied. Ultimate and chain damage cannot hit the core.

`CampaignDirector` plays deterministic cues, reserves complete groups and runs a separate boss-core emission path using the five saved `BossPhaseDefinition` assets. Each wave/phase carries a warning/active/restore event window, validated for an eight-second minimum spacing. Phase transitions clear targets and replace event state; replay/navigation clear all state.

Lane movement uses absolute simulation age: 450 ms directional warning, 220 ms transit and a serialized 1.0–1.6 s cadence. TargetPool checks the full swept path against neighboring hit regions before moving, so a blocked path cannot cross another target or Bomb. The saved target prefab has a stable input root and a separate animated `Visuals` child; spring and chain compression never shrink the input region. Essential arrows, destination rings, target IDs and boss state cues remain readable under reduced motion; cosmetic pulses/shakes/bursts are suppressed.

R conduits are single-use graph roots. The handler snapshots the breadth-first route before committing hits, visits each target once, caps at eight members and respects Bomb boundaries. Priority/pair/core targets are excluded. Chain links now resolve at 70 ms each. In this 1080-reference portrait layout, 88 logical dp is authored as 264 reference units. Existing Small targets explicitly teach precision; normal hit regions retain the 66-reference-unit radius floor.

Sentinel uses a rotating cyan shield/gold OPEN halo. Architect uses linked hexagonal energy nodes and pair/chain counterplay. Each phase exposes one core hit after its authored skill gate; the two pair phases specifically require the correct direct pair before exposure. The final skill check cannot be bypassed. Boss defeat commits scoring immediately, disables targets, plays pooled core cracks/cascades/final shockwave and waits two seconds before result/reward delivery. It is cleared on navigation/replay, and cannot award twice.

`World2Effects` draws all new cues in the existing shared mesh; there are no live particle/material/GameObject allocations. Popups, callouts, chimes and shakes use the existing feedback system. The established spring/pop/bomb styles are retained where v5 offers suggested prototype timings rather than a new rule. Unsupported later-world mechanics are not shown as active controls.

Primary touch ownership is enforced in `TapInput` using the Input System's primary touch ID; mouse/pen remain supported. EventSystem pointer-down commits gameplay before `GameManager.LateUpdate` advances expiry/spawns. Target ID, pointer position, target position, warning state and input dispatch time are buffered in telemetry. Device memory tier/platform and frame percentiles are sampled outside target loops. `Tap Blast > Campaign balance dashboard` reads the local JSONL only on request and displays attempts, wins/failures/quits, first wins, accuracy, dispatch delay and frame P95. Dispatch delay is not a measurement of physical display latency.

The default ResponsiveCanvas policy remains off in code; the v5 scene and UI template explicitly opt into safe-area insets. Both retain 1080×1920 Expand scaling and the owner's unpacked scene root.

## Authoring and verification

`CampaignV5Authoring.Author` is idempotent and saves the twenty levels, ten boss phase assets, target prefab, unpacked scene and UI template. Invoke `Tools/LiveEdits/21-campaign-v5.cs` after compiling through Unity Pipeline. `Tools/Invoke-UnityPipeline.ps1` is a local adapter for the same command endpoint when the `unity` CLI is absent from PATH; it reads the editor's protected descriptor and never prints its bearer token.

The earlier [v5 audit](GDD-v5-Level01-20-Audit.md) is a historical pre-implementation snapshot. The new campaign is exercised through the existing compatibility-named `CampaignV3Smoke` harness, extended with v5 acceptance checks. It drives real target handlers and completes every level at 30/60/120 Hz, checks core damage exclusions, delayed boss rewards, primary/secondary touch, lane warning/transit/pause, conduit cycle/once-only/Bomb boundaries, stable hit regions, Practice/replay, fixed UI object counts, reduced motion and effect draining.

## Validation — 1 November 2026

| Check | Result | Report |
|---|---|---|
| Saved references, runtime/editor compile and pure rules | PASS; 123 rule checks | `Logs/Validation/round-rules.txt` |
| Saved v5 campaign, animation and 30/60/120 Hz acceptance | PASS; 1,248 checks | `Temp/Campaign20Validation/campaign20-results.txt` |
| Saved UI, text fit and device-aspect layouts | PASS; 1,076 checks | `Temp/Campaign20Validation/gui-smoke-results.txt` |
| Fresh-process records, wallet and selection | PASS, including all four rewarded campaign passes | `Temp/Campaign20Validation/campaign20-persistence.txt` |
| World 1 / Sentinel regression | PASS; 606 checks | `Temp/Level1Validation/world1-results.txt` |
| Owner's open editor | Recompile completed with no compilation errors | Unity Pipeline `script recompile_status` |

Reviewed captures include `v5-boss-10-core-0.png`, `v5-boss-20-core-1.png`, `v5-lane-warning.png`, `v5-conduit-routing.png` and `v5-defeat-full-01-crack.png` through `v5-defeat-full-03-finale.png`. The full-quality effect-library sample is captured in the Golden lesson harness; the `v5-boss-*-defeat-*` frames exercise actual boss flow under the stress test's lower effects budget. Both paths drain and reduced motion emits no animated defeat effects.

The compatibility-named test harness still prints some “V3” labels; its current fixture explicitly validates all saved assets as design version 5 and runs the v5 tests. Persistence expectations include one main campaign pass plus three refresh-rate replays, rather than the old one-pass reward total.

No `error CS` occurred in the final validation logs. Some batch editors log an internal `UnityEditor.Search.SearchDatabase` indexing exception after assertions/scene restoration. It is outside the Tap Blast stack; the acceptance reports pass, but those editor processes are not exception-free. Unity-authored YAML also retains its conventional blank-value whitespace.

Physical Android/iOS safe-area/touch testing, human balance approval and measured battery/thermal/device performance remain production QA; automated editor checks cannot establish these. Exact authored positions, one-hit phase cores and encounter timings are executable prototype tuning, not a claim of device certification or final human balance sign-off.
