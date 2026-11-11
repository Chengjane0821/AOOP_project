using TMPro;
using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridView : MonoBehaviour {
        public TMP_Text status,feedback,mainLabel,backupLabel;
        public TMP_Text[] sourceLabels=new TMP_Text[2],machineLabels=new TMP_Text[5];
        public Renderer[] sourceFaces=new Renderer[2],sourceRims=new Renderer[2],roundLights=new Renderer[3],loadFills=new Renderer[2];
        public Transform[] heatBars=new Transform[2],loadBars=new Transform[2];
        public Transform stabilityBar,mainSwitch,backupSwitch;
        public PowerGridCable[] cables=new PowerGridCable[5];
        public PowerGridMachine[] machines=new PowerGridMachine[5];
        public GameObject[] machineControls=new GameObject[5],slotCovers=new GameObject[5];
        MaterialPropertyBlock block;
        float feedbackUntil;
        public void Feedback(string message){if(feedback)feedback.text=message;feedbackUntil=Time.unscaledTime+2;}
        void Tint(Renderer renderer,Color colour,float emission=0){
            if(!renderer)return;if(block==null)block=new MaterialPropertyBlock();
            block.Clear();block.SetColor("_BaseColor",colour);block.SetColor("_EmissionColor",colour*emission);renderer.SetPropertyBlock(block);
        }
        static void Fill(Transform fill,float amount){if(!fill)return;fill.gameObject.SetActive(amount>.001f);fill.localScale=new Vector3(Mathf.Clamp01(amount),1,1);}
        public void Render(PowerGridSession session,int selected,PowerGridTuning tuning,bool running,bool restored,float delta) {
            if(session==null||!tuning)return;
            var circuit=session.Circuit;
            if(status)status.text=running?"CIRCUIT "+(session.Round+1)+" / 3   /   "+Mathf.CeilToInt((float)session.Remaining)+"s   /   STABLE "+session.Stable.ToString("F1")+" / 4s":
                restored?"WORKSHOP ONLINE   /   DIAGNOSTIC RECORD RESTORED":"CONNECT MACHINES   /   BALANCE THE GENERATORS";
            for(int source=0;source<2;source++) {
                bool overloaded=circuit.Load(source)>circuit.Capacity(source),tripped=circuit.Tripped(source);
                Color colour=source==0?tuning.sourceA:tuning.sourceB;
                if(sourceLabels[source]){
                    sourceLabels[source].text=circuit.Load(source)+" / "+circuit.Capacity(source)+" kW\n"+(tripped?"BREAKER TRIPPED":overloaded?"OVERLOAD "+Mathf.RoundToInt((float)circuit.Heat(source)*100)+"%":"CAPACITY NORMAL");
                    sourceLabels[source].color=tripped||overloaded?tuning.fault:new Color(.86f,.93f,.95f);
                }
                Tint(sourceFaces[source],tripped||overloaded?tuning.fault:colour,tripped||overloaded?1.3f:.65f);
                if(sourceRims[source])sourceRims[source].enabled=running&&selected==source;
                Fill(loadBars[source],circuit.Load(source)/(float)circuit.Capacity(source));
                Fill(heatBars[source],(float)circuit.Heat(source));
                Tint(loadFills[source],overloaded?tuning.fault:colour,.5f);
            }
            for(int i=0;i<roundLights.Length;i++)Tint(roundLights[i],restored||i<session.Round?tuning.online:i==session.Round?tuning.sourceB:tuning.offline,restored||i<=session.Round?.7f:0);
            Fill(stabilityBar,restored?1:(float)(session.Stable/PowerGridLevel.HoldSeconds));
            if(mainLabel)mainLabel.text=circuit.MainPower?"ON / SPACE":"OFF / SPACE";
            if(backupLabel)backupLabel.text=circuit.Level.NeedsBackup?(circuit.BackupOnline?"BACKUP ON / B":"BACKUP +2 / B"):"BACKUP / ROUND 3";
            if(delta>0){
                if(mainSwitch)mainSwitch.localRotation=Quaternion.Slerp(mainSwitch.localRotation,Quaternion.Euler(0,0,circuit.MainPower?-12:12),Mathf.Min(1,delta*12));
                if(backupSwitch)backupSwitch.localRotation=Quaternion.Slerp(backupSwitch.localRotation,Quaternion.Euler(0,0,circuit.BackupOnline?-12:12),Mathf.Min(1,delta*12));
            }
            for(int i=0;i<5;i++) {
                bool active=i<circuit.MachineCount;
                if(machineControls[i])machineControls[i].SetActive(active);
                if(slotCovers[i])slotCovers[i].SetActive(!active&&!restored);
                if(machineLabels[i]){
                    machineLabels[i].gameObject.SetActive(active);
                    if(active)machineLabels[i].text="0"+(i+1)+"  /  "+circuit.Level.Demand[i]+" kW";
                }
                if(machines[i]){machines[i].gameObject.SetActive(active||restored);machines[i].Render(restored||(active&&circuit.Powered(i)),tuning,delta);}
                int from=active?circuit.SourceFor(i):-1;
                if(cables[i])cables[i].Render(from,from==0?tuning.sourceA:tuning.sourceB,tuning.cableWidth,active&&circuit.Powered(i),delta);
            }
            bool hasFeedback=feedback&&Time.unscaledTime<=feedbackUntil&&!string.IsNullOrEmpty(feedback.text);
            if(feedback){if(!hasFeedback)feedback.text="";feedback.enabled=hasFeedback;}
            if(status)status.enabled=!hasFeedback;
        }
    }
}
