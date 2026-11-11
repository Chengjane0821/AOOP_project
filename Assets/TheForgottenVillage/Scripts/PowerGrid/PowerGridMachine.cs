using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridMachine : MonoBehaviour {
        public Renderer indicator;
        public Transform rotor;
        public Transform[] beltMarks;
        public Light workLight;
        MaterialPropertyBlock block;
        float power,travel;
        bool initialized;
        public bool Online{get;private set;}
        public void Render(bool online,PowerGridTuning tuning,float delta) {
            Online=online;
            if(!initialized){power=online?1:0;initialized=true;}
            if(delta>0)power=Mathf.MoveTowards(power,online?1:0,delta*(online?2.8f:4));
            if(block==null)block=new MaterialPropertyBlock();
            var colour=Color.Lerp(tuning.offline,tuning.online,power);
            block.SetColor("_BaseColor",colour);block.SetColor("_EmissionColor",tuning.online*(power*.85f));
            if(indicator)indicator.SetPropertyBlock(block);
            if(workLight){workLight.color=tuning.online;workLight.intensity=power*.3f;}
            if(delta<=0)return;
            if(rotor)rotor.Rotate(0,0,delta*tuning.machineSpeed*power,Space.Self);
            if(beltMarks!=null&&beltMarks.Length>0){
                travel=Mathf.Repeat(travel+delta*power*.12f,.24f);
                for(int i=0;i<beltMarks.Length;i++)if(beltMarks[i]){var pos=beltMarks[i].localPosition;pos.x=Mathf.Repeat(i*.06f+travel,.24f)-.12f;beltMarks[i].localPosition=pos;}
            }
        }
    }
}
