// Nachbarkörper am Himmel, nachgebaut wie die echten: Erde (Kontinente, Ozeane, Polkappen, Wolken, Atmosphäre),
// Jupiter (Wolkenbänder, Großer Roter Fleck), Saturn (Bänder und Ringe), Neptun (dunkler Fleck), Monde mit Kratern,
// rote Zwerge, die nahe Sonne und ein Pulsar. Alle Texturen werden beim Start berechnet (keine Bilddateien).
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        static readonly Dictionary<string, Texture2D> bodyTex = new Dictionary<string, Texture2D>();

        void BuildSkyBody(string pid, string gi)
        {
            string kind = pid == "moon" ? "earth" : gi; if (string.IsNullOrEmpty(kind)) return;
            Vector3 gp; float gr;
            switch (kind)
            {
                case "earth": gp = new Vector3(-900, 500, -1500); gr = 120; break;
                case "jupiter": gp = new Vector3(-1500, 700, -1900); gr = 420; break;
                case "saturn": gp = new Vector3(1300, 600, -1900); gr = 300; break;
                case "neptune": gp = new Vector3(-1200, 500, -1700); gr = 260; break;
                case "phobos": case "charon": case "dysnomia": gp = new Vector3(900, 400, -1300); gr = 70; break;
                case "reddwarf": case "trappist": gp = new Vector3(1300, 650, -1900); gr = 140; break;
                case "sunclose": gp = new Vector3(1100, 500, -1500); gr = 380; break;
                case "pulsar": gp = new Vector3(-1200, 600, -1700); gr = 30; break;
                default: return;
            }
            var root = new GameObject("Nachbar " + kind).transform; root.SetParent(skyRoot, false); root.localPosition = gp;
            bool star = kind == "reddwarf" || kind == "trappist" || kind == "sunclose" || kind == "pulsar";
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Material M(Texture2D tex, float emit)
            {
                var m = new Material(lit); m.SetTexture("_BaseMap", tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .15f); m.SetFloat("_Metallic", 0);
                m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", Color.white * emit); return m;
            }
            if (star)
            {
                Color c = kind == "sunclose" ? new Color(1f, .93f, .75f) : kind == "pulsar" ? new Color(.85f, 1f, .95f) : new Color(1f, .42f, .2f);
                var tex = Tex(kind, () => StarTex(c));
                var um = MeshKit.Unlit(Color.white); um.SetTexture("_BaseMap", tex); um.mainTexture = tex;
                var b = MeshKit.Ball(root, um, Vector3.zero, gr * 2); NoShadow(b); b.name = "Stern";
                var halo = MeshKit.Part(root, MeshKit.Quad, Glow(c, .9f), Vector3.zero, Vector3.one * gr * (kind == "pulsar" ? 14 : 4.2f)); NoShadow(halo); halo.AddComponent<Billboard>();
                if (kind == "pulsar") for (int s = -1; s <= 1; s += 2) { var beam = MeshKit.Cyl(root, Glow(c, .45f), new Vector3(0, s * 260, 0), s > 0 ? 2 : 60, s > 0 ? 60 : 2, 520, 12); NoShadow(beam); beam.name = "Strahl"; }
                root.gameObject.AddComponent<Spin>().speed = kind == "pulsar" ? 90 : 1;
                if (kind == "pulsar") root.localRotation = Quaternion.Euler(25, 0, 30);
                return;
            }
            Texture2D T = kind == "earth" ? Tex("earth", EarthTex) : kind == "jupiter" ? Tex("jupiter", () => GasTex(false)) : kind == "saturn" ? Tex("saturn", () => GasTex(true)) : kind == "neptune" ? Tex("neptune", NeptuneTex) : Tex("moon_" + kind, () => RockTex(kind));
            var body = MeshKit.Ball(root, M(T, kind == "earth" ? .08f : .12f), Vector3.zero, gr * 2); body.name = "Oberfläche"; NoShadow(body);
            body.transform.localRotation = Quaternion.Euler(kind == "earth" ? -23 : kind == "saturn" ? -27 : -6, 0, 0);
            body.AddComponent<Spin>().speed = kind == "jupiter" || kind == "saturn" ? 1.2f : .6f;
            if (kind == "phobos") body.transform.localScale = new Vector3(gr * 2.3f, gr * 1.7f, gr * 1.9f); // Phobos ist eine Kartoffel
            if (kind == "earth")
            {
                var cm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); var ct = Tex("clouds", CloudTex); cm.SetTexture("_BaseMap", ct); cm.SetColor("_BaseColor", new Color(.92f, .94f, 1f, .9f)); MeshKit.MakeTransparent(cm, false); /* Wolken durchsichtig (wie die Ringe) */
                var clouds = MeshKit.Ball(root, cm, Vector3.zero, gr * 2.04f); clouds.name = "Wolken"; NoShadow(clouds); clouds.transform.localRotation = Quaternion.Euler(-23, 0, 0); clouds.AddComponent<Spin>().speed = .9f;
                var atm = MeshKit.Part(root, MeshKit.Quad, Glow(new Color(.35f, .6f, 1f), .3f), Vector3.zero, Vector3.one * gr * 2.5f); NoShadow(atm); atm.AddComponent<Billboard>(); atm.name = "Atmosphäre";
            }
            if (kind == "neptune") { var atm = MeshKit.Part(root, MeshKit.Quad, Glow(new Color(.3f, .5f, 1f), .35f), Vector3.zero, Vector3.one * gr * 2.5f); NoShadow(atm); atm.AddComponent<Billboard>(); }
            if (kind == "saturn")
            {
                var rm = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); var rt = Tex("rings", RingTex); rm.SetTexture("_BaseMap", rt); rm.SetColor("_BaseColor", new Color(1, 1, 1, .9f)); MeshKit.MakeTransparent(rm, false); rm.SetFloat("_Cull", 0);
                var ring = new GameObject("Ringe"); ring.transform.SetParent(root, false); ring.transform.localRotation = Quaternion.Euler(-27 + 90, 0, 8);
                ring.AddComponent<MeshFilter>().sharedMesh = RingMesh(gr * 1.25f, gr * 2.3f); var rr = ring.AddComponent<MeshRenderer>(); rr.sharedMaterial = rm; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Texture2D Tex(string key, System.Func<Texture2D> make) { if (bodyTex.TryGetValue(key, out var t) && t) return t; t = make(); t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Trilinear; t.anisoLevel = 2; bodyTex[key] = t; return t; }

        // ---------- 3D-Rauschen (nahtlos auf der Kugel) ----------
        static float H3(int x, int y, int z) { unchecked { int h = x * 374761393 + y * 668265263 + z * 1274126177; h = (h ^ (h >> 13)) * 1103515245; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float N3(Vector3 p)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z); float fx = p.x - x, fy = p.y - y, fz = p.z - z;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
            float a = Mathf.Lerp(Mathf.Lerp(H3(x, y, z), H3(x + 1, y, z), fx), Mathf.Lerp(H3(x, y + 1, z), H3(x + 1, y + 1, z), fx), fy);
            float b = Mathf.Lerp(Mathf.Lerp(H3(x, y, z + 1), H3(x + 1, y, z + 1), fx), Mathf.Lerp(H3(x, y + 1, z + 1), H3(x + 1, y + 1, z + 1), fx), fy);
            return Mathf.Lerp(a, b, fz) * 2 - 1;
        }
        static float Fbm(Vector3 p, int oct) { float s = 0, a = .5f, n = 0; for (int i = 0; i < oct; i++) { s += a * N3(p); n += a; p *= 2.03f; p += new Vector3(1.7f, 9.2f, 4.1f); a *= .5f; } return s / n; }
        delegate Color PixelFn(Vector3 d, float lat, float lon);
        static Texture2D Paint(int W, int H, PixelFn f)
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, true); var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float lat = (y + .5f) / H * Mathf.PI - Mathf.PI / 2;
                for (int x = 0; x < W; x++) { float lon = (x + .5f) / W * Mathf.PI * 2; var d = new Vector3(Mathf.Cos(lat) * Mathf.Cos(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Sin(lon)); px[y * W + x] = f(d, lat, lon); }
            }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        static Texture2D EarthTex() => Paint(512, 256, (d, lat, lon) =>
        {
            float h = Fbm(d * 1.6f, 6) + .12f * Fbm(d * 6f, 3); float al = Mathf.Abs(lat) * Mathf.Rad2Deg;
            if (al > 72 + 6 * Fbm(d * 4, 2)) return new Color(.93f, .95f, .98f);                                  // Polkappen
            if (h < .05f) { float k = Mathf.Clamp01((h + .5f) / .55f); return Color.Lerp(new Color(.02f, .1f, .32f), new Color(.08f, .35f, .62f), k * k); } // Ozean, flach heller
            float dry = Mathf.Clamp01(1 - Mathf.Abs(al - 24) / 14) * Mathf.Clamp01(.6f + Fbm(d * 3, 3));          // Wüstengürtel um 25°
            Color green = Color.Lerp(new Color(.13f, .32f, .1f), new Color(.3f, .45f, .18f), Mathf.Clamp01(Fbm(d * 5, 3) + .5f));
            Color land = Color.Lerp(green, new Color(.7f, .58f, .38f), dry); if (al > 55) land = Color.Lerp(land, new Color(.35f, .38f, .3f), (al - 55) / 17);
            if (h > .32f) land = Color.Lerp(land, new Color(.55f, .5f, .45f), (h - .32f) * 3); return land;   // Gebirge
        });
        static Texture2D CloudTex() => Paint(512, 256, (d, lat, lon) =>
        {
            float c = Fbm(d * 2.4f + new Vector3(0, lat * 2, 0), 6); float a = Mathf.Clamp01((c - .02f) * 2.6f) * .85f; return new Color(1, 1, 1, a);
        });
        static Texture2D GasTex(bool saturn) => Paint(512, 256, (d, lat, lon) =>
        {
            float turb = Fbm(d * 3f, 5) * (saturn ? .25f : .55f); float b = Mathf.Sin(lat * (saturn ? 18 : 22) + turb * 6);
            Color c1 = saturn ? new Color(.92f, .84f, .62f) : new Color(.93f, .87f, .76f), c2 = saturn ? new Color(.78f, .66f, .45f) : new Color(.72f, .47f, .3f);
            Color c = Color.Lerp(c2, c1, b * .5f + .5f); c *= .92f + .12f * Fbm(d * 9, 3);
            if (!saturn)
            {
                // Großer Roter Fleck bei 22° Süd
                float dl = (lat * Mathf.Rad2Deg + 22) / 6f, dlo = Mathf.DeltaAngle(lon * Mathf.Rad2Deg, 120) / 13f; float e = dl * dl + dlo * dlo;
                if (e < 1) c = Color.Lerp(c, new Color(.78f, .36f, .22f), Mathf.SmoothStep(0, 1, 1 - e) * .9f);
            }
            return c;
        });
        static Texture2D NeptuneTex() => Paint(512, 256, (d, lat, lon) =>
        {
            float b = Mathf.Sin(lat * 14 + Fbm(d * 3, 4) * 2); Color c = Color.Lerp(new Color(.16f, .3f, .78f), new Color(.3f, .48f, .92f), b * .5f + .5f);
            float dl = (lat * Mathf.Rad2Deg + 20) / 5f, dlo = Mathf.DeltaAngle(lon * Mathf.Rad2Deg, 60) / 10f; float e = dl * dl + dlo * dlo; if (e < 1) c = Color.Lerp(c, new Color(.08f, .14f, .42f), (1 - e) * .8f);
            if (Fbm(d * 7, 3) > .38f) c = Color.Lerp(c, Color.white, .5f); return c;                        // helle Methanwolken
        });
        static Texture2D RockTex(string kind)
        {
            var rnd = new System.Random(kind.GetHashCode()); var cr = new List<Vector4>();
            for (int i = 0; i < 60; i++) { var v = new Vector3((float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1, (float)rnd.NextDouble() * 2 - 1).normalized; cr.Add(new Vector4(v.x, v.y, v.z, .03f + Mathf.Pow((float)rnd.NextDouble(), 3) * .25f)); }
            Color b0 = kind == "charon" ? new Color(.55f, .52f, .5f) : kind == "dysnomia" ? new Color(.45f, .45f, .5f) : new Color(.45f, .38f, .32f);
            return Paint(256, 128, (d, lat, lon) =>
            {
                float s = .85f + .3f * Fbm(d * 4, 4);
                foreach (var c in cr) { float dd = Mathf.Acos(Mathf.Clamp(Vector3.Dot(d, new Vector3(c.x, c.y, c.z)), -1, 1)) / c.w; if (dd < 1.3f) s *= dd < 1 ? .78f + .18f * dd : 1.12f; }
                if (kind == "charon" && lat > 1.1f) return Color.Lerp(b0 * s, new Color(.45f, .22f, .15f), .6f); // Charons rote Polkappe
                return b0 * s;
            });
        }
        static Texture2D StarTex(Color c) => Paint(256, 128, (d, lat, lon) => { float g = .85f + .25f * Fbm(d * 10, 4); var o = c * g; o.a = 1; return o; });
        static Texture2D RingTex()
        {
            int W = 256; var t = new Texture2D(W, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color[W];
            for (int i = 0; i < W; i++) { float r = i / (W - 1f); float a = .55f + .35f * Mathf.Sin(r * 60) * Mathf.Sin(r * 23 + 1); if (r > .62f && r < .67f) a = .05f; /* Cassini-Teilung */ a *= Mathf.SmoothStep(0, 1, r / .06f) * Mathf.SmoothStep(0, 1, (1 - r) / .08f); px[i] = new Color(.9f, .82f, .65f, Mathf.Clamp01(a)); }
            t.SetPixels(px); t.Apply(); return t;
        }
        static Mesh RingMesh(float r0, float r1)
        {
            int n = 96; var v = new List<Vector3>(); var uv = new List<Vector2>(); var tr = new List<int>();
            for (int i = 0; i <= n; i++) { float a = i / (float)n * Mathf.PI * 2; var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); v.Add(dir * r0); v.Add(dir * r1); uv.Add(new Vector2(0, .5f)); uv.Add(new Vector2(1, .5f)); if (i < n) { int k = i * 2; tr.AddRange(new[] { k, k + 1, k + 2, k + 1, k + 3, k + 2 }); } }
            var m = new Mesh { name = "Ringe" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tr, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
        }
    }

    /// <summary>Langsame Drehung um die eigene Achse (Grad pro Sekunde).</summary>
    public class Spin : MonoBehaviour { public float speed = 1; void Update() { transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self); } }
}
