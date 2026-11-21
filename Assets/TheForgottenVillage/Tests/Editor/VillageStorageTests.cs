using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
namespace ForgottenVillage.Tests {
 public class VillageStorageTests {
  string folder,previous;
  [SetUp] public void Setup(){previous=VillageStorage.overridePath;folder=Path.Combine(Path.GetTempPath(),"village-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);VillageStorage.overridePath=Path.Combine(folder,"save.json");}
  [TearDown] public void Cleanup(){VillageStorage.overridePath=previous;Directory.Delete(folder,true);}
  [Test] public void RepeatedAtomicSavesRetainLatestStateAndPreviousBackup(){
   var state=new VillageSave();
   for(int i=0;i<20;i++){state.inventory.Add("record-"+i);VillageStorage.Save(state);}
   Assert.AreEqual(20,VillageStorage.Load().inventory.Count);
   var backup=JsonUtility.FromJson<VillageSave>(File.ReadAllText(VillageStorage.PathName+".bak"));
   Assert.AreEqual(19,backup.inventory.Count);
   Assert.IsFalse(File.Exists(VillageStorage.PathName+".tmp"));
  }
  [Test] public void TemporaryWindowsReadHandleDoesNotLoseProgress(){
   if(Application.platform!=RuntimePlatform.WindowsEditor)Assert.Ignore("Windows atomic replacement regression");
   VillageStorage.Save(new VillageSave());
   var handle=new FileStream(VillageStorage.PathName,FileMode.Open,FileAccess.Read,FileShare.Read);
   var release=new Thread(()=>{Thread.Sleep(35);handle.Dispose();});release.Start();
   try{VillageStorage.Save(new VillageSave{elder=true});Assert.IsTrue(VillageStorage.Load().elder);}
   finally{release.Join();handle.Dispose();}
  }
  [Test] public void MissingDestinationDirectoryStillReportsFailure(){
   VillageStorage.overridePath=Path.Combine(folder,"missing","save.json");
   Assert.Throws<DirectoryNotFoundException>(()=>VillageStorage.Save(new VillageSave()));
  }
 }
}
