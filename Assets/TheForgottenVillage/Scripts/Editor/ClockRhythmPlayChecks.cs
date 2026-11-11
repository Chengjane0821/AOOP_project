using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ClockRhythmPlayChecks {
        static IEnumerator routine;
        static int frame;
        static StringBuilder report;
        static Keyboard keyboard;
        static InputSettings.BackgroundBehavior oldBackground;
        static ClockRhythmRun live;
        static bool follow;
        static readonly List<string> errors = new List<string>();
        static ClockRhythmPlayChecks() {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (m,s,t) => { if(routine!=null&&(t==LogType.Error||t==LogType.Exception)) errors.Add(m); };
        }
        [MenuItem("Forgotten Village/Test Clockmaker rhythm repair")]
        public static void Request() {
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(gameView!=null)EditorWindow.GetWindow(gameView).Show();
            SessionState.SetBool("CLOCK_TEST_PENDING",true); EditorApplication.isPlaying=true;
        }
        static void Tick() {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(SessionState.GetBool("CLOCK_TEST_PENDING",false)&&Time.frameCount>12) {
                SessionState.SetBool("CLOCK_TEST_PENDING",false); report=new StringBuilder();errors.Clear();routine=Run();
            }
            if(routine==null||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(!routine.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
        }
        static void Finish(Exception error) {
            if(live)live.GetComponent<ClockRhythmInput>().BeforeCapture=null;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;InputSystem.settings.backgroundBehavior=oldBackground;}
            report.AppendLine(error==null?"PASS: Clock rhythm Play Mode suite completed":"FAIL: "+error);
            foreach(var e in errors)report.AppendLine("Runtime error: "+e);
            report.AppendLine("Scope: actual additive scene, real-time complete runs, UI callbacks and injected A/S/D input. Camera evidence simulated. Physical VR and audio latency acceptance remain manual.");
            File.WriteAllText("Verification/clock-rhythm-playmode.txt",report.ToString());
            follow=false;live=null;routine=null;VillageStorage.overridePath=null;EditorApplication.isPlaying=false;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);File.WriteAllText("Verification/clock-rhythm-progress.txt",report.ToString());}
        static void Button(VillageGame g,string name){g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b.name==name).onClick.Invoke();}
        static IEnumerable<object> Wait(double seconds){double end=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<end)yield return null;}
        static IEnumerable<object> Transition(VillageGame g){double end=Time.realtimeSinceStartupAsDouble+30;while(g.transitions.Busy){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Transition timeout");yield return null;}yield return null;}
        static IEnumerable<object> UntilFinished(ClockRhythmRun run){double end=Time.realtimeSinceStartupAsDouble+100;while(run.Running){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Run timeout");yield return null;}}
        static void Keys() {
            if(keyboard==null)return; keyboard.MakeCurrent(); Key[] held=Array.Empty<Key>();
            if(follow&&live&&live.AcceptsInput)for(int i=0;i<live.Session.Notes.Length;i++) {
                var note=live.Session.Notes[i];
                if(!live.Session.IsResolved(i)&&Math.Abs(note.Time-live.Elapsed)<.065){held=new[]{new[]{Key.A,Key.S,Key.D}[note.Lane]};break;}
            }
            InputState.Change(keyboard,new KeyboardState(held),InputUpdateType.Dynamic);
        }
        public static IEnumerable<object> CompleteClockmaker(VillageGame g) {
            var house=UnityEngine.Object.FindFirstObjectByType<ClockRhythmHouse>();
            if(!house)throw new Exception("Missing clock rhythm activity");
            house.run.Begin(); double deadline=Time.realtimeSinceStartupAsDouble+90;
            while(house.run.Running) {
                if(Time.realtimeSinceStartupAsDouble>deadline)throw new Exception("Clock regression timeout");
                for(int i=0;i<house.run.Session.Notes.Length;i++) {
                    var note=house.run.Session.Notes[i];
                    if(!house.run.Session.IsResolved(i)&&Math.Abs(note.Time-house.run.Elapsed)<.07)house.run.Strike(note.Lane);
                }
                yield return null;
            }
            if(!g.state.clockRhythmWon)throw new Exception("Clock regression did not win");
            Button(g,"Remember together");g.recognition.simulation=true;g.recognition.Begin(4);g.recognition.SimulateDetection("clock");
            if(!g.Award(4))throw new Exception("Clock regression failed clue claim");
        }
        static IEnumerator Run() {
            VillageStorage.overridePath=Path.GetFullPath("Verification/clock-rhythm-test-save.json");
            var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();Check(g&&g.transitions,"Village services loaded");
            g.NewGame();foreach(var x in Transition(g))yield return x;g.state.elder=true;g.ui.Close();g.Enter(4);foreach(var x in Transition(g))yield return x;
            var house=UnityEngine.Object.FindFirstObjectByType<ClockRhythmHouse>();
            Check(house&&house.game==g,"Clockmaker activity bound after additive entry");
            Check(!UnityEngine.Object.FindFirstObjectByType<EngineeringStation>(),"Calculation station removed from Clockmaker scene");
            Check(!g.Award(4),"Fifth clue cannot be claimed before activity");
            var resident=UnityEngine.Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).First(a=>a.kind=="dialogue"&&a.index==4);
            resident.GetComponent<VillageInteractable>().Activate();Button(g,"Maybe later");
            Check(!g.ui.IsOpen&&!house.run.Running&&!g.state.puzzleSolved[4],"Declining invitation leaves progress unchanged");
            resident.GetComponent<VillageInteractable>().Activate();Button(g,"Repair the clocks");live=house.run;
            var position=g.player.View.transform.position;
            Check(live.Running&&g.player.ActivityLocksMovement,"Starting repairs locks locomotion");
            foreach(var x in Wait(.5))yield return x;g.ui.Pause();double paused=live.Elapsed;
            foreach(var x in Wait(.4))yield return x;
            Check(live.Elapsed==paused,"Pause freezes musical time");
            Button(g,"Resume");foreach(var x in Wait(.2))yield return x;
            Check(live.Elapsed>paused&&live.Elapsed<paused+.35,"Resume does not catch up paused time");
            g.ui.Pause();Button(g,"Leave clock");Check(!live.Running&&!g.player.ActivityLocksMovement,"Leave releases locomotion lock");
            live.Begin();foreach(var x in UntilFinished(live))yield return x;
            Check(!g.state.clockRhythmWon&&live.Session.Misses==32&&g.ui.heading.text.Contains("another try"),"Full unattended run fails without awarding progress");
            Button(g,"Retry");Check(live.Elapsed==0&&live.Session.Resolved==0,"Retry resets chart and score");
            oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();keyboard.MakeCurrent();live.GetComponent<ClockRhythmInput>().BeforeCapture=Keys;follow=true;
            foreach(var x in Wait(5))yield return x;
            Check(live.Session.Hits>=2,"Injected desktop A key hits actual chart notes");
            VillageAutomation.Capture(g,"clock-rhythm-play-preview");
            foreach(var x in UntilFinished(live))yield return x;follow=false;
            Check(g.state.clockRhythmWon&&g.state.puzzleSolved[4]&&live.Session.Hits==32,"Full 32-note run succeeds through real A/S/D input");
            Check(Vector3.Distance(position,g.player.View.transform.position)<.001f&&!g.player.ActivityLocksMovement,"Camera remains stationary and movement unlocks on success");
            Check(!g.state.clues[4]&&!g.state.clockMemoryRecovered,"Victory alone does not award memory or clue");
            Button(g,"Remember together");Check(g.state.clockMemoryRecovered&&!g.state.clues[4],"Memory recovery still requires object evidence");
            g.recognition.simulation=true;g.recognition.Begin(4);Check(g.recognition.SimulateDetection("clock"),"Existing camera simulation accepts clock evidence");
            g.ui.ObjectQuest(4);Button(g,"Collect clue");Check(g.state.clues[4]&&g.state.CanClaim(4),"Existing object quest awards fifth clue after rhythm and memory");
            g.Award(4);Check(g.state.inventory.Count(x=>x==VillageGame.Clues[4])==1,"Repeat claim does not duplicate record");
            var saved=VillageStorage.Load();Check(saved.clockRhythmWon&&saved.clockMemoryRecovered&&saved.clues[4],"Disk save restores rhythm memory and clue");
            g.Exit();foreach(var x in Transition(g))yield return x;g.Enter(4);foreach(var x in Transition(g))yield return x;
            house=UnityEngine.Object.FindFirstObjectByType<ClockRhythmHouse>();Check(house&&g.state.CanClaim(4)&&!house.run.Running,"Re-entry retains progression without restarting activity");
            house.run.Begin();g.ui.Menu();yield return null;yield return null;
            Check(!house.run.Running&&!g.player.ActivityLocksMovement,"Returning to title cancels active rhythm run");
            Check(errors.Count==0,"No runtime errors during clock checks");
        }
    }
}
