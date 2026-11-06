using UnityEngine;

namespace ForgottenVillage {
public sealed class BirdCallAmbience : MonoBehaviour {
 public VillageGame game; public AudioSource source; public AudioClip[] calls; public float minimumDelay=18f, maximumDelay=34f;
 float next;
 void OnEnable(){next=Time.time+Random.Range(3f,9f);}
 void OnDisable(){if(source)source.Stop();}
 void Update(){if(!game||!game.playing||!source||calls==null||calls.Length==0)return;if(Time.time<next)return;var clip=calls[Random.Range(0,calls.Length)];source.PlayOneShot(clip);next=Time.time+Random.Range(minimumDelay,maximumDelay);}
}
public sealed class GentlePulse : MonoBehaviour {
 public Light target; public float baseIntensity=1f, amplitude=.12f, frequency=1f;
 void Update(){if(target)target.intensity=baseIntensity*(1f+Mathf.Sin(Time.time*frequency*Mathf.PI*2f)*amplitude);}
}
public sealed class ResidentIdle : MonoBehaviour {
 public Transform body,head; Vector3 bodyHome,headHome; float phase;
 void Start(){if(body)bodyHome=body.localPosition;if(head)headHome=head.localPosition;phase=Random.value*6.28f;}
 void Update(){float breath=Mathf.Sin(Time.time*1.5f+phase);if(body)body.localPosition=bodyHome+Vector3.up*(.009f*breath);if(head)head.localPosition=headHome+Vector3.up*(.006f*breath);}
}
}
