using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ForgottenVillage {
public class AlchemistDreamHouse : MonoBehaviour {
 public VillageGame game;public PlaneDreamRun dream;public GameObject evidence;public Transform returnPoint;
 public GameObject[] roomObjects;public bool InDream {get;private set;}bool transitioning;
 readonly List<GameObject> hidden=new List<GameObject>();string oldPrompt;
 public void Bind(VillageGame value){game=value;dream.house=this;RefreshEvidence();}
 public void Offer(){if(!game||game.house!=1)return;if(InDream){dream.PauseMenu();return;}
  if(game.state.alchemistDreamWon&&!game.state.alchemistMemoryRecovered){Recovery();return;}
  game.ui.Show("Alchemist",game.state.alchemistDreamWon?"The dream is finally over. Please inspect the strange record I recovered. You can revisit the sky whenever you wish.":"Ah... you're here. Maybe you can help me.\nI've been having the strangest dream: an airplane flying endlessly through the sky.\nNo matter how far it flies, the dream never ends. I feel trapped inside it.\nCould you enter my dream and help me finally reach the end?");
  game.ui.Button("I'll help you.",EnterDream);game.ui.Button("Maybe later.",game.ui.Close);
 }
 public void EnterDream(){if(transitioning||InDream||!game||game.house!=1)return;StartCoroutine(Enter());}
 IEnumerator Enter(){transitioning=true;game.ui.Show("The dream opens...","A quiet viewing station appears beneath an endless sky.");yield return new WaitForSecondsRealtime(.4f);
  InDream=true;hidden.Clear();foreach(var o in roomObjects)if(o&&o.activeSelf){hidden.Add(o);o.SetActive(false);}
  dream.gameObject.SetActive(true);oldPrompt=game.player.ActivityPrompt;game.player.ActivityLocksMovement=true;game.player.ActivityPrompt="";game.player.Teleport(dream.viewingStation.position,dream.viewingStation.eulerAngles.y);dream.Introduction();transitioning=false;
 }
 public void CompleteDream(){if(!InDream||transitioning)return;game.state.alchemistDreamWon=true;game.state.puzzleSolved[1]=true;game.Commit();StartCoroutine(Success());}
 IEnumerator Success(){transitioning=true;game.ui.Show("The horizon is within reach","The endless sky begins to fade...\nThe airplane finally reaches the horizon.\nSomething hidden in the dream has been released.");yield return new WaitForSecondsRealtime(2);transitioning=false;ReturnFromDream(true);}
 public void ReturnFromDream(bool success){if(!InDream)return;StopAllCoroutines();transitioning=false;dream.Stop();InDream=false;dream.gameObject.SetActive(false);foreach(var o in hidden)if(o)o.SetActive(true);hidden.Clear();game.player.ActivityLocksMovement=false;game.player.ActivityPrompt=oldPrompt;game.player.Teleport(returnPoint.position,returnPoint.eulerAngles.y);RefreshEvidence();if(success)Recovery();else game.ui.Close();}
 public void Recovery(){if(!game.state.alchemistDreamWon)return;game.ui.Show("Alchemist — I remember now","The dream... it's finally over.\nThat airplane wasn't just a dream. It was connected to an experiment about memories.\nI found this strange record after the incident.\nPlease take a look. Maybe it will help you discover the truth.");game.ui.Button("Show me the record",()=>{game.state.alchemistMemoryRecovered=true;game.Commit();RefreshEvidence();game.ui.Close();game.ui.Toast("Inspect the recovered dream record to collect Clue #2.");});}
 public void InspectEvidence(){if(!game||game.house!=1||!game.state.alchemistDreamWon||!game.state.alchemistMemoryRecovered){if(game)game.ui.Toast("End the dream and hear the Alchemist's memory first.");return;}game.state.alchemistEvidenceInspected=true;if(game.Award(1))game.ui.Message("Recovered dream record",VillageGame.Notes[1]);}
 public void RefreshEvidence(){if(evidence&&game)evidence.SetActive(game.state.alchemistDreamWon&&game.state.alchemistMemoryRecovered);}
 void OnDisable(){if(game&&game.player&&InDream){game.player.ActivityLocksMovement=false;game.player.ActivityPrompt=oldPrompt;}InDream=false;}
}
}
