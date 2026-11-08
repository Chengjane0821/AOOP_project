using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
namespace ForgottenVillage {
public class VillagePlayer : MonoBehaviour {
 public GameObject xrRig; public Camera desktopCamera; public CharacterController body; public VillageGame game; public XROrigin origin; public bool forceDesktop; public float speed=2.5f; public bool IsVR{get;private set;}
 public bool ActivityLocksMovement;
 float pitch,fall;MonoBehaviour[] locomotion;bool menuPressed;
 void Awake(){locomotion=xrRig.GetComponentsInChildren<MonoBehaviour>(true);IsVR=XRSettings.isDeviceActive&&!forceDesktop;xrRig.SetActive(IsVR);desktopCamera.gameObject.SetActive(!IsVR);body.enabled=!IsVR;}
 public Camera View=>IsVR?origin.Camera:desktopCamera;
 public void Teleport(Vector3 p,float yaw){if(IsVR){origin.transform.rotation=Quaternion.Euler(0,yaw,0);origin.MoveCameraToWorldLocation(p+Vector3.up*origin.CameraInOriginSpaceHeight);}else{body.enabled=false;transform.position=p;transform.rotation=Quaternion.Euler(0,yaw,0);body.enabled=true;pitch=0;desktopCamera.transform.localRotation=Quaternion.identity;fall=0;}}
 void Update(){if(IsVR){bool allow=!ActivityLocksMovement&&game.playing&&!game.ui.IsOpen&&!(game.transitions&&game.transitions.Busy);foreach(var b in locomotion)if(b&&(b.GetType().Name.Contains("MoveProvider")||b.GetType().Name.Contains("SnapTurnProvider")))b.enabled=allow;var device=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton,out bool pressed);if(pressed&&!menuPressed){if(game.ui.IsOpen)game.ui.Close();else game.ui.Pause();}menuPressed=pressed;}var k=Keyboard.current;var m=Mouse.current;if(k!=null&&k.escapeKey.wasPressedThisFrame){if(game.ui.IsOpen)game.ui.Close();else game.ui.Pause();}if(k!=null&&k.jKey.wasPressedThisFrame)game.ui.Journal();if(k!=null&&k.iKey.wasPressedThisFrame)game.ui.Inventory();if(IsVR||k==null||m==null)return;bool move=game.playing&&!game.ui.IsOpen&&!(game.transitions&&game.transitions.Busy);Cursor.lockState=move?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!move;if(!move)return;if(ActivityLocksMovement){game.ui.Prompt("Mouse: paddle | F: forward swing | Space: Grip | Esc: pause");return;}var d=m.delta.ReadValue()*.09f;transform.Rotate(0,d.x,0);pitch=Mathf.Clamp(pitch-d.y,-80,80);desktopCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);Vector3 v=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));fall=body.isGrounded?-1:fall-9.81f*Time.deltaTime;body.Move((transform.TransformDirection(v.normalized)*speed+Vector3.up*fall)*Time.deltaTime);if(transform.position.y< -30)Teleport(game.villageSpawn.position,0);if(Physics.Raycast(View.transform.position,View.transform.forward,out var hit,4)){var target=hit.collider.GetComponentInParent<VillageInteractable>();game.ui.Prompt(target?"E - "+target.prompt:"");if(target&&k.eKey.wasPressedThisFrame)target.Activate();}else game.ui.Prompt("");}
}
}



