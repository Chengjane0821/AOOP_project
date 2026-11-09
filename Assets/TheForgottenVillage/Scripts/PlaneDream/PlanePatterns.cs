using UnityEngine;
namespace ForgottenVillage {
public struct PlanePattern {
 public Bounds first,second;public bool hasSecond;public Vector2 safeTarget;public string cue;
 public bool SafeFor(Vector3 point,Vector3 planeHalf){var a=first;a.Expand(planeHalf*2);var b=second;b.Expand(planeHalf*2);return !a.Contains(point)&&(!hasSecond||!b.Contains(point));}
}
public static class PlanePatterns {
 public const int Count=6;
 public static PlanePattern Create(int kind,float lane,PlaneDreamTuning tuning){
  float w=tuning.halfWidth+.6f,h=tuning.normalHeight+tuning.verticalRange+.8f;
  var p=new PlanePattern();float neutral=tuning.normalHeight;
  switch(kind){
   case 0:p.first=new Bounds(new Vector3(0,(neutral+.15f)*.5f,0),new Vector3(w*2,neutral+.15f,.6f));p.safeTarget=new Vector2(0,neutral+tuning.verticalRange);p.cue="RISE";break;
   case 1:float bottom=neutral-.15f;p.first=new Bounds(new Vector3(0,(h+bottom)*.5f,0),new Vector3(w*2,h-bottom,.6f));p.safeTarget=new Vector2(0,neutral-tuning.verticalRange);p.cue="LOWER";break;
   case 2:p.first=new Bounds(new Vector3((-w+.3f)*.5f,h*.5f,0),new Vector3(w+.3f,h,.6f));p.safeTarget=new Vector2(tuning.halfWidth*.75f,neutral);p.cue="MOVE RIGHT";break;
   case 3:p.first=new Bounds(new Vector3((w-.3f)*.5f,h*.5f,0),new Vector3(w+.3f,h,.6f));p.safeTarget=new Vector2(-tuning.halfWidth*.75f,neutral);p.cue="MOVE LEFT";break;
   case 4:p.first=new Bounds(new Vector3(0,h*.5f,0),new Vector3(.9f,h,.6f));p.safeTarget=new Vector2(tuning.halfWidth*.75f,neutral);p.cue="PASS EITHER SIDE";break;
   default:float center=Mathf.Clamp(lane,-.65f,.65f),gap=.8f;
    p.first=new Bounds(new Vector3((-w+center-gap)*.5f,h*.5f,0),new Vector3(w+center-gap,h,.6f));
    p.second=new Bounds(new Vector3((w+center+gap)*.5f,h*.5f,0),new Vector3(w-center-gap,h,.6f));p.hasSecond=true;p.safeTarget=new Vector2(center,neutral);p.cue="THROUGH THE GAP";break;
  }
  return p;
 }
 // Relative swept AABB: collision tests account for both plane and barrier movement.
 public static bool SweptHit(Vector3 planeFrom,Vector3 planeTo,Vector3 half,Bounds obstacle,float fromZ,float toZ){
  var expanded=obstacle;expanded.Expand(half*2);var a=planeFrom-Vector3.forward*fromZ;var b=planeTo-Vector3.forward*toZ;
  if(expanded.Contains(a)||expanded.Contains(b))return true;var delta=b-a;
  return delta.sqrMagnitude>1e-10f&&expanded.IntersectRay(new Ray(a,delta.normalized),out float distance)&&distance<=delta.magnitude;
 }
}
}
