using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Non-interactive procedural hand. Allocated once and ticked by the HUD.</summary>
    public sealed class TapHandHint
    {
        private readonly RectTransform root, hand, ripple;
        private readonly Image rippleImage;
        private float elapsed;
        private bool requested;
        public bool IsVisible => root.gameObject.activeSelf;
        public TapHandHint(UIArt art, Transform parent)
        {
            root = art.Fixed("Tutorial tap hand", parent, Vector2.zero, Vector2.one * 180);
            var group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            ripple = art.Fixed("Touch ripple", root, Vector2.zero, Vector2.one * 34);
            rippleImage = art.Image(ripple, Color.white, art.Ring);
            hand = art.Fixed("Hand", root, new Vector2(32, -78), new Vector2(75, 100));
            hand.localEulerAngles = new Vector3(0, 0, 18);
            art.Image(art.Fixed("Palm", hand, new Vector2(0, -5), new Vector2(48, 47)), Color.white, art.Panel);
            art.Image(art.Fixed("Index finger", hand, new Vector2(-15, 28), new Vector2(15, 68)), Color.white, art.Panel);
            art.Image(art.Fixed("Middle finger", hand, new Vector2(0, 11), new Vector2(14, 40)), Color.white, art.Panel);
            art.Image(art.Fixed("Ring finger", hand, new Vector2(14, 7), new Vector2(13, 33)), Color.white, art.Panel);
            art.Image(art.Fixed("Little finger", hand, new Vector2(25, 0), new Vector2(12, 26)), Color.white, art.Panel);
            var thumb = art.Fixed("Thumb", hand, new Vector2(-27, -6), new Vector2(16, 38));
            thumb.localEulerAngles = new Vector3(0, 0, 35); art.Image(thumb, Color.white, art.Panel);
            art.Image(art.Fixed("Wrist", hand, new Vector2(3, -33), new Vector2(33, 24)), Color.white, art.Panel);
            Hide();
        }
        public void Show() { requested = true; elapsed = 0; }
        public void Hide() { requested = false; root.gameObject.SetActive(false); }
        public void Tick(float delta, bool playing, bool reducedMotion)
        {
            bool visible = requested && playing;
            if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
            if (!visible) return;
            if (!reducedMotion) elapsed += delta;
            float cycle = elapsed % 1.35f / 1.35f;
            float press = reducedMotion ? 0 : Mathf.Sin(cycle * Mathf.PI) * 13;
            hand.anchoredPosition = new Vector2(32, -78 + press);
            hand.localScale = Vector3.one * (reducedMotion ? 1 : 1 - Mathf.Sin(cycle * Mathf.PI) * .06f);
            rippleImage.enabled = !reducedMotion;
            ripple.localScale = Vector3.one * (1 + cycle * 1.4f);
            rippleImage.color = new Color(1, 1, 1, 1 - cycle);
        }
    }
}
