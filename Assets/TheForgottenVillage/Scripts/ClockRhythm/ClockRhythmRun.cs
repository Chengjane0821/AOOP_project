using UnityEngine;
namespace ForgottenVillage {
    public sealed class ClockRhythmRun : MonoBehaviour {
        public ClockRhythmHouse house;
        public ClockRhythmTuning tuning;
        public ClockRhythmView view;
        public ClockRhythmAudio sound;
        public Transform playingPoint;
        public Transform vrPlayingPoint;
        public ClockSession Session { get; private set; }
        public double Elapsed { get; private set; }
        public bool Running { get; private set; }
        public bool AcceptsInput => Running && house && house.game && house.game.playing &&
            house.game.house == 4 && !house.game.ui.IsOpen;
        double lastTime;
        int lastBeat = -1;
        readonly double[] lastStrike = new double[ClockChart.LaneCount];
        string previousPrompt;
        bool previousLock, previousPointer, previousJournal, ownsPlayer;
        public void Begin() {
            if (!house || !house.game || !house.game.playing || house.game.house != 4 || !house.game.state.elder) return;
            Stop();
            Session = new ClockSession(ClockChart.Create()); Elapsed = 0; lastBeat = -1;
            for (int i = 0; i < lastStrike.Length; i++) lastStrike[i] = -1;
            var player = house.game.player;
            previousPrompt = player.ActivityPrompt;
            previousLock = player.ActivityLocksMovement; previousPointer = player.ActivityUsesPointer;
            previousJournal = player.ActivityConsumesJournalKey; ownsPlayer = true;
            player.ActivityPrompt = "";
            player.ActivityLocksMovement = true; player.ActivityUsesPointer = false;
            player.ActivityConsumesJournalKey = true;
            var point = player.IsVR && vrPlayingPoint ? vrPlayingPoint : playingPoint;
            if (point) player.Teleport(point.position, point.eulerAngles.y);
            var input = GetComponent<ClockRhythmInput>(); if (input) input.ResetContacts();
            house.game.ui.Close();house.game.ui.notice.text=""; Running = true; lastTime = Time.realtimeSinceStartupAsDouble;
        }
        void Update() {
            double now = Time.realtimeSinceStartupAsDouble, delta = System.Math.Max(0, now - lastTime); lastTime = now;
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
            if (!AcceptsInput || lane < 0 || lane >= ClockChart.LaneCount || Elapsed - lastStrike[lane] < tuning.strikeCooldown) return;
            lastStrike[lane] = Elapsed;
            var judgement = Session.Strike(lane, Elapsed - tuning.inputOffsetSeconds);
            sound.Bell(lane, tuning.volume); ClockBell.Haptic(judgement != ClockHit.Miss);
            view.Feedback(judgement == ClockHit.Perfect ? "PERFECT!" : judgement == ClockHit.Good ? "GOOD" : "OFF BEAT");
            view.Pulse(lane,judgement);
        }
        void Finish() {
            bool passed = Session.Passed; Stop();
            if (passed) { house.Win(); return; }
            var ui = house.game.ui;
            ui.Show("The clocks need another try", "Hits " + Session.Hits + "/" + Session.Notes.Length +
                " | Best combo " + Session.BestCombo + "\nHit at least " + Session.RequiredHits + " notes, with no more than " + Session.MaxStrays + " extra strikes.\nWatch a hand reach the gold mark at twelve o'clock.");
            ui.Button("Retry", Begin); ui.Button("Leave clock", ui.Close);
        }
        public void PauseMenu() {
            if (!Running) return;
            sound.Silence();
            var ui = house.game.ui;
            ui.Show("Clock repair / Paused", "Musical time is frozen.\nTop row A / S / D; lower row J / K / L.\nVR: touch the bells with your controllers, or select them.\nStrike when the moving hand reaches the gold mark.");
            ui.Button("Resume", () => { lastTime = Time.realtimeSinceStartupAsDouble; ui.Close(); });
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
            if (ownsPlayer && house && house.game && house.game.player) {
                house.game.player.ActivityLocksMovement = previousLock;
                house.game.player.ActivityUsesPointer = previousPointer;
                house.game.player.ActivityConsumesJournalKey = previousJournal;
                house.game.player.ActivityPrompt = previousPrompt;
            }
            Running = false; ownsPlayer = false;
        }
        void OnDisable() { Stop(); }
    }
}
