using UnityEngine;
namespace ForgottenVillage {
public sealed class GentlePulse : MonoBehaviour {
 public Light target; public float baseIntensity=1f, amplitude=.12f, frequency=1f;
 void Update(){if(target)target.intensity=baseIntensity*(1f+Mathf.Sin(Time.time*frequency*Mathf.PI*2f)*amplitude);}
}
}
