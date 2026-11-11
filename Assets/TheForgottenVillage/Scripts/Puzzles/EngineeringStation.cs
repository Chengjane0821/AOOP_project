using System;
using UnityEngine;
namespace ForgottenVillage {
public class EngineeringStation : MonoBehaviour {
 public int topic; public VillageGame game; public float setting; public bool connected=true, measuredUnloaded, measuredLoaded, referenceFix;
 public LineRenderer inputTrace,outputTrace; bool active; float lastPot=-1;
 public const string FaultyCode="struct Controller { virtual bool run() const { return true; } };\nstruct Safety : Controller { bool unsafeState = true;\n  bool run() const override { return !unsafeState; } };\nbool evaluate(Controller c) { return c.run(); } // BY VALUE: SLICES\nSafety s; // evaluate(s) returns RUN; unsafeState was lost";
 public const string FixedCode="bool evaluate(const Controller& c) { return c.run(); }\n// The derived object is retained; virtual dispatch returns STOP.\n// A checked Controller* also preserves dynamic dispatch.";
 public void Bind(VillageGame owner){if(game&&game.hardware)game.hardware.Diagnostic-=HardwareDiagnostic;game=owner;game.hardware.Diagnostic+=HardwareDiagnostic;ResetValues();}
 void Start(){ResetValues();}
 void OnEnable(){if(game&&game.hardware){game.hardware.Diagnostic-=HardwareDiagnostic;game.hardware.Diagnostic+=HardwareDiagnostic;}}
 void OnDisable(){if(game&&game.hardware)game.hardware.Diagnostic-=HardwareDiagnostic;}
 public void Open(){if(!game)return;if(topic==2){var workshop=GetComponentInParent<ActiveWorkshopHouse>();if(workshop)workshop.Offer();return;}if(topic==3){var grid=GetComponentInParent<PowerGridHouse>();if(grid)grid.Offer();return;}if(topic==4){var clock=GetComponentInParent<ClockRhythmHouse>();if(clock)clock.Offer();return;}if(!game.state.elder){game.ui.Toast("Talk to the Village Elder first.");return;}active=true;Render();}
 public void ResetValues(){setting=topic==0?10000:topic==1?1000:topic==2?2000:10;connected=true;measuredUnloaded=measuredLoaded=false;referenceFix=false;lastPot=-1;if(topic==1&&game&&game.hardware&&game.hardware.simulation)game.hardware.Receive("POT:0");}
 void Update(){if(!game)return;if(topic==1&&game.house==1&&Mathf.Abs(lastPot-game.hardware.potentiometer)>.002f)ApplyPotentiometer();Draw();}
 public void ApplyPotentiometer(){lastPot=game.hardware.potentiometer;setting=Mathf.Lerp(1000,15000,lastPot);if(active&&game.ui.IsOpen&&game.ui.heading.text.Contains("Engineering"))Render();}
 void Draw(){if(!inputTrace||!outputTrace)return;int samples=topic==1?256:96;inputTrace.positionCount=outputTrace.positionCount=samples;double fc=Engineering.Cutoff(Math.Max(setting,1),1e-7);for(int i=0;i<samples;i++){float t=i/(float)(samples-1);float a,b;if(topic==1){a=.22f*(Mathf.Sin(t*2.513274f)+.45f*Mathf.Sin(t*125.6637f));b=.22f*((float)Engineering.Magnitude(100,fc)*Mathf.Sin(t*2.513274f-(float)Math.Atan(100/fc))+.45f*(float)Engineering.Magnitude(5000,fc)*Mathf.Sin(t*125.6637f-(float)Math.Atan(5000/fc)));}else if(topic==4){a=(t*20%setting)<setting/2?.2f:-.2f;b=t*20<10?-.2f:.2f;}else{a=.025f*Mathf.Sin(t*18.85f);b=a*(float)Engineering.Gain(.002,Math.Max(setting,1),10000);}inputTrace.SetPosition(i,new Vector3((t-.5f)*2,1.9f+a,0));outputTrace.SetPosition(i,new Vector3((t-.5f)*2,1.3f+b,0));}}
 public void Adjust(float amount){setting=Mathf.Clamp(setting+amount,topic==4?5:1000,topic==4?20:topic==1?15000:50000);if(topic==1&&game.hardware.simulation){lastPot=Mathf.InverseLerp(1000,15000,setting);game.hardware.Receive("POT:"+Mathf.RoundToInt(lastPot*1023));}Render();}
 public void ConnectLoad(){connected=true;if(active)Render();} public void DisconnectLoad(){connected=false;if(active)Render();} public void ToggleLoad(){connected=!connected;Open();}
 public void Measure(){if(topic==0){if(connected)measuredLoaded=true;else measuredUnloaded=true;}if(topic==4&&Engineering.Slack(setting,1,.5,1,9)<0){game.state.timingViolation=true;game.Commit();}game.ui.Toast("Measurement recorded.");Render();}
 public void Diagnostic(){if(topic!=3)return;bool decision=Engineering.ControllerAllowsRun(referenceFix,true);if(referenceFix&&!decision)game.state.fixedTest=true;else if(!referenceFix&&decision)game.state.faultyTest=true;game.Commit();game.ui.Toast(referenceFix?"Reference diagnostic: STOP (expected safe behavior)":"Deployed diagnostic: RUN (unsafe state asserted; incorrect)");Render();}
 void HardwareDiagnostic(){if(game&&topic==3&&game.house==3&&game.playing)Diagnostic();}
 public bool Validate(){
  if(!game||!game.playing||!game.state.elder||topic<0||topic>=2)return false;
  bool ok=topic==0?game.state.photograph&&measuredLoaded&&measuredUnloaded&&connected&&Engineering.DividerSolved(setting):
   topic==1?Engineering.FilterSolved(setting):topic==2?Engineering.GainSolved(setting):
   topic==3?game.state.faultyTest&&game.state.fixedTest&&referenceFix:
   game.state.timingViolation&&Mathf.Abs(setting-11.5f)<.001f;
  if(ok)game.CompletePuzzle(topic);else game.ui.Toast("Conditions not yet met. Check the target and investigation steps.");
  return ok;
 }
 public void Render(){var ui=game.ui;string text="";
  if(topic==0){float unloaded=(float)Engineering.Divider(10,10000,10000,setting,false);float loaded=(float)Engineering.Divider(10,10000,10000,setting,true);text="VOLTAGE DIVIDER WITH LOAD\nVin=10 V; R1=10 kohm; R2=10 kohm; RL="+(setting/1000).ToString("F1")+" kohm\nRlow = R2 || RL\nVout = Vin x Rlow / (R1 + Rlow)\nUnloaded prediction: "+unloaded.ToString("F3")+" V\nLoaded prediction: "+loaded.ToString("F3")+" V | target 4.00 V +/- 0.08 V\nInspect the scratched photograph. Measure with the load detached and connected.\nRecorded measurements: unloaded="+measuredUnloaded+", loaded="+measuredLoaded+".";}
  if(topic==1){double fc=Engineering.Cutoff(setting,1e-7);text="RC LOW-PASS FILTER\nR="+(setting/1000).ToString("F2")+" kohm; C=100 nF; cutoff fc="+fc.ToString("F1")+" Hz\nfc = 1 / (2 x pi x R x C)\nMagnitude |H(f)| = 1 / sqrt(1 + (f/fc)^2)\n100 Hz retained: "+(100*Engineering.Magnitude(100,fc)).ToString("F1")+"% (target >= 90%)\n5 kHz retained: "+(100*Engineering.Magnitude(5000,fc)).ToString("F1")+"% (target <= 10%)\nCyan is input; amber is filtered output. The plotted window is 4 ms.\n"+(game.hardware.simulation?"Simulation: +/- changes the potentiometer equivalent.":"The USB potentiometer controls R in real time.");}
  if(topic==2)text="MOSFET COMMON-SOURCE AMPLIFIER\nModel: source bypassed; ro infinite; gm=2 mS; RL=10 kohm.\nDrain resistance RD="+(setting/1000).ToString("F2")+" kohm\nAv approx -gm x (RD || RL) = "+Engineering.Gain(.002,setting,10000).ToString("F3")+" V/V\nTarget: -8 +/- 0.2 V/V. Negative gain means the output is inverted.\nInput amplitude is 25 mV; the output is in the assumed linear range.\nCyan is input; amber is the inverted output.";
  if(topic==3)text=(referenceFix?FixedCode:FaultyCode)+"\n\nUnsafe sensor = TRUE. Expected decision = STOP.\nFaulty diagnostic: "+(game.state.faultyTest?"RUN":"not run")+" | Corrected: "+(game.state.fixedTest?"STOP":"not run")+"\nRun the deployed version, apply the reference fix, then rerun. The USB button triggers the diagnostic.";
  if(topic==4)text="REGISTER-TO-REGISTER SETUP TIMING\nLaunch FF -> clock-to-Q -> logic -> Capture FF\nClock period="+setting.ToString("F2")+" ns; setup=1 ns; uncertainty=0.5 ns\nActual arrival = 1 ns clock-to-Q + 9 ns logic = 10 ns\nRequired arrival = T - setup - uncertainty = "+(setting-1.5f).ToString("F2")+" ns\nSlack = required - actual = "+Engineering.Slack(setting,1,.5,1,9).ToString("F2")+" ns ("+(setting<11.5f?"FAIL":Mathf.Approximately(setting,11.5f)?"ZERO / BOUNDARY":"PASS")+")\nMeasure the original path, then find the minimum passing period in 0.25 ns steps.\nNegative slack is a possible risk, not proof of incident causation.";
  ui.Show(VillageGame.Houses[topic]+" - Engineering",text);ui.body.fontSize=topic==3?23:25;
  if(topic==3){ui.Button(referenceFix?"View deployed code":"Apply reference fix",()=>{referenceFix=!referenceFix;Render();});ui.Button("Run diagnostic",Diagnostic);ui.Button("Simulate button",()=>game.hardware.SimulateButton());}
  else{float step=topic==4?.25f:topic==0?1000:250;string unit=topic==4?"0.25 ns":step+" ohms";ui.Button("- "+unit,()=>Adjust(-step));ui.Button("+ "+unit,()=>Adjust(step));if(topic==0)ui.Button("Connect / detach load",()=>{connected=!connected;Render();});else ui.Button("Measure",Measure);}
  ui.Button("Validate configuration",()=>Validate());ui.Button("Reset station",()=>{ResetValues();Render();});ui.Button("Leave station",()=>{active=false;ui.Close();});if(topic==0)ui.Button("Measure",Measure);
 }
}
}
