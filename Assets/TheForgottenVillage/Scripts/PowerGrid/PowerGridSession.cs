using System;
namespace ForgottenVillage {
    public sealed class PowerGridSession {
        public PowerGridCircuit Circuit {get;private set;}
        public int Round {get;private set;}
        public double Elapsed {get;private set;}
        public double Stable {get;private set;}
        public double Remaining=>Math.Max(0,PowerGridLevel.TimeLimit-Elapsed);
        public bool RoundComplete=>Stable>=PowerGridLevel.HoldSeconds;
        public bool Failed=>!RoundComplete&&Elapsed>=PowerGridLevel.TimeLimit;
        public bool Won=>Round==PowerGridLevels.Count-1&&RoundComplete;
        public PowerGridSession(){Load(0);}
        void Load(int round){Round=round;Circuit=new PowerGridCircuit(PowerGridLevels.Create(round));Elapsed=Stable=0;}
        public void Retry(){Load(Round);}
        public bool Next(){if(!RoundComplete||Won)return false;Load(Round+1);return true;}
        public void Advance(double seconds) {
            if(Failed||RoundComplete||double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)return;
            double step=Math.Min(seconds,Remaining);bool safeBefore=Circuit.Safe;
            Circuit.Advance(step);Elapsed+=step;
            Stable=safeBefore&&Circuit.Safe?Math.Min(PowerGridLevel.HoldSeconds,Stable+step):0;
        }
    }
}
