using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    public sealed partial class GameHUD
    {
        [SerializeField] private TMP_Text fever;
        [SerializeField] private Image feverLine;
        [SerializeField] private RectTransform[] confetti;
        [SerializeField] private CanvasGroup celebration;
        [SerializeField, Range(0, .3f)] private float punchStrength = .16f;
        private float punchAge = 1, celebrationAge = 5;
        private int juiceCombo;
        public void HitJuice(int streak)
        {
            juiceCombo = streak; punchAge = 0;
            fever.text = streak >= 50 ? "MASTER COMBO" : streak >= 30 ? "ULTRA FEVER" : streak >= 20 ? "SUPER FEVER" : streak >= 10 ? "COMBO FEVER" : streak >= 5 ? "GREAT STREAK" : "BUILD YOUR STREAK";
            fever.color = streak >= 10 ? UIArt.Gold : UIArt.Cyan;
            feverLine.color = fever.color;
        }
        public void MissJuice(Vector2 position)
        {
            Pop(position, 0, UIArt.Muted, reducedMotion);
            var popup = popups[(popupCursor - 1) % popups.Length];
            popup.Label.text = "MISS"; popup.Core.enabled = false;
            HitJuice(0);
        }
        private void ResetJuice()
        {
            HitJuice(0); punchAge = 1; celebrationAge = 5;
            score.rectTransform.localScale = combo.rectTransform.localScale = multiplier.rectTransform.localScale = Vector3.one;
            fever.rectTransform.localScale = Vector3.one; celebration.gameObject.SetActive(false);
        }
        private void Celebrate(bool won)
        {
            celebrationAge = 0;
            celebration.gameObject.SetActive(won && !reducedMotion);
        }
        private void TickJuice(float delta)
        {
            if (animateFeedback) punchAge += delta;
            float punch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(punchAge / .24f) * Mathf.PI);
            score.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * .6f);
            combo.rectTransform.localScale = multiplier.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength);
            fever.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * (juiceCombo >= 10 ? .5f : .2f));
            if (!celebration.gameObject.activeSelf) return;
            if (reducedMotion || !results.activeSelf) { celebration.gameObject.SetActive(false); return; }
            celebrationAge += delta;
            celebration.alpha = Mathf.Clamp01((2.8f - celebrationAge) / .65f);
            for (int i = 0; i < confetti.Length; i++)
            {
                float t = celebrationAge;
                confetti[i].anchoredPosition = new Vector2(-285 + i * 570f / (confetti.Length - 1) + Mathf.Sin(t * 3 + i) * 24, 520 - (160 + i % 5 * 24) * t);
                confetti[i].localEulerAngles = new Vector3(0, 0, i * 31 + t * (i % 2 == 0 ? 170 : -130));
            }
            if (celebrationAge >= 2.8f) celebration.gameObject.SetActive(false);
        }
    }
}
