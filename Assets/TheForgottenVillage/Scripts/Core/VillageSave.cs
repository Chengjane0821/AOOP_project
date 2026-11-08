using System;
using System.Collections.Generic;
using System.Linq;

namespace ForgottenVillage {
    [Serializable]
    public class VillageSave {
        public int version = 3;
        public bool elderTennisWon, elderMemoryRecovered;
        public bool elder, photograph, faultyTest, fixedTest, timingViolation, ended;
        public bool[] clues = new bool[5];
        public bool[] puzzleSolved = new bool[5];
        public ObjectEvidence[] objects = new ObjectEvidence[5];
        public bool[] evidence = new bool[4];
        public List<string> inventory = new List<string>();
        public bool ObjectVerified(int id) => id >= 0 && id < 5 && objects != null &&
            objects.Length == 5 && objects[id] != null && objects[id].ValidFor(id);
        public bool CanClaim(int id) => elder && id >= 0 && id < 5 && puzzleSolved != null &&
            puzzleSolved.Length == 5 && puzzleSolved[id] && (id == 0 ? elderTennisWon && elderMemoryRecovered && photograph : ObjectVerified(id));
        public bool Unlocked => clues != null && clues.Length == 5 &&
            Enumerable.Range(0, 5).All(i => clues[i] && CanClaim(i));
        public bool Authorized => Unlocked && faultyTest && fixedTest && evidence != null &&
            evidence.Length == 4 && evidence.All(x => x);
        public bool PhysicalChestAuthorized => Authorized && Enumerable.Range(1, 4).All(i => ObjectVerified(i) && !objects[i].simulated);
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
            if (version < 3) {
                // Retain an already earned Elder clue; an unfinished divider is not a tennis victory.
                elderTennisWon = elderMemoryRecovered = clues[0];
                photograph = clues[0]; puzzleSolved[0] = clues[0];
                inventory.Remove("The Forgotten Person / Damaged Circuit Notes");
                if (clues[0] && !inventory.Contains(VillageGame.Clues[0])) inventory.Add(VillageGame.Clues[0]);
                version = 3;
            }
            for (int i = 0; i < 5; i++) if (!CanClaim(i)) clues[i] = false;
            if (!Authorized) ended = false;
        }
    }
}
