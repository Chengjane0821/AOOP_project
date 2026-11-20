using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad] public static class NpcPlayChecks {
        static IEnumerator routine;
        static int frame;
        static double deadline;
        static readonly StringBuilder report=new StringBuilder();
        static readonly List<string> errors=new List<string>();
        static NpcPlayChecks(){EditorApplication.update+=Tick;Application.logMessageReceived+=(m,s,t)=>{if(SessionState.GetBool("NPC_PLAY_ACTIVE",false)&&(t==LogType.Error||t==LogType.Exception))errors.Add(m+"\n"+s);};}
        public static void Request(){EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");SessionState.SetBool("NPC_PLAY_PENDING",true);EditorApplication.isPlaying=true;}
        static void Tick(){
            if(EditorApplication.isCompiling)return;
            if(EditorApplication.isPlaying&&SessionState.GetBool("NPC_PLAY_PENDING",false)&&Time.frameCount>15){SessionState.SetBool("NPC_PLAY_PENDING",false);SessionState.SetBool("NPC_PLAY_ACTIVE",true);report.Clear();errors.Clear();routine=Run();deadline=EditorApplication.timeSinceStartup+120;}
            if(routine==null)return;
            if(EditorApplication.timeSinceStartup>deadline){Finish(new Exception("NPC Play Mode timeout"));return;}
            if(frame==Time.frameCount)return;frame=Time.frameCount;
            try{if(!routine.MoveNext())Finish(null);}catch(Exception e){Finish(e);}
        }
        static void Finish(Exception error){report.AppendLine(error==null&&errors.Count==0?"PASS: NPC Play Mode suite":"FAIL: "+error);foreach(var e in errors)report.AppendLine(e);File.WriteAllText("Verification/npc-playmode.txt",report.ToString());routine=null;VillageStorage.overridePath=null;SessionState.SetBool("NPC_PLAY_ACTIVE",false);EditorApplication.isPlaying=false;}
        static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);}
        static IEnumerable WaitTransition(VillageGame game){while(game.transitions.Busy)yield return null;yield return null;}
        static IEnumerable Wait(float seconds){float end=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<end)yield return null;}
        static IEnumerator Run(){
            VillageStorage.overridePath=Path.GetFullPath("Temp/npc-test-save.json");
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGame>();game.NewGame();game.state.elder=true;game.ui.Close();
            var village=UnityEngine.Object.FindObjectsByType<VillageAction>(FindObjectsSortMode.None).Single(a=>a.kind=="dialogue");
            foreach(var step in CheckActor(game,village))yield return step;
            for(int i=0;i<6;i++){
                game.transitions.Enter(i);foreach(var step in WaitTransition(game))yield return step;
                Check(game.transitions.Current&&game.transitions.Current.houseIndex==i,"House entry "+i);
                if(i<5){var actor=game.transitions.Current.GetComponentsInChildren<VillageAction>().Single(a=>a.kind=="dialogue");foreach(var step in CheckActor(game,actor))yield return step;}
                Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled)==1,"One active listener in house "+i);
                game.Exit();foreach(var step in WaitTransition(game))yield return step;
                Check(!game.transitions.Current&&SceneManager.sceneCount==1&&!game.player.ActivityLocksMovement,"Return restores village control "+i);
            }
            Check(errors.Count==0,"No runtime exceptions during NPC interaction and six-house transitions");
        }
        static IEnumerable CheckActor(VillageGame game,VillageAction actor){
            var npc=actor.GetComponentInChildren<NpcPresentation>();var target=actor.GetComponent<Collider>();
            Check(npc&&npc.animator&&npc.animator.isActiveAndEnabled,"Visible animated resident "+actor.index);
            var p=npc.transform.position+new Vector3(0,.05f,-2.8f);game.player.Teleport(p,0);game.ui.Close();
            foreach(var step in Wait(.15f))yield return step;
            var origin=target.bounds.center+Vector3.back*1.3f;
            Check(Physics.Raycast(origin,Vector3.forward,out var hit,2f,~0,QueryTriggerInteraction.Ignore)&&hit.collider==target,"Solid desktop/XR ray reaches resident "+actor.index);
            actor.GetComponent<VillageInteractable>().Activate();
            Check(game.ui.IsOpen,"Desktop activation opens role dialogue "+actor.index);game.ui.Close();
            var interactable=actor.GetComponent<XRSimpleInteractable>();
            Check(game.interactionManager.TryGetInteractableForCollider(target,out var registered)&&registered==(IXRInteractable)interactable,"NPC collider registered with XR manager");
            var fixture=new GameObject("NPC XR selection fixture");var ray=fixture.AddComponent<XRRayInteractor>();ray.interactionManager=game.interactionManager;
            Check(game.interactionManager.IsSelectPossible(ray,interactable),"XR ray selection allowed");
            game.interactionManager.SelectEnter((IXRSelectInteractor)ray,(IXRSelectInteractable)interactable);
            Check(game.ui.IsOpen,"XR selection opens dialogue "+actor.index);
            game.interactionManager.SelectExit((IXRSelectInteractor)ray,(IXRSelectInteractable)interactable);UnityEngine.Object.Destroy(fixture);
            var position=npc.transform.position;float time=npc.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            foreach(var step in Wait(.25f))yield return step;
            Check(npc.animator.GetCurrentAnimatorStateInfo(0).normalizedTime>time,"Idle animation advances");
            Check(Vector3.Distance(position,npc.transform.position)<.001f,"Animation does not displace resident");
            if(npc.keepFacingCourt){var match=actor.GetComponentInParent<ElderTennis>();Check(Physics.GetIgnoreCollision(target,match.ball.GetComponent<Collider>()),"Elder ray collider excluded from ball contacts");}
            game.ui.Close();
        }
    }
}
