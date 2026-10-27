# Live editor edits

Script-eval snippets applied to the open editor (scene `Level01.unity`, whose UI is unpacked, and the
`GameCanvas.prefab` template) through the Unity Pipeline package. They are not compiled into the
project. Re-apply one with:

```bash
unity command script eval_file --project-path . --file Tools/LiveEdits/<file>.cs
```

1. `01-objective-card.cs` – objective card proportions: rating stars beside the label, slim progress line.
2. `02-arena-pills-scroll.cs` – deep arena backdrops, round pill ends at any height, white popup labels, elastic level list.
3. `03-colour-pass.cs` – header bands on modal cards, tinted settings rows, wallet gradient panel, combo/hint tints.
4. `04-briefing-page.cs` – briefing page: hero card fills free height (target on an indigo stage), larger star-rule tiles and rewards, personal-best pill above the call to action.
5. `05-level-rows-countdown.cs` – pastel level-row tiles matching each badge colour; countdown card with an indigo stage, glowing white bubble and gradient number.
6. `06-results-best-pill.cs` – results page (outcome chip, score card with gradient band, best chip, stats/rewards styling) and the briefing best pill (trophy + 3 stars); binds the new GameHUD chip/star fields.
7. `07-results-balance.cs` – stars centred in the score band, score cluster centred in the lower half.
8. `08-depth-glow.cs` – global depth pass: top-lit gradient + glossy shine + coloured glow on every pastel tile, shine on icon tiles and badges, gold/blue halos, glow under primary buttons, warm gradient best pill.
9. `09-depth-glow-strength.cs` – glow and halo intensity (edit the alpha values here to tune).
10. `10-progress-glow-star-slots-background.cs` – glowing objective bar (halo, shine and head that follow progress via GameHUD.progressFollowers / progressHead), lavender star slots behind every rating star, periwinkle-lilac-rose background with stronger glows.
11. `11-target-labels.cs` – GOLD / +3s / CORE labels auto-size inside the middle 64% x 34% of the target (max 44); smaller BOMB and LINK labels for the wide Poppins font.
12. `12-progress-refine.cs` – calmer objective bar: tighter softer glow, small spark, inset track gradient, amber-to-coral fill, larger stars on deeper slots (fill easing and star pop live in GameHUD.TickProgress).
13. `13-hint-tones-goal-bar.cs` – binds GameHUD hintEdge / hintFill / progressFillGradient / progressGlow. SetHint tints the hint card by message: green for success (goal met, golden, time bubble, chain, shield), rose for a miss or Bomb hit, lavender for neutral tips. The objective bar turns green once the goal is met. The hint detail text is darkened for contrast.
14. `14-level-chip-coin-pill.cs` – header: "LEVEL NN" lavender chip (GameHUD.levelChip) above a one-line bold level name (worldLabel, auto-shrinks with ellipsis). Coin pill is 280 wide with a one-line auto-sizing balance. Amounts use UIArt.Compact: full digits to 99,999, then 100K / 1.23M / 2.14B. The wallet panel still shows the exact amount.
15. `15-pop-animations.cs` – pop animation pool: 16 "Tap feedback" popups (was 10), each with a "Face" star image on the squashing core, single-line labels, and the callout glow sprite bound to GameHUD.calloutGlow. The motion is in GameHUD.Pop / AnimatePopup / Callout, GameHUD.Juice.Shake and World2Effects.Burst / Finale / Puff.
