using NUnit.Framework;

namespace ForgottenVillage.Tests {
    public class VillageModelTests {
        [Test]
        public void NormalizeRepairsMissingProgressArraysAndInventory() {
            var save = new VillageSave { clues = null, evidence = null, inventory = null };

            save.Normalize();

            Assert.That(save.clues, Has.Length.EqualTo(5));
            Assert.That(save.evidence, Has.Length.EqualTo(4));
            Assert.That(save.inventory, Is.Not.Null);
        }

        [Test]
        public void MalformedEvidenceCannotAuthorizeFinalGate() {
            var save = new VillageSave {
                elder = true,
                clues = new[] { true, true, true, true, true },
                faultyTest = true,
                fixedTest = true,
                evidence = null
            };

            Assert.That(save.Authorized, Is.False);

            save.Normalize();
            Assert.That(save.Authorized, Is.False);
        }

        [Test]
        public void EngineeringTargetsMatchProposal() {
            Assert.That(Engineering.DividerSolved(20000), Is.True);
            Assert.That(Engineering.FilterSolved(5000), Is.True);
            Assert.That(Engineering.GainSolved(6750), Is.True);
            Assert.That(Engineering.Slack(11.5, 1, .5, 1, 9), Is.EqualTo(0).Within(.0001));
            Assert.That(Engineering.Slack(10, 1, .5, 1, 9), Is.LessThan(0));
        }

        [TestCase("ACK:GATE:OPEN", true, "ACK:GATE:OPEN", 0)]
        [TestCase("BTN:1", true, "BTN", 1)]
        [TestCase("POT:512", true, "POT", 512)]
        [TestCase("POT:1024", false, "", 0)]
        [TestCase("garbage", false, "", 0)]
        public void SerialProtocolAcceptsOnlyBoundedMessages(string input, bool expected, string expectedKind, int expectedValue) {
            var parsed = SerialProtocol.Parse(input, out var kind, out var value);

            Assert.That(parsed, Is.EqualTo(expected));
            Assert.That(kind, Is.EqualTo(expectedKind));
            Assert.That(value, Is.EqualTo(expectedValue));
        }
    }
}
