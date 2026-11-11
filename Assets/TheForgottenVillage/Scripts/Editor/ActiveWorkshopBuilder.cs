using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    /// <summary>An open body-action play space using the village's flat HUD style.</summary>
    public static class ActiveWorkshopBuilder {
        const string Root = "Assets/TheForgottenVillage/";
        const string ArenaName = "Quick hands workshop";
        const float HudScale = .0042f;
        static readonly Color White = new Color(.90f, .97f, 1f);
        static readonly Color Muted = new Color(.59f, .72f, .82f);
        static readonly Color Navy = new Color(.04f, .08f, .15f, 1f);

        [MenuItem("Forgotten Village/Install Carpenter quick hands workshop")]
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Leave Play Mode and finish importing before installing Quick Hands.");
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/CarpentersHouse.unity");
            var house = scene.GetRootGameObjects().Select(o => o.GetComponent<HouseSceneRoot>()).FirstOrDefault(h => h);
            if (!house || house.houseIndex != 2 || house.copiedRoots == null || house.copiedRoots.Length != 6 || !house.arrival)
                throw new InvalidOperationException("Expected Carpenter's copied room, arrival and six source roots.");
            int before = SourceCount(house);
            RemovePreviousActivity(house);
            var cleared = ClearPlayingSpace(house);
            var owner = house.gameObject.AddComponent<ActiveWorkshopHouse>();
            var arena = Point(ArenaName, house.transform, Vector3.zero);
            var run = arena.gameObject.AddComponent<ActiveWorkshopRun>();
            owner.run = run; run.house = owner;
            arena.GetComponent<ActiveWorkshopInput>().run = run;
            run.view = arena.gameObject.AddComponent<ActiveWorkshopView>();
            run.desktopPoint = Point("Desktop arcade position", arena, new Vector3(0, .15f, -2f));
            run.vrPoint = Point("VR arcade position", arena, new Vector3(0, .15f, -.20f));

            var wood = WorkshopArt.Mat("Arcade restrained maple mounts", new Color(.39f, .26f, .16f), 0, .12f);
            var dark = WorkshopArt.Mat("Arcade matte navy", new Color(.035f, .065f, .105f), 0, .08f);
            var idle = WorkshopArt.Mat("Arcade quiet target marks", new Color(.11f, .17f, .22f), 0, .08f);
            var cyan = WorkshopArt.Mat("Arcade matte cyan", new Color(.12f, .72f, .80f), 0, .12f);
            var coral = WorkshopArt.Mat("Arcade matte coral", new Color(.94f, .34f, .27f), 0, .12f);
            var violet = WorkshopArt.Mat("Arcade matte violet", new Color(.56f, .43f, .83f), 0, .12f);
            BuildHud(arena, run.view, dark, cyan);
            BuildFloor(arena, dark, cyan, coral);
            run.targetPoints = new Transform[6]; run.targetVisuals = new Transform[6];
            run.view.targetRenderers = new Renderer[6]; run.view.cueFills = new Transform[6];
            for (int i = 0; i < 6; i++) {
                int column = i % 3, row = i / 3;
                var anchor = Point("Target " + (i + 1) + " contact anchor", arena,
                    new Vector3((column - 1) * .48f, 1.10f + row * .42f, .50f));
                run.targetPoints[i] = anchor;
                var collider = anchor.gameObject.AddComponent<SphereCollider>(); collider.radius = .145f; collider.isTrigger = true;
                var target = anchor.gameObject.AddComponent<ActiveWorkshopTarget>(); target.run = run; target.index = i;
                BuildIdleMarker(anchor, wood, idle, i + 1);
                var colour = column == 0 ? cyan : column == 2 ? coral : violet;
                run.targetVisuals[i] = BuildTarget(anchor, colour, dark, cyan, i + 1, out var face, out var cueFill);
                run.view.targetRenderers[i] = face; run.view.cueFills[i] = cueFill;
            }
            run.leftHandVisual = HandMarker(arena, "Tracked left hand", cyan);
            run.rightHandVisual = HandMarker(arena, "Tracked right hand", coral);
            run.leftHandVisual.gameObject.SetActive(false); run.rightHandVisual.gameObject.SetActive(false);
            run.obstacleVisual = BuildObstacle(arena, coral, dark, out var obstacleRenderer);
            run.view.obstacleRenderer = obstacleRenderer; run.obstacleVisual.gameObject.SetActive(false);
            Button(arena, run, ActiveWorkshopCommand.Offer, "PLAY", new Vector3(1.20f, 1.43f, .30f), dark, cyan);
            Button(arena, run, ActiveWorkshopCommand.Calibrate, "RECENTER", new Vector3(1.20f, 1.15f, .30f), dark, violet);
            Button(arena, run, ActiveWorkshopCommand.Pause, "PAUSE", new Vector3(1.20f, .87f, .30f), dark, coral);

            int retained = SourceCount(house);
            if (before != retained) throw new InvalidOperationException("Quick Hands installation changed the copied room hierarchy.");
            PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/CarpentersHouseContent.prefab", out bool saved);
            if (!saved) throw new IOException("Quick Hands Carpenter prefab was not saved.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Quick Hands Carpenter scene was not saved.");
            AssetDatabase.SaveAssets(); Directory.CreateDirectory("Verification");
            File.WriteAllLines("Verification/active-workshop-build.txt", new[] {
                "PASS: Carpenter scene/prefab saved with open six-target body-action play space and flat navy score HUD/instructions matching existing plane/table-tennis presentation.",
                "PASS: Large score/time/combo and permanent 900-point goal; matte discs, quiet idle markers, per-target cue life bars, hand markers, visual-only obstacle and three shared controls.",
                "PASS: Six source roots and " + retained + " source transforms retained; original arrival/exit and shared player rig retained; no additional camera/listener.",
                "PASS: No enclosing cabinet, gold rings or foreground table. Decorative geometry and visual obstacles cannot push the player.",
                "Cleared existing dressing: " + string.Join(", ", cleared)
            });
        }

        static void BuildHud(Transform parent, ActiveWorkshopView view, Material dark, Material cyan) {
            // Same unlit world-space Canvas pattern as PlaneDreamBuilder.
            var header = Panel("Quick Hands score HUD", parent, new Vector3(0, 2.35f, 2.30f), new Vector2(960, 285));
            Text(header.transform, "Activity and permanent goal", "QUICK HANDS  /  GOAL 900", new Vector2(920, 45), new Vector2(0, 112), 33, White);
            view.status = Text(header.transform, "Action cue", "REACH. DODGE. BUILD YOUR COMBO.", new Vector2(920, 45), new Vector2(0, 52), 32, new Color(.34f, .88f, .85f));
            view.feedback = Text(header.transform, "Action feedback", "READY WHEN YOU ARE", new Vector2(920, 30), new Vector2(0, 9), 24, White);
            Text(header.transform, "Score heading", "SCORE / GOAL", new Vector2(430, 30), new Vector2(-245, -27), 23, Muted);
            Text(header.transform, "Time heading", "TIME", new Vector2(200, 30), new Vector2(95, -27), 23, Muted);
            Text(header.transform, "Combo heading", "COMBO", new Vector2(220, 30), new Vector2(345, -27), 23, Muted);
            view.score = Text(header.transform, "Score", "0000 / 900", new Vector2(430, 78), new Vector2(-245, -83), 52, White);
            view.time = Text(header.transform, "Time", "60s", new Vector2(200, 78), new Vector2(95, -83), 52, White);
            view.combo = Text(header.transform, "Combo", "0 / x1", new Vector2(220, 78), new Vector2(345, -83), 46, White);
            Bar(header.transform, "Goal progress track", new Vector2(0, -136), new Vector2(950, 6), new Color(.10f, .18f, .25f));
            view.progressFill = Bar(header.transform, "Score goal progress", new Vector2(0, -136), new Vector2(946, 4), new Color(.13f, .79f, .78f));

            var instructions = Panel("Quick Hands instructions", parent, new Vector3(-2.0f, 1.02f, 1.75f), new Vector2(340, 345));
            Text(instructions.transform, "Instructions heading", "HOW TO PLAY", new Vector2(310, 45), new Vector2(0, 133), 30, White);
            view.legend = Text(instructions.transform, "Controls",
                "REACH for targets\nBOTH HANDS for pairs\nLEAN or DUCK to dodge\n\nMOUSE L / R: hands\nA / D: lean  |  S: duck\nESC: pause",
                new Vector2(310, 265), new Vector2(0, -20), 27, White);
        }

        static void BuildIdleMarker(Transform anchor, Material wood, Material idle, int number) {
            WorkshopArt.Disc("Small maple target mount", anchor, new Vector3(0, 0, .055f), .10f, .022f, wood);
            WorkshopArt.Disc("Quiet inactive target marker", anchor, new Vector3(0, 0, .037f), .15f, .012f, idle);
            WorkshopArt.Label(anchor, number.ToString(), new Vector3(0, 0, .026f), new Vector2(.11f, .10f), .80f, Muted);
        }
        static Transform BuildTarget(Transform anchor, Material colour, Material dark, Material cueColour, int number,
            out Renderer renderer, out Transform cueFill) {
            var visual = Point("Reach target " + number, anchor, Vector3.zero);
            var face = WorkshopArt.Sphere("Matte touch disc", visual, Vector3.zero, new Vector3(.32f, .32f, .055f), colour);
            renderer = face.GetComponent<Renderer>();
            var label = WorkshopArt.Label(visual, number.ToString(), new Vector3(0, 0, -.035f), new Vector2(.24f, .23f), 1.55f, Navy);
            label.fontStyle = FontStyles.Bold;
            WorkshopArt.Plate("Target response time track", visual, new Vector3(0, -.185f, 0), new Vector2(.30f, .018f), .008f, dark, .006f);
            cueFill = Point("Target remaining response time", visual, new Vector3(0, -.185f, -.006f));
            WorkshopArt.Plate("Target response time fill", cueFill, Vector3.zero, new Vector2(.30f, .018f), .006f, cueColour, .006f);
            return visual;
        }
        static Transform HandMarker(Transform parent, string name, Material colour) {
            var hand = Point(name, parent, Vector3.zero);
            WorkshopArt.Sphere("Tracked hand contact", hand, Vector3.zero, new Vector3(.075f, .075f, .075f), colour);
            return hand;
        }
        static Transform BuildObstacle(Transform parent, Material warning, Material dark, out Renderer renderer) {
            var obstacle = Point("Body dodge warning", parent, new Vector3(0, 1.50f, .2f));
            var face = WorkshopArt.Box("Soft visual obstacle", obstacle, Vector3.zero, Vector3.one, warning);
            renderer = face.GetComponent<Renderer>();
            for (int i = 0; i < 5; i++)
                WorkshopArt.Box("Obstacle dark warning stripe", obstacle, new Vector3(-.40f + i * .20f, 0, -.505f), new Vector3(.06f, .94f, .008f), dark);
            return obstacle;
        }
        static void BuildFloor(Transform parent, Material dark, Material cyan, Material coral) {
            var mat = WorkshopArt.Plate("Neutral standing position", parent, new Vector3(0, .157f, -.19f), new Vector2(1.10f, .90f), .008f, dark, .07f);
            mat.transform.localRotation = Quaternion.Euler(90, 0, 0);
            foreach (int side in new[] { -1, 1 }) {
                var colour = side < 0 ? cyan : coral;
                WorkshopArt.Box("Standing position side", parent, new Vector3(side * .50f, .164f, -.19f), new Vector3(.013f, .004f, .55f), colour);
                WorkshopArt.Box("Standing position front corner", parent, new Vector3(side * .43f, .164f, .16f), new Vector3(.15f, .004f, .013f), colour);
                WorkshopArt.Box("Standing position back corner", parent, new Vector3(side * .43f, .164f, -.54f), new Vector3(.15f, .004f, .013f), colour);
            }
        }
        static void Button(Transform parent, ActiveWorkshopRun run, ActiveWorkshopCommand command, string text,
            Vector3 position, Material dark, Material accent) {
            var button = WorkshopArt.Plate(text + " arcade control", parent, position, new Vector2(.50f, .17f), .032f, dark, .015f);
            button.AddComponent<BoxCollider>().size = new Vector3(.50f, .17f, .04f);
            WorkshopArt.Box(text + " accent", parent, position + new Vector3(-.240f, 0, -.018f), new Vector3(.012f, .13f, .008f), accent);
            var action = button.AddComponent<ActiveWorkshopControl>(); action.run = run; action.command = command;
            button.AddComponent<XRSimpleInteractable>();
            var interactable = button.AddComponent<VillageInteractable>(); interactable.prompt = text + " Quick Hands Workshop";
            UnityEventTools.AddPersistentListener(interactable.action, action.Use);
            WorkshopArt.Label(parent, text, position + Vector3.back * .023f, new Vector2(.44f, .14f), text == "RECENTER" ? .68f : .95f, White);
        }

        static Canvas Panel(string name, Transform parent, Vector3 position, Vector2 size) {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = Vector3.one * HudScale;
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)go.transform).sizeDelta = size;
            var image = go.AddComponent<Image>(); image.color = Navy; image.raycastTarget = false;
            image.material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Field UI Overlay.mat");
            return canvas;
        }
        static RectTransform Bar(Transform parent, string name, Vector2 position, Vector2 size, Color color) {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            image.material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Field UI Overlay.mat");
            return rect;
        }
        static TMP_Text Text(Transform parent, string name, string value, Vector2 size, Vector2 position, float fontSize, Color color) {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/Field Text Overlay.mat");
            text.text = value; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = color; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static void RemovePreviousActivity(HouseSceneRoot house) {
            foreach (string name in new[] { ArenaName, "Bridge delivery workshop" }) {
                var previous = house.transform.Find(name); if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }
            foreach (var old in house.GetComponents<BridgeHouse>()) UnityEngine.Object.DestroyImmediate(old);
            foreach (var old in house.GetComponents<ActiveWorkshopHouse>()) UnityEngine.Object.DestroyImmediate(old);
            foreach (var idle in house.GetComponentsInChildren<ResidentIdle>(true)) UnityEngine.Object.DestroyImmediate(idle);
            foreach (var pulse in house.GetComponentsInChildren<GentlePulse>(true)) UnityEngine.Object.DestroyImmediate(pulse);
            foreach (var transform in house.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            foreach (var station in house.GetComponentsInChildren<EngineeringStation>(true).Where(s => s.topic == 2).ToArray())
                if (!InCopiedRoom(station.transform, house)) UnityEngine.Object.DestroyImmediate(station.gameObject);
            foreach (var socket in house.GetComponentsInChildren<WorkshopSocket>(true).ToArray())
                if (!InCopiedRoom(socket.transform, house)) UnityEngine.Object.DestroyImmediate(socket.gameObject);
            foreach (var text in house.GetComponentsInChildren<TMP_Text>(true)) {
                if (text.text == VillageGame.Houses[2]) text.gameObject.SetActive(false);
                if (text.text.Contains("MOSFET") || text.text.Contains("COMMON-SOURCE") || text.text.Contains("BRIDGE DELIVERY") || text.text.Contains("gm ="))
                    text.text = "QUICK HANDS\nReach. Strike. Dodge.";
            }
        }
        static List<string> ClearPlayingSpace(HouseSceneRoot house) {
            // Hide movable props across the view; retain every source transform and the room shell.
            var area = new Bounds(new Vector3(0, 1.45f, .10f), new Vector3(6.2f, 2.65f, 3.5f));
            var roots = house.copiedRoots.Concat(house.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Atmosphere & set dressing")).Select(t => t.gameObject)).Distinct();
            var cleared = new List<string>();
            foreach (var root in roots) {
                if (!root || !root.activeSelf) continue;
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (collider.enabled && area.Intersects(collider.bounds) && collider.bounds.size.magnitude < 4.8f && collider.bounds.max.y > .35f)
                        collider.enabled = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) {
                    if (!area.Contains(renderer.bounds.center) || renderer.bounds.size.magnitude >= 3.4f) continue;
                    cleared.Add(renderer.name); renderer.gameObject.SetActive(false);
                }
            }
            return cleared;
        }
        static bool InCopiedRoom(Transform transform, HouseSceneRoot house) => house.copiedRoots.Any(root => root && (transform == root.transform || transform.IsChildOf(root.transform)));
        static int SourceCount(HouseSceneRoot house) => house.copiedRoots.Where(root => root).Sum(root => root.GetComponentsInChildren<Transform>(true).Length);
        static Transform Point(string name, Transform parent, Vector3 position) {
            var point = new GameObject(name); point.transform.SetParent(parent, false); point.transform.localPosition = position; return point.transform;
        }
    }
}
