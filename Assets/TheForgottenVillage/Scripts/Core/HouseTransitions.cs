using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ForgottenVillage {
public class HouseTransitions : MonoBehaviour {
 public VillageGame game;public GameObject[] outdoorRoots;public bool Busy{get;private set;}public HouseSceneRoot Current{get;private set;}
 public static readonly string[] SceneNames={"EldersHouse","AlchemistsHouse","CarpentersHouse","TechniciansHouse","ClockmakersHouse","AbandonedHouse"};
 Scene villageScene;
 readonly List<GameObject> hiddenOutdoorRoots=new List<GameObject>();
 void Awake(){villageScene=gameObject.scene;}
 public void Enter(int index){if(Busy||Current||index<0||index>=SceneNames.Length)return;StartCoroutine(Load(index));}
 // Keep the village's deliberately inactive cameras inactive. Only restore roots
 // actually hidden by this transition, never every serialized outdoor reference.
 void HideOutdoors(){
  hiddenOutdoorRoots.Clear();
  foreach(var root in outdoorRoots){
   if(!root||!root.activeSelf||ContainsSharedPlayer(root.transform))continue;
   hiddenOutdoorRoots.Add(root);root.SetActive(false);
  }
 }
 bool ContainsSharedPlayer(Transform root){
  return transform.IsChildOf(root)||game.transform.IsChildOf(root)||game.player.transform.IsChildOf(root)
   ||game.player.xrRig.transform.IsChildOf(root)||game.ui.transform.IsChildOf(root)
   ||(game.interactionManager&&game.interactionManager.transform.IsChildOf(root));
 }
 void RestoreOutdoors(){foreach(var root in hiddenOutdoorRoots)if(root)root.SetActive(true);hiddenOutdoorRoots.Clear();}
 IEnumerator Load(int index){
  if(index==5&&!game.state.Unlocked){game.ui.Toast("Collect all five verified clues first.");yield break;}
  game.recognition.Cancel();Busy=true;game.ui.Message("Entering "+VillageGame.Houses[index],"Loading the room and restoring your investigation...");
  var op=SceneManager.LoadSceneAsync(SceneNames[index],LoadSceneMode.Additive);if(op==null){Busy=false;game.ui.Message("Room unavailable","The house scene is missing from Build Settings.");yield break;}yield return op;
  var scene=SceneManager.GetSceneByName(SceneNames[index]);foreach(var root in scene.GetRootGameObjects()){var candidate=root.GetComponent<HouseSceneRoot>();if(candidate){Current=candidate;break;}}
  if(!Current){yield return SceneManager.UnloadSceneAsync(scene);Busy=false;game.ui.Message("Room unavailable","No configured house root was found.");yield break;}
  Current.Bind(game);game.house=index;SceneManager.SetActiveScene(scene);HideOutdoors();
  game.player.Teleport(Current.arrival.position,Current.arrival.eulerAngles.y);Physics.SyncTransforms();game.Commit();Busy=false;game.ui.Close();game.ui.Toast(VillageGame.Houses[index]);if(index==0){var tennis=Current.GetComponentInChildren<ElderTennis>();if(tennis)tennis.Offer();}
 }
 public void Exit(){if(Busy||!Current)return;StartCoroutine(Return(false));}
 public void ReturnToStart(){if(Busy)return;if(Current)StartCoroutine(Return(true));else game.player.Teleport(game.villageSpawn.position,game.villageSpawn.eulerAngles.y);}
 IEnumerator Return(bool start){
  game.recognition.Cancel();Busy=true;int index=Current.houseIndex;var scene=Current.gameObject.scene;
  // Finish activity cleanup before moving the persistent player or unloading its references.
  Current.PrepareForUnload();Time.timeScale=1;
  game.ui.Message("Returning to village","Your records have been saved.");game.Commit();SceneManager.SetActiveScene(villageScene);RestoreOutdoors();
  var destination=start?game.villageSpawn:game.entrances[index];game.player.Teleport(destination.position,destination.eulerAngles.y);game.house=-1;game.gate=null;Current=null;yield return SceneManager.UnloadSceneAsync(scene);Busy=false;game.ui.Close();
 }
}
}
