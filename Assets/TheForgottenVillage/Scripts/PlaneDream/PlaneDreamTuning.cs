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
 public float WorldSpeed(float progress)=>Mathf.Lerp(initialWorldSpeed,maximumWorldSpeed,Mathf.Clamp01(progress));
 public float SpawnInterval(float progress)=>Mathf.Lerp(initialSpawnInterval,finalSpawnInterval,Mathf.Clamp01(progress));
 public float SafeSpacing=>Mathf.Max(maximumWorldSpeed*minimumPassageInterval,maximumWorldSpeed*(2*halfWidth/Mathf.Max(.1f,movementSpeed)+2*movementSpeed/Mathf.Max(.1f,acceleration)+1.2f));
 public float SafeSpawnDistance=>Mathf.Max(spawnDistance,maximumWorldSpeed*minimumReactionTime+1);
}
}
