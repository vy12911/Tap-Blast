# Editing the Canvas and gameplay feedback

All UI labels use **TextMeshProUGUI** with a saved TMP font/material. Script bindings, including result reveals and pooled feedback labels, use **`TMPro.TMP_Text`**. Always use TMP for new text; do not add legacy `UnityEngine.UI.Text`. Keep prefab/scene references wired and text raycasts disabled. Shared fonts, shaders and default settings are in `Assets/TextMesh Pro/`.

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
| Level select | Ten authored level buttons and saved progress labels |
| Countdown | Countdown card and text |

## Responsive reference layout

The saved Canvas Scaler uses **Scale With Screen Size**, reference resolution **1080 × 1920**, and **Expand** screen matching. The Portrait frame is authored at 1080 × 1920. Phones and tablets keep this portrait layout proportional and centered within the device safe area; the background fills the remaining screen. Wider tablets show more background beside the frame.

`ResponsiveCanvas`, wired on the Canvas prefab, adjusts only **Device safe area** anchors/offsets and **Portrait frame** scale in Edit Mode and Play Mode. Edit child RectTransforms, Images and TMP components directly in the Canvas. Keep the frame centered and retain the component's two references. Do not generate or rebuild UI from scripts. GameHUD feedback travel distances and confetti dimensions are Inspector fields.

Preview the Game view at 1080 × 1920, 1080 × 2400 and 1536 × 2048 to check edits on phones and tablets. Target diameters, spacing, movement and feedback distances were increased by 1.5 when migrating from the previous 720 × 1280 reference, preserving their proportions. Level configuration sizes now use 1080-reference Canvas units.

Overlays are inactive in the saved prefab so the gameplay layout can be edited. Temporarily enable the desired panel while editing and disable it when finished. Gameplay controls their visibility. Dynamic labels such as scores, objectives and wallet values are supplied by the game; change their fonts, alignment, colors and sizes in the Inspector. The controller's serialized fields hold the references, so retain those references when replacing objects. Level 2's size-preview diameters follow its level configuration.

Edit target visuals in `Assets/Prefabs/Targets/NormalTarget.prefab` (also available through **Tap Blast > Select Normal Target Prefab**). Its root Image receives circular tap input; its decorative glow, lifetime ring and star Images have raycasts disabled. The pool creates 12 copies before play and reuses them across all 20 levels. Target sizes and spacing remain in the level configuration assets.

The shared sprite assets are in `Assets/Art/UI`. You can assign replacement sprites to the existing Images. **Tap Blast > Select Game Canvas** selects the UI prefab. Safe-area fitting scales the 1080-by-1920 frame to the device; it does not rebuild the hierarchy.

## Feedback now included

See [animation coverage through level 20](Animations-Level01-20.md) for the GDD mapping and animation verification details.

- Normal targets: arrival scale, soft glow, star emblem, lifetime ring and idle pulse.
- Successful tap: shrinking star, expanding ring, eight outward sparks, floating points, score/combo punch and pitched sound.
- Combo thresholds: Great Streak at 5, Combo Fever at 10, Super Fever at 20, Ultra Fever at 30 and Master Combo at 50; milestone sound and supported-device vibration accompany major thresholds. These are presentation states using the existing GDD score multipliers, not extra score bonuses or spawn-rate changes.
- Expiry: MISS popup and cleared Fever state.
- Completion: pooled confetti, then the existing score/star/reward reveal.
- Pause freezes gameplay feedback. Restart and navigation clear it. Reduced motion suppresses bursts, bounce, confetti and target pulsing while retaining score text and state labels.

The supplied image guides the neon/star presentation. This is a restrained Canvas illustration, not a reproduction of the image's detailed painted city. The later mechanics pictured there remain gated by the GDD: Golden at Level 6, Time at 8, Chain at 11, Bomb at 16 and Shield at 17; other power-ups, Perfect and Ultimate belong to later progression. No inactive power-up buttons or fake Ultimate meter are shown in World 1.

## World 1 bindings

The level-select card fits all ten buttons and progress rows inside the 1080 × 1920 reference frame. `GameHUD.levelButtons` and `selectionTexts` bind the saved objects; keep their ordering aligned with level IDs. No runtime layout groups, scrolling hierarchy or button clones are constructed.

The target prefab contains the normal star plus non-raycasting GOLD, +3s and CORE labels. `TargetView` toggles these authored symbols by target kind. Edit their appearance directly in the prefab. The Core's existing ring shows health rather than lifetime. Special target effects and objective-specific hints are updated through the same pooled feedback/HUD bindings.

`LevelConfig.mechanicHint` supplies contextual briefing and gameplay instructions. New objectives use the common progress bar for best streak, Golden count, Time count or Core destruction. Levels 2–10 explicitly remain timed so players can keep scoring for mastery after achieving their primary goal.

## World 2 bindings

The saved Canvas includes `Previous world` and `Next world` buttons inside Level select, a Shield button above the arena, and a World 2 progress label. `introWorld`, `selectionWorld` and `selectionTitle` update the visible world headings. These objects and all TMP labels are authored in the prefab; no runtime buttons, labels or layout objects are constructed. World navigation reuses the same ten level rows for each page.

The target prefab retains the legacy `chainSymbol` child but color-chain targets now show the original normal artwork; LINK is hidden. `bombSymbol` (X plus BOMB) remains distinct, with non-raycasting labels. Edit these saved children for art changes. Chain waves use the existing preallocated target pool; chain ripples reuse the existing popup pool with short staggered delays. Reduced motion removes ripple motion without changing chain scoring, cluster movement or Bomb rules.

Long objective, description, countdown and result labels use authored TMP autosizing with a 20-reference-pixel minimum. Do not remove font assignments or saved references. The level-20 briefing checks both 30 best combo and five chains; its results show World 2 completion. Results include chain/Mega/Bomb/Shield statistics.

`Tools/World2Authoring.cs` is a one-time editor utility run in the disposable project by `Tools/Run-Campaign20.ps1 -Mode Author`. It saves prefab assets; it is not a runtime UI builder. The saved prefabs in Assets are authoritative.
