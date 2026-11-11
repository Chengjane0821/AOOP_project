using System;
namespace ForgottenVillage {
    public sealed class PowerGridLevel {
        public readonly string Name;
        public readonly int[] Capacity, Demand;
        public readonly bool NeedsBackup;
        public readonly int InitialTrippedSource;
        public const double TimeLimit = 75;
        public const double HoldSeconds = 4;
        public PowerGridLevel(string name, int[] capacity, int[] demand, bool needsBackup = false, int tripped = -1) {
            if(capacity==null||capacity.Length!=2||demand==null||demand.Length<1||demand.Length>5)
                throw new ArgumentException("Grid requires two generators and one to five machines.");
            foreach(int n in capacity)if(n<=0)throw new ArgumentException("Capacity must be positive.");
            foreach(int n in demand)if(n<=0)throw new ArgumentException("Demand must be positive.");
            if(tripped < -1 || tripped>1)throw new ArgumentOutOfRangeException(nameof(tripped));
            Name=name; Capacity=(int[])capacity.Clone(); Demand=(int[])demand.Clone();
            NeedsBackup=needsBackup; InitialTrippedSource=tripped;
        }
    }
}
