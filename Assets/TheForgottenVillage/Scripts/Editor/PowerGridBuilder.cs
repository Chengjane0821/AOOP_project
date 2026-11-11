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
        static readonly Color shadowColour=new Color(.015f,.035f,.05f);
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
            var point=new GameObject("Stationary grid playing point");point.transform.SetParent(panel.transform,false);point.transform.localPosition=new Vector3(.2f,.15f,-1.7f);run.playingPoint=point.transform;
            var navy=WorkshopArt.Mat("Grid navy enamel",new Color(.035f,.075f,.12f),.45f,.36f);
            var steel=WorkshopArt.Mat("Grid brushed steel",new Color(.35f,.43f,.48f),.8f,.32f);
            var shadow=WorkshopArt.Mat("Grid inset charcoal",new Color(.012f,.025f,.04f),.25f,.28f);
            var rubber=WorkshopArt.Mat("Grid connector rubber",new Color(.027f,.043f,.055f),.05f,.25f);
            var pale=WorkshopArt.Mat("Grid porcelain",new Color(.52f,.62f,.66f),.4f,.42f);
            var a=WorkshopArt.Mat("Grid cyan indicator",tuning.sourceA,.2f,.36f,true);
            var b=WorkshopArt.Mat("Grid amber indicator",tuning.sourceB,.2f,.36f,true);
            var face=WorkshopArt.Mat("Grid machine status lens",tuning.offline,.1f,.5f,true);
            var red=WorkshopArt.Mat("Grid alarm indicator",tuning.fault,.1f,.4f,true);
            var cableMaterial=WorkshopArt.Mat("Grid insulated cable",Color.white,.15f,.42f,true);
            var pulse=WorkshopArt.Mat("Grid current pulse",new Color(.72f,.95f,1),.1f,.45f,true);
            var white=new Color(.86f,.93f,.95f);var muted=new Color(.47f,.62f,.68f);
            WorkshopArt.Plate("Grid steel chassis",panel.transform,new Vector3(.2f,1.47f,1.04f),new Vector2(3.4f,1.99f),.2f,steel,.1f);
            WorkshopArt.Plate("Grid navy enclosure",panel.transform,new Vector3(.2f,1.47f,.93f),new Vector2(3.31f,1.9f),.12f,navy,.08f);
            WorkshopArt.Box("Grid workbench",panel.transform,new Vector3(.2f,.36f,.95f),new Vector3(3.5f,.12f,.7f),navy);
            for(int corner=0;corner<4;corner++){
                float x=corner%2==0?-1.36f:1.76f,y=corner<2?.61f:2.33f;
                WorkshopArt.Disc("Recessed chassis screw",panel.transform,new Vector3(x,y,.842f),.052f,.014f,steel);
                WorkshopArt.Box("Screw slot",panel.transform,new Vector3(x,y,.831f),new Vector3(.028f,.005f,.005f),shadow);
            }
            WorkshopArt.Plate("Power station nameplate",panel.transform,new Vector3(.2f,2.49f,.91f),new Vector2(3.0f,.36f),.08f,shadow,.045f);
            Label(panel.transform,"POWER-GRID EMERGENCY",new Vector3(.2f,2.54f,.858f),new Vector2(2.85f,.14f),.55f,white);
            run.view.status=Label(panel.transform,"WORKSHOP POWER DISTRIBUTION",new Vector3(.2f,2.405f,.85f),new Vector2(2.9f,.13f),.37f,muted);
            for(int round=0;round<3;round++){
                var led=WorkshopArt.Disc("Circuit progress lamp "+(round+1),panel.transform,new Vector3(-.02f+round*.22f,2.285f,.82f),.055f,.015f,face);
                run.view.roundLights[round]=led.GetComponent<Renderer>();
            }
            var generators=new Transform[2];
            for(int source=0;source<2;source++){
                float x=source==0?-.65f:1.05f;
                var trim=WorkshopArt.Plate("Selected generator trim",panel.transform,new Vector3(x,1.96f,.852f),new Vector2(1.49f,.62f),.055f,source==0?a:b,.06f);run.view.sourceRims[source]=trim.GetComponent<Renderer>();
                var button=Button("Generator "+(source==0?"A":"B"),panel.transform,new Vector3(x,1.96f,.801f),new Vector3(1.43f,.56f,.08f),shadow,run,GridCommand.SelectSource,source);
                var emblem=WorkshopArt.Disc("Generator selector lens",panel.transform,new Vector3(x-.51f,2.02f,.741f),.18f,.026f,source==0?a:b);run.view.sourceFaces[source]=emblem.GetComponent<Renderer>();
                WorkshopArt.Ring("Generator selector bezel",panel.transform,new Vector3(x-.51f,2.02f,.72f),.095f,.011f,steel);
                Label(panel.transform,source==0?"A":"B",new Vector3(x-.51f,2.02f,.713f),new Vector2(.16f,.15f),.72f,shadowColour);
                Label(panel.transform,"GENERATOR "+(source==0?"A  /  A":"B  /  S"),new Vector3(x+.1f,2.15f,.738f),new Vector2(1.03f,.11f),.33f,muted);
                run.view.sourceLabels[source]=Label(panel.transform,"",new Vector3(x+.1f,2.005f,.728f),new Vector2(1.05f,.22f),.48f,white);
                Label(panel.transform,"LOAD",new Vector3(x-.53f,1.84f,.731f),new Vector2(.22f,.08f),.24f,muted);
                Label(panel.transform,"TEMP",new Vector3(x-.53f,1.735f,.731f),new Vector2(.22f,.08f),.24f,muted);
                run.view.loadBars[source]=Gauge(panel.transform,"Generator load",new Vector3(x-.37f,1.84f,.739f),.93f,source==0?a:b,rubber);
                run.view.loadFills[source]=run.view.loadBars[source].GetComponentInChildren<Renderer>();
                run.view.heatBars[source]=Gauge(panel.transform,"Breaker heat",new Vector3(x-.37f,1.735f,.739f),.93f,red,rubber);
                var terminal=new GameObject("Generator output terminal");terminal.transform.SetParent(panel.transform,false);terminal.transform.localPosition=new Vector3(x,1.625f,.74f);generators[source]=terminal.transform;
                WorkshopArt.Ring("Insulated generator output",panel.transform,terminal.transform.localPosition,.055f,.014f,steel);
            }
            for(int i=0;i<5;i++){
                float x=-1.1f+i*.65f;
                WorkshopArt.Plate("Machine bay "+(i+1),panel.transform,new Vector3(x,1.165f,.86f),new Vector2(.58f,.64f),.065f,steel,.045f);
                WorkshopArt.Plate("Inset machine bay",panel.transform,new Vector3(x,1.165f,.815f),new Vector2(.54f,.6f),.04f,shadow,.03f);
                var socket=Button("Machine socket "+(i+1),panel.transform,new Vector3(x,1.355f,.77f),new Vector3(.46f,.19f,.08f),rubber,run,GridCommand.ConnectMachine,i);
                WorkshopArt.Ring("Socket retaining nut",panel.transform,new Vector3(x,1.355f,.712f),.063f,.013f,steel);
                WorkshopArt.Disc("Socket centre",panel.transform,new Vector3(x,1.355f,.71f),.09f,.012f,rubber);
                for(int pin=-1;pin<=1;pin+=2)WorkshopArt.Disc("Socket contact",panel.transform,new Vector3(x+pin*.023f,1.355f,.697f),.018f,.012f,pale);
                run.view.machineControls[i]=socket;run.view.machineLabels[i]=Label(panel.transform,"",new Vector3(x,1.18f,.699f),new Vector2(.49f,.18f),.48f,white);
                var locked=WorkshopArt.Plate("Reserved machine cover "+(i+1),panel.transform,new Vector3(x,1.245f,.697f),new Vector2(.5f,.39f),.03f,navy,.03f);
                Label(locked.transform,"0"+(i+1)+"\nRESERVED",new Vector3(0,0,-.021f),new Vector2(.46f,.28f),.38f,muted);run.view.slotCovers[i]=locked;
                var cable=new GameObject("Cable to machine "+(i+1),typeof(LineRenderer),typeof(PowerGridCable));cable.transform.SetParent(panel.transform,false);
                var line=cable.GetComponent<LineRenderer>();line.sharedMaterial=cableMaterial;line.useWorldSpace=true;line.positionCount=24;line.numCapVertices=4;line.numCornerVertices=4;line.enabled=false;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;
                var visual=cable.GetComponent<PowerGridCable>();visual.socket=socket.transform;visual.generators=generators;run.view.cables[i]=visual;
                for(int dot=0;dot<3;dot++){visual.flowParticles[dot]=WorkshopArt.Disc("Current pulse "+dot,cable.transform,Vector3.zero,.027f,.012f,pulse).transform;visual.flowParticles[dot].gameObject.SetActive(false);}
                run.view.machines[i]=Machine(panel.transform,i,new Vector3(x,.98f,.71f),steel,rubber,pale,face);
            }
            float[] positions={-1.05f,-.22f,.61f,1.44f};
            string[] names={"Main power","Reset A","Reset B","Backup"};
            for(int i=0;i<4;i++){
                var command=i==0?GridCommand.MainPower:i==3?GridCommand.Backup:GridCommand.ResetBreaker;
                WorkshopArt.Plate("Switch surround",panel.transform,new Vector3(positions[i],.695f,.85f),new Vector2(.73f,.31f),.05f,steel,.04f);
                var control=Button(names[i],panel.transform,new Vector3(positions[i],.695f,.797f),new Vector3(.68f,.26f,.07f),shadow,run,command,i==2?1:0);
                var label=Label(panel.transform,i==1?"RESET A / R":i==2?"RESET B / R":"",new Vector3(positions[i],.62f,.744f),new Vector2(.64f,.11f),.38f,white);
                var lever=new GameObject("Industrial switch rocker");lever.transform.SetParent(panel.transform,false);lever.transform.localPosition=new Vector3(positions[i],.735f,.734f);
                WorkshopArt.Plate("Switch rocker",lever.transform,Vector3.zero,new Vector2(.18f,.075f),.045f,i==0?a:i==3?b:steel,.015f);
                if(i==0)run.view.mainSwitch=lever.transform;if(i==3)run.view.backupSwitch=lever.transform;
                if(i==0)run.view.mainLabel=label;if(i==3)run.view.backupLabel=label;
            }
            run.view.stabilityBar=Gauge(panel.transform,"Safe supply",new Vector3(-1.23f,.525f,.8f),2.86f,a,rubber);
            run.view.feedback=Label(panel.transform,"",new Vector3(.2f,2.405f,.84f),new Vector2(2.9f,.13f),.37f,white);
            WorkshopArt.Plate("Grid operating legend",panel.transform,new Vector3(.2f,.465f,.75f),new Vector2(3.25f,.12f),.045f,shadow,.025f);
            Label(panel.transform,"A/S  SELECT    1-5  CONNECT    SPACE  POWER    R  RESET    B  BACKUP    ESC  PAUSE",new Vector3(.2f,.465f,.718f),new Vector2(3.12f,.1f),.28f,muted);
            Button("Start grid repairs",panel.transform,new Vector3(-1.65f,1.92f,.73f),new Vector3(.24f,.36f,.1f),navy,run,GridCommand.Offer,0);
            WorkshopArt.Ring("Start button bezel",panel.transform,new Vector3(-1.65f,1.94f,.665f),.065f,.012f,steel);
            WorkshopArt.Disc("Start light",panel.transform,new Vector3(-1.65f,1.94f,.658f),.09f,.02f,a);
            Label(panel.transform,"PLAY",new Vector3(-1.65f,1.81f,.668f),new Vector2(.22f,.1f),.29f,white);
            var lightObject=new GameObject("Grid front fill",typeof(Light));lightObject.transform.SetParent(panel.transform,false);lightObject.transform.localPosition=new Vector3(.2f,2.2f,-.9f);var light=lightObject.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(.77f,.88f,1);light.intensity=2.0f;light.range=5;light.shadows=LightShadows.None;
            PrefabUtility.SaveAsPrefabAsset(house.gameObject,Root+"Prefabs/TechniciansHouseContent.prefab",out bool saved);
            if(!saved)throw new Exception("Technician prefab could not be saved.");
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Technician scene could not be saved.");AssetDatabase.SaveAssets();
            File.WriteAllText("Verification/power-grid-build.txt","PASS: Technician scene/prefab saved with navy enamel/steel enclosure, inset generator cards, selection rims, separate load/heat gauges, five fixed machine bays, powered current pulses and industrial controls. Twelve original control bindings retained; decoration has no colliders. No new camera or XR rig.\n");
        }
        static Transform Gauge(Transform parent,string name,Vector3 origin,float width,Material fill,Material rail){
            WorkshopArt.Plate(name+" track",parent,origin+Vector3.right*(width*.5f),new Vector2(width+.025f,.05f),.025f,rail,.018f);
            var pivot=new GameObject(name+" fill pivot");pivot.transform.SetParent(parent,false);pivot.transform.localPosition=origin;
            WorkshopArt.Box(name+" fill",pivot.transform,new Vector3(width*.5f,0,-.02f),new Vector3(width,.025f,.012f),fill);
            return pivot.transform;
        }
        static PowerGridMachine Machine(Transform parent,int index,Vector3 position,Material steel,Material rubber,Material pale,Material lens){
            var go=new GameObject("Workshop machine "+(index+1));go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var display=go.AddComponent<PowerGridMachine>();
            var rotor=new GameObject("Motor rotor");rotor.transform.SetParent(go.transform,false);rotor.transform.localPosition=new Vector3(0,0,-.065f);display.rotor=rotor.transform;
            if(index==0){
                WorkshopArt.Disc("Task lamp housing",go.transform,Vector3.zero,.22f,.06f,steel);
                WorkshopArt.Ring("Lamp safety rim",go.transform,new Vector3(0,0,-.045f),.092f,.012f,pale);
                display.indicator=WorkshopArt.Disc("Lamp glass",go.transform,new Vector3(0,0,-.05f),.145f,.02f,lens).GetComponent<Renderer>();
                for(int fin=0;fin<4;fin++){var vane=WorkshopArt.Box("Lamp cooling fin",rotor.transform,new Vector3(0,.084f,0),new Vector3(.015f,.03f,.01f),steel);vane.transform.RotateAround(rotor.transform.position,rotor.transform.forward,90*fin);}
            }else if(index==1){
                WorkshopArt.Disc("Ventilation housing",go.transform,Vector3.zero,.22f,.065f,rubber);
                WorkshopArt.Ring("Fan retaining bezel",go.transform,new Vector3(0,0,-.048f),.105f,.014f,steel);
                for(int blade=0;blade<3;blade++){var vane=WorkshopArt.Box("Ventilation blade",rotor.transform,new Vector3(0,.045f,0),new Vector3(.045f,.09f,.017f),pale);vane.transform.RotateAround(rotor.transform.position,rotor.transform.forward,120*blade);}
                display.indicator=WorkshopArt.Disc("Fan bearing light",rotor.transform,new Vector3(0,0,-.015f),.04f,.02f,lens).GetComponent<Renderer>();
            }else if(index==2){
                WorkshopArt.Plate("Conveyor casing",go.transform,Vector3.zero,new Vector2(.33f,.14f),.07f,steel,.04f);
                WorkshopArt.Plate("Conveyor belt",go.transform,new Vector3(0,0,-.048f),new Vector2(.27f,.085f),.025f,rubber,.035f);
                display.beltMarks=new Transform[4];
                for(int mark=0;mark<4;mark++)display.beltMarks[mark]=WorkshopArt.Box("Conveyor tread",go.transform,new Vector3(-.09f+mark*.06f,0,-.066f),new Vector3(.012f,.064f,.008f),pale).transform;
                rotor.transform.localPosition=new Vector3(-.13f,0,-.08f);
                WorkshopArt.Box("Conveyor drive spoke",rotor.transform,Vector3.zero,new Vector3(.04f,.009f,.008f),pale);
                display.indicator=WorkshopArt.Disc("Conveyor status",go.transform,new Vector3(.13f,-.045f,-.045f),.028f,.015f,lens).GetComponent<Renderer>();
            }else if(index==3){
                WorkshopArt.Plate("Servo body",go.transform,new Vector3(.05f,0,0),new Vector2(.17f,.17f),.07f,steel,.03f);
                WorkshopArt.Box("Servo cooling spine",go.transform,new Vector3(.05f,0,-.043f),new Vector3(.02f,.13f,.012f),pale);
                rotor.transform.localPosition=new Vector3(-.065f,0,-.06f);
                WorkshopArt.Disc("Servo drive",rotor.transform,Vector3.zero,.12f,.025f,rubber);
                WorkshopArt.Box("Servo arm",rotor.transform,new Vector3(0,.025f,-.018f),new Vector3(.02f,.09f,.014f),pale);
                display.indicator=WorkshopArt.Disc("Servo ready light",go.transform,new Vector3(.11f,.045f,-.05f),.025f,.015f,lens).GetComponent<Renderer>();
            }else{
                WorkshopArt.Plate("Archive drive enclosure",go.transform,Vector3.zero,new Vector2(.32f,.17f),.07f,steel,.025f);
                for(int bay=0;bay<3;bay++)WorkshopArt.Plate("Archive drive bay",go.transform,new Vector3(-.02f,-.045f+bay*.045f,-.047f),new Vector2(.24f,.025f),.014f,rubber,.006f);
                rotor.transform.localPosition=new Vector3(.125f,0,-.059f);
                for(int blade=0;blade<2;blade++){var vane=WorkshopArt.Box("Archive cooling fan",rotor.transform,Vector3.zero,new Vector3(.035f,.008f,.007f),pale);vane.transform.localRotation=Quaternion.Euler(0,0,90*blade);}
                display.indicator=WorkshopArt.Disc("Archive access light",go.transform,new Vector3(-.12f,.061f,-.06f),.025f,.012f,lens).GetComponent<Renderer>();
            }
            var lamp=new GameObject("Restored task lamp",typeof(Light));lamp.transform.SetParent(go.transform,false);lamp.transform.localPosition=new Vector3(0,.03f,-.16f);
            display.workLight=lamp.GetComponent<Light>();display.workLight.type=LightType.Point;display.workLight.range=.45f;display.workLight.intensity=0;display.workLight.shadows=LightShadows.None;
            return display;
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
            var go=WorkshopArt.Plate(name,parent,position,new Vector2(size.x,size.y),size.z,mat,.035f);go.AddComponent<BoxCollider>().size=size;var control=go.AddComponent<PowerGridControl>();control.run=run;control.command=command;control.index=index;
            go.AddComponent<XRSimpleInteractable>();var use=go.AddComponent<VillageInteractable>();use.prompt=name;UnityEventTools.AddPersistentListener(use.action,control.Use);return go;
        }
        static TextMeshPro Label(Transform parent,string text,Vector3 position,Vector2 size,float font,Color colour){return WorkshopArt.Label(parent,text,position,size,font,colour);}
    }
}
