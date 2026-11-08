using System.Collections.Generic;
using UnityEngine;
namespace ForgottenVillage {
public class PlaneObstaclePool : MonoBehaviour {
 public PlaneDreamTuning tuning;public PlaneObstacleGroup prefab;
 readonly List<PlaneObstacleGroup> groups=new List<PlaneObstacleGroup>();System.Random random;float sinceSpawn;
 public int Spawned {get;private set;} public int ActiveCount {get{int n=0;foreach(var g in groups)if(g.gameObject.activeSelf)n++;return n;}}
 public IEnumerable<PlaneObstacleGroup> Groups=>groups;
 public void Initialize(){if(groups.Count>0)return;for(int i=0;i<Mathf.Max(4,tuning.poolSize);i++){var g=Instantiate(prefab,transform);g.gameObject.SetActive(false);groups.Add(g);}}
 public void ResetPool(int seed){Initialize();foreach(var g in groups)g.gameObject.SetActive(false);random=new System.Random(seed);sinceSpawn=100;Spawned=0;}
 public bool Step(float dt,float progress,PlaneAircraft plane){
  float distance=tuning.WorldSpeed(progress)*dt;
  foreach(var g in groups){if(!g.gameObject.activeSelf)continue;g.Step(distance);if(g.Hits(plane))return true;if(g.Z< -2)g.gameObject.SetActive(false);}
  sinceSpawn+=dt;if(sinceSpawn<tuning.SpawnInterval(progress))return false;
  // Every group shares one world speed, and spatial spacing is sized for the maximum speed.
  foreach(var g in groups)if(g.gameObject.activeSelf&&g.Z>tuning.SafeSpawnDistance-tuning.SafeSpacing)return false;
  foreach(var g in groups){if(g.gameObject.activeSelf)continue;var p=PlanePatterns.Create(random.Next(PlanePatterns.Count),(float)(random.NextDouble()*1.3-.65),tuning);g.Place(p,tuning.SafeSpawnDistance);Spawned++;sinceSpawn=0;break;}
  return false;
 }
 public PlaneObstacleGroup Next(){PlaneObstacleGroup next=null;foreach(var g in groups)if(g.gameObject.activeSelf&&g.Z>-.6f&&(!next||g.Z<next.Z))next=g;return next;}
 public void Clear(){foreach(var g in groups)if(g)g.gameObject.SetActive(false);}
}
}
