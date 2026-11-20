using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    public static class NpcValidation {
        public static void Run() {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var report = new StringBuilder();
            int total = 0;
            try {
                foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled)) {
                    var scene = EditorSceneManager.OpenScene(entry.path);
                    var roots = scene.GetRootGameObjects();
                    foreach (var tr in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                        Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject) == 0, scene.name + " missing script: " + tr.name);
                    var actors = roots.SelectMany(r => r.GetComponentsInChildren<VillageAction>(true)).Where(a => a.kind == "dialogue").ToArray();
                    foreach (var action in actors) {
                        var npc = action.GetComponentInChildren<NpcPresentation>(true);
                        Require(npc && npc.role == action.index, "NPC role does not match dialogue " + action.name);
                        Require(npc.animator && npc.animator.avatar && npc.animator.avatar.isValid && npc.animator.avatar.isHuman, "Avatar invalid");
                        Require(!npc.animator.applyRootMotion, "Root motion enabled");
                        var controller = npc.animator.runtimeAnimatorController as AnimatorController;
                        Require(controller && controller.animationClips.Length == 1 && controller.animationClips[0].name == "metarig|Idle", "Stationary Idle missing");
                        Require(npc.head, "Head bone missing");
                        Require(npc.GetComponentsInChildren<Collider>(true).Length == 0 && npc.GetComponentsInChildren<Rigidbody>(true).Length == 0, "Visual adds physics");
                        var renderers = npc.GetComponentsInChildren<SkinnedMeshRenderer>();
                        Require(renderers.Length == 2, "Expected body and hat");
                        var bounds = renderers[0].bounds;
                        foreach (var r in renderers) {
                            Require(r.enabled && r.sharedMesh, "Hidden or missing mesh");
                            Require(r.sharedMaterials.All(m => m && m.shader && m.shader.isSupported), "Invalid material/shader");
                            bounds.Encapsulate(r.bounds);
                        }
                        Require(bounds.size.y > 1.5f && bounds.size.y < 1.95f, "Human scale invalid: " + bounds);
                        Require(Mathf.Abs(bounds.min.y - action.GetComponent<Collider>().bounds.min.y) < .02f, "Feet not grounded to original NPC floor");
                        Require(action.GetComponent<XRSimpleInteractable>() && action.GetComponent<VillageInteractable>(), "Desktop/XR interaction missing");
                        var interaction=action.GetComponent<VillageInteractable>();
                        Require(interaction.action.GetPersistentEventCount()>0 && interaction.action.GetPersistentTarget(0)==action, "Dialogue event disconnected");
                        if(npc.keepFacingCourt)Require(action.GetComponent<Collider>().isTrigger, "Elder blocks ball");
                        report.AppendLine("PASS: " + scene.name + " / " + action.index + " height=" + bounds.size.y.ToString("F2") + "; avatar, idle, materials, feet, interaction and collision verified");
                        Capture(npc, scene.name);
                        total++;
                    }
                    if (scene.name != "rpgpp_lt_scene_1.0") {
                        Require(!roots.SelectMany(r=>r.GetComponentsInChildren<AudioListener>()).Any(l=>l.enabled), "Extra house audio listener");
                        Require(!roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Any(c=>c.enabled), "Extra house camera");
                    }
                    report.AppendLine("PASS: "+scene.name+" missing scripts=0");
                }
                Require(total==6,"Expected village Elder plus five house residents");
                report.AppendLine("PASS: all six NPC instances validated. Screenshots are Editor camera renders, not headset tests.");
            } catch(Exception e) {report.AppendLine("FAIL: "+e);throw;}
            finally {File.WriteAllText("Verification/npc-validation.txt",report.ToString());EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        static void Capture(NpcPresentation npc,string scene) {
            var go=new GameObject("Temporary NPC verification camera");
            var camera=go.AddComponent<Camera>();camera.fieldOfView=45;
            camera.transform.position=npc.transform.position+new Vector3(0,1.35f,-3.5f);
            camera.transform.LookAt(npc.transform.position+Vector3.up*.95f);
            var texture=RenderTexture.GetTemporary(640,640,24);var previous=RenderTexture.active;
            var image=new Texture2D(640,640,TextureFormat.RGB24,false);
            try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes("Verification/npc-"+scene+".png",image.EncodeToPNG());}
            finally{RenderTexture.active=previous;camera.targetTexture=null;RenderTexture.ReleaseTemporary(texture);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
