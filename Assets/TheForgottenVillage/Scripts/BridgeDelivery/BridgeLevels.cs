using UnityEngine;

namespace ForgottenVillage {
    // Dimensions are workshop-local metres. Visual builders and runtime share this data.
    public static class BridgeLevels {
        public const int Count = 3;
        public static int DeckCount(int round) => round == 2 ? 3 : 2;
        public static int PieceCount(int round) => round == 0 ? 3 : round == 1 ? 4 : 6;
        public static float Gap(int round) => round == 0 ? .78f : round == 1 ? 1.08f : .99f;
        public static float RequiredLength(int round) => round == 0 ? .8f : round == 1 ? 1.06f : .68f;
        public static float Load(int round) => round == 2 ? 2f : 1f;
        public static float Centre(int round, int index) => round == 0 ? (index == 0 ? -.38f : .38f) :
            round == 1 ? (index == 0 ? -.52f : .52f) : (index - 1) * .64f;
        public static bool IsSupport(int round, int index) => round == 2 && index >= 3;
        public static float PieceLength(int round, int index) => round == 0 ? (index < 2 ? .8f : .48f) :
            round == 1 ? (index < 2 ? 1.10f : .68f) : (index < 3 ? .68f : .56f);
        public static string Name(int round) => round == 0 ? "FIRST CROSSING" : round == 1 ? "MIND THE GAP" : "HEAVY DELIVERY";
        public static string Hint(int round) => round == 0 ? "Place two boards across the blue outlines. Test the cart!" :
            round == 1 ? "The river is wider. Compare each board with its outline." :
            "A heavy load needs a support under every board.";
    }

    public static class BridgeRules {
        public const float SnapDistance = .30f;
        public const float AlignmentTolerance = 20f;
        public static bool Aligned(float yaw) => Mathf.Min(Mathf.Abs(Mathf.DeltaAngle(yaw, 0)),
            Mathf.Abs(Mathf.DeltaAngle(yaw, 180))) <= AlignmentTolerance;
        public static bool Spans(float length, float required) => length + .015f >= required;
    }
}
