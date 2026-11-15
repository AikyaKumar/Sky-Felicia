using System;
using OrbitalFrontier.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalFrontier.Runtime
{
    sealed class ExpeditionStage : IDisposable
    {
        readonly ExpeditionCatalog catalog;
        readonly SurfaceGrid[] grids;
        readonly Camera camera;
        readonly Material hull, plate, dark, stripe, cyan, violet, red, foil, solar, atmosphere, terrainTint;
        readonly GameObject root, cabin, deckRoof, planet, yard, field, crewInside, crewOutside, rover;
        readonly Transform lander;
        readonly GameObject[] crewIn = new GameObject[4], crewOut = new GameObject[4];
        readonly Mesh fieldMesh;
        readonly Material moon;
        readonly Texture2D[] albedo, texture;
        readonly LineRenderer orbit;
        readonly MeshFilter planetMesh;
        readonly Renderer statusLamp;
        GameObject model, landModel;
        string lastDesign;
        int lastWorld = -1;
        VectorD[] trajectory;
        float orbitYaw = 40, orbitPitch = 20, zoom = 8.7f;
        public float HeadYaw, HeadPitch = -1;
        public Camera Camera => camera;

        public ExpeditionStage(ExpeditionCatalog catalog, SurfaceGrid[] grids, Transform parent)
        {
            this.catalog = catalog; this.grids = grids;
            root = new GameObject("Expedition visual environments");
            root.transform.SetParent(parent, false);
            var cam = new GameObject("Crew optical system");
            cam.transform.SetParent(root.transform, false);
            camera = cam.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f,.040f,.073f);
            camera.nearClipPlane = .06f;
            camera.farClipPlane = 9000;
            camera.allowMSAA = true;
            hull = Lit(new Color(.81f,.81f,.82f));
            plate = Lit(new Color(.32f,.37f,.46f));
            dark = Lit(new Color(.10f,.12f,.18f));
            stripe = Lit(new Color(.76f,.46f,.30f));
            foil = Lit(new Color(.74f,.47f,.23f));
            solar = Lit(new Color(.12f,.19f,.54f));
            cyan = Flat(new Color(.53f,.88f,.84f));
            violet = Flat(new Color(.71f,.59f,.93f));
            red = Flat(new Color(.92f,.39f,.26f));
            atmosphere = Flat(new Color(.10f,.11f,.18f));
            terrainTint = Lit(Color.white);
            moon = new Material(Shader.Find("Orbital Frontier/Lunar Surface"));
            moon.SetVector("_SunDir",new Vector4(-.45f,.60f,-.67f));
            planet = New(root.transform,"Destination globe");
            planetMesh = planet.AddComponent<MeshFilter>();
            planetMesh.sharedMesh = PlanetGeometry();
            planet.AddComponent<MeshRenderer>().sharedMaterial = moon;
            orbit = Line(planet.transform,"Orbit projection",violet,.007f);
            orbit.useWorldSpace = false;
            albedo = new [] { Resources.Load<Texture2D>("Frontier/lunar_albedo"), Resources.Load<Texture2D>("Frontier/mars_albedo"),
                 Resources.Load<Texture2D>("Frontier/earth_albedo") };
            texture = Array.ConvertAll(catalog.worlds,w=>Resources.Load<Texture2D>("Frontier/"+w.texture));

            yard = New(root.transform,"Shipyard");
            for(int i=-8;i<=8;i++)
            {
                float p=i*.59f;
                DrawLine(yard.transform,Vector(p,-1.85f,-4.72f),Vector(p,-1.85f,4.72f),atmosphere,.012f);
                DrawLine(yard.transform,Vector(-4.72f,-1.85f,p),Vector(4.72f,-1.85f,p),atmosphere,.012f);
            }
            field = New(root.transform,"Measured landing region");
            var surface=New(field.transform,"Terrain");
            fieldMesh = new Mesh {name="Native terrain grid",indexFormat=IndexFormat.UInt32};
            surface.AddComponent<MeshFilter>().sharedMesh=fieldMesh;
            surface.AddComponent<MeshRenderer>().sharedMaterial=terrainTint;
            lander = New(field.transform,"Lander").transform;
            rover = New(field.transform,"Electric EVA rover");
            DrawRover(rover.transform);
            for(int i=0;i<2;i++)
            {
                var mark = New(field.transform,"Field beacon "+i);
                Stick(mark.transform,Vector(0,0,0),Vector(0,5.5f,0),.13f,violet);
                Block(mark.transform,PrimitiveType.Sphere,Vector(0,5.4f,0),Vector(.85f,.85f,.85f),violet);
                var ring = new Vector3[41];
                for(int j=0;j<ring.Length;j++)ring[j]=Vector(Mathf.Cos(j*2*Mathf.PI/40)*5,.32f,Mathf.Sin(j*2*Mathf.PI/40)*5);
                var plot=Line(mark.transform,"Survey area",violet,.22f);
                plot.positionCount=ring.Length;plot.SetPositions(ring);
            }
            cabin = New(root.transform,"Pressurized ship interior");
            deckRoof = New(cabin.transform,"Deck roof");
            statusLamp = BuildCabin(cabin.transform,deckRoof.transform);
            crewInside = New(cabin.transform,"Crew avatars");
            crewOutside = New(field.transform,"EVA crew avatars");
            for (int i = 0; i < 4; i++)
            {
                crewIn[i] = BuildCrew(crewInside.transform,i);
                crewOut[i] = BuildCrew(crewOutside.transform,i);
            }
            LightFor(root.transform,"Forward lights",Vector(3,-4,5),2.1f,new Color(1,.911f,.805f));
            LightFor(root.transform,"Cool fill",Vector(-4,-2,-5),1.19f,new Color(.71f,.75f,1));
            for(int i=0;i<5;i++)
            {
                var lamp=New(cabin.transform,"Deck luminaire "+i);
                lamp.transform.localPosition=Vector(0,2.27f,-7.35f+i*3.05f);
                var light=lamp.AddComponent<Light>();
                light.type=LightType.Point;
                light.range=7;
                light.intensity=3;
                light.color=i==4?new Color(1,.74f,.48f):new Color(.75f,.83f,1);
            }
            SetActive(planet);
        }

        static Vector3 Vector(float x,float y,float z)=>new Vector3(x,y,z);
        static GameObject New(Transform parent,string name)
        {
            var o=new GameObject(name);
            o.transform.SetParent(parent,false);
            return o;
        }
        Material Lit(Color c)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")) { color=c };
            m.SetFloat("_Smoothness",.29f);
            return m;
        }
        Material Flat(Color c)=>new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color=c };
        GameObject Block(Transform root,PrimitiveType shape,Vector3 p,Vector3 scale,Material material)
        {
            var g=GameObject.CreatePrimitive(shape);
            g.name=shape.ToString();
            g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=scale;
            g.GetComponent<Renderer>().sharedMaterial=material;
            var collider=g.GetComponent<Collider>();
            if(collider) UnityEngine.Object.Destroy(collider);
            return g;
        }
        GameObject Box(Transform root,float x,float y,float z,float w,float h,float l,Material m) =>
            Block(root,PrimitiveType.Cube,Vector(x,y,z),Vector(w,h,l),m);
        void Stick(Transform root,Vector3 a,Vector3 b,float r,Material m)
        {
            var tube=Block(root,PrimitiveType.Cylinder,(a+b)*.5f,Vector(r*2,(b-a).magnitude*.5f,r*2),m);
            tube.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        LineRenderer Line(Transform root,string name,Material m,float width)
        {
            var line=New(root,name).AddComponent<LineRenderer>();
            line.sharedMaterial=m;line.useWorldSpace=false;line.widthMultiplier=width;
            line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
            return line;
        }
        void DrawLine(Transform root,Vector3 a,Vector3 b,Material m,float thickness)
        {
            var line=Line(root,"Grid",m,thickness);
            line.positionCount=2;line.SetPositions(new [] { a,b });
        }
        void LightFor(Transform root,string name,Vector3 direction,float intensity,Color color)
        {
            var o=New(root,name);o.transform.rotation=Quaternion.LookRotation(direction);
            var light=o.AddComponent<Light>();
            light.type=LightType.Directional;light.intensity=intensity;light.color=color;
        }

        Renderer BuildCabin(Transform r,Transform roof)
        {
            Box(r,0,-.15f,-.75f,4.92f,.30f,16.8f,dark);
            for(int i=0;i<27;i++)
            {
                float z=-8.75f+i*.63f;
                Box(r,0,.015f,z,4.29f,.025f,.025f,plate);
                Box(r,-1.18f,.006f,z,.10f,.022f,.23f,stripe);
                Box(r,1.18f,.006f,z,.10f,.022f,.23f,stripe);
            }
            Box(roof,0,2.94f,-.75f,4.92f,.28f,16.8f,dark);
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<8;i++)
                {
                    float z=-7.91f+i*2.10f;
                    Box(roof,side*2.44f,1.36f,z,.29f,2.80f,2.04f,plate);
                    Box(roof,side*2.28f,.30f,z,.15f,.48f,2.04f,dark);
                    Box(roof,side*2.28f,2.24f,z,.105f,.05f,1.94f,stripe);
                }
                foreach (float z in new[]{-4.37f,4.33f})
                {
                    Box(r,side*1.92f,1.31f,z,1.15f,2.62f,.36f,plate);
                    Box(r,side*1.31f,1.31f,z,.073f,2.63f,.39f,stripe);
                }
                Stick(roof,Vector(side*1.79f,2.52f,-8.8f),Vector(side*1.79f,2.52f,7.1f),.034f,foil);
            }
            foreach(float z in new[]{-7.34f,-4.37f,.4f,4.32f,6.60f})
            {
                Box(roof,0,2.64f,z,4.65f,.30f,.28f,hull);
                Box(roof,0,2.48f,z,.63f,.047f,.29f,cyan);
            }
            Box(r,0,.45f,-9,4.87f,.90f,.23f,plate);
            Box(roof,0,2.53f,-9,4.87f,.48f,.23f,plate);
            foreach(float x in new[]{-2.28f,0,2.28f})Box(roof,x,1.63f,-8.97f,.22f,1.50f,.30f,hull);
            Box(r,0,1.25f,7.33f,4.87f,2.51f,.26f,plate);
            Box(r,0,1.28f,7.18f,1.51f,2.43f,.13f,dark);
            Box(r,.76f,1.28f,7.10f,.055f,2.41f,.068f,stripe);
            Box(r,-.76f,1.28f,7.10f,.055f,2.41f,.068f,stripe);

            for(int i=0;i<6;i++)
            {
                var station=New(r,CrewStation.Name[i]);
                station.transform.localPosition=Vector((float)CrewStation.X[i],0,(float)CrewStation.Z[i]);
                station.transform.localRotation=Quaternion.Euler(0,i<2?0:i%2==0?90:-90,0);
                var desk=station.transform;
                Box(desk,0,.70f,.04f,1.62f,1.39f,.59f,dark);
                Box(desk,0,1.38f,.11f,1.73f,.095f,.65f,hull);
                Box(desk,0,1.71f,.036f,1.55f,.61f,.17f,dark);
                Box(desk,0,1.69f,.127f,1.28f,.44f,.016f,i==2?stripe:plate);
                for(int a=0;a<5;a++)
                {
                    Box(desk,-.49f+a*.24f,1.445f,.31f,.13f,.038f,.070f,a%2==0?cyan:foil);
                    Box(desk,-.49f+a*.24f,1.68f+(.08f*(a%3)),.148f,.15f,.025f,.023f,cyan);
                }
                Label(desk,CrewStation.Name[i].ToUpperInvariant(),Vector(0,2.21f,.18f),0,i%2==0?new Color(.81f,.95f,.89f):new Color(.86f,.76f,1));
            }
            Label(r,"EVA AIRLOCK",Vector(0,2.41f,7.06f),180,new Color(.98f,.73f,.46f));
            return Box(r,-1.74f,1.94f,-2.81f,.23f,.09f,.032f,cyan).GetComponent<Renderer>();
        }

        void Label(Transform r,string text,Vector3 p,float yaw,Color color)
        {
            var item=New(r,text);
            item.transform.localPosition=p;
            item.transform.localRotation=Quaternion.Euler(0,yaw+180,0);
            var label=item.AddComponent<TextMesh>();label.text=text;label.fontSize=42;label.characterSize=.047f;
            label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.color=color;
            var f=Resources.Load<Font>("Frontier/IBMPlexMono-Regular");
            if(f){label.font=f;item.GetComponent<MeshRenderer>().sharedMaterial=f.material;}
        }

        GameObject BuildCrew(Transform r,int i)
        {
            var g=New(r,"Crew "+(i+1));var a=g.transform;
            var suit=i%2==0?hull:stripe;
            Box(a,0,1.05f,0,.42f,.62f,.30f,suit);
            Block(a,PrimitiveType.Sphere,Vector(0,1.57f,0),Vector(.40f,.40f,.40f),suit);
            Block(a,PrimitiveType.Sphere,Vector(0,1.59f,-.10f),Vector(.35f,.30f,.24f),dark);
            Box(a,0,1.13f,.18f,.36f,.46f,.12f,plate);
            for(int j=-1;j<=1;j+=2)
            {
                Stick(a,Vector(j*.14f,.8f,0),Vector(j*.16f,.19f,-.02f),.095f,suit);
                Box(a,j*.16f,.10f,-.05f,.18f,.16f,.27f,dark);
                Stick(a,Vector(j*.23f,1.29f,0),Vector(j*.33f,.78f,-.02f),.074f,suit);
            }
            return g;
        }

        void DrawRover(Transform r)
        {
            Box(r,0,.59f,0,1.51f,.23f,2.2f,hull);
            Box(r,0,.80f,.19f,.88f,.42f,.70f,dark);
            foreach(float x in new[]{-.83f,.83f})
                foreach(float z in new[]{-.72f,.72f})
                    Stick(r,Vector(x-.19f,.32f,z),Vector(x+.19f,.32f,z),.34f,dark);
        }

        Mesh PlanetGeometry()
        {
            const int w=96,h=64;
            var verts=new Vector3[(w+1)*(h+1)];
            var uv=new Vector2[verts.Length];
            var indices=new int[w*h*6];
            for(int y=0;y<=h;y++)
                for(int x=0;x<=w;x++)
                {
                    int i=y*(w+1)+x;
                    float u=(float)x/w,v=(float)y/h;
                    var p=Orbit.Direction(180*v-90,360*u-180);
                    verts[i]=Vector((float)p.x,(float)p.y,-(float)p.z);
                    uv[i]=new Vector2(u,v);
                    if(x==w||y==h)continue;
                    int t=(y*w+x)*6,a=i+w+1;
                    indices[t]=i;indices[t+1]=i+1;indices[t+2]=a;
                    indices[t+3]=i+1;indices[t+4]=a+1;indices[t+5]=a;
                }
            var mesh=new Mesh{name="Geographic globe"};
            mesh.vertices=verts;mesh.normals=verts;mesh.uv=uv;mesh.triangles=indices;
            return mesh;
        }

        void ChangeWorld(int index)
        {
            if(lastWorld==index)return;
            lastWorld=index;
            var g=grids[index];
            int step=index==0?2:1;
            int n=(g.Side-1)/step+1;
            var points=new Vector3[n*n];
            var uv=new Vector2[points.Length];
            var indices=new int[(n-1)*(n-1)*6];
            for(int z=0;z<n;z++)
                for(int x=0;x<n;x++)
                {
                    int i=z*n+x;
                    float u=x/(float)(n-1),v=z/(float)(n-1);
                    double a=(2*u-1)*g.Extent,b=(2*v-1)*g.Extent;
                    points[i]=Vector((float)a,(float)g.Height(a,b),-(float)b);
                    uv[i]=new Vector2(u,1-v);
                    if(z==n-1||x==n-1)continue;
                    int t=(z*(n-1)+x)*6;
                    indices[t]=i;indices[t+1]=i+1;indices[t+2]=i+n;
                    indices[t+3]=i+1;indices[t+4]=i+n+1;indices[t+5]=i+n;
                }
            fieldMesh.Clear();fieldMesh.vertices=points;fieldMesh.uv=uv;fieldMesh.triangles=indices;fieldMesh.RecalculateNormals();
            terrainTint.SetTexture("_BaseMap",texture[index]);
            var w=catalog.worlds[index];
            for(int i=0;i<w.points.Length;i++)
            {
                var p=w.points[i];
                field.transform.Find("Field beacon "+i).localPosition=Vector((float)p.x,(float)g.Height(p.x,p.z),-(float)p.z);
            }
        }

        void UpdateShip(ExpeditionDesign design,int current)
        {
            string key=string.Join(",",design.parts)+string.Join(",",design.rotations)+":"+design.vehicle+":"+current;
            if(lastDesign==key)return;
            lastDesign=key;
            if(model)UnityEngine.Object.Destroy(model);
            if(landModel)UnityEngine.Object.Destroy(landModel);
            model=Ship(yard.transform,design,current);
            landModel=Ship(lander,design,-1);
            landModel.transform.localScale=Vector3.one*3.70f;
        }

        GameObject Ship(Transform parent,ExpeditionDesign d,int selected)
        {
            var ship=New(parent,catalog.vehicles[d.vehicle].name);
            for(int i=0;i<d.parts.Length;i++)
            {
                var m=New(ship.transform,ExpeditionSlots.Names[i]).transform;
                if(string.IsNullOrEmpty(d.parts[i]))continue;
                m.localRotation=Quaternion.Euler(0,d.rotations[i]*90,0);
                Material P(Material normal)=>selected==i?violet:normal;
                string id=d.parts[i];
                switch(i)
                {
                    case 0:
                        float radius=d.vehicle==2?2.35f:d.vehicle==1?2.1f:1.88f;
                        Block(m,PrimitiveType.Cylinder,Vector(0,.75f,0),Vector(radius,.69f,radius),P(hull));
                        Block(m,PrimitiveType.Cylinder,Vector(0,1.54f,0),Vector(1.12f,.14f,1.12f),P(plate));
                        for(int v=0;v<4;v++)
                        {
                            var window=New(m,"Viewport");window.transform.localRotation=Quaternion.Euler(0,v*90+23,0);
                            Box(window.transform,0,1.11f,radius*.50f,.51f,.26f,.10f,P(dark));
                        }
                        break;
                    case 1:
                        float width=id=="compact"?1.42f:id=="twin"?2.06f:1.61f;
                        Block(m,PrimitiveType.Cylinder,Vector(0,-.36f,0),Vector(width,.48f,width),P(foil));
                        for(int band=0;band<4;band++)
                            Block(m,PrimitiveType.Cylinder,Vector(0,-.82f+band*.29f,0),Vector(width+.09f,.034f,width+.09f),P(plate));
                        break;
                    case 2:
                        foreach(float x in id=="atlas"?new[]{-.58f,0,.58f}:new[]{0f})
                        {
                            float size=id=="atlas"?.60f:id=="horizon"?.77f:.92f;
                            Block(m,PrimitiveType.Cylinder,Vector(x,-1.14f,0),Vector(size,.26f,size),P(dark));
                            Block(m,PrimitiveType.Cylinder,Vector(x,-1.45f,0),Vector(size*1.15f,.06f,size*1.15f),P(plate));
                        }
                        break;
                    case 3:
                        if(id=="isotope")
                        {
                            for(int c=0;c<6;c++) Box(m,1.30f,-.60f+c*.13f,0,.55f,.048f,.69f,P(plate));
                        }
                        else for(int side=-1;side<=1;side+=2)
                        {
                            float l=id=="wide"?2.24f:1.61f;
                            Box(m,side*1.15f,.12f,0,.72f,.14f,.16f,P(hull));
                            Box(m,side*(1.36f+l*.5f),.12f,0,l,.06f,1.02f,P(plate));
                            for(int a=0;a<7;a++)
                                for(int b=0;b<3;b++)
                                    Box(m,side*(1.36f+(a+.5f)*l/7),.166f,(b-1)*.325f,l/7-.015f,.027f,.30f,P(solar));
                        }
                        break;
                    case 6:
                        Stick(m,Vector(.66f,.70f,.31f),Vector(1.22f,1.42f,.31f),.043f,P(hull));
                        var disk=Block(m,PrimitiveType.Sphere,Vector(1.3f,1.42f,.30f),Vector(.73f,.12f,.64f),P(plate));
                        disk.transform.localRotation=Quaternion.Euler(0,0,-37);
                        break;
                    case 7:
                        for(int a=0;a<4;a++)
                        {
                            float angle=(a+.5f)*Mathf.PI/2;
                            float r=id=="shocklegs"?1.77f:1.47f;
                            var tip=Vector(Mathf.Cos(angle)*r,-1.52f,Mathf.Sin(angle)*r);
                            Stick(m,Vector(Mathf.Cos(angle)*.83f,-.66f,Mathf.Sin(angle)*.83f),tip,.064f,P(plate));
                            Block(m,PrimitiveType.Cylinder,tip,Vector(.37f,.055f,.37f),P(hull));
                        }
                        break;
                    case 9:
                        Block(m,PrimitiveType.Cylinder,Vector(0,-.87f,0),Vector(id=="shield-plus"?2.6f:2.3f,.09f,2.3f),P(dark));
                        break;
                    default:
                        float t=i*1.38f;
                        Box(m,Mathf.Sin(t)*.91f,(i%3)*.43f-.62f,Mathf.Cos(t)*.94f,.41f,.31f,.29f,P(i==5||i==10?cyan:i==12?stripe:hull));
                        break;
                }
            }
            return ship;
        }

        void SetActive(GameObject mode)
        {
            planet.SetActive(mode==planet);yard.SetActive(mode==yard);cabin.SetActive(mode==cabin);field.SetActive(mode==field);
        }

        public void Update(ExpeditionView s,int mode,int seat,int selected,Rect viewport)
        {
            camera.rect=viewport;
            ChangeWorld(s.design.world);
            UpdateShip(s.design,selected);
            int phase=s.phase;
            var crew=s.crew[seat];
            var t=s.telemetry;
            bool planning=phase==0, inside=mode==2&&!crew.outside&&!planning;
            bool outdoor=!planning&&!inside&&(phase==4||phase==5);
            bool deck=planning&&mode==5;
            SetActive(planning&&mode==4?yard:inside||deck?cabin:outdoor?field:planet);
            deckRoof.SetActive(!deck);
            crewInside.SetActive(!deck);
            camera.backgroundColor=outdoor&&s.design.world==1?new Color(.56f,.37f,.33f):new Color(.036f,.040f,.075f);
            if(planning)
            {
                camera.fieldOfView=39;
                if(mode==4) Aim(Vector3.zero,zoom,orbitYaw,orbitPitch);
                else if(deck) Aim(Vector(0,0,.85f),23,orbitYaw*.25f,59);
                else
                {
                    planet.transform.localPosition=Vector(.76f,.02f,0);
                    planet.transform.localRotation=Quaternion.Euler(-10,45+Time.unscaledTime*.25f,-6);
                    planet.transform.localScale=Vector3.one*1.42f;
                    moon.SetTexture("_BaseMap",albedo[s.design.world]);orbit.enabled=false;
                    camera.transform.SetPositionAndRotation(Vector(0,0,-4.5f),Quaternion.identity);
                }
                return;
            }
            if(inside)
            {
                camera.fieldOfView=67;
                var p=Vector((float)crew.x,1.64f,-(float)crew.z);
                camera.transform.SetPositionAndRotation(p,Quaternion.Euler(-HeadPitch,HeadYaw,0));
                for(int i=0;i<s.crew.Length;i++)
                {
                    var other=s.crew[i];
                    var avatar=crewIn[i];avatar.SetActive(i!=seat&&other.active&&!other.outside);
                    avatar.transform.localPosition=Vector((float)other.x,0,-(float)other.z);
                    avatar.transform.localRotation=Quaternion.Euler(0,(float)other.yaw,0);
                }
                statusLamp.sharedMaterial=t.fault==1?red:cyan;
            }
            else if(outdoor)
            {
                var g=grids[s.design.world];var x=(float)t.x;var z=-(float)t.z;
                float y=phase==4?(float)t.altitude:(float)g.Height(t.x,t.z);
                lander.localPosition=Vector(x,y+5.75f,z);
                lander.localRotation=Quaternion.Euler((float)t.vz*.08f,0,(float)t.vx*.08f);
                rover.SetActive(phase==5&&!string.IsNullOrEmpty(s.design.parts[14]));
                double rx=t.roverX,rz=t.roverZ;
                rover.transform.localPosition=Vector((float)rx,(float)g.Height(rx,rz),-(float)rz);
                rover.transform.localRotation=Quaternion.Euler(0,(float)t.roverYaw,0);
                for(int i=0;i<s.crew.Length;i++)
                {
                    var other=s.crew[i];
                    var avatar=crewOut[i];avatar.SetActive((i!=seat||mode!=2)&&other.active&&other.outside);
                    avatar.transform.localPosition=Vector((float)other.x,(float)g.Height(other.x,other.z),-(float)other.z);
                    avatar.transform.localRotation=Quaternion.Euler(0,(float)other.yaw,0);
                }
                if(mode==2&&crew.outside)
                {
                    camera.fieldOfView=67;
                    camera.transform.SetPositionAndRotation(Vector((float)crew.x,(float)g.Height(crew.x,crew.z)+(crew.rover?1.2f:1.78f),-(float)crew.z),
                        Quaternion.Euler(-HeadPitch,HeadYaw,0));
                }
                else
                {
                    camera.fieldOfView=49;
                    float d=phase==4?Mathf.Clamp(32+y*.21f,32,125):43;
                    Aim(Vector(x,y+(phase==4?-3:2),z),d,orbitYaw,orbitPitch+4);
                }
            }
            else
            {
                camera.fieldOfView=43;
                planet.transform.localPosition=Vector3.zero;
                planet.transform.localRotation=Quaternion.identity;
                planet.transform.localScale=Vector3.one;
                moon.SetTexture("_BaseMap",albedo[phase==1?2:s.design.world]);
                orbit.enabled=phase==3;
                if(phase==3&&t.track!=null&&t.track!=trajectory)
                {
                    trajectory=t.track;
                    orbit.positionCount=trajectory.Length;
                    var r=(float)catalog.worlds[s.design.world].radius;
                    for(int i=0;i<trajectory.Length;i++)
                        orbit.SetPosition(i,Vector((float)trajectory[i].x/r,(float)trajectory[i].y/r,-(float)trajectory[i].z/r));
                }
                Aim(Vector3.zero,3.7f,orbitYaw,orbitPitch+12);
            }
        }

        void Aim(Vector3 center,float radius,float yaw,float pitch)
        {
            var position=center+Quaternion.Euler(pitch,yaw,0)*Vector3.back*radius;
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(center-position,Vector3.up));
        }

        public int PickMount(Vector3 screenPoint)
        {
            if (!model || !yard.activeSelf || !camera.pixelRect.Contains(screenPoint)) return -1;
            var ray = camera.ScreenPointToRay(screenPoint);
            float nearest = float.PositiveInfinity;
            int result=-1;
            for (int mount = 0; mount < model.transform.childCount; mount++)
            {
                foreach (var item in model.transform.GetChild(mount).GetComponentsInChildren<Renderer>())
                {
                    if (item.bounds.IntersectRay(ray, out float distance) && distance < nearest)
                    {
                        nearest = distance;
                        result = mount;
                    }
                }
            }
            return result;
        }

        public void Rotate(float dx,float dy){orbitYaw-=dx*.55f;orbitPitch=Mathf.Clamp(orbitPitch+dy*.49f,-38,63);}
        public void Zoom(float d){zoom=Mathf.Clamp(zoom+d*.45f,4,20);}
        public void Dispose()
        {
            foreach(var m in new[]{hull,plate,dark,stripe,cyan,violet,red,foil,solar,atmosphere,moon,terrainTint})
                if(m)UnityEngine.Object.Destroy(m);
            UnityEngine.Object.Destroy(fieldMesh);
            if(planetMesh && planetMesh.sharedMesh)UnityEngine.Object.Destroy(planetMesh.sharedMesh);
            if(root)UnityEngine.Object.Destroy(root);
        }
    }
}
