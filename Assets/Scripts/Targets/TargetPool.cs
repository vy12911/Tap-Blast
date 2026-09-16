using System;
using UnityEngine;

namespace TapBlast
{
    /// <summary>Fixed-capacity pool; no Instantiate/Destroy or searches during gameplay.</summary>
    public sealed class TargetPool
    {
        private readonly TargetView[] targets;
        public int ActiveCount { get; private set; }
        public TargetPool(RectTransform parent, TargetView prefab, int capacity, Action<TargetView> tapped, Action<TargetView> expired)
        {
            targets = new TargetView[capacity];
            for (int i = 0; i < capacity; i++)
            {
                targets[i] = UnityEngine.Object.Instantiate(prefab, parent, false);
                targets[i].name = "Normal target " + (i + 1);
                targets[i].Initialize(tapped, expired);
            }
        }
        public bool Spawn(Vector2 position, float size, float lifetime, Color color, bool tutorial, bool reducedMotion)
        {
            for (int i = 0; i < targets.Length; i++) if (!targets[i].Active)
            {
                targets[i].Spawn(position, size, lifetime, color, tutorial, reducedMotion); ActiveCount++; return true;
            }
            return false;
        }
        public bool HasSpace(Vector2 position, float separation)
        {
            for (int i = 0; i < targets.Length; i++) if (targets[i].Active && (targets[i].Position - position).sqrMagnitude < separation * separation) return false;
            return true;
        }
        public void Release(TargetView target) { if (!target.Active) return; target.Release(); ActiveCount--; }
        public void Tick(float delta, bool reducedMotion) { for (int i = 0; i < targets.Length; i++) targets[i].Tick(delta, reducedMotion); }
        public void Clear() { for (int i = 0; i < targets.Length; i++) targets[i].Release(); ActiveCount = 0; }
        public void SetInteractable(bool value) { for (int i = 0; i < targets.Length; i++) targets[i].SetInteractable(value); }
        public void ClampToArena(Rect bounds, float margin) { for (int i = 0; i < targets.Length; i++) if (targets[i].Active) targets[i].MoveInside(bounds, margin); }
        public void Dispose() { for (int i = 0; i < targets.Length; i++) if (targets[i] != null) UnityEngine.Object.Destroy(targets[i].gameObject); }
    }
}
