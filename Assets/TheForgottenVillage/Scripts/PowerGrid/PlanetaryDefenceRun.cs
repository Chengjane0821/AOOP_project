using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace ForgottenVillage {
    public sealed class PlanetaryDefenceRun : MonoBehaviour {
        public PowerGridHouse house;
        public Transform playingPoint, planet;
        public Transform presentation, returnPoint;
        public GameObject[] roomObjects;
        public float spawnRadius = 3, planetRadius = 1.3f;
        public PlanetaryDefenceThreat[] threats;
        public PlanetaryDefenceProjectile[] projectiles;
        public Transform[] flashes;
        public TMP_Text status, instructions;
        public AudioSource sound;
        public AudioClip shotClip, hitClip, impactClip;
        public PlanetaryDefenceSession Session { get; private set; }
        public bool Running { get; private set; }
        public bool AcceptsInput => Running && Allowed && !house.game.ui.IsOpen && Session != null && !Session.Finished && Session.Elapsed >= PlanetaryDefenceSession.CountIn;
        bool Allowed => house && house.game && house.game.playing && house.game.house == 3 && house.game.state.elder;
        bool savedPlayer, oldLock, oldPointer;
        string oldPrompt;
        double lastTime, lastShot;
        readonly float[] flashTime = new float[6];
        System.Random random;
        readonly List<GameObject> hiddenRoom = new List<GameObject>();
        bool inSpace, trackerWasActive, oldFog;
        Material oldSky;
        AmbientMode oldAmbientMode;
        Color oldAmbient, oldBackground;
        Camera spaceCamera;
        VillageGame spaceGame;
        CameraClearFlags oldClearFlags;

        public void Offer() {
            if (!Allowed) return;
            if (Running) { PauseMenu(); return; }
            var ui = house.game.ui;
            ui.Show("Planetary Defence", "Travel to the orbital defence arena and protect the large planet from 20 incoming enemy ships.\nAim at a ship and use your normal XR Select control to launch a homing interceptor. Desktop: click a ship.\nThe planet has 5 integrity. Clear the wave before five impacts.\nStay comfortably in place; Menu pauses. Leave or finish to return to this house.");
            ui.Button("Defend planet", Begin); ui.Button("Maybe later", ui.Close);
            if (house.game.state.technicianMemoryRecovered) ui.Button("Investigate record", () => ui.ObjectQuest(3));
        }
        public void Begin() {
            if (!Allowed) return;
            Stop();
            var player = house.game.player;
            oldLock = player.ActivityLocksMovement; oldPointer = player.ActivityUsesPointer; oldPrompt = player.ActivityPrompt; savedPlayer = true;
            player.ActivityLocksMovement = true; player.ActivityUsesPointer = !player.IsVR; player.ActivityPrompt = "";
            EnterSpace();
            player.Teleport(playingPoint.position, playingPoint.eulerAngles.y);
            if (player.IsVR && player.origin) player.origin.MatchOriginUpCameraForward(Vector3.up, playingPoint.forward);
            Session = new PlanetaryDefenceSession(); random = new System.Random(); Running = true;
            lastTime = Time.realtimeSinceStartupAsDouble; lastShot = -1;
            house.game.ui.Close(); if (house.game.ui.notice) house.game.ui.notice.text = ""; Render();
        }
        void Update() {
            double now = Time.realtimeSinceStartupAsDouble;
            float dt = (float)System.Math.Min(.1, System.Math.Max(0, now - lastTime)); lastTime = now;
            if (Running && !Allowed) { Stop(); return; }
            if (Running && !house.game.ui.IsOpen) {
                Session.Advance(dt);
                foreach (var threat in threats) if (!threat.Live && Session.TrySpawn(out int id)) {
                    float angle = (float)(random.NextDouble() * Mathf.PI * 2);
                    threat.Launch(id, planet.localPosition + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * spawnRadius, .36f + id * .01f);
                }
                foreach (var projectile in projectiles) projectile.Step(dt);
                foreach (var threat in threats) threat.Step(dt);
                for (int i = 0; i < flashes.Length; i++) if (flashTime[i] > 0) {
                    flashTime[i] -= dt; flashes[i].localScale = Vector3.one * (.22f + (.3f - flashTime[i]) * 1.3f);
                    flashes[i].gameObject.SetActive(flashTime[i] > 0);
                }
                DesktopInput();
                if (Session.Finished) {
                    bool won = Session.Won; Stop();
                    if (won) house.Win();
                    else {
                        var ui = house.game.ui;
                        ui.Show("Planet lost", "Five threats reached the planet.\nIntercepted " + Session.Destroyed + " / 20. Aim at the next ship and select to intercept it.");
                        ui.Button("Retry defence", Begin); ui.Button("Leave defence", ui.Close);
                    }
                }
            }
            Render();
        }
#if UNITY_EDITOR
        public System.Action AutomationBeforeDesktopInput;
#endif
        void DesktopInput() {
#if UNITY_EDITOR
            AutomationBeforeDesktopInput?.Invoke();
#endif
            var player = house.game.player;
            if (!AcceptsInput || player.IsVR || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            var ray = player.View.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hit, 10, ~0, QueryTriggerInteraction.Collide)) {
                var threat = hit.collider.GetComponent<PlanetaryDefenceThreat>();
                if (threat) Fire(threat, ray.origin);
            }
        }
        public void Fire(PlanetaryDefenceThreat target, Vector3 origin) {
            if (!AcceptsInput || !target || target.run != this || !target.Live || target.Targeted || Session.Elapsed - lastShot < .22) return;
            foreach (var projectile in projectiles) if (!projectile.gameObject.activeSelf) {
                target.Targeted = true; lastShot = Session.Elapsed;
                projectile.Launch(target, origin); Play(shotClip); return;
            }
        }
        public void Resolve(PlanetaryDefenceThreat threat, bool intercepted) {
            if (!Running || !threat.Live || !Session.Resolve(threat.Id, intercepted)) return;
            for (int i = 0; i < flashes.Length; i++) if (flashTime[i] <= 0) {
                flashes[i].position = intercepted ? threat.transform.position : planet.position;
                flashTime[i] = .3f; flashes[i].gameObject.SetActive(true); break;
            }
            threat.Targeted = false; threat.gameObject.SetActive(false);
            Play(intercepted ? hitClip : impactClip);
        }
        void Play(AudioClip clip) { if (sound && clip) sound.PlayOneShot(clip, .3f); }
        void EnterSpace() {
            spaceGame = house.game;
            hiddenRoom.Clear();
            foreach (var item in roomObjects) if (item && item.activeSelf) { hiddenRoom.Add(item); item.SetActive(false); }
            oldSky = RenderSettings.skybox; oldFog = RenderSettings.fog; oldAmbient = RenderSettings.ambientLight; oldAmbientMode = RenderSettings.ambientMode;
            RenderSettings.skybox = null; RenderSettings.fog = false; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.08f, .1f, .16f);
            spaceCamera = house.game.player.View;
            oldClearFlags = spaceCamera.clearFlags; oldBackground = spaceCamera.backgroundColor;
            spaceCamera.clearFlags = CameraClearFlags.SolidColor; spaceCamera.backgroundColor = new Color(.002f, .004f, .015f);
            trackerWasActive = house.game.ui.tracker && house.game.ui.tracker.gameObject.activeSelf;
            if (house.game.ui.tracker) house.game.ui.tracker.gameObject.SetActive(false);
            presentation.gameObject.SetActive(true); inSpace = true;
        }
        void ReturnToRoom() {
            if (!inSpace) return;
            inSpace = false;
            // If the whole house is being unloaded, the village transition has
            // already placed the player. Never overwrite that destination.
            bool stillInHouse = spaceGame && spaceGame.house == 3 && !(spaceGame.transitions && spaceGame.transitions.Busy);
            if (stillInHouse) {
                foreach (var item in hiddenRoom) if (item) item.SetActive(true);
                if (returnPoint) {
                    spaceGame.player.Teleport(returnPoint.position, returnPoint.eulerAngles.y);
                    if (spaceGame.player.IsVR && spaceGame.player.origin) spaceGame.player.origin.MatchOriginUpCameraForward(Vector3.up, returnPoint.forward);
                }
            }
            hiddenRoom.Clear();
            if (gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene()) {
                RenderSettings.skybox = oldSky; RenderSettings.fog = oldFog; RenderSettings.ambientMode = oldAmbientMode; RenderSettings.ambientLight = oldAmbient;
            }
            if (spaceCamera) { spaceCamera.clearFlags = oldClearFlags; spaceCamera.backgroundColor = oldBackground; }
            if (spaceGame && spaceGame.ui.tracker) spaceGame.ui.tracker.gameObject.SetActive(trackerWasActive);
            if (presentation) presentation.gameObject.SetActive(false);
        }
        public void PauseMenu() {
            if (!Running || !Allowed) return;
            if (sound) sound.Stop();
            var ui = house.game.ui;
            ui.Show("Planetary Defence / Paused", "Enemy ships, interceptors and the wave timer are frozen.\nAim at an enemy and Select to fire. Protect the planet through 20 threats.");
            ui.Button("Resume", () => { lastTime = Time.realtimeSinceStartupAsDouble; ui.Close(); });
            ui.Button("Retry defence", Begin); ui.Button("Leave defence", () => { Stop(); ui.Close(); });
        }
        public void Stop() {
            if (sound) sound.Stop();
            if (threats != null) foreach (var threat in threats) if (threat) { threat.Targeted = false; threat.gameObject.SetActive(false); }
            if (projectiles != null) foreach (var projectile in projectiles) if (projectile) projectile.gameObject.SetActive(false);
            if (flashes != null) for (int i = 0; i < flashes.Length; i++) if (flashes[i]) { flashes[i].gameObject.SetActive(false); flashTime[i] = 0; }
            ReturnToRoom();
            var game = house ? house.game : spaceGame;
            if (savedPlayer && game && game.player) {
                var player = game.player; player.ActivityLocksMovement = oldLock; player.ActivityUsesPointer = oldPointer; player.ActivityPrompt = oldPrompt;
            }
            Running = false; savedPlayer = false;
        }
        void Render() {
            if (status) status.text = "PLANETARY DEFENCE\n" + (Session == null ? "20 THREATS  /  5 INTEGRITY" :
                "INTEGRITY " + Session.Integrity + " / 5    INTERCEPTED " + Session.Destroyed + " / 20\n" +
                (Running && Session.Elapsed < 3 ? "GET READY " + Mathf.CeilToInt(3 - (float)Session.Elapsed) : Running && house.game.ui.IsOpen ? "PAUSED" : Session.Won ? "PLANET DEFENDED" : Session.Failed ? "RETRY TO DEFEND THE PLANET" : "RESOLVED " + Session.Resolved + " / 20"));
        }
        void OnDisable() { Stop(); }
    }
}
