using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
namespace ForgottenVillage {
public class PlaneDreamRun : MonoBehaviour {
 public AlchemistDreamHouse house;public PlaneDreamTuning tuning;public PlaneAircraft aircraft;public PlaneObstaclePool obstacles;public PlaneBodyInput input;
 public Transform viewingStation;public TextMeshProUGUI timerLabel,statusLabel,instructionLabel;public UnityEngine.UI.Image progressBar;
 public Transform[] scenery;public float sceneryLength=24;
 public readonly PlaneRunState state=new PlaneRunState();public float DistanceTravelled {get;private set;}
 public bool InDream=>house&&house.InDream;
 float countdown;bool reported;Vector3[] sceneryStart;
 void Awake(){aircraft.run=this;sceneryStart=new Vector3[scenery.Length];for(int i=0;i<scenery.Length;i++)sceneryStart[i]=scenery[i].localPosition;}
 public void Introduction(){state.Stop();var g=house.game;
  g.ui.Show("Enter the endless sky","Survive for 60 seconds. The aircraft flies automatically; guide it around each barrier.\n\nUse a clear, unobstructed play area. Do not make large jumps. A small controlled upward motion or the optional A button replaces jumping. Seated/controller mode is available.\n\nStand or sit comfortably, then begin to calibrate your neutral position.");
  g.ui.Button("Begin flight",Begin);g.ui.Button(input.controllerMode?"Use body controls":"Seated controls",()=>{input.controllerMode=!input.controllerMode;Introduction();});g.ui.Button("Return to house",()=>house.ReturnFromDream(false));Refresh();
 }
 public void Begin(){if(!InDream)return;var g=house.game;state.Reset();reported=false;DistanceTravelled=0;countdown=3;aircraft.ResetFlight();obstacles.ResetPool(Random.Range(1,int.MaxValue));
  for(int i=0;i<scenery.Length;i++)scenery[i].localPosition=sceneryStart[i];
  g.ui.Close();g.ui.Toast("");g.player.ActivityLocksMovement=true;g.player.ActivityPrompt=g.player.IsVR?"":"A/D: steer | Space: rise | Ctrl: lower | Esc: pause | R: recalibrate";
  if(!input.Calibrate(g.player))PauseMenu();Refresh();
 }
 void Update(){if(!InDream)return;var g=house.game;
  if(!g.playing){house.ReturnFromDream(false);return;}
  if((state.phase==PlaneRunState.Phase.Flying||state.phase==PlaneRunState.Phase.Countdown)&&g.ui.IsOpen)state.Pause();
  if(state.phase==PlaneRunState.Phase.Paused&&!g.ui.IsOpen)Resume();
  if(!g.player.IsVR&&Keyboard.current?.rKey.wasPressedThisFrame==true){if(state.phase==PlaneRunState.Phase.Failed)Begin();else Recalibrate();}
  Refresh();
 }
 void FixedUpdate(){if(!InDream||house.game.ui.IsOpen)return;
  if(state.phase!=PlaneRunState.Phase.Countdown&&state.phase!=PlaneRunState.Phase.Flying)return;
  float dt=Time.fixedDeltaTime;if(!input.Sample(house.game.player,dt)){PauseMenu();return;}
  if(state.phase==PlaneRunState.Phase.Countdown){countdown-=dt;if(countdown<=0)state.Launch();return;}
  StepFlight(input.Command,dt);
 }
 // Shared deterministic simulation path; tests feed commands here while real input uses FixedUpdate.
 public void StepFlight(Vector2 command,float dt){float active=(float)state.StepDuration(dt);if(active<=0)return;
  aircraft.Step(command,active);if(obstacles.Step(active,state.Progress,aircraft)){Crash();return;}
  float distance=tuning.WorldSpeed(state.Progress)*active;DistanceTravelled+=distance;
  foreach(var part in scenery){var p=part.localPosition;p.z-=distance;if(p.z< -sceneryLength)p.z+=sceneryLength*scenery.Length;part.localPosition=p;}
  state.Advance(active);if(state.phase==PlaneRunState.Phase.Succeeded&&!reported){reported=true;obstacles.Clear();house.CompleteDream();}Refresh();
 }
 public void Crash(){if(state.phase!=PlaneRunState.Phase.Flying)return;state.Crash();reported=true;house.game.ui.Show("Dream Failed","The airplane struck a barrier. Take a moment, then try a fresh 60-second flight.");house.game.ui.Button("Retry",Begin);house.game.ui.Button("Return to house",()=>house.ReturnFromDream(false));Refresh();}
 public void PauseMenu(){if(!InDream)return;if(state.phase==PlaneRunState.Phase.Failed){CrashMenu();return;}if(state.phase==PlaneRunState.Phase.Succeeded||state.phase==PlaneRunState.Phase.Returning)return;
  state.Pause();var g=house.game;g.ui.Show("Dream paused",input.Status+"\n\nThe timer and obstacles are stopped. Recalibrate after tracking loss or changing your neutral position.");g.ui.Button("Resume",Resume);g.ui.Button("Recalibrate",Recalibrate);g.ui.Button("Retry",Begin);g.ui.Button("Return to house",()=>house.ReturnFromDream(false));g.ui.Button(input.controllerMode?"Body controls":"Seated controls",()=>{input.controllerMode=!input.controllerMode;Recalibrate();});
 }
 void CrashMenu(){house.game.ui.Show("Dream Failed","Every retry starts from 60 seconds.");house.game.ui.Button("Retry",Begin);house.game.ui.Button("Return to house",()=>house.ReturnFromDream(false));}
 public void Recalibrate(){if(!InDream)return;state.Pause();input.Calibrate(house.game.player);PauseMenu();}
 public void Resume(){if(!input.Ready){PauseMenu();return;}state.Resume();house.game.ui.Close();}
 public void Stop(){state.Stop();obstacles.Clear();}
 public void Refresh(){if(!timerLabel||!house||!house.game)return;timerLabel.text=$"ENDLESS SKY   {Mathf.CeilToInt((float)state.Remaining):00}s";if(progressBar)progressBar.fillAmount=state.Progress;
  var next=obstacles.Next();statusLabel.text=state.phase==PlaneRunState.Phase.Countdown?"READY  "+Mathf.Max(1,Mathf.CeilToInt(countdown)):state.phase==PlaneRunState.Phase.Flying?(next?next.Pattern.cue:"Fly toward the horizon"):state.phase.ToString().ToUpperInvariant();
  instructionLabel.text=house.game.player.IsVR?(input.controllerMode?"SEATED CONTROLS\nLeft stick: steer / height\nA: rise   B: lower\nMenu: pause / recalibrate":"BODY CONTROLS\nSmall lift: rise | crouch: lower\nSmall side steps: steer\nA: accessible rise\nMenu: pause / recalibrate") : "A / D: steer\nSpace: rise | Left Ctrl: lower\nEsc: pause | R: recalibrate";
 }
}
}
