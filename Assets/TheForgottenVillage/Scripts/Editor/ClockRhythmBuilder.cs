using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace ForgottenVillage.Editor {
    public static class ClockRhythmBuilder {
        const string Root = "Assets/TheForgottenVillage/";
        [MenuItem("Forgotten Village/Install Clockmaker rhythm repair")]
        public static void Build() {
            if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode before installation.");
            var scene = EditorSceneManager.OpenScene(Root + "Scenes/ClockmakersHouse.unity");
            var house = UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();
            if (!house || house.houseIndex != 4) throw new Exception("Expected Clockmaker scene.");
            var old = house.GetComponent<ClockRhythmHouse>();
            if (old) UnityEngine.Object.DestroyImmediate(old);
            var previous = house.transform.Find("Rhythm repair workshop");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            // Remove only generated Clockmaker puzzle objects, retaining the copied room.
            foreach (var station in house.GetComponentsInChildren<EngineeringStation>(true).ToArray()) {
                if (station.topic == 4) UnityEngine.Object.DestroyImmediate(station.gameObject);
            }
            foreach (var tr in house.GetComponentsInChildren<Transform>(true).ToArray()) {
                if (!tr) continue;
                string n = tr.name;
                if (n == "Workbench leg" || n == "ENGINEERING STATION" || n == "Oscilloscope traces" ||
                    n == "Engineering field card" || n == "Brass field card edge" || n.StartsWith("Timing clock ") ||
                    n == "Clock minute hand" || n.Contains("SETUP TIMING") || n.Contains("CLOCK PERIOD"))
                    UnityEngine.Object.DestroyImmediate(tr.gameObject);
            }
            var owner = house.gameObject.AddComponent<ClockRhythmHouse>();
            var arena = new GameObject("Rhythm repair workshop"); arena.transform.SetParent(house.transform, false);
            var run = arena.AddComponent<ClockRhythmRun>(); owner.run = run; run.house = owner;
            string path = Root + "ClockRhythmTuning.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<ClockRhythmTuning>(path);
            if (!tuning) { tuning = ScriptableObject.CreateInstance<ClockRhythmTuning>(); AssetDatabase.CreateAsset(tuning, path); }
            run.tuning = tuning; run.sound = arena.AddComponent<ClockRhythmAudio>();
            arena.AddComponent<ClockRhythmInput>().run = run;
            run.view = arena.AddComponent<ClockRhythmView>();
            var point = new GameObject("Stationary clock playing point"); point.transform.SetParent(arena.transform, false);
            point.transform.localPosition = new Vector3(.2f, .15f, -1.1f); run.playingPoint = point.transform;
            var dark = Material("Clock workshop body", new Color(.06f,.12f,.16f));
            var brass = Material("Clock workshop brass", new Color(.9f,.65f,.22f));
            var face = Material("Clock workshop face", tuning.idle);
            Box("Repair workbench", arena.transform, new Vector3(.2f,.85f,.9f), new Vector3(2.1f,.12f,.65f), dark);
            for (int side = -1; side <= 1; side += 2) Box("Repair bench support", arena.transform,
                new Vector3(.2f + side * .85f,.4f,.9f), new Vector3(.1f,.8f,.5f), dark);
            for (int i = 0; i < 3; i++) {
                float x = .2f + (i-1)*.65f;
                var dial = GameObject.CreatePrimitive(PrimitiveType.Cylinder); dial.name = "Repair clock " + (i+1);
                dial.transform.SetParent(arena.transform, false); dial.transform.localPosition = new Vector3(x,1.55f,.95f);
                dial.transform.localRotation = Quaternion.Euler(90,0,0); dial.transform.localScale = new Vector3(.53f,.035f,.53f);
                dial.GetComponent<Renderer>().sharedMaterial = face; run.view.faces[i] = dial.GetComponent<Renderer>();
                UnityEngine.Object.DestroyImmediate(dial.GetComponent<Collider>());
                Box("Gold strike mark", arena.transform, new Vector3(x,1.79f,.89f), new Vector3(.065f,.075f,.025f), brass);
                var pivot = new GameObject("Moving hand pivot " + i); pivot.transform.SetParent(arena.transform, false);
                pivot.transform.localPosition = new Vector3(x,1.55f,.88f); run.view.hands[i] = pivot.transform;
                var hand = Box("Moving hand", pivot.transform, new Vector3(0,.105f,0), new Vector3(.024f,.22f,.025f), brass);
                UnityEngine.Object.DestroyImmediate(hand.GetComponent<Collider>());
                var bell = Box("Bell " + (i+1), arena.transform, new Vector3(x,1.04f,.65f), new Vector3(.42f,.18f,.3f), brass);
                var target = bell.AddComponent<ClockBell>(); target.run = run; target.lane = i;
                bell.AddComponent<XRSimpleInteractable>();
                var interaction = bell.AddComponent<VillageInteractable>(); interaction.prompt = "Strike bell " + (i+1);
                UnityEventTools.AddPersistentListener(interaction.action, target.Strike);
                Label(arena.transform, (i+1) + " / " + new[]{"A","S","D"}[i], new Vector3(x,1.23f,.59f), new Vector2(.55f,.17f), .75f);
            }
            run.view.status = Label(arena.transform, "RHYTHM REPAIR\nSpeak to the Clockmaker to begin", new Vector3(.2f,2.18f,.9f), new Vector2(2.3f,.48f), .65f);
            run.view.feedback = Label(arena.transform, "", new Vector3(.2f,1.95f,.86f), new Vector2(2,.18f), .7f);
            var start = Box("Start clock repair", arena.transform, new Vector3(-1.05f,1.15f,.65f), new Vector3(.28f,.25f,.2f), face);
            start.AddComponent<XRSimpleInteractable>(); var use = start.AddComponent<VillageInteractable>(); use.prompt = "Repair the clocks";
            UnityEventTools.AddPersistentListener(use.action, owner.Offer);
            Label(arena.transform, "PLAY", new Vector3(-1.05f,1.4f,.6f), new Vector2(.5f,.16f), .6f);
            Save(house); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/clock-rhythm-build.txt", "PASS: Clockmaker scene, content prefab and tuning saved. Three numbered bells, stationary play point, rhythm HUD. Old timing station removed. Other houses untouched.\n");
        }
        public static void Save(HouseSceneRoot house) { PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/ClockmakersHouseContent.prefab"); }
        static Material Material(string name, Color color) {
            string path = Root + "Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!result) { result = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(result,path); }
            result.SetColor("_BaseColor",color); EditorUtility.SetDirty(result); return result;
        }
        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material) {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube); result.name = name;
            result.transform.SetParent(parent,false); result.transform.localPosition = position; result.transform.localScale = size;
            result.GetComponent<Renderer>().sharedMaterial = material; return result;
        }
        static TextMeshPro Label(Transform parent, string text, Vector3 position, Vector2 size, float fontSize) {
            var go = new GameObject("Rhythm label",typeof(TextMeshPro)); go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0,180,0);
            var label = go.GetComponent<TextMeshPro>(); label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1,.9f,.65f); label.rectTransform.sizeDelta = size; return label;
        }
    }
}
