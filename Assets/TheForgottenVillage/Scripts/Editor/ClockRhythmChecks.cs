using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace ForgottenVillage.Editor {
    public static class ClockRhythmChecks {
        static StringBuilder report;
        static void Check(bool value, string message) { if (!value) throw new Exception(message); report.AppendLine("PASS: " + message); }
        [MenuItem("Forgotten Village/Check Clockmaker rhythm repair")]
        public static void Run() {
            report = new StringBuilder();
            try {
                Check(ClockJudgement.Judge(0) == ClockHit.Perfect && ClockJudgement.Judge(-.09) == ClockHit.Perfect, "Perfect timing boundary");
                Check(ClockJudgement.Judge(.2) == ClockHit.Good && ClockJudgement.Judge(-.201) == ClockHit.Miss, "Good and miss timing boundaries");
                Check(ClockJudgement.Passed(23,32) && !ClockJudgement.Passed(22,32) && !ClockJudgement.Passed(0,0), "70 percent threshold requires 23 of 32 notes");
                var notes = ClockChart.Create();
                Check(notes.Length == 32 && notes.All(n=>n.Lane>=0&&n.Lane<3), "Chart has 32 valid single-hand notes");
                Check(notes.Zip(notes.Skip(1),(a,b)=>b.Time>a.Time).All(x=>x), "Chart notes are strictly chronological");
                var session = new ClockSession(notes);
                foreach(var n in notes) session.Strike(n.Lane,n.Time);
                Check(session.Passed && session.Perfects==32 && session.BestCombo==32, "Full chart can be completed with exact strokes");
                session.Strike(notes[31].Lane,notes[31].Time);
                Check(session.Hits==32 && session.Strays==1, "Duplicate strike never awards the same note twice");
                session = new ClockSession(notes); session.Advance(100);
                Check(session.Finished && !session.Passed && session.Misses==32, "Idle run fails and resolves all missed notes");
                session = new ClockSession(notes); session.Strike(2,notes[0].Time);
                Check(session.Hits==0 && session.Strays==1, "Wrong bell does not consume a correct note");
                for(int i=0;i<32;i++) session.Strike(notes[i].Lane,notes[i].Time);
                for(int i=0;i<9;i++) session.Strike(0,100+i);
                Check(!session.Passed, "Excessive stray strikes cannot pass by key spamming");
                var save = new VillageSave { version=4,elder=true }; save.puzzleSolved[4]=true; save.Normalize();
                Check(save.version==5&&!save.clockRhythmWon&&!save.puzzleSolved[4], "Unfinished legacy timing progress does not invent a rhythm victory");
                save = new VillageSave {version=4,elder=true}; save.puzzleSolved[4]=save.clues[4]=true;
                save.objects[4]=new ObjectEvidence { requestId=Guid.NewGuid().ToString("N"),objectName="clock",snapshot="",capturedUtc=DateTimeOffset.UtcNow.ToString("O"),confidence=.99f,frames=3,simulated=true};
                save.Normalize(); Check(save.clockRhythmWon&&save.clockMemoryRecovered&&save.CanClaim(4)&&save.clues[4], "Earned legacy clock clue survives migration");
                var restored=JsonUtility.FromJson<VillageSave>(JsonUtility.ToJson(save)); restored.Normalize();
                Check(restored.CanClaim(4), "Schema five JSON round trip retains completion");
                restored.clockMemoryRecovered=false; Check(!restored.CanClaim(4), "Memory is required before clock clue claim");
                restored.clockMemoryRecovered=true; restored.objects[4]=null; Check(!restored.CanClaim(4), "Object verification remains mandatory");
                report.AppendLine("PASS: deterministic rhythm and save checks completed.");
            } catch(Exception e) { report.AppendLine("FAIL: "+e); throw; }
            finally { File.WriteAllText("Verification/clock-rhythm-model.txt",report.ToString()); }
        }
    }
}
