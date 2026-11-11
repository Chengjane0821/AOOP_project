using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage {
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(XRGrabInteractable))]
    public sealed class BridgePiece : MonoBehaviour {
        public BridgeRun run;
        public int index;
        public bool support;
        public float length = .8f, capacity = 1;
        public Transform home;
        public Renderer visual;
        public GameObject supportVisual;
        public Rigidbody Body { get; private set; }
        public XRGrabInteractable Grabber { get; private set; }
        public BridgeSocket Socket { get; private set; }
        public BridgeSocket AssignedSocket { get; private set; }
        public bool Broken { get; private set; }
        public bool Held { get; private set; }
        bool suppressRelease, paused, wasDynamic;
        float loadedFor;
        Vector3 pausedVelocity, pausedSpin;
        BoxCollider shape;
        MaterialPropertyBlock block;
        Color originalColour = new Color(.69f, .44f, .22f);

        void Awake() { Ensure(); }
        void Ensure() {
            if (Body) return;
            Body = GetComponent<Rigidbody>(); shape = GetComponent<BoxCollider>();
            Grabber = GetComponent<XRGrabInteractable>();
            Body.mass = 2f; Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Grabber.throwOnDetach = false;
            Grabber.movementType = XRBaseInteractable.MovementType.Kinematic;
            Grabber.selectEntered.AddListener(OnGrab);
            Grabber.selectExited.AddListener(OnRelease);
            if (visual && visual.sharedMaterial && visual.sharedMaterial.HasProperty("_BaseColor"))
                originalColour = visual.sharedMaterial.GetColor("_BaseColor");
        }
        void OnGrab(SelectEnterEventArgs _) { if (!suppressRelease && run) run.Grab(this); }
        void OnRelease(SelectExitEventArgs _) { if (!suppressRelease && run) run.Release(this); }

        internal void Configure(bool isSupport, float newLength) {
            Ensure(); ReturnHome(); support = isSupport; length = newLength; capacity = 1;
            shape.size = support ? new Vector3(.15f, .34f, .15f) : new Vector3(length, .06f, .26f);
            // The project's XR rays ignore triggers. Keep a real grip volume;
            // reinforcement tops stay below the deck, outside the cart's path.
            shape.enabled = true; shape.isTrigger = false;
            if (visual) visual.gameObject.SetActive(!support);
            if (supportVisual) supportVisual.SetActive(support);
            if (visual && !support) visual.transform.localScale = new Vector3(length, .06f, .26f);
            Tint(originalColour);
        }

        internal void SetHeld() {
            Ensure(); Socket?.Detach(this); Socket = null; AssignedSocket = null;
            Broken = false; loadedFor = 0; Held = true;
            Body.isKinematic = true; Body.useGravity = false; Tint(originalColour);
        }

        internal void EndHeld() { Held = false; }
        internal void SetAvailable(bool available) {
            Ensure();
            if (run && run.house && run.house.game && run.house.game.interactionManager &&
                Grabber.interactionManager != run.house.game.interactionManager)
                Grabber.interactionManager = run.house.game.interactionManager;
            if (Grabber.enabled != available) Grabber.enabled = available;
        }

        internal void Deselect() {
            Ensure(); suppressRelease = true;
            var manager = Grabber.interactionManager;
            if (manager) foreach (var interactor in Grabber.interactorsSelecting.ToArray())
                manager.SelectExit(interactor, Grabber);
            Held = false; suppressRelease = false;
        }

        internal void Attach(BridgeSocket socket) {
            Ensure(); if (Socket && Socket != socket) Socket.Detach(this); Socket = AssignedSocket = socket;
            Broken = false; Held = false; loadedFor = 0; paused = false;
            Body.isKinematic = true; Body.useGravity = false;
            transform.SetPositionAndRotation(socket.Point.position, socket.Point.rotation);
            Body.position = transform.position; Body.rotation = transform.rotation;
            Tint(!support && !BridgeRules.Spans(length, socket.requiredLength) ? new Color(.96f, .49f, .19f) : originalColour);
        }

        public void ReturnHome() {
            Ensure(); Deselect(); Socket?.Detach(this); Socket = AssignedSocket = null;
            Broken = false; Held = false; loadedFor = 0; paused = false;
            Body.isKinematic = true; Body.useGravity = false;
            if (home) transform.SetPositionAndRotation(home.position, home.rotation);
            Body.position = transform.position; Body.rotation = transform.rotation; Tint(originalColour);
        }

        internal void Repair() {
            Ensure(); var target = AssignedSocket;
            Deselect(); Broken = false; loadedFor = 0; paused = false;
            if (target && target.isActiveAndEnabled) target.Attach(this); else ReturnHome();
        }

        internal void SetPaused(bool value) {
            Ensure(); if (paused == value) return; paused = value;
            if (value) {
                wasDynamic = !Body.isKinematic;
                if (wasDynamic) { pausedVelocity = Body.linearVelocity; pausedSpin = Body.angularVelocity; Body.isKinematic = true; }
            } else if (wasDynamic) {
                Body.isKinematic = false; Body.linearVelocity = pausedVelocity; Body.angularVelocity = pausedSpin;
            }
        }

        void OnCollisionStay(Collision collision) {
            if (!run || !run.Testing || run.IsPaused || support || Broken || !Socket ||
                !collision.rigidbody || collision.rigidbody.GetComponent<BridgeCart>() != run.cart) return;
            if (BridgeRules.Spans(length, Socket.requiredLength) && run.CapacityFor(Socket) >= BridgeLevels.Load(run.Round)) {
                loadedFor = 0; return;
            }
            loadedFor += Time.fixedDeltaTime;
            if (loadedFor < .15f) return;
            var previous = Socket; previous.Detach(this); Socket = null; Broken = true;
            Body.isKinematic = false; Body.useGravity = true;
            Body.linearVelocity = Vector3.down * .15f;
            Tint(new Color(.84f, .20f, .13f));
            if (run.view) run.view.Feedback("CRACK! Rebuild this span or add its support.");
        }

        void Tint(Color colour) {
            if (!visual) return; if (block == null) block = new MaterialPropertyBlock();
            visual.GetPropertyBlock(block); block.SetColor("_BaseColor", colour); block.SetColor("_Color", colour);
            visual.SetPropertyBlock(block);
        }
    }
}
