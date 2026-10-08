# Animation coverage through level 20

Reference: supplied Tap Blast GDD v1.0, animation table on pages 13–14, target/tutorial requirements on pages 2 and 8, and the detailed early-level progression on pages 12–13.

| GDD animation | Levels using it | Implementation |
|---|---|---|
| Tap: scale down, pop, ring burst | 1–20 | A pooled shrinking core leads the expanding ring by 80 ms, with outward sparks and floating score. Scoring and input remain immediate. |
| Combo: HUD punch / number bounce | 1–20 | Score, combo, multiplier and Fever label punch at hits, with stronger emphasis at combo thresholds. |
| Golden: pulse and rotating/radiating ring | 6 onward where Golden targets spawn | The ring expands/contracts over a one-second cycle, accompanied by a glow pulse. Lifetime fill remains independent and accurate. This implements the GDD's radiating-ring option. |
| Bomb: warning pulse with a fair visual cue | 16–20 | A smooth 1.5 Hz red glow/body pulse; the X/BOMB label stays steady and readable. The pulse does not move the Bomb or alter its tap radius. |
| Chain: sequential blast wave | 11–20 | Same-color matching across the board, ordered nearest-first from the tapped target, drives target removal and pooled bursts from the tapped member outward, with 160 ms between members and a distinct rising detonation sound on each impact. The next target heats from its color to white and compresses just before detonation and cannot be tapped or expire. Chain 2 and Mega use larger rings. Score commits once; the visual ripple cannot grant additional rewards. |
| Level complete: confetti and reward reveal | 1–20 | Confetti, score count-up, personal-best reveal, staggered bouncing stars, then reward fade-in. Replay/navigation can interrupt the reveal safely. |

Additional contextual feedback supports the first 20 levels: the level-1 tutorial hand presses with a ripple; targets animate their arrival and lifetime rings; Time Bubble collection punches the timer; surviving Golden Core hits produce a glow impact and health-ring highlight; Shield activation pulses the button, holds a gentle active pulse, and confirms a protected Bomb hit with a short punch.

## Pause, replay and accessibility

Target cosmetics use the existing pool tick. HUD effects use one shared feedback tick. There are no per-target Update methods, coroutines, or new runtime UI objects. Existing target and feedback pools are reused.

The saved feedback container has a RectMask2D so large chain rings cannot spill onto the objective, hints or combo HUD. Chain rings use partial opacity to keep remaining targets readable.

Pause freezes target, chain, Shield and other gameplay animation clocks. Replay and navigation reset transient transforms and queued feedback. Enabling reduced motion while paused immediately resets target pulses and HUD punches; pending chain feedback becomes static score text. Golden/Bomb identities, lifetime/health rings, Shield status and actual gameplay movement remain available. Reduced-motion results show final values immediately without confetti.

Perfect Tap's micro-freeze/precision ring and Ultimate's time-stretch/full-screen blast belong to later campaign systems, so they are not part of the animation coverage for levels 1–20.

## Verification

`Tools/Validate-Level1.ps1` checks runtime/editor compilation and round rules. `Tools/Run-Campaign20.ps1 -Mode Smoke` runs the full campaign plus animation assertions: ordering, pulse changes, health/lifetime accuracy, pause, reduced motion, reset, result sequencing and unchanged object counts.

Frame captures are saved as `animation-*.png` under `Temp/Campaign20Validation/`, including Golden ring phases, Bomb warning, successive chain-wave frames, and result reveal stages. Animation timing is tested in Unity; on-device frame rate, touch feel and final Android presentation still need APK testing.
