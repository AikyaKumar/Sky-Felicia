using OrbitalFrontier.Core;
using UnityEngine;

namespace OrbitalFrontier.Runtime
{
    public sealed class SpacecraftVisual : MonoBehaviour
    {
        static readonly Color White = new Color(0.81f, 0.84f, 0.91f);
        static readonly Color Alloy = new Color(0.42f, 0.46f, 0.61f);
        static readonly Color Dark = new Color(0.085f, 0.10f, 0.15f);
        static readonly Color Gold = new Color(0.78f, 0.52f, 0.28f);
        static readonly Color Blue = new Color(0.07f, 0.20f, 0.53f);
        [SerializeField] Transform[] sockets = null;
        readonly Material[] materials = new Material[6];
        readonly GameObject[] sections = new GameObject[7];
        string current;

        public void Assemble(CraftDesign design, int selected = -1)
        {
            string key = string.Join("|", design.parts) + string.Join(",", design.angles) + selected;
            if (key == current) return;
            current = key;
            if (!materials[0])
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                Color[] colors = { White, Alloy, Dark, Gold, Blue, new Color(0.76f, 0.65f, 1f) };
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new Material(shader) { color = colors[i], name = "Flight hardware " + i };
                    materials[i].SetFloat("_Metallic", i == 3 ? 0.45f : 0.23f);
                    materials[i].SetFloat("_Smoothness", 0.42f);
                }
            }

            for (int slot = 0; slot < sections.Length; slot++)
            {
                if (sections[slot]) Destroy(sections[slot]);
                sections[slot] = null;
                string id = design.parts[slot];
                if (string.IsNullOrEmpty(id)) continue;
                var section = new GameObject(Slot.Names[slot]);
                sections[slot] = section;
                var anchor = sockets != null && slot < sockets.Length && sockets[slot] ? sockets[slot] : transform;
                section.transform.SetParent(anchor, false);
                section.transform.localRotation = Quaternion.Euler(0, design.angles[slot] * 90, 0);
                DrawModule(section.transform, slot, id.EndsWith("-l"), slot == selected);
            }
        }

        void DrawModule(Transform mount, int slot, bool large, bool selected)
        {
            void Box(string name, int mat, float x, float y, float z, float w, float h, float d, float rz = 0) =>
                Add(mount, name, PrimitiveType.Cube, mat, selected, new Vector3(x, y, z), new Vector3(w, h, d), rz);
            void Tube(string name, int mat, float x, float y, float z, float radius, float height, float rz = 0) =>
                Add(mount, name, PrimitiveType.Cylinder, mat, selected, new Vector3(x, y, z), new Vector3(radius * 2, height * 0.5f, radius * 2), rz);
            switch (slot)
            {
                case Slot.Bus:
                    Tube("Command pressure shell", 0, 0, 0.85f, 0, 0.67f, 0.8f);
                    Tube("Docking collar", 1, 0, 1.32f, 0, 0.38f, 0.19f);
                    Tube("Launch adapter", 2, 0, 0.44f, 0, 0.74f, 0.19f);
                    for (int quarter = 0; quarter < 4; quarter++)
                    {
                        var rotated = new GameObject("Avionics and attitude jets").transform;
                        rotated.SetParent(mount, false);
                        rotated.localRotation = Quaternion.Euler(0, quarter * 90, 0);
                        Add(rotated, "Quartz window", PrimitiveType.Cube, 4, selected, new Vector3(0, 1.04f, 0.665f), new Vector3(0.36f, 0.16f, 0.09f));
                        Add(rotated, "RCS pod", PrimitiveType.Cube, 2, selected, new Vector3(0, 0.58f, 0.755f), new Vector3(0.25f, 0.21f, 0.24f));
                    }
                    break;
                case Slot.Tank:
                    float r = large ? 0.82f : 0.66f;
                    Tube("Gold thermal blanket", 3, 0, -0.10f, 0, r, 0.88f);
                    Tube("Upper tank flange", 1, 0, 0.31f, 0, r + 0.042f, 0.12f);
                    Tube("Tank band", 1, 0, -0.20f, 0, r + 0.028f, 0.055f);
                    Tube("Lower tank flange", 0, 0, -0.53f, 0, r + 0.034f, 0.16f);
                    break;
                case Slot.Engine:
                    Tube("Thrust mount", 1, 0, -0.76f, 0, 0.32f, 0.30f);
                    AddBell(mount, large ? 0.61f : 0.49f, selected ? materials[5] : materials[2]);
                    Tube("Nozzle lip", 1, 0, -1.39f, 0, large ? 0.59f : 0.47f, 0.08f);
                    break;
                case Slot.Solar:
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float width = large ? 1.76f : 1.35f;
                        Box("Hinge", 1, side * 0.84f, 0.19f, 0, 0.65f, 0.10f, 0.17f);
                        Box("Panel frame", 1, side * (1.04f + width / 2), 0.19f, 0, width, 0.065f, 0.87f);
                        for (int i = 0; i < 6; i++)
                            for (int j = 0; j < 3; j++)
                            {
                                float x = side * (1.04f + width * (i + 0.5f) / 6);
                                float z = (j - 1) * 0.278f;
                                Box("Solar cell", 4, x, 0.225f, z, width / 6 - 0.018f, 0.02f, 0.259f);
                                Box("Solar cell", 4, x, 0.153f, z, width / 6 - 0.018f, 0.02f, 0.259f);
                            }
                    }
                    break;
                case Slot.Battery:
                    Box("Battery enclosure", 0, 0.73f, -0.35f, 0, 0.33f, 0.43f, large ? 0.57f : 0.36f);
                    Box("Insulation band", 2, 0.74f, -0.34f, 0, 0.36f, 0.08f, large ? 0.58f : 0.37f);
                    break;
                case Slot.Lidar:
                    Box("Lidar optical bench", 1, 0.16f, 0.69f, 0.66f, 0.53f, 0.20f, large ? 0.49f : 0.36f);
                    Box("Lidar aperture", 4, 0.13f, 0.69f, large ? 0.91f : 0.85f, 0.23f, 0.12f, 0.08f);
                    break;
                case Slot.Antenna:
                    Tube("Antenna arm", 1, 0.90f, 0.91f, 0.03f, 0.055f, 0.52f, -52);
                    if (large)
                    {
                        Add(mount, "S-band reflector", PrimitiveType.Sphere, 0, selected, new Vector3(1.12f, 1.1f, 0.03f),
                            new Vector3(0.61f, 0.10f, 0.59f), -28);
                        Tube("S-band feed", 3, 1.21f, 1.16f, 0.03f, 0.044f, 0.30f, -28);
                    }
                    else
                    {
                        Box("S-band patch", 0, 1.12f, 1.08f, 0.02f, 0.40f, 0.065f, 0.34f, -33);
                        Box("Patch radiating face", 3, 1.14f, 1.12f, 0.02f, 0.34f, 0.025f, 0.28f, -33);
                    }
                    break;
            }
        }

        void Add(Transform parent, string label, PrimitiveType kind, int mat, bool selected, Vector3 p, Vector3 scale, float rz = 0)
        {
            var obj = GameObject.CreatePrimitive(kind);
            obj.name = label;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = p;
            obj.transform.localScale = scale;
            obj.transform.localRotation = Quaternion.Euler(0, 0, rz);
            obj.GetComponent<Renderer>().sharedMaterial = materials[selected ? 5 : mat];
            Collider collider = obj.GetComponent<Collider>();
            if (collider) Destroy(collider);
        }

        void AddBell(Transform parent, float rim, Material material)
        {
            const int count = 24;
            var mesh = new Mesh { name = "Engine bell", hideFlags = HideFlags.DontSave };
            var vertices = new Vector3[count * 3];
            var indices = new int[count * 12];
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                vertices[i] = d * 0.22f + Vector3.down * 0.82f;
                vertices[count + i] = d * rim * 0.64f + Vector3.down * 1.10f;
                vertices[count * 2 + i] = d * rim + Vector3.down * 1.40f;
                for (int l = 0; l < 2; l++)
                {
                    int j = (l * count + i) * 6;
                    int a0 = l * count + i, b = l * count + (i + 1) % count;
                    indices[j] = a0;
                    indices[j + 1] = b;
                    indices[j + 2] = b + count;
                    indices[j + 3] = a0;
                    indices[j + 4] = b + count;
                    indices[j + 5] = a0 + count;
                }
            }
            mesh.vertices = vertices;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            var obj = new GameObject("Bell contour");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            obj.AddComponent<RuntimeMesh>().ownedMesh = mesh;
        }

        void OnDestroy()
        {
            foreach (var material in materials)
                if (material) Destroy(material);
        }
    }

    public sealed class RuntimeMesh : MonoBehaviour
    {
        public Mesh ownedMesh;
        void OnDestroy() { if (ownedMesh) Destroy(ownedMesh); }
    }
}
