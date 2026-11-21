// Enable the GDD animation presentation in this editor's local player settings.
// Preserve sound, UI layout, prefab contents and every art asset.
TapBlast.ProgressStore.SaveSettings(TapBlast.ProgressStore.Sound, false);
if (UnityEditor.EditorApplication.isPlaying)
{
    var feedback = UnityEngine.Object.FindFirstObjectByType<TapBlast.FeedbackPlayer>();
    if (feedback != null && feedback.ReducedMotion) feedback.ToggleMotion();
    var hud = UnityEngine.Object.FindFirstObjectByType<TapBlast.GameHUD>();
    if (hud != null) hud.SetSettings(TapBlast.ProgressStore.Sound, false);
}
UnityEngine.Debug.Log("GDD animations enabled. Reduced motion remains available in Pause settings.");
