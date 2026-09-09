using System;
using UnityEngine;

namespace TapBlast
{
    public static class ProgressStore
    {
        private const string Prefix = "TapBlast.v1.";
        private const string ProgressKey = "TapBlast.v2.Progress";
        [Serializable]
        private sealed class ProgressData
        {
            public int bestScore, bestStars, coins, xp;
            public int level2BestScore, level2BestStars;
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
        // One record keeps the consumed round token and its balances together.
        // Existing Level 1 records are migrated on the first write.
        private static ProgressData Load()
        {
            if (PlayerPrefs.HasKey(ProgressKey)) return JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(ProgressKey));
            return new ProgressData { bestScore = PlayerPrefs.GetInt(Prefix + "Level01.Best", 0), bestStars = PlayerPrefs.GetInt(Prefix + "Level01.Stars", 0) };
        }
        private static void Save(ProgressData data) { PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(data)); PlayerPrefs.Save(); }
        public static int BestScore => Load().bestScore;
        public static int BestStars => Load().bestStars;
        public static int GetBestScore(int level) => level == 1 ? Load().bestScore : level == 2 ? Load().level2BestScore : 0;
        public static int GetBestStars(int level) => level == 1 ? Load().bestStars : level == 2 ? Load().level2BestStars : 0;
        public static bool IsUnlocked(int level) => level == 1 || level == 2 && Load().bestStars > 0;
        public static int SelectedLevel => Load().selectedLevel == 2 && IsUnlocked(2) ? 2 : 1;
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
            if (session.LevelId == 1)
            { data.bestScore = Mathf.Max(session.Score, data.bestScore); data.bestStars = Mathf.Max(session.Stars, data.bestStars); }
            else
            { data.level2BestScore = Mathf.Max(session.Score, data.level2BestScore); data.level2BestStars = Mathf.Max(session.Stars, data.level2BestStars); }
            data.activeRound = ""; Save(data);
            receipt = new RewardReceipt(earnedCoins, earnedXP, data.coins, data.xp); return true;
        }
        public static bool Sound => PlayerPrefs.GetInt(Prefix + "Sound", 1) == 1;
        public static bool ReducedMotion => PlayerPrefs.GetInt(Prefix + "ReducedMotion", 0) == 1;
        public static bool Haptics => PlayerPrefs.GetInt(Prefix + "Haptics", 1) == 1;
        public static void SaveSettings(bool sound, bool reducedMotion, bool haptics)
        {
            PlayerPrefs.SetInt(Prefix + "Sound", sound ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "ReducedMotion", reducedMotion ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Haptics", haptics ? 1 : 0); PlayerPrefs.Save();
        }
    }
}
