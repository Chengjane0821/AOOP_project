using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ImportedMiniGameAutomation {
        static ImportedMiniGameAutomation() { EditorApplication.update += Tick; }
        static void Tick() {
            const string request = "Verification/imported-mini-games.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            string command;
            try { command = File.ReadAllText(request).Trim(); File.Delete(request); } catch (IOException) { return; }
            try {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Existing Play Mode session is in use.");
                if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new Exception("An open scene has unsaved edits. No scene was replaced.");
                if (command == "build") {
                    var setup = EditorSceneManager.GetSceneManagerSetup();
                    try { PlanetaryDefenceBuilder.BuildBoth(); }
                    finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
                } else if (command == "play") ImportedMiniGamePlayChecks.Request();
                else if (command == "preview-tools") ImportedMiniGamePlayChecks.PreviewTools();
                else if (command == "unit") ImportedMiniGamePlayChecks.UnitTests();
                else if (command == "validate") {
                    var setup = EditorSceneManager.GetSceneManagerSetup();
                    try { PowerGridChecks.Scenes(); ActiveWorkshopChecks.Scenes(); }
                    finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
                } else throw new Exception("Unknown mini-game command: " + command);
                File.WriteAllText("Verification/imported-mini-games.status", command + " dispatched");
            } catch (Exception e) { File.WriteAllText("Verification/imported-mini-games.status", "FAIL: " + e); Debug.LogException(e); }
        }
    }
}
