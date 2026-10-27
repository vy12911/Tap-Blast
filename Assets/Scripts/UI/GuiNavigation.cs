using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Additional reference-screen controls, bound to saved Canvas objects.</summary>
    public sealed class GuiNavigation : MonoBehaviour
    {
        [SerializeField] private Button walletButton, selectionClose, walletClose;
        [SerializeField] private GameObject walletPanel;
        [SerializeField] private TMP_Text walletText, headerBalance;
        private GameManager game;
        private bool resumeAfterWallet;
        private int displayedCoins = -1;
        private void Start()
        {
            game = FindFirstObjectByType<GameManager>();
            selectionClose.onClick.AddListener(() => game.SelectLevel(ProgressStore.SelectedLevel));
            walletButton.onClick.AddListener(OpenWallet);
            walletClose.onClick.AddListener(CloseWallet);
        }
        private void Update()
        {
            if (displayedCoins != ProgressStore.Coins) { displayedCoins = ProgressStore.Coins; headerBalance.text = UIArt.Compact(displayedCoins); }
        }
        private void OpenWallet()
        {
            resumeAfterWallet = game.Session.State == RoundState.Playing;
            if (resumeAfterWallet) game.Pause();
            // Big values with coloured labels; the explanation stays secondary.
            walletText.text = "<size=150%><b>" + ProgressStore.Coins.ToString("N0") + "</b></size>\n<color=#C46800>COINS</color>\n\n" +
                "<size=150%><b>" + ProgressStore.XP.ToString("N0") + "</b></size>\n<color=#7A55F0>XP  ·  PLAYER LEVEL " + (1 + ProgressStore.XP / 100) + "</color>\n\n" +
                "<size=80%><color=#6B7190>Complete levels to earn coins and XP.\nEvery 100 XP raises your player level.</color></size>";
            walletPanel.SetActive(true);
        }
        public bool TryCloseWallet()
        {
            if (!walletPanel.activeSelf) return false;
            CloseWallet(); return true;
        }
        private void CloseWallet()
        {
            walletPanel.SetActive(false);
            if (resumeAfterWallet) game.Resume();
            resumeAfterWallet = false;
        }
    }
}
