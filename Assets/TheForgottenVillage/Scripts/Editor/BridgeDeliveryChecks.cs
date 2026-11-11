using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage.Editor {
    public static class BridgeDeliveryChecks {
        static TestRunnerApi api;

        public static void NUnit(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results());api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"ForgottenVillage.Tests"}}));}
        sealed class Results:ICallbacks {
            public void RunStarted(ITestAdaptor tests){}public void TestStarted(ITestAdaptor test){}public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,"Verification/bridge-delivery-editmode.xml");File.WriteAllText("Verification/bridge-delivery-editmode.txt",$"Passed: {result.PassCount}; failed: {result.FailCount}; skipped: {result.SkipCount}\n{result.ResultState}\n");}
        }

        public static void Scenes(){
            var report=new StringBuilder();
            try{
                foreach(var name in HouseTransitions.SceneNames){
                    var scene=EditorSceneManager.OpenScene("Assets/TheForgottenVillage/Scenes/"+name+".unity");var roots=scene.GetRootGameObjects();var house=roots.Select(r=>r.GetComponent<HouseSceneRoot>()).First(h=>h);
                    if(house.copiedRoots.Length!=6||!house.arrival)throw new Exception("Copied room roots missing: "+name);
                    if(roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)))!=0)throw new Exception("Missing scripts: "+name);
                    if(roots.Sum(r=>r.GetComponentsInChildren<Camera>().Count(c=>c.enabled))!=0||roots.Sum(r=>r.GetComponentsInChildren<AudioListener>().Count(l=>l.enabled))!=0)throw new Exception("Enabled copied view/listener: "+name);
                    if(house.houseIndex==2){var bridge=house.GetComponent<BridgeHouse>();if(!bridge||!bridge.run||!bridge.run.cart||!bridge.run.view||!bridge.run.desktopPoint||!bridge.run.vrPoint)throw new Exception("Bridge setup incomplete");
                        if(bridge.run.pieces.Length!=6||bridge.run.sockets.Length!=6||bridge.run.pieces.Any(p=>!p||!p.GetComponent<Rigidbody>()||!p.GetComponent<XRGrabInteractable>()))throw new Exception("Bridge piece pool incomplete");
                        if(roots.Sum(r=>r.GetComponentsInChildren<EngineeringStation>(true).Length)!=0||roots.Sum(r=>r.GetComponentsInChildren<WorkshopSocket>(true).Length)!=0)throw new Exception("Legacy Carpenter station remains");
                    }
                    report.AppendLine("PASS: "+name+"; six source roots, no missing scripts or enabled copied cameras/listeners");
                }
                report.AppendLine("PASS: Carpenter has six grab pieces, six assembly sockets, a physical cart and desktop/VR stations.");
            }catch(Exception e){report.AppendLine("FAIL: "+e);throw;}
            finally{File.WriteAllText("Verification/bridge-delivery-scenes.txt",report.ToString());EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");}
        }
    }
}
