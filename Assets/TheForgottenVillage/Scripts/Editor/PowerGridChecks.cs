using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace ForgottenVillage.Editor {
    public static class PowerGridChecks {
        static TestRunnerApi api;
        [MenuItem("Forgotten Village/Check power-grid rules")]
        public static void Model(){RunTests("ForgottenVillage.Tests.PowerGridTests","power-grid-model");}
        [MenuItem("Forgotten Village/Run power-grid regression unit tests")]
        public static void NUnit(){RunTests("ForgottenVillage.Tests","power-grid-editmode");}
        static void RunTests(string names,string prefix){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(prefix));api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{names}}));}
        sealed class Results:ICallbacks {
            readonly string prefix;
            public Results(string prefix){this.prefix=prefix;}
            public void RunStarted(ITestAdaptor tests){}
            public void TestStarted(ITestAdaptor test){}
            public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,"Verification/"+prefix+".xml");File.WriteAllText("Verification/"+prefix+".txt",$"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}\n{result.ResultState}\n");}
        }
        [MenuItem("Forgotten Village/Validate power-grid integration")]
        public static void Scenes(){
            var report=new StringBuilder();
            try{
                foreach(var name in HouseTransitions.SceneNames){
                    var scene=EditorSceneManager.OpenScene("Assets/TheForgottenVillage/Scenes/"+name+".unity");var roots=scene.GetRootGameObjects();
                    var house=roots.Select(r=>r.GetComponent<HouseSceneRoot>()).First(h=>h);
                    if(house.copiedRoots.Length!=6||!house.arrival)throw new Exception("Missing room foundation: "+name);
                    int missing=roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));
                    if(missing!=0||roots.Sum(r=>r.GetComponentsInChildren<Camera>().Count(c=>c.enabled))!=0||roots.Sum(r=>r.GetComponentsInChildren<AudioListener>().Count(l=>l.enabled))!=0)throw new Exception("Copied camera/listener/script problem in "+name);
                    if(house.houseIndex==3){
                        var grid=house.GetComponent<PowerGridHouse>();
                        if(!grid||!grid.run||!grid.run.tuning||!grid.run.view||!grid.run.sound||!grid.run.playingPoint)throw new Exception("Incomplete grid binding");
                        if(house.GetComponentsInChildren<PowerGridCable>(true).Length!=5||house.GetComponentsInChildren<PowerGridControl>(true).Length!=12||
                            roots.Sum(r=>r.GetComponentsInChildren<EngineeringStation>(true).Length)!=0)throw new Exception("Wrong grid controls or legacy station remains");
                        foreach(var cable in house.GetComponentsInChildren<PowerGridCable>(true))if(!cable.socket||cable.generators.Length!=2||cable.generators.Any(g=>!g))throw new Exception("Unbound cable");
                    }
                    report.AppendLine("PASS: "+name+"; six source roots; missing scripts=0; enabled copied cameras/listeners=0");
                }
                report.AppendLine("PASS: Technician has two generators, five bound cables, twelve controls and no legacy station.");
            }catch(Exception ex){report.AppendLine("FAIL: "+ex);throw;}
            finally{File.WriteAllText("Verification/power-grid-scenes.txt",report.ToString());EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");}
        }
    }
}
