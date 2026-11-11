using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class PowerGridAutomation {
        static double allowAt;
        static PowerGridAutomation(){
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)allowAt=EditorApplication.timeSinceStartup+1;};
        }
        static void Tick(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<allowAt)return;
            const string path="Verification/power-grid.request";if(!File.Exists(path))return;
            string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try {
                if(command=="build")PowerGridBuilder.Build();
                else if(command=="refresh")AssetDatabase.Refresh();
                else {
                    string menu=command=="model-tests"?"Check power-grid rules":command=="scene-checks"?"Validate power-grid integration":
                        command=="nunit"?"Run power-grid regression unit tests":command=="play-tests"?"Test Technician power-grid emergency":null;
                    if(menu==null||!EditorApplication.ExecuteMenuItem("Forgotten Village/"+menu))throw new ArgumentException("Unavailable grid command: "+command);
                }
                File.WriteAllText("Verification/power-grid.status",command+" dispatched "+DateTimeOffset.Now.ToString("O"));
            }catch(Exception ex){File.WriteAllText("Verification/power-grid-error.txt",ex.ToString());Debug.LogException(ex);}
        }
    }
}
