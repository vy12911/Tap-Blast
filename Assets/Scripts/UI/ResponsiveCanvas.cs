using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Fits the portrait frame to the device. By default the frame fills the whole
    /// screen (Canvas Scaler keeps it at least 1080 x 1920 reference units) and child layouts
    /// anchor to its top, bottom and edges. Safe-area insets and fixed-frame scaling are optional.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform frame;
        [Tooltip("Inset the UI to Screen.safeArea (notches, gesture bars). Off: the UI uses the full screen.")]
        [SerializeField] private bool respectSafeArea;
        [Tooltip("Stretch the frame to fill the screen. Off: scale a fixed 1080 x 1920 frame to fit, leaving bands.")]
        [SerializeField] private bool fillScreen = true;
        private RectTransform canvasRect;
        private DrivenRectTransformTracker tracker;
        private Vector2 lastScreen, lastCanvas, lastFrame;
        private Rect lastSafe;
        public RectTransform Frame => frame;
        public RectTransform SafeArea => safeArea;

        private void OnEnable()
        {
            canvasRect = (RectTransform)transform;
            tracker.Clear();
            if (safeArea != null) tracker.Add(this, safeArea, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta);
            if (frame != null) tracker.Add(this, frame, fillScreen
                ? DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Scale
                : DrivenTransformProperties.Scale);
            lastScreen = Vector2.zero;
            Canvas.willRenderCanvases += Refresh;
            Refresh();
        }
        private void OnDisable() { Canvas.willRenderCanvases -= Refresh; tracker.Clear(); }
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            if (canvasRect == null || safeArea == null || frame == null) return;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            if (screen == lastScreen && safe == lastSafe && canvasRect.rect.size == lastCanvas && frame.rect.size == lastFrame) return;
            FitToViewport(screen, safe);
        }

        // Also used by the isolated device-layout validation with simulated safe areas.
        public void FitToViewport(Vector2 screen, Rect safePixels)
        {
            if (canvasRect == null) canvasRect = (RectTransform)transform;
            if (safeArea == null || frame == null || screen.x <= 0 || screen.y <= 0) return;
            lastSafe = safePixels; // Cache the input so Refresh only refits when the device changes.
            if (!respectSafeArea || safePixels.width <= 0 || safePixels.height <= 0) safePixels = new Rect(Vector2.zero, screen);
            Vector2 min = new Vector2(Mathf.Clamp01(safePixels.xMin / screen.x), Mathf.Clamp01(safePixels.yMin / screen.y));
            Vector2 max = new Vector2(Mathf.Clamp01(safePixels.xMax / screen.x), Mathf.Clamp01(safePixels.yMax / screen.y));
            safeArea.anchorMin = min; safeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            if (fillScreen)
            {
                frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.pivot = new Vector2(.5f, .5f);
                frame.offsetMin = frame.offsetMax = Vector2.zero; frame.localScale = Vector3.one;
            }
            else if (frame.rect.width > 0 && frame.rect.height > 0)
            {
                Vector2 available = Vector2.Scale(canvasRect.rect.size, max - min);
                float scale = Mathf.Min(available.x / frame.rect.width, available.y / frame.rect.height);
                frame.localScale = Vector3.one * Mathf.Max(.001f, scale);
            }
            lastScreen = screen; lastCanvas = canvasRect.rect.size; lastFrame = frame.rect.size;
        }
    }
}
