using System;
using UnityEngine;

namespace TapBlast
{
    /// <summary>Fixed-capacity pool; no Instantiate/Destroy or searches during gameplay.</summary>
    public sealed class TargetPool
    {
        private readonly TargetView[] targets;
        private readonly RectTransform arena;


        private readonly Action<TargetView> blastImpact;
        public int ActiveCount { get; private set; }
        public int BombCount
        {
            get { int count = 0; for (int i = 0; i < targets.Length; i++) if (targets[i].Active && targets[i].Kind == TargetKind.Bomb) count++; return count; }
        }
        public int FreeCount => targets.Length - ActiveCount;
        public TargetPool(RectTransform parent, TargetView prefab, int capacity, Action<TargetView> tapped, Action<TargetView> expired, Action<TargetView> onBlastImpact = null)
        {
            blastImpact = onBlastImpact;
            arena = parent;
            targets = new TargetView[capacity];

            for (int i = 0; i < capacity; i++)
            {
                targets[i] = UnityEngine.Object.Instantiate(prefab, parent, false);
                targets[i].name = "Normal target " + (i + 1);
                targets[i].Initialize(tapped, expired, ResolveBlast);
            }
        }
        public bool Spawn(Vector2 position, float size, float lifetime, Color color, bool tutorial, bool reducedMotion, TargetKind kind = TargetKind.Normal, Vector2 moveVelocity = default, int health = 1)
        {
            for (int i = 0; i < targets.Length; i++) if (!targets[i].Active)
            {
                targets[i].Spawn(position, size, lifetime, color, tutorial, reducedMotion, kind, moveVelocity, health); ActiveCount++; return true;
            }
            return false;
        }
        public bool HasSpace(Vector2 position, float separation)
        {
            for (int i = 0; i < targets.Length; i++) if (targets[i].Active && (targets[i].Position - position).sqrMagnitude < separation * separation) return false;
            return true;
        }
        public void Release(TargetView target) { if (!target.Active) return; target.Release(); ActiveCount--; }
        private void ResolveBlast(TargetView target) { blastImpact?.Invoke(target); Release(target); }
        public void Tick(float delta, bool reducedMotion)
        {
            Rect bounds = arena.rect;
            // Small movement steps prevent hazards crossing valid targets, even
            // during a frame hitch. Lifetimes still advance by the full delta.
            float movement = Mathf.Clamp(delta, 0, .25f);
            int steps = Mathf.Max(1, Mathf.CeilToInt(movement / (1f / 60)));
            float step = movement / steps;
            for (int n = 0; n < steps; n++)
                for (int i = 0; i < targets.Length; i++)
                {
                    var target = targets[i];
                    if (!target.Active || target.BlastPending || target.Velocity == Vector2.zero) continue;
                    target.KeepInside(bounds);
                    Vector2 next = target.Position + target.Velocity * step;
                    float margin = target.Radius + 27;
                    next = new Vector2(Mathf.Clamp(next.x, bounds.xMin + margin, bounds.xMax - margin),
                        Mathf.Clamp(next.y, bounds.yMin + margin, bounds.yMax - margin));
                    if (ClearHazardPath(target, next)) target.SetPosition(next);
                    else target.SetVelocity(-target.Velocity);
                    target.KeepInside(bounds);
                }
            for (int i = 0; i < targets.Length; i++) targets[i].Tick(delta, reducedMotion, false);
        }
        private bool ClearHazardPath(TargetView target, Vector2 next)
        {
            Vector2 path = next - target.Position;
            for (int i = 0; i < targets.Length; i++)
            {
                var other = targets[i];
                if (other == target || !other.Active || (target.Kind != TargetKind.Bomb && other.Kind != TargetKind.Bomb)) continue;
                float along = path.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(other.Position - target.Position, path) / path.sqrMagnitude) : 0;
                Vector2 nearest = target.Position + path * along;
                float gap = target.Radius + other.Radius + 20;
                if ((other.Position - nearest).sqrMagnitude < gap * gap) return false;
            }
            return true;
        }
        // Matching uses the original spawn color, never the animated flash color.
        // The full board participates; distance only orders the visual cascade.
        public int FindChain(TargetView origin, TargetView[] result)
        {
            if (!origin.Active || origin.BlastPending || origin.Kind != TargetKind.Chain) return 0;
            int count = 1;
            if (result.Length == 0) return 0;
            result[0] = origin;
            for (int i = 0; i < targets.Length; i++)
            {
                var candidate = targets[i];
                if (candidate == origin || !candidate.Active || candidate.BlastPending ||
                    candidate.Kind != TargetKind.Chain || candidate.Tint != origin.Tint) continue;
                if (count == result.Length) return 0;
                result[count++] = candidate;
            }
            if (count < 3) return 0;
            for (int i = 1; i < count; i++)
            {
                int nearest = i;
                for (int j = i + 1; j < count; j++)
                    if ((result[j].Position - result[i - 1].Position).sqrMagnitude <
                        (result[nearest].Position - result[i - 1].Position).sqrMagnitude) nearest = j;
                var swap = result[i]; result[i] = result[nearest]; result[nearest] = swap;
            }
            return count;
        }
        public void Clear() { for (int i = 0; i < targets.Length; i++) targets[i].Release(); ActiveCount = 0; }
        public void SetInteractable(bool value) { for (int i = 0; i < targets.Length; i++) targets[i].SetInteractable(value); }
        public void SetReducedMotion(bool value) { for (int i = 0; i < targets.Length; i++) if (targets[i].Active) targets[i].SetReducedMotion(value); }
        public void ClampToArena(Rect bounds, float margin) { for (int i = 0; i < targets.Length; i++) if (targets[i].Active) targets[i].Bounce(bounds, margin); }
        public void Dispose() { for (int i = 0; i < targets.Length; i++) if (targets[i] != null) UnityEngine.Object.Destroy(targets[i].gameObject); }
    }
}
