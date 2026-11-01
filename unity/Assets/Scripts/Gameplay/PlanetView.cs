// Runder Planet aus den echten Parzellen. Jede Parzelle hat ein eigenes Feld in einer Textur je Planetenabschnitt;
// dort wird der Schmutz aus dem 32x32-Raster der Simulation gemalt (mit Musterung, Kanten und Basisplatten).
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public class PlanetView : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { detail = null; }
        public const int SUB = 3; int TILE = 64; /* Pixel je Parzelle; auf großen Planeten kleiner, sonst werden die Texturen zu groß (18.000 Parzellen × 64² wären über 300 MB) */
        class Chunk { public Texture2D tex, nrm, gtex; public Color32[] px, gpx; public int per, size, gs; public bool dirty; public Material mat; }
        /// <summary>Fabian 01.11.: grüne Ränder sauberer Parzellen leuchten weit rausgezoomt (0 = nah, 1 = ganzer Planet); setzt GameRoot.</summary>
        public static float GlowK; float glowSet = -1;
        int NT = 64; /* Pixel je Parzelle in der Reliefkarte */
        PState ps; PlanetData pd; Theme th;
        readonly List<Chunk> chunks = new List<Chunk>();
        int[] tileOf; // Platz der Zelle in ihrer Textur
        static Texture2D detail;

        public void Build(PState state)
        {
            ps = state; pd = ps.pd; th = ps.P.theme;
            TILE = pd.N > 12000 ? 24 : pd.N > 7000 ? 32 : pd.N > 3500 ? 48 : pd.N > 1500 ? 64 : 96; /* kleine Planeten: feinere Textur, aus der Nähe nicht mehr blockig */ NT = pd.N > 7000 ? TILE / 2 + 4 : TILE; craters.Clear();
            foreach (Transform c in transform) Destroy(c.gameObject); foreach (var oc in chunks) if (oc != null) { Destroy(oc.tex); if (oc.nrm) Destroy(oc.nrm); if (oc.gtex) Destroy(oc.gtex); Destroy(oc.mat); } chunks.Clear(); /* alte Texturen freigeben (sonst wächst der Speicher bei jedem Planetenwechsel) */
            int F = pd.faceCenters.Length; var lists = new List<int>[F]; chunkIds = new List<int>[F]; for (int i = 0; i < F; i++) lists[i] = new List<int>();
            foreach (var cl in pd.cells) lists[cl.chunk].Add(cl.id);
            tileOf = new int[pd.N];
            for (int f = 0; f < F; f++)
            {
                var ids = lists[f]; if (ids.Count == 0) { chunks.Add(null); continue; }
                int per = Mathf.CeilToInt(Mathf.Sqrt(ids.Count)), size = per * TILE;
                var ch = new Chunk { per = per, size = size, px = new Color32[size * size], tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 } };
                ch.gs = Mathf.Max(1, size / 2); ch.gpx = new Color32[ch.gs * ch.gs]; ch.gtex = new Texture2D(ch.gs, ch.gs, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
                for (int i = 0; i < ids.Count; i++) tileOf[ids[i]] = i;
                var mesh = BuildMesh(ids, per);
                var go = new GameObject("Abschnitt " + f); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Hügel werfen keine langen schwarzen Schatten über den Planeten
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", ch.tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .05f); m.SetFloat("_Metallic", 0); m.SetFloat("_SpecularHighlights", 0); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.SetFloat("_EnvironmentReflections", 0); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); // kein Blenden bei flachem Sonnenstand
                m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); /* Normal-Map wird unten parallel berechnet: kleine Krater, Steine und Plattenfugen reagieren auf das Sonnenlicht */
                m.SetTexture("_DetailAlbedoMap", Detail()); m.SetTextureScale("_DetailAlbedoMap", new Vector2(per * 13, per * 13)); m.SetFloat("_DetailAlbedoMapScale", .16f); /* feine Körnung statt grober Flecken */ m.EnableKeyword("_DETAIL_MULX2");
                m.EnableKeyword("_EMISSION"); m.SetTexture("_EmissionMap", ch.gtex); m.SetColor("_EmissionColor", Color.black); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                mr.sharedMaterial = m; ch.mat = m;
                chunks.Add(ch); chunkIds[f] = ids;
            }
            /* Fabian 01.11.: Reisen schneller – Boden und Normal-Maps auf allen Prozessorkernen berechnen (reine Rechnerei, keine Unity-Objekte) */
            style = StyleFor(ps.pid); foreach (var cl in pd.cells) Craters(cl);
            var nrmPx = new Color32[F][];
            LunarCleanup.Core.Par.For(0, F, f => { if (chunks[f] != null) nrmPx[f] = NormalPixels(chunkIds[f], chunks[f].per); });
            LunarCleanup.Core.Par.For(0, pd.N, id => PaintCell(id));
            for (int f = 0; f < F; f++)
            {
                var ch = chunks[f]; if (ch == null) continue; int size = ch.per * NT;
                var t = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 }; t.SetPixels32(nrmPx[f]); t.Apply(true, true);
                ch.nrm = t; ch.mat.SetTexture("_BumpMap", t);
                ch.tex.SetPixels32(ch.px); ch.tex.Apply(true); ch.gtex.SetPixels32(ch.gpx); ch.gtex.Apply(true); ch.dirty = false;
            }
            glowSet = -1;
            // Planetenkugel darunter, damit zwischen den Platten nie Lücken sichtbar werden
            var core = MeshKit.Ball(transform, MeshKit.Mat(th.baseC[0] * .5f, 0, .1f), Vector3.zero, (pd.R - 4f) * 2); core.name = "Kern"; // tief genug unter Tälern und Kratern
        }

        Mesh BuildMesh(List<int> ids, int per)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float inv = 1f / per, pad = PAD;
            foreach (int id in ids)
            {
                var cl = pd.cells[id]; int ti = tileOf[id]; float u0 = (ti % per) * inv, v0 = (ti / per) * inv;
                System.Func<Vector2, Vector2> UV = p => new Vector2(u0 + (pad + (p.x + cl.H) / (2 * cl.H) * (1 - 2 * pad)) * inv, v0 + (pad + (p.y + cl.H) / (2 * cl.H) * (1 - 2 * pad)) * inv);
                int K = cl.poly.Length; int SUB = pd.HasFeatures(id) ? 12 : PlanetView.SUB; /* Geländeformen brauchen feinere Dreiecke */
                for (int k = 0; k < K; k++)
                {
                    Vector2 A = Vector2.zero, B = cl.poly[k] * 1.004f, C = cl.poly[(k + 1) % K] * 1.004f; int b0 = v.Count;
                    for (int i = 0; i <= SUB; i++) for (int j = 0; j <= SUB - i; j++)
                        {
                            Vector2 p = A + (B - A) * (i / (float)SUB) + (C - A) * (j / (float)SUB);
                            var d = pd.ToDir(cl, p.x, p.y); v.Add(pd.Surf(d, 0, id)); n.Add(pd.SurfNormal(d, id)); uv.Add(UV(p));
                        }
                    System.Func<int, int, int> I = (i, j) => { int idx = 0; for (int a = 0; a < i; a++) idx += SUB + 1 - a; return b0 + idx + j; };
                    for (int i = 0; i < SUB; i++) for (int j = 0; j < SUB - i; j++)
                        {
                            t.Add(I(i, j)); t.Add(I(i, j + 1)); t.Add(I(i + 1, j));
                            if (j < SUB - i - 1) { t.Add(I(i + 1, j)); t.Add(I(i, j + 1)); t.Add(I(i + 1, j + 1)); }
                        }
                }
            }
            var mesh = new Mesh { name = "Planet", indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(v); mesh.SetNormals(n); mesh.SetUVs(0, uv); mesh.SetTriangles(t, 0);
            // Dreiecke so wenden, dass sie nach außen zeigen
            var tris = mesh.triangles; for (int i = 0; i < tris.Length; i += 3) { var a = v[tris[i]]; var b = v[tris[i + 1]]; var c = v[tris[i + 2]]; if (Vector3.Dot(Vector3.Cross(b - a, c - a), a) < 0) { int x = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = x; } }
            mesh.triangles = tris; mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }

        static Texture2D Detail()
        {
            if (detail) return detail; int S = 256; detail = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var px = new Color32[S * S]; var r = new System.Random(3);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float nz = .5f + .22f * Noise(x * .055f, y * .055f, 7) + .14f * Noise(x * .19f, y * .19f, 9) + .06f * Noise(x * .6f, y * .6f, 4);
                    byte g = (byte)Mathf.Clamp(nz * 255, 0, 255); px[y * S + x] = new Color32(g, g, g, 255);
                }
            detail.SetPixels32(px); detail.Apply(true); return detail;
        }

        static float Hash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 1442695041; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float Noise(float x, float y, int s)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            float a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy) * 2 - 1;
        }

        /* Fugen: dezent statt knallig – helle Stahlfuge an der Basis, warmes Bernstein an putzbaren Parzellen, Rot bei Meteoritenstaub */
        static Color CleanLine => LunarCleanup.Core.Game.LineColor; /* Fabian 01.11.: Farbe einstellbar (Einstellungen) */
        static readonly Color BaseLine = new Color(.22f, .58f, 1f) /* Basis: kräftiges Blau, damit man das Basisgelände sofort erkennt */, FrontLine = new Color(1f, .74f, .38f), MeteorLine = new Color(1f, .42f, .3f);
        const float PAD = 1.5f / 64f; // Rand je Feld in der Textur, damit beim Verkleinern (Mipmaps) keine Nachbarfarben durchscheinen
        static float EdgeA(float ed, float w) => ed >= w ? 0 : ed <= w * .4f ? 1 : 1 - Mathf.SmoothStep(0, 1, (ed - w * .4f) / (w * .6f));

        // ---------- Sauberer Boden je Planet (Fabian 01.11.: geputzt soll der Planet strahlen, jeder anders) ----------
        class CStyle { public Color a, b, c, acc; public int kind; public float sparkle; }
        CStyle style;
        static Color Hx(string h) => GameData.Hex(h);
        static CStyle St(string a, string b, string c, string acc, int kind, float sparkle) => new CStyle { a = Hx(a), b = Hx(b), c = Hx(c), acc = Hx(acc), kind = kind, sparkle = sparkle };
        /// <summary>kind: 0 Regolith mit hellen Bändern, 1 Dünen, 2 Eis mit Rissen, 3 Schwefelflecken, 4 Moos/Algen, 5 Diamantfunkeln, 6 leuchtende Adern, 7 Tholin-Flecken</summary>
        static CStyle StyleFor(string pid)
        {
            switch (pid)
            {
                case "moon": return St("#8f96a3", "#c9ced8", "#eef1f6", "#ffffff", 0, .006f);
                case "mars": return St("#a8532e", "#c97a4a", "#e3a56f", "#5a2c20", 1, .002f);
                case "europa": return St("#cfe3f2", "#eaf4fb", "#ffffff", "#b5643e", 2, .01f);
                case "titan": return St("#b7742e", "#d79a4e", "#8a5426", "#5a3a1e", 1, .002f);
                case "io": return St("#e8d24a", "#f2e27a", "#d9a531", "#d0521c", 3, .003f);
                case "venus": return St("#b88a4a", "#d6ab68", "#8e6232", "#f2c674", 1, .004f);
                case "ganymed": return St("#7d7468", "#a99f90", "#ddd5c7", "#efe9dd", 0, .004f);
                case "enceladus": return St("#e2f0fc", "#ffffff", "#c8e2f8", "#3d93ff", 2, .014f);
                case "triton": return St("#d8a9a0", "#ecc8bc", "#f6e0d6", "#a96a66", 7, .006f);
                case "pluto": return St("#ead8bf", "#f6ecdc", "#c07a52", "#9a4a32", 7, .006f);
                case "eris": return St("#c8d0dc", "#eef2f8", "#ffffff", "#a6c8ff", 0, .012f);
                case "sedna": return St("#9a3a2e", "#c25a44", "#e08a66", "#6a2420", 1, .003f);
                case "proxima": return St("#6e3424", "#9a5236", "#c87a4e", "#ff8a4a", 1, .004f);
                case "trappist": return St("#3d8a96", "#5fb0b6", "#8fd4c8", "#2e6a80", 4, .004f);
                case "kepler": return St("#4f7a3a", "#6f9a4a", "#9cbf62", "#3a5a2a", 4, .002f);
                case "cancri": return St("#2a2a32", "#3e3e4a", "#5a5866", "#c8f6ff", 5, .035f);
                case "gliese": return St("#6f8e9a", "#9ab6c0", "#c6dce2", "#eaf7ff", 2, .008f);
                case "poltergeist": return St("#1f3a32", "#2e5446", "#3f6a58", "#5effd0", 6, .004f);
                default: return null;
            }
        }
        static float H3(int x, int y, int z) { unchecked { int h = x * 374761393 + y * 668265263 + z * 1442695041; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        /// <summary>Glatter 3D-Rauschwert 0..1 (gleich über Parzellengrenzen hinweg).</summary>
        static float N3(Vector3 p)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z); float fx = p.x - xi, fy = p.y - yi, fz = p.z - zi; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz);
            float L(int dx, int dy, int dz) => H3(xi + dx, yi + dy, zi + dz);
            float x00 = Mathf.Lerp(L(0, 0, 0), L(1, 0, 0), fx), x10 = Mathf.Lerp(L(0, 1, 0), L(1, 1, 0), fx), x01 = Mathf.Lerp(L(0, 0, 1), L(1, 0, 1), fx), x11 = Mathf.Lerp(L(0, 1, 1), L(1, 1, 1), fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }
        static Color CleanCol(CStyle st, Vector3 w, float nz, float nz2)
        {
            float n1 = N3(w * .06f), n2 = N3(w * .28f + new Vector3(31, 7, 13)), n3 = N3(w * 1.4f + new Vector3(5, 91, 3));
            var col = Color.Lerp(st.a, st.b, Mathf.SmoothStep(0, 1, n1 * .8f + n3 * .2f)); col = Color.Lerp(col, st.c, Mathf.Clamp01((n2 - .55f) * 2.2f));
            switch (st.kind)
            {
                case 0: { float band = Mathf.Abs(N3(w * .045f + new Vector3(3, 3, 3)) - .5f); if (band < .05f) col = Color.Lerp(col, st.acc, (1 - band / .05f) * .45f); col *= .94f + .1f * n3; break; } // helle Rillenbänder
                case 1: { float r = Mathf.Sin((w.x * .8f + w.z * .55f + w.y * .3f) * 2.4f + n2 * 7f) * .5f + .5f; col *= .86f + .22f * r; if (n1 < .3f) col = Color.Lerp(col, st.acc, (.3f - n1) * 1.6f); break; } // Dünenrippel, dunkle Basaltflecken
                case 2: { float cr = Mathf.Abs(N3(w * .2f + new Vector3(17, 2, 9)) - .5f), cr2 = Mathf.Abs(N3(w * .5f + new Vector3(4, 40, 1)) - .5f); if (cr < .018f) col = Color.Lerp(col, st.acc, (1 - cr / .018f) * .85f); else if (cr2 < .012f) col = Color.Lerp(col, st.acc, (1 - cr2 / .012f) * .4f); break; } // Eis mit farbigen Rissen
                case 3: { float sp = N3(w * .15f + new Vector3(8, 8, 8)); if (sp > .66f) col = Color.Lerp(col, st.acc, Mathf.Clamp01((sp - .66f) * 5)); if (n3 > .86f) col = Color.Lerp(col, new Color(.12f, .1f, .08f), (n3 - .86f) * 6); float g = N3(w * .09f + new Vector3(60, 1, 1)); if (g < .22f) col = Color.Lerp(col, new Color(.55f, .75f, .3f), (.22f - g) * 2.5f); break; } // Schwefel: orange Flecken, schwarze Schlote, grüne Stellen
                case 4: { float tuft = N3(w * 2.2f); col *= .85f + .3f * tuft; if (n2 > .7f) col = Color.Lerp(col, st.acc, (n2 - .7f) * 2.5f); break; } // Moos/Algen
                case 5: { col *= .9f + .2f * n3; break; } // Graphit (Funkeln unten)
                case 6: { float v = Mathf.Abs(N3(w * .18f + new Vector3(2, 9, 4)) - .5f); if (v < .03f) col = Color.Lerp(col, st.acc * 1.4f, (1 - v / .03f)); break; } // leuchtende Adern
                case 7: { if (n1 > .62f) col = Color.Lerp(col, st.acc, Mathf.Clamp01((n1 - .62f) * 3)); float cell = N3(w * .7f); col *= .92f + .14f * Mathf.Abs(cell - .5f) * 2; break; } // Tholin-Flecken, Grübchen
            }
            if (st.sparkle > 0) { var q = w * 6f; if (H3(Mathf.FloorToInt(q.x), Mathf.FloorToInt(q.y), Mathf.FloorToInt(q.z)) > 1 - st.sparkle) col = Color.Lerp(col, st.kind == 5 ? st.acc * 1.3f : Color.white, .75f); } // Glitzern
            return col;
        }

        // ---------- Relief: kleine Einschläge und Steinchen (nur Optik; große Krater/Hügel stecken im Gelände selbst) ----------
        readonly Dictionary<int, Vector4[]> craters = new Dictionary<int, Vector4[]>();
        Vector4[] Craters(Cell cl)
        {
            if (craters.TryGetValue(cl.id, out var a)) return a;
            if (cl.IsBase || cl.Wet) { a = new Vector4[0]; craters[cl.id] = a; return a; }
            var r = new System.Random(cl.id * 7919 + 13); float H = cl.H; int n = Mathf.Clamp(Mathf.RoundToInt(H * H / 7f), 1, 8);
            a = new Vector4[n];
            for (int i = 0; i < n; i++)
            {
                float rad = .25f + Mathf.Pow((float)r.NextDouble(), 3f) * Mathf.Min(2f, H * .4f); float ang = (float)r.NextDouble() * 6.2832f, dist = Mathf.Sqrt((float)r.NextDouble()) * Mathf.Max(0, H * .82f - rad * .6f);
                a[i] = new Vector4(Mathf.Cos(ang) * dist, Mathf.Sin(ang) * dist, rad, rad * (.1f + .07f * (float)r.NextDouble()));
            }
            craters[cl.id] = a; return a;
        }
        /// <summary>Reliefhöhe in Metern an (x, y) der Parzelle.</summary>
        static float Relief(Vector4[] crs, float x, float y, float ed, float edgeW)
        {
            float h = .035f * Noise(x * 2.3f, y * 2.3f, 21) + .018f * Noise(x * 6.1f, y * 6.1f, 23);
            foreach (var c in crs)
            {
                float dx = x - c.x, dy = y - c.y, d2 = dx * dx + dy * dy, r2 = c.z * c.z * 2.6f; if (d2 > r2) continue;
                float q = Mathf.Sqrt(d2) / c.z; h += q < 1 ? c.w * (q * q - 1) : 0; h += c.w * .55f * Mathf.Exp(-(q - 1) * (q - 1) / .07f);
            }
            return h * Mathf.SmoothStep(0, 1, Mathf.Clamp01(ed / (edgeW * 3f)));
        }
        static float PanelRelief(int id, float x, float y)
        {
            float gx = Mathf.Abs(Mathf.Repeat(x + id * .37f, 2.4f) - 1.2f), gy = Mathf.Abs(Mathf.Repeat(y, 2.4f) - 1.2f); float g = Mathf.Max(gx, gy);
            return g > 1.12f ? -.03f : g > 1.02f ? -.03f * (g - 1.02f) / .1f : .004f * Noise(x * 3f, y * 3f, id);
        }
        List<int>[] chunkIds;
        Color32[] NormalPixels(List<int> ids, int per)
        {
            int size = per * NT; var px = new Color32[size * size]; var hgt = new float[(NT + 2) * (NT + 2)];
            for (int i = 0; i < size * size; i++) px[i] = new Color32(128, 128, 255, 255);
            foreach (int id in ids)
            {
                var cl = pd.cells[id]; int ti = tileOf[id], ox = (ti % per) * NT, oy = (ti / per) * NT; float H = cl.H, edgeW = Mathf.Max(.13f, H * .024f); var crs = Craters(cl); int Kc = cl.poly.Length;
                if (cl.Wet) continue; float step = 2 * H / (1 - 2 * PAD) / NT; // Meter je Pixel
                for (int py = -1; py <= NT; py++) for (int pxi = -1; pxi <= NT; pxi++)
                    {
                        float y = -H + ((py + .5f) / NT - PAD) / (1 - 2 * PAD) * 2 * H, x = -H + ((pxi + .5f) / NT - PAD) / (1 - 2 * PAD) * 2 * H; float ed = 1e9f;
                        for (int k = 0; k < Kc; k++) { var a = cl.poly[k]; var b = cl.poly[(k + 1) % Kc]; float ex = b.x - a.x, ey = b.y - a.y, d = (ex * (y - a.y) - ey * (x - a.x)) / Mathf.Sqrt(ex * ex + ey * ey); if (d < ed) ed = d; }
                        hgt[(py + 1) * (NT + 2) + pxi + 1] = cl.IsBase ? PanelRelief(id, x, y) : Relief(crs, x, y, ed, edgeW);
                    }
                for (int py = 0; py < NT; py++) for (int pxi = 0; pxi < NT; pxi++)
                    {
                        int c = (py + 1) * (NT + 2) + pxi + 1; float dx = (hgt[c + 1] - hgt[c - 1]) / (2 * step), dy = (hgt[c + NT + 2] - hgt[c - NT - 2]) / (2 * step);
                        var nn = new Vector3(-dx * 1.25f, -dy * 1.25f, 1).normalized;
                        px[(oy + py) * size + ox + pxi] = new Color32((byte)Mathf.Clamp((nn.x * .5f + .5f) * 255, 0, 255), (byte)Mathf.Clamp((nn.y * .5f + .5f) * 255, 0, 255), (byte)Mathf.Clamp((nn.z * .5f + .5f) * 255, 0, 255), 255);
                    }
            }
            /* linear (keine Gamma-Umrechnung); R=x, G=y, B=z, A=1: passt zu beiden Normal-Map-Formaten (RGB und AG) */
            return px;
        }


        public void PaintCell(int id)
        {
            var cl = pd.cells[id]; var ch = chunks[cl.chunk]; if (ch == null) return;
            int ti = tileOf[id], ox = (ti % ch.per) * TILE, oy = (ti / ch.per) * TILE, G = GameData.GRID;
            bool clean = ps.clean[id], isBase = cl.IsBase, front = ps.front.Contains(id);
            ps.act.TryGetValue(id, out var act);
            var D = GameData.Dirt[cl.type]; var K = GameData.Kinds[cl.kind];
            Color c0 = th.clean.Length > 0 ? th.clean[0] : Color.gray, c1 = th.clean.Length > 1 ? th.clean[1] : c0, c2 = th.clean.Length > 2 ? th.clean[2] : c0;
            Color b0 = th.baseC.Length > 0 ? th.baseC[0] : Color.gray, b1 = th.baseC.Length > 1 ? th.baseC[1] : b0;
            Color kindTint = K.key == "rich" ? new Color(1.08f, 1.02f, .85f) : K.key == "industrial" ? new Color(.82f, .82f, .86f) : K.key == "anomaly" ? new Color(.95f, .85f, 1.12f) : Color.white;
            float H = cl.H; int Kc = cl.poly.Length; float edgeW = Mathf.Max(.13f, H * .024f); var crs = Craters(cl); bool hasF = pd.HasFeatures(id); Color fGlow = Color.black;
            for (int py = 0; py < TILE; py++)
            {
                float y = -H + ((py + .5f) / TILE - PAD) / (1 - 2 * PAD) * 2 * H;
                for (int pxi = 0; pxi < TILE; pxi++)
                {
                    float x = -H + ((pxi + .5f) / TILE - PAD) / (1 - 2 * PAD) * 2 * H;
                    float ed = 1e9f;
                    for (int k = 0; k < Kc; k++) { var a = cl.poly[k]; var b = cl.poly[(k + 1) % Kc]; float ex = b.x - a.x, ey = b.y - a.y, d = (ex * (y - a.y) - ey * (x - a.x)) / Mathf.Sqrt(ex * ex + ey * ey); if (d < ed) ed = d; }
                    Color col;
                    if (isBase)
                    {
                        float gx = Mathf.Abs(Mathf.Repeat(x + id * .37f, 2.4f) - 1.2f), gy = Mathf.Abs(Mathf.Repeat(y, 2.4f) - 1.2f);
                        int pid0 = Mathf.FloorToInt((x + id * .37f) / 2.4f) * 31 + Mathf.FloorToInt(y / 2.4f); col = Color.Lerp(b0, b1, .5f + .14f * Noise(x * .3f, y * .3f, id) + .1f * (Hash(pid0, id, 5) - .5f)) * (.97f + .05f * Noise(x * 1.7f, y * 1.7f, 2)); /* ruhige Platten, jede leicht anders getönt */ if (gx > 1.12f || gy > 1.12f) col *= .84f; else if (gx > 1.02f || gy > 1.02f) col *= 1.06f; // Fuge mit hellerer Kante
                        float ea = EdgeA(ed, edgeW * 1.5f); if (ea > 0) col = Color.Lerp(col, BaseLine * 1.15f, ea * .92f);
                    }
                    else
                    {
                        float g;
                        if (act == null) g = clean ? 0 : 1; /* Meteoritenstaub: auch saubere Parzellen können wieder Schmutz haben */
                        else
                        {
                            float fx = (x + H) / cl.cs - .5f, fy = (y + H) / cl.cs - .5f; int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, G - 1), iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, G - 1);
                            int jx = Mathf.Min(G - 1, ix + 1), jy = Mathf.Min(G - 1, iy + 1); float tx = Mathf.Clamp01(fx - ix), ty = Mathf.Clamp01(fy - iy);
                            g = Mathf.Lerp(Mathf.Lerp(act.grid[iy * G + ix], act.grid[iy * G + jx], tx), Mathf.Lerp(act.grid[jy * G + ix], act.grid[jy * G + jx], tx), ty);
                            g = Mathf.SmoothStep(0, 1, Mathf.Clamp01(g * 1.15f));
                        }
                        float nz = Noise(x * .9f + id, y * .9f, 11), nz2 = Noise(x * 2.7f, y * 2.7f + id, 5);
                        Color cleanC = style != null ? CleanCol(style, cl.c * pd.R + cl.e1 * x + cl.e2 * y, nz, nz2) : Color.Lerp(Color.Lerp(c1, c0, .5f + .5f * nz), c2, Mathf.Clamp01(nz2 - .55f) * 1.5f);
                        Color dirtC = Color.Lerp(D.darkC, D.baseC, .5f + .5f * nz); if (nz2 > .45f) dirtC = Color.Lerp(dirtC, D.lightC, (nz2 - .45f) * 1.6f);
                        dirtC *= kindTint; dirtC *= .93f + .14f * Noise(x * 2.1f, y * 2.1f, 3); /* weiche Körnung statt Pixelrauschen */
                        if (cl.Wet)
                        {
                            // See: Wellenmuster in der Farbe der Flüssigkeit, hellerer Uferstreifen, Schmutz schwimmt als Film darauf
                            var L = GameData.Liq[cl.liq]; float wv = .5f + .5f * Mathf.Sin(x * 1.7f + nz * 3f) * Mathf.Sin(y * 1.3f - nz2 * 2f);
                            Color liqC = Color.Lerp(L.deep, L.col, .35f + .5f * wv + .15f * nz2); float shore = Mathf.Clamp01(1 - (ed - edgeW) / (H * .12f));
                            if (wv > .88f) liqC = Color.Lerp(liqC, Color.white, (wv - .88f) * 2.5f); // Glanzlichter auf den Wellen
                            cleanC = Color.Lerp(liqC, cleanC, shore * .8f); dirtC = Color.Lerp(liqC, dirtC, .38f);
                        }
                        col = Color.Lerp(cleanC, dirtC, g);
                        if (!cl.Wet) { float rh = Relief(crs, x, y, ed, edgeW); col *= 1 + Mathf.Clamp(rh * 1.8f, -.16f, .1f); } // Kraterböden dunkler, Ränder heller
                        float ea = EdgeA(ed, edgeW); float fa0 = 0;
                        if (hasF && pd.FeaturePaint(id, pd.ToDir(cl, x, y), out var fc, out var fa, out var fg)) { col = Color.Lerp(col, fc, fa * (1 - g * .55f)); fGlow = fg * (1 - g * .7f); fa0 = fa; } else fGlow = Color.black;
                        ea *= 1 - fa0; /* keine Parzellenfuge quer über Vulkan, Berg oder Becken */
                        if (ea > 0) col = clean && act == null ? (CleanLine.a > 0 ? Color.Lerp(col, CleanLine, .75f * ea) : col * (1 - .12f * ea)) : clean ? Color.Lerp(col, MeteorLine, .5f * ea) : front ? Color.Lerp(col, FrontLine, .55f * ea) : col * (1 - .2f * ea);
                        if ((py & 1) == 0 && (pxi & 1) == 0) { float gl = clean && act == null ? ea : 0; int gx = (ox + pxi) / 2, gy = (oy + py) / 2; if (gx < ch.gs && gy < ch.gs) ch.gpx[gy * ch.gs + gx] = new Color(Mathf.Min(1, CleanLine.r * gl + fGlow.r), Mathf.Min(1, CleanLine.g * gl + fGlow.g), Mathf.Min(1, CleanLine.b * gl + fGlow.b), 1); }
                    }
                    col.a = 1; ch.px[(oy + py) * ch.size + ox + pxi] = col;
                }
            }
            ch.dirty = true;
        }

        public void Tick()
        {
            if (ps == null) return;
            if (ps.dirtyCells.Count > 0) { foreach (int id in ps.dirtyCells) PaintCell(id); ps.dirtyCells.Clear(); }
            foreach (var ch in chunks) if (ch != null && ch.dirty) { ch.tex.SetPixels32(ch.px); ch.tex.Apply(true); ch.gtex.SetPixels32(ch.gpx); ch.gtex.Apply(true); ch.dirty = false; }
            if (Mathf.Abs(GlowK - glowSet) > .02f) { glowSet = GlowK; var ec = Color.white * (.12f + 1.6f * GlowK); foreach (var ch in chunks) if (ch != null) ch.mat.SetColor("_EmissionColor", ec); }
        }

        /// <summary>Strahl gegen die Planetenkugel; liefert die Richtung des Treffpunkts.</summary>
        public static bool RayHit(Ray ray, float R, out Vector3 dir)
        {
            dir = Vector3.zero; var o = ray.origin; var d = ray.direction; float b = Vector3.Dot(o, d), c = o.sqrMagnitude - R * R, disc = b * b - c;
            if (disc < 0) return false; float t = -b - Mathf.Sqrt(disc); if (t < 0) return false; dir = (o + d * t).normalized; return true;
        }
        /// <summary>Strahl gegen das Gelände (Hügel/Krater): zweimal gegen die Kugel in der Geländehöhe nachrechnen.</summary>
        public static bool RayHit(Ray ray, PlanetData pd, out Vector3 dir)
        {
            if (!RayHit(ray, pd.R, out dir)) return false;
            for (int it = 0; it < 2; it++) { float h = pd.Height(dir); if (Mathf.Abs(h) < .02f) break; if (!RayHit(ray, pd.R + h, out var d2)) break; dir = d2; }
            return true;
        }
    }
}
