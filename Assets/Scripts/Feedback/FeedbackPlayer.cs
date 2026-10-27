using UnityEngine;

namespace TapBlast
{
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip tap, success, failure, comboRise, chain, shield, chainImpact, boom;
        public bool SoundEnabled { get; private set; }
        public bool ReducedMotion { get; private set; }
        public void Initialize()
        {
            SoundEnabled = ProgressStore.Sound; ReducedMotion = ProgressStore.ReducedMotion;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            tap = Tone("Tap", 620, .09f); success = Tone("Complete", 880, .45f); failure = Tone("Time up", 220, .25f);
            comboRise = Tone("Combo flourish", 1046, .22f);
            chain = Tone("Chain blast", 440, .3f); shield = Tone("Shield block", 1320, .2f);
            chainImpact = BlastSound(); boom = BoomSound();
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
        // Bomb explosion: a falling bass thump, low-passed rumble and a short crack.
        private static AudioClip BoomSound()
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * .7f)];
            var random = new System.Random(331);
            float rumble = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float noise = (float)random.NextDouble() * 2 - 1;
                rumble += (noise - rumble) * .08f;
                float thump = Mathf.Sin(2 * Mathf.PI * (95 * t - 60 * t * t)) * Mathf.Exp(-t * 9);
                float crack = noise * Mathf.Exp(-t * 40);
                float envelope = Mathf.Min(1, t * 400) * Mathf.Exp(-t * 5);
                samples[i] = Mathf.Clamp((thump * .55f + rumble * 1.8f + crack * .25f) * envelope * .7f, -1, 1);
            }
            var clip = AudioClip.Create("Bomb explosion", samples.Length, 1, rate, false);
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
            if (!SoundEnabled) return;
            source.pitch = 1 + Mathf.Min(combo, 50) * .009f;
            source.PlayOneShot(tap);
            if (milestone) source.PlayOneShot(comboRise, .7f);
        }
        public void PlayResult(bool won)
        {
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
            if (!SoundEnabled) return;
            source.pitch = 1; source.PlayOneShot(blocked ? shield : boom);
        }
        public void ToggleSound() { SoundEnabled = !SoundEnabled; if (!SoundEnabled) source.Stop(); Save(); }
        public void ToggleMotion() { ReducedMotion = !ReducedMotion; Save(); }
        private void Save() { ProgressStore.SaveSettings(SoundEnabled, ReducedMotion); }
        private void OnDestroy() { Destroy(tap); Destroy(success); Destroy(failure); Destroy(comboRise); Destroy(chain); Destroy(shield); Destroy(chainImpact); Destroy(boom); }
    }
}
