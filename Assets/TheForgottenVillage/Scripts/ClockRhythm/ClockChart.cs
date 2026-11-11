namespace ForgottenVillage {
    public readonly struct ClockNote {
        public readonly int Lane;
        public readonly double Time;
        public ClockNote(int lane, double time) { Lane = lane; Time = time; }
    }
    public static class ClockChart {
        public const int BeatsPerMinute = 90;
        public const double Beat = 60.0 / BeatsPerMinute;
        public const int CountIn = 4;
        public const int LaneCount = 6;
        public const int NoteCount = 48;
        public const int RequiredHits = (NoteCount * 7 + 9) / 10;
        public const int MaxStrays = NoteCount / 4;
        // Three phrases alternate between the two rows, then build to a finale.
        // Single notes and at least one beat between notes retain one-hand play.
        public static ClockNote[] Create() {
            int[] lanes = {
                0,1,2,0,1,2,3,4,5,3,4,5,0,3,1,4,
                0,3,1,4,2,5,2,5,1,4,0,3,2,1,4,5,
                0,3,1,4,2,5,5,2,4,1,3,0,5,4,3,2
            };
            var notes = new ClockNote[lanes.Length];
            double beat = CountIn;
            for (int i = 0; i < lanes.Length; i++) {
                notes[i] = new ClockNote(lanes[i], beat * Beat);
                beat += i < 16 ? 1.5 : i < 32 ? (i % 2 == 0 ? 1 : 1.5) : 1;
            }
            return notes;
        }
        public static string PhraseName(int resolved) => resolved < 16 ? "01 / FIND THE BELLS" :
            resolved < 32 ? "02 / HAND TO HAND" : "03 / CLOCKWORK FINALE";
    }
}
