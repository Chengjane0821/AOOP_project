using System;
using NUnit.Framework;
using UnityEngine;
namespace ForgottenVillage.Tests {
    public sealed class BridgeDeliveryTests {
        [TestCase(0,true)][TestCase(180,true)][TestCase(360,true)][TestCase(-180,true)][TestCase(20,true)][TestCase(21,false)][TestCase(90,false)]
        public void OnlyAlignedTimberCanFasten(float angle,bool expected){Assert.That(BridgeRules.Aligned(angle),Is.EqualTo(expected));}
        [Test]public void WideCrossingRejectsShortTimber(){Assert.IsFalse(BridgeRules.Spans(.68f,BridgeLevels.RequiredLength(1)));Assert.IsTrue(BridgeRules.Spans(1.1f,BridgeLevels.RequiredLength(1)));}
        [Test]public void HeavyDeliveryAddsThreeReinforcements(){Assert.That(BridgeLevels.Load(2),Is.EqualTo(2));Assert.That(BridgeLevels.DeckCount(2),Is.EqualTo(3));Assert.That(BridgeLevels.PieceCount(2),Is.EqualTo(6));for(int i=3;i<6;i++)Assert.IsTrue(BridgeLevels.IsSupport(2,i));}
        [Test]public void NewGameDoesNotInventBridgeProgress(){var s=new VillageSave();Assert.IsFalse(s.carpenterBridgeWon||s.carpenterMemoryRecovered||s.puzzleSolved[2]||s.CanClaim(2));}
        [Test]public void BridgeClueRequiresVictoryMemoryAndCorrectObject(){var s=Earned();s.carpenterBridgeWon=false;Assert.IsFalse(s.CanClaim(2));s.carpenterBridgeWon=true;s.carpenterMemoryRecovered=false;Assert.IsFalse(s.CanClaim(2));s.carpenterMemoryRecovered=true;s.objects[2]=null;Assert.IsFalse(s.CanClaim(2));}
        [Test]public void WrongCameraCategoryCannotAwardBridgeClue(){var s=Earned();s.objects[2].objectName="clock";Assert.IsFalse(s.CanClaim(2));}
        [Test]public void UnfinishedLegacyGainPuzzleMustPlayBridge(){var s=new VillageSave{version=6,elder=true};s.puzzleSolved[2]=true;s.Normalize();Assert.That(s.version,Is.EqualTo(VillageSave.CurrentVersion));Assert.IsFalse(s.carpenterBridgeWon||s.carpenterMemoryRecovered||s.puzzleSolved[2]);}
        [Test]public void EarnedLegacyMouseClueSurvivesMigration(){var s=Earned();s.version=6;s.carpenterBridgeWon=s.carpenterMemoryRecovered=false;s.Normalize();Assert.IsTrue(s.carpenterBridgeWon&&s.carpenterMemoryRecovered&&s.clues[2]&&s.CanClaim(2));}
        [Test]public void InvalidLegacyEvidenceDoesNotCreateVictory(){var s=Earned();s.version=6;s.objects[2].frames=0;s.Normalize();Assert.IsFalse(s.carpenterBridgeWon||s.puzzleSolved[2]||s.clues[2]);}
        [Test]public void JsonRoundTripPreservesBridgeAndExistingMemories(){var s=Earned();s.clockRhythmWon=s.clockMemoryRecovered=s.technicianGridWon=s.technicianMemoryRecovered=true;var loaded=JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(s));loaded.Normalize();Assert.IsTrue(loaded.CanClaim(2)&&loaded.clockRhythmWon&&loaded.technicianGridWon);}
        [Test]public void FinalArchiveStillRequiresCarpenterMemory(){var s=AllEvidence();Assert.IsTrue(s.Authorized);s.carpenterMemoryRecovered=false;Assert.IsFalse(s.Unlocked||s.Authorized);}
        [Test]public void PhysicalChestRequiresRealMouseEvidenceAfterBridge(){var s=AllEvidence();Assert.IsTrue(s.Authorized);Assert.IsFalse(s.PhysicalChestAuthorized);for(int i=2;i<5;i++){s.objects[i].simulated=false;s.objects[i].snapshot=s.objects[i].requestId+".jpg";}Assert.IsTrue(s.PhysicalChestAuthorized);s.carpenterBridgeWon=false;Assert.IsFalse(s.PhysicalChestAuthorized);}
        static VillageSave Earned(){var s=new VillageSave{elder=true,carpenterBridgeWon=true,carpenterMemoryRecovered=true};s.puzzleSolved[2]=s.clues[2]=true;s.objects[2]=Evidence(2);return s;}
        static ObjectEvidence Evidence(int id)=>new ObjectEvidence{requestId=Guid.NewGuid().ToString("N"),objectName=ObjectQuests.Categories[id],snapshot="",capturedUtc=DateTimeOffset.UtcNow.ToString("O"),confidence=.99f,frames=3,simulated=true};
        static VillageSave AllEvidence(){var s=Earned();s.elderTennisWon=s.elderMemoryRecovered=s.photograph=s.alchemistDreamWon=s.alchemistMemoryRecovered=s.alchemistEvidenceInspected=s.technicianGridWon=s.technicianMemoryRecovered=s.clockRhythmWon=s.clockMemoryRecovered=s.faultyTest=s.fixedTest=true;for(int i=0;i<5;i++)s.puzzleSolved[i]=s.clues[i]=true;for(int i=2;i<5;i++)s.objects[i]=Evidence(i);for(int i=0;i<4;i++)s.evidence[i]=true;return s;}
    }
}
