using UnityEngine;

namespace TapBlast
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip tap, success, failure, comboRise;
        public bool SoundEnabled { get; private set; }
        public bool ReducedMotion { get; private set; }
        public bool HapticsEnabled { get; private set; }
        public void Initialize()
        {
            SoundEnabled = ProgressStore.Sound; ReducedMotion = ProgressStore.ReducedMotion;
            HapticsEnabled = ProgressStore.Haptics;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            tap = Tone("Tap", 620, .09f); success = Tone("Complete", 880, .45f); failure = Tone("Time up", 220, .25f);
            comboRise = Tone("Combo flourish", 1046, .22f);
        }
        private static AudioClip Tone(string name, float frequency, float seconds)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(rate * seconds)];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Min(1, t * 200) * Mathf.Pow(1 - (float)i / samples.Length, 2) * .22f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public void PlayTap(int combo)
        {
            bool milestone = combo == 5 || combo == 10 || combo == 20 || combo == 30 || combo == 50;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (milestone && combo >= 10 && HapticsEnabled) Handheld.Vibrate();
#endif
            if (!SoundEnabled) return;
            source.pitch = 1 + Mathf.Min(combo, 50) * .009f;
            source.PlayOneShot(tap);
            if (milestone) source.PlayOneShot(comboRise, .7f);
        }
        public void PlayResult(bool won)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (won && HapticsEnabled) Handheld.Vibrate();
#endif
            if (!SoundEnabled) return; source.pitch = 1; source.PlayOneShot(won ? success : failure);
        }
        public void ToggleSound() { SoundEnabled = !SoundEnabled; if (!SoundEnabled) source.Stop(); Save(); }
        public void ToggleMotion() { ReducedMotion = !ReducedMotion; Save(); }
        public void ToggleHaptics() { HapticsEnabled = !HapticsEnabled; Save(); }
        private void Save() { ProgressStore.SaveSettings(SoundEnabled, ReducedMotion, HapticsEnabled); }
        private void OnDestroy() { Destroy(tap); Destroy(success); Destroy(failure); Destroy(comboRise); }
    }
}
