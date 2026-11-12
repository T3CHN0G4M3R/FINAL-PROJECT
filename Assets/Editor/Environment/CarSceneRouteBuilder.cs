using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CarSceneRouteBuilder
{
    public const string Folder = "Assets/Environment/CarScene";
    [Serializable] public sealed class RouteData { public Vector3[] points; public float length; public float maxSlope; public int crossings; }
    public static RouteData Data;
    private static readonly List<Vector3> Points = new List<Vector3>();
    private static readonly List<float> Distances = new List<float>();
    private static Material _road, _curb, _guard, _support;
    private static Transform _root;

    [MenuItem("Tools/Environment/Build CarScene Tangled Route")]
    public static void Build()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/CarScene.unity") throw new InvalidOperationException("Open CarScene first");
        GameObject old = null, floor = null;
        foreach (var g in scene.GetRootGameObjects()) { if (g.name == "TwistedRoad") old = g; if (g.name == "Floor") floor = g; }
        if (floor == null) throw new InvalidOperationException("Missing Floor");
        if (old != null) foreach (var c in old.GetComponentsInChildren<Component>(true))
            if (!(c is Transform || c is Renderer || c is MeshFilter || c is Collider || c is TextMesh)) throw new InvalidOperationException("Unexpected road component: " + c);
        Directory.CreateDirectory(Folder + "/Meshes"); Directory.CreateDirectory(Folder + "/Materials");
        AssetDatabase.Refresh();
        _road = MaterialCopy("Road Shade", "Weathered Asphalt", new Color(.34f,.35f,.32f));
        _curb = MaterialCopy("Sidewalk Shade", "Stained Concrete", new Color(.43f,.42f,.36f));
        _guard = MaterialCopy("Guard Shade", "Corroded Steel", new Color(.24f,.20f,.15f));
        _support = MaterialCopy("Support Shade", "Aged Supports", new Color(.27f,.27f,.23f));
        Points.Clear(); Distances.Clear();
        var b = floor.GetComponent<Renderer>().bounds;
        var coordinates = new Vector2[] { new Vector2(-520,-520),new Vector2(-300,-520),new Vector2(-160,-380),new Vector2(-420,-220),new Vector2(-500,-380),new Vector2(-400,-460),new Vector2(-200,-320),new Vector2(80,-460),new Vector2(420,-500),new Vector2(500,-280),new Vector2(260,-120),new Vector2(80,-280),new Vector2(280,-420),new Vector2(440,-280),new Vector2(300,-20),new Vector2(-180,-100),new Vector2(-460,60),new Vector2(-480,300),new Vector2(-260,440),new Vector2(-100,240),new Vector2(-300,100),new Vector2(-420,260),new Vector2(-200,380),new Vector2(80,100),new Vector2(320,240),new Vector2(440,60),new Vector2(520,300),new Vector2(360,440),new Vector2(160,280),new Vector2(0,420),new Vector2(240,500),new Vector2(520,520) };
        var controls = new Vector3[coordinates.Length];
        for(int i=0;i<controls.Length;i++) controls[i]=new Vector3(b.center.x+coordinates[i].x,b.max.y+1,b.center.z+coordinates[i].y);
        Add(controls[0]);
        for(int i=1;i<controls.Length-1;i++)
        {
            var incoming=(controls[i]-controls[i-1]).normalized; var outgoing=(controls[i+1]-controls[i]).normalized;
            float trim=Mathf.Min(45,Vector3.Distance(controls[i-1],controls[i])*.24f,Vector3.Distance(controls[i],controls[i+1])*.24f);
            var a=controls[i]-incoming*trim; var c=controls[i]+outgoing*trim;
            Line(Points[Points.Count-1],a);
            for(int j=1;j<=32;j++){float t=j/32f; Add((1-t)*(1-t)*a+2*(1-t)*t*controls[i]+t*t*c);}
        }
        Line(Points[Points.Count-1],controls[controls.Length-1]);
        Distances.Add(0); for(int i=1;i<Points.Count;i++) Distances.Add(Distances[i-1]+Vector3.Distance(Points[i-1],Points[i]));
        int crossings=0;
        // Raise later visits through intersections; the plateau protects the whole road width.
        for(int pass=0;pass<4;pass++)
        for(int j=1;j<Points.Count;j++)
        for(int i=0;i<j;i+=2)
        {
            if(Distances[j]-Distances[i]<130) continue;
            var delta=Points[j]-Points[i]; delta.y=0;
            if(delta.sqrMagnitude>225 || Points[j].y-Points[i].y>=7.5f) continue;
            float height=Points[i].y+9;
            float targetDistance=Distances[j];
            for(int k=0;k<Points.Count;k++)
            {
                float separation=Mathf.Abs(Distances[k]-targetDistance);
                if(separation>180) continue;
                float blend=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((separation-40)/140));
                var p=Points[k]; p.y=Mathf.Max(p.y,b.max.y+1+(height-b.max.y-1)*blend); Points[k]=p;
            }
            crossings++;
        }
        float slope=0,length=0;
        for(int i=1;i<Points.Count;i++){var d=Points[i]-Points[i-1]; length+=d.magnitude;slope=Mathf.Max(slope,Mathf.Atan2(Mathf.Abs(d.y),new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
        if(slope>25) throw new InvalidOperationException("Unsafe ramp slope " + slope);
        if(old!=null) Undo.DestroyObjectImmediate(old);
        _root=new GameObject("TwistedRoad").transform; Undo.RegisterCreatedObjectUndo(_root.gameObject,"Tangled route");
        for(int start=0,chunk=0;start<Points.Count-1;start+=64,chunk++)
        {
            int end=Mathf.Min(start+64,Points.Count-1);
            var section=new GameObject("Section "+chunk.ToString("000")+" - Tangled Road").transform;section.SetParent(_root,false);Undo.RegisterCreatedObjectUndo(section.gameObject,"Route section");
            Band(section,start,end,-4.4f,4.4f,-.7f,.1f,_road,"Asphalt");
            Band(section,start,end,-6,-4.4f,-.7f,.2f,_curb,"Left curb"); Band(section,start,end,4.4f,6,-.7f,.2f,_curb,"Right curb");
            Band(section,start,end,-5.65f,-5.45f,.2f,1.25f,_guard,"Left rail"); Band(section,start,end,5.45f,5.65f,.2f,1.25f,_guard,"Right rail");
        }
        for(int i=0;i<Points.Count;i+=22)
        {
            if(Points[i].y<3)continue;
            var right=Right(i);
            foreach(float side in new[]{-7f,7f})
            {
                var position=Points[i]+right*side;
                bool blocked=false;for(int k=0;k<Points.Count;k+=3){if(Mathf.Abs(Distances[k]-Distances[i])<80)continue;var d=Points[k]-position;d.y=0;if(d.sqrMagnitude<64&&Points[k].y<Points[i].y)blocked=true;}
                if(blocked)continue;
                Box("Bridge support",position-Vector3.up*(position.y*.5f+.35f),new Vector3(1.2f,position.y-.7f,1.2f),_support,_root,true);
            }
        }
        Mark("START",0);Mark("FINISH",Points.Count-1);
        Data=new RouteData {points=Points.ToArray(),length=length,maxSlope=slope,crossings=crossings};
        File.WriteAllText(Folder+"/RouteData.json",JsonUtility.ToJson(Data));AssetDatabase.ImportAsset(Folder+"/RouteData.json");
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject=_root.gameObject;
        Debug.Log("Tangled route length "+length+", max slope "+slope+", bridge adjustments "+crossings+", points "+Points.Count);
    }
    private static void Add(Vector3 p){if(Points.Count==0||Vector3.Distance(Points[Points.Count-1],p)>.001f)Points.Add(p);}
    private static void Line(Vector3 a,Vector3 b){int steps=Mathf.CeilToInt(Vector3.Distance(a,b)/2);for(int i=1;i<=steps;i++)Add(Vector3.Lerp(a,b,i/(float)steps));}
    private static Vector3 Right(int i){var d=Points[Mathf.Min(i+1,Points.Count-1)]-Points[Mathf.Max(0,i-1)];d.y=0;return Vector3.Cross(Vector3.up,d.normalized);}
    public static Material MaterialCopy(string source,string name,Color color)
    {
        string path=Folder+"/Materials/"+name+".mat"; var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;
        var m=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Imported/Simple Roads/Materials/"+source+".mat"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.06f);AssetDatabase.CreateAsset(m,path);return m;
    }
    private static void Band(Transform parent,int first,int last,float left,float right,float bottom,float top,Material material,string name)
    {
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();var origin=Points[first];
        for(int i=first;i<=last;i++){var p=Points[i]-origin;var r=Right(i);vertices.Add(p+r*left+Vector3.up*bottom);vertices.Add(p+r*left+Vector3.up*top);vertices.Add(p+r*right+Vector3.up*top);vertices.Add(p+r*right+Vector3.up*bottom);for(int k=0;k<4;k++)uvs.Add(new Vector2(k<2?0:1,Distances[i]/8));}
        for(int row=0;row<last-first;row++)for(int k=0;k<4;k++){int a=row*4+k,b=a+4,c=row*4+(k+1)%4+4,d=row*4+(k+1)%4;triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a);triangles.Add(c);triangles.Add(d);}
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/Meshes/"+parent.name.Split(' ')[1]+"_"+name.Replace(' ','_')+".asset";
        var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior!=null){EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);mesh=prior;}else AssetDatabase.CreateAsset(mesh,path);
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=origin;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;g.AddComponent<MeshCollider>().sharedMesh=mesh;Undo.RegisterCreatedObjectUndo(g,"Road mesh");
    }
    public static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent,bool collision)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=pos;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());Undo.RegisterCreatedObjectUndo(g,"Road dressing");return g;
    }
    [MenuItem("Tools/Environment/Dress CarScene Abandoned Route")]
    public static void Decorate()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject root=null;foreach(var g in scene.GetRootGameObjects())if(g.name=="TwistedRoad")root=g;
        if(root==null)throw new InvalidOperationException("Build route first");
        if(root.transform.Find("Abandoned Dressing")!=null)throw new InvalidOperationException("Dressing already exists");
        Data=JsonUtility.FromJson<RouteData>(File.ReadAllText(Folder+"/RouteData.json"));Points.Clear();Points.AddRange(Data.points);
        var group=new GameObject("Abandoned Dressing").transform;group.SetParent(root.transform,false);Undo.RegisterCreatedObjectUndo(group.gameObject,"Dressing");
        var road=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Weathered Asphalt.mat");
        var concrete=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Stained Concrete.mat");
        var rust=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Corroded Steel.mat");
        var random=new System.Random(4107);
        var texture=new Texture2D(256,256,TextureFormat.RGB24,false);var colors=new Color[256*256];
        for(int y=0;y<256;y++)for(int x=0;x<256;x++){float value=.65f+(float)random.NextDouble()*.3f;colors[y*256+x]=new Color(value,value,value*.97f);}
        for(int crack=0;crack<22;crack++){int x=random.Next(256),y=random.Next(256);for(int j=0;j<60;j++){x=Mathf.Clamp(x+random.Next(-3,4),0,255);y=(y+1)%256;colors[y*256+x]=new Color(.18f,.18f,.16f);}}
        texture.SetPixels(colors);texture.Apply();string image=Folder+"/Materials/AsphaltDecay.png";File.WriteAllBytes(image,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(image);
        road.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(image));EditorUtility.SetDirty(road);
        var treeAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imported/ALP_Assets/Poplar Tree FREE/Prefabs/PoplarTree001_pr.prefab");
        MeshFilter source=null;foreach(var f in treeAsset.GetComponentsInChildren<MeshFilter>(true))if(f.name.Contains("LOD0")){source=f;break;}
        Material[] treeMaterials=null;
        if(source!=null){var src=source.GetComponent<Renderer>().sharedMaterials;treeMaterials=new Material[src.Length];for(int k=0;k<src.Length;k++){string path=Folder+"/Materials/Neglected Poplar "+k+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(road);m.SetTexture("_BaseMap",src[k].mainTexture);m.SetColor("_BaseColor",k==0?new Color(.36f,.32f,.24f):new Color(.40f,.44f,.24f));m.SetFloat("_AlphaClip",1);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cutoff",.4f);m.SetFloat("_Cull",0);AssetDatabase.CreateAsset(m,path);}treeMaterials[k]=m;}}
        var lampAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imported/ModularHousePack1/Prefabs/Modules/Roads/Streetlight.prefab");
        for(int i=20;i<Points.Count-20;i+=40)
        {
            var p=Points[i];var right=Right(i);float side=i%80==20?1:-1;
            // Keep trees and rubble away from other stretches and elevated walking lanes.
            var shoulder=p+right*(side*9);bool obstructs=false;
            for(int k=0;k<Points.Count;k+=4){if(Mathf.Abs(k-i)<50)continue;var d=Points[k]-shoulder;d.y=0;if(d.sqrMagnitude<100&&Mathf.Abs(Points[k].y-p.y)<3)obstructs=true;}
            if(obstructs)continue;
            for(int j=0;j<3;j++){var rock=GameObject.CreatePrimitive(PrimitiveType.Sphere);rock.name="Shoulder rubble";rock.transform.SetParent(group);rock.transform.position=p+right*(side*(4.9f+j*.3f))+Vector3.up*.3f;rock.transform.localScale=new Vector3(.3f+(float)random.NextDouble()*.4f,.18f,.4f);rock.GetComponent<Renderer>().sharedMaterial=concrete;Undo.RegisterCreatedObjectUndo(rock,"Rubble");}
            if(i%120==20&&source!=null&&p.y<3){var tree=new GameObject("Neglected roadside poplar");tree.transform.SetParent(group);tree.transform.position=new Vector3(shoulder.x,0,shoulder.z);tree.transform.localScale=Vector3.one*(.65f+(float)random.NextDouble()*.4f);tree.transform.rotation=Quaternion.Euler(0,random.Next(360),0);tree.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;tree.AddComponent<MeshRenderer>().sharedMaterials=treeMaterials;Undo.RegisterCreatedObjectUndo(tree,"Poplar");}
            if(i%120==60){var lamp=(GameObject)PrefabUtility.InstantiatePrefab(lampAsset);lamp.name="Unpowered leaning streetlight";lamp.transform.SetParent(group);lamp.transform.position=p+right*side*6.7f;lamp.transform.rotation=Quaternion.LookRotation(-right*side)*Quaternion.Euler(0,0,side*7);foreach(var m in lamp.GetComponentsInChildren<Renderer>())m.sharedMaterial=rust;foreach(var light in lamp.GetComponentsInChildren<Light>())light.enabled=false;Undo.RegisterCreatedObjectUndo(lamp,"Streetlight");}
            if(i%120==100){var weed=Box("Dry roadside weed patch",p+right*side*5.1f+Vector3.up*.28f,new Vector3(.7f,.1f,1.7f),treeMaterials!=null?treeMaterials[treeMaterials.Length-1]:concrete,group,false);weed.transform.rotation=Quaternion.LookRotation(Vector3.Cross(right,Vector3.up));}
        }
        for(int i=0;i<Points.Count;i+=220)
        {
            var right=Right(i);var p=Points[i]+right*6.7f;
            Box("Rusted signpost",p+Vector3.up*1.3f,new Vector3(.12f,2.6f,.12f),rust,group,true);
            var board=Box("Faded pedestrian route sign",p+Vector3.up*2.4f,new Vector3(2,.8f,.1f),concrete,group,true);board.transform.rotation=Quaternion.LookRotation(Vector3.Cross(right,Vector3.up))*Quaternion.Euler(0,0,-6);
            var caption=new GameObject("Route caption");caption.transform.SetParent(board.transform,false);caption.transform.localPosition=new Vector3(0,0,-.6f);caption.transform.localRotation=Quaternion.Euler(0,180,0);caption.transform.localScale=new Vector3(.5f,1.25f,10);var text=caption.AddComponent<TextMesh>();text.text=i==0?"ABANDONED ROAD\nON FOOT ONLY":"PEDESTRIAN ROUTE  >";text.fontSize=42;text.characterSize=.055f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.15f,.16f,.12f);Undo.RegisterCreatedObjectUndo(caption,"Sign text");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
    }
    private static void Mark(string name,int i){var g=new GameObject(name);g.transform.SetParent(_root);g.transform.position=Points[i]+Vector3.up*.1f;g.transform.rotation=Quaternion.LookRotation(Vector3.Cross(Right(i),Vector3.up));Undo.RegisterCreatedObjectUndo(g,"Route marker");}
}
