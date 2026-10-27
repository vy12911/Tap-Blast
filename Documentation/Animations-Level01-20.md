# Gameplay animations

Reference: supplied Tap Blast GDD v1.0, animation table on pages 13–14, target/tutorial requirements on pages 2 and 8, and the detailed early-level progression on pages 12–13.

**Rule for every level, including new ones:** feedback is chosen by target kind and event, never by level number, so a new level gets everything below automatically. The full rule set (required feedback per event, restraint limits, how to add an effect) is in [AGENTS.md section 5](../AGENTS.md).

## Core feedback (all levels)

| Event | What the player sees | Where |
|---|---|---|
| Spawn | Target springs in from 40% with a ~4% overshoot, settled by 0.2 s. | `TargetView.SetReducedMotion` |
| Tap | The ball squashes wide and short with its star face, then swells and bursts; a soft white flash; a lightened ring thins as it expands; gold star sparkles and white streaks fly out on gravity arcs; the score springs in and floats up. ~0.7 s. | `GameHUD.Pop` / `AnimatePopup`, `World2Effects.Burst` |
| Golden | Gold shards, a gold star shower, a gold ring and a larger "+100". | `Burst` (Golden) |
| Time Bubble | Two soft ripples, plus the timer punch. | `Burst` (Time), `TimeJuice` |
| Golden Core | Each hit: gold shards, ring, small shake. Destroyed: arena finale, bigger shake, "CORE DESTROYED!" callout. | `Burst` (Boss), `Finale`, `Shake` |
| Combo milestones 5/10/20/30/50 | Callout word NICE! / GREAT! / AWESOME! / AMAZING! / LEGENDARY! springs in over the arena on a soft dark plate; HUD fever label and milestone sound. | `GameHUD.Callout` |
| Miss (expiry) | "MISS" text and a quiet grey puff. | `MessagePop`, `World2Effects.Puff` |
| Combo HUD | Score, combo, multiplier and fever label punch on hits. | `GameHUD.Juice` |
| Level complete | Star stage with a slowly turning sunburst and gold glow, score count-up, staggered star bounce, stat tiles, reward chips, confetti. | `ShowResults`, `ResultReveal` |

## Chains (any level with chain targets)

Same-colour matching across the board, ordered nearest-first from the tapped target, detonates one target every 160 ms (`TargetView.ChainStepSeconds`). The next target heats from its colour to white and compresses just before it blows, and cannot be tapped or expire. Each detonation gets a white flash and a pooled pop (squash, ring, sparkles, score). The last link brings a callout: CHAIN! (3–4), SUPER CHAIN! (5–7, light shake) or MEGA CHAIN! (8+, gold arena flash, shard fountain, star shower, stronger shake). Score commits once; the visuals cannot grant extra rewards.

Successive chains rotate three reaction styles: Electric → Comet → Split Wave. Electric uses angular lightning and sharp shards; Comet follows a curved sparkling path and unwinds into twin spirals; Split Wave sends two ribbons around the path and bursts into four diagonal rays. Comet and Split Wave suppress the popup ring and sparks so the silhouettes stay distinct. Replay resets the rotation.

## Bombs and Shield (any level with Bombs)

- **Warning:** a smooth 1.5 Hz red glow pulse and four amber brackets; the X/BOMB label stays steady and the tap radius never changes.
- **Tapped:** the bomb swells white-hot for 70 ms (`World2Effects.BombFuseSeconds`), then explodes in layers: white flash, lumpy fireball (white core, orange body, red rim), crisp shockwave ring, charcoal and glowing debris under gravity, rising flickering embers, drifting smoke, a brief scorch mark and a faint red arena wash. The arena shakes as it blows, a synthesised explosion plays (falling bass thump, rumble, crack), and "BOOM! -N" shakes in.
- **Blocked by Shield:** the bomb implodes, with sparks and a ring rushing inward, into a cyan hexagon bubble; "BLOCKED!", a light shake and the shield sound.
- **Left alone:** when it expires it fizzles out in a small puff of smoke, with no penalty.

## Shake

`GameHUD.Shake(strength, seconds, delay)` offsets the whole arena card (rim, targets, effects) with a decaying wobble and always restores the exact saved position. A stronger request overrides a weaker running one. It is used only for big moments: Bomb 16, Mega Chain 10, Core destroyed 12, 5+ chain 5, Core hit 5, Shield block 6 (reference pixels). `Shake Scale` on GameHUD tunes all of them (0 disables).

## World 2 themed flourishes

The saved **Pooled feedback / World 2 chain energy** Graphic (`World2Effects`) draws all arena effects for every level in one procedural mesh. On top of the core feedback above, levels 11–20 add these per-level touches:

| Level | Visual identity |
|---|---|
| 11 Chain Spark | Coloured energy travels between matched targets; hexagonal halos identify ready groups. |
| 12 Double Impact | Every detonation has a delayed second golden shockwave. |
| 13 Mega Burst | Eight matching targets gain orbiting golden glints. |
| 14 Drift Cluster | Moving coloured targets leave three fading contour trails. |
| 15 Combo City | Hits sparkle; crossing a ten-hit milestone launches a skyline of ascending gold energy. |
| 16 Danger Signs | Safe hits produce green hexagonal pulses. |
| 17 Shield School | Activating Shield sends an expanding cyan hexagon across the arena; Bombs gain a cyan outline while it is active. |
| 18 Safe Chain | Green hexagonal ripples accompany each safe chain detonation. |
| 19 Mega Mastery | Mega finishes add a delayed second cyan shockwave. |
| 20 Chain Master | Every chain launches five gold/cyan fireworks from its final target. |

These are optional decoration. New levels do not need one.

## Pause, replay, accessibility and performance

Target cosmetics use the pool tick; HUD and arena effects share one feedback clock. There are no per-target Update methods, coroutines, particle systems or runtime UI objects. Effects use 192 fixed slots; overload replaces the oldest cosmetics only, never target state or scoring. Popups come from 16 authored "Tap feedback" objects. The effect mesh only rebuilds while something is playing (World 2 boards animate every frame). The feedback container has a RectMask2D, so large rings cannot spill onto the HUD.

Pause freezes every effect clock and the shake. Replay and navigation clear queued effects, popups and shake. Reduced motion removes squash, rings, sparks, flashes, explosions, callout motion, shake and confetti immediately, while score text, state labels, Golden/Bomb identities, lifetime/health rings and Shield status stay readable.

Perfect Tap's micro-freeze and Ultimate's full-screen blast belong to later campaign systems.

## Verification

`Tools/Run-Campaign20.ps1 -Mode Smoke` checks ordering, pulses, pause, reduced motion, reset, result sequencing, effect draining, shake restoration, Bomb explosion and Shield block layering, and unchanged object counts. Captures land in `Temp/Campaign20Validation/`: `pop-*` (single tap and callout), `bomb-*` and `bomb-blocked-*` (four stages each), `animation-chain-*`, `reaction-*`, `vfx-11` to `vfx-20`. On-device frame rate and touch feel still need APK testing.
