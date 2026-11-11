using UnityEngine;
using UnityEngine.InputSystem;
namespace ForgottenVillage {
    public sealed class ClockRhythmInput : MonoBehaviour {
        public ClockRhythmRun run;
#if UNITY_EDITOR
        public System.Action BeforeCapture;
#endif
        void Update() {
            if (!run || !run.AcceptsInput || !run.house.game || run.house.game.player.IsVR) return;
#if UNITY_EDITOR
            BeforeCapture?.Invoke();
#endif
            var keys = Keyboard.current; if (keys == null) return;
            if (keys.aKey.wasPressedThisFrame) run.Strike(0);
            if (keys.sKey.wasPressedThisFrame) run.Strike(1);
            if (keys.dKey.wasPressedThisFrame) run.Strike(2);
        }
    }
}
