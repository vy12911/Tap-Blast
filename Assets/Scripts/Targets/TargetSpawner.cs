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
        private int wave, emitted, waveColor;
        private float waveStarted;
        // World 1 colors are decorative; World 2 uses this same palette for matching.
        private static readonly Color[] Palette = { UIArt.Cyan, new Color32(127, 235, 156, 255), new Color32(133, 166, 255, 255) };
        public TargetSpawner(TargetPool pool, RectTransform arena, LevelConfig level) { this.pool = pool; this.arena = arena; this.level = level; }
        public void Reset() { elapsed = roundElapsed = 0; colorIndex = timeTargetsSpawned = wave = emitted = 0; waveStarted = 0; }
        public void SpawnTutorial(bool reducedMotion)
        {
            if (level.ClusterSize >= 3) StartColorWave(reducedMotion);
            else SpawnAt(Vector2.zero, reducedMotion, true);
        }
        public void Tick(float delta, bool reducedMotion)
        {
            float margin = level.LargestTargetSize * .5f + 27;
            Rect bounds = arena.rect;
            roundElapsed += delta;
            elapsed += delta;
            if (level.ClusterSize >= 3)
            {
                if (emitted < level.ClusterSize + 2)
                {
                    if (elapsed >= .2f && SpawnColorTarget(reducedMotion)) elapsed = 0;
                }
                else if (roundElapsed - waveStarted >= level.ClusterInterval && pool.ActiveCount == pool.BombCount)
                    StartColorWave(reducedMotion);
                return;
            }
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

        private void StartColorWave(bool reducedMotion)
        {
            emitted = 0; waveColor = wave % Palette.Length;
            waveStarted = roundElapsed; elapsed = 0;
            if (pool.BombCount == 0 && level.BombChance > 0 && (wave == 0 || random.NextDouble() < level.BombChance))
                SpawnBomb(reducedMotion);
            SpawnColorTarget(reducedMotion); wave++;
        }

        private void SpawnBomb(bool reducedMotion)
        {
            Rect bounds = arena.rect;
            float size = level.TargetSize, margin = size * .5125f + 30;
            if (bounds.width < margin * 2 || bounds.height < margin * 2) return;
            for (int attempt = 0; attempt < 96; attempt++)
            {
                Vector2 position = new Vector2(Mathf.Lerp(bounds.xMin + margin, bounds.xMax - margin, (float)random.NextDouble()),
                    Mathf.Lerp(bounds.yMin + margin, bounds.yMax - margin, (float)random.NextDouble()));
                if (!pool.HasSpace(position, size * 1.025f + 40)) continue;
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float speed = 60 + (float)random.NextDouble() * 40 + (level.LevelId - 16) * 10;
                pool.Spawn(position, size, level.TargetLifetime, UIArt.Gold, false, reducedMotion,
                    TargetKind.Bomb, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed);
                return;
            }
        }

        private bool SpawnColorTarget(bool reducedMotion)
        {
            if (pool.ActiveCount >= level.MaxTargets) return false;
            Rect bounds = arena.rect;
            float size = level.TargetSize, margin = size * .5125f + 30;
            float bottom = bounds.yMin + margin;
            float top = bounds.yMax - margin;
            if (bounds.width < margin * 2 || top <= bottom) return false;
            for (int attempt = 0; attempt < 96; attempt++)
            {
                Vector2 position = new Vector2(Mathf.Lerp(bounds.xMin + margin, bounds.xMax - margin, (float)random.NextDouble()),
                    Mathf.Lerp(bottom, top, (float)random.NextDouble()));
                if (!pool.HasSpace(position, size * 1.025f + 20)) continue;
                // Two other colors teach the choice; the main color offers the
                // authored 3/5/8 opportunity without relying on lucky randomness.
                int color = (waveColor + (emitted == 1 ? 1 : emitted == 3 ? 2 : 0)) % Palette.Length;
                Vector2 velocity = level.MovingTargets ? new Vector2(.8f, .6f) * level.MovementSpeed : Vector2.zero;
                if (!pool.Spawn(position, size, level.TargetLifetime, Palette[color], false, reducedMotion, TargetKind.Chain, velocity)) return false;
                emitted++; return true;
            }
            return false;
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
