// Felsen (Fabian 01.11.): „Felsen sollen wie Felsen aussehen, nicht wie komische Klötze.“
// Prozedurale Brocken: Kugel aus vielen Dreiecken, mit Rauschen ausgebeult, mit ein paar schrägen Schnittflächen
// abgeschlagen (wie gebrochener Stein), Unterseite flach und im Boden versenkt. Dazu eine kachelbare Gesteinstextur
// (Körnung, Adern, dunkle Risse, helle Sprenkel) – eingefärbt in der Farbe des Planeten.
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public static class RockKit
    {
        public const int Variants = 8;
        static readonly Mesh[] meshes = new Mesh[Variants * 2];
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static Texture2D tex;

        // ---------- Rauschen ----------
        static float Hash(int x, int y, int z, int s) { unchecked { int h = x * 374761393 + y * 668265263 + z * 2147483647 + s * 1274126177; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float Noise3(Vector3 p, int s)
        {
            int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y), z = Mathf.FloorToInt(p.z); float fx = p.x - x, fy = p.y - y, fz = p.z - z;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
            float L(int a, int b, int c) => Hash(x + a, y + b, z + c, s);
            float x00 = Mathf.Lerp(L(0, 0, 0), L(1, 0, 0), fx), x10 = Mathf.Lerp(L(0, 1, 0), L(1, 1, 0), fx), x01 = Mathf.Lerp(L(0, 0, 1), L(1, 0, 1), fx), x11 = Mathf.Lerp(L(0, 1, 1), L(1, 1, 1), fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz) * 2 - 1;
        }
        static float Fbm(Vector3 p, int s, int oct) { float a = 1, t = 0, n = 0; for (int i = 0; i < oct; i++) { t += Noise3(p, s + i * 17) * a; n += a; a *= .5f; p *= 2.03f; } return t / n; }

        // ---------- Form ----------
        /// <summary>Gesteinsbrocken Nr. v (0–7), Durchmesser etwa 1, Unterseite bei y ≈ −0,1.</summary>
        public static Mesh Mesh(int v, bool fine = false) /* fine: 642 Ecken für große Deko-Felsen, sonst 162 (Leistung: tausende Bodenteile) */
        {
            v = Mathf.Abs(v) % Variants; int slot = v + (fine ? Variants : 0); if (meshes[slot]) return meshes[slot];
            var rnd = new System.Random(911 + v * 7919); float R() => (float)rnd.NextDouble();
            // Ikosaeder, 3× unterteilt (642 Ecken) – genug für Beulen und Kanten
            var verts = new List<Vector3>(); var tris = new List<int>(); var cache = new Dictionary<long, int>();
            float t0 = (1 + Mathf.Sqrt(5)) / 2;
            foreach (var p in new[] { new Vector3(-1, t0, 0), new Vector3(1, t0, 0), new Vector3(-1, -t0, 0), new Vector3(1, -t0, 0), new Vector3(0, -1, t0), new Vector3(0, 1, t0), new Vector3(0, -1, -t0), new Vector3(0, 1, -t0), new Vector3(t0, 0, -1), new Vector3(t0, 0, 1), new Vector3(-t0, 0, -1), new Vector3(-t0, 0, 1) }) verts.Add(p.normalized);
            int[] f = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            tris.AddRange(f);
            int Mid(int a, int b) { long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; if (cache.TryGetValue(k, out int i)) return i; verts.Add(((verts[a] + verts[b]) * .5f).normalized); cache[k] = verts.Count - 1; return verts.Count - 1; }
            for (int r = 0; r < (fine ? 3 : 2); r++) { var nt = new List<int>(); for (int i = 0; i < tris.Count; i += 3) { int a = tris[i], b = tris[i + 1], c = tris[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a); nt.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca }); } tris = nt; }
            // Grundform: gestaucht und gestreckt, je Variante anders
            var scale = new Vector3(.95f + R() * .45f, .55f + R() * .35f, .8f + R() * .35f); int seed = 31 + v * 101;
            // abgeschlagene Flächen: 3–6 Ebenen, an denen der Stein „gebrochen“ ist
            int nc = 2 + rnd.Next(3); var cuts = new List<(Vector3 n, float d)>();
            for (int i = 0; i < nc; i++) { var n = new Vector3(R() * 2 - 1, R() * 1.4f - .2f, R() * 2 - 1).normalized; cuts.Add((n, .72f + R() * .22f)); }
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i];
                float bump = .3f * Fbm(p * 1.4f, seed, 3) + .07f * Fbm(p * 5f, seed + 5, 3); // große Beulen + feine Unebenheit
                p *= 1 + bump; p = Vector3.Scale(p, scale);
                foreach (var (n, d) in cuts) { float k = Vector3.Dot(p, n) - d * scale.magnitude * .58f; if (k > 0) p -= n * k * .65f; } // Bruchflächen (nicht ganz eben)
                if (p.y < -.12f) p.y = -.12f + (p.y + .12f) * .15f; // flache Unterseite – liegt auf dem Boden
                verts[i] = p * .5f;
            }
            var m = new Mesh { name = "Fels" + v }; m.SetVertices(verts); m.SetTriangles(tris, 0); m.RecalculateNormals();
            // UV je Fläche aus der Hauptrichtung der Normale (Würfelprojektion) – Textur sitzt ohne sichtbare Verzerrung
            var nr = m.normals; var uv = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++) { var p = verts[i] * 1.6f; var a = new Vector3(Mathf.Abs(nr[i].x), Mathf.Abs(nr[i].y), Mathf.Abs(nr[i].z)); uv[i] = a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z) : a.x >= a.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y); uv[i] += new Vector2(v * .37f, v * .61f); }
            m.uv = uv; m.RecalculateBounds(); meshes[slot] = m; return m;
        }

        // ---------- Oberfläche ----------
        /// <summary>Kachelbare Gesteinstextur in Graustufen (wird über die Materialfarbe eingefärbt).</summary>
        static Texture2D Tex()
        {
            if (tex) return tex; const int N = 256; tex = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "Gestein", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 2 };
            var px = new Color32[N * N]; const float P = 8; // Periode des Rauschens = kachelbar
            float TN(float x, float y, float per, int s) // periodisches 2D-Rauschen
            {
                int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); int p = (int)per;
                float H(int a, int b) => Hash(((xi + a) % p + p) % p, ((yi + b) % p + p) % p, 0, s);
                return Mathf.Lerp(Mathf.Lerp(H(0, 0), H(1, 0), fx), Mathf.Lerp(H(0, 1), H(1, 1), fx), fy);
            }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float u = x / (float)N, w = y / (float)N; float g = 0, a = .5f, per = P;
                for (int o = 0; o < 5; o++) { g += TN(u * per, w * per, per, 3 + o) * a; a *= .5f; per *= 2; } // Körnung
                float vein = Mathf.Abs(TN(u * 16, w * 16, 16, 40) - .5f) * 2; float crack = Mathf.Clamp01(1 - vein * 16) * Mathf.Clamp01((TN(u * 4, w * 4, 4, 41) - .5f) * 4); /* nur vereinzelte, feine Risse */ // dunkle Risse
                float band = .5f + .5f * Mathf.Sin((u * 2 + w * 3 + TN(u * 8, w * 8, 8, 50) * .9f) * Mathf.PI * 2); // leichte Schichtung
                float speck = Hash(x, y, 1, 77) > .985f ? .25f : Hash(x, y, 2, 78) < .02f ? -.2f : 0; // Sprenkel
                float l = .62f + (g - .5f) * .55f + (band - .5f) * .08f - crack * .12f + speck;
                byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(l * 255), 0, 255); px[y * N + x] = new Color32(b, b, (byte)Mathf.Clamp(b - 4, 0, 255), 255);
            }
            tex.SetPixels32(px); tex.Apply(true, true); return tex;
        }
        static Texture2D crack;
        /// <summary>Leuchtende Risse (für frisch eingeschlagene Meteoriten): schwarz mit glühenden Adern.</summary>
        public static Texture2D CrackTex()
        {
            if (crack) return crack; const int N = 128; crack = new Texture2D(N, N, TextureFormat.RGBA32, true) { name = "Glutrisse", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                var p = new Vector3(x / (float)N * 6, y / (float)N * 6, 0); float v = Mathf.Abs(Fbm(p, 91, 3)); float c = Mathf.Clamp01(1 - v * 7); c = c * c;
                float spot = Mathf.Clamp01((Fbm(p * .5f, 92, 2) + .1f) * 3); c = Mathf.Max(c, spot * .35f);
                px[y * N + x] = new Color32((byte)(255 * c), (byte)(150 * c), (byte)(60 * c), 255);
            }
            crack.SetPixels32(px); crack.Apply(true, true); return crack;
        }
        /// <summary>Eigenes Material für einen glühenden Meteoriten (Leuchtkraft wird beim Abkühlen heruntergeregelt).</summary>
        public static Material MeteorMat()
        {
            var m = new Material(MeshKit.Mat(new Color(.22f, .19f, .17f), .1f, .25f)) { name = "Meteorit" }; m.SetTexture("_BaseMap", Tex());
            m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", CrackTex()); m.SetColor("_EmissionColor", new Color(1f, .45f, .12f) * 3); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None; return m;
        }
        /// <summary>Gesteinsmaterial in einer Grundfarbe (je Farbe einmal, für GPU-Instancing).</summary>
        public static Material Mat(Color c)
        {
            string k = ColorUtility.ToHtmlStringRGB(c); if (mats.TryGetValue(k, out var m) && m) return m;
            m = new Material(MeshKit.Mat(c, 0, .14f)) { name = "Fels " + k, enableInstancing = true }; m.SetTexture("_BaseMap", Tex()); m.mainTexture = Tex(); mats[k] = m; return m;
        }
        /// <summary>Felsfarbe aus der Schmutz- bzw. Planetenfarbe: gedeckter und dunkler als der Staub darum.</summary>
        public static Color Tint(Color dust) { float g = dust.grayscale; var c = Color.Lerp(dust, new Color(g, g, g), .4f) * 1.15f; c.a = 1; return c; }

        /// <summary>Ein einzelner Felsbrocken als Objekt (für die Deko).</summary>
        public static GameObject Spawn(Transform parent, int v, Color c, float size)
        {
            var g = new GameObject("Fels"); g.transform.SetParent(parent, false); g.transform.localScale = Vector3.one * size;
            g.AddComponent<MeshFilter>().sharedMesh = Mesh(v, size > 1.2f); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Mat(c); if (size < .6f) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return g;
        }
    }
}
