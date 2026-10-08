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
        [SerializeField] private Vector2 confettiArea = new Vector2(855, 780);
        [SerializeField] private float confettiSway = 36, confettiFallSpeed = 240, confettiSpeedStep = 36;
        private float punchAge = 1, celebrationAge = 5;
        private int juiceCombo;
        private float shieldAge = 1, shieldClock, timeAge = 1;
        private bool shieldActive;
        public void ShieldJuice() { shieldAge = 0; }
        public void TimeJuice() { timeAge = 0; }
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
            shieldAge = timeAge = 1; shieldClock = 0; shieldActive = false;
            Shield.transform.localScale = timer.rectTransform.localScale = Vector3.one;
            Shield.targetGraphic.color = new Color32(28, 67, 87, 255);
        }
        private void Celebrate(bool won)
        {
            celebrationAge = 0;
            celebration.gameObject.SetActive(won && !reducedMotion);
        }
        private void TickJuice(float delta)
        {
            if (animateFeedback) { punchAge += delta; shieldAge += delta; timeAge += delta; shieldClock += delta; }
            float punch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(punchAge / .24f) * Mathf.PI);
            score.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * .6f);
            combo.rectTransform.localScale = multiplier.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength);
            fever.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * (juiceCombo >= 10 ? .5f : .2f));
            float shieldPunch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(shieldAge / .35f) * Mathf.PI);
            float shieldPulse = !reducedMotion && shieldActive ? (.5f + .5f * Mathf.Sin(shieldClock * 4)) * .025f : 0;
            Shield.transform.localScale = Vector3.one * (1 + shieldPunch * punchStrength + shieldPulse);
            Shield.targetGraphic.color = shieldActive ? new Color32(28, 126, 148, 255) : Color.Lerp(new Color32(28, 67, 87, 255), UIArt.Cyan, shieldPunch * .6f);
            float timePunch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(timeAge / .3f) * Mathf.PI);
            timer.rectTransform.localScale = Vector3.one * (1 + timePunch * punchStrength);
            if (!celebration.gameObject.activeSelf) return;
            if (reducedMotion || !results.activeSelf) { celebration.gameObject.SetActive(false); return; }
            celebrationAge += delta;
            celebration.alpha = Mathf.Clamp01((2.8f - celebrationAge) / .65f);
            for (int i = 0; i < confetti.Length; i++)
            {
                float t = celebrationAge;
                confetti[i].anchoredPosition = new Vector2(-confettiArea.x * .5f + i * confettiArea.x / Mathf.Max(1, confetti.Length - 1) + Mathf.Sin(t * 3 + i) * confettiSway, confettiArea.y - (confettiFallSpeed + i % 5 * confettiSpeedStep) * t);
                confetti[i].localEulerAngles = new Vector3(0, 0, i * 31 + t * (i % 2 == 0 ? 170 : -130));
            }
            if (celebrationAge >= 2.8f) celebration.gameObject.SetActive(false);
        }
    }
}
