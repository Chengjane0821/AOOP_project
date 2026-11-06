using UnityEngine;
namespace ForgottenVillage {
public class VillageAudio : MonoBehaviour {
 public AudioSource effects,ambient;public AudioClip clickAsset,successAsset;AudioClip click,success;
 void Awake(){effects=gameObject.AddComponent<AudioSource>();effects.playOnAwake=false;effects.volume=.24f;effects.spatialBlend=0;ambient=gameObject.AddComponent<AudioSource>();ambient.playOnAwake=false;ambient.loop=true;ambient.volume=.025f;click=clickAsset?clickAsset:Tone("Console click",.065f,660,880);success=successAsset?successAsset:Tone("Record recovered",.45f,440,880);ambient.clip=Tone("Memory room tone",6,82,82);ambient.Play();}
 static AudioClip Tone(string name,float duration,float start,float end){int rate=22050;var data=new float[(int)(duration*rate)];float phase=0;for(int i=0;i<data.Length;i++){float t=i/(float)rate;phase+=Mathf.Lerp(start,end,t/duration)/rate*2*Mathf.PI;float env=Mathf.Min(1,t/.01f)*Mathf.Min(1,(duration-t)/.03f);data[i]=Mathf.Sin(phase)*env*.18f;}var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;}
 public void Click(){if(effects)effects.PlayOneShot(click);}
 public void Success(){if(effects)effects.PlayOneShot(success);}
}
}
