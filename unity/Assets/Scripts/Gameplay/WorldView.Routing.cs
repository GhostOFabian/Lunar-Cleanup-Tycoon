// Bandführung in Unity (nur Optik, die Spiellogik rechnet weiter mit der Bandlänge aus dem Browser-Layout):
// Jedes Band verlässt ein Gebäude in der Mitte einer geraden Fläche, läuft rechtwinklig mit runden 90°-Kurven
// um alle Gebäude herum und führt Bänder mit gleichem Ziel (oder gleicher Quelle) zu einem Y zusammen.
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        readonly Dictionary<string, Vector2[]> routed = new Dictionary<string, Vector2[]>();
        string routedKey;
        const float GC = .5f; const int GN = 156; const float GOFF = GN * GC / 2; // Raster 0,5 m, ±39 m um die Basis

        struct Foot { public Vector2 min, max, c; public string id; }
        struct BPort { public string bid; public int face; public Vector2 p, n; }

        void RouteBelts()
        {
            var live = ps.lay.belts.Where(x => Game.BeltLive(ps, x)).ToList();
            string key = ps.pid + "|" + ps.sv.freq + "|" + string.Join(",", ps.bld.OrderBy(x => x)) + "|" + string.Join(",", live.Select(x => x.id));
            if (key == routedKey) return; routedKey = key; routed.Clear();
            // Grundflächen aus den echten Modellen (Planebene), Höhe 0,3–3,5 m
            var feet = new Dictionary<string, Foot>();
            foreach (var id in ps.bld)
            {
                if (!ps.lay.pos.ContainsKey(id)) continue; var T = TrisOf(id); var c = ps.lay.pos[id];
                Vector2 mn = new Vector2(1e9f, 1e9f), mx = -mn; int nPts = 0;
                foreach (var v in T) { float h = v.magnitude - pd.R; if (h < .3f || h > 3.5f) continue; var q = pd.PlanLocal(v.normalized); mn = Vector2.Min(mn, q); mx = Vector2.Max(mx, q); nPts++; }
                if (nPts == 0) { float f = GameData.Bld[id].fp * .7f; mn = c - Vector2.one * f; mx = c + Vector2.one * f; }
                feet[id] = new Foot { min = mn, max = mx, c = c, id = id };
            }
            // Hindernisraster: Gebäude (+0,7 m Abstand), Gruben der Entladestationen, Laternen
            var block = new bool[GN * GN];
            void Mark(Vector2 a, Vector2 b) { int x0 = Ix(a.x), x1 = Ix(b.x), y0 = Ix(a.y), y1 = Ix(b.y); for (int y = Mathf.Max(0, y0); y <= Mathf.Min(GN - 1, y1); y++) for (int x = Mathf.Max(0, x0); x <= Mathf.Min(GN - 1, x1); x++) block[y * GN + x] = true; }
            foreach (var f in feet.Values) Mark(f.min - Vector2.one * .7f, f.max + Vector2.one * .7f);
            foreach (var id in new[] { "u1", "u2", "u3" }) if (feet.ContainsKey(id) && GameData.Bld[id].st != null) for (int k = 0; k < GameData.Bld[id].st.docks; k++) { var dp = Game.DockPos(ps, id, k); var cp = ps.lay.pos[id]; var pp = cp + (dp - cp) * .72f; Mark(pp - Vector2.one * 1.4f, pp + Vector2.one * 1.4f); var vp = dp; Mark(vp - Vector2.one * 1.2f, vp + Vector2.one * 1.2f); }
            var lampIds = new[] { "hq", "roh", "u1", "u2", "u3", "prod", "presse", "sell", "werkstatt", "schmelze" }; int nl = 0;
            foreach (var id in lampIds) { if (!ps.bld.Contains(id) || !ps.lay.pos.ContainsKey(id) || nl >= 6) continue; nl++; var off = ps.lay.pos[id] + new Vector2(GameData.Bld[id].fp * .7f + .8f, GameData.Bld[id].fp * .7f + .8f); Mark(off - Vector2.one * .4f, off + Vector2.one * .4f); }
            var used = new Dictionary<string, int>();            // belegte Port-Plätze je Gebäude/Fläche
            var occ = new Dictionary<int, List<(string from, string to)>>(); // Rasterzellen schon verlegter Bänder
            var portOf = new Dictionary<string, (BPort a, BPort b)>();
            foreach (var b in live)
            {
                if (!feet.TryGetValue(b.from, out var FA) || !feet.TryGetValue(b.to, out var FB)) continue;
                var cands = new List<(BPort a, BPort b, bool shareA, bool shareB)>();
                // Y: denselben Port eines Bandes mit gleichem Ziel/gleicher Quelle mitbenutzen
                foreach (var kv in portOf)
                {
                    var o = live.First(x => x.id == kv.Key);
                    if (o.to == b.to) foreach (var pa in Ports(FA, FB.c, b.from, used, 2)) cands.Add((pa, kv.Value.b, false, true));
                    if (o.from == b.from) foreach (var pb in Ports(FB, FA.c, b.to, used, 2)) cands.Add((kv.Value.a, pb, true, false));
                }
                foreach (var pa in Ports(FA, FB.c, b.from, used, 2)) foreach (var pb in Ports(FB, FA.c, b.to, used, 2)) cands.Add((pa, pb, false, false));
                List<int> best = null; float bc = 1e9f; (BPort a, BPort b, bool, bool) bestC = default;
                foreach (var cnd in cands)
                {
                    var path = AStar(cnd.a.p + cnd.a.n * 1.1f, cnd.a.n, cnd.b.p + cnd.b.n * 1.1f, -cnd.b.n, block, occ, cnd.shareA ? b.from : null, cnd.shareB ? b.to : null, out float cost); // mitlaufen nur, wenn wirklich ein Y entsteht
                    if (path != null && cost < bc) { bc = cost; best = path; bestC = cnd; }
                }
                if (best == null) continue;
                var A = bestC.a; var B = bestC.b;
                if (!bestC.Item3) used[A.bid + A.face] = used.TryGetValue(A.bid + A.face, out var ua) ? ua + 1 : 1;
                if (!bestC.Item4) used[B.bid + B.face] = used.TryGetValue(B.bid + B.face, out var ub) ? ub + 1 : 1;
                portOf[b.id] = (A, B);
                // belegte Fläche: Bandzellen plus zwei Zellen ringsum (Bandbreite + Abstand), damit nichts eng nebeneinander läuft
                var mark = new HashSet<int>(); foreach (int cell in best) { int cx = cell % GN, cy = cell / GN; for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) { int x = cx + dx, y = cy + dy; if (x >= 0 && y >= 0 && x < GN && y < GN) mark.Add(y * GN + x); } }
                foreach (int cell in mark) { if (!occ.TryGetValue(cell, out var l)) { l = new List<(string, string)>(); occ[cell] = l; } l.Add((b.from, b.to)); }
                // Ecken der Rasterlinie → Polylinie; davor/dahinter gerade aus der Fläche heraus und bis ins Gebäude (wird an der Wand gekappt)
                var pts = new List<Vector2> { A.p - A.n * 1.6f, A.p };
                var cor = Corners(best); foreach (var q in cor) pts.Add(q); pts.Add(B.p); pts.Add(B.p - B.n * 1.6f);
                routed[b.id] = Round(Dedupe(pts), 1.1f);
            }
        }

        static int Ix(float v) => Mathf.FloorToInt((v + GOFF) / GC);
        static Vector2 Cc(int x, int y) => new Vector2((x + .5f) * GC - GOFF, (y + .5f) * GC - GOFF);

        /// <summary>Port-Kandidaten: Flächen, die zum Ziel zeigen, je ein freier Platz (Mitte, dann ±1 m).</summary>
        IEnumerable<BPort> Ports(Foot f, Vector2 toward, string id, Dictionary<string, int> used, int nFaces)
        {
            var faces = new List<(int k, Vector2 n, float score)>();
            var dir = (toward - f.c).normalized;
            Vector2[] ns = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
            for (int k = 0; k < 4; k++)
            {
                // Entladestationen: nicht die Seite der Andockplätze/Gruben
                if ((id == "u1" || id == "u2" || id == "u3"))
                {
                    var dp = (Game.DockPos(ps, id, 0) - ps.lay.pos[id]).normalized; if (Vector2.Dot(dp, ns[k]) > .6f) continue;
                }
                faces.Add((k, ns[k], Vector2.Dot(ns[k], dir)));
            }
            foreach (var fc in faces.OrderByDescending(x => x.score).Take(nFaces))
            {
                int u = used.TryGetValue(id + fc.k, out var uu) ? uu : 0; var n = fc.n; var t = new Vector2(-n.y, n.x);
                float half = Mathf.Abs(n.x) > .5f ? (f.max.y - f.min.y) / 2 : (f.max.x - f.min.x) / 2;
                float slot = u == 0 ? 0 : (u % 2 == 1 ? 1 : -1) * Mathf.Ceil(u / 2f) * 1.25f; if (Mathf.Abs(slot) > half - .5f) slot = 0;
                var mid = new Vector2(n.x > .5f ? f.max.x : n.x < -.5f ? f.min.x : (f.min.x + f.max.x) / 2, n.y > .5f ? f.max.y : n.y < -.5f ? f.min.y : (f.min.y + f.max.y) / 2);
                var pp = mid + t * slot; if (Mathf.Abs(n.x) > .5f) pp.y = Cc(0, Ix(pp.y)).y; else pp.x = Cc(Ix(pp.x), 0).x; // quer zur Fläche aufs Raster
                yield return new BPort { bid = id, face = fc.k, p = pp, n = n };
            }
        }

        /// <summary>A* mit Richtungszustand: Schritt 0,5, Abbiegen +3 m, neben Hindernissen +0,4, fremde Bänder kreuzen +2,5,
        /// auf Bändern mit gleichem Ziel/gleicher Quelle mitlaufen ist günstig (Y-Zusammenführung).</summary>
        List<int> AStar(Vector2 s, Vector2 sDir, Vector2 g, Vector2 gDir, bool[] block, Dictionary<int, List<(string, string)>> occ, string from, string to, out float cost)
        {
            cost = 1e9f; int sx = Ix(s.x), sy = Ix(s.y), gx = Ix(g.x), gy = Ix(g.y);
            if (sx < 1 || sy < 1 || gx < 1 || gy < 1 || sx >= GN - 1 || sy >= GN - 1 || gx >= GN - 1 || gy >= GN - 1) return null;
            int[] DX = { 1, -1, 0, 0 }, DY = { 0, 0, 1, -1 }; int Dir(Vector2 v) => Mathf.Abs(v.x) > Mathf.Abs(v.y) ? (v.x > 0 ? 0 : 1) : (v.y > 0 ? 2 : 3);
            int sd = Dir(sDir), gd = Dir(gDir); int NS = GN * GN * 4;
            var gs = new float[NS]; for (int i = 0; i < NS; i++) gs[i] = 1e9f; var prev = new int[NS];
            var open = new SortedSet<(float f, int st)>(); int s0 = (sy * GN + sx) * 4 + sd; gs[s0] = 0; prev[s0] = -1; open.Add((0, s0));
            int found = -1, guard = 0;
            while (open.Count > 0 && guard++ < 200000)
            {
                var cur = open.Min; open.Remove(cur); int st = cur.st, cell = st / 4, d = st % 4, x = cell % GN, y = cell / GN;
                if (x == gx && y == gy && d == gd) { found = st; break; }
                for (int nd = 0; nd < 4; nd++)
                {
                    if ((nd ^ 1) == d && (nd >> 1) == (d >> 1)) continue; // nicht umkehren
                    int nx = x + DX[nd], ny = y + DY[nd]; if (nx < 0 || ny < 0 || nx >= GN || ny >= GN) continue; int nc = ny * GN + nx;
                    bool isGoal = nx == gx && ny == gy;
                    if (block[nc] && !isGoal && !(nx == sx && ny == sy)) continue;
                    float c = GC + (nd != d ? 3f : 0);
                    if (occ.TryGetValue(nc, out var l)) { bool same = l.Any(o => (to != null && o.Item2 == to) || (from != null && o.Item1 == from)); c += same ? -GC * .45f : 2.5f; }
                    for (int k = 0; k < 4; k++) { int ax = nx + DX[k], ay = ny + DY[k]; if (ax >= 0 && ay >= 0 && ax < GN && ay < GN && block[ay * GN + ax]) { c += .4f; break; } }
                    int ns = nc * 4 + nd; float ng = gs[st] + c; if (ng >= gs[ns]) continue;
                    if (gs[ns] < 1e9f) open.Remove((gs[ns] + Mathf.Abs(nx - gx) * GC + Mathf.Abs(ny - gy) * GC, ns));
                    gs[ns] = ng; prev[ns] = st; open.Add((ng + Mathf.Abs(nx - gx) * GC + Mathf.Abs(ny - gy) * GC, ns));
                }
            }
            if (found < 0) return null; cost = gs[found];
            var path = new List<int>(); for (int st = found; st >= 0; st = prev[st]) path.Add(st / 4); path.Reverse(); return path;
        }

        static List<Vector2> Corners(List<int> cells)
        {
            var o = new List<Vector2>(); if (cells.Count == 0) return o;
            Vector2 P(int c) => Cc(c % GN, c / GN);
            o.Add(P(cells[0]));
            for (int i = 1; i < cells.Count - 1; i++) { var a = P(cells[i - 1]); var b = P(cells[i]); var c = P(cells[i + 1]); var d1 = b - a; var d2 = c - b; if (Mathf.Abs(d1.x * d2.y - d1.y * d2.x) > 1e-4f) o.Add(b); }
            o.Add(P(cells[cells.Count - 1])); return o;
        }
        static List<Vector2> Dedupe(List<Vector2> p) { var o = new List<Vector2>(); foreach (var q in p) if (o.Count == 0 || (q - o[o.Count - 1]).sqrMagnitude > 1e-4f) o.Add(q); return o; }

        /// <summary>Ecken als Viertelkreise mit Radius r (kleiner, wenn die Strecken kurz sind).</summary>
        static Vector2[] Round(List<Vector2> p, float r)
        {
            if (p.Count < 3) return p.ToArray(); var o = new List<Vector2> { p[0] };
            for (int i = 1; i < p.Count - 1; i++)
            {
                var a = p[i - 1]; var b = p[i]; var c = p[i + 1]; var u = (a - b); var w = (c - b); float lu = u.magnitude, lw = w.magnitude; if (lu < 1e-4f || lw < 1e-4f) continue; u /= lu; w /= lw;
                float cosA = Mathf.Clamp(Vector2.Dot(u, w), -1, 1); if (cosA < -.999f) { o.Add(b); continue; } // gerade
                float ang = Mathf.Acos(cosA); float t = r / Mathf.Tan(ang / 2); t = Mathf.Min(t, lu * .5f, lw * .5f); float rr = t * Mathf.Tan(ang / 2);
                var p0 = b + u * t; var p1 = b + w * t; var bis = (u + w).normalized; var ctr = b + bis * Mathf.Sqrt(rr * rr + t * t);
                float a0 = Mathf.Atan2(p0.y - ctr.y, p0.x - ctr.x), a1 = Mathf.Atan2(p1.y - ctr.y, p1.x - ctr.x); float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= 8; k++) { float aa = a0 + da * k / 8f; o.Add(ctr + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * rr); }
            }
            o.Add(p[p.Count - 1]); return o.ToArray();
        }
    }
}
