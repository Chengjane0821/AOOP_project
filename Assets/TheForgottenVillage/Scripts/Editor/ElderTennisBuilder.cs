using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;
namespace ForgottenVillage.Editor {
public static class ElderTennisBuilder {
 const string Root="Assets/TheForgottenVillage/";
 public static void Audit(){var scene=EditorSceneManager.OpenScene(Root+"Scenes/EldersHouse.unity");File.WriteAllLines("Verification/elder-room-bounds.txt",scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Select(r=>r.name+" | "+r.bounds.center+" | "+r.bounds.size));}
 [MenuItem("Forgotten Village/Install Elder table tennis")]
 public static void Build(){
  var scene=EditorSceneManager.OpenScene(Root+"Scenes/EldersHouse.unity");var house=UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();
  if(!house||house.houseIndex!=0)throw new Exception("Expected Elder house scene");
  var old=house.GetComponentInChildren<ElderTennis>(true);if(old)throw new Exception("Table tennis already installed; edit the saved scene instead of reinstalling.");
  // Remove this house's obsolete activity and signage; other scene assets are untouched.
  foreach(var tr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray()){
   if(!tr)continue;string n=tr.name;
   if(n=="Engineering workbench"||n=="Workbench leg"||n=="ENGINEERING STATION"||n=="Engineering field card"||n=="Brass field card edge"||n.Contains("LOADED DIVIDER")||n.Contains("Control_Lever"))UnityEngine.Object.DestroyImmediate(tr.gameObject);
  }
  var activity=new GameObject("Elder table tennis");activity.transform.SetParent(house.transform,false);activity.transform.position=Vector3.zero;
  var match=activity.AddComponent<ElderTennis>();
  string tuningPath=Root+"TableTennisTuning.asset";var tuning=AssetDatabase.LoadAssetAtPath<TennisTuning>(tuningPath);if(!tuning){tuning=ScriptableObject.CreateInstance<TennisTuning>();AssetDatabase.CreateAsset(tuning,tuningPath);}match.tuning=tuning;
  var resident=new GameObject("Elder opponent");resident.transform.SetParent(activity.transform,false);resident.transform.localPosition=new Vector3(0,0,1.95f);
  var residentOrigin=house.GetComponentsInChildren<Transform>().First(t=>t.name=="Resident interaction").position-Vector3.up*.8f;
  foreach(string name in new[]{"Resident interaction","Resident coat","Resident face","Resident hat","TALK"}){
   var tr=house.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);if(!tr)continue;var offset=tr.position-residentOrigin;tr.SetParent(resident.transform,false);tr.localPosition=name=="TALK"?new Vector3(0,1.95f,0):offset;
  }
  var photoGroup=new GameObject("Recovered memory photograph");photoGroup.transform.SetParent(house.transform,false);
  foreach(var tr in house.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Six villagers photograph"||t.name=="Villager silhouette"||t.name=="Scratched face").ToArray()){tr.SetParent(photoGroup.transform,true);tr.position+=new Vector3(3.1f,0,-.3f);}
  match.photograph=photoGroup;photoGroup.SetActive(false);
  match.playerStanding=Point("Player standing position",new Vector3(0,.1f,-2.05f),activity.transform);
  match.npcStanding=Point("Elder standing position",new Vector3(0,0,1.95f),activity.transform);
  house.arrival.position=new Vector3(-1.65f,.15f,-2.25f);
  var table=Visual("PingPongTable",activity.transform,new Vector3(1.525f,.9125f,2.74f));table.name="Table tennis table and net";table.transform.localPosition=new Vector3(0,.45625f,0);
  var rubber=new PhysicsMaterial("Tennis bounce"){bounciness=.88f,dynamicFriction=.12f,staticFriction=.12f,bounceCombine=PhysicsMaterialCombine.Maximum};var storedRubber=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root+"Materials/TennisBounce.physicMaterial");if(storedRubber){UnityEngine.Object.DestroyImmediate(rubber);rubber=storedRubber;}else AssetDatabase.CreateAsset(rubber,Root+"Materials/TennisBounce.physicMaterial");
  var top=Box("Table collision surface",activity.transform,new Vector3(0,.735f,0),new Vector3(1.525f,.05f,2.74f),false);top.AddComponent<TennisSurface>().kind=TennisSurface.Kind.Table;top.GetComponent<Collider>().sharedMaterial=rubber;
  var net=Box("Net collision",activity.transform,new Vector3(0,.836f,0),new Vector3(1.7f,.1525f,.025f),false);net.AddComponent<TennisSurface>().kind=TennisSurface.Kind.Net;
  var ballRoot=new GameObject("Physics ball");ballRoot.transform.SetParent(activity.transform,false);ballRoot.transform.localPosition=new Vector3(0,1.2f,0);ballRoot.AddComponent<Rigidbody>();ballRoot.AddComponent<SphereCollider>().radius=.02f;ballRoot.GetComponent<Collider>().sharedMaterial=rubber;match.ball=ballRoot.AddComponent<TennisBall>();match.ball.match=match;
  Visual("Ball",ballRoot.transform,Vector3.one*.04f);
  var trail=ballRoot.AddComponent<TrailRenderer>();trail.time=.12f;trail.startWidth=.018f;trail.endWidth=.002f;trail.minVertexDistance=.015f;trail.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Amber.mat");trail.startColor=Color.white;trail.endColor=new Color(1,1,1,0);
  match.playerPaddle=Paddle("Player paddle",true,match,new Vector3(0,1.1f,-1.45f));match.playerPaddle.gameObject.SetActive(false);
  match.elderPaddle=Paddle("Elder paddle",false,match,new Vector3(0,1.1f,1.48f));
  var ai=activity.AddComponent<TennisOpponent>();ai.match=match;ai.resident=resident.transform;
  match.feel=activity.AddComponent<TennisFeedback>();match.feel.match=match;
  match.scoreboard=Text("Scoreboard",activity.transform,new Vector3(0,2.25f,2.5f),new Vector2(2.8f,1.15f),.17f);
  match.instructions=Text("Match instructions",activity.transform,new Vector3(-2,1.6f,.9f),new Vector2(1.2f,1.4f),.105f);
  match.instructions.text="TABLE TENNIS\nFirst to 5 points wins a game.\nWin 2 games to help the Elder remember.\nReturn after one bounce.\nSwing your paddle to hit.\nHold Grip on a high, descending ball for a smash.\n2 smashes or the hit target tire the Elder.\nDesktop: mouse + F swing; Space = Grip.\nTalk to Elder to restart.";
  match.feedback=Text("Rally result",activity.transform,new Vector3(0,1.7f,1.8f),new Vector2(2.5f,.4f),.22f);
  Box("Player floor marker",activity.transform,new Vector3(0,.005f,-2.05f),new Vector3(.7f,.008f,.5f),true);
  // Keep the original room and its entrance. Expand only structural meshes and relocate furniture.
  ExpandRoom(house,activity.transform);
  PrefabUtility.SaveAsPrefabAsset(house.gameObject,Root+"Prefabs/EldersHouseContent.prefab");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  File.WriteAllText("Verification/elder-tennis-build.txt","PASS: Elder scene and content prefab saved with physical table, net, ball, paddles, opponent, UI and evidence.\n");
 }
 static void ExpandRoom(HouseSceneRoot house,Transform activity){
  // Audit measured an 11.1 x 7.5 m room: no enlargement or furniture scaling is needed.
  // Relocate complete furniture instances, preserving mesh/collider relationships.
  foreach(var root in house.copiedRoots){if(!root||!root.activeSelf)continue;
   foreach(var tr in root.GetComponentsInChildren<Transform>().ToArray()){
    if(tr.name=="Couch_Rustic_Brown")tr.position+=Vector3.left*1.5f;
    if(tr.name=="Table_Coffee_Traditional"||tr.name=="TV_FlatWithStand"||tr.name=="Rug_Rect_Gray")tr.position+=Vector3.left*1.8f;
   }
  }
  foreach(var tr in house.GetComponentsInChildren<Transform>().ToArray()){
   if(tr.name=="Damaged archive volume"||tr.name=="Notebook_Brown")tr.position+=Vector3.left*1.8f;
   if(tr.name=="Elder's House")tr.position=new Vector3(0,2.95f,3.1f);
  }
  var stand=Box("Photograph stand",house.transform,new Vector3(2.6f,.75f,1.4f),new Vector3(.12f,1.5f,.12f),true);
  stand.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Warm timber.mat");
  var light=new GameObject("Warm table lamp",typeof(Light));light.transform.SetParent(activity,false);light.transform.localPosition=new Vector3(0,2.8f,.3f);var l=light.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.84f,.65f);l.intensity=2;l.range=6;
 }
 static TennisPaddle Paddle(string name,bool player,ElderTennis match,Vector3 p){var o=new GameObject(name);o.transform.SetParent(match.transform,false);o.transform.localPosition=p;o.AddComponent<Rigidbody>();var c=o.AddComponent<BoxCollider>();c.size=new Vector3(.17f,.19f,.022f);c.isTrigger=true;var paddle=o.AddComponent<TennisPaddle>();paddle.player=player;paddle.match=match;var v=Visual("PingPongBat",o.transform,new Vector3(.17f,.27f,.022f));v.transform.localPosition=new Vector3(0,-.04f,0);return paddle;}
 static GameObject Visual(string name,Transform parent,Vector3 size){
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/LowPolyTableTennis/Prefabs/"+name+".prefab");if(!asset)throw new Exception("Missing table tennis asset: "+name);
  var wrapper=new GameObject(name+" visual");wrapper.transform.SetParent(parent,false);
  var o=(GameObject)PrefabUtility.InstantiatePrefab(asset);PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);o.transform.SetParent(wrapper.transform,false);o.transform.localPosition=Vector3.zero;
  if(name=="PingPongBat"){
   o.transform.localRotation=Quaternion.identity;var meshBounds=BoundsOf(o);var dimensions=meshBounds.size;
   if(dimensions.x<dimensions.y&&dimensions.x<dimensions.z)o.transform.localRotation=Quaternion.Euler(0,90,0);
   else if(dimensions.y<dimensions.x&&dimensions.y<dimensions.z)o.transform.localRotation=Quaternion.Euler(90,0,0);
   dimensions=BoundsOf(o).size;if(dimensions.x>dimensions.y)o.transform.localRotation=Quaternion.Euler(0,0,90)*o.transform.localRotation;
  }
  if(name=="PingPongTable")o.transform.localRotation=Quaternion.Euler(0,90,0)*o.transform.localRotation;
  foreach(var c in o.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);foreach(var rb in o.GetComponentsInChildren<Rigidbody>())UnityEngine.Object.DestroyImmediate(rb);
  var rr=o.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);
  wrapper.transform.localScale=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
  b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);o.transform.position+=wrapper.transform.position-b.center;
  return wrapper;
 }
 static Bounds BoundsOf(GameObject o){var rr=o.GetComponentsInChildren<Renderer>();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
 public static void Polish(){
  var scene=EditorSceneManager.OpenScene(Root+"Scenes/EldersHouse.unity");var match=UnityEngine.Object.FindFirstObjectByType<ElderTennis>();
  foreach(var paddle in new[]{match.playerPaddle,match.elderPaddle}){
   foreach(Transform child in paddle.transform)UnityEngine.Object.DestroyImmediate(child.gameObject);
   var v=Visual("PingPongBat",paddle.transform,new Vector3(.17f,.27f,.022f));v.transform.localPosition=new Vector3(0,-.04f,0);
  }
  // Readable surfaces behind world-space text; no UI collider blocks the ball or rays.
  foreach(var label in new[]{match.scoreboard,match.instructions}){
   string name=label.name+" backing";var existing=match.transform.Find(name);if(existing)UnityEngine.Object.DestroyImmediate(existing.gameObject);
   var panel=Box(name,match.transform,label.transform.localPosition+Vector3.forward*.035f,new Vector3(label.rectTransform.sizeDelta.x+.12f,label.rectTransform.sizeDelta.y+.12f,.035f),true);
   panel.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Ink.mat");UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
  }
  match.instructions.fontSize=.8f;match.instructions.rectTransform.sizeDelta=new Vector2(1.4f,1.4f);
  var green=AssetDatabase.LoadAssetAtPath<Material>("Assets/LowPolyTableTennis/Materials/Table/TableGreen.mat");green.SetColor("_BaseColor",new Color(.07f,.28f,.22f));green.SetColor("_Color",new Color(.07f,.28f,.22f));EditorUtility.SetDirty(green);
  PrefabUtility.SaveAsPrefabAsset(match.GetComponentInParent<HouseSceneRoot>().gameObject,Root+"Prefabs/EldersHouseContent.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  var audit=new System.Text.StringBuilder();
  foreach(var filter in match.GetComponentsInChildren<MeshFilter>(true)){
   var renderer=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;if(!renderer||!mesh)continue;
   for(int sub=0;sub<Mathf.Min(mesh.subMeshCount,renderer.sharedMaterials.Length);sub++){
    var triangles=mesh.GetTriangles(sub);if(triangles.Length==0)continue;var vertices=mesh.vertices;var b=new Bounds(filter.transform.TransformPoint(vertices[triangles[0]]),Vector3.zero);foreach(int index in triangles)b.Encapsulate(filter.transform.TransformPoint(vertices[index]));
    audit.AppendLine(filter.name+" / "+renderer.sharedMaterials[sub].name+" center="+b.center+" size="+b.size);
   }
  }
  File.WriteAllText("Verification/elder-model-dimensions.txt",audit.ToString());
 }
 static GameObject Box(string n,Transform parent,Vector3 p,Vector3 size,bool visible){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().enabled=visible;return o;}
 static Transform Point(string n,Vector3 p,Transform parent){var o=new GameObject(n);o.transform.SetParent(parent,false);o.transform.localPosition=p;return o.transform;}
 static TextMeshPro Text(string n,Transform parent,Vector3 p,Vector2 size,float fontSize){var o=new GameObject(n);o.transform.SetParent(parent,false);o.transform.localPosition=p;var t=o.AddComponent<TextMeshPro>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");t.rectTransform.sizeDelta=size;t.fontSize=fontSize*10;t.alignment=TextAlignmentOptions.Center;t.color=new Color(1,.9f,.7f);return t;}
}
}
