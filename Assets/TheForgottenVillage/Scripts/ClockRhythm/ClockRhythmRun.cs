using UnityEngine;
namespace ForgottenVillage {
    public sealed class ClockRhythmRun : MonoBehaviour {
        public ClockRhythmHouse house;
        public ClockRhythmTuning tuning;
        public ClockRhythmView view;
        public ClockRhythmAudio sound;
        public Transform playingPoint;
        public ClockSession Session { get; private set; }
        public double Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool AcceptsInput => Running && house && house.game && house.game.playing &&
            house.game.house == 4 && !house.game.ui.IsOpen;
        double lastDsp;
        int lastBeat = -1;
        readonly double[] lastStrike = { -1, -1, -1 };
        string previousPrompt;
        public void Begin() {
            if (!house || !house.game || !house.game.playing || house.game.house != 4 || !house.game.state.elder) return;
            Stop();
            Session = new ClockSession(ClockChart.Create()); Elapsed = 0; lastBeat = -1;
            for (int i = 0; i < 3; i++) lastStrike[i] = -1;
            previousPrompt = house.game.player.ActivityPrompt;
            house.game.player.ActivityPrompt = "A / S / D: bells 1 / 2 / 3 | Gold mark: strike | Esc: pause";
            house.game.player.ActivityLocksMovement = true;
            house.game.player.Teleport(playingPoint.position, playingPoint.eulerAngles.y);
            house.game.ui.Close(); Running = true; lastDsp = AudioSettings.dspTime;
        }
        void Update() {
            double now = AudioSettings.dspTime, delta = System.Math.Max(0, now - lastDsp); lastDsp = now;
            if (Running && (!house.game.playing || house.game.house != 4)) { Stop(); return; }
            if (AcceptsInput) {
                Elapsed += delta;
                Session.Advance(Elapsed - tuning.inputOffsetSeconds);
                int beat = (int)(Elapsed / ClockChart.Beat);
                if (beat != lastBeat) { lastBeat = beat; sound.Tick(tuning.volume); }
                if (Session.Finished) { Finish(); return; }
            }
            if (view && tuning) view.Render(Session, Elapsed, tuning, !Running && house && house.game && house.game.state.clockRhythmWon);
        }
        public void Strike(int lane) {
            if (!AcceptsInput || lane < 0 || lane > 2 || Elapsed - lastStrike[lane] < tuning.strikeCooldown) return;
            lastStrike[lane] = Elapsed;
            var judgement = Session.Strike(lane, Elapsed - tuning.inputOffsetSeconds);
            sound.Bell(lane, tuning.volume); ClockBell.Haptic(judgement != ClockHit.Miss);
            view.Feedback(judgement == ClockHit.Perfect ? "PERFECT!" : judgement == ClockHit.Good ? "GOOD" : "OFF BEAT");
        }
        void Finish() {
            bool passed = Session.Passed; Stop();
            if (passed) { house.Win(); return; }
            var ui = house.game.ui;
            ui.Show("The clocks need another try", "Hits " + Session.Hits + "/" + Session.Notes.Length +
                " | Best combo " + Session.BestCombo + "\nHit at least 70% (23 notes), with no more than 8 extra strikes.\nWatch a hand reach the gold mark at twelve o'clock.");
            ui.Button("Retry", Begin); ui.Button("Leave clock", ui.Close);
        }
        public void PauseMenu() {
            if (!Running) return;
            sound.Silence();
            var ui = house.game.ui;
            ui.Show("Clock repair / Paused", "Musical time is frozen.\nA / S / D or select bells 1 / 2 / 3.\nStrike when the moving hand reaches the gold mark.");
            ui.Button("Resume", () => { lastDsp = AudioSettings.dspTime; ui.Close(); });
            ui.Button("Restart", Begin); ui.Button("Leave clock", () => { Stop(); ui.Close(); });
            ui.Button("Offset -10 ms", () => Calibrate(-.01f));
            ui.Button("Offset +10 ms", () => Calibrate(.01f));
        }
        void Calibrate(float change) {
            tuning.inputOffsetSeconds = Mathf.Clamp(tuning.inputOffsetSeconds + change, -.25f, .25f);
            PauseMenu(); house.game.ui.Toast("Input offset: " + Mathf.RoundToInt(tuning.inputOffsetSeconds * 1000) + " ms (positive allows later input)");
        }
        public void Stop() {
            if (sound) sound.Silence();
            if (Running && house && house.game && house.game.player) {
                house.game.player.ActivityLocksMovement = false;
                house.game.player.ActivityPrompt = previousPrompt;
            }
            Running = false;
        }
        void OnDisable() { Stop(); }
    }
}
