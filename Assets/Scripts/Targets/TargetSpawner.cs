using UnityEngine;

namespace TapBlast
{
    public sealed class TargetSpawner
    {
        private readonly TargetPool pool;
        private readonly RectTransform arena;
        private readonly LevelConfig level;
        private readonly System.Random random = new System.Random();
        private float elapsed;
        private Vector2 previousSize;
        private int colorIndex;
        // Size variants remain normal targets; color is decorative, scoring unchanged.
        private static readonly Color[] Palette = { UIArt.Cyan, new Color32(127, 235, 156, 255), new Color32(133, 166, 255, 255) };
        public TargetSpawner(TargetPool pool, RectTransform arena, LevelConfig level) { this.pool = pool; this.arena = arena; this.level = level; }
        public void Reset() { elapsed = 0; colorIndex = 0; previousSize = arena.rect.size; }
        public void SpawnTutorial(bool reducedMotion) { pool.Spawn(Vector2.zero, level.SizeAt(colorIndex++), level.TargetLifetime, Palette[0], level.FirstTapTutorial, reducedMotion); }
        public void Tick(float delta, bool reducedMotion)
        {
            float margin = level.LargestTargetSize * .5f + 18;
            Rect bounds = arena.rect;
            if (bounds.size != previousSize)
            {
                pool.ClampToArena(bounds, margin); previousSize = bounds.size;
            }
            elapsed += delta;
            if (elapsed < level.SpawnInterval || pool.ActiveCount >= level.MaxTargets) return;
            elapsed = 0;
            if (bounds.width < margin * 2 || bounds.height < margin * 2) return;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 p = new Vector2(Mathf.Lerp(bounds.xMin + margin, bounds.xMax - margin, (float)random.NextDouble()), Mathf.Lerp(bounds.yMin + margin, bounds.yMax - margin, (float)random.NextDouble()));
                if (!pool.HasSpace(p, level.LargestTargetSize * 1.025f + level.TargetSpacing)) continue;

                TargetKind kind = TargetKind.Normal;
                int health = 1;
                float roll = (float)random.NextDouble();
                if (level.IsBossLevel && pool.ActiveCount == 0 && colorIndex == 0)
                {
                    kind = TargetKind.Boss; health = level.BossHealth;
                }
                else if (level.GoldenChance > 0 && roll < level.GoldenChance)
                {
                    kind = TargetKind.Golden;
                }
                else if (level.TimeChance > 0 && roll < level.GoldenChance + level.TimeChance)
                {
                    kind = TargetKind.Time;
                }

                Vector2 velocity = Vector2.zero;
                if (level.MovingTargets && kind != TargetKind.Boss)
                {
                    float angle = (float)(random.NextDouble() * Mathf.PI * 2);
                    float speed = 65f + (float)random.NextDouble() * 55f;
                    velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                }

                pool.Spawn(p, level.SizeAt(colorIndex), level.TargetLifetime, Palette[colorIndex % Palette.Length], false, reducedMotion, kind, velocity, health);
                colorIndex++; return;
            }
            // Skip saturated layouts instead of forcing overlaps or allocating more targets.
        }
    }
}
