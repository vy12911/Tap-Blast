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
        [Tooltip("Scales every arena shake (bomb hit, Mega Chain, Golden Core). 0 disables shake.")]
        [SerializeField, Range(0, 2)] private float shakeScale = 1;
        private float shakeAge = 1, shakeLife, shakeStrength;
        private Vector2 shakeOffset, shakeHome;
        /// <summary>Short decaying shake of the arena card, in reference pixels. Stronger requests win.</summary>
        public void Shake(float strength, float seconds, float delay = 0)
        {
            if (reducedMotion || shakeScale <= 0) return;
            bool running = shakeAge >= 0 && shakeAge < shakeLife;
            if (running && strength < shakeStrength * (1 - shakeAge / shakeLife)) return;
            shakeStrength = strength * shakeScale; shakeLife = seconds; shakeAge = -delay;
        }
        public void ShieldJuice() { shieldAge = 0; }
        public void TimeJuice() { timeAge = 0; }
        public void HitJuice(int streak)
        {
            juiceCombo = streak; punchAge = 0;
            fever.text = streak >= 50 ? "MASTER COMBO" : streak >= 30 ? "ULTRA FEVER" : streak >= 20 ? "SUPER FEVER" : streak >= 10 ? "COMBO FEVER" : streak >= 5 ? "GREAT STREAK" : "BUILD YOUR STREAK";
            fever.color = streak >= 10 ? new Color32(214, 96, 0, 255) : UIArt.Primary;
            feverLine.color = fever.color;
        }
        public void MissJuice(Vector2 position)
        {
            MessagePop(position, "MISS", UIArt.Muted, reducedMotion);
            WorldEffects.Puff(position);
            HitJuice(0);
        }
        private void ResetJuice()
        {
            HitJuice(0); punchAge = 1; celebrationAge = 5;
            score.rectTransform.localScale = combo.rectTransform.localScale = multiplier.rectTransform.localScale = Vector3.one;
            fever.rectTransform.localScale = Vector3.one; celebration.gameObject.SetActive(false);
            shieldAge = timeAge = 1; shieldClock = 0; shieldActive = false;
            shakeAge = shakeLife = 0; ApplyShake(Vector2.zero);
            Shield.transform.localScale = timer.rectTransform.localScale = Vector3.one;
            // Objective bar animation state.
            for (int i = 0; i < starPop.Length; i++) starPop[i] = 1;
            foreach (var star in objectiveStars) star.rectTransform.localScale = Vector3.one;
            progressHead.localScale = Vector3.one; headClock = 0; earnedStars = 0;
            Shield.targetGraphic.color = UIArt.Primary;
        }
        // Offsets the whole arena card (rim, targets, effects) and always restores the authored position.
        private void ApplyShake(Vector2 offset)
        {
            if (offset == shakeOffset) return;
            var card = (RectTransform)Arena.parent;
            if (shakeOffset == Vector2.zero) shakeHome = card.anchoredPosition;
            card.anchoredPosition = shakeHome + offset; shakeOffset = offset;
        }
        private void Celebrate(bool won)
        {
            celebrationAge = 0;
            celebration.gameObject.SetActive(won && !reducedMotion);
        }
        private void TickJuice(float delta)
        {
            if (animateFeedback) { punchAge += delta; shieldAge += delta; timeAge += delta; shieldClock += delta; shakeAge += delta; }
            float shake = !animateFeedback || reducedMotion || shakeAge < 0 || shakeAge >= shakeLife ? 0 : (1 - shakeAge / shakeLife) * (1 - shakeAge / shakeLife);
            ApplyShake(shake <= 0 ? Vector2.zero : new Vector2(Mathf.Sin(shakeAge * 61), Mathf.Cos(shakeAge * 47)) * (shakeStrength * shake));
            float punch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(punchAge / .24f) * Mathf.PI);
            score.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * .6f);
            combo.rectTransform.localScale = multiplier.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength);
            fever.rectTransform.localScale = Vector3.one * (1 + punch * punchStrength * (juiceCombo >= 10 ? .5f : .2f));
            float shieldPunch = reducedMotion ? 0 : Mathf.Sin(Mathf.Clamp01(shieldAge / .35f) * Mathf.PI);
            float shieldPulse = !reducedMotion && shieldActive ? (.5f + .5f * Mathf.Sin(shieldClock * 4)) * .025f : 0;
            Shield.transform.localScale = Vector3.one * (1 + shieldPunch * punchStrength + shieldPulse);
            Shield.targetGraphic.color = shieldActive ? UIArt.Success : Color.Lerp(UIArt.Primary, UIArt.Accent, shieldPunch * .6f);
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
