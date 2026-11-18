using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class HouseTransitionPlayChecks {
        static IEnumerator routine;
        static int frame;
        static StringBuilder report;
        static HouseTransitionPlayChecks() { EditorApplication.update += Tick; }
        public static void Run() {
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            SessionState.SetBool("HOUSE_TRANSITION_CHECK", true);
            EditorApplication.isPlaying = true;
        }
        static void Tick() {
            if (!SessionState.GetBool("HOUSE_TRANSITION_CHECK", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || Time.frameCount < 15) return;
            if (routine == null) { report = new StringBuilder(); routine = CheckHouses(); }
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            try { if (!routine.MoveNext()) Finish(null); }
            catch (Exception e) { Finish(e); }
        }
        static void Finish(Exception error) {
            report.AppendLine(error == null ? "PASS: transition checks complete" : "FAIL: " + error);
            File.WriteAllText("Verification/house-transition-playmode.txt", report.ToString());
            VillageStorage.overridePath = null;
            SessionState.SetBool("HOUSE_TRANSITION_CHECK", false);
            EditorApplication.Exit(error == null ? 0 : 1);
        }
        static void Check(bool value, string message) {
            report.AppendLine((value ? "PASS: " : "FAIL: ") + message);
            File.WriteAllText("Verification/house-transition-playmode.txt", report.ToString());
            if (!value) throw new Exception(message);
        }
        static IEnumerable Wait(HouseTransitions transitions) {
            float end = Time.realtimeSinceStartup + 30;
            do {
                if (Time.realtimeSinceStartup > end) throw new Exception("Transition timeout");
                yield return null;
            } while (transitions.Busy);
        }
        static IEnumerator CheckHouses() {
            VillageStorage.overridePath = Path.GetFullPath("Temp/house-transition-test-save.json");
            var game = UnityEngine.Object.FindFirstObjectByType<VillageGame>();
            game.playing = true;
            game.state.elder = true; // Entry prerequisite fixture; no puzzle execution.
            game.ui.Close();
            for (int i = 0; i < 6; i++) {
                report.AppendLine("Testing " + HouseTransitions.SceneNames[i]);
                var entry = UnityEngine.Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).Single(a => a.kind == "enter" && a.index == i);
                Probe(game, entry, game.entrances[i].position + Vector3.up * 1.2f);
                game.player.Teleport(game.entrances[i].position, game.entrances[i].eulerAngles.y);
                if (i == 5) game.transitions.Enter(i); // Test locked room loading independently of progression.
                else Select(game, entry);
                foreach (var step in Wait(game.transitions)) yield return step;
                Check(game.transitions.Current && game.house == i, "Entry loaded house " + i);
                Check(SceneManager.sceneCount == 2, "Only village and current house loaded " + i);
                Check(Vector3.Distance(game.player.transform.position, game.transitions.Current.arrival.position) < .3f, "Arrival position " + i);
                game.ui.Close();
                if (i == 0) {
                    var tennis = UnityEngine.Object.FindFirstObjectByType<ElderTennis>();
                    tennis.StartMatch();
                    yield return null;
                    tennis.StopMatch();
                    yield return null;
                    Check(!game.player.ActivityLocksMovement, "Leaving tennis releases movement");
                }
                if (i == 1) {
                    var plane = UnityEngine.Object.FindFirstObjectByType<AlchemistDreamHouse>();
                    plane.EnterDream();
                    float end = Time.realtimeSinceStartup + 10;
                    while (!plane.InDream && Time.realtimeSinceStartup < end) yield return null;
                    Check(plane.InDream, "Entered dream relocation");
                    plane.ReturnFromDream(false);
                    yield return null;
                    Check(!plane.InDream && !game.player.ActivityLocksMovement, "Returning from dream releases movement");
                }
                var exit = UnityEngine.Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).Single(a => a.kind == "exit");
                Probe(game, exit, game.transitions.Current.arrival.position + Vector3.up * 1.2f);
                Check(exit.game == game, "Exit action bound " + i);
                Select(game, exit);
                foreach (var step in Wait(game.transitions)) yield return step;
                Check(!game.transitions.Current && game.house == -1 && SceneManager.sceneCount == 1, "Exit unloaded house " + i);
                Check(Vector3.Distance(game.player.transform.position, game.entrances[i].position) < .3f, "Correct village return " + i);
            }
        }
        static void Select(VillageGame game, VillageAction action) {
            var interactable = action.GetComponent<XRSimpleInteractable>();
            Check(game.interactionManager.TryGetInteractableForCollider(action.GetComponent<Collider>(), out var registered) && registered == (IXRInteractable)interactable, "XR collider registration: " + action.name);
            var host = new GameObject("Temporary XR selection fixture");
            var interactor = host.AddComponent<XRRayInteractor>();
            interactor.interactionManager = game.interactionManager;
            Check(game.interactionManager.IsSelectPossible(interactor, interactable), "XR selection allowed: " + action.name);
            game.interactionManager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
            UnityEngine.Object.Destroy(host);
        }
        static void Probe(VillageGame game, VillageAction action, Vector3 hand) {
            var host = new GameObject("Temporary door XR cast probe");
            var caster = host.AddComponent<CurveInteractionCaster>();
            caster.castOrigin = host.transform;
            caster.enableStabilization = false;
            caster.hitDetectionType = CurveInteractionCaster.HitDetectionType.ConeCast;
            caster.coneCastAngle = 6;
            caster.sphereCastRadius = .1f;
            caster.castDistance = 10;
            caster.targetNumCurveSegments = 1;
            caster.raycastMask = unchecked((int)2147483681);
            caster.raycastTriggerInteraction = QueryTriggerInteraction.Ignore;
            var colliders = new List<Collider>();
            var hits = new List<RaycastHit>();
            var target = action.GetComponent<Collider>();
            report.AppendLine("XR cone: " + action.name + " at " + action.transform.position.ToString("F3"));
            for (int x = -2; x <= 2; x++) for (int y = -2; y <= 2; y++) {
                var aim = target.bounds.center + action.transform.right * (x * target.bounds.extents.x * .4f) + Vector3.up * (y * target.bounds.extents.y * .4f);
                host.transform.SetPositionAndRotation(hand, Quaternion.LookRotation(aim - hand));
                caster.TryGetColliderTargets(game.interactionManager, colliders, hits);
                report.AppendLine("  aim=" + x + "," + y + " first=" + (colliders.Count > 0 ? colliders[0].name : "NONE"));
            }
            UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
