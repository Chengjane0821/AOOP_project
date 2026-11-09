using System.IO;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
namespace ForgottenVillage.Editor {
public class PlaneDreamTestRunner : ICallbacks {
 static TestRunnerApi api;
 public static void Run(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new PlaneDreamTestRunner());api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"ForgottenVillage.Tests"}}));}
 public void RunStarted(ITestAdaptor tests){}
 public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,"Verification/plane-dream-editmode.xml");File.WriteAllText("Verification/plane-dream-editmode.txt",$"Passed: {r.PassCount}; failed: {r.FailCount}; skipped: {r.SkipCount}\n{r.ResultState}");}
 public void TestStarted(ITestAdaptor test){}public void TestFinished(ITestResultAdaptor result){}
}
}
