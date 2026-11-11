using System;
using System.Linq;
using NUnit.Framework;

namespace ForgottenVillage.Tests {
    public sealed class ActiveWorkshopSessionTests {
        [Test]
        public void SeedReproducesTheChallengeWhileDifferentSeedsChangeIt() {
            var first = new ActiveWorkshopSession(1842);
            var replay = new ActiveWorkshopSession(1842);
            var other = new ActiveWorkshopSession(1843);
            Assert.That(Signature(replay), Is.EqualTo(Signature(first)));
            Assert.That(Signature(other), Is.Not.EqualTo(Signature(first)));
        }

        [TestCase(0)] [TestCase(42)] [TestCase(1842)]
        public void EveryChallengeFitsSixtySecondsWithReachableSeparatedCues(int seed) {
            var session = new ActiveWorkshopSession(seed);
            Assert.That(session.Cues.Length, Is.GreaterThan(20));
            double previousEnd = 0;
            foreach (var cue in session.Cues) {
                Assert.That(cue.Start, Is.GreaterThanOrEqualTo(previousEnd));
                Assert.That(cue.End, Is.GreaterThan(cue.Start));
                Assert.That(cue.End, Is.LessThanOrEqualTo(ActiveWorkshopSession.Duration));
                Assert.That(cue.First, Is.InRange(0, 5));
                if (cue.Kind == WorkshopCueKind.Double) {
                    Assert.That(cue.First, Is.Not.EqualTo(cue.Second));
                    Assert.That(cue.First % 3, Is.Zero, "A double should provide a separate left-side target.");
                    Assert.That(cue.Second % 3, Is.EqualTo(2), "A double should provide a separate right-side target.");
                }
                if (cue.IsDodge)
                    Assert.That(cue.End - cue.Start, Is.GreaterThan(ActiveWorkshopSession.DodgeWarning + ActiveWorkshopSession.DodgeHold));
                previousEnd = cue.End;
            }
        }

        [Test]
        public void LaterStagesAskForFasterReactionsAndIncludeBothHandsAndDodges() {
            var cues = new ActiveWorkshopSession(42).Cues;
            double early = cues.Where(c => c.Start < 20).Average(c => c.End - c.Start);
            double middle = cues.Where(c => c.Start >= 20 && c.Start < 40).Average(c => c.End - c.Start);
            double late = cues.Where(c => c.Start >= 40).Average(c => c.End - c.Start);
            Assert.That(early, Is.GreaterThan(middle));
            Assert.That(middle, Is.GreaterThan(late));
            Assert.That(cues.Any(c => c.Kind == WorkshopCueKind.Double), Is.True);
            Assert.That(cues.Any(c => c.IsDodge), Is.True);
        }

        [Test]
        public void IdlePlayerFailsEvenAfterTimeJumpsAcrossAllCues() {
            var session = new ActiveWorkshopSession(42);
            session.Advance(ActiveWorkshopSession.Duration + 10);
            Assert.That(session.Finished, Is.True);
            Assert.That(session.Passed, Is.False);
            Assert.That(session.Elapsed, Is.EqualTo(ActiveWorkshopSession.Duration));
            Assert.That(session.Remaining, Is.Zero);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Misses, Is.EqualTo(session.Cues.Length));
            Assert.That(session.Multiplier, Is.EqualTo(1));
        }

        [Test]
        public void CorrectReachesAndDodgesCanWinWithoutChangingSuccessFlags() {
            var session = new ActiveWorkshopSession(1842);
            foreach (var cue in session.Cues) Complete(session, cue);
            Assert.That(session.Passed, Is.False, "Scoring enough points must not end the timed challenge early.");
            session.Advance(ActiveWorkshopSession.Duration - session.Elapsed);
            Assert.That(session.Finished && session.Passed, Is.True);
            Assert.That(session.Score, Is.GreaterThanOrEqualTo(ActiveWorkshopSession.Goal));
            Assert.That(session.Misses, Is.Zero);
            Assert.That(session.BestCombo, Is.EqualTo(session.Cues.Length));
            Assert.That(session.TargetsHit, Is.EqualTo(session.Cues.Sum(c => c.IsDodge ? 0 : c.Kind == WorkshopCueKind.Double ? 2 : 1)));
            Assert.That(session.Dodges, Is.EqualTo(session.Cues.Count(c => c.IsDodge)));
            Assert.That(session.Multiplier, Is.EqualTo(4));
            Assert.That(session.Score, Is.LessThanOrEqualTo(session.Cues.Length * 120 * 4));
        }

        [Test]
        public void OneTargetCanOnlyScoreOnceAndInvalidHitsDoNotGrantPoints() {
            var session = At(WorkshopCueKind.Target);
            var cue = session.Current;
            Assert.That(session.Hit(-1, 0), Is.EqualTo(WorkshopHit.None));
            Assert.That(session.Hit(cue.First, 2), Is.EqualTo(WorkshopHit.None));
            Assert.That(session.Hit((cue.First + 1) % 6, 0), Is.EqualTo(WorkshopHit.None));
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Hit(cue.First, 0), Is.EqualTo(WorkshopHit.Success));
            int score = session.Score, revision = session.Revision;
            Assert.That(session.Hit(cue.First, 1), Is.EqualTo(WorkshopHit.None));
            Assert.That(session.Score, Is.EqualTo(score));
            Assert.That(session.Revision, Is.EqualTo(revision));
            Assert.That(session.TargetsHit, Is.EqualTo(1));
        }

        [Test]
        public void HitsBeforeCueAppearsCannotPreloadAReward() {
            var session = new ActiveWorkshopSession(42);
            Assert.That(session.Current, Is.Null);
            Assert.That(session.Hit(session.Cues[0].First, 0), Is.EqualTo(WorkshopHit.None));
            AdvanceTo(session, session.Cues[0].Start + .001);
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.CueResolved, Is.False);
        }

        [Test]
        public void DoubleRequiresTwoDifferentTargetsAndTwoDifferentHands() {
            var session = At(WorkshopCueKind.Double);
            var cue = session.Current;
            Assert.That(session.Hit(cue.First, 0), Is.EqualTo(WorkshopHit.Partial));
            Assert.That(session.Hit(cue.First, 1), Is.EqualTo(WorkshopHit.None));
            Assert.That(session.Hit(cue.Second, 0), Is.EqualTo(WorkshopHit.WrongHand));
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Hit(cue.Second, 1), Is.EqualTo(WorkshopHit.Success));
            Assert.That(session.TargetsHit, Is.EqualTo(2));
            Assert.That(session.Hit(cue.First, 0), Is.EqualTo(WorkshopHit.None));
        }

        [Test]
        public void DoubleAcceptsASecondHandInsideItsWindow() {
            var session = At(WorkshopCueKind.Double);
            var cue = session.Current;
            session.Hit(cue.First, 0);
            session.Advance(ActiveWorkshopSession.DoubleWindow - .01);
            Assert.That(session.Hit(cue.Second, 1), Is.EqualTo(WorkshopHit.Success));
        }

        [Test]
        public void ExpiredDoubleContactMustBePairedAgainButCanRecover() {
            var session = At(WorkshopCueKind.Double);
            var cue = session.Current;
            session.Hit(cue.First, 0);
            session.Advance(ActiveWorkshopSession.DoubleWindow + .01);
            Assert.That(session.FirstHand, Is.EqualTo(-1));
            Assert.That(session.Hit(cue.Second, 1), Is.EqualTo(WorkshopHit.Partial));
            Assert.That(session.Score, Is.Zero);
            Assert.That(session.Hit(cue.First, 0), Is.EqualTo(WorkshopHit.Success));
        }

        [TestCase(WorkshopCueKind.DodgeLeft)]
        [TestCase(WorkshopCueKind.DodgeRight)]
        [TestCase(WorkshopCueKind.Duck)]
        public void DodgeNeedsWarningThenCorrectDirectionHeldContinuously(WorkshopCueKind kind) {
            var session = At(kind);
            var cue = session.Current;
            Pose(kind, out float x, out float y);
            Assert.That(session.ObserveDodge(x, y, .24), Is.False, "Moving during the warning must not instantly resolve a hazard.");
            AdvanceTo(session, cue.Start + ActiveWorkshopSession.DodgeWarning + .001);
            session.Advance(.10);
            Assert.That(session.ObserveDodge(x, y, .10), Is.False);
            session.Advance(.05);
            Assert.That(session.ObserveDodge(-x, -y, .05), Is.False, "Wrong direction should clear the safe hold.");
            session.Advance(.11);
            Assert.That(session.ObserveDodge(x, y, .11), Is.False);
            session.Advance(.11);
            Assert.That(session.ObserveDodge(x, y, .11), Is.True);
            int score = session.Score;
            Assert.That(session.ObserveDodge(x, y, .20), Is.False);
            Assert.That(session.Score, Is.EqualTo(score));
            Assert.That(session.Dodges, Is.EqualTo(1));
        }

        [Test]
        public void NonFiniteDodgeTimeCannotPoisonTheNextValidHold() {
            var session = At(WorkshopCueKind.Duck);
            var cue = session.Current;
            AdvanceTo(session, cue.Start + ActiveWorkshopSession.DodgeWarning + .001);
            Assert.That(session.ObserveDodge(0, -.25f, double.NaN), Is.False);
            Assert.That(session.ObserveDodge(0, -.25f, double.PositiveInfinity), Is.False);
            Assert.That(session.ObserveDodge(0, -.25f, -.10), Is.False);
            Assert.That(session.ObserveDodge(0, -.25f, .30), Is.False);
            session.Advance(.11); Assert.That(session.ObserveDodge(0, -.25f, .11), Is.False);
            session.Advance(.11); Assert.That(session.ObserveDodge(0, -.25f, .11), Is.True);
        }

        [Test]
        public void AFailedCueBreaksMultiplierButPreservesBestCombo() {
            var session = new ActiveWorkshopSession(42);
            foreach (var cue in session.Cues.Take(6)) Complete(session, cue);
            Assert.That(session.Combo, Is.EqualTo(6));
            Assert.That(session.Multiplier, Is.GreaterThan(1));
            var missed = session.Cues[6];
            AdvanceTo(session, missed.End + .001);
            Assert.That(session.Combo, Is.Zero);
            Assert.That(session.Multiplier, Is.EqualTo(1));
            Assert.That(session.BestCombo, Is.EqualTo(6));
            Assert.That(session.Misses, Is.EqualTo(1));
            Assert.That(session.Score, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void InvalidTimeAndPostFinishInputDoNotChangeCompletedResults() {
            var session = new ActiveWorkshopSession(42);
            session.Advance(double.NaN); session.Advance(double.PositiveInfinity); session.Advance(-1);
            Assert.That(session.Elapsed, Is.Zero);
            session.Advance(ActiveWorkshopSession.Duration);
            int score = session.Score, misses = session.Misses, revision = session.Revision;
            session.Advance(10); session.Hit(0, 0); session.ObserveDodge(-1, -1, .2);
            Assert.That(session.Score, Is.EqualTo(score));
            Assert.That(session.Misses, Is.EqualTo(misses));
            Assert.That(session.Revision, Is.EqualTo(revision));
        }

        static string Signature(ActiveWorkshopSession session) => string.Join(";", session.Cues.Select(c => $"{c.Kind}:{c.First}:{c.Second}:{c.Start:R}:{c.End:R}"));
        static void AdvanceTo(ActiveWorkshopSession session, double time) => session.Advance(Math.Max(0, time - session.Elapsed));
        static ActiveWorkshopSession At(WorkshopCueKind kind) {
            for (int seed = 0; seed < 100; seed++) {
                var session = new ActiveWorkshopSession(seed);
                var cue = session.Cues.FirstOrDefault(c => c.Kind == kind);
                if (cue == null) continue;
                AdvanceTo(session, cue.Start + .001); return session;
            }
            throw new InvalidOperationException("Could not find a representative cue: " + kind);
        }
        static void Pose(WorkshopCueKind kind, out float x, out float y) {
            x = kind == WorkshopCueKind.DodgeLeft ? -.20f : kind == WorkshopCueKind.DodgeRight ? .20f : 0;
            y = kind == WorkshopCueKind.Duck ? -.25f : 0;
        }
        static void Complete(ActiveWorkshopSession session, WorkshopCue cue) {
            AdvanceTo(session, cue.Start + .001);
            if (!cue.IsDodge) {
                session.Hit(cue.First, 0);
                if (cue.Kind == WorkshopCueKind.Double) session.Hit(cue.Second, 1);
            } else {
                AdvanceTo(session, cue.Start + ActiveWorkshopSession.DodgeWarning + .001);
                Pose(cue.Kind, out float x, out float y);
                for (int step = 0; step < 5 && !session.CueResolved; step++) {
                    session.Advance(.05); session.ObserveDodge(x, y, .05);
                }
            }
            Assert.That(session.CueResolved && session.LastSuccess, Is.True, "A physically valid response should complete " + cue.Kind);
        }
    }
}
