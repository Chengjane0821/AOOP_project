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
    public static class PowerGridBuilder {
        const string Root="Assets/TheForgottenVillage/";
        [MenuItem("Forgotten Village/Install Technician power-grid emergency")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play Mode before building.");
            var scene=EditorSceneManager.OpenScene(Root+"Scenes/TechniciansHouse.unity");
            var house=UnityEngine.Object.FindFirstObjectByType<HouseSceneRoot>();
            if(!house||house.houseIndex!=3)throw new Exception("Expected Technician house.");
            foreach(var action in house.GetComponentsInChildren<VillageAction>(true).Where(a=>a.kind=="diagnostic").ToArray())UnityEngine.Object.DestroyImmediate(action.gameObject);
            foreach(var action in house.GetComponentsInChildren<VillageAction>(true).Where(a=>a.kind=="dialogue"&&a.index==3).ToArray()){
                var position=action.transform.position;UnityEngine.Object.DestroyImmediate(action.gameObject);
                var host=new GameObject("Resident interaction");host.transform.SetParent(house.transform,false);host.transform.position=position;
                host.AddComponent<BoxCollider>().size=new Vector3(.45f,1.6f,.45f);
                var speak=host.AddComponent<VillageAction>();speak.kind="dialogue";speak.index=3;
                host.AddComponent<XRSimpleInteractable>();var use=host.AddComponent<VillageInteractable>();use.prompt="Speak to the Technician";
                UnityEventTools.AddPersistentListener(use.action,speak.Use);
            }
            foreach(var idle in house.GetComponentsInChildren<ResidentIdle>(true))UnityEngine.Object.DestroyImmediate(idle);
            foreach(var tr in house.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
            foreach(var station in house.GetComponentsInChildren<EngineeringStation>(true).ToArray())if(station.topic==3)UnityEngine.Object.DestroyImmediate(station.gameObject);
            var old=house.GetComponent<PowerGridHouse>();if(old)UnityEngine.Object.DestroyImmediate(old);
            var previous=house.transform.Find("Power-grid emergency workshop");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            foreach(var tr in house.GetComponentsInChildren<Transform>(true).ToArray()){
                if(!tr)continue;string n=tr.name;
                if(n=="Workbench leg"||n=="ENGINEERING STATION"||n=="Engineering field card"||n=="Brass field card edge"||n.Contains("SAFETY DIAGNOSTICS"))UnityEngine.Object.DestroyImmediate(tr.gameObject);
            }
            foreach(var root in scene.GetRootGameObjects())foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){
                if(text.text==VillageGame.Houses[3])text.gameObject.SetActive(false);
                if(text.text.Contains("OBJECT SLICING")||text.text.Contains("PASS BY VALUE")||text.text.Contains("PASS-BY-VALUE")||text.text.Contains("CONTROLLER SOFTWARE")||text.text.Contains("MIC-17 SAFETY CONTROLLER"))
                    text.text="RESTORE THE WORKSHOP\n\nSelect generator A or B\nConnect numbered machines\nKeep loads within capacity";
            }
            var owner=house.gameObject.AddComponent<PowerGridHouse>();
            ClearPanelSpace(house);
            var panel=new GameObject("Power-grid emergency workshop");panel.transform.SetParent(house.transform,false);panel.transform.localPosition=new Vector3(-.3f,0,0);
            var run=panel.AddComponent<PowerGridRun>();owner.run=run;run.house=owner;
            string path=Root+"PowerGridTuning.asset";var tuning=AssetDatabase.LoadAssetAtPath<PowerGridTuning>(path);
            if(!tuning){tuning=ScriptableObject.CreateInstance<PowerGridTuning>();AssetDatabase.CreateAsset(tuning,path);}run.tuning=tuning;
            run.sound=panel.AddComponent<PowerGridAudio>();panel.AddComponent<PowerGridInput>().run=run;run.view=panel.AddComponent<PowerGridView>();
            var point=new GameObject("Stationary grid playing point");point.transform.SetParent(panel.transform,false);point.transform.localPosition=new Vector3(.2f,.15f,-1.45f);run.playingPoint=point.transform;
            var dark=Material("Grid panel dark",new Color(.035f,.07f,.11f));var brass=Material("Grid panel frame",new Color(.6f,.48f,.2f));
            var a=Material("Grid generator A",tuning.sourceA);var b=Material("Grid generator B",tuning.sourceB);
            var face=Material("Grid machine offline",tuning.offline);var red=Material("Grid warning",tuning.fault);
            var cableMaterial=Material("Grid cable",Color.white,true);
            Box("Grid workbench",panel.transform,new Vector3(.2f,.52f,.95f),new Vector3(3.35f,.12f,.65f),dark);
            Box("Grid mounting board",panel.transform,new Vector3(.2f,1.4f,1),new Vector3(3.35f,1.7f,.09f),dark);
            var generators=new Transform[2];
            for(int source=0;source<2;source++){
                float x=source==0?-.65f:1.05f;
                var button=Button("Generator "+(source==0?"A":"B"),panel.transform,new Vector3(x,1.81f,.84f),new Vector3(1.25f,.22f,.12f),source==0?a:b,run,GridCommand.SelectSource,source);
                generators[source]=button.transform;run.view.sourceFaces[source]=button.GetComponent<Renderer>();
                run.view.sourceLabels[source]=Label(panel.transform,"",new Vector3(x,2.05f,.78f),new Vector2(1.45f,.32f),.52f);
                var rail=Box("Breaker heat rail",panel.transform,new Vector3(x,1.61f,.83f),new Vector3(1.3f,.05f,.03f),brass);
                var pivot=new GameObject("Heat fill pivot");pivot.transform.SetParent(panel.transform,false);pivot.transform.localPosition=new Vector3(x-.65f,1.61f,.79f);
                var fill=Box("Breaker heat",pivot.transform,new Vector3(.65f,0,0),new Vector3(1.3f,.045f,.035f),red);UnityEngine.Object.DestroyImmediate(fill.GetComponent<Collider>());run.view.heatBars[source]=pivot.transform;
                UnityEngine.Object.DestroyImmediate(rail.GetComponent<Collider>());
            }
            for(int i=0;i<5;i++){
                float x=-1.1f+i*.65f;
                var socket=Button("Machine socket "+(i+1),panel.transform,new Vector3(x,1.17f,.82f),new Vector3(.5f,.27f,.12f),face,run,GridCommand.ConnectMachine,i);
                run.view.machineControls[i]=socket;run.view.machineLabels[i]=Label(panel.transform,"",new Vector3(x,1.41f,.77f),new Vector2(.61f,.2f),.55f);
                var cable=new GameObject("Cable to machine "+(i+1),typeof(LineRenderer),typeof(PowerGridCable));cable.transform.SetParent(panel.transform,false);
                var line=cable.GetComponent<LineRenderer>();line.sharedMaterial=cableMaterial;line.useWorldSpace=true;line.positionCount=16;line.enabled=false;
                var visual=cable.GetComponent<PowerGridCable>();visual.socket=socket.transform;visual.generators=generators;run.view.cables[i]=visual;
                var machine=new GameObject("Workshop machine "+(i+1));machine.transform.SetParent(panel.transform,false);machine.transform.localPosition=new Vector3(x,.99f,.72f);
                var display=machine.AddComponent<PowerGridMachine>();
                var body=Box("Machine motor",machine.transform,Vector3.zero,new Vector3(.35f,.1f,.18f),face);UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());display.indicator=body.GetComponent<Renderer>();
                var rotor=new GameObject("Motor rotor");rotor.transform.SetParent(machine.transform,false);rotor.transform.localPosition=new Vector3(0,0,-.12f);display.rotor=rotor.transform;
                for(int blade=0;blade<2;blade++){var vane=Box("Rotating conveyor motor",rotor.transform,Vector3.zero,new Vector3(.2f,.025f,.02f),brass);vane.transform.localRotation=Quaternion.Euler(0,0,90*blade);UnityEngine.Object.DestroyImmediate(vane.GetComponent<Collider>());}
                var lamp=new GameObject("Restored task lamp",typeof(Light));lamp.transform.SetParent(machine.transform,false);lamp.transform.localPosition=new Vector3(0,.07f,-.1f);
                display.workLight=lamp.GetComponent<Light>();display.workLight.type=LightType.Point;display.workLight.range=.7f;display.workLight.color=tuning.online;display.workLight.intensity=0;display.workLight.shadows=LightShadows.None;
                run.view.machines[i]=display;
            }
            float[] positions={-1.05f,-.22f,.61f,1.44f};
            string[] names={"Main power","Reset A","Reset B","Backup"};
            for(int i=0;i<4;i++){
                var command=i==0?GridCommand.MainPower:i==3?GridCommand.Backup:GridCommand.ResetBreaker;
                Button(names[i],panel.transform,new Vector3(positions[i],.72f,.81f),new Vector3(.72f,.23f,.12f),i==3?b:brass,run,command,i==2?1:0);
                var label=Label(panel.transform,i==1?"RESET A\nR":i==2?"RESET B\nR":"",new Vector3(positions[i],.72f,.735f),new Vector2(.7f,.2f),.43f);
                if(i==0)run.view.mainLabel=label;if(i==3)run.view.backupLabel=label;
            }
            run.view.status=Label(panel.transform,"POWER-GRID EMERGENCY\nSpeak to the Technician to begin",new Vector3(.2f,2.4f,.81f),new Vector2(3.2f,.34f),.65f);
            run.view.feedback=Label(panel.transform,"",new Vector3(.2f,.43f,.79f),new Vector2(3.1f,.15f),.45f);
            Button("Start grid repairs",panel.transform,new Vector3(-1.65f,1.65f,.73f),new Vector3(.26f,.4f,.16f),a,run,GridCommand.Offer,0);
            Label(panel.transform,"PLAY",new Vector3(-1.65f,1.96f,.7f),new Vector2(.4f,.16f),.5f);
            PrefabUtility.SaveAsPrefabAsset(house.gameObject,Root+"Prefabs/TechniciansHouseContent.prefab",out bool saved);
            if(!saved)throw new Exception("Technician prefab could not be saved.");
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Technician scene could not be saved.");AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/power-grid-build.txt","PASS: Technician scene/prefab saved with two generators, five cable sockets, load and heat displays, working machines, main/reset/backup controls; obsolete diagnostic station removed. No new camera or XR rig.\n");
        }
        static void ClearPanelSpace(HouseSceneRoot house){
            var area=new Bounds(new Vector3(0,1,.1f),new Vector3(3.6f,1.2f,2.7f));
            var cleared=new System.Collections.Generic.List<string>();
            var roots=house.copiedRoots.Concat(house.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Atmosphere & set dressing")).Select(t=>t.gameObject)).Distinct();
            foreach(var root in roots){
                if(!root||!root.activeSelf)continue;
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if(area.Contains(renderer.bounds.center)&&renderer.bounds.size.magnitude<2.8f){
                        cleared.Add(renderer.name+" at "+renderer.bounds.center);renderer.gameObject.SetActive(false);
                    }
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))
                    if(area.Contains(collider.bounds.center)&&collider.bounds.size.magnitude<2.8f)collider.enabled=false;
            }
            File.WriteAllLines("Verification/power-grid-cleared-props.txt",cleared);
        }
        static GameObject Button(string name,Transform parent,Vector3 position,Vector3 size,Material mat,PowerGridRun run,GridCommand command,int index){
            var go=Box(name,parent,position,size,mat);var control=go.AddComponent<PowerGridControl>();control.run=run;control.command=command;control.index=index;
            go.AddComponent<XRSimpleInteractable>();var use=go.AddComponent<VillageInteractable>();use.prompt=name;UnityEventTools.AddPersistentListener(use.action,control.Use);return go;
        }
        static GameObject Box(string name,Transform parent,Vector3 position,Vector3 size,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;return go;}
        static Material Material(string name,Color colour,bool cable=false){string path=Root+"Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find(cable?"Universal Render Pipeline/Particles/Unlit":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,path);}mat.SetColor("_BaseColor",colour);EditorUtility.SetDirty(mat);return mat;}
        static TextMeshPro Label(Transform parent,string text,Vector3 position,Vector2 size,float font){var go=new GameObject("Grid label",typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=position;var label=go.GetComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");label.text=text;label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.color=new Color(.94f,.96f,1);label.rectTransform.sizeDelta=size;return label;}
    }
}
