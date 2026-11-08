using UnityEngine;
namespace ForgottenVillage {
[RequireComponent(typeof(Rigidbody),typeof(SphereCollider))]
public class TennisBall : MonoBehaviour {
 public ElderTennis match; public Rigidbody Body {get;private set;}
 public const float Radius=.02f; Vector3 previous; bool swept; float lastHit=-10;
 void Awake(){Body=GetComponent<Rigidbody>();Body.mass=.0027f;Body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;Body.interpolation=RigidbodyInterpolation.Interpolate;Body.maxAngularVelocity=80;Body.solverIterations=12;Body.solverVelocityIterations=8;previous=transform.position;}
 public void Park(Vector3 at){Body.isKinematic=true;Body.position=at;previous=at;swept=false;var trail=GetComponent<TrailRenderer>();if(trail)trail.Clear();}
 public void Serve(Vector3 at,Vector3 velocity){Park(at);Body.isKinematic=false;Body.linearVelocity=velocity;Body.angularVelocity=Vector3.zero;lastHit=Time.fixedTime-.2f;}
 void FixedUpdate(){if(!match||!match.RallyActive||Body.isKinematic)return;
  Body.linearVelocity=Vector3.ClampMagnitude(Body.linearVelocity,match.tuning.maximumBallSpeed);
  CheckPaddle(match.playerPaddle);CheckPaddle(match.elderPaddle);
  previous=Body.position;swept=true;
  var local=match.transform.InverseTransformPoint(Body.position);
  if(local.y<.12f||local.y>4||Mathf.Abs(local.x)>2.7f||Mathf.Abs(local.z)>3.3f)match.BallOut();
 }
 // Relative sweep catches a 40 mm ball crossing a moving paddle between physics steps.
 // The same guarded contact path is used by trigger callbacks and the sweep.
 void CheckPaddle(TennisPaddle p){if(!p||!p.gameObject.activeInHierarchy||!p.TrackingValid)return;
  var start=p.transform.InverseTransformPoint(swept?previous:Body.position);
  start+=p.transform.InverseTransformVector(p.velocity*Time.fixedDeltaTime);
  var end=p.transform.InverseTransformPoint(Body.position+Body.linearVelocity*Time.fixedDeltaTime);
  var box=p.GetComponent<BoxCollider>();var bounds=new Bounds(box.center,box.size+Vector3.one*Radius*2);
  var delta=end-start;bool inside=bounds.Contains(start)||bounds.Contains(end);
  if(inside||(delta.sqrMagnitude>.000001f&&bounds.IntersectRay(new Ray(start,delta.normalized),out float distance)&&distance<=delta.magnitude))TryHit(p);
 }
 void OnTriggerEnter(Collider other){var p=other.GetComponent<TennisPaddle>();if(p)TryHit(p);}
 void TryHit(TennisPaddle p){if(Time.fixedTime-lastHit<match.tuning.contactCooldown||!match.RallyActive||!p.TrackingValid)return;
  if(match.PaddleContact(p,Body.position,Body.linearVelocity)){lastHit=Time.fixedTime;previous=Body.position;}
 }
 void OnCollisionEnter(Collision collision){if(!match||!match.RallyActive)return;var surface=collision.collider.GetComponent<TennisSurface>();
  if(surface&&surface.kind==TennisSurface.Kind.Table){if(collision.contactCount>0&&collision.GetContact(0).normal.y>.5f&&match.transform.InverseTransformPoint(Body.position).y>=.75f)match.TableBounce(Body.position);else match.BallOut();}
  else if(surface&&surface.kind==TennisSurface.Kind.Net)match.NetFault();
  else match.BallOut();
 }
 public void Strike(Vector3 outgoing){Body.AddForce((outgoing-Body.linearVelocity)*Body.mass,ForceMode.Impulse);}
}
}
