using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class HouseSceneChecks {
 static IEnumerator routine;static int lastFrame;static StringBuilder report;
 static HouseSceneChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=CaptureError;}
 static void CaptureError(string message,string trace,LogType type){if(SessionState.GetBool("FV_CAPTURE_ERRORS",false)&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))SessionState.SetInt("FV_TEST_ERRORS",SessionState.GetInt("FV_TEST_ERRORS",0)+1);}
 [MenuItem("Forgotten Village/Run integrated Play Mode checks")]
 public static void Request(){Directory.CreateDirectory("Verification");File.WriteAllText("Verification/house-playtest.request","run");}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlaying&&File.Exists("Verification/house-playtest.request")){try{File.Delete("Verification/house-playtest.request");}catch(IOException){return;}if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");SessionState.SetInt("FV_TEST_ERRORS",0);SessionState.SetBool("FV_CAPTURE_ERRORS",true);SessionState.SetBool("FV_HOUSE_TEST",true);EditorApplication.isPlaying=true;return;}
  if(!EditorApplication.isPlaying)return;
  if(SessionState.GetBool("FV_HOUSE_TEST",false)&&Time.frameCount>8){SessionState.SetBool("FV_HOUSE_TEST",false);routine=Run();}
  if(routine==null||lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;
  try{if(!routine.MoveNext()){routine=null;VillageStorage.overridePath=null;SessionState.SetBool("FV_CAPTURE_ERRORS",false);EditorApplication.isPlaying=false;}}catch(Exception e){File.WriteAllText("Verification/house-playtest-results.txt",report+"\nFAIL: "+e);Debug.LogException(e);routine=null;VillageStorage.overridePath=null;SessionState.SetBool("FV_CAPTURE_ERRORS",false);EditorApplication.isPlaying=false;}
 }
 static void Check(bool value,string name){if(!value)throw new Exception(name);report.AppendLine("PASS: "+name);}
 static void Use(VillageGame game,string kind,int index){var a=UnityEngine.Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).First(x=>x.game==game&&x.kind==kind&&x.index==index);a.GetComponent<VillageInteractable>().Activate();}
 static IEnumerator Run(){
  report=new StringBuilder();var game=UnityEngine.Object.FindFirstObjectByType<VillageGame>();VillageStorage.overridePath=Path.GetFullPath("Verification/house-test-save.json");game.hardware.simulation=true;game.hardware.Connect();
  Check(game&&game.transitions&&game.ui.IsOpen,"Main village and additive transition service initialized");Check(Time.frameCount>8,"Runtime frames advancing");game.NewGame();game.ui.Close();Use(game,"enter",5);Check(game.house==-1&&!game.transitions.Busy,"Abandoned entrance refuses entry without five clues");
  Use(game,"dialogue",0);for(int line=0;line<4;line++){game.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf).onClick.Invoke();yield return null;}Check(game.state.elder,"Village Elder dialogue completes through UI callbacks");
  for(int i=0;i<6;i++){
   Use(game,"enter",i);float timeout=Time.realtimeSinceStartup+30;while(game.transitions.Busy){if(Time.realtimeSinceStartup>timeout)throw new Exception("House load timed out "+i);yield return null;}yield return null;
   Check(game.house==i&&SceneManager.GetSceneByName(HouseTransitions.SceneNames[i]).isLoaded,"Load actual "+HouseTransitions.SceneNames[i]+" scene");Check(SceneManager.sceneCount==2,"Only village and current house loaded "+i);Check(game.transitions.Current.copiedRoots.Length>=6,"Copied source scene roots retained "+i);
   Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"One active camera in house "+i);Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"One active audio listener in house "+i);
   Check(SessionState.GetInt("FV_TEST_ERRORS",0)==0,"No runtime errors through house "+i);
   VillageAutomation.Capture(game,"copied-house-"+i);
   if(i<5){Use(game,"puzzle",i);yield return null;var station=UnityEngine.Object.FindObjectsByType<EngineeringStation>(FindObjectsSortMode.None).Single();Check(station.game==game&&game.ui.IsOpen,"Puzzle Inspector bindings restored "+i);
    if(i==0){Use(game,"photo",0);station.Open();station.DisconnectLoad();station.Measure();station.ConnectLoad();station.Measure();station.setting=20000;}
    if(i==1){game.hardware.Receive("POT:292");yield return null;Check(Mathf.Abs(station.setting-5000)<15,"Simulated potentiometer drives RC station across scene load");}
    if(i==2){station.setting=6750;var part=UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).First(r=>r.name=="Amplifier component 0");var socket=UnityEngine.Object.FindFirstObjectByType<WorkshopSocket>();part.position=socket.transform.position;Physics.SyncTransforms();for(int frame=0;frame<10;frame++)yield return null;Check(game.state.inventory.Contains("Workshop component examined"),"Grab component activates workshop trigger socket");station.Open();}
    if(i==3){game.hardware.SimulateButton();Check(game.state.faultyTest,"Simulated Arduino button invokes bound diagnostic");station.referenceFix=true;Use(game,"diagnostic",0);Check(game.state.fixedTest,"In-room diagnostic button invokes corrected test");}
    if(i==4){station.setting=10;station.Measure();station.setting=11.5f;}
    Check(station.Validate()&&game.state.clues[i],"Solve engineering puzzle and earn record "+i);int count=game.state.inventory.Count;station.Validate();Check(game.state.inventory.Count==count,"Duplicate clue reward prevented "+i);station.Render();yield return null;VillageAutomation.Capture(game,"copied-puzzle-"+i);game.ui.Journal();Check(game.ui.body.text.Length>0,"Journal available in house "+i);
   }else{
    Check(!game.Finish(),"Final command blocked until all laboratory evidence reviewed");for(int e=0;e<4;e++)Use(game,"evidence",e);Check(game.Finish()&&game.state.ended,"Full final investigation reaches identity reveal");Check(game.hardware.lastCommand=="GATE:OPEN"&&game.hardware.acknowledged,"Verified command receives SIMULATED gate acknowledgment");yield return null;VillageAutomation.Capture(game,"copied-ending");
   }
   Use(game,"exit",i);timeout=Time.realtimeSinceStartup+30;while(game.transitions.Busy){if(Time.realtimeSinceStartup>timeout)throw new Exception("House unload timed out");yield return null;}yield return null;Check(game.house==-1&&SceneManager.sceneCount==1,"Unload house and return to village "+i);if(i<5)Check(game.state.clues[i],"Clue persists after scene unload "+i);
  }
  Check(game.state.Unlocked,"Five independent scene records unlock final house");var saved=VillageStorage.Load();Check(saved.ended&&saved.Unlocked&&saved.inventory.Count>=6,"JSON save round trip retains cross-scene progression");game.NewGame();Check(!game.state.Unlocked&&!game.state.ended&&game.state.inventory.Count==0,"New Game resets cross-scene progress");game.ui.Menu();yield return null;VillageAutomation.Capture(game,"copied-title");report.AppendLine("Scope: real additive scene load/unload, UnityEvent callbacks, physics trigger and simulated hardware; physical VR/Arduino not tested.");File.WriteAllText("Verification/house-playtest-results.txt",report.ToString());Debug.Log("SIX_HOUSE_PLAYTHROUGH_PASSED");
 }
}
}

