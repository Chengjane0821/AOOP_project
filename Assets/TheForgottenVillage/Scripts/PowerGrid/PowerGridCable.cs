using UnityEngine;
namespace ForgottenVillage {
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PowerGridCable : MonoBehaviour {
        public Transform socket;
        public Transform[] generators;
        LineRenderer line;
        void Awake(){line=GetComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=16;}
        public void Render(int source,Color colour,float width,bool power) {
            if(!line)line=GetComponent<LineRenderer>();
            line.enabled=source>=0;
            if(source<0||source>=generators.Length||!socket||!generators[source])return;
            line.widthMultiplier=width;line.startColor=line.endColor=power?colour:colour*.45f;
            Vector3 from=generators[source].position,to=socket.position;
            line.positionCount=16;
            for(int i=0;i<16;i++) {
                float t=i/15f;Vector3 p=Vector3.Lerp(from,to,t);
                p+=Vector3.down*(Mathf.Sin(t*Mathf.PI)*.12f)+Vector3.back*(Mathf.Sin(t*Mathf.PI)*.08f);
                line.SetPosition(i,p);
            }
        }
    }
}
