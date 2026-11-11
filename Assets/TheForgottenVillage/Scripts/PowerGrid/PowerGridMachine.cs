using UnityEngine;
namespace ForgottenVillage {
    public sealed class PowerGridMachine : MonoBehaviour {
        public Renderer indicator;
        public Transform rotor;
        public Light workLight;
        MaterialPropertyBlock block;
        public bool Online{get;private set;}
        public void Render(bool online,PowerGridTuning tuning,float delta) {
            Online=online;
            if(block==null)block=new MaterialPropertyBlock();
            block.SetColor("_BaseColor",online?tuning.online:tuning.offline);
            if(indicator)indicator.SetPropertyBlock(block);
            if(workLight)workLight.intensity=online?.8f:0;
            if(online&&rotor)rotor.Rotate(0,0,delta*tuning.machineSpeed,Space.Self);
        }
    }
}
