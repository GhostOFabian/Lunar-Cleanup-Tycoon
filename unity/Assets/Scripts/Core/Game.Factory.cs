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
            if (planCache.TryGetValue(key, out var c)) { ps.plan = c.P; return c.L; }
            var pd = ps.pd; var pinned = new Dictionary<string, Vector2>();
            if (ps.sv.bMove != null) foreach (var m in ps.sv.bMove) if (!FactoryPlanner.InModule(GroupId(m.id))) pinned[GroupId(m.id)] = new Vector2(m.x, m.y);
            bool InBase(int x, int y) { int cc = pd.CellOf(pd.PlanDir(x, y)); return cc >= 0 && pd.cells[cc].IsBase && !pd.cells[cc].Wet; }
            var P = FactoryPlanner.Plan(sk, T, InBase, pinned);
            if (!P.ok) Debug.LogWarning("Lunar Cleanup: Fabrik-Planer " + sk + ": " + string.Join(" | ", P.errors));
            else Debug.Log("Lunar Cleanup: Fabrik-Planer " + sk + " ok in " + P.ms + " ms (Lage " + P.rot + "/" + P.mir + " " + P.shift + ")");
            var L = P.ok ? Build(T, P) : T; /* ohne gültigen Plan: Vorlage unverändert (Prüfung meldet den Fehler) */
            planCache[key] = (L, P); ps.plan = P; return L;
        }

        /// <summary>Editor/Test: Ergebnis des Planers für den aktuellen Planeten.</summary>
        public static string DevPlanInfo()
        {
            var P = Cur?.plan; if (P == null) return "kein Plan"; return (P.ok ? "ok" : "FEHLER") + " " + P.key + " " + P.ms + " ms, Lage " + P.rot + "/" + P.mir + " " + P.shift + ", Bänder " + P.cells.Count + ", Y " + P.merge.Count + (P.errors.Count > 0 ? " | " + string.Join(" | ", P.errors.Take(6)) : "");
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
                var P = FactoryPlanner.Plan(k, OneStation(GameData.Layouts[k]), InBase); ms += P.ms; mx = System.Math.Max(mx, P.ms);
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
            Vector2 mp = m; int best = 0; float bd = 1e9f; for (int i = 0; i < tr.Length; i++) { float d = (tr[i] - mp).sqrMagnitude; if (d < bd) { bd = d; best = i; } }
            for (int i = best + 1; i < tr.Length; i++) yield return tr[i];
        }
        static float Len(Vector2[] p) { float s = 0; for (int i = 1; i < p.Length; i++) s += (p[i] - p[i - 1]).magnitude; return s; }
        static Vector2 FreePoint(FactoryPlan P, Vector2 q)
        {
            bool Free(Vector2 p) { foreach (var kv in P.pos) { float h = P.half[kv.Key] + 1.5f; if (Mathf.Abs(p.x - kv.Value.x) < h && Mathf.Abs(p.y - kv.Value.y) < h) return false; } var c = new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)); return P.InBase(p) && !P.reserved.Contains(c); }
            if (Free(q)) return q;
            for (int r = 1; r < 30; r++) for (int k = 0; k < 16; k++) { float a = k * Mathf.PI / 8; var p = q + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; if (Free(p)) return p; }
            return q;
        }
    }
}
