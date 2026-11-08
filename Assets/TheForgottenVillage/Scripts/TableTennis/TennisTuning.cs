using UnityEngine;
namespace ForgottenVillage {
[CreateAssetMenu(menuName="Forgotten Village/Table tennis tuning")]
public class TennisTuning : ScriptableObject {
 [Header("Ball and normal hits")]
 [Range(3,12)] public float maximumBallSpeed=9;
 [Range(0,1)] public float aimAssist=.75f;
 public float minimumSwingSpeed=.25f, contactCooldown=.12f, rallyTimeout=15;
 [Header("Grip smash: every condition must pass")]
 public float smashMinimumHeight=1.08f, smashMaximumHeight=2.1f;
 public float smashMinimumSwingSpeed=1.5f, smashMinimumForwardSpeed=.6f;
 public float smashMaximumRisingSpeed=.6f, smashMinimumFaceAlignment=.35f;
 [Header("Elder movement")]
 public float elderSpeed=2.8f, elderReactionTime=.12f;
 [Range(0,.25f)] public float elderError=.045f;
}
public static class TennisShots {
 public static bool IsSmash(TennisTuning t,Vector3 ball,Vector3 incoming,Vector3 swing,Vector3 normal){
  return ball.z<-.05f&&ball.z>-2.2f&&Mathf.Abs(ball.x)<1.05f&&ball.y>=t.smashMinimumHeight&&ball.y<=t.smashMaximumHeight
   &&incoming.z<0&&incoming.y<=t.smashMaximumRisingSpeed&&swing.magnitude>=t.smashMinimumSwingSpeed
   &&swing.z>=t.smashMinimumForwardSpeed&&Mathf.Abs(normal.z)>=t.smashMinimumFaceAlignment;
 }
 public static Vector3 ToTarget(Vector3 from,Vector3 target,float duration){return (target-from)/duration-Physics.gravity*(duration*.5f);}
 public static Vector3 PlayerReturn(TennisTuning t,Vector3 at,Vector3 incoming,Vector3 swing,Vector3 normal,bool smash){
  if(Vector3.Dot(normal,swing-incoming)<0)normal=-normal;
  var reflected=incoming+normal*(2*Mathf.Max(.3f,Vector3.Dot(swing-incoming,normal)));
  float side=Mathf.Clamp(swing.x*.18f+normal.x*.35f,-.55f,.55f);
  float duration=smash?.36f:Mathf.Clamp(.8f-swing.magnitude*.035f,.5f,.78f);
  var assisted=ToTarget(at,new Vector3(side,.79f,.85f),duration);
  return Vector3.ClampMagnitude(Vector3.Lerp(reflected,assisted,t.aimAssist),t.maximumBallSpeed);
 }
}
}
