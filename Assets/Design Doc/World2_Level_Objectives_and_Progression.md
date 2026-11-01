# World 2 — GDD v3

See [GDD v3 levels 1–20](../../Documentation/GDD-v3-Level01-20.md) for the complete wave recipes, feedback and specification decisions.

Levels: 11 two chains; 12 three chains; 13 one Mega; 14 three numbered pairs; 15 direct streak 20; 16 zero-Bomb wave gates; 17 Shield block plus pair gates; 18 three chains plus pair with no Bomb hit; 19 two Megas plus pair; 20 five-phase boss requiring three chains, two pairs, safe direct hits and the final Priority check.

Normal durations are 45 seconds, boss 60. Pressure limit is three. Graph chains resolve breadth-first with a fixed visited queue; Bombs block links and are excluded. Pair and Priority gates require direct input. Ordinary waves cap at four active targets; Mega waves explicitly allow eight to make the eight-linked-target requirement feasible. Root direct hits build combo; blast-cleared members do not count as direct accuracy. Ultimate cannot pass mandatory gates.

Pairs require 1 then 2 within 2.5 seconds; wrong order resets only that pair without World 2 pressure. The second node shows the remaining window. Shield intercepts one Bomb penalty/pressure event. Every level must pass all wave gates, regardless of score. The v1 board-wide matching interpretation and old score-only objectives are historical.