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
        [field: SerializeField] public Button Next { get; private set; }
        [field: SerializeField] public Button Shield { get; private set; }
        [SerializeField] private TMP_Text shieldLabel, mechanicStatus;
        [field: SerializeField] public Button IntroMenu { get; private set; }
        [field: SerializeField] public Button PauseMenu { get; private set; }
        [field: SerializeField] public Button ResultMenu { get; private set; }
        [field: SerializeField] public Button LevelOne { get; private set; }
        [field: SerializeField] public Button LevelTwo { get; private set; }
        [SerializeField] private GameObject levelSelect, introHero, sizePreview;
        [SerializeField] private ScrollRect selectionScroll;
        [SerializeField] private GameObject hintPlate;
        [SerializeField] private TMP_Text worldLabel, levelLabel, description, countdownGoal, selectionWallet, selectionExperience;
        [SerializeField] private TMP_Text introWorld, selectionWorld, selectionTitle;
        [Tooltip("'LEVEL NN' label inside the header chip; worldLabel holds the level name.")]
        [SerializeField] private TMP_Text levelChip;
        private LevelConfig currentConfig;
        [SerializeField] private GameObject intro, pausePanel, results, countdownPanel;
        [SerializeField] private TMP_Text score, timer, objective, combo, multiplier, hint, subhint, countdownText;
        [SerializeField] private TMP_Text introName, introGoal, introBest, starRules, resultTitle, resultSubtitle, resultScore, resultStats, resultBest, resultHint;
        [SerializeField] private Image progressFill, timerFill;
        [Tooltip("Glow and shine that stretch with the objective fill (anchorMax.x = progress), and the glowing head at its leading edge.")]
        [SerializeField] private RectTransform[] progressFollowers = new RectTransform[0];
        [SerializeField] private RectTransform progressHead;
        [Tooltip("How quickly the objective bar eases toward its value (higher = snappier).")]
        [SerializeField] private float progressEase = 9;
        [SerializeField] private UIGradient progressFillGradient;
        [SerializeField] private Image progressGlow, hintEdge, hintFill;
        private bool progressGoalMet;
        private float progressShown = -1, progressTarget, headClock;
        private int earnedStars;
        private readonly float[] starPop = { 1, 1, 1 };
        [SerializeField] private Image[] objectiveStars = new Image[0];
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
        [Tooltip("Coloured chips behind the results outcome and personal-best labels; three star icons on the briefing best pill.")]
        [SerializeField] private Image resultOutcomeChip, resultBestChip;
        [SerializeField] private Image[] introBestStars = new Image[3];
        [Header("Results visuals")]
        [Tooltip("Gradient on the star stage (top of the score card); warm for a win, slate for a miss.")]
        [SerializeField] private UIGradient resultStageGradient;
        [SerializeField] private Image resultStageGlow;
        [Tooltip("Sunburst behind the stars. It sits on its own nested Canvas so its slow spin never re-batches the page.")]
        [SerializeField] private RectTransform resultRays;
        [Tooltip("Three stat tiles under the score card (value + label).")]
        [SerializeField] private TMP_Text[] resultStatTiles = new TMP_Text[3];
        [SerializeField] private TMP_Text rewardCoins, rewardXP;
        [SerializeField] private GameObject rewardChips;
        [SerializeField] private Image resultHintChip;
        [Header("Feedback distances in Canvas reference pixels")]
        [SerializeField] private float popupRise = 48, sparkStartRadius = 33, sparkTravel = 127.5f, previewGlowPadding = 120;
        [Tooltip("Soft plate drawn behind callout words (CHAIN!, MEGA CHAIN!, combo milestones).")]
        [SerializeField] private Sprite calloutGlow;
        [Tooltip("Height of callout words above the arena centre.")]
        [SerializeField] private float calloutHeight = 150;
        private Sprite coreSprite;
        public void Initialize()
        {
            resultReveal = new ResultReveal(resultScore, resultBest, resultStars, rewardGroup);
            coreSprite = popups[0].Core.sprite;
            foreach (var p in popups) p.LabelHome = p.Label.rectTransform.anchoredPosition;
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
            [Tooltip("Star face drawn on the squashing ball (child of Core).")]
            public Image Face;
            public RectTransform[] Sparks;
            public CanvasGroup Alpha;
            public Vector2 Origin;
            public float Age, BlastScale = 1;
            public int ReactionStyle;
            public bool Active, ReducedMotion;
            // Runtime state only. Kind: 0 = target pop, 1 = message (MISS), 2 = callout word, 3 = bomb.
            [System.NonSerialized] public int Kind;
            [System.NonSerialized] public float Life, LabelScale, SizeScale;
            [System.NonSerialized] public Vector2 LabelHome;
        }

        private void HideOverlays()
        {
            resultReveal.Cancel();
            levelSelect.SetActive(false);
            intro.SetActive(false); pausePanel.SetActive(false); results.SetActive(false); countdownPanel.SetActive(false);
            Shield.gameObject.SetActive(false);
            mechanicStatus.gameObject.SetActive(false);
        }
        public void ShowIntro(LevelConfig config, int best)
        {
            HideOverlays(); intro.SetActive(true); Pause.interactable = false; animateFeedback = false;
            currentConfig = config;
            string levelName = TitleCase(config.DisplayName);
            introWorld.text = "Level " + config.LevelId.ToString("00");
            levelChip.text = "LEVEL " + config.LevelId.ToString("00");
            worldLabel.text = levelName;
            levelLabel.text = config.LevelId.ToString("00");
            countdownGoal.text = config.GoalText;
            introHero.SetActive(config.LevelId != 2); sizePreview.SetActive(config.LevelId == 2);
            // How to do it, then when you win: the same two lines on every level.
            description.text = config.MechanicHint + "\n<color=#6B7190>" + config.WinRule + "</color>";
            for (int i = 0; i < 3; i++)
            {
                ((RectTransform)sizePreview.transform.GetChild(i * 2)).sizeDelta = Vector2.one * (config.SizeAt(i) + previewGlowPadding);
                ((RectTransform)sizePreview.transform.GetChild(i * 2 + 1)).sizeDelta = Vector2.one * config.SizeAt(i);
            }
            introName.text = levelName;
            introGoal.text = config.LevelId == 1 ? "Tap <color=#5B6CFF>" + config.RequiredTaps + "</color> targets in <color=#5B6CFF>" + config.Duration.ToString("0") + "</color> seconds" : config.GoalText;
            countdownGoal.text = introGoal.text;
            if (config.FirstTapTutorial) description.text = config.MechanicHint + "\n<color=#6B7190>Your first tap starts the clock. " + config.WinRule + "</color>";
            starRules.text = "1 STAR   " + config.OneStarRule + "\n2 STARS   " + config.TwoStarRule + "\n3 STARS   " + config.ThreeStarRule;
            rewardPreview.text = "COMPLETE:  +" + config.CoinReward + " COINS   +" + config.XPReward + " XP";
            int bestStars = ProgressStore.GetBestStars(config.LevelId);
            introBest.text = best > 0 ? "Personal best  <color=#1E2240>" + best.ToString("N0") + "</color>" : "No best yet  ·  set one!";
            for (int i = 0; i < introBestStars.Length; i++) introBestStars[i].color = i < bestStars ? Color.white : UIArt.Unearned;
            GetComponent<GuiBriefing>()?.Refresh(config);
            GetComponent<GuiArenaTheme>()?.Refresh(config.LevelId);
            GetComponent<GuiScreenVisibility>()?.Refresh();
        }
        // "FIRST SPARK" -> "First Spark", matching the theme's title-case headings.
        private static string TitleCase(string name) => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name.ToLowerInvariant());
        public void ShowCountdown() { HideOverlays(); countdownPanel.SetActive(true); Pause.interactable = false; animateFeedback = false; GetComponent<GuiScreenVisibility>()?.Refresh(); }
        public void SetCountdown(int count) { if (count == lastCountdown) return; lastCountdown = count; countdownText.text = count.ToString(); }
        public void ShowPlaying() { HideOverlays(); Pause.interactable = true; animateFeedback = true; GetComponent<GuiScreenVisibility>()?.Refresh(); }
        public void ShowPause() { HideOverlays(); pausePanel.SetActive(true); Pause.interactable = false; animateFeedback = false; GetComponent<GuiScreenVisibility>()?.Refresh(); }
        // Hint tone by message: rewards read green, misses rose, everything else neutral lavender.
        // Frequent "NICE TAP" stays neutral so the card does not flicker on every tap.
        private static readonly string[] successHints = { "GOAL MET", "GOLDEN", "TIME BUBBLE", "CHAIN", "MEGA CHAIN", "SHIELD BLOCKED" };
        private static readonly string[] missHints = { "TARGET MISSED", "BOMB HIT" };
        private static bool HintStarts(string title, string[] prefixes) { foreach (var p in prefixes) if (title.StartsWith(p)) return true; return false; }
        public void SetHint(string title, string detail) {
            hint.text = title; subhint.text = detail;
            hintPlate.SetActive(!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(detail));
            int tone = HintStarts(title, missHints) ? 2 : HintStarts(title, successHints) ? 1 : 0;
            hint.color = tone == 2 ? new Color32(176, 36, 76, 255) : tone == 1 ? new Color32(20, 112, 64, 255) : UIArt.Ink;
            hintEdge.color = tone == 2 ? new Color32(242, 172, 192, 255) : tone == 1 ? new Color32(158, 220, 180, 255) : new Color32(222, 226, 245, 255);
            hintFill.color = tone == 2 ? new Color32(255, 233, 239, 255) : tone == 1 ? new Color32(228, 248, 235, 255) : new Color32(247, 245, 255, 255);
        }
        public void Refresh(RoundSession session, LevelConfig config)
        {
            WorldEffects.Configure(config.LevelId, session.ShieldActive);
            score.text = session.Score.ToString("D5"); timer.text = Mathf.CeilToInt(session.RemainingTime).ToString("00");
            timerFill.fillAmount = session.RemainingTime / config.Duration;
            timerFill.color = session.RemainingTime <= 10 ? UIArt.Warning : UIArt.Primary;
            objective.text = config.ObjectiveLabel + "  " + session.ObjectiveProgress + " / " + session.ObjectiveTarget;
            if (session.BombObjectiveFailed) objective.text = "BOMB HIT - REPLAY TO RETRY";
            // The bar eases toward its target during play (TickProgress); outside play, with reduced
            // motion, or when a new round resets it, it shows the value at once.
            progressTarget = Mathf.Clamp01((float)session.ObjectiveProgress / session.ObjectiveTarget);
            if (session.State != RoundState.Playing || reducedMotion || progressTarget < progressShown) ShowProgress(progressTarget);
            // Goal met: the bar turns success-green ("done — now chase stars"); next round restores warm.
            bool goalMet = session.GoalMet && !session.BombObjectiveFailed;
            if (goalMet != progressGoalMet)
            {
                progressGoalMet = goalMet;
                progressFillGradient.Set(goalMet ? new Color32(74, 222, 128, 255) : new Color32(255, 196, 70, 255), goalMet ? new Color32(22, 170, 90, 255) : new Color32(255, 122, 92, 255), false);
                progressGlow.color = goalMet ? new Color(60 / 255f, 200 / 255f, 120 / 255f, .38f) : new Color(1, 150 / 255f, 90 / 255f, .38f);
            }
            int projectedStars = !session.GoalMet ? 0 : session.Score < config.TwoStarScore ? 1 : session.Misses == 0 && session.BombHits == 0 && session.BestCombo >= config.ThreeStarCombo ? 3 : 2;
            for (int i = 0; i < objectiveStars.Length; i++)
            {
                bool earned = i < projectedStars;
                objectiveStars[i].color = earned ? Color.white : UIArt.Unearned;
                if (earned && i >= earnedStars && i < starPop.Length && session.State == RoundState.Playing) starPop[i] = 0; // Pop newly earned stars.
            }
            earnedStars = projectedStars;
            combo.text = session.Combo.ToString(); multiplier.text = "x" + session.Multiplier.ToString("0.##");
            bool playing = session.State == RoundState.Playing;
            Shield.gameObject.SetActive(playing && config.ShieldCharges > 0);
            Shield.interactable = playing && !session.ShieldActive && session.ShieldsRemaining > 0;
            shieldActive = session.ShieldActive;
            shieldLabel.text = session.ShieldActive ? "SHIELD ON" : session.ShieldsRemaining > 0 ? "USE SHIELD" : "SHIELD USED";
            mechanicStatus.gameObject.SetActive(playing && config.LevelId > 10);
            mechanicStatus.text = "CHAINS " + session.Chains + "   MEGA " + session.MegaChains + "   BOMBS " + session.BombHits +
                (config.RequiredCombo > 0 ? "   BEST " + session.BestCombo + "/" + config.RequiredCombo : "");
        }
        public void ShowResults(RoundSession session, bool newBest, int best, ProgressStore.RewardReceipt receipt)
        {
            HideOverlays(); results.SetActive(true); Pause.interactable = false; animateFeedback = false; ClearFeedback();
            SetHint("", "");
            HideTapHand();
            resultTitle.text = session.Won ? (session.Stars == 3 ? "Brilliant!" : "Well done!") : "So close!";
            SetLabelColors(resultTitle, session.Won ? new Color32(64, 76, 230, 255) : new Color32(46, 52, 92, 255),
                session.Won ? new Color32(150, 72, 224, 255) : new Color32(98, 92, 150, 255));
            // Star stage: deep indigo to violet for a win, calm slate for a miss.
            resultStageGradient.Set(session.Won ? new Color32(92, 106, 255, 255) : new Color32(112, 122, 168, 255),
                session.Won ? new Color32(160, 88, 236, 255) : new Color32(140, 128, 186, 255), true);
            resultStageGlow.enabled = session.Won;
            resultRays.GetComponent<Image>().color = new Color(1, 1, 1, session.Won ? .24f : .1f);
            resultSubtitle.text = session.Won ? "Level " + session.LevelId.ToString("00") + " complete" : session.BombObjectiveFailed ? "Bomb hit  ·  objective missed" : "Time up  ·  objective missed";
            // Outcome chip: green for a win, rose for a miss (text keeps 4.5:1 on its chip).
            resultOutcomeChip.color = session.Won ? new Color32(220, 246, 229, 255) : new Color32(255, 226, 233, 255);
            resultSubtitle.color = session.Won ? new Color32(20, 112, 64, 255) : new Color32(176, 36, 76, 255);
            bool hasNext = session.Won && session.LevelId < ProgressStore.LevelCount && ProgressStore.IsUnlocked(session.LevelId + 1);
            Next.gameObject.SetActive(hasNext);
            Replay.GetComponentInChildren<TMP_Text>().text = hasNext ? "Replay" : "Play again";
            for (int i = 0; i < 3; i++) resultStars[i].color = i < session.Stars ? Color.white : UIArt.Unearned;
            resultScore.text = session.Score.ToString("N0");
            SetLabelColors(resultScore, new Color32(40, 46, 112, 255), new Color32(112, 72, 214, 255));
            resultBest.text = newBest ? "New personal best!" : "Personal best  " + best.ToString("N0");
            resultBestChip.color = newBest ? new Color32(255, 239, 196, 255) : new Color32(238, 235, 255, 255);
            resultBest.color = newBest ? new Color32(160, 84, 0, 255) : new Color32(72, 80, 112, 255);
            // Three stat tiles: big coloured value over a short label (World 2 adds a second stat to each).
            resultStats.text = "This round";
            bool chains = session.LevelId > 10;
            StatTile(0, chains ? session.Chains : session.Hits, chains ? "Chains  ·  Mega " + session.MegaChains : "Targets", "3B5BDB");
            StatTile(1, session.BestCombo, chains ? "Best combo  ·  Missed " + session.Misses : "Best combo", "D9600F");
            StatTile(2, chains ? session.BombHits : session.Misses, chains ? "Bombs  ·  Shields " + session.ShieldBlocks : "Missed",
                (chains ? session.BombHits : session.Misses) == 0 ? "16834A" : "C2264F");
            resultHint.text = !session.Won ? "Tap targets as they appear.\nYou can try again immediately." : session.Stars == 3 ? "Mastery achieved! All three stars earned!" : "Chase three stars: keep a clean streak\nand aim for the score goal.";
            if (hasNext) resultHint.text = "LEVEL " + (session.LevelId + 1) + " UNLOCKED\nGet ready for the next challenge!";
            else if (session.Won && session.LevelId == ProgressStore.LevelCount) resultHint.text = "ALL LEVELS COMPLETE!\nReplay levels to earn every star.";
            rewardEarned.text = session.Won ? "Rewards earned" : "Complete the level to earn rewards";
            rewardChips.SetActive(session.Won);
            rewardCoins.text = "+" + receipt.Coins.ToString("N0"); rewardXP.text = "+" + receipt.XP.ToString("N0") + " XP";
            rewardBalance.text = "Wallet  <color=#1E2240><b>" + UIArt.Compact(receipt.TotalCoins) + "</b></color> coins   ·   Total  <color=#1E2240><b>" + UIArt.Compact(receipt.TotalXP) + "</b></color> XP";
            bool leveledUp = receipt.TotalXP / 100 > (receipt.TotalXP - receipt.XP) / 100;
            playerProgress.text = (leveledUp ? "Level up!  Player level " : "Player level ") + receipt.PlayerLevel + "   ·   " + (receipt.TotalXP % 100) + " / 100 XP";
            xpFill.rectTransform.anchorMax = new Vector2(receipt.TotalXP % 100 / 100f, 1);
            // Hint chip: green when a level unlocks, gold for mastery, lavender otherwise.
            bool unlocked = hasNext || (session.Won && session.LevelId == ProgressStore.LevelCount), mastery = session.Won && session.Stars == 3 && !unlocked;
            resultHintChip.color = unlocked ? new Color32(222, 247, 231, 255) : mastery ? new Color32(255, 241, 204, 255) : new Color32(238, 235, 255, 255);
            resultHint.color = unlocked ? new Color32(20, 112, 64, 255) : mastery ? new Color32(150, 84, 0, 255) : new Color32(72, 80, 112, 255);
            resultReveal.Begin(session.Score, session.Stars, reducedMotion);
            Celebrate(session.Won);
            GetComponent<GuiScreenVisibility>()?.Refresh();
        }
        private void StatTile(int index, int value, string label, string hex)
        {
            resultStatTiles[index].text = "<size=150%><b><color=#" + hex + ">" + value.ToString("N0") + "</color></b></size>\n<color=#5E6488>" + label + "</color>";
        }
        public void SetSettings(bool sound, bool reducedMotion)
        {
            this.reducedMotion = reducedMotion;
            WorldEffects.SetMotion(reducedMotion);
            if (reducedMotion)
            {
                foreach (var p in popups)
                {
                    p.ReducedMotion = true; p.Ring.enabled = false; p.Core.enabled = p.Face.enabled = false;
                    p.Age = Mathf.Max(0, p.Age); p.Root.anchoredPosition = p.Origin;
                    if (p.Active) p.Alpha.alpha = Mathf.Clamp01(1 - p.Age / .55f);
                    foreach (var spark in p.Sparks) spark.gameObject.SetActive(false);
                }
                celebration.gameObject.SetActive(false);
                TickJuice(0);
            }
            // Setting rows: name left, state in a right-hand column.
            Sound.GetComponentInChildren<TMP_Text>().text = "Sound<pos=78%>" + (sound ? "On" : "Off");
            Motion.GetComponentInChildren<TMP_Text>().text = "Reduced motion<pos=78%>" + (reducedMotion ? "On" : "Off");
        }
        /// <summary>Target pop: the ball squashes and bursts (body sprite), a ring and candy sparks fly out,
        /// and the score bounces in, then floats away. Pass the tapped target so the burst matches its look.</summary>
        public void Pop(Vector2 position, int points, Color tint, bool reducedMotion, float delay = 0, float blastScale = 1, int reactionStyle = 0,
            TargetView source = null, float labelScale = 1)
        {
            Sprite body = source != null ? source.Body : null, face = source != null ? source.Symbol : null;
            float diameter = source != null ? source.Diameter : 0;
            var p = popups[popupCursor++ % popups.Length]; p.Age = reducedMotion ? 0 : -delay; p.Active = true;
            p.ReducedMotion = reducedMotion; p.BlastScale = blastScale; p.ReactionStyle = reactionStyle; p.Origin = position; p.Root.anchoredPosition = position;
            p.Kind = 0; p.Life = .7f; p.LabelScale = labelScale; p.SizeScale = diameter > 0 ? diameter / 150 : 1;
            p.Label.text = "+" + points;
            SetLabelColors(p.Label, Color.white, Color.Lerp(tint, Color.white, .55f));
            p.Label.rectTransform.anchoredPosition = p.LabelHome; p.Label.rectTransform.localScale = Vector3.one * labelScale;
            p.Label.rectTransform.localEulerAngles = Vector3.zero;
            // Ring and sparks lean toward white so they read on an arena of the same hue.
            p.Ring.color = Color.Lerp(tint, Color.white, .45f);
            p.RingRect.localScale = Vector3.one * p.SizeScale;
            p.Alpha.alpha = p.Age < 0 ? 0 : 1; p.Root.gameObject.SetActive(true);
            p.Core.enabled = !reducedMotion; p.Core.sprite = body != null ? body : coreSprite;
            p.Core.color = body != null ? Color.white : tint; p.Core.rectTransform.localScale = Vector3.one;
            p.Core.rectTransform.sizeDelta = Vector2.one * (body != null && diameter > 0 ? diameter : 90);
            p.Face.enabled = !reducedMotion && face != null; p.Face.sprite = face; p.Face.color = Color.white;
            p.Ring.enabled = false; // The squashing ball leads the ring burst.
            Color shard = new Color(1, .84f, .36f); // The star shatters into golden sparkles.
            for (int i = 0; i < p.Sparks.Length; i++)
            {
                p.Sparks[i].gameObject.SetActive(!reducedMotion && reactionStyle == 0);
                p.Sparks[i].anchoredPosition = Vector2.zero; p.Sparks[i].localScale = Vector3.one;
                p.Sparks[i].GetComponent<Image>().color = i % 2 == 0 ? shard : new Color(1, 1, 1, .9f);
            }
        }
        public void MessagePop(Vector2 position, string text, Color tint, bool reducedMotion)
        {
            Pop(position, 0, tint, reducedMotion);
            var p = popups[(popupCursor - 1) % popups.Length];
            p.Kind = 1; p.Label.text = text; p.Core.enabled = p.Face.enabled = false;
            SetLabelColors(p.Label, Color.white, Color.Lerp(tint, Color.white, .35f));
            foreach (var spark in p.Sparks) spark.gameObject.SetActive(false);
        }
        /// <summary>Bomb tap: the bomb swells and heats up before the blast, or shrinks into the shield when
        /// blocked; the penalty (or BLOCKED!) text shakes in.</summary>
        public void HazardPop(Vector2 position, string text, Color tint, bool reducedMotion, TargetView bomb, bool blocked)
        {
            MessagePop(position, text, tint, reducedMotion);
            var p = popups[(popupCursor - 1) % popups.Length];
            p.Kind = 3; p.Life = .85f; p.LabelScale = 1.2f; p.ReactionStyle = blocked ? 1 : 0;
            p.Core.enabled = !reducedMotion; p.Core.sprite = bomb.Body; p.Core.color = Color.white;
            p.Core.rectTransform.sizeDelta = Vector2.one * bomb.Diameter; p.Core.rectTransform.localScale = Vector3.one;
        }
        /// <summary>Big word over the arena (CHAIN!, MEGA CHAIN!, combo milestones) on a soft dark plate.</summary>
        public void Callout(string text, Color accent, float delay = 0)
        {
            var p = popups[popupCursor++ % popups.Length]; p.Age = reducedMotion ? 0 : -delay; p.Active = true;
            p.ReducedMotion = reducedMotion; p.ReactionStyle = 1; p.Kind = 2; p.Life = 1.05f; p.LabelScale = 1.55f; p.SizeScale = 1;
            p.Origin = new Vector2(0, calloutHeight); p.Root.anchoredPosition = p.Origin;
            p.Label.text = text; SetLabelColors(p.Label, new Color(1, .98f, .9f), Color.Lerp(accent, Color.white, .15f));
            p.Label.rectTransform.anchoredPosition = Vector2.zero; p.Label.rectTransform.localScale = Vector3.one * p.LabelScale;
            p.Label.rectTransform.localEulerAngles = Vector3.zero;
            p.Alpha.alpha = p.Age < 0 ? 0 : 1; p.Root.gameObject.SetActive(true);
            p.Ring.enabled = false;
            p.Face.enabled = false;
            p.Core.enabled = true; p.Core.sprite = calloutGlow; p.Core.color = new Color(.16f, .07f, .36f, .55f);
            p.Core.rectTransform.sizeDelta = new Vector2(620, 230); p.Core.rectTransform.localScale = Vector3.one;
            foreach (var spark in p.Sparks) spark.gameObject.SetActive(false);
        }
        private static void SetLabelColors(TMP_Text label, Color top, Color bottom)
        {
            label.color = Color.white; label.enableVertexGradient = true;
            label.colorGradient = new VertexGradient(top, top, bottom, bottom);
        }
        // Back-out ease: rises past 1 by about 7% and settles (bouncy pop-in).
        private static float BackOut(float t) { t -= 1; return 1 + 2.4f * t * t * t + 1.4f * t * t; }
        public void ClearFeedback() { WorldEffects.Clear(); foreach (var p in popups) { p.Active = false; p.Root.gameObject.SetActive(false); } popupCursor = 0; ResetJuice(); }
        private void Update() { TickFeedback(Time.unscaledDeltaTime); }
        // Fill, follower glow/shine and head share one value; only touched when it changes.
        private void ShowProgress(float progress)
        {
            if (Mathf.Approximately(progress, progressShown)) return;
            progressShown = progress;
            progressFill.fillAmount = progress;
            foreach (var follower in progressFollowers) { follower.anchorMax = new Vector2(progress, follower.anchorMax.y); follower.gameObject.SetActive(progress > 0); }
            progressHead.anchorMin = new Vector2(progress, progressHead.anchorMin.y); progressHead.anchorMax = new Vector2(progress, progressHead.anchorMax.y);
            progressHead.gameObject.SetActive(progress > 0);
        }
        private void TickProgress(float delta)
        {
            if (progressShown != progressTarget)
                ShowProgress(Mathf.Abs(progressTarget - progressShown) < .002f ? progressTarget : Mathf.Lerp(progressShown, progressTarget, 1 - Mathf.Exp(-progressEase * delta)));
            if (reducedMotion) return;
            if (progressHead.gameObject.activeSelf) { headClock += delta; progressHead.localScale = Vector3.one * (1 + .05f * Mathf.Sin(headClock * 3.5f)); }
            for (int i = 0; i < starPop.Length && i < objectiveStars.Length; i++)
            {
                if (starPop[i] >= 1) continue;
                starPop[i] = Mathf.Min(1, starPop[i] + delta / .4f);
                float t = starPop[i];
                objectiveStars[i].rectTransform.localScale = Vector3.one * (1 + .4f * Mathf.Sin(t * Mathf.PI) * (1 - t));
            }
        }
        private void TickFeedback(float delta)
        {
            TickJuice(delta);
            tapHand.Tick(delta, animateFeedback, reducedMotion);
            if (results.activeSelf)
            {
                resultReveal.Tick(delta, reducedMotion);
                if (!reducedMotion) resultRays.Rotate(0, 0, -6 * delta);
            }
            if (!animateFeedback) return;
            TickProgress(delta);
            WorldEffects.Tick(delta);
            for (int i = 0; i < popups.Length; i++)
            {
                var p = popups[i]; if (!p.Active) continue;
                p.Age += delta; float t = p.Age / p.Life;
                if (p.Age < 0) continue;
                if (t >= 1) { p.Active = false; p.Root.gameObject.SetActive(false); continue; }
                // Hold full strength, then fade over the last 40%.
                p.Alpha.alpha = p.ReducedMotion ? 1 - t : t < .6f ? 1 : 1 - (t - .6f) / .4f;
                p.Ring.enabled = !p.ReducedMotion && p.Kind == 0 && p.ReactionStyle == 0 && p.Age >= .06f;
                if (p.ReducedMotion) continue;
                AnimatePopup(p, i, t);
            }
        }
        private void AnimatePopup(Popup p, int index, float t)
        {
            var label = p.Label.rectTransform;
            if (p.Kind == 2)
            {
                // Callout: springs in from small, sways once, drifts up a little.
                float pop = BackOut(Mathf.Clamp01(p.Age / .28f));
                label.localScale = Vector3.one * (p.LabelScale * Mathf.LerpUnclamped(.25f, 1, pop));
                label.localEulerAngles = new Vector3(0, 0, Mathf.Sin(p.Age * 9) * 4 * (1 - t));
                label.anchoredPosition = Vector2.up * (popupRise * .5f * t);
                p.Core.rectTransform.localScale = new Vector3(Mathf.LerpUnclamped(.4f, 1, pop), Mathf.LerpUnclamped(.6f, 1, pop), 1);
                return;
            }
            // Score text: bouncy pop-in, then an ease-out float upward.
            float rise = 1 - (1 - t) * (1 - t);
            label.localScale = Vector3.one * (p.LabelScale * Mathf.LerpUnclamped(.3f, 1, BackOut(Mathf.Clamp01(p.Age / .2f))));
            label.anchoredPosition = p.LabelHome + Vector2.up * (popupRise * 1.5f * rise);
            if (p.Kind == 3)
            {
                // Shaky penalty text; the bomb swells white-hot for the fuse time, or implodes into the shield.
                label.localEulerAngles = new Vector3(0, 0, Mathf.Sin(p.Age * 45) * 7 * (1 - t));
                bool blocked = p.ReactionStyle == 1;
                float f = p.Age / (blocked ? .22f : World2Effects.BombFuseSeconds);
                p.Core.rectTransform.localScale = f >= 1 ? Vector3.zero : Vector3.one * (blocked ? 1 - f * f : 1 + .3f * f);
                p.Core.color = blocked ? Color.Lerp(Color.white, UIArt.Cyan, f) : Color.Lerp(Color.white, new Color(1, .75f, .6f), f);
                return;
            }
            if (p.Kind != 0) return;
            // Ball: quick squash (wide and short), then swells and fades as it bursts.
            float c = p.Age / .17f;
            p.Core.rectTransform.localScale = c >= 1 ? Vector3.zero : c < .45f
                ? new Vector3(1 + .18f * Mathf.Sin(c / .45f * Mathf.PI * .5f), 1 - .16f * Mathf.Sin(c / .45f * Mathf.PI * .5f), 1)
                : Vector3.one * Mathf.Lerp(1.12f, 1.4f, (c - .45f) / .55f);
            var core = p.Core.color; core.a = c < .45f ? 1 : Mathf.Clamp01(1 - (c - .45f) / .55f); p.Core.color = core;
            p.Face.color = new Color(1, 1, 1, core.a);
            // Ring: fast ease-out shockwave that thins as it fades.
            float r = Mathf.Clamp01((p.Age - .06f) / .42f), ringEase = 1 - (1 - r) * (1 - r) * (1 - r);
            p.RingRect.localScale = Vector3.one * (Mathf.Lerp(.55f, 1.75f, ringEase) * p.BlastScale * p.SizeScale);
            var ring = p.Ring.color; ring.a = (p.BlastScale > 1 ? .6f : .9f) * Mathf.Pow(1 - r, 1.5f); p.Ring.color = ring;
            // Candy sparks: burst out on arcs, fall with gravity and spin; streaks point along their path.
            float s = Mathf.Clamp01(p.Age / .62f), travel = 1 - (1 - s) * (1 - s);
            for (int j = 0; j < p.Sparks.Length; j++)
            {
                float angle = j * Mathf.PI * 2 / p.Sparks.Length + index * .9f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float reach = (sparkStartRadius + sparkTravel * (j % 3 == 0 ? 1.25f : 1) * travel) * p.SizeScale;
                p.Sparks[j].anchoredPosition = direction * reach + Vector2.down * (150 * s * s);
                p.Sparks[j].localEulerAngles = new Vector3(0, 0, j % 2 == 0 ? j * 45 + s * (j % 4 == 0 ? 240 : -240) : angle * Mathf.Rad2Deg - 90);
                p.Sparks[j].localScale = Vector3.one * ((j % 2 == 0 ? 1.6f : 1.25f) * Mathf.Pow(1 - s, .8f));
            }
        }
        public void ShowTapHand() { tapHand.Show(); tapHand.Tick(0, animateFeedback, reducedMotion); }
        public void HideTapHand() { tapHand.Hide(); }

        // One saved row per level (index = LevelId - 1) in a single scrolling list.
        [SerializeField] private Button[] levelButtons = new Button[20];
        [SerializeField] private TMP_Text[] selectionTexts = new TMP_Text[20];
        [Tooltip("Vertical distance between saved level rows, in Canvas reference pixels.")]
        [SerializeField] private float levelRowPitch = 160;
        private LevelConfig[] cachedConfigs;
        private System.Func<int, bool> cachedOnSelectLevel;

        public void ShowLevels(LevelConfig[] configs, System.Func<int, bool> onSelectLevel)
        {
            cachedConfigs = configs;
            cachedOnSelectLevel = onSelectLevel;
            HideOverlays(); levelSelect.SetActive(true); Pause.interactable = false; animateFeedback = false;
            GetComponent<GuiScreenVisibility>()?.Refresh();
            SetHint("", "");

            if (worldLabel != null) worldLabel.text = "Tap Blast";
            selectionWorld.text = "Tap Blast";
            selectionTitle.text = "Choose a Level";

            // Open with the current level near the top; earlier rows stay one scroll away.
            int selected = currentConfig != null ? currentConfig.LevelId : ProgressStore.SelectedLevel;
            float maxScroll = Mathf.Max(0, selectionScroll.content.rect.height - selectionScroll.viewport.rect.height);
            selectionScroll.StopMovement();
            selectionScroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp((selected - 2) * levelRowPitch, 0, maxScroll));

            if (selectionWallet != null)
            {
                selectionWallet.text = UIArt.Compact(ProgressStore.Coins) + "\n<size=58%><color=#6B7190>Coins</color></size>";
                selectionExperience.text = ProgressStore.XP.ToString("N0") + " XP\n<size=58%><color=#6B7190>Player level " + (1 + ProgressStore.XP / 100) + "</color></size>";
            }

            for (int i = 0; i < levelButtons.Length; i++)
            {
                int levelId = i + 1;
                LevelConfig cfg = null;
                if (cachedConfigs != null) foreach (var c in cachedConfigs) if (c != null && c.LevelId == levelId) cfg = c;
                string nameStr = cfg != null ? cfg.DisplayName : "LEVEL " + levelId.ToString("00");

                if (levelButtons[i] != null)
                {
                    TMP_Text mainText = levelButtons[i].GetComponentInChildren<TMP_Text>();
                    if (mainText != null) mainText.text = nameStr;

                    bool unlocked = cfg != null && ProgressStore.IsUnlocked(levelId);
                    int stars = ProgressStore.GetBestStars(levelId);
                    int score = ProgressStore.GetBestScore(levelId);

                    levelButtons[i].interactable = unlocked;
                    levelButtons[i].GetComponent<GuiLevelRow>()?.Refresh(levelId, stars);

                    if (selectionTexts[i] != null)
                    {
                        // All tones keep at least 4.5:1 contrast on the pastel row tiles.
                        if (stars > 0)
                        {
                            selectionTexts[i].text = "Completed  ·  Best " + score.ToString("N0");
                            selectionTexts[i].color = new Color32(72, 80, 112, 255);
                        }
                        else if (unlocked)
                        {
                            selectionTexts[i].text = "Ready to play";
                            selectionTexts[i].color = new Color32(70, 82, 220, 255);
                        }
                        else
                        {
                            selectionTexts[i].text = "Locked  ·  finish level " + (levelId - 1);
                            selectionTexts[i].color = new Color32(72, 86, 98, 255);
                        }
                    }

                    int id = levelId;
                    levelButtons[i].onClick.RemoveAllListeners();
                    levelButtons[i].onClick.AddListener(() => cachedOnSelectLevel(id));
                }
            }
        }

        public void ShowLevels() { ShowLevels(null, (id) => false); }
    }
}
