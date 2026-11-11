using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class ClockRhythmAutomation {
        static ClockRhythmAutomation() { EditorApplication.update += Tick; }
        static void Tick() {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string request = "Verification/clock-rhythm.request";
            if (!File.Exists(request)) return;
            string command;
            try { command = File.ReadAllText(request).Trim(); File.Delete(request); } catch (IOException) { return; }
            try {
                if (command == "build") ClockRhythmBuilder.Build();
                else if (command == "refresh") AssetDatabase.Refresh();
                else if (command == "checks") EditorApplication.ExecuteMenuItem("Forgotten Village/Check Clockmaker rhythm repair");
                else if (command == "play-tests") EditorApplication.ExecuteMenuItem("Forgotten Village/Test Clockmaker rhythm repair");
                else if (command == "nunit") ClockRhythmChecks.NUnit();
                else if (command == "scene-checks") ClockRhythmChecks.Scenes();
                else throw new ArgumentException("Unknown clock command: " + command);
            } catch (Exception ex) { File.WriteAllText("Verification/clock-rhythm-error.txt", ex.ToString()); Debug.LogException(ex); }
        }
    }
}
