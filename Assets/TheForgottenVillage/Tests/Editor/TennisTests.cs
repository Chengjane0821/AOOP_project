using NUnit.Framework;
using UnityEngine;
namespace ForgottenVillage.Tests {
public class TennisTests {
 [Test] public void ThresholdIsInclusiveAndFreshEachGame(){var r=new TennisRules();bool low=false,high=false;Random.InitState(745);for(int i=0;i<1000;i++){r.BeginGame();Assert.That(r.threshold,Is.InRange(20,30));low|=r.threshold==20;high|=r.threshold==30;}Assert.IsTrue(low&&high);}
 [Test] public void ContactsAccumulateAcrossRalliesButNotOutsidePlay(){var r=new TennisRules();r.BeginGame(20);Assert.IsFalse(r.Contact(true,true));r.BeginRally();for(int i=0;i<10;i++)r.Contact(i%2==0,false);r.Fault(true);r.BeginRally();Assert.AreEqual(0,r.rallyHits);Assert.AreEqual(10,r.gameHits);for(int i=0;i<10;i++)r.Contact(i%2==0,false);Assert.IsTrue(r.elderMustMiss);Assert.AreEqual(0,r.playerGames);r.Fault(true);Assert.AreEqual(1,r.playerGames);}
 [Test] public void TwoSmashesWaitForFinalOutcome(){var r=new TennisRules();r.BeginMatch();r.BeginRally();r.Contact(true,true);r.Contact(false,false);r.Contact(true,true);Assert.IsTrue(r.elderMustMiss);Assert.AreEqual(TennisRules.Phase.Rally,r.phase);r.Fault(true);Assert.AreEqual(1,r.playerGames);r.Fault(true);Assert.AreEqual(1,r.playerGames);r.BeginGame();Assert.AreEqual(0,r.smashes);}
 [Test] public void BestOfThreeWinAndLossReset(){var r=new TennisRules();r.BeginMatch();WinGame(r,true);r.BeginGame();WinGame(r,false);r.BeginGame();WinGame(r,false);Assert.AreEqual(TennisRules.Phase.Lost,r.phase);r.ResetLoss();Assert.AreEqual(0,r.playerGames+r.elderGames+r.smashes+r.gameHits);r.BeginMatch();WinGame(r,true);r.BeginGame();WinGame(r,true);Assert.AreEqual(TennisRules.Phase.Won,r.phase);}
 static void WinGame(TennisRules r,bool player){for(int i=0;i<5;i++){r.BeginRally();r.Fault(player);}}
 [Test] public void SmashRequiresHeightDescentSpeedDirectionAndPlayerSide(){var t=ScriptableObject.CreateInstance<TennisTuning>();var ball=new Vector3(0,1.4f,-1);var incoming=new Vector3(0,-1,-3);var swing=new Vector3(0,0,2);
  Assert.IsTrue(TennisShots.IsSmash(t,ball,incoming,swing,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,new Vector3(0,.8f,-1),incoming,swing,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,new Vector3(0,1.4f,1),incoming,swing,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,ball,new Vector3(0,3,-3),swing,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,ball,incoming,Vector3.zero,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,ball,incoming,-swing,Vector3.forward));
  Assert.IsFalse(TennisShots.IsSmash(t,ball,incoming,swing,Vector3.right));
  Assert.IsTrue(TennisShots.IsSmash(t,ball,incoming,swing,Vector3.back));Object.DestroyImmediate(t);
 }
 [Test] public void ShotSpeedCannotExceedConfiguredCap(){var t=ScriptableObject.CreateInstance<TennisTuning>();var shot=TennisShots.PlayerReturn(t,new Vector3(0,1.5f,-1),Vector3.back*20,Vector3.forward*100,Vector3.forward,true);Assert.LessOrEqual(shot.magnitude,t.maximumBallSpeed+.001f);Object.DestroyImmediate(t);}
 [Test] public void ElderClueRequiresWinMemoryAndInspection(){var s=new VillageSave{elder=true};s.puzzleSolved[0]=true;Assert.IsFalse(s.CanClaim(0));s.elderTennisWon=true;Assert.IsFalse(s.CanClaim(0));s.elderMemoryRecovered=true;Assert.IsFalse(s.CanClaim(0));s.photograph=true;Assert.IsTrue(s.CanClaim(0));Assert.IsFalse(s.Unlocked);s.clues[0]=true;Assert.IsFalse(s.Unlocked);}
 [Test] public void CompletedLegacyClueMigratesButUnfinishedDividerDoesNot(){var s=new VillageSave{version=2,elder=true};s.puzzleSolved[0]=true;s.Normalize();Assert.IsFalse(s.elderTennisWon);Assert.IsFalse(s.puzzleSolved[0]);s=new VillageSave{version=2,elder=true};s.clues[0]=true;s.Normalize();Assert.IsTrue(s.elderTennisWon&&s.elderMemoryRecovered&&s.photograph&&s.CanClaim(0));}
 [Test] public void SaveRoundTripPreservesElderEvidence(){var s=new VillageSave{elder=true,elderTennisWon=true,elderMemoryRecovered=true,photograph=true};s.puzzleSolved[0]=s.clues[0]=true;var loaded=JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(s));loaded.Normalize();Assert.IsTrue(loaded.clues[0]);Assert.IsFalse(loaded.Unlocked);}
}
}
