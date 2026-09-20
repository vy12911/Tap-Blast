using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Presentation only. Never grants rewards; replay may interrupt this safely.</summary>
    public sealed class ResultReveal
    {
        private readonly TMP_Text score, best;
        private readonly Image[] stars;
        private readonly CanvasGroup rewards;
        private float elapsed;
        private int finalScore, finalStars, displayedScore = -1;
        private bool active;
        public bool IsComplete => !active;
        public ResultReveal(TMP_Text score, TMP_Text best, Image[] stars, CanvasGroup rewards)
        { this.score = score; this.best = best; this.stars = stars; this.rewards = rewards; }
        public void Begin(int points, int earnedStars, bool reducedMotion)
        {
            finalScore = points; finalStars = earnedStars; elapsed = 0; displayedScore = -1; active = true;
            Tick(0, reducedMotion);
        }
        public void Cancel()
        {
            active = false;
            foreach (var star in stars) star.rectTransform.localScale = Vector3.one;
        }
        public void Tick(float delta, bool reducedMotion)
        {
            if (!active) return;
            elapsed = reducedMotion ? 3 : elapsed + delta;
            float progress = Mathf.Clamp01(elapsed / .9f);
            int value = Mathf.RoundToInt(finalScore * (1 - Mathf.Pow(1 - progress, 3)));
            if (value != displayedScore) { displayedScore = value; score.text = value.ToString("N0"); }
            best.enabled = elapsed >= .9f;
            for (int i = 0; i < stars.Length; i++)
            {
                float starTime = elapsed - (1.1f + i * .25f);
                bool earned = i < finalStars && starTime >= 0;
                stars[i].color = earned ? UIArt.Gold : new Color32(57, 77, 90, 255);
                float bounce = earned && !reducedMotion ? Mathf.Sin(Mathf.Clamp01(starTime / .3f) * Mathf.PI) * .2f : 0;
                stars[i].rectTransform.localScale = Vector3.one * (1 + bounce);
            }
            rewards.alpha = Mathf.Clamp01((elapsed - 1.95f) / .35f);
            if (elapsed >= 2.3f) { score.text = finalScore.ToString("N0"); active = false; }
        }
    }
}
