using UnityEngine;
namespace ForgottenVillage {
public class VillageAction : MonoBehaviour {
 public VillageGame game;public EngineeringStation station;public string kind;public int index;public string item,description;
 public void Use(){if(!game)return;if(game.audioFeedback)game.audioFeedback.Click();switch(kind){case "diagnostic":station.Diagnostic();break;case "switch":station.ToggleLoad();break;case "enter":game.Enter(index);break;case "exit":game.Exit();break;case "dialogue":game.ui.Dialogue(index);break;case "puzzle":station.Open();break;case "photo":game.InspectPhoto();break;case "evidence":game.Review(index);break;case "final":game.Finish();break;case "menu":game.ui.Pause();break;case "pickup":game.PickUp(item,description);gameObject.SetActive(false);break;}}
 void Start(){if(game&&kind=="pickup"&&game.state.inventory.Contains(item))gameObject.SetActive(false);}
}
}



