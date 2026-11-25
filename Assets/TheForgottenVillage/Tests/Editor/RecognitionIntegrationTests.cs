using System;
using NUnit.Framework;
using UnityEngine;
namespace ForgottenVillage.Tests {
    public class RecognitionIntegrationTests {
        public static VillageSave Completed() => new VillageSave { elder=true,
            elderTennisWon=true, elderMemoryRecovered=true, photograph=true,
            alchemistDreamWon=true, alchemistMemoryRecovered=true, alchemistEvidenceInspected=true,
            carpenterArcadeWon=true, carpenterMemoryRecovered=true, technicianGridWon=true,
            technicianMemoryRecovered=true, clockRhythmWon=true, clockMemoryRecovered=true };
        public static RecognitionResult Evidence(int house, bool mock=false) {
            var id = Guid.NewGuid().ToString("N");
            var r = RecognitionMock.Create(MockOutcome.Success, house, id, ObjectQuests.Categories[house]);
            r.simulated=mock; r.snapshot=mock?"":id+".jpg";
            return r;
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void FiveObjectsRequireMatchingRequestAndHouse(int house) {
            var r=Evidence(house);
            Assert.That(ObjectQuests.Accept(r,house,r.requestId,false),Is.True);
            Assert.That(ObjectQuests.Accept(r,house,Guid.NewGuid().ToString("N"),false),Is.False);
            Assert.That(ObjectQuests.Accept(r,(house+1)%5,r.requestId,false),Is.False);
            Assert.That(ObjectQuests.Accept(r,house,r.requestId,true),Is.False);
        }
        [TestCase(MockOutcome.WrongObject)] [TestCase(MockOutcome.LowConfidence)]
        [TestCase(MockOutcome.NoObject)] [TestCase(MockOutcome.Timeout)] [TestCase(MockOutcome.ServerError)]
        public void MockFailuresCannotAwardEvidence(MockOutcome outcome) {
            var id=Guid.NewGuid().ToString("N");
            var r=RecognitionMock.Create(outcome,2,id,"mouse");
            Assert.That(ObjectQuests.Accept(r,2,id,true),Is.False);
        }
        [Test]
        public void MiniGameMustPrecedeClueEvenWithValidEvidence() {
            var save=Completed();
            save.objects[0]=Evidence(0).ToEvidence();
            Assert.That(save.CanClaim(0),Is.False);
            save.puzzleSolved[0]=true;
            Assert.That(save.CanClaim(0),Is.True);
        }
        [Test]
        public void FiveDistinctCluesSurviveSerializationAndUnlockFinalHouse() {
            var save=Completed();
            foreach(var house in new[]{4,1,3,0,2}) {
                Assert.That(save.Unlocked,Is.False);
                save.puzzleSolved[house]=true;save.objects[house]=Evidence(house).ToEvidence();
                save.clues[house]=true;
            }
            Assert.That(save.Unlocked,Is.True);
            var restored=JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(save));
            restored.Normalize();
            Assert.That(restored.Unlocked,Is.True);
            restored.objects[2]=restored.objects[0];
            Assert.That(restored.Unlocked,Is.False);
        }
        [Test]
        public void OldPhotoAndDreamRewardsCannotBypassNewScans() {
            var save=new VillageSave{version=8,elder=true,elderTennisWon=true,alchemistDreamWon=true};
            save.clues[0]=save.clues[1]=save.puzzleSolved[0]=save.puzzleSolved[1]=true;
            save.Normalize();
            Assert.That(save.clues[0]||save.clues[1],Is.False);
            Assert.That(save.puzzleSolved[0]&&save.puzzleSolved[1],Is.True);
        }
        [Test]
        public void MockEvidenceNeverUnlocksLiveSave() {
            var save=Completed();
            for(int i=0;i<5;i++){save.puzzleSolved[i]=save.clues[i]=true;save.objects[i]=Evidence(i,true).ToEvidence();}
            Assert.That(save.Unlocked,Is.False);
            save.simulation=true;
            Assert.That(save.Unlocked,Is.True);
            Assert.That(save.PhysicalChestAuthorized,Is.False);
        }
        [Test]
        public void RepeatedNormalizationDoesNotDuplicateInventoryClues() {
            var save=Completed();
            save.puzzleSolved[2]=save.clues[2]=true;save.objects[2]=Evidence(2).ToEvidence();
            save.Normalize();save.Normalize();
            Assert.That(save.inventory,Has.Count.EqualTo(1));
            Assert.That(save.inventory[0],Is.EqualTo("Analog Front-End Records"));
        }
    }
}
