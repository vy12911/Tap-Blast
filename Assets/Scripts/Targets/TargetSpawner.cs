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
                if (level.DesignVersion == 1)
                {
                    bool reserved = false;
                    for (int slot = 0; slot < level.ClusterSize + 2; slot++)
                        if ((ClusterPosition(slot, bounds) - position).sqrMagnitude < (size * 1.025f + 40) * (size * 1.025f + 40)) { reserved = true; break; }
                    if (reserved) continue;
                }
                float angle = (float)random.NextDouble() * Mathf.PI * 2;
                float speed = level.DesignVersion == 1 ? 0 : 60 + (float)random.NextDouble() * 40 + (level.LevelId - 16) * 10;
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
            if (level.DesignVersion == 1)
            {
                // Connected, separated circles teach spatial chains without changing their artwork.
                Vector2 p = ClusterPosition(emitted, bounds);
                if (!bounds.Contains(p + Vector2.one * margin) || !bounds.Contains(p - Vector2.one * margin) || !pool.HasSpace(p, size * 1.025f + 20)) return false;
                int color = (waveColor + (emitted == 1 ? 1 : emitted == 3 ? 2 : 0)) % Palette.Length;
                if (!pool.Spawn(p, size, level.TargetLifetime, Palette[color], false, reducedMotion, TargetKind.Chain, graph: true)) return false;
                emitted++;
                if (emitted == level.ClusterSize + 2 && level.MovingTargets)
                    for (int i = 0; i < pool.Capacity; i++)
                        if (pool.At(i).Active && pool.At(i).Kind == TargetKind.Chain) pool.At(i).SetVelocity(new Vector2(.8f, .6f) * level.MovementSpeed);
                return true;
            }
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
        private Vector2 ClusterPosition(int slot, Rect bounds)
        {
            float step = level.TargetSize * 1.025f + 24;
            int index = slot - (slot > 1 ? 1 : 0) - (slot > 3 ? 1 : 0);
            int rows = Mathf.CeilToInt(level.ClusterSize / 3f);
            return bounds.center + (slot == 1 || slot == 3
                ? new Vector2((slot == 1 ? -1 : 1) * step * 1.95f, (slot == 1 ? -1 : 1) * step * 1.5f)
                : new Vector2((index % 3 - 1) * step, (index / 3 - (rows - 1) * .5f) * step));
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
