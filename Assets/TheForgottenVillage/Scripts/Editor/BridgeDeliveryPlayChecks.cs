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
using UnityEngine.XR.Interaction.Toolkit;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]public static class BridgeDeliveryPlayChecks {
        static IEnumerator routine;static int frame;static StringBuilder report;
        static readonly List<string> errors=new List<string>();
        static Keyboard keyboard;static Mouse mouse;static Key[] held=Array.Empty<Key>();static Vector2 pointer;static bool down;
        static InputSettings.BackgroundBehavior oldBackground;static BridgeRun live;
        static BridgeDeliveryPlayChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(routine!=null&&(t==LogType.Error||t==LogType.Exception))errors.Add(m);};}
        [MenuItem("Forgotten Village/Test Carpenter bridge delivery")]
        public static void Request(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit current Play Mode first.");EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");if(type!=null)EditorWindow.GetWindow(type).Show();SessionState.SetBool("BRIDGE_TEST_PENDING",true);EditorApplication.isPlaying=true;}
        static void Tick(){
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            if(routine!=null||SessionState.GetBool("BRIDGE_TEST_PENDING",false))EditorApplication.QueuePlayerLoopUpdate();
            if(SessionState.GetBool("BRIDGE_TEST_PENDING",false)&&Time.frameCount>12){SessionState.SetBool("BRIDGE_TEST_PENDING",false);report=new StringBuilder();errors.Clear();routine=Run();}
            if(routine==null||frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(!routine.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
        }
        static void Finish(Exception failure){
            if(live)live.GetComponent<BridgeInput>().BeforeCapture=null;
            if(keyboard!=null){InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=oldBackground;}if(mouse!=null)InputSystem.RemoveDevice(mouse);
            keyboard=null;mouse=null;held=Array.Empty<Key>();down=false;live=null;
            report.AppendLine(failure==null?"PASS: Bridge Delivery Play Mode suite completed":"FAIL: "+failure);foreach(var error in errors)report.AppendLine("Runtime error: "+error);
            report.AppendLine("Scope: actual Carpenter scene, injected mouse/keys, XR grab callbacks, real Rigidbody cart crossings/falls, reinforcement, pause, save and clue flow. No physics time or success flags are altered. Camera evidence simulated; physical VR/Pi acceptance remains manual.");
            File.WriteAllText("Verification/bridge-delivery-playmode.txt",report.ToString());routine=null;VillageStorage.overridePath=null;EditorApplication.isPlaying=false;
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);File.WriteAllText("Verification/bridge-delivery-progress.txt",report.ToString());}
        static void Button(VillageGame g,string name){g.ui.buttons.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.gameObject.activeSelf&&b.name==name).onClick.Invoke();}
        static IEnumerable<object> Wait(double seconds){double end=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<end)yield return null;}
        static IEnumerable<object> Transition(VillageGame g){double end=Time.realtimeSinceStartupAsDouble+30;while(g.transitions.Busy){if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Bridge transition timed out");yield return null;}yield return null;}
        static void Control(BridgeRun run,BridgeCommand command){run.GetComponentsInChildren<BridgeControl>(true).First(c=>c.command==command).GetComponent<VillageInteractable>().Activate();}
        static void Inject(){keyboard.MakeCurrent();mouse.MakeCurrent();InputState.Change(keyboard,new KeyboardState(held),InputUpdateType.Dynamic);var state=new MouseState{position=pointer};if(down)state=state.WithButton(MouseButton.Left);InputState.Change(mouse,state,InputUpdateType.Dynamic);}
        static IEnumerable<object> Press(params Key[] keys){held=keys;yield return null;yield return null;held=Array.Empty<Key>();yield return null;yield return null;}
        static void Place(BridgeRun run,int piece,int socket,float yaw=0){run.Grab(run.pieces[piece]);run.DropAt(run.pieces[piece],run.sockets[socket].Point.position,yaw);}
        static void ControlRays(BridgeRun run){
            Physics.SyncTransforms();var camera=run.house.game.player.View.transform;
            foreach(var control in run.GetComponentsInChildren<BridgeControl>()){
                var bounds=control.GetComponent<Collider>().bounds;
                foreach(var offset in new[]{Vector3.zero,Vector3.left,Vector3.right,Vector3.up,Vector3.down}){
                    var target=bounds.center+Vector3.Scale(offset,bounds.extents)*.6f;
                    if(!Physics.Raycast(camera.position,(target-camera.position).normalized,out var hit,6)||hit.collider.GetComponentInParent<BridgeControl>()!=control)
                        throw new Exception("Bridge control obstructed: "+control.name+" by "+(hit.collider?hit.collider.name:"no hit"));
                }
            }
        }
        static IEnumerable<object> Trial(BridgeRun run,bool success,bool capture=false){
            if(!run.Testing)Control(run,BridgeCommand.Test);
            double end=Time.realtimeSinceStartupAsDouble+17;bool shot=false;
            while(run.Testing){
                if(Time.realtimeSinceStartupAsDouble>end)throw new Exception("Cart trial did not resolve");
                float x=run.transform.InverseTransformPoint(run.cart.transform.position).x;
                if(capture&&!shot&&Mathf.Abs(x)<.2f){VillageAutomation.Capture(run.house.game,"bridge-delivery-play-preview");shot=true;}
                yield return null;
            }
            if(run.LastTrialSucceeded!=success)throw new Exception("Unexpected cart result in round "+run.Round+" at "+run.cart.transform.position+"; expected "+success);
            yield return null;
        }
        public static IEnumerable<object> CompleteCarpenter(VillageGame g){
            var house=UnityEngine.Object.FindFirstObjectByType<BridgeHouse>();if(!house)throw new Exception("Missing bridge activity");house.run.Begin();
            for(int round=0;round<3;round++){
                for(int i=0;i<BridgeLevels.DeckCount(round);i++)Place(house.run,i,i);
                if(round==2)for(int i=3;i<6;i++)Place(house.run,i,i);
                foreach(var step in Trial(house.run,true))yield return step;
                if(round<2)Button(g,"Next delivery");
            }
            if(!g.state.carpenterBridgeWon)throw new Exception("Bridge regression did not win");Button(g,"Remember together");g.recognition.simulation=true;g.recognition.Begin(2);g.recognition.SimulateDetection("mouse");if(!g.Award(2))throw new Exception("Bridge clue regression failed");
        }
        static IEnumerator Run(){
            VillageStorage.overridePath=Path.GetFullPath("Verification/bridge-delivery-test-save.json");var g=UnityEngine.Object.FindFirstObjectByType<VillageGame>();Check(g&&g.transitions,"Village services initialized");
            g.NewGame();foreach(var x in Transition(g))yield return x;g.state.elder=true;g.ui.Close();g.Enter(2);foreach(var x in Transition(g))yield return x;
            var house=UnityEngine.Object.FindFirstObjectByType<BridgeHouse>();Check(house&&house.game==g,"Carpenter bridge game bound in actual additive scene");
            Check(!UnityEngine.Object.FindFirstObjectByType<EngineeringStation>()&&!UnityEngine.Object.FindFirstObjectByType<WorkshopSocket>(),"Legacy amplifier station and component socket removed");
            g.CompletePuzzle(2);Check(!g.state.puzzleSolved[2]&&!g.Award(2),"Legacy completion and early clue cannot bypass deliveries");
            Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)==1,"One active player camera");
            g.ui.Dialogue(2);Button(g,"Maybe later");Check(!house.run.Running&&!g.ui.IsOpen,"Invitation can be declined without progress");var originalView=g.player.desktopCamera.transform.localRotation;
            g.ui.Dialogue(2);Button(g,"Build a bridge");live=house.run;var input=live.GetComponent<BridgeInput>();
            Check(live.Running&&g.player.ActivityUsesPointer&&g.player.ActivityLocksMovement,"Build mode enables pointer and reserves the tabletop view");Check(Quaternion.Angle(originalView,g.player.desktopCamera.transform.localRotation)>10,"Desktop receives a downward view without rotating XR tracking");
            ControlRays(live);Check(true,"Workshop control centers and inner edges have clear selection rays");
            var piece=live.pieces[0];piece.Grabber.selectEntered.Invoke(new SelectEnterEventArgs());Check(piece.Held,"XR grab callback selects timber");piece.Grabber.selectExited.Invoke(new SelectExitEventArgs());Check(!piece.Held,"XR release callback returns unplaced timber safely");
            Control(live,BridgeCommand.Test);Check(live.Testing&&!live.cart.Body.isKinematic&&live.cart.Body.useGravity,"TEST releases a real gravity-driven Rigidbody cart");foreach(var x in Trial(live,false))yield return x;
            Check(live.cart.transform.position.y<live.transform.TransformPoint(new Vector3(0,live.deckY-.1f,0)).y,"Empty bridge makes the actual cart fall below the deck");Check(live.FailedTrials==1&&!g.state.puzzleSolved[2],"Failed crossing never awards house progress");Button(g,"Retry delivery");Check(live.AcceptsInput&&!live.Testing,"Retry returns to building without automatically running the cart");
            oldBackground=InputSystem.settings.backgroundBehavior;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();input.BeforeCapture=Inject;
            var camera=g.player.View;pointer=camera.WorldToScreenPoint(piece.GetComponent<Collider>().bounds.center);
            var plane=new UnityEngine.Plane(Vector3.up,live.transform.TransformPoint(new Vector3(0,live.deckY+.16f,0)));var ray=camera.ScreenPointToRay(pointer);plane.Raycast(ray,out float distance);var offset=piece.transform.position-ray.GetPoint(distance);
            down=true;yield return null;yield return null;Check(input.Selected==piece&&piece.Held,"Actual mouse ray grabs a timber piece");
            var rotation=piece.transform.rotation;foreach(var x in Press(Key.E))yield return x;Check(Quaternion.Angle(rotation,piece.transform.rotation)>10,"E rotates held timber");foreach(var x in Press(Key.Q))yield return x;
            var desired=live.sockets[0].Point.position;desired.y=live.transform.TransformPoint(new Vector3(0,live.deckY+.16f,0)).y;pointer=camera.WorldToScreenPoint(desired-new Vector3(offset.x,0,offset.z));yield return null;yield return null;down=false;yield return null;yield return null;
            Check(live.sockets[0].Occupant==piece&&!piece.Held,"Mouse drag and release fastens timber to its bridge guide");
            Place(live,1,1,90);Check(!live.sockets[1].Occupant,"Wrongly rotated timber does not fasten");Place(live,1,1);Check(live.MissingBoards==0,"Two correctly aligned boards complete the first layout");
            foreach(var x in Press(Key.Space))yield return x;Check(live.Testing,"Space starts the physical cart");foreach(var x in Wait(.4))yield return x;g.ui.Pause();var paused=live.cart.Body.position;foreach(var x in Wait(.4))yield return x;
            Check(live.cart.Body.isKinematic&&Vector3.Distance(paused,live.cart.Body.position)<.001f,"Pause freezes the cart without advancing its physics");
            Check(Vector3.Angle(g.ui.transform.forward,g.player.View.transform.forward)<.1f,"Desktop pause panel faces the downward workshop view");
            Button(g,"Resume");yield return null;yield return null;foreach(var x in Trial(live,true,true))yield return x;
            Check(live.RoundComplete&&!g.state.carpenterBridgeWon,"Successful first cart cannot finish the whole house");Button(g,"Next delivery");Check(live.Round==1,"Wider crossing begins with fresh materials");
            Place(live,2,0);Place(live,3,1);foreach(var x in Trial(live,false))yield return x;Button(g,"Retry delivery");Check(live.sockets[0].Occupant==live.pieces[2]&&live.sockets[1].Occupant==live.pieces[3],"Retry preserves the player's short-board layout");
            live.ResetBuild();Place(live,0,0);Place(live,1,1);foreach(var x in Trial(live,true))yield return x;Button(g,"Next delivery");Check(live.Round==2,"Heavy delivery starts after two true crossings");
            for(int i=0;i<3;i++)Place(live,i,i);foreach(var x in Trial(live,false))yield return x;Check(live.pieces.Take(3).Any(p=>p.Broken),"Heavy cart physically breaks an unreinforced board");Button(g,"Retry delivery");
            Check(live.pieces.Take(3).All(p=>!p.Broken&&p.Socket),"Retry repairs fallen boards while retaining the bridge configuration");
            pointer=camera.WorldToScreenPoint(live.pieces[3].GetComponent<Collider>().bounds.center);down=true;yield return null;yield return null;
            Check(input.Selected==live.pieces[3],"Mouse can grab a reinforcement from its tray");pointer=camera.WorldToScreenPoint(live.pieces[0].GetComponent<Collider>().bounds.center);yield return null;yield return null;down=false;yield return null;yield return null;
            Check(live.sockets[3].Occupant==live.pieces[3],"Dropping reinforcement onto a visible board fastens it underneath");
            for(int i=4;i<6;i++)Place(live,i,i);Check(live.sockets.Take(3).All(s=>live.CapacityFor(s)>=2),"Three reinforcements increase the heavy bridge capacity");
            foreach(var x in Trial(live,true))yield return x;Check(live.Won&&g.state.carpenterBridgeWon&&g.state.puzzleSolved[2]&&!g.state.clues[2],"Three actual cart deliveries unlock memory but not the clue early");
            Check(!g.player.ActivityUsesPointer&&!g.player.ActivityLocksMovement&&Quaternion.Angle(originalView,g.player.desktopCamera.transform.localRotation)<.001f,"Finishing restores camera rotation locomotion and cursor ownership");Button(g,"Remember together");Check(g.state.carpenterMemoryRecovered&&!g.state.clues[2],"Recovered memory still requires the original mouse evidence");
            g.recognition.simulation=true;g.recognition.Begin(2);Check(g.recognition.SimulateDetection("mouse"),"Existing mouse object quest accepts simulated evidence");g.ui.ObjectQuest(2);Button(g,"Collect clue");g.Award(2);Check(g.state.CanClaim(2)&&g.state.inventory.Count(s=>s==VillageGame.Clues[2])==1,"Third clue is awarded once after the bridge and evidence requirements");var saved=VillageStorage.Load();Check(saved.version==VillageSave.CurrentVersion&&saved.carpenterBridgeWon&&saved.clues[2],"Actual disk save retains Carpenter progress");
            g.Exit();foreach(var x in Transition(g))yield return x;g.Enter(2);foreach(var x in Transition(g))yield return x;house=UnityEngine.Object.FindFirstObjectByType<BridgeHouse>();Check(house&&g.state.CanClaim(2)&&!house.run.Running,"Re-entry restores the clue without restarting the activity");
            house.run.Begin();house.run.StartTrial();g.ui.Menu();yield return null;yield return null;Check(!house.run.Running&&!g.player.ActivityUsesPointer&&!g.player.ActivityLocksMovement,"Returning to title stops the trial and releases controls");Check(errors.Count==0,"No runtime errors during Bridge Delivery checks");
        }
    }
}
