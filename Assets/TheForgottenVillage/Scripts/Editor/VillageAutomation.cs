using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class VillageAutomation {
 static double next;static int frames;static bool running;
 static VillageAutomation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
 static void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){try{File.AppendAllText("Verification/editor-errors.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+trace+"\n");}catch(IOException){}}}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
 if(!EditorApplication.isPlaying&&File.Exists("Verification/houses.request")){if(!Consume("Verification/houses.request"))return;try{SeparateHouseBuilder.Build();}catch(Exception e){File.WriteAllText("Verification/separate-house-build-error.txt",e.ToString());Debug.LogException(e);}return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/configure.request")){if(!Consume("Verification/configure.request"))return;ConfigureWindows();return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/build.request")){if(!Consume("Verification/build.request"))return;next=EditorApplication.timeSinceStartup+2;try{VillageBuilder.Build();File.WriteAllText("Verification/editor-build-result.txt","PASS: scene generated through Unity Editor APIs.");}catch(Exception e){File.WriteAllText("Verification/editor-build-result.txt",e.ToString());Debug.LogException(e);}return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/playtest.request")){if(!Consume("Verification/playtest.request"))return;EditorSceneManager.OpenScene("Assets/TheForgottenVillage/Scenes/ForgottenVillage.unity");frames=0;running=false;SessionState.SetBool("FV_TEST",true);EditorApplication.isPlaying=true;return;}
 if(EditorApplication.isPlaying&&SessionState.GetBool("FV_TEST",false)&&!running){if(++frames<60)return;running=true;SessionState.SetBool("FV_TEST",false);try{Run();}catch(Exception e){File.WriteAllText("Verification/playtest-results.txt","FAIL\n"+e);Debug.LogException(e);}next=EditorApplication.timeSinceStartup+3;SessionState.SetBool("FV_STOP",true);}
 if(EditorApplication.isPlaying&&SessionState.GetBool("FV_STOP",false)&&EditorApplication.timeSinceStartup>=next){SessionState.SetBool("FV_STOP",false);EditorApplication.isPlaying=false;}
 }
 public static void ConfigureWindows(){var settings=UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);if(settings){var touch=settings.GetFeature<UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile>();if(touch)touch.enabled=true;EditorUtility.SetDirty(settings);}PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64);File.WriteAllText("Verification/windows-xr.txt","Standalone Windows64 selected; OpenXR Oculus Touch profile enabled; run in background enabled.");}
 static bool Consume(string path){try{File.Delete(path);return true;}catch(IOException){return false;}}
 static System.Text.StringBuilder report;
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);report.AppendLine("PASS: "+name);}
 [MenuItem("Forgotten Village/Run legacy single-scene checks")]
 public static void RequestTest(){File.WriteAllText("Verification/playtest.request","run");}
 public static void Capture(VillageGame g,string name){var cam=g.player.View;var old=cam.targetTexture;var rt=RenderTexture.GetTemporary(1280,720,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Verification/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);cam.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
 static void Run(){report=new System.Text.StringBuilder();var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();VillageStorage.overridePath=Path.GetFullPath("Verification/test-save.json");Check(g&&g.ui.IsOpen&&!g.playing,"Main menu initialized");Check(Time.frameCount>2,"Play Mode frames advance");g.NewGame();Check(g.playing&&!g.state.elder,"New game starts and resets");g.ui.Close();Check(Vector3.Distance(g.player.transform.position,g.villageSpawn.position)<1,"Village spawn");g.Enter(5);Check(g.house==-1,"Abandoned house locked");g.ui.Dialogue(0);for(int line=0;line<4;line++){var button=g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf);button.onClick.Invoke();}Check(g.state.elder,"Elder dialogue completion starts quest");g.Commit();g.InspectPhoto();Check(g.state.photograph&&g.state.inventory.Contains("Scratched photograph"),"Photo inspection and inventory");var stations=UnityEngine.Object.FindObjectsByType<EngineeringStation>(FindObjectsSortMode.None).OrderBy(s=>s.topic).ToArray();
 for(int i=0;i<5;i++){g.Enter(i);Check(g.house==i,"Enter house "+i);Capture(g,"house-"+i);var s=stations[i];s.Open();Capture(g,"puzzle-"+i);Check(g.ui.IsOpen,"Station panel "+i);switch(i){case 0:s.connected=false;s.Measure();s.connected=true;s.Measure();s.setting=20000;break;case 1:g.hardware.Receive("POT:292");s.ApplyPotentiometer();Check(Mathf.Abs(s.setting-5000)<15,"Simulated potentiometer updates filter resistance");break;case 2:s.setting=6750;break;case 3:g.hardware.SimulateButton();Check(g.state.faultyTest,"Simulated physical diagnostic button");s.referenceFix=true;s.Diagnostic();break;case 4:s.setting=10;s.Measure();s.setting=11.5f;break;}Check(s.Validate()&&g.state.clues[i],"Solve puzzle and award clue "+i);g.Exit();Check(g.house==-1,"Return to village "+i);}
 Check(g.state.Unlocked,"Five clues unlock laboratory");g.Enter(5);Check(g.house==5,"Enter laboratory");Capture(g,"house-5");Check(!g.Finish(),"Final gate blocked before evidence");for(int i=0;i<4;i++)g.Review(i);Check(g.state.Authorized,"Evidence authorizes safety command");Check(g.Finish()&&g.state.ended,"Ending reached");Capture(g,"ending");Check(g.hardware.lastCommand=="GATE:OPEN"&&g.hardware.acknowledged,"Simulated gate command and ACK");var restored=VillageStorage.Load();Check(restored.ended&&restored.Unlocked&&restored.inventory.Count>=6,"Actual JSON save/load round trip");Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(x=>x.enabled)==1,"One active audio listener");g.NewGame();Check(!g.state.Unlocked&&!g.state.ended&&g.state.inventory.Count==0,"Replay reset");g.ui.Menu();Capture(g,"title-playmode");report.AppendLine("Scope: automated scene method-level playthrough; not human input or physical hardware verification.");File.WriteAllText("Verification/playtest-results.txt",report.ToString());VillageStorage.overridePath=null;Debug.Log("FV_INTEGRATED_PLAYTEST_PASSED");}
}
}






