using System;
namespace ForgottenVillage {
[Serializable] public sealed class PlaneRunState {
 public const double Duration=60;
 public enum Phase { Idle, Countdown, Flying, Paused, Failed, Succeeded, Returning }
 public Phase phase {get;private set;}=Phase.Idle;
 public double elapsed {get;private set;}
 public double Remaining=>Math.Max(0,Duration-elapsed);
 public float Progress=>(float)(elapsed/Duration);
 Phase resume;
 public void Reset(){elapsed=0;phase=Phase.Countdown;resume=Phase.Countdown;}
 public void Launch(){if(phase==Phase.Countdown)phase=Phase.Flying;}
 public void Pause(){if(phase!=Phase.Flying&&phase!=Phase.Countdown)return;resume=phase;phase=Phase.Paused;}
 public void Resume(){if(phase==Phase.Paused)phase=resume;}
 public void Crash(){if(phase==Phase.Flying)phase=Phase.Failed;}
 // Only the exact active portion of a physics step can advance the run.
 public double StepDuration(double dt)=>phase==Phase.Flying?Math.Min(Math.Max(0,dt),Remaining):0;
 public void Advance(double dt){if(phase!=Phase.Flying)return;elapsed+=StepDuration(dt);if(elapsed>=Duration-1e-8){elapsed=Duration;phase=Phase.Succeeded;}}
 public void Return(){phase=Phase.Returning;}
 public void Stop(){phase=Phase.Idle;}
}
}
