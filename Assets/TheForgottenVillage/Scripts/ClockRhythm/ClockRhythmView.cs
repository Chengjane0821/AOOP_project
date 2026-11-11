using TMPro;
using UnityEngine;
namespace ForgottenVillage {
    public sealed class ClockRhythmView : MonoBehaviour {
        public Transform[] hands = new Transform[3];
        public Renderer[] faces = new Renderer[3];
        public TMP_Text status, feedback;
        MaterialPropertyBlock block;
        float feedbackUntil;
        public void Feedback(string text) { if (feedback) feedback.text = text; feedbackUntil = Time.unscaledTime + .65f; }
        public void Render(ClockSession session, double time, ClockRhythmTuning tuning, bool repaired) {
            if (block == null) block = new MaterialPropertyBlock();
            for (int lane = 0; lane < 3; lane++) {
                double remaining = double.PositiveInfinity;
                if (session != null) for (int i = 0; i < session.Notes.Length; i++)
                    if (!session.IsResolved(i) && session.Notes[i].Lane == lane) { remaining = session.Notes[i].Time - time; break; }
                bool visible = remaining <= ClockJudgement.PreviewSeconds;
                Color color = repaired ? tuning.perfect : visible ? tuning.incoming : tuning.idle;
                if (visible && System.Math.Abs(remaining) <= ClockJudgement.GoodWindow) color = tuning.perfect;
                if (faces[lane]) { block.SetColor("_BaseColor", color); faces[lane].SetPropertyBlock(block); }
                if (hands[lane]) {
                    float angle = repaired ? (float)(Time.unscaledTime * -45) : visible ? (float)(remaining / ClockJudgement.PreviewSeconds * 360) : 180;
                    hands[lane].localRotation = Quaternion.Euler(0, 0, angle);
                }
            }
            if (status && session != null) status.text = time < ClockChart.CountIn * ClockChart.Beat ?
                "GET READY  " + Mathf.CeilToInt((float)((ClockChart.CountIn * ClockChart.Beat - time) / ClockChart.Beat)) :
                "REPAIR " + session.Hits + "/" + session.Notes.Length + "   COMBO " + session.Combo + "\n" +
                "MISSES " + session.Misses + "   EXTRA " + session.Strays + "   TARGET 70%";
            if (feedback && Time.unscaledTime > feedbackUntil) feedback.text = "";
        }
    }
}
