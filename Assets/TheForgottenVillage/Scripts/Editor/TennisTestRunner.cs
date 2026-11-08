using System.IO;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
namespace ForgottenVillage.Editor {
public class TennisTestRunner : ICallbacks {
 static TestRunnerApi api;
 public static void Run(){api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new TennisTestRunner());api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=new[]{"ForgottenVillage.Tests"}}));}
 public void RunStarted(ITestAdaptor tests){}
 public void RunFinished(ITestResultAdaptor result){TestRunnerApi.SaveResultToFile(result,"Verification/elder-tennis-editmode.xml");File.WriteAllText("Verification/elder-tennis-editmode.txt",$"Tests: {result.PassCount+result.FailCount+result.SkipCount}, passed: {result.PassCount}, failed: {result.FailCount}, skipped: {result.SkipCount}\n{result.ResultState}");}
 public void TestStarted(ITestAdaptor test){}
 public void TestFinished(ITestResultAdaptor result){}
}
}
