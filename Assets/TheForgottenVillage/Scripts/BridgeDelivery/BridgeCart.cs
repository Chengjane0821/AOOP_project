using UnityEngine;

namespace ForgottenVillage {
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class BridgeCart : MonoBehaviour {
        public Transform[] wheels = new Transform[0];
        public Rigidbody Body { get; private set; }
        BridgeRun run;
        bool moving, paused;
        float elapsed;
        Vector3 pausedVelocity;
        public float TravelTime => elapsed;

        void Awake() { Ensure(); }
        void Ensure() {
            if (Body) return; Body = GetComponent<Rigidbody>();
            Body.mass = 1; Body.constraints = RigidbodyConstraints.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        internal void Park(BridgeRun owner) {
            Ensure(); run = owner; moving = paused = false; elapsed = 0;
            Body.isKinematic = true; Body.useGravity = false;
            if (owner.cartStart) transform.SetPositionAndRotation(owner.cartStart.position, owner.transform.rotation);
            Body.position = transform.position; Body.rotation = transform.rotation;
        }
        internal void Launch(BridgeRun owner) {
            Park(owner); moving = true; Body.mass = BridgeLevels.Load(owner.Round);
            Body.useGravity = true; Body.isKinematic = false;
            Body.linearVelocity = owner.transform.right * .42f;
        }
        internal void SetPaused(bool value) {
            Ensure(); if (!moving || paused == value) return;
            paused = value;
            if (value) { pausedVelocity = Body.linearVelocity; Body.isKinematic = true; }
            else { Body.isKinematic = false; Body.linearVelocity = pausedVelocity; }
        }
        internal void Halt() {
            Ensure(); moving = false; paused = false; Body.isKinematic = true; Body.useGravity = false;
        }

        void FixedUpdate() {
            if (!moving || !run) return;
            // Inspect the UI here as well: FixedUpdate can precede BridgeRun.Update after Escape.
            if (run.ShouldPause) { SetPaused(true); return; }
            if (paused) SetPaused(false);
            elapsed += Time.fixedDeltaTime;
            Vector3 local = run.transform.InverseTransformPoint(Body.position);
            if (local.y < run.deckY - .16f) { Finish(false, "The cart fell into the river. Adjust your bridge and try again."); return; }
            if (elapsed >= 12) { Finish(false, "The cart could not cross in time. Check the gaps and try again."); return; }
            float finishX = run.transform.InverseTransformPoint(run.cartFinish.position).x;
            if (local.x >= finishX && local.y >= run.deckY + .05f) { Finish(true, "DELIVERY COMPLETE"); return; }
            // Constrain lateral drift in workshop coordinates, so rotated house roots work too.
            local.z = run.pathZ;
            Body.position = run.transform.TransformPoint(local);
            Body.linearVelocity = run.transform.right * .42f + Vector3.up * Body.linearVelocity.y;
            foreach (var wheel in wheels) if (wheel) wheel.Rotate(Vector3.forward, -Time.fixedDeltaTime * 250, Space.Self);
        }
        void Finish(bool success, string message) { Halt(); run.CartFinished(this, success, message); }
    }
}
