using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage {
public class WorkshopSocket : MonoBehaviour {
 public VillageGame game;public Light indicator;
 void OnTriggerEnter(Collider other){if(!game)return;if(!other.GetComponentInParent<XRGrabInteractable>())return;if(indicator)indicator.color=new Color(.25f,1,.6f);game.PickUp("Workshop component examined","The restored measurement socket confirms these are analog front-end parts. Solve the gain calculation to recover the engineering record.");}
}
}

