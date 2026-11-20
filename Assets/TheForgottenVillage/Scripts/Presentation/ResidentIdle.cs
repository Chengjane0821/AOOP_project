using UnityEngine;
namespace ForgottenVillage {
public sealed class ResidentIdle : MonoBehaviour {
 public Transform body,head; Vector3 bodyHome,headHome; float phase;
 void Start(){if(body)bodyHome=body.localPosition;if(head)headHome=head.localPosition;phase=Random.value*6.28f;}
 void Update(){float breath=Mathf.Sin(Time.time*1.5f+phase);if(body)body.localPosition=bodyHome+Vector3.up*(.009f*breath);if(head)head.localPosition=headHome+Vector3.up*(.006f*breath);}
}
}
