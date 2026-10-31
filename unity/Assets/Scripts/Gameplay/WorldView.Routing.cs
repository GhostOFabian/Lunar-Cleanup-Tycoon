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
        readonly Dictionary<string, string> sharedEnd = new Dictionary<string, string>(); // Band → Band, in dessen Eingang es als Y mündet
        string routedKey;
        const float GC = .5f; const int GN = 156; const float GOFF = GN * GC / 2; // Raster 0,5 m, ±39 m um die Basis

        struct Foot { public Vector2 min, max, c; public string id; }
        struct BPort { public string bid; public int face, slot; public Vector2 p, n; }

        void RouteBelts()
        {
            var live = ps.lay.belts.Where(x => Game.BeltLive(ps, x) && !Game.HiddenBelt(x)).ToList();
            string key = ps.pid + "|" + ps.sv.freq + "|" + string.Join(",", ps.bld.OrderBy(x => x)) + "|" + string.Join(",", live.Select(x => x.id));
            if (key == routedKey) return; routedKey = key; routed.Clear(); sharedEnd.Clear();
            // Grundflächen aus den echten Modellen (Planebene), Höhe 0,3–3,5 m
            var feet = new Dictionary<string, Foot>();
            foreach (var id in ps.bld)
            {
                if (!ps.lay.pos.ContainsKey(id) || (Game.IsUnit(id) && id != "u1")) continue; var T = TrisOf(id); var c = ps.lay.pos[id];
                Vector2 mn = new Vector2(1e9f, 1e9f), mx = -mn; int nPts = 0;
                foreach (var v in T) { float h = v.magnitude - pd.R; if (h < .3f || h > 3.5f) continue; var q = pd.PlanLocal(v.normalized); mn = Vector2.Min(mn, q); mx = Vector2.Max(mx, q); nPts++; }
                if (nPts == 0) { float f = GameData.Bld[id].fp * .7f; mn = c - Vector2.one * f; mx = c + Vector2.one * f; }
                feet[id] = new Foot { min = mn, max = mx, c = c, id = id };
            }
            // Hindernisraster: Gebäude (+0,7 m Abstand), Gruben der Entladestationen, Laternen
            var block = new bool[GN * GN];
            void Mark(Vector2 a, Vector2 b) { int x0 = Ix(a.x), x1 = Ix(b.x), y0 = Ix(a.y), y1 = Ix(b.y); for (int y = Mathf.Max(0, y0); y <= Mathf.Min(GN - 1, y1); y++) for (int x = Mathf.Max(0, x0); x <= Mathf.Min(GN - 1, x1); x++) block[y * GN + x] = true; }
            foreach (var f in feet.Values) Mark(f.min - Vector2.one * .7f, f.max + Vector2.one * .7f);
            foreach (var id in new[] { "u1", "u2", "u3" }) if (ps.bld.Contains(id) && feet.ContainsKey("u1") && GameData.Bld[id].st != null) for (int k = 0; k < GameData.Bld[id].st.docks; k++) { var dp = Game.DockPos(ps, id, k); var cp = ps.lay.pos[id]; var pp = cp + (dp - cp) * .72f; Mark(pp - Vector2.one * 1.4f, pp + Vector2.one * 1.4f); var vp = dp; Mark(vp - Vector2.one * 1.2f, vp + Vector2.one * 1.2f); }
            var lampIds = new[] { "hq", "roh", "u1", "prod", "presse", "sell", "werkstatt", "schmelze" }; int nl = 0;
            foreach (var id in lampIds) { if (!ps.bld.Contains(id) || !ps.lay.pos.ContainsKey(id) || nl >= 6) continue; nl++; var off = ps.lay.pos[id] + new Vector2(GameData.Bld[id].fp * .7f + .8f, GameData.Bld[id].fp * .7f + .8f); Mark(off - Vector2.one * .4f, off + Vector2.one * .4f); }
                        
            /* Reihenfolge der Bänder: kreuzt ein Band ein anderes nah an einem Ein-/Ausgang (dort geht keine Brücke), wird es beim nächsten Versuch zuerst verlegt */
            var order = new List<BeltDef>(live); Dictionary<string, Vector2[]> bestR = null; Dictionary<string, string> bestS = null; int bestBad = int.MaxValue;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                routed.Clear(); sharedEnd.Clear(); var bad = RouteOnce(order, feet, block);
                if (bad.Count < bestBad) { bestBad = bad.Count; bestR = new Dictionary<string, Vector2[]>(routed); bestS = new Dictionary<string, string>(sharedEnd); }
                if (bad.Count == 0) break;
                var front = order.Where(x => bad.Contains(x.id)).ToList(); var rest = order.Where(x => !bad.Contains(x.id)).ToList();
                var nord = front.Concat(rest).ToList(); if (nord.Select(x => x.id).SequenceEqual(order.Select(x => x.id))) { nord = rest.Concat(front).ToList(); } order = nord;
            }
            routed.Clear(); sharedEnd.Clear(); foreach (var kv in bestR) routed[kv.Key] = kv.Value; foreach (var kv in bestS) sharedEnd[kv.Key] = kv.Value;
        }

        /// <summary>Alle Bänder in dieser Reihenfolge verlegen. Rückgabe: Bänder, die ein anderes nah an einem Bandende kreuzen.</summary>
        HashSet<string> RouteOnce(List<BeltDef> live, Dictionary<string, Foot> feet, bool[] block)
        {
            var bad = new HashSet<string>();
            var used = new Dictionary<string, HashSet<int>>();   // belegte Port-Plätze je Gebäude/Fläche
            var occ = new Dictionary<int, List<(string from, string to, string id)>>(); // Rasterzellen schon verlegter Bänder
            var ends = new Dictionary<string, (int a, int b)>(); var raw = new Dictionary<int, List<string>>();
            var portOf = new Dictionary<string, (BPort a, BPort b)>();
            foreach (var b in live)
            {
                if (!feet.TryGetValue(b.from, out var FA) || !feet.TryGetValue(b.to, out var FB)) continue;
                // 1. eigener Ausgang und eigener Eingang (bis 3 Flächen, je Fläche bis 3 Plätze)
                var cands = new List<(BPort a, BPort b, bool shareA, bool shareB)>();
                foreach (var pa in Ports(FA, FB.c, b.from, used, 3)) foreach (var pb in Ports(FB, FA.c, b.to, used, 3)) cands.Add((pa, pb, false, false));
                if (cands.Count == 0) foreach (var pa in Ports(FA, FB.c, b.from, used, 4)) foreach (var pb in Ports(FB, FA.c, b.to, used, 4)) cands.Add((pa, pb, false, false)); /* alle Flächen, wenn die drei vorderen voll sind */
                List<int> best = null; float bc = 1e9f; (BPort a, BPort b, bool, bool) bestC = default; string shareWith = null;
                foreach (var cnd in cands)
                {
                    var path = AStar(cnd.a.p + cnd.a.n * 1.1f, cnd.a.n, cnd.b.p + cnd.b.n * 1.1f, -cnd.b.n, block, occ, null, null, out float cost);
                    if (path == null) continue;
                    /* lieber eine freie Gebäudefläche als neben einem anderen Band (dort laufen sie sonst ineinander), und keine zu engen Kurven */
                    int nA = used.TryGetValue(cnd.a.bid + cnd.a.face, out var tA) ? tA.Count : 0, nB = used.TryGetValue(cnd.b.bid + cnd.b.face, out var tB) ? tB.Count : 0;
                    cost += 7f * (nA + nB);
                    if (MinRadius(Shape(cnd.a, cnd.b, path)) < .56f) cost += 25f;
                    if (cost < bc) { bc = cost; best = path; bestC = cnd; }
                }
                // 2. nur wenn das nicht geht: als Y in den Eingang eines Bandes mit gleichem Ziel münden (nie als Abzweig am Anfang)
                if (best == null)
                    foreach (var kv in portOf)
                    {
                        var o = live.First(x => x.id == kv.Key); if (o.to != b.to) continue;
                        foreach (var pa in Ports(FA, FB.c, b.from, used, 3))
                        {
                            var path = AStar(pa.p + pa.n * 1.1f, pa.n, kv.Value.b.p + kv.Value.b.n * 1.1f, -kv.Value.b.n, block, occ, null, b.to, out float cost);
                            if (path != null && cost < bc) { bc = cost; best = path; bestC = (pa, kv.Value.b, false, true); shareWith = kv.Key; }
                        }
                    }
                if (shareWith != null) sharedEnd[b.id] = shareWith;
                if (best == null) continue;
                var A = bestC.a; var B = bestC.b;
                if (!bestC.Item3) { if (!used.TryGetValue(A.bid + A.face, out var ua)) used[A.bid + A.face] = ua = new HashSet<int>(); ua.Add(A.slot); }
                if (!bestC.Item4) { if (!used.TryGetValue(B.bid + B.face, out var ub)) used[B.bid + B.face] = ub = new HashSet<int>(); ub.Add(B.slot); }
                portOf[b.id] = (A, B);
                // belegte Fläche: Bandzellen plus zwei Zellen ringsum (Bandbreite + Abstand), damit nichts eng nebeneinander läuft
                var mark = new HashSet<int>(); foreach (int cell in best) { int cx = cell % GN, cy = cell / GN; for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++) { int x = cx + dx, y = cy + dy; if (x >= 0 && y >= 0 && x < GN && y < GN) mark.Add(y * GN + x); } }
                int ca = best[0], cb = best[best.Count - 1]; ends[b.id] = (ca, cb);
                int Md(int p, int q) => Mathf.Abs(p % GN - q % GN) + Mathf.Abs(p / GN - q / GN);
                /* echte Kreuzung (Bandmitte auf Bandmitte ±1 Zelle) nah an einem Bandende? */ foreach (int cell in best) for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) { int nc = cell + dy * GN + dx; if (!raw.TryGetValue(nc, out var ol)) continue; foreach (var oid in ol) { if (sharedEnd.TryGetValue(b.id, out var se) && se == oid) continue; var oe = ends[oid]; if (Md(cell, ca) < 11 || Md(cell, cb) < 11 || Md(cell, oe.a) < 11 || Md(cell, oe.b) < 11) bad.Add(b.id); } }
                foreach (int cell in best) { if (!raw.TryGetValue(cell, out var rl)) { rl = new List<string>(); raw[cell] = rl; } if (!rl.Contains(b.id)) rl.Add(b.id); }
                foreach (int cell in mark) { if (!occ.TryGetValue(cell, out var l)) { l = new List<(string, string, string)>(); occ[cell] = l; } l.Add((b.from, b.to, b.id)); }
                // Ecken der Rasterlinie → Polylinie; davor/dahinter gerade aus der Fläche heraus und bis ins Gebäude (wird an der Wand gekappt)
                routed[b.id] = Shape(A, B, best);
            }
            return bad;
        }
        static Vector2[] Shape(BPort A, BPort B, List<int> cells)
        {
            var pts = new List<Vector2> { A.p - A.n * 1.6f, A.p };
            foreach (var q in Corners(cells)) pts.Add(q); pts.Add(B.p); pts.Add(B.p - B.n * 1.6f);
            return Round(Dedupe(pts), 1.1f);
        }
        /// <summary>Kleinster Kurvenradius der fertigen Linie (Sehne / Winkel).</summary>
        static float MinRadius(Vector2[] rp)
        {
            float m = 9; for (int i = 1; i + 1 < rp.Length; i++) { var u1 = rp[i] - rp[i - 1]; var u2 = rp[i + 1] - rp[i]; float l1 = u1.magnitude, l2 = u2.magnitude; if (l1 < 1e-4f || l2 < 1e-4f) continue; float an = Vector2.Angle(u1, u2) * Mathf.Deg2Rad; if (an < .05f) continue; m = Mathf.Min(m, Mathf.Min(l1, l2) / an); }
            return m;
        }

        static int Ix(float v) => Mathf.FloorToInt((v + GOFF) / GC);
        static Vector2 Cc(int x, int y) => new Vector2((x + .5f) * GC - GOFF, (y + .5f) * GC - GOFF);

        /// <summary>Port-Kandidaten: Flächen, die zum Ziel zeigen, je ein freier Platz (Mitte, dann ±1 m).</summary>
        IEnumerable<BPort> Ports(Foot f, Vector2 toward, string id, Dictionary<string, HashSet<int>> used, int nFaces)
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
                var n = fc.n; var t = new Vector2(-n.y, n.x); used.TryGetValue(id + fc.k, out var taken);
                float half = Mathf.Abs(n.x) > .5f ? (f.max.y - f.min.y) / 2 : (f.max.x - f.min.x) / 2;
                var mid = new Vector2(n.x > .5f ? f.max.x : n.x < -.5f ? f.min.x : (f.min.x + f.max.x) / 2, n.y > .5f ? f.max.y : n.y < -.5f ? f.min.y : (f.min.y + f.max.y) / 2);
                /* freier Platz: Mitte, dann abwechselnd 1,45 m daneben – zuerst auf der Seite, wo das Ziel liegt (Bänder kreuzen sich nicht am Gebäude) */
                int sg = Vector2.Dot(t, toward - mid) >= 0 ? 1 : -1; int slot = 0; bool ok = false;
                foreach (int sl in new[] { 0, sg, -sg, 2 * sg, -2 * sg }) { if (Mathf.Abs(sl * 1.45f) > half - .45f) continue; if (taken != null && taken.Contains(sl)) continue; slot = sl; ok = true; break; }
                if (!ok) continue; /* Fläche voll */
                var pp = mid + t * (slot * 1.45f); if (Mathf.Abs(n.x) > .5f) pp.y = Cc(0, Ix(pp.y)).y; else pp.x = Cc(Ix(pp.x), 0).x; // quer zur Fläche aufs Raster
                yield return new BPort { bid = id, face = fc.k, slot = slot, p = pp, n = n };
            }
        }

        /// <summary>A* mit Richtungszustand: Schritt 0,5, Abbiegen +3 m, neben Hindernissen +0,4, fremde Bänder kreuzen +2,5,
        /// auf Bändern mit gleichem Ziel/gleicher Quelle mitlaufen ist günstig (Y-Zusammenführung).</summary>
        List<int> AStar(Vector2 s, Vector2 sDir, Vector2 g, Vector2 gDir, bool[] block, Dictionary<int, List<(string, string, string)>> occ, string from, string to, out float cost)
        {
            cost = 1e9f; int sx = Ix(s.x), sy = Ix(s.y), gx = Ix(g.x), gy = Ix(g.y);
            if (sx < 1 || sy < 1 || gx < 1 || gy < 1 || sx >= GN - 1 || sy >= GN - 1 || gx >= GN - 1 || gy >= GN - 1) return null;
            int[] DX = { 1, -1, 0, 0 }, DY = { 0, 0, 1, -1 }; int Dir(Vector2 v) => Mathf.Abs(v.x) > Mathf.Abs(v.y) ? (v.x > 0 ? 0 : 1) : (v.y > 0 ? 2 : 3);
            int sd = Dir(sDir), gd = Dir(gDir); const int RN = 4; int NS = GN * GN * 4 * RN; /* Zustand: Zelle, Richtung, gerade Schritte seit der letzten Ecke (0..3) */
            var gs = new float[NS]; for (int i = 0; i < NS; i++) gs[i] = 1e9f; var prev = new int[NS];
            var open = new SortedSet<(float f, int st)>(); int s0 = ((sy * GN + sx) * 4 + sd) * RN + (RN - 1); gs[s0] = 0; prev[s0] = -1; open.Add((0, s0));
            int found = -1, guard = 0;
            while (open.Count > 0 && guard++ < 400000)
            {
                var cur = open.Min; open.Remove(cur); int st = cur.st, run = st % RN, sc = st / RN, cell = sc / 4, d = sc % 4, x = cell % GN, y = cell / GN;
                if (x == gx && y == gy && d == gd) { found = st; break; }
                for (int nd = 0; nd < 4; nd++)
                {
                    if ((nd ^ 1) == d && (nd >> 1) == (d >> 1)) continue; /* nicht umkehren */
                    if (nd != d && run < RN - 1) continue; /* zwischen zwei Ecken mind. 1,5 m gerade: Kurvenradius bleibt groß */
                    int nx = x + DX[nd], ny = y + DY[nd]; if (nx < 0 || ny < 0 || nx >= GN || ny >= GN) continue; int nc = ny * GN + nx;
                    bool isGoal = nx == gx && ny == gy;
                    if (block[nc] && !isGoal && !(nx == sx && ny == sy)) continue;
                    float c = GC + (nd != d ? 3f : 0);
                    if (occ.TryGetValue(nc, out var l)) { bool same = l.Any(o => (to != null && o.Item2 == to) || (from != null && o.Item1 == from)); c += same ? -GC * .45f : 4f; /* Kreuzen ist teuer: lieber Umweg */ if (!same && (Mathf.Abs(nx - sx) + Mathf.Abs(ny - sy) < 11 || Mathf.Abs(nx - gx) + Mathf.Abs(ny - gy) < 11)) c += 14f; /* nah an Ein-/Ausgang kann keine Brücke hin: dort fast nie kreuzen */ }
                    for (int k = 0; k < 4; k++) { int ax = nx + DX[k], ay = ny + DY[k]; if (ax >= 0 && ay >= 0 && ax < GN && ay < GN && block[ay * GN + ax]) { c += .4f; break; } }
                    int nrun = nd != d ? 0 : Mathf.Min(RN - 1, run + 1);
                    int ns = ((nc * 4) + nd) * RN + nrun; float ng = gs[st] + c; if (ng >= gs[ns]) continue;
                    float hn = Mathf.Abs(nx - gx) * GC + Mathf.Abs(ny - gy) * GC;
                    if (gs[ns] < 1e9f) open.Remove((gs[ns] + hn, ns));
                    gs[ns] = ng; prev[ns] = st; open.Add((ng + hn, ns));
                }
            }
            if (found < 0) return null; cost = gs[found];
            var path = new List<int>(); for (int st = found; st >= 0; st = prev[st]) path.Add(st / RN / 4); path.Reverse(); return path;
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
            if (p.Count < 3) return p.ToArray();
            p = new List<Vector2>(p);
            /* Zwischenpunkte auf einer Geraden entfernen (Port-Punkte Index 1 und Count-2 bleiben), damit Kurven die ganze Gerade nutzen */
            for (int i = 2; i < p.Count - 2;)
            {
                var d1 = p[i] - p[i - 1]; var d2 = p[i + 1] - p[i];
                if (d1.sqrMagnitude < 1e-6f || (Mathf.Abs(d1.x * d2.y - d1.y * d2.x) < 1e-3f * d1.magnitude * Mathf.Max(1e-3f, d2.magnitude) && Vector2.Dot(d1, d2) > 0)) p.RemoveAt(i); else i++;
            }
            // Kurzer Versatz (S-Knick mit Mittelstück < 2r): als schräges Stück mit weiten Bögen statt zwei engen Ecken (sonst Zacken im Band)
            for (int i = 2; i + 3 < p.Count; i++) /* Port-Punkte (Index 1 und Count-2) bleiben fest */
            {
                var m = p[i + 1] - p[i]; float lm = m.magnitude; if (lm < 1e-4f || lm >= 2 * r * .95f) continue;
                var din = p[i] - p[i - 1]; var dout = p[i + 2] - p[i + 1]; float li = din.magnitude, lo = dout.magnitude; if (li < 1e-4f || lo < 1e-4f) continue;
                float c1 = din.x * m.y - din.y * m.x, c2 = m.x * dout.y - m.y * dout.x;
                if (c1 * c2 >= 0) continue; /* nur S-Form (entgegengesetzte Drehung) */
                float s = Mathf.Min(r * 1.3f, li * .45f, lo * .45f); if (s < .2f) continue;
                p[i] = p[i] - din / li * s; p[i + 1] = p[i + 1] + dout / lo * s; i++;
            }
            int last = p.Count - 1;
            float Avail(int j, float len) => (j <= 1 || j >= last - 1) ? Mathf.Max(0, len - .3f) : len * .5f; /* zum Port hin ganze Gerade (0,3 m bleiben gerade), sonst halbe */
            var o = new List<Vector2> { p[0] };
            for (int i = 1; i < p.Count - 1; i++)
            {
                var a = p[i - 1]; var b = p[i]; var c = p[i + 1]; var u = (a - b); var w = (c - b); float lu = u.magnitude, lw = w.magnitude; if (lu < 1e-4f || lw < 1e-4f) continue; u /= lu; w /= lw;
                float cosA = Mathf.Clamp(Vector2.Dot(u, w), -1, 1); if (cosA < -.999f) { o.Add(b); continue; } // gerade
                float ang = Mathf.Acos(cosA); float t = r / Mathf.Tan(ang / 2); t = Mathf.Min(t, Avail(i - 1, lu), Avail(i + 1, lw)); float rr = t * Mathf.Tan(ang / 2);
                var p0 = b + u * t; var p1 = b + w * t; var bis = (u + w).normalized; var ctr = b + bis * Mathf.Sqrt(rr * rr + t * t);
                float a0 = Mathf.Atan2(p0.y - ctr.y, p0.x - ctr.x), a1 = Mathf.Atan2(p1.y - ctr.y, p1.x - ctr.x); float da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= 8; k++) { float aa = a0 + da * k / 8f; o.Add(ctr + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * rr); }
            }
            o.Add(p[p.Count - 1]); return o.ToArray();
        }
    }
}
