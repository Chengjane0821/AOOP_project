using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenVillage.Editor {
    // Read-only scene inspection. Does not run gameplay, alter saves, or save scenes.
    public static class HouseTransitionGeometryChecks {
        public static void Run() {
            var report = new StringBuilder();
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            var game = Object.FindFirstObjectByType<VillageGame>();
            Physics.SyncTransforms();
            foreach (var action in Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).Where(a => a.kind == "enter").OrderBy(a => a.index)) {
                Inspect(report, action, game.entrances[action.index].position + Vector3.up * 1.2f);
            }
            foreach (var name in HouseTransitions.SceneNames) {
                EditorSceneManager.OpenScene("Assets/TheForgottenVillage/Scenes/" + name + ".unity");
                Physics.SyncTransforms();
                var house = Object.FindFirstObjectByType<HouseSceneRoot>();
                foreach (var action in Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).Where(a => a.kind == "exit"))
                    Inspect(report, action, house.arrival.position + Vector3.up * 1.2f);
            }
            File.WriteAllText("Verification/house-transition-geometry.txt", report.ToString());
        }

        static void Inspect(StringBuilder report, VillageAction action, Vector3 eye) {
            var target = action.GetComponent<Collider>();
            report.AppendLine(action.gameObject.scene.name + " / " + action.name + " index=" + action.index + " position=" + action.transform.position.ToString("F3"));
            report.AppendLine("  reference-eye=" + eye.ToString("F3"));
            var delta = target.bounds.center - eye;
            report.AppendLine("  reference-ray=" + string.Join(", ", Physics.RaycastAll(eye, delta.normalized, delta.magnitude + .1f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).Select(h => h.collider.name + "@" + h.distance.ToString("F3"))));
            foreach (var c in Physics.OverlapCapsule(eye - Vector3.up * .9f, eye + Vector3.up * .3f, .2f, ~0, QueryTriggerInteraction.Ignore))
                report.AppendLine("  reference-body-overlap=" + c.name);
            // Sample approaches around the entrance/exit at interaction height.
            for (int i = 0; i < 8; i++) {
                var direction = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
                var start = target.bounds.center + direction * 2;
                var hits = Physics.RaycastAll(start, target.bounds.center - start, 2.5f, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance).ToArray();
                report.AppendLine("  approach=" + (i * 45) + " first=" + (hits.Length == 0 ? "NONE" : hits[0].collider.name) + " hits=" + string.Join(", ", hits.Take(5).Select(h => h.collider.name + "@" + h.distance.ToString("F3"))));
            }
            foreach (var collider in Physics.OverlapBox(target.bounds.center, target.bounds.extents + Vector3.one * .01f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Where(c => c != target))
                report.AppendLine("  nearby=" + collider.name + " bounds=" + collider.bounds);
        }
    }
}
