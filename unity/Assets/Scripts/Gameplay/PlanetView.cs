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
        const int TILE = 64, SUB = 3;
        class Chunk { public Texture2D tex; public Color32[] px; public int per, size; public bool dirty; public Material mat; }
        PState ps; PlanetData pd; Theme th;
        readonly List<Chunk> chunks = new List<Chunk>();
        int[] tileOf; // Platz der Zelle in ihrer Textur
        static Texture2D detail;

        public void Build(PState state)
        {
            ps = state; pd = ps.pd; th = ps.P.theme;
            foreach (Transform c in transform) Destroy(c.gameObject); chunks.Clear();
            int F = pd.faceCenters.Length; var lists = new List<int>[F]; for (int i = 0; i < F; i++) lists[i] = new List<int>();
            foreach (var cl in pd.cells) lists[cl.chunk].Add(cl.id);
            tileOf = new int[pd.N];
            for (int f = 0; f < F; f++)
            {
                var ids = lists[f]; if (ids.Count == 0) { chunks.Add(null); continue; }
                int per = Mathf.CeilToInt(Mathf.Sqrt(ids.Count)), size = per * TILE;
                var ch = new Chunk { per = per, size = size, px = new Color32[size * size], tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 } };
                for (int i = 0; i < ids.Count; i++) tileOf[ids[i]] = i;
                var mesh = BuildMesh(ids, per);
                var go = new GameObject("Abschnitt " + f); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Hügel werfen keine langen schwarzen Schatten über den Planeten
                var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.SetTexture("_BaseMap", ch.tex); m.SetColor("_BaseColor", Color.white); m.SetFloat("_Smoothness", .05f); m.SetFloat("_Metallic", 0); m.SetFloat("_SpecularHighlights", 0); m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); m.SetFloat("_EnvironmentReflections", 0); m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); // kein Blenden bei flachem Sonnenstand
                m.SetTexture("_DetailAlbedoMap", Detail()); m.SetTextureScale("_DetailAlbedoMap", new Vector2(per * 9, per * 9)); m.SetFloat("_DetailAlbedoMapScale", .3f); m.EnableKeyword("_DETAIL_MULX2");
                mr.sharedMaterial = m; ch.mat = m;
                chunks.Add(ch);
                foreach (int id in ids) PaintCell(id);
                ch.tex.SetPixels32(ch.px); ch.tex.Apply(true); ch.dirty = false;
            }
            // Planetenkugel darunter, damit zwischen den Platten nie Lücken sichtbar werden
            var core = MeshKit.Ball(transform, MeshKit.Mat(th.baseC[0] * .5f, 0, .1f), Vector3.zero, (pd.R - 4f) * 2); core.name = "Kern"; // tief genug unter Tälern und Kratern
        }

        Mesh BuildMesh(List<int> ids, int per)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float inv = 1f / per, pad = .5f / TILE;
            foreach (int id in ids)
            {
                var cl = pd.cells[id]; int ti = tileOf[id]; float u0 = (ti % per) * inv, v0 = (ti / per) * inv;
                System.Func<Vector2, Vector2> UV = p => new Vector2(u0 + (pad + (p.x + cl.H) / (2 * cl.H) * (1 - 2 * pad)) * inv, v0 + (pad + (p.y + cl.H) / (2 * cl.H) * (1 - 2 * pad)) * inv);
                int K = cl.poly.Length;
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
            if (detail) return detail; int S = 128; detail = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S]; var r = new System.Random(3);
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float nz = .5f + .22f * Noise(x * .11f, y * .11f, 7) + .14f * Noise(x * .37f, y * .37f, 9) + (float)(r.NextDouble() - .5) * .08f;
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

        static readonly Color BaseLine = new Color(.31f, .46f, .9f), CleanLine = new Color(.36f, .92f, .68f), FrontLine = new Color(1f, .45f, .32f);

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
            float H = cl.H; int Kc = cl.poly.Length; float edgeW = Mathf.Max(.16f, H * .028f);
            for (int py = 0; py < TILE; py++)
            {
                float y = -H + (py + .5f) / TILE * 2 * H;
                for (int pxi = 0; pxi < TILE; pxi++)
                {
                    float x = -H + (pxi + .5f) / TILE * 2 * H;
                    float ed = 1e9f;
                    for (int k = 0; k < Kc; k++) { var a = cl.poly[k]; var b = cl.poly[(k + 1) % Kc]; float ex = b.x - a.x, ey = b.y - a.y, d = (ex * (y - a.y) - ey * (x - a.x)) / Mathf.Sqrt(ex * ex + ey * ey); if (d < ed) ed = d; }
                    Color col;
                    if (isBase)
                    {
                        float gx = Mathf.Abs(Mathf.Repeat(x + id * .37f, 2.4f) - 1.2f), gy = Mathf.Abs(Mathf.Repeat(y, 2.4f) - 1.2f);
                        col = Color.Lerp(b0, b1, .5f + .5f * Noise(x * .7f, y * .7f, id)); if (gx > 1.12f || gy > 1.12f) col *= .78f;
                        if (ed < edgeW) col = Color.Lerp(col, BaseLine, .85f);
                    }
                    else
                    {
                        float g;
                        if (clean) g = 0; else if (act == null) g = 1;
                        else
                        {
                            float fx = (x + H) / cl.cs - .5f, fy = (y + H) / cl.cs - .5f; int ix = Mathf.Clamp(Mathf.FloorToInt(fx), 0, G - 1), iy = Mathf.Clamp(Mathf.FloorToInt(fy), 0, G - 1);
                            int jx = Mathf.Min(G - 1, ix + 1), jy = Mathf.Min(G - 1, iy + 1); float tx = Mathf.Clamp01(fx - ix), ty = Mathf.Clamp01(fy - iy);
                            g = Mathf.Lerp(Mathf.Lerp(act.grid[iy * G + ix], act.grid[iy * G + jx], tx), Mathf.Lerp(act.grid[jy * G + ix], act.grid[jy * G + jx], tx), ty);
                            g = Mathf.SmoothStep(0, 1, Mathf.Clamp01(g * 1.15f));
                        }
                        float nz = Noise(x * .9f + id, y * .9f, 11), nz2 = Noise(x * 2.7f, y * 2.7f + id, 5);
                        Color cleanC = Color.Lerp(Color.Lerp(c1, c0, .5f + .5f * nz), c2, Mathf.Clamp01(nz2 - .55f) * 1.5f);
                        Color dirtC = Color.Lerp(D.darkC, D.baseC, .5f + .5f * nz); if (nz2 > .45f) dirtC = Color.Lerp(dirtC, D.lightC, (nz2 - .45f) * 1.6f);
                        dirtC *= kindTint; dirtC *= .92f + .16f * Noise(x * 5.3f, y * 5.3f, 3);
                        if (cl.Wet)
                        {
                            // See: Wellenmuster in der Farbe der Flüssigkeit, hellerer Uferstreifen, Schmutz schwimmt als Film darauf
                            var L = GameData.Liq[cl.liq]; float wv = .5f + .5f * Mathf.Sin(x * 1.7f + nz * 3f) * Mathf.Sin(y * 1.3f - nz2 * 2f);
                            Color liqC = Color.Lerp(L.deep, L.col, .35f + .5f * wv + .15f * nz2); float shore = Mathf.Clamp01(1 - (ed - edgeW) / (H * .12f));
                            if (wv > .88f) liqC = Color.Lerp(liqC, Color.white, (wv - .88f) * 2.5f); // Glanzlichter auf den Wellen
                            cleanC = Color.Lerp(liqC, cleanC, shore * .8f); dirtC = Color.Lerp(liqC, dirtC, .38f);
                        }
                        col = Color.Lerp(cleanC, dirtC, g);
                        if (ed < edgeW) col = clean ? Color.Lerp(col, CleanLine, .8f) : front ? Color.Lerp(col, FrontLine, .7f) : col * .72f;
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
            foreach (var ch in chunks) if (ch != null && ch.dirty) { ch.tex.SetPixels32(ch.px); ch.tex.Apply(true); ch.dirty = false; }
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
