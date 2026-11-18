using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ForgottenVillage.Editor {
    public static class PlanetaryDefenceBuilder {
        const string Root = "Assets/TheForgottenVillage/";
        [MenuItem("Forgotten Village/Install both imported mini-game replacements")]
        public static void BuildBoth() { Build(); CarpenterToolsBuilder.Build(); }
        public static void Build() {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode first.");
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/TechniciansHouse.unity");
            var house = UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();
            if (!house || house.houseIndex != 3) throw new Exception("Technician room is missing.");
            var owner = house.GetComponent<PowerGridHouse>();
            // The full village generator first creates the raw copied room.
            // Reuse its existing Technician setup once, then replace only the
            // activity, just as we do for an already-built project.
            if (!owner) { PowerGridBuilder.BuildLegacy(); Build(); return; }
            string before = TransitionSnapshot(house);
            foreach (var run in house.GetComponentsInChildren<PowerGridRun>(true).ToArray()) UnityEngine.Object.DestroyImmediate(run.gameObject);
            foreach (var run in house.GetComponentsInChildren<PlanetaryDefenceRun>(true).ToArray()) UnityEngine.Object.DestroyImmediate(run.gameObject);
            owner.run = null;
            var arena = new GameObject("Planetary Defence station"); arena.transform.SetParent(house.transform, false);
            var defence = arena.AddComponent<PlanetaryDefenceRun>(); owner.defence = defence; defence.house = owner;
            var space = Point("Orbital defence space", arena.transform, new Vector3(0, 0, 250));
            defence.presentation = space; defence.returnPoint = house.arrival;
            defence.spawnRadius = 3; defence.planetRadius = 1.3f;
            defence.playingPoint = Point("Stationary orbital playing point", space, new Vector3(0, .15f, -5.5f));
            defence.planet = Point("Defended planet", space, new Vector3(0, 1.65f, 0));
            Visual("PlanetDetailed", defence.planet, 2.6f);
            var platform = WorkshopArt.Box("Orbital standing platform", space, new Vector3(0, .02f, -5.5f), new Vector3(1.6f, .05f, 1.4f), WorkshopArt.Mat("Defence platform", new Color(.05f, .12f, .2f), .1f, .2f));
            platform.AddComponent<BoxCollider>();
            var starRandom = new System.Random(834);
            for (int i = 0; i < 40; i++) {
                var star = Visual("SquareBullet", space, .025f + (float)starRandom.NextDouble() * .05f);
                star.name = "Distant imported star " + i;
                star.transform.localPosition = new Vector3(((float)starRandom.NextDouble() - .5f) * 38, ((float)starRandom.NextDouble() - .5f) * 25, 8 + (float)starRandom.NextDouble() * 12);
            }
            defence.sound = arena.AddComponent<AudioSource>(); defence.sound.playOnAwake = false; defence.sound.spatialBlend = 0;
            defence.shotClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PlanetaryDefence/Audio/Shoot.wav");
            defence.hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PlanetaryDefence/Audio/EnemyDeath.wav");
            defence.impactClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PlanetaryDefence/Audio/EnemyHit.wav");
            defence.threats = new PlanetaryDefenceThreat[8];
            for (int i = 0; i < defence.threats.Length; i++) {
                var go = new GameObject("Incoming enemy " + i); go.transform.SetParent(space, false); go.SetActive(false);
                var collider = go.AddComponent<SphereCollider>(); collider.radius = .4f;
                var body = go.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                go.AddComponent<XRSimpleInteractable>();
                var threat = go.AddComponent<PlanetaryDefenceThreat>(); threat.run = defence;
                threat.variants = new GameObject[3];
                for (int j = 0; j < 3; j++) { threat.variants[j] = Visual("Enemy" + (j + 1), go.transform, .75f); threat.variants[j].SetActive(j == 0); }
                defence.threats[i] = threat;
            }
            defence.projectiles = new PlanetaryDefenceProjectile[8];
            for (int i = 0; i < defence.projectiles.Length; i++) {
                var go = new GameObject("Homing interceptor " + i); go.transform.SetParent(space, false); go.SetActive(false);
                Visual("Rocket", go.transform, .22f);
                var projectile = go.AddComponent<PlanetaryDefenceProjectile>(); projectile.run = defence; defence.projectiles[i] = projectile;
            }
            defence.flashes = new Transform[6];
            for (int i = 0; i < defence.flashes.Length; i++) {
                var flash = new GameObject("Intercept shield flash " + i); flash.transform.SetParent(space, false);
                Visual("PlanetShield", flash.transform, 1); flash.SetActive(false); defence.flashes[i] = flash.transform;
            }
            defence.status = Label(space, "PLANETARY DEFENCE\n20 THREATS / 5 INTEGRITY", new Vector3(0, 5.05f, 1), new Vector2(7.5f, .95f), .44f);
            defence.instructions = Label(space, "SELECT enemy ships to fire  |  Desktop: click\nMENU: pause / retry / return to house", new Vector3(0, -1.05f, .7f), new Vector2(7, .6f), .35f);
            var terminal = Point("Room defence terminal", arena.transform, Vector3.zero);
            Control(terminal, defence, "PLAY DEFENCE", new Vector3(-1.6f, 1.2f, .6f), false);
            var icon = Visual("PlanetDetailed", terminal, .3f); icon.transform.localPosition = new Vector3(-1.6f, 1.65f, .6f);
            defence.roomObjects = house.copiedRoots.Concat(house.transform.Cast<Transform>().Where(t => t != arena.transform).Select(t => t.gameObject)).Concat(new[] { terminal.gameObject }).Distinct().ToArray();
            space.gameObject.SetActive(false);
            foreach (var label in house.GetComponentsInChildren<TMP_Text>(true))
                if (label.text.StartsWith("RESTORE THE WORKSHOP", StringComparison.Ordinal))
                    label.text = "PLANETARY DEFENCE\n\nAim at incoming ships\nSELECT to intercept\nProtect the planet";
            if (TransitionSnapshot(house) != before) throw new Exception("House transition wiring changed.");
            PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/TechniciansHouseContent.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/planetary-defence-build.txt", "Planetary Defence installed with a remote orbital arena 250m from the retained Technician room. Planet diameter=2.6m; threat size=.75m; orbit radius=3m; same player rig, room return point and progression adapter. Removed active PowerGrid gameplay root; copied room, resident, arrival and exit retained.\n" + before);
        }
        public static string TransitionSnapshot(HouseSceneRoot house) {
            var exit = house.GetComponentsInChildren<VillageAction>(true).Single(a => a.kind == "exit");
            return "index=" + house.houseIndex + "; arrival=" + house.arrival.position.ToString("F5") + "; exit=" + exit.transform.position.ToString("F5") + "; exitAction=" + EditorJsonUtility.ToJson(exit) + "; copiedRoots=" + string.Join(",", house.copiedRoots.Select(r => r.name));
        }
        static Transform Point(string name, Transform parent, Vector3 position) {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        static GameObject Visual(string sprite, Transform parent, float size) {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PlanetaryDefence/Sprites/" + sprite + ".png");
            if (!texture) throw new Exception("Missing imported texture " + sprite);
            string path = Root + "Materials/Defence " + sprite + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); material.SetFloat("_ZWrite", 0); material.SetFloat("_Cull", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "Imported " + sprite;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(size * texture.width / texture.height, size, 1);
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return go;
        }
        static TMP_Text Label(Transform parent, string text, Vector3 pos, Vector2 size, float fontSize) {
            var point = Point(text.Split('\n')[0], parent, pos);
            var label = point.gameObject.AddComponent<TextMeshPro>(); label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.text = text; label.fontSize = fontSize * 5; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = size;
            return label;
        }
        static void Control(Transform parent, PlanetaryDefenceRun run, string name, Vector3 pos, bool pause) {
            var material = WorkshopArt.Mat("Defence controls navy", new Color(.04f, .12f, .2f));
            var go = WorkshopArt.Box(name, parent, pos, new Vector3(.55f, .22f, .07f), material);
            go.AddComponent<BoxCollider>(); go.AddComponent<XRSimpleInteractable>();
            var action = go.AddComponent<PlanetaryDefenceControl>(); action.run = run; action.pause = pause;
            var use = go.AddComponent<VillageInteractable>(); use.prompt = name; UnityEventTools.AddPersistentListener(use.action, action.Use);
            Label(parent, name, pos - Vector3.forward * .05f, new Vector2(.52f, .2f), .1f);
        }
    }
}
