# Tap Blast — GDD v5 campaign, levels 1–20

The first twenty levels now include the v5 encounters, lane warnings/transits, conduit routing, ordered Priority finales, Sentinel and Architect boss phases, protected exposed cores and two-second boss defeat sequences. See the [v5 implementation and validation guide](Documentation/GDD-v5-Level01-20.md). The earlier [audit](Documentation/GDD-v5-Level01-20-Audit.md) records the state before this migration.

Unity 6000.3.17f1 · URP/uGUI · portrait 1080 × 1920 reference · hard 60 fps cap.

The active design is [Step-by-Step Development GDD v5.0](Assets/Design%20Doc/Tap_Blast_v5_Full_Step_by_Step_Development_GDD.pdf). The [v3 guide](Documentation/GDD-v3-Level01-20.md) documents the retained scoring, pressure and progression baseline; the v5 guide supersedes its timing/encounter/boss details.

Each normal level has three waves over 45 seconds. Bosses 10 and 20 have five phases over 60 seconds, ending in an eight-second direct Priority check. Passing requires every gate and a safe pressure level. Score cannot bypass a failed skill gate.

The campaign includes Small and moving circles, Golden and Time rewards, Priority ! targets, graph-based breadth-first chains and Mega blasts, numbered Linked Pairs, Bomb avoidance, Shield interception, direct accuracy, capped Ultimate charge and safe clearing, pressure, explicit failure causes, recovery tips and optional Practice. Practice grants no stars, records or rewards. Later-world mechanics are not introduced before their v3 recipes.

- 1 star: all mandatory gates pass.
- 2 stars: pass plus either 85% direct accuracy or the authored secondary score.
- 3 stars: all gates, zero unprotected Bomb hits, at most three critical misses, and no Practice/continue.
- Pressure: four in World 1, three in World 2; Priority expiry and unprotected Bombs add pressure. Ordinary misses do not.
- Pairs: tap 1 then 2 within 2.5 seconds. Wrong order resets the pair without World 2 pressure.
- Chains: 3/5/8/12 linked matching targets, once-only breadth-first resolution, Bomb-blocked propagation. Only the direct root increments direct combo/accuracy. Mega waves explicitly allow eight active targets; other waves cap at four.
- Ultimate: data-driven charge capped at 100; required/direct/ordered gates cannot be cleared or credited. Eligible optional targets award score only.
- Time bonuses: at most +6 seconds per round.

Coins, XP, best score/stars/combo, attempt/failure counts and settings persist locally. A level-bound round token prevents duplicate rewards. Local telemetry is buffered in a fixed array during play and flushed to `campaign-v3-events.jsonl` on completion.

The UI remains the owner's unpacked **Tap Blast UI** in `Assets/Scenes/Level01.unity`, with a matching saved `Assets/Prefabs/UI/GameCanvas.prefab`. Use TMP/Poppins labels. Author both through the live editor; do not build UI at runtime. See [Canvas editing](Documentation/Canvas-Editing.md) and [animations](Documentation/Animations-Level01-20.md).

To re-author the v5 levels and saved UI, recompile then evaluate `Tools/LiveEdits/21-campaign-v5.cs` through Unity Pipeline. Its idempotent implementation is `Assets/Editor/CampaignV5Authoring.cs`. The v3 author is a baseline helper; do not invoke it alone to replace v5 content.

Validation (run Unity modes one at a time and wait for each final report/process):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Validate-Level1.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Smoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode GuiSmoke
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-Campaign20.ps1 -Mode Persistence
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Run-UnitySmoke.ps1 -Method RunWorld1
```

Reports and captures: `Temp/Campaign20Validation/`, `Temp/Level1Validation/`; logs: `Logs/Validation/`. Runtime/editor compilation and pure tests do not prove device performance or visual feel. Physical Android/iOS QA and human playtests remain part of production sign-off.
