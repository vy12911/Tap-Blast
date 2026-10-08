# GDD audit: levels 1–20

**Subsequent user-requested change:** World 2 now matches three or more targets of the same color anywhere on the board, with gradual scattered spawning and the original target artwork. This replaces the spatial LINK and area-blast behavior described in the historical audit below. Other colors and special targets are excluded. Current rules are in the World 2 guide.

Reference: `Assets/Design Doc/Tap_Blast_FULL_GDD_v1.0.pdf`, especially the detailed campaign table (pages 12–13), chain tiers (page 11), and animation table (pages 13–14).

The earlier passing tests established that the implemented rules worked. They did not establish complete GDD fidelity. Re-auditing the actual presentation identified these gaps:

| Finding | Correction |
|---|---|
| Chain targets all disappeared immediately; only score popups were staggered. | Targets now remain visible until their individual blast step, shrink, then disappear with a burst and layered impact sound. |
| Chains only cleared links and granted a tier bonus; there was no area effect. | Tier-dependent area blasts also resolve nearby Normal, Golden and Time targets. Levels 12/13 demonstrate this with an adjacent Normal target. |
| A surviving Bomb prevented another cluster from spawning. | Clusters can spawn while the Bomb remains, preserving its safe lane and preventing stacked Bombs. |
| Bomb probability was identical throughout World 2. | Later-wave probability rises gradually from level 16 through level 20. The opening lesson still guarantees one safe Bomb. |
| Large Mega rings could spill onto HUD labels. | The authored feedback container clips effects to the arena, with less opaque rings. |

## Campaign requirements

| Levels | GDD requirement checked |
|---|---|
| 1–5 | 20 taps; 500 score; 5-hit streak; 1,000 score; 10-hit streak with moving targets |
| 6–10 | 3 Golden targets; 2,000 score; 3 Time Bubbles; 15-hit streak; Golden Core |
| 11 / 12 / 13 | 2 chains / 3 chains / 1 Mega Chain; linked groups teach the explicit 3/5/8 tier thresholds |
| 14 / 15 | Moving clusters and 3,000 score / 20-hit streak |
| 16 / 17 | Avoid Bombs / use Shield to absorb a Bomb |
| 18 / 19 | 3 chains without Bomb damage / 2 Mega Chains |
| 20 | 30-hit best streak and 5 chains; Chain Master tests combined mechanics |

The GDD describes Chain Master as requiring several chain reactions; it does not specify a second giant health-bar boss for level 20. The current finale uses moving clusters, hazards, Shield and the combined objective.

## Design interpretations

The GDD specifies chain thresholds and cascading area blasts, but does not specify a precise activation gesture or timing window. Current behavior is one tap on a connected cluster, with each resolved target advancing combo. Multi-tap chain charging is not implemented or claimed as a GDD requirement. Exact distances, timings, rewards and three-star thresholds are implementation tuning. Level 16 additionally requires 20 safe hits to prevent winning by idling; this is an explicit extra rule.

The detailed early-level campaign is the scope of this audit. General product features such as daily challenges, shops, other power-ups, Perfect Tap and Ultimate are not implemented by this work. The GDD's broad MVP recommendations differ from its detailed campaign progression, so “all GDD features” would be an inaccurate claim.

## Verification boundaries

Audit validation on 2026-10-08: runtime/editor compilation and 88 pure rule checks passed; the isolated Unity campaign finished with 911 passing checks. Successive chain-frame captures were inspected after adding the arena mask. Canvas input tests wait for a rendered frame after a level change so Unity can update graphic raycast depth.

The CLI suite verifies runtime/editor compilation and pure round rules. The Unity campaign harness verifies objectives, three-star attainability, level 10→11 routing, saved references, rewards, spatial chains, area effects, cascade ordering, duplicate-input rejection, pause/replay, reduced motion, and Canvas raycast input for every level 11–20. Captures inspect animation stages and phone/tablet layouts.

These checks do not establish Android device frame rate, touch latency, or human difficulty. No newly rebuilt APK has been tested on a physical device during this audit. Those claims must remain separate from the Unity validation results.
