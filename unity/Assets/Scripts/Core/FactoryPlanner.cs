// Fabian 10.11.: Fabrik-Planer – plant Gebäude, Anschlüsse und Bänder gemeinsam (ersetzt die alte Bandverlegung, die Brücken und den Band-Editor).
//
// Grundsätze (docs/DECISIONS.md, 10.11.):
//  • Produktionsgebäude stehen in einem Modul nach dem Materialfluss (Entladestation → Rohstofflager → Presse/Raffinerie → Schmelze → Produktlager → Verkauf).
//    Das Modul wird gedreht/gespiegelt in die echte Basisfläche eingepasst, so nah wie möglich an den alten Plätzen.
//  • Jedes Gebäude hat feste Anschlüsse: ein Eingang (Fläche + Position), Ausgänge nebeneinander auf einer anderen Fläche, Abstand 2 m.
//  • Mehrere Bänder zum selben Ziel: das längste ist das Hauptband, die anderen münden als Y-Stück (2 m seitlich, 3 m lang, schräg) auf sein letztes gerades Stück.
//  • Alle Bänder auf einer Höhe. Kreuzen ist verboten: fertige Bänder samt 1 m Abstandszone sind harte Hindernisse. Kein Weg → andere Reihenfolge → andere Modul-Lage.
//  • Rechtwinklig im 1-m-Raster, mind. 2 m gerade zwischen zwei Kurven (Radius 1 m), gerade Ein- und Ausfahrt an jedem Anschluss.
//  • Übrige Gebäude werden um das Modul gepackt (nächster freier Platz zum alten Ort), Andockfläche und Zufahrt der Fahrzeuge bleiben frei.
//  • Validate() prüft das fertige Netz unabhängig von der Wegsuche; Fehler werden gemeldet, nie stillschweigend übernommen.
// Reines C# (nur UnityEngine-Vektoren), deterministisch: gleiche Eingabe → gleiches Ergebnis.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class FactoryPlan
    {
        public string key; public bool ok; public readonly List<string> errors = new List<string>();
        public readonly Dictionary<string, Vector2Int> pos = new Dictionary<string, Vector2Int>();
        public readonly Dictionary<string, int> half = new Dictionary<string, int>();
        public readonly Dictionary<string, int> faceIn = new Dictionary<string, int>(), faceOut = new Dictionary<string, int>();
        public readonly Dictionary<string, List<Vector2Int>> cells = new Dictionary<string, List<Vector2Int>>();   // Rasterweg je Band (Anschluss → Anschluss bzw. Y)
        public readonly Dictionary<string, (string trunk, Vector2Int m, int side)> merge = new Dictionary<string, (string, Vector2Int, int)>();
        public readonly Dictionary<string, Vector2Int> start = new Dictionary<string, Vector2Int>();
        public readonly Dictionary<string, Vector2Int> goal = new Dictionary<string, Vector2Int>();   // Eingang je Zielgebäude
        public RectInt dock; public float unitYaw; public int rot, mir; public Vector2Int shift; public long ms;
        public bool hasPad, padOutside; public Vector2Int pad; public int padH = PAD_H; public readonly List<Vector2Int> grow = new List<Vector2Int>(); public const int PAD_H = 6; /* 13.11. (Fabian): größere U-Landebahn, 13 × 13 m – die Basis wächst am Rand um sie herum */ // 11.11.: Landeplatz für alle Schiffe (9 × 9 m), immer vorhanden – passt er nicht, wird die Basis dort größer (grow = Rasterpunkte, die zur Basis werden)
        public readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();   // Bänder + Abstand, Y-Stücke, Andockfläche + Zufahrt
        public bool[] mask;
        public bool InBase(Vector2 p) { int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y); return x >= -FactoryPlanner.EXT && x <= FactoryPlanner.EXT && y >= -FactoryPlanner.EXT && y <= FactoryPlanner.EXT && mask != null && mask[(y + FactoryPlanner.EXT) * FactoryPlanner.N + x + FactoryPlanner.EXT]; }
    }

    public static class FactoryPlanner
    {
        public const int EXT = 44, N = 2 * EXT + 1, MINRUN = 2, CLR = 2, ML = 3, SLOT = 2;
        static readonly Vector2Int[] DX = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
        public static Vector2Int Dir(int k) => DX[k];

        // ---------- Produktionsmodul (Flussrichtung +x) ----------
        static readonly Dictionary<string, Vector2Int> MOD = new Dictionary<string, Vector2Int>
        { { "u1", new Vector2Int(0, -13) }, { "roh", new Vector2Int(0, 0) }, { "presse", new Vector2Int(14, 9) }, { "raff", new Vector2Int(14, -9) },
          { "schmelze", new Vector2Int(28, 13) }, { "prod", new Vector2Int(42, 0) }, { "sell", new Vector2Int(42, 13) } };
        /// <summary>Anschlussseiten im Modul (Eingang, Ausgang), 0=+x 1=+y 2=−x 3=−y, −1 = keine.</summary>
        static readonly Dictionary<string, (int i, int o)> FACES = new Dictionary<string, (int, int)>
        { { "u1", (-1, 1) }, { "roh", (3, 0) }, { "presse", (2, 0) }, { "raff", (2, 0) }, { "schmelze", (2, 3) }, { "prod", (2, 1) }, { "sell", (3, -1) } };
        static readonly RectInt DOCK = new RectInt(-6, -21, 13, 4);   // Andockplätze vor der Entladestation (x −6…6, y −21…−18), bis 6 Fahrzeuge
        const int DOCK_DIR = 3;                                       // Fahrzeuge kommen von −y
        public static bool InModule(string id) => MOD.ContainsKey(id);

        /// <summary>Gemessene halbe Ausdehnung der Modelle (m, bis 3,5 m Höhe, drehfest = größere Achse), Editor 10.11. (call DevFootprints).</summary>
        static readonly Dictionary<string, float> REAL = new Dictionary<string, float>
        { { "energy", 7.6f }, { "geo", 3.6f }, { "hangar", 3.8f }, { "hq", 4.1f }, { "lifter", 4.4f }, { "port", 4.4f }, { "presse", 2.6f }, { "prod", 3.6f }, { "raff", 2.6f },
          { "reactor", 3.6f }, { "research", 3.4f }, { "roh", 3.9f }, { "rtg", 2.5f }, { "schmelze", 2.5f }, { "sell", 4.2f }, { "u1", 4.0f }, { "werkstatt", 4.6f } };
        /// <summary>Halbe Rasterbreite: alle Zellen, deren Mitte im Modell liegt. Bandkante bleibt ≥ 0,6 m von der Wand (Band ab Zelle h+2).</summary>
        public static int Half(string id) => Mathf.Max(1, REAL.TryGetValue(id, out var e) ? Mathf.RoundToInt(e - .5f) : Mathf.RoundToInt(GameData.Bld[id].fp * .9f));
        static Vector2Int Tf(Vector2Int p, int rot, int mir) { int x = p.x, y = mir == 1 ? -p.y : p.y; for (int i = 0; i < rot; i++) { int t = x; x = -y; y = t; } return new Vector2Int(x, y); }
        static int TFace(int k, int rot, int mir) { if (k < 0) return -1; if (mir == 1 && (k == 1 || k == 3)) k = (k + 2) % 4; return (k + rot) % 4; }

        class Net { public string id, from, to; }
        class Ctx
        {
            public bool[] inBase; public Dictionary<string, Vector2Int> pos; public Dictionary<string, int> hs; public List<Net> nets; public RectInt dock;
            public Dictionary<string, int> fIn = new Dictionary<string, int>(), fOut = new Dictionary<string, int>();
            public Dictionary<string, List<Net>> ins, outs;
            public bool In(int x, int y) => x >= -EXT && x <= EXT && y >= -EXT && y <= EXT && inBase[(y + EXT) * N + x + EXT];
        }
        static int Idx(int x, int y) => (y + EXT) * N + (x + EXT);
        static bool Inside(int x, int y) => x >= -EXT && x <= EXT && y >= -EXT && y <= EXT;

        /// <summary>Plan für ein Layout. inBase(x, y): liegt der Meter-Punkt auf einer Basiszelle? pinned: vom Spieler verschobene Nebengebäude.</summary>
        [ThreadStatic] static Func<int, int, bool> growF;
        public static FactoryPlan Plan(string key, LayoutDef L, Func<int, int, bool> inBase, Dictionary<string, Vector2> pinned = null, Func<int, int, bool> canGrow = null)
        {
            growF = canGrow;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var mask = new bool[N * N]; for (int y = -EXT; y <= EXT; y++) for (int x = -EXT; x <= EXT; x++) mask[Idx(x, y)] = inBase(x, y);
            var ids = L.pos.Keys.Where(k => GameData.Bld.ContainsKey(k) && (!Game.IsUnit(k) || k == "u1")).ToList();
            var hs = ids.ToDictionary(k => k, Half);
            var orig = ids.ToDictionary(k => k, k => new Vector2Int(Mathf.RoundToInt(L.pos[k].x), Mathf.RoundToInt(L.pos[k].y)));
            var nets = L.belts.Where(b => !Game.HiddenBelt(b) && ids.Contains(b.from) && ids.Contains(b.to)).Select(b => new Net { id = b.id, from = b.from, to = b.to }).ToList();
            var cands = ModuleCandidates(ids, hs, mask, orig);
            FactoryPlan last = null; int tried = 0;
            foreach (var c in cands)
            {
                var P = TryAt(key, c, ids, hs, mask, orig, nets, pinned);
                if (P.ok) { P.ms = sw.ElapsedMilliseconds; return P; }
                last = P; if (++tried >= 400) break;
            }
            if (last == null) { last = new FactoryPlan { key = key }; last.errors.Add("Das Produktionsmodul passt nicht in die Basis"); }
            last.ms = sw.ElapsedMilliseconds; return last;
        }

        static bool RectIn(bool[] mask, int x0, int y0, int x1, int y1)
        {
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (!Inside(x, y) || !mask[Idx(x, y)]) return false;
            return true;
        }

        static List<(float sc, int rot, int mir, int tx, int ty)> ModuleCandidates(List<string> ids, Dictionary<string, int> hs, bool[] mask, Dictionary<string, Vector2Int> orig)
        {
            var res = new List<(float, int, int, int, int)>(); var mod = MOD.Keys.Where(ids.Contains).ToList();
            for (int rot = 0; rot < 4; rot++) for (int mir = 0; mir < 2; mir++)
                {
                    var pts = mod.ToDictionary(k => k, k => Tf(MOD[k], rot, mir));
                    var a = Tf(new Vector2Int(DOCK.xMin, DOCK.yMin), rot, mir); var b = Tf(new Vector2Int(DOCK.xMax - 1, DOCK.yMax - 1), rot, mir);
                    for (int tx = -36; tx <= 36; tx++) for (int ty = -36; ty <= 36; ty++)
                        {
                            bool ok = true;
                            foreach (var k in mod) { var p = pts[k]; int X = p.x + tx, Y = p.y + ty, h = hs[k] + 1; if (!RectIn(mask, X - h, Y - h, X + h, Y + h)) { ok = false; break; } }
                            if (!ok) continue; /* Andockfeld darf über den Rand reichen – die Fahrzeuge kommen von außen */
                            float sc = 0; foreach (var k in mod) sc += (new Vector2(pts[k].x + tx - orig[k].x, pts[k].y + ty - orig[k].y)).magnitude;
                            res.Add((sc, rot, mir, tx, ty));
                        }
                }
            res.Sort((p, q) => p.Item1.CompareTo(q.Item1)); return res;
        }

        static FactoryPlan TryAt(string key, (float sc, int rot, int mir, int tx, int ty) c, List<string> ids, Dictionary<string, int> hs, bool[] mask, Dictionary<string, Vector2Int> orig, List<Net> nets, Dictionary<string, Vector2> pinned)
        {
            var P = new FactoryPlan { key = key, rot = c.rot, mir = c.mir, shift = new Vector2Int(c.tx, c.ty) };
            var pos = new Dictionary<string, Vector2Int>();
            foreach (var k in MOD.Keys) if (ids.Contains(k)) { var q = Tf(MOD[k], c.rot, c.mir); pos[k] = new Vector2Int(q.x + c.tx, q.y + c.ty); }
            var a = Tf(new Vector2Int(DOCK.xMin, DOCK.yMin), c.rot, c.mir); var b = Tf(new Vector2Int(DOCK.xMax - 1, DOCK.yMax - 1), c.rot, c.mir);
            var dock = new RectInt(Mathf.Min(a.x, b.x) + c.tx, Mathf.Min(a.y, b.y) + c.ty, Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);
            P.dock = dock; int dd = TFace(DOCK_DIR, c.rot, c.mir); P.unitYaw = Mathf.Atan2(DX[dd].y, DX[dd].x);
            var X = new Ctx { inBase = mask, pos = pos, hs = hs, nets = nets.Where(n => pos.ContainsKey(n.from) && pos.ContainsKey(n.to)).ToList(), dock = dock };
            foreach (var k in pos.Keys) { var f = FACES[k]; X.fIn[k] = TFace(f.i, c.rot, c.mir); X.fOut[k] = TFace(f.o, c.rot, c.mir); }
            if (!Ports(X, P)) return P;
            if (!Route(X, P)) return P;
            // reservierte Zellen: Bänder + Abstand, Y-Stücke, Andockfläche + 8 m Zufahrt
            var res = new HashSet<Vector2Int>();
            foreach (var kv in P.cells) { foreach (var q in Halo(kv.Value)) res.Add(q); if (P.merge.TryGetValue(kv.Key, out var mg)) foreach (var q in Halo(Connector(kv.Value[kv.Value.Count - 1], mg.m))) res.Add(q); }
            var dv = DX[dd]; var dvOut = new Vector2Int(-dv.x, -dv.y); // nach außen = Gegenrichtung der Anfahrt
            for (int s = 0; s <= 4; s++) for (int x = dock.xMin - 1; x <= dock.xMax; x++) for (int y = dock.yMin - 1; y <= dock.yMax; y++) res.Add(new Vector2Int(x + dvOut.x * s, y + dvOut.y * s));
            // übrige Gebäude packen: größte zuerst, jeweils dorthin, wo sie am meisten Kontakt zu Rand/Bändern/Gebäuden haben (freie Fläche bleibt zusammenhängend)
            var placed = new Dictionary<string, Vector2Int>(pos);
            var others = ids.Where(k => !pos.ContainsKey(k)).OrderByDescending(k => hs[k]).ThenBy(k => k, StringComparer.Ordinal).ToList();
            var pk = new Packer(mask, res); foreach (var kv in placed) pk.Add(kv.Value, hs[kv.Key], true);
            foreach (var k in others)
            {
                Vector2Int want = orig[k]; Vector2Int? spot = null;
                if (pinned != null && pinned.TryGetValue(k, out var pv)) { var w = new Vector2Int(Mathf.RoundToInt(pv.x), Mathf.RoundToInt(pv.y)); if (pk.Fits(w, hs[k])) spot = w; want = w; } /* Wunschplatz des Spielers, wenn er frei ist */
                if (!spot.HasValue) spot = pk.Best(hs[k], want);
                if (!spot.HasValue) { P.errors.Add("Kein Platz für " + GameData.Bld[k].name); return P; }
                placed[k] = spot.Value; pk.Add(spot.Value, hs[k], false);
            }
            /* Fabian 11.11.: Landeplatz für Lieferschiffe von anderen Planeten – zuletzt gepackt, gern nahe der Verkaufsrakete. Kein Platz = kein Fehler (dann landet die alte Lieferrakete). */
            {
                var want = placed.TryGetValue("sell", out var sp) ? sp : new Vector2Int(dock.x + dock.width / 2, dock.y + dock.height / 2);
                int ph = FactoryPlan.PAD_H; var pp = pk.Best(ph, want); if (pp.HasValue) { P.hasPad = true; P.pad = pp.Value; pk.Add(pp.Value, ph, false); }
                /* Fabian 11.11.: Schiffe sollen nie schweben – passt der Landeplatz nicht, wird die Basis am Rand um ihn herum größer. Nichts anderes wird verschoben. */
                if (!P.hasPad && growF != null) { var po = pk.BestGrow(ph, want, growF); if (po.HasValue) { P.hasPad = true; P.padOutside = true; P.pad = po.Value; for (int y = po.Value.y - ph - 1; y <= po.Value.y + ph + 1; y++) for (int x = po.Value.x - ph - 1; x <= po.Value.x + ph + 1; x++) if (Inside(x, y) && !mask[Idx(x, y)]) P.grow.Add(new Vector2Int(x, y)); pk.Add(po.Value, ph, false); } }
            }
            foreach (var kv in placed) { P.pos[kv.Key] = kv.Value; P.half[kv.Key] = hs[kv.Key]; }
            foreach (var q in res) P.reserved.Add(q); P.mask = mask;
            foreach (var kv in X.fIn) P.faceIn[kv.Key] = kv.Value; foreach (var kv in X.fOut) P.faceOut[kv.Key] = kv.Value;
            Validate(P, mask);
            P.ok = P.errors.Count == 0; return P;
        }

        /// <summary>Platzsuche mit Summentabellen: jede Rechteckprüfung kostet gleich viel. Abstände: 1 m zwischen Nebengebäuden, 2 m zum Modul; unter dem Gebäude und 1 m drumherum keine Bänder/Zufahrt.</summary>
        public class Packer
        {
            const int G = N + 2; readonly bool[] mask; readonly HashSet<Vector2Int> res; int[] sOut, sRes, sOcc1, sOcc2, sHard; readonly List<(Vector2Int p, int h, bool mod)> bl = new List<(Vector2Int, int, bool)>();
            public Packer(bool[] mask, HashSet<Vector2Int> res) { this.mask = mask; this.res = res; Rebuild(); }
            public void Add(Vector2Int p, int h, bool mod) { bl.Add((p, h, mod)); Rebuild(); }
            static int I(int x, int y) => (y + EXT + 1) * G + (x + EXT + 1);
            int[] Sum(Func<int, int, bool> f) { var a = new int[(G + 1) * (G + 1)]; for (int y = -EXT - 1; y <= EXT; y++) for (int x = -EXT - 1; x <= EXT; x++) { int v = (x >= -EXT && y >= -EXT && f(x, y)) ? 1 : 0; int i = (y + EXT + 1 + 1) * (G + 1) + (x + EXT + 1 + 1); a[i] = v + a[i - 1] + a[i - (G + 1)] - a[i - (G + 1) - 1]; } return a; }
            static int Q(int[] a, int x0, int y0, int x1, int y1)
            {
                x0 = Mathf.Max(x0, -EXT - 1); y0 = Mathf.Max(y0, -EXT - 1); x1 = Mathf.Min(x1, EXT); y1 = Mathf.Min(y1, EXT); if (x1 < x0 || y1 < y0) return 0;
                int A(int x, int y) => a[(y + EXT + 1 + 1) * (G + 1) + (x + EXT + 1 + 1)];
                return A(x1, y1) - A(x0 - 1, y1) - A(x1, y0 - 1) + A(x0 - 1, y0 - 1);
            }
            void Rebuild()
            {
                var o1 = new bool[N * N]; var o2 = new bool[N * N]; var hard = new bool[N * N];
                foreach (var (p, h, mod) in bl) for (int y = p.y - h - 2; y <= p.y + h + 2; y++) for (int x = p.x - h - 2; x <= p.x + h + 2; x++)
                        {
                            if (!Inside(x, y)) continue; int d = Mathf.Max(Mathf.Abs(x - p.x), Mathf.Abs(y - p.y)) - h; int ix = Idx(x, y);
                            if (d <= 0) hard[ix] = true; if (d <= 1) o1[ix] = true; if (mod && d <= 2) o2[ix] = true;
                        }
                sOut = Sum((x, y) => !mask[Idx(x, y)]); sRes = Sum((x, y) => res.Contains(new Vector2Int(x, y))); sOcc1 = Sum((x, y) => o1[Idx(x, y)]); sOcc2 = Sum((x, y) => o2[Idx(x, y)]);
                sHard = Sum((x, y) => !mask[Idx(x, y)] || hard[Idx(x, y)] || res.Contains(new Vector2Int(x, y)));
            }
            public bool Fits(Vector2Int q, int h)
            {
                if (q.x - h < -EXT || q.x + h > EXT || q.y - h < -EXT || q.y + h > EXT) return false;
                if (Q(sOut, q.x - h, q.y - h, q.x + h, q.y + h) > 0) return false;                       // ganz in der Basis
                if (Q(sRes, q.x - h - 1, q.y - h - 1, q.x + h + 1, q.y + h + 1) > 0) return false;       // keine Bänder/Zufahrt unter und neben dem Gebäude
                if (Q(sOcc1, q.x - h, q.y - h, q.x + h, q.y + h) > 0) return false;                      // 1 m zu Nebengebäuden
                if (Q(sOcc2, q.x - h, q.y - h, q.x + h, q.y + h) > 0) return false;                      // 2 m zum Modul
                return true;
            }
            public Vector2Int? Best(int h, Vector2Int want)
            {
                Vector2Int? best = null; int bc = -1, bd = int.MaxValue; int r = h + 2;
                for (int y = -EXT + h; y <= EXT - h; y++) for (int x = -EXT + h; x <= EXT - h; x++)
                    {
                        var q = new Vector2Int(x, y); if (!Fits(q, h)) continue;
                        int ring = Q(sHard, x - r, y - r, x + r, y + r) - Q(sHard, x - r + 1, y - r + 1, x + r - 1, y + r - 1) + Ext(x - r, y - r, x + r, y + r) - Ext(x - r + 1, y - r + 1, x + r - 1, y + r - 1);
                        int d = Mathf.Abs(x - want.x) + Mathf.Abs(y - want.y);
                        if (ring > bc || (ring == bc && d < bd)) { bc = ring; bd = d; best = q; }
                    }
                return best;
            }
            /// <summary>11.11.: Platz am Basisrand, der teils über die Basis hinausragt: innen frei wie sonst (keine Bänder, Zufahrt, Gebäude), außen nur normale Parzellen (canGrow). Nächster Platz zum Wunschort.</summary>
            public Vector2Int? BestGrow(int h, Vector2Int want, Func<int, int, bool> canGrow)
            {
                Vector2Int? best = null; int bd = int.MaxValue; int area = (2 * h + 1) * (2 * h + 1), r = h + 1;
                for (int y = -EXT + r; y <= EXT - r; y++) for (int x = -EXT + r; x <= EXT - r; x++)
                    {
                        int outN = Q(sOut, x - r, y - r, x + r, y + r); if (outN == 0) continue;                    // passt schon ganz rein (dann hätte Best ihn gefunden)
                        if (outN >= (2 * r + 1) * (2 * r + 1)) continue;                                            // muss an die Basis grenzen
                        if (Q(sRes, x - h - 1, y - h - 1, x + h + 1, y + h + 1) > 0) continue;                      // keine Bänder/Zufahrt
                        if (Q(sOcc1, x - h, y - h, x + h, y + h) > 0 || Q(sOcc2, x - h, y - h, x + h, y + h) > 0) continue; // Abstand zu Gebäuden
                        int d = Mathf.Abs(x - want.x) + Mathf.Abs(y - want.y); if (d >= bd) continue;
                        bool ok = true; for (int yy = y - r; yy <= y + r && ok; yy++) for (int xx = x - r; xx <= x + r; xx++) if (!mask[Idx(xx, yy)] && !canGrow(xx, yy)) { ok = false; break; }
                        if (ok) { bd = d; best = new Vector2Int(x, y); }
                    }
                return best;
            }
            /// <summary>Zellen außerhalb des Rasters zählen als belegt (Rand).</summary>
            static int Ext(int x0, int y0, int x1, int y1) { int all = (x1 - x0 + 1) * (y1 - y0 + 1); int cx0 = Mathf.Max(x0, -EXT), cy0 = Mathf.Max(y0, -EXT), cx1 = Mathf.Min(x1, EXT), cy1 = Mathf.Min(y1, EXT); int inside = cx1 < cx0 || cy1 < cy0 ? 0 : (cx1 - cx0 + 1) * (cy1 - cy0 + 1); return all - inside; }
        }

        // ---------- Anschlüsse ----------
        static bool Ports(Ctx X, FactoryPlan P)
        {
            var outs = new Dictionary<string, List<Net>>(); var ins = new Dictionary<string, List<Net>>();
            foreach (var n in X.nets) { if (!outs.TryGetValue(n.from, out var lo)) outs[n.from] = lo = new List<Net>(); lo.Add(n); if (!ins.TryGetValue(n.to, out var li)) ins[n.to] = li = new List<Net>(); li.Add(n); }
            foreach (var kv in outs)
            {
                var b = X.pos[kv.Key]; int k = X.fOut[kv.Key]; if (k < 0) { P.errors.Add(kv.Key + ": kein Ausgang festgelegt"); return false; }
                var n = DX[k]; var t = new Vector2Int(-n.y, n.x); int h = X.hs[kv.Key];
                var L = kv.Value.OrderBy(l => (X.pos[l.to] - b).x * t.x + (X.pos[l.to] - b).y * t.y).ThenBy(l => l.id, StringComparer.Ordinal).ToList(); int m = L.Count;
                for (int i = 0; i < m; i++) { int o = (2 * i - (m - 1)) * SLOT / 2; P.start[L[i].id] = new Vector2Int(b.x + n.x * (h + 1) + t.x * o, b.y + n.y * (h + 1) + t.y * o); }
            }
            foreach (var kv in ins)
            {
                var b = X.pos[kv.Key]; int k = X.fIn[kv.Key]; if (k < 0) { P.errors.Add(kv.Key + ": kein Eingang festgelegt"); return false; }
                var n = DX[k]; int h = X.hs[kv.Key]; P.goal[kv.Key] = new Vector2Int(b.x + n.x * (h + 1), b.y + n.y * (h + 1));
            }
            X.ins = ins; X.outs = outs; return true;
        }

        // ---------- Wegsuche ----------
        static bool[] Obstacles(Ctx X)
        {
            var blk = new bool[N * N];
            for (int y = -EXT; y <= EXT; y++) for (int x = -EXT; x <= EXT; x++) if (!X.inBase[Idx(x, y)]) blk[Idx(x, y)] = true;
            foreach (var kv in X.pos) { int r = X.hs[kv.Key] + 1; var b = kv.Value; for (int y = b.y - r; y <= b.y + r; y++) for (int x = b.x - r; x <= b.x + r; x++) if (Inside(x, y)) blk[Idx(x, y)] = true; }
            for (int y = X.dock.yMin; y < X.dock.yMax; y++) for (int x = X.dock.xMin; x < X.dock.xMax; x++) if (Inside(x, y)) blk[Idx(x, y)] = true;
            return blk;
        }

        static List<Vector2Int> Halo(List<Vector2Int> cells)
        {
            var o = new HashSet<Vector2Int>();
            foreach (var c in cells) for (int dy = -CLR + 1; dy < CLR; dy++) for (int dx = -CLR + 1; dx < CLR; dx++) o.Add(new Vector2Int(c.x + dx, c.y + dy));
            return o.ToList();
        }
        /// <summary>Schräges Y-Stück: Zellen vom Zweig-Ende e bis auf das Hauptband bei m.</summary>
        public static List<Vector2Int> Connector(Vector2Int e, Vector2Int m)
        {
            int n = Mathf.Max(Mathf.Abs(m.x - e.x), Mathf.Abs(m.y - e.y)); var o = new List<Vector2Int>();
            for (int i = 1; i <= n; i++) o.Add(new Vector2Int(Mathf.RoundToInt(e.x + (m.x - e.x) * i / (float)n), Mathf.RoundToInt(e.y + (m.y - e.y) * i / (float)n)));
            return o;
        }
        /// <summary>Y-Plätze auf dem letzten geraden Stück des Hauptbandes: Zweig-Ende ML davor und 2 m seitlich, gleiche Richtung.</summary>
        static List<(Vector2Int e, int d, Vector2Int m, int side)> MergeSpots(List<Vector2Int> tp)
        {
            var res = new List<(Vector2Int, int, Vector2Int, int)>(); int L = tp.Count; if (L < 3) return res;
            var last = tp[L - 1] - tp[L - 2]; int j = L - 1; while (j > 0 && tp[j] - tp[j - 1] == last) j--;
            for (int i = Mathf.Max(ML + 2, j + ML + 2); i < L - 3; i++)
            {
                var d0 = tp[i] - tp[i - 1]; int k = Array.IndexOf(DX, d0); var t = new Vector2Int(-d0.y, d0.x); var b = tp[i - ML];
                foreach (int sd in new[] { 1, -1 }) res.Add((new Vector2Int(b.x + t.x * 2 * sd, b.y + t.y * 2 * sd), k, tp[i], sd));
            }
            return res;
        }

        static bool Route(Ctx X, FactoryPlan P)
        {
            var blk0 = Obstacles(X); var ins = X.ins;
            var trunk = new Dictionary<string, string>();
            foreach (var kv in ins) trunk[kv.Key] = kv.Value.OrderByDescending(l => Man(P.start[l.id], P.goal[kv.Key])).ThenBy(l => l.id, StringComparer.Ordinal).First().id;
            var order = X.nets.OrderBy(l => trunk[l.to] == l.id ? 0 : 1).ThenBy(l => Man(P.start[l.id], P.goal[l.to])).ThenBy(l => l.id, StringComparer.Ordinal).ToList();
            var rng = new System.Random(7); (int f, int len)? bestS = null; Dictionary<string, List<Vector2Int>> bestP = null; Dictionary<string, (string, Vector2Int, int)> bestM = null; List<string> bestF = null;
            for (int t = 0; t < 30; t++)
            {
                var blk = (bool[])blk0.Clone(); var own = new Dictionary<Vector2Int, HashSet<string>>(); var paths = new Dictionary<string, List<Vector2Int>>(); var merge = new Dictionary<string, (string, Vector2Int, int)>(); var failed = new List<Net>();
                foreach (var l in order)
                {
                    string D = l.to; var goals = new Dictionary<Vector2Int, (int d, Vector2Int m, int side, bool mg)>();
                    if (trunk[D] == l.id) { var g = P.goal[D]; goals[g] = ((X.fIn[D] + 2) % 4, default, 0, false); }
                    else if (paths.TryGetValue(trunk[D], out var tp))
                        foreach (var (e, d, m, sd) in MergeSpots(tp))
                        {
                            if (!Inside(e.x, e.y) || blk[Idx(e.x, e.y)]) continue; bool ok = true;
                            foreach (var cc in Connector(e, m)) if (Inside(cc.x, cc.y) && blk[Idx(cc.x, cc.y)] && !(own.TryGetValue(cc, out var ow) && ow.Count == 1 && ow.Contains(trunk[D]))) { ok = false; break; }
                            if (ok) goals[e] = (d, m, sd, true);
                        }
                    var s = P.start[l.id]; int sd0 = X.fOut[l.from];
                    if (goals.Count == 0) { failed.Add(l); continue; }
                    int need = trunk[D] == l.id && ins[D].Count > 1 ? MINRUN + (ins[D].Count - 1) * (ML + 2) + 2 : MINRUN;
                    var p = AStar(s, sd0, goals.ToDictionary(kv => kv.Key, kv => kv.Value.d), blk, need);
                    if (p == null) { failed.Add(l); continue; }
                    paths[l.id] = p; var gg = goals[p[p.Count - 1]]; var cells = new List<Vector2Int>(p);
                    if (gg.mg) { merge[l.id] = (trunk[D], gg.m, gg.side); cells.AddRange(Connector(p[p.Count - 1], gg.m)); }
                    foreach (var q in Halo(cells))
                    {
                        if (!Inside(q.x, q.y)) continue; int ix = Idx(q.x, q.y);
                        if (!blk[ix] || own.ContainsKey(q)) { if (!own.TryGetValue(q, out var hs2)) own[q] = hs2 = new HashSet<string>(); hs2.Add(l.id); }
                        blk[ix] = true;
                    }
                }
                int len = paths.Values.Sum(p => p.Count + 4 * Turns(p)); var sc = (failed.Count, len);
                if (bestS == null || sc.Item1 < bestS.Value.f || (sc.Item1 == bestS.Value.f && sc.len < bestS.Value.len)) { bestS = sc; bestP = paths; bestM = merge; bestF = failed.Select(f => f.id).ToList(); }
                if (failed.Count > 0) { var fs = new HashSet<Net>(failed); order = failed.Concat(order.Where(o => !fs.Contains(o))).ToList(); }
                else { for (int i = order.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var tmp = order[i]; order[i] = order[j]; order[j] = tmp; } }
                order = order.OrderBy(l => trunk[l.to] == l.id ? 0 : 1).ToList(); /* stabil: Hauptbänder vor ihren Zweigen */
            }
            foreach (var kv in bestP) P.cells[kv.Key] = kv.Value; foreach (var kv in bestM) P.merge[kv.Key] = kv.Value;
            foreach (var f in bestF) P.errors.Add("Kein Weg für Band " + f);
            return bestF.Count == 0;
        }
        static int Man(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        static int Turns(List<Vector2Int> p) { int n = 0; for (int i = 1; i + 1 < p.Count; i++) if (p[i] - p[i - 1] != p[i + 1] - p[i]) n++; return n; }

        // A* mit Zustand (Zelle, Richtung, gerade Schritte seit der letzten Kurve). Kurve +4, Schritt 1.
        static float[] gBuf; static int[] prevBuf; static readonly List<int> touched = new List<int>();
        static List<Vector2Int> AStar(Vector2Int s, int sd, Dictionary<Vector2Int, int> goals, bool[] blk, int need)
        {
            int RN = need + 1; int NS = N * N * 4 * RN;
            if (gBuf == null || gBuf.Length < NS) { gBuf = new float[NS]; prevBuf = new int[NS]; for (int i = 0; i < NS; i++) gBuf[i] = float.MaxValue; }
            foreach (int i in touched) gBuf[i] = float.MaxValue; touched.Clear();
            int St(int x, int y, int d, int r) => ((Idx(x, y) * 4) + d) * RN + r;
            var gl = goals.Keys.ToList();
            float H(int x, int y) { int b = int.MaxValue; foreach (var g in gl) b = Mathf.Min(b, Mathf.Abs(x - g.x) + Mathf.Abs(y - g.y)); return b; }
            var heap = new MinHeap(); int s0 = St(s.x, s.y, sd, 0); gBuf[s0] = 0; prevBuf[s0] = -1; touched.Add(s0); heap.Push(H(s.x, s.y), s0);
            int found = -1, guard = 0;
            while (heap.Count > 0 && guard++ < 600000)
            {
                heap.Pop(out float f, out int st); int run = st % RN, rest = st / RN, d = rest % 4, cell = rest / 4; int x = cell % N - EXT, y = cell / N - EXT; float g0 = gBuf[st];
                if (f - H(x, y) > g0 + 1e-3f) continue;
                var here = new Vector2Int(x, y);
                if (goals.TryGetValue(here, out int gd) && gd == d && run >= need) { found = st; break; }
                for (int turn = 0; turn < 3; turn++)
                {
                    int nd = turn == 0 ? d : turn == 1 ? (d + 1) % 4 : (d + 3) % 4; if (nd != d && run < MINRUN) continue;
                    int nx = x + DX[nd].x, ny = y + DX[nd].y; if (!Inside(nx, ny)) continue; var nq = new Vector2Int(nx, ny);
                    if (blk[Idx(nx, ny)] && !goals.ContainsKey(nq)) continue;
                    int nr = nd == d ? Mathf.Min(need, run + 1) : 1; float ng = g0 + 1 + (nd != d ? 4 : 0);
                    int ns = St(nx, ny, nd, nr); if (ng >= gBuf[ns]) continue;
                    if (gBuf[ns] == float.MaxValue) touched.Add(ns); gBuf[ns] = ng; prevBuf[ns] = st; heap.Push(ng + H(nx, ny), ns);
                }
            }
            if (found < 0) return null;
            var path = new List<Vector2Int>(); for (int st = found; st >= 0; st = prevBuf[st]) { int cell = st / RN / 4; path.Add(new Vector2Int(cell % N - EXT, cell / N - EXT)); }
            path.Reverse(); return path;
        }
        class MinHeap
        {
            readonly List<(float k, int v)> a = new List<(float, int)>(); public int Count => a.Count;
            public void Push(float k, int v) { a.Add((k, v)); int i = a.Count - 1; while (i > 0) { int p = (i - 1) / 2; if (a[p].k < k || (a[p].k == k && a[p].v <= v)) break; a[i] = a[p]; i = p; } a[i] = (k, v); }
            public void Pop(out float k, out int v)
            {
                k = a[0].k; v = a[0].v; var last = a[a.Count - 1]; a.RemoveAt(a.Count - 1); if (a.Count == 0) return; int i = 0;
                while (true) { int l = 2 * i + 1, r = l + 1, m = i; var cur = last; if (l < a.Count && Less(a[l], cur)) { m = l; cur = a[l]; } if (r < a.Count && Less(a[r], cur)) m = r; if (m == i) break; a[i] = a[m]; i = m; }
                a[i] = last;
            }
            static bool Less((float k, int v) p, (float k, int v) q) => p.k < q.k || (p.k == q.k && p.v < q.v);
        }

        // ---------- Prüfung (unabhängig von der Wegsuche) ----------
        public static void Validate(FactoryPlan P, bool[] mask)
        {
            var E = P.errors; var all = new Dictionary<string, List<Vector2Int>>();
            foreach (var kv in P.cells)
            {
                var c = new List<Vector2Int>(kv.Value); if (P.merge.TryGetValue(kv.Key, out var mg)) c.AddRange(Connector(c[c.Count - 1], mg.m)); all[kv.Key] = c;
                var p = kv.Value; if (p.Count < 2) { E.Add(kv.Key + ": zu kurz"); continue; }
                // Anfang am eigenen Ausgang, Ende am Eingang oder exakt an einem Y-Platz des Hauptbandes
                if (!P.start.TryGetValue(kv.Key, out var s) || p[0] != s) E.Add(kv.Key + ": beginnt nicht am Ausgang");
                for (int i = 1; i < p.Count; i++) if (Man(p[i], p[i - 1]) != 1) { E.Add(kv.Key + ": Lücke im Weg"); break; }
                int run = 0; for (int i = 1; i < p.Count; i++) { bool turn = i >= 2 && p[i] - p[i - 1] != p[i - 1] - p[i - 2]; if (turn && run < MINRUN) { E.Add(kv.Key + ": Kurve zu eng bei " + p[i - 1]); break; } run = turn ? 1 : run + 1; }
                foreach (var q in p) if (!Inside(q.x, q.y) || !mask[Idx(q.x, q.y)]) { E.Add(kv.Key + ": außerhalb der Basis bei " + q); break; }
                for (int i = 1; i < p.Count - 1; i++) foreach (var b in P.pos) { int h = P.half[b.Key]; if (Mathf.Abs(p[i].x - b.Value.x) <= h + 1 && Mathf.Abs(p[i].y - b.Value.y) <= h + 1) { E.Add(kv.Key + ": läuft durch " + b.Key); i = p.Count; break; } }
                if (mg.trunk != null)
                {
                    if (!P.cells.TryGetValue(mg.trunk, out var tp) || !tp.Contains(mg.m)) E.Add(kv.Key + ": Y-Stück sitzt nicht auf dem Hauptband " + mg.trunk);
                    else { int at = tp.IndexOf(mg.m); bool straight = at >= ML + 1 && at + 1 < tp.Count; for (int i = at - ML; straight && i < at + 1; i++) if (tp[i + 1] - tp[i] != tp[at] - tp[at - 1]) straight = false; if (!straight) E.Add(kv.Key + ": Y-Stück nicht auf geradem Stück"); }
                }
            }
            // Abstand zwischen Bändern (Mittellinien ≥ 2 m, außer am eigenen Y)
            var keys = all.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            for (int i = 0; i < keys.Count; i++) for (int j = i + 1; j < keys.Count; j++)
                {
                    string A = keys[i], B = keys[j]; Vector2Int? mA = P.merge.TryGetValue(A, out var ma) && ma.trunk == B ? ma.m : (Vector2Int?)null, mB = P.merge.TryGetValue(B, out var mb) && mb.trunk == A ? mb.m : (Vector2Int?)null;
                    var setB = new HashSet<Vector2Int>(all[B]); bool hit = false;
                    foreach (var q in all[A])
                    {
                        for (int dy = -CLR + 1; dy < CLR && !hit; dy++) for (int dx = -CLR + 1; dx < CLR; dx++)
                            {
                                var o = new Vector2Int(q.x + dx, q.y + dy); if (!setB.Contains(o)) continue;
                                var near = mA ?? mB; if (near.HasValue && Man(q, near.Value) <= ML + 2) continue;
                                E.Add("Bänder " + A + " und " + B + " zu nah bei " + q); hit = true; break;
                            }
                        if (hit) break;
                    }
                }
            // Gebäude: in der Basis, ohne Überlappung
            var ids = P.pos.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            foreach (var k in ids) { var b = P.pos[k]; int h = P.half[k]; if (!RectIn(mask, b.x - h, b.y - h, b.x + h, b.y + h)) E.Add(k + ": außerhalb der Basis"); }
            for (int i = 0; i < ids.Count; i++) for (int j = i + 1; j < ids.Count; j++) { var a = P.pos[ids[i]]; var b = P.pos[ids[j]]; int need = P.half[ids[i]] + P.half[ids[j]] + 2; if (Mathf.Abs(a.x - b.x) < need && Mathf.Abs(a.y - b.y) < need) E.Add(ids[i] + " und " + ids[j] + " überlappen"); }
            // jeder Eingang mit genau einem Hauptband verbunden, jedes Band mit Ziel
            foreach (var g in P.goal) { int n = P.cells.Count(kv => !P.merge.ContainsKey(kv.Key) && kv.Value.Count > 0 && kv.Value[kv.Value.Count - 1] == g.Value); if (n != 1) E.Add(g.Key + ": Eingang hat " + n + " Hauptbänder"); }
        }

        // ---------- Linienform: Ecken als Kreisbögen ----------
        static List<Vector2> Dedupe(List<Vector2> p) { var o = new List<Vector2>(); foreach (var q in p) if (o.Count == 0 || (q - o[o.Count - 1]).sqrMagnitude > 1e-4f) o.Add(q); return o; }
        /// <summary>Ecken als Kreisbögen mit Radius r (kleiner, wenn die Strecken kurz sind); gerade Zwischenpunkte fallen weg.</summary>
        public static Vector2[] Round(List<Vector2> p, float r)
        {
            p = Dedupe(p); if (p.Count < 3) return p.ToArray();
            for (int i = 1; i < p.Count - 1;) { var d1 = p[i] - p[i - 1]; var d2 = p[i + 1] - p[i]; if (Mathf.Abs(d1.x * d2.y - d1.y * d2.x) < 1e-4f * d1.magnitude * Mathf.Max(1e-3f, d2.magnitude) && Vector2.Dot(d1, d2) > 0) p.RemoveAt(i); else i++; }
            var o = new List<Vector2> { p[0] };
            for (int i = 1; i < p.Count - 1; i++)
            {
                var a = p[i - 1]; var b = p[i]; var c = p[i + 1]; var u = a - b; var w = c - b; float lu = u.magnitude, lw = w.magnitude; if (lu < 1e-4f || lw < 1e-4f) continue; u /= lu; w /= lw;
                float cosA = Mathf.Clamp(Vector2.Dot(u, w), -1, 1); if (cosA < -.999f) { o.Add(b); continue; }
                float ang = Mathf.Acos(cosA), t = r / Mathf.Tan(ang / 2); t = Mathf.Min(t, lu * .5f, lw * .5f); float rr = t * Mathf.Tan(ang / 2);
                var p0 = b + u * t; var p1 = b + w * t; var ctr = b + (u + w).normalized * Mathf.Sqrt(rr * rr + t * t);
                float a0 = Mathf.Atan2(p0.y - ctr.y, p0.x - ctr.x), a1 = Mathf.Atan2(p1.y - ctr.y, p1.x - ctr.x), da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                for (int k = 0; k <= 8; k++) { float aa = a0 + da * k / 8f; o.Add(ctr + new Vector2(Mathf.Cos(aa), Mathf.Sin(aa)) * rr); }
            }
            o.Add(p[p.Count - 1]); return o.ToArray();
        }
    }
}