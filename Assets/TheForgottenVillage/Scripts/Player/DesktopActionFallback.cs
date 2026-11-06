using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgottenVillage {
    /// <summary>
    /// Keeps desktop E interaction useful for scene objects that expose a
    /// VillageAction without also having a VillageInteractable component.
    /// The existing player script remains untouched so generated scenes keep
    /// their serialized references and legacy encoding.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class DesktopActionFallback : MonoBehaviour {
        static DesktopActionFallback instance;
        VillagePlayer player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install() {
            if (instance) return;
            var host = new GameObject("Desktop Action Fallback");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DesktopActionFallback>();
        }

        void Update() {
            if (!player) player = FindFirstObjectByType<VillagePlayer>();
            if (!player || player.IsVR || !player.game || !player.game.playing ||
                player.game.ui.IsOpen || Keyboard.current == null) return;

            if (!Physics.Raycast(player.View.transform.position, player.View.transform.forward, out var hit, 4f)) return;
            if (hit.collider.GetComponentInParent<VillageInteractable>()) return;
            var action = hit.collider.GetComponentInParent<VillageAction>();
            if (!action) return;
            player.game.ui.Prompt("E - " + PromptFor(action));
            if (Keyboard.current.eKey.wasPressedThisFrame) action.Use();
        }

        static string PromptFor(VillageAction action) {
            switch (action.kind) {
                case "enter": return "Enter house";
                case "exit": return "Return to village";
                case "dialogue": return action.index == 0 ? "Speak to the Village Elder" : "Speak";
                case "puzzle": return "Open engineering station";
                case "evidence": return "Review laboratory evidence";
                case "final": return "Execute verified safe-state command";
                case "photo": return "Inspect photograph";
                case "pickup": return "Collect evidence";
                default: return "Interact";
            }
        }
    }
}
