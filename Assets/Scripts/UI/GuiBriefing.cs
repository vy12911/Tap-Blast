using TMPro;
using UnityEngine;

namespace TapBlast
{
    public sealed class GuiBriefing : MonoBehaviour
    {
        [SerializeField] private TMP_Text oneStar, twoStars, threeStars, coins, xp, walletCoins, walletXP;
        public void Refresh(LevelConfig level)
        {
            oneStar.text = "1 STAR\n<size=30><color=#548DAA>Complete the level</color></size>";
            twoStars.text = "2 STARS\n<size=30><color=#548DAA>Complete +\nscore " + level.TwoStarScore + "+</color></size>";
            threeStars.text = "3 STARS\n<size=30><color=#548DAA>Both + no mistakes\n+ " + level.ThreeStarCombo + " combo</color></size>";
            coins.text = "+" + level.CoinReward + " COINS";
            xp.text = "+" + level.XPReward + " XP";
            walletCoins.text = "<size=28><color=#6698AF>WALLET</color></size>\n" + ProgressStore.Coins.ToString("N0") + " COINS";
            walletXP.text = ProgressStore.XP.ToString("N0") + " XP\n<size=24><color=#6698AF>PLAYER LEVEL " + (1 + ProgressStore.XP / 100) + "</color></size>";
        }
    }
}
