using UnityEngine;
namespace ForgottenVillage {
    public enum GridCommand { Offer, SelectSource, ConnectMachine, MainPower, ResetBreaker, Backup }
    public sealed class PowerGridControl : MonoBehaviour {
        public PowerGridRun run;
        public GridCommand command;
        public int index;
        public void Use() {
            if(!run)return;
            if(command==GridCommand.Offer){run.house.Offer();return;}
            if(!run.AcceptsInput)return;
            switch(command) {
                case GridCommand.SelectSource:run.SelectSource(index);break;
                case GridCommand.ConnectMachine:run.ConnectMachine(index);break;
                case GridCommand.MainPower:run.TogglePower();break;
                case GridCommand.ResetBreaker:run.ResetBreaker(index);break;
                case GridCommand.Backup:run.ToggleBackup();break;
            }
        }
    }
}
