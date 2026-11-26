using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ForgottenVillage {
    [RequireComponent(typeof(ActiveWorkshopInput), typeof(ActiveWorkshopAudio))]
    public sealed class ActiveWorkshopRun : MonoBehaviour {
        public const double CountIn = 3;
        const float ContactRadius = .145f;
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
        public bool AcceptsInput => Running && !changingRoom && Allowed() && !house.game.ui.IsOpen && Elapsed >= CountIn;
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
        Vector3 previousCameraPosition, desktopRest, returnPosition, houseArenaPosition;
        float returnYaw;
        Vector3 desktopVelocity;
        GameObject shed;
        Image curtain;
        bool changingRoom;
        double lastTime;
        int lastCue = -1, lastRevision;
        ActiveWorkshopAudio sound;
        double trackingLostFor;

        bool Allowed() => house && house.game && house.game.playing && house.game.house == 2 && house.game.state.elder &&
            (!house.game.transitions || !house.game.transitions.Busy);
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
        public void Begin() => StartRound(false);
        public void BeginPractice() => StartRound(true);
        public void EnterShed(bool practice) {
            if (!changingRoom && Allowed()) StartCoroutine(ChangeRoom(() => StartRound(practice)));
        }

        IEnumerator ChangeRoom(Action move) {
            changingRoom = true;
            if (!curtain) {
                var canvas = new GameObject("Workshop transition", typeof(RectTransform), typeof(Canvas));
                canvas.transform.SetParent(transform, false);
                var display = canvas.GetComponent<Canvas>();
                display.renderMode = RenderMode.WorldSpace;
                display.sortingOrder = short.MaxValue;
                curtain = VillageUI.Rect("Fade", canvas.transform, new Vector2(4, 4), Vector2.zero).gameObject.AddComponent<Image>();
                curtain.material = house.game.ui.panelMaterial;
                curtain.raycastTarget = false;
            }
            curtain.transform.parent.gameObject.SetActive(true);
            for (float time = 0; time < .18f; time += Time.unscaledDeltaTime) {
                DrawCurtain(Mathf.SmoothStep(0, 1, time / .18f));
                yield return null;
            }
            DrawCurtain(1);
            if (Allowed()) move();
            for (float time = 0; time < .22f; time += Time.unscaledDeltaTime) {
                DrawCurtain(Mathf.SmoothStep(1, 0, time / .22f));
                yield return null;
            }
            curtain.transform.parent.gameObject.SetActive(false);
            changingRoom = false;
        }

        void DrawCurtain(float alpha) {
            var camera = house.game.player.View.transform;
            curtain.transform.parent.SetPositionAndRotation(camera.position + camera.forward * .35f, camera.rotation);
            curtain.color = new Color(0, 0, 0, alpha);
        }

        void StartRound(bool practice) {
            if (!Allowed()) return;
            RecordTargets();
            var player = house.game.player;
            if (!savedPlayer) {
                previousLock = player.ActivityLocksMovement; previousPointer = player.ActivityUsesPointer;
                previousPrompt = player.ActivityPrompt;
                returnPosition = player.IsVR
                    ? player.View.transform.position - Vector3.up * player.origin.CameraInOriginSpaceHeight
                    : player.transform.position;
                returnYaw = player.IsVR ? player.origin.transform.eulerAngles.y : player.transform.eulerAngles.y;
                if (player.desktopCamera) {
                    previousCameraRotation = player.desktopCamera.transform.localRotation;
                    previousCameraPosition = player.desktopCamera.transform.localPosition;
                }
                houseArenaPosition = transform.position;
                // Keep the shed inside the loaded house scene, above the original room.
                transform.position += Vector3.up * 24;
                if (!shed) BuildShed();
                shed.SetActive(true);
            }
            savedPlayer = true; player.ActivityLocksMovement = true; player.ActivityUsesPointer = !player.IsVR; player.ActivityPrompt = "";
            var point = player.IsVR && vrPoint ? vrPoint : desktopPoint;
            if (point) player.Teleport(point.position, point.eulerAngles.y);
            if (!player.IsVR && player.desktopCamera) {
                player.desktopCamera.transform.localRotation = Quaternion.identity;
                player.desktopCamera.transform.localPosition = previousCameraPosition;
                desktopRest = player.desktopCamera.transform.localPosition;
                for (int i = 0; i < targetPoints.Length && i < 6; i++) if (targetPoints[i]) {
                    var authored = authoredTargets[i];
                    targetPoints[i].localPosition = new Vector3(authored.x * 1.55f, authored.y, authored.z + .30f);
                }
            }
            Physics.SyncTransforms();
            Session = new ActiveWorkshopSession(seedOverride < 0 ? UnityEngine.Random.Range(0, int.MaxValue) : seedOverride, practice);
            Elapsed = 0; lastCue = -1; lastRevision = 0; HeadOffset = Vector3.zero; trackingLostFor = 0;
            desktopVelocity = Vector3.zero;
            LeftTracked = RightTracked = false; HeadTracked = true;
            house.game.ui.Close(); if (house.game.ui.notice) house.game.ui.notice.text = "";
            Running = true; CalibratePose(); lastTime = Time.realtimeSinceStartupAsDouble;
            if (view) view.Feedback("GET READY / stand or sit comfortably, hands in front", true);
        }

        void BuildShed() {
            shed = new GameObject("Quick Hands practice shed");
            shed.transform.SetParent(transform, false);
            var finish = new MaterialPropertyBlock();
            var surface = view && view.targetRenderers.Length > 0 && view.targetRenderers[0]
                ? view.targetRenderers[0].sharedMaterial : null;
            void Panel(string label, Vector3 center, Vector3 size, Color color) {
                var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                panel.name = label;
                panel.transform.SetParent(shed.transform, false);
                panel.transform.localPosition = center;
                panel.transform.localScale = size;
                panel.GetComponent<Collider>().enabled = false;
                var renderer = panel.GetComponent<Renderer>();
                if (surface) renderer.sharedMaterial = surface;
                finish.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(finish);
            }
            var timber = new Color(.30f, .21f, .14f);
            var wall = new Color(.21f, .26f, .29f);
            Panel("Floor", new Vector3(0, .025f, 0), new Vector3(7, .20f, 8), timber);
            Panel("Ceiling", new Vector3(0, 4, 0), new Vector3(7, .20f, 8), timber);
            Panel("Back wall", new Vector3(0, 2, 4), new Vector3(7, 4, .18f), wall);
            Panel("Entry wall", new Vector3(0, 2, -4), new Vector3(7, 4, .18f), wall);
            for (int side = -1; side <= 1; side += 2) {
                Panel("Side wall", new Vector3(side * 3.5f, 2, 0), new Vector3(.18f, 4, 8), wall);
                for (int beam = -1; beam <= 1; beam++)
                    Panel("Wall stud", new Vector3(side * 3.32f, 2, beam * 2.6f), new Vector3(.18f, 4, .22f), timber);
            }
            var fixture = new GameObject("Warm overhead light");
            fixture.transform.SetParent(shed.transform, false);
            fixture.transform.localPosition = new Vector3(0, 3.5f, -1.5f);
            var light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10;
            light.intensity = 2;
            light.color = new Color(1, .87f, .73f);
        }

        void Update() {
            double now = Time.realtimeSinceStartupAsDouble;
            double dt = Math.Max(0, now - lastTime); lastTime = now;
            if (savedPlayer && (!Allowed() || (!Running && !house.game.ui.IsOpen))) { Stop(); return; }
            if (changingRoom) return;
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
                    bool inside = Vector3.Distance(hand == 0 ? left : right, targetPoints[target].position) <= ContactRadius;
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
            if (IsPaused || changingRoom) return;
            Vector3 offset = new Vector3(Mathf.Clamp(lean, -1, 1) * .24f, duck ? -.25f : 0, 0);
            HeadOffset = Vector3.SmoothDamp(HeadOffset, offset, ref desktopVelocity, .10f, Mathf.Infinity, (float)dt);
            house.game.player.desktopCamera.transform.localPosition = desktopRest + HeadOffset;
            HeadTracked = true;
            if (AcceptsInput) Session.ObserveDodge(HeadOffset.x, HeadOffset.y, dt);
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
                    targetPoints[i].localPosition = head + new Vector3((i % 3 - 1) * .44f, i < 3 ? -.32f : -.04f, i % 3 == 1 ? .59f : .62f);
            }
            lastCue = -1;
        }
        public void Recalibrate() {
            if (!Running || !Allowed()) return;
            if (!IsVR && house.game.player.desktopCamera) house.game.player.desktopCamera.transform.localPosition = desktopRest;
            CalibratePose();
            desktopVelocity = Vector3.zero;
            if (view) view.Feedback("NEUTRAL POSE SAVED / comfortable reach, small lean or duck", true);
            house.game.ui.Close(); lastTime = Time.realtimeSinceStartupAsDouble;
        }
        public void PauseMenu() {
            if (!Running || !Allowed()) return;
            if (sound) sound.Silence();
            var ui = house.game.ui;
            ui.Show("Quick Hands / Paused", "Time and targets are frozen.\nReach for lit targets; use two hands on a pair. Follow LEFT / RIGHT / DUCK warnings.\nDesktop: left/right mouse = hands; both buttons = pair; A/D = lean; S = duck.\nRecenter while sitting or standing comfortably to adjust your neutral pose.");
            ui.Button("Resume", () => { lastTime = Time.realtimeSinceStartupAsDouble; ui.Close(); });
            ui.Button("Recenter reach", Recalibrate); ui.Button("Restart", () => StartRound(Session.Practice));
            ui.Button("Leave workshop", () => ReturnToCarpenter(ui.Close));
        }
        void Finish() {
            Running = false;
            if (sound) sound.Silence();
            if (view) view.Render(this, 0);
            if (Session.Practice) {
                var ui = house.game.ui;
                ui.Show("Quick Hands / Practice complete", "Actions completed " + (Session.Cues.Length - Session.Misses) + " / " + Session.Cues.Length);
                ui.Button("Start challenge", Begin);
                ui.Button("Practice again", BeginPractice);
                ui.Button("Return to Carpenter", () => ReturnToCarpenter(ui.Close));
                return;
            }
            bool passed = Session.Passed;
            var game = house.game;
            game.state.carpenterBestScore = Math.Max(game.state.carpenterBestScore, Session.Score); game.Commit();
            if (passed) {
                Running = true;
                ReturnToCarpenter(house.Win);
                return;
            }
            game.ui.Show("Quick Hands / Keep trying!", "Score " + Session.Score + " / goal " + ActiveWorkshopSession.Goal +
                "\nAccuracy " + Session.AccuracyPercent + "% / goal " + ActiveWorkshopSession.MinimumAccuracyPercent + "%" +
                "\nBest combo " + Session.BestCombo + " / targets " + Session.TargetsHit + " / dodges " + Session.Dodges +
                "\nEvery retry has a fresh pattern.");
            game.ui.Button("Try again", Begin); game.ui.Button("Return to Carpenter", () => ReturnToCarpenter(game.ui.Close));
        }
        void ReturnToCarpenter(Action next) {
            if (!changingRoom) StartCoroutine(ChangeRoom(() => { Stop(); next(); }));
        }
        public void Stop() {
            if (sound) sound.Silence();
            if (savedPlayer && house && house.game && house.game.player) {
                var player = house.game.player;
                if (house.game.house == 2) player.Teleport(returnPosition, returnYaw);
                player.ActivityLocksMovement = previousLock; player.ActivityUsesPointer = previousPointer; player.ActivityPrompt = previousPrompt;
                if (player.desktopCamera) { player.desktopCamera.transform.localRotation = previousCameraRotation; player.desktopCamera.transform.localPosition = previousCameraPosition; }
            }
            if (savedPlayer) {
                transform.position = houseArenaPosition;
                if (shed) shed.SetActive(false);
                for (int i = 0; i < targetPoints.Length && i < 6; i++)
                    if (targetPoints[i]) targetPoints[i].localPosition = authoredTargets[i];
                Physics.SyncTransforms();
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
