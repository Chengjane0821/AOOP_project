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
using Unity.XR.CoreUtils;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class VRLifecycleChecks {
 static IEnumerator run; static int frame; static double deadline;
 static readonly StringBuilder report=new StringBuilder();
 static readonly List<string> errors=new List<string>();
 static VRLifecycleChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
 static void Log(string message,string stack,LogType type){if(run!=null&&(type==LogType.Exception||type==LogType.Error||type==LogType.Assert))errors.Add(message+"\n"+stack);}
 static void Tick(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlaying&&File.Exists("Verification/vr-lifecycle.request")){
   File.Delete("Verification/vr-lifecycle.request");
   EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
   SessionState.SetBool("VR_LIFECYCLE_PENDING",true);EditorApplication.isPlaying=true;return;
  }
  if(EditorApplication.isPlaying&&SessionState.GetBool("VR_LIFECYCLE_PENDING",false)&&Time.frameCount>12){
   SessionState.SetBool("VR_LIFECYCLE_PENDING",false);report.Clear();errors.Clear();run=Run();deadline=EditorApplication.timeSinceStartup+150;
  }
  if(run==null)return;
  if(EditorApplication.timeSinceStartup>deadline){Finish(new Exception("Play Mode frame/transition timeout"));return;}
  if(frame==Time.frameCount)return;frame=Time.frameCount;
  try{if(!run.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
 }
 static void Finish(Exception error){
  report.AppendLine(error==null&&errors.Count==0?"PASS: focused lifecycle suite":"FAIL: "+error);
  foreach(var item in errors)report.AppendLine(item);
  report.AppendLine("Unity Editor Play Mode; desktop plus simulated XR activation. No physical headset/controller tracking assertion.");
  File.WriteAllText("Verification/vr-lifecycle-playmode.txt",report.ToString());run=null;Time.timeScale=1;VillageStorage.overridePath=null;EditorApplication.isPlaying=false;
 }
 static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);File.WriteAllText("Verification/vr-lifecycle-progress.txt",report.ToString());}
 static IEnumerable<object> Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
 static IEnumerable<object> Transition(VillageGame g){float end=Time.realtimeSinceStartup+30;while(g.transitions.Busy){if(Time.realtimeSinceStartup>end)throw new Exception("Transition timed out");yield return null;}yield return null;}
 static void View(VillageGame g,int rigID,int cameraID){
  Check(g.player.xrRig.GetInstanceID()==rigID&&g.player.origin.Camera.GetInstanceID()==cameraID,"Original XR rig and headset camera references retained");
  Check(g.player.gameObject.activeInHierarchy&&g.player.View.isActiveAndEnabled,"Player and selected view active");
  Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"Exactly one active camera");
  Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"Exactly one active listener");
  Check(UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsSortMode.None).Length==(g.player.IsVR?1:0),"No duplicate active XR origins");
  Check(Time.timeScale==1,"Time scale restored");
 }
 static IEnumerator Run(){
  VillageStorage.overridePath=Path.GetFullPath("Temp/vr-lifecycle-save.json");
  var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();g.NewGame();g.state.elder=true;g.ui.Close();
  int rigID=g.player.xrRig.GetInstanceID(),cameraID=g.player.origin.Camera.GetInstanceID();
  var inactive=g.transitions.outdoorRoots.Where(o=>o&&!o.activeSelf).ToArray();
  Check(inactive.Any(o=>o.GetComponent<Camera>()),"Regression fixture contains intentionally disabled legacy Main Camera");
  for(int pass=0;pass<2;pass++){
   if(pass==1){
    typeof(VillagePlayer).GetProperty("IsVR").SetValue(g.player,true);
    g.player.desktopCamera.gameObject.SetActive(false);g.player.body.enabled=false;g.player.xrRig.SetActive(true);
   }
   for(int repeat=0;repeat<2;repeat++){
    g.Enter(1);g.Enter(1);foreach(var w in Transition(g))yield return w;View(g,rigID,cameraID);
    var h=g.transitions.Current.GetComponent<AlchemistDreamHouse>();h.EnterDream();foreach(var w in Wait(.6f))yield return w;
    Check(h.InDream&&g.player.ActivityLocksMovement,"Plane dream entered and movement reserved");
    h.ReturnFromDream(false);Check(!h.InDream&&!g.player.ActivityLocksMovement,"Plane dream returns to room");
    h.EnterDream();foreach(var w in Wait(.6f))yield return w;Time.timeScale=0;g.Exit();foreach(var w in Transition(g))yield return w;
    Check(!g.player.ActivityLocksMovement&&!g.ui.IsOpen,"Direct exit while dream paused restores control");
    Check(inactive.All(o=>!o.activeSelf),"Returning never activates legacy camera/inactive outdoors");View(g,rigID,cameraID);
    g.Enter(0);foreach(var w in Transition(g))yield return w;View(g,rigID,cameraID);
    var t=g.transitions.Current.GetComponentInChildren<ElderTennis>();Check(t&&t.ball.Body&&t.playerPaddle&&t.elderPaddle,"Elder entry initializes ball and paddle references");
    t.StartMatch();foreach(var w in Wait(.1f))yield return w;Check(t.Running,"Table tennis starts");
    t.StopMatch();Check(!t.Running&&!g.player.ActivityLocksMovement,"Leave table restores house movement");
    t.StartMatch();g.Exit();foreach(var w in Transition(g))yield return w;View(g,rigID,cameraID);
    Check(!g.player.ActivityLocksMovement&&!g.ui.IsOpen,"Exit during match restores village input");
    g.Enter(2);foreach(var w in Transition(g))yield return w;View(g,rigID,cameraID);g.Exit();foreach(var w in Transition(g))yield return w;
    Check(SceneManager.sceneCount==1,"Only village remains loaded after each cycle");
    Check(!g.state.clues[0]&&!g.state.clues[1],"Cancelled activities do not award quest clues");
   }
  }
  Check(errors.Count==0,"No runtime errors or repeated-load failures");
 }
}
}
