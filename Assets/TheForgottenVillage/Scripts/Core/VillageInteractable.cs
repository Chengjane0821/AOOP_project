using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage {
[RequireComponent(typeof(Collider))] public class VillageInteractable : MonoBehaviour {
 public string prompt="Inspect"; public UnityEvent action=new UnityEvent();
 void Awake(){var xr=GetComponent<XRSimpleInteractable>();if(xr)xr.selectEntered.AddListener(_=>Activate());}
 public void Activate(){action.Invoke();}
}
}
