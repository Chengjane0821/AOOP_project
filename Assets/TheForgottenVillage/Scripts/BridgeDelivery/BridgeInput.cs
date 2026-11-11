using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenVillage {
    public sealed class BridgeInput : MonoBehaviour {
        public BridgeRun run;
#if UNITY_EDITOR
        public System.Action BeforeCapture;
#endif
        BridgePiece selected;
        Vector3 dragOffset;
        float yaw;
        readonly RaycastHit[] reinforcementHits=new RaycastHit[24];
        public BridgePiece Selected => selected;

        void Update() {
            if (!run || !run.Running || !run.house || !run.house.game) return;
#if UNITY_EDITOR
            BeforeCapture?.Invoke();
#endif
            var game = run.house.game;
            if (game.player.IsVR || game.ui.IsOpen) { CancelDrag(); return; }
            var mouse = Mouse.current; var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null || !game.player.View) return;
            Ray ray = game.player.View.ScreenPointToRay(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var controlHit, 20, ~0, QueryTriggerInteraction.Collide)) {
                var control = controlHit.collider.GetComponentInParent<BridgeControl>();
                if (control && control.run == run) { CancelDrag(); control.Use(); return; }
            }
            if (run.AwaitingRetry && keyboard.rKey.wasPressedThisFrame) { run.RetryTrial(); return; }
            if (run.RoundComplete && keyboard.spaceKey.wasPressedThisFrame) { run.NextRound(); return; }
            if (!run.AcceptsInput) { CancelDrag(); return; }
            if (keyboard.rKey.wasPressedThisFrame) {
                if (selected) { selected.ReturnHome(); selected = null; }
                else run.ResetBuild();
                return;
            }
            if (keyboard.spaceKey.wasPressedThisFrame) { Release(); run.StartTrial(); return; }
            if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out var hit, 20, ~0, QueryTriggerInteraction.Collide)) {
                var piece = hit.collider.GetComponentInParent<BridgePiece>();
                if (piece && piece.run == run && piece.gameObject.activeInHierarchy) {
                    selected = piece; run.Grab(piece);
                    yaw = (Quaternion.Inverse(run.transform.rotation) * piece.transform.rotation).eulerAngles.y;
                    UnityEngine.Plane plane = DragPlane();
                    dragOffset = plane.Raycast(ray, out float enter) ? piece.transform.position - ray.GetPoint(enter) : Vector3.zero;
                }
            }
            if (!selected) return;
            if (keyboard.qKey.wasPressedThisFrame) yaw -= 15;
            if (keyboard.eKey.wasPressedThisFrame) yaw += 15;
            UnityEngine.Plane drag = DragPlane();
            if (drag.Raycast(ray, out float distance)) {
                Vector3 local = run.transform.InverseTransformPoint(ray.GetPoint(distance) + dragOffset);
                local.x = Mathf.Clamp(local.x, -2.15f, 2.15f); local.z = Mathf.Clamp(local.z, -1.25f, 1.45f);
                local.y = run.deckY + .16f;
                // Desktop users drop a reinforcement onto the visible deck; it
                // fastens underneath. The held trigger must not hide that deck.
                if(selected.support){
                    int count=Physics.RaycastNonAlloc(ray,reinforcementHits,20,~0,QueryTriggerInteraction.Collide);float nearest=float.MaxValue;BridgePiece deck=null;
                    for(int i=0;i<count;i++){
                        var candidate=reinforcementHits[i].collider.GetComponentInParent<BridgePiece>();
                        if(candidate&&candidate!=selected&&!candidate.support&&candidate.Socket&&candidate.run==run&&reinforcementHits[i].distance<nearest){deck=candidate;nearest=reinforcementHits[i].distance;}
                    }
                    if(deck){var target=run.sockets[deck.Socket.index+3];if(target&&target.isActiveAndEnabled&&!target.Occupant){var point=run.transform.InverseTransformPoint(target.Point.position);local.x=point.x;local.z=point.z;}}
                }
                selected.transform.SetPositionAndRotation(run.transform.TransformPoint(local), run.transform.rotation * Quaternion.Euler(0, yaw, 0));
                if (selected.Body) { selected.Body.position = selected.transform.position; selected.Body.rotation = selected.transform.rotation; }
            }
            if (mouse.leftButton.wasReleasedThisFrame) Release();
        }
        UnityEngine.Plane DragPlane() => new UnityEngine.Plane(Vector3.up, run.transform.TransformPoint(new Vector3(0, run.deckY + .16f, 0)));
        void Release() {
            if (!selected) return; var piece = selected; selected = null;
            run.DropAt(piece, piece.transform.position, yaw);
        }
        public void CancelDrag() { if (!selected) return; var piece = selected; selected = null; piece.ReturnHome(); }
        void OnDisable() { CancelDrag(); }
    }
}
