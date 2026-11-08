using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace ForgottenVillage {
[RequireComponent(typeof(Rigidbody),typeof(BoxCollider))]
public class TennisPaddle : MonoBehaviour {
 public ElderTennis match; public bool player; public XRNode hand=XRNode.RightHand;
 public Vector3 gripOffset=new Vector3(0,0,.14f), rotationOffset=new Vector3(90,0,0);
 public float maxTrackingSpeed=12; public Vector3 velocity {get;private set;} public bool Grip {get;private set;}
 public bool TrackingValid {get;private set;}
 Rigidbody body; Vector3 target,previous; Quaternion orientation; bool initialized;
 void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;target=previous=transform.position;orientation=transform.rotation;}
 void Update(){if(!player||!match||!match.game)return;
  var p=match.game.player;
  if(p.IsVR){
   // Read the same tracking-space poses used by XRI; transform through the camera offset.
   var device=InputDevices.GetDeviceAtXRNode(hand);
   Grip=device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,out bool grip)&&grip;
   TrackingValid=false;
   if(!device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool valid)||!valid){initialized=false;return;}
   if(device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition,out var pos)&&device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation,out var rot)){
    TrackingValid=true;var space=p.origin.Camera.transform.parent;orientation=space.rotation*rot*Quaternion.Euler(rotationOffset);target=space.TransformPoint(pos)+space.rotation*rot*gripOffset;
   }
  } else {
   TrackingValid=true;
   // Mouse motion physically moves a simulated paddle; Space only holds Grip.
   var mouse=Mouse.current;var key=Keyboard.current;Grip=key!=null&&key.spaceKey.isPressed;
   if(mouse!=null){var delta=mouse.delta.ReadValue();desktopOffset+=new Vector2(delta.x,delta.y)*.003f;desktopOffset.x=Mathf.Clamp(desktopOffset.x,-.85f,.85f);desktopOffset.y=Mathf.Clamp(desktopOffset.y,-.2f,.9f);}
   target=match.transform.TransformPoint(new Vector3(desktopOffset.x,1.05f+desktopOffset.y,-1.25f));
   float push=key!=null&&key.fKey.isPressed?.38f:0;target+=match.transform.forward*push;
   orientation=match.transform.rotation;
  }
 }
 Vector2 desktopOffset;
 void FixedUpdate(){if(!player||!match||!match.Running)return;
  if(!TrackingValid)return;
  if(!initialized){previous=target;body.position=target;initialized=true;}
  velocity=Vector3.ClampMagnitude((target-previous)/Time.fixedDeltaTime,maxTrackingSpeed);previous=target;
  body.MovePosition(target);body.MoveRotation(orientation);
 }
 public void MoveAI(Vector3 position,Quaternion rotation){TrackingValid=true;velocity=(position-body.position)/Time.fixedDeltaTime;body.MovePosition(position);body.MoveRotation(rotation);}
 public void ResetTracking(){initialized=false;TrackingValid=!player;velocity=Vector3.zero;}
}
}
