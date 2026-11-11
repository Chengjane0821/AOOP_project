using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ForgottenVillage.Editor {
    // Shared construction details for the two physical workshop games. Visual
    // meshes never add colliders; interaction volumes are authored separately.
    public static class WorkshopArt {
        const string Root="Assets/TheForgottenVillage/";
        public static Material Mat(string name,Color color,float metallic=0,float smoothness=.4f,bool glow=false){
            string path=Root+"Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}else if(mat.shader!=shader)mat.shader=shader;
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smoothness);
            mat.enableInstancing=true;
            if(glow){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*1.4f);}else{mat.DisableKeyword("_EMISSION");mat.SetColor("_EmissionColor",Color.black);}
            EditorUtility.SetDirty(mat);return mat;
        }
        public static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material material){
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        public static GameObject Disc(string name,Transform parent,Vector3 pos,float diameter,float depth,Material material){
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(90,0,0);go.transform.localScale=new Vector3(diameter,depth*.5f,diameter);
            go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        public static GameObject Sphere(string name,Transform parent,Vector3 pos,Vector3 size,Material material){
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);
            go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        public static GameObject Plate(string name,Transform parent,Vector3 pos,Vector2 size,float depth,Material material,float radius=.04f){
            radius=Mathf.Min(radius,Mathf.Min(size.x,size.y)*.49f);
            string key="Plate_"+Number(size.x)+"_"+Number(size.y)+"_"+Number(depth)+"_"+Number(radius);
            var mesh=MeshAsset(key,()=>RoundedPlate(size,depth,radius));return MeshObject(name,parent,pos,mesh,material);
        }
        public static GameObject Ring(string name,Transform parent,Vector3 pos,float radius,float tube,Material material){
            string key="Ring_"+Number(radius)+"_"+Number(tube);
            return MeshObject(name,parent,pos,MeshAsset(key,()=>Torus(radius,tube)),material);
        }
        public static GameObject Bell(string name,Transform parent,Vector3 pos,Material material){
            return MeshObject(name,parent,pos,MeshAsset("ClockBell",BellMesh),material);
        }
        public static GameObject TickRing(string name,Transform parent,Vector3 pos,float radius,Material material){
            return MeshObject(name,parent,pos,MeshAsset("ClockTicks_"+Number(radius),()=>{
                var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;
                var pieces=new CombineInstance[60];
                for(int i=0;i<60;i++){
                    float angle=i*6*Mathf.Deg2Rad;bool major=i%5==0;
                    pieces[i]=new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(new Vector3(Mathf.Sin(angle)*radius,Mathf.Cos(angle)*radius,0),Quaternion.Euler(0,0,-i*6),new Vector3(major?.012f:.005f,major?.036f:.015f,.004f))};
                }
                var mesh=new Mesh();mesh.CombineMeshes(pieces,true,true);UnityEngine.Object.DestroyImmediate(primitive);return mesh;
            }),material);
        }
        public static TextMeshPro Label(Transform parent,string text,Vector3 pos,Vector2 size,float fontSize,Color color){
            var go=new GameObject("Workshop label",typeof(TextMeshPro));go.transform.SetParent(parent,false);go.transform.localPosition=pos;
            var label=go.GetComponent<TextMeshPro>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            label.text=text;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=color;
            label.rectTransform.sizeDelta=size;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;return label;
        }
        public static void Screw(Transform parent,Vector3 pos,Material metal){
            Disc("Recessed screw",parent,pos,.025f,.012f,metal);
            Box("Screw slot",parent,pos+Vector3.back*.008f,new Vector3(.015f,.003f,.003f),Mat("Workshop screw slot",new Color(.035f,.045f,.055f),.4f,.3f));
        }
        static string Number(float n)=>n.ToString("0.000",CultureInfo.InvariantCulture).Replace('.','_');
        static GameObject MeshObject(string name,Transform parent,Vector3 pos,Mesh mesh,Material material){
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=pos;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static Mesh MeshAsset(string key,Func<Mesh> factory){
            string folder=Root+"Meshes";if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder(Root.TrimEnd('/'),"Meshes");
            string path=folder+"/Workshop_"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=factory();mesh.name="Workshop "+key;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}return mesh;
        }
        static Mesh RoundedPlate(Vector2 size,float depth,float radius){
            const int perCorner=8;const int count=4*(perCorner+1);var outline=new Vector2[count];
            int k=0;
            for(int corner=0;corner<4;corner++){
                float cx=(corner==0||corner==3?1:-1)*(size.x*.5f-radius);
                float cy=(corner<2?1:-1)*(size.y*.5f-radius);
                for(int i=0;i<=perCorner;i++){float a=(corner*90+i*90f/perCorner)*Mathf.Deg2Rad;outline[k++]=new Vector2(cx+Mathf.Cos(a)*radius,cy+Mathf.Sin(a)*radius);}
            }
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
            for(int face=0;face<2;face++){
                int start=vertices.Count;float z=(face==0?-1:1)*depth*.5f;var normal=face==0?Vector3.back:Vector3.forward;
                vertices.Add(new Vector3(0,0,z));normals.Add(normal);
                foreach(var p in outline){vertices.Add(new Vector3(p.x,p.y,z));normals.Add(normal);}
                for(int i=0;i<count;i++){int a=start+1+i,b=start+1+(i+1)%count;triangles.Add(start);triangles.Add(face==0?b:a);triangles.Add(face==0?a:b);}
            }
            for(int i=0;i<count;i++){
                Vector2 a=outline[i],b=outline[(i+1)%count];var edge=b-a;var normal=new Vector3(edge.y,-edge.x,0).normalized;int start=vertices.Count;
                vertices.Add(new Vector3(a.x,a.y,-depth*.5f));vertices.Add(new Vector3(a.x,a.y,depth*.5f));vertices.Add(new Vector3(b.x,b.y,depth*.5f));vertices.Add(new Vector3(b.x,b.y,-depth*.5f));
                for(int n=0;n<4;n++)normals.Add(normal);
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(vertices[i].x/size.x+.5f,vertices[i].y/size.y+.5f);
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.uv=uv;mesh.SetTriangles(triangles,0);return mesh;
        }
        static Mesh Torus(float radius,float tube){
            const int around=64,sides=10;var vertices=new Vector3[(around+1)*(sides+1)];var normals=new Vector3[vertices.Length];var triangles=new List<int>();
            for(int i=0;i<=around;i++)for(int j=0;j<=sides;j++){
                float u=i*2*Mathf.PI/around,v=j*2*Mathf.PI/sides;var normal=new Vector3(Mathf.Cos(u)*Mathf.Cos(v),Mathf.Sin(u)*Mathf.Cos(v),Mathf.Sin(v));
                int n=i*(sides+1)+j;normals[n]=normal;vertices[n]=new Vector3(Mathf.Cos(u)*radius,Mathf.Sin(u)*radius,0)+normal*tube;
                if(i<around&&j<sides){int b=n+sides+1;triangles.AddRange(new[]{n,b,n+1,n+1,b,b+1});}
            }
            var mesh=new Mesh{vertices=vertices,normals=normals,triangles=triangles.ToArray()};return mesh;
        }
        static Mesh BellMesh(){
            var profile=new[]{new Vector2(.177f,0),new Vector2(.182f,.025f),new Vector2(.147f,.055f),new Vector2(.118f,.1f),new Vector2(.105f,.17f),new Vector2(.075f,.218f),new Vector2(.026f,.242f),new Vector2(0,.244f)};
            const int sides=48;var vertices=new Vector3[profile.Length*(sides+1)];var triangles=new List<int>();
            for(int p=0;p<profile.Length;p++)for(int i=0;i<=sides;i++){
                float angle=i*2*Mathf.PI/sides;int n=p*(sides+1)+i;vertices[n]=new Vector3(Mathf.Cos(angle)*profile[p].x,profile[p].y,Mathf.Sin(angle)*profile[p].x);
                if(p<profile.Length-1&&i<sides){int next=n+sides+1;triangles.AddRange(new[]{n,next,n+1,n+1,next,next+1});}
            }
            var mesh=new Mesh{vertices=vertices,triangles=triangles.ToArray()};mesh.RecalculateNormals();return mesh;
        }
    }
}
