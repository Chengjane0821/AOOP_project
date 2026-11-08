using UnityEngine;
namespace ForgottenVillage {
public class TennisOpponent : MonoBehaviour {
 public ElderTennis match;public Transform resident; Vector3 target;float reactAt;int observedHits=-1;
 void FixedUpdate(){if(!match||!match.Running)return;var paddle=match.elderPaddle;var frame=match.transform;
  if(Time.fixedTime>=reactAt){reactAt=Time.fixedTime+match.tuning.elderReactionTime;
   var ball=frame.InverseTransformPoint(match.ball.Body.position);var v=frame.InverseTransformDirection(match.ball.Body.linearVelocity);
   target=new Vector3(0,1.05f,1.48f);
   if(match.RallyActive&&v.z>0){float time=Mathf.Clamp((1.48f-ball.z)/Mathf.Max(.1f,v.z),0,.6f);
    target.x=Mathf.Clamp(ball.x+v.x*time,-.85f,.85f);target.y=Mathf.Clamp(PredictHeight(ball,v,time),.82f,1.8f);
   }
   // A small error is sampled once per incoming shot, never used for player smash validation.
   if(observedHits!=match.rules.gameHits){observedHits=match.rules.gameHits;error=Random.insideUnitSphere*match.tuning.elderError;}
   target+=error;
   if(match.rules.elderMustMiss){target.x=1.35f;target.y=.48f;}
  }
  var next=Vector3.MoveTowards(paddle.transform.position,frame.TransformPoint(target),match.tuning.elderSpeed*Time.fixedDeltaTime);
  paddle.MoveAI(next,frame.rotation);
  if(resident){var p=resident.localPosition;p.x=Mathf.MoveTowards(p.x,target.x,Time.fixedDeltaTime*1.1f);resident.localPosition=p;}
 }
 // Predict the table bounce before contact instead of chasing a ball through the tabletop.
 internal static float PredictHeight(Vector3 ball,Vector3 velocity,float time){
  float g=-Physics.gravity.y;float height=ball.y+velocity.y*time-.5f*g*time*time;
  float discriminant=velocity.y*velocity.y+2*g*(ball.y-.78f);
  if(discriminant>=0){float bounceTime=(velocity.y+Mathf.Sqrt(discriminant))/g;
   float z=ball.z+velocity.z*bounceTime;
   if(bounceTime>=0&&bounceTime<time&&Mathf.Abs(z)<=1.37f&&Mathf.Abs(ball.x+velocity.x*bounceTime)<=.7625f){
    float remaining=time-bounceTime;float up=-(velocity.y-g*bounceTime)*.88f;
    height=.78f+up*remaining-.5f*g*remaining*remaining;
   }
  }
  return height;
 }
 Vector3 error;
}
}
