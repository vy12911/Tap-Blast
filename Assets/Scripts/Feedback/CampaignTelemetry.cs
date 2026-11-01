using System;
using System.IO;
using UnityEngine;
namespace TapBlast
{
    // Bounded event collection: no file IO or JSON parsing while targets are active.
    public static class CampaignTelemetry
    {
        [Serializable] public struct Entry { public string name, reason, targetId; public int level, wave, value, pointerId; public float time, latencyMs; public Vector2 inputPosition, targetPosition; public bool warning; }
        public static void Input(string targetId, int pointerId, Vector2 input, Vector2 target, bool warning, float timestamp)
        {
            if (count >= entries.Length) return;
            entries[count++] = new Entry { name = "pointer_snapshot", level = currentLevel, wave = currentWave, targetId = targetId, pointerId = pointerId,
                inputPosition = input, targetPosition = target, warning = warning, time = timestamp,
                latencyMs = Mathf.Max(0, (float)(Time.realtimeSinceStartupAsDouble - timestamp) * 1000) };
        }
        public static void InputResult(string result)
        {
            for (int i = count - 1; i >= 0; i--) if (entries[i].name == "pointer_snapshot") { entries[i].reason = result; return; }
        }
        [Serializable] private sealed class Batch { public int seed, attempt, frameP50ms, frameP95ms; public string deviceTier, platform; public Entry[] events; }
        private static readonly Entry[] entries = new Entry[1024];
        private static int count, seed, attempt, frames;
        private static int currentLevel, currentWave;
        private static string deviceTier, platform;
        private static readonly int[] frameTimes = new int[128];
        public static void SampleFrame(float delta) { frameTimes[Mathf.Clamp(Mathf.RoundToInt(delta * 1000), 0, 127)]++; frames++; }
        private static int Percentile(float ratio) { int target = Mathf.CeilToInt(frames * ratio), sum = 0; for (int i = 0; i < frameTimes.Length; i++) { sum += frameTimes[i]; if (sum >= target) return i; } return 0; }
        public static void Begin(int level, int replaySeed, int attemptNumber)
        { count = frames = 0; currentLevel = level; currentWave = 0; deviceTier = SystemInfo.systemMemorySize < 4096 ? "low" : SystemInfo.systemMemorySize < 8192 ? "mid" : "high"; platform = Application.platform.ToString(); Array.Clear(frameTimes, 0, frameTimes.Length); seed = replaySeed; attempt = attemptNumber; Add("level_attempt", level, 0, attempt); }
        public static void Add(string name, int level, int wave, int value, string reason = "")
        {
            if (count >= entries.Length) return;
            currentWave = wave;
            entries[count++] = new Entry { name = name, reason = reason, level = level, wave = wave, value = value, time = Time.realtimeSinceStartup };
        }
        public static void Flush()
        {
            if (count == 0) return;
            var batch = new Batch { seed = seed, attempt = attempt, deviceTier = deviceTier, platform = platform, frameP50ms = Percentile(.5f), frameP95ms = Percentile(.95f), events = new Entry[count] };
            Array.Copy(entries, batch.events, count);
            try { File.AppendAllText(Path.Combine(Application.persistentDataPath, "campaign-v3-events.jsonl"), JsonUtility.ToJson(batch) + "\n"); }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
            count = 0;
        }
    }
}
