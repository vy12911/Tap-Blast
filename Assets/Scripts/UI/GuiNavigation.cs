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
        [Header("Wallet panel")]
        [SerializeField] private TMP_Text walletCoins, walletXP, walletLevel;
        [SerializeField] private Image walletXpFill;
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
            if (resumeAfterWallet) game.PauseBehindOverlay();
            // Exact amounts here (the header pill shows the compact form).
            int xp = ProgressStore.XP, level = 1 + xp / 100;
            walletCoins.text = ProgressStore.Coins.ToString("N0") + "\n<size=48%><color=#9A5B00>Coins</color></size>";
            walletXP.text = xp.ToString("N0") + "\n<size=48%><color=#6A45D8>Total XP</color></size>";
            walletLevel.text = "Player level <b>" + level + "</b><size=80%><color=#6B7190>   ·   " + xp % 100 + " / 100 XP to level " + (level + 1) + "</color></size>";
            walletXpFill.rectTransform.anchorMax = new Vector2(xp % 100 / 100f, 1);
            walletText.text = "Complete levels to earn coins and XP.\nEvery 100 XP raises your player level.";
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
