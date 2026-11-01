using UnityEngine;
namespace TapBlast
{
    // Required queues are authored and replayable. No optional RNG can steal a reserved slot.
    public sealed class CampaignDirector
    {
        private readonly TargetPool pool;
        private readonly RectTransform arena;
        private readonly LevelConfig level;
        private int wave = -1, next, sequence;
        private float blocked;
        public int SpawnedCues { get; private set; }
        public EncounterEvent CurrentEncounter(RoundSession round)
        {
            if (!level.Scripted || round.State != RoundState.Playing) return null;
            var events = level.Waves[round.WaveIndex].encounters;
            if (events != null) foreach (var e in events)
                if (round.WaveElapsed >= e.at && round.WaveElapsed < e.at + e.warning + e.active) return e;
            return null;
        }
        private void TickBoss(RoundSession round, bool reduced)
        {
            if (!level.V5Boss || !round.SkillGateMet || round.WaveCoreHits >= round.Wave.coreHits) return;
            for (int i = 0; i < pool.Capacity; i++) if (pool.At(i).Active && pool.At(i).Kind == TargetKind.Boss) return;
            var phase = level.BossPhases[round.WaveIndex];
            float size = 156;
            Vector2 p = new Vector2(phase.corePosition.x * (arena.rect.width * .5f - size), phase.corePosition.y * (arena.rect.height * .5f - size));
            if (pool.ActiveCount >= level.Waves[round.WaveIndex].maxActive || !pool.HasSpace(p, size + 40)) return;
            pool.Spawn(p, size, 100, UIArt.Gold, false, reduced, TargetKind.Boss, default, 1, true, false, ++sequence, 0, false);
            for (int i = 0; i < pool.Capacity; i++)
                if (pool.At(i).Active && pool.At(i).SequenceId == sequence) pool.At(i).ConfigureCue(phase.phaseId + "-core", p, .45f, .22f, 1.3f, false);
        }
        public CampaignDirector(TargetPool pool, RectTransform arena, LevelConfig level)
        { this.pool = pool; this.arena = arena; this.level = level; }
        public void Reset() { wave = -1; next = sequence = SpawnedCues = 0; blocked = 0; }
        public void Tick(RoundSession round, float delta, bool reduced)
        {
            if (round.State != RoundState.Playing) return;
            if (wave != round.WaveIndex)
            {
                pool.Clear(); wave = round.WaveIndex; next = 0; blocked = 0;
            }
            var profile = level.Waves[wave];
            TickBoss(round, reduced);
            if (next >= profile.schedule.Length || profile.rules.duration - round.WaveElapsed < .6f) return;
            var cue = profile.schedule[next];
            if (cue.kind == TargetKind.LinkedPair && profile.rules.duration - round.WaveElapsed < 1) return;
            if (round.WaveElapsed < cue.at) return;
            // Atomic groups reserve every member before anything is emitted.
            if (pool.ActiveCount + cue.count > profile.maxActive) { blocked += delta; return; }
            Rect bounds = arena.rect;
            float diameter = Mathf.Min(cue.size, bounds.width / 4.6f, bounds.height / 4.6f);
            if (diameter < 105 || bounds.height < diameter * 3) return;
            Vector2 origin = new Vector2(cue.position.x * (bounds.width * .5f - diameter), cue.position.y * (bounds.height * .5f - diameter));
            float spacing = diameter + 26;
            // Position candidates are deterministic; groups are centered, with safe margins on every aspect ratio.
            for (int attempt = 0; attempt < 9; attempt++)
            {
                Vector2 offset = attempt == 0 ? Vector2.zero : new Vector2(((attempt - 1) % 3 - 1) * spacing, ((attempt - 1) / 3 - 1) * spacing);
                bool clear = true;
                for (int i = 0; i < cue.count; i++)
                {
                    Vector2 p = Position(origin + offset, i, cue.count, spacing);
                    float margin = diameter * .52f + 27;
                    if (p.x < bounds.xMin + margin || p.x > bounds.xMax - margin || p.y < bounds.yMin + margin || p.y > bounds.yMax - margin || !pool.HasSpace(p, diameter + 40)) { clear = false; break; }
                }
                if (!clear) continue;
                sequence++;
                Color color = cue.color % 3 == 0 ? UIArt.Cyan : cue.color % 3 == 1 ? new Color32(127,235,156,255) : new Color32(180,150,255,255);
                for (int i = 0; i < cue.count; i++)
                {
                    var p = Position(origin + offset, i, cue.count, spacing);
                    bool lanes = level.DesignVersion >= 5 && cue.moving;
                    Vector2 velocity = cue.moving && !lanes ? new Vector2((sequence + i) % 2 == 0 ? 65 : -65, 25) : Vector2.zero;
                    var kind = cue.kind == TargetKind.Conduit && i > 0 ? TargetKind.Chain : cue.kind;
                    pool.Spawn(p, diameter, cue.life, color, false, reduced, kind, velocity, 1,
                        cue.required, cue.critical, sequence, cue.kind == TargetKind.LinkedPair ? i + 1 : cue.order, true);
                    for (int slot = 0; slot < pool.Capacity; slot++)
                    {
                        var target = pool.At(slot);
                        if (!target.Active || target.SequenceId != sequence || target.StableId != "") continue;
                        float dx = p.x > 0 ? -100 : 100;
                        var end = p + Vector2.right * dx;
                        end.x = Mathf.Clamp(end.x, bounds.xMin + diameter, bounds.xMax - diameter);
                        target.ConfigureCue(cue.id + "-" + i, end, cue.telegraph, cue.transit, cue.cadence, lanes);
                    }
                }
                next++; SpawnedCues++; blocked = 0; return;
            }
            blocked += delta; // Retry the same reservation; never silently skip a required target.
        }
        private static Vector2 Position(Vector2 center, int index, int count, float spacing)
        {
            if (count == 1) return center;
            if (count == 2) return center + new Vector2((index == 0 ? -.6f : .6f) * spacing, 0);
            int columns = count > 4 ? 3 : 2;
            int rows = (count + columns - 1) / columns;
            return center + new Vector2((index % columns - (columns - 1) * .5f) * spacing, (index / columns - (rows - 1) * .5f) * spacing);
        }
    }
}
