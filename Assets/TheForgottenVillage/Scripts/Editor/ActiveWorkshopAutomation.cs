using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ActiveWorkshopAutomation {
        static double allowAt;
        static ActiveWorkshopAutomation() {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode) allowAt = EditorApplication.timeSinceStartup + 1;
            };
        }
        static void Tick() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < allowAt) return;
            const string request = "Verification/active-workshop.request";
            if (!File.Exists(request)) return;
            string command;
            try { command = File.ReadAllText(request).Trim(); File.Delete(request); }
            catch (IOException) { return; }
            try {
                if (command == "build") ActiveWorkshopBuilder.Build();
                else if (command == "refresh") AssetDatabase.Refresh();
                else if (command == "nunit") ActiveWorkshopChecks.NUnit();
                else if (command == "scene-checks") ActiveWorkshopChecks.Scenes();
                else if (command == "play-tests") ActiveWorkshopPlayChecks.Request();
                else if (command == "preview") ActiveGamePresentationChecks.Request();
                else throw new ArgumentException("Unknown workshop command: " + command);
                File.WriteAllText("Verification/active-workshop.status", command + " dispatched " + DateTimeOffset.Now.ToString("O"));
            } catch (Exception exception) {
                File.WriteAllText("Verification/active-workshop-error.txt", exception.ToString());
                Debug.LogException(exception);
            }
        }
    }
}
