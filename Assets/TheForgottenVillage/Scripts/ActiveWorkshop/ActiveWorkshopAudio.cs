using UnityEngine;

namespace ForgottenVillage {
    [RequireComponent(typeof(AudioSource))]
    public sealed class ActiveWorkshopAudio : MonoBehaviour {
        AudioSource source;
        AudioClip hit, miss;
        void Awake() {
            source = GetComponent<AudioSource>(); source.playOnAwake = false; source.spatialBlend = 0;
            hit = Tone("Woodling pop", 660, .11f, false); miss = Tone("Workshop miss", 170, .12f, true);
        }
        static AudioClip Tone(string name, float frequency, float duration, bool low) {
            int count = Mathf.CeilToInt(duration * 44100); var samples = new float[count];
            for (int i = 0; i < count; i++) {
                float t = i / 44100f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t + (low ? t * t * -400 : 0)) * Mathf.Exp(-t * 28) * .7f * Mathf.Min(1, i / 50f);
            }
            var clip = AudioClip.Create(name, count, 1, 44100, false); clip.SetData(samples, 0); return clip;
        }
        public void Play(bool success, int combo) {
            if (!source) return;
            source.pitch = success ? 1 + Mathf.Min(combo, 20) * .012f : .9f;
            source.PlayOneShot(success ? hit : miss, .6f);
        }
        public void Silence() { if (source) source.Stop(); }
        void OnDestroy() { if (hit) Destroy(hit); if (miss) Destroy(miss); }
    }
}
