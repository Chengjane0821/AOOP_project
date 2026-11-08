using System;
using System.Globalization;

namespace ForgottenVillage {
    public static class ObjectQuests {
        public static readonly string[] Categories = { "book", "bottle", "mouse", "cell phone", "clock" };
        public static readonly string[] Roles = { "research notes", "sample container", "workstation tool", "diagnostic record", "experiment timeline" };
        public const float MinimumConfidence = .70f;
        public const int MinimumFrames = 3;
        public static bool ValidId(string value) => value != null && value.Length == 32 && Guid.TryParseExact(value, "N", out _);
        public static bool Accept(RecognitionResult result, int house, string request, bool simulation) {
            return result != null && house >= 0 && house < 5 && ValidId(request) &&
                result.requestId == request && result.questIndex == house && result.status == "verified" &&
                result.objectName == Categories[house] && result.simulated == simulation && result.ToEvidence().ValidFor(house);
        }
    }
    [Serializable]
    public class ObjectEvidence {
        public string requestId, objectName, snapshot, capturedUtc;
        public float confidence;
        public int frames;
        public bool simulated;
        public bool ValidFor(int house) {
            return house >= 0 && house < 5 && ObjectQuests.ValidId(requestId) &&
                objectName == ObjectQuests.Categories[house] && !float.IsNaN(confidence) &&
                confidence >= ObjectQuests.MinimumConfidence && confidence <= 1 && frames >= ObjectQuests.MinimumFrames &&
                DateTimeOffset.TryParse(capturedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _) &&
                (simulated ? snapshot == "" : snapshot == requestId + ".jpg");
        }
    }
    [Serializable]
    public class RecognitionResult {
        public string requestId, status, objectName, snapshot, capturedUtc, message;
        public int questIndex, frames;
        public float confidence;
        public bool simulated;
        public ObjectEvidence ToEvidence() => new ObjectEvidence { requestId = requestId, objectName = objectName,
            snapshot = snapshot, capturedUtc = capturedUtc, confidence = confidence, frames = frames, simulated = simulated };
    }
}
