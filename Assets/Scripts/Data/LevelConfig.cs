using UnityEngine;

namespace TapBlast
{
    [CreateAssetMenu(menuName = "Tap Blast/Level", fileName = "Level01")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField] private string displayName = "FIRST SPARK";
        [SerializeField, Range(1, 10)] private int levelId = 1;
        [SerializeField] private ObjectiveKind objective = ObjectiveKind.Taps;
        [SerializeField, Min(1)] private int targetScore = 500;
        [SerializeField] private bool firstTapTutorial = true;
        [SerializeField] private float[] targetSizes = new float[0];
        [SerializeField, Min(1)] private float duration = 60;
        [SerializeField, Min(1)] private int requiredTaps = 20;
        [SerializeField, Min(1)] private int basePoints = 10;
        [Header("Spawning (UI reference pixels)")]
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.1f;
        [SerializeField, Min(1)] private float targetLifetime = 4.5f;
        [SerializeField, Range(1, 12)] private int maxTargets = 4;
        [SerializeField, Range(70, 160)] private float targetSize = 112;
        [SerializeField, Min(0)] private float targetSpacing = 24;
        [Header("Special Target Mechanics")]
        [SerializeField] private bool movingTargets = false;
        [SerializeField, Range(0f, 1f)] private float goldenChance = 0f;
        [SerializeField, Range(0f, 1f)] private float timeChance = 0f;
        [SerializeField] private bool isBossLevel = false;
        [SerializeField, Min(1)] private int bossHealth = 5;
        [Header("Stars: completion / score / score + no misses + streak")]
        [SerializeField, Min(1)] private int twoStarScore = 240;
        [SerializeField, Min(1)] private int threeStarCombo = 20;
        [Header("Reward for each successful round (including replay)")]
        [SerializeField, Min(0)] private int coinReward = 50;
        [SerializeField, Min(0)] private int xpReward = 25;
        public int CoinReward => Mathf.Max(0, coinReward);
        public int LevelId => Mathf.Clamp(levelId, 1, 10);
        public ObjectiveKind Objective => objective;
        public int TargetScore => Mathf.Max(1, targetScore);
        public int BasePoints => Mathf.Max(1, basePoints);
        public bool FirstTapTutorial => firstTapTutorial;
        public string GoalText => objective == ObjectiveKind.Score ? "Score " + TargetScore + " in " + Duration.ToString("0") + " seconds" : "Tap " + RequiredTaps + " targets in " + Duration.ToString("0") + " seconds";
        public float SizeAt(int index) => targetSizes == null || targetSizes.Length == 0 ? TargetSize : Mathf.Clamp(targetSizes[index % targetSizes.Length], 70, 160);
        public float LargestTargetSize
        {
            get { float largest = TargetSize; if (targetSizes != null) foreach (float size in targetSizes) largest = Mathf.Max(largest, Mathf.Clamp(size, 70, 160)); return largest; }
        }
        public int XPReward => Mathf.Max(0, xpReward);
        public string DisplayName => displayName;
        public float Duration => Mathf.Max(1, duration);
        public int RequiredTaps => Mathf.Max(1, requiredTaps);
        public float SpawnInterval => Mathf.Max(0.2f, spawnInterval);
        public float TargetLifetime => Mathf.Max(1, targetLifetime);
        public int MaxTargets => Mathf.Clamp(maxTargets, 1, 12);
        public float TargetSize => Mathf.Clamp(targetSize, 70, 160);
        public float TargetSpacing => Mathf.Max(0, targetSpacing);
        public bool MovingTargets => movingTargets;
        public float GoldenChance => Mathf.Clamp01(goldenChance);
        public float TimeChance => Mathf.Clamp01(timeChance);
        public bool IsBossLevel => isBossLevel;
        public int BossHealth => Mathf.Max(1, bossHealth);
        public int TwoStarScore => Mathf.Max(1, twoStarScore);
        public int ThreeStarCombo => Mathf.Max(1, threeStarCombo);
        public RoundRules Rules => new RoundRules(Duration, RequiredTaps, basePoints, TwoStarScore, ThreeStarCombo, objective, TargetScore, LevelId);
    }
}
