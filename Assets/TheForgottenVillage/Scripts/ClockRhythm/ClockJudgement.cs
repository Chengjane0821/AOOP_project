using System;

namespace ForgottenVillage {
    public enum ClockHit { Miss, Good, Perfect }

    // Pure timing rules: shared by live input and deterministic checks.
    public static class ClockJudgement {
        public const double PerfectWindow = .09;
        public const double GoodWindow = .20;
        public const double PreviewSeconds = 1.8;
        public static ClockHit Judge(double error) {
            double distance = Math.Abs(error);
            return distance <= PerfectWindow ? ClockHit.Perfect :
                distance <= GoodWindow ? ClockHit.Good : ClockHit.Miss;
        }
        public static bool Passed(int hits, int total) => total > 0 && hits * 10 >= total * 7;
    }
}
