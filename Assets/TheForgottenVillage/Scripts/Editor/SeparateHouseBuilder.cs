using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using TMPro;
namespace ForgottenVillage.Editor {
public static class SeparateHouseBuilder {
 const string Root="Assets/TheForgottenVillage/";
 const string Source="Assets/Scenes/My VR room.unity";
 static TMP_FontAsset font;static Material dark,teal,gold;static List<string> audit;
 [MenuItem("Forgotten Village/Build six copied house scenes")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave Play Mode before building scenes.");
  var originalHash=Hash(Source);audit=new List<string>();
  if(!File.Exists(Root+"Scenes/VillageEnvironmentReference.unity")){if(!AssetDatabase.CopyAsset("Assets/Scenes/rpgpp_lt_scene_1.0.unity",Root+"Scenes/VillageEnvironmentReference.unity"))throw new Exception("Could not preserve original village reference");}
  VillageBuilder.Build();
  var village=SceneManager.GetActiveScene();var game=UnityEngine.Object.FindFirstObjectByType<VillageGame>();
  // Remove the superseded remote room areas from the generated village only.
  foreach(var room in village.GetRootGameObjects().Where(r=>VillageGame.Houses.Contains(r.name)).ToArray())UnityEngine.Object.DestroyImmediate(room);
  game.interiors=new Transform[0];game.gate=null;game.interactionManager=UnityEngine.Object.FindFirstObjectByType<XRInteractionManager>();
  var transitions=game.gameObject.AddComponent<HouseTransitions>();transitions.game=game;game.transitions=transitions;
  transitions.outdoorRoots=village.GetRootGameObjects().Where(r=>r!=game.gameObject&&r!=game.player.gameObject&&r!=game.player.xrRig&&r!=game.ui.gameObject&&!r.GetComponent<EventSystem>()&&!r.GetComponent<XRInteractionManager>()).ToArray();
  ApplyUIOverlay(game.ui);EditorSceneManager.SaveScene(village);EditorSceneManager.SaveScene(village,"Assets/Scenes/rpgpp_lt_scene_1.0.unity");
  font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
  dark=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Ink.mat");teal=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Teal.mat");gold=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Amber.mat");
  for(int i=0;i<6;i++)BuildHouse(i);
  ElderTennisBuilder.Build();
  PlaneDreamBuilder.Build();
  var names=new[]{"Assets/Scenes/rpgpp_lt_scene_1.0.unity"}.Concat(HouseTransitions.SceneNames.Select(n=>Root+"Scenes/"+n+".unity")).ToArray();
  EditorBuildSettings.scenes=names.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!names.Contains(s.path)).Select(s=>new EditorBuildSettingsScene(s.path,false))).ToArray();
  AssetDatabase.SaveAssets();if(Hash(Source)!=originalHash)throw new Exception("Original VR room changed unexpectedly");
  audit.Add("Original My VR room SHA256 unchanged: "+originalHash);File.WriteAllLines("Verification/separate-house-build.txt",audit);
  EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");Validate();Debug.Log("SIX_COPIED_HOUSE_SCENES_BUILT");
 }
  static void ApplyUIOverlay(VillageUI ui){
  string path=Root+"Materials/Field UI Overlay.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("ForgottenVillage/World UI Overlay"));AssetDatabase.CreateAsset(material,path);}ui.panelMaterial=material;foreach(var img in ui.GetComponentsInChildren<UnityEngine.UI.Image>(true))img.material=material;
  string textPath=Root+"Materials/Field Text Overlay.mat";var text=AssetDatabase.LoadAssetAtPath<Material>(textPath);if(!text){text=new Material(ui.font.material);text.shader=Shader.Find("TextMeshPro/Distance Field Overlay");text.renderQueue=4001;AssetDatabase.CreateAsset(text,textPath);}ui.textMaterial=text;foreach(var label in ui.GetComponentsInChildren<TMP_Text>(true))label.fontSharedMaterial=text;
  var ray=ui.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();ray.checkFor3DOcclusion=false;ray.checkFor2DOcclusion=false;ui.canvas.overrideSorting=true;ui.canvas.sortingOrder=1000;
 }
 [MenuItem("Forgotten Village/Validate six copied scenes")]
 public static void Validate(){
  var summary=new List<string>();var main=SceneManager.GetActiveScene();var game=UnityEngine.Object.FindFirstObjectByType<VillageGame>();if(!game||!game.transitions||game.entrances.Length!=6)throw new Exception("Main village wiring incomplete");
  foreach(var name in HouseTransitions.SceneNames){var scene=EditorSceneManager.OpenScene(Root+"Scenes/"+name+".unity",OpenSceneMode.Additive);var roots=scene.GetRootGameObjects();var house=roots.Select(r=>r.GetComponent<HouseSceneRoot>()).FirstOrDefault(r=>r);if(!house||!house.arrival||house.copiedRoots.Length!=6)throw new Exception("House root incomplete: "+name);int missing=roots.Sum(r=>r.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)));int cameras=roots.Sum(r=>r.GetComponentsInChildren<Camera>().Count(c=>c.enabled));int listeners=roots.Sum(r=>r.GetComponentsInChildren<AudioListener>().Count(c=>c.enabled));int puzzles=roots.Sum(r=>r.GetComponentsInChildren<EngineeringStation>(true).Length);if(missing!=0||cameras!=0||listeners!=0||puzzles!=(house.houseIndex>=2&&house.houseIndex<5?1:0))throw new Exception("House validation failed: "+name);summary.Add(name+": source roots=6; missing scripts=0; enabled copied cameras/listeners=0; puzzles="+puzzles);EditorSceneManager.CloseScene(scene,true);}
  SceneManager.SetActiveScene(main);summary.Add("Village: one persistent game service; six entrance destinations; additive transition service assigned.");File.WriteAllLines("Verification/separate-scene-validation.txt",summary);
 }
 static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","");}
 static void BuildHouse(int id){
  // Open the ACTUAL source scene and Save As; never reconstruct its environment from prefabs.
  var scene=EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);var copied=scene.GetRootGameObjects();int sourceObjects=copied.Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);
  var scriptAudit=new List<string>();
  foreach(var root in copied){
   if(root.name=="XR"||root.GetComponentsInChildren<XROrigin>(true).Length>0)root.SetActive(false);
   foreach(var cam in root.GetComponentsInChildren<Camera>(true))cam.enabled=false;
   foreach(var listener in root.GetComponentsInChildren<AudioListener>(true))listener.enabled=false;
   foreach(var manager in root.GetComponentsInChildren<XRInteractionManager>(true))manager.enabled=false;
   foreach(var ev in root.GetComponentsInChildren<EventSystem>(true))ev.enabled=false;
   foreach(var b in root.GetComponentsInChildren<MonoBehaviour>(true))if(b){scriptAudit.Add(b.name+": "+b.GetType().Name);if(new[]{"TeleportPlayer","LoadScene","OnSceneLoad","TeleportAreaWithFade","TeleportAnchorWithFade"}.Contains(b.GetType().Name))b.enabled=false;}
   foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){if(r.bounds.size.magnitude<15&&!r.GetComponent<Collider>()&&r.GetComponent<MeshFilter>()){var col=r.gameObject.AddComponent<MeshCollider>();col.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;}}
  }
  HouseSceneRoot.ClearInactiveStartingSelections(copied);
  if(id==0)File.WriteAllLines("Verification/copied-room-component-audit.txt",scriptAudit);
  var content=new GameObject(HouseTransitions.SceneNames[id]+" - Investigation");var house=content.AddComponent<HouseSceneRoot>();house.houseIndex=id;house.copiedRoots=copied;
  house.arrival=Point("Player arrival",new Vector3(.5f,.15f,-1.25f),content.transform);
  var floor=Box("Comfort teleport floor",new Vector3(.1f,-.065f,-.1f),new Vector3(5.7f,.08f,3.7f),dark,content.transform);floor.GetComponent<Renderer>().enabled=false;floor.AddComponent<TeleportationArea>();
  var exit=Box("Return to village",new Vector3(-.7f,1.2f,-1.8f),new Vector3(.45f,.45f,.12f),teal,content.transform);Action(exit,"exit",id,"Return to village");var sign=Label("RETURN TO\nVILLAGE",new Vector3(-.7f,1.65f,-1.7f),1,content.transform);sign.transform.rotation=Quaternion.Euler(0,180,0);
  Label(VillageGame.Houses[id],new Vector3(.2f,2.28f,1.65f),3,content.transform);
  var lamp=new GameObject("Investigation task light",typeof(Light));lamp.transform.SetParent(content.transform,false);lamp.transform.position=new Vector3(.25f,2.3f,0);var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=id==5?1.8f:2.5f;Color[] colors={new Color(1,.78f,.5f),new Color(.4f,.95f,.8f),new Color(1,.7f,.4f),new Color(.5f,.75f,1),new Color(1,.9f,.55f),new Color(.35f,.55f,.85f)};light.color=colors[id];
  if(id<5){
   var desk=Box("Engineering workbench",new Vector3(.2f,.9f,.85f),new Vector3(1.3f,.14f,.6f),dark,content.transform);for(int leg=-1;leg<=1;leg+=2)Box("Workbench leg",new Vector3(.2f+leg*.5f,.43f,.85f),new Vector3(.08f,.86f,.5f),dark,content.transform);
   var station=desk.AddComponent<EngineeringStation>();station.topic=id;Action(desk,"puzzle",id,"Investigate engineering station",station);Label("ENGINEERING STATION",new Vector3(.2f,1.28f,.73f),2.2f,content.transform);
   var npc=Box("Resident interaction",new Vector3(1.65f,.8f,.8f),new Vector3(.45f,1.6f,.45f),id%2==0?gold:teal,content.transform);npc.GetComponent<Renderer>().enabled=false;Action(npc,"dialogue",id,"Speak to "+(id==0?"the Elder":VillageGame.Houses[id].Replace("'s House","")));var coat=GameObject.CreatePrimitive(PrimitiveType.Capsule);coat.name="Resident coat";coat.transform.SetParent(content.transform);coat.transform.position=new Vector3(1.65f,.75f,.8f);coat.transform.localScale=new Vector3(.4f,.6f,.4f);coat.GetComponent<Renderer>().sharedMaterial=id%2==0?dark:teal;UnityEngine.Object.DestroyImmediate(coat.GetComponent<Collider>());var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Resident face";head.transform.SetParent(content.transform);head.transform.position=new Vector3(1.65f,1.5f,.8f);head.transform.localScale=Vector3.one*.3f;head.GetComponent<Renderer>().sharedMaterial=gold;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());Box("Resident hat",new Vector3(1.65f,1.7f,.8f),new Vector3(.43f,.09f,.4f),dark,content.transform);Label("TALK",new Vector3(1.65f,1.95f,.7f),.8f,content.transform);
   if(id==0){var photo=Box("Six villagers photograph",new Vector3(-.5f,1.65f,1.65f),new Vector3(.75f,.45f,.07f),gold,content.transform);Action(photo,"photo",0,"Inspect scratched photograph");for(int person=0;person<6;person++)Box(person==5?"Scratched face":"Villager silhouette",new Vector3(-.8f+person*.12f,1.65f,1.6f),new Vector3(.075f,.2f,.025f),person==5?dark:teal,content.transform);for(int b=0;b<6;b++)Box("Damaged archive volume",new Vector3(-2.05f+b*.16f,.51f,.75f),new Vector3(.13f,.12f,.25f),b%2==0?gold:dark,content.transform);Prop("Notebook_Brown",new Vector3(-1,.5f,.75f),content.transform);var lever=Prop("Control_Lever_Blue",new Vector3(-.23f,1,.85f),content.transform);if(lever){var xl=lever.GetComponentInChildren<XRLever>();if(xl){xl.defaultValue=true;UnityEventTools.AddPersistentListener(xl.OnLeverActivate,station.ConnectLoad);UnityEventTools.AddPersistentListener(xl.OnLeverDeactivate,station.DisconnectLoad);}var va=lever.AddComponent<VillageAction>();va.station=station;va.kind="switch";if(!lever.GetComponent<Collider>())lever.AddComponent<BoxCollider>();var vi=lever.AddComponent<VillageInteractable>();vi.prompt="Toggle circuit load";UnityEventTools.AddPersistentListener(vi.action,va.Use);}}
   if(id==1){for(int bottle=0;bottle<6;bottle++){var o=GameObject.CreatePrimitive(PrimitiveType.Cylinder);o.name="Filter reagent bottle";o.transform.SetParent(content.transform);o.transform.position=new Vector3(-2+bottle*.18f,.65f,.7f);o.transform.localScale=new Vector3(.10f,.17f,.10f);o.GetComponent<Renderer>().sharedMaterial=bottle%2==0?teal:gold;}Label("MEMORY SIGNALS\n100 Hz / 5 kHz",new Vector3(-1.4f,1.7f,1.68f),1.3f,content.transform);}
   if(id==2){for(int part=0;part<3;part++){var o=Box("Amplifier component "+part,new Vector3(-.2f+part*.25f,1.1f,.85f),new Vector3(.16f,.15f,.18f),gold,content.transform);o.AddComponent<Rigidbody>();o.AddComponent<XRGrabInteractable>();}var socket=Box("Measurement socket",new Vector3(.95f,.55f,-.1f),new Vector3(.45f,.2f,.45f),teal,content.transform);socket.GetComponent<Collider>().isTrigger=true;var sensor=socket.AddComponent<WorkshopSocket>();sensor.indicator=light;Label("COMPONENT SOCKET",new Vector3(.95f,.95f,-.1f),1.3f,content.transform);}
   if(id==3){Label("MIC-17\nSAFETY DIAGNOSTICS",new Vector3(-1.35f,1.55f,1.6f),1.4f,content.transform);var testButton=Prop("Control_Button_Play",new Vector3(.65f,1,.85f),content.transform);if(testButton){var press=testButton.GetComponentInChildren<XRButton>();if(press)UnityEventTools.AddPersistentListener(press.OnPress,station.Diagnostic);var action=testButton.AddComponent<VillageAction>();action.station=station;action.kind="diagnostic";if(!testButton.GetComponent<Collider>())testButton.AddComponent<BoxCollider>();var interaction=testButton.AddComponent<VillageInteractable>();interaction.prompt="Run controller diagnostic";UnityEventTools.AddPersistentListener(interaction.action,action.Use);}}
   if(id==4){for(int c=0;c<3;c++){var clock=GameObject.CreatePrimitive(PrimitiveType.Cylinder);clock.name="Timing clock "+c;clock.transform.SetParent(content.transform);clock.transform.position=new Vector3(-1.6f+c*.65f,2.05f,1.7f);clock.transform.rotation=Quaternion.Euler(90,0,0);clock.transform.localScale=new Vector3(.45f,.025f,.45f);clock.GetComponent<Renderer>().sharedMaterial=gold;Box("Clock minute hand",clock.transform.position+new Vector3(0,.08f,-.04f),new Vector3(.02f,.18f,.02f),dark,content.transform);}Label("SETUP TIMING\nRisk is not causation",new Vector3(-1.35f,1.5f,1.6f),1.5f,content.transform);}
   if(id==1||id==2||id==4){var scope=new GameObject("Oscilloscope traces");scope.transform.SetParent(content.transform);scope.transform.position=new Vector3(.2f,.65f,1.5f);scope.transform.localScale=new Vector3(.6f,.6f,.6f);station.inputTrace=Trace("Input",scope.transform,teal);station.outputTrace=Trace("Output",scope.transform,gold);}
   string[] items={"Blue Flower A","Blue Flower B","Mushroom","Strange Herb","Brass timing token"};var item=Box(items[id],new Vector3(-2.1f,.98f,-.8f),Vector3.one*.12f,teal,content.transform);Action(item,"pickup",id,"Collect "+items[id]);item.GetComponent<VillageAction>().item=items[id];item.GetComponent<VillageAction>().description="A village specimen recovered during your investigation.";
  }else{
   string[] titles={"Five records","Software + logs","Timing risk","Identity record"};for(int e=0;e<4;e++){float x=-.9f+e*.65f;var record=Box(titles[e],new Vector3(x,1.05f,1.1f),new Vector3(.45f,.12f,.4f),e==2?teal:gold,content.transform);Action(record,"evidence",e,"Review "+titles[e]);Box("Evidence pedestal",new Vector3(x,.5f,1.1f),new Vector3(.38f,1,.34f),dark,content.transform);Label(titles[e],new Vector3(x,1.45f,1.1f),.6f,content.transform);}
   var final=Box("Verified safe-state console",new Vector3(.4f,.85f,0),new Vector3(.75f,.18f,.5f),teal,content.transform);Action(final,"final",0,"Execute verified safe-state command");Box("Safety console stand",new Vector3(.4f,.4f,0),new Vector3(.65f,.8f,.4f),dark,content.transform);Label("VERIFIED SAFE STATE",new Vector3(.4f,1.2f,-.1f),1.5f,content.transform);house.safetyGate=Box("Miniature safety gate",new Vector3(1.85f,.8f,.3f),new Vector3(.65f,.08f,.08f),gold,content.transform).transform;Box("Gate hinge",new Vector3(1.55f,.4f,.3f),new Vector3(.08f,.8f,.08f),dark,content.transform);Label("EXPERIMENT INCIDENT\nEvidence before conclusions",new Vector3(-1.3f,1.5f,1.55f),1.5f,content.transform);
   foreach(var root in copied)foreach(var tr in root.GetComponentsInChildren<Transform>())if(tr.name=="TV_FlatWithStand")tr.Rotate(0,0,-12);
  }
  PrefabUtility.SaveAsPrefabAsset(content,Root+"Prefabs/"+HouseTransitions.SceneNames[id]+"Content.prefab");
  string path=Root+"Scenes/"+HouseTransitions.SceneNames[id]+".unity";EditorSceneManager.SaveScene(scene,path);
  int retained=copied.Sum(r=>r.GetComponentsInChildren<Transform>(true).Length);if(retained<sourceObjects)throw new Exception("Copied source objects lost in "+path);
  audit.Add(path+": actual source objects retained="+retained+"; original rigs/cameras disabled; house index="+id);
 }
 static Transform Point(string n,Vector3 p,Transform parent){var o=new GameObject(n);o.transform.SetParent(parent);o.transform.position=p;return o.transform;}
 static GameObject Box(string n,Vector3 p,Vector3 size,Material m,Transform parent){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=n;o.transform.SetParent(parent);o.transform.position=p;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
 static TextMeshPro Label(string text,Vector3 p,float width,Transform parent){var o=new GameObject(text,typeof(TextMeshPro));o.transform.SetParent(parent);o.transform.position=p;var t=o.GetComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=1;t.enableAutoSizing=true;t.fontSizeMin=.35f;t.fontSizeMax=1;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.96f,.86f,.63f);t.rectTransform.sizeDelta=new Vector2(width,.65f);return t;}
 static void Action(GameObject o,string kind,int id,string prompt,EngineeringStation station=null){var a=o.AddComponent<VillageAction>();a.kind=kind;a.index=id;a.station=station;var v=o.AddComponent<VillageInteractable>();v.prompt=prompt;o.AddComponent<XRSimpleInteractable>();UnityEventTools.AddPersistentListener(v.action,a.Use);}
 static GameObject Prop(string name,Vector3 p,Transform parent){var path=AssetDatabase.FindAssets(name+" t:Prefab",new[]{"Assets/_Course Library"}).Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(x=>Path.GetFileNameWithoutExtension(x)==name);if(path==null)return null;var o=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));o.transform.SetParent(parent);o.transform.position=p;audit.Add("Reused: "+path);return o;}
 static LineRenderer Trace(string name,Transform parent,Material material){var o=new GameObject(name,typeof(LineRenderer));o.transform.SetParent(parent,false);var l=o.GetComponent<LineRenderer>();l.useWorldSpace=false;l.widthMultiplier=.018f;l.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/"+(name=="Input"?"Input waveform":"Output waveform")+" trace.mat")??material;return l;}
}
}


