using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenVillage {
    public sealed class ActiveWorkshopInput : MonoBehaviour {
        public ActiveWorkshopRun run;
#if UNITY_EDITOR
        public System.Action BeforeCapture;
#endif
        void Update() {
            if (!run || !run.house || !run.house.game || run.IsVR) return;
#if UNITY_EDITOR
            BeforeCapture?.Invoke();
#endif
            var game = run.house.game;
            if (!game.playing || game.house != 2 || game.ui.IsOpen || !game.player.View) return;
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (keys != null && run.Running) {
                float lean = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                run.DesktopPose(lean, keys.sKey.isPressed, Time.unscaledDeltaTime);
            }
            if (mouse == null) return;
            Ray ray = game.player.View.ScreenPointToRay(mouse.position.ReadValue());
            bool hit = Physics.Raycast(ray, out RaycastHit contact, 20, ~0, QueryTriggerInteraction.Collide);
            if (mouse.leftButton.wasPressedThisFrame && hit) {
                var control = contact.collider.GetComponentInParent<ActiveWorkshopControl>();
                if (control && control.run == run) { control.Use(); return; }
            }
            if (!run.AcceptsInput) return;
            var target = hit ? contact.collider.GetComponentInParent<ActiveWorkshopTarget>() : null;
            bool both = mouse.leftButton.isPressed && mouse.rightButton.isPressed;
            bool leftDown = mouse.leftButton.wasPressedThisFrame, rightDown = mouse.rightButton.wasPressedThisFrame;
            if (target && target.run == run && (leftDown || rightDown)) {
                if (both) run.DesktopPair(target.index);
                else if (leftDown) run.DesktopStrike(target.index, 0);
                else if (rightDown) run.DesktopStrike(target.index, 1);
            }
            UpdateHand(run.leftHandVisual, ray, mouse.leftButton.isPressed, -.08f);
            UpdateHand(run.rightHandVisual, ray, mouse.rightButton.isPressed, .08f);
        }
        void UpdateHand(Transform hand, Ray ray, bool pressed, float side) {
            if (!hand || !run.Running) return;
            hand.gameObject.SetActive(pressed && !run.IsPaused);
            if (!pressed) return;
            var target = run.targetPoints != null && run.targetPoints.Length > 0 ? run.targetPoints[0] : null;
            if (!target) return;
            var plane = new UnityEngine.Plane(-run.transform.forward, target.position);
            if (plane.Raycast(ray, out float distance)) hand.position = ray.GetPoint(distance) + run.transform.right * side;
        }
    }
}
