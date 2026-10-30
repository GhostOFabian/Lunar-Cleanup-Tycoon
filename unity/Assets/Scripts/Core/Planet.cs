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
        public int id, ring, type, kind, chunk;
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

        public static PlanetData Get(string pid)
        {
            if (Cache.TryGetValue(pid, out var pd)) return pd;
            pd = Build(pid); Cache[pid] = pd; return pd;
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

        static PlanetData Build(string pid)
        {
            var P = GameData.Planets[pid]; var CD = GameData.Cells[pid]; int n = P.freq;
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
                var cl = pd.cells[i]; cl.type = CD.type[i]; cl.kind = CD.kind[i]; cl.ring = CD.ring[i];
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
        public float Arc(Vector3 a, Vector3 b) => Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1, 1)) * R;
        public Vector3 GridDir(Cell cl, int i) { int G = GameData.GRID; return ToDir(cl, -cl.H + ((i % G) + .5f) * cl.cs, -cl.H + ((i / G) + .5f) * cl.cs); }
        public float CellArea(Cell cl) => cl.nIn * cl.cs * cl.cs;
    }
}
