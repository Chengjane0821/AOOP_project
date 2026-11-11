using System;
using NUnit.Framework;
using UnityEngine;

namespace ForgottenVillage.Tests {
    public sealed class CarpenterArcadeSaveTests {
        static ObjectEvidence Mouse() => new ObjectEvidence {
            requestId = Guid.NewGuid().ToString("N"), objectName = "mouse", confidence = .99f,
            frames = 3, capturedUtc = DateTimeOffset.UtcNow.ToString("O"), snapshot = "", simulated = true
        };
        static VillageSave EarnedBridge() {
            var save = new VillageSave { version = 7, elder = true, carpenterBridgeWon = true, carpenterMemoryRecovered = true };
            save.clues[2] = save.puzzleSolved[2] = true; save.objects[2] = Mouse(); return save;
        }
        [Test] public void EarnedVersionSevenMouseClueSurvivesWithoutInventedHighScore() {
            var save = EarnedBridge(); save.Normalize();
            Assert.That(save.version, Is.EqualTo(8)); Assert.IsTrue(save.CanClaim(2));
            Assert.IsTrue(save.carpenterArcadeWon); Assert.That(save.carpenterBestScore, Is.Zero);
        }
        [Test] public void UnclaimedBridgeVictoryMustPlayCurrentChallenge() {
            var save = EarnedBridge(); save.clues[2] = false; save.Normalize();
            Assert.IsFalse(save.carpenterArcadeWon || save.carpenterMemoryRecovered || save.puzzleSolved[2]);
        }
        [Test] public void InvalidOldMouseEvidenceCannotMigrateIntoArcadeVictory() {
            var save = EarnedBridge(); save.objects[2].frames = 0; save.Normalize();
            Assert.IsFalse(save.carpenterArcadeWon || save.clues[2]);
        }
        [Test] public void CurrentSchemaBridgeFlagCannotBypassArcade() {
            var save = EarnedBridge(); save.version = 8; save.Normalize();
            Assert.IsFalse(save.CanClaim(2) || save.clues[2]);
        }
        [Test] public void CurrentVictoryStillNeedsMemoryAndMouse() {
            var save = new VillageSave { elder = true, carpenterArcadeWon = true }; save.puzzleSolved[2] = true;
            Assert.IsFalse(save.CanClaim(2)); save.carpenterMemoryRecovered = true;
            Assert.IsFalse(save.CanClaim(2)); save.objects[2] = Mouse(); Assert.IsTrue(save.CanClaim(2));
        }
        [Test] public void BestScorePersistsAlongsideCurrentVictory() {
            var save = EarnedBridge(); save.Normalize(); save.carpenterBestScore = 1234;
            var loaded = JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(save)); loaded.Normalize();
            Assert.IsTrue(loaded.CanClaim(2)); Assert.That(loaded.carpenterBestScore, Is.EqualTo(1234));
        }
        [Test] public void NegativeScoresNormalizeToZero() {
            var save = new VillageSave { carpenterBestScore = -1 }; save.Normalize(); Assert.That(save.carpenterBestScore, Is.Zero);
        }
    }
}
