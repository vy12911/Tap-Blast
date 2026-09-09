using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TapBlast
{
    public sealed class TargetView : MonoBehaviour, IPointerDownHandler, ICanvasRaycastFilter
    {
        private RectTransform rect;
        private Image lifetimeRing;
        private Image hitImage;
        private Image glow;
        private Action<TargetView> tapped, expired;
        private float age, lifetime;
        private bool tutorial, interactable;
        public bool Active { get; private set; }
        public Vector2 Position => rect.anchoredPosition;
        public Color Tint { get; private set; }
        public void Initialize(UIArt art, Action<TargetView> onTap, Action<TargetView> onExpire)
        {
            rect = (RectTransform)transform; tapped = onTap; expired = onExpire;
            glow = art.Image(art.Rect("Glow", rect, Vector2.zero, Vector2.one, Vector2.one * -35, Vector2.one * 35), Color.white, art.Glow);
            hitImage = art.Image(rect, Color.white, art.Disc); hitImage.raycastTarget = true;
            lifetimeRing = art.Image(art.Rect("Lifetime ring", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), Color.white, art.Ring);
            lifetimeRing.type = Image.Type.Filled; lifetimeRing.fillMethod = Image.FillMethod.Radial360;
            lifetimeRing.fillOrigin = 2; lifetimeRing.fillClockwise = false;
            art.Image(art.Fixed("Highlight", rect, new Vector2(-16, 20), new Vector2(13, 8)), new Color(1, 1, 1, .65f), art.Disc);
            gameObject.SetActive(false);
        }
        public void Spawn(Vector2 position, float size, float seconds, Color tint, bool first, bool reducedMotion)
        {
            rect.anchoredPosition = position; rect.sizeDelta = Vector2.one * size;
            rect.localScale = Vector3.one; age = 0; lifetime = seconds; tutorial = first;
            Tint = tint; hitImage.color = new Color(tint.r, tint.g, tint.b, .24f);
            lifetimeRing.color = tint; lifetimeRing.fillAmount = 1;
            glow.color = tint;
            Active = true; interactable = true; gameObject.SetActive(true);
        }
        public void Tick(float delta, bool reducedMotion)
        {
            if (!Active) return;
            age += delta;
            rect.localScale = Vector3.one * (reducedMotion ? 1 : 1 + Mathf.Sin(age * 3.5f) * .025f);
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
