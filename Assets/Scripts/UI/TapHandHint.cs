using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Animates the authored, non-interactive Canvas hand.</summary>
    [System.Serializable] public sealed class TapHandHint
    {
        [SerializeField] private RectTransform root, hand, ripple;
        [SerializeField] private Image rippleImage;
        [SerializeField, Min(0)] private float pressDistance = 19.5f;
        private float elapsed;
        private bool requested;
        private bool hasRestPosition;
        private Vector2 restPosition;
        public bool IsVisible => root.gameObject.activeSelf;
        public void Show() { if (!hasRestPosition) { restPosition = hand.anchoredPosition; hasRestPosition = true; } requested = true; elapsed = 0; }
        public void Hide() { requested = false; root.gameObject.SetActive(false); if (hasRestPosition) hand.anchoredPosition = restPosition; }
        public void Tick(float delta, bool playing, bool reducedMotion)
        {
            bool visible = requested && playing;
            if (root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
            if (!visible) return;
            if (!reducedMotion) elapsed += delta;
            float cycle = elapsed % 1.35f / 1.35f;
            float press = reducedMotion ? 0 : Mathf.Sin(cycle * Mathf.PI) * pressDistance;
            hand.anchoredPosition = restPosition + Vector2.up * press;
            hand.localScale = Vector3.one * (reducedMotion ? 1 : 1 - Mathf.Sin(cycle * Mathf.PI) * .06f);
            rippleImage.enabled = !reducedMotion;
            ripple.localScale = Vector3.one * (1 + cycle * 1.4f);
            rippleImage.color = new Color(1, 1, 1, 1 - cycle);
        }
    }
}
