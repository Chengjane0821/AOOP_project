using UnityEngine;

namespace ForgottenVillage {
    public sealed class ActiveWorkshopHouse : MonoBehaviour {
        public VillageGame game;
        public ActiveWorkshopRun run;
        public void Bind(VillageGame owner) { game = owner; if (run) run.house = this; }
        public void Offer() {
            if (!game || !run || !game.playing || game.house != 2 || !game.state.elder) return;
            if (run.Running) { run.PauseMenu(); return; }
            if (game.state.carpenterArcadeWon && !game.state.carpenterMemoryRecovered) { Recovery(); return; }
            game.ui.Show("Carpenter / Quick Hands Workshop", "Hit the lit targets, use BOTH hands on a pair, and dodge the striped boards.\n60 seconds / goal " + ActiveWorkshopSession.Goal + " points / best " + game.state.carpenterBestScore +
                "\nVR: touch targets with controllers; lean left/right or lower your head at the warning.\nDesktop: mouse left/right = hands; A/D = lean; S = duck. Press both mouse buttons on a glowing pair.");
            game.ui.Button("Let's play!", run.Begin);
            if (game.state.carpenterMemoryRecovered) game.ui.Button("Investigate record", () => game.ui.ObjectQuest(2));
            game.ui.Button("Maybe later", game.ui.Close);
        }
        public void Win() {
            if (!game || !run || !game.playing || game.house != 2 || run.Session == null || !run.Session.Passed) return;
            game.state.carpenterArcadeWon = true; game.state.puzzleSolved[2] = true; game.Commit();
            if (game.audioFeedback) game.audioFeedback.Success(); Recovery();
        }
        public void Recovery() {
            if (!game || !game.state.carpenterArcadeWon || game.house != 2) return;
            game.ui.Show("Carpenter / I remember", "We used to play this game after working in the shop. You always tried to beat my score.\nI remember your experiment now. I kept its analog front-end record: the common-source stage inverted the signal, and transconductance with the drain load set its gain.");
            game.ui.Button("Remember together", () => {
                if (!game || game.house != 2 || !game.state.carpenterArcadeWon) return;
                game.state.carpenterMemoryRecovered = true; game.Commit(); game.ui.ObjectQuest(2);
            });
        }
        void OnDisable() { if (run) run.Stop(); }
    }
}
