using UnityEngine;

namespace TapBlast
{
    [System.Serializable] public sealed class SpawnCue
    {
        public string id, gateId;
        public int laneId, order;
        public float telegraph = .45f, transit = .22f, cadence = 1.3f;
        public float at;
        public TargetKind kind;
        public int count = 1, color;
        public Vector2 position;
        public float life = 3.8f, size = 160;
        public bool moving, required = true, critical;
    }
    [System.Serializable] public sealed class ScriptedWave
    {
        public WaveRules rules = new WaveRules();
        public int maxActive = 4;
        public SpawnCue[] schedule = new SpawnCue[0];
        public EncounterEvent[] encounters = new EncounterEvent[0];
    }
    public enum ArenaEventKind { LanePreview, PulsePreview, SafeCorridor, TempoFakeout, SafeOpening, TwoLaneRhythm, ConduitPreview, PriorityFinale }
    [System.Serializable] public sealed class EncounterEvent
    {
        public string id;
        public ArenaEventKind kind;
        public float at, warning = .45f, active = 2;
        public float corridorX = .7f;
    }
    [CreateAssetMenu(menuName = "Tap Blast/Level", fileName = "Level01")]
    public sealed class LevelConfig : ScriptableObject
    {
        [SerializeField] private int designVersion = 1;
        [SerializeField] private BossPhaseDefinition[] bossPhases = new BossPhaseDefinition[0];
        public int DesignVersion => designVersion;
        public BossPhaseDefinition[] BossPhases => bossPhases;
        public bool V5Boss => designVersion >= 5 && bossPhases != null && bossPhases.Length == 5;
        [SerializeField] private string displayName = "FIRST SPARK";
        [SerializeField, Range(1, 100)] private int levelId = 1;
        [SerializeField] private ObjectiveKind objective = ObjectiveKind.Taps;
        [SerializeField, Min(1)] private int targetScore = 500;
        [SerializeField, Min(1)] private int objectiveCount = 1;
        [Tooltip("One exact 'how to do it' sentence shown under the goal (briefing and hint card). Keep it under ~80 characters.")]
        [SerializeField, TextArea] private string mechanicHint = "Tap the glowing circles before their ring runs out.";
        [SerializeField] private bool firstTapTutorial = true;
        [SerializeField] private float[] targetSizes = new float[0];
        [SerializeField, Min(1)] private float duration = 60;
        [SerializeField, Min(1)] private int requiredTaps = 20;
        [SerializeField, Min(1)] private int basePoints = 10;
        [Header("Spawning (UI reference pixels)")]
        [SerializeField, Min(0.2f)] private float spawnInterval = 1.1f;
        [SerializeField, Min(0)] private float finalSpawnInterval = 0;
        [SerializeField, Min(1)] private float accelerationSeconds = 20;
        [SerializeField, Min(1)] private float targetLifetime = 4.5f;
        [SerializeField, Range(1, 12)] private int maxTargets = 4;
        [SerializeField, Range(105, 240)] private float targetSize = 168;
        [SerializeField, Min(0)] private float targetSpacing = 36;
        [Header("Special Target Mechanics")]
        [SerializeField] private bool movingTargets = false;
        [SerializeField, Range(0f, 1f)] private float goldenChance = 0f;
        [SerializeField, Range(0f, 1f)] private float timeChance = 0f;
        [SerializeField] private bool isBossLevel = false;
        [SerializeField, Min(1)] private int bossHealth = 5;
        [SerializeField, Min(1)] private int goldenPoints = 100;
        [SerializeField, Min(0)] private float timeBonus = 3;
        [SerializeField, Min(3)] private int maxTimeTargets = 6;
        [SerializeField, Range(216, 360)] private float bossSize = 270;
        [Header("World 2: clusters, hazards and Shield")]
        [Tooltip("Same-color opportunity per wave in World 2; two other colors also spawn. Zero keeps World 1 spawning.")]
        [SerializeField, Range(0, 10)] private int clusterSize = 0;
        [SerializeField, Min(.5f)] private float clusterInterval = 2.4f;
        [SerializeField, Min(0)] private float movementSpeed = 100;
        [SerializeField, Range(0, 1)] private float bombChance = 0;
        [SerializeField, Min(0)] private int shieldCharges = 0;
        [SerializeField, Min(0)] private int bombPenalty = 50, chainBonus = 25, requiredCombo = 0;
        [Header("Stars: completion / score / score + no misses + streak")]
        [SerializeField, Min(1)] private int twoStarScore = 240;
        [SerializeField, Min(1)] private int threeStarCombo = 20;
        [Header("Reward for each successful round (including replay)")]
        [SerializeField, Min(0)] private int coinReward = 50;
        [SerializeField, Min(0)] private int xpReward = 25;
        [Header("GDD v3 scripted campaign")]
        [SerializeField] private bool scripted;
        [SerializeField] private int seed = 1701, pressureLimit = 4, priorityMissLimit = 99;
        [SerializeField] private bool noBombs, allowUltimate = true;
        [SerializeField] private int normalCharge = 3, goldenCharge = 6, chainCharge = 10;
        [SerializeField] private TargetKind[] ultimateEligible = { TargetKind.Normal, TargetKind.Small, TargetKind.Golden, TargetKind.Time, TargetKind.Chain };
        public bool AllowUltimate => allowUltimate;
        public bool CanUltimateClear(TargetKind kind) { if (!allowUltimate || ultimateEligible == null) return false; foreach (var type in ultimateEligible) if (type == kind) return true; return false; }
        [SerializeField] private string scriptedGoal = "Clear every wave";
        [SerializeField] private ScriptedWave[] waves = new ScriptedWave[0];
        public bool Scripted => scripted && waves != null && waves.Length > 0;
        public string MechanicBadge
        {
            get
            {
                if (!Scripted) return "TARGETS";
                if (waves.Length == 5) return "5-PHASE BOSS";
                if (noBombs) return "X AVOID BOMBS";
                foreach (var phase in waves) if (phase.rules.gate == GateKind.Shield) return "SHIELD";
                foreach (var phase in waves) if (phase.rules.gate == GateKind.MegaChains) return "8+ MEGA";
                foreach (var phase in waves) if (phase.rules.gate == GateKind.Combo) return "STREAK";
                return waves[0].rules.gate switch { GateKind.Priority => "! PRIORITY", GateKind.Pairs => "1 > 2 PAIRS",
                    GateKind.Chains => "3+ CHAIN", GateKind.Small => "SMALL", GateKind.Moving => "MOVING",
                    GateKind.Golden => "GOLDEN", _ => "DIRECT TAPS" };
            }
        }
        public int Seed => seed;
        public ScriptedWave[] Waves => waves;
        public int PressureLimit => pressureLimit;
        public int PriorityMissLimit => priorityMissLimit;
        public bool NoBombs => noBombs;
        public CampaignRules BuildCampaignRules()
        {
            if (!Scripted) return null;
            var result = new CampaignRules { waves = new WaveRules[waves.Length], pressureLimit = pressureLimit,
                normalCharge = normalCharge, goldenCharge = goldenCharge, chainCharge = chainCharge,
                noBombs = noBombs, maxCriticalMisses = 3, priorityMissLimit = priorityMissLimit };
            for (int i = 0; i < waves.Length; i++) result.waves[i] = waves[i].rules;
            return result;
        }
        public int CoinReward => Mathf.Max(0, coinReward);
        public int LevelId => Mathf.Clamp(levelId, 1, 100);
        public ObjectiveKind Objective => objective;
        public int TargetScore => Mathf.Max(1, targetScore);
        public int BasePoints => Mathf.Max(1, basePoints);
        public bool FirstTapTutorial => firstTapTutorial;
        public int ObjectiveCount => Mathf.Max(1, objectiveCount);
        public int GoldenPoints => Mathf.Max(1, goldenPoints);
        public float TimeBonus => Mathf.Max(0, timeBonus);
        public int MaxTimeTargets => Mathf.Max(objective == ObjectiveKind.Time ? ObjectiveCount : 0, maxTimeTargets);
        public float BossSize => Mathf.Clamp(bossSize, 216, 360);
        public int ClusterSize => Mathf.Clamp(clusterSize, 0, Mathf.Max(0, MaxTargets - 2 - (BombChance > 0 ? 1 : 0)));
        public float ClusterInterval => Mathf.Max(.5f, clusterInterval);
        public float MovementSpeed => Mathf.Max(0, movementSpeed);
        public float BombChance => Mathf.Clamp01(bombChance);
        public int ShieldCharges => Mathf.Max(0, shieldCharges);
        public int RequiredCombo => Mathf.Max(0, requiredCombo);
        public string MechanicHint => mechanicHint;
        public string ObjectiveLabel => Scripted ? "WAVE GOAL" : objective switch
        {
            ObjectiveKind.Score => "SCORE", ObjectiveKind.Combo => "BEST STREAK",
            ObjectiveKind.Golden => "GOLDEN", ObjectiveKind.Time => "TIME BUBBLES",
            ObjectiveKind.Boss => "CORE DESTROYED", ObjectiveKind.MegaChains => "MEGA CHAINS",
            ObjectiveKind.Chains or ObjectiveKind.ChainsNoBombs or ObjectiveKind.ChainMaster => "CHAINS",
            ObjectiveKind.NoBombs => "SAFE TARGETS", ObjectiveKind.Shield => "BOMBS BLOCKED", _ => "TARGETS"
        };
        // Player-facing instructions. Every level shows the same three parts: the exact goal, one "how to"
        // sentence (mechanicHint) and the win rule. New objective kinds must add a line to each switch.
        /// <summary>The exact goal, without the time limit: "Make 2 chains".</summary>
        public string Goal => Scripted ? scriptedGoal : objective switch
        {
            ObjectiveKind.Score => "Score " + TargetScore.ToString("N0") + " points",
            ObjectiveKind.Combo => "Hit " + ObjectiveCount + " targets in a row",
            ObjectiveKind.Golden => "Tap " + ObjectiveCount + " Golden target" + Plural(ObjectiveCount),
            ObjectiveKind.Time => "Tap " + ObjectiveCount + " Time Bubble" + Plural(ObjectiveCount),
            ObjectiveKind.Boss => "Destroy the Golden Core (" + BossHealth + " hits)",
            ObjectiveKind.Chains => "Make " + ObjectiveCount + " chain" + Plural(ObjectiveCount),
            ObjectiveKind.MegaChains => "Make " + ObjectiveCount + " Mega Chain" + Plural(ObjectiveCount),
            ObjectiveKind.NoBombs => "Tap " + RequiredTaps + " targets without tapping a Bomb",
            ObjectiveKind.Shield => "Block " + ObjectiveCount + " Bomb with your Shield",
            ObjectiveKind.ChainsNoBombs => "Make " + ObjectiveCount + " chains without tapping a Bomb",
            ObjectiveKind.ChainMaster => "Hit " + RequiredCombo + " in a row and make " + ObjectiveCount + " chains",
            _ => "Tap " + RequiredTaps + " targets"
        };
        /// <summary>The goal with its time limit, for the briefing and countdown.</summary>
        public string GoalText => Goal + " in " + Duration.ToString("0") + " seconds";
        /// <summary>When the level ends and what passes it.</summary>
        public string WinRule => Scripted ? "Clear every wave. Pressure " + pressureLimit + " ends the run." : objective switch
        {
            ObjectiveKind.Taps => "The level ends as soon as you reach " + RequiredTaps + ".",
            ObjectiveKind.NoBombs or ObjectiveKind.ChainsNoBombs => "Reach it before time runs out. One Bomb tap fails the goal.",
            _ => "Reach it before time runs out, then keep scoring for more stars."
        };
        public string OneStarRule => "Complete the goal";
        public string TwoStarRule => Scripted ? "All gates + 85% accuracy or score " + TwoStarScore : "Goal + score " + TwoStarScore.ToString("N0") + "+";
        public string ThreeStarRule => Scripted ? "All gates, zero Bomb hits, at most 3 critical misses" : "Score " + TwoStarScore.ToString("N0") + "+, " + ThreeStarCombo + " in a row, no misses" + (BombChance > 0 ? " or Bomb hits" : "");
        private static string Plural(int count) => count == 1 ? "" : "s";
        public float SizeAt(int index) => targetSizes == null || targetSizes.Length == 0 ? TargetSize : Mathf.Clamp(targetSizes[index % targetSizes.Length], 105, 240);
        public float LargestTargetSize
        {
            get { float largest = IsBossLevel ? Mathf.Max(TargetSize, BossSize) : TargetSize; if (targetSizes != null) foreach (float size in targetSizes) largest = Mathf.Max(largest, Mathf.Clamp(size, 105, 240)); return largest; }
        }
        public int XPReward => Mathf.Max(0, xpReward);
        public string DisplayName => displayName;
        public float Duration => Mathf.Max(1, duration);
        public int RequiredTaps => Mathf.Max(1, requiredTaps);
        public float SpawnInterval => Mathf.Max(0.2f, spawnInterval);
        public float SpawnIntervalAt(float seconds) => finalSpawnInterval <= 0 ? SpawnInterval : Mathf.Lerp(SpawnInterval, Mathf.Max(.2f, finalSpawnInterval), Mathf.Clamp01(seconds / Mathf.Max(1, accelerationSeconds)));
        public float TargetLifetime => Mathf.Max(1, targetLifetime);
        public int MaxTargets => Mathf.Clamp(maxTargets, 1, 12);
        public float TargetSize => Mathf.Clamp(targetSize, 105, 240);
        public float TargetSpacing => Mathf.Max(0, targetSpacing);
        public bool MovingTargets => movingTargets;
        public float GoldenChance => Mathf.Clamp01(goldenChance);
        public float TimeChance => Mathf.Clamp01(timeChance);
        public bool IsBossLevel => isBossLevel;
        public int BossHealth => Mathf.Max(1, bossHealth);
        public int TwoStarScore => Mathf.Max(1, twoStarScore);
        public int ThreeStarCombo => Mathf.Max(1, threeStarCombo);
        public RoundRules Rules => new RoundRules(Duration, RequiredTaps, basePoints, TwoStarScore, ThreeStarCombo, objective, TargetScore, LevelId, ObjectiveCount, GoldenPoints, TimeBonus, RequiredCombo, ShieldCharges, bombPenalty, chainBonus, BuildCampaignRules());
    }
}
