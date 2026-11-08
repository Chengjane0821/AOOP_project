using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
namespace ForgottenVillage {
public static class VillageStorage {
 public static string overridePath; public static string PathName=>overridePath??Path.Combine(Application.persistentDataPath,"forgotten-village-v1.json");
 public static void Save(VillageSave s){ var tmp=PathName+".tmp"; File.WriteAllText(tmp,JsonUtility.ToJson(s,true)); if(File.Exists(PathName))File.Replace(tmp,PathName,PathName+".bak");else File.Move(tmp,PathName); }
 public static VillageSave Load(){foreach(var p in new[]{PathName,PathName+".bak"}){try{if(File.Exists(p)){var s=JsonUtility.FromJson<VillageSave>(File.ReadAllText(p));if(s!=null&&s.version>=1&&s.version<=3){s.Normalize();return s;}}}catch(Exception e){Debug.LogWarning("Save recovery: "+e.Message);}}return new VillageSave();}
}
public class VillageGame : MonoBehaviour {
 public HouseTransitions transitions;public UnityEngine.XR.Interaction.Toolkit.XRInteractionManager interactionManager; public VillageAudio audioFeedback; public VillagePlayer player; public VillageUI ui; public HardwareConsole hardware; public Transform villageSpawn; public Transform[] interiors, entrances; public Transform gate;
 public VillageSave state=new VillageSave(); public bool playing; public int house=-1;
 public ObjectRecognitionClient recognition;
 void Awake(){if(!recognition)recognition=GetComponent<ObjectRecognitionClient>();if(!recognition)recognition=gameObject.AddComponent<ObjectRecognitionClient>();recognition.game=this;state.Normalize();}
 public static readonly string[] Houses={"Elder's House","Alchemist's House","Carpenter's House","Technician's House","Clockmaker's House","Abandoned House"};
 public static readonly string[] Clues={"The Forgotten Person / Scratched Photograph","Memory Signal Filtering Record","Analog Front-End Record","Safety Controller Software Record","Timing Analysis Record"};
 public static readonly string[] Notes={"A table tennis match restored the Elder's memory of a missing friend who investigated memories and strange experiments. The scratched photograph hides a sixth villager.","The Memory Interface Controller isolates a useful low-frequency band from high-frequency noise.","The analog front end uses an inverting common-source stage. The small-signal gain depends on transconductance and the parallel drain load.","Deployed pass-by-value code slices the derived safety controller. The unsafe test logs RUN; the reference fix correctly returns STOP.","The original path has negative setup slack. This is a potential timing violation, not proof that it caused the incident."};
 void Start(){player.Teleport(villageSpawn.position, villageSpawn.eulerAngles.y);ui.Menu();}
 public void NewGame(){if(transitions&&transitions.Busy)return;recognition.Cancel();state=new VillageSave();if(gate)gate.localRotation=Quaternion.identity;hardware.ResetSession();foreach(var a in FindObjectsByType<VillageAction>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(a.kind=="pickup")a.gameObject.SetActive(true);foreach(var station in FindObjectsByType<EngineeringStation>(FindObjectsSortMode.None))station.ResetValues();Commit();Begin();}
 public void Continue(){if(transitions&&transitions.Busy)return;recognition.Cancel();state=VillageStorage.Load();hardware.ResetSession();Begin();}
 void Begin(){playing=true;StartCoroutine(BeginAfterReturn());}
 System.Collections.IEnumerator BeginAfterReturn(){
  if(transitions){transitions.ReturnToStart();while(transitions.Busy)yield return null;}
  house=-1;player.Teleport(villageSpawn.position,villageSpawn.eulerAngles.y);ui.RefreshTracker();
  if(state.ended){ui.Ending();yield break;}
  ui.Message("The Missing Researcher",state.elder?"Your investigation is restored. Win the Elder's table tennis match and inspect his photograph. Complete the other residents' puzzles and object requests to recover all five clues.":"You arrive at a quiet village surrounded by mountains. Something feels wrong.\n\nSpeak to the Village Elder beside the amber sign.");
 }
 public void Commit(){try{VillageStorage.Save(state);}catch(Exception e){Debug.LogError(e);ui.Toast("Save failed: "+e.Message);}ui.RefreshTracker();}
 public bool Award(int id){if(!state.CanClaim(id)){ui.Toast("Complete this house's challenge and evidence requirements first.");return false;}if(state.clues[id])return true;state.clues[id]=true;if(!state.inventory.Contains(Clues[id]))state.inventory.Add(Clues[id]);Commit();if(audioFeedback)audioFeedback.Success();ui.Toast("Record recovered: "+Clues[id]+(state.Unlocked?" - Abandoned House unlocked":""));return true;}
 public void CompletePuzzle(int id){if(!state.elder||id<1||id>=5)return;state.puzzleSolved[id]=true;Commit();ui.Show("Engineering puzzle completed",Notes[id]+"\n\nThe resident now needs a real-world "+ObjectQuests.Categories[id]+" ("+ObjectQuests.Roles[id]+"). Camera verification is required before this clue is awarded.");ui.Button("Speak to resident",()=>ui.Dialogue(id));ui.Button("Close",ui.Close);}
 public void Enter(int id){if(!playing||id<0||id>=6)return;if(!state.elder){ui.Toast("Meet the Village Elder before investigating.");return;}if(id==5&&!state.Unlocked){ui.Toast("Locked: complete all five house challenges and collect their clues.");return;}if(transitions){transitions.Enter(id);return;}house=id;player.Teleport(interiors[id].position,interiors[id].eulerAngles.y);ui.Close();ui.Toast(Houses[id]);}
 public void Exit(){if(house<0)return;if(transitions){transitions.Exit();return;}player.Teleport(entrances[house].position,entrances[house].eulerAngles.y);house=-1;ui.Close();Commit();}
 public void PickUp(string id,string description){if(!state.inventory.Contains(id)){state.inventory.Add(id);Commit();}ui.Message(id,description);}
 public void InspectPhoto(){if(house!=0||!state.elderTennisWon||!state.elderMemoryRecovered){ui.Toast("Win the Elder's match and hear his recovered memory first.");return;}state.photograph=true;Award(0);PickUp("Scratched photograph","Six villagers stand around the controller. One face has been scratched away. The figure wears a researcher's wristband identical to yours.");}
 public void Review(int i){if(!state.Unlocked)return;string[] text={"The photograph links the missing villager to the Memory Interface Controller. Four technical records describe its filter, analog front end, software safety decisions and timing. The deployed revision is MIC-17.","Deployed MIC-17 uses evaluate(Controller c). Runtime log: unsafe=true; evaluate=RUN; gate enabled. Reproducing that exact implementation confirms object slicing.","Timing report: T=10 ns, setup=1 ns, uncertainty=0.5 ns, clock-to-Q=1 ns, combinational=9 ns. Slack=-1.5 ns. No incident waveform exists: timing remains an additional risk.","Unsafe sequence: unsafe sensor asserted -> derived controller copied into base value -> RUN logged -> experiment enabled. Your signed notebook and wristband identify you as the researcher."};state.evidence[i]=true;Commit();ui.Message("Laboratory evidence "+(i+1),text[i]);}
 public bool Finish(){if(!state.Authorized){ui.Toast("Review all four laboratory records and reproduce both controller diagnostics first.");return false;}if(state.ended){ui.Ending();return true;}state.ended=true;Commit();hardware.RequestGate(state);if(gate)gate.localRotation=Quaternion.Euler(0,0,80);ui.Ending();return true;}
 public string Objective=>!state.elder?"Meet the Village Elder":!state.Unlocked?"Challenges "+state.puzzleSolved.Count(x=>x)+"/5 | Objects "+Enumerable.Range(1,4).Count(state.ObjectVerified)+"/4 | Clues "+state.clues.Count(x=>x)+"/5":!state.Authorized?"Enter the Abandoned House; review all four evidence stations":!state.ended?"Release the archive chest latch":"The truth is remembered";
}
}




