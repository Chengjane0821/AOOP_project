using UnityEngine;
namespace ForgottenVillage {
    public sealed class ClockRhythmHouse : MonoBehaviour {
        public VillageGame game;
        public ClockRhythmRun run;
        public void Bind(VillageGame owner) { game = owner; run.house = this; }
        public void Offer() {
            if (!game || game.house != 4 || !game.playing || !game.state.elder) return;
            if (run.Running) { run.PauseMenu(); return; }
            if (game.state.clockRhythmWon && !game.state.clockMemoryRecovered) { Recovery(); return; }
            game.ui.Show("Clockmaker / Rhythm repair", game.state.clockRhythmWon ?
                "The clocks are alive again. Thank you.\nYou can replay, or continue investigating my timing record." :
                "Every clock stopped on the night we forgot. Help me bring them back.\n\nWatch the three clock hands. Strike a bell at the gold mark.\nA / S / D on desktop; select numbered bells in VR.\nHit 23 of 32 notes; no more than 8 extra strikes. About 45 seconds.");
            game.ui.Button("Repair the clocks", run.Begin);
            if (game.state.clockMemoryRecovered) game.ui.Button("Investigate record", () => game.ui.ObjectQuest(4));
            game.ui.Button("Maybe later", game.ui.Close);
        }
        public void Win() {
            if (!game || game.house != 4 || run.Session == null || !run.Session.Passed) return;
            game.state.clockRhythmWon = true; game.state.puzzleSolved[4] = true;
            game.Commit(); if (game.audioFeedback) game.audioFeedback.Success(); Recovery();
        }
        public void Recovery() {
            if (!game.state.clockRhythmWon) return;
            game.ui.Show("Clockmaker / I remember", "That sound... we used to repair these clocks together.\nYou were the researcher. I kept a timing report from your experiment.\nIts negative slack is a risk, but it cannot prove what caused the incident.");
            game.ui.Button("Remember together", () => {
                game.state.clockMemoryRecovered = true; game.Commit(); game.ui.ObjectQuest(4);
            });
        }
    }
}
