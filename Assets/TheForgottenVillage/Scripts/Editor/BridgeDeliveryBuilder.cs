using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    /// <summary>Installs the Carpenter game into its copied room, retaining the source environment.</summary>
    public static class BridgeDeliveryBuilder {
        const string Root = "Assets/TheForgottenVillage/";
        const string WorkshopName = "Bridge delivery workshop";
        const float Deck = .98f;
        const float Path = .8f;
        static readonly Color Ivory = new Color(.94f, .89f, .74f);
        static readonly Color Muted = new Color(.62f, .72f, .69f);

        [MenuItem("Forgotten Village/Install Carpenter bridge delivery")]
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Finish compilation and leave Play Mode before building the Carpenter game.");
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/CarpentersHouse.unity");
            var house = scene.GetRootGameObjects().Select(o => o.GetComponent<HouseSceneRoot>()).FirstOrDefault(h => h);
            if (!house || house.houseIndex != 2 || house.copiedRoots == null || house.copiedRoots.Length != 6 || !house.arrival)
                throw new InvalidOperationException("Expected the copied Carpenter house and its six source roots.");
            int sourceCount = house.copiedRoots.Where(o => o).Sum(o => o.GetComponentsInChildren<Transform>(true).Length);
            RemoveLegacyGame(house);
            var cleared = ClearWorkbenchSpace(house);

            var owner = house.gameObject.AddComponent<BridgeHouse>();
            var arena = new GameObject(WorkshopName); arena.transform.SetParent(house.transform, false);
            arena.transform.localPosition = Vector3.zero; arena.transform.localRotation = Quaternion.identity; arena.transform.localScale = Vector3.one;
            var run = arena.AddComponent<BridgeRun>(); owner.run = run; run.house = owner;
            run.deckY = Deck; run.pathZ = Path;
            arena.AddComponent<BridgeInput>().run = run;
            run.view = arena.AddComponent<BridgeView>();
            run.desktopPoint = Point("Desktop bridge playing point", arena.transform, new Vector3(0, .15f, -2.0f));
            run.vrPoint = Point("VR bridge playing point", arena.transform, new Vector3(0, .15f, -.2f));

            var walnut = WorkshopArt.Mat("Bridge oiled walnut", new Color(.29f, .145f, .065f), 0, .33f);
            var timber = WorkshopArt.Mat("Bridge pale cut timber", new Color(.68f, .43f, .20f), 0, .28f);
            var endGrain = WorkshopArt.Mat("Bridge end grain", new Color(.43f, .235f, .095f), 0, .24f);
            ApplyWoodTexture(walnut, new Vector2(1.8f, .8f));
            ApplyWoodTexture(timber, new Vector2(3.5f, .6f));
            var brass = WorkshopArt.Mat("Bridge aged brass", new Color(.69f, .45f, .17f), .76f, .48f);
            var ink = WorkshopArt.Mat("Bridge midnight enamel", new Color(.035f, .075f, .080f), .25f, .38f);
            var stone = WorkshopArt.Mat("Bridge riverbank stone", new Color(.31f, .39f, .36f), .02f, .22f);
            var road = WorkshopArt.Mat("Bridge bank road", new Color(.14f, .22f, .21f), .05f, .32f);
            var water = WorkshopArt.Mat("Bridge turquoise river", new Color(.025f, .30f, .32f), .30f, .78f);
            var ripple = WorkshopArt.Mat("Bridge river glints", new Color(.16f, .52f, .50f), .15f, .65f);
            var ghost = WorkshopArt.Mat("Bridge socket guide", new Color(.31f, .65f, .58f), .15f, .50f, true);
            var cartPaint = WorkshopArt.Mat("Bridge delivery cart enamel", new Color(.69f, .23f, .10f), .32f, .48f);
            var rubber = WorkshopArt.Mat("Bridge cart rubber", new Color(.025f, .035f, .037f), .02f, .25f);
            var glass = WorkshopArt.Mat("Bridge cab glass", new Color(.14f, .39f, .41f), .55f, .70f);
            var green = WorkshopArt.Mat("Bridge test green", new Color(.13f, .43f, .33f), .25f, .40f);

            BuildBench(arena.transform, walnut, brass, ink, water, ripple, run.view);
            BuildHeader(arena.transform, run.view, walnut, brass, ink);
            run.banks = new Transform[2];
            for (int i = 0; i < 2; i++) {
                int side = i == 0 ? -1 : 1;
                run.banks[i] = Bank(arena.transform, side, road, stone, brass, timber, ink);
            }
            BuildFixedPier(arena.transform, stone, brass);
            run.sockets = new BridgeSocket[6]; run.pieces = new BridgePiece[6];
            for (int i = 0; i < 6; i++) {
                bool support = i >= 3; int column = i % 3;
                float x = column == 0 ? -.38f : column == 1 ? .38f : 0;
                run.sockets[i] = Socket(arena.transform, run, i, support, new Vector3(x, support ? Deck - .22f : Deck - .03f, Path), ghost, brass);
                // The third span and its reinforcement are introduced in the final round.
                run.sockets[i].gameObject.SetActive(column < 2);
                Vector3 homePosition = new Vector3(-1.1f + column * 1.1f, support ? 1.04f : .96f, support ? .18f : -.21f);
                var home = Point((support ? "Support" : "Timber") + " tray home " + (column + 1), arena.transform, homePosition);
                run.pieces[i] = Piece(arena.transform, run, i, support, home, timber, endGrain, brass, ink);
                Tray(arena.transform, homePosition, support, column, walnut, brass, ink);
            }
            run.cartStart = Point("Cart start", arena.transform, new Vector3(-1.18f, Deck + .12f, Path));
            run.cartFinish = Point("Cart finish", arena.transform, new Vector3(1.18f, Deck + .12f, Path));
            run.cart = Cart(arena.transform, run.cartStart.localPosition, cartPaint, brass, rubber, glass, timber, ink);

            // All four controls expose the same callback to a desktop pointer and XR selection.
            Button(arena.transform, run, BridgeCommand.Offer, "PLAY", new Vector3(-1.58f, 1.03f, -.31f), ink, brass);
            Button(arena.transform, run, BridgeCommand.Test, "TEST", new Vector3(1.58f, 1.03f, -.31f), green, brass);
            Button(arena.transform, run, BridgeCommand.Retry, "RETRY", new Vector3(1.58f, 1.03f, .10f), ink, brass);
            Button(arena.transform, run, BridgeCommand.Reset, "RESET", new Vector3(-1.58f, 1.03f, .10f), ink, brass);
            var lampObject = new GameObject("Carpenter warm work light", typeof(Light));
            lampObject.transform.SetParent(arena.transform, false); lampObject.transform.localPosition = new Vector3(-.8f, 2.55f, -.35f);
            var lamp = lampObject.GetComponent<Light>(); lamp.type = LightType.Point; lamp.color = new Color(1, .84f, .61f);
            lamp.range = 5; lamp.intensity = 2.1f; lamp.shadows = LightShadows.None;

            int retained = house.copiedRoots.Where(o => o).Sum(o => o.GetComponentsInChildren<Transform>(true).Length);
            if (retained != sourceCount) throw new InvalidOperationException("Bridge installation altered the copied source hierarchy.");
            Save(house);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Carpenter scene could not be saved.");
            AssetDatabase.SaveAssets();
            File.WriteAllLines("Verification/bridge-delivery-build.txt", new[] {
                "PASS: Carpenter scene and prefab saved with walnut/brass bridge bench, recessed riverbed, two movable banks, six sockets, six XR materials, physical cart and four shared interaction controls.",
                "Copied source objects retained: " + retained + "; original arrival and village exit preserved; no camera, audio listener or XR rig added.",
                "The recessed riverbed floor remains below the fall plane; banks, timber, cart and below-deck reinforcement grips have physical collision. Decoration/socket previews have none.",
                "Cleared copied/dressing props: " + string.Join(", ", cleared)
            });
        }

        static void RemoveLegacyGame(HouseSceneRoot house) {
            var previous = house.transform.Find(WorkshopName);
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            foreach (var old in house.GetComponents<BridgeHouse>()) UnityEngine.Object.DestroyImmediate(old);
            // These optional legacy animations share a source file and cannot be safely serialized into a prefab.
            foreach (var idle in house.GetComponentsInChildren<ResidentIdle>(true)) UnityEngine.Object.DestroyImmediate(idle);
            foreach (var pulse in house.GetComponentsInChildren<GentlePulse>(true)) UnityEngine.Object.DestroyImmediate(pulse);
            foreach (var tr in house.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
            foreach (var action in house.GetComponentsInChildren<VillageAction>(true).Where(a => a.kind == "dialogue" && a.index == 2).ToArray()) {
                Vector3 position = action.transform.position; UnityEngine.Object.DestroyImmediate(action.gameObject);
                var host = new GameObject("Resident interaction"); host.transform.SetParent(house.transform, false); host.transform.position = position;
                host.AddComponent<BoxCollider>().size = new Vector3(.45f, 1.6f, .45f);
                var speak = host.AddComponent<VillageAction>(); speak.kind = "dialogue"; speak.index = 2;
                host.AddComponent<XRSimpleInteractable>(); var use = host.AddComponent<VillageInteractable>(); use.prompt = "Speak to the Carpenter";
                UnityEventTools.AddPersistentListener(use.action, speak.Use);
            }
            foreach (var station in house.GetComponentsInChildren<EngineeringStation>(true).ToArray())
                if (station.topic == 2) UnityEngine.Object.DestroyImmediate(station.gameObject);
            foreach (var socket in house.GetComponentsInChildren<WorkshopSocket>(true).ToArray()) UnityEngine.Object.DestroyImmediate(socket.gameObject);
            foreach (var tr in house.GetComponentsInChildren<Transform>(true).ToArray()) {
                if (!tr) continue; string name = tr.name;
                if (name == "Workbench leg" || name == "ENGINEERING STATION" || name == "Oscilloscope traces" ||
                    name == "Engineering field card" || name == "Brass field card edge" || name == "COMPONENT SOCKET" ||
                    name == "Measurement socket" || name.StartsWith("Amplifier component ") || name.Contains("COMMON-SOURCE AMPLIFIER"))
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
            }
            foreach (var text in house.GetComponentsInChildren<TMP_Text>(true)) {
                if (text.text == VillageGame.Houses[2]) text.gameObject.SetActive(false);
                if (text.text.Contains("MOSFET") || text.text.Contains("COMMON-SOURCE") || text.text.Contains("Av approx") || text.text.Contains("gm ="))
                    text.text = "BRIDGE DELIVERY\nBuild a bridge. Send the cart. Improve your design.";
            }
        }

        static List<string> ClearWorkbenchSpace(HouseSceneRoot house) {
            var area = new Bounds(new Vector3(0, 1.05f, .25f), new Vector3(4.1f, 1.7f, 3.9f));
            var roots = house.copiedRoots.Concat(house.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith("Atmosphere & set dressing")).Select(t => t.gameObject)).Distinct();
            var cleared = new List<string>();
            foreach (var root in roots) {
                if (!root || !root.activeSelf) continue;
                // Remove tabletop collision before hiding renderers, since disabled objects have empty collider bounds.
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    if (collider.enabled && area.Intersects(collider.bounds) && collider.bounds.size.magnitude < 4.8f && collider.bounds.max.y > .6f)
                        collider.enabled = false;
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) {
                    if (!area.Contains(renderer.bounds.center) || renderer.bounds.size.magnitude >= 3.4f) continue;
                    cleared.Add(renderer.name); renderer.gameObject.SetActive(false);
                }
            }
            return cleared;
        }

        static void BuildBench(Transform parent, Material wood, Material brass, Material ink, Material water, Material ripple, BridgeView view) {
            // A recessed riverbed is deliberately below the failure plane; no tabletop spans the bridge opening.
            var floor = WorkshopArt.Box("Recessed riverbed floor", parent, new Vector3(0, .48f, .7f), new Vector3(3.8f, .12f, 2.4f), ink);
            var floorCollider = floor.AddComponent<BoxCollider>(); floorCollider.size = Vector3.one;
            var river = HorizontalPlate("Turquoise river surface", parent, new Vector3(0, .596f, .8f), new Vector2(3.63f, 1.87f), .025f, water, .06f);
            view.water = river.GetComponent<Renderer>();
            for (int i = 0; i < 14; i++) {
                float x = -1.65f + (i % 7) * .55f, z = .42f + (i / 7) * .72f;
                WorkshopArt.Box("River current glint", parent, new Vector3(x, .613f, z), new Vector3(.13f + (i % 3) * .045f, .003f, .012f), ripple);
            }
            foreach (int side in new[] { -1, 1 }) {
                WorkshopArt.Box("Walnut river tray side", parent, new Vector3(side * 1.86f, .64f, .7f), new Vector3(.08f, .30f, 2.4f), wood);
                WorkshopArt.Box("Brass tray side inlay", parent, new Vector3(side * 1.86f, .795f, .7f), new Vector3(.085f, .012f, 2.4f), brass);
                foreach (float z in new[] { -.31f, 1.71f }) {
                    WorkshopArt.Box("Tapered workbench leg", parent, new Vector3(side * 1.66f, .235f, z), new Vector3(.16f, .47f, .16f), wood);
                    WorkshopArt.Box("Brass leg foot", parent, new Vector3(side * 1.66f, .045f, z), new Vector3(.17f, .09f, .17f), brass);
                }
            }
            foreach (float z in new[] { -.46f, 1.86f }) {
                WorkshopArt.Box("Walnut river tray end", parent, new Vector3(0, .64f, z), new Vector3(3.8f, .30f, .08f), wood);
                WorkshopArt.Box("Brass tray end inlay", parent, new Vector3(0, .795f, z), new Vector3(3.8f, .012f, .085f), brass);
            }
            HorizontalPlate("Material shelf", parent, new Vector3(0, .844f, -.035f), new Vector2(3.65f, .78f), .065f, wood, .04f);
            WorkshopArt.Box("Shelf brass front strip", parent, new Vector3(0, .866f, -.43f), new Vector3(3.55f, .018f, .016f), brass);
            WorkshopArt.Label(parent, "DRAG  /  GRIP     •     ROTATE Q / E     •     SPACE: TEST", new Vector3(0, .72f, -.51f), new Vector2(2.7f, .12f), .32f, Ivory);
            for (int tick = 0; tick <= 30; tick++)
                WorkshopArt.Box("Bench ruler engraving", parent, new Vector3(-1.5f + tick * .1f, .883f, -.36f), new Vector3(.005f, .003f, tick % 5 == 0 ? .055f : .025f), brass);
        }

        static void BuildHeader(Transform parent, BridgeView view, Material wood, Material brass, Material ink) {
            WorkshopArt.Plate("Walnut bridge workshop backplate", parent, new Vector3(0, 1.63f, 1.9f), new Vector2(3.65f, 1.12f), .13f, wood, .065f);
            WorkshopArt.Plate("Brass header surround", parent, new Vector3(0, 1.64f, 1.82f), new Vector2(3.47f, .95f), .035f, brass, .045f);
            WorkshopArt.Plate("Bridge status enamel", parent, new Vector3(0, 1.64f, 1.795f), new Vector2(3.40f, .88f), .035f, ink, .035f);
            var title = WorkshopArt.Label(parent, "THE CARPENTER  /  BRIDGE DELIVERY", new Vector3(0, 1.97f, 1.765f), new Vector2(3.1f, .16f), .65f, Ivory); title.characterSpacing = 2;
            view.status = WorkshopArt.Label(parent, "BUILD. TEST. IMPROVE.", new Vector3(0, 1.72f, 1.765f), new Vector2(3.05f, .28f), .85f, Ivory);
            view.feedback = WorkshopArt.Label(parent, "Deliver the memory archive across three rivers.", new Vector3(0, 1.425f, 1.765f), new Vector2(3.0f, .23f), .55f, new Color(.39f, .83f, .70f));
            WorkshopArt.Label(parent, "01  FIRST CROSSING     /     02  LONG SPAN     /     03  HEAVY CARGO", new Vector3(0, 1.25f, 1.765f), new Vector2(3.1f, .12f), .38f, Muted);
            foreach (int side in new[] { -1, 1 }) {
                foreach (float y in new[] { 1.25f, 2.025f }) WorkshopArt.Screw(parent, new Vector3(side * 1.64f, y, 1.765f), brass);
                WorkshopArt.Box("Header upright", parent, new Vector3(side * 1.65f, .97f, 1.89f), new Vector3(.10f, .45f, .10f), wood);
            }
        }

        static Transform Bank(Transform parent, int side, Material road, Material stone, Material brass, Material timber, Material ink) {
            var bank = Point(side < 0 ? "Left movable riverbank" : "Right movable riverbank", parent, new Vector3(side * 1.14f, Deck - .12f, Path));
            var collider = bank.gameObject.AddComponent<BoxCollider>(); collider.size = new Vector3(.72f, .24f, .50f);
            HorizontalPlate("Riverbank stone abutment", bank, new Vector3(0, -.055f, 0), new Vector2(.72f, .50f), .13f, stone, .035f);
            HorizontalPlate("Driving surface", bank, new Vector3(0, .085f, 0), new Vector2(.72f, .50f), .07f, road, .025f);
            for (int stripe = 0; stripe < 4; stripe++)
                WorkshopArt.Box("Road center dash", bank, new Vector3(-.27f + stripe * .18f, .121f, 0), new Vector3(.08f, .003f, .012f), brass);
            foreach (int edge in new[] { -1, 1 }) {
                WorkshopArt.Box("Riverbank timber curb", bank, new Vector3(0, .115f, edge * .242f), new Vector3(.72f, .025f, .025f), timber);
                WorkshopArt.Box("Abutment footing", bank, new Vector3(edge * .255f, -.22f, 0), new Vector3(.12f, .23f, .42f), stone);
            }
            // Gate details move with their bank and never contribute collision.
            float gateX = side * .25f;
            WorkshopArt.Box("Delivery gate upright", bank, new Vector3(gateX, .32f, .28f), new Vector3(.035f, .43f, .035f), brass);
            WorkshopArt.Plate("Delivery gate sign", bank, new Vector3(gateX, .49f, .27f), new Vector2(.39f, .11f), .025f, ink, .018f);
            WorkshopArt.Label(bank, side < 0 ? "DEPOT" : "DELIVERY", new Vector3(gateX, .49f, .25f), new Vector2(.35f, .08f), .25f, Ivory);
            return bank;
        }

        static void BuildFixedPier(Transform parent, Material stone, Material brass) {
            // Visual only: placing material is what makes a span driveable.
            HorizontalPlate("Central masonry pier foot", parent, new Vector3(0, .64f, Path), new Vector2(.20f, .40f), .08f, stone, .025f);
            WorkshopArt.Box("Central masonry pier", parent, new Vector3(0, .765f, Path), new Vector3(.13f, .21f, .32f), stone);
            HorizontalPlate("Pier brass bearing", parent, new Vector3(0, .895f, Path), new Vector2(.18f, .36f), .045f, brass, .018f);
        }

        static BridgeSocket Socket(Transform parent, BridgeRun run, int index, bool support, Vector3 position, Material ghost, Material brass) {
            var origin = Point((support ? "Support" : "Timber") + " placement " + (index % 3 + 1), parent, position);
            var socket = origin.gameObject.AddComponent<BridgeSocket>(); socket.run = run; socket.index = index; socket.support = support;
            socket.requiredLength = support ? .15f : .8f; socket.snapPoint = Point("Material snap point", origin, Vector3.zero);
            if (support) {
                HorizontalPlate("Support placement marker", origin, new Vector3(0, -.12f, 0), new Vector2(.23f, .23f), .008f, ghost, .025f);
                foreach (int side in new[] { -1, 1 })
                    WorkshopArt.Box("Support alignment guide", origin, new Vector3(side * .1f, 0, 0), new Vector3(.008f, .22f, .008f), ghost);
                // The matching marker remains visible on the near side after a deck board covers the socket.
                WorkshopArt.Plate("Near-side reinforcement marker", origin, new Vector3(0, -.06f, -.28f), new Vector2(.15f, .15f), .014f, brass, .02f);
                WorkshopArt.Label(origin, "+", new Vector3(0, -.06f, -.291f), new Vector2(.13f, .13f), .40f, new Color(.10f, .21f, .20f));
            } else {
                foreach (int side in new[] { -1, 1 }) {
                    WorkshopArt.Box("Timber placement side", origin, new Vector3(0, -.033f, side * .15f), new Vector3(.80f, .008f, .009f), ghost);
                    WorkshopArt.Box("Timber placement end", origin, new Vector3(side * .4f, -.033f, 0), new Vector3(.009f, .008f, .30f), ghost);
                    WorkshopArt.Box("Brass socket corner", origin, new Vector3(side * .4f, -.028f, -.15f), new Vector3(.045f, .012f, .035f), brass);
                }
            }
            return socket;
        }

        static BridgePiece Piece(Transform parent, BridgeRun run, int index, bool support, Transform home, Material timber, Material endGrain, Material brass, Material ink) {
            var origin = Point((support ? "Reinforcement post " : "Bridge timber ") + (index % 3 + 1), parent, home.localPosition);
            var body = origin.gameObject.AddComponent<Rigidbody>(); body.mass = support ? .25f : .4f; body.isKinematic = true; body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            var collider = origin.gameObject.AddComponent<BoxCollider>(); collider.size = support ? new Vector3(.15f, .34f, .15f) : new Vector3(.8f, .06f, .26f);
            collider.isTrigger = false;
            var piece = origin.gameObject.AddComponent<BridgePiece>(); piece.run = run; piece.index = index; piece.support = support;
            piece.length = support ? .15f : .8f; piece.capacity = support ? 3 : 1; piece.home = home;
            var visual = WorkshopArt.Box("Cut timber deck", origin, Vector3.zero, new Vector3(.8f, .06f, .26f), timber);
            piece.visual = visual.GetComponent<Renderer>();
            // Decorative children use normalized cube coordinates, so they follow the Run's visual length changes.
            foreach (int side in new[] { -1, 1 }) {
                WorkshopArt.Box("Deck end grain", visual.transform, new Vector3(side * .501f, 0, 0), new Vector3(.002f, 1, 1), endGrain);
                WorkshopArt.Box("Deck brass retaining band", visual.transform, new Vector3(side * .405f, .515f, 0), new Vector3(.027f, .12f, 1.025f), brass);
            }
            for (int grain = 0; grain < 3; grain++)
                WorkshopArt.Box("Timber grain groove", visual.transform, new Vector3(0, .505f, -.27f + grain * .27f), new Vector3(.87f, .018f, .005f), endGrain);
            var post = Point("Reinforcement post model", origin, Vector3.zero); piece.supportVisual = post.gameObject;
            WorkshopArt.Box("Square timber reinforcement post", post, Vector3.zero, new Vector3(.15f, .34f, .15f), timber);
            HorizontalPlate("Post load bearing cap", post, new Vector3(0, .18f, 0), new Vector2(.23f, .24f), .04f, brass, .018f);
            HorizontalPlate("Post riverbed foot", post, new Vector3(0, -.18f, 0), new Vector2(.23f, .24f), .04f, endGrain, .018f);
            WorkshopArt.Box("Post front brass strap", post, new Vector3(0, 0, -.080f), new Vector3(.065f, .27f, .012f), brass);
            WorkshopArt.Label(post, "+", new Vector3(0, .015f, -.089f), new Vector2(.08f, .10f), .30f, new Color(.13f, .19f, .18f));
            visual.SetActive(!support); post.gameObject.SetActive(support);
            // BridgePiece requires an XRGrabInteractable, which may already have been added by Unity.
            var grab = origin.GetComponent<XRGrabInteractable>();
            if (!grab) grab = origin.gameObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;
            grab.movementType = XRBaseInteractable.MovementType.Kinematic;
            if (!grab.colliders.Contains(collider)) grab.colliders.Add(collider);
            return piece;
        }

        static void Tray(Transform parent, Vector3 home, bool support, int index, Material wood, Material brass, Material ink) {
            float shelfY = support ? .855f : .892f;
            HorizontalPlate((support ? "Post" : "Timber") + " felt material cradle", parent, new Vector3(home.x, shelfY, home.z),
                new Vector2(support ? .34f : .89f, support ? .30f : .30f), .025f, ink, .025f);
            if (!support) foreach (int side in new[] { -1, 1 })
                WorkshopArt.Box("Timber cradle stop", parent, new Vector3(home.x + side * .46f, .919f, home.z), new Vector3(.028f, .07f, .31f), wood);
            var plaque = WorkshopArt.Plate("Material tray numbered label", parent, new Vector3(home.x, .84f, support ? .30f : -.38f), new Vector2(.32f, .075f), .015f, brass, .012f);
            WorkshopArt.Label(parent, (support ? "SUPPORT " : "TIMBER ") + (index + 1), plaque.transform.localPosition + Vector3.back * .012f,
                new Vector2(.30f, .058f), .21f, new Color(.10f, .16f, .15f));
        }

        static BridgeCart Cart(Transform parent, Vector3 start, Material paint, Material brass, Material rubber, Material glass, Material timber, Material ink) {
            var origin = Point("Physics delivery cart", parent, start);
            var body = origin.gameObject.AddComponent<Rigidbody>(); body.mass = .65f; body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            var collider = origin.gameObject.AddComponent<BoxCollider>(); collider.size = new Vector3(.23f, .14f, .18f);collider.center=new Vector3(0,-.05f,0);
            string rollingPath=Root+"Materials/Bridge cart rolling.physicMaterial";
            var rolling=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(rollingPath);
            if(!rolling){rolling=new PhysicsMaterial("Bridge cart rolling");AssetDatabase.CreateAsset(rolling,rollingPath);}
            rolling.staticFriction=rolling.dynamicFriction=.02f;rolling.bounciness=0;rolling.frictionCombine=PhysicsMaterialCombine.Minimum;EditorUtility.SetDirty(rolling);collider.sharedMaterial=rolling;
            var cart = origin.gameObject.AddComponent<BridgeCart>(); cart.wheels = new Transform[4];
            WorkshopArt.Box("Cart walnut chassis", origin, new Vector3(0, -.005f, 0), new Vector3(.27f, .065f, .19f), timber);
            WorkshopArt.Box("Cart painted cargo bed", origin, new Vector3(-.045f, .055f, 0), new Vector3(.18f, .065f, .19f), paint);
            WorkshopArt.Box("Cart cab", origin, new Vector3(.083f, .069f, 0), new Vector3(.105f, .13f, .18f), paint);
            WorkshopArt.Box("Cab windscreen", origin, new Vector3(.138f, .087f, 0), new Vector3(.004f, .055f, .135f), glass);
            foreach (int side in new[] { -1, 1 }) {
                WorkshopArt.Box("Cab side glass", origin, new Vector3(.083f, .087f, side * .093f), new Vector3(.065f, .055f, .006f), glass);
                WorkshopArt.Box("Cargo brass side rail", origin, new Vector3(-.055f, .10f, side * .102f), new Vector3(.18f, .025f, .014f), brass);
                WorkshopArt.Sphere("Cart headlamp", origin, new Vector3(.141f, .006f, side * .062f), new Vector3(.018f, .018f, .018f), brass);
            }
            WorkshopArt.Box("Archive cargo box", origin, new Vector3(-.05f, .112f, 0), new Vector3(.115f, .08f, .12f), timber);
            WorkshopArt.Box("Archive box brass strap", origin, new Vector3(-.05f, .154f, 0), new Vector3(.020f, .005f, .13f), brass);
            WorkshopArt.Box("Cart front bumper", origin, new Vector3(.153f, -.025f, 0), new Vector3(.025f, .025f, .21f), brass);
            for (int i = 0; i < 4; i++) {
                int side = i < 2 ? -1 : 1; float x = i % 2 == 0 ? -.083f : .083f;
                var pivot = Point("Wheel rotation pivot " + (i + 1), origin, new Vector3(x, -.05f, side * .12f)); cart.wheels[i] = pivot;
                WorkshopArt.Disc("Rubber cart wheel", pivot, Vector3.zero, .14f, .028f, rubber);
                WorkshopArt.Disc("Brass cart wheel hub", pivot, new Vector3(0, 0, side * .018f), .070f, .012f, brass);
                WorkshopArt.Box("Wheel hub spoke", pivot, new Vector3(0, 0, side * .026f), new Vector3(.05f, .009f, .006f), ink);
            }
            return cart;
        }

        static void Button(Transform parent, BridgeRun run, BridgeCommand command, string label, Vector3 position, Material enamel, Material brass) {
            WorkshopArt.Box(label+" control stem",parent,position+new Vector3(0,-.105f,.035f),new Vector3(.045f,.10f,.06f),brass);
            WorkshopArt.Plate(label + " control base", parent, position + Vector3.forward * .035f, new Vector2(.36f, .19f), .07f, brass, .03f);
            var button = WorkshopArt.Plate(label + " bridge control", parent, position, new Vector2(.30f, .14f), .07f, enamel, .025f);
            button.AddComponent<BoxCollider>().size = new Vector3(.30f, .14f, .07f);
            var control = button.AddComponent<BridgeControl>(); control.run = run; control.command = command;
            button.AddComponent<XRSimpleInteractable>(); var use = button.AddComponent<VillageInteractable>(); use.prompt = label + " bridge delivery";
            UnityEventTools.AddPersistentListener(use.action, control.Use);
            WorkshopArt.Label(parent, label, position + Vector3.back * .041f, new Vector2(.26f, .10f), .33f, Ivory);
        }

        static GameObject HorizontalPlate(string name, Transform parent, Vector3 position, Vector2 size, float thickness, Material material, float radius) {
            var plate = WorkshopArt.Plate(name, parent, position, size, thickness, material, radius);
            plate.transform.localRotation = Quaternion.Euler(90, 0, 0); return plate;
        }
        static Transform Point(string name, Transform parent, Vector3 position) {
            var point = new GameObject(name); point.transform.SetParent(parent, false); point.transform.localPosition = position; return point.transform;
        }
        static void ApplyWoodTexture(Material target, Vector2 tiling) {
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Course Library/Materials/Object Materials/Material_Wood_Dark.mat");
            if (!source || !source.HasProperty("_BaseMap")) return;
            target.SetTexture("_BaseMap", source.GetTexture("_BaseMap")); target.SetTextureScale("_BaseMap", tiling); EditorUtility.SetDirty(target);
        }
        public static void Save(HouseSceneRoot house) {
            PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/CarpentersHouseContent.prefab", out bool saved);
            if (!saved) throw new IOException("Carpenter prefab could not be saved.");
        }
    }
}
