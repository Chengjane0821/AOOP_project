using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage.Editor {
public static class PlaneDreamBuilder {
 const string Root="Assets/TheForgottenVillage/";
 [MenuItem("Forgotten Village/Install Alchemist plane dream")]
 public static void Build(){
  var scene=EditorSceneManager.OpenScene(Root+"Scenes/AlchemistsHouse.unity");var house=UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();if(!house||house.houseIndex!=1)throw new Exception("Expected Alchemist scene");if(house.GetComponent<AlchemistDreamHouse>())throw new Exception("Dream already installed; edit the saved scene.");
  foreach(var tr in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray()){
   if(!tr)continue;var n=tr.name;
   if(n=="Engineering workbench"||n=="Workbench leg"||n=="ENGINEERING STATION"||n=="Oscilloscope traces"||n=="Engineering field card"||n=="Brass field card edge"||n.Contains("MEMORY SIGNAL")||n.Contains("RC FILTER")||n.Contains("LOW-PASS")||n.Contains("FILTER THE MEMORY"))UnityEngine.Object.DestroyImmediate(tr.gameObject);
  }
  var owner=house.gameObject.AddComponent<AlchemistDreamHouse>();owner.returnPoint=house.arrival;
  string path=Root+"PlaneDreamTuning.asset";var tuning=AssetDatabase.LoadAssetAtPath<PlaneDreamTuning>(path);if(!tuning){tuning=ScriptableObject.CreateInstance<PlaneDreamTuning>();AssetDatabase.CreateAsset(tuning,path);}
  var arena=new GameObject("Alchemist dream flight area");arena.transform.SetParent(house.transform,false);arena.transform.localPosition=new Vector3(0,0,250);arena.SetActive(false);var run=arena.AddComponent<PlaneDreamRun>();owner.dream=run;run.house=owner;run.tuning=tuning;
  run.input=arena.AddComponent<PlaneBodyInput>();run.input.tuning=tuning;
  run.viewingStation=Point("Fixed VR viewing station",arena.transform,new Vector3(0,.05f,-6));
  var skyMat=MaterialAsset("Dream sky",new Color(.16f,.27f,.46f),true);skyMat.SetFloat("_Cull",1);EditorUtility.SetDirty(skyMat);
  var sky=GameObject.CreatePrimitive(PrimitiveType.Sphere);sky.name="Still dream sky";sky.transform.SetParent(arena.transform,false);sky.transform.localPosition=new Vector3(0,0,10);sky.transform.localScale=Vector3.one*150;sky.GetComponent<Renderer>().sharedMaterial=skyMat;UnityEngine.Object.DestroyImmediate(sky.GetComponent<Collider>());
  var platformMat=MaterialAsset("Dream viewing platform",new Color(.15f,.20f,.28f),false);Box("Stable viewing platform",arena.transform,new Vector3(0,-.1f,-6),new Vector3(5,.2f,3),platformMat);
  for(int side=-1;side<=1;side+=2)Box("Stationary comfort rail",arena.transform,new Vector3(side*2.45f,.35f,-6),new Vector3(.08f,.7f,3),platformMat);
  var lamp=new GameObject("Dream sunlight",typeof(Light));lamp.transform.SetParent(arena.transform,false);lamp.transform.localRotation=Quaternion.Euler(35,-30,0);var light=lamp.GetComponent<Light>();light.type=LightType.Directional;light.color=new Color(.8f,.88f,1);light.intensity=1.1f;
  var plane=new GameObject("Dream airplane",typeof(Rigidbody),typeof(BoxCollider));plane.transform.SetParent(arena.transform,false);plane.transform.localPosition=new Vector3(0,tuning.normalHeight,0);plane.GetComponent<BoxCollider>().size=new Vector3(.72f,.24f,.72f);
  run.aircraft=plane.AddComponent<PlaneAircraft>();run.aircraft.tuning=tuning;run.aircraft.run=run;run.aircraft.visual=Visual("Assets/PlaneRunner/Prefabs/Gameplay/player-1.prefab",plane.transform,new Vector3(.8f,.25f,.9f)).transform;
  var pool=new GameObject("Pooled dream barriers");pool.transform.SetParent(arena.transform,false);run.obstacles=pool.AddComponent<PlaneObstaclePool>();run.obstacles.tuning=tuning;run.obstacles.prefab=CreateObstaclePrefab();
  run.scenery=new Transform[3];for(int i=0;i<3;i++){var land=Visual("Assets/PlaneRunner/Prefabs/Gameplay/valley-part-"+(i%2+1)+".prefab",arena.transform,new Vector3(18,3,24));land.name="Scrolling valley "+i;land.transform.localPosition=new Vector3(0,-3,i*24);run.scenery[i]=land.transform;}
  var board=Canvas("Flight HUD",arena.transform,new Vector3(0,3.65f,1),new Vector2(900,230),.0035f);run.timerLabel=Text(board.transform,"Countdown",new Vector2(860,80),new Vector2(0,65),44);run.statusLabel=Text(board.transform,"Survival status",new Vector2(860,70),new Vector2(0,-10),31);
  var track=VillageUI.Rect("Progress",board.transform,new Vector2(800,16),new Vector2(0,-85));var bar=track.gameObject.AddComponent<Image>();bar.color=new Color(.35f,.9f,.8f);bar.type=Image.Type.Filled;bar.fillMethod=Image.FillMethod.Horizontal;bar.fillOrigin=0;bar.fillAmount=0;bar.raycastTarget=false;run.progressBar=bar;
  var instructions=Canvas("Body movement instructions",arena.transform,new Vector3(-3.35f,2.05f,.5f),new Vector2(500,500),.0035f);run.instructionLabel=Text(instructions.transform,"Controls",new Vector2(460,450),Vector2.zero,33);
  var pedestal=Box("Dream record pedestal",house.transform,new Vector3(-1.5f,.5f,.8f),new Vector3(.65f,1,.6f),platformMat);
  var evidence=Box("Recovered dream experiment record",house.transform,new Vector3(-1.5f,1.05f,.8f),new Vector3(.48f,.08f,.32f),AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Amber.mat"));owner.evidence=evidence;evidence.AddComponent<XRSimpleInteractable>();var interact=evidence.AddComponent<VillageInteractable>();interact.prompt="Inspect recovered dream record";UnityEventTools.AddPersistentListener(interact.action,owner.InspectEvidence);evidence.SetActive(false);
  owner.roomObjects=house.copiedRoots.Concat(house.transform.Cast<Transform>().Where(t=>t!=arena.transform).Select(t=>t.gameObject)).Distinct().ToArray();
  RefineScene(house);
  PrefabUtility.SaveAsPrefabAsset(house.gameObject,Root+"Prefabs/AlchemistsHouseContent.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("Verification/plane-dream-build.txt","PASS: Alchemist scene and prefab saved; RC station removed; isolated fixed-view dream area uses PlaneRunner aircraft, rocks and valleys; no added camera or XR Origin.\n");
 }
 static PlaneObstacleGroup CreateObstaclePrefab(){
  var root=new GameObject("Dream obstacle group",typeof(Rigidbody));var group=root.AddComponent<PlaneObstacleGroup>();
  for(int i=0;i<2;i++){var b=new GameObject("Barrier "+i,typeof(BoxCollider),typeof(PlaneObstacleBlock));b.transform.SetParent(root.transform,false);b.GetComponent<BoxCollider>().isTrigger=true;Visual("Assets/PlaneRunner/Prefabs/Gameplay/rock-"+(i+1)+".prefab",b.transform,Vector3.one);if(i==0)group.first=b.transform;else group.second=b.transform;}
  string path=Root+"Prefabs/DreamObstacleGroup.prefab";var asset=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return asset.GetComponent<PlaneObstacleGroup>();
 }
 public static GameObject Visual(string path,Transform parent,Vector3 size){
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new Exception("Missing PlaneRunner prefab: "+path);var wrapper=new GameObject(Path.GetFileNameWithoutExtension(path)+" visual");wrapper.transform.SetParent(parent,false);
  var o=(GameObject)PrefabUtility.InstantiatePrefab(source);PrefabUtility.UnpackPrefabInstance(o,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);o.transform.SetParent(wrapper.transform,false);o.transform.localPosition=Vector3.zero;
  // The package singleton scripts, legacy keyboard polling and follow cameras are not instantiated in RPG gameplay.
  foreach(var script in o.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
  foreach(var c in o.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in o.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(b);
  foreach(var p in o.GetComponentsInChildren<ParticleSystem>(true))UnityEngine.Object.DestroyImmediate(p.gameObject);
  foreach(var renderer in o.GetComponentsInChildren<MeshRenderer>(true))ConvertMaterials(renderer);
  var renderers=o.GetComponentsInChildren<MeshRenderer>(true);if(renderers.Length==0)throw new Exception("No mesh in "+path);var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);wrapper.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),size.z/Mathf.Max(.001f,bounds.size.z));bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);o.transform.position+=wrapper.transform.position-bounds.center;return wrapper;
 }
 public static void Refine(){var scene=EditorSceneManager.OpenScene(Root+"Scenes/AlchemistsHouse.unity");var house=UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();RefineScene(house);PrefabUtility.SaveAsPrefabAsset(house.gameObject,Root+"Prefabs/AlchemistsHouseContent.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();}
 static void RefineScene(HouseSceneRoot house){
  foreach(var root in house.gameObject.scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true).ToArray())if(text.text.Contains("MEMORY SIGNAL FILTER")||text.text.Contains("100 Hz")||text.text.Contains("5 kHz"))UnityEngine.Object.DestroyImmediate(text.gameObject);
  foreach(var tr in house.GetComponentsInChildren<Transform>(true))if(tr.name=="Filter reagent bottle")tr.name="Alchemical dream vessel";
  var run=house.GetComponent<AlchemistDreamHouse>().dream;foreach(var renderer in run.GetComponentsInChildren<MeshRenderer>(true))ConvertMaterials(renderer);
  var bar=run.progressBar;bar.type=Image.Type.Simple;bar.rectTransform.pivot=new Vector2(0,.5f);bar.rectTransform.anchoredPosition=new Vector2(-400,-85);bar.rectTransform.localScale=new Vector3(0,1,1);
  string path=Root+"Prefabs/DreamObstacleGroup.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
  foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))ConvertMaterials(renderer);
  var group=prefab.GetComponent<PlaneObstacleGroup>();var accent=MaterialAsset("Dream barrier edges",new Color(1,.48f,.26f),true);
  foreach(var block in new[]{group.first,group.second}){if(block.Find("Collision boundary"))continue;var outline=new GameObject("Collision boundary");outline.transform.SetParent(block,false);
   for(int axis=0;axis<3;axis++)for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2){var center=Vector3.zero;center[(axis+1)%3]=a*.5f;center[(axis+2)%3]=b*.5f;var size=Vector3.one*.018f;size[axis]=1;var edge=Box("Barrier edge",outline.transform,center,size,accent);UnityEngine.Object.DestroyImmediate(edge.GetComponent<Collider>());}
  }
  PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
 }
 static void ConvertMaterials(MeshRenderer renderer){var materials=renderer.sharedMaterials;bool changed=false;for(int i=0;i<materials.Length;i++){
  var source=materials[i];if(!source||!AssetDatabase.GetAssetPath(source).StartsWith("Assets/PlaneRunner/"))continue;
  string path=Root+"Materials/PlaneRunner "+source.name+".mat";var target=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!target){target=new Material(Shader.Find("Universal Render Pipeline/Lit"));target.SetTexture("_BaseMap",source.HasProperty("_MainTex")?source.GetTexture("_MainTex"):null);target.SetColor("_BaseColor",source.HasProperty("_Color")?source.GetColor("_Color"):Color.white);target.SetFloat("_Smoothness",.15f);AssetDatabase.CreateAsset(target,path);}materials[i]=target;changed=true;
  }if(changed)renderer.sharedMaterials=materials;
 }
 static Material MaterialAsset(string name,Color color,bool unlit){string path=Root+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_BaseColor",color);return m;}
 static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;return o;}
 static Transform Point(string name,Transform parent,Vector3 p){var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=p;return o.transform;}
 static Canvas Canvas(string name,Transform parent,Vector3 p,Vector2 size,float scale){var o=new GameObject(name,typeof(RectTransform),typeof(Canvas));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=Vector3.one*scale;var c=o.GetComponent<Canvas>();c.renderMode=RenderMode.WorldSpace;((RectTransform)o.transform).sizeDelta=size;var bg=o.AddComponent<Image>();bg.color=new Color(.04f,.08f,.15f,.92f);bg.raycastTarget=false;return c;}
 static TextMeshProUGUI Text(Transform parent,string name,Vector2 size,Vector2 p,float fontSize){var rect=VillageUI.Rect(name,parent,size,p);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");label.fontSize=fontSize;label.color=new Color(.9f,.97f,1);label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;}
}
}
