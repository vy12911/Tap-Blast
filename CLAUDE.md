# CLAUDE.md

@AGENTS.md

AGENTS.md is the single source of project rules; keep rules there, not here. The sections that matter most for new work:

- **Section 5 – Gameplay feedback & animation rules:** every level, including new ones, gets the same event-driven feedback (pop, chains, bomb explosion, callouts, shake). Never gate core feedback on a level number.
- **Section 5 – Player instructions:** every level must state its exact goal, one "how to" sentence and its win rule (from `LevelConfig`), so the player always knows exactly what clears it.
- **Section 6 – Adding levels beyond 20:** `ProgressStore.LevelCount`, level rows, configs and tests.
- **Section 7 – UI, visual and performance rules.**

Working with this owner:

- Unity is usually open. Edit it live through the Unity CLI (`script recompile`, then `script eval_file`) instead of asking them to close it. Keep each edit as a numbered, re-runnable script in `Tools/LiveEdits/` and apply it to both the unpacked scene UI and `GameCanvas.prefab`.
- Keep the scene's **Tap Blast UI** unpacked.
- Do not commit or push unless asked.
- Run the verification in AGENTS.md section 8 before reporting work as done.
