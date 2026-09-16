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
        private Action<TargetView> tapped, expired;
        private float age, lifetime;
        private bool tutorial, interactable;
        public bool Active { get; private set; }
        public Vector2 Position => rect.anchoredPosition;
        public Color Tint { get; private set; }
        public void Initialize(Action<TargetView> onTap, Action<TargetView> onExpire)
        { tapped = onTap; expired = onExpire; gameObject.SetActive(false); }
        public void Spawn(Vector2 position, float size, float seconds, Color tint, bool first, bool reducedMotion)
        {
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            rect.localScale = Vector3.one; age = 0; lifetime = seconds; tutorial = first;
            Tint = tint; hitImage.color = new Color(tint.r, tint.g, tint.b, .52f);
            lifetimeRing.color = tint; lifetimeRing.fillAmount = 1;
            glow.color = tint;
            Active = true; interactable = true; gameObject.SetActive(true);
        }
        public void Tick(float delta, bool reducedMotion)
        {
            if (!Active) return;
            age += delta;
            float arrival = Mathf.Clamp01(age / .16f);
            rect.localScale = Vector3.one * (reducedMotion ? 1 : Mathf.Lerp(.82f, 1, arrival) + Mathf.Sin(age * 3.5f) * .025f);
            if (tutorial) return;
            lifetimeRing.fillAmount = Mathf.Clamp01(1 - age / lifetime);
            if (age >= lifetime) expired(this);
        }
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
