# World 1 objectives and progression

The supplied GDD v1.0 page 12 is the basis for the primary objectives below. This guide replaces the older tap-count substitutes for combo/collection levels and the score-only boss objective. The GDD's early “5x”, “10x” and “15x” goals are interpreted as streak lengths, not scoring multipliers.

| Level | Name | Primary objective | Base time | Two-star score | Three-star combo | Coins | XP |
|---|---|---|---|---|---|---|---|
| 1 | FIRST SPARK | 20 taps | 60s | 240 | 20 | 50 | 25 |
| 2 | SIZE IT UP | 500 points | 45s | 750 | 20 | 75 | 35 |
| 3 | STREAK STARTER | 5-hit best streak | 45s | 600 | 15 | 85 | 40 |
| 4 | PRECISION PUSH | 1,000 points | 45s | 1,250 | 15 | 100 | 45 |
| 5 | ON THE MOVE | 10-hit best streak | 45s | 800 | 15 | 110 | 50 |
| 6 | GOLDEN OPPORTUNITY | 3 Golden targets | 45s | 1,800 | 15 | 125 | 60 |
| 7 | FAST & FURIOUS | 2,000 points | 40s | 2,500 | 20 | 140 | 70 |
| 8 | TICK TOCK | 3 Time Bubbles | 35s | 1,400 | 20 | 150 | 75 |
| 9 | MASTER STREAK | 15-hit best streak | 45s | 1,600 | 25 | 175 | 85 |
| 10 | GOLDEN CORE | Destroy the 5-hit Core | 50s | 3,000 | 25 | 250 | 100 |

## Completion and mastery

- Level 1 finishes immediately on its twentieth tap; the clock starts with its tutorial tap.
- Levels 2–10 start their clock after countdown and finish at timeout. Meeting the objective secures a win while leaving time for mastery. Briefings and HUD communicate this.
- One star: achieve the primary objective. Two: also meet the score threshold. Three: also have zero expired targets and meet the best-combo threshold.
- Misses break the current streak but do not erase a previously reached best streak. Normal taps never count toward special collection goals. Core health damage is not Core destruction.
- Timings, star scores and rewards are tunable implementation choices rather than fixed GDD values. Score-level two-star thresholds are higher than the winning score. The Unity World 1 harness checks that an ideal clean run can reach all three stars with the actual spawn pacing.

## Mechanics

Level 2 cycles 216/150/168px normal targets. Level 4 uses 127.5/142.5px targets worth 15 base points. Level 5 introduces movement, with boundaries enforced every pooled tick. Level 6 starts with a Golden target and guarantees regular Golden opportunities; Golden targets give 100 base points multiplied by combo. Level 7 accelerates from 0.75s to 0.5s spawns over 20 seconds. Level 8 starts with a Time Bubble and guarantees regular opportunities; each adds 3 seconds. At most six Time Bubbles spawn per round (up to 18 extra seconds). Level 9 combines movement and specials with a higher streak goal. Level 10 begins with a 270px, 5-hit Core whose health ring remains visible until destruction.

## Player XP and saves

Successful rounds and successful replays grant the full configured coin/XP reward. Failed or abandoned rounds grant neither. Profile level is `1 + TotalXP / 100`; XP within that level is `TotalXP % 100`. Results identify a level-up when a 100-XP boundary is crossed.

Per-level best scores and stars are stored independently. Finishing a level with at least one star unlocks the next. Rewards and the consumed level-bound round token are saved together before the result animation. Existing saves remain valid; previously earned records are not removed when objectives are updated.

## Content boundary

The detailed GDD campaign introduces Chain at Level 11 and Bombs at Level 16, with later power-up/Perfect/Ultimate progression. These are outside this World 1 change. The general MVP list elsewhere in the PDF is a separate scope recommendation, not the first-ten-level mechanic schedule.