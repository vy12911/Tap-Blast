using UnityEngine;

namespace TapBlast
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip tap, success, failure, comboRise, chain, bomb, shield, chainImpact;
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
            chain = Tone("Chain blast", 440, .3f); bomb = Tone("Bomb warning", 110, .25f); shield = Tone("Shield block", 1320, .2f);
            chainImpact = BlastSound();
        }
        private static AudioClip BlastSound()
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * .22f)];
            var random = new System.Random(117);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1, t * 500) * Mathf.Exp(-t * 24);
                float thump = Mathf.Sin(2 * Mathf.PI * (150 * t - 180 * t * t));
                float crack = ((float)random.NextDouble() * 2 - 1) * Mathf.Exp(-t * 45);
                samples[i] = (thump * .65f + crack * .35f) * envelope * .5f;
            }
            var clip = AudioClip.Create("Chain detonation", samples.Length, 1, rate, false);
            clip.SetData(samples, 0); return clip;
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
        public void PlayChain(int count)
        {
            if (!SoundEnabled) return;
            source.pitch = count >= 8 ? 1.3f : 1; source.PlayOneShot(chain);
        }
        public void PlayChainImpact(int index)
        {
            if (!SoundEnabled) return;
            source.pitch = 1 + Mathf.Min(index, 11) * .045f;
            source.PlayOneShot(chainImpact, .8f);
        }
        public void PlayHazard(bool blocked)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (HapticsEnabled) Handheld.Vibrate();
#endif
            if (!SoundEnabled) return;
            source.pitch = 1; source.PlayOneShot(blocked ? shield : bomb);
        }
        public void ToggleSound() { SoundEnabled = !SoundEnabled; if (!SoundEnabled) source.Stop(); Save(); }
        public void ToggleMotion() { ReducedMotion = !ReducedMotion; Save(); }
        public void ToggleHaptics() { HapticsEnabled = !HapticsEnabled; Save(); }
        private void Save() { ProgressStore.SaveSettings(SoundEnabled, ReducedMotion, HapticsEnabled); }
        private void OnDestroy() { Destroy(tap); Destroy(success); Destroy(failure); Destroy(comboRise); Destroy(chain); Destroy(bomb); Destroy(shield); Destroy(chainImpact); }
    }
}
