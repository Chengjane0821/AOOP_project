using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridHouse : MonoBehaviour {
        public VillageGame game;
        public PowerGridRun run;
        public void Bind(VillageGame owner){Unsubscribe();game=owner;run.house=this;Subscribe();run.RefreshIdle();}
        void Subscribe(){if(game&&game.hardware){game.hardware.Diagnostic-=HardwareButton;game.hardware.Diagnostic+=HardwareButton;}}
        void Unsubscribe(){if(game&&game.hardware)game.hardware.Diagnostic-=HardwareButton;}
        void HardwareButton(){if(run&&run.AcceptsInput)run.TogglePower();}
        void OnEnable(){Subscribe();}
        void OnDisable(){Unsubscribe();if(run)run.Stop();}
        public void Offer(){
            if(!game||game.house!=3||!game.playing||!game.state.elder)return;
            if(run.Running){run.PauseMenu();return;}
            if(game.state.technicianGridWon&&!game.state.technicianMemoryRecovered){Recovery();return;}
            game.ui.Show("Technician / Power-grid emergency",game.state.technicianGridWon?
                "You brought the workshop back to life. My recovered diagnostic record may help your investigation.\nYou can replay the emergency panel anytime.":
                "The workshop has gone dark. Help me restore its three circuits.\nSelect A or B, then a machine socket. Keep each load within capacity.\nIf a breaker trips, move loads and RESET. Round 3 needs BACKUP.\nMouse / VR selection, or A/S, 1-5, Space, R and B.");
            game.ui.Button("Restore power",run.Begin);
            if(game.state.technicianMemoryRecovered)game.ui.Button("Investigate record",()=>game.ui.ObjectQuest(3));
            game.ui.Button("Maybe later",game.ui.Close);
        }
        public void Win(){
            if(!game||!game.playing||game.house!=3||run.Session==null||!run.Session.Won)return;
            game.state.technicianGridWon=true;game.state.puzzleSolved[3]=true;game.Commit();
            if(game.audioFeedback)game.audioFeedback.Success();Recovery();
        }
        public void Recovery(){
            if(!game||game.house!=3||!game.state.technicianGridWon)return;
            game.ui.Show("Technician / I remember","The machines... you brought them back. We repaired this workshop together.\nThe restored controller log records unsafe=true, yet the old software said RUN.\nA repaired reference-based controller correctly says STOP. I can recover both tests for you.");
            game.ui.Button("Recover diagnostic log",()=>{
                game.state.faultyTest=Engineering.ControllerAllowsRun(false,true);
                game.state.fixedTest=!Engineering.ControllerAllowsRun(true,true);
                game.state.technicianMemoryRecovered=true;game.Commit();game.ui.ObjectQuest(3);
            });
        }
    }
}
