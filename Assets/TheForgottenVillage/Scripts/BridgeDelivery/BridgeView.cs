using TMPro;
using UnityEngine;

namespace ForgottenVillage {
    public sealed class BridgeView : MonoBehaviour {
        public TMP_Text status, feedback;
        public Renderer water;
        float feedbackUntil, riverTime;
        MaterialPropertyBlock block;
        public void Feedback(string message) { if (feedback) feedback.text = message; feedbackUntil = Time.unscaledTime + 5f; }
        public void Render(BridgeRun run, float dt) {
            if (!run) return;
            if (status) status.text = run.Won ? "ALL DELIVERIES COMPLETE / TALK TO THE CARPENTER" : !run.Running ?
                "BRIDGE DELIVERY / BUILD • TEST • IMPROVE" :
                "DELIVERY " + (run.Round + 1) + " / 3  —  " + BridgeLevels.Name(run.Round) + "\n" +
                (run.RoundComplete ? "DELIVERED!" : run.AwaitingRetry ? "SPLASH! / RETRY TO REPAIR YOUR BUILD" :
                run.Testing ? "CART CROSSING / LOAD " + BridgeLevels.Load(run.Round).ToString("0") :
                "SPAN " + BridgeLevels.RequiredLength(run.Round).ToString("F2") + " m  /  " + run.MissingBoards + " BOARD(S) TO FIT");
            if (feedback) feedback.enabled = !string.IsNullOrEmpty(feedback.text) && Time.unscaledTime <= feedbackUntil;
            riverTime += dt;
            if (water) {
                if (block == null) block = new MaterialPropertyBlock(); water.GetPropertyBlock(block);
                float shimmer = .02f * Mathf.Sin(riverTime * 1.4f);
                block.SetColor("_BaseColor", new Color(.075f + shimmer, .32f + shimmer, .39f + shimmer));
                water.SetPropertyBlock(block);
            }
        }
    }
}
