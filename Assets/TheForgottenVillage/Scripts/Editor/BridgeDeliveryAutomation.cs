using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public static class BridgeDeliveryAutomation {
        static double allowAt;
        static BridgeDeliveryAutomation(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode)allowAt=EditorApplication.timeSinceStartup+1;};}
        static void Tick(){
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<allowAt)return;
            const string path="Verification/bridge-delivery.request";if(!File.Exists(path))return;
            string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
            try{
                if(command=="build")BridgeDeliveryBuilder.Build();
                else if(command=="refresh")AssetDatabase.Refresh();
                else{
                    string menu=command=="nunit"?"Run bridge delivery unit tests":command=="scene-checks"?"Validate bridge delivery integration":command=="play-tests"?"Test Carpenter bridge delivery":null;
                    if(menu==null||!EditorApplication.ExecuteMenuItem("Forgotten Village/"+menu))throw new ArgumentException("Unavailable bridge command: "+command);
                }
                File.WriteAllText("Verification/bridge-delivery.status",command+" dispatched "+DateTimeOffset.Now.ToString("O"));
            }catch(Exception exception){File.WriteAllText("Verification/bridge-delivery-error.txt",exception.ToString());Debug.LogException(exception);}
        }
    }
}
