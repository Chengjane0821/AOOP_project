using UnityEngine;
namespace ForgottenVillage {
    public sealed class PlanetaryDefenceControl : MonoBehaviour {
        public PlanetaryDefenceRun run;
        public bool pause;
        public void Use() { if (run) { if (pause) run.PauseMenu(); else run.Offer(); } }
    }
}
