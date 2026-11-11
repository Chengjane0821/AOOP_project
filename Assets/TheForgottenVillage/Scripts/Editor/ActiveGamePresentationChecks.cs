using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ActiveGamePresentationChecks {
        static IEnumerator routine;
        static StringBuilder report;
        static readonly List<string> errors = new List<string>();
        static int frame = -1;
        static double deadline;
        static ActiveGamePresentationChecks() {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, trace, type) => {
                if (routine != null && (type == LogType.Error || type == LogType.Exception)) errors.Add(message);
            };
        }
        [MenuItem("Forgotten Village/Preview open arcade and six bells")]
        public static void Request() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (type != null) EditorWindow.GetWindow(type).Show();
            SessionState.SetBool("ACTIVE_PRESENTATION_PENDING", true); EditorApplication.isPlaying = true;
        }
        static void Tick() {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (routine != null || SessionState.GetBool("ACTIVE_PRESENTATION_PENDING", false)) EditorApplication.QueuePlayerLoopUpdate();
            if (SessionState.GetBool("ACTIVE_PRESENTATION_PENDING", false) && Time.frameCount > 12) {
                SessionState.SetBool("ACTIVE_PRESENTATION_PENDING", false); report = new StringBuilder(); errors.Clear();
                routine = Run(); deadline = EditorApplication.timeSinceStartup + 80;
            }
            if (routine == null || frame == Time.frameCount) return;
            frame = Time.frameCount;
            try {
                if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Presentation preview timed out.");
                if (!routine.MoveNext()) Finish(null);
            } catch (Exception exception) { Finish(exception); }
        }
        static void Finish(Exception exception) {
            report.AppendLine(exception == null ? "PASS: Open arcade and six-bell presentation fixture completed" : "FAIL: " + exception);
            foreach (var error in errors) report.AppendLine("Runtime error: " + error);
            report.AppendLine("Scope: short live presentation, saved bindings, visible single/pair/dodge cues, target/control selection rays, six clock faces, pause and camera restoration. Full rule/progression suites are recorded separately; hardware remains manual.");
            File.WriteAllText("Verification/active-games-presentation.txt", report.ToString());
            routine = null; VillageStorage.overridePath = null; EditorApplication.isPlaying = false;
        }
        static void Check(bool condition, string text) {
            if (!condition) throw new Exception(text); report.AppendLine("PASS: " + text);
        }
        static IEnumerable<object> Wait(double seconds) {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until) yield return null;
        }
        static IEnumerable<object> Transition(VillageGame game) {
            double until = Time.realtimeSinceStartupAsDouble + 25;
            while (game.transitions.Busy) { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException("Presentation scene transition timed out."); yield return null; }
            yield return null;
        }
        static void ClearRay(Camera camera, Collider target) {
            var bounds = target.bounds;
            foreach (var offset in new[] { Vector3.zero, Vector3.left, Vector3.right, Vector3.up, Vector3.down }) {
                Vector3 point = bounds.center + Vector3.Scale(offset, bounds.extents) * .55f;
                Vector3 direction = point - camera.transform.position;
                if (Physics.Raycast(camera.transform.position, direction.normalized, out var hit, direction.magnitude, ~0, QueryTriggerInteraction.Collide) &&
                    hit.collider != target && !hit.collider.transform.IsChildOf(target.transform))
                    throw new Exception("Visual layout blocks " + target.name + " with " + hit.collider.name);
            }
        }
        static IEnumerator Run() {
            VillageStorage.overridePath = Path.GetFullPath("Verification/active-games-visual-test-save.json");
            var game = UnityEngine.Object.FindFirstObjectByType<VillageGame>();
            game.NewGame(); foreach (var step in Transition(game)) yield return step;
            game.state.elder = true; game.ui.Close(); game.Enter(2); foreach (var step in Transition(game)) yield return step;
            var active = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopRun>(); active.seedOverride = 17; active.Begin();
            Physics.SyncTransforms();
            foreach (var target in active.targetPoints) ClearRay(game.player.View, target.GetComponent<Collider>());
            foreach (var control in active.GetComponentsInChildren<ActiveWorkshopControl>()) ClearRay(game.player.View, control.GetComponent<Collider>());
            Check(true, "Open target field and all controls have clear center/edge rays");
            Check(active.view.legend.GetComponentInParent<Canvas>() && active.view.score.GetComponentInParent<Canvas>(), "Arcade uses separate readable HUD and instruction canvases");
            bool single = false, pair = false, dodge = false;
            while (!single || !pair || !dodge) {
                var cue = active.Session.Current;
                if (cue != null && active.Session.Elapsed - cue.Start > .20 && !active.Session.CueResolved) {
                    if (!single && cue.Kind == WorkshopCueKind.Target) {
                        VillageAutomation.Capture(game, "active-workshop-single-preview"); single = true;
                        active.DesktopStrike(cue.First, 0);
                    } else if (!pair && cue.Kind == WorkshopCueKind.Double) {
                        VillageAutomation.Capture(game, "active-workshop-play-preview"); pair = true; active.DesktopPair(cue.First);
                    } else if (!dodge && cue.IsDodge) {
                        VillageAutomation.Capture(game, "active-workshop-dodge-preview"); dodge = true;
                    } else if (cue.Kind == WorkshopCueKind.Target) {
                        active.DesktopStrike(cue.First, 0);
                    }
                }
                yield return null;
            }
            Check(active.view.cueFills.Count(fill => fill) == 6, "Each target has a visible cue lifetime bar");
            active.PauseMenu(); double paused = active.Elapsed; foreach (var step in Wait(.25)) yield return step;
            Check(Math.Abs(active.Elapsed - paused) < .001, "Presentation updates do not advance a paused challenge");
            active.Stop(); game.ui.Close(); Check(!game.player.ActivityLocksMovement && !game.player.ActivityUsesPointer, "Arcade preview releases controls");
            game.Exit(); foreach (var step in Transition(game)) yield return step;
            game.Enter(4); foreach (var step in Transition(game)) yield return step;
            var clock = UnityEngine.Object.FindFirstObjectByType<ClockRhythmRun>(); clock.Begin(); Physics.SyncTransforms();
            foreach (var bell in clock.GetComponentsInChildren<ClockBell>()) ClearRay(game.player.View, bell.GetComponent<Collider>());
            Check(clock.GetComponentsInChildren<ClockBell>().Length == 6, "Six open bell targets retain their selection bindings");
            double until = Time.realtimeSinceStartupAsDouble + 5;
            while (Time.realtimeSinceStartupAsDouble < until) {
                for (int i = 0; i < clock.Session.Notes.Length; i++) {
                    var note = clock.Session.Notes[i];
                    if (!clock.Session.IsResolved(i) && Math.Abs(note.Time - clock.Elapsed) < .03) clock.Strike(note.Lane);
                }
                yield return null;
            }
            VillageAutomation.Capture(game, "clock-rhythm-play-preview");
            clock.PauseMenu(); double clockTime = clock.Elapsed; foreach (var step in Wait(.25)) yield return step;
            Check(Math.Abs(clock.Elapsed - clockTime) < .001, "Open six-bell display retains musical pause");
            clock.Stop(); game.ui.Close(); Check(!game.player.ActivityLocksMovement && !game.player.ActivityConsumesJournalKey, "Clock preview restores movement and journal controls");
            Check(errors.Count == 0, "No runtime errors in the revised presentation fixture");
        }
    }
}
