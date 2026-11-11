using System;
using UnityEngine;
using UnityEngine.XR;

namespace ForgottenVillage {
    [RequireComponent(typeof(ActiveWorkshopInput), typeof(ActiveWorkshopAudio))]
    public sealed class ActiveWorkshopRun : MonoBehaviour {
        public const double CountIn = 3;
        public ActiveWorkshopHouse house;
        public ActiveWorkshopView view;
        public Transform desktopPoint, vrPoint;
        public Transform[] targetPoints = new Transform[6];
        public Transform[] targetVisuals = new Transform[6];
        public Transform leftHandVisual, rightHandVisual, obstacleVisual;
        [Tooltip("Negative uses a fresh pattern on each play; a nonnegative seed is useful for reproducible checks.")]
        public int seedOverride = -1;
        public bool Running { get; private set; }
        public bool IsPaused => Running && (!Allowed() || house.game.ui.IsOpen);
        public bool AcceptsInput => Running && Allowed() && !house.game.ui.IsOpen && Elapsed >= CountIn;
        public double Elapsed { get; private set; }
        public double CountInRemaining => Math.Max(0, CountIn - Elapsed);
        public ActiveWorkshopSession Session { get; private set; }
        public Vector3 NeutralHead { get; private set; }
        public Vector3 HeadOffset { get; private set; }
        public bool LeftTracked { get; private set; }
        public bool RightTracked { get; private set; }
        public bool HeadTracked { get; private set; }
        public int BestScore => house && house.game ? house.game.state.carpenterBestScore : 0;
        public bool IsVR => house && house.game && house.game.player && house.game.player.IsVR;
        readonly bool[,] armed = new bool[2, 6];
        readonly Vector3[] authoredTargets = new Vector3[6];
        bool targetsRecorded, savedPlayer, previousLock, previousPointer;
        string previousPrompt;
        Quaternion previousCameraRotation;
        Vector3 previousCameraPosition, desktopRest;
        double lastTime;
        int lastCue = -1, lastRevision;
        ActiveWorkshopAudio sound;
        double trackingLostFor;

        bool Allowed() => house && house.game && house.game.playing && house.game.house == 2 && house.game.state.elder;
        void Awake() {
            sound = GetComponent<ActiveWorkshopAudio>();
            var input = GetComponent<ActiveWorkshopInput>(); if (input) input.run = this;
            RecordTargets();
        }
        void RecordTargets() {
            if (targetsRecorded) return;
            for (int i = 0; i < targetPoints.Length && i < 6; i++) if (targetPoints[i]) authoredTargets[i] = targetPoints[i].localPosition;
            targetsRecorded = true;
        }
        public void Begin() {
            if (!Allowed()) return;
            Stop(); RecordTargets();
            var player = house.game.player;
            previousLock = player.ActivityLocksMovement; previousPointer = player.ActivityUsesPointer;
            previousPrompt = player.ActivityPrompt;
            if (player.desktopCamera) {
                previousCameraRotation = player.desktopCamera.transform.localRotation;
                previousCameraPosition = player.desktopCamera.transform.localPosition;
            }
            savedPlayer = true; player.ActivityLocksMovement = true; player.ActivityUsesPointer = !player.IsVR; player.ActivityPrompt = "";
            var point = player.IsVR && vrPoint ? vrPoint : desktopPoint;
            if (point) player.Teleport(point.position, point.eulerAngles.y);
            if (!player.IsVR && player.desktopCamera) {
                player.desktopCamera.transform.localRotation = Quaternion.identity;
                desktopRest = player.desktopCamera.transform.localPosition;
                for (int i = 0; i < targetPoints.Length && i < 6; i++) if (targetPoints[i]) targetPoints[i].localPosition = authoredTargets[i];
            }
            Session = new ActiveWorkshopSession(seedOverride < 0 ? UnityEngine.Random.Range(0, int.MaxValue) : seedOverride);
            Elapsed = 0; lastCue = -1; lastRevision = 0; HeadOffset = Vector3.zero; trackingLostFor = 0;
            LeftTracked = RightTracked = false; HeadTracked = true;
            house.game.ui.Close(); if (house.game.ui.notice) house.game.ui.notice.text = "";
            Running = true; CalibratePose(); lastTime = Time.realtimeSinceStartupAsDouble;
            if (view) view.Feedback("GET READY / stand or sit comfortably, hands in front", true);
        }

        void Update() {
            double now = Time.realtimeSinceStartupAsDouble;
            double dt = Math.Max(0, now - lastTime); lastTime = now;
            if (Running && !Allowed()) { Stop(); return; }
            if (Running && !IsPaused) {
                double previousActive = Math.Max(0, Elapsed - CountIn);
                Elapsed += dt;
                Session.Advance(Math.Max(0, Elapsed - CountIn) - previousActive);
                if (IsVR) CaptureVR(dt);
                if (Session.Revision != lastRevision) {
                    lastRevision = Session.Revision;
                    if (view) view.Feedback(Session.LastFeedback, Session.LastSuccess);
                    if (sound) sound.Play(Session.LastSuccess, Session.Combo);
                    if (IsVR) Haptic(Session.LastSuccess);
                }
                if (Session.Finished) { Finish(); return; }
            }
            if (view) view.Render(this, Running && !IsPaused ? (float)dt : 0);
        }

        void CaptureVR(double dt) {
            var player = house.game.player;
            bool headValid = InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
            Vector3 left = Vector3.zero, right = Vector3.zero;
            bool leftValid = DevicePosition(XRNode.LeftHand, out left), rightValid = DevicePosition(XRNode.RightHand, out right);
            if (!player.View) return;
            SubmitTracking(player.View.transform.position, left, right, headValid, leftValid, rightValid, dt);
            trackingLostFor = headValid && (leftValid || rightValid) ? 0 : trackingLostFor + dt;
            if (trackingLostFor > .75 && Running && !house.game.ui.IsOpen) {
                PauseMenu();
                house.game.ui.Toast("Tracking lost / game paused. Restore headset and controller tracking, then resume or recenter.");
                trackingLostFor = 0;
            }
        }

        bool DevicePosition(XRNode node, out Vector3 world) {
            world = Vector3.zero;
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked ||
                !device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position)) return false;
            var origin = house.game.player.origin;
            Transform space = origin && origin.CameraFloorOffsetObject ? origin.CameraFloorOffsetObject.transform : house.game.player.xrRig.transform;
            world = space.TransformPoint(position); return true;
        }

        /// <summary>Shared tracked-pose path. World coordinates, with tracking validity checked before contacts.</summary>
        public void SubmitTracking(Vector3 head, Vector3 left, Vector3 right, bool headValid, bool leftValid, bool rightValid, double dt) {
            headValid &= Finite(head); leftValid &= Finite(left); rightValid &= Finite(right);
            HeadTracked = headValid; LeftTracked = leftValid; RightTracked = rightValid;
            if (leftHandVisual) { leftHandVisual.gameObject.SetActive(Running && leftValid); if (leftValid) leftHandVisual.position = left; }
            if (rightHandVisual) { rightHandVisual.gameObject.SetActive(Running && rightValid); if (rightValid) rightHandVisual.position = right; }
            if (!AcceptsInput) return;
            HeadOffset = headValid ? transform.InverseTransformVector(head - NeutralHead) : Vector3.zero;
            var cue = Session.Current;
            if (cue == null || Session.CueResolved) return;
            bool newCue = lastCue != Session.CurrentIndex;
            if (newCue) lastCue = Session.CurrentIndex;
            for (int target = 0; target < targetPoints.Length && target < 6; target++) {
                if (!targetPoints[target]) continue;
                bool relevant = !cue.IsDodge && (cue.First == target || cue.Second == target);
                for (int hand = 0; hand < 2; hand++) {
                    bool valid = hand == 0 ? leftValid : rightValid;
                    // Tracking loss is not a real withdrawal. Reacquiring a resting hand must not score.
                    if (!valid) { armed[hand, target] = false; continue; }
                    bool inside = Vector3.Distance(hand == 0 ? left : right, targetPoints[target].position) <= .18f;
                    if (newCue) armed[hand, target] = !inside;
                    if (!inside) armed[hand, target] = true;
                    else if (relevant && armed[hand, target]) {
                        armed[hand, target] = false; HandleHit(Session.Hit(target, hand));
                    }
                }
            }
            if (headValid) Session.ObserveDodge(HeadOffset.x, HeadOffset.y, dt);
        }
        static bool Finite(Vector3 position) => !(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
            float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z));

        public void DesktopStrike(int target, int hand) {
            if (!AcceptsInput || IsVR || Session == null) return;
            HandleHit(Session.Hit(target, hand));
        }
        public void DesktopPair(int hoveredTarget) {
            if (!AcceptsInput || IsVR || Session.Current == null || Session.Current.Kind != WorkshopCueKind.Double) return;
            var cue = Session.Current;
            if (hoveredTarget != cue.First && hoveredTarget != cue.Second) return;
            HandleHit(Session.Hit(cue.First, 0)); HandleHit(Session.Hit(cue.Second, 1));
            if (leftHandVisual && targetPoints[cue.First]) { leftHandVisual.gameObject.SetActive(true); leftHandVisual.position = targetPoints[cue.First].position; }
            if (rightHandVisual && targetPoints[cue.Second]) { rightHandVisual.gameObject.SetActive(true); rightHandVisual.position = targetPoints[cue.Second].position; }
        }
        public void DesktopPose(float lean, bool duck, double dt) {
            if (!Running || IsVR || !house || !house.game || !house.game.player.desktopCamera) return;
            if (IsPaused) return;
            Vector3 offset = new Vector3(Mathf.Clamp(lean, -1, 1) * .24f, duck ? -.25f : 0, 0);
            house.game.player.desktopCamera.transform.localPosition = desktopRest + offset;
            HeadOffset = offset; HeadTracked = true;
            if (AcceptsInput) Session.ObserveDodge(offset.x, offset.y, dt);
        }
        void HandleHit(WorkshopHit result) {
            if (!view) return;
            if (result == WorkshopHit.Partial) view.Feedback("PAIR / reach the other target with your OTHER hand", true);
            else if (result == WorkshopHit.WrongHand) view.Feedback("USE BOTH HANDS / one hand on each target", false);
        }

        void CalibratePose() {
            var player = house.game.player;
            if (!player.View) return;
            NeutralHead = player.View.transform.position; HeadOffset = Vector3.zero;
            if (player.IsVR) {
                Vector3 head = transform.InverseTransformPoint(NeutralHead);
                for (int i = 0; i < targetPoints.Length && i < 6; i++) if (targetPoints[i])
                    targetPoints[i].localPosition = head + new Vector3((i % 3 - 1) * .36f, i < 3 ? -.30f : .02f, .50f);
            }
            lastCue = -1;
        }
        public void Recalibrate() {
            if (!Running || !Allowed()) return;
            if (!IsVR && house.game.player.desktopCamera) house.game.player.desktopCamera.transform.localPosition = desktopRest;
            CalibratePose();
            if (view) view.Feedback("NEUTRAL POSE SAVED / comfortable reach, small lean or duck", true);
            house.game.ui.Close(); lastTime = Time.realtimeSinceStartupAsDouble;
        }
        public void PauseMenu() {
            if (!Running || !Allowed()) return;
            if (sound) sound.Silence();
            var ui = house.game.ui;
            ui.Show("Quick Hands / Paused", "Time and targets are frozen.\nReach for lit toys; use two hands on a pair. Follow LEFT / RIGHT / DUCK warnings.\nDesktop: left/right mouse = hands; both buttons = pair; A/D = lean; S = duck.\nRecenter while sitting or standing comfortably to adjust your neutral pose.");
            ui.Button("Resume", () => { lastTime = Time.realtimeSinceStartupAsDouble; ui.Close(); });
            ui.Button("Recenter reach", Recalibrate); ui.Button("Restart", Begin);
            ui.Button("Leave workshop", () => { Stop(); ui.Close(); });
        }
        void Finish() {
            bool passed = Session.Passed;
            var game = house.game;
            game.state.carpenterBestScore = Math.Max(game.state.carpenterBestScore, Session.Score); game.Commit();
            Stop();
            if (passed) { house.Win(); return; }
            game.ui.Show("Quick Hands / Keep trying!", "Score " + Session.Score + " / goal " + ActiveWorkshopSession.Goal +
                "\nBest combo " + Session.BestCombo + " / targets " + Session.TargetsHit + " / dodges " + Session.Dodges +
                "\nReach for the next toy, then watch the striped-board warning. Every retry has a fresh pattern.");
            game.ui.Button("Try again", Begin); game.ui.Button("Leave workshop", game.ui.Close);
        }
        public void Stop() {
            if (sound) sound.Silence();
            if (savedPlayer && house && house.game && house.game.player) {
                var player = house.game.player;
                player.ActivityLocksMovement = previousLock; player.ActivityUsesPointer = previousPointer; player.ActivityPrompt = previousPrompt;
                if (player.desktopCamera) { player.desktopCamera.transform.localRotation = previousCameraRotation; player.desktopCamera.transform.localPosition = previousCameraPosition; }
            }
            savedPlayer = false; Running = false; LeftTracked = RightTracked = false;
            if (leftHandVisual) leftHandVisual.gameObject.SetActive(false);
            if (rightHandVisual) rightHandVisual.gameObject.SetActive(false);
            if (obstacleVisual) obstacleVisual.gameObject.SetActive(false);
        }
        static void Haptic(bool success) {
            foreach (var node in new[] { XRNode.LeftHand, XRNode.RightHand }) {
                var device = InputDevices.GetDeviceAtXRNode(node);
                if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) device.SendHapticImpulse(0, success ? .22f : .08f, .045f);
            }
        }
        void OnDisable() { Stop(); }
    }
}
