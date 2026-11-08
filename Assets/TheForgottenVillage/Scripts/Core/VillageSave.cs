using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgottenVillage {
    [Serializable]
    public class VillageSave {
        public int version = 2;
        public bool elder, photograph, faultyTest, fixedTest, timingViolation, ended;
        public bool[] clues = new bool[5];
        public bool[] puzzleSolved = new bool[5];
        public ObjectEvidence[] objects = new ObjectEvidence[5];
        public bool[] evidence = new bool[4];
        public List<string> inventory = new List<string>();
        public bool ObjectVerified(int id) => id >= 0 && id < 5 && objects != null &&
            objects.Length == 5 && objects[id] != null && objects[id].ValidFor(id);
        public bool CanClaim(int id) => elder && id >= 0 && id < 5 && puzzleSolved != null &&
            puzzleSolved.Length == 5 && puzzleSolved[id] && ObjectVerified(id);
        public bool Unlocked => clues != null && clues.Length == 5 &&
            Enumerable.Range(0, 5).All(i => clues[i] && CanClaim(i));
        public bool Authorized => Unlocked && faultyTest && fixedTest && evidence != null &&
            evidence.Length == 4 && evidence.All(x => x);
        public bool PhysicalChestAuthorized => Authorized && objects.All(x => !x.simulated);
        public void Normalize() {
            if (inventory == null) inventory = new List<string>();
            inventory = inventory.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
            if (clues == null || clues.Length != 5) clues = new bool[5];
            if (puzzleSolved == null || puzzleSolved.Length != 5) puzzleSolved = new bool[5];
            if (objects == null || objects.Length != 5) objects = new ObjectEvidence[5];
            if (evidence == null || evidence.Length != 4) evidence = new bool[4];
            if (version < 2) {
                // Keep completed engineering work; never invent camera evidence.
                for (int i = 0; i < 5; i++) puzzleSolved[i] |= clues[i];
                clues = new bool[5]; objects = new ObjectEvidence[5]; ended = false; version = 2;
            }
            for (int i = 0; i < 5; i++) if (!CanClaim(i)) clues[i] = false;
            if (!Authorized) ended = false;
        }
    }
}
