using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ForgottenVillage {
    public class ObjectRecognitionClient : MonoBehaviour {
        public VillageGame game;
        public bool simulation = true;
        public string endpoint = "http://raspberrypi.local:8765", accessToken = "", status = "Camera simulation ready";
        public int ActiveQuest { get; private set; } = -1;
        public string RequestId { get; private set; }
        public bool Waiting => ActiveQuest >= 0;
        Coroutine polling;
        UnityWebRequest current;
        [Serializable] class Request { public string requestId, objectName; public int questIndex; }
        void Awake() {
            var configured = Environment.GetEnvironmentVariable("FV_CAMERA_URL");
            if (!string.IsNullOrWhiteSpace(configured)) endpoint = configured;
            accessToken = Environment.GetEnvironmentVariable("FV_CAMERA_TOKEN") ?? accessToken;
            Application.runInBackground = true;
        }
        public void Cancel() {
            ActiveQuest = -1; RequestId = null;
            if (current != null) { current.Abort(); current.Dispose(); current = null; }
            if (polling != null) StopCoroutine(polling);
            polling = null; status = "Verification stopped. You can try again.";
        }
        void OnDisable() { Cancel(); }
        public void Begin(int house) {
            Cancel();
            if (!game || !game.playing || !game.state.elder || house < 0 || house >= 5 || !game.state.puzzleSolved[house]) {
                status = "Complete this house's engineering puzzle first."; return;
            }
            if (!simulation && (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https") || string.IsNullOrWhiteSpace(accessToken))) {
                status = "Set FV_CAMERA_URL and FV_CAMERA_TOKEN before using the Pi camera."; return;
            }
            ActiveQuest = house; RequestId = Guid.NewGuid().ToString("N");
            status = simulation ? "SIMULATION: use the test detection button; no camera is connected." : "Connecting to the camera...";
            if (!simulation) polling = StartCoroutine(Poll(house, RequestId));
        }
        public bool SimulateDetection(string category) {
            if (!simulation || !Waiting) return false;
            return Receive(new RecognitionResult { requestId = RequestId, questIndex = ActiveQuest,
                objectName = category, status = "verified", confidence = .99f, frames = 3,
                capturedUtc = DateTimeOffset.UtcNow.ToString("O"), snapshot = "", simulated = true });
        }
        public bool Receive(RecognitionResult result) {
            if (!game || !game.playing || !ObjectQuests.Accept(result, ActiveQuest, RequestId, simulation)) return false;
            int house = ActiveQuest;
            if (!game.state.elder || !game.state.puzzleSolved[house]) return false;
            game.state.objects[house] = result.ToEvidence(); game.Commit();
            ActiveQuest = -1; RequestId = null;
            status = (result.simulated ? "SIMULATED" : "Camera") + " evidence recorded: " + result.objectName + ". Return to the resident to collect your clue.";
            game.ui.Toast(status); game.ui.RefreshObjectQuest(house);
            return true;
        }
        IEnumerator Poll(int house, string id) {
            var payload = JsonUtility.ToJson(new Request { requestId = id, questIndex = house, objectName = ObjectQuests.Categories[house] });
            current = new UnityWebRequest(endpoint.TrimEnd('/') + "/v1/quests", "POST");
            current.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            Configure(current); current.SetRequestHeader("Content-Type", "application/json");
            yield return current.SendWebRequest();
            bool created = current.result == UnityWebRequest.Result.Success;
            current.Dispose(); current = null;
            if (!created) { Fail("Camera unavailable. Check the Pi, network and token, then retry."); yield break; }
            float deadline = Time.realtimeSinceStartup + 120;
            status = "Camera ready. Remove the headset, present a " + ObjectQuests.Categories[house] + ", then return to VR.";
            game.ui.RefreshObjectQuest(house);
            while (Waiting && RequestId == id && Time.realtimeSinceStartup < deadline) {
                yield return new WaitForSecondsRealtime(.75f);
                current = UnityWebRequest.Get(endpoint.TrimEnd('/') + "/v1/quests/" + id); Configure(current);
                yield return current.SendWebRequest();
                string json = current.result == UnityWebRequest.Result.Success ? current.downloadHandler.text : null;
                current.Dispose(); current = null;
                if (json == null) { Fail("Camera connection lost. Check the Pi and retry; no clue was awarded."); yield break; }
                RecognitionResult result = null;
                try { result = JsonUtility.FromJson<RecognitionResult>(json); } catch (ArgumentException) { }
                if (result == null || result.requestId != id) { Fail("Camera returned an invalid response. Retry verification."); yield break; }
                if (result.status == "verified") {
                    if (!Receive(result)) Fail("Camera evidence did not match this request. Retry verification.");
                    yield break;
                }
                if (result.status == "error" || result.status == "expired") { Fail("Camera could not complete verification. Check the camera and retry."); yield break; }
            }
            if (Waiting && RequestId == id) Fail("No matching object found within two minutes. Improve lighting and try again.");
        }
        void Configure(UnityWebRequest request) {
            if (request.downloadHandler == null) request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 5; request.SetRequestHeader("X-FV-Token", accessToken);
        }
        void Fail(string message) {
            int house = ActiveQuest;
            ActiveQuest = -1; RequestId = null; polling = null; status = message;
            if (game && game.ui) { game.ui.Toast(message); game.ui.RefreshObjectQuest(house); }
        }
    }
}
