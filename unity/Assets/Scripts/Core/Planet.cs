// Planetenaufbau 1:1 wie im Browser (geodesic / planetData): Goldberg-Polyeder aus Sechsecken und 12 Fünfecken.
// Reihenfolge der Zellen, Nachbarn, Ecken und das 32x32-Raster je Parzelle entsprechen exakt dem Prototyp;
// Sorte, Art, Ring, Basiszelle und Startparzelle kommen aus den exportierten Daten.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class Cell
    {
        public int id, ring, type, kind, chunk, liq = -1; // liq: Index in GameData.Liq, -1 = festes Land
        public bool Wet => liq >= 0;
        public Vector3 c, e1, e2;
        public Vector2[] poly;
        public float H, cs;
        public bool[] inside;
        public int nIn;
        public int[] nb;
        public bool IsBase => kind == 0;
    }

    public class PlanetData
    {
        public string pid;
        public float R;
        public int N, baseCell, start;
        public Cell[] cells;
        public Vector3[] faceCenters;
        public bool verified;

        static readonly Dictionary<string, PlanetData> Cache = new Dictionary<string, PlanetData>();

        /// <summary>Planetendaten in der Größe, die der Spielstand für diesen Planeten festhält (alte Stände behalten ihre alte Größe).</summary>
        public static PlanetData Get(string pid) => GetKey(pid, GameData.SizeKey(pid, Game.FreqOf(pid)));
        public static PlanetData GetKey(string pid, string key)
        {
            if (Cache.TryGetValue(key, out var pd)) return pd;
            pd = Build(pid, key); Cache[key] = pd; return pd;
        }

        // ---------- Vektorrechnung in double, damit die Zellreihenfolge exakt wie im Browser entsteht ----------
        struct V { public double x, y, z; public V(double a, double b, double c) { x = a; y = b; z = c; } }
        static V Add(V a, V b) => new V(a.x + b.x, a.y + b.y, a.z + b.z);
        static V Sub(V a, V b) => new V(a.x - b.x, a.y - b.y, a.z - b.z);
        static V Sc(V a, double s) => new V(a.x * s, a.y * s, a.z * s);
        static double Dot(V a, V b) => a.x * b.x + a.y * b.y + a.z * b.z;
        static V Cross(V a, V b) => new V(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        static V Norm(V a) { double l = Math.Sqrt(Dot(a, a)); return l > 0 ? Sc(a, 1 / l) : a; }
        static long Rnd(double v) => (long)Math.Floor(v + 0.5); // wie Math.round in JavaScript
        static Vector3 F(V v) => new Vector3((float)v.x, (float)v.y, (float)v.z);

        static PlanetData Build(string pid, string sizeKey)
        {
            var P = GameData.Planets[pid]; var CD = GameData.Cells[sizeKey]; int n = CD.freq > 0 ? CD.freq : P.freq;
            double t = (1 + Math.Sqrt(5)) / 2;
            var IV = new[] { new V(-1, t, 0), new V(1, t, 0), new V(-1, -t, 0), new V(1, -t, 0), new V(0, -1, t), new V(0, 1, t), new V(0, -1, -t), new V(0, 1, -t), new V(t, 0, -1), new V(t, 0, 1), new V(-t, 0, -1), new V(-t, 0, 1) }.Select(Norm).ToArray();
            int[][] IF = { new[] { 0, 11, 5 }, new[] { 0, 5, 1 }, new[] { 0, 1, 7 }, new[] { 0, 7, 10 }, new[] { 0, 10, 11 }, new[] { 1, 5, 9 }, new[] { 5, 11, 4 }, new[] { 11, 10, 2 }, new[] { 10, 7, 6 }, new[] { 7, 1, 8 }, new[] { 3, 9, 4 }, new[] { 3, 4, 2 }, new[] { 3, 2, 6 }, new[] { 3, 6, 8 }, new[] { 3, 8, 9 }, new[] { 4, 9, 5 }, new[] { 2, 4, 11 }, new[] { 6, 2, 10 }, new[] { 8, 6, 7 }, new[] { 9, 8, 1 } };
            var verts = new List<V>(); var map = new Dictionary<string, int>(); var tris = new List<int[]>();
            foreach (var f in IF)
            {
                V A = IV[f[0]], B = IV[f[1]], C = IV[f[2]]; var idx = new int[n + 1][];
                for (int i = 0; i <= n; i++)
                {
                    idx[i] = new int[n + 1 - i];
                    for (int j = 0; j <= n - i; j++)
                    {
                        int k = n - i - j;
                        var p = Norm(new V((A.x * k + B.x * i + C.x * j) / n, (A.y * k + B.y * i + C.y * j) / n, (A.z * k + B.z * i + C.z * j) / n));
                        string key = Rnd(p.x * 1e6) + "_" + Rnd(p.y * 1e6) + "_" + Rnd(p.z * 1e6);
                        if (!map.TryGetValue(key, out int id)) { id = verts.Count; verts.Add(p); map[key] = id; }
                        idx[i][j] = id;
                    }
                }
                for (int i = 0; i < n; i++) for (int j = 0; j < n - i; j++) { tris.Add(new[] { idx[i][j], idx[i + 1][j], idx[i][j + 1] }); if (i + j < n - 1) tris.Add(new[] { idx[i + 1][j], idx[i + 1][j + 1], idx[i][j + 1] }); }
            }
            int NV = verts.Count; var inc = new List<int>[NV]; var nbs = new List<int>[NV];
            for (int i = 0; i < NV; i++) { inc[i] = new List<int>(); nbs[i] = new List<int>(); }
            for (int ti = 0; ti < tris.Count; ti++)
            {
                var tr = tris[ti];
                for (int q = 0; q < 3; q++)
                {
                    inc[tr[q]].Add(ti);
                    int a = tr[(q + 1) % 3], b = tr[(q + 2) % 3];
                    if (!nbs[tr[q]].Contains(a)) nbs[tr[q]].Add(a);
                    if (!nbs[tr[q]].Contains(b)) nbs[tr[q]].Add(b);
                }
            }
            var tc = tris.Select(tr => Norm(Add(Add(verts[tr[0]], verts[tr[1]]), verts[tr[2]]))).ToArray();
            double R = Math.Sqrt(NV * 140 / (4 * Math.PI));
            var pd = new PlanetData { pid = pid, N = NV, R = (float)R, cells = new Cell[NV] };
            pd.faceCenters = IF.Select(f => F(Norm(Add(Add(IV[f[0]], IV[f[1]]), IV[f[2]])))).ToArray();
            int G = GameData.GRID;
            for (int i = 0; i < NV; i++)
            {
                V c = verts[i]; V r0 = tc[inc[i][0]]; V rf = Norm(Sub(r0, Sc(c, Dot(r0, c)))); V rf2 = Cross(c, rf);
                var corners = inc[i].Select(ti => tc[ti]).OrderBy(p => Math.Atan2(Dot(p, rf2), Dot(p, rf))).ToList();
                V e1 = Norm(Sub(corners[0], Sc(c, Dot(corners[0], c)))); V e2 = Cross(c, e1);
                var poly = new Vector2[corners.Count];
                for (int k = 0; k < corners.Count; k++) { double tt = R / Dot(corners[k], c); poly[k] = new Vector2((float)(Dot(corners[k], e1) * tt), (float)(Dot(corners[k], e2) * tt)); }
                double H = 0; foreach (var q in poly) H = Math.Max(H, Math.Sqrt(q.x * (double)q.x + q.y * (double)q.y)); H *= 1.02;
                double cs = 2 * H / G; var inside = new bool[G * G]; int nIn = 0;
                for (int gy = 0; gy < G; gy++) for (int gx = 0; gx < G; gx++)
                    {
                        double x = -H + (gx + .5) * cs, y = -H + (gy + .5) * cs; bool ok = true;
                        for (int k = 0; k < poly.Length; k++)
                        {
                            var a = poly[k]; var b = poly[(k + 1) % poly.Length]; double ex = b.x - a.x, ey = b.y - a.y;
                            if ((ex * (y - a.y) - ey * (x - a.x)) / Math.Sqrt(ex * ex + ey * ey) < .08) { ok = false; break; }
                        }
                        if (ok) { inside[gy * G + gx] = true; nIn++; }
                    }
                pd.cells[i] = new Cell { id = i, c = F(c), e1 = F(e1), e2 = F(e2), poly = poly, H = (float)H, cs = (float)cs, inside = inside, nIn = nIn, nb = nbs[i].ToArray() };
            }
            // Zuordnung aus dem Browser (Sorte, Art, Ring, Basis, Start)
            pd.verified = CD.N == NV && (pd.cells[0].c - CD.c0).sqrMagnitude < 1e-8f && (pd.cells[Math.Min(7, NV - 1)].c - CD.c7).sqrMagnitude < 1e-8f;
            if (!pd.verified) Debug.LogWarning("Lunar Cleanup: Planetenaufbau von " + pid + " weicht vom Browser ab (N " + NV + " / " + CD.N + ")");
            int baseKind = GameData.Kinds.FindIndex(k => k.key == "base");
            for (int i = 0; i < NV && i < CD.N; i++)
            {
                var cl = pd.cells[i]; cl.type = CD.type[i]; cl.kind = CD.kind[i]; cl.ring = CD.ring[i]; cl.liq = CD.liq != null && i < CD.liq.Length ? CD.liq[i] : -1;
                float best = -2; for (int f = 0; f < pd.faceCenters.Length; f++) { float d = Vector3.Dot(cl.c, pd.faceCenters[f]); if (d > best) { best = d; cl.chunk = f; } }
            }
            if (baseKind != 0) Debug.LogWarning("Lunar Cleanup: Art 'base' ist nicht Index 0");
            pd.baseCell = CD.baseCell; pd.start = CD.start;
            return pd;
        }

        // ---------- Umrechnungen ----------
        public Vector3 ToDir(Cell cl, float x, float y) => (cl.c * R + cl.e1 * x + cl.e2 * y).normalized;
        public Vector2 ToLocal(Cell cl, Vector3 d)
        {
            float dd = Vector3.Dot(d, cl.c);
            if (dd <= .05f)
            {
                // Rückseite des Planeten: weit weg, aber in der richtigen Richtung (sonst verlassen Fahrzeuge die Basis immer zur selben Ecke und kreisen)
                var fd = new Vector2(Vector3.Dot(d, cl.e1), Vector3.Dot(d, cl.e2)); return fd.sqrMagnitude > 1e-8f ? fd.normalized * 1e4f : new Vector2(1e4f, 0);
            }
            float t = R / dd; return new Vector2(Vector3.Dot(d, cl.e1) * t, Vector3.Dot(d, cl.e2) * t);
        }
        public Vector3 PlanDir(float x, float y) => ToDir(cells[baseCell], x, y);
        public Vector3 PlanDir(Vector2 p) => ToDir(cells[baseCell], p.x, p.y);
        public Vector2 PlanLocal(Vector3 d) => ToLocal(cells[baseCell], d);
        public int CellOf(Vector3 d, int guess = -1)
        {
            int cur = guess >= 0 && guess < N ? guess : baseCell; float best = Vector3.Dot(cells[cur].c, d);
            for (int it = 0; it < 500; it++)
            {
                int nx = -1; foreach (int b in cells[cur].nb) { float v = Vector3.Dot(cells[b].c, d); if (v > best) { best = v; nx = b; } }
                if (nx < 0) break; cur = nx;
            }
            return cur;
        }
        // ---------- Gelände: sanfte Hügel und Meteoritenkrater (nur Optik – die Simulation rechnet weiter auf der Kugel) ----------
        // Die Basis (bis ~31 m) und die Außenlager-Plätze bleiben flach, damit Gebäude, Bänder und Pipelines gerade stehen.
        Vector3[] hk; float[] hf, hp, cth; Vector4[] cr; int[][] cc; int lastHC = -1; float hAmp; List<Vector3> flat = new List<Vector3>();
        void InitTerrain()
        {
            int seed = 7; foreach (char ch in pid) seed = seed * 31 + ch; var rnd = new System.Random(seed & 0x7fffffff);
            System.Func<float> r = () => (float)rnd.NextDouble();
            hk = new Vector3[5]; hf = new float[5]; hp = new float[5];
            for (int i = 0; i < 5; i++) { hk[i] = new Vector3(r() - .5f, r() - .5f, r() - .5f).normalized; hf[i] = R / (14f + r() * 30f) * (i < 2 ? .6f : 1.3f); hp[i] = r() * 6.283f; }
            hAmp = Mathf.Clamp(.45f + R * .006f, .5f, 1.5f);
            // Krater: Mulde mit aufgeworfenem Rand; ihre Zahl wächst mit der Planetengröße
            int nc = Mathf.Clamp(N / 14, 12, 160); cr = new Vector4[nc];
            for (int i = 0; i < nc; i++) { var d = new Vector3(r() - .5f, r() - .5f, r() - .5f).normalized; float rad = 3.5f + Mathf.Pow(r(), 2.2f) * 13f; cr[i] = new Vector4(d.x, d.y, d.z, rad); }
            cth = new float[nc]; for (int i = 0; i < nc; i++) cth[i] = Mathf.Cos(Mathf.Min(Mathf.PI, cr[i].w * 1.8f / R));
            // Krater je Zelle vorsortieren, damit Height() nicht alle Krater prüfen muss
            cc = new int[N][]; var tmp = new List<int>();
            for (int z = 0; z < N; z++) { tmp.Clear(); var cz = cells[z]; for (int i = 0; i < nc; i++) { float lim = (cr[i].w * 1.8f + cz.H * 1.4f) / R; if (Vector3.Dot(cz.c, new Vector3(cr[i].x, cr[i].y, cr[i].z)) > Mathf.Cos(Mathf.Min(Mathf.PI, lim))) tmp.Add(i); } cc[z] = tmp.ToArray(); }
            try { foreach (int c in Game.OutpostSites(pid)) Flatten(cells[c].c, 7f); } catch (Exception) { }
        }
        /// <summary>Flache Stelle (z. B. Außenlager), Radius in Metern.</summary>
        public void Flatten(Vector3 dir, float radius) { flat.Add(dir.normalized * radius); }
        public float Height(Vector3 dir, int cell = -1)
        {
            if (hk == null) InitTerrain(); dir.Normalize();
            var b = PlanLocal(dir); float bd = b.magnitude; float k = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(33f, 46f, bd > 1e3f ? 1e3f : bd)); if (k <= 0) return 0;
            float h = 0; for (int i = 0; i < hk.Length; i++) h += Mathf.Sin(Vector3.Dot(dir, hk[i]) * hf[i] + hp[i]) * (i < 2 ? 1f : .35f);
            h *= hAmp * .55f;
            if (cell < 0) { cell = CellOf(dir, lastHC); lastHC = cell; }
            foreach (int i in cc[cell])
            {
                var c = cr[i]; float cd = Vector3.Dot(dir, new Vector3(c.x, c.y, c.z)); if (cd < cth[i]) continue; float dist = Mathf.Acos(Mathf.Min(1, cd)) * R; float x = dist / c.w; if (x > 1.8f) continue;
                float depth = c.w * .09f; h += x < 1 ? depth * (x * x - 1) * .9f + depth * .25f * x * x * x * x : 0; h += depth * .45f * Mathf.Exp(-(x - 1) * (x - 1) / .06f);
            }
            foreach (var f in flat) { float fr = f.magnitude; float dd = Mathf.Acos(Mathf.Clamp(Vector3.Dot(dir, f / fr), -1, 1)) * R; if (dd < fr * 1.8f) k *= Mathf.SmoothStep(0, 1, Mathf.InverseLerp(fr, fr * 1.8f, dd)); }
            return h * k;
        }
        /// <summary>Punkt auf der Geländeoberfläche (plus Höhe darüber).</summary>
        public Vector3 Surf(Vector3 dir, float lift = 0, int cell = -1) { dir.Normalize(); return dir * (R + Height(dir, cell) + lift); }
        /// <summary>Gelände-Normale (für Licht und zum Ausrichten von Fahrzeugen).</summary>
        public Vector3 SurfNormal(Vector3 dir, int cell = -1)
        {
            dir.Normalize(); var t1 = Vector3.Cross(dir, Mathf.Abs(dir.y) < .9f ? Vector3.up : Vector3.right).normalized; var t2 = Vector3.Cross(dir, t1); float e = .6f / R;
            var p1 = Surf(dir + t1 * e, 0, cell) - Surf(dir - t1 * e, 0, cell); var p2 = Surf(dir + t2 * e, 0, cell) - Surf(dir - t2 * e, 0, cell); var n = Vector3.Cross(p1, p2).normalized; return Vector3.Dot(n, dir) < 0 ? -n : n;
        }
        public float Arc(Vector3 a, Vector3 b) => Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1, 1)) * R;
        public Vector3 GridDir(Cell cl, int i) { int G = GameData.GRID; return ToDir(cl, -cl.H + ((i % G) + .5f) * cl.cs, -cl.H + ((i / G) + .5f) * cl.cs); }
        public float CellArea(Cell cl) => cl.nIn * cl.cs * cl.cs;
    }
}
