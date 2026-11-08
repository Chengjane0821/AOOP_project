using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
namespace ForgottenVillage {
public class HardwareConsole : MonoBehaviour {
 public bool simulation=true;
 public string portName="COM3";
 public int baud=115200;
 [Range(0,1)] public float potentiometer=.5f;
 public string status="Simulation ready", lastCommand="";
 public bool acknowledged;
 public event Action Diagnostic;
 readonly ConcurrentQueue<string> input=new ConcurrentQueue<string>();
 readonly ConcurrentQueue<string> output=new ConcurrentQueue<string>();
 Thread worker; volatile bool running,connected; bool previousButton,waiting; float sentAt;
 public void Connect(){
  Disconnect();
  if(worker!=null&&worker.IsAlive){status="Previous serial connection is closing; retry Connect.";return;}
  if(simulation){status="Simulation ready";return;}
  running=true;status="Connecting to "+portName;
  worker=new Thread(SerialLoop){IsBackground=true};worker.Start();
 }
 void SerialLoop(){
  object port=null;
  try{
   var type=Type.GetType("System.IO.Ports.SerialPort, System")??Type.GetType("System.IO.Ports.SerialPort, System.IO.Ports");
   if(type==null)throw new Exception("SerialPort unavailable: use Windows Mono / .NET Framework");
   port=Activator.CreateInstance(type,new object[]{portName,baud});
   type.GetProperty("ReadTimeout").SetValue(port,150);
   type.GetProperty("WriteTimeout").SetValue(port,150);
   type.GetMethod("Open").Invoke(port,null);
   connected=true;input.Enqueue("STATUS:Connected "+portName);
   while(running){
    while(running&&output.TryDequeue(out var cmd))type.GetMethod("WriteLine",new[]{typeof(string)}).Invoke(port,new object[]{cmd});
    try{var line=((string)type.GetMethod("ReadLine").Invoke(port,null)).Trim();if(line.Length<=64&&input.Count<128)input.Enqueue(line);}
    catch(System.Reflection.TargetInvocationException e){if(!(e.InnerException is TimeoutException))throw;}
   }
  }catch(Exception e){input.Enqueue("STATUS:Disconnected: "+(e.InnerException?.Message??e.Message));}
  finally{running=false;connected=false;if(port is IDisposable d)d.Dispose();}
 }
 void Start(){Connect();}
 void Update(){
  while(input.TryDequeue(out var line)){if(line.StartsWith("STATUS:")){status=line.Substring(7);continue;}Receive(line);}
  if(waiting&&Time.unscaledTime-sentAt>3){waiting=false;status="Gate ACK timed out. Inspect hardware, then explicitly retry.";}
 }
 public void Receive(string line){
  if(!SerialProtocol.Parse(line,out var kind,out var n))return;
  if(kind=="POT")potentiometer=n/1023f;
  if(kind=="BTN"){bool down=n==1;if(down&&!previousButton)Diagnostic?.Invoke();previousButton=down;}
  if(kind=="ACK:GATE:OPEN"&&lastCommand=="GATE:OPEN"){waiting=false;acknowledged=true;status="Gate OPEN acknowledged";}
 }
 public void SimulateButton(){Receive("BTN:1");Receive("BTN:0");}
 public bool RequestGate(VillageSave state){
  if(state==null||!state.Authorized)return false;
  if(!simulation&&!state.PhysicalChestAuthorized){status="Physical chest requires the Elder photograph, Alchemist dream record and three LIVE camera records; simulated evidence cannot release the latch.";return false;}
  if(waiting||acknowledged)return true;
  if(!simulation&&!connected){status="Validated; hardware disconnected. Connect, then retry the verified command.";return false;}
  lastCommand="GATE:OPEN";waiting=true;sentAt=Time.unscaledTime;acknowledged=false;
  if(simulation){Receive("ACK:GATE:OPEN");status="SIMULATED gate OPEN acknowledgment";}else output.Enqueue(lastCommand);
  return true;
 }
 public void ResetSession(){lastCommand="";acknowledged=false;waiting=false;previousButton=false;while(output.TryDequeue(out _)){}status=simulation?"Simulation ready":"Chest session reset; reset the physical latch manually.";}
 public void Disconnect(){
  running=false;
  if(worker!=null){worker.Join(500);if(!worker.IsAlive)worker=null;}
  while(output.TryDequeue(out _)){}while(input.TryDequeue(out _)){}
  connected=false;previousButton=false;waiting=false;acknowledged=false;lastCommand="";
 }
 void OnDestroy(){Disconnect();}void OnApplicationQuit(){Disconnect();}
}
}
