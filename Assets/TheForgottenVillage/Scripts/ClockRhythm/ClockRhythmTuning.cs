using UnityEngine;
namespace ForgottenVillage {
    [CreateAssetMenu(menuName = "Forgotten Village/Clock rhythm tuning")]
    public sealed class ClockRhythmTuning : ScriptableObject {
        [Range(.04f, .3f)] public float strikeCooldown = .18f;
        [Range(0, .5f)] public float volume = .18f;
        [Range(-.25f, .25f)] public float inputOffsetSeconds;
        public Color idle = new Color(.15f, .3f, .35f);
        public Color incoming = new Color(.2f, .85f, 1f);
        public Color perfect = new Color(1f, .8f, .25f);
        public Color missed = new Color(1f, .3f, .25f);
    }
}
