using UnityEngine;
namespace ForgottenVillage {
public class PlaneObstacleGroup : MonoBehaviour {
 public Transform first,second;public PlanePattern Pattern {get;private set;}
 public float Z {get;private set;} public float PreviousZ {get;private set;}
 Rigidbody body;
 void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;}
 public void Place(PlanePattern pattern,float z){Pattern=pattern;Z=PreviousZ=z;Apply(first,pattern.first);second.gameObject.SetActive(pattern.hasSecond);if(pattern.hasSecond)Apply(second,pattern.second);body.position=transform.parent.TransformPoint(Vector3.forward*z);gameObject.SetActive(true);}
 static void Apply(Transform block,Bounds bounds){block.localPosition=bounds.center;block.localScale=bounds.size;}
 public void Step(float distance){PreviousZ=Z;Z-=distance;body.MovePosition(transform.parent.TransformPoint(Vector3.forward*Z));}
 public bool Hits(PlaneAircraft plane)=>PlanePatterns.SweptHit(plane.Previous,plane.Position,plane.HalfExtents,Pattern.first,PreviousZ,Z)||(Pattern.hasSecond&&PlanePatterns.SweptHit(plane.Previous,plane.Position,plane.HalfExtents,Pattern.second,PreviousZ,Z));
}
}
