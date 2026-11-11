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
        // Single notes only: usable with one controller or one hand.
        public static ClockNote[] Create() {
            int[] lanes = { 0,0,1,1,2,2,1,0, 0,1,2,1,0,2,0,1,
                2,1,0,1,2,0,2,1, 0,1,2,0,2,1,0,2 };
            var notes = new ClockNote[lanes.Length];
            for (int i = 0; i < lanes.Length; i++)
                notes[i] = new ClockNote(lanes[i], (CountIn + i * 2) * Beat);
            return notes;
        }
    }
}
