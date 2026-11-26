using UnityEngine;

namespace ForgottenVillage {
    // Only the visual child turns: quest roots, ray colliders and tennis AI stay fixed.
    public sealed class NpcPresentation : MonoBehaviour {
        public int role;
        public Animator animator;
        public Transform head;
        public bool keepFacingCourt;
        VillageAction action;
        Quaternion restRotation;
        float greetingUntil;
        float carpenterTurnSpeed;
        bool conversing;

        void Awake() {
            restRotation = transform.localRotation;
            action = GetComponentInParent<VillageAction>();
            if (animator) animator.applyRootMotion = false;
        }

        void Start() {
            if (!keepFacingCourt || !action) return;
            var match = action.GetComponentInParent<ElderTennis>();
            var target = action.GetComponent<Collider>();
            if (match && match.ball && target)
                Physics.IgnoreCollision(target, match.ball.GetComponent<Collider>());
        }

        public void Greet() { conversing = true; greetingUntil = Time.time + 1.4f; }

        void LateUpdate() {
            if (!action || !action.game || !action.game.player || !action.game.player.View) return;
            var game = action.game;
            if (!game.ui || !game.ui.IsOpen) conversing = false;
            var direction = game.player.View.transform.position - transform.position;
            direction.y = 0;
            bool nearby = direction.sqrMagnitude < 16f && direction.sqrMagnitude > .04f;
            var desired = restRotation;
            if (nearby && !keepFacingCourt) {
                var facing = Quaternion.LookRotation(direction);
                desired = transform.parent ? Quaternion.Inverse(transform.parent.rotation) * facing : facing;
                // Avoid spinning all the way around when the player walks behind the resident.
                if (Quaternion.Angle(restRotation, desired) > 80f) desired = restRotation;
            }
            float turnSpeed = 70f;
            if (role == 2) {
                float angle = Quaternion.Angle(transform.localRotation, desired);
                carpenterTurnSpeed = Mathf.MoveTowards(carpenterTurnSpeed, Mathf.Min(95, angle * 6), 360 * Time.deltaTime);
                turnSpeed = carpenterTurnSpeed;
            }
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, desired, turnSpeed * Time.deltaTime);
            // The imported Idle pose is evaluated first. Add a small nod, never root displacement.
            if (head && animator && animator.isActiveAndEnabled && nearby && conversing) {
                float nod = role == 2
                    ? 7f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01((greetingUntil - Time.time) / 1.4f)), 2)
                    : Mathf.Sin(Time.time * 5f) * (Time.time < greetingUntil ? 5f : 1.5f);
                head.localRotation *= Quaternion.Euler(nod, 0, 0);
            }
        }
    }
}
