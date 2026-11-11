using System;
namespace ForgottenVillage {
    public sealed class PowerGridCircuit {
        public readonly PowerGridLevel Level;
        readonly int[] assignments;
        readonly bool[] tripped = new bool[2];
        readonly double[] heat = new double[2];
        public int Trips {get;private set;}
        public const double TripSeconds=3;
        public bool MainPower { get; private set; }
        public bool BackupOnline { get; private set; }
        public int MachineCount => assignments.Length;
        public PowerGridCircuit(PowerGridLevel level) {
            Level=level??throw new ArgumentNullException(nameof(level));
            assignments=new int[level.Demand.Length];
            for(int i=0;i<assignments.Length;i++)assignments[i]=-1;
            if(level.InitialTrippedSource>=0)tripped[level.InitialTrippedSource]=true;
        }
        public int SourceFor(int machine) => machine>=0&&machine<MachineCount?assignments[machine]:-1;
        public int Capacity(int source) => source>=0&&source<2?Level.Capacity[source]+(source==1&&BackupOnline?2:0):0;
        public int Load(int source) {
            if(source<0||source>1)return 0;
            int total=0;for(int i=0;i<MachineCount;i++)if(assignments[i]==source)total+=Level.Demand[i];return total;
        }
        public bool Tripped(int source) => source>=0&&source<2&&tripped[source];
        public double Heat(int source) => source>=0&&source<2?heat[source]:0;
        public void Advance(double seconds) {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            for(int source=0;source<2;source++) {
                if(tripped[source])continue;
                if(MainPower&&Load(source)>Capacity(source)) {
                    heat[source]=Math.Min(1,heat[source]+seconds/TripSeconds);
                    if(heat[source]>=1){tripped[source]=true;Trips++;}
                } else heat[source]=Math.Max(0,heat[source]-seconds/TripSeconds);
            }
        }
        public bool ResetBreaker(int source) {
            if(source<0||source>1||Load(source)>Capacity(source))return false;
            tripped[source]=false;heat[source]=0;return true;
        }
        public bool Connect(int machine,int source) {
            if(machine<0||machine>=MachineCount||source< -1||source>1)return false;
            assignments[machine]=source;return true;
        }
        public void TogglePower(){MainPower=!MainPower;}
        public bool ToggleBackup(){if(!Level.NeedsBackup)return false;BackupOnline=!BackupOnline;return true;}
        public bool Powered(int machine) {
            int source=SourceFor(machine);return source>=0&&MainPower&&!tripped[source];
        }
        public bool Safe {
            get {
                if(!MainPower||(Level.NeedsBackup&&!BackupOnline))return false;
                for(int i=0;i<2;i++)if(Tripped(i)||Load(i)>Capacity(i))return false;
                for(int i=0;i<MachineCount;i++)if(!Powered(i))return false;
                return true;
            }
        }
    }
}
