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
    public static class PowerGridPlayChecks {
        static IEnumerator routine;static int frame;static StringBuilder report;
        static readonly List<string> errors=new List<string>();
        static Keyboard keyboard;static Mouse mouse;static Key[] held=Array.Empty<Key>();
        static Vector2 pointer;static bool clicking;static InputSettings.BackgroundBehavior oldBackground;
        static PowerGridRun live;
        static PowerGridPlayChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(routine!=null&&(t==LogType.Error||t==LogType.Exception))errors.Add(m);};}
        [MenuItem("Forgotten Village/Test Technician power-grid emergency")]
        public static void Request(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop the current play session first.");
            EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type).Show();
            SessionState.SetBool("GRID_TEST_PENDING",true);EditorApplication.isPlaying=true;
        }
        static void Tick(){
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(routine!=null||SessionState.GetBool("GRID_TEST_PENDING",false))EditorApplication.QueuePlayerLoopUpdate();
            if(SessionState.GetBool("GRID_TEST_PENDING",false)&&Time.frameCount>12){SessionState.SetBool("GRID_TEST_PENDING",false);report=new StringBuilder();errors.Clear();routine=Run();}
            if(routine==null||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(!routine.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
        }
        static void Finish(Exception failure){
            if(live)live.GetComponent<PowerGridInput>().BeforeCapture=null;
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);
            if(keyboard!=null)InputSystem.settings.backgroundBehavior=oldBackground;
            keyboard=null;mouse=null;held=Array.Empty<Key>();clicking=false;live=null;
            report.AppendLine(failure==null?"PASS: Power-grid Play Mode suite completed":"FAIL: "+failure);
            foreach(var error in errors)report.AppendLine("Runtime error: "+error);
            report.AppendLine("Scope: actual additive scenes, injected keyboard/mouse, shared VR interaction callbacks, real overload/timeout/stability time, save and clue flow. Camera/USB simulated; physical VR/Pi/Arduino not tested.");
            File.WriteAllText("Verification/power-grid-playmode.txt",report.ToString());routine=null;VillageStorage.overridePath=null;EditorApplication.isPlaying=false;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);File.WriteAllText("Verification/power-grid-progress.txt",report.ToString());}
        static void Button(VillageGame g,string name){g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b.name==name).onClick.Invoke();}
        static IEnumerable<object> Wait(double seconds){double end=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<end)yield return null;}
        static IEnumerable<object> Transition(VillageGame g){double end=Time.realtimeSinceStartupAsDouble+30;while(g.transitions.Busy){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("House transition timed out");yield return null;}yield return null;}
        static IEnumerable<object> Press(params Key[] keys){held=keys;yield return null;yield return null;held=Array.Empty<Key>();yield return null;yield return null;}
        static IEnumerable<object> RoundDone(PowerGridRun run){double end=Time.realtimeSinceStartupAsDouble+15;while(run.Running&&!run.Session.RoundComplete){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Stable power did not finish round");yield return null;}}
        static void Inject(){keyboard.MakeCurrent();mouse.MakeCurrent();InputState.Change(keyboard,new KeyboardState(held),InputUpdateType.Dynamic);var state=new MouseState{position=pointer};if(clicking)state=state.WithButton(MouseButton.Left);InputState.Change(mouse,state,InputUpdateType.Dynamic);}
        static void Control(PowerGridRun run,GridCommand command,int index=0){run.GetComponentsInChildren<PowerGridControl>(true).First(c=>c.command==command&&c.index==index).GetComponent<VillageInteractable>().Activate();}
        static void ClearControls(PowerGridRun run){
            Physics.SyncTransforms();var camera=run.house.game.player.View.transform;
            foreach(var control in run.GetComponentsInChildren<PowerGridControl>()){
                var bounds=control.GetComponent<Collider>().bounds;
                foreach(var offset in new[]{Vector3.zero,Vector3.left,Vector3.right,Vector3.up,Vector3.down}){
                    var target=bounds.center+Vector3.Scale(offset,bounds.extents)*.6f;
                    if(!Physics.Raycast(camera.position,(target-camera.position).normalized,out var hit,5)||hit.collider.GetComponentInParent<PowerGridControl>()!=control)
                        throw new Exception("Panel control obstructed: "+control.name+" by "+(hit.collider?hit.collider.name:"no hit"));
                }
            }
        }
        public static IEnumerable<object> CompleteTechnician(VillageGame g){
            var house=UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>();if(!house)throw new Exception("Missing grid activity");house.run.Begin();
            for(int round=0;round<3;round++){
                var circuit=house.run.Session.Circuit;int mask=BalancedMask(circuit);
                for(int i=0;i<circuit.MachineCount;i++){Control(house.run,GridCommand.SelectSource,(mask>>i)&1);Control(house.run,GridCommand.ConnectMachine,i);}
                if(circuit.Level.NeedsBackup)Control(house.run,GridCommand.Backup);
                Control(house.run,GridCommand.ResetBreaker,0);Control(house.run,GridCommand.ResetBreaker,1);Control(house.run,GridCommand.MainPower);
                foreach(var x in RoundDone(house.run))yield return x;if(round<2)Button(g,"Next circuit");
            }
            if(!g.state.technicianGridWon)throw new Exception("Grid regression did not win");Button(g,"Recover diagnostic log");
            g.recognition.simulation=true;g.recognition.Begin(3);g.recognition.SimulateDetection("cell phone");if(!g.Award(3))throw new Exception("Grid clue regression failed");
        }
        static int BalancedMask(PowerGridCircuit circuit){for(int mask=0;mask<(1<<circuit.MachineCount);mask++){int a=0,b=0;for(int i=0;i<circuit.MachineCount;i++){if(((mask>>i)&1)==0)a+=circuit.Level.Demand[i];else b+=circuit.Level.Demand[i];}if(a<=circuit.Capacity(0)&&b<=circuit.Capacity(1)+(circuit.Level.NeedsBackup&&!circuit.BackupOnline?2:0))return mask;}throw new Exception("Unsolvable grid");}
        static IEnumerator Run(){
            VillageStorage.overridePath=Path.GetFullPath("Verification/power-grid-test-save.json");var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();Check(g&&g.transitions,"Village services loaded");
            g.NewGame();foreach(var x in Transition(g))yield return x;g.state.elder=true;g.ui.Close();g.Enter(3);foreach(var x in Transition(g))yield return x;
            var house=UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>();Check(house&&house.game==g,"Technician activity bound in real additive scene");
            Check(!UnityEngine.Object.FindFirstObjectByType<EngineeringStation>(),"Obsolete code station removed");Check(!g.Award(3),"Fourth clue blocked before grid victory");g.CompletePuzzle(3);Check(!g.state.puzzleSolved[3],"Legacy completion call cannot bypass activity");
            Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"One active player camera");
            g.ui.Dialogue(3);Button(g,"Maybe later");Check(!g.ui.IsOpen&&!house.run.Running,"Declining invitation makes no progress");g.ui.Dialogue(3);Button(g,"Restore power");live=house.run;
            Check(live.Running&&g.player.ActivityLocksMovement&&g.player.ActivityUsesPointer,"Panel reserves stationary view with usable mouse pointer");var position=g.player.View.transform.position;
            ClearControls(live);Check(true,"All first-round controls have unobstructed selection rays");
            oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();live.GetComponent<PowerGridInput>().BeforeCapture=Inject;
            var sourceB=live.GetComponentsInChildren<PowerGridControl>().First(c=>c.command==GridCommand.SelectSource&&c.index==1);
            pointer=g.player.View.WorldToScreenPoint(sourceB.transform.position);clicking=true;yield return null;yield return null;clicking=false;yield return null;
            Check(live.SelectedSource==1,"Mouse ray selects actual generator B control");
            foreach(var x in Press(Key.A))yield return x;foreach(var x in Press(Key.Digit1,Key.Digit2,Key.Digit3))yield return x;foreach(var x in Press(Key.Space))yield return x;
            Check(live.Session.Circuit.Load(0)==9&&live.Session.Circuit.MainPower,"Actual A/1/2/3/Space input creates overload");
            foreach(var x in Wait(.6))yield return x;Check(live.Session.Circuit.Heat(0)>0,"Overload heats visible breaker");VillageAutomation.Capture(g,"power-grid-overload-preview");
            g.ui.Pause();double elapsed=live.Session.Elapsed,heat=live.Session.Circuit.Heat(0);var pausedRotor=live.view.machines[1].rotor.localRotation;foreach(var x in Wait(.5))yield return x;
            Check(live.Session.Elapsed==elapsed&&live.Session.Circuit.Heat(0)==heat,"Pause freezes both timer and overload heat");
            Check(Quaternion.Angle(pausedRotor,live.view.machines[1].rotor.localRotation)<.001f,"Refined machine animation freezes while paused");
            Button(g,"Resume");foreach(var x in Wait(3))yield return x;
            Check(live.Session.Circuit.Tripped(0)&&!live.Session.Circuit.Powered(0),"Sustained actual overload trips breaker and stops machines");
            foreach(var x in Press(Key.R))yield return x;Check(live.Session.Circuit.Tripped(0),"Reset rejected while generator remains overloaded");
            foreach(var x in Press(Key.S))yield return x;foreach(var x in Press(Key.Digit3))yield return x;foreach(var x in Press(Key.A,Key.R))yield return x;
            Check(live.Session.Circuit.Safe,"Rerouting cable and resetting repairs circuit");foreach(var x in Wait(2.2))yield return x;
            Check(live.view.machines.Take(3).All(m=>m.Online),"Connected machines visibly run");VillageAutomation.Capture(g,"power-grid-play-preview");
            foreach(var x in RoundDone(live))yield return x;Check(live.Session.RoundComplete&&!g.state.technicianGridWon,"First circuit alone never awards victory");Button(g,"Next circuit");
            Check(live.Session.Round==1&&live.Session.Circuit.Tripped(0),"Second round starts with damaged breaker");
            Control(live,GridCommand.SelectSource,0);Control(live,GridCommand.ConnectMachine,0);Control(live,GridCommand.ConnectMachine,1);Control(live,GridCommand.SelectSource,1);Control(live,GridCommand.ConnectMachine,2);Control(live,GridCommand.ConnectMachine,3);
            Control(live,GridCommand.ResetBreaker,0);g.hardware.SimulateButton();Check(live.Session.Circuit.MainPower&&live.Session.Circuit.Safe,"Shared VR callbacks and USB simulation operate repaired workshop");
            foreach(var x in RoundDone(live))yield return x;Button(g,"Next circuit");Check(live.Session.Round==2,"Final archive circuit starts");
            ClearControls(live);Check(true,"All five machine sockets and backup controls remain unobstructed");
            double deadline=Time.realtimeSinceStartupAsDouble+90;while(live.Running){if(Time.realtimeSinceStartupAsDouble>deadline)throw new Exception("Timeout check stalled");yield return null;}
            Check(live.Session.Failed&&!g.state.technicianGridWon&&!g.player.ActivityUsesPointer,"Full 75-second timeout fails without progress and releases pointer");
            Button(g,"Retry circuit");Check(live.Session.Round==2&&live.Session.Elapsed==0&&!live.Session.Circuit.BackupOnline,"Retry preserves round and clears connections timer and backup");
            foreach(var x in Press(Key.A,Key.Digit1,Key.Digit4,Key.Digit5))yield return x;foreach(var x in Press(Key.S,Key.Digit2,Key.Digit3))yield return x;foreach(var x in Press(Key.Space))yield return x;
            Check(!live.Session.Circuit.Safe&&live.Session.Circuit.Load(1)>live.Session.Circuit.Capacity(1),"Archive cannot run safely without backup");
            foreach(var x in Press(Key.B))yield return x;Check(live.Session.Circuit.Safe,"Backup key adds required capacity");foreach(var x in RoundDone(live))yield return x;
            Check(g.state.technicianGridWon&&g.state.puzzleSolved[3]&&!g.state.clues[3]&&!g.state.technicianMemoryRecovered,"Three complete circuits unlock memory without granting clue early");
            Check(Vector3.Distance(position,g.player.View.transform.position)<.001f&&!g.player.ActivityLocksMovement&&!g.player.ActivityUsesPointer,"Success keeps camera still and restores locomotion and cursor ownership");
            Button(g,"Recover diagnostic log");Check(g.state.technicianMemoryRecovered&&g.state.faultyTest&&g.state.fixedTest,"Recovered diagnostic log preserves deployed RUN and corrected STOP evidence");
            g.recognition.simulation=true;g.recognition.Begin(3);Check(g.recognition.SimulateDetection("cell phone"),"Existing cell phone object quest accepts simulated evidence");g.ui.ObjectQuest(3);Button(g,"Collect clue");g.Award(3);
            Check(g.state.CanClaim(3)&&g.state.inventory.Count(s=>s==VillageGame.Clues[3])==1,"Fourth clue awarded once after full requirements");var saved=VillageStorage.Load();Check(saved.version==VillageSave.CurrentVersion&&saved.technicianGridWon&&saved.clues[3],"Actual disk save preserves grid and clue progress");
            g.Exit();foreach(var x in Transition(g))yield return x;g.Enter(3);foreach(var x in Transition(g))yield return x;house=UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>();Check(house&&g.state.CanClaim(3)&&house.run.view.machines.All(m=>m.Online),"Re-entry restores clue and operating workshop visuals");
            house.run.Begin();g.ui.Menu();yield return null;yield return null;Check(!house.run.Running&&!g.player.ActivityLocksMovement&&!g.player.ActivityUsesPointer,"Return to title cancels running activity");
            Check(errors.Count==0,"No runtime errors in grid playthrough");
        }
    }
}
