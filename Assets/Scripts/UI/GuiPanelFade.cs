using UnityEngine;

namespace TapBlast
{
    // Each panel owns its authored CanvasGroup; no layout or UI is created at runtime.
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class GuiPanelFade : MonoBehaviour
    {
        private CanvasGroup group;
        private float elapsed;
        private void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            elapsed = ProgressStore.ReducedMotion ? .16f : 0;
            group.alpha = elapsed >= .16f ? 1 : 0;
        }
        private void Update()
        {
            // Settings are stored by the game, including reduced motion.
            if (elapsed >= .16f) return;
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / .16f));
        }
        private void OnDisable() { if (group != null) group.alpha = 1; }
    }
}
