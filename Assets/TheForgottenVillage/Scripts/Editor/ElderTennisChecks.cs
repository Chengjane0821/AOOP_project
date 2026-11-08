using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ForgottenVillage.Editor {
// Real Play Mode scene/physics integration checks. Fixtures position balls/paddles;
// these checks do not claim that a human wore a headset or physically swung a controller.
[InitializeOnLoad] public static class ElderTennisChecks {
 static IEnumerator run;static int frame;static StringBuilder report;static readonly List<string> errors=new List<string>();
 static ElderTennisChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool("ELDER_TEST_ACTIVE",false)&&(t==LogType.Error||t==LogType.Exception||t==LogType.Assert))errors.Add(m);};}
 [MenuItem("Forgotten Village/Test Elder tennis and RPG progression")]
 public static void Request(){EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");SessionState.SetBool("ELDER_TEST_PENDING",true);EditorApplication.isPlaying=true;}
 static void Tick(){if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
  if(SessionState.GetBool("ELDER_TEST_PENDING",false)&&Time.frameCount>12){SessionState.SetBool("ELDER_TEST_PENDING",false);SessionState.SetBool("ELDER_TEST_ACTIVE",true);errors.Clear();report=new StringBuilder();run=Run();}
  if(run==null||frame==Time.frameCount)return;frame=Time.frameCount;
  try{if(!run.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
 }
 static void Finish(Exception failure){report.AppendLine(failure==null?"PASS: integrated Play Mode suite completed":"FAIL: "+failure);foreach(var e in errors)report.AppendLine("Runtime error: "+e);report.AppendLine("Scope: real Unity scenes and physics, scripted input/contact fixtures. No physical Quest/Arduino/camera testing.");File.WriteAllText("Verification/elder-tennis-playmode.txt",report.ToString());SessionState.SetBool("ELDER_TEST_ACTIVE",false);run=null;VillageStorage.overridePath=null;EditorApplication.isPlaying=false;}
 static void Check(bool condition,string message){if(!condition)throw new Exception(message);report.AppendLine("PASS: "+message);}
 static IEnumerable<object> Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
 static IEnumerable<object> Transition(VillageGame g){float end=Time.realtimeSinceStartup+30;while(g.transitions.Busy){if(Time.realtimeSinceStartup>end)throw new Exception("Scene transition timed out");yield return null;}yield return null;}
 static void Button(VillageGame g,string name){var b=g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(x=>x.gameObject.activeSelf&&x.name==name);b.onClick.Invoke();}
 static void Serve(ElderTennis t){typeof(ElderTennis).GetMethod("Serve",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(t,null);}
 static void ContactFixture(ElderTennis t,bool grip,float height){
  Serve(t);t.playerPaddle.enabled=false;
  var p=t.transform.TransformPoint(new Vector3(0,height,-1.25f));var rb=t.playerPaddle.GetComponent<Rigidbody>();rb.position=p-Vector3.forward*.04f;rb.rotation=t.transform.rotation;
  t.playerPaddle.MoveAI(p,t.transform.rotation);typeof(TennisPaddle).GetProperty("Grip").SetValue(t.playerPaddle,grip);
  t.TableBounce(t.transform.TransformPoint(new Vector3(0,.78f,-.8f)));
  t.ball.Serve(p+Vector3.forward*.08f,new Vector3(0,-.5f,-3));Physics.SyncTransforms();
 }
 static IEnumerator Run(){
  VillageStorage.overridePath=Path.GetFullPath("Verification/elder-tennis-test-save.json");var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();Check(g&&g.transitions,"Main village systems present");g.hardware.simulation=true;g.hardware.Connect();g.NewGame();g.ui.Close();g.ui.Dialogue(0);for(int i=0;i<4;i++){g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf).onClick.Invoke();yield return null;}Check(g.state.elder,"Village Elder quest introduction retained");g.Enter(0);foreach(var w in Transition(g))yield return w;
  var t=UnityEngine.Object.FindFirstObjectByType<ElderTennis>();Check(t&&t.game==g&&g.ui.IsOpen,"House entry automatically opens Elder conversation");Check(!UnityEngine.Object.FindFirstObjectByType<EngineeringStation>(),"No divider station remains in Elder house");Check(!t.photograph.activeSelf,"Photograph locked before victory");g.InspectPhoto();Check(!g.state.clues[0]&&!g.state.photograph,"Early photograph call cannot award clue");Button(g,"Maybe later");Check(!g.ui.IsOpen&&!t.Running,"Maybe later safely closes conversation");g.ui.Dialogue(0);Button(g,"Let's play!");Check(t.Running&&g.player.ActivityLocksMovement,"Let's play starts match and reserves swing space");foreach(var w in Wait(3.2f))yield return w;Check(t.RallyActive,"Start countdown launches physical serve");Check(!t.ball.Body.isKinematic&&t.ball.Body.useGravity,"Ball uses dynamic Unity physics");
  foreach(var w in Wait(2.5f))yield return w;Check(t.rules.elderPoints>0,"Unreturned physical serve resolves a point without stalling");
  t.StartMatch();g.ui.Close();ContactFixture(t,false,1.35f);foreach(var w in Wait(.08f))yield return w;Check(t.rules.gameHits==1&&t.rules.smashes==0,"Moving paddle physically hits ball once without Grip");Check(t.ball.Body.linearVelocity.z>0,"Normal contact sends ball toward Elder");foreach(var w in Wait(1.2f))yield return w;Check(t.rules.gameHits>=2,"NPC moves paddle and physically returns a normal shot");
  t.StartMatch();ContactFixture(t,true,1.4f);foreach(var w in Wait(.08f))yield return w;Check(t.rules.smashes==1&&t.rules.gameHits==1,"Grip plus valid physical contact counts exactly one smash");
  ContactFixture(t,true,1.4f);foreach(var w in Wait(.08f))yield return w;Check(t.rules.smashes==2&&t.rules.elderMustMiss&&t.rules.playerGames==0,"Two smashes queue visible failure without instant game win");foreach(var w in Wait(2))yield return w;Check(t.rules.playerGames==1,"Decisive smash ball physically resolves Elder game loss");
  t.StartMatch();ContactFixture(t,true,.95f);foreach(var w in Wait(.08f))yield return w;Check(t.rules.smashes==0&&t.rules.elderPoints==1,"Invalid low Grip contact is MISS and Elder point");
  t.StartMatch();ContactFixture(t,false,1.35f);t.rules.gameHits=t.rules.threshold-1;foreach(var w in Wait(2))yield return w;Check(t.rules.playerGames==1,"Hit-target fatigue waits for physical missed return");
  // Score-path fixtures exercise coordinator outcomes; pure counter tests cover contact accumulation.
  t.StartMatch();for(int game=0;game<2;game++){for(int point=0;point<5;point++){Serve(t);t.TableBounce(t.transform.TransformPoint(new Vector3(0,.78f,-.8f)));t.BallOut();}if(game==0)t.rules.BeginGame();}
  Check(!t.Running&&t.rules.playerGames==0&&t.rules.elderGames==0&&t.rules.smashes==0,"Losing best-of-three clears all match progress");Button(g,"Retry match");Check(t.Running&&t.rules.threshold>=20&&t.rules.threshold<=30,"Immediate retry samples fresh valid target");
  for(int game=0;game<2;game++){Serve(t);t.rules.Contact(true,true);t.rules.Contact(false,false);t.rules.Contact(true,true); // ready the earned-failure condition
   // A valid player shot landed, then the opponent failed to return it.
   typeof(ElderTennis).GetField("lastPlayer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(t,true);t.TableBounce(t.transform.TransformPoint(new Vector3(0,.78f,.8f)));t.BallOut();if(game==0)t.rules.BeginGame();}
  Check(g.state.elderTennisWon&&g.ui.heading.text.Contains("remember"),"Match victory triggers memory dialogue");Check(!t.photograph.activeSelf&&!g.state.clues[0],"Victory alone does not award evidence");Button(g,"Look at the photograph");Check(t.photograph.activeSelf,"Memory acknowledgment unlocks photograph");t.photograph.GetComponentInChildren<VillageInteractable>().Activate();Check(g.state.clues[0]&&g.state.inventory.Contains(VillageGame.Clues[0]),"Photo interaction awards Clue #1 and inventory");int count=g.state.inventory.Count;g.InspectPhoto();Check(g.state.inventory.Count==count,"Repeated inspection cannot duplicate inventory");g.ui.Journal();Check(g.ui.body.text.Contains("Table Tennis"),"Journal describes official Elder challenge");g.Exit();foreach(var w in Transition(g))yield return w;Check(!g.player.ActivityLocksMovement,"House exit restores locomotion");Check(VillageStorage.Load().clues[0],"Actual JSON save and scene unload preserve Elder completion");Check(!g.state.Unlocked,"Elder clue alone does not unlock Abandoned House");
  for(int i=1;i<5;i++){
   g.Enter(i);foreach(var w in Transition(g))yield return w;var s=UnityEngine.Object.FindFirstObjectByType<EngineeringStation>();Check(s&&s.topic==i,"Original engineering station retained in house "+i);s.Open();
   if(i==1)s.setting=5000;if(i==2)s.setting=6750;if(i==3){s.referenceFix=false;s.Diagnostic();s.referenceFix=true;s.Diagnostic();}if(i==4){s.setting=10;s.Measure();s.setting=11.5f;}
   Check(s.Validate()&&g.state.puzzleSolved[i],"Other house puzzle still solves "+i);Check(!g.Award(i),"Other house still requires camera evidence "+i);
   g.recognition.simulation=true;g.recognition.Begin(i);g.recognition.SimulateDetection(ObjectQuests.Categories[i]);Check(g.Award(i),"Other house simulated object quest awards original clue "+i);g.Exit();foreach(var w in Transition(g))yield return w;
  }
  Check(g.state.Unlocked,"All five clues unlock Abandoned House");g.Enter(5);foreach(var w in Transition(g))yield return w;for(int i=0;i<4;i++)g.Review(i);Check(g.Finish()&&g.state.ended,"Original final ending remains reachable");Check(g.hardware.lastCommand=="GATE:OPEN","Arduino simulation still receives final gate command");Check(errors.Count==0,"No Unity runtime errors during suite");
 }
}
}
