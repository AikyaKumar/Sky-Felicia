using OrbitalFrontier.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrbitalFrontier.Runtime
{
    public sealed class FlightStage : MonoBehaviour
    {
        Camera camera3d;
        Material lunar, lineLive, linePlan, marker, markerDone, spark, grid, terrainMaterial;
        Mesh terrainMesh;
        int terrainSite = -1, terrainLevel;
        GameObject moon, globeRoot, hangarRoot, shipMarker, starRoot, keyRoot, terrainRoot;
        SpacecraftVisual craft, chaseCraft;
        LineRenderer live, plan;
        readonly GameObject[] sites = new GameObject[3];
        float yaw = 64, pitch = 33, distance = 4.1f;
        int display = -1;
        int mode;
        GameCatalog catalog;

        public Camera Camera => camera3d;

        public void Make(GameCatalog data, Transform spacecraftTemplate)
        {
            catalog = data;
            var cameraObject = new GameObject("Flight cameras");
            cameraObject.transform.SetParent(transform, false);
            camera3d = cameraObject.AddComponent<Camera>();
            camera3d.clearFlags = CameraClearFlags.SolidColor;
            camera3d.backgroundColor = new Color(0.03f, 0.035f, 0.063f);
            camera3d.fieldOfView = 39;
            camera3d.nearClipPlane = 0.002f;
            camera3d.farClipPlane = 180;
            camera3d.allowHDR = true;
            camera3d.allowMSAA = true;

            keyRoot = new GameObject("Hangar lighting");
            keyRoot.transform.SetParent(transform, false);
            Lamp("Key", 1.7f, new Color(1f, .92f, .83f), new Vector3(4, -3, 5));
            Lamp("Fill", 1.22f, new Color(0.66f, .70f, 1f), new Vector3(-4, -2, -6));
            Lamp("Rim", 0.8f, new Color(0.72f, .66f, .96f), new Vector3(6, 2, -2));

            lineLive = Flat(new Color(.56f, .45f, .96f));
            linePlan = Flat(new Color(.36f, .89f, .68f));
            marker = Flat(new Color(1f, .59f, .30f));
            markerDone = Flat(new Color(.38f, .94f, .77f));
            spark = Flat(new Color(.69f, .78f, .92f));
            grid = Flat(new Color(0.12f, 0.13f, 0.24f));
            starRoot = new GameObject("Fixed star field");
            starRoot.transform.SetParent(transform, false);
            BuildStars();
            globeRoot = new GameObject("Lunar reference frame");
            globeRoot.transform.SetParent(transform, false);
            moon = new GameObject("LRO global lunar albedo");
            moon.transform.SetParent(globeRoot.transform, false);
            lunar = new Material(Shader.Find("Orbital Frontier/Lunar Surface"));
            lunar.SetTexture("_BaseMap", Resources.Load<Texture2D>("Frontier/lunar_albedo"));
            var mesh = MoonMesh();
            moon.AddComponent<MeshFilter>().sharedMesh = mesh;
            moon.AddComponent<MeshRenderer>().sharedMaterial = lunar;
            moon.AddComponent<RuntimeMesh>().ownedMesh = mesh;
            live = NewLine(globeRoot.transform, "Osculating orbit", lineLive, 0.006f);
            plan = NewLine(globeRoot.transform, "Planned orbit", linePlan, 0.005f);
            shipMarker = Dot("Spacecraft marker", globeRoot.transform, spark, 0.034f);
            chaseCraft = NewCraft(globeRoot.transform, "Flight vehicle", spacecraftTemplate);
            chaseCraft.transform.localScale = Vector3.one * 0.017f;
            for (int i = 0; i < sites.Length; i++)
                sites[i] = Dot(data.sites[i].name, globeRoot.transform, marker, 0.024f);

            terrainRoot = new GameObject("LOLA survey terrain");
            terrainRoot.transform.SetParent(transform, false);
            terrainMesh = new Mesh { name = "Survey elevation tile", hideFlags = HideFlags.DontSave };
            terrainRoot.AddComponent<RuntimeMesh>().ownedMesh = terrainMesh;
            terrainRoot.AddComponent<MeshFilter>().sharedMesh = terrainMesh;
            terrainMaterial = new Material(Shader.Find("Orbital Frontier/Survey Terrain"));
            terrainRoot.AddComponent<MeshRenderer>().sharedMaterial = terrainMaterial;
            terrainRoot.SetActive(false);

            hangarRoot = new GameObject("Assembly preview");
            hangarRoot.transform.SetParent(transform, false);
            craft = NewCraft(hangarRoot.transform, "Vehicle model", spacecraftTemplate);
            for (int i = -7; i <= 7; i++)
            {
                float w = i * 0.50f;
                var a = NewLine(hangarRoot.transform, "Deck grid X", grid, .013f);
                var b = NewLine(hangarRoot.transform, "Deck grid Z", grid, .013f);
                a.positionCount = b.positionCount = 2;
                a.SetPositions(new[] { new Vector3(w, -1.62f, -3.5f), new Vector3(w, -1.62f, 3.5f) });
                b.SetPositions(new[] { new Vector3(-3.5f, -1.62f, w), new Vector3(3.5f, -1.62f, w) });
            }
        }

        public void Show(int screen)
        {
            if (display == screen) return;
            display = screen;
            camera3d.enabled = true;
            globeRoot.SetActive(screen == 0 || screen == 2);
            starRoot.SetActive(screen == 0 || screen == 2);
            hangarRoot.SetActive(screen == 1);
            keyRoot.SetActive(screen == 1 || screen == 2);
            bool flight = screen == 2;
            live.gameObject.SetActive(flight);
            plan.gameObject.SetActive(flight);
            shipMarker.SetActive(flight);
            chaseCraft.gameObject.SetActive(false);
            foreach (GameObject site in sites) site.SetActive(flight);
            Reset(0);
        }

        public void Reset(int viewMode)
        {
            mode = viewMode;
            yaw = display == 1 ? 36 : mode == 2 ? 23 : 65;
            pitch = display == 1 ? 19 : mode == 2 ? 47 : 41;
            distance = display == 1 ? 9.5f : mode == 2 ? 6.4f : 4.0f;
            terrainRoot.SetActive(display == 2 && mode == 2);
            globeRoot.SetActive(display == 0 || display == 2 && mode != 2);
            starRoot.SetActive(display == 0 || display == 2 && mode != 2);
        }

        public void Drag(float dx, float dy)
        {
            yaw -= dx * 0.48f;
            pitch = Mathf.Clamp(pitch + dy * 0.43f, -72, 80);
        }

        public void Zoom(float delta)
        {
            distance = Mathf.Clamp(distance + delta * 0.25f, display == 1 ? 4 : 2.1f, display == 1 ? 17 : 8);
        }

        public void UpdateView(GameView data, int mount, Rect region, bool motion)
        {
            camera3d.rect = region;
            if (data.screen == 0)
            {
                moon.transform.localPosition = new Vector3(.84f, .07f, 0);
                moon.transform.localScale = new Vector3(1.60f,1.60f,1.60f);
                moon.transform.localRotation = Quaternion.Euler(-4, motion ? 52 + Time.unscaledTime * .4f : 52, -12);
                lunar.SetVector("_SunDir", new Vector4(-.38f, .45f, -.83f, 0));
                camera3d.transform.SetPositionAndRotation(new Vector3(0, 0, -5.25f), Quaternion.identity);
                return;
            }
            if (data.screen == 1)
            {
                craft.Assemble(data.design, mount);
                OrbitCamera(Vector3.zero);
                return;
            }
            if (data.flight == null || data.screen != 2) return;

            var flight = data.flight;
            if (mode == 2)
            {
                var product = flight.products[flight.target];
                if (product != null)
                {
                    BuildTerrain(product, flight.target);
                    OrbitCamera(Vector3.zero);
                    return;
                }
                Reset(0);
            }
            bool chase = mode == 1;
            float meterToWorld = (float)(1 / catalog.moonRadius);
            moon.transform.localPosition = Vector3.zero;
            moon.transform.localScale = Vector3.one;
            moon.transform.localRotation = Quaternion.Euler(0, (float)(-flight.time / catalog.moonDay * 360), 0);
            var sunlight = ToUnity(flight.sun);
            lunar.SetVector("_SunDir", new Vector4(sunlight.x, sunlight.y, sunlight.z, 0));
            Trace(live, flight.trajectory, meterToWorld, !chase);
            Trace(plan, flight.preview, meterToWorld, !chase);
            var p = ToUnity(flight.orbit.p, meterToWorld);
            shipMarker.SetActive(!chase);
            shipMarker.transform.position = p;
            for (int i = 0; i < sites.Length; i++)
            {
                sites[i].transform.position = ToUnity(flight.markers[i], 1.008f);
                sites[i].transform.localScale = Vector3.one * (i == flight.target ? .036f : .022f);
                sites[i].GetComponent<Renderer>().sharedMaterial = flight.scanned[i] > 0 ? markerDone : marker;
            }
            chaseCraft.gameObject.SetActive(chase);
            if (chase)
            {
                chaseCraft.Assemble(data.design);
                Vector3 normal = p.normalized;
                Vector3 velocity = Vector3.ProjectOnPlane(ToUnity(flight.orbit.v), normal).normalized;
                Vector3 heading = velocity * Mathf.Cos((float)flight.heading) + normal * Mathf.Sin((float)flight.heading);
                chaseCraft.transform.SetPositionAndRotation(p, Quaternion.FromToRotation(Vector3.up, heading));
                Vector3 lateral = Vector3.Cross(normal, velocity).normalized;
                var eye = p - velocity * 0.155f + normal * .062f + lateral * .085f;
                var look = p + velocity * .035f - normal * .026f;
                camera3d.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, normal));
            }
            else OrbitCamera(Vector3.zero);
        }

        void BuildTerrain(SurveyProduct product, int site)
        {
            if (terrainSite == site && terrainLevel == product.level) return;
            terrainSite = site;
            terrainLevel = product.level;
            int n = product.side;
            var p = new Vector3[n * n];
            var colors = new Color[n * n];
            var tris = new int[(n - 1) * (n - 1) * 6];
            float worldPerMetre = 3.35f / ((n - 1) * (float)product.cellKm * 1000);
            double center = (product.low + product.high) * .5;
            for (int row = 0; row < n; row++)
                for (int column = 0; column < n; column++)
                {
                    int i = row * n + column;
                    p[i] = new Vector3((column / (float)(n - 1) - .5f) * 3.35f,
                        (float)(product.height[i] - center) * worldPerMetre * 8,
                        (.5f - row / (float)(n - 1)) * 3.35f);
                    colors[i] = Color.Lerp(new Color(.19f, .56f, .53f), new Color(.91f, .66f, .36f), (float)(product.slope[i] / 15));
                    if (row == n - 1 || column == n - 1) continue;
                    int t = (row * (n - 1) + column) * 6;
                    tris[t] = i; tris[t + 1] = i + 1; tris[t + 2] = i + n;
                    tris[t + 3] = i + 1; tris[t + 4] = i + n + 1; tris[t + 5] = i + n;
                }
            terrainMesh.Clear();
            terrainMesh.vertices = p;
            terrainMesh.colors = colors;
            terrainMesh.triangles = tris;
            terrainMesh.RecalculateNormals();
        }

        void OrbitCamera(Vector3 center)
        {
            var offset = Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            camera3d.transform.SetPositionAndRotation(center + offset, Quaternion.LookRotation(-offset, Vector3.up));
        }

        void Trace(LineRenderer track, VectorD[] points, float scale, bool shown)
        {
            track.enabled = shown && points != null && points.Length > 1;
            if (!track.enabled) return;
            track.positionCount = points.Length;
            for (int i = 0; i < points.Length; ++i) track.SetPosition(i, ToUnity(points[i], scale));
        }

        static Vector3 ToUnity(VectorD v, float scale = 1) => new Vector3((float)v.x, (float)v.y, (float)v.z) * scale;

        SpacecraftVisual NewCraft(Transform parent, string label, Transform template)
        {
            var obj = template ? Instantiate(template.gameObject, parent) : new GameObject(label);
            obj.name = label;
            obj.transform.SetParent(parent, false);
            return obj.TryGetComponent<SpacecraftVisual>(out var model) ? model : obj.AddComponent<SpacecraftVisual>();
        }

        void Lamp(string name, float intensity, Color color, Vector3 direction)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(keyRoot.transform, false);
            obj.transform.rotation = Quaternion.LookRotation(direction);
            var light = obj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
        }

        Material Flat(Color color) => new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };

        LineRenderer NewLine(Transform parent, string label, Material material, float width)
        {
            var obj = new GameObject(label);
            obj.transform.SetParent(parent, false);
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        GameObject Dot(string label, Transform parent, Material material, float diameter)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = label;
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = Vector3.one * diameter;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
            return obj;
        }

        void BuildStars()
        {
            const int count = 2300;
            var rand = new System.Random(613);
            var mesh = new Mesh { name = "Star points", hideFlags = HideFlags.DontSave };
            var points = new Vector3[count * 4];
            var ids = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                var center = ToUnity(Orbit.Direction(Mathf.Asin((float)(rand.NextDouble() * 2 - 1)) * Mathf.Rad2Deg,
                    rand.NextDouble() * 360), 71);
                float size = .018f + Mathf.Pow((float)rand.NextDouble(), 4) * .048f;
                var right = Vector3.Cross(Vector3.up, center).normalized * size;
                var up = Vector3.Cross(center, right).normalized * size;
                int v = i * 4, t = i * 6;
                points[v] = center - up - right;
                points[v + 1] = center - up + right;
                points[v + 2] = center + up - right;
                points[v + 3] = center + up + right;
                ids[t] = v; ids[t + 1] = v + 2; ids[t + 2] = v + 1;
                ids[t + 3] = v + 1; ids[t + 4] = v + 2; ids[t + 5] = v + 3;
            }
            mesh.vertices = points;
            mesh.triangles = ids;
            starRoot.AddComponent<MeshFilter>().sharedMesh = mesh;
            starRoot.AddComponent<MeshRenderer>().sharedMaterial = spark;
            starRoot.AddComponent<RuntimeMesh>().ownedMesh = mesh;
        }

        static Mesh MoonMesh()
        {
            const int w = 120, h = 64;
            var mesh = new Mesh { name = "LRO map sphere", hideFlags = HideFlags.DontSave };
            var p = new Vector3[(w + 1) * (h + 1)];
            var uv = new Vector2[p.Length];
            var tri = new int[w * h * 6];
            for (int y = 0; y <= h; y++)
                for (int x = 0; x <= w; x++)
                {
                    int i = y * (w + 1) + x;
                    float u = (float)x / w, v = (float)y / h;
                    p[i] = ToUnity(Orbit.Direction(180 * v - 90, 360 * u - 180));
                    uv[i] = new Vector2(u, v);
                    if (x == w || y == h) continue;
                    int t = (y * w + x) * 6, a = i + w + 1;
                    tri[t] = i; tri[t + 1] = a; tri[t + 2] = i + 1;
                    tri[t + 3] = i + 1; tri[t + 4] = a; tri[t + 5] = a + 1;
                }
            mesh.vertices = p;
            mesh.normals = p;
            mesh.uv = uv;
            mesh.triangles = tri;
            return mesh;
        }

        void OnDestroy()
        {
            foreach (var mat in new[] { lunar, lineLive, linePlan, marker, markerDone, spark, grid, terrainMaterial })
                if (mat) Destroy(mat);
        }
    }
}
