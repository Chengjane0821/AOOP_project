using UnityEngine;
namespace ForgottenVillage {
// Pure tracking-space mapping. Head rotation is deliberately absent.
public sealed class PlaneBodyMapper {
 public Vector3 Neutral {get;private set;}
 public bool Calibrated {get;private set;}
 public Vector2 Command {get;private set;}
 Vector3 previous;float liftTime,crouchTime;bool liftCandidate,lift,crouch;
 public void Calibrate(Vector3 position){Neutral=previous=position;Calibrated=true;Command=Vector2.zero;liftTime=crouchTime=0;liftCandidate=lift=crouch=false;}
 public bool Sample(Vector3 position,float dt,PlaneDreamTuning t){
  if(!Calibrated||dt<=0||float.IsNaN(position.sqrMagnitude)||float.IsInfinity(position.sqrMagnitude))return false;
  if(Vector3.Distance(position,previous)>.5f){Calibrated=false;return false;}
  var delta=position-Neutral;float upwardSpeed=(position.y-previous.y)/dt;previous=position;
  if(delta.y<t.liftRelease){liftCandidate=lift=false;liftTime=0;}
  if(delta.y>=t.liftThreshold&&upwardSpeed>=t.minimumLiftVelocity)liftCandidate=true;
  if(liftCandidate&&delta.y>=t.liftThreshold){liftTime+=dt;lift=liftTime>=t.gestureDwell;}
  if(delta.y>-t.crouchRelease){crouch=false;crouchTime=0;}
  if(delta.y<=-t.crouchThreshold){crouchTime+=dt;crouch=crouchTime>=t.gestureDwell;}
  float x=Mathf.Sign(delta.x)*Mathf.Clamp01((Mathf.Abs(delta.x)-t.lateralDeadZone)/Mathf.Max(.01f,t.lateralFullRange-t.lateralDeadZone));
  var target=new Vector2(x,crouch?-1:lift?1:0);
  Command=Vector2.Lerp(Command,target,1-Mathf.Exp(-dt/Mathf.Max(.01f,t.inputSmoothing)));return true;
 }
}
}
