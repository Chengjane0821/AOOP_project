using System.Linq;
using NUnit.Framework;

namespace ForgottenVillage.Tests {
    public sealed class ClockSixBellTests {
        [Test] public void ChartVisitsEveryBellWithAReadableSingleNoteGap() {
            var notes = ClockChart.Create();
            Assert.That(notes.Length, Is.EqualTo(48));
            Assert.That(notes.Select(n => n.Lane).Distinct().OrderBy(i => i), Is.EqualTo(Enumerable.Range(0, 6)));
            Assert.That(notes[0].Time, Is.EqualTo(ClockChart.CountIn * ClockChart.Beat).Within(.000001));
            for (int i = 1; i < notes.Length; i++)
                Assert.That(notes[i].Time - notes[i - 1].Time, Is.GreaterThanOrEqualTo(ClockChart.Beat - .000001));
            foreach (int lane in Enumerable.Range(0, 6)) Assert.That(notes.Count(n => n.Lane == lane), Is.InRange(7, 9));
        }
        [Test] public void FinaleIsFasterThanWarmupAndAllThreePhrasesUseBothRows() {
            var notes = ClockChart.Create();
            Assert.That(notes[47].Time - notes[32].Time, Is.LessThan(notes[15].Time - notes[0].Time));
            for (int phrase = 0; phrase < 3; phrase++) {
                var lanes = notes.Skip(phrase * 16).Take(16).Select(n => n.Lane);
                Assert.IsTrue(lanes.Any(l => l < 3) && lanes.Any(l => l >= 3));
            }
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void EveryBellCanResolveItsNoteExactlyOnce(int lane) {
            var session = new ClockSession(new[] { new ClockNote(lane, 2) });
            Assert.That(session.Strike(lane, 2), Is.EqualTo(ClockHit.Perfect));
            session.Strike(lane, 2);
            Assert.That(session.Hits, Is.EqualTo(1));
            Assert.That(session.Strays, Is.EqualTo(1));
        }
        [Test] public void ThirtyFourHitsPassButThirtyThreeDoNot() {
            var passing = HitFirst(34); var failing = HitFirst(33);
            Assert.That(passing.RequiredHits, Is.EqualTo(34));
            Assert.IsTrue(passing.Passed);
            Assert.IsFalse(failing.Passed);
        }
        [Test] public void TwelveExtraStrikesAreAllowedButThirteenFail() {
            var session = HitFirst(48);
            for (int i = 0; i < 12; i++) session.Strike(0, 100 + i);
            Assert.IsTrue(session.Passed);
            session.Strike(0, 120);
            Assert.IsFalse(session.Passed);
        }
        [Test] public void WrongRowDoesNotConsumeTheExpectedBell() {
            var session = new ClockSession(new[] { new ClockNote(5, 2) });
            Assert.That(session.Strike(2, 2), Is.EqualTo(ClockHit.Miss));
            Assert.That(session.Hits, Is.Zero);
            Assert.That(session.Strike(5, 2), Is.EqualTo(ClockHit.Perfect));
            Assert.That(session.Hits, Is.EqualTo(1));
        }
        [Test] public void InvalidInputCannotCorruptAnUnresolvedSession() {
            var session = new ClockSession(new[] { new ClockNote(0, 2) });
            session.Strike(-1, 2); session.Strike(6, 2); session.Strike(0, double.NaN); session.Strike(0, double.PositiveInfinity);
            Assert.That(session.Hits, Is.Zero); Assert.That(session.Strays, Is.Zero);
            Assert.That(session.Strike(0, 2), Is.EqualTo(ClockHit.Perfect));
        }
        [Test] public void ANewRunReceivesItsOwnChartArray() {
            var first = ClockChart.Create(); first[0] = new ClockNote(5, 100);
            var second = ClockChart.Create();
            Assert.That(second[0].Lane, Is.EqualTo(0));
            Assert.That(second[0].Time, Is.LessThan(3));
        }
        static ClockSession HitFirst(int hits) {
            var notes = ClockChart.Create(); var session = new ClockSession(notes);
            for (int i = 0; i < hits; i++) session.Strike(notes[i].Lane, notes[i].Time);
            session.Advance(100); return session;
        }
    }
}
