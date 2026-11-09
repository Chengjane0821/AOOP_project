using UnityEngine;
namespace ForgottenVillage {
[RequireComponent(typeof(Rigidbody),typeof(BoxCollider))]
public class PlaneAircraft : MonoBehaviour {
 public PlaneDreamTuning tuning;public Transform visual;public PlaneDreamRun run;
 public Vector3 Position {get;private set;} public Vector3 Previous {get;private set;}
 public Vector3 HalfExtents=>GetComponent<BoxCollider>().size*.5f;
 public Vector3 Velocity {get;private set;} Rigidbody body;
 void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;}
 public void ResetFlight(){Position=Previous=new Vector3(0,tuning.normalHeight,0);Velocity=Vector3.zero;body.position=transform.parent.TransformPoint(Position);if(visual)visual.localRotation=Quaternion.identity;}
 public void Step(Vector2 input,float dt){
  Previous=Position;input.x=Mathf.Clamp(input.x,-1,1);input.y=Mathf.Clamp(input.y,-1,1);
  var target=new Vector3(input.x*tuning.halfWidth,tuning.normalHeight+input.y*tuning.verticalRange,0);
  var desired=Vector3.ClampMagnitude((target-Position)/.18f,tuning.movementSpeed);
  Velocity=Vector3.MoveTowards(Velocity,desired,tuning.acceleration*dt);
  var next=Position+Velocity*dt;next.x=Mathf.Clamp(next.x,-tuning.halfWidth,tuning.halfWidth);next.y=Mathf.Clamp(next.y,tuning.normalHeight-tuning.verticalRange,tuning.normalHeight+tuning.verticalRange);next.z=0;Position=next;
  body.MovePosition(transform.parent.TransformPoint(Position));
  if(visual)visual.localRotation=Quaternion.Slerp(visual.localRotation,Quaternion.Euler(-Velocity.y/tuning.movementSpeed*tuning.pitchAngle,0,-Velocity.x/tuning.movementSpeed*tuning.bankAngle),1-Mathf.Exp(-dt*8));
 }
 void OnTriggerEnter(Collider other){if(!run||!other.GetComponent<PlaneObstacleBlock>())return;var group=other.GetComponentInParent<PlaneObstacleGroup>();if(group&&group.gameObject.activeSelf&&group.Hits(this))run.Crash();}
}
}
