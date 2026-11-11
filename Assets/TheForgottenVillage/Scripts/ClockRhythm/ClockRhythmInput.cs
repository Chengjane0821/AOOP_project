using UnityEngine;
using UnityEngine.InputSystem;
namespace ForgottenVillage {
    public sealed class ClockRhythmInput : MonoBehaviour {
        public ClockRhythmRun run;
        void Update() {
            if (!run || !run.AcceptsInput || !run.house.game || run.house.game.player.IsVR) return;
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.aKey.wasPressedThisFrame) run.Strike(0);
            if (keys.sKey.wasPressedThisFrame) run.Strike(1);
            if (keys.dKey.wasPressedThisFrame) run.Strike(2);
        }
    }
}
