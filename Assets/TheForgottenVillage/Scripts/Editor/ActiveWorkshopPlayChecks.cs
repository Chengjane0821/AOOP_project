using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ActiveWorkshopPlayChecks {
        static IEnumerator routine;
        static StringBuilder report;
        static int frame = -1;
        static readonly List<string> errors = new List<string>();
        static ActiveWorkshopRun live;
        static Keyboard keyboard;
        static Mouse mouse;
        static InputSettings.BackgroundBehavior oldBackground;
        static bool follow, leftDown, rightDown;
        static Vector2 pointer;
        static Key[] held = Array.Empty<Key>();
        static double deadline;
        static ActiveWorkshopPlayChecks() {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, trace, type) => {
                if (routine != null && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) errors.Add(message);
            };
        }
        [MenuItem("Forgotten Village/Test Carpenter quick hands workshop")]
        public static void Request() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit current Play Mode first.");
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type != null) EditorWindow.GetWindow(type).Show();
            SessionState.SetBool("ACTIVE_WORKSHOP_PENDING", true); EditorApplication.isPlaying = true;
        }
        static void Tick() {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (routine != null || SessionState.GetBool("ACTIVE_WORKSHOP_PENDING", false)) EditorApplication.QueuePlayerLoopUpdate();
            if (SessionState.GetBool("ACTIVE_WORKSHOP_PENDING", false) && Time.frameCount > 12) {
                SessionState.SetBool("ACTIVE_WORKSHOP_PENDING", false); report = new StringBuilder(); errors.Clear();
                routine = Run(); deadline = EditorApplication.timeSinceStartup + 220;
            }
            if (routine == null || frame == Time.frameCount) return;
            frame = Time.frameCount;
            try {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Quick Hands playtest exceeded 220 seconds.");
                if (!routine.MoveNext()) Finish(null);
            } catch (Exception exception) { Finish(exception); }
        }
        static void Finish(Exception exception) {
            if (live) live.GetComponent<ActiveWorkshopInput>().BeforeCapture = null;
            if (keyboard != null) { InputSystem.RemoveDevice(keyboard); InputSystem.settings.backgroundBehavior = oldBackground; }
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            live = null; keyboard = null; mouse = null; follow = leftDown = rightDown = false; held = Array.Empty<Key>();
            report.AppendLine(exception == null ? "PASS: Quick Hands Workshop Play Mode suite completed" : "FAIL: " + exception);
            foreach (string error in errors) report.AppendLine("Runtime error: " + error);
            report.AppendLine("Scope: actual Carpenter additive scene, injected mouse/keyboard, shared tracked-hand/head contact path, real-time count-in and 60-second failure/success, pause, memory, simulated mouse evidence, unique reward, disk save and re-entry. No success flags or physics/time scaling changed. Physical headset/controller feel and live Pi camera remain manual acceptance.");
            File.WriteAllText("Verification/active-workshop-playmode.txt", report.ToString()); routine = null;
            VillageStorage.overridePath = null; EditorApplication.isPlaying = false;
        }
        static void Check(bool condition, string message) {
            if (!condition) throw new Exception(message);
            report.AppendLine("PASS: " + message); File.WriteAllText("Verification/active-workshop-progress.txt", report.ToString());
        }
        static void Button(VillageGame game, string name) => game.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>()
            .First(button => button.gameObject.activeSelf && button.name == name).onClick.Invoke();
        static IEnumerable<object> Wait(double seconds) {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until) yield return null;
        }
        static IEnumerable<object> Transition(VillageGame game) {
            double until = Time.realtimeSinceStartupAsDouble + 30;
            while (game.transitions.Busy) { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException("House transition timed out."); yield return null; }
            yield return null;
        }
        static void Inject() {
            if (follow && live && live.Running) {
                held = Array.Empty<Key>(); leftDown = rightDown = false;
                var cue = live.Session.Current;
                if (cue != null && !live.Session.CueResolved) {
                    if (cue.IsDodge) held = new[] { cue.Kind == WorkshopCueKind.DodgeLeft ? Key.A : cue.Kind == WorkshopCueKind.DodgeRight ? Key.D : Key.S };
                    else if (live.Session.Elapsed - cue.Start > .10) {
                        pointer = live.house.game.player.View.WorldToScreenPoint(live.targetPoints[cue.First].position);
                        leftDown = Time.frameCount % 2 == 0; rightDown = leftDown && cue.Kind == WorkshopCueKind.Double;
                    }
                }
            }
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            InputState.Change(keyboard, new KeyboardState(held), InputUpdateType.Dynamic);
            var state = new MouseState { position = pointer };
            if (leftDown) state = state.WithButton(MouseButton.Left);
            if (rightDown) state = state.WithButton(MouseButton.Right);
            InputState.Change(mouse, state, InputUpdateType.Dynamic);
        }
        static void ControlRays(ActiveWorkshopRun run) {
            Physics.SyncTransforms(); var camera = run.house.game.player.View.transform;
            foreach (var control in run.GetComponentsInChildren<ActiveWorkshopControl>()) {
                var bounds = control.GetComponent<Collider>().bounds;
                foreach (var offset in new[] { Vector3.zero, Vector3.left, Vector3.right, Vector3.up, Vector3.down }) {
                    var target = bounds.center + Vector3.Scale(offset, bounds.extents) * .6f;
                    if (!Physics.Raycast(camera.position, (target - camera.position).normalized, out var hit, 10, ~0, QueryTriggerInteraction.Collide) ||
                        hit.collider.GetComponentInParent<ActiveWorkshopControl>() != control) throw new Exception("Obstructed workshop control: " + control.name);
                }
            }
        }
        // Uses the production pose/contact path while allowing the normal 60 seconds to elapse.
        static void DrivePose(ActiveWorkshopRun run) {
            var cue = run.Session.Current; if (cue == null || run.Session.CueResolved) return;
            Vector3 head = run.NeutralHead, away = run.NeutralHead - run.transform.forward;
            Vector3 left = away, right = away;
            if (cue.IsDodge) head += run.transform.TransformVector(cue.Kind == WorkshopCueKind.DodgeLeft ? Vector3.left * .24f :
                cue.Kind == WorkshopCueKind.DodgeRight ? Vector3.right * .24f : Vector3.down * .25f);
            else if (run.Session.Elapsed - cue.Start > .12) {
                left = run.targetPoints[cue.First].position;
                if (cue.Kind == WorkshopCueKind.Double) right = run.targetPoints[cue.Second].position;
            }
            run.SubmitTracking(head, left, right, true, true, true, Time.unscaledDeltaTime);
        }
        public static IEnumerable<object> CompleteCarpenter(VillageGame game) {
            var house = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopHouse>();
            if (!house) throw new Exception("Quick Hands Carpenter is absent.");
            house.run.Begin(); double until = Time.realtimeSinceStartupAsDouble + 75;
            while (house.run.Running) {
                if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException("Quick Hands completion timed out.");
                DrivePose(house.run); yield return null;
            }
            if (!game.state.carpenterArcadeWon) throw new Exception("Tracked-pose challenge did not pass.");
            Button(game, "Remember together"); game.recognition.simulation = true; game.recognition.Begin(2);
            game.recognition.SimulateDetection("mouse"); if (!game.Award(2)) throw new Exception("Carpenter mouse clue failed.");
        }
        static IEnumerator Run() {
            VillageStorage.overridePath = Path.GetFullPath("Verification/active-workshop-test-save.json");
            var game = UnityEngine.Object.FindFirstObjectByType<VillageGame>(); Check(game && game.transitions, "Village services initialized");
            game.NewGame(); foreach (var step in Transition(game)) yield return step;
            game.state.elder = true; game.ui.Close(); game.Enter(2); foreach (var step in Transition(game)) yield return step;
            var house = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopHouse>();
            Check(house && house.game == game, "Quick Hands binds in the actual Carpenter additive scene");
            Check(!UnityEngine.Object.FindFirstObjectByType<BridgeRun>() && !UnityEngine.Object.FindFirstObjectByType<EngineeringStation>(), "Bridge and amplifier removed from the active house");
            game.CompletePuzzle(2); Check(!game.state.puzzleSolved[2] && !game.Award(2), "Legacy completion and early clue cannot bypass the challenge");
            game.ui.Dialogue(2); Button(game, "Maybe later"); Check(!house.run.Running && !game.ui.IsOpen, "Invitation can be declined");
            var camera = game.player.desktopCamera.transform; var originalPosition = camera.localPosition; var originalRotation = camera.localRotation;
            game.ui.Dialogue(2); Button(game, "Let's play!"); live = house.run;
            Check(live.Running && game.player.ActivityLocksMovement && game.player.ActivityUsesPointer, "Challenge reserves movement and enables desktop pointer");
            ControlRays(live); Check(true, "All control centers and inner edges have unobstructed selection rays");
            Check(!live.AcceptsInput, "Three-second count-in blocks scoring");
            foreach (var step in Wait(ActiveWorkshopRun.CountIn + .38)) yield return step;
            var first = live.Session.Current; Check(first != null && first.Kind == WorkshopCueKind.Target, "Count-in opens a real target cue");
            Vector3 target = live.targetPoints[first.First].position, away = live.NeutralHead - live.transform.forward;
            live.SubmitTracking(new Vector3(float.NaN, 0, 0), new Vector3(float.PositiveInfinity, 0, 0), away, true, true, true, .016);
            Check(!live.HeadTracked && !live.LeftTracked && live.Session.TargetsHit == 0, "Nonfinite device poses are rejected before visual or scoring updates");
            live.SubmitTracking(live.NeutralHead, target, away, true, false, false, .016);
            live.SubmitTracking(live.NeutralHead, target, away, true, true, true, .016);
            live.SubmitTracking(live.NeutralHead, target, away, true, true, true, .016);
            Check(live.Session.TargetsHit == 0, "Hand resting inside a new target or reacquired tracking cannot auto-hit");
            live.SubmitTracking(live.NeutralHead, away, away, true, true, true, .016);
            live.SubmitTracking(live.NeutralHead, target, away, true, false, true, .016);
            Check(live.Session.TargetsHit == 0, "Invalid hand tracking never scores a contact");
            live.SubmitTracking(live.NeutralHead, away, away, true, true, true, .016);
            live.SubmitTracking(live.NeutralHead, target, away, true, true, true, .016);
            Check(live.Session.TargetsHit == 1, "Leaving and reaching back into a target scores through the tracked-hand path");
            int hits = live.Session.TargetsHit; live.SubmitTracking(live.NeutralHead, target, away, true, true, true, .016);
            Check(live.Session.TargetsHit == hits, "Held contact cannot repeatedly score the same target");
            game.ui.Pause(); double paused = live.Elapsed; int revision = live.Session.Revision;
            foreach (var step in Wait(.45)) yield return step;
            Check(live.IsPaused && Math.Abs(live.Elapsed - paused) < .001 && live.Session.Revision == revision, "Pause freezes countdown, cue windows and score");
            Button(game, "Resume");
            double timeout = Time.realtimeSinceStartupAsDouble + 66;
            while (live.Running) { if (Time.realtimeSinceStartupAsDouble > timeout) throw new TimeoutException("Idle challenge failed to end."); yield return null; }
            Check(live.Session.Finished && !live.Session.Passed && !game.state.carpenterArcadeWon, "Full 60-second idle failure cannot award progress");
            Check(!game.player.ActivityLocksMovement && !game.player.ActivityUsesPointer && Vector3.Distance(camera.localPosition, originalPosition) < .001f, "Failure restores camera and activity controls");
            Button(game, "Try again");
            oldBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); live.GetComponent<ActiveWorkshopInput>().BeforeCapture = Inject; follow = true;
            bool captured = false; timeout = Time.realtimeSinceStartupAsDouble + 75;
            while (live.Running) {
                if (Time.realtimeSinceStartupAsDouble > timeout) throw new TimeoutException("Mouse/key challenge timed out.");
                if (!captured && live.Session.Current != null && live.Session.Current.Kind == WorkshopCueKind.Double && !live.Session.CueResolved) {
                    VillageAutomation.Capture(game, "active-workshop-play-preview"); captured = true;
                }
                yield return null;
            }
            follow = false; held = Array.Empty<Key>(); leftDown = rightDown = false;
            Check(live.Session.Passed && live.Session.TargetsHit > 10 && live.Session.Dodges > 2, "Actual mouse hits, two-button pairs and A/D/S dodges complete a real 60-second run");
            Check(game.state.carpenterArcadeWon && game.state.puzzleSolved[2] && !game.state.clues[2], "Success unlocks memory without granting the clue early");
            Check(game.state.carpenterBestScore >= live.Session.Score && game.state.carpenterBestScore >= ActiveWorkshopSession.Goal, "Completed score persists as the personal best");
            Check(!game.player.ActivityLocksMovement && !game.player.ActivityUsesPointer && Vector3.Distance(camera.localPosition, originalPosition) < .001f && Quaternion.Angle(camera.localRotation, originalRotation) < .001f, "Success restores camera position rotation movement and pointer");
            Button(game, "Remember together"); Check(game.state.carpenterMemoryRecovered && !game.state.clues[2], "Recovered memory still requires original mouse evidence");
            game.recognition.simulation = true; game.recognition.Begin(2); Check(game.recognition.SimulateDetection("mouse"), "Existing mouse object quest accepts explicitly simulated evidence");
            game.ui.ObjectQuest(2); Button(game, "Collect clue"); game.Award(2);
            Check(game.state.CanClaim(2) && game.state.inventory.Count(item => item == VillageGame.Clues[2]) == 1, "Mouse clue is awarded once after current activity memory and evidence");
            var saved = VillageStorage.Load(); Check(saved.version == 8 && saved.carpenterArcadeWon && saved.clues[2] && saved.carpenterBestScore == game.state.carpenterBestScore, "Actual JSON disk save retains arcade victory clue and best score");
            live.GetComponent<ActiveWorkshopInput>().BeforeCapture = null;
            game.Exit(); foreach (var step in Transition(game)) yield return step;
            game.Enter(2); foreach (var step in Transition(game)) yield return step;
            house = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopHouse>(); live = house.run;
            Check(house && game.state.CanClaim(2) && !live.Running, "Additive exit and re-entry restore earned progress");
            live.Begin(); live.PauseMenu(); Button(game, "Recenter reach"); Check(live.Running && !game.ui.IsOpen, "Recenter action resumes using a fresh neutral pose");
            game.ui.Menu(); yield return null; yield return null;
            Check(!live.Running && !game.player.ActivityLocksMovement && !game.player.ActivityUsesPointer, "Returning to title cancels the activity and releases controls");
            Check(errors.Count == 0, "No runtime errors captured during the complete Quick Hands suite");
        }
    }
}
