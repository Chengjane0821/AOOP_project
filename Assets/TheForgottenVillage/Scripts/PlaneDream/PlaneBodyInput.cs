using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace ForgottenVillage {
public class PlaneBodyInput : MonoBehaviour {
 public PlaneDreamTuning tuning;public bool controllerMode;public bool allowControllerLift=true;
 public Vector2 Command {get;private set;} public string Status {get;private set;}="Calibrate before flight";
 public bool Ready {get;private set;}
 public readonly PlaneBodyMapper mapper=new PlaneBodyMapper();
 float desktopX;Vector3 originPosition;Quaternion originRotation;bool calibrated;
 public bool Calibrate(VillagePlayer player){
  Command=Vector2.zero;desktopX=0;Ready=calibrated=false;
  if(!player.IsVR){calibrated=Ready=true;Status="Desktop controls ready";return true;}
  if(!TrackedPosition(out var position)){Status="Headset tracking unavailable";return false;}
  mapper.Calibrate(position);originPosition=player.origin.transform.position;originRotation=player.origin.transform.rotation;
  calibrated=Ready=true;Status=controllerMode?"Seated / controller controls":"Body tracking calibrated";return true;
 }
 static bool TrackedPosition(out Vector3 position){var device=InputDevices.GetDeviceAtXRNode(XRNode.Head);position=default;
  return device.isValid&&device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool tracked)&&tracked&&device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out position);
 }
 public bool Sample(VillagePlayer player,float dt){
  if(!calibrated){Ready=false;return false;}
  if(!player.IsVR){var k=Keyboard.current;if(k!=null)desktopX=Mathf.Clamp(desktopX+((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0))*dt*1.3f,-1,1);Command=k==null?Vector2.zero:new Vector2(desktopX,(k.spaceKey.isPressed?1:0)-(k.leftCtrlKey.isPressed?1:0));Ready=true;return true;}
  if(!TrackedPosition(out var position)){Ready=false;calibrated=false;Status="Tracking lost. Restore tracking and recalibrate.";return false;}
  // Locomotion/recenter is not body motion. Freeze rather than interpreting an origin jump as input.
  if(Vector3.Distance(originPosition,player.origin.transform.position)>.02f||Quaternion.Angle(originRotation,player.origin.transform.rotation)>1){Ready=calibrated=false;Status="Play-space origin changed. Recalibrate to continue.";return false;}
  if(!mapper.Sample(position,dt,tuning)){Ready=calibrated=false;Status="Tracking discontinuity. Recalibrate to continue.";return false;}
  var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
  right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out bool up);right.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out bool down);
  if(controllerMode){left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out var axis);Command=new Vector2(Mathf.Abs(axis.x)>.12f?axis.x:0,up?1:down?-1:Mathf.Abs(axis.y)>.12f?axis.y:0);}
  else{Command=mapper.Command;if(allowControllerLift&&up)Command=new Vector2(Command.x,1);}
  Ready=true;return true;
 }
}
}
