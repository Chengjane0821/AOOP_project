using System;

namespace ForgottenVillage {
    public enum WorkshopCueKind { Target, Double, DodgeLeft, DodgeRight, Duck }
    public enum WorkshopHit { None, Partial, Success, WrongHand }

    public sealed class WorkshopCue {
        public readonly WorkshopCueKind Kind;
        public readonly int First, Second;
        public readonly double Start, End;
        public bool IsDodge => Kind != WorkshopCueKind.Target && Kind != WorkshopCueKind.Double;
        public WorkshopCue(WorkshopCueKind kind, int first, int second, double start, double end) {
            Kind = kind; First = first; Second = second; Start = start; End = end;
        }
    }

    /// <summary>Pure, seeded rules. Time advances only through Advance, so menus never consume a cue.</summary>
    public sealed class ActiveWorkshopSession {
        public const double Duration = 60;
        public const int Goal = 900;
        public const double DoubleWindow = .40;
        public const double DodgeWarning = .60;
        public const double DodgeHold = .20;
        public readonly WorkshopCue[] Cues;
        public double Elapsed { get; private set; }
        public double Remaining => Math.Max(0, Duration - Elapsed);
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        public int TargetsHit { get; private set; }
        public int Dodges { get; private set; }
        public int Misses { get; private set; }
        public int Revision { get; private set; }
        public bool LastSuccess { get; private set; }
        public string LastFeedback { get; private set; }
        public int Multiplier => Math.Min(4, 1 + Combo / 5);
        public int CurrentIndex { get; private set; }
        public bool CueResolved { get; private set; }
        public int FirstHand { get; private set; } = -1;
        public int FirstTarget { get; private set; } = -1;
        public bool Finished => Elapsed >= Duration;
        public bool Passed => Finished && Score >= Goal;
        public WorkshopCue Current => CurrentIndex < Cues.Length && Elapsed >= Cues[CurrentIndex].Start &&
            Elapsed < Cues[CurrentIndex].End && !Finished ? Cues[CurrentIndex] : null;
        double pairAt, dodgeHeld;

        public ActiveWorkshopSession(int seed) { Cues = Create(seed); }

        static WorkshopCue[] Create(int seed) {
            var random = new Random(seed);
            var cues = new System.Collections.Generic.List<WorkshopCue>();
            double time = .35; int previous = -1;
            while (time < Duration - 1.1) {
                int stage = time < 20 ? 0 : time < 40 ? 1 : 2;
                double length = stage == 0 ? 1.85 : stage == 1 ? 1.55 : 1.30;
                int number = cues.Count;
                WorkshopCueKind kind = number >= 3 && number % 4 == 3 ?
                    (WorkshopCueKind)((int)WorkshopCueKind.DodgeLeft + random.Next(3)) :
                    number >= 2 && (number % 5 == 2 || (stage == 2 && number % 5 == 0)) ? WorkshopCueKind.Double : WorkshopCueKind.Target;
                int first = random.Next(6);
                if (first == previous) first = (first + 1 + random.Next(5)) % 6;
                int second = -1;
                if (kind == WorkshopCueKind.Double) {
                    first = random.Next(2) * 3; // left column, with a separate right-column target
                    second = random.Next(2) * 3 + 2;
                }
                cues.Add(new WorkshopCue(kind, first, second, time, Math.Min(Duration, time + length)));
                previous = first; time += length + .22;
            }
            return cues.ToArray();
        }

        public void Advance(double delta) {
            if (delta <= 0 || double.IsNaN(delta) || double.IsInfinity(delta) || Finished) return;
            Elapsed = Math.Min(Duration, Elapsed + delta);
            while (CurrentIndex < Cues.Length && Elapsed >= Cues[CurrentIndex].End) {
                if (!CueResolved) Resolve(false, Cues[CurrentIndex].IsDodge ? "OUCH! Watch the next warning" : "MISSED / reach for the next target");
                CurrentIndex++; CueResolved = false; FirstHand = FirstTarget = -1; dodgeHeld = 0;
            }
            if (FirstHand >= 0 && Elapsed - pairAt > DoubleWindow) FirstHand = FirstTarget = -1;
        }

        public WorkshopHit Hit(int target, int hand) {
            var cue = Current;
            if (cue == null || CueResolved || cue.IsDodge || target < 0 || target >= 6 || hand < 0 || hand > 1) return WorkshopHit.None;
            if (cue.Kind == WorkshopCueKind.Target) {
                if (target != cue.First) return WorkshopHit.None;
                TargetsHit++; Resolve(true, "GREAT REACH!"); return WorkshopHit.Success;
            }
            if (target != cue.First && target != cue.Second) return WorkshopHit.None;
            if (FirstHand < 0 || Elapsed - pairAt > DoubleWindow) {
                FirstHand = hand; FirstTarget = target; pairAt = Elapsed; return WorkshopHit.Partial;
            }
            if (target == FirstTarget) return WorkshopHit.None;
            if (hand == FirstHand) return WorkshopHit.WrongHand;
            TargetsHit += 2; Resolve(true, "BOTH HANDS! + DOUBLE"); return WorkshopHit.Success;
        }

        // Head displacement is relative to the calibrated neutral pose, in the workshop's axes.
        public bool ObserveDodge(float offsetX, float offsetY, double delta) {
            var cue = Current;
            if (cue == null || !cue.IsDodge || CueResolved || Elapsed < cue.Start + DodgeWarning || delta <= 0 || delta > .25 ||
                double.IsNaN(delta) || double.IsInfinity(delta) || float.IsNaN(offsetX) || float.IsNaN(offsetY) ||
                float.IsInfinity(offsetX) || float.IsInfinity(offsetY)) return false;
            bool safe = cue.Kind == WorkshopCueKind.DodgeLeft ? offsetX <= -.16f :
                cue.Kind == WorkshopCueKind.DodgeRight ? offsetX >= .16f : offsetY <= -.18f;
            dodgeHeld = safe ? dodgeHeld + delta : 0;
            if (dodgeHeld < DodgeHold) return false;
            Dodges++; Resolve(true, cue.Kind == WorkshopCueKind.Duck ? "NICE DUCK!" : "NICE DODGE!"); return true;
        }

        void Resolve(bool success, string text) {
            CueResolved = true; LastSuccess = success; LastFeedback = text; Revision++;
            if (success) {
                int value = Cues[CurrentIndex].Kind == WorkshopCueKind.Double ? 120 : Cues[CurrentIndex].IsDodge ? 80 : 60;
                Score += value * Multiplier; Combo++; BestCombo = Math.Max(BestCombo, Combo);
            } else {
                Combo = 0; Misses++;
                if (Cues[CurrentIndex].IsDodge) Score = Math.Max(0, Score - 40);
            }
        }
    }
}
