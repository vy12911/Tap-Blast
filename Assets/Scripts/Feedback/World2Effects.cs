using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Authored Canvas graphic for arena effects on every level (pop flashes, shards, chain
    /// reactions, hazards). Fixed effect storage, one clock, no runtime UI creation.</summary>
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
            public float Age, Life, Size, Seed;
            public int Kind;
        }
        // 0 link, 1 ring, 2 star shower, 3 hexagon, 4-9 chain reactions, 10 soft flash, 11 candy shards,
        // 12 arena flash, 13 fireball, 14 smoke, 15 scorch, 16 embers, 17 debris, 18 implosion.
        // Overload replaces oldest cosmetics only.
        private readonly Effect[] effects = new Effect[192];
        private int cursor, level, reactionSequence;
        public int LastReactionStyle { get; private set; }
        private float clock;
        private bool reduced, shield, drawn;
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
            if (!reduced) clock += delta;
            bool live = false;
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].Life <= 0) continue;
                effects[i].Age += delta;
                if (effects[i].Age >= effects[i].Life) effects[i].Life = 0; else live = true;
            }
            // World 2 boards animate every frame; otherwise rebuild only while effects play (plus one clearing frame).
            if (live || drawn || level >= 11) SetVerticesDirty();
            drawn = live;
        }
        private void Emit(int kind, Vector2 from, Vector2 to, Color tint, float size, float delay = 0, float life = .65f)
        {
            if (reduced) return;
            effects[cursor % effects.Length] = new Effect { Kind = kind, From = from, To = to,
                Tint = tint, Size = size, Age = -delay, Life = life, Seed = cursor * 2.39996f };
            cursor++;
        }
        /// <summary>Accent for a single target pop: a soft white flash, plus themed extras for special targets.</summary>
        public void Burst(Vector2 position, Color tint, float radius, TargetKind kind)
        {
            Emit(10, position, position, new Color(1, 1, 1, .85f), radius * 1.25f, 0, .24f);
            if (kind == TargetKind.Golden)
            {
                Emit(11, position, position, UIArt.Gold, radius * 1.1f, 0, .7f);
                Emit(2, position, position, new Color(1, .85f, .3f), radius * 1.2f, .05f, .7f);
                Emit(1, position, position, new Color(1, .82f, .3f), radius * 2.3f, 0, .5f);
            }
            else if (kind == TargetKind.Time)
            {
                // Two ripples, like a drop in water: the bonus seconds land softly.
                Emit(1, position, position, tint, radius * 2, 0, .5f);
                Emit(1, position, position, tint, radius * 2.7f, .13f, .55f);
            }
            else if (kind == TargetKind.Boss)
            {
                Emit(11, position, position, UIArt.Gold, radius * .8f, 0, .55f);
                Emit(1, position, position, new Color(1, .82f, .3f), radius * 1.9f, 0, .4f);
            }
        }
        /// <summary>Large finish: arena-wide colour flash, a big shard fountain and a star shower.</summary>
        public void Finale(Vector2 position, Color tint, float delay = 0)
        {
            Emit(12, position, position, new Color(tint.r, tint.g, tint.b, .32f), 0, delay, .5f);
            Emit(11, position, position, tint, 190, delay, .85f);
            Emit(2, position, position, new Color(1, .86f, .35f), 240, delay + .05f, .9f);
            Emit(1, position, position, Color.white, 330, delay, .6f);
        }
        /// <summary>Quiet grey puff where a target expired; misses should read, not celebrate.</summary>
        public void Puff(Vector2 position) { Emit(10, position, position, new Color(.78f, .8f, .92f, .45f), 95, 0, .4f); }
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
                Emit(10, p, p, new Color(1, 1, 1, .8f), members[i].Radius * 1.3f, delay, .24f);
                Emit(7 + style, p, p, tint, size, delay, .75f);
                if (style == 0) Emit(2, p, p, tint, size * .7f, delay, .5f);
                if (level == 12) Emit(1, p, p, UIArt.Gold, size * .8f, delay + .18f);
                if (level == 18) Emit(3, p, p, new Color(.4f, 1, .65f), size * .65f, delay);
            }
            Vector2 last = members[count - 1].Position;
            float finish = (count - 1) * TargetView.ChainStepSeconds;
            if (count >= 8)
            {
                Finale(last, UIArt.Gold, finish);
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
        // The bomb swells for this long (HUD popup) before it blows.
        public const float BombFuseSeconds = .07f;
        public void Hazard(Vector2 position, bool blocked)
        {
            Vector2 p = position;
            if (blocked)
            {
                // Shield: the bomb is sucked into a cyan hex bubble and fizzles out.
                Emit(18, p, p, UIArt.Cyan, 170, 0, .32f);
                Emit(3, p, p, UIArt.Cyan, 190, .18f, .6f);
                Emit(3, p, p, UIArt.Cyan, 260, .28f, .7f);
                Emit(10, p, p, new Color(.7f, 1, 1, .8f), 110, .2f, .25f);
                Emit(14, p, p, new Color(.72f, .8f, .9f), 70, .28f, .9f);
                return;
            }
            // Layered explosion, back to front: scorch, smoke, fireball, debris, embers, shockwave, flash.
            float boom = BombFuseSeconds;
            Emit(15, p, p, new Color(.12f, .06f, .1f, .38f), 120, boom, 1.4f);
            Emit(14, p, p, new Color(.3f, .26f, .34f), 150, boom + .1f, 1.2f);
            Emit(13, p, p, Color.white, 240, boom, .65f);
            Emit(17, p, p, new Color(1, .43f, .16f), 170, boom, .8f);
            Emit(16, p, p, new Color(1, .7f, .25f), 210, boom, 1);
            Emit(1, p, p, Color.white, 330, boom, .35f);
            Emit(10, p, p, new Color(1, .95f, .8f, .95f), 260, boom, .18f);
            Emit(12, p, p, new Color(1, .3f, .15f, .32f), 0, boom, .5f);
        }
        /// <summary>A bomb left alone fizzles out in a small puff (no penalty).</summary>
        public void Fizzle(Vector2 position) { Emit(14, position, position, new Color(.55f, .52f, .62f), 55, 0, .9f); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (pool == null) return;
            if (level >= 11) DrawBoard(vh);
            for (int i = 0; i < effects.Length; i++)
            {
                Effect e = effects[i];
                if (e.Life <= 0 || e.Age < 0) continue;
                float t = Mathf.Clamp01(e.Age / e.Life), ease = 1 - (1 - t) * (1 - t);
                Color c = e.Tint; c.a = (1 - t) * intensity;
                if (e.Kind >= 10)
                {
                    DrawPop(vh, e, t);
                }
                else if (e.Kind >= 4)
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
        private void DrawPop(VertexHelper vh, Effect e, float t)
        {
            float ease = 1 - (1 - t) * (1 - t);
            Color c = e.Tint;
            if (e.Kind == 10) // Soft flash: bright centre fading to a transparent rim.
            {
                c.a *= (1 - t) * (1 - t) * intensity;
                Glow(vh, e.From, e.Size * Mathf.Lerp(.55f, 1, ease), c);
            }
            else if (e.Kind == 12) // Arena flash: a huge, faint wash from the impact point.
            {
                c.a *= (1 - t) * (1 - t);
                Glow(vh, e.From, 1300, c);
            }
            else if (e.Kind >= 13)
            {
                DrawExplosion(vh, e, t);
            }
            else // Candy shards: ten pieces burst out, fall under gravity, spin and shrink.
            {
                Color light = Color.Lerp(c, Color.white, .45f);
                for (int j = 0; j < 10; j++)
                {
                    float angle = e.Seed + j * Mathf.PI * 2 / 10;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    float reach = e.Size * (1.1f + (j * 7 % 10) * .07f);
                    Vector2 p = e.From + direction * (reach * ease) + Vector2.down * (240 * t * t);
                    float size = (j % 3 == 0 ? 15 : 10) * (1 - t * .85f);
                    Color piece = j % 3 == 0 ? Color.white : j % 3 == 1 ? c : light;
                    piece.a = (1 - t * t) * intensity;
                    if (j % 2 == 0) Shard(vh, p, size, e.Seed + j + t * 7, piece);
                    else Star(vh, p, size, piece);
                }
            }
        }
        private void DrawExplosion(VertexHelper vh, Effect e, float t)
        {
            float fast = 1 - (1 - t) * (1 - t) * (1 - t), fade = Mathf.Pow(1 - t, 1.4f) * intensity;
            float fire = Mathf.Pow(1 - t, 1.1f); // Fire ignores the global intensity so it reads on any arena.
            Vector2 o = e.From;
            if (e.Kind == 13) // Fireball: red rim, orange body, white-hot core that cools first.
            {
                Blob(vh, o, e.Size * (.35f + .75f * fast), e.Seed + t * 2, .12f, new Color(1, .3f, .12f, .9f * fire), new Color(1, .2f, .1f, 0));
                Blob(vh, o, e.Size * (.25f + .55f * fast), e.Seed * 1.3f, .15f, new Color(1, .62f, .18f, fire), new Color(1, .5f, .15f, .15f * fire));
                Blob(vh, o, e.Size * (.2f + .3f * fast) * (1 - t * .6f), e.Seed * 2.1f, .08f, new Color(1, .98f, .88f, fire), new Color(1, .85f, .4f, .4f * fire));
            }
            else if (e.Kind == 14) // Smoke: soft lumpy puffs fade in, swell and drift upward.
            {
                float alpha = .34f * (1 - t) * Mathf.Clamp01(t / .15f) * intensity;
                for (int j = 0; j < 6; j++)
                {
                    float a = e.Seed + j * Mathf.PI / 3;
                    Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (e.Size * .5f * fast) + Vector2.up * (70 * t);
                    Color smoke = e.Tint; smoke.a = alpha; Color edge = smoke; edge.a = 0;
                    Blob(vh, p, e.Size * (.28f + .35f * fast), e.Seed + j, .18f, smoke, edge);
                }
            }
            else if (e.Kind == 15) // Scorch: a dark mark that lingers briefly.
            {
                Color mark = e.Tint; mark.a *= (1 - t) * (1 - t); Color edge = mark; edge.a = 0;
                Blob(vh, o, e.Size * Mathf.Lerp(.7f, 1, fast), e.Seed, .2f, mark, edge);
            }
            else if (e.Kind == 16) // Embers: flickering sparks that rise as they die.
            {
                for (int j = 0; j < 14; j++)
                {
                    float a = e.Seed + j * 2.39996f, reach = e.Size * (.45f + (j * 7 % 10) * .06f);
                    Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (reach * fast) + Vector2.up * (90 * t * t);
                    Color ember = Color.Lerp(e.Tint, new Color(1, .95f, .7f), j % 3 == 0 ? .6f : 0);
                    ember.a = (1 - t) * (.6f + .4f * Mathf.Sin(t * 34 + j * 1.7f)) * intensity;
                    Diamond(vh, p, (j % 4 == 0 ? 7 : 5) * (1 - t * .7f), ember);
                }
            }
            else if (e.Kind == 17) // Debris: charcoal chunks and glowing bits thrown out under gravity.
            {
                for (int j = 0; j < 10; j++)
                {
                    float a = e.Seed + j * Mathf.PI / 5, speed = e.Size * (1.1f + (j * 3 % 5) * .2f);
                    Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (speed * fast) + Vector2.down * (420 * t * t);
                    Color chunk = j % 2 == 0 ? new Color(.18f, .14f, .2f) : e.Tint; chunk.a = (1 - t * t) * intensity;
                    Shard(vh, p, (j % 3 == 0 ? 17 : 11) * (1 - t * .5f), e.Seed + j + t * 9, chunk);
                }
            }
            else // Implosion: sparks and a ring rush inward into the shield.
            {
                for (int j = 0; j < 12; j++)
                {
                    float a = e.Seed + j * Mathf.PI / 6;
                    Vector2 p = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (e.Size * (1 - fast));
                    Color spark = e.Tint; spark.a = (.4f + .6f * t) * intensity;
                    Star(vh, p, 10 * (1 - t * .5f), spark);
                }
                Color ring = e.Tint; ring.a = t * intensity;
                Ring(vh, o, e.Size * (1 - fast) + 8, 4, ring, 32, 0);
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
        private static void Glow(VertexHelper vh, Vector2 center, float radius, Color tint)
        {
            const int segments = 28;
            Color rim = tint; rim.a = 0;
            int start = vh.currentVertCount;
            vh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, rim, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % segments);
        }
        // Lumpy soft disc for fire, smoke and scorch: the rim wobbles so clouds never look like circles.
        private static void Blob(VertexHelper vh, Vector2 center, float radius, float seed, float lump, Color inner, Color outer)
        {
            const int segments = 32;
            int start = vh.currentVertCount;
            vh.AddVert(center, inner, Vector2.zero);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments;
                float r = radius * (1 + lump * (Mathf.Sin(a * 5 + seed) * .6f + Mathf.Sin(a * 3 - seed * 1.7f) * .4f));
                vh.AddVert(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, outer, Vector2.zero);
            }
            for (int i = 0; i < segments; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % segments);
        }
        // Rotated candy fragment: a long diamond.
        private static void Shard(VertexHelper vh, Vector2 p, float size, float rotation, Color tint)
        {
            var along = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * size;
            var across = new Vector2(-along.y, along.x) * .55f;
            int start = vh.currentVertCount;
            vh.AddVert(p + along, tint, Vector2.zero); vh.AddVert(p + across, tint, Vector2.zero);
            vh.AddVert(p - along, tint, Vector2.zero); vh.AddVert(p - across, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
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
