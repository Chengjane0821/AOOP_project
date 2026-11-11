using UnityEngine;

namespace ForgottenVillage {
    public enum BridgeCommand { Offer, Test, Retry, Reset }
    public sealed class BridgeControl : MonoBehaviour {
        public BridgeRun run;
        public BridgeCommand command;
        public void Use() {
            if (!run || !run.house || !run.house.game || !run.house.game.playing || run.house.game.house != 2) return;
            switch (command) {
                case BridgeCommand.Offer: run.house.Offer(); break;
                case BridgeCommand.Test: run.StartTrial(); break;
                case BridgeCommand.Retry: if (run.Running) run.RetryTrial(); break;
                case BridgeCommand.Reset: if (run.AcceptsInput) run.ResetBuild(); break;
            }
        }
    }
}
