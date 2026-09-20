using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Fits the authored portrait frame inside the device safe area.
    /// Owns only safe-area anchors and frame scale; child layouts stay designer-authored.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform frame;
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
            if (frame != null) tracker.Add(this, frame, DrivenTransformProperties.Scale);
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
            if (safeArea == null || frame == null || screen.x <= 0 || screen.y <= 0 || frame.rect.width <= 0 || frame.rect.height <= 0) return;
            if (safePixels.width <= 0 || safePixels.height <= 0) safePixels = new Rect(Vector2.zero, screen);
            Vector2 min = new Vector2(Mathf.Clamp01(safePixels.xMin / screen.x), Mathf.Clamp01(safePixels.yMin / screen.y));
            Vector2 max = new Vector2(Mathf.Clamp01(safePixels.xMax / screen.x), Mathf.Clamp01(safePixels.yMax / screen.y));
            safeArea.anchorMin = min; safeArea.anchorMax = max;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            Vector2 available = Vector2.Scale(canvasRect.rect.size, max - min);
            float scale = Mathf.Min(available.x / frame.rect.width, available.y / frame.rect.height);
            frame.localScale = Vector3.one * Mathf.Max(.001f, scale);
            lastScreen = screen; lastSafe = safePixels; lastCanvas = canvasRect.rect.size; lastFrame = frame.rect.size;
        }
    }
}
