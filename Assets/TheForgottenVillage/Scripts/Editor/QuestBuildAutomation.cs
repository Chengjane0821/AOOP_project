using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ForgottenVillage.Editor {
// Rebuild serialized scene data together with IL2CPP instead of reusing stale player data.
[InitializeOnLoad]
public static class QuestBuildAutomation {
    const string Request = "Verification/quest-build.request";
    static QuestBuildAutomation() { EditorApplication.update += Tick; }
    static void Tick() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying || !File.Exists(Request)) return;
        File.Delete(Request);
        try { Build(); }
        catch (Exception e) { File.WriteAllText("Verification/quest-build-result.txt", e.ToString()); Debug.LogException(e); }
    }
    [MenuItem("Forgotten Village/Build Quest (clean scene data)")]
    public static void Build() {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Select the Android build target first.");
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = "Builds/ForgottenVillage-Quest.apk",
            target = BuildTarget.Android,
            options = BuildOptions.CleanBuildCache
        });
        File.WriteAllText("Verification/quest-build-result.txt", report.summary.result + "\nErrors: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Quest build failed.");
    }
}

}
