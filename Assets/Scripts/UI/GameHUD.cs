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
        [field: SerializeField] public Button Shield { get; private set; }
        [SerializeField] private TMP_Text shieldLabel, mechanicStatus;
        [field: SerializeField] public Button IntroMenu { get; private set; }
        [field: SerializeField] public Button PauseMenu { get; private set; }
        [field: SerializeField] public Button ResultMenu { get; private set; }
        [field: SerializeField] public Button LevelOne { get; private set; }
        [field: SerializeField] public Button LevelTwo { get; private set; }
        [SerializeField] private GameObject levelSelect, introHero, sizePreview;
        [SerializeField] private TMP_Text worldLabel, levelLabel, description, countdownGoal, selectionWallet;
        [SerializeField] private TMP_Text introWorld, selectionWorld, selectionTitle;
        private LevelConfig currentConfig;
        [SerializeField] private GameObject intro, pausePanel, results, countdownPanel;
        [SerializeField] private TMP_Text score, timer, objective, combo, multiplier, hint, subhint, countdownText;
        [SerializeField] private TMP_Text introName, introGoal, introBest, starRules, resultTitle, resultSubtitle, resultScore, resultStats, resultBest, resultHint;
        [SerializeField] private Image progressFill, timerFill;
        [SerializeField] private Image[] resultStars = new Image[3];
        [SerializeField] private Popup[] popups = new Popup[10];
        [field: SerializeField] public World2Effects WorldEffects { get; private set; }
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
        public void Initialize()
        {
            resultReveal = new ResultReveal(resultScore, resultBest, resultStars, rewardGroup);
            prevWorldBtn.onClick.AddListener(() => ShowLevelsPage(0));
            nextWorldBtn.onClick.AddListener(() => ShowLevelsPage(1));
            HideOverlays();
        }
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
            public float Age, BlastScale = 1;
            public int ReactionStyle;
            public bool Active, ReducedMotion;
        }

        private void HideOverlays()
        {
            resultReveal.Cancel();
            levelSelect.SetActive(false);
            intro.SetActive(false); pausePanel.SetActive(false); results.SetActive(false); countdownPanel.SetActive(false);
            if (prevWorldBtn != null) prevWorldBtn.gameObject.SetActive(false);
            if (nextWorldBtn != null) nextWorldBtn.gameObject.SetActive(false);
            Shield.gameObject.SetActive(false);
            mechanicStatus.gameObject.SetActive(false);
        }
        public void ShowIntro(LevelConfig config, int best)
        {
            HideOverlays(); intro.SetActive(true); Pause.interactable = false; animateFeedback = false;
            currentConfig = config;
            introWorld.text = config.LevelId > 10 ? "COMBO CITY" : "LEARN TO TAP";
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
            starRules.text = "1 STAR   Complete the level\n2 STARS   Complete + score " + config.TwoStarScore + "+\n3 STARS   Both + no mistakes + " + config.ThreeStarCombo + " combo";
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
            WorldEffects.Configure(config.LevelId, session.ShieldActive);
            score.text = session.Score.ToString("D5"); timer.text = Mathf.CeilToInt(session.RemainingTime).ToString("00");
            timerFill.fillAmount = session.RemainingTime / config.Duration;
            timerFill.color = session.RemainingTime <= 10 ? UIArt.Gold : UIArt.Cyan;
            objective.text = config.ObjectiveLabel + "  " + session.ObjectiveProgress + " / " + session.ObjectiveTarget;
            if (session.BombObjectiveFailed) objective.text = "BOMB HIT - REPLAY TO RETRY";
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)session.ObjectiveProgress / session.ObjectiveTarget), 1);
            combo.text = session.Combo.ToString(); multiplier.text = "x" + session.Multiplier.ToString("0.##");
            bool playing = session.State == RoundState.Playing;
            Shield.gameObject.SetActive(playing && config.ShieldCharges > 0);
            Shield.interactable = playing && !session.ShieldActive && session.ShieldsRemaining > 0;
            shieldActive = session.ShieldActive;
            shieldLabel.text = session.ShieldActive ? "SHIELD ACTIVE" : session.ShieldsRemaining > 0 ? "SHIELD  " + session.ShieldsRemaining : "SHIELD USED";
            mechanicStatus.gameObject.SetActive(playing && config.LevelId > 10);
            mechanicStatus.text = "CHAINS " + session.Chains + "   MEGA " + session.MegaChains + "   BOMBS " + session.BombHits +
                (config.RequiredCombo > 0 ? "   BEST " + session.BestCombo + "/" + config.RequiredCombo : "");
        }
        public void ShowResults(RoundSession session, bool newBest, int best, ProgressStore.RewardReceipt receipt)
        {
            HideOverlays(); results.SetActive(true); Pause.interactable = false; animateFeedback = false; ClearFeedback();
            SetHint("", "");
            HideTapHand();
            resultTitle.text = session.Won ? "BRILLIANT!" : "TRY AGAIN";
            resultSubtitle.text = session.Won ? "LEVEL " + session.LevelId.ToString("00") + " COMPLETE" : session.BombObjectiveFailed ? "BOMB HIT  /  OBJECTIVE MISSED" : "TIME UP  /  OBJECTIVE MISSED";
            bool hasNext = session.Won && session.LevelId < 20 && ProgressStore.IsUnlocked(session.LevelId + 1);
            Next.gameObject.SetActive(hasNext);
            Replay.GetComponentInChildren<TMP_Text>().text = hasNext ? "REPLAY" : "PLAY AGAIN";
            for (int i = 0; i < 3; i++) resultStars[i].color = i < session.Stars ? UIArt.Gold : new Color32(57, 77, 90, 255);
            resultScore.text = session.Score.ToString("N0");
            resultBest.text = newBest ? "NEW PERSONAL BEST" : "PERSONAL BEST  " + best.ToString("N0");
            resultStats.text = "Targets tapped     " + session.Hits + "\nBest combo          " + session.BestCombo + "\nTargets missed     " + session.Misses;
            if (session.LevelId > 10)
                resultStats.text = "BEST COMBO " + session.BestCombo + "   MISSES " + session.Misses + "\nCHAINS " + session.Chains + "   MEGA " + session.MegaChains + "\nBOMBS " + session.BombHits + "   SHIELD BLOCKS " + session.ShieldBlocks;
            resultHint.text = !session.Won ? "Tap targets as they appear.\nYou can try again immediately." : session.Stars == 3 ? "Mastery achieved! All three stars earned!" : "Chase three stars: keep a clean streak\nand aim for the score goal.";
            if (hasNext) resultHint.text = "LEVEL " + (session.LevelId + 1) + " UNLOCKED\nGet ready for the next challenge!";
            else if (session.Won && session.LevelId == 20) resultHint.text = "WORLD 2 COMPLETE!\nReplay levels to earn every star.";
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
            WorldEffects.SetMotion(reducedMotion);
            if (reducedMotion)
            {
                foreach (var p in popups)
                {
                    p.ReducedMotion = true; p.Ring.enabled = false; p.Core.enabled = false;
                    p.Age = Mathf.Max(0, p.Age); p.Root.anchoredPosition = p.Origin;
                    if (p.Active) p.Alpha.alpha = Mathf.Clamp01(1 - p.Age / .55f);
                    foreach (var spark in p.Sparks) spark.gameObject.SetActive(false);
                }
                celebration.gameObject.SetActive(false);
                TickJuice(0);
            }
            Sound.GetComponentInChildren<TMP_Text>().text = "SOUND  " + (sound ? "ON" : "OFF");
            Motion.GetComponentInChildren<TMP_Text>().text = "REDUCED MOTION  " + (reducedMotion ? "ON" : "OFF");
            Haptics.GetComponentInChildren<TMP_Text>().text = "VIBRATION  " + (haptics ? "ON" : "OFF");
        }
        public void Pop(Vector2 position, int points, Color tint, bool reducedMotion, float delay = 0, float blastScale = 1, int reactionStyle = 0)
        {
            var p = popups[popupCursor++ % popups.Length]; p.Age = reducedMotion ? 0 : -delay; p.Active = true;
            p.ReducedMotion = reducedMotion; p.BlastScale = blastScale; p.ReactionStyle = reactionStyle; p.Origin = position; p.Root.anchoredPosition = position;
            p.Label.text = "+" + points;
            p.Ring.color = new Color(tint.r, tint.g, tint.b, tint.a * (blastScale > 1 ? .55f : 1));
            p.RingRect.localScale = Vector3.one;
            p.Ring.enabled = !reducedMotion; p.Alpha.alpha = p.Age < 0 ? 0 : 1; p.Root.gameObject.SetActive(true);
            p.Core.enabled = !reducedMotion; p.Core.color = tint; p.Core.rectTransform.localScale = Vector3.one;
            p.Ring.enabled = false; // The shrinking tap core leads the ring burst.
            for (int i = 0; i < p.Sparks.Length; i++) { p.Sparks[i].gameObject.SetActive(!reducedMotion && reactionStyle == 0); p.Sparks[i].anchoredPosition = Vector2.zero; p.Sparks[i].localScale = Vector3.one; }
        }
        public void MessagePop(Vector2 position, string text, Color tint, bool reducedMotion)
        {
            Pop(position, 0, tint, reducedMotion);
            popups[(popupCursor - 1) % popups.Length].Label.text = text;
            popups[(popupCursor - 1) % popups.Length].Core.enabled = false;
        }
        public void ClearFeedback() { WorldEffects.Clear(); foreach (var p in popups) { p.Active = false; p.Root.gameObject.SetActive(false); } popupCursor = 0; ResetJuice(); }
        private void Update() { TickFeedback(Time.unscaledDeltaTime); }
        private void TickFeedback(float delta)
        {
            TickJuice(delta);
            tapHand.Tick(delta, animateFeedback, reducedMotion);
            if (results.activeSelf) resultReveal.Tick(delta, reducedMotion);
            if (!animateFeedback) return;
            WorldEffects.Tick(delta);
            for (int i = 0; i < popups.Length; i++)
            {
                var p = popups[i]; if (!p.Active) continue;
                p.Age += delta; float t = p.Age / .55f;
                if (p.Age < 0) continue;
                if (t >= 1) { p.Active = false; p.Root.gameObject.SetActive(false); continue; }
                p.Alpha.alpha = 1 - t;
                p.Ring.enabled = !p.ReducedMotion && p.ReactionStyle == 0 && p.Age >= .08f;
                if (!p.ReducedMotion) { p.Root.anchoredPosition = p.Origin + Vector2.up * (t * popupRise); p.RingRect.localScale = Vector3.one * (1 + t * .8f) * p.BlastScale; }
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

        [SerializeField] private Button prevWorldBtn, nextWorldBtn;
        [SerializeField] private Image prevWorldImg, nextWorldImg;
        [SerializeField] private TMP_Text prevWorldText, nextWorldText;
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


            if (worldLabel != null)
                worldLabel.text = currentLevelPage == 1 ? "WORLD 02   /   COMBO CITY" : "WORLD 01   /   LEARN TO TAP";
            selectionWorld.text = currentLevelPage == 1 ? "WORLD 02" : "WORLD 01";
            selectionTitle.text = currentLevelPage == 1 ? "COMBO CITY" : "LEARN TO TAP";

            if (selectionWallet != null)
            {
                selectionWallet.text = ProgressStore.Coins.ToString("N0") + " COINS   /   " + ProgressStore.XP.ToString("N0") + " XP\nPLAYER LEVEL " + (1 + ProgressStore.XP / 100);
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
