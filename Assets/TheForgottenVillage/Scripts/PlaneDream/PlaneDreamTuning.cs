using UnityEngine;
namespace ForgottenVillage {
[CreateAssetMenu(menuName="Forgotten Village/Plane dream tuning")]
public class PlaneDreamTuning : ScriptableObject {
 [Header("Small, comfortable body motions (metres)")]
 [Range(.05f,.25f)] public float liftThreshold=.11f;
 [Range(.02f,.12f)] public float liftRelease=.05f;
 [Range(.05f,.4f)] public float crouchThreshold=.18f;
 [Range(.02f,.2f)] public float crouchRelease=.09f;
 public float minimumLiftVelocity=.12f, gestureDwell=.06f;
 [Range(.01f,.12f)] public float lateralDeadZone=.035f;
 [Range(.1f,.5f)] public float lateralFullRange=.25f;
 [Range(.02f,.3f)] public float inputSmoothing=.08f;
 [Header("Airplane response and bounds")]
 public float halfWidth=1.7f, normalHeight=1.8f, verticalRange=1.0f;
 public float movementSpeed=3.5f, acceleration=12f, bankAngle=18f, pitchAngle=10f;
 [Header("Fair obstacle approach")]
 public float initialWorldSpeed=4.5f, maximumWorldSpeed=6f, spawnDistance=22f;
 public float initialSpawnInterval=4.2f, finalSpawnInterval=3.2f;
 public float minimumPassageInterval=2.8f, minimumReactionTime=3f;
 public int poolSize=8;
 public void Validate(){
  liftThreshold=Mathf.Clamp(liftThreshold,.05f,.25f);liftRelease=Mathf.Clamp(liftRelease,.01f,liftThreshold-.01f);
  crouchThreshold=Mathf.Clamp(crouchThreshold,.05f,.4f);crouchRelease=Mathf.Clamp(crouchRelease,.01f,crouchThreshold-.01f);
  minimumLiftVelocity=Mathf.Clamp(minimumLiftVelocity,.02f,1);gestureDwell=Mathf.Clamp(gestureDwell,.02f,.3f);
  lateralDeadZone=Mathf.Clamp(lateralDeadZone,.01f,.12f);lateralFullRange=Mathf.Clamp(lateralFullRange,lateralDeadZone+.05f,.5f);
  inputSmoothing=Mathf.Clamp(inputSmoothing,.02f,.3f);halfWidth=Mathf.Clamp(halfWidth,1.2f,3);verticalRange=Mathf.Clamp(verticalRange,.5f,1.5f);normalHeight=Mathf.Max(normalHeight,verticalRange+.3f);
  movementSpeed=Mathf.Clamp(movementSpeed,1,6);acceleration=Mathf.Clamp(acceleration,4,20);
  initialWorldSpeed=Mathf.Clamp(initialWorldSpeed,2,8);maximumWorldSpeed=Mathf.Clamp(maximumWorldSpeed,initialWorldSpeed,10);
  minimumReactionTime=Mathf.Max(3,minimumReactionTime);minimumPassageInterval=Mathf.Max(2.8f,minimumPassageInterval);
  initialSpawnInterval=Mathf.Max(3,initialSpawnInterval);finalSpawnInterval=Mathf.Clamp(finalSpawnInterval,2.8f,initialSpawnInterval);poolSize=Mathf.Clamp(poolSize,4,24);
 }
 void OnValidate(){Validate();}
 void OnEnable(){Validate();}
 public float WorldSpeed(float progress)=>Mathf.Lerp(initialWorldSpeed,maximumWorldSpeed,Mathf.Clamp01(progress));
 public float SpawnInterval(float progress)=>Mathf.Lerp(initialSpawnInterval,finalSpawnInterval,Mathf.Clamp01(progress));
 public float SafeSpacing=>Mathf.Max(maximumWorldSpeed*minimumPassageInterval,maximumWorldSpeed*(2*halfWidth/Mathf.Max(.1f,movementSpeed)+2*movementSpeed/Mathf.Max(.1f,acceleration)+1.2f));
 public float SafeSpawnDistance=>Mathf.Max(spawnDistance,maximumWorldSpeed*minimumReactionTime+1);
}
}
