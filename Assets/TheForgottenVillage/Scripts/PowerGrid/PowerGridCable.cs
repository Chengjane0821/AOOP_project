using UnityEngine;
namespace ForgottenVillage {
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PowerGridCable : MonoBehaviour {
        public Transform socket;
        public Transform[] generators;
        public Transform[] flowParticles=new Transform[3];
        LineRenderer line;
        MaterialPropertyBlock block;
        float travel;
        void Awake(){line=GetComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=24;}
        Vector3 Point(Vector3 from,Vector3 to,float t){
            Vector3 p=Vector3.Lerp(from,to,t);
            p+=Vector3.down*(Mathf.Sin(t*Mathf.PI)*.035f)+Vector3.forward*(Mathf.Sin(t*Mathf.PI)*.045f);
            return p;
        }
        public void Render(int source,Color colour,float width,bool power,float delta=0) {
            if(!line)line=GetComponent<LineRenderer>();
            bool connected=source>=0&&source<generators.Length&&socket&&generators[source];
            line.enabled=connected;
            if(flowParticles!=null)foreach(var particle in flowParticles)if(particle)particle.gameObject.SetActive(connected&&power);
            if(!connected)return;
            line.widthMultiplier=width*.78f;
            if(block==null)block=new MaterialPropertyBlock();
            block.SetColor("_BaseColor",power?colour:colour*.45f);block.SetColor("_EmissionColor",power?colour*.48f:Color.black);line.SetPropertyBlock(block);
            Vector3 from=generators[source].position,to=socket.position+Vector3.back*.075f;
            line.positionCount=24;
            for(int i=0;i<24;i++)line.SetPosition(i,Point(from,to,i/23f));
            if(power&&delta>0)travel=Mathf.Repeat(travel+delta*.62f,1);
            if(flowParticles!=null)for(int i=0;i<flowParticles.Length;i++)if(flowParticles[i])flowParticles[i].position=Point(from,to,Mathf.Repeat(travel+i/(float)flowParticles.Length,1))+Vector3.back*.018f;
        }
    }
}
