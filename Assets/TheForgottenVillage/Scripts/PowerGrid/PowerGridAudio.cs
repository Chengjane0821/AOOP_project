using UnityEngine;
namespace ForgottenVillage {
    [RequireComponent(typeof(AudioSource))]
    public sealed class PowerGridAudio : MonoBehaviour {
        AudioSource source;
        AudioClip click,trip,restored;
        void Awake(){source=GetComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            click=Tone("Cable click",780,.07f);trip=Tone("Breaker trip",120,.3f);restored=Tone("Power restored",660,.35f);}
        static AudioClip Tone(string name,float frequency,float duration){
            int length=Mathf.CeilToInt(44100*duration);var samples=new float[length];
            for(int i=0;i<length;i++){float t=i/44100f;samples[i]=Mathf.Sin(2*Mathf.PI*frequency*t)*Mathf.Exp(-10*t)*Mathf.Min(1,i/60f);}
            var clip=AudioClip.Create(name,length,1,44100,false);clip.SetData(samples,0);return clip;
        }
        public void Click(float volume){if(source)source.PlayOneShot(click,volume);}
        public void Fault(float volume){if(source)source.PlayOneShot(trip,volume);}
        public void Restored(float volume){if(source)source.PlayOneShot(restored,volume);}
        public void Silence(){if(source)source.Stop();}
        void OnDestroy(){if(click)Destroy(click);if(trip)Destroy(trip);if(restored)Destroy(restored);}
    }
}
