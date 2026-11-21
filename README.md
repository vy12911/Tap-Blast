# Tap Blast - restored GDD v1 campaign, levels 1-20

The active design is the user's previous [GDD v1.0](Assets/Design%20Doc/Tap_Blast_FULL_GDD_v1.0.pdf), restored on 21 November 2026. Its detailed early-level table (pages 12-13) supersedes the v3/v5 wave recipes. The current scene, UI template, fonts, target sprites and art assets are preserved.

See [restoration requirements and validation](Documentation/GDD-v1-Restoration.md) for the complete level table, shared mechanics, design interpretations and scope. The v3/v5 guides and authors remain historical references; do not run those authors over this campaign.

Unity 6000.3.17f1 · URP/uGUI · portrait 1080 × 1920 reference · hard 60 fps cap.

The campaign uses the original timed objective path, with a first-tap tutorial at level 1, a five-hit Golden Core at level 10, and the combined 30-hit streak / five-chain challenge at level 20. All twenty saved level assets have designVersion 1 and scripted waves disabled.

- Combo multipliers: 1 / 1.25 / 1.5 / 2 / 3 / 4 at 0 / 5 / 10 / 20 / 30 / 50 hits.
- Spatial chains: 3 / 5 / 8 / 12 connected matching targets, sequential blasts and tiered area effects. Bombs, Core and other chain colors are excluded from area clearing.
- Ultimate: successful direct taps and chains fill the existing meter, capped at 100. Automatic clearing grants score without collecting Golden/Time objectives, advancing tap/combo goals or damaging the Core.
- Stars: complete the goal; then reach the authored score; then also meet the streak requirement with zero misses or unprotected Bomb hits.
- Shield protects one Bomb hit and preserves combo. Time Bubbles grant three seconds, with an authored per-round spawn budget.
- Pause, restart, reduced motion, reward-free Practice and level-bound reward receipts remain supported.

The UI remains the owner's unpacked **Tap Blast UI** in `Assets/Scenes/Level01.unity`, with a matching saved `Assets/Prefabs/UI/GameCanvas.prefab`. Use TMP/Poppins labels. Author both through the live editor; do not build UI at runtime. See [Canvas editing](Documentation/Canvas-Editing.md) and [animations](Documentation/Animations-Level01-20.md).

To import and verify the restored campaign in the open editor, recompile and evaluate `Tools/LiveEdits/22-campaign-v1.cs` through Unity Pipeline. It preserves the scene and all UI/art assets.

Validation (run Unity modes one at a time and wait for each final report/process):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode GuiSmoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Persistence
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnitySmoke.ps1 -Method RunWorld1
```

Reports and captures: `Temp/Campaign20Validation/`, `Temp/Level1Validation/`; logs: `Logs/Validation/`. Runtime/editor compilation and pure tests do not prove device performance or visual feel. Physical Android/iOS QA and human playtests remain part of production sign-off.
