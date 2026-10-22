using UnityEngine;

namespace TapBlast
{
    public sealed class GuiScreenVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject briefing, levels, results;
        [SerializeField] private CanvasGroup[] gameplay;
        [SerializeField] private GameObject paused, countdown;
        [SerializeField] private CanvasGroup[] modalObscured;
        private bool? lastVisible;
        private bool? lastModal;
        private void LateUpdate() { Refresh(); }
        public void Refresh()
        {
            bool visible = !briefing.activeSelf && !levels.activeSelf && !results.activeSelf;
            bool modal=paused.activeSelf || countdown.activeSelf;
            if(lastVisible == visible && lastModal==modal) return;
            lastVisible = visible; lastModal=modal;
            foreach(var group in gameplay) { group.alpha = visible ? 1 : 0; group.blocksRaycasts = visible; }
            foreach(var group in modalObscured) {group.alpha=visible && !modal ? 1 : 0;group.blocksRaycasts=visible && !modal;}
        }
    }
}
