using TMPro;
using UnityEngine;

namespace ForgottenVillage {
    public sealed class ActiveWorkshopView : MonoBehaviour {
        public TMP_Text status, feedback, score, time, combo, legend;
        public Renderer[] targetRenderers = new Renderer[6];
        public Transform[] cueFills = new Transform[6];
        public Renderer obstacleRenderer;
        public Transform progressFill;
        readonly Vector3[] initialScale = new Vector3[6];
        readonly Vector3[] initialCueScale = new Vector3[6];
        MaterialPropertyBlock block;
        float animationTime, feedbackTime;
        bool recorded;
        Vector3 fillScale;
        public void Feedback(string message, bool good) {
            if (feedback) { feedback.text = message; feedback.color = good ? new Color(.75f, 1, .81f) : new Color(1, .64f, .43f); }
            feedbackTime = 2.2f;
        }
        public void Render(ActiveWorkshopRun run, float dt) {
            if (!recorded) {
                for (int i = 0; i < run.targetVisuals.Length && i < 6; i++) if (run.targetVisuals[i]) initialScale[i] = run.targetVisuals[i].localScale;
                for (int i = 0; i < cueFills.Length && i < 6; i++) if (cueFills[i]) initialCueScale[i] = cueFills[i].localScale;
                if (progressFill) fillScale = progressFill.localScale;
                recorded = true;
            }
            animationTime += dt; feedbackTime = Mathf.Max(0, feedbackTime - dt);
            var session = run.Session; var cue = session != null ? session.Current : null;
            bool active = run.Running && run.CountInRemaining <= 0 && cue != null && !session.CueResolved;
            if (score) score.text = (session == null ? "0000" : session.Score.ToString("0000")) + " / " + ActiveWorkshopSession.Goal;
            if (time) time.text = (session == null ? 60 : Mathf.CeilToInt((float)session.Remaining)) + "s";
            if (combo) combo.text = session == null ? "0 / x1" : session.Combo + " / x" + session.Multiplier;
            if (legend) legend.text = run.IsVR ?
                "Touch lit targets\nTwo hands for pairs\n\nLean left / right\nLower head to duck\nMenu: pause\n\n900 points in 60s" :
                "Mouse L / R: hit\nBoth buttons: pair\n\nA / D: lean\nS: duck\nEsc: pause\n\n900 points in 60s";
            if (status) {
                if (!run.Running) status.text = "GOAL " + ActiveWorkshopSession.Goal + " / BEST " + run.BestScore;
                else if (run.IsPaused) status.text = "PAUSED / YOUR PATTERN IS WAITING";
                else if (run.CountInRemaining > 0) status.text = "GET READY  " + Mathf.CeilToInt((float)run.CountInRemaining) + " / REACH & DODGE";
                else {
                    string instruction = !active ? "NEXT TARGET..." : cue.IsDodge ?
                        cue.Kind == WorkshopCueKind.DodgeLeft ? "<<< LEAN LEFT" : cue.Kind == WorkshopCueKind.DodgeRight ? "LEAN RIGHT >>>" : "DUCK DOWN vvv" :
                        cue.Kind == WorkshopCueKind.Double ? "BOTH HANDS / " + (cue.First + 1) + " + " + (cue.Second + 1) : "HIT TARGET " + (cue.First + 1);
                    status.text = "STAGE " + (session.Elapsed < 20 ? 1 : session.Elapsed < 40 ? 2 : 3) + " / " + instruction;
                }
            }
            if (feedback) {
                if (run.IsVR && run.Running && run.CountInRemaining <= 0 && (!run.HeadTracked || (!run.LeftTracked && !run.RightTracked))) {
                    feedback.text = "TRACKING / keep headset and hands visible; pause to recenter"; feedback.enabled = true;
                } else feedback.enabled = feedbackTime > 0;
            }
            for (int i = 0; i < run.targetVisuals.Length && i < 6; i++) {
                bool lit = active && !cue.IsDodge && (i == cue.First || i == cue.Second);
                float life = lit ? Mathf.Clamp01((float)((cue.End - session.Elapsed) / (cue.End - cue.Start))) : 0;
                var visual = run.targetVisuals[i];
                if (visual) {
                    visual.gameObject.SetActive(!run.Running || lit);
                    float pop = lit ? Mathf.SmoothStep(.35f, 1, Mathf.Clamp01((float)(session.Elapsed - cue.Start) / .12f)) : 1;
                    visual.localScale = initialScale[i] * pop * (lit ? 1 + .035f * Mathf.Sin(animationTime * 8) : 1);
                    if (run.targetPoints[i]) {
                        var collider = run.targetPoints[i].GetComponent<Collider>(); if (collider) collider.enabled = lit;
                    }
                }
                if (i < cueFills.Length && cueFills[i]) {
                    cueFills[i].gameObject.SetActive(lit);
                    cueFills[i].localScale = new Vector3(initialCueScale[i].x * life, initialCueScale[i].y, initialCueScale[i].z);
                }
                if (i < targetRenderers.Length && targetRenderers[i]) Tint(targetRenderers[i], lit ?
                    life < .25f ? new Color(1, .36f, .22f) : cue.Kind == WorkshopCueKind.Double ? new Color(.72f, .49f, 1) : new Color(.18f, .83f, .73f) : new Color(.21f, .34f, .40f));
            }
            if (progressFill) progressFill.localScale = new Vector3(fillScale.x * Mathf.Clamp01(session == null ? 0 : session.Score / (float)ActiveWorkshopSession.Goal), fillScale.y, fillScale.z);
            bool dodge = active && cue.IsDodge;
            if (run.obstacleVisual) {
                run.obstacleVisual.gameObject.SetActive(dodge);
                if (dodge) {
                    Vector3 head = run.transform.InverseTransformPoint(run.NeutralHead);
                    float approach = Mathf.Clamp01((float)(session.Elapsed - cue.Start - ActiveWorkshopSession.DodgeWarning) / .55f);
                    bool duck = cue.Kind == WorkshopCueKind.Duck;
                    run.obstacleVisual.localPosition = head + new Vector3(duck ? 0 : cue.Kind == WorkshopCueKind.DodgeLeft ? .25f : -.25f, duck ? .02f : -.10f, Mathf.Lerp(.72f, .12f, approach));
                    run.obstacleVisual.localScale = duck ? new Vector3(1.25f, .14f, .10f) : new Vector3(.42f, .75f, .10f);
                    if (obstacleRenderer) Tint(obstacleRenderer, approach <= 0 ? new Color(1, .68f, .12f) : new Color(1, .28f, .12f));
                }
            }
        }
        void Tint(Renderer renderer, Color color) {
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
        }
    }
}
