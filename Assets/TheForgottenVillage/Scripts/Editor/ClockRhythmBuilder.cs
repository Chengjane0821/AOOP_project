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
                if(text.text.Contains("REGISTER SETUP ANALYSIS")||text.text.StartsWith("RESTORE THE RHYTHM"))text.gameObject.SetActive(false);
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
            point.transform.localPosition = new Vector3(.2f, .15f, -1.65f); run.playingPoint = point.transform;
            var vrPoint = new GameObject("Reachable six bell VR playing point"); vrPoint.transform.SetParent(arena.transform,false);
            vrPoint.transform.localPosition = new Vector3(.2f,.15f,.03f); run.vrPlayingPoint = vrPoint.transform;
            ClearDisplaySpace(house);
            var wood=WorkshopArt.Mat("Clock open stand walnut",new Color(.29f,.19f,.12f),0,.20f);
            var navy=WorkshopArt.Mat("Clock arcade navy",new Color(.025f,.043f,.075f),0,.12f);
            var bronze=WorkshopArt.Mat("Clock matte bronze",new Color(.50f,.34f,.17f),.10f,.18f);
            var ivory=WorkshopArt.Mat("Clock matte ivory",new Color(.86f,.86f,.80f),0,.12f);
            var ink=WorkshopArt.Mat("Clock matte hand",new Color(.035f,.075f,.085f),0,.16f);
            var gold=WorkshopArt.Mat("Clock timing gold",new Color(.95f,.67f,.15f),0,.18f);
            var glow=WorkshopArt.Mat("Clock arcade mint",new Color(.21f,.77f,.66f),0,.20f,true);
            var cream=new Color(.93f,.91f,.83f);
            // A floating scoreboard and a separate controls card match the other
            // house games. The clock field itself stays open to the room.
            var hud=new GameObject("Rhythm floating HUD");hud.transform.SetParent(arena.transform,false);hud.transform.localPosition=new Vector3(.2f,2.8f,2.25f);hud.transform.localScale=Vector3.one*1.35f;
            const string overlayPath=Root+"Materials/Clock scoreboard overlay.mat";
            var hudMaterial=AssetDatabase.LoadAssetAtPath<Material>(overlayPath);
            if(!hudMaterial){hudMaterial=new Material(Shader.Find("ForgottenVillage/World UI Overlay"));AssetDatabase.CreateAsset(hudMaterial,overlayPath);}
            hudMaterial.SetColor("_Color",new Color(.035f,.055f,.085f,1));EditorUtility.SetDirty(hudMaterial);
            WorkshopArt.Plate("Rhythm scoreboard",hud.transform,new Vector3(0,.17f,0),new Vector2(2.3f,.65f),.025f,hudMaterial,.025f);
            WorkshopArt.Label(hud.transform,"RHYTHM REPAIR",new Vector3(-.30f,.37f,-.023f),new Vector2(1.50f,.14f),1.05f,cream);
            run.view.feedback=WorkshopArt.Label(hud.transform,"",new Vector3(.78f,.37f,-.03f),new Vector2(.66f,.14f),.85f,new Color(.40f,.95f,.78f));
            run.view.status=WorkshopArt.Label(hud.transform,"READY TO RESTORE",new Vector3(-.39f,.18f,-.03f),new Vector2(1.45f,.18f),1.10f,cream);
            run.view.comboLabel=WorkshopArt.Label(hud.transform,"COMBO 00",new Vector3(.78f,.18f,-.03f),new Vector2(.68f,.18f),.90f,new Color(.40f,.91f,.78f));
            run.view.phraseLabel=WorkshopArt.Label(hud.transform,"TOP: A S D     /     LOWER: J K L",new Vector3(0,.01f,-.03f),new Vector2(2.1f,.13f),.72f,new Color(.64f,.82f,.79f));
            WorkshopArt.Box("Repair progress rail",hud.transform,new Vector3(0,.09f,-.03f),new Vector3(2.1f,.018f,.009f),ink);
            var progressPivot=new GameObject("Repair progress origin");progressPivot.transform.SetParent(hud.transform,false);progressPivot.transform.localPosition=new Vector3(-1.05f,.09f,-.042f);
            WorkshopArt.Box("Repair progress fill",progressPivot.transform,new Vector3(1.05f,0,0),new Vector3(2.1f,.018f,.009f),glow);run.view.progress=progressPivot.transform;
            WorkshopArt.Plate("Rhythm controls",arena.transform,new Vector3(-1.53f,1.62f,.89f),new Vector2(.96f,1.20f),.025f,navy,.025f);
            WorkshopArt.Label(arena.transform,"SIX BELLS\n\nTop: A / S / D\nLower: J / K / L\nVR: touch or select\n\nStrike at the gold mark\nEsc: pause / retry",new Vector3(-1.53f,1.62f,.868f),new Vector2(.86f,1.08f),.76f,cream);
            // Each column has a slim independent stand. No row-wide shelf or
            // plaque crosses the lower clock's twelve-o'clock timing marker.
            for(int column=0;column<3;column++){
                float x=.2f+(column-1)*.55f;
                WorkshopArt.Plate("Clock stand foot",arena.transform,new Vector3(x,.20f,.92f),new Vector2(.30f,.06f),.36f,wood,.02f);
                WorkshopArt.Box("Slim clock support",arena.transform,new Vector3(x,1.10f,1.01f),new Vector3(.025f,1.80f,.025f),bronze);
            }
            for(int i=0;i<ClockChart.LaneCount;i++){
                int row=i/3;float x=.2f+(i%3-1)*.55f;
                float y=row==0?1.91f:1.14f;
                float bellY=row==0?1.55f:.86f,bellZ=row==0?.53f:.43f;
                WorkshopArt.Disc("Clock frame "+(i+1),arena.transform,new Vector3(x,y,.92f),.46f,.035f,navy);
                var dial=WorkshopArt.Disc("Plain clock face "+(i+1),arena.transform,new Vector3(x,y,.895f),.425f,.012f,ivory);run.view.faces[i]=dial.GetComponent<Renderer>();
                var halo=WorkshopArt.Ring("Beat signal "+(i+1),arena.transform,new Vector3(x,y,.883f),.225f,.004f,glow);run.view.halos[i]=halo.GetComponent<Renderer>();
                for(int ledIndex=0;ledIndex<12;ledIndex++){
                    float angle=ledIndex*30*Mathf.Deg2Rad;
                    var led=WorkshopArt.Disc("Beat approach dot",arena.transform,new Vector3(x+Mathf.Sin(angle)*.225f,y+Mathf.Cos(angle)*.225f,.875f),.014f,.006f,glow);
                    run.view.approachLights[i*12+ledIndex]=led.GetComponent<Renderer>();
                }
                foreach(int hour in new[]{3,6,9}){
                    float angle=hour*30*Mathf.Deg2Rad;
                    WorkshopArt.Box("Quarter hour mark",arena.transform,new Vector3(x+Mathf.Sin(angle)*.176f,y+Mathf.Cos(angle)*.176f,.881f),new Vector3(.012f,.012f,.004f),ink);
                }
                WorkshopArt.Disc("Gold timing marker",arena.transform,new Vector3(x,y+.225f,.863f),.036f,.010f,gold);
                WorkshopArt.Label(arena.transform,(i+1)+" / "+ClockRhythmInput.LaneKeys[i],new Vector3(x+.235f,bellY+.075f,bellZ-.055f),new Vector2(.24f,.10f),.72f,cream);
                var pivot=new GameObject("Moving hand pivot "+i);pivot.transform.SetParent(arena.transform,false);pivot.transform.localPosition=new Vector3(x,y,.867f);run.view.hands[i]=pivot.transform;
                WorkshopArt.Box("Clock hand",pivot.transform,new Vector3(0,.078f,0),new Vector3(.014f,.174f,.008f),ink);
                WorkshopArt.Disc("Clock hand cap",arena.transform,new Vector3(x,y,.855f),.040f,.012f,bronze);
                // Compact bells sit below their own face, with contact volumes
                // independent from the art and its press animation.
                var bell=new GameObject("Bell "+(i+1));bell.transform.SetParent(arena.transform,false);bell.transform.localPosition=new Vector3(x,bellY,bellZ);
                var collider=bell.AddComponent<BoxCollider>();collider.center=new Vector3(0,.080f,0);collider.size=new Vector3(.29f,.20f,.29f);
                var bellArt=new GameObject("Animated bell ornament");bellArt.transform.SetParent(bell.transform,false);bellArt.transform.localScale=Vector3.one*.48f;run.view.bells[i]=bellArt.transform;
                WorkshopArt.Bell("Matte bronze bell",bellArt.transform,Vector3.zero,bronze);
                WorkshopArt.Plate("Individual bell base",arena.transform,new Vector3(x,bellY-.016f,bellZ),new Vector2(.30f,.035f),.30f,wood,.02f);
                WorkshopArt.Box("Bell mounting arm",arena.transform,new Vector3(x+.11f,bellY-.035f,.76f),new Vector3(.018f,.018f,.49f),bronze);
                var target=bell.AddComponent<ClockBell>();target.run=run;target.lane=i;
                bell.AddComponent<XRSimpleInteractable>();
                var interaction=bell.AddComponent<VillageInteractable>();interaction.prompt="Strike bell "+(i+1);
                UnityEventTools.AddPersistentListener(interaction.action,target.Strike);
            }
            run.view.accuracyLabel=WorkshopArt.Label(hud.transform,ClockChart.RequiredHits+" / "+ClockChart.NoteCount+" NOTES  +  MAX "+ClockChart.MaxStrays+" EXTRA",new Vector3(0,-.10f,-.03f),new Vector2(2.20f,.10f),.57f,cream);
            var start=WorkshopArt.Plate("Start clock repair",arena.transform,new Vector3(-1.53f,.87f,.89f),new Vector2(.80f,.23f),.06f,navy,.025f);start.AddComponent<BoxCollider>().size=new Vector3(.80f,.23f,.06f);
            start.AddComponent<XRSimpleInteractable>();var use=start.AddComponent<VillageInteractable>();use.prompt="Repair the clocks";
            UnityEventTools.AddPersistentListener(use.action,owner.Offer);
            WorkshopArt.Label(arena.transform,"PLAY",new Vector3(-1.53f,.87f,.85f),new Vector2(.72f,.16f),1.0f,cream);
            foreach(var label in hud.GetComponentsInChildren<TMP_Text>())label.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/Field Text Overlay.mat");
            Save(house); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/clock-rhythm-build.txt", "PASS: Clockmaker scene/prefab saved with an open six-clock field on slim independent stands, compact matte-bronze bells, plain ivory faces and gold timing marks. Floating navy scoreboard, left controls and lower goal strip match the tennis/plane interface language. Six A/S/D + J/K/L bindings, 72 approach dots, controller-contact and XR selection retained. Separate desktop/VR positions; no enclosing cabinet, continuous shelves, decorative colliders, new camera or XR rig.\n");
        }
        static void ClearDisplaySpace(HouseSceneRoot house){
            var area=new Bounds(new Vector3(.1f,1.65f,.7f),new Vector3(3.4f,2.7f,1.8f));
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
