using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace ForgottenVillage.Editor {
// File requests let the already-open editor perform scene work without opening a second instance.
[InitializeOnLoad] public static class ElderTennisAutomation {
 static ElderTennisAutomation(){EditorApplication.update+=Tick;File.WriteAllText("Verification/elder-automation-ready.txt","bounce-prediction-v5");}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;
  const string request="Verification/elder-tennis.request";if(!File.Exists(request))return;
  string command;try{command=File.ReadAllText(request).Trim();File.Delete(request);}catch(IOException){return;}
  try{if(command=="audit")ElderTennisBuilder.Audit();else if(command=="build")ElderTennisBuilder.Build();else if(command=="edit-tests")TennisTestRunner.Run();else if(command=="refresh")AssetDatabase.Refresh();else if(command=="test")ElderTennisChecks.Request();}
  catch(Exception e){File.WriteAllText("Verification/elder-tennis-error.txt",e.ToString());Debug.LogException(e);}
 }
}
}
