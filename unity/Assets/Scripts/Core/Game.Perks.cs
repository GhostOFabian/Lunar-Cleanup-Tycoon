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
    [Serializable] public class HelperSave { public int id, cell = -1; public float t, life; public Vector3 d; } /* life > 0: Tornado (verschwindet) */
    public class PerkDef { public string id, name, desc, icon; public int kind; public float dur; } // kind 0 = Werkzeug (Ziel antippen), 1 = Buff, 2 = Helfer
    public static partial class Game
    {
        public static readonly List<PerkDef> Perks = new List<PerkDef>
        {
            new PerkDef { id = "bomb", name = "Putzbombe", icon = "B", kind = 0, desc = "Reinigt bis zu 5 Parzellen auf einmal rund um die Stelle, die du antippst" },
            new PerkDef { id = "nuke", name = "Putz-Nuke", icon = "N", kind = 0, desc = "Reinigt auf einen Schlag so viel wie möglich – bis zu 60 Parzellen im großen Umkreis" },
            new PerkDef { id = "kaercher", name = "Hochdruck-Kärcher", icon = "K", kind = 0, desc = "Eine schnurgerade Schneise: reinigt bis zu 8 Parzellen in einer Linie, von der Basis aus durch die Stelle, die du antippst" },
            new PerkDef { id = "foam", name = "Schaumkanone", icon = "S", kind = 0, desc = "Weicht im Umkreis von 15 m allen Schmutz ein – danach wischst du ihn mit einem Streich weg (70 % schon gelöst)" },
            new PerkDef { id = "tornado", name = "Staubsauger-Tornado", icon = "W", kind = 0, desc = "Ein kleiner Wirbelsturm saust 25 Sekunden über den Planeten und saugt Parzelle um Parzelle weg" },
            new PerkDef { id = "turbo", name = "Turbo-Wischen", icon = "T", kind = 1, dur = 90, desc = "90 s lang wischst du mit 2,5-facher Kraft" },
            new PerkDef { id = "wide", name = "Breiter Pinsel", icon = "P", kind = 1, dur = 90, desc = "90 s lang ist dein Pinsel 1,7-mal so breit" },
            new PerkDef { id = "magnet", name = "Magnet-Behälter", icon = "M", kind = 1, dur = 90, desc = "90 s lang fasst dein Behälter dreimal so viel" },
            new PerkDef { id = "espresso", name = "Espresso für die Helfer", icon = "E", kind = 1, dur = 120, desc = "2 Minuten lang laufen und fegen deine Putzhelfer dreimal so schnell" },
            new PerkDef { id = "goldmop", name = "Goldener Mopp", icon = "G", kind = 1, dur = 90, desc = "90 s lang bringt jede fertige Parzelle den dreifachen Bonus" },
            new PerkDef { id = "rohrpost", name = "Rohrpost", icon = "R", kind = 1, dur = 120, desc = "2 Minuten lang saugt eine Rohrpost deinen Behälter alle 3 Sekunden leer – direkt ins Rohstofflager, ganz ohne Sammelwagen" },
            new PerkDef { id = "helper", name = "Putzhelfer", icon = "H", kind = 2, desc = "Ein kleiner Helfer mit Besen putzt dauerhaft Parzelle für Parzelle auf diesem Planeten – auch wenn du offline bist (bis zu 8 je Planet)" },
        };
        public const int HELPER_MAX = 8;
        public static PerkDef Perk(string id) => Perks.FirstOrDefault(p => p.id == id);
        public static bool PerksOpen => Hard;
        public static int PerkN(string id) { var e = S.perkN.Find(x => x.k == id); return e == null ? 0 : e.v; }
        static void PerkAdd(string id, int n) { var e = S.perkN.Find(x => x.k == id); if (e == null) { e = new KV { k = id, v = 0 }; S.perkN.Add(e); } e.v = Math.Max(0, e.v + n); }
        public static float BuffLeft(string id) { var e = S.buffT.Find(x => x.k == id); return e == null ? 0 : e.v; }
        static float BuffF(string id, float f) => Hard && BuffLeft(id) > 0 ? f : 1;
        public static float GoldF => BuffF("goldmop", 3f); public static float EspressoF => BuffF("espresso", 3f);
        public static float PowerF => BuffF("turbo", 2.5f); public static float RadiusF => BuffF("wide", 1.7f); public static float HandF => BuffF("magnet", 3f);
        /// <summary>Preis: wächst mit Schwierigkeit und Basisstufe des aktuellen Planeten (Hard-Mode-Einnahmen ×3 sind eingerechnet).</summary>
        /* Fabian 03.11.: Perks dürfen das Spiel später nicht billig machen. Drei Bremsen:
           1. Grundpreis wächst mit Planet (Schwierigkeit × 1,35 je Planet in der Reihe) und Basisstufe.
           2. (entfernt 03.11.: Einkommens-Mindestpreis – der Preis stieg ständig, auch ohne Kauf)
           3. Jeder Kauf auf diesem Planeten macht den nächsten teurer (+30 %, Nuke +80 %); alle 10 min sinkt das um eine Stufe.
           Dazu: man trägt nur wenige Ladungen (Nuke 2). */
        static readonly Dictionary<string, (double f, int cap)> PerkVal = new Dictionary<string, (double, int)>
        {
            { "bomb", (1, 10) }, { "nuke", (10, 2) }, { "kaercher", (2, 5) }, { "foam", (1.2, 8) }, { "tornado", (4, 3) },
            { "turbo", (.8, 6) }, { "wide", (.8, 6) }, { "magnet", (.6, 6) }, { "espresso", (1.5, 4) }, { "goldmop", (2.5, 4) }, { "rohrpost", (1, 6) }, { "helper", (6, 99) },
        };
        const double PERK_INC_MIN = 2; /* Faktor 1 = 2 min Einkommen */ const float HEAT_DECAY = 600;
        static Dictionary<string, int> pRank;
        static int PlanetRank(string pid)
        {
            if (pRank == null) { pRank = new Dictionary<string, int>(); int r = 0; foreach (var kv in GameData.Planets.OrderBy(x => x.Value.diff)) pRank[kv.Key] = r++; }
            return pRank.TryGetValue(pid, out var v) ? v : 0;
        }
        static FKV Heat(PState ps, string id) { if (ps.sv.perkHeat == null) ps.sv.perkHeat = new List<FKV>(); var e = ps.sv.perkHeat.Find(x => x.k == id); if (e == null) { e = new FKV { k = id }; ps.sv.perkHeat.Add(e); } return e; }
        public static float PerkHeat(string id) => Cur.sv.perkHeat == null ? 0 : (Cur.sv.perkHeat.Find(x => x.k == id)?.v ?? 0);
        static double HeatMul(string id) => 1; /* Fabian 03.11.: kein Aufschlag mehr je Kauf – nur die Tragegrenze bremst */
        /// <summary>Nur für Tests: Perk-Preise und Ladungen zurücksetzen.</summary>
        public static void DevResetPerks() { S.perkInc = 0; S.perkN.Clear(); S.buffT.Clear(); foreach (var p in P.Values) p.sv.perkHeat?.Clear(); }
        public static int PerkCap(string id) => PerkVal.TryGetValue(id, out var v) ? v.cap : 5;
        public static double PerkCost(string id)
        {
            var ps = Cur; if (!PerkVal.TryGetValue(id, out var pv)) return 0;
            double b = 150 * ps.P.diff * Math.Pow(1.2, PlanetRank(ps.pid)) * Math.Pow(2, Math.Max(0, Gs(ps) - 1)); /* Fabian 03.11.: günstiger – Mond am Anfang: Bombe 150, Nuke 1 500 */
            double c = b * pv.f * HeatMul(id); /* Fabian 03.11.: kein Einkommens-Mindestpreis mehr – der Preis steigt nur mit Planet, Basisstufe und eigenen Käufen */
            /* 03.11.: Helfer werden je Kauf nicht mehr teurer (Höchstzahl je Planet bleibt) */
            return Math.Round(c);
        }
        public static string PerkWhy(string id)
        {
            if (!Hard) return "Nur im Hard Mode";
            if (id == "helper" && Cur.sv.helpers.Count(x => x.life <= 0) >= HELPER_MAX) return "Höchstens " + HELPER_MAX + " Helfer je Planet";
            if (id != "helper" && PerkN(id) >= PerkCap(id)) return "Du trägst schon " + PerkCap(id) + " – erst einsetzen";
            return S.credits < PerkCost(id) ? Lacks(PerkCost(id)) : "";
        }
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
            if (id == "foam") return Foam(ps, d);
            if (id == "tornado") { var t0 = new HelperSave { id = ++ps.sv.helperNext, d = d.normalized, life = 25 }; ps.sv.helpers.Add(t0); PerkAdd(id, -1); Toast("Staubsauger-Tornado losgelassen – 25 Sekunden Chaos!", "good"); Save(); return 1; }
            if (id == "kaercher") return Kaercher(ps, d);
            int max = id == "nuke" ? 60 : 5; float reach = id == "nuke" ? 45f : 12f; int c0 = SnapWork(ps, d); if (c0 < 0) { Toast("Hier gibt es nichts zu reinigen – der Planet ist schon sauber", "bad"); return 0; } d = pd.cells[c0].c.normalized; { float nd = 0; int nn = 0; foreach (int b in pd.cells[c0].nb) { nd += pd.Arc(pd.cells[b].c, d); nn++; } if (nn > 0) { nd /= nn; reach = Mathf.Max(reach, nd * (id == "nuke" ? 6.5f : 2.3f)); } } /* Reichweite nach Parzellengröße – auf großen Parzellen reichten 12 m nicht bis zum Nachbarn */ /* Fabian 03.11.: Tipp muss nicht genau sitzen – es geht an der nächsten reinigbaren Parzelle los */
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
        /// <summary>Nächste reinigbare (dreckige, angrenzende) Parzelle zur Stelle d – so muss man nicht genau treffen.</summary>
        static int SnapWork(PState ps, Vector3 d)
        {
            var pd = ps.pd; int c0 = pd.CellOf(d); if (c0 >= 0 && !ps.clean[c0] && Workable(ps, c0) && !pd.cells[c0].IsBase) return c0;
            int best = -1; float bd = float.MaxValue;
            for (int c = 0; c < pd.N; c++) { if (ps.clean[c] || pd.cells[c].IsBase || !Workable(ps, c)) continue; float a = pd.Arc(pd.cells[c].c, d); if (a < bd) { bd = a; best = c; } }
            return best;
        }
        static float rohrT;
        /// <summary>Rohrpost: Behälter direkt ins Rohstofflager.</summary>
        static void PipeHand(PState ps)
        {
            float tot = HandSum(), free = RohCap(ps) - RohSum(ps); if (tot < .05f || free < .05f) return; float amt = Mathf.Min(tot, free), k = amt / tot;
            for (int m = 0; m < NM; m++) { float x = ps.sv.hand[m] * k; ps.sv.hand[m] -= x; ps.sv.roh[m] += x; } FloatAmt(ps.pd.PlanDir(ps.lay.pos["roh"]), amt, "Rohrpost");
        }
        /// <summary>Schaumkanone: Schmutz im Umkreis zu 70 % lösen.</summary>
        static int Foam(PState ps, Vector3 d)
        {
            var pd = ps.pd; int n = 0; { int sc = SnapWork(ps, d); if (sc >= 0 && pd.Arc(pd.cells[sc].c, d) > 6f) d = pd.cells[sc].c.normalized; } /* 03.11.: neben das Saubere gezielt? dann dort einschäumen */
            for (int c = 0; c < pd.N; c++)
            {
                if (ps.clean[c] && !ps.act.ContainsKey(c)) continue; if (pd.Arc(pd.cells[c].c, d) > 15f || !Workable(ps, c)) continue;
                var a = EnsureAct(ps, c); if (a == null) continue; float sum = 0; for (int k = 0; k < a.grid.Length; k++) { a.grid[k] *= .3f; sum += a.grid[k]; } a.sum = sum; ps.dirtyCells.Add(c); n++;
            }
            if (n == 0) { Toast("Hier gibt es nichts einzuschäumen – tippe auf dreckige Parzellen am Rand des Sauberen", "bad"); return 0; }
            PerkAdd("foam", -1); Toast("Schaumkanone: " + n + " Parzellen eingeweicht – jetzt einmal drüberwischen!", "good"); OnBoom?.Invoke(d); Save(); return n;
        }
        /// <summary>Kärcher: Linie von der Basis durch das Ziel, bis zu 8 Parzellen.</summary>
        static int Kaercher(PState ps, Vector3 d)
        {
            var pd = ps.pd; var b0 = pd.cells[pd.baseCell].c.normalized; d = d.normalized; var axis = Vector3.Cross(b0, d).normalized; if (axis.sqrMagnitude < .5f) axis = Vector3.Cross(b0, Vector3.up).normalized;
            var line = new List<int>();
            for (int c = 0; c < pd.N; c++) { var p = pd.cells[c].c.normalized; float off = Mathf.Abs(Vector3.Dot(p, axis)) * pd.R; if (off > 4.5f || Vector3.Dot(Vector3.Cross(axis, p), d) < 0) continue; float along = pd.Arc(p, d); if (along > 60) continue; line.Add(c); }
            line.Sort((a, b) => pd.Arc(pd.cells[a].c, b0).CompareTo(pd.Arc(pd.cells[b].c, b0)));
            int done = 0; float got = 0;
            for (int pass = 0; pass < 10 && done < 8; pass++) foreach (int c in line) { if (done >= 8) break; if (ps.clean[c] || pd.cells[c].IsBase || !Workable(ps, c)) continue; var a = EnsureAct(ps, c); float frac = a != null ? a.sum / Mathf.Max(1, a.cl.nIn) : 1; int m = GameData.Dirt[pd.cells[c].type].mat; float add = Mathf.Min(HandCap() - HandSum(), ParcelYield(pd.cells[c]) * frac); if (add > 0) ps.sv.hand[m] += add; got += add; FinishCell(ps, c); OnBoom?.Invoke(pd.cells[c].c); done++; }
            if (done == 0) { Toast("Die Schneise trifft nichts Dreckiges – zieh sie vom Sauberen aus ins Dreckige", "bad"); return 0; }
            PerkAdd("kaercher", -1); Toast("Hochdruck-Kärcher: " + done + " Parzellen in einer Schneise!", "good"); Save(); return done;
        }
        /// <summary>Simulation (live und offline): Buffs laufen ab, Helfer putzen.</summary>
        static void PerkStep(PState ps, float dt)
        {
            if (!Hard) return;
            if (ps.sv.perkHeat != null) foreach (var e in ps.sv.perkHeat) if (e.v > 0) e.v = Mathf.Max(0, e.v - dt / HEAT_DECAY); /* Aufschlag sinkt (auch offline) */
            if (ps.pid == S.planet && !Offline && IncomeSpan() >= 120) { double inc = Math.Max(0, IncomePerMin()); S.perkInc += (inc - S.perkInc) * Math.Min(1, dt / (inc > S.perkInc ? 300.0 : 1800.0)); } /* steigt in ~5 min, sinkt in ~30 min – ein einzelner Raketenverkauf verzieht den Preis kaum */
            if (ps.pid == S.planet) foreach (var e in S.buffT) if (e.v > 0) e.v = Mathf.Max(0, e.v - dt);
            if (ps.pid == S.planet && BuffLeft("rohrpost") > 0) { rohrT -= dt; if (rohrT <= 0) { rohrT = 3; PipeHand(ps); } }
            if (ps.sv.helpers.Count == 0) return; var pd = ps.pd;
            ps.sv.helpers.RemoveAll(x => x.life > 0 && (x.life -= dt) <= 0); /* Tornado vorbei */
            foreach (var h in ps.sv.helpers)
            {
                bool tor = h.life > 0; float spd = tor ? 7f : 2.2f * EspressoF, sweepF = tor ? 9f : EspressoF;
                if (h.cell >= 0 && (ps.clean[h.cell] || !Workable(ps, h.cell))) h.cell = -1;
                if (h.cell < 0)
                {
                    /* nächste reinigbare Parzelle, nicht die eines anderen Helfers */
                    int best = -1; float bd = 1e9f; foreach (int c in ps.front) { if (pd.cells[c].Wet || ps.sv.helpers.Any(o => o != h && o.cell == c)) continue; float a = pd.Arc(pd.cells[c].c, h.d); if (a < bd) { bd = a; best = c; } }
                    h.cell = best; h.t = 0; if (best < 0) continue;
                }
                var tgt = pd.cells[h.cell].c.normalized; float dist = pd.Arc(h.d, tgt);
                if (dist > 1.5f) { h.d = Vector3.Slerp(h.d, tgt, Mathf.Min(1, spd * dt / Mathf.Max(.01f, dist))).normalized; continue; } /* läuft hin, 2,2 m/s (Tornado 7) */
                var cl = pd.cells[h.cell]; float need = 18f * Mathf.Max(.6f, cl.H / 4f) * GameData.Dirt[cl.type].hard * GameData.Kinds[cl.kind].hard;
                h.t += dt * sweepF; if (h.t < need) continue;
                int m = GameData.Dirt[cl.type].mat; float room = RohCap(ps) - RohSum(ps); if (room > 0) ps.sv.roh[m] += Mathf.Min(room, ParcelYield(cl) * .6f); /* trägt den Schmutz zur Basis (etwas geht verloren) */
                FinishCell(ps, h.cell); h.cell = -1; h.t = 0;
            }
        }
    }
}
