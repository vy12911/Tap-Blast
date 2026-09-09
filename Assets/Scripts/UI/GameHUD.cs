using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TapBlast
{
    /// <summary>Builds a self-contained portrait UI once. Text updates on events/seconds;
    /// target and feedback canvases isolate frequent geometry rebuilds.</summary>
    public sealed class GameHUD : MonoBehaviour
    {
        public UIArt Art { get; private set; }
        public RectTransform Arena { get; private set; }
        public Button Play { get; private set; }
        public Button Pause { get; private set; }
        public Button Resume { get; private set; }
        public Button Restart { get; private set; }
        public Button Replay { get; private set; }
        public Button Sound { get; private set; }
        public Button Motion { get; private set; }
        public Button Haptics { get; private set; }
        public Button Next { get; private set; }
        public Button IntroMenu { get; private set; }
        public Button PauseMenu { get; private set; }
        public Button ResultMenu { get; private set; }
        public Button LevelOne { get; private set; }
        public Button LevelTwo { get; private set; }
        private GameObject levelSelect, introHero, sizePreview;
        private Text worldLabel, levelLabel, description, countdownGoal, selectionWallet, selectionOne, selectionTwo;
        private LevelConfig currentConfig;
        private RectTransform safeArea, frame;
        private GameObject intro, pausePanel, results, countdownPanel;
        private Text score, timer, objective, combo, multiplier, hint, subhint, countdownText;
        private Text introName, introGoal, introBest, starRules, resultTitle, resultSubtitle, resultScore, resultStats, resultBest, resultHint;
        private Image progressFill, timerFill;
        private readonly Image[] resultStars = new Image[3];
        private readonly Popup[] popups = new Popup[10];
        private int popupCursor;
        private bool animateFeedback;
        private Rect lastSafe;
        private Vector2 lastScreen;
        private Vector2 lastCanvasSize;
        private int lastCountdown = -1;
        private TapHandHint tapHand;
        private ResultReveal resultReveal;
        private bool reducedMotion;
        private Text rewardPreview, rewardEarned, rewardBalance, playerProgress;
        private Image xpFill;
        private CanvasGroup rewardGroup;
        public bool TutorialHandVisible => tapHand.IsVisible;
        public bool ResultRevealComplete => resultReveal.IsComplete;
        private sealed class Popup
        {
            public RectTransform Root, RingRect;
            public Text Label;
            public Image Ring;
            public CanvasGroup Alpha;
            public Vector2 Origin;
            public float Age;
            public bool Active, ReducedMotion;
        }

        public static GameHUD Create(Transform parent)
        {
            var root = new GameObject("Tap Blast UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); scaler.matchWidthOrHeight = .5f;
            var hud = root.AddComponent<GameHUD>(); hud.Build(); return hud;
        }
        private RectTransform Box(string name, Transform parent, float x, float y, float w, float h) => Art.Fixed(name, parent, new Vector2(x, y), new Vector2(w, h));
        private Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size, Color color, bool bold = false) => Art.Label(Box(name, parent, x, y, w, h), text, size, color, TextAnchor.MiddleCenter, bold);
        private Button Button(Transform parent, string name, string text, float x, float y, float w, float h, bool primary = false) => Art.Button(Box(name, parent, x, y, w, h), text, primary ? UIArt.Cyan : new Color32(38, 64, 81, 255), primary ? UIArt.Ink : Color.white);

        private void Build()
        {
            Art = new UIArt();
            Art.Image(Art.Rect("Background", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), UIArt.Ink);
            safeArea = Art.Rect("Device safe area", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            frame = Box("Portrait frame", safeArea, 0, 0, 720, 1280);
            Art.Image(Box("Teal atmosphere", frame, -210, 470, 950, 950), new Color(.04f, .55f, .52f, .35f), Art.Glow);
            Art.Image(Box("Blue atmosphere", frame, 280, -360, 1050, 1050), new Color(.13f, .25f, .64f, .25f), Art.Glow);
            Label(frame, "Wordmark", "TAP <color=#FFCE58>BLAST</color>", 0, 582, 470, 66, 46, Color.white, true);
            worldLabel = Label(frame, "World", "WORLD 01   /   FIRST SPARK", 0, 528, 530, 32, 17, UIArt.Muted, true);
            var scoreCard = Box("Score card", frame, -212, 444, 220, 100);
            Art.Image(scoreCard, UIArt.Card, Art.Panel);
            Label(scoreCard, "Caption", "SCORE", 0, 26, 190, 26, 16, UIArt.Muted, true);
            score = Label(scoreCard, "Value", "00000", 0, -13, 200, 48, 36, Color.white, true);
            var levelCard = Box("Level card", frame, 0, 444, 160, 100);
            Label(levelCard, "Caption", "LEVEL", 0, 26, 150, 26, 16, UIArt.Muted, true);
            levelLabel = Label(levelCard, "Value", "01", 0, -12, 150, 50, 38, UIArt.Cyan, true);
            var timeCard = Box("Timer card", frame, 200, 444, 130, 118);
            Art.Image(Box("Track", timeCard, 0, 0, 118, 118), UIArt.Card, Art.Ring);
            timerFill = Art.Image(Box("Countdown ring", timeCard, 0, 0, 118, 118), UIArt.Cyan, Art.Ring);
            timerFill.type = Image.Type.Filled; timerFill.fillMethod = Image.FillMethod.Radial360; timerFill.fillOrigin = 2;
            timer = Label(timeCard, "Time", "60", 0, 7, 96, 48, 34, Color.white, true);
            Label(timeCard, "Caption", "SEC", 0, -25, 80, 23, 13, UIArt.Muted, true);
            Pause = Button(frame, "Pause button", "II", 302, 444, 52, 64);
            objective = Label(frame, "Objective", "TAP 20 TARGETS", 0, 351, 640, 32, 21, Color.white, true);
            var track = Box("Objective track", frame, 0, 318, 640, 8);
            Art.Image(track, UIArt.Card, Art.Panel);
            progressFill = Art.Image(Art.Rect("Progress", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), UIArt.Cyan, Art.Panel);

            Arena = Art.Rect("Target arena", frame, Vector2.zero, Vector2.one, new Vector2(24, 222), new Vector2(-24, -348));
            Art.Image(Arena, new Color32(12, 29, 43, 255), Art.Panel);
            Arena.gameObject.AddComponent<Canvas>(); Arena.gameObject.AddComponent<GraphicRaycaster>();
            for (int row = 0; row < 8; row++) for (int col = 0; col < 7; col++)
            {
                var dot = Art.Rect("Arena dot", Arena, new Vector2((col + .5f) / 7, (row + .5f) / 8), new Vector2((col + .5f) / 7, (row + .5f) / 8), Vector2.one * -1.5f, Vector2.one * 1.5f);
                Art.Image(dot, new Color(1, 1, 1, .06f), Art.Disc);
            }
            var feedback = Art.Rect("Pooled feedback", frame, Arena.anchorMin, Arena.anchorMax, Arena.offsetMin, Arena.offsetMax);
            feedback.gameObject.AddComponent<Canvas>();
            tapHand = new TapHandHint(Art, feedback);
            for (int i = 0; i < popups.Length; i++)
            {
                var p = new Popup(); p.Root = Box("Tap feedback " + i, feedback, 0, 0, 150, 150);
                p.Alpha = p.Root.gameObject.AddComponent<CanvasGroup>(); p.Alpha.blocksRaycasts = false;
                p.RingRect = Box("Burst", p.Root, 0, 0, 110, 110); p.Ring = Art.Image(p.RingRect, UIArt.Cyan, Art.Ring);
                p.Label = Label(p.Root, "Points", "+10", 0, 62, 170, 50, 28, Color.white, true);
                p.Root.gameObject.SetActive(false); popups[i] = p;
            }
            hint = Label(frame, "Tutorial hint", "TAP THE GLOWING TARGET", 0, -455, 660, 38, 23, UIArt.Cyan, true);
            subhint = Label(frame, "Tutorial detail", "Your first tap starts the clock", 0, -493, 660, 32, 18, UIArt.Muted);
            var comboCard = Box("Combo card", frame, 0, -574, 640, 94);
            Art.Image(comboCard, UIArt.Card, Art.Panel);
            Label(comboCard, "Combo caption", "COMBO", -220, 19, 160, 27, 15, UIArt.Muted, true);
            combo = Label(comboCard, "Combo value", "0", -220, -17, 160, 44, 32, Color.white, true);
            Label(comboCard, "Tip", "EVERY TAP COUNTS", 0, 0, 240, 40, 17, UIArt.Muted, true);
            multiplier = Label(comboCard, "Multiplier", "x1", 227, 0, 150, 52, 34, UIArt.Cyan, true);
            BuildIntro(); BuildPause(); BuildResults(); BuildLevels();
            var countdownRoot = Overlay("Countdown", out var countdownCard, 400, 340); countdownPanel = countdownRoot;
            Label(countdownCard, "Caption", "GET READY", 0, 90, 350, 40, 22, UIArt.Cyan, true);
            countdownText = Label(countdownCard, "Count", "3", 0, 0, 350, 140, 96, Color.white, true);
            countdownGoal = Label(countdownCard, "Goal", "Find your first spark", 0, -110, 350, 34, 20, UIArt.Muted);
            if (EventSystem.current == null)
            {
                var events = new GameObject("Tap Blast input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            HideOverlays(); UpdateSafeArea();
        }
        private GameObject Overlay(string name, out RectTransform card, float width, float height)
        {
            var overlay = Art.Rect(name, frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dim = Art.Image(overlay, new Color(0.015f, .035f, .06f, .91f)); dim.raycastTarget = true;
            card = Box(name + " card", overlay, 0, 0, width, height);
            Art.Image(card, UIArt.Card, Art.Panel); return overlay.gameObject;
        }
        private void BuildIntro()
        {
            intro = Overlay("Level briefing", out var card, 624, 850);
            Label(card, "Eyebrow", "LEARN TO TAP", 0, 353, 420, 36, 18, UIArt.Cyan, true);
            IntroMenu = Button(card, "Back to levels", "<", -248, 353, 60, 48);
            introName = Label(card, "Name", "FIRST SPARK", 0, 289, 560, 70, 43, Color.white, true);
            var ring = Box("Hero target", card, 0, 156, 140, 140);
            introHero = ring.gameObject;
            Art.Image(Box("Glow", ring, 0, 0, 240, 240), UIArt.Cyan, Art.Glow);
            Art.Image(ring, UIArt.Cyan, Art.Ring);
            Art.Image(Box("Core", ring, 0, 0, 111, 111), new Color(.2f, .9f, .85f, .18f), Art.Disc);
            sizePreview = Box("Target size preview", card, 0, 156, 540, 180).gameObject;
            for (int i = 0; i < 3; i++)
            {
                float size = i == 0 ? 100 : i == 1 ? 144 : 112;
                Art.Image(Box("Size glow", sizePreview.transform, (i - 1) * 168, 0, size + 80, size + 80), UIArt.Cyan, Art.Glow);
                Art.Image(Box("Normal size " + i, sizePreview.transform, (i - 1) * 168, 0, size, size), UIArt.Cyan, Art.Ring);
            }
            introGoal = Label(card, "Goal", "Tap 20 targets in 60 seconds", 0, 37, 560, 50, 27, Color.white, true);
            description = Label(card, "Description", "Tap glowing circles before they fade.\nKeep your streak to multiply your score.", 0, -36, 550, 75, 21, UIArt.Muted);
            starRules = Label(card, "Star rules", "", 0, -137, 530, 105, 19, UIArt.Muted);
            rewardPreview = Label(card, "Reward preview", "", 0, -209, 550, 27, 18, UIArt.Gold, true);
            Play = Button(card, "Start level", "LET'S PLAY", 0, -268, 520, 76, true);
            introBest = Label(card, "Best", "A new personal best starts here", 0, -348, 550, 38, 17, UIArt.Muted);
        }
        private void BuildPause()
        {
            pausePanel = Overlay("Paused", out var card, 624, 850);
            Label(card, "Eyebrow", "TAKE A BREATHER", 0, 300, 550, 36, 18, UIArt.Cyan, true);
            Label(card, "Title", "PAUSED", 0, 232, 550, 76, 48, Color.white, true);
            Label(card, "Detail", "Your time and targets are waiting.", 0, 152, 550, 45, 21, UIArt.Muted);
            Resume = Button(card, "Resume", "KEEP PLAYING", 0, 57, 520, 76, true);
            Restart = Button(card, "Restart", "RESTART LEVEL", 0, -39, 520, 70);
            Sound = Button(card, "Sound", "SOUND ON", 0, -136, 520, 62);
            Motion = Button(card, "Motion", "MOTION ON", 0, -215, 520, 62);
            Haptics = Button(card, "Vibration", "VIBRATION ON", 0, -294, 520, 62);
            PauseMenu = Button(card, "Exit to levels", "EXIT TO LEVEL SELECT", 0, -371, 520, 48);
        }
        private void BuildResults()
        {
            results = Overlay("Results", out var card, 624, 960);
            resultSubtitle = Label(card, "Eyebrow", "LEVEL 01 COMPLETE", 0, 402, 550, 38, 18, UIArt.Cyan, true);
            resultTitle = Label(card, "Title", "BRILLIANT!", 0, 330, 560, 72, 46, Color.white, true);
            for (int i = 0; i < 3; i++) resultStars[i] = Art.Image(Box("Star " + (i + 1), card, (i - 1) * 130, 216, 100, 100), UIArt.Gold, Art.Star);
            Label(card, "Score caption", "FINAL SCORE", 0, 108, 540, 30, 16, UIArt.Muted, true);
            resultScore = Label(card, "Score", "00000", 0, 48, 540, 88, 64, Color.white, true);
            resultBest = Label(card, "Personal best", "NEW PERSONAL BEST", 0, -20, 540, 34, 18, UIArt.Cyan, true);
            resultStats = Label(card, "Stats", "", 0, -100, 530, 90, 21, Color.white);
            var rewardCard = Box("Round rewards", card, 0, -213, 544, 119);
            Art.Image(rewardCard, new Color32(14, 33, 46, 255), Art.Panel);
            rewardGroup = rewardCard.gameObject.AddComponent<CanvasGroup>(); rewardGroup.blocksRaycasts = false;
            rewardEarned = Label(rewardCard, "Earned", "", 0, 35, 520, 30, 22, UIArt.Gold, true);
            rewardBalance = Label(rewardCard, "Balance", "", 0, 4, 520, 26, 16, UIArt.Muted);
            playerProgress = Label(rewardCard, "Player progress", "", 0, -21, 520, 25, 15, UIArt.Cyan, true);
            var xpTrack = Box("XP track", rewardCard, 0, -44, 485, 6);
            Art.Image(xpTrack, UIArt.Card, Art.Panel);
            xpFill = Art.Image(Art.Rect("XP progress", xpTrack, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), UIArt.Cyan, Art.Panel);
            resultHint = Label(card, "Mastery hint", "", 0, -309, 535, 48, 18, UIArt.Muted);
            Replay = Button(card, "Replay", "PLAY AGAIN", 0, -381, 520, 70, true);
            Next = Button(card, "Next level", "NEXT LEVEL", 135, -381, 250, 70, true);
            Next.gameObject.SetActive(false);
            ResultMenu = Button(card, "Result level select", "LEVEL SELECT", 0, -444, 520, 42);
            resultReveal = new ResultReveal(resultScore, resultBest, resultStars, rewardGroup);
        }
        private void HideOverlays()
        {
            resultReveal.Cancel();
            levelSelect.SetActive(false);
            intro.SetActive(false); pausePanel.SetActive(false); results.SetActive(false); countdownPanel.SetActive(false);
        }
        public void ShowIntro(LevelConfig config, int best)
        {
            HideOverlays(); intro.SetActive(true); Pause.interactable = false; animateFeedback = false;
            currentConfig = config;
            worldLabel.text = "WORLD 01   /   " + config.DisplayName;
            levelLabel.text = config.LevelId.ToString("00");
            countdownGoal.text = config.Objective == ObjectiveKind.Score ? "Reach " + config.TargetScore + " points" : "Find your first spark";
            introHero.SetActive(config.LevelId == 1); sizePreview.SetActive(config.LevelId == 2);
            description.text = config.Objective == ObjectiveKind.Score ? "Different sizes. Same " + config.BasePoints + " base points.\nKeep scoring until the timer runs out." : "Tap glowing circles before they fade.\nKeep your streak to multiply your score.";
            for (int i = 0; i < 3; i++)
            {
                ((RectTransform)sizePreview.transform.GetChild(i * 2)).sizeDelta = Vector2.one * (config.SizeAt(i) + 80);
                ((RectTransform)sizePreview.transform.GetChild(i * 2 + 1)).sizeDelta = Vector2.one * config.SizeAt(i);
            }
            introName.text = config.DisplayName;
            introGoal.text = config.GoalText;
            starRules.text = "1 STAR   Complete the level\n2 STARS   Complete + score " + config.TwoStarScore + "+\n3 STARS   Both + no misses + " + config.ThreeStarCombo + " combo";
            rewardPreview.text = "COMPLETE:  +" + config.CoinReward + " COINS   +" + config.XPReward + " XP";
            introBest.text = "WALLET  " + ProgressStore.Coins.ToString("N0") + " COINS   /   " + ProgressStore.XP.ToString("N0") + " XP\n" + (best > 0 ? "BEST  " + best.ToString("N0") + "   /   " + ProgressStore.GetBestStars(config.LevelId) + " STARS" : "A new personal best starts here");
            introBest.rectTransform.sizeDelta = new Vector2(550, 68);
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
            bool scoreGoal = config.Objective == ObjectiveKind.Score;
            objective.text = (scoreGoal ? "SCORE GOAL  " + session.Score + " / " + config.TargetScore : "TARGETS  " + session.Hits + " / " + config.RequiredTaps);
            progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(scoreGoal ? (float)session.Score / config.TargetScore : (float)session.Hits / config.RequiredTaps), 1);
            combo.text = session.Combo.ToString(); multiplier.text = "x" + session.Multiplier.ToString("0.##");
        }
        public void ShowResults(RoundSession session, bool newBest, int best, ProgressStore.RewardReceipt receipt)
        {
            HideOverlays(); results.SetActive(true); Pause.interactable = false; animateFeedback = false; ClearFeedback();
            SetHint("", "");
            HideTapHand();
            resultTitle.text = session.Won ? "BRILLIANT!" : "TRY AGAIN";
            resultSubtitle.text = session.Won ? "LEVEL " + session.LevelId.ToString("00") + " COMPLETE" : "TIME UP  /  OBJECTIVE MISSED";
            bool hasNext = session.Won && session.LevelId == 1 && ProgressStore.IsUnlocked(2);
            Next.gameObject.SetActive(hasNext);
            Replay.GetComponent<RectTransform>().anchoredPosition = new Vector2(hasNext ? -135 : 0, -381);
            Replay.GetComponent<RectTransform>().sizeDelta = new Vector2(hasNext ? 250 : 520, 70);
            Replay.GetComponentInChildren<Text>().text = hasNext ? "REPLAY" : "PLAY AGAIN";
            for (int i = 0; i < 3; i++) resultStars[i].color = i < session.Stars ? UIArt.Gold : new Color32(57, 77, 90, 255);
            resultScore.text = session.Score.ToString("N0");
            resultBest.text = newBest ? "NEW PERSONAL BEST" : "PERSONAL BEST  " + best.ToString("N0");
            resultStats.text = "Targets tapped     " + session.Hits + "\nBest combo          " + session.BestCombo + "\nTargets missed     " + session.Misses;
            resultHint.text = !session.Won ? "Tap targets as they appear.\nYou can try again immediately." : session.Stars == 3 ? "A perfect first spark. All three stars earned!" : "Chase three stars: keep a clean streak\nand aim for the score goal.";
            if (hasNext) resultHint.text = "LEVEL 2 UNLOCKED\nDiscover different target sizes!";
            else if (session.Won && session.LevelId == 2) resultHint.text = session.Stars == 3 ? "Sizes mastered. All three stars earned!" : "Replay for " + currentConfig.TwoStarScore + "+ points\nand a clean streak for three stars.";
            rewardEarned.text = session.Won ? "+" + receipt.Coins + " COINS     +" + receipt.XP + " XP" : "COMPLETE THE LEVEL TO EARN REWARDS";
            rewardEarned.fontSize = session.Won ? 22 : 17;
            rewardBalance.text = "WALLET  " + receipt.TotalCoins.ToString("N0") + " COINS   /   TOTAL XP  " + receipt.TotalXP.ToString("N0");
            bool leveledUp = receipt.TotalXP / 100 > (receipt.TotalXP - receipt.XP) / 100;
            playerProgress.text = (leveledUp ? "LEVEL UP!  " : "PLAYER LEVEL ") + receipt.PlayerLevel + "   /   " + (receipt.TotalXP % 100) + " / 100 XP";
            xpFill.rectTransform.anchorMax = new Vector2(receipt.TotalXP % 100 / 100f, 1);
            resultReveal.Begin(session.Score, session.Stars, reducedMotion);
        }
        public void SetSettings(bool sound, bool reducedMotion, bool haptics)
        {
            this.reducedMotion = reducedMotion;
            Sound.GetComponentInChildren<Text>().text = "SOUND  " + (sound ? "ON" : "OFF");
            Motion.GetComponentInChildren<Text>().text = "REDUCED MOTION  " + (reducedMotion ? "ON" : "OFF");
            Haptics.GetComponentInChildren<Text>().text = "VIBRATION  " + (haptics ? "ON" : "OFF");
        }
        public void Pop(Vector2 position, int points, Color tint, bool reducedMotion)
        {
            var p = popups[popupCursor++ % popups.Length]; p.Age = 0; p.Active = true;
            p.ReducedMotion = reducedMotion; p.Origin = position; p.Root.anchoredPosition = position;
            p.Label.text = "+" + points; p.Ring.color = tint; p.RingRect.localScale = Vector3.one;
            p.Ring.enabled = !reducedMotion; p.Alpha.alpha = 1; p.Root.gameObject.SetActive(true);
        }
        public void ClearFeedback() { foreach (var p in popups) { p.Active = false; p.Root.gameObject.SetActive(false); } popupCursor = 0; }
        private void Update()
        {
            UpdateSafeArea();
            tapHand.Tick(Time.unscaledDeltaTime, animateFeedback, reducedMotion);
            if (results.activeSelf) resultReveal.Tick(Time.unscaledDeltaTime, reducedMotion);
            if (!animateFeedback) return;
            for (int i = 0; i < popups.Length; i++)
            {
                var p = popups[i]; if (!p.Active) continue;
                p.Age += Time.unscaledDeltaTime; float t = p.Age / .55f;
                if (t >= 1) { p.Active = false; p.Root.gameObject.SetActive(false); continue; }
                p.Alpha.alpha = 1 - t;
                if (!p.ReducedMotion) { p.Root.anchoredPosition = p.Origin + Vector2.up * (t * 32); p.RingRect.localScale = Vector3.one * (1 + t * .8f); }
            }
        }
        private void UpdateSafeArea()
        {
            var screen = new Vector2(Screen.width, Screen.height); var safe = Screen.safeArea;
            var canvasSize = ((RectTransform)transform).rect.size;
            if (screen == lastScreen && safe == lastSafe && canvasSize == lastCanvasSize) return;
            lastScreen = screen; lastSafe = safe;
            lastCanvasSize = canvasSize;
            if (screen.x <= 0 || screen.y <= 0) return;
            safeArea.anchorMin = new Vector2(safe.xMin / screen.x, safe.yMin / screen.y);
            safeArea.anchorMax = new Vector2(safe.xMax / screen.x, safe.yMax / screen.y);
            Canvas.ForceUpdateCanvases();
            float scale = Mathf.Min(safeArea.rect.width / 720, safeArea.rect.height / 1280);
            frame.localScale = Vector3.one * Mathf.Max(.01f, scale);
        }
        public void DisposeAssets() { Art?.Dispose(); }
        public void ShowTapHand() { tapHand.Show(); tapHand.Tick(0, animateFeedback, reducedMotion); }
        public void HideTapHand() { tapHand.Hide(); }
        private void BuildLevels()
        {
            levelSelect = Overlay("Level select", out var card, 624, 790);
            Label(card, "World", "WORLD 01", 0, 315, 550, 38, 19, UIArt.Cyan, true);
            Label(card, "Title", "LEARN TO TAP", 0, 250, 560, 65, 40, Color.white, true);
            Label(card, "Description", "Choose your next challenge", 0, 183, 550, 36, 21, UIArt.Muted);
            LevelOne = Button(card, "Select Level 1", "01  /  FIRST SPARK", 0, 82, 520, 80, true);
            selectionOne = Label(card, "Level 1 progress", "", 0, 16, 530, 36, 18, UIArt.Muted);
            LevelTwo = Button(card, "Select Level 2", "02  /  SIZE IT UP", 0, -91, 520, 80, true);
            selectionTwo = Label(card, "Level 2 progress", "", 0, -157, 530, 40, 18, UIArt.Muted);
            selectionWallet = Label(card, "Wallet", "", 0, -267, 550, 80, 21, UIArt.Gold, true);
            Label(card, "Footer", "TWO CHALLENGES. KEEP YOUR STREAK.", 0, -350, 560, 32, 15, UIArt.Muted, true);
        }
        public void ShowLevels()
        {
            HideOverlays(); levelSelect.SetActive(true); Pause.interactable = false; animateFeedback = false;
            SetHint("", "");
            selectionOne.text = "BEST " + ProgressStore.GetBestScore(1) + "   /   " + ProgressStore.GetBestStars(1) + " / 3 STARS";
            LevelTwo.interactable = ProgressStore.IsUnlocked(2);
            selectionTwo.text = LevelTwo.interactable ? "BEST " + ProgressStore.GetBestScore(2) + "   /   " + ProgressStore.GetBestStars(2) + " / 3 STARS" : "LOCKED  /  Complete Level 1 to unlock";
            selectionWallet.text = ProgressStore.Coins.ToString("N0") + " COINS   /   " + ProgressStore.XP.ToString("N0") + " XP\nPLAYER LEVEL " + (1 + ProgressStore.XP / 100);
        }
    }
}
