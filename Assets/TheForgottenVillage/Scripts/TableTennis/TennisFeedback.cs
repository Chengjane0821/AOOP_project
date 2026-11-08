using UnityEngine;
using UnityEngine.XR;
namespace ForgottenVillage {
public class TennisFeedback : MonoBehaviour {
 public ElderTennis match; public AudioSource audioSource; public AudioClip hitClip,smashClip,bounceClip;
 AudioClip generatedHit,generatedSmash;
 void Awake(){if(!audioSource)audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=.7f;audioSource.minDistance=1;audioSource.maxDistance=8;generatedHit=Tone("Paddle contact",650,.06f);generatedSmash=Tone("Smash contact",1100,.16f);}
 static AudioClip Tone(string name,float frequency,float duration){const int rate=22050;var data=new float[(int)(rate*duration)];for(int i=0;i<data.Length;i++)data[i]=Mathf.Sin(i*frequency*2*Mathf.PI/rate)*Mathf.Exp(-i/(rate*.018f))*.6f;var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;}
 public void Hit(bool player,bool smash){audioSource.PlayOneShot(smash?(smashClip?smashClip:generatedSmash):(hitClip?hitClip:generatedHit),smash?.7f:.45f);
  if(player&&match.game.player.IsVR){var device=InputDevices.GetDeviceAtXRNode(match.playerPaddle.hand);if(device.TryGetHapticCapabilities(out var capabilities)&&capabilities.supportsImpulse)device.SendHapticImpulse(0,smash?.65f:.2f,smash?.1f:.035f);}
  var trail=match.ball.GetComponent<TrailRenderer>();if(trail){trail.startColor=smash?new Color(1,.45f,.05f):Color.white;trail.endColor=new Color(1,1,1,0);}
 }
 public void Bounce(){audioSource.PlayOneShot(bounceClip?bounceClip:generatedHit,.16f);}
 void OnDestroy(){if(generatedHit)Destroy(generatedHit);if(generatedSmash)Destroy(generatedSmash);}
}
}
