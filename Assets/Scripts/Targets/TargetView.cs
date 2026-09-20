using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapBlast
{
    public sealed class TargetView : MonoBehaviour, IPointerDownHandler, ICanvasRaycastFilter
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private Image lifetimeRing;
        [SerializeField] private Image hitImage;
        [SerializeField] private Image glow;
        [SerializeField] private GameObject normalSymbol, normalOutline, normalHighlight, goldenSymbol, timeSymbol, bossSymbol;
        private Action<TargetView> tapped, expired;
        private float age, lifetime;
        private bool tutorial, interactable;
        private Vector2 velocity;
        public bool Active { get; private set; }
        public Vector2 Position => rect.anchoredPosition;
        public Color Tint { get; private set; }
        public TargetKind Kind { get; private set; } = TargetKind.Normal;
        public int Health { get; private set; } = 1;
        public int MaxHealth { get; private set; } = 1;

        public void Initialize(Action<TargetView> onTap, Action<TargetView> onExpire)
        { tapped = onTap; expired = onExpire; gameObject.SetActive(false); }

        public void Spawn(Vector2 position, float size, float seconds, Color tint, bool first, bool reducedMotion, TargetKind kind = TargetKind.Normal, Vector2 moveVelocity = default, int health = 1)
        {
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            rect.localScale = Vector3.one; age = 0; lifetime = seconds; tutorial = first;
            Kind = kind; velocity = moveVelocity; Health = MaxHealth = Mathf.Max(1, health);
            normalSymbol.SetActive(kind == TargetKind.Normal);
            normalOutline.SetActive(kind == TargetKind.Normal);
            normalHighlight.SetActive(kind == TargetKind.Normal);
            goldenSymbol.SetActive(kind == TargetKind.Golden);
            timeSymbol.SetActive(kind == TargetKind.Time);
            bossSymbol.SetActive(kind == TargetKind.Boss);
            Tint = kind == TargetKind.Golden || kind == TargetKind.Boss ? UIArt.Gold : kind == TargetKind.Time ? new Color32(80, 240, 220, 255) : tint;
            hitImage.color = new Color(Tint.r, Tint.g, Tint.b, .52f);
            lifetimeRing.color = Tint; lifetimeRing.fillAmount = 1;
            glow.color = Tint;
            Active = true; interactable = true; gameObject.SetActive(true);
        }

        public void Tick(float delta, bool reducedMotion)
        {
            if (!Active) return;
            age += delta;
            if (velocity != Vector2.zero) rect.anchoredPosition += velocity * delta;
            float arrival = Mathf.Clamp01(age / .16f);
            rect.localScale = Vector3.one * (reducedMotion ? 1 : Mathf.Lerp(.82f, 1, arrival) + Mathf.Sin(age * 3.5f) * .025f);
            if (tutorial) return;
            lifetimeRing.fillAmount = Kind == TargetKind.Boss ? (float)Health / MaxHealth : Mathf.Clamp01(1 - age / lifetime);
            if (age >= lifetime && Kind != TargetKind.Boss) expired(this);
        }

        public bool Damage()
        {
            Health--;
            if (Health <= 0) return true;
            lifetimeRing.fillAmount = (float)Health / MaxHealth;
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
        public void KeepInside(Rect bounds) { if (Active) Bounce(bounds, rect.rect.width * .5125f + 18); }

        public void SetInteractable(bool value) { interactable = value; }
        public void Release() { Active = false; interactable = false; gameObject.SetActive(false); }
        public void MoveInside(Rect bounds, float margin)
        {
            rect.anchoredPosition = new Vector2(Mathf.Clamp(Position.x, bounds.xMin + margin, bounds.xMax - margin), Mathf.Clamp(Position.y, bounds.yMin + margin, bounds.yMax - margin));
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (Active && interactable && eventData.button == PointerEventData.InputButton.Left) tapped(this);
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            return Active && interactable && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out var p) && p.sqrMagnitude <= rect.rect.width * rect.rect.width * .25f;
        }
    }
}
