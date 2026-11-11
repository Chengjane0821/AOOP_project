using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForgottenVillage.Editor {
    // A short visual fixture. Full rule/progression suites remain separate.
    [InitializeOnLoad]
    public static class WorkshopPolishChecks {
        const string Active="WORKSHOP_PREVIEW_ACTIVE", Pending="WORKSHOP_PREVIEW_PENDING";
        const string Finished="WORKSHOP_PREVIEW_FINISHED", Previous="WORKSHOP_PREVIEW_PREVIOUS_SAVE";
        const string PreviousNull="WORKSHOP_PREVIEW_PREVIOUS_NULL", Deadline="WORKSHOP_PREVIEW_DEADLINE";
        static IEnumerator routine;
        static StringBuilder report;
        static int frame=-1;
        static readonly List<string> errors=new List<string>();
        static ClockRhythmRun clock;
        static PowerGridRun grid;
        static string SavePath=>Path.GetFullPath("Verification/workshop-polish-test-save.json");

        static WorkshopPolishChecks(){
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=PlayModeChanged;
            Application.logMessageReceived+=(message,trace,type)=>{
                if(routine!=null&&(type==LogType.Error||type==LogType.Exception||type==LogType.Assert))errors.Add(message);
            };
            // Keep isolation through both entering/exiting domain reloads.
            if(SessionState.GetBool(Active,false))VillageStorage.overridePath=SavePath;
        }

        [MenuItem("Forgotten Village/Preview polished workshops")]
        public static void Request(){
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||SessionState.GetBool(Active,false))
                throw new InvalidOperationException("Finish the current play/preview session before previewing workshops.");
            Directory.CreateDirectory("Verification");
            SessionState.SetBool(PreviousNull,VillageStorage.overridePath==null);
            SessionState.SetString(Previous,VillageStorage.overridePath??"");
            SessionState.SetBool(Active,true);SessionState.SetBool(Pending,true);SessionState.SetBool(Finished,false);
            SessionState.SetFloat(Deadline,(float)(EditorApplication.timeSinceStartup+180));
            VillageStorage.overridePath=SavePath;report=new StringBuilder();errors.Clear();frame=-1;
            try {
                EditorSceneManager.OpenScene("Assets/Scenes/rpgpp_lt_scene_1.0.unity");
                var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if(type!=null)EditorWindow.GetWindow(type).Show();
                EditorApplication.isPlaying=true;
            } catch(Exception exception){Finish(exception);throw;}
        }

        static void Tick(){
            if(!SessionState.GetBool(Active,false)||SessionState.GetBool(Finished,false))return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Deadline,0)){
                Finish(new TimeoutException("Workshop preview exceeded its 180-second deadline."));return;
            }
            if(EditorApplication.isCompiling||!EditorApplication.isPlaying)return;
            EditorApplication.QueuePlayerLoopUpdate();
            if(SessionState.GetBool(Pending,false)&&Time.frameCount>12){
                SessionState.SetBool(Pending,false);VillageStorage.overridePath=SavePath;
                report=new StringBuilder();errors.Clear();routine=Run();
            }
            if(SessionState.GetBool(Pending,false))return;
            if(routine==null){Finish(new InvalidOperationException("Workshop preview interrupted by script/domain reload."));return;}
            if(frame==Time.frameCount)return;frame=Time.frameCount;
            try {if(!routine.MoveNext())Finish(null);}catch(Exception exception){Finish(exception);}
        }

        static void PlayModeChanged(PlayModeStateChange state){
            if(!SessionState.GetBool(Active,false))return;
            if(state==PlayModeStateChange.ExitingPlayMode&&!SessionState.GetBool(Finished,false))
                Finish(new OperationCanceledException("Workshop preview was manually interrupted."),false);
            if(state==PlayModeStateChange.EnteredEditMode)RestoreSavePath();
        }

        static void Finish(Exception failure,bool exitPlay=true){
            if(SessionState.GetBool(Finished,false))return;
            SessionState.SetBool(Finished,true);SessionState.SetBool(Pending,false);
            var activeRoutine=routine;routine=null;
            try{(activeRoutine as IDisposable)?.Dispose();}catch(Exception exception){failure=failure??exception;}
            if(clock)clock.Stop();if(grid)grid.Stop();clock=null;grid=null;
            if(report==null)report=new StringBuilder();
            if(failure==null&&errors.Count>0)failure=new Exception("Runtime errors occurred during the preview.");
            report.AppendLine(failure==null?"PASS: Polished workshop visual preview completed":"FAIL: "+failure);
            foreach(var error in errors)report.AppendLine("Runtime error: "+error);
            report.AppendLine("Scope: quick art preview fixture using actual additive scenes, one real timed bell interaction, physical panel callbacks, safe power and overload captures, selection rays and camera checks. No gameplay time/score is modified. This does not replace complete playtests or physical VR/device acceptance.");
            File.WriteAllText("Verification/workshop-polish-preview.txt",report.ToString());
            // Never restore the real path before Play Mode teardown is finished.
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(exitPlay)EditorApplication.isPlaying=false;}
            else if(exitPlay)RestoreSavePath();
        }

        static void RestoreSavePath(){
            VillageStorage.overridePath=SessionState.GetBool(PreviousNull,true)?null:SessionState.GetString(Previous,"");
            SessionState.SetBool(Active,false);SessionState.SetBool(Pending,false);SessionState.SetBool(Finished,false);
        }

        static void Check(bool value,string message){if(!value)throw new Exception(message);report.AppendLine("PASS: "+message);}
        static IEnumerable<object> Wait(double seconds){double end=Time.realtimeSinceStartupAsDouble+seconds;while(Time.realtimeSinceStartupAsDouble<end)yield return null;}
        static IEnumerable<object> Transition(VillageGame game){
            double end=Time.realtimeSinceStartupAsDouble+30;
            while(game.transitions.Busy){if(Time.realtimeSinceStartupAsDouble>end)throw new TimeoutException("Preview house transition timed out.");yield return null;}
            yield return null;
        }
        static void CameraCheck(){Check(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(camera=>camera.enabled)==1,"Exactly one active player camera");}
        static void RayCheck(Component target,Collider collider,Camera camera,Type expected){
            Check(collider&&collider.enabled,"Selection collider available for "+target.name);
            Physics.SyncTransforms();var bounds=collider.bounds;
            foreach(var offset in new[]{Vector3.zero,Vector3.left,Vector3.right,Vector3.up,Vector3.down}){
                var point=bounds.center+Vector3.Scale(offset,bounds.extents)*.6f;
                if(!Physics.Raycast(camera.transform.position,(point-camera.transform.position).normalized,out var hit,5)||
                    hit.collider.GetComponentInParent(expected)!=target)
                    throw new Exception("Selection ray obstructed for "+target.name+" by "+(hit.collider?hit.collider.name:"no hit"));
            }
        }
        static void Control(GridCommand command,int index=0){
            grid.GetComponentsInChildren<PowerGridControl>(true).First(control=>control.command==command&&control.index==index)
                .GetComponent<VillageInteractable>().Activate();
        }

        static IEnumerator Run(){
            var game=UnityEngine.Object.FindFirstObjectByType<VillageGame>();
            Check(game&&game.transitions&&game.player&&game.ui,"Village services initialized with isolated preview save");
            game.NewGame();foreach(var step in Transition(game))yield return step;
            game.state.elder=true;game.ui.Close();game.Enter(4);foreach(var step in Transition(game))yield return step;
            var clockHouse=UnityEngine.Object.FindFirstObjectByType<ClockRhythmHouse>();
            Check(clockHouse&&clockHouse.game==game,"Clock workshop loaded and bound");clock=clockHouse.run;
            var bells=clock.GetComponentsInChildren<ClockBell>(true);Check(bells.Length==3,"Clock workshop retains three bell targets");
            clock.Begin();yield return null;CameraCheck();
            foreach(var bell in bells)RayCheck(bell,bell.GetComponent<Collider>(),game.player.View,typeof(ClockBell));
            Check(true,"Clock bell center and four inset selection rays remain clear");
            var note=clock.Session.Notes[0];double bellDeadline=Time.realtimeSinceStartupAsDouble+12;
            while(clock.Elapsed<note.Time-.025){if(Time.realtimeSinceStartupAsDouble>bellDeadline||!clock.AcceptsInput)throw new TimeoutException("First real clock note did not arrive.");yield return null;}
            int before=clock.Session.Hits;bells.First(bell=>bell.lane==note.Lane).GetComponent<VillageInteractable>().Activate();
            yield return null;Check(clock.Session.Hits==before+1,"Actual timed bell callback hits one note without altering clock or score");
            VillageAutomation.Capture(game,"clock-rhythm-polished-preview");
            foreach(var step in Wait(.8))yield return step;VillageAutomation.Capture(game,"clock-rhythm-polished-overview");
            clock.Stop();game.Exit();foreach(var step in Transition(game))yield return step;clock=null;

            game.Enter(3);foreach(var step in Transition(game))yield return step;
            var gridHouse=UnityEngine.Object.FindFirstObjectByType<PowerGridHouse>();
            Check(gridHouse&&gridHouse.game==game,"Power-grid workshop loaded and bound");grid=gridHouse.run;
            Check(grid.GetComponentsInChildren<PowerGridControl>(true).Length==12,"Grid workshop retains twelve controls");
            grid.Begin();yield return null;CameraCheck();
            foreach(var control in grid.GetComponentsInChildren<PowerGridControl>())RayCheck(control,control.GetComponent<Collider>(),game.player.View,typeof(PowerGridControl));
            Check(true,"Active grid center and four inset selection rays remain clear");
            Control(GridCommand.SelectSource,0);Control(GridCommand.ConnectMachine,0);Control(GridCommand.ConnectMachine,1);
            Control(GridCommand.SelectSource,1);Control(GridCommand.ConnectMachine,2);Control(GridCommand.MainPower);
            foreach(var step in Wait(2.2))yield return step;
            Check(grid.Session.Circuit.Safe,"Panel callbacks establish balanced 5/6 and 4/6 power");
            VillageAutomation.Capture(game,"power-grid-polished-preview");
            Control(GridCommand.SelectSource,0);Control(GridCommand.ConnectMachine,2);foreach(var step in Wait(.5))yield return step;
            Check(grid.Session.Circuit.Load(0)==9&&grid.Session.Circuit.Heat(0)>0&&!grid.Session.Circuit.Tripped(0),"Panel callback creates a real 9/6 overload with rising heat");
            VillageAutomation.Capture(game,"power-grid-polished-overload");
            grid.Stop();game.Exit();foreach(var step in Transition(game))yield return step;grid=null;
            Check(game.player.View&&game.player.View.isActiveAndEnabled,"Player view restored after returning to village");
            Check(!game.state.puzzleSolved[3]&&!game.state.puzzleSolved[4]&&!game.state.clues[3]&&!game.state.clues[4],"Preview leaves challenge and clue rewards unearned");
            Check(errors.Count==0,"No runtime errors during quick visual fixture");
        }
    }
}
