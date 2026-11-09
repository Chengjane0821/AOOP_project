using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class PlaneDreamAutomation {
 static PlaneDreamAutomation(){EditorApplication.update+=Tick;File.WriteAllText("Verification/plane-dream-ready.txt","pool-contact-v5");}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;const string path="Verification/plane-dream.request";if(!File.Exists(path))return;
  string command;try{command=File.ReadAllText(path).Trim();File.Delete(path);}catch(IOException){return;}
  try{if(command=="build")PlaneDreamBuilder.Build();else if(command=="refine")PlaneDreamBuilder.Refine();else if(command=="edit-tests")PlaneDreamTestRunner.Run();else if(command=="test")PlaneDreamChecks.Request();else if(command=="refresh")AssetDatabase.Refresh();}
  catch(Exception e){File.WriteAllText("Verification/plane-dream-error.txt",e.ToString());Debug.LogException(e);}
 }
}
}
