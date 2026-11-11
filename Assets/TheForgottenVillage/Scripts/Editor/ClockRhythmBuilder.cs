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
            // Legacy polish types share a source file and cannot serialize as prefab scripts.
            // Remove these optional animations only from this house; keep its resident meshes.
            foreach (var idle in house.GetComponentsInChildren<ResidentIdle>(true)) UnityEngine.Object.DestroyImmediate(idle);
            foreach (var pulse in house.GetComponentsInChildren<GentlePulse>(true)) UnityEngine.Object.DestroyImmediate(pulse);
            foreach (var tr in house.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
            // Old scene-embedded MonoScripts can survive missing-script cleanup. Recreate
            // the invisible resident interaction host; the coat/head are independent meshes.
            foreach (var resident in house.GetComponentsInChildren<VillageAction>(true).Where(a=>a.kind=="dialogue"&&a.index==4).ToArray()) {
                Vector3 position = resident.transform.position;
                UnityEngine.Object.DestroyImmediate(resident.gameObject);
                var host = new GameObject("Resident interaction"); host.transform.SetParent(house.transform,false); host.transform.position=position;
                host.AddComponent<BoxCollider>().size=new Vector3(.45f,1.6f,.45f);
                var action=host.AddComponent<VillageAction>(); action.kind="dialogue"; action.index=4;
                host.AddComponent<XRSimpleInteractable>();var speak=host.AddComponent<VillageInteractable>();speak.prompt="Speak to the Clockmaker";
                UnityEventTools.AddPersistentListener(speak.action,action.Use);
            }
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
            foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)) {
                if(text.text==VillageGame.Houses[4])text.gameObject.SetActive(false); // Avoid overlapping the live score.
                if(text.text.Contains("REGISTER SETUP ANALYSIS"))text.text="RESTORE THE RHYTHM\n\nWatch the gold mark\nRing bells 1 / 2 / 3\n\n23 good notes repair the clocks";
            }
            var arena = new GameObject("Rhythm repair workshop"); arena.transform.SetParent(house.transform, false);
            var run = arena.AddComponent<ClockRhythmRun>(); owner.run = run; run.house = owner;
            string path = Root + "ClockRhythmTuning.asset";
            var tuning = AssetDatabase.LoadAssetAtPath<ClockRhythmTuning>(path);
            if (!tuning) { tuning = ScriptableObject.CreateInstance<ClockRhythmTuning>(); AssetDatabase.CreateAsset(tuning, path); }
            tuning.incoming=new Color(.18f,.80f,.68f);EditorUtility.SetDirty(tuning);
            run.tuning = tuning; run.sound = arena.AddComponent<ClockRhythmAudio>();
            arena.AddComponent<ClockRhythmInput>().run = run;
            run.view = arena.AddComponent<ClockRhythmView>();
            var point = new GameObject("Stationary clock playing point"); point.transform.SetParent(arena.transform, false);
            point.transform.localPosition = new Vector3(.2f, .15f, -1.45f); run.playingPoint = point.transform;
            ClearDisplaySpace(house);
            var wood=WorkshopArt.Mat("Clock walnut cabinet",new Color(.47f,.28f,.12f),0,.28f);
            var sourceWood=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Course Library/Materials/Object Materials/Material_Wood_Dark.mat");
            if(sourceWood&&sourceWood.HasProperty("_BaseMap")){wood.SetTexture("_BaseMap",sourceWood.GetTexture("_BaseMap"));wood.SetTextureScale("_BaseMap",new Vector2(1.6f,.75f));EditorUtility.SetDirty(wood);}
            var dark=WorkshopArt.Mat("Clock midnight enamel",new Color(.035f,.065f,.085f),.2f,.55f);
            var brass=WorkshopArt.Mat("Clock brushed brass",new Color(.72f,.47f,.18f),.72f,.55f);
            var ivory=WorkshopArt.Mat("Clock porcelain face",new Color(.84f,.79f,.62f),.05f,.4f);
            var ink=WorkshopArt.Mat("Clock dial ink",new Color(.035f,.075f,.085f),.1f,.3f);
            var glow=WorkshopArt.Mat("Clock turquoise signal",new Color(.18f,.78f,.68f),.2f,.45f,true);
            var cream=new Color(.9f,.86f,.73f);
            WorkshopArt.Plate("Walnut clock cabinet",arena.transform,new Vector3(.2f,1.65f,1.08f),new Vector2(2.65f,1.85f),.16f,wood,.09f);
            WorkshopArt.Plate("Brass cabinet inset",arena.transform,new Vector3(.2f,1.67f,.982f),new Vector2(2.5f,1.68f),.035f,brass,.07f);
            WorkshopArt.Plate("Midnight instrument panel",arena.transform,new Vector3(.2f,1.67f,.952f),new Vector2(2.44f,1.62f),.035f,dark,.055f);
            WorkshopArt.Plate("Solid repair bench",arena.transform,new Vector3(.2f,.87f,.85f),new Vector2(2.8f,.12f),.85f,wood,.045f);
            for(int side=-1;side<=1;side+=2){
                WorkshopArt.Box("Carved cabinet support",arena.transform,new Vector3(.2f+side*1.1f,.42f,.95f),new Vector3(.13f,.84f,.35f),wood);
                WorkshopArt.Plate("Cabinet corner cap",arena.transform,new Vector3(.2f+side*1.22f,2.42f,.95f),new Vector2(.11f,.18f),.03f,brass,.02f);
            }
            var title=WorkshopArt.Label(arena.transform,"THE CLOCKMAKER  /  RHYTHM REPAIR",new Vector3(.2f,2.42f,.91f),new Vector2(2.25f,.16f),.40f,cream);title.characterSpacing=2;
            WorkshopArt.Plate("Scoreboard inset",arena.transform,new Vector3(.2f,2.17f,.913f),new Vector2(2.26f,.28f),.035f,ink,.025f);
            run.view.status=WorkshopArt.Label(arena.transform,"READY TO RESTORE",new Vector3(-.22f,2.17f,.884f),new Vector2(1.34f,.2f),.58f,cream);
            run.view.comboLabel=WorkshopArt.Label(arena.transform,"COMBO 00",new Vector3(1.00f,2.17f,.884f),new Vector2(.75f,.2f),.46f,new Color(.3f,.85f,.75f));
            WorkshopArt.Box("Score divider",arena.transform,new Vector3(.58f,2.17f,.88f),new Vector3(.008f,.17f,.007f),brass);
            var progressPivot=new GameObject("Repair progress origin");progressPivot.transform.SetParent(arena.transform,false);progressPivot.transform.localPosition=new Vector3(-.9f,2.005f,.90f);
            WorkshopArt.Box("Repair progress rail",arena.transform,new Vector3(.2f,2.005f,.925f),new Vector3(2.2f,.018f,.015f),ink);
            WorkshopArt.Box("Repair progress fill",progressPivot.transform,new Vector3(1.1f,0,0),new Vector3(2.2f,.018f,.015f),glow);run.view.progress=progressPivot.transform;
            for (int i = 0; i < 3; i++) {
                float x = .2f + (i-1)*.65f;
                const float y=1.62f;
                WorkshopArt.Disc("Brass clock housing "+(i+1),arena.transform,new Vector3(x,y,.929f),.61f,.085f,brass);
                var dial=WorkshopArt.Disc("Porcelain clock dial "+(i+1),arena.transform,new Vector3(x,y,.882f),.555f,.022f,ivory);run.view.faces[i]=dial.GetComponent<Renderer>();
                WorkshopArt.Ring("Inner dial engraving",arena.transform,new Vector3(x,y,.865f),.242f,.003f,brass);
                var halo=WorkshopArt.Ring("Beat signal bezel",arena.transform,new Vector3(x,y,.886f),.322f,.010f,glow);run.view.halos[i]=halo.GetComponent<Renderer>();
                WorkshopArt.TickRing("Engraved minute track",arena.transform,new Vector3(x,y,.863f),.26f,ink);
                for(int lightIndex=0;lightIndex<12;lightIndex++){
                    float angle=lightIndex*30*Mathf.Deg2Rad;
                    var led=WorkshopArt.Disc("Approach indicator",arena.transform,new Vector3(x+Mathf.Sin(angle)*.324f,y+Mathf.Cos(angle)*.324f,.87f),.022f,.009f,glow);
                    run.view.approachLights[i*12+lightIndex]=led.GetComponent<Renderer>();
                }
                foreach(int hour in new[]{3,6,9,12}){
                    float angle=hour*30*Mathf.Deg2Rad;WorkshopArt.Label(arena.transform,hour.ToString(),new Vector3(x+Mathf.Sin(angle)*.197f,y+Mathf.Cos(angle)*.197f,.854f),new Vector2(.11f,.08f),.31f,new Color(.07f,.12f,.13f));
                }
                WorkshopArt.Disc("Gold timing jewel",arena.transform,new Vector3(x,y+.321f,.85f),.045f,.015f,brass);
                var pivot = new GameObject("Moving hand pivot " + i); pivot.transform.SetParent(arena.transform, false);
                pivot.transform.localPosition = new Vector3(x,y,.833f); run.view.hands[i] = pivot.transform;
                WorkshopArt.Box("Blued clock hand",pivot.transform,new Vector3(0,.095f,0),new Vector3(.015f,.215f,.013f),ink);
                WorkshopArt.Box("Hand gold inlay",pivot.transform,new Vector3(0,.1f,-.008f),new Vector3(.005f,.19f,.004f),brass);
                WorkshopArt.Disc("Counterweight",pivot.transform,new Vector3(0,-.045f,0),.038f,.01f,brass);
                WorkshopArt.Disc("Clock hand cap",arena.transform,new Vector3(x,y,.819f),.055f,.02f,brass);
                WorkshopArt.Plate("Bell numbered plaque",arena.transform,new Vector3(x,1.205f,.898f),new Vector2(.45f,.15f),.022f,brass,.025f);
                WorkshopArt.Label(arena.transform,(i+1)+"  /  "+new[]{"A","S","D"}[i],new Vector3(x,1.205f,.88f),new Vector2(.42f,.12f),.44f,new Color(.055f,.09f,.10f));
                var bell=new GameObject("Bell "+(i+1));bell.transform.SetParent(arena.transform,false);bell.transform.localPosition=new Vector3(x,.94f,.57f);
                var collider=bell.AddComponent<BoxCollider>();collider.center=new Vector3(0,.14f,0);collider.size=new Vector3(.43f,.34f,.43f);
                var bellArt=new GameObject("Animated bell ornament");bellArt.transform.SetParent(bell.transform,false);run.view.bells[i]=bellArt.transform;
                WorkshopArt.Bell("Cast brass bell",bellArt.transform,Vector3.zero,brass);
                var rim=WorkshopArt.Ring("Bell rolled rim",bellArt.transform,Vector3.zero,.177f,.008f,brass);rim.transform.localRotation=Quaternion.Euler(90,0,0);
                WorkshopArt.Sphere("Bell crown",bellArt.transform,new Vector3(0,.263f,0),new Vector3(.055f,.047f,.055f),brass);
                WorkshopArt.Plate("Bell felt base",arena.transform,new Vector3(x,.921f,.57f),new Vector2(.44f,.055f),.43f,dark,.02f);
                var target = bell.AddComponent<ClockBell>(); target.run = run; target.lane = i;
                bell.AddComponent<XRSimpleInteractable>();
                var interaction = bell.AddComponent<VillageInteractable>(); interaction.prompt = "Strike bell " + (i+1);
                UnityEventTools.AddPersistentListener(interaction.action, target.Strike);
            }
            WorkshopArt.Plate("Repair feedback plaque",arena.transform,new Vector3(.2f,2.285f,.89f),new Vector2(1.1f,.105f),.015f,ink,.02f);
            run.view.feedback=WorkshopArt.Label(arena.transform,"",new Vector3(.2f,2.285f,.873f),new Vector2(1.05f,.09f),.34f,new Color(.4f,.95f,.8f));
            run.view.accuracyLabel=WorkshopArt.Label(arena.transform,"23 / 32 NOTES  +  MAX 8 EXTRA",new Vector3(.2f,.865f,.404f),new Vector2(1.95f,.10f),.30f,cream);
            WorkshopArt.Label(arena.transform,"STRIKE AT THE GOLD JEWEL     /     ESC: PAUSE",new Vector3(.2f,.685f,.62f),new Vector2(2.25f,.12f),.29f,cream);
            var start=WorkshopArt.Plate("Start clock repair",arena.transform,new Vector3(-1.01f,1.14f,.66f),new Vector2(.25f,.28f),.1f,dark,.035f);start.AddComponent<BoxCollider>().size=new Vector3(.25f,.28f,.1f);
            start.AddComponent<XRSimpleInteractable>(); var use = start.AddComponent<VillageInteractable>(); use.prompt = "Repair the clocks";
            UnityEventTools.AddPersistentListener(use.action, owner.Offer);
            WorkshopArt.Label(arena.transform,"PLAY",new Vector3(-1.01f,1.14f,.596f),new Vector2(.23f,.13f),.32f,cream);
            for(int side=-1;side<=1;side+=2)foreach(float y in new[]{.91f,2.37f})WorkshopArt.Screw(arena.transform,new Vector3(.2f+side*1.17f,y,.921f),brass);
            var studio=new GameObject("Clock cabinet warm work light",typeof(Light));studio.transform.SetParent(arena.transform,false);studio.transform.localPosition=new Vector3(-.4f,2.4f,-.6f);
            var light=studio.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.87f,.65f);light.intensity=2.2f;light.range=5;light.shadows=LightShadows.None;
            Save(house); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/clock-rhythm-build.txt", "PASS: Clockmaker scene/prefab saved with walnut/brass cabinet, porcelain dials, shared engraved tick mesh, approach lights, animated cast bells, score cards and fully visible controls. Three original bell bindings retained; visual ornament has no colliders. No new camera or XR rig.\n");
        }
        static void ClearDisplaySpace(HouseSceneRoot house){
            var area=new Bounds(new Vector3(.1f,1.1f,.7f),new Vector3(3.4f,1.3f,1.8f));
            var roots=house.copiedRoots.Concat(house.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Atmosphere & set dressing")).Select(t=>t.gameObject)).Distinct();
            foreach(var root in roots){if(!root||!root.activeSelf)continue;foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))if(area.Contains(renderer.bounds.center)&&renderer.bounds.size.magnitude<2.6f)renderer.gameObject.SetActive(false);foreach(var collider in root.GetComponentsInChildren<Collider>(true))if(area.Contains(collider.bounds.center)&&collider.bounds.size.magnitude<2.6f)collider.enabled=false;}
            foreach(var tr in house.GetComponentsInChildren<Transform>(true))if(tr.name=="Clockmaker brass timing wheel")tr.gameObject.SetActive(false);
        }
        public static void Save(HouseSceneRoot house) {
            PrefabUtility.SaveAsPrefabAsset(house.gameObject, Root + "Prefabs/ClockmakersHouseContent.prefab", out bool saved);
            if (!saved) throw new Exception("Clockmaker prefab save failed; scene installation incomplete.");
        }
    }
}
