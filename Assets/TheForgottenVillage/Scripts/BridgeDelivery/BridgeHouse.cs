using UnityEngine;

namespace ForgottenVillage {
    public sealed class BridgeHouse : MonoBehaviour {
        public VillageGame game;
        public BridgeRun run;
        public void Bind(VillageGame owner) {
            game = owner;
            if (run) { run.house = this; foreach (var piece in run.pieces) if (piece) { piece.run = run; piece.SetAvailable(false); } }
        }
        public void Offer() {
            if (!game || !game.playing || game.house != 2 || !game.state.elder || !run) return;
            if (run.Running) { run.PauseMenu(); return; }
            if (game.state.carpenterBridgeWon && !game.state.carpenterMemoryRecovered) { Recovery(); return; }
            game.ui.Show("Carpenter / Bridge Delivery", game.state.carpenterBridgeWon ?
                "Our deliveries are moving again. Want to build another bridge?\nMy recovered analog record is ready for your investigation." :
                "Help my little cart cross the river in three deliveries.\nGrab, rotate and place boards on the blue outlines. Then TEST your bridge!\nMouse: drag | Q / E: rotate | Space: test | R: return part\nVR: grab and release boards; select the workshop controls.");
            game.ui.Button("Build a bridge", run.Begin);
            if (game.state.carpenterMemoryRecovered) game.ui.Button("Investigate record", () => game.ui.ObjectQuest(2));
            game.ui.Button("Maybe later", game.ui.Close);
        }
        public void Win() {
            if (!game || !game.playing || game.house != 2 || !run || !run.Won) return;
            game.state.carpenterBridgeWon = true; game.state.puzzleSolved[2] = true; game.Commit();
            if (game.audioFeedback) game.audioFeedback.Success(); Recovery();
        }
        public void Recovery() {
            if (!game || !game.playing || game.house != 2 || !game.state.carpenterBridgeWon) return;
            game.ui.Show("Carpenter / I remember", "Those little wheels... we built these bridges together. You always tested the load before trusting a design.\nI kept the analog front-end record from your experiment. Its common-source stage inverted the signal; transconductance and the drain load set its gain.\nLet us recover the record together.");
            game.ui.Button("Remember together", () => {
                if (!game || game.house != 2 || !game.state.carpenterBridgeWon) return;
                game.state.carpenterMemoryRecovered = true; game.Commit(); game.ui.ObjectQuest(2);
            });
        }
        void OnDisable() { if (run) run.Stop(); }
    }
}
