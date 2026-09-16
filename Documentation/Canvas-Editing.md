# Editing the Canvas and gameplay feedback

Open `Assets/Scenes/Level01.unity`. **Tap Blast UI** is a saved instance of `Assets/Prefabs/UI/GameCanvas.prefab`; expand it in the Hierarchy before entering Play Mode. Double-click the prefab to edit the shared UI in Prefab Mode, or make scene overrides and apply them deliberately. No startup script regenerates its layout.

Inside **Device safe area / Portrait frame**:

| Canvas object | Edit here |
|---|---|
| Score card, Timer card, Level card | HUD typography, graphics and positions |
| Target arena / Neon City backdrop | Layered city illustration, neon accents and readability veil |
| Pooled feedback | Ten reusable bursts, each containing a ring, shrinking star, eight sparks and score label |
| Combo card | Combo number, score multiplier, Fever state and accent line |
| Level briefing | Goal/reward preview, Play button and normal-target size previews |
| Paused | Resume, restart, settings and exit controls |
| Results | Score, stars, rewards, confetti and result navigation |
| Results / Results card / Result actions | Horizontal Layout Group arranging Replay and Next automatically |
| Level select | Level buttons and saved progress |
| Countdown | Countdown card and text |

Overlays are inactive in the saved prefab so the gameplay layout can be edited. Temporarily enable the desired panel while editing and disable it when finished. Gameplay controls their visibility. Dynamic labels such as scores, objectives and wallet values are supplied by the game; change their fonts, alignment, colors and sizes in the Inspector. The controller's serialized fields hold the references, so retain those references when replacing objects. Level 2's size-preview diameters follow its level configuration.

Edit target visuals in `Assets/Prefabs/Targets/NormalTarget.prefab` (also available through **Tap Blast > Select Normal Target Prefab**). Its root Image receives circular tap input; its decorative glow, lifetime ring and star Images have raycasts disabled. The pool creates four copies before play and reuses them across both levels. Target sizes and spacing remain in the level configuration assets.

The shared sprite assets are in `Assets/Art/UI`. You can assign replacement sprites to the existing Images. **Tap Blast > Select Game Canvas** selects the UI prefab. Safe-area fitting scales the 720-by-1280 frame to the device; it does not rebuild the hierarchy.

## Feedback now included

- Normal targets: arrival scale, soft glow, star emblem, lifetime ring and idle pulse.
- Successful tap: shrinking star, expanding ring, eight outward sparks, floating points, score/combo punch and pitched sound.
- Combo thresholds: Great Streak at 5, Combo Fever at 10, Super Fever at 20, Ultra Fever at 30 and Master Combo at 50; milestone sound and supported-device vibration accompany major thresholds. These are presentation states using the existing GDD score multipliers, not extra score bonuses or spawn-rate changes.
- Expiry: MISS popup and cleared Fever state.
- Completion: pooled confetti, then the existing score/star/reward reveal.
- Pause freezes gameplay feedback. Restart and navigation clear it. Reduced motion suppresses bursts, bounce, confetti and target pulsing while retaining score text and state labels.

The supplied image guides the neon/star presentation. This is a restrained Canvas illustration, not a reproduction of the image's detailed painted city. The later mechanics pictured there remain gated by the GDD: Golden at Level 6, Time at 8, Chain at 11, Bomb at 16; power-ups, Perfect and Ultimate belong to later progression. No inactive power-up buttons or fake Ultimate meter are shown in Levels 1–2.
