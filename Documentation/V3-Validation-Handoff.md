# GDD v3 implementation handoff — levels 1–20

The active specification is `Assets/Design Doc/Tap_Blast_FULL_Development_GDD_v3.0.pdf`. See [the implementation guide](GDD-v3-Level01-20.md) for all twenty recipes and design decisions.

## Delivered

All twenty saved level configurations use deterministic waves and mandatory gates. The runtime includes Priority targets, ordered moving pairs, graph chains and circular Mega blasts, Shield/Bomb interactions, pressure, direct accuracy, Ultimate protection, recovery tips and optional reward-free Practice. The unpacked scene, matching UI prefab and target prefab are saved. Existing progression is preserved.

The authoring entry point is `Tools/LiveEdits/20-campaign-v3.cs`; the build validator rejects impossible quotas, insufficient opportunities and invalid wave caps. Runtime feedback uses the existing fixed pools, honors pause/reduced motion and clears on navigation/replay. Local bounded telemetry records attempts, mistakes, phase outcomes and frame-time summaries.

## Automated validation — 30 October 2026

| Check | Result | Evidence |
|---|---|---|
| Scene references, runtime/editor compilation and pure round rules | PASS; 104 rule checks | `Logs/Validation/round-rules.txt` and compiled runtime/editor assemblies |
| Full twenty-level campaign and animation acceptance | PASS; 1,106 checks | `Temp/Campaign20Validation/campaign20-results.txt` |
| Saved Canvas, text fit and device-aspect checks | PASS; caption and stat text fit their bounded cards | `Temp/Campaign20Validation/gui-smoke-results.txt` |
| Progress survives a fresh Unity process | PASS | `Temp/Campaign20Validation/campaign20-persistence.txt` |
| First ten levels / World 1 regression | PASS; 572 checks | `Temp/Level1Validation/world1-results.txt` |

The campaign harness drives real target handlers at 2.5 direct taps per second. The animation polish adds contracting chain anticipation, radial impact streaks, a local Priority focus burst and stable settled target scale. It checks every gate/reward, invalid pair order and timeout/retry, required-target protection from Ultimate, input raycasts, deterministic replay, practice, pause, reduced motion, fixed UI object counts, feedback draining and the optional slow-frame effects budget. These checks establish scripted solvability and integration; they are not measurements of human difficulty.

Rendered evidence includes `v3-mega-01-first-pop.png`, `v3-mega-02-cascade.png`, `v3-mega-03-central-shockwave.png`, `v3-level-14-wave-0.png`, `v3-pressure-warning.png` and `gui-results-complete.png` under `Temp/Campaign20Validation/`. These generated files are local validation artifacts and are not source assets.

## Scope decisions and remaining production QA

The specification requests both a four-target active cap and an eight-target Mega. Scripted Mega waves allow eight; all other waves retain four. This exception is serialized and validated. The more specific level 8 timing/miss recipe takes precedence over generic wave percentages. Other interpretation details are in the implementation guide.

Physical Android/iOS testing, measured device frame times and thermal behavior, accessibility checks on actual screens, and human difficulty/playtest notes remain pending. Automated editor checks do not satisfy the GDD's full production-readiness definition. Later-world target families and campaign content above level 20 are outside this delivery.