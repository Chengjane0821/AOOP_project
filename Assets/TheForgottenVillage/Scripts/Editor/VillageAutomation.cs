using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class VillageAutomation {
 static double next;
 static VillageAutomation(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
 static void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error){try{File.AppendAllText("Verification/editor-errors.txt",DateTime.Now.ToString("s")+" "+type+" "+message+"\n"+trace+"\n");}catch(IOException){}}}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
 if(!EditorApplication.isPlaying&&File.Exists("Verification/houses.request")){if(!Consume("Verification/houses.request"))return;try{SeparateHouseBuilder.Build();}catch(Exception e){File.WriteAllText("Verification/separate-house-build-error.txt",e.ToString());Debug.LogException(e);}return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/configure.request")){if(!Consume("Verification/configure.request"))return;ConfigureWindows();return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/build.request")){if(!Consume("Verification/build.request"))return;next=EditorApplication.timeSinceStartup+2;try{VillageBuilder.Build();File.WriteAllText("Verification/editor-build-result.txt","PASS: scene generated through Unity Editor APIs.");}catch(Exception e){File.WriteAllText("Verification/editor-build-result.txt",e.ToString());Debug.LogException(e);}return;}
 if(!EditorApplication.isPlaying&&File.Exists("Verification/playtest.request")){if(!Consume("Verification/playtest.request"))return;ElderTennisChecks.Request();return;}
 }
 public static void ConfigureWindows(){var settings=UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);if(settings){var touch=settings.GetFeature<UnityEngine.XR.OpenXR.Features.Interactions.OculusTouchControllerProfile>();if(touch)touch.enabled=true;EditorUtility.SetDirty(settings);}PlayerSettings.runInBackground=true;AssetDatabase.SaveAssets();EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64);File.WriteAllText("Verification/windows-xr.txt","Standalone Windows64 selected; OpenXR Oculus Touch profile enabled; run in background enabled.");}
 static bool Consume(string path){try{File.Delete(path);return true;}catch(IOException){return false;}}
 [MenuItem("Forgotten Village/Run legacy single-scene checks")]
 public static void RequestTest(){ElderTennisChecks.Request();}
 public static void Capture(VillageGame g,string name){var cam=g.player.View;var old=cam.targetTexture;var rt=RenderTexture.GetTemporary(1280,720,24);cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Verification/"+name+".png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);cam.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
}
}
