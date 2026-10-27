# Illustrated GUI implementation

The saved UI lives in `Assets/Prefabs/UI/GameCanvas.prefab`; the build scene is `Assets/Scenes/Level01.unity`. All panels, controls, artwork, TMP labels, and component references are authored assets. Runtime scripts update values, switch visibility, and animate existing objects.

## Reference coverage

Source: `D:/own/tap-blast/tap blast gui/01-Screens` (1800 x 3200). The game uses the proportional 1080 x 1920 portrait frame.

| Reference | Implemented state |
| --- | --- |
| 01 get ready | Countdown modal, numeric orb, live objective |
| 02 paused | Resume, restart, sound, reduced motion, level selection (vibration was removed) |
| 03 waterfall target | Level 8 waterfall arena artwork and live Time Bubble gameplay |
| 04 world ready | World label, score/level/timer HUD, target objective and progress |
| 05 first tap | Illustrated target and animated tutorial hand; clock waits for first tap |
| 06 target missed | Gameplay message plate and existing miss/combo reset feedback |
| 07 first spark briefing | Level title, target preview, objective, three mastery columns, coins/XP, Play, wallet/best |
| 08 four active targets | Live multi-target gameplay; target counts and sizes follow level configuration |
| 09 level selection | Ten reusable rows per world, numbered badges, live titles, locked/completed status, earned stars, world navigation, wallet and close |

Results and wallet details extend the same art style. Wallet + displays real local coins and XP; it does not pretend to purchase currency. Opening it during play pauses the round; closing resumes that round. Closing it from an existing menu leaves that menu intact.

## Assets and editable components

- Supplied art: `Assets/Art/GUI`. Full reference screenshots and panels with baked sample values are not used as live screens.
- Fonts: the user-supplied Roboto Condensed family in `Assets/Fonts/RobotoCondensed`. Static Black, Bold, and SemiBold TMP assets cover headings/numbers, buttons/rows, and instructions respectively. The original OFL license and font README are bundled.
- `GuiBriefing`: saved three-star/reward and separate coin/XP wallet text bindings.
- `GuiLevelRow`: saved number and earned-star bindings.
- `GuiNavigation`: header pause, wallet and close controls.
- `GuiScreenVisibility`: hides gameplay graphics beneath full-screen menus without rebuilding their hierarchy.
- `GuiArenaTheme`: framed city for tutorial/World 2; full-screen waterfall for Level 8 and open valley for other World 1 levels. Arena hit bounds stay unchanged.
- `GuiPanelFade`: 160 ms fade on saved panels; reduced motion displays them immediately.
- Existing pooled target, chain, Bomb, Shield, score and result effects remain in place. HUD punch strength is reduced to 0.08.

Sprite metadata trims transparent export margins and uses sliced blank artwork for scalable panels/buttons. Gameplay text and progress come from LevelConfig/RoundSession/ProgressStore, so reference sample scores, unlocks and balances are intentionally replaced with actual values. No economy or objective rules are changed.

## Verification commands

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode GuiSmoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Persistence
```

Unity runs asynchronously in the disposable `Temp/Campaign20Validation` project with a separate player-save identity. Wait for each Unity process to exit before starting the next mode. Reports and rendered PNGs are written inside that disposable project; editor logs are in `Logs/Validation`.

`GuiSmoke` checks new component bindings and wallet/header-pause/selection-close actions, then renders reference states plus tall-phone and tablet previews. Campaign smoke checks all 20 levels, text fitting, target mechanics, pooling, transitions and reward/persistence behavior, and captures World 2 and remaining gameplay themes.

`Tools/GuiAuthoring.cs` is an editor-only authoring utility used with `-Mode GuiAuthor`. It saves assets in the disposable project; it is not shipped runtime UI generation. Review prefab changes before copying them back. Scene appearance overrides should be patched narrowly; do not replace the entire scene from a test project.

## Verified implementation

- CLI validation: 88 pure C# round-rule checks and runtime/editor compile checks passed.
- GUI validation: 774 checks passed, including rounded progress caps, saved bindings, wallet pause/resume, Back handling, header pause, selection close, reduced motion, and control bounds on portrait phone, tall phone and tablet.
- Full Unity campaign: 1,082 checks passed across levels 1�20, including text fit, input, chains/Mega chains, Bomb spacing/movement, Shield, fixed pooling, results, navigation and rewards.
- Rendered screenshots were reviewed and retained in `Logs/Validation/GUI` (outside the shipped Assets folder).
- The new progress-bar stars preview currently satisfied mastery conditions. The final rating remains governed by RoundSession.

The implementation uses live TMP text and current progression rather than the sample values baked into the reference pictures. It follows the reference composition and supplied artwork; it is not a pixel-diff-certified reproduction of every painted text detail. Target hit sizes remain governed by the level configuration, and the existing safe-area frame adapts the portrait composition on taller or wider devices.
- Fresh-process persistence passed: all 20 records, wallet, XP, unlocks and selected level restored.

## Reference refinement

The second pass measures the source composition at 1080 x 1920: compact briefing capsule, two-color display title, title/hero stars, lighter glass rims, mastery dividers, coin stack, separate wallet figures, and a 600 x 110 play button with its icon on the right. Pause uses a masked scenic background and source button sprites without vertical slicing seams. Level selection uses the larger display banner, colored number badges, status capsules and adjusted row spacing. Existing 160 ms fades and reduced-motion behavior remain.

## Level selection reference correction

The final supplied level-select image is the layout reference for both worlds. Rows use 40-point maximum titles, 28-point status text, larger rounded number badges, and cropped landscape thumbnails behind those badges. Completed status uses the reference wording without the old slash. World navigation uses 230 x 90 panels with selected/available colors; unlock rules remain unchanged.

The wallet has separate live coin and XP/player-level labels, illustrated coin stack and XP icon, a divider, and the footer inside the wallet panel. The footer is explicitly above the wallet background in saved draw order. The close control stays below the main card inside the portrait safe frame.

`-Mode SelectionAuthor` runs the saved layout refinement pass for selection and shared screen spacing without rebuilding gameplay bindings. `GuiSmoke` now captures both world pages at 343 x 728, the reference frame, and tablet size. It checks footer containment and draw order, title/status text fit, thumbnail separation, and world navigation. Screenshot fixtures are applied only to the disposable IsolatedGui identity and restored afterward; the actual game always displays live progression.

## Supplied Roboto Condensed font

Imported the static Black, Bold and SemiBold TTFs from `C:/Users/zod/Downloads/Roboto_Condensed.zip`. `Tools/TypographyAuthoring.cs` saves font and compatible material assignments across both game prefabs and sets the TMP project default to Roboto Condensed Bold. All saved labels are migrated, including inactive panels and feedback/target labels. The GUI smoke checks all instantiated labels, including pooled targets, for the correct family, static atlas and matching material texture.

Existing measured text boxes and maximum role sizes are retained, with actual font weights rather than synthetic bold. Briefing requirement text is 30 reference pixels; its small wallet caption/player-level labels are 28/24 pixels. The new title material has a lighter outline and short shadow suited to Roboto Condensed. The supplied raster logo and XP icon retain their baked artwork lettering.

Use `-Mode TypographyAuthor` for a focused font migration. Both general GUI authoring and focused selection authoring apply the typography pass before saving, so they cannot restore the previous font assignments.

## Scrollable list and full-screen spacing review

The level list is an authored vertical ScrollRect with a RectMask2D viewport and a slim scrollbar. Header, wallet and close control remain fixed. Ten 144-pixel rows use a 160-pixel pitch, 16-pixel list end padding, and matching 22-pixel top/bottom text insets with a 12-pixel gap between title/status boxes. Switching world pages stops scrolling and returns the list to the top. Buttons and all progression bindings remain the original saved objects.

All non-background Images have Preserve Aspect enabled. Illustrated foreground sprites use uniform fitting; resizable panel backgrounds retain their nine-slice corners. Landscape thumbnails crop a proportional image through a saved mask. Status capsules use a separate sprite referencing the existing disc texture, with corner dimensions matched to the displayed height.

The shared HUD now has aligned score/level/timer values, a full-height progress track, a combined hint/subhint plate, and a reordered combo panel. Progress updates the Image fill amount rather than changing layout anchors. Pause button icons fit inside the preserved button artwork. Countdown has a larger title, gold number and decorative stars. Briefing mastery headings share their top baseline and dividers have bottom padding. Modal visibility hides the arena and hints beneath countdown/pause, while fullscreen panels hide the complete HUD. The arena artwork is clipped inside its rounded frame.

GUI validation includes drag/wheel scrolling, page reset, mask/scrollbar bindings, equal text padding, art containment, font/material assignments, non-background aspect flags and modal restoration. The screen set includes a close-up row capture and a scrolled World 2 preview. Live target sizes and random positions continue to follow level rules rather than the painted screenshot examples.

The saved progress component is `RoundedProgressImage`, an Image subclass that draws rounded caps at both ends while retaining the standard fillAmount binding. Empty progress draws no fill; very small values shrink the caps without overflowing the track. World 2 counters and Shield occupy the gap between the track and arena, with separate text bounds. Pause refreshes modal visibility immediately, hiding these gameplay elements along with the arena.

Both selection headers use a 290 x 56 sliced world pill with a 34-pixel label. The title and subtitle have separate bounds with a 10-pixel gap, and the subtitle has 16.5 pixels of bottom inset inside the blue header panel.
