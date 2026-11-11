using TMPro;
using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridView : MonoBehaviour {
        public TMP_Text status,feedback,mainLabel,backupLabel;
        public TMP_Text[] sourceLabels=new TMP_Text[2],machineLabels=new TMP_Text[5];
        public Renderer[] sourceFaces=new Renderer[2];
        public Transform[] heatBars=new Transform[2];
        public PowerGridCable[] cables=new PowerGridCable[5];
        public PowerGridMachine[] machines=new PowerGridMachine[5];
        public GameObject[] machineControls=new GameObject[5];
        MaterialPropertyBlock block;
        float feedbackUntil;
        public void Feedback(string message){if(feedback)feedback.text=message;feedbackUntil=Time.unscaledTime+2;}
        public void Render(PowerGridSession session,int selected,PowerGridTuning tuning,bool running,bool restored,float delta) {
            if(session==null||!tuning)return;
            var circuit=session.Circuit;if(block==null)block=new MaterialPropertyBlock();
            if(status)status.text=running?"ROUND "+(session.Round+1)+" / 3   "+circuit.Level.Name+"\n"+
                Mathf.CeilToInt((float)session.Remaining)+"s   STABLE "+session.Stable.ToString("F1")+" / 4s":
                restored?"WORKSHOP RESTORED\nSpeak to the Technician for the record":"POWER-GRID EMERGENCY\nSpeak to the Technician or select PLAY";
            for(int source=0;source<2;source++) {
                bool overloaded=circuit.Load(source)>circuit.Capacity(source);
                string name=source==0?"A":"B";
                if(sourceLabels[source])sourceLabels[source].text=(selected==source?"[ "+name+" ]":name)+"   "+circuit.Load(source)+" / "+circuit.Capacity(source)+" kW\n"+
                    (circuit.Tripped(source)?"TRIPPED - RESET":overloaded?"OVERLOAD! "+Mathf.RoundToInt((float)circuit.Heat(source)*100)+"%":"CAPACITY OK");
                block.SetColor("_BaseColor",circuit.Tripped(source)||overloaded?tuning.fault:source==0?tuning.sourceA:tuning.sourceB);
                if(sourceFaces[source])sourceFaces[source].SetPropertyBlock(block);
                if(heatBars[source])heatBars[source].localScale=new Vector3(Mathf.Max(.01f,(float)circuit.Heat(source)),1,1);
            }
            if(mainLabel)mainLabel.text=circuit.MainPower?"POWER ON\nSPACE":"POWER OFF\nSPACE";
            if(backupLabel)backupLabel.text=circuit.Level.NeedsBackup?(circuit.BackupOnline?"BACKUP ON\n+2 kW / B":"BACKUP OFF\n+2 kW / B"):"BACKUP\nROUND 3";
            for(int i=0;i<5;i++) {
                bool active=i<circuit.MachineCount;
                if(machineControls[i])machineControls[i].SetActive(active);
                if(machineLabels[i]) {machineLabels[i].gameObject.SetActive(active);if(active)machineLabels[i].text=(i+1)+"  |  "+circuit.Level.Demand[i]+" kW";}
                if(machines[i]){machines[i].gameObject.SetActive(active||restored);machines[i].Render(restored||(active&&circuit.Powered(i)),tuning,delta);}
                int from=active?circuit.SourceFor(i):-1;
                if(cables[i])cables[i].Render(from,from==0?tuning.sourceA:tuning.sourceB,tuning.cableWidth,active&&circuit.Powered(i));
            }
            if(feedback&&Time.unscaledTime>feedbackUntil)feedback.text="";
        }
    }
}
