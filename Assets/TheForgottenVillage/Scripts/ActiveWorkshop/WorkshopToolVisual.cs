using UnityEngine;

namespace ForgottenVillage {
    // Visual-only attachment to the original dodge warning transform. Cue timing,
    // movement, head/contact rules and score continue to belong to Quick Hands.
    public sealed class WorkshopToolVisual : MonoBehaviour {
        public ActiveWorkshopRun run;
        public Transform[] tools;
        public Renderer[] renderers;
        public GameObject instructionsPanel;
        public Transform scorePanel;
        Vector3 scoreRestPosition;
        bool instructionsWereVisible;
        MaterialPropertyBlock block;
        void OnEnable() {
            instructionsWereVisible = instructionsPanel && instructionsPanel.activeSelf;
            if (instructionsPanel) instructionsPanel.SetActive(false);
            if (scorePanel) { scoreRestPosition = scorePanel.localPosition; scorePanel.localPosition += Vector3.up * .65f; }
        }
        void OnDisable() {
            if (instructionsPanel && instructionsWereVisible) instructionsPanel.SetActive(true);
            if (scorePanel) scorePanel.localPosition = scoreRestPosition;
        }
        void LateUpdate() {
            if (!run || run.Session == null || tools == null || tools.Length == 0) return;
            if (block == null) block = new MaterialPropertyBlock();
            int selected = run.Session.CurrentIndex % tools.Length;
            Vector3 inherited = transform.lossyScale;
            for (int i = 0; i < tools.Length; i++) if (tools[i]) {
                tools[i].gameObject.SetActive(i == selected);
                // The legacy warning is flattened/nonuniform. Keep a real tool's
                // proportions while retaining that warning's exact trajectory.
                tools[i].localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(inherited.x)), 1 / Mathf.Max(.001f, Mathf.Abs(inherited.y)), 1 / Mathf.Max(.001f, Mathf.Abs(inherited.z)));
                tools[i].localRotation = Quaternion.identity;
                bool duck = run.Session.Current != null && run.Session.Current.Kind == WorkshopCueKind.Duck;
                // Rotate below the compensation frame, so S*R*S^-1 cannot
                // shear a saw or hammer when the old warning flattens.
                tools[i].GetChild(0).localRotation = Quaternion.Euler(0, 0, duck ? 90 : -12);
            }
            foreach (var renderer in renderers) if (renderer) {
                block.SetColor("_BaseColor", new Color(1, .88f, .72f)); renderer.SetPropertyBlock(block);
            }
        }
    }
}
