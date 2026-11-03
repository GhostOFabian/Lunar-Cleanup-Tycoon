// Fabian 03.11.: Perk-Shop für den Hard Mode – Werkzeuge, Buffs und kleine Putzhelfer, gekauft mit Credits.
// Werkzeuge (Ladungen): Putzbombe (bis 5 Parzellen auf einmal), Putz-Nuke (bis 60 Parzellen im großen Umkreis).
// Buffs (90 s): Turbo-Wischen (Kraft ×2,5), Breiter Pinsel (Radius ×1,7), Magnet-Behälter (Behälter ×3).
// Putzhelfer: laufen mit dem Besen herum und putzen selbst Parzelle für Parzelle (auch offline, je Planet bis 8).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class HelperSave { public int id, cell = -1; public float t; public Vector3 d; }
    public class PerkDef { public string id, name, desc, icon; public int kind; public float dur; } // kind 0 = Werkzeug (Ziel antippen), 1 = Buff, 2 = Helfer
    public static partial class Game
    {
        public static readonly List<PerkDef> Perks = new List<PerkDef>
        {
            new PerkDef { id = "bomb", name = "Putzbombe", icon = "B", kind = 0, desc = "Reinigt bis zu 5 Parzellen auf einmal rund um die Stelle, die du antippst" },
            new PerkDef { id = "nuke", name = "Putz-Nuke", icon = "N", kind = 0, desc = "Reinigt auf einen Schlag so viel wie möglich – bis zu 60 Parzellen im großen Umkreis" },
            new PerkDef { id = "turbo", name = "Turbo-Wischen", icon = "T", kind = 1, dur = 90, desc = "90 s lang wischst du mit 2,5-facher Kraft" },
            new PerkDef { id = "wide", name = "Breiter Pinsel", icon = "P", kind = 1, dur = 90, desc = "90 s lang ist dein Pinsel 1,7-mal so breit" },
            new PerkDef { id = "magnet", name = "Magnet-Behälter", icon = "M", kind = 1, dur = 90, desc = "90 s lang fasst dein Behälter dreimal so viel" },
            new PerkDef { id = "helper", name = "Putzhelfer", icon = "H", kind = 2, desc = "Ein kleiner Helfer mit Besen putzt dauerhaft Parzelle für Parzelle auf diesem Planeten – auch wenn du offline bist (bis zu 8 je Planet)" },
        };
        public const int HELPER_MAX = 8;
        public static PerkDef Perk(string id) => Perks.FirstOrDefault(p => p.id == id);
        public static bool PerksOpen => Hard;
        public static int PerkN(string id) { var e = S.perkN.Find(x => x.k == id); return e == null ? 0 : e.v; }
        static void PerkAdd(string id, int n) { var e = S.perkN.Find(x => x.k == id); if (e == null) { e = new KV { k = id, v = 0 }; S.perkN.Add(e); } e.v = Math.Max(0, e.v + n); }
        public static float BuffLeft(string id) { var e = S.buffT.Find(x => x.k == id); return e == null ? 0 : e.v; }
        static float BuffF(string id, float f) => Hard && BuffLeft(id) > 0 ? f : 1;
        public static float PowerF => BuffF("turbo", 2.5f); public static float RadiusF => BuffF("wide", 1.7f); public static float HandF => BuffF("magnet", 3f);
        /// <summary>Preis: wächst mit Schwierigkeit und Basisstufe des aktuellen Planeten (Hard-Mode-Einnahmen ×3 sind eingerechnet).</summary>
        public static double PerkCost(string id)
        {
            var ps = Cur; double b = 400 * ps.P.diff * Math.Pow(2.6, Math.Max(0, Gs(ps) - 1));
            switch (id) { case "bomb": return Math.Round(b); case "nuke": return Math.Round(b * 10); case "turbo": case "wide": return Math.Round(b * .8); case "magnet": return Math.Round(b * .6); case "helper": return Math.Round(b * 6 * Math.Pow(1.6, ps.sv.helpers.Count)); }
            return 0;
        }
        public static string PerkWhy(string id) { if (!Hard) return "Nur im Hard Mode"; if (id == "helper" && Cur.sv.helpers.Count >= HELPER_MAX) return "Höchstens " + HELPER_MAX + " Helfer je Planet"; return S.credits < PerkCost(id) ? Lacks(PerkCost(id)) : ""; }
        public static bool BuyPerk(string id)
        {
            string w = PerkWhy(id); if (w != "") return Fail(w); var ps = Cur; S.credits -= PerkCost(id);
            if (id == "helper") { ps.sv.helpers.Add(new HelperSave { id = ++ps.sv.helperNext, d = ps.pd.PlanDir(ps.lay.start) }); Toast("Neuer Putzhelfer auf " + ps.P.name + " – er legt gleich los", "good"); }
            else { PerkAdd(id, 1); Toast(Perk(id).name + " gekauft · " + PerkN(id) + " bereit (Leiste unten)", "good"); }
            Save(); return true;
        }
        /// <summary>Buff starten (verbraucht eine Ladung).</summary>
        public static bool UseBuff(string id)
        {
            if (PerkN(id) <= 0) return Fail("Keine Ladung – im Perk-Shop kaufen"); PerkAdd(id, -1);
            var e = S.buffT.Find(x => x.k == id); if (e == null) { e = new FKV { k = id }; S.buffT.Add(e); } e.v += Perk(id).dur;
            Toast(Perk(id).name + " aktiv · " + Mathf.RoundToInt(e.v) + " s", "good"); return true;
        }
        /// <summary>Werkzeug an der Stelle d einsetzen. Rückgabe: gereinigte Parzellen.</summary>
        public static int UseTool(string id, Vector3 d)
        {
            var ps = Cur; var pd = ps.pd; if (PerkN(id) <= 0) { Fail("Keine Ladung – im Perk-Shop kaufen"); return 0; }
            int max = id == "nuke" ? 60 : 5; float reach = id == "nuke" ? 45f : 12f; int c0 = pd.CellOf(d);
            /* Ausbreitung vom Ziel aus über reinigbare Parzellen (jede fertige macht ihre Nachbarn reinigbar) */
            var q = new List<int> { c0 }; var seen = new HashSet<int> { c0 }; int done = 0; float got = 0;
            while (q.Count > 0 && done < max)
            {
                q.Sort((a, b) => pd.Arc(pd.cells[a].c, d).CompareTo(pd.Arc(pd.cells[b].c, d))); int c = q[0]; q.RemoveAt(0);
                if (pd.Arc(pd.cells[c].c, d) > reach) continue;
                if (!ps.clean[c] && Workable(ps, c) && !pd.cells[c].IsBase)
                {
                    var a = EnsureAct(ps, c); float frac = a != null ? a.sum / Mathf.Max(1, a.cl.nIn) : 1; got += ParcelYield(pd.cells[c]) * frac;
                    int m = GameData.Dirt[pd.cells[c].type].mat; float room = HandCap() - HandSum(); float add = Mathf.Min(room, ParcelYield(pd.cells[c]) * frac); if (add > 0) ps.sv.hand[m] += add; /* der Schmutz landet im Behälter, so weit er passt */
                    FinishCell(ps, c); done++; OnBoom?.Invoke(pd.cells[c].c);
                }
                if (ps.clean[c] || c == c0) foreach (int b in pd.cells[c].nb) if (seen.Add(b)) q.Add(b);
            }
            if (done == 0) { Toast("Hier gibt es nichts zu reinigen – tippe auf dreckige Parzellen am Rand des Sauberen", "bad"); return 0; }
            PerkAdd(id, -1); Toast(Perk(id).name + ": " + done + " Parzellen auf einen Schlag sauber!", "good"); Save(); return done;
        }
        /// <summary>Simulation (live und offline): Buffs laufen ab, Helfer putzen.</summary>
        static void PerkStep(PState ps, float dt)
        {
            if (!Hard) return;
            if (ps.pid == S.planet) foreach (var e in S.buffT) if (e.v > 0) e.v = Mathf.Max(0, e.v - dt);
            if (ps.sv.helpers.Count == 0) return; var pd = ps.pd;
            foreach (var h in ps.sv.helpers)
            {
                if (h.cell >= 0 && (ps.clean[h.cell] || !Workable(ps, h.cell))) h.cell = -1;
                if (h.cell < 0)
                {
                    /* nächste reinigbare Parzelle, nicht die eines anderen Helfers */
                    int best = -1; float bd = 1e9f; foreach (int c in ps.front) { if (pd.cells[c].Wet || ps.sv.helpers.Any(o => o != h && o.cell == c)) continue; float a = pd.Arc(pd.cells[c].c, h.d); if (a < bd) { bd = a; best = c; } }
                    h.cell = best; h.t = 0; if (best < 0) continue;
                }
                var tgt = pd.cells[h.cell].c.normalized; float dist = pd.Arc(h.d, tgt);
                if (dist > 1.5f) { h.d = Vector3.Slerp(h.d, tgt, Mathf.Min(1, 2.2f * dt / Mathf.Max(.01f, dist))).normalized; continue; } /* läuft hin, 2,2 m/s */
                var cl = pd.cells[h.cell]; float need = 18f * Mathf.Max(.6f, cl.H / 4f) * GameData.Dirt[cl.type].hard * GameData.Kinds[cl.kind].hard;
                h.t += dt; if (h.t < need) continue;
                int m = GameData.Dirt[cl.type].mat; float room = RohCap(ps) - RohSum(ps); if (room > 0) ps.sv.roh[m] += Mathf.Min(room, ParcelYield(cl) * .6f); /* trägt den Schmutz zur Basis (etwas geht verloren) */
                FinishCell(ps, h.cell); h.cell = -1; h.t = 0;
            }
        }
    }
}
