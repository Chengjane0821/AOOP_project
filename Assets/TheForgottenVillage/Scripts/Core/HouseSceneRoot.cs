using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage {
public class HouseSceneRoot : MonoBehaviour {
 public int houseIndex;public Transform arrival;public Transform safetyGate;public GameObject[] copiedRoots;
 void Awake(){ClearInactiveStartingSelections(gameObject.scene.GetRootGameObjects());}
 // XRI starting selections bypass target validation. Hidden props have not run
 // Awake yet, so their cached Rigidbody is null even if the component exists.
 public static void ClearInactiveStartingSelections(GameObject[] roots){
  foreach(var root in roots)
   foreach(var socket in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true)){
    var target=socket.startingSelectedInteractable;
    if(target&&!target.isActiveAndEnabled)socket.startingSelectedInteractable=null;
   }
 }
 public void Bind(VillageGame game){
  foreach(var root in gameObject.scene.GetRootGameObjects()){
   foreach(var action in root.GetComponentsInChildren<VillageAction>(true)){action.game=game;if(action.kind=="pickup"&&game.state.inventory.Contains(action.item))action.gameObject.SetActive(false);}
   foreach(var station in root.GetComponentsInChildren<EngineeringStation>(true))station.Bind(game);
   foreach(var socket in root.GetComponentsInChildren<WorkshopSocket>(true))socket.game=game;
   foreach(var interactor in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true))if(interactor.gameObject.activeInHierarchy)interactor.interactionManager=game.interactionManager;
   foreach(var teleport in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.BaseTeleportationInteractable>(true))teleport.teleportationProvider=game.player.xrRig.GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider>(true);
   foreach(var interactable in root.GetComponentsInChildren<XRBaseInteractable>(true))if(interactable.gameObject.activeInHierarchy)interactable.interactionManager=game.interactionManager;
  }
  foreach(var tennis in GetComponentsInChildren<ElderTennis>(true))tennis.Bind(game);
  var alchemist=GetComponent<AlchemistDreamHouse>();if(alchemist)alchemist.Bind(game);
  var clock=GetComponent<ClockRhythmHouse>();if(clock)clock.Bind(game);
  var grid=GetComponent<PowerGridHouse>();if(grid)grid.Bind(game);
  var bridge=GetComponent<BridgeHouse>();if(bridge)bridge.Bind(game);
  game.gate=safetyGate;if(game.gate&&game.state.ended)game.gate.localRotation=Quaternion.Euler(0,0,80);
 }
}
}

