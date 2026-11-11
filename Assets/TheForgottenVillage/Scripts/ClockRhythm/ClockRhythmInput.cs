using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace ForgottenVillage {
    public sealed class ClockRhythmInput : MonoBehaviour {
        public ClockRhythmRun run;
        public static readonly Key[] LaneKeys = { Key.A, Key.S, Key.D, Key.J, Key.K, Key.L };
#if UNITY_EDITOR
        public System.Action BeforeCapture;
#endif
        ClockBell[] targets;
        readonly int[] inside = { -1, -1 };
        readonly bool[] primed = new bool[2];
        public void ResetContacts() {
            targets = GetComponentsInChildren<ClockBell>();
            for (int i = 0; i < inside.Length; i++) { inside[i] = -1; primed[i] = false; }
        }
        void Update() {
            if (!run || !run.house || !run.house.game) return;
            if (run.house.game.player.IsVR) {
                if (run.Running) { SampleHand(XRNode.LeftHand, 0); SampleHand(XRNode.RightHand, 1); }
                return;
            }
            if (!run.AcceptsInput) return;
#if UNITY_EDITOR
            BeforeCapture?.Invoke();
#endif
            var keys = Keyboard.current; if (keys == null) return;
            for (int i = 0; i < LaneKeys.Length; i++) if (keys[LaneKeys[i]].wasPressedThisFrame) run.Strike(i);
        }
        void SampleHand(XRNode node, int hand) {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid || !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked) || !tracked ||
                !device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 position)) {
                inside[hand] = -1; primed[hand] = false; return;
            }
            var origin = run.house.game.player.origin;
            if (!origin) return;
            var floor = origin.CameraFloorOffsetObject ? origin.CameraFloorOffsetObject.transform : origin.transform;
            SampleContact(hand, floor.TransformPoint(position), run.AcceptsInput);
        }
        // Entry edges prevent holding one controller inside a bell to repeat.
        public void SampleContact(int hand, Vector3 position, bool acceptInput) {
            if (hand < 0 || hand >= inside.Length) return;
            if (targets == null) targets = GetComponentsInChildren<ClockBell>();
            int contact = -1; float nearest = .055f * .055f;
            foreach (var bell in targets) {
                if (!bell || !bell.gameObject.activeInHierarchy) continue;
                var collider = bell.GetComponent<Collider>(); if (!collider || !collider.enabled) continue;
                float distance = (collider.ClosestPoint(position) - position).sqrMagnitude;
                if (distance <= nearest) { nearest = distance; contact = bell.lane; }
            }
            if (primed[hand] && contact >= 0 && inside[hand] != contact && acceptInput && run) run.Strike(contact);
            inside[hand] = contact; primed[hand] = true;
        }
    }
}
