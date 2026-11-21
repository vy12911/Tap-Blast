using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapBlast
{
    public sealed class TargetView : MonoBehaviour, IPointerDownHandler, ICanvasRaycastFilter
    {
        public const float ChainStepSeconds = .07f;
        public string StableId { get; private set; }
        public bool LaneShift { get; private set; }
        public bool Moving => LaneShift || velocity != Vector2.zero;
        public Vector2 LaneStart { get; private set; }
        public Vector2 LaneEnd { get; private set; }
        private float laneWarning, laneTransit, laneCadence;
        public bool LaneWarning => LaneShift && age % laneCadence < laneWarning;
        public Vector2 LaneDestination => ((int)(age / laneCadence) % 2 == 0) ? LaneEnd : LaneStart;
        public Vector2 LanePosition(float ahead) => Vector2.Lerp(LaneStart, LaneEnd, LaneTiming.Fraction(age + ahead, laneWarning, laneTransit, laneCadence));
        public void ConfigureCue(string id, Vector2 end, float warning, float transit, float cadence, bool lane)
        { StableId = id; LaneShift = lane; LaneStart = Position; LaneEnd = end; laneWarning = warning; laneTransit = transit; laneCadence = Mathf.Max(warning + transit, cadence); }
        public int BlastIndex { get; private set; }
        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform visualRoot;
        private RectTransform VisualRect => visualRoot == null ? rect : visualRoot;
        [SerializeField] private Image lifetimeRing;
        [SerializeField] private Image hitImage;
        [SerializeField] private Sprite[] illustratedBodies;
        [SerializeField] private Image glow;
        [SerializeField] private GameObject normalSymbol, normalOutline, normalHighlight, goldenSymbol, timeSymbol, bossSymbol;
        [SerializeField] private GameObject chainSymbol, bombSymbol;
        private Action<TargetView> tapped, expired, blastResolved;
        [SerializeField] private TMPro.TMP_Text campaignSymbol;
        public bool Required { get; private set; }
        public bool Critical { get; private set; }
        public bool GraphChain { get; private set; }
        public Vector2 ChainFrom { get; private set; }
        public void SetChainParent(Vector2 position) { ChainFrom = position; }
        public int SequenceId { get; private set; }
        public int Order { get; private set; }
        public void SetCampaign(bool required, bool critical, int sequenceId, int order, bool graph)
        {
            Required = required; Critical = critical; SequenceId = sequenceId; Order = order; GraphChain = graph;
            bool special = Kind == TargetKind.Priority || Kind == TargetKind.LinkedPair || Kind == TargetKind.Conduit || Kind == TargetKind.Multiplier;
            if (campaignSymbol != null)
            {
                campaignSymbol.gameObject.SetActive(special);
                campaignSymbol.text = Kind == TargetKind.Priority ? (order > 0 ? "!" + order : "!") : Kind == TargetKind.Conduit ? "R" : Kind == TargetKind.Multiplier ? "x2" : order.ToString();
            }
            if (special) { normalSymbol.SetActive(false); normalOutline.SetActive(false); normalHighlight.SetActive(false); }
        }
        public void HoldPairWindow(float seconds) { if (Kind == TargetKind.LinkedPair) lifetime = Mathf.Max(lifetime, age + seconds); }
        public void ShowPairState(bool firstDone, float remaining)
        {
            if (Kind != TargetKind.LinkedPair || campaignSymbol == null) return;
            campaignSymbol.color = Order == 1 && firstDone ? UIArt.Muted : Color.white;
            glow.color = Order == (firstDone ? 2 : 1) ? UIArt.Gold : Tint;
            if (firstDone && Order == 2) lifetimeRing.fillAmount = Mathf.Clamp01(remaining / 2.5f);
        }
        private float blastDelay;
        public bool BlastPending { get; private set; }
        private float age, lifetime, impactAge = 1;
        private bool motionReduced;
        private bool tutorial, interactable;
        private Vector2 velocity;
        public bool Active { get; private set; }
        public Vector2 Position => rect.anchoredPosition;
        public void SetPosition(Vector2 value) { rect.anchoredPosition = value; }
        public float Radius => rect.rect.width * .5125f;
        public float Diameter => rect.rect.width;
        public Sprite Body => hitImage.sprite;
        // Star face of normal and chain targets (null for Golden, Time, Core and Bomb labels).
        public Sprite Symbol => normalSymbol.activeSelf ? normalSymbol.GetComponent<Image>().sprite : null;
        public Vector2 Velocity => velocity;
        public void SetVelocity(Vector2 value) { velocity = value; }
        public Color Tint { get; private set; }
        public TargetKind Kind { get; private set; } = TargetKind.Normal;
        public int Health { get; private set; } = 1;
        public int MaxHealth { get; private set; } = 1;

        public void Initialize(Action<TargetView> onTap, Action<TargetView> onExpire, Action<TargetView> onBlast = null)
        { tapped = onTap; expired = onExpire; blastResolved = onBlast; gameObject.SetActive(false); }

        public void BeginBlast(float delay, bool reducedMotion, int index = 0)
        {
            BlastPending = true; blastDelay = delay; BlastIndex = index;
            if (reducedMotion || delay <= 0) blastResolved(this);
        }

        public void Spawn(Vector2 position, float size, float seconds, Color tint, bool first, bool reducedMotion, TargetKind kind = TargetKind.Normal, Vector2 moveVelocity = default, int health = 1)
        {
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            rect.localScale = Vector3.one; age = 0; lifetime = seconds; tutorial = first;
            VisualRect.localScale = Vector3.one;
            if (illustratedBodies != null && illustratedBodies.Length == 5)
            {
                int body = kind == TargetKind.Golden || kind == TargetKind.Boss ? 3 : kind == TargetKind.Bomb ? 4 : tint.g > tint.b * 1.15f ? 2 : tint.r > .4f ? 1 : 0;
                hitImage.sprite = illustratedBodies[body];
            }
            Required = Critical = GraphChain = false; SequenceId = Order = 0; if (campaignSymbol != null) campaignSymbol.gameObject.SetActive(false);
            LaneShift = false; StableId = "";
            Kind = kind; velocity = moveVelocity; Health = MaxHealth = Mathf.Max(1, health);
            normalSymbol.SetActive(kind == TargetKind.Normal || kind == TargetKind.Chain || kind == TargetKind.Small);
            normalOutline.SetActive(kind == TargetKind.Normal || kind == TargetKind.Chain || kind == TargetKind.Small);
            normalHighlight.SetActive(kind == TargetKind.Normal || kind == TargetKind.Chain || kind == TargetKind.Small);
            goldenSymbol.SetActive(kind == TargetKind.Golden);
            timeSymbol.SetActive(kind == TargetKind.Time);
            bossSymbol.SetActive(kind == TargetKind.Boss);
            chainSymbol.SetActive(false);
            bombSymbol.SetActive(kind == TargetKind.Bomb);
            Tint = kind == TargetKind.Bomb ? new Color32(255, 88, 112, 255) : kind == TargetKind.Golden || kind == TargetKind.Boss ? UIArt.Gold : kind == TargetKind.Time ? new Color32(80, 240, 220, 255) : tint;
            hitImage.color = illustratedBodies != null && illustratedBodies.Length == 5 ? Color.white : new Color(Tint.r, Tint.g, Tint.b, .52f);
            lifetimeRing.color = Tint; lifetimeRing.fillAmount = 1;
            glow.color = Tint;
            BlastPending = false; Active = true; interactable = true; gameObject.SetActive(true);
            impactAge = 1; SetReducedMotion(reducedMotion);
        }

        public void Tick(float delta, bool reducedMotion, bool move = true)
        {
            if (!Active) return;
            if (BlastPending)
            {
                blastDelay -= delta;
                if (reducedMotion || blastDelay <= 0) blastResolved(this);
                else
                {
                    // Only the next link charges: color heats to white, then
                    // compresses into its burst before the following link ignites.
                    float charge = 1 - Mathf.Clamp01(blastDelay / .12f);
                    Color energy = Color.Lerp(Tint, Color.white, charge * .85f);
                    hitImage.color = illustratedBodies != null && illustratedBodies.Length == 5 ? Color.white : new Color(energy.r, energy.g, energy.b, .52f + charge * .48f);
                    lifetimeRing.color = energy; glow.color = energy;
                    glow.rectTransform.localScale = Vector3.one * (1 + charge * .35f);
                    VisualRect.localScale = Vector3.one * (blastDelay < .04f ? Mathf.Lerp(.65f, 1.12f, blastDelay / .04f) : 1 + charge * .12f);
                }
                return; // Committed hits cannot expire, move or receive another tap.
            }
            age += delta;
            impactAge += delta;
            if (move && velocity != Vector2.zero) rect.anchoredPosition += velocity * delta;
            SetReducedMotion(reducedMotion);
            if (tutorial) return;
            lifetimeRing.fillAmount = Kind == TargetKind.Boss ? (float)Health / MaxHealth : Mathf.Clamp01(1 - age / lifetime);
            if (age >= lifetime && Kind != TargetKind.Boss) expired(this);
        }

        // Cosmetics are driven by the pool clock, never a per-target Update.
        // Hazard warning pulses use decorative children; identity stays readable.
        public void SetReducedMotion(bool value)
        {
            motionReduced = value;
            // Spawn: springs up from 40% with a small overshoot (about 4%), settled by 0.2 s.
            float arrival = Mathf.Clamp01(age / .2f), t = arrival - 1;
            float spring = arrival >= 1 ? 1 : Mathf.LerpUnclamped(.4f, 1, 1 + 2.4f * t * t * t + 1.4f * t * t);
            float impact = value ? 0 : Mathf.Sin(Mathf.Clamp01(impactAge / .22f) * Mathf.PI);
            float idle = Kind == TargetKind.Bomb ? 0 : Mathf.Sin(age * 3.5f) * .025f;
            Vector3 scale = Vector3.one * (value ? 1 : spring * (Kind == TargetKind.Boss ? 1 - impact * .08f : 1));
            if (VisualRect.localScale != scale) VisualRect.localScale = scale;
            lifetimeRing.rectTransform.localScale = Vector3.one;
            Vector3 ringRotation = new Vector3(0, 0, !value && Kind == TargetKind.Golden ? age * 65 : 0);
            if (lifetimeRing.rectTransform.localEulerAngles != ringRotation) lifetimeRing.rectTransform.localEulerAngles = ringRotation;
            glow.rectTransform.localScale = Vector3.one * (value ? 1 : 1 + idle);
            lifetimeRing.color = Tint; glow.color = Tint;
            hitImage.color = illustratedBodies != null && illustratedBodies.Length == 5 ? Color.white : new Color(Tint.r, Tint.g, Tint.b, .52f);
            if (value) return;
            if (Kind == TargetKind.Golden)
            {
                float wave = .5f + .5f * Mathf.Sin(age * Mathf.PI * 2);
                lifetimeRing.rectTransform.localScale = Vector3.one * (1 + wave * .1f);
                glow.rectTransform.localScale = Vector3.one * (1 + wave * .16f);
                glow.color = new Color(Tint.r, Tint.g, Tint.b, .65f + wave * .3f);
            }
            else if (Kind == TargetKind.Priority)
            {
                float urgency = Mathf.SmoothStep(0, 1, Mathf.Clamp01((age / lifetime - .45f) / .55f));
                float warning = .5f + .5f * Mathf.Sin(age * Mathf.PI * (2.4f + urgency));
                Color cue = Color.Lerp(UIArt.Gold, UIArt.Warning, urgency * .65f);
                lifetimeRing.color = Color.Lerp(cue, Color.white, warning * .25f);
                glow.color = Color.Lerp(cue, Color.white, warning * .45f);
                glow.rectTransform.localScale = Vector3.one * (1 + warning * (.08f + urgency * .1f));
            }
            else if (Kind == TargetKind.Bomb)
            {
                // Smooth 1.5 Hz warning rather than a flashing label or hitbox.
                float warning = .5f + .5f * Mathf.Sin(age * Mathf.PI * 3);
                glow.rectTransform.localScale = Vector3.one * (1 + warning * .18f);
                glow.color = new Color(Tint.r, Tint.g, Tint.b, .6f + warning * .35f);
                hitImage.color = new Color(Tint.r, Tint.g, Tint.b, .42f + warning * .16f);
            }
            else if (Kind == TargetKind.Boss)
            {
                glow.rectTransform.localScale = Vector3.one * (1 + impact * .18f);
                lifetimeRing.color = Color.Lerp(Tint, Color.white, impact * .8f);
            }
        }

        public bool Damage()
        {
            Health--;
            if (Health <= 0) return true;
            lifetimeRing.fillAmount = (float)Health / MaxHealth;
            impactAge = .04f; SetReducedMotion(motionReduced);
            return false;
        }

        public void Bounce(Rect bounds, float margin)
        {
            Vector2 p = rect.anchoredPosition;
            float mx = Mathf.Min(margin, bounds.width * .5f), my = Mathf.Min(margin, bounds.height * .5f);
            if (p.x <= bounds.xMin + mx) velocity.x = Mathf.Abs(velocity.x);
            else if (p.x >= bounds.xMax - mx) velocity.x = -Mathf.Abs(velocity.x);
            if (p.y <= bounds.yMin + my) velocity.y = Mathf.Abs(velocity.y);
            else if (p.y >= bounds.yMax - my) velocity.y = -Mathf.Abs(velocity.y);
            rect.anchoredPosition = new Vector2(Mathf.Clamp(p.x, bounds.xMin + mx, bounds.xMax - mx), Mathf.Clamp(p.y, bounds.yMin + my, bounds.yMax - my));
        }
        public void KeepInside(Rect bounds) { if (Active) Bounce(bounds, rect.rect.width * .5125f + 27); }

        public void SetInteractable(bool value) { interactable = value; }
        public void Release() { Active = false; BlastPending = false; interactable = false; gameObject.SetActive(false); }
        public void MoveInside(Rect bounds, float margin)
        {
            rect.anchoredPosition = new Vector2(Mathf.Clamp(Position.x, bounds.xMin + margin, bounds.xMax - margin), Mathf.Clamp(Position.y, bounds.yMin + margin, bounds.yMax - margin));
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (Active && !BlastPending && interactable && TapInput.Accept(eventData)) { TapInput.Record(this, eventData); tapped(this); }
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return Active && !BlastPending && interactable && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out var p) && p.sqrMagnitude <= Mathf.Pow(Mathf.Max(rect.rect.width * .5f, 66), 2);
        }
    }
}
