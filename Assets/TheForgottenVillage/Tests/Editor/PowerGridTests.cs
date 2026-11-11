using System;
using NUnit.Framework;
using UnityEngine;
namespace ForgottenVillage.Tests {
    public sealed class PowerGridTests {
        [Test] public void EveryRoundHasReachableBalancedSolution(){
            for(int round=0;round<PowerGridLevels.Count;round++){
                var circuit=new PowerGridCircuit(PowerGridLevels.Create(round));bool found=false;
                if(circuit.Level.NeedsBackup)circuit.ToggleBackup();
                for(int mask=0;mask<(1<<circuit.MachineCount);mask++){
                    for(int i=0;i<circuit.MachineCount;i++)circuit.Connect(i,(mask>>i)&1);
                    if(circuit.Load(0)<=circuit.Capacity(0)&&circuit.Load(1)<=circuit.Capacity(1)){found=true;break;}
                }
                Assert.That(found,Is.True,"No solution for round "+round);
            }
        }
        [Test] public void CablesMoveInsteadOfDuplicatingLoad(){var c=new PowerGridCircuit(PowerGridLevels.Create(0));c.Connect(2,0);Assert.That(c.Load(0),Is.EqualTo(4));c.Connect(2,1);Assert.That(c.Load(0),Is.Zero);Assert.That(c.Load(1),Is.EqualTo(4));c.Connect(2,-1);Assert.That(c.Load(1),Is.Zero);}
        [Test] public void InvalidSocketAndSourceDoNotMutateCircuit(){var c=new PowerGridCircuit(PowerGridLevels.Create(0));Assert.IsFalse(c.Connect(5,0));Assert.IsFalse(c.Connect(0,2));Assert.IsFalse(c.Connect(-1,0));Assert.That(c.SourceFor(0),Is.EqualTo(-1));}
        [Test] public void OverloadTripsAfterThreeActiveSeconds(){var c=Overloaded();c.Advance(2.9);Assert.IsFalse(c.Tripped(0));c.Advance(.11);Assert.IsTrue(c.Tripped(0));Assert.That(c.Trips,Is.EqualTo(1));Assert.IsFalse(c.Powered(2));c.Advance(10);Assert.That(c.Trips,Is.EqualTo(1));}
        [Test] public void OverloadCannotBeResetUntilLoadsMove(){var c=Overloaded();c.Advance(3);Assert.IsFalse(c.ResetBreaker(0));c.Connect(2,1);Assert.IsTrue(c.ResetBreaker(0));Assert.IsFalse(c.Tripped(0));Assert.That(c.Heat(0),Is.Zero);}
        [Test] public void PowerOffCoolsWithoutTripping(){var c=Overloaded();c.Advance(1.5);c.TogglePower();c.Advance(3);Assert.That(c.Heat(0),Is.Zero);Assert.IsFalse(c.Tripped(0));Assert.IsFalse(c.Powered(0));}
        [Test] public void SecondRoundStartsWithRepairableBreaker(){var c=new PowerGridCircuit(PowerGridLevels.Create(1));Assert.IsTrue(c.Tripped(0));Assert.IsTrue(c.ResetBreaker(0));Assert.IsFalse(c.Tripped(0));}
        [Test] public void FinalRoundRequiresBackupCapacity(){var c=new PowerGridCircuit(PowerGridLevels.Create(2));Assert.That(c.Capacity(1),Is.EqualTo(6));c.ToggleBackup();Assert.That(c.Capacity(1),Is.EqualTo(8));c.ToggleBackup();Assert.That(c.Capacity(1),Is.EqualTo(6));}
        [Test] public void EarlierRoundsCannotInventBackupCapacity(){var c=new PowerGridCircuit(PowerGridLevels.Create(0));Assert.IsFalse(c.ToggleBackup());Assert.That(c.Capacity(1),Is.EqualTo(6));}
        [Test] public void EveryMachineMustBeConnectedAndStable(){var s=new PowerGridSession();Balance(s.Circuit);s.Advance(3.9);Assert.IsFalse(s.RoundComplete);s.Advance(.1);Assert.IsTrue(s.RoundComplete);Assert.IsTrue(s.Next());Assert.That(s.Round,Is.EqualTo(1));}
        [Test] public void LosingPowerResetsStability(){var s=new PowerGridSession();Balance(s.Circuit);s.Advance(2);s.Circuit.TogglePower();s.Advance(.1);Assert.That(s.Stable,Is.Zero);}
        [Test] public void TimeoutFailsAndRetryResetsOnlyCurrentRound(){var s=new PowerGridSession();Balance(s.Circuit);s.Advance(4);s.Next();s.Advance(75);Assert.IsTrue(s.Failed);s.Retry();Assert.That(s.Round,Is.EqualTo(1));Assert.That(s.Elapsed,Is.Zero);Assert.That(s.Circuit.SourceFor(0),Is.EqualTo(-1));Assert.IsTrue(s.Circuit.Tripped(0));}
        [Test] public void ThreeActualRoundsAreRequiredForWin(){var s=new PowerGridSession();Assert.IsFalse(s.Next());for(int i=0;i<3;i++){Balance(s.Circuit);s.Advance(4);Assert.IsTrue(s.RoundComplete);Assert.That(s.Won,Is.EqualTo(i==2));if(i<2)Assert.IsTrue(s.Next());}Assert.IsFalse(s.Next());}
        [Test] public void NonFiniteAndNegativeTimeCannotCorruptState(){var s=new PowerGridSession();s.Advance(double.NaN);s.Advance(double.PositiveInfinity);s.Advance(-1);Assert.That(s.Elapsed,Is.Zero);Assert.That(s.Circuit.Heat(0),Is.Zero);}
        [Test] public void GridClueRequiresWinMemoryAndObject(){var s=CompletedSave();s.technicianGridWon=false;Assert.IsFalse(s.CanClaim(3));s.technicianGridWon=true;s.technicianMemoryRecovered=false;Assert.IsFalse(s.CanClaim(3));s.technicianMemoryRecovered=true;s.objects[3]=null;Assert.IsFalse(s.CanClaim(3));}
        [Test] public void EarnedLegacyClueAndOtherProgressSurviveMigration(){var s=CompletedSave();s.version=5;s.clockRhythmWon=true;s.elderTennisWon=true;s.technicianGridWon=s.technicianMemoryRecovered=false;s.Normalize();Assert.That(s.version,Is.EqualTo(VillageSave.CurrentVersion));Assert.IsTrue(s.CanClaim(3)&&s.clues[3]);Assert.IsTrue(s.clockRhythmWon&&s.elderTennisWon);}
        [Test] public void IncompleteLegacyDiagnosticIsNotGridVictory(){var s=new VillageSave{version=5,elder=true,faultyTest=true,fixedTest=true};s.puzzleSolved[3]=true;s.Normalize();Assert.IsFalse(s.technicianGridWon||s.puzzleSolved[3]);Assert.IsTrue(s.faultyTest&&s.fixedTest);}
        [Test] public void NewSaveRoundTripRetainsGridRecord(){var s=CompletedSave();var restored=JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(s));restored.Normalize();Assert.IsTrue(restored.CanClaim(3)&&restored.clues[3]);}
        [Test] public void RecoveryDiagnosticsPreserveFinalEvidenceMeaning(){Assert.IsTrue(Engineering.ControllerAllowsRun(false,true));Assert.IsFalse(Engineering.ControllerAllowsRun(true,true));}
        static PowerGridCircuit Overloaded(){var c=new PowerGridCircuit(PowerGridLevels.Create(0));for(int i=0;i<3;i++)c.Connect(i,0);c.TogglePower();return c;}
        static VillageSave CompletedSave(){var s=new VillageSave{elder=true,technicianGridWon=true,technicianMemoryRecovered=true,faultyTest=true,fixedTest=true};s.puzzleSolved[3]=s.clues[3]=true;s.objects[3]=new ObjectEvidence{requestId=Guid.NewGuid().ToString("N"),objectName="cell phone",capturedUtc=DateTimeOffset.UtcNow.ToString("O"),snapshot="",confidence=.99f,frames=3,simulated=true};return s;}
        public static void Balance(PowerGridCircuit circuit){
            if(circuit.Level.NeedsBackup&&!circuit.BackupOnline)circuit.ToggleBackup();
            for(int mask=0;mask<(1<<circuit.MachineCount);mask++){
                for(int i=0;i<circuit.MachineCount;i++)circuit.Connect(i,(mask>>i)&1);
                if(circuit.Load(0)<=circuit.Capacity(0)&&circuit.Load(1)<=circuit.Capacity(1))break;
            }
            circuit.ResetBreaker(0);circuit.ResetBreaker(1);if(!circuit.MainPower)circuit.TogglePower();
        }
    }
}
