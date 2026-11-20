using UnityEngine;
namespace ForgottenVillage {
public sealed class BirdCallAmbience : MonoBehaviour {
 public VillageGame game; public AudioSource source; public AudioClip[] calls; public float minimumDelay=18f, maximumDelay=34f;
 float next;
 void OnEnable(){next=Time.time+Random.Range(3f,9f);}
 void OnDisable(){if(source)source.Stop();}
 void Update(){if(!game||!game.playing||!source||calls==null||calls.Length==0)return;if(Time.time<next)return;var clip=calls[Random.Range(0,calls.Length)];source.PlayOneShot(clip);next=Time.time+Random.Range(minimumDelay,maximumDelay);}
}
}
