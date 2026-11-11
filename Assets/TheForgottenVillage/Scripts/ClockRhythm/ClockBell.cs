using UnityEngine;
using UnityEngine.XR;
namespace ForgottenVillage {
    public sealed class ClockBell : MonoBehaviour {
        public ClockRhythmRun run;
        [Range(0, ClockChart.LaneCount - 1)] public int lane;
        public void Strike() { if (run) run.Strike(lane); }
        public static void Haptic(bool success) {
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand }) {
                var device = InputDevices.GetDeviceAtXRNode(node);
                if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    device.SendHapticImpulse(0, success ? .25f : .08f, .045f);
            }
        }
    }
}
