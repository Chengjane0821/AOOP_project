using UnityEngine;
namespace ForgottenVillage {
public class TennisOpponent : MonoBehaviour {
 public ElderTennis match;public Transform resident; Vector3 target;float reactAt;int observedHits=-1;
 void FixedUpdate(){if(!match||!match.Running)return;var paddle=match.elderPaddle;var frame=match.transform;
  if(Time.fixedTime>=reactAt){reactAt=Time.fixedTime+match.tuning.elderReactionTime;
   var ball=frame.InverseTransformPoint(match.ball.Body.position);var v=frame.InverseTransformDirection(match.ball.Body.linearVelocity);
   target=new Vector3(0,1.05f,1.48f);
   if(match.RallyActive&&v.z>0){float time=Mathf.Clamp((1.48f-ball.z)/Mathf.Max(.1f,v.z),0,.6f);
    target.x=Mathf.Clamp(ball.x+v.x*time,-.85f,.85f);target.y=Mathf.Clamp(ball.y+v.y*time+Physics.gravity.y*time*time*.5f,.82f,1.8f);
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
 Vector3 error;
}
}
