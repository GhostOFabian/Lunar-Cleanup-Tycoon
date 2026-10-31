// Prozedurale Meshes und Materialien (keine gekauften Assets).
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LunarCleanup.Gameplay
{
    public static class MeshKit
    {
        // Unity 6 lädt beim Play-Start die Skripte nicht neu – zwischengespeicherte Meshes/Materialien aus der letzten Runde sind dann zerstört.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { Mats.Clear(); MeshCache.Clear(); glowTex = null; Cube = Sphere = Quad = null; }
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static Shader lit, unlit;
        public static Mesh Cube, Sphere, Quad;

        public static void Init()
        {
            lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (Cube == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Cube); Cube = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); }
            if (Sphere == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Sphere); Sphere = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); }
            if (Quad == null) { var g = GameObject.CreatePrimitive(PrimitiveType.Quad); Quad = g.GetComponent<MeshFilter>().sharedMesh; Object.Destroy(g); }
        }

        /// <summary>Lack/Metall-Material, wird nach Farbe zwischengespeichert.</summary>
        public static Material Mat(Color c, float metal = .2f, float smooth = .45f, Color? emit = null)
        {
            string k = ColorUtility.ToHtmlStringRGBA(c) + metal.ToString("0.00") + smooth.ToString("0.00") + (emit.HasValue ? ColorUtility.ToHtmlStringRGBA(emit.Value) : "");
            if (Mats.TryGetValue(k, out var m)) return m;
            m = new Material(lit); m.SetColor("_BaseColor", c); m.color = c; m.SetFloat("_Metallic", metal); m.SetFloat("_Smoothness", smooth); m.enableInstancing = true;
            if (emit.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emit.Value); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; }
            Mats[k] = m; return m;
        }
        public static Material Glow(Color c, float intensity = 2.5f) => Mat(c * .4f, 0, .2f, c * intensity);
        public static Material Unlit(Color c, bool transparent = false)
        {
            string k = "U" + ColorUtility.ToHtmlStringRGBA(c) + transparent;
            if (Mats.TryGetValue(k, out var m)) return m;
            m = new Material(unlit); m.SetColor("_BaseColor", c); m.color = c;
            if (transparent) MakeTransparent(m, true);
            Mats[k] = m; return m;
        }
        public static void MakeTransparent(Material m, bool additive)
        {
            m.SetFloat("_Surface", 1); m.SetFloat("_Blend", additive ? 2 : 0);
            m.SetOverrideTag("RenderType", "Transparent"); m.renderQueue = (int)RenderQueue.Transparent;
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha); m.SetInt("_DstBlend", additive ? (int)BlendMode.One : (int)BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        public static GameObject Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Vector3 euler = default)
        {
            var g = new GameObject(mesh.name); g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = scale; g.transform.localRotation = Quaternion.Euler(euler);
            g.AddComponent<MeshFilter>().sharedMesh = mesh; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            return g;
        }
        public static GameObject Box(Transform p, Material m, Vector3 pos, Vector3 size, Vector3 euler = default) => Part(p, Cube, m, pos, size, euler);
        public static GameObject Ball(Transform p, Material m, Vector3 pos, float d) => Part(p, Sphere, m, pos, Vector3.one * d);
        public static GameObject Cyl(Transform p, Material m, Vector3 pos, float rTop, float rBot, float h, int seg = 20, Vector3 euler = default) => Part(p, CylMesh(rTop, rBot, h, seg), m, pos, Vector3.one, euler);

        static readonly Dictionary<string, Mesh> MeshCache = new Dictionary<string, Mesh>();
        /// <summary>Zylinder oder Kegelstumpf, Mittelpunkt in der Mitte der Höhe.</summary>
        public static Mesh CylMesh(float rTop, float rBot, float h, int seg = 20)
        {
            string k = "c" + rTop + "_" + rBot + "_" + h + "_" + seg; if (MeshCache.TryGetValue(k, out var cm)) return cm;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            float slope = (rBot - rTop) / h;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2, c = Mathf.Cos(a), s = Mathf.Sin(a); var nn = new Vector3(c, slope, s).normalized;
                v.Add(new Vector3(c * rBot, -h / 2, s * rBot)); n.Add(nn); v.Add(new Vector3(c * rTop, h / 2, s * rTop)); n.Add(nn);
            }
            for (int i = 0; i < seg; i++) { int b = i * 2; t.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }); }
            foreach (var top in new[] { true, false })
            {
                float r = top ? rTop : rBot; if (r <= 1e-4f) continue; float y = top ? h / 2 : -h / 2; int c0 = v.Count; v.Add(new Vector3(0, y, 0)); n.Add(top ? Vector3.up : Vector3.down);
                for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI * 2; v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); n.Add(top ? Vector3.up : Vector3.down); }
                for (int i = 0; i < seg; i++) { if (top) t.AddRange(new[] { c0, c0 + i + 2, c0 + i + 1 }); else t.AddRange(new[] { c0, c0 + i + 1, c0 + i + 2 }); }
            }
            var mesh = new Mesh { name = "Cyl" }; mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0); mesh.RecalculateBounds(); MeshCache[k] = mesh; return mesh;
        }

        /// <summary>Halbe Röhre (Hangardach), Achse entlang z.</summary>
        public static Mesh ArchMesh(float r, float len, int seg = 16)
        {
            string k = "a" + r + "_" + len; if (MeshCache.TryGetValue(k, out var cm)) return cm;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI, c = Mathf.Cos(a), s = Mathf.Sin(a); var nn = new Vector3(c, s, 0); v.Add(new Vector3(c * r, s * r, -len / 2)); n.Add(nn); v.Add(new Vector3(c * r, s * r, len / 2)); n.Add(nn); }
            for (int i = 0; i < seg; i++) { int b = i * 2; t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 }); }
            foreach (float z in new[] { -len / 2, len / 2 })
            {
                int c0 = v.Count; var nz = new Vector3(0, 0, Mathf.Sign(z)); v.Add(new Vector3(0, 0, z)); n.Add(nz);
                for (int i = 0; i <= seg; i++) { float a = i / (float)seg * Mathf.PI; v.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z)); n.Add(nz); }
                for (int i = 0; i < seg; i++) { if (z > 0) t.AddRange(new[] { c0, c0 + i + 1, c0 + i + 2 }); else t.AddRange(new[] { c0, c0 + i + 2, c0 + i + 1 }); }
            }
            var mesh = new Mesh { name = "Arch" }; mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetTriangles(t, 0); mesh.RecalculateBounds(); MeshCache[k] = mesh; return mesh;
        }

        /// <summary>Weicher Leuchtpunkt (für Sterne, Lichter, Atmosphäre).</summary>
        static Texture2D glowTex;
        public static Texture2D GlowTex()
        {
            if (glowTex) return glowTex; int S = 64; glowTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S]; for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) { float d = new Vector2(x - S / 2f + .5f, y - S / 2f + .5f).magnitude / (S / 2f); float a = Mathf.Clamp01(1 - d); a = a * a * a; px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255)); }
            glowTex.SetPixels32(px); glowTex.Apply(); return glowTex;
        }
    }
}
