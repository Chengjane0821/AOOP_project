using NUnit.Framework;
namespace ForgottenVillage.Tests {
    public sealed class PlanetaryDefenceTests {
        [Test] public void CountdownAndSpawnCadenceCannotBeSkippedByRepeatedCalls() {
            var s = new PlanetaryDefenceSession(); int id;
            s.Advance(2.9); Assert.IsFalse(s.TrySpawn(out id));
            s.Advance(.11); Assert.IsTrue(s.TrySpawn(out id)); Assert.AreEqual(0, id); Assert.IsFalse(s.TrySpawn(out id));
            s.Advance(1.35); Assert.IsTrue(s.TrySpawn(out id)); Assert.AreEqual(1, id);
        }
        [Test] public void EmptySurvivalIsNotVictoryAndFiveImpactsFail() {
            var s = new PlanetaryDefenceSession(); s.Advance(1000); Assert.IsFalse(s.Won);
            for (int i = 0; i < 5; i++) { Assert.IsTrue(s.TrySpawn(out int id)); Assert.IsTrue(s.Resolve(id, false)); }
            Assert.IsTrue(s.Failed); Assert.IsFalse(s.Won); Assert.IsFalse(s.TrySpawn(out _));
        }
        [Test] public void EveryThreatMustResolveAndDuplicatesNeverAwardTwice() {
            var s = new PlanetaryDefenceSession(); s.Advance(100);
            Assert.IsFalse(s.Resolve(0, true));
            for (int i = 0; i < 20; i++) {
                Assert.IsTrue(s.TrySpawn(out int id)); Assert.IsTrue(s.Resolve(id, i >= 4)); Assert.IsFalse(s.Resolve(id, true));
                Assert.AreEqual(i == 19, s.Won);
            }
            Assert.AreEqual(1, s.Integrity); Assert.AreEqual(16, s.Destroyed); Assert.IsFalse(s.TrySpawn(out _));
        }
        [Test] public void InvalidTimeDoesNotAdvanceAndRetryStartsClean() {
            var s = new PlanetaryDefenceSession(); s.Advance(double.NaN); s.Advance(double.PositiveInfinity); s.Advance(-1);
            Assert.AreEqual(0, s.Elapsed); s.Advance(50); s.TrySpawn(out int id); s.Resolve(id, false);
            var retry = new PlanetaryDefenceSession(); Assert.AreEqual(5, retry.Integrity); Assert.AreEqual(0, retry.Spawned); Assert.IsFalse(retry.Won);
        }
    }
}
