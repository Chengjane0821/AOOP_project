using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
namespace ForgottenVillage.Editor {
    [InitializeOnLoad]
    public class RecognitionTestRunner : ICallbacks {
        static TestRunnerApi api;
        static RecognitionTestRunner(){ EditorApplication.update += Tick; }
        static void Tick(){
            const string path="Verification/recognition-tests.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying||!File.Exists(path))return;
            File.Delete(path); Run();
        }
        [MenuItem("Tools/Forgotten Village/Run recognition and progress tests")]
        public static void Run(){
            api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new RecognitionTestRunner());
            api.Execute(new ExecutionSettings(new Filter {testMode=TestMode.EditMode,testNames=new[]{"ForgottenVillage.Tests"}}));
        }
        public void RunStarted(ITestAdaptor tests){}
        public void RunFinished(ITestResultAdaptor r){
            TestRunnerApi.SaveResultToFile(r,"Verification/recognition-editmode.xml");
            File.WriteAllText("Verification/recognition-editmode.txt",$"Passed: {r.PassCount}; failed: {r.FailCount}; skipped: {r.SkipCount}\n{r.ResultState}");
        }
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){}
    }
}
