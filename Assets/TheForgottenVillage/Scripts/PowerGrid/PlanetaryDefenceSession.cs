using System;

namespace ForgottenVillage {
    // Uses the existing Technician reward/save fields; no save schema change.
    public sealed class PlanetaryDefenceSession {
        public const int ThreatCount = 20, StartingIntegrity = 5;
        public const double CountIn = 3, SpawnInterval = 1.35;
        readonly bool[] resolved = new bool[ThreatCount];
        public double Elapsed { get; private set; }
        public int Spawned { get; private set; }
        public int Destroyed { get; private set; }
        public int Impacts { get; private set; }
        public int Integrity => StartingIntegrity - Impacts;
        public int Resolved => Destroyed + Impacts;
        public bool Failed => Integrity <= 0;
        public bool Won => !Failed && Resolved == ThreatCount;
        public bool Finished => Failed || Won;
        public void Advance(double dt) {
            if (!Finished && !double.IsNaN(dt) && !double.IsInfinity(dt) && dt > 0) Elapsed += dt;
        }
        public bool TrySpawn(out int id) {
            id = -1;
            if (Finished || Spawned >= ThreatCount || Elapsed < CountIn + Spawned * SpawnInterval) return false;
            id = Spawned++;
            return true;
        }
        public bool Resolve(int id, bool intercepted) {
            if (Finished || id < 0 || id >= Spawned || resolved[id]) return false;
            resolved[id] = true;
            if (intercepted) Destroyed++; else Impacts++;
            return true;
        }
    }
}
