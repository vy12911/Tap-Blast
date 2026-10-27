using System;
using UnityEngine;

namespace TapBlast
{
    public static class ProgressStore
    {
        private const string Prefix = "TapBlast.v1.";
        private const string ProgressKey = "TapBlast.v2.Progress";
        /// <summary>Number of authored campaign levels. Raise it when adding levels (save arrays grow on load).</summary>
        public const int LevelCount = 20;
        [Serializable]
        private sealed class ProgressData
        {
            public int bestScore, bestStars, coins, xp;
            public int level2BestScore, level2BestStars;
            public int[] scores = new int[LevelCount];
            public int[] stars = new int[LevelCount];
            public int[] bestCombos = new int[LevelCount];
            public int selectedLevel = 1, activeLevel = 1;
            public string activeRound = "";
        }
        public readonly struct RewardReceipt
        {
            public readonly int Coins, XP, TotalCoins, TotalXP;
            public int PlayerLevel => 1 + TotalXP / 100;
            public RewardReceipt(int coins, int xp, int totalCoins, int totalXP)
            { Coins = coins; XP = xp; TotalCoins = totalCoins; TotalXP = totalXP; }
        }
        // Parsed save kept in memory: reading PlayerPrefs (a JNI call on Android) and parsing JSON on every
        // getter cost time and garbage each frame. Every write goes through Save, which refreshes it.
        private static ProgressData cached;
        /// <summary>Drops the in-memory copy. Call after writing the progress key outside ProgressStore.</summary>
        public static void Invalidate() { cached = null; }
        private static ProgressData Load()
        {
            if (cached != null) return cached;
            ProgressData data;
            if (PlayerPrefs.HasKey(ProgressKey))
                data = JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(ProgressKey));
            else
                data = new ProgressData { bestScore = PlayerPrefs.GetInt(Prefix + "Level01.Best", 0), bestStars = PlayerPrefs.GetInt(Prefix + "Level01.Stars", 0) };

            if (data.scores == null || data.scores.Length < LevelCount) Array.Resize(ref data.scores, LevelCount);
            if (data.stars == null || data.stars.Length < LevelCount) Array.Resize(ref data.stars, LevelCount);
            if (data.bestCombos == null || data.bestCombos.Length < LevelCount) Array.Resize(ref data.bestCombos, LevelCount);
            if (data.bestScore > data.scores[0]) data.scores[0] = data.bestScore;
            if (data.bestStars > data.stars[0]) data.stars[0] = data.bestStars;
            if (data.level2BestScore > data.scores[1]) data.scores[1] = data.level2BestScore;
            if (data.level2BestStars > data.stars[1]) data.stars[1] = data.level2BestStars;
            cached = data;
            return data;
        }
        private static void Save(ProgressData data)
        {
            if (data.scores != null && data.scores.Length >= 2) { data.bestScore = data.scores[0]; data.level2BestScore = data.scores[1]; }
            if (data.stars != null && data.stars.Length >= 2) { data.bestStars = data.stars[0]; data.level2BestStars = data.stars[1]; }
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(data)); PlayerPrefs.Save();
            cached = data;
        }
        public static int BestScore => GetBestScore(1);
        public static int BestStars => GetBestStars(1);
        public static int GetBestScore(int level) => level >= 1 && level <= LevelCount ? Load().scores[level - 1] : 0;
        public static int GetBestStars(int level) => level >= 1 && level <= LevelCount ? Load().stars[level - 1] : 0;
        public static int GetBestCombo(int level) => level >= 1 && level <= LevelCount ? Load().bestCombos[level - 1] : 0;
        public static bool IsUnlocked(int level) => level == 1 || (level >= 2 && level <= LevelCount && GetBestStars(level - 1) > 0);
        public static int SelectedLevel => IsUnlocked(Load().selectedLevel) ? Load().selectedLevel : 1;
        public static bool SelectLevel(int level)
        {
            if (!IsUnlocked(level)) return false;
            var data = Load(); data.selectedLevel = level; Save(data); return true;
        }
        public static int Coins => Load().coins;
        public static int XP => Load().xp;
        public static string BeginRound(int level = 1)
        {
            if (!IsUnlocked(level)) throw new InvalidOperationException("Level is locked or unavailable.");
            var data = Load(); data.activeRound = Guid.NewGuid().ToString("N"); data.activeLevel = level; Save(data); return data.activeRound;
        }
        public static void AbandonRound(string round)
        {
            var data = Load(); if (string.IsNullOrEmpty(round) || data.activeRound != round) return;
            data.activeRound = ""; Save(data);
        }
        public static bool CompleteRound(string round, RoundSession session, int coins, int xp, out RewardReceipt receipt)
        {
            var data = Load(); receipt = new RewardReceipt(0, 0, data.coins, data.xp);
            if (string.IsNullOrEmpty(round) || data.activeRound != round || data.activeLevel != session.LevelId || session.State != RoundState.Results) return false;
            int earnedCoins = session.Won ? (int)Math.Min(Math.Max(0, coins), (long)int.MaxValue - data.coins) : 0;
            int earnedXP = session.Won ? (int)Math.Min(Math.Max(0, xp), (long)int.MaxValue - data.xp) : 0;
            data.coins += earnedCoins; data.xp += earnedXP;
            int idx = Mathf.Clamp(session.LevelId - 1, 0, LevelCount - 1);
            data.scores[idx] = Mathf.Max(session.Score, data.scores[idx]);
            data.stars[idx] = Mathf.Max(session.Stars, data.stars[idx]);
            data.bestCombos[idx] = Mathf.Max(session.BestCombo, data.bestCombos[idx]);
            data.activeRound = ""; Save(data);
            receipt = new RewardReceipt(earnedCoins, earnedXP, data.coins, data.xp); return true;
        }
        public static bool Sound => PlayerPrefs.GetInt(Prefix + "Sound", 1) == 1;
        public static bool ReducedMotion => PlayerPrefs.GetInt(Prefix + "ReducedMotion", 0) == 1;
        public static void SaveSettings(bool sound, bool reducedMotion)
        {
            PlayerPrefs.SetInt(Prefix + "Sound", sound ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ReducedMotion", reducedMotion ? 1 : 0); PlayerPrefs.Save();
        }
    }
}
