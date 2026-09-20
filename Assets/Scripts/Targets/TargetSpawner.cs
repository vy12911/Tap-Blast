using UnityEngine;

namespace TapBlast
{
    public sealed class TargetSpawner
    {
        private readonly TargetPool pool;
        private readonly RectTransform arena;
        private readonly LevelConfig level;
        private readonly System.Random random = new System.Random();
        private float elapsed, roundElapsed;
        private int colorIndex;
        private int timeTargetsSpawned;
        // Size variants remain normal targets; color is decorative, scoring unchanged.
        private static readonly Color[] Palette = { UIArt.Cyan, new Color32(127, 235, 156, 255), new Color32(133, 166, 255, 255) };
        public TargetSpawner(TargetPool pool, RectTransform arena, LevelConfig level) { this.pool = pool; this.arena = arena; this.level = level; }
        public void Reset() { elapsed = roundElapsed = 0; colorIndex = timeTargetsSpawned = 0; }
        public void SpawnTutorial(bool reducedMotion) { SpawnAt(Vector2.zero, reducedMotion, true); }
        public void Tick(float delta, bool reducedMotion)
        {
            float margin = level.LargestTargetSize * .5f + 27;
            Rect bounds = arena.rect;
            roundElapsed += delta;
            elapsed += delta;
            if (elapsed < level.SpawnIntervalAt(roundElapsed) || pool.ActiveCount >= level.MaxTargets) return;
            elapsed = 0;
            if (bounds.width < margin * 2 || bounds.height < margin * 2) return;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 p = new Vector2(Mathf.Lerp(bounds.xMin + margin, bounds.xMax - margin, (float)random.NextDouble()), Mathf.Lerp(bounds.yMin + margin, bounds.yMax - margin, (float)random.NextDouble()));
                if (!pool.HasSpace(p, level.LargestTargetSize * 1.025f + level.TargetSpacing)) continue;

                SpawnAt(p, reducedMotion, false); return;
            }
            // Skip saturated layouts instead of forcing overlaps or allocating more targets.
        }

        private void SpawnAt(Vector2 position, bool reducedMotion, bool first)
        {
            TargetKind kind = TargetKind.Normal;
            float roll = (float)random.NextDouble();
            // Collection lessons start with their special target and guarantee regular
            // opportunities, so random luck cannot make the primary objective impossible.
            if (first && level.IsBossLevel) kind = TargetKind.Boss;
            else if (level.Objective == ObjectiveKind.Golden && colorIndex % 4 == 0) kind = TargetKind.Golden;
            else if (level.Objective == ObjectiveKind.Time && colorIndex % 4 == 0) kind = TargetKind.Time;
            else if (!first && roll < level.GoldenChance) kind = TargetKind.Golden;
            else if (!first && roll < level.GoldenChance + level.TimeChance) kind = TargetKind.Time;
            if (kind == TargetKind.Time && timeTargetsSpawned >= level.MaxTimeTargets) kind = TargetKind.Normal;
            Vector2 velocity = Vector2.zero;
            if (level.MovingTargets && kind != TargetKind.Boss)
            {
                float angle = (float)(random.NextDouble() * Mathf.PI * 2);
                float speed = 97.5f + (float)random.NextDouble() * 82.5f;
                velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            }
            if (pool.Spawn(position, kind == TargetKind.Boss ? level.BossSize : level.SizeAt(colorIndex),
                level.TargetLifetime, Palette[colorIndex % Palette.Length], first && level.FirstTapTutorial,
                reducedMotion, kind, velocity, kind == TargetKind.Boss ? level.BossHealth : 1))
            { colorIndex++; if (kind == TargetKind.Time) timeTargetsSpawned++; }
        }
    }
}
