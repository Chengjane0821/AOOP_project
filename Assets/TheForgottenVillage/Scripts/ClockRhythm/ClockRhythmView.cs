using TMPro;
using UnityEngine;
namespace ForgottenVillage {
    public sealed class ClockRhythmView : MonoBehaviour {
        public Transform[] hands = new Transform[ClockChart.LaneCount];
        public Renderer[] faces = new Renderer[ClockChart.LaneCount];
        public Renderer[] halos=new Renderer[ClockChart.LaneCount],approachLights=new Renderer[ClockChart.LaneCount*12];
        public Transform[] bells=new Transform[ClockChart.LaneCount];
        public Transform progress;
        public TMP_Text comboLabel,accuracyLabel,phraseLabel;
        public TMP_Text status, feedback;
        MaterialPropertyBlock block;
        float feedbackUntil;
        readonly float[] pulseUntil=new float[ClockChart.LaneCount];
        readonly ClockHit[] lastHit=new ClockHit[ClockChart.LaneCount];
        public void Feedback(string text) { if (feedback) feedback.text = text; feedbackUntil = Time.unscaledTime + .65f; }
        public void Pulse(int lane,ClockHit result){if(lane<0||lane>=ClockChart.LaneCount)return;pulseUntil[lane]=Time.unscaledTime+.22f;lastHit[lane]=result;if(feedback)feedback.color=result==ClockHit.Perfect?new Color(1,.82f,.35f):result==ClockHit.Good?new Color(.3f,.92f,.77f):new Color(1,.45f,.35f);}
        public void Render(ClockSession session, double time, ClockRhythmTuning tuning, bool repaired) {
            if (block == null) block = new MaterialPropertyBlock();
            for (int lane = 0; lane < ClockChart.LaneCount; lane++) {
                double remaining = double.PositiveInfinity;
                if (session != null) for (int i = 0; i < session.Notes.Length; i++)
                    if (!session.IsResolved(i) && session.Notes[i].Lane == lane) { remaining = session.Notes[i].Time - time; break; }
                bool visible = remaining <= ClockJudgement.PreviewSeconds;
                bool atMark=visible&&System.Math.Abs(remaining)<=ClockJudgement.GoodWindow;
                float pulse=Mathf.Clamp01((pulseUntil[lane]-Time.unscaledTime)/.22f);
                Color color=repaired||atMark?tuning.perfect:visible?tuning.incoming:new Color(.08f,.15f,.16f);
                if(pulse>0)color=lastHit[lane]==ClockHit.Miss?tuning.missed:lastHit[lane]==ClockHit.Perfect?tuning.perfect:tuning.incoming;
                if(lane<faces.Length&&faces[lane]){block.Clear();block.SetColor("_BaseColor",new Color(.93f,.94f,.91f));faces[lane].SetPropertyBlock(block);}
                if(lane<halos.Length&&halos[lane]){block.Clear();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*(visible||pulse>0||repaired?1.0f:.1f));halos[lane].SetPropertyBlock(block);}
                float approach=visible?Mathf.Clamp01(1-(float)(remaining/ClockJudgement.PreviewSeconds)):0;
                for(int led=0;led<12;led++){
                    int index=lane*12+led;if(index>=approachLights.Length)continue;var light=approachLights[index];if(!light)continue;
                    bool lit=repaired||atMark||pulse>0||led<Mathf.CeilToInt(approach*12);
                    var ledColor=lit?color:new Color(.055f,.10f,.105f);block.Clear();block.SetColor("_BaseColor",ledColor);block.SetColor("_EmissionColor",lit?ledColor*.8f:Color.black);light.SetPropertyBlock(block);
                }
                if(lane<bells.Length&&bells[lane])bells[lane].localPosition=Vector3.down*(Mathf.Sin(pulse*Mathf.PI)*.022f);
                if (lane<hands.Length&&hands[lane]) {
                    float angle = repaired ? (float)(Time.unscaledTime * -45) : visible ? (float)(remaining / ClockJudgement.PreviewSeconds * 360) : 180;
                    hands[lane].localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
            if(status)status.text=repaired?"CLOCKS RESTORED":session==null?"READY TO RESTORE":time<ClockChart.CountIn*ClockChart.Beat?
                "GET READY  "+Mathf.CeilToInt((float)((ClockChart.CountIn*ClockChart.Beat-time)/ClockChart.Beat)).ToString("00"):
                "REPAIRED "+session.Hits.ToString("00")+" / "+session.Notes.Length;
            if(comboLabel)comboLabel.text="COMBO "+(session==null?"00":session.Combo.ToString("00"));
            if(accuracyLabel)accuracyLabel.text=session==null?ClockChart.RequiredHits+" / "+ClockChart.NoteCount+" NOTES   +   MAX "+ClockChart.MaxStrays+" EXTRA":"TARGET "+session.RequiredHits+" HITS   /   MISSED "+session.Misses+"   /   EXTRA "+session.Strays+" OF "+session.MaxStrays;
            if(phraseLabel)phraseLabel.text=repaired?"SIX BELLS / ONE SHARED MEMORY":session==null?"TOP: A S D     /     LOWER: J K L":ClockChart.PhraseName(session.Resolved);
            if(progress)progress.localScale=new Vector3(repaired?1:session==null?0:session.Resolved/(float)session.Notes.Length,1,1);
            if (feedback && Time.unscaledTime > feedbackUntil) feedback.text = "";
        }
    }
}
