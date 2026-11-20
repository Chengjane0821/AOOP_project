using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class NpcIntegration {
 const string Source = "Assets/Stylized NPC - Peasant Nolant/";
 const string Dest = "Assets/TheForgottenVillage/";
 static NpcIntegration(){ EditorApplication.update += Tick; }
 static void Tick(){
  const string request="Verification/npc.request";
  if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || !File.Exists(request))return;
  var command=File.ReadAllText(request).Trim();File.Delete(request);
  try { if(command=="audit")Audit(); else if(command=="build")Build(); else if(command=="tests")TennisTestRunner.Run(); }
  catch(Exception e){File.WriteAllText("Verification/npc-result.txt",e.ToString());Debug.LogException(e);}
 }
 [MenuItem("Forgotten Village/NPC/Audit")]
 public static void Audit(){
  var log=new StringBuilder();
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
 public static void Build(){throw new NotImplementedException();}
}
}
