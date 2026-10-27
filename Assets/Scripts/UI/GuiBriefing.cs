using TMPro;
using UnityEngine;

namespace TapBlast
{
    public sealed class GuiBriefing : MonoBehaviour
    {
        [SerializeField] private TMP_Text oneStar, twoStars, threeStars, coins, xp, walletCoins, walletXP;
        public void Refresh(LevelConfig level)
        {
            // Theme: bold title line, secondary detail in the subtle text colour.
            oneStar.text = "1 star\n<size=78%><color=#6B7190>" + level.OneStarRule + "</color></size>";
            twoStars.text = "2 stars\n<size=78%><color=#6B7190>" + level.TwoStarRule + "</color></size>";
            threeStars.text = "3 stars\n<size=78%><color=#6B7190>" + level.ThreeStarRule + "</color></size>";
            coins.text = "+" + level.CoinReward + " coins";
            xp.text = "+" + level.XPReward + " XP";
            walletCoins.text = UIArt.Compact(ProgressStore.Coins) + "\n<size=62%><color=#6B7190>Coins</color></size>";
            walletXP.text = ProgressStore.XP.ToString("N0") + " XP\n<size=62%><color=#6B7190>Player level " + (1 + ProgressStore.XP / 100) + "</color></size>";
        }
    }
}
