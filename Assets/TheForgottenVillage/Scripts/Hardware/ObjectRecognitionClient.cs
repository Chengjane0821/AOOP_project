using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ForgottenVillage {
    public class ObjectRecognitionClient : MonoBehaviour {
        public VillageGame game;
        public bool simulation = false;
        public string endpoint = "http://raspberrypi.local:8765", accessToken = "", status = "LIVE camera: connection not tested";
        [Range(2, 15)] public int requestTimeout = 5;
        [Range(100, 180)] public int verificationTimeout = 120;
        public MockOutcome mockOutcome = MockOutcome.Success;
        public event Action<RecognitionResult> VerificationSucceeded;
        public event Action<string> VerificationFailed;
        public int ActiveQuest { get; private set; } = -1;
        public string RequestId { get; private set; }
        public bool Waiting => ActiveQuest >= 0;
        Coroutine polling, healthCheck;
        UnityWebRequest current, healthRequest;
        [Serializable] class Request { public string request_id, house_id, expected_object; }
        [Serializable] class Health { public string status, message; public bool ready, camera, model, busy; public int protocol; }
        void Awake() {
            var configured = Environment.GetEnvironmentVariable("FV_CAMERA_URL");
            if (!string.IsNullOrWhiteSpace(configured)) endpoint = configured;
            accessToken = Environment.GetEnvironmentVariable("FV_CAMERA_TOKEN") ?? accessToken;
            // Live is the startup default even if an old scene serialized simulation=true.
            simulation = false;
            VillageStorage.Simulation = false;
            Application.runInBackground = true;
        }
        public void SetSimulation(bool value) {
            if (game && game.house >= 0) { status = "Return to the village before changing camera mode."; return; }
            Cancel(); simulation = value; VillageStorage.Simulation = value;
            if (game) { game.state = VillageStorage.Load(); if (game.ui) game.ui.RefreshTracker(); }
            status = value ? "MOCK MODE: separate test save; no real camera" : "LIVE MODE: restored real-camera save";
        }
        public void Cancel() {
            ActiveQuest = -1; RequestId = null;
            if (current != null) { current.Abort(); current.Dispose(); current = null; }
            if (polling != null) StopCoroutine(polling);
            polling = null;
        }
        void OnDisable() {
            Cancel();
            if (healthRequest != null) { healthRequest.Abort(); healthRequest.Dispose(); healthRequest = null; }
            if (healthCheck != null) StopCoroutine(healthCheck);
            healthCheck = null;
        }
        bool Configured() {
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
                (uri.Scheme == "http" || uri.Scheme == "https") && string.IsNullOrEmpty(uri.UserInfo) &&
                !string.IsNullOrWhiteSpace(accessToken)) return true;
            status = "Set Camera URL (including port) and Access token in Camera settings.";
            return false;
        }
        public void TestConnection() {
            if (healthCheck == null && Configured()) healthCheck = StartCoroutine(CheckHealth());
        }
        IEnumerator CheckHealth() {
            healthRequest = UnityWebRequest.Get(endpoint.TrimEnd('/') + "/health"); Configure(healthRequest);
            yield return healthRequest.SendWebRequest();
            Health result = null;
            if (healthRequest.result == UnityWebRequest.Result.Success) {
                try { result = JsonUtility.FromJson<Health>(healthRequest.downloadHandler.text); } catch (ArgumentException) { }
            }
            status = result != null && result.protocol == 1 && result.ready && result.camera && result.model
                ? "LIVE connection passed: camera and model ready"
                : result != null ? "Camera unavailable: " + result.message : "Health check failed: " + healthRequest.error;
            healthRequest.Dispose(); healthRequest = null; healthCheck = null;
            if (game && game.ui) game.ui.Toast(status);
        }
        public void Begin(int house) {
            if (Waiting) { status = "Verification already running. Cancel before restarting."; return; }
            if (!game || !game.playing || !game.state.elder || house < 0 || house >= 5 || !game.state.puzzleSolved[house]) {
                status = "Complete this house's mini-game first."; return;
            }
            if (game.state.ObjectVerified(house)) { status = "Object already verified; collect the clue."; return; }
            if (!simulation && !Configured()) return;
            ActiveQuest = house; RequestId = Guid.NewGuid().ToString("N");
            status = simulation ? "MOCK: choose a simulated outcome; no camera is connected." : "Connecting to the camera...";
            if (!simulation) polling = StartCoroutine(Poll(house, RequestId));
        }
        public bool SimulateDetection(string category) {
            if (!simulation || !Waiting) return false;
            var result = RecognitionMock.Create(mockOutcome, ActiveQuest, RequestId, category);
            if (result.status != "success") { Fail("MOCK: " + result.message); return false; }
            bool accepted = Receive(result);
            if (!accepted) Fail("MOCK: object or confidence rejected; no clue awarded.");
            return accepted;
        }
        public bool Receive(RecognitionResult result) {
            if (!game || !game.playing || !ObjectQuests.Accept(result, ActiveQuest, RequestId, simulation)) return false;
            int house = ActiveQuest;
            if (!game.state.elder || !game.state.puzzleSolved[house] || game.state.simulation != simulation) return false;
            game.state.objects[house] = result.ToEvidence(); game.Commit();
            ActiveQuest = -1; RequestId = null;
            status = (result.simulated ? "MOCK" : "Camera") + " verified " + result.objectName + ". Collect your clue.";
            if (game.ui) { game.ui.Toast(status); game.ui.RefreshObjectQuest(house); }
            VerificationSucceeded?.Invoke(result);
            return true;
        }
        IEnumerator Poll(int house, string id) {
            float deadline = Time.realtimeSinceStartup + verificationTimeout;
            var payload = JsonUtility.ToJson(new Request { request_id = id, house_id = ObjectQuests.HouseIds[house], expected_object = ObjectQuests.Categories[house] });
            bool created = false;
            for (int attempt = 0; attempt < 3 && Waiting; attempt++) {
                current = new UnityWebRequest(endpoint.TrimEnd('/') + "/verify", "POST");
                current.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                Configure(current); current.SetRequestHeader("Content-Type", "application/json");
                yield return current.SendWebRequest();
                created = current.result == UnityWebRequest.Result.Success;
                long code = current.responseCode;
                string error = current.error;
                current.Dispose(); current = null;
                if (created) break;
                if (code >= 400 && code < 500 && code != 429) { Fail("Camera rejected request (HTTP " + code + "): " + error); yield break; }
                yield return new WaitForSecondsRealtime(1 + attempt);
            }
            if (!created) { Fail("Camera unavailable after 3 attempts. Check Pi, network, token and /health."); yield break; }
            status = "Show a " + ObjectQuests.Categories[house] + " to the camera, then return to VR.";
            if (game.ui) game.ui.RefreshObjectQuest(house);
            int failures = 0;
            while (Waiting && RequestId == id && Time.realtimeSinceStartup < deadline) {
                yield return new WaitForSecondsRealtime(.75f);
                current = UnityWebRequest.Get(endpoint.TrimEnd('/') + "/verify/" + id); Configure(current);
                yield return current.SendWebRequest();
                string json = current.result == UnityWebRequest.Result.Success ? current.downloadHandler.text : null;
                long code = current.responseCode;
                current.Dispose(); current = null;
                if (json == null) {
                    if (++failures >= 3 || (code >= 400 && code < 500 && code != 429)) { Fail("Camera connection lost or request expired (HTTP " + code + "). Retry verification."); yield break; }
                    continue;
                }
                failures = 0;
                RecognitionResult result = null;
                try { result = JsonUtility.FromJson<RecognitionResult>(json); } catch (ArgumentException) { }
                if (result == null || result.requestId != id || result.questIndex != house || result.simulated) { Fail("Camera returned mismatched or invalid JSON evidence."); yield break; }
                if (result.status == "success") {
                    if (!Receive(result)) Fail("Camera evidence failed confidence, object or frame validation.");
                    yield break;
                }
                if (result.status != "pending") { Fail("Camera " + result.status + ": " + result.message); yield break; }
            }
            if (Waiting && RequestId == id) Fail("Verification timed out. No clue awarded; retry when ready.");
        }
        void Configure(UnityWebRequest request) {
            if (request.downloadHandler == null) request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = requestTimeout; request.SetRequestHeader("X-FV-Token", accessToken);
        }
        void Fail(string message) {
            int house = ActiveQuest;
            ActiveQuest = -1; RequestId = null; polling = null; status = message;
            if (game && game.ui) { game.ui.Toast(message); game.ui.RefreshObjectQuest(house); }
            VerificationFailed?.Invoke(message);
        }
    }
}
