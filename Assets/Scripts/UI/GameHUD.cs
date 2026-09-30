using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Binds authored Canvas elements. TMP_Text updates on events/seconds;
    /// target and feedback canvases isolate frequent geometry rebuilds.</summary>
    public sealed partial class GameHUD : MonoBehaviour
    {
        [field: SerializeField] public TargetView TargetPrefab { get; set; }
        [field: SerializeField] public RectTransform Arena { get; private set; }
        [field: SerializeField] public Button Play { get; private set; }
        [field: SerializeField] public Button Pause { get; private set; }
        [field: SerializeField] public Button Resume { get; private set; }
        [field: SerializeField] public Button Restart { get; private set; }
        [field: SerializeField] public Button Replay { get; private set; }
        [field: SerializeField] public Button Sound { get; private set; }
        [field: SerializeField] public Button Motion { get; private set; }
        [field: SerializeField] public Button Haptics { get; private set; }
        [field: SerializeField] public Button Next { get; private set; }
        [field: SerializeField] public Button IntroMenu { get; private set; }
        [field: SerializeField] public Button PauseMenu { get; private set; }
        [field: SerializeField] public Button ResultMenu { get; private set; }
        [field: SerializeField] public Button LevelOne { get; private set; }
        [field: SerializeField] public Button LevelTwo { get; private set; }
        [SerializeField] private GameObject levelSelect, introHero, sizePreview;
        [SerializeField] private TMP_Text worldLabel, levelLabel, description, countdownGoal, selectionWallet;
        private LevelConfig currentConfig;
        [SerializeField] private GameObject intro, pausePanel, results, countdownPanel;
        [SerializeField] private TMP_Text score, timer, objective, combo, multiplier, hint, subhint, countdownText;
        [SerializeField] private TMP_Text introName, introGoal, introBest, starRules, resultTitle, resultSubtitle, resultScore, resultStats, resultBest, resultHint;
        [SerializeField] private Image progressFill, timerFill;
        [SerializeField] private Image[] resultStars = new Image[3];
        [SerializeField] private Popup[] popups = new Popup[10];
        private int popupCursor;
        private bool animateFeedback;
        private int lastCountdown = -1;
        [SerializeField] private TapHandHint tapHand;
        private ResultReveal resultReveal;
        private bool reducedMotion;
        [SerializeField] private TMP_Text rewardPreview, rewardEarned, rewardBalance, playerProgress;
        [SerializeField] private Image xpFill;
        [SerializeField] private CanvasGroup rewardGroup;
        [Header("Feedback distances in Canvas reference pixels")]
        [SerializeField] private float popupRise = 48, sparkStartRadius = 33, sparkTravel = 127.5f, previewGlowPadding = 120;
        public void Initialize() { resultReveal = new ResultReveal(resultScore, resultBest, resultStars, rewardGroup); HideOverlays(); }
        public bool TutorialHandVisible => tapHand.IsVisible;
        public bool ResultRevealComplete => resultReveal.IsComplete;
        [System.Serializable] private sealed class Popup
        {
            public RectTransform Root, RingRect;
            public TMP_Text Label;
            public Image Ring;
            public Image Core;
            public RectTransform[] Sparks;
            public CanvasGroup Alpha;
            public Vector2 Origin;
            public float Age;
            public bool Active, ReducedMotion;
        }

        private void HideOverlays()
        {
            resultReveal.Cancel();
            levelSelect.SetActive(false);
            intro.SetActive(false); pausePanel.SetActive(false); results.SetActive(false); countdownPanel.SetActive(false);
            if (prevWorldBtn != null) prevWorldBtn.gameObject.SetActive(false);
            if (nextWorldBtn != null) nextWorldBtn.gameObject.SetActive(false);
        }
        public void ShowIntro(LevelConfig config, int best)
        {
            HideOverlays(); intro.SetActive(true); Pause.interactable = false; animateFeedback = false;
            currentConfig = config;
            worldLabel.text = (config.LevelId > 10 ? "WORLD 02   /   " : "WORLD 01   /   ") + config.DisplayName;
            levelLabel.text = config.LevelId.ToString("00");
            countdownGoal.text = config.GoalText;
            introHero.SetActive(config.LevelId == 1); sizePreview.SetActive(config.LevelId == 2);
            description.text = config.MechanicHint + (config.FirstTapTutorial ? "" : "\nKeep playing for mastery until time runs out.");
            for (int i = 0; i < 3; i++)
            {
                ((RectTransform)sizePreview.transform.GetChild(i * 2)).sizeDelta = Vector2.one * (config.SizeAt(i) + previewGlowPadding);
                ((RectTransform)sizePreview.transform.GetChild(i * 2 + 1)).sizeDelta = Vector2.one * config.SizeAt(i);
            }
            introName.text = config.DisplayName;
            introGoal.text = config.GoalText;
            starRules.text = "1 STAR   Complete the level\n2 STARS   Complete + score " + config.TwoStarScore + "+\n3 STARS   Both + no misses + " + config.ThreeStarCombo + " combo";
            rewardPreview.text = "COMPLETE:  +" + config.CoinReward + " COINS   +" + config.XPReward + " XP";
            introBest.text = "WALLET  " + ProgressStore.Coins.ToString("N0") + " COINS   /   " + ProgressStore.XP.ToString("N0") + " XP\n" + (best > 0 ? "BEST  " + best.ToString("N0") + "   /   " + ProgressStore.GetBestStars(config.LevelId) + " STARS" : "A new personal best starts here");
        }
        public void ShowCountdown() { HideOverlays(); countdownPanel.SetActive(true); Pause.interactable = false; animateFeedback = false; }
        public void SetCountdown(int count) { if (count == lastCountdown) return; lastCountdown = count; countdownText.text = count.ToString(); }
        public void ShowPlaying() { HideOverlays(); Pause.interactable = true; animateFeedback = true; }
        public void ShowPause() { HideOverlays(); pausePanel.SetActive(true); Pause.interactable = false; animateFeedback = false; }
        public void SetHint(string title, string detail) { hint.text = title; subhint.text = detail; }
        public void Refresh(RoundSession session, LevelConfig config)
        {
            score.text = session.Score.ToString("D5"); timer.text = Mathf.CeilToInt(session.RemainingTime).ToString("00");
            timerFill.fillAmount = session.RemainingTime / config.Duration;
            timerFill.color = session.RemainingTime <= 10 ? UIArt.Gold : UIArt.Cyan;
            objective.text = config.ObjectiveLabel + "  " + session.ObjectiveProgress + " / " + session.ObjectiveTarget;
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)session.ObjectiveProgress / session.ObjectiveTarget), 1);
            combo.text = session.Combo.ToString(); multiplier.text = "x" + session.Multiplier.ToString("0.##");
        }
        public void ShowResults(RoundSession session, bool newBest, int best, ProgressStore.RewardReceipt receipt)
        {
            HideOverlays(); results.SetActive(true); Pause.interactable = false; animateFeedback = false; ClearFeedback();
            SetHint("", "");
            HideTapHand();
            resultTitle.text = session.Won ? "BRILLIANT!" : "TRY AGAIN";
            resultSubtitle.text = session.Won ? "LEVEL " + session.LevelId.ToString("00") + " COMPLETE" : "TIME UP  /  OBJECTIVE MISSED";
            bool hasNext = session.Won && session.LevelId < 20 && ProgressStore.IsUnlocked(session.LevelId + 1);
            Next.gameObject.SetActive(hasNext);
            Replay.GetComponentInChildren<TMP_Text>().text = hasNext ? "REPLAY" : "PLAY AGAIN";
            for (int i = 0; i < 3; i++) resultStars[i].color = i < session.Stars ? UIArt.Gold : new Color32(57, 77, 90, 255);
            resultScore.text = session.Score.ToString("N0");
            resultBest.text = newBest ? "NEW PERSONAL BEST" : "PERSONAL BEST  " + best.ToString("N0");
            resultStats.text = "Targets tapped     " + session.Hits + "\nBest combo          " + session.BestCombo + "\nTargets missed     " + session.Misses;
            resultHint.text = !session.Won ? "Tap targets as they appear.\nYou can try again immediately." : session.Stars == 3 ? "Mastery achieved! All three stars earned!" : "Chase three stars: keep a clean streak\nand aim for the score goal.";
            if (hasNext) resultHint.text = "LEVEL " + (session.LevelId + 1) + " UNLOCKED\nGet ready for the next challenge!";
            rewardEarned.text = session.Won ? "+" + receipt.Coins + " COINS     +" + receipt.XP + " XP" : "COMPLETE THE LEVEL TO EARN REWARDS";
            rewardBalance.text = "WALLET  " + receipt.TotalCoins.ToString("N0") + " COINS   /   TOTAL XP  " + receipt.TotalXP.ToString("N0");
            bool leveledUp = receipt.TotalXP / 100 > (receipt.TotalXP - receipt.XP) / 100;
            playerProgress.text = (leveledUp ? "LEVEL UP!  " : "PLAYER LEVEL ") + receipt.PlayerLevel + "   /   " + (receipt.TotalXP % 100) + " / 100 XP";
            xpFill.rectTransform.anchorMax = new Vector2(receipt.TotalXP % 100 / 100f, 1);
            resultReveal.Begin(session.Score, session.Stars, reducedMotion);
            Celebrate(session.Won);
        }
        public void SetSettings(bool sound, bool reducedMotion, bool haptics)
        {
            this.reducedMotion = reducedMotion;
            if (reducedMotion)
            {
                foreach (var p in popups)
                {
                    p.ReducedMotion = true; p.Ring.enabled = false; p.Core.enabled = false;
                    foreach (var spark in p.Sparks) spark.gameObject.SetActive(false);
                }
                celebration.gameObject.SetActive(false);
            }
            Sound.GetComponentInChildren<TMP_Text>().text = "SOUND  " + (sound ? "ON" : "OFF");
            Motion.GetComponentInChildren<TMP_Text>().text = "REDUCED MOTION  " + (reducedMotion ? "ON" : "OFF");
            Haptics.GetComponentInChildren<TMP_Text>().text = "VIBRATION  " + (haptics ? "ON" : "OFF");
        }
        public void Pop(Vector2 position, int points, Color tint, bool reducedMotion)
        {
            var p = popups[popupCursor++ % popups.Length]; p.Age = 0; p.Active = true;
            p.ReducedMotion = reducedMotion; p.Origin = position; p.Root.anchoredPosition = position;
            p.Label.text = "+" + points; p.Ring.color = tint; p.RingRect.localScale = Vector3.one;
            p.Ring.enabled = !reducedMotion; p.Alpha.alpha = 1; p.Root.gameObject.SetActive(true);
            p.Core.enabled = !reducedMotion; p.Core.rectTransform.localScale = Vector3.one;
            for (int i = 0; i < p.Sparks.Length; i++) { p.Sparks[i].gameObject.SetActive(!reducedMotion); p.Sparks[i].anchoredPosition = Vector2.zero; p.Sparks[i].localScale = Vector3.one; }
        }
        public void ClearFeedback() { foreach (var p in popups) { p.Active = false; p.Root.gameObject.SetActive(false); } popupCursor = 0; ResetJuice(); }
        private void Update()
        {
            TickJuice(Time.unscaledDeltaTime);
            tapHand.Tick(Time.unscaledDeltaTime, animateFeedback, reducedMotion);
            if (results.activeSelf) resultReveal.Tick(Time.unscaledDeltaTime, reducedMotion);
            if (!animateFeedback) return;
            for (int i = 0; i < popups.Length; i++)
            {
                var p = popups[i]; if (!p.Active) continue;
                p.Age += Time.unscaledDeltaTime; float t = p.Age / .55f;
                if (t >= 1) { p.Active = false; p.Root.gameObject.SetActive(false); continue; }
                p.Alpha.alpha = 1 - t;
                if (!p.ReducedMotion) { p.Root.anchoredPosition = p.Origin + Vector2.up * (t * popupRise); p.RingRect.localScale = Vector3.one * (1 + t * .8f); }
                p.Core.rectTransform.localScale = Vector3.one * Mathf.Clamp01(1 - p.Age / .12f);
                for (int j = 0; j < p.Sparks.Length; j++)
                {
                    float angle = j * Mathf.PI * 2 / p.Sparks.Length;
                    p.Sparks[j].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (sparkStartRadius + sparkTravel * (1 - Mathf.Pow(1 - t, 2)));
                    p.Sparks[j].localScale = Vector3.one * (1 - t);
                }
            }
        }
        public void ShowTapHand() { tapHand.Show(); tapHand.Tick(0, animateFeedback, reducedMotion); }
        public void HideTapHand() { tapHand.Hide(); }

        [SerializeField] private Button[] levelButtons = new Button[10];
        [SerializeField] private TMP_Text[] selectionTexts = new TMP_Text[10];
        private int currentLevelPage = 0;
        private LevelConfig[] cachedConfigs;
        private System.Func<int, bool> cachedOnSelectLevel;

        private Button prevWorldBtn, nextWorldBtn;
        private Image prevWorldImg, nextWorldImg;
        private TMP_Text prevWorldText, nextWorldText;

        private void EnsureWorldButtons()
        {
            if (worldLabel == null || prevWorldBtn != null) return;
            Transform parent = worldLabel.transform.parent;
            if (parent == null) return;

            GameObject leftObj = new GameObject("PrevWorldButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            leftObj.transform.SetParent(parent, false);
            RectTransform leftRect = leftObj.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0.5f, 0.5f);
            leftRect.anchorMax = new Vector2(0.5f, 0.5f);
            leftRect.anchoredPosition = new Vector2(-270, 792);
            leftRect.sizeDelta = new Vector2(110, 52);

            prevWorldImg = leftObj.GetComponent<Image>();
            prevWorldImg.color = new Color32(24, 85, 110, 240);

            GameObject leftTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            leftTextObj.transform.SetParent(leftObj.transform, false);
            RectTransform ltRect = leftTextObj.GetComponent<RectTransform>();
            ltRect.anchorMin = Vector2.zero; ltRect.anchorMax = Vector2.one; ltRect.sizeDelta = Vector2.zero;
            prevWorldText = leftTextObj.AddComponent<TextMeshProUGUI>();
            prevWorldText.text = "< W1";
            prevWorldText.font = worldLabel.font;
            prevWorldText.fontSize = 22;
            prevWorldText.fontStyle = FontStyles.Bold;
            prevWorldText.alignment = TextAlignmentOptions.Center;
            prevWorldText.color = UIArt.Cyan;
            prevWorldText.raycastTarget = false;

            prevWorldBtn = leftObj.GetComponent<Button>();
            prevWorldBtn.transition = Selectable.Transition.None;
            prevWorldBtn.onClick.AddListener(() => ShowLevelsPage(0));

            GameObject rightObj = new GameObject("NextWorldButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            rightObj.transform.SetParent(parent, false);
            RectTransform rightRect = rightObj.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.5f, 0.5f);
            rightRect.anchorMax = new Vector2(0.5f, 0.5f);
            rightRect.anchoredPosition = new Vector2(270, 792);
            rightRect.sizeDelta = new Vector2(110, 52);

            nextWorldImg = rightObj.GetComponent<Image>();
            nextWorldImg.color = new Color32(95, 75, 24, 240);

            GameObject rightTextObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
            rightTextObj.transform.SetParent(rightObj.transform, false);
            RectTransform rtRect = rightTextObj.GetComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero; rtRect.anchorMax = Vector2.one; rtRect.sizeDelta = Vector2.zero;
            nextWorldText = rightTextObj.AddComponent<TextMeshProUGUI>();
            nextWorldText.text = "W2 >";
            nextWorldText.font = worldLabel.font;
            nextWorldText.fontSize = 22;
            nextWorldText.fontStyle = FontStyles.Bold;
            nextWorldText.alignment = TextAlignmentOptions.Center;
            nextWorldText.color = UIArt.Gold;
            nextWorldText.raycastTarget = false;

            nextWorldBtn = rightObj.GetComponent<Button>();
            nextWorldBtn.transition = Selectable.Transition.None;
            nextWorldBtn.onClick.AddListener(() => ShowLevelsPage(1));
        }

        public void ShowLevels(LevelConfig[] configs, System.Func<int, bool> onSelectLevel)
        {
            cachedConfigs = configs;
            cachedOnSelectLevel = onSelectLevel;
            int selected = currentConfig != null ? currentConfig.LevelId : ProgressStore.SelectedLevel;
            int targetPage = selected > 10 ? 1 : 0;
            ShowLevelsPage(targetPage);
        }

        public void ShowLevelsPage(int page)
        {
            currentLevelPage = Mathf.Clamp(page, 0, 1);
            HideOverlays(); levelSelect.SetActive(true); Pause.interactable = false; animateFeedback = false;
            SetHint("", "");
            EnsureWorldButtons();

            if (worldLabel != null)
                worldLabel.text = currentLevelPage == 1 ? "WORLD 02   /   COMBO CITY" : "WORLD 01   /   LEARN TO TAP";

            if (selectionWallet != null)
            {
                string switchHint = ProgressStore.IsUnlocked(11) ? (currentLevelPage == 0 ? "   •   TAP < W1 OR W2 > TO SWITCH" : "   •   TAP < W1 OR W2 > TO SWITCH") : "";
                selectionWallet.text = ProgressStore.Coins.ToString("N0") + " COINS   /   " + ProgressStore.XP.ToString("N0") + " XP\nPLAYER LEVEL " + (1 + ProgressStore.XP / 100) + switchHint;
            }

            bool w2Unlocked = ProgressStore.IsUnlocked(11);

            if (prevWorldBtn != null)
            {
                prevWorldBtn.gameObject.SetActive(true);
                prevWorldBtn.interactable = currentLevelPage == 1;
                prevWorldText.text = currentLevelPage == 1 ? "< W1" : "W1";
                prevWorldImg.color = currentLevelPage == 1 ? new Color32(24, 85, 110, 240) : new Color32(18, 38, 52, 160);
                prevWorldText.color = currentLevelPage == 1 ? UIArt.Cyan : new Color32(100, 130, 140, 255);
            }

            if (nextWorldBtn != null)
            {
                nextWorldBtn.gameObject.SetActive(true);
                nextWorldBtn.interactable = w2Unlocked && currentLevelPage == 0;
                nextWorldText.text = !w2Unlocked ? "LOCK" : (currentLevelPage == 0 ? "W2 >" : "W2");
                nextWorldImg.color = (w2Unlocked && currentLevelPage == 0) ? new Color32(95, 75, 24, 240) : new Color32(18, 38, 52, 160);
                nextWorldText.color = (w2Unlocked && currentLevelPage == 0) ? UIArt.Gold : new Color32(120, 130, 140, 255);
            }

            int startLevel = currentLevelPage * 10 + 1;
            for (int i = 0; i < 10; i++)
            {
                int levelId = startLevel + i;
                LevelConfig cfg = null;
                if (cachedConfigs != null) foreach (var c in cachedConfigs) if (c != null && c.LevelId == levelId) cfg = c;
                string nameStr = cfg != null ? cfg.DisplayName : "LEVEL " + levelId.ToString("00");

                if (levelButtons[i] != null)
                {
                    TMP_Text mainText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                    if (mainText != null) mainText.text = levelId.ToString("00") + "  /  " + nameStr;

                    bool unlocked = cfg != null && ProgressStore.IsUnlocked(levelId);
                    int stars = ProgressStore.GetBestStars(levelId);
                    int score = ProgressStore.GetBestScore(levelId);

                    levelButtons[i].interactable = unlocked;

                    if (selectionTexts[i] != null)
                    {
                        if (stars > 0)
                        {
                            selectionTexts[i].text = "COMPLETED   BEST " + score.ToString("N0") + "   /   " + stars + " / 3 STARS";
                            selectionTexts[i].color = UIArt.Gold;
                        }
                        else if (unlocked)
                        {
                            selectionTexts[i].text = "UNLOCKED   BEST " + score.ToString("N0") + "   /   0 / 3 STARS";
                            selectionTexts[i].color = UIArt.Cyan;
                        }
                        else
                        {
                            selectionTexts[i].text = "LOCKED   Complete Level " + (levelId - 1) + " to unlock";
                            selectionTexts[i].color = new Color32(120, 140, 150, 255);
                        }
                    }

                    int id = levelId;
                    levelButtons[i].onClick.RemoveAllListeners();
                    levelButtons[i].onClick.AddListener(() => cachedOnSelectLevel(id));
                }
            }

            if (w2Unlocked)
            {
                if (worldLabel != null)
                {
                    worldLabel.raycastTarget = true;
                    var wBtn = worldLabel.GetComponent<Button>() ?? worldLabel.gameObject.AddComponent<Button>();
                    wBtn.onClick.RemoveAllListeners();
                    wBtn.onClick.AddListener(ToggleLevelPage);
                }
                if (selectionWallet != null)
                {
                    selectionWallet.raycastTarget = true;
                    var wletBtn = selectionWallet.GetComponent<Button>() ?? selectionWallet.gameObject.AddComponent<Button>();
                    wletBtn.onClick.RemoveAllListeners();
                    wletBtn.onClick.AddListener(ToggleLevelPage);
                }
            }
        }

        public void ToggleLevelPage()
        {
            if (ProgressStore.IsUnlocked(11))
            {
                ShowLevelsPage(1 - currentLevelPage);
            }
        }

        public void ShowLevels() { ShowLevels(null, (id) => false); }
    }
}
