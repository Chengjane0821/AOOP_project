using UnityEngine;
using UnityEngine.XR;

namespace ForgottenVillage {
    public sealed class BridgeRun : MonoBehaviour {
        public BridgeHouse house;
        public BridgeView view;
        public Transform desktopPoint, vrPoint;
        public Transform[] banks = new Transform[2];
        public BridgeSocket[] sockets = new BridgeSocket[6];
        public BridgePiece[] pieces = new BridgePiece[6];
        public BridgeCart cart;
        public Transform cartStart, cartFinish;
        public float deckY = .98f, pathZ = .8f;
        public int Round { get; private set; }
        public bool Running { get; private set; }
        public bool Testing { get; private set; }
        public bool Won { get; private set; }
        public int FailedTrials { get; private set; }
        public bool LastTrialSucceeded { get; private set; }
        public bool AwaitingRetry { get; private set; }
        public bool RoundComplete { get; private set; }
        public bool IsPaused { get; private set; }
        public bool ShouldPause => !Running || !Allowed() || (house.game.ui && house.game.ui.IsOpen);
        public bool AcceptsInput => Running && Allowed() && !house.game.ui.IsOpen && !Testing && !RoundComplete && !AwaitingRetry;
        bool resetting, savedPlayer;
        bool previousLock, previousPointer;
        string previousPrompt;
        Quaternion previousCameraRotation;
        BridgeInput input;

        bool Allowed() => house && house.game && house.game.playing && house.game.house == 2 && house.game.state.elder;
        void Awake() { input = GetComponent<BridgeInput>(); }

        public void Begin() {
            if (!Allowed()) return;
            Stop(); Round = 0; Won = false; FailedTrials = 0; LastTrialSucceeded = false;
            var player = house.game.player;
            previousLock = player.ActivityLocksMovement; previousPointer = player.ActivityUsesPointer;
            previousPrompt = player.ActivityPrompt;
            if (player.desktopCamera) previousCameraRotation = player.desktopCamera.transform.localRotation;
            savedPlayer = true; Running = true;
            player.ActivityLocksMovement = true; player.ActivityUsesPointer = true;
            player.ActivityPrompt = ""; // The timber shelf carries the controls without covering the materials.
            var point = player.IsVR && vrPoint ? vrPoint : desktopPoint;
            if (point) player.Teleport(point.position, point.eulerAngles.y);
            if (!player.IsVR && player.desktopCamera) player.desktopCamera.transform.localRotation = Quaternion.Euler(20, 0, 0);
            StartRound();
        }

        void StartRound() {
            if (!Running || !Allowed()) return;
            resetting = true; CancelSelection(); Testing = false; RoundComplete = false; AwaitingRetry = false; IsPaused = false;
            foreach (var piece in pieces) if (piece) piece.ReturnHome();
            foreach (var socket in sockets) if (socket) socket.Clear();
            for (int i = 0; i < sockets.Length; i++) {
                var socket = sockets[i]; if (!socket) continue;
                socket.run = this; socket.index = i; socket.support = i >= 3;
                bool active = i < BridgeLevels.DeckCount(Round) || (Round == 2 && i >= 3 && i < 6);
                socket.gameObject.SetActive(active); socket.requiredLength = BridgeLevels.RequiredLength(Round);
                if (active) {
                    socket.transform.localPosition = new Vector3(BridgeLevels.Centre(Round, i % 3), deckY - (socket.support ? .22f : .03f), pathZ);
                    socket.transform.localRotation = Quaternion.identity;
                    socket.transform.localScale = new Vector3(socket.support ? 1 : socket.requiredLength / .8f, 1, 1);
                }
            }
            for (int i = 0; i < pieces.Length; i++) {
                var piece = pieces[i]; if (!piece) continue;
                piece.run = this; piece.index = i;
                bool active = i < BridgeLevels.PieceCount(Round); piece.gameObject.SetActive(active);
                if (active) piece.Configure(BridgeLevels.IsSupport(Round, i), BridgeLevels.PieceLength(Round, i));
            }
            float gap = BridgeLevels.Gap(Round);
            for (int i = 0; i < banks.Length && i < 2; i++) if (banks[i]) {
                var position = banks[i].localPosition; position.x = (i == 0 ? -1 : 1) * (gap + .36f); position.z = pathZ;
                banks[i].localPosition = position;
            }
            if (cartStart) cartStart.localPosition = new Vector3(-gap - .4f, deckY + .12f, pathZ);
            if (cartFinish) cartFinish.localPosition = new Vector3(gap + .4f, deckY + .12f, pathZ);
            if (cart) cart.Park(this);
            resetting = false; house.game.ui.Close();house.game.ui.notice.text=""; SetDesktopView(true);SetGrabs(true);
            if (view) view.Feedback(BridgeLevels.Hint(Round));
        }

        void Update() {
            if (Running && !Allowed()) { Stop(); return; }
            if (Running) {
                bool pause = ShouldPause;
                if (pause != IsPaused) {
                    IsPaused = pause;
                    if (pause) CancelSelection();
                    if (cart) cart.SetPaused(pause);
                    foreach (var piece in pieces) if (piece) piece.SetPaused(pause);
                }
                SetGrabs(AcceptsInput);
            }
            if (view) view.Render(this, Running && !IsPaused ? Time.deltaTime : 0);
        }

        public void Grab(BridgePiece piece) {
            if (!piece || resetting) return;
            if (!AcceptsInput || piece.run != this) { piece.Deselect(); return; }
            piece.SetHeld();
            if (view) view.Feedback(piece.support ? (house.game.player.IsVR?"Support: place under a board for heavy loads":"Support: drop onto a board to reinforce it underneath") :
                "Board " + piece.length.ToString("F2") + " m / rotate along the river crossing");
        }

        public void Release(BridgePiece piece) {
            if (!piece || resetting) return;
            float yaw = (Quaternion.Inverse(transform.rotation) * piece.transform.rotation).eulerAngles.y;
            DropAt(piece, piece.transform.position, yaw);
        }

        // World position, workshop-local yaw. Desktop and XR drops follow the same checks.
        public void DropAt(BridgePiece piece, Vector3 position, float yaw) {
            if (!piece || resetting || piece.run != this) return;
            if (!AcceptsInput) { if (piece.Held) piece.ReturnHome(); return; }
            piece.EndHeld();
            BridgeSocket nearest = null; float distance = float.MaxValue;
            foreach (var socket in sockets) {
                if (!socket || !socket.CanAccept(piece, position, yaw)) continue;
                float current = (position - socket.Point.position).sqrMagnitude;
                if (current < distance) { nearest = socket; distance = current; }
            }
            if (nearest) {
                piece.Deselect(); nearest.Attach(piece);
                SnapFeedback();
                if (view) view.Feedback(piece.support ? "SUPPORT FITTED / paired board capacity 3" :
                    BridgeRules.Spans(piece.length, nearest.requiredLength) ? "BOARD FITTED / press Space or TEST" :
                    "SHORT BOARD / it may collapse under the cart. Try a longer board.");
            } else {
                piece.ReturnHome();
                if (view) view.Feedback(BridgeRules.Aligned(yaw) ? "Place the part closer to a matching outline" :
                    "Rotate the board along the crossing / Q or E");
            }
        }

        public float CapacityFor(BridgeSocket socket) {
            if (!socket || !socket.Occupant) return 0;
            int supportIndex = socket.index + 3;
            return supportIndex < sockets.Length && sockets[supportIndex] && sockets[supportIndex].gameObject.activeInHierarchy &&
                sockets[supportIndex].Occupant ? 3 : socket.Occupant.capacity;
        }

        public int MissingBoards {
            get { int count = 0; for (int i = 0; i < BridgeLevels.DeckCount(Round); i++)
                if (i >= sockets.Length || !sockets[i] || !sockets[i].Occupant) count++; return count; }
        }

        public void StartTrial() {
            if (!AcceptsInput || !cart || !cartStart || !cartFinish) return;
            CancelSelection();
            bool incomplete = MissingBoards > 0;
            Testing = true; LastTrialSucceeded = false; SetGrabs(false); cart.Launch(this);
            if(house.game.audioFeedback)house.game.audioFeedback.Click();
            if (view) view.Feedback(incomplete ? "MISSING BOARDS / let's see what happens to the cart!" :
                "CART RELEASED / watch your bridge carry the load!");
        }

        internal void CartFinished(BridgeCart source, bool success, string message) {
            if (!Running || !Testing || source != cart || !Allowed()) return;
            Testing = false; LastTrialSucceeded = success;
            if (view) view.Feedback(message);
            var ui = house.game.ui;
            if (!success) {
                FailedTrials++; AwaitingRetry = true; SetGrabs(false);
                ui.Show("Bridge Delivery / Splash!", message + "\nYour layout is saved. Retry repairs fallen boards so you can adjust them.\nLonger spans need longer boards; heavy loads need supports.");
                ui.Button("Retry delivery", RetryTrial);
                ui.Button("Leave workbench", () => { Stop(); ui.Close(); });
                return;
            }
            RoundComplete = true; SetGrabs(false);
            if (house.game.audioFeedback) house.game.audioFeedback.Success();
            if (Round == BridgeLevels.Count - 1) { Won = true; Stop(); house.Win(); return; }
            ui.Show("Bridge Delivery / Delivered!", "Delivery " + (Round + 1) + " crossed your bridge safely.\n" +
                (Round == 0 ? "The next crossing is wider. Pick your timber carefully." : "The final cart is heavier. Give every board a support."));
            ui.Button("Next delivery", NextRound);
            ui.Button("Leave workbench", () => { Stop(); ui.Close(); });
        }

        public void RetryTrial() {
            if (!Running || !Allowed() || RoundComplete) return;
            resetting = true; CancelSelection(); Testing = false;
            if (cart) cart.Park(this);
            foreach (var piece in pieces) if (piece && piece.gameObject.activeInHierarchy) {
                if (piece.Broken || piece.AssignedSocket) piece.Repair();
                else { piece.SetPaused(false); if (piece.Held) piece.ReturnHome(); }
            }
            AwaitingRetry = false; IsPaused = false; resetting = false;
            house.game.ui.Close();SetDesktopView(true); SetGrabs(true);
            if (view) view.Feedback("LAYOUT RESTORED / adjust the bridge, then TEST again");
        }

        public void NextRound() {
            if (!Running || !Allowed() || !RoundComplete || Round >= BridgeLevels.Count - 1) return;
            Round++; StartRound();
        }
        public void ResetBuild() {
            if (!Running || !Allowed() || Testing || RoundComplete) return;
            StartRound();
        }
        public void PauseMenu() {
            if (!Running || !Allowed()) return;
            if (RoundComplete) {
                house.game.ui.Show("Bridge Delivery / Delivered!", "Your cart made it safely. Continue to the next crossing.");
                house.game.ui.Button("Next delivery", NextRound);
            } else {
                house.game.ui.Show("Bridge Delivery / Paused", "The cart and river trial are paused.\nDrag boards; Q / E rotates; Space tests; R returns a selected part.\nVR: grab, rotate and release near the blue outlines.");
                house.game.ui.Button("Resume", house.game.ui.Close);
                house.game.ui.Button("Retry delivery", RetryTrial);
            }
            house.game.ui.Button("Restart all", Begin);
            house.game.ui.Button("Leave workbench", () => { Stop(); house.game.ui.Close(); });
            IsPaused = true; CancelSelection();
            if (cart) cart.SetPaused(true);
            foreach (var piece in pieces) if (piece) piece.SetPaused(true);
        }

        void SetGrabs(bool available) { foreach (var piece in pieces) if (piece && piece.gameObject.activeInHierarchy) piece.SetAvailable(available); }
        void SnapFeedback(){
            if(house.game.audioFeedback)house.game.audioFeedback.Click();
            if(!house.game.player.IsVR)return;
            foreach(var node in new[]{XRNode.LeftHand,XRNode.RightHand}){var device=InputDevices.GetDeviceAtXRNode(node);if(device.TryGetHapticCapabilities(out var capabilities)&&capabilities.supportsImpulse)device.SendHapticImpulse(0,.18f,.04f);}
        }
        void SetDesktopView(bool building){if(savedPlayer&&house&&house.game&&!house.game.player.IsVR&&house.game.player.desktopCamera)house.game.player.desktopCamera.transform.localRotation=building?Quaternion.Euler(20,0,0):previousCameraRotation;}
        void CancelSelection() {
            if (!input) input = GetComponent<BridgeInput>();
            if (input) input.CancelDrag();
            foreach (var piece in pieces) if (piece) {
                bool held = piece.Held; piece.Deselect(); if (held) piece.ReturnHome();
            }
        }
        public void Stop() {
            resetting = true; CancelSelection(); if (cart) cart.Halt();
            foreach (var piece in pieces) if (piece) { piece.SetPaused(true); piece.SetAvailable(false); }
            if (savedPlayer && house && house.game && house.game.player) {
                var player = house.game.player; player.ActivityLocksMovement = previousLock; player.ActivityUsesPointer = previousPointer;
                player.ActivityPrompt = previousPrompt;
                if (!player.IsVR && player.desktopCamera) player.desktopCamera.transform.localRotation = previousCameraRotation;
            }
            savedPlayer = false; Running = Testing = IsPaused = false; resetting = false;
        }
        void OnDisable() { Stop(); }
    }
}
