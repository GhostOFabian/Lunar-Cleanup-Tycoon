// Fabian 10.11.: Layout eines Planeten kommt aus dem Fabrik-Planer (Gebäude, Anschlüsse, Bänder, Y-Stücke gemeinsam geplant).
// Die Simulation rechnet mit der Länge der verlegten Bänder (Zweigbänder: eigener Weg + Y + restliches Hauptband bis zum Eingang).
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        static readonly Dictionary<string, (LayoutDef L, FactoryPlan P)> planCache = new Dictionary<string, (LayoutDef, FactoryPlan)>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetPlans() => planCache.Clear();

        /// <summary>Geplantes Layout für diesen Planeten (Vorlage + Planer + verschobene Nebengebäude). Ergebnis je Größe und Verschiebungen gemerkt.</summary>
        static LayoutDef PlannedLayout(PState ps)
        {
            string sk = GameData.SizeKey(ps.pid, ps.sv.freq); var T = OneStation(GameData.Layouts[sk]); string key = sk + MoveKey(ps);
            if (planCache.TryGetValue(key, out var c)) { ps.plan = c.P; Grow(pd0(ps), c.P); return c.L; }
            var pd = ps.pd; var pinned = new Dictionary<string, Vector2>();
            if (ps.sv.bMove != null) foreach (var m in ps.sv.bMove) if (!FactoryPlanner.InModule(GroupId(m.id))) pinned[GroupId(m.id)] = new Vector2(m.x, m.y);
            bool InBase(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && pd.cells[cc].IsBase && !pd.cells[cc].Wet; }
            int normal = GameData.Kinds.FindIndex(k => k.key == "normal");
            bool CanGrow(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && !pd.cells[cc].Wet && pd.cells[cc].kind == normal; }
            var P = FactoryPlanner.Plan(sk, T, InBase, pinned, CanGrow); Grow(pd, P);
            if (!P.ok) Debug.LogWarning("Lunar Cleanup: Fabrik-Planer " + sk + ": " + string.Join(" | ", P.errors));
            else Debug.Log("Lunar Cleanup: Fabrik-Planer " + sk + " ok in " + P.ms + " ms (Lage " + P.rot + "/" + P.mir + " " + P.shift + ")");
            var L = P.ok ? Build(T, P) : T; /* ohne gültigen Plan: Vorlage unverändert (Prüfung meldet den Fehler) */
            planCache[key] = (L, P); ps.plan = P; return L;
        }

        static PlanetData pd0(PState ps) => ps.pd;
        /// <summary>11.11.: Parzellen unter dem Landeplatz am Basisrand werden Basis (immer gleich für diesen Plan – auch nach dem Laden).</summary>
        static void Grow(PlanetData pd, FactoryPlan P)
        {
            if (P == null || P.grow.Count == 0) return; int bk = GameData.Kinds.FindIndex(k => k.key == "base"); if (bk < 0) return;
            foreach (var g in P.grow) { int cc = pd.CellOf(pd.PlanDir(g.x, g.y)); if (cc >= 0 && !pd.cells[cc].Wet && pd.cells[cc].kind != bk) { pd.cells[cc].kind = bk; pd.cells[cc].ring = 0; } }
        }
        /// <summary>Nach dem Wachsen: neue Basisparzellen sauber, Flaggen/Kristalle/Bohrtürme/Außenposten darauf zur nächsten freien Parzelle.</summary>
        static void GrewBase(PState ps)
        {
            var pd = ps.pd; var sv = ps.sv;
            for (int i = 0; i < pd.N; i++) if (pd.cells[i].IsBase && !ps.clean[i]) ps.clean[i] = true;
            int Near(int c)
            {
                var seen = new HashSet<int> { c }; var q = new Queue<int>(); q.Enqueue(c);
                while (q.Count > 0) { int x = q.Dequeue(); var cl = pd.cells[x]; if (!cl.IsBase && !cl.Wet) return x; foreach (int nb in cl.nb) if (seen.Add(nb)) q.Enqueue(nb); }
                return c;
            }
            if (sv.flags != null) foreach (var f in sv.flags) if (f.cell >= 0 && f.cell < pd.N && pd.cells[f.cell].IsBase) { f.cell = Near(f.cell); f.x = f.y = 0; }
            if (sv.crystals != null) foreach (var k in sv.crystals) if (k.cell >= 0 && k.cell < pd.N && pd.cells[k.cell].IsBase) { k.cell = Near(k.cell); k.x = k.y = 0; }
            if (sv.rigs != null) foreach (var r in sv.rigs) if (r.cell >= 0 && r.cell < pd.N && pd.cells[r.cell].IsBase) r.cell = Near(r.cell);
            if (sv.outposts != null) foreach (var o in sv.outposts) if (o.cell >= 0 && o.cell < pd.N && pd.cells[o.cell].IsBase) o.cell = Near(o.cell);
        }
        /// <summary>Editor/Test: Landeplatz auf allen Layouts (Lage, Größe, gewachsene Basisfläche).</summary>
        public static string DevPadAll()
        {
            var sb = new System.Text.StringBuilder(); int grown = 0, n = 0;
            foreach (var k in GameData.Layouts.Keys)
            {
                string pid = k.Split('@')[0]; if (!GameData.Planets.ContainsKey(pid) || !GameData.Cells.ContainsKey(k)) continue; var pd = PlanetData.GetKey(pid, k); n++;
                bool InBase(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && pd.cells[cc].IsBase && !pd.cells[cc].Wet; }
                int normal = GameData.Kinds.FindIndex(q => q.key == "normal"); bool CanGrow(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && !pd.cells[cc].Wet && pd.cells[cc].kind == normal; }
                var P = FactoryPlanner.Plan(k, OneStation(GameData.Layouts[k]), InBase, null, CanGrow);
                if (!P.hasPad) sb.Append(k).Append(": KEIN Landeplatz | "); else if (P.padOutside) { grown++; sb.Append(k).Append(": Basis +").Append(P.grow.Count).Append(" | "); }
            }
            return n + " Layouts, " + grown + " mit gewachsener Basis | " + sb;
        }
        /// <summary>Editor/Test: Schiffe auf den Landeplatz schicken (nur Darstellung).</summary>
        public static string DevShip() { OnSupply?.Invoke(S.planet, Goods[0].id, 1); return "Lieferschiff"; }
        public static string DevVisitor() { OnOrderDone?.Invoke(new VisitOrder { pid = S.planet }); return "Händlerschiff (Abholung)"; }
        public static string DevCashShip() { OnCashSent?.Invoke(S.planet); return "Zuschuss-Schiff"; }
        /// <summary>Editor/Test: gelbe/rote Ampeln des aktuellen Planeten.</summary>
        public static string DevFlow() { var ps = Cur; var r = FlowReport(ps).Where(f => f.lvl >= 1).Select(f => (f.lvl == 2 ? "ROT " : "GELB ") + f.name + ": " + f.why).ToList(); return ps.pid + " " + (r.Count == 0 ? "alles grün" : string.Join(" | ", r)); }
        public static string DevPad() { var P = Cur?.plan; return P == null ? "kein Plan" : (P.hasPad ? "Landeplatz " + P.pad + " h" + P.padH + (P.padOutside ? " gewachsen " + P.grow.Count : "") : "kein Landeplatz"); }
        /// <summary>Editor/Test: Ergebnis des Planers für den aktuellen Planeten.</summary>
        public static string DevPlanInfo()
        {
            var P = Cur?.plan; if (P == null) return "kein Plan"; return (P.ok ? "ok" : "FEHLER") + " " + P.key + " " + P.ms + " ms, Lage " + P.rot + "/" + P.mir + " " + P.shift + ", Bänder " + P.cells.Count + ", Y " + P.merge.Count + (P.errors.Count > 0 ? " | " + string.Join(" | ", P.errors.Take(6)) : "");
        }
        public static string DevBeltInfo()
        {
            var ps = Cur; var sb = new System.Text.StringBuilder();
            foreach (var b in ps.lay.belts) { if (ps.plan == null || !ps.plan.cells.ContainsKey(b.id)) continue; ps.belts.TryGetValue(b.id, out var its); sb.Append(b.id).Append(ps.plan.merge.ContainsKey(b.id) ? "(Y)" : "").Append(" L ").Append(b.L.ToString("0.0")).Append(" rp ").Append(Len(b.rp).ToString("0.0")).Append(" live ").Append(BeltLive(ps, b) ? 1 : 0).Append(" n ").Append(its?.Count ?? 0).Append(" max ").Append(its != null && its.Count > 0 ? its.Max(i => i.s).ToString("0.0") : "-").Append(" | "); }
            return sb.ToString();
        }
        public static System.Func<string> FootprintHook;
        public static string DevFootprints() => FootprintHook != null ? FootprintHook() : "keine Ansicht";
        /// <summary>Editor/Test: alle 49 Layouts planen und prüfen.</summary>
        public static string DevPlanAll()
        {
            int ok = 0, n = 0; long ms = 0, mx = 0; var bad = new List<string>();
            foreach (var k in GameData.Layouts.Keys)
            {
                string pid = k.Split('@')[0]; if (!GameData.Planets.ContainsKey(pid) || !GameData.Cells.ContainsKey(k)) continue; var pd = PlanetData.GetKey(pid, k); n++;
                bool InBase(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && pd.cells[cc].IsBase && !pd.cells[cc].Wet; }
                int normal = GameData.Kinds.FindIndex(q => q.key == "normal"); bool CanGrow(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && !pd.cells[cc].Wet && pd.cells[cc].kind == normal; }
                var P = FactoryPlanner.Plan(k, OneStation(GameData.Layouts[k]), InBase, null, CanGrow); ms += P.ms; if (!P.hasPad) bad.Add(k + ": kein Landeplatz"); mx = System.Math.Max(mx, P.ms);
                if (P.ok) ok++; else bad.Add(k + ": " + string.Join("; ", P.errors.Take(3)));
            }
            return ok + "/" + n + " ok, im Mittel " + (ms / System.Math.Max(1, n)) + " ms, höchstens " + mx + " ms" + (bad.Count > 0 ? " | " + string.Join(" | ", bad.Take(8)) : "");
        }
        static LayoutDef Build(LayoutDef T, FactoryPlan P)
        {
            var L = new LayoutDef { pos = new Dictionary<string, Vector2>(T.pos), yaw = new Dictionary<string, float>(T.yaw), shop = T.shop, start = T.start, park = T.park };
            foreach (var kv in P.pos) L.pos[kv.Key] = kv.Value;
            foreach (var u in Units) if (L.pos.ContainsKey(u) && P.pos.ContainsKey("u1")) { L.pos[u] = P.pos["u1"]; L.yaw[u] = P.unitYaw; }
            // Werkstatt-Platz, Startplatz und Parkplatz wandern mit dem nächsten Gebäude mit und bleiben frei
            Vector2 Follow(Vector2 p)
            {
                string nb = null; float bd = 1e9f; foreach (var kv in T.pos) { if (!P.pos.ContainsKey(kv.Key)) continue; float d = (kv.Value - p).sqrMagnitude; if (d < bd) { bd = d; nb = kv.Key; } }
                var q = nb == null ? p : p + ((Vector2)P.pos[nb] - T.pos[nb]); return FreePoint(P, q);
            }
            L.shop = Follow(T.shop); L.start = Follow(T.start); L.park = Follow(T.park);
            // Bänder: sichtbare aus dem Plan, unsichtbare (u2, u3) gerade von der Entladestation
            var lines = new Dictionary<string, Vector2[]>();
            foreach (var b in T.belts) if (P.cells.ContainsKey(b.id)) lines[b.id] = Line(b, P);
            foreach (var b in T.belts)
            {
                var nb = new BeltDef { id = b.id, from = b.from, to = b.to, h = .5f, stage = b.stage, port0 = b.port0, port1 = b.port1 };
                if (lines.TryGetValue(b.id, out var rp))
                {
                    if (P.merge.TryGetValue(b.id, out var mg) && lines.TryGetValue(mg.trunk, out var tr)) rp = rp.Concat(TailFrom(tr, mg.m)).ToArray(); /* Waren laufen über das Hauptband bis ins Gebäude */
                    nb.rp = rp; nb.L = Len(rp);
                }
                else { var a = L.pos.ContainsKey(b.from) ? L.pos[b.from] : b.rp[0]; var e = L.pos.ContainsKey(b.to) ? L.pos[b.to] : b.rp[b.rp.Length - 1]; nb.rp = new[] { a, e }; nb.L = Mathf.Max(b.L, (e - a).magnitude); }
                L.belts.Add(nb);
            }
            return L;
        }
        /// <summary>Sichtbare Linie eines Bandes: 1,6 m im Gebäude, Rasterweg mit runden Kurven (1 m), beim Y schräg bis auf die Mitte des Hauptbandes.</summary>
        static Vector2[] Line(BeltDef b, FactoryPlan P)
        {
            var cells = P.cells[b.id]; var pts = new List<Vector2>();
            int fo = P.faceOut[b.from]; var n0 = (Vector2)FactoryPlanner.Dir(fo); pts.Add((Vector2)cells[0] - n0 * 2.2f); pts.Add((Vector2)cells[0] - n0 * .6f);
            for (int i = 0; i < cells.Count; i++) { if (i == 0 || i == cells.Count - 1 || cells[i] - cells[i - 1] != cells[i + 1] - cells[i]) pts.Add(cells[i]); }
            if (P.merge.TryGetValue(b.id, out var mg)) pts.Add(mg.m);
            else { int fi = P.faceIn[b.to]; var n1 = (Vector2)FactoryPlanner.Dir(fi); pts.Add((Vector2)cells[cells.Count - 1] - n1 * .6f); pts.Add((Vector2)cells[cells.Count - 1] - n1 * 2.2f); }
            return FactoryPlanner.Round(pts, 1f);
        }
        /// <summary>Hauptband ab dem Y-Punkt m bis zum Ende (ohne m selbst).</summary>
        static IEnumerable<Vector2> TailFrom(Vector2[] tr, Vector2Int m)
        {
            /* 10.11.: nächster Abschnitt (nicht Punkt) – gerade Hauptbänder haben nach dem Glätten nur Anfangs- und Endpunkt */
            Vector2 mp = m; int best = 1; float bd = 1e9f;
            for (int i = 1; i < tr.Length; i++) { var a = tr[i - 1]; var d = tr[i] - a; float l2 = d.sqrMagnitude; float t = l2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(mp - a, d) / l2) : 0; float dd = (a + d * t - mp).sqrMagnitude; if (dd < bd) { bd = dd; best = i; } }
            for (int i = best; i < tr.Length; i++) yield return tr[i];
        }
        static float Len(Vector2[] p) { float s = 0; for (int i = 1; i < p.Length; i++) s += (p[i] - p[i - 1]).magnitude; return s; }
        static Vector2 FreePoint(FactoryPlan P, Vector2 q)
        {
            bool Free(Vector2 p) { foreach (var kv in P.pos) { float h = P.half[kv.Key] + 1.5f; if (Mathf.Abs(p.x - kv.Value.x) < h && Mathf.Abs(p.y - kv.Value.y) < h) return false; } if (P.hasPad && Mathf.Abs(p.x - P.pad.x) < P.padH + 1.5f && Mathf.Abs(p.y - P.pad.y) < P.padH + 1.5f) return false; var c = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)); return P.InBase(p) && !P.reserved.Contains(c); }
            if (Free(q)) return q;
            for (int r = 1; r < 30; r++) for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; var p = q + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; if (Free(p)) return p; }
            return q;
        }
    }
}
