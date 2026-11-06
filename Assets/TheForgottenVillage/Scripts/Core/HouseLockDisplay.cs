using System.Linq;
using TMPro;
using UnityEngine;
namespace ForgottenVillage {
public class HouseLockDisplay : MonoBehaviour {
 public VillageGame game;public TextMeshPro label,detailLabel;public VillageInteractable interaction;public GameObject sealVisual;bool last;bool initialized;int lastCount=-1;
 void Update(){if(!game||!label||!interaction)return;bool open=game.state.Unlocked;int count=game.state.clues.Count(x=>x);if(initialized&&last==open&&lastCount==count)return;initialized=true;last=open;lastCount=count;label.text="Abandoned House\n"+(open?"UNLOCKED - INVESTIGATE":"LOCKED - FIVE RECORDS REQUIRED");if(detailLabel)detailLabel.text=open?"SEAL RELEASED - INVESTIGATE":"SEALED - RECORDS "+count+" / 5";if(sealVisual)sealVisual.SetActive(!open);interaction.prompt=open?"Enter Abandoned House":"Inspect locked laboratory door";}
}
}
