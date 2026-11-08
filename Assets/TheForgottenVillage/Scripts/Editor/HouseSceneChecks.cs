using System.IO;
using UnityEditor;
namespace ForgottenVillage.Editor {
[InitializeOnLoad] public static class HouseSceneChecks {
 static HouseSceneChecks(){EditorApplication.update+=Tick;}
 [MenuItem("Forgotten Village/Run integrated Play Mode checks")]
 public static void Request(){ElderTennisChecks.Request();}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;const string request="Verification/house-playtest.request";if(!File.Exists(request))return;try{File.Delete(request);}catch(IOException){return;}Request();}
}
}
