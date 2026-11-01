# GDD v3 — Levels 1–20

Active specification: `Assets/Design Doc/Tap_Blast_FULL_Development_GDD_v3.0.pdf`.

The v3 recipes replace the old score-only campaign. All levels have deterministic, serialized spawn cues and mandatory wave gates. Normal levels last 45 seconds in three waves; bosses last 60 seconds in five phases. All wave gates must pass. Score and Ultimate never substitute for a direct-tap or ordered gate.

## Authored campaign

| Level | GDD name | Mandatory recipe |
|---|---|---|
| 1 | First Spark | 4 / 10 / 6 direct taps |
| 2 | Quick Fingers | 2 / 4 / 2 Small taps |
| 3 | Side Step | 4 warm-up taps / 5-hit streak / 4 finishing taps |
| 4 | Pulse Check | 2 / 8 / 2 Small taps |
| 5 | New Twist | 2 / 6 / 2 moving target taps |
| 6 | Combo Craft | 1 / 1 / 1 Golden collections, with optional score circles |
| 7 | Danger Read | 3 warm-up taps / 15 rush hits / 3 cleanup taps |
| 8 | Beat Shift | Priority demonstrations / Priority test / moving finish; at least 5 direct ! taps and at most 2 Priority misses |
| 9 | Skill Trial | Warm-up / 15-hit streak / direct finishing wave |
| 10 | World Boss | Direct 5 / moving 4 / combo 5 / Priority 3 / final Priority 1 |
| 11 | First Spark | Chain 1 / chain 1 / direct finishing wave |
| 12 | Quick Fingers | One chain in each of three waves |
| 13 | Side Step | Warm-up / one 8-target Mega / direct finish |
| 14 | Pulse Check | One numbered pair in each wave; later pairs move |
| 15 | New Twist | Warm-up / 20-hit direct streak / moving finish |
| 16 | Combo Craft | Three direct-safe waves; zero unprotected Bomb taps |
| 17 | Danger Read | Ordered pair / activate Shield and block a Bomb / ordered pair |
| 18 | Beat Shift | Chain / chain plus pair / chain; zero unprotected Bomb taps |
| 19 | Skill Trial | Ordered pair / Mega / Mega |
| 20 | World Boss | Chain / pair / two chains plus pair / five safe direct hits / final Priority check |

Normal wave durations are 9 / 22.5 / 13.5 seconds. Level 8 follows the more specific sample: 10 / 20 / 15 seconds, 3.0-second teaching Priority lifetimes, then 2.7 seconds. Boss phase durations are 10 / 12 / 15 / 15 / 8 seconds. Surplus authored opportunities let a player recover from an early miss, subject to the actual quota, timer and pressure rules.

## Resolved specification ambiguities

- The level recipes cap World 2 at four active targets but require eight linked targets for Mega. Only scripted Mega waves have an eight-target cap; other waves retain four. Pool capacity is eight, not a license for every wave to spawn eight targets.
- The primary-gate line takes precedence over generic recipe prose such as level 2's “gate uses normal” beside “small 8”.
- The level 8 detailed sample takes precedence over the generic percentages and imposes its two-Priority-miss limit.
- A normal wave retains its allocated time even when its quota is met early. There is no hidden carry-forward or quota reduction. Time rewards explicitly extend the current wave and global time by at most six seconds total.
- World 1 pressure limit is four; World 2 is three. Required Priority expiry and unprotected Bomb taps add pressure. Ordinary misses never add pressure. A wrong World 2 pair resets only that sequence, without pressure.
- Mastery follows the first-20 recipes: all gates, no unprotected Bomb hits, at most three critical misses. Two stars require the gates plus either 85% direct accuracy or the authored secondary score. Practice grants zero stars, records or currency.
- Priority and Linked Pair are the new types required through level 20. Switch, Armored, Perfect, Echo, Relay and the later-world power systems remain outside this milestone; the first 20 do not display fake active controls for them.

## Rules and input

`RoundSession` owns wave quotas, pressure, accuracy, combo, pair timing, Ultimate charge, time caps and pass/fail. It remains pure C#. `CampaignDirector` reserves a complete group before emitting it and retries blocked required cues instead of skipping them. The scripted schedule is replayable; no optional RNG changes mandatory gates. Moving targets reverse before crossing another target's tap area. Existing targets tick before new cues spawn, preserving each new target's full lifetime. Small targets use a 132-reference-pixel diameter.

Chain detection is breadth-first over nearby matching targets. The fixed result array is its once-only visited queue. Bombs block links. Pair nodes and Priority targets are never chain members. Only the directly tapped chain root adds a direct tap and combo increment; other blast-cleared members award score without direct accuracy or skill credit.

Pairs require direct taps 1 then 2 within 2.5 seconds. The second ring shows that remaining window. Wrong order and window expiry reset the pair without undoing other completed gates. Both nodes clear together on success or target expiry. A valid first tap reserves the full 2.5-second window on both nodes; timeout gives a retry hint without pressure. Ultimate never completes a pair.

Ultimate charge is per-level data (defaults: direct 3, Golden 6, chain 10; capped at 100). The activation consumes it once. A 0.18-second cinematic movement slowdown uses the same slowed simulation delta for the global timer, wave timer, target lifetimes and pair window. Reduced motion disables this slowdown. The per-level eligible-type list and mandatory-target exclusions protect all required gates.

Empty arena taps, invalid order and hazard taps enter the direct-accuracy denominator; chain-cleared targets do not. The saved arena input surface receives empty pointer-down events. Required misses are tracked separately.

## Feedback and saved UI

The unpacked `Tap Blast UI` scene root and `GameCanvas.prefab` are both authored by `CampaignV3Authoring.Author`, invoked through `Tools/LiveEdits/20-campaign-v3.cs`. Target symbols are saved in `NormalTarget.prefab`. Runtime code binds or updates them; it never constructs replacement UI.

- Priority: ! identifier, urgency pulse, gold/white collection burst, distinct chime, grey MISS and pressure warning on expiry.
- Pair: saved 1/2 identifiers, first-node dimming, second-node gold highlight and remaining-time ring, traveling gold link, two completion rings and shard bursts, wrong-order sound/message.
- Chains: sequential squash/pop at each member, circular candy shockwaves for all travel styles, colored shards, escalating chain audio, callout. Mega adds a centered group shockwave and finale; Ultra has a distinct callout if an expanded future pool permits 12 targets.
- Ultimate: bright central finale, callout and short restrained shake; protected gates remain visible.
- Pressure: saved Image pips, numeric limit and warning color/sound. Unsupported circle font glyphs are not used.
- Countdown: mechanic-specific teaching text is shown before the active clock starts. Level 1 includes the tap-hand hint.
- Failure: exact gate/pressure cause. A fatal Bomb or Priority miss gets a brief feedback interval before results replace it; scoring stops immediately.
- Results: existing score/star/reward reveal plus direct accuracy, gates, critical misses, pressure, pairs, zero Perfects/Gems for this scope, and next-skill text. After two failures a non-paid tip appears; after three a saved Practice button is offered.
- Color-safe matching cues are on by default, persist as a setting, and use shape geometry as well as color. Bomb X, Priority ! and pair numbers remain explicit identifiers.

## Progress and telemetry

Existing coins, XP and records migrate without clearing them. Attempts/failures are added to the cached save. Round tokens still guard completion and duplicate rewards; Practice consumes/abandons its token without awarding records or currency.

A bounded 1,024-entry event buffer records level attempts, wave starts/passes, taps, misses, chains, power use, mistakes and completion. It also collects frame-time histograms. JSON is written only when a round finishes; no JSON parsing or file writes happen in the target loop. Slow frames temporarily reduce optional showers, extra shards and arena washes; required target cues and gates retain their timings. The local JSONL log is `campaign-v3-events.jsonl` in Unity's persistent-data directory. This is local instrumentation, not an installed analytics backend or measured retention study.

## Validation and handoff

`Validate-Level1.ps1` retains the legacy pure-rule regression checks and adds v3 gate/pressure/pair/Ultimate/accuracy/Practice checks. `Campaign20Smoke` routes authored v3 campaigns to `CampaignV3Smoke`, which plays all levels with real target handlers at 2.5 taps per second and checks schedule feasibility with 20% reaction margin, mastery, rewards, replay, pause, object counts and feedback draining. World 1 runs the same first-ten recipes rather than asserting obsolete v1 objectives.

Final automated results and capture paths are recorded in [the validation handoff](V3-Validation-Handoff.md). Physical-device QA and human difficulty/playtest notes are still required for the GDD's production-ready definition; automated solvability does not establish either.