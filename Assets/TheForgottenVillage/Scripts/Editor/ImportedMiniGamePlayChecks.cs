using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ImportedMiniGamePlayChecks {
        static IEnumerator routine;
        static int frame;
        static StringBuilder report;
        static readonly List<string> errors = new List<string>();
        static string originalScene;
        static TestRunnerApi testApi;
        static Mouse testMouse;
        static InputSettings.BackgroundBehavior oldBackground;
        static string ReportPrefix => SessionState.GetBool("IMPORTED_TOOLS_PREVIEW", false) ? "carpenter-tools-preview" : "imported-mini-games";
        static ImportedMiniGamePlayChecks() {
            if (SessionState.GetBool("IMPORTED_MINI_ACTIVE", false)) {
                SessionState.SetBool("IMPORTED_MINI_ACTIVE", false);
                File.WriteAllText("Verification/" + ReportPrefix + "-playmode.txt", "INTERRUPTED: Unity reloaded scripts during the test. Run again after imports complete; this is not a passing result.");
                EditorApplication.delayCall += () => { if (EditorApplication.isPlaying) EditorApplication.isPlaying = false; };
            }
            EditorApplication.update += Tick;
            Application.logMessageReceived += (m, s, t) => { if (routine != null && (t == LogType.Error || t == LogType.Exception)) errors.Add(m); };
        }
        public static void UnitTests() {
            testApi = ScriptableObject.CreateInstance<TestRunnerApi>(); testApi.RegisterCallbacks(new Results());
            testApi.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "ForgottenVillage.Tests" } }));
        }
        sealed class Results : ICallbacks {
            public void RunStarted(ITestAdaptor t) { } public void TestStarted(ITestAdaptor t) { } public void TestFinished(ITestResultAdaptor t) { }
            public void RunFinished(ITestResultAdaptor result) {
                TestRunnerApi.SaveResultToFile(result, "Verification/imported-mini-games-editmode.xml");
                File.WriteAllText("Verification/imported-mini-games-editmode.txt", "Passed: " + result.PassCount + "; failed: " + result.FailCount + "; skipped: " + result.SkipCount + "\n" + result.ResultState);
            }
        }
        [MenuItem("Forgotten Village/Test imported mini-games and progression")]
        public static void Request() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Leave existing Play Mode first.");
            originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            SessionState.SetString("IMPORTED_ORIGINAL_SCENE", originalScene);
            SessionState.SetBool("IMPORTED_TOOLS_PREVIEW", false);
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            SessionState.SetBool("IMPORTED_MINI_PENDING", true); EditorApplication.isPlaying = true;
        }
        public static void PreviewTools() { Request(); SessionState.SetBool("IMPORTED_TOOLS_PREVIEW", true); }
        static void Tick() {
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (routine != null || SessionState.GetBool("IMPORTED_MINI_PENDING", false)) EditorApplication.QueuePlayerLoopUpdate();
            if (SessionState.GetBool("IMPORTED_MINI_PENDING", false) && Time.frameCount > 15) {
                SessionState.SetBool("IMPORTED_MINI_PENDING", false); SessionState.SetBool("IMPORTED_MINI_ACTIVE", true); report = new StringBuilder(); errors.Clear(); routine = SessionState.GetBool("IMPORTED_TOOLS_PREVIEW", false) ? PreviewToolsRun() : Run();
            }
            if (routine == null || frame == Time.frameCount) return; frame = Time.frameCount;
            try { if (!routine.MoveNext()) Finish(null); } catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception error) {
            if (testMouse != null) { InputSystem.RemoveDevice(testMouse); testMouse = null; InputSystem.settings.backgroundBehavior = oldBackground; }
            report.AppendLine(error == null ? (SessionState.GetBool("IMPORTED_TOOLS_PREVIEW", false) ? "PASS: tool presentation preview completed" : "PASS: imported mini-game suite completed") : "FAIL: " + error);
            foreach (var message in errors) report.AppendLine("Runtime error: " + message);
            report.AppendLine("Scope: actual additive scenes and normal elapsed time, simulated XR selection and tracked poses; temporary save, explicitly simulated camera evidence. No physical Quest/Pi/Arduino acceptance testing.");
            File.WriteAllText("Verification/" + ReportPrefix + "-playmode.txt", report.ToString());
            SessionState.SetBool("IMPORTED_MINI_ACTIVE", false); routine = null; VillageStorage.overridePath = null; EditorApplication.isPlaying = false;
        }
        static void Check(bool condition, string message) {
            if (!condition) throw new Exception(message);
            report.AppendLine("PASS: " + message); File.WriteAllText("Verification/" + ReportPrefix + "-progress.txt", report.ToString());
        }
        static IEnumerable<object> Wait(float seconds) {
            double end = Time.realtimeSinceStartupAsDouble + seconds; while (Time.realtimeSinceStartupAsDouble < end) yield return null;
        }
        static IEnumerable<object> Transition(VillageGame g) {
            double end = Time.realtimeSinceStartupAsDouble + 30;
            while (g.transitions.Busy) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("House transition"); yield return null; }
            yield return null;
        }
        static void Button(VillageGame g, string name) { g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.gameObject.activeSelf && b.name == name).onClick.Invoke(); }
        static void Shoot(VillageGame g, PlanetaryDefenceThreat threat, XRRayInteractor interactor) {
            if (threat.Targeted) return;
            interactor.transform.position = g.player.View.transform.position;
            var target = threat.GetComponent<XRSimpleInteractable>();
            if (!g.interactionManager.TryGetInteractableForCollider(threat.GetComponent<Collider>(), out _)) throw new Exception("Threat collider not registered for XR.");
            g.interactionManager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)target);
            if (interactor.interactablesSelected.Contains(target)) g.interactionManager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)target);
        }
        public static IEnumerable<object> CompleteTechnician(VillageGame g) {
            var run = UnityEngine.Object.FindFirstObjectByType<PlanetaryDefenceRun>();
            run.Begin(); var fixture = new GameObject("Editor-only defence selection fixture");
            var interactor = fixture.AddComponent<XRRayInteractor>(); interactor.interactionManager = g.interactionManager;
            double end = Time.realtimeSinceStartupAsDouble + 60;
            // Exercise the actual desktop ray/input route once, then the shared
            // XR selection route for the rest of the wave.
            if (!g.player.IsVR) {
                while (!run.AcceptsInput || !run.threats.Any(t => t.Live)) { if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("First ship"); yield return null; }
                var first = run.threats.First(t => t.Live);
                oldBackground = InputSystem.settings.backgroundBehavior; InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                testMouse = InputSystem.AddDevice<Mouse>();
                var pointer = (Vector2)g.player.View.WorldToScreenPoint(first.transform.position);
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = pointer }); yield return null;
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = pointer }.WithButton(MouseButton.Left)); yield return null;
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = pointer }); yield return null;
                if (!first.Targeted && first.Live) throw new Exception("Desktop mouse ray did not launch an interceptor.");
                InputSystem.RemoveDevice(testMouse); testMouse = null; InputSystem.settings.backgroundBehavior = oldBackground;
            }
            while (run.Running) {
                if (Time.realtimeSinceStartupAsDouble > end) throw new TimeoutException("Defence wave");
                if (run.AcceptsInput) foreach (var threat in run.threats) if (threat.Live && !threat.Targeted) { Shoot(g, threat, interactor); break; }
                yield return null;
            }
            UnityEngine.Object.Destroy(fixture);
            if (!run.Session.Won) throw new Exception("Defence did not win through real interceptors.");
            Button(g, "Recover diagnostic log"); g.recognition.simulation = true; g.recognition.Begin(3);
            if (!g.recognition.SimulateDetection("cell phone") || !g.Award(3)) throw new Exception("Technician reward path failed.");
        }
        static IEnumerator Run() {
            VillageStorage.overridePath = Path.GetFullPath("Temp/imported-mini-games-test-save.json");
            var g = UnityEngine.Object.FindFirstObjectByType<VillageGame>(); Check(g && g.transitions, "Existing village services loaded");
            g.NewGame(); foreach (var step in Transition(g)) yield return step;
            g.state.elder = true; g.ui.Close(); g.Enter(3); foreach (var step in Transition(g)) yield return step;
            var owner = UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>(); var run = owner.defence;
            Check(run && run.house == owner && !owner.run && !UnityEngine.Object.FindFirstObjectByType<PowerGridRun>(), "Planetary Defence replaces active PowerGrid inside the retained house");
            Check(!UnityEngine.Object.FindFirstObjectByType<global::Game>(), "Imported desktop singleton game/camera was not loaded");
            owner.Win(); Check(!g.state.technicianGridWon && !g.Award(3), "Early completion/clue cannot bypass defence");
            var originalSky = RenderSettings.skybox; var originalClear = g.player.View.clearFlags;
            g.ui.Dialogue(3); Button(g, "Defend planet"); Check(run.Running && g.player.ActivityLocksMovement, "Existing Technician invitation starts defence");
            Check(run.presentation.gameObject.activeInHierarchy && Vector3.Distance(g.player.transform.position, run.returnPoint.position) > 200 && g.house == 3, "Shared player teleports into large isolated space arena while Technician house remains loaded");
            Check(run.roomObjects.All(o => !o || !o.activeSelf) && !g.ui.tracker.gameObject.activeSelf, "Room geometry and normal tracker are hidden during orbital defence");
            foreach (var step in Wait(3.4f)) yield return step;
            Check(run.threats.Any(t => t.Live), "Incoming imported enemy ships spawn after countdown");
            var enemy = run.threats.First(t => t.Live); Vector3 before = enemy.transform.position;
            double elapsed = run.Session.Elapsed; run.PauseMenu(); foreach (var step in Wait(.6f)) yield return step;
            Check(run.Session.Elapsed == elapsed && enemy.transform.position == before, "Pause freezes threat movement and wave clock");
            Button(g, "Resume"); foreach (var step in Wait(.3f)) yield return step;
            Check(enemy.transform.position != before, "Resume restarts incoming threat movement");
            VillageAutomation.Capture(g, "planetary-defence-play-preview");
            double end = Time.realtimeSinceStartupAsDouble + 30;
            while (run.Running && Time.realtimeSinceStartupAsDouble < end) yield return null;
            Check(!run.Running && run.Session.Failed && run.Session.Impacts == 5 && !g.state.technicianGridWon, "Five actual planet impacts fail without awarding progress");
            Check(!run.presentation.gameObject.activeSelf && RenderSettings.skybox == originalSky && g.player.View.clearFlags == originalClear && Vector3.Distance(g.player.transform.position, run.returnPoint.position) < .3f, "Failure restores house position, sky, camera and room presentation");
            Button(g, "Retry defence"); Check(run.Session.Integrity == 5 && run.Session.Spawned == 0, "Retry resets planet and pooled wave");
            run.PauseMenu(); Button(g, "Leave defence");
            Check(!run.Running && !run.presentation.gameObject.activeSelf && g.ui.tracker.gameObject.activeSelf && !g.player.ActivityLocksMovement, "Leaving the orbital arena restores the house and shared player controls");
            run.Begin(); g.Exit();
            foreach (var step in Transition(g)) yield return step;
            Check(!g.player.ActivityLocksMovement && g.ui.tracker.gameObject.activeSelf && Vector3.Distance(g.player.transform.position, g.entrances[3].position) < .3f, "Unloading a running arena keeps the village return destination and releases shared controls");
            g.Enter(3); foreach (var step in Transition(g)) yield return step;
            owner = UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>(); run = owner.defence;
            foreach (var step in CompleteTechnician(g)) yield return step;
            Check(run.Session.Destroyed == 20 && run.Session.Impacts == 0 && run.Session.Won, "Actual desktop click and simulated XR selections launch real moving interceptors and clear all twenty ships");
            Check(g.state.technicianGridWon && g.state.puzzleSolved[3] && g.state.technicianMemoryRecovered && g.state.faultyTest && g.state.fixedTest && g.state.clues[3], "Original Technician victory, recovery, controller records and cell-phone clue path retained");
            Check(!g.player.ActivityLocksMovement && !g.player.ActivityUsesPointer, "Defence restores movement and pointer after completion");
            Check(VillageStorage.Load().clues[3], "Existing save schema round-trip retains earned Technician clue");
            g.ui.Close(); g.Exit();
            Check(Vector3.Distance(g.player.transform.position, g.entrances[3].position) < .01f, "Technician exit teleports to original village destination");
            foreach (var step in Transition(g)) yield return step;
            Check(g.house == -1 && !g.transitions.Current, "Technician scene unload completes after leaving defence");
            g.Enter(2); foreach (var step in Transition(g)) yield return step;
            var workshop = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopRun>(); var display = workshop.obstacleVisual.GetComponent<WorkshopToolVisual>();
            Check(display && display.tools.Length == 3 && display.tools.All(t => t), "Three imported Carpenter tool variants are bound to original obstacle transform");
            Check(workshop.obstacleVisual.GetComponentsInChildren<Collider>(true).Length == 3 && workshop.obstacleVisual.GetComponentsInChildren<Collider>(true).All(c => c.isTrigger) && workshop.obstacleVisual.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Tool colliders fit visuals and cannot physically shove the player");
            var observed = new HashSet<string>(); bool captured = false;
            foreach (var step in ActiveWorkshopPlayChecks.CompleteCarpenter(g)) {
                if (workshop.obstacleVisual.gameObject.activeInHierarchy) {
                    if (display.instructionsPanel && display.instructionsPanel.activeSelf) throw new Exception("Instructions obscure flying tools.");
                    foreach (var tool in display.tools) if (tool.gameObject.activeInHierarchy) {
                        observed.Add(tool.name);
                        var bounds = tool.GetComponentsInChildren<Renderer>().First().bounds;
                        CheckSize(bounds.size);
                    }
                    if (!captured) { VillageAutomation.Capture(g, "carpenter-flying-tools-preview"); captured = true; }
                }
                yield return step;
            }
            Check(observed.Count == 3, "Hammer, saw and crowbar all appear on original timed dodge cues with undistorted dimensions");
            Check(display.instructionsPanel && display.instructionsPanel.activeSelf, "Instructions hide only during tool warnings and return after the challenge");
            Check(g.state.carpenterArcadeWon && g.state.carpenterBestScore >= 900 && g.state.clues[2], "Unchanged 60-second Carpenter rules, tracked contacts/dodges, score and mouse clue still complete");
            g.ui.Close(); g.Exit();
            Check(Vector3.Distance(g.player.transform.position, g.entrances[2].position) < .01f, "Carpenter exit teleports to original village destination before normal ground settling");
            foreach (var step in Transition(g)) yield return step;
            Check(g.house == -1 && !g.transitions.Current, "Carpenter scene unload completes");
            Check(errors.Count == 0, "No runtime errors during imported mini-game checks");
        }
        static void CheckSize(Vector3 size) { if (Mathf.Max(size.x, size.y, size.z) < .3f || Mathf.Max(size.x, size.y, size.z) > 1.1f) throw new Exception("Flying tool scale invalid: " + size); }
        static IEnumerator PreviewToolsRun() {
            VillageStorage.overridePath = Path.GetFullPath("Temp/carpenter-tools-preview-save.json");
            var g = UnityEngine.Object.FindFirstObjectByType<VillageGame>(); g.NewGame();
            foreach (var step in Transition(g)) yield return step;
            g.state.elder = true; g.ui.Close(); g.Enter(2); foreach (var step in Transition(g)) yield return step;
            var run = UnityEngine.Object.FindFirstObjectByType<ActiveWorkshopRun>(); run.seedOverride = 14;
            var display = run.obstacleVisual.GetComponent<WorkshopToolVisual>(); Vector3 rest = display.scorePanel.localPosition;
            run.Begin(); double until = Time.realtimeSinceStartupAsDouble + 18;
            while (!run.obstacleVisual.gameObject.activeInHierarchy) { if (Time.realtimeSinceStartupAsDouble > until) throw new TimeoutException("First tool warning"); yield return null; }
            foreach (var step in Wait(.12f)) yield return step;
            Check(!display.instructionsPanel.activeSelf && display.scorePanel.localPosition.y > rest.y + .6f, "Tool warning clears the side card and lifts the existing direction/score HUD");
            VillageAutomation.Capture(g, "carpenter-flying-tools-preview"); run.Stop();
            Check(display.instructionsPanel.activeSelf && Vector3.Distance(display.scorePanel.localPosition, rest) < .001f, "Stopping restores the authored instruction and score panel positions");
            g.Exit(); foreach (var step in Transition(g)) yield return step;
            Check(errors.Count == 0, "Tool presentation preview has no runtime errors");
        }
    }
}
