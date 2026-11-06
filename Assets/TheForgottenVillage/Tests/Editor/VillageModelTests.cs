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
