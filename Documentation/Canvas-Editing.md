# Editing the Canvas and gameplay feedback

All UI labels use **TextMeshProUGUI** with a saved TMP font/material. Script bindings, including result reveals and pooled feedback labels, use **`TMPro.TMP_Text`**. Always use TMP for new text; do not add legacy `UnityEngine.UI.Text`. Keep prefab/scene references wired and text raycasts disabled. Shared fonts, shaders and default settings are in `Assets/TextMesh Pro/`.

Open `Assets/Scenes/Level01.unity`. **Tap Blast UI** is an **unpacked** copy of the Canvas, edited directly in the scene (the project owner's choice). `Assets/Prefabs/UI/GameCanvas.prefab` is kept as a matching template but no longer updates the scene; change the scene's objects. No startup script regenerates the layout.

## Visual system

A soft, card-based style: lavender-to-white gradient background with two colour glows and a faint line motif (`Tap Blast UI/Background`), white rounded cards with soft lavender shadows (each card's `… shadow` sibling), navy **Poppins** text (ExtraBold titles and numbers, Bold buttons, SemiBold labels, Medium body), blue→violet gradient pills for primary actions, dark navy pills for secondary actions, outlined white rows for settings and levels, and pastel tiles (peach, blue, purple, yellow) with darker bottom lips. Art is in `Assets/Art/UI/Modern` (procedural sprites and flat icons); gradients use the `UIGradient` mesh effect. Colours set from code are tokens in `UIArt`.

**Device safe area / Portrait frame** has exactly two children: **HUD** (gameplay) and **Screens** (full-screen pages and modal cards). Section objects fill their parent; each element pins to the screen edge it belongs to.

| Canvas object | Contents |
|---|---|
| HUD / Header | Round pause button, lavender "LEVEL NN" chip above the bold level name, wallet pill with compact coin amount (pinned to the top) |
| HUD / Stats | White stats card with Score, Level and Timer chips |
| HUD / Objective | Objective card: label, rounded progress track and three star markers |
| HUD / Mechanics | Chain/Mega/Bomb status and the Shield pill (levels 11+) |
| HUD / Arena | Arena card stretching between the top stack and bottom stack; scenery shown softly inside; pooled feedback |
| HUD / Tutorial hint, Combo card | Hint card and combo card (pinned to the bottom) |
| Screens / Level select | Page: back button, eyebrow + title, coins/XP chips, level list filling the rest of the screen |
| Screens / Level briefing | Page: back button, title, hero card, star-rule tiles, reward chips, Let's Play button pinned to the bottom |
| Screens / Results | Page: outcome chip + gradient title; score card whose top is an indigo→violet star stage (`Band`, its own nested Canvas + RectMask2D, holding the turning `Sunburst` and `Star glow`), gradient score and trophy best chip; "This round" card with three coloured stat tiles; rewards card with coin / XP chips and level bar; tinted hint chip; Play again / Next and Level select pinned to the bottom |
| Screens / Countdown, Paused | Dimmed overlay with a centred white card |
| Screens / Wallet information | Darker overlay with a white card: gradient header, coin and XP tiles (exact amounts), player-level card with progress bar, footer note. Opening it mid-round pauses the round without showing the Pause card |

`GuiScreenVisibility` fades the single **HUD** CanvasGroup behind full pages and the Objective, Mechanics, Arena and Tutorial hint groups beneath the Pause and Countdown cards.

## Screen fitting

The Canvas Scaler uses **Scale With Screen Size**, reference **1080 × 1920**, **Expand**: the canvas is always at least 1080 wide and 1920 tall in reference units, so a 20:9 phone gets a taller canvas (for example 1080 × 2400). `ResponsiveCanvas` stretches the Portrait frame over the whole screen (`Fill Screen`) and, by default, ignores the device safe area (`Respect Safe Area` off), so nothing is letterboxed. Headers stay pinned to the top, action bars to the bottom, and the arena and level list stretch to use the extra height. Turn `Respect Safe Area` on later if notched devices need insets.

Preview the Game view at 1080 × 1920, 1080 × 2400 and 1536 × 2048 to check edits on phones and tablets. Target diameters, spacing, movement and feedback distances were increased by 1.5 when migrating from the previous 720 × 1280 reference, preserving their proportions. Level configuration sizes now use 1080-reference Canvas units.

Overlays are inactive in the saved prefab so the gameplay layout can be edited. Temporarily enable the desired panel while editing and disable it when finished. Gameplay controls their visibility. Dynamic labels such as scores, objectives and wallet values are supplied by the game; change their fonts, alignment, colors and sizes in the Inspector. The controller's serialized fields hold the references, so retain those references when replacing objects. Level 2's size-preview diameters follow its level configuration.

Edit target visuals in `Assets/Prefabs/Targets/NormalTarget.prefab` (also available through **Tap Blast > Select Normal Target Prefab**). Its root Image receives circular tap input; its decorative glow, lifetime ring and star Images have raycasts disabled. The pool creates 12 copies before play and reuses them across all 20 levels. Target sizes and spacing remain in the level configuration assets.

The shared sprite assets are in `Assets/Art/UI`. You can assign replacement sprites to the existing Images. **Tap Blast > Select Game Canvas** selects the UI prefab. Safe-area fitting scales the 1080-by-1920 frame to the device; it does not rebuild the hierarchy.

## Feedback now included

See [gameplay animations](Animations-Level01-20.md) for every effect and its verification, and [AGENTS.md section 5](../AGENTS.md) for the rules new levels must follow.

- Targets: spring-in arrival, soft glow, star emblem, lifetime ring and idle pulse.
- Tap: squash-and-burst of the ball with its star face, white flash, ring, golden sparkles, bouncy floating score, HUD punch and pitched sound.
- Combo milestones (5/10/20/30/50): callout words over the arena plus the HUD fever label. These are presentation states using the existing score multipliers, not extra bonuses.
- Chains: per-link flash and pop, CHAIN! / SUPER CHAIN! / MEGA CHAIN! callouts, Mega finale and shake.
- Bombs: swell, layered explosion, shake, explosion sound; Shield block implodes into a cyan hexagon; untouched bombs fizzle out.
- Expiry: MISS text and a grey puff.
- Completion: star stage reveal, stat tiles, reward chips and confetti.
- Pause freezes all feedback, replay and navigation clear it, and reduced motion removes motion while keeping text and state labels.

**Pooled feedback objects:** `HUD/Arena/Pooled feedback` holds 16 `Tap feedback N` popups (each: Burst ring, Tap squash core with a `Face` star child, eight sparks, Points label) bound to `GameHUD.popups`, and the `World 2 chain energy` effects Graphic. Keep the array and these children intact; add more popups by duplicating one and extending the array (see `Tools/LiveEdits/15-pop-animations.cs`).

**Nested canvases (performance):** `Target arena`, `Pooled feedback`, the level list's `Level list content` and the results `Band` each carry their own Canvas, so their constant movement does not re-batch the rest of the UI. Keep a GraphicRaycaster on any nested Canvas that holds buttons.

The supplied GUI artwork now provides the illustrated backgrounds, panels, buttons, icons and target bodies. See [GUI reference implementation](GUI-Reference-Implementation.md) for screen mappings, component bindings and verification. The later mechanics pictured there remain gated by the GDD: Golden at Level 6, Time at 8, Chain at 11, Bomb at 16 and Shield at 17; other power-ups, Perfect and Ultimate belong to later progression. No inactive power-up buttons or fake Ultimate meter are shown in World 1.

## World 1 bindings

Level select is one page with no worlds: all twenty saved level rows sit in the `Level list content` of a saved vertical ScrollRect inside the 1080 × 1920 reference frame. `GameHUD.levelButtons` and `selectionTexts` bind the saved rows; element *i* is Level *i + 1*, rows are 160 reference pixels apart (`GameHUD.levelRowPitch`), and the list opens scrolled to the current level. To add rows, run `Tools/Run-Campaign20.ps1 -Mode ListAuthor` (it clones saved rows in the editor) or duplicate a row in the prefab, then extend both arrays and the content height. No runtime layout groups or button clones are constructed.

The target prefab contains the normal star plus non-raycasting GOLD, +3s and CORE labels. `TargetView` toggles these authored symbols by target kind. Edit their appearance directly in the prefab. The Core's existing ring shows health rather than lifetime. Special target effects and objective-specific hints are updated through the same pooled feedback/HUD bindings.

Player instructions come from `LevelConfig`: `GoalText` (exact goal + time) on the briefing goal line and countdown, `mechanicHint` (one exact "how to" sentence) plus `WinRule` in the briefing description, `Goal` / `mechanicHint` on the hint card, and `OneStarRule` / `TwoStarRule` / `ThreeStarRule` on the star tiles. Edit the sentence in each level asset; do not hard-code instructions in the Canvas. New objectives use the common progress bar for best streak, Golden count, Time count or Core destruction. Levels 2–10 explicitly remain timed so players can keep scoring for mastery after achieving their primary goal.

## World 2 bindings

The saved Canvas includes a Shield button above the arena and a mechanics progress label (`World 2 progress`). The former world page buttons were removed. `introWorld` now shows the level number on the briefing, and `selectionWorld` / `selectionTitle` show the fixed ALL LEVELS / LEVEL SELECT heading. These objects and all TMP labels are authored in the prefab; no runtime buttons, labels or layout objects are constructed.

World 2 counters and Shield share the `HUD/Mechanics` band at y=308, below the objective track and above the arena rim. Keep their separate text bounds and modal-obscured CanvasGroup bindings. The saved `RoundedProgressImage` draws both progress caps without stretching the track or changing its RectTransform; continue updating it through `fillAmount`.

The target prefab retains the legacy `chainSymbol` child but color-chain targets now show the original normal artwork; LINK is hidden. `bombSymbol` (X plus BOMB) remains distinct, with non-raycasting labels. Edit these saved children for art changes. Chain waves use the existing preallocated target pool; chain ripples reuse the existing popup pool with short staggered delays. Reduced motion removes ripple motion without changing chain scoring, cluster movement or Bomb rules.

Long objective, description, countdown and result labels use authored TMP autosizing with a 20-reference-pixel minimum. Do not remove font assignments or saved references. The level-20 briefing checks both 30 best combo and five chains; its results show World 2 completion. Results include chain/Mega/Bomb/Shield statistics.

`Tools/World2Authoring.cs` is a one-time editor utility run in the disposable project by `Tools/Run-Campaign20.ps1 -Mode Author`. It saves prefab assets; it is not a runtime UI builder. The saved prefabs in Assets are authoritative.

`Tools/CanvasLayoutAuthoring.cs` (`-Mode LayoutAuthor`) produced the HUD / Screens hierarchy from the earlier flat Canvas. On an already structured Canvas it only refits section containers to their contents, so it is safe to re-run after moving elements. `Tools/GuiAuthoring.cs` targets the old flat Canvas and refuses to run on the new one.

World 2 now includes a saved **World 2 chain energy** Graphic under Pooled feedback. Tune intensity, link width and burst radius on that component; keep its GameHUD reference wired. See [level-specific effects](Animations-Level01-20.md#world-2-effects-by-level). Its fixed geometry pool draws travelling chain links, readiness halos, level-specific bursts and hazard/Shield indicators without creating UI objects.

### Scrollable level list and image sizing

The twenty saved level buttons live under `Screens/Level select/Level select card/Level list/Level list viewport/Level list content`. The viewport has a RectMask2D and vertical ScrollRect, with a separate fixed scrollbar. Keep the header, wallet, and close button outside this hierarchy. Rows are 900 x 144 reference pixels, with 16-pixel gaps and end padding; title/status boxes have equal 22-pixel outer insets. `GameHUD.selectionScroll` scrolls the list to the current level each time it opens.

Foreground illustrations use Preserve Aspect. Panel backgrounds use nine-slice borders so designers can resize panels without stretching their corners. `StatusCapsule.asset` and `ProgressFill.asset` reference the existing disc texture; their metadata supplies the capsule borders and a proportional strip for the objective fill. Keep the objective fill at 900 x 30 and update `fillAmount`, not anchors.

The gameplay hint title and subtitle share a saved background referenced by `GameHUD.hintPlate`; both text objects must remain later in sibling order than that background inside `HUD/Tutorial hint`. `GuiScreenVisibility.modalObscured` hides the Objective, Mechanics, Arena and Tutorial hint sections under pause/countdown, and restores them on return to play. Arena scenery is a child of the saved rounded mask, independently bound to `GuiArenaTheme`.
