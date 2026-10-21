using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Authored Canvas graphic. Fixed effect storage, one clock, no runtime UI creation.</summary>
    [DisallowMultipleComponent]
    public sealed class World2Effects : MaskableGraphic
    {
        [SerializeField, Range(.1f, 1)] private float intensity = .8f;
        [SerializeField, Range(2, 12)] private float linkWidth = 5;
        [SerializeField, Range(80, 260)] private float burstRadius = 160;
        private struct Effect
        {
            public Vector2 From, To;
            public Color Tint;
            public float Age, Life, Size;
            public int Kind;
        }
        // Link, ring, star shower, hexagon. Overload replaces oldest cosmetics only.
        private readonly Effect[] effects = new Effect[128];
        private int cursor, level, reactionSequence;
        public int LastReactionStyle { get; private set; }
        private float clock;
        private bool reduced, shield;
        private TargetPool pool;
        public int ActiveEffectCount
        {
            get { int count = 0; for (int i = 0; i < effects.Length; i++) if (effects[i].Life > 0) count++; return count; }
        }
        public void Bind(TargetPool value) { pool = value; raycastTarget = false; }
        public void Configure(int id, bool shieldActive) { level = id; shield = shieldActive; }
        public void Clear()
        {
            for (int i = 0; i < effects.Length; i++) effects[i] = default;
            cursor = 0; clock = 0; shield = false; reactionSequence = 0; LastReactionStyle = 0; SetVerticesDirty();
        }
        public void SetMotion(bool value)
        {
            reduced = value;
            if (value)
            {
                bool activeShield = shield;
                Clear();
                shield = activeShield;
            }
            SetVerticesDirty();
        }
        public void Tick(float delta)
        {
            if (level < 11) return;
            if (!reduced) clock += delta;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].Life <= 0) continue;
                effects[i].Age += delta;
                if (effects[i].Age >= effects[i].Life) effects[i].Life = 0;
            }
            SetVerticesDirty();
        }
        private void Emit(int kind, Vector2 from, Vector2 to, Color tint, float size, float delay = 0, float life = .65f)
        {
            if (reduced || level < 11) return;
            effects[cursor++ % effects.Length] = new Effect { Kind = kind, From = from, To = to,
                Tint = tint, Size = size, Age = -delay, Life = life };
        }
        public int Chain(TargetView[] members, int count)
        {
            // Cycle without repeats, independently of target/scoring order.
            int style = reactionSequence;
            reactionSequence = (reactionSequence + 1) % 3;
            LastReactionStyle = style;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = members[i].Position;
                Color tint = members[i].Tint;
                float delay = i * TargetView.ChainStepSeconds;
                if (i > 0) Emit(4 + style, members[i - 1].Position, p, tint, linkWidth,
                    delay - TargetView.ChainStepSeconds, TargetView.ChainStepSeconds + .23f);
                float size = burstRadius * (count >= 8 ? 1.3f : 1);
                Emit(7 + style, p, p, tint, size, delay, .75f);
                if (style == 0) Emit(2, p, p, tint, size * .7f, delay, .5f);
                if (level == 12) Emit(1, p, p, UIArt.Gold, size * .8f, delay + .18f);
                if (level == 18) Emit(3, p, p, new Color(.4f, 1, .65f), size * .65f, delay);
            }
            Vector2 last = members[count - 1].Position;
            float finish = (count - 1) * TargetView.ChainStepSeconds;
            if (count >= 8)
            {
                Emit(1, last, last, UIArt.Gold, burstRadius * 2.1f, finish, .9f);
                Emit(2, last, last, Color.white, burstRadius * 1.8f, finish, 1);
                if (level == 19) Emit(1, last, last, UIArt.Cyan, burstRadius * 2.4f, finish + .22f, 1);
            }
            if (level == 20)
                for (int i = 0; i < 5; i++)
                {
                    Vector2 p = new Vector2((i - 2) * 130, 160 + (i % 2) * 130);
                    Emit(0, last, p, UIArt.Gold, 6, finish + i * .08f, .45f);
                    Emit(2, p, p, i % 2 == 0 ? UIArt.Gold : UIArt.Cyan, 180, finish + .25f + i * .08f, .9f);
                }
            return style;
        }
        public void Hit(Vector2 position, Color tint, int previousCombo, int combo)
        {
            if (level == 16) Emit(3, position, position, new Color(.4f, 1, .65f), 95);
            if (level != 15 && level != 20) return;
            Emit(2, position, position, tint, 100);
            // A chain can cross a milestone without landing exactly on it.
            if (combo / 10 <= previousCombo / 10) return;
            for (int i = 0; i < 7; i++)
            {
                Vector2 p = new Vector2((i - 3) * 115, rectTransform.rect.yMin + 30);
                Emit(0, p, p + Vector2.up * (180 + i % 3 * 95), UIArt.Gold, 8, i * .05f, .65f);
                Vector2 top = p + Vector2.up * (180 + i % 3 * 95);
                Emit(2, top, top, tint, 115, .3f + i * .05f);
            }
        }
        public void ShieldActivate() { Emit(3, Vector2.zero, Vector2.zero, UIArt.Cyan, 420, 0, .85f); }
        public void Hazard(Vector2 position, bool blocked)
        {
            Color tint = blocked ? UIArt.Cyan : new Color(1, .3f, .2f);
            Emit(blocked ? 3 : 1, position, position, tint, blocked ? 190 : 220);
            Emit(2, position, position, blocked ? Color.white : UIArt.Gold, 180, 0, .8f);
            if (blocked) Emit(3, position, position, tint, 240, .16f, .8f);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (level < 11 || pool == null) return;
            DrawBoard(vh);
            for (int i = 0; i < effects.Length; i++)
            {
                Effect e = effects[i];
                if (e.Life <= 0 || e.Age < 0) continue;
                float t = Mathf.Clamp01(e.Age / e.Life), ease = 1 - (1 - t) * (1 - t);
                Color c = e.Tint; c.a = (1 - t) * intensity;
                if (e.Kind >= 4)
                {
                    DrawReaction(vh, e, t, c);
                }
                else if (e.Kind == 0)
                {
                    float travel = Mathf.Clamp01(e.Age / TargetView.ChainStepSeconds);
                    Vector2 head = Vector2.Lerp(e.From, e.To, travel);
                    Vector2 tail = Vector2.Lerp(e.From, e.To, Mathf.Clamp01(travel - .7f));
                    Color glow = c; glow.a *= .18f;
                    Line(vh, e.From, head, e.Size * 4, glow);
                    Line(vh, tail, head, e.Size, c);
                    Star(vh, head, 13 * (1 - t), Color.white);
                }
                else if (e.Kind == 1 || e.Kind == 3)
                {
                    float radius = Mathf.Lerp(20, e.Size, ease);
                    Ring(vh, e.From, radius, Mathf.Lerp(9, 1, t), c, e.Kind == 3 ? 6 : 40, e.Kind == 3 ? Mathf.PI / 6 : 0);
                    c.a *= .3f;
                    Ring(vh, e.From, radius * .85f, 4, c, e.Kind == 3 ? 6 : 40, 0);
                }
                else
                    for (int j = 0; j < 12; j++)
                    {
                        float angle = j * Mathf.PI * 2 / 12 + i * .71f;
                        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                        Vector2 p = e.From + direction * (18 + ease * e.Size) + Vector2.down * (t * t * 65);
                        Star(vh, p, (j % 3 == 0 ? 13 : 7) * (1 - t), c);
                    }
            }
        }
        private void DrawReaction(VertexHelper vh, Effect e, float t, Color c)
        {
            Color core = Color.Lerp(c, Color.white, .7f); core.a = c.a;
            if (e.Kind <= 6)
            {
                float travel = Mathf.Clamp01(e.Age / TargetView.ChainStepSeconds);
                Vector2 delta = e.To - e.From;
                Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
                if (e.Kind == 4) // Lightning: angular discharge with short side branches.
                {
                    Vector2 previous = e.From;
                    for (int j = 1; j <= 12; j++)
                    {
                        float along = Mathf.Min(j / 12f, travel);
                        Vector2 p = Vector2.Lerp(e.From, e.To, along) + normal * Mathf.Sin(along * Mathf.PI) * (j % 2 == 0 ? -20 : 20);
                        Line(vh, previous, p, e.Size * 3, new Color(c.r, c.g, c.b, c.a * .18f));
                        Line(vh, previous, p, e.Size * .65f, core);
                        if (j % 3 == 0) Line(vh, p, p - delta.normalized * 24 + normal * 28, 2, c);
                        previous = p;
                        if (along >= travel) break;
                    }
                }
                else if (e.Kind == 5) // Comet: curved path, tapered tail and bright head.
                {
                    Vector2 head = Vector2.zero;
                    for (int j = 0; j < 10; j++)
                    {
                        float along = Mathf.Clamp01(travel - j * .035f);
                        Vector2 p = Vector2.Lerp(e.From, e.To, along) + normal * Mathf.Sin(along * Mathf.PI) * 65;
                        Color tail = c; tail.a *= 1 - j / 10f;
                        Star(vh, p, (18 - j * 1.5f) * (1 - t * .5f), j == 0 ? core : tail);
                        if (j == 0) head = p;
                    }
                    Ring(vh, head, 23 * (1 - t), 2, c, 20, 0);
                }
                else // Split wave: two opposing ribbons converge at the next target.
                {
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 previous = e.From;
                        for (int j = 1; j <= 10; j++)
                        {
                            float along = Mathf.Min(j / 10f, travel);
                            Vector2 p = Vector2.Lerp(e.From, e.To, along) + normal * (side * Mathf.Sin(along * Mathf.PI) * 48);
                            Line(vh, previous, p, e.Size, side < 0 ? core : c);
                            previous = p;
                            if (along >= travel) break;
                        }
                    }
                }
                return;
            }
            float ease = 1 - Mathf.Pow(1 - t, 3);
            if (e.Kind == 7) // Sharp lightning shards, not another circular explosion.
            {
                for (int j = 0; j < 8; j++)
                {
                    float a = j * Mathf.PI / 4;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 p = e.From + d * (20 + ease * e.Size);
                    Line(vh, p - d * (35 * (1 - t)), p, 5 * (1 - t), core);
                }
            }
            else if (e.Kind == 8) // Comet impact curls into two expanding spiral arms.
            {
                for (int arm = 0; arm < 2; arm++)
                {
                    Vector2 previous = e.From;
                    for (int j = 1; j <= 24; j++)
                    {
                        float a = j * .24f + t * 4 + arm * Mathf.PI;
                        Vector2 p = e.From + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (j / 24f * e.Size * ease);
                        Line(vh, previous, p, 4 * (1 - t), c);
                        previous = p;
                    }
                    Star(vh, previous, 18 * (1 - t), core);
                }
            }
            else // Split impact sends four broad, tapered rays out in a cross.
            {
                for (int j = 0; j < 4; j++)
                {
                    float a = j * Mathf.PI / 2 + Mathf.PI / 4;
                    Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 p = e.From + d * (ease * e.Size * 1.2f);
                    Line(vh, e.From + d * (ease * e.Size * .4f), p, 16 * (1 - t), c);
                    Star(vh, p, 18 * (1 - t), core);
                }
            }
        }
        private void DrawBoard(VertexHelper vh)
        {
            for (int i = 0; i < pool.Capacity; i++)
            {
                TargetView target = pool.At(i);
                if (!target.Active || target.BlastPending) continue;
                Vector2 p = target.Position;
                float radius = target.Radius + 12;
                if (target.Kind == TargetKind.Bomb)
                {
                    Color c = shield ? UIArt.Cyan : new Color(1, .43f, .22f);
                    c.a = .65f;
                    // Four warning brackets remain static and readable in reduced motion.
                    for (int j = 0; j < 4; j++)
                    {
                        float a = j * Mathf.PI * .5f + Mathf.PI * .25f;
                        Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Vector2 side = new Vector2(-d.y, d.x);
                        Vector2 tip = p + d * radius;
                        Line(vh, tip + d * 10 + side * 12, tip, 4, c);
                        Line(vh, tip, tip + d * 10 - side * 12, 4, c);
                    }
                    if (shield) Ring(vh, p, radius + 12, 3, c, 6, Mathf.PI / 6);
                    continue;
                }
                if (target.Kind != TargetKind.Chain) continue;
                int matches = 0;
                for (int j = 0; j < pool.Capacity; j++)
                {
                    TargetView other = pool.At(j);
                    if (other.Active && !other.BlastPending && other.Kind == TargetKind.Chain && other.Tint == target.Tint) matches++;
                }
                Color tint = target.Tint;
                if (level == 14 && !reduced && target.Velocity.sqrMagnitude > 0)
                    for (int j = 1; j <= 3; j++)
                    {
                        tint.a = .22f / j;
                        Ring(vh, p - target.Velocity.normalized * j * 22, radius * (1 - j * .12f), 3, tint, 24, 0);
                    }
                if (matches < 3) continue;
                tint.a = .5f;
                Ring(vh, p, radius, 2, tint, matches >= 8 ? 8 : 6, reduced ? 0 : clock * .4f);
                if (matches >= 8)
                    for (int j = 0; j < 3; j++)
                    {
                        float a = j * Mathf.PI * 2 / 3 + (reduced ? 0 : clock);
                        Star(vh, p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, 7, UIArt.Gold);
                    }
            }
        }
        private static void Ring(VertexHelper vh, Vector2 center, float radius, float width, Color tint, int segments, float rotation)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments + rotation, b = (i + 1) * Mathf.PI * 2 / segments + rotation;
                Line(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, width, tint);
            }
        }
        private static void Star(VertexHelper vh, Vector2 p, float size, Color tint)
        {
            // Tapered four-point glints, with a translucent colored halo.
            Color halo = tint; halo.a *= .16f;
            Diamond(vh, p, size * 1.6f, halo);
            int start = vh.currentVertCount;
            vh.AddVert(p, tint, Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4;
                float radius = size * (i % 2 == 0 ? 1 : .26f);
                vh.AddVert(p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
            }
            for (int i = 0; i < 8; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 8);
        }
        private static void Diamond(VertexHelper vh, Vector2 p, float size, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(p + Vector2.up * size, tint, Vector2.zero);
            vh.AddVert(p + Vector2.right * size, tint, Vector2.zero);
            vh.AddVert(p - Vector2.up * size, tint, Vector2.zero);
            vh.AddVert(p - Vector2.right * size, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < .001f) return;
            Vector2 n = new Vector2(-d.y, d.x).normalized * width * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - n, tint, Vector2.zero); vh.AddVert(a + n, tint, Vector2.zero);
            vh.AddVert(b + n, tint, Vector2.zero); vh.AddVert(b - n, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
