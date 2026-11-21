using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ForgottenVillage;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class NpcIntegration {
 const string Source = "Assets/Stylized NPC - Peasant Nolant/";
 const string Dest = "Assets/TheForgottenVillage/";
 static NpcIntegration(){ EditorApplication.update += Tick; File.WriteAllText("Verification/npc-ready.txt","integration-v6"); }
 static void Tick(){
  const string request="Verification/npc.request";
  if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || !File.Exists(request))return;
  string command;try{command=File.ReadAllText(request).Trim();File.Delete(request);}catch(IOException){return;}
  try { if(command=="audit")Audit(); else if(command=="build")Build(); else if(command=="tests")TennisTestRunner.Run(); else if(command=="validate")NpcValidation.Run(); else if(command=="remove-unused-oculus")RemoveUnusedOculus(); else if(command=="refine")Refine(); else if(command=="play")NpcPlayChecks.Request(); else if(command=="palette")Palette(); else throw new ArgumentException("Unknown NPC command: "+command); }
  catch(Exception e){File.WriteAllText("Verification/npc-result.txt",e.ToString());Debug.LogException(e);}
 }
 [MenuItem("Forgotten Village/NPC/Audit")]
 public static void Audit(){
  var log=new StringBuilder();
  foreach(var plugin in PluginImporter.GetAllImporters().Where(p=>p.assetPath.Contains("OVRPlugin"))){var info=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(plugin.assetPath);log.AppendLine("PLUGIN "+plugin.assetPath+" package="+(info==null?"NULL":info.resolvedPath));}
  foreach(var a in AssetDatabase.LoadAllAssetsAtPath(Source+"Models/Peasant Nolant(Free Version).fbx")){
   log.AppendLine(a.GetType().Name+" "+a.name);
   if(a is Avatar av)log.AppendLine("valid="+av.isValid+" human="+av.isHuman);
   if(a is Mesh mesh)log.AppendLine("vertices="+mesh.vertexCount+" submeshes="+mesh.subMeshCount);
   if(a is AnimationClip clip)log.AppendLine("length="+clip.length+" loop="+clip.isLooping);
  }
  foreach(var s in EditorBuildSettings.scenes.Where(s=>s.enabled)){
   var scene=EditorSceneManager.OpenScene(s.path);
   log.AppendLine("SCENE "+s.path);
   foreach(var a in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VillageAction>(true)).Where(a=>a.kind=="dialogue"))
    log.AppendLine("NPC "+a.index+" "+a.name+" position="+a.transform.position+" scale="+a.transform.lossyScale+" collider="+a.GetComponent<Collider>().bounds);
   foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)log.AppendLine("MISSING SCRIPT "+t.name);
  }
  File.WriteAllText("Verification/npc-audit.txt",log.ToString());
  File.WriteAllText("Verification/npc-result.txt","Audit complete");
 }
 static void Palette(){
  Directory.CreateDirectory(Dest+"Materials/NPC");AssetDatabase.Refresh();
  for(int role=0;role<Roles.Length;role++){
   string materialPath=Dest+"Materials/NPC/"+Colors[role]+".mat";
   var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
   if(!material){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Source+"Materials/Peasant Nolant "+Colors[role]+".mat"));material.name="Resident "+Colors[role];material.enableInstancing=true;AssetDatabase.CreateAsset(material,materialPath);}
   string path=Dest+"Prefabs/NPC/"+Roles[role]+".prefab";var prefab=PrefabUtility.LoadPrefabContents(path);
   try{foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText("Verification/npc-result.txt","PASS: project-owned material palette shares original textures");
 }
 static void Refine(){
  var setup=EditorSceneManager.GetSceneManagerSetup();
  try{foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
   var scene=EditorSceneManager.OpenScene(entry.path);bool changed=false;
   foreach(var action in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VillageAction>(true)).Where(a=>a.kind=="dialogue")){
    var npc=action.GetComponentInChildren<NpcPresentation>();if(!npc)continue;
    if(npc.keepFacingCourt)action.GetComponent<Collider>().isTrigger=false;
    if(action.index==3 || action.index==4){var position=action.transform.position;position.x=1.25f;action.transform.position=position;}
    var scope=action.transform.parent;
    if(scope)foreach(var label in scope.GetComponentsInChildren<TMPro.TMP_Text>(true).Where(t=>t.name=="TALK" || t.text=="SPEAK")){
     label.transform.position=npc.transform.position+Vector3.up*2.05f;
     label.text=Roles[action.index].ToUpperInvariant()+" / SPEAK";
    }
    changed=true;
   }
   if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);var house=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<HouseSceneRoot>(true)).FirstOrDefault();if(house && !PrefabUtility.SaveAsPrefabAsset(house.gameObject,Dest+"Prefabs/"+scene.name+"Content.prefab"))throw new Exception("Failed saving house prefab: "+scene.name);}
  }}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
  File.WriteAllText("Verification/npc-result.txt","PASS: resident prompts aligned and solid XR targets retained");
 }
 static void RemoveUnusedOculus(){
  foreach(var group in new[]{BuildTargetGroup.Standalone,BuildTargetGroup.Android}){
   var settings=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
   if(!settings || !settings.Manager || settings.Manager.activeLoaders.Count!=1 || settings.Manager.activeLoaders[0].GetType().Name!="OpenXRLoader")throw new Exception("Expected OpenXR-only provider: "+group);
  }
  EditorBuildSettings.RemoveConfigObject("Unity.XR.Oculus.Settings");
  AssetDatabase.DeleteAsset("Assets/XR/Settings/Oculus Settings.asset");
  AssetDatabase.DeleteAsset("Assets/XR/Loaders/Oculus Loader.asset");
  UnityEditor.PackageManager.Client.Remove("com.unity.xr.oculus");
  File.WriteAllText("Verification/npc-oculus-removal.txt","Verified Windows and Android OpenXR-only loaders; removed unused Oculus provider and orphaned settings. Package resolution pending.");
 }
 static readonly string[] Roles={"Elder","Alchemist","Carpenter","Technician","Clockmaker"};
 static readonly string[] Colors={"Brown","Green","Brown","Blue","Yellow"};
 static readonly float[] Heights={1.68f,1.72f,1.82f,1.76f,1.70f};
 static Bounds BoundsOf(GameObject root){var rr=root.GetComponentsInChildren<Renderer>();if(rr.Length==0)throw new Exception("No NPC renderers");var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
 [MenuItem("Forgotten Village/NPC/Integrate saved scenes")]
 public static void Build(){
  // Never discard an unsaved user scene when the automation request is consumed.
  for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save the open scene before NPC integration.");
  var setup=EditorSceneManager.GetSceneManagerSetup();
  var log=new StringBuilder();
  try {
   Directory.CreateDirectory(Dest+"Prefabs/NPC");Directory.CreateDirectory(Dest+"Animations");
   AssetDatabase.Refresh();
   var clips=AssetDatabase.LoadAllAssetsAtPath(Source+"Models/Peasant Nolant(Free Version).fbx").OfType<AnimationClip>().ToArray();
   var idle=clips.FirstOrDefault(c=>c.name=="metarig|Idle");if(!idle)throw new Exception("Imported Idle clip missing");
   string controllerPath=Dest+"Animations/ResidentIdle.controller";
   var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
   if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);var state=controller.layers[0].stateMachine.AddState("Idle");state.motion=idle;controller.layers[0].stateMachine.defaultState=state;}
   for(int role=0;role<Roles.Length;role++){
    var wrapper=new GameObject(Roles[role]+" Nolant visual");
    try{
     var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"Prefabs/Peasant Nolant "+Colors[role]+"(Free Version).prefab");if(!source)throw new Exception("Missing source prefab");
     var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(wrapper.transform,false);
     foreach(var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
     foreach(var rb in model.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(rb);
     var animator=model.GetComponentInChildren<Animator>();if(!animator || !animator.avatar || !animator.avatar.isValid)throw new Exception("Invalid Nolant avatar");
     animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
     var b=BoundsOf(wrapper);if(b.size.y<.01f)throw new Exception("Invalid model height");model.transform.localScale*=Heights[role]/b.size.y;
     b=BoundsOf(wrapper);model.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
     var presentation=wrapper.AddComponent<NpcPresentation>();presentation.role=role;presentation.animator=animator;presentation.head=animator.isHuman?animator.GetBoneTransform(HumanBodyBones.Head):null;
     PrefabUtility.SaveAsPrefabAsset(wrapper,Dest+"Prefabs/NPC/"+Roles[role]+".prefab");
     log.AppendLine(Roles[role]+": "+Colors[role]+", height="+BoundsOf(wrapper).size.y+", renderers="+wrapper.GetComponentsInChildren<Renderer>().Length);
    }finally{UnityEngine.Object.DestroyImmediate(wrapper);}
   }
   foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
    var scene=EditorSceneManager.OpenScene(entry.path);
    var actions=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<VillageAction>(true)).Where(a=>a.kind=="dialogue").ToArray();
    foreach(var action in actions){
     if(action.index<0||action.index>=Roles.Length)throw new Exception("Unsupported dialogue NPC "+action.index);
     if(action.GetComponentInChildren<NpcPresentation>(true))continue;
     var collider=action.GetComponent<Collider>();if(!collider)throw new Exception("NPC has no interaction collider");
     var floor=collider.bounds.min.y;
     var scope=action.transform.parent;
     // Existing house placeholders are siblings of their interaction root.
     // Disable only their visual renderers; retain object identities and scripts.
     foreach(var t in (scope?scope:action.transform).GetComponentsInChildren<Transform>(true)){
      bool placeholder=t.name=="Resident coat"||t.name=="Resident face"||t.name=="Resident hat"||(t.name=="Hat"&&t.IsChildOf(action.transform));
      if(placeholder){foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;}
     }
     var rootRenderer=action.GetComponent<Renderer>();if(rootRenderer)rootRenderer.enabled=false;
     foreach(var idleBehaviour in action.GetComponentsInChildren<ResidentIdle>(true))idleBehaviour.enabled=false;
     var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dest+"Prefabs/NPC/"+Roles[action.index]+".prefab"),scene);
     visual.transform.position=new Vector3(collider.bounds.center.x,floor,collider.bounds.center.z);
     var compensation=new GameObject("NPC scale compensation");compensation.transform.position=visual.transform.position;compensation.transform.SetParent(action.transform,true);
     visual.transform.SetParent(compensation.transform,false);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.Euler(0,180,0);
     var presentation=visual.GetComponent<NpcPresentation>();presentation.keepFacingCourt=action.index==0&&action.GetComponentInParent<ElderTennis>();
     // Preserve the existing ray target while excluding it from physical ball contacts.
     if(presentation.keepFacingCourt)collider.isTrigger=false;
     PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);
     PrefabUtility.RecordPrefabInstancePropertyModifications(visual.transform);
     log.AppendLine(scene.name+" / "+action.name+" role="+action.index+" bounds="+BoundsOf(visual));
    }
    if(actions.Length>0){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
     var house=scene.GetRootGameObjects().Select(r=>r.GetComponent<HouseSceneRoot>()).FirstOrDefault(h=>h);
     if(house && !PrefabUtility.SaveAsPrefabAsset(house.gameObject,Dest+"Prefabs/"+scene.name+"Content.prefab"))throw new Exception("Failed saving house prefab: "+scene.name);
    }
   }
   AssetDatabase.SaveAssets();File.WriteAllText("Verification/npc-build.txt",log.ToString());File.WriteAllText("Verification/npc-result.txt","PASS: NPC integration saved");
  }finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
 }
}
}
