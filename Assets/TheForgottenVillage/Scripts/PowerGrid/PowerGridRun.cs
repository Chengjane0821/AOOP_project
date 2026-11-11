using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridRun : MonoBehaviour {
        public PowerGridHouse house;
        public PowerGridTuning tuning;
        public PowerGridView view;
        public PowerGridAudio sound;
        public Transform playingPoint;
        public PowerGridSession Session{get;private set;}
        public int SelectedSource{get;private set;}
        public bool Running{get;private set;}
        public bool AcceptsInput=>Running&&house&&house.game&&house.game.playing&&house.game.house==3&&
            !house.game.ui.IsOpen&&Session!=null&&!Session.RoundComplete&&!Session.Failed;
        double lastTime;
        string previousPrompt;
        bool roundShown;
        public void RefreshIdle(){if(Session==null)Session=new PowerGridSession();Render(0);}
        public void Begin(){if(!Allowed())return;Stop();Session=new PowerGridSession();SelectedSource=0;StartRound();}
        bool Allowed()=>house&&house.game&&house.game.playing&&house.game.house==3&&house.game.state.elder;
        void StartRound(){
            if(!Allowed())return;
            if(!Running)previousPrompt=house.game.player.ActivityPrompt;
            Running=true;roundShown=false;lastTime=Time.realtimeSinceStartupAsDouble;
            house.game.player.ActivityLocksMovement=true;house.game.player.ActivityUsesPointer=true;
            house.game.player.ActivityPrompt="A/S: source | 1-5: cable | Space: power | R: reset | B: backup | Esc: pause";
            house.game.player.Teleport(playingPoint.position,playingPoint.eulerAngles.y);house.game.ui.Close();house.game.ui.notice.text="";Render(0);
        }
        void Update(){
            double now=Time.realtimeSinceStartupAsDouble,delta=System.Math.Max(0,now-lastTime);lastTime=now;
            if(Running&&!Allowed()){Stop();return;}
            if(AcceptsInput){
                int trips=Session.Circuit.Trips;Session.Advance(delta);
                if(Session.Circuit.Trips>trips){sound.Fault(tuning.volume);view.Feedback("BREAKER TRIPPED - reroute loads, then RESET");}
                if(Session.Failed){Stop();ShowFailure();}
                else if(Session.Won){Stop();house.Win();}
                else if(Session.RoundComplete&&!roundShown){roundShown=true;ShowNext();}
            }
            Render((float)(Running&&!house.game.ui.IsOpen?delta:0));
        }
        void Render(float delta){if(view&&house&&house.game)view.Render(Session,SelectedSource,tuning,Running,house.game.state.technicianGridWon,delta);}
        public void SelectSource(int source){if(!AcceptsInput||source<0||source>1)return;SelectedSource=source;Click("Generator "+(source==0?"A":"B")+" selected");}
        public void ConnectMachine(int machine){
            if(!AcceptsInput||machine<0||machine>=Session.Circuit.MachineCount)return;
            int target=Session.Circuit.SourceFor(machine)==SelectedSource?-1:SelectedSource;
            Session.Circuit.Connect(machine,target);Click(target<0?"Cable disconnected":"Machine "+(machine+1)+" connected to "+(target==0?"A":"B"));
        }
        public void TogglePower(){if(!AcceptsInput)return;Session.Circuit.TogglePower();Click(Session.Circuit.MainPower?"Main power ON":"Main power OFF");}
        public void ResetBreaker(int source){
            if(!AcceptsInput)return;
            if(Session.Circuit.ResetBreaker(source))Click("Breaker reset");
            else {sound.Fault(tuning.volume);view.Feedback("MOVE LOADS FIRST - generator is still overloaded");}
        }
        public void ToggleBackup(){if(!AcceptsInput)return;if(Session.Circuit.ToggleBackup())Click("Backup "+(Session.Circuit.BackupOnline?"ON":"OFF"));else view.Feedback("Backup is needed in round 3");}
        void Click(string message){sound.Click(tuning.volume);view.Feedback(message);Render(0);}
        void ShowNext(){sound.Restored(tuning.volume);var ui=house.game.ui;ui.Show("Circuit restored",Session.Circuit.Level.Name+"\nAll machines held stable for four seconds.");ui.Button("Next circuit",Next);ui.Button("Leave panel",()=>{Stop();ui.Close();});}
        public void Next(){if(!Allowed()||Session==null||!Session.Next())return;SelectedSource=0;StartRound();}
        void ShowFailure(){var ui=house.game.ui;ui.Show("Emergency timed out","The machines need another try. Balance the load before turning on power.\nReduce overloaded loads before resetting a tripped breaker.");ui.Button("Retry circuit",Retry);ui.Button("Leave panel",ui.Close);}
        public void Retry(){if(!Allowed()||Session==null)return;Session.Retry();SelectedSource=0;StartRound();}
        public void PauseMenu(){
            if(!Running)return;if(Session.RoundComplete){ShowNext();return;}
            sound.Silence();var ui=house.game.ui;
            ui.Show("Power-grid / Paused","Heat, machines and the emergency timer are paused.\nA/S selects a generator; 1-5 connects or disconnects machines.\nSpace: main power | R: selected breaker reset | B: backup");
            ui.Button("Resume",()=>{lastTime=Time.realtimeSinceStartupAsDouble;ui.Close();});
            ui.Button("Retry circuit",Retry);ui.Button("Restart all",Begin);ui.Button("Leave panel",()=>{Stop();ui.Close();});
        }
        public void Stop(){
            if(sound)sound.Silence();
            if(Running&&house&&house.game&&house.game.player){house.game.player.ActivityLocksMovement=false;house.game.player.ActivityUsesPointer=false;house.game.player.ActivityPrompt=previousPrompt;}
            Running=false;
        }
        void OnDisable(){Stop();}
    }
}
