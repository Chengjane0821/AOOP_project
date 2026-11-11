using System;

namespace ForgottenVillage {
    public sealed class ClockSession {
        public readonly ClockNote[] Notes;
        readonly bool[] resolved;
        public int Hits { get; private set; }
        public int Perfects { get; private set; }
        public int Misses { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int Strays { get; private set; }
        public int Resolved => Hits + Misses;
        public bool Finished => Resolved == Notes.Length;
        public bool Passed => Finished && ClockJudgement.Passed(Hits, Notes.Length) && Strays <= Notes.Length / 4;
        public ClockSession(ClockNote[] notes) { Notes = notes; resolved = new bool[notes.Length]; }
        public bool IsResolved(int index) => resolved[index];
        public ClockHit Strike(int lane, double time) {
            int best = -1; double distance = ClockJudgement.GoodWindow;
            for (int i = 0; i < Notes.Length; i++) {
                double error = Math.Abs(Notes[i].Time - time);
                if (!resolved[i] && Notes[i].Lane == lane && error <= distance) { best = i; distance = error; }
            }
            if (best < 0) { Combo = 0; Strays++; return ClockHit.Miss; }
            resolved[best] = true; Hits++; Combo++; BestCombo = Math.Max(BestCombo, Combo);
            var result = ClockJudgement.Judge(Notes[best].Time - time);
            if (result == ClockHit.Perfect) Perfects++;
            return result;
        }
        public void Advance(double time) {
            for (int i = 0; i < Notes.Length; i++)
                if (!resolved[i] && time > Notes[i].Time + ClockJudgement.GoodWindow) {
                    resolved[i] = true; Misses++; Combo = 0;
                }
        }
    }
}
