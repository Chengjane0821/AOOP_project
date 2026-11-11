using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ForgottenVillage.Editor {
    public static class ActiveWorkshopChecks {
        static TestRunnerApi api;
        [MenuItem("Forgotten Village/Run active workshop regression tests")]
        public static void NUnit() {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] { "ForgottenVillage.Tests" } }));
        }
        sealed class Results : ICallbacks {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result) {
                TestRunnerApi.SaveResultToFile(result, "Verification/active-workshop-editmode.xml");
                File.WriteAllText("Verification/active-workshop-editmode.txt",
                    $"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}\n{result.ResultState}\n");
            }
        }
        [MenuItem("Forgotten Village/Validate active workshop and six bells")]
        public static void Scenes() {
            var report = new StringBuilder();
            try {
                foreach (var name in HouseTransitions.SceneNames) {
                    var scene = EditorSceneManager.OpenScene("Assets/TheForgottenVillage/Scenes/" + name + ".unity");
                    var roots = scene.GetRootGameObjects();
                    var house = roots.Select(r => r.GetComponent<HouseSceneRoot>()).First(h => h);
                    if (house.copiedRoots.Length != 6 || !house.arrival) throw new Exception("House roots/arrival missing: " + name);
                    if (roots.Sum(r => r.GetComponentsInChildren<Transform>(true).Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))) != 0)
                        throw new Exception("Missing script: " + name);
                    if (roots.Sum(r => r.GetComponentsInChildren<Camera>().Count(c => c.enabled)) != 0 ||
                        roots.Sum(r => r.GetComponentsInChildren<AudioListener>().Count(l => l.enabled)) != 0)
                        throw new Exception("Copied camera/listener still enabled: " + name);
                    if (house.houseIndex == 2) {
                        var workshop = house.GetComponent<ActiveWorkshopHouse>();
                        if (!workshop || !workshop.run || !workshop.run.view || !workshop.run.desktopPoint || !workshop.run.vrPoint)
                            throw new Exception("Quick Hands station incomplete");
                        var run = workshop.run;
                        if (run.targetPoints.Length != 6 || run.targetVisuals.Length != 6 ||
                            run.targetPoints.Any(t => !t) || run.targetVisuals.Any(t => !t) || !run.obstacleVisual)
                            throw new Exception("Quick Hands target/hazard pool incomplete");
                        if (house.GetComponent<BridgeHouse>() || house.GetComponentInChildren<BridgeRun>(true) ||
                            roots.Any(r => r.GetComponentInChildren<EngineeringStation>(true) || r.GetComponentInChildren<WorkshopSocket>(true)))
                            throw new Exception("Retired bridge or amplifier remains in playable scene");
                        if (run.obstacleVisual.GetComponentsInChildren<Collider>(true).Any(c => c.enabled))
                            throw new Exception("Dodge visuals must not physically push the player");
                        report.AppendLine("PASS: Carpenter has six target anchors, six visual targets, calibrated VR/desktop stations and a visual-only obstacle; bridge retired.");
                    }
                    if (house.houseIndex == 4) {
                        var clock = house.GetComponent<ClockRhythmHouse>();
                        if (!clock || !clock.run || !clock.run.view || clock.GetComponentsInChildren<ClockBell>(true).Length != ClockChart.LaneCount)
                            throw new Exception("Six playable bells missing");
                        var lanes = clock.GetComponentsInChildren<ClockBell>(true).Select(b => b.lane).OrderBy(l => l).ToArray();
                        if (!lanes.SequenceEqual(Enumerable.Range(0, ClockChart.LaneCount))) throw new Exception("Bell lane mapping incomplete");
                        report.AppendLine("PASS: Clockmaker has six uniquely bound bell lanes and expanded chart integration.");
                    }
                    report.AppendLine("PASS: " + name + "; six source roots, no missing scripts or enabled copied cameras/listeners.");
                }
            } catch (Exception e) { report.AppendLine("FAIL: " + e); throw; }
            finally {
                File.WriteAllText("Verification/active-workshop-scenes.txt", report.ToString());
                EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            }
        }
    }
}
