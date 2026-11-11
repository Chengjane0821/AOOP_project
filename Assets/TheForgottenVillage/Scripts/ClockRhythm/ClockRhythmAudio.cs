using UnityEngine;
namespace ForgottenVillage {
    [RequireComponent(typeof(AudioSource))]
    public sealed class ClockRhythmAudio : MonoBehaviour {
        AudioSource source;
        readonly AudioClip[] bells = new AudioClip[3];
        AudioClip tick;
        void Awake() {
            source = GetComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.ignoreListenerPause = false;
            for (int i = 0; i < 3; i++) bells[i] = Tone("Repair bell " + i, 440f * Mathf.Pow(2, i / 3f), .35f);
            tick = Tone("Clock tick", 1100, .035f);
        }
        static AudioClip Tone(string name, float frequency, float duration) {
            int count = Mathf.CeilToInt(44100 * duration); var samples = new float[count];
            for (int i = 0; i < count; i++) {
                float t = i / 44100f;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Exp(-t * 14) * Mathf.Min(1, i / 80f);
            }
            var clip = AudioClip.Create(name, count, 1, 44100, false); clip.SetData(samples, 0); return clip;
        }
        public void Tick(float volume) { if (source && tick) source.PlayOneShot(tick, volume * .35f); }
        public void Bell(int lane, float volume) { if (source && lane >= 0 && lane < 3) source.PlayOneShot(bells[lane], volume); }
        public void Silence() { if (source) source.Stop(); }
        void OnDestroy() { foreach (var clip in bells) if (clip) Destroy(clip); if (tick) Destroy(tick); }
    }
}
