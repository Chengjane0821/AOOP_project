using UnityEngine;

namespace ForgottenVillage {
    public enum ActiveWorkshopCommand { Offer, Pause, Restart, Calibrate }
    public sealed class ActiveWorkshopControl : MonoBehaviour {
        public ActiveWorkshopRun run;
        public ActiveWorkshopCommand command;
        public void Use() {
            if (!run || !run.house || !run.house.game || !run.house.game.playing || run.house.game.house != 2) return;
            switch (command) {
                case ActiveWorkshopCommand.Offer: run.house.Offer(); break;
                case ActiveWorkshopCommand.Pause: run.PauseMenu(); break;
                case ActiveWorkshopCommand.Restart: run.Begin(); break;
                case ActiveWorkshopCommand.Calibrate: run.Recalibrate(); break;
            }
        }
    }
}
