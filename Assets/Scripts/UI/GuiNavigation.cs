using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Additional reference-screen controls, bound to saved Canvas objects.</summary>
    public sealed class GuiNavigation : MonoBehaviour
    {
        [SerializeField] private Button headerPause, walletButton, selectionClose, walletClose;
        [SerializeField] private GameObject walletPanel;
        [SerializeField] private TMP_Text walletText, headerBalance;
        private GameManager game;
        private bool resumeAfterWallet;
        private int displayedCoins = -1;
        private void Start()
        {
            game = FindFirstObjectByType<GameManager>();
            headerPause.onClick.AddListener(() => game.Pause());
            selectionClose.onClick.AddListener(() => game.SelectLevel(ProgressStore.SelectedLevel));
            walletButton.onClick.AddListener(OpenWallet);
            walletClose.onClick.AddListener(CloseWallet);
        }
        private void Update()
        {
            if (displayedCoins != ProgressStore.Coins) { displayedCoins = ProgressStore.Coins; headerBalance.text = displayedCoins.ToString("N0"); }
            headerPause.interactable = game != null && game.Session != null && game.Session.State == RoundState.Playing;
        }
        private void OpenWallet()
        {
            resumeAfterWallet = game.Session.State == RoundState.Playing;
            if (resumeAfterWallet) game.Pause();
            walletText.text = ProgressStore.Coins.ToString("N0") + " COINS\n" + ProgressStore.XP.ToString("N0") + " XP\n\nPLAYER LEVEL " + (1 + ProgressStore.XP / 100) + "\n\nComplete levels to earn coins and XP.\nEvery 100 XP raises your player level.";
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
