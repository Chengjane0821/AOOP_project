using UnityEngine;

namespace ForgottenVillage {
    public sealed class ActiveWorkshopTarget : MonoBehaviour {
        public ActiveWorkshopRun run;
        public int index;
        public void StrikeLeft() { if (run) run.DesktopStrike(index, 0); }
        public void StrikeRight() { if (run) run.DesktopStrike(index, 1); }
    }
}
