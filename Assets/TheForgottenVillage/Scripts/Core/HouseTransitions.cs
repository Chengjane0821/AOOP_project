using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ForgottenVillage {
public class HouseTransitions : MonoBehaviour {
 public VillageGame game;public GameObject[] outdoorRoots;public bool Busy{get;private set;}public HouseSceneRoot Current{get;private set;}
 public static readonly string[] SceneNames={"EldersHouse","AlchemistsHouse","CarpentersHouse","TechniciansHouse","ClockmakersHouse","AbandonedHouse"};
 Scene villageScene;
 void Awake(){villageScene=gameObject.scene;}
 public void Enter(int index){if(Busy||Current)return;StartCoroutine(Load(index));}
 IEnumerator Load(int index){
  Busy=true;game.ui.Message("Entering "+VillageGame.Houses[index],"Loading the room and restoring your investigation...");
  var op=SceneManager.LoadSceneAsync(SceneNames[index],LoadSceneMode.Additive);if(op==null){Busy=false;game.ui.Message("Room unavailable","The house scene is missing from Build Settings.");yield break;}yield return op;
  var scene=SceneManager.GetSceneByName(SceneNames[index]);foreach(var root in scene.GetRootGameObjects()){var candidate=root.GetComponent<HouseSceneRoot>();if(candidate){Current=candidate;break;}}
  if(!Current){yield return SceneManager.UnloadSceneAsync(scene);Busy=false;game.ui.Message("Room unavailable","No configured house root was found.");yield break;}
  Current.Bind(game);game.house=index;SceneManager.SetActiveScene(scene);foreach(var root in outdoorRoots)if(root)root.SetActive(false);
  game.player.Teleport(Current.arrival.position,Current.arrival.eulerAngles.y);Physics.SyncTransforms();game.Commit();Busy=false;game.ui.Close();game.ui.Toast(VillageGame.Houses[index]);
 }
 public void Exit(){if(Busy||!Current)return;StartCoroutine(Return(false));}
 public void ReturnToStart(){if(Busy)return;if(Current)StartCoroutine(Return(true));else game.player.Teleport(game.villageSpawn.position,game.villageSpawn.eulerAngles.y);}
 IEnumerator Return(bool start){
  Busy=true;int index=Current.houseIndex;var scene=Current.gameObject.scene;game.ui.Message("Returning to village","Your records have been saved.");game.Commit();SceneManager.SetActiveScene(villageScene);foreach(var root in outdoorRoots)if(root)root.SetActive(true);
  var destination=start?game.villageSpawn:game.entrances[index];game.player.Teleport(destination.position,destination.eulerAngles.y);game.house=-1;game.gate=null;Current=null;yield return SceneManager.UnloadSceneAsync(scene);Busy=false;game.ui.Close();
 }
}
}
