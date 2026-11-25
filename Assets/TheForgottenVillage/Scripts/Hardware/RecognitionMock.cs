using System;
namespace ForgottenVillage {
    public enum MockOutcome { Success, WrongObject, NoObject, LowConfidence, Timeout, ServerError }
    public static class RecognitionMock {
        public static RecognitionResult Create(MockOutcome outcome, int house, string requestId, string category) {
            var result = new RecognitionResult { requestId = requestId, questIndex = house,
                objectName = category, status = "success", confidence = .99f, frames = 3,
                capturedUtc = DateTimeOffset.UtcNow.ToString("O"), snapshot = "", simulated = true };
            switch (outcome) {
                case MockOutcome.WrongObject: result.objectName = "wrong object"; break;
                case MockOutcome.LowConfidence: result.confidence = .2f; break;
                case MockOutcome.NoObject: result.status = "failed"; result.message = "No object detected"; break;
                case MockOutcome.Timeout: result.status = "timeout"; result.message = "Simulated timeout"; break;
                case MockOutcome.ServerError: result.status = "model_error"; result.message = "Simulated server error"; break;
            }
            return result;
        }
    }
}
