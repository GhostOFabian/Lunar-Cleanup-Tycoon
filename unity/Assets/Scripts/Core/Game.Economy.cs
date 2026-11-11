// Wirtschaft wie im Browser-Prototyp: Produktionsplan, Verkaufsband je Produkt, Frachtauftrag (+60 %), Verkaufsautomat,
// Produktions-Ampel mit Betriebsbonus (bis +30 %) und Gebäudestufen (Gebäude wachsen, wenn ihre Upgrades ausgereizt sind).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        // ---------- Produktionsplan ----------
        public static bool PlanOn(PState ps, string key) => string.IsNullOrEmpty(key) || !ps.sv.planOff.Contains(key);
        public static void TogglePlan(string key) { var sv = Cur.sv; if (sv.planOff.Contains(key)) sv.planOff.Remove(key); else sv.planOff.Add(key); Toast(PlanOn(Cur, key) ? "Rezept eingeschaltet" : "Rezept ausgeschaltet – die Ware bleibt roh"); }
        /// <summary>Ist die Ware zum Verkauf freigegeben? Standard: alle Produkte und Rohstoffe, die keine Maschine hier veredelt.</summary>
        /// <summary>10.11. (Fabian): alles ist freigegeben – Produkte immer, Rohstoffe, die hier keine Maschine braucht. Zurückgehalten wird nur, was für Bestellungen bereitliegt (KeepProd/KeepRoh).</summary>
        public static bool SellOn(PState ps, int m) => !GameData.Mats[m].raw || m == KristallIx || !RawRoute(ps, m); /* Fabian 01.11.: geerntete Kristalle werden standardmäßig mit verschickt */
        static int KristallIx => GameData.MatIx.TryGetValue("kristall", out var k) ? k : -1;
        public static bool AutoOn(PState ps, int m) => SellOn(ps, m);
        public static void ToggleAuto(int m)
        {
            var sv = Cur.sv; bool on = SellOn(Cur, m); sv.sellYes.Remove(m); sv.autoOff.Remove(m); if (on) sv.autoOff.Add(m); else sv.sellYes.Add(m);
            Toast(GameData.Mats[m].name + (SellOn(Cur, m) ? " ist zum Verkauf freigegeben – fährt in den Frachtraum der Rakete" : " ist gesperrt – bleibt im Lager"));
        }
        /// <summary>Fabian 03.11.: Anfangsbereich (vor Basisstufe 2) – liegt verkaufbare Ware im Lager, die von Hand in die Rakete muss?</summary>
        public static bool CargoHint(PState ps)
        {
            if (Gs(ps) >= 2 || ps.sv.rk > 0 || SellCap(ps) - Sum(ps.sv.sellBuf) <= .5f) return false;
            for (int m = 0; m < NM; m++) { bool raw = GameData.Mats[m].raw; if ((raw ? ps.sv.roh : ps.sv.prod)[m] - (raw ? KeepRoh(ps, m) : KeepProd(ps, m)) >= (raw ? .5f : 1) && SellOn(ps, m)) return true; }
            return false;
        }
        /// <summary>Alle freigegebenen Waren auf einmal in den Frachtraum (Anfangsbereich).</summary>
        public static void LoadAllCargo()
        {
            var ps = Cur; if (ps.sv.rk > 0) { Fail("Die Rakete ist unterwegs"); return; }
            float before = Sum(ps.sv.sellBuf);
            for (int m = 0; m < NM; m++)
            {
                bool raw = GameData.Mats[m].raw; var store = raw ? ps.sv.roh : ps.sv.prod; float keep = raw ? KeepRoh(ps, m) : KeepProd(ps, m); if (!SellOn(ps, m) || store[m] - keep < (raw ? .5f : 1)) continue;
                float room = SellCap(ps) - Sum(ps.sv.sellBuf); if (room <= .01f) break; float u = Mathf.Min(store[m] - keep, room); if (!raw) u = Mathf.Floor(u + 1e-6f); if (u <= .01f) continue;
                store[m] -= u; ps.sv.sellBuf[m] += u;
            }
            float got = Sum(ps.sv.sellBuf) - before;
            if (got <= .01f) { Fail(SellCap(ps) - before <= .01f ? "Der Frachtraum ist voll – starte die Rakete" : "Nichts Freigegebenes auf Lager"); return; }
            Toast(Fmt(got) + " Waren in die Rakete geladen – jetzt starten, dann gibt es Geld", "good"); Save();
        }
        /// <summary>Freigegebene Ware von Hand in den Frachtraum laden (vor allem bevor es Förderbänder gibt).</summary>
        public static void LoadRocket(int m)
        {
            var ps = Cur; var sv = ps.sv; if (!SellOn(ps, m)) { Fail("Erst zum Verkauf freigeben"); return; } if (sv.rk > 0) { Fail("Die Rakete ist unterwegs"); return; }
            bool raw = GameData.Mats[m].raw; var store = raw ? sv.roh : sv.prod; float room = SellCap(ps) - Sum(sv.sellBuf); float u = Mathf.Min(store[m] - (raw ? KeepRoh(ps, m) : KeepProd(ps, m)), room); /* 10.11.: Bestelltes bleibt im Lager */ if (!raw) u = Mathf.Floor(u + 1e-6f);
            if (u <= .01f) { Fail(room <= .01f ? "Der Frachtraum ist voll – starte die Rakete" : "Nichts auf Lager"); return; }
            store[m] -= u; sv.sellBuf[m] += u; Toast(Fmt(u) + " " + GameData.Mats[m].name + " in den Frachtraum geladen", "good");
        }

        // ---------- Frachtauftrag ----------
        public const double ORDER_BONUS = 1.6;
        /// <summary>Was aus einem Rohstoff auf diesem Planeten am Ende wird (Presse → Schmelze, Raffinerie oder roh).</summary>
        public static int EndGood(PState ps, int m)
        {
            if (!RawRoute(ps, m)) return m;
            var pr = ps.bld.Contains("presse") && Pressable(ps, m) ? ps.line.presse.FirstOrDefault(r => r.inp == m && PlanOn(ps, r.plan)) : null;
            if (pr != null) { var sm = SmeltOn(ps) ? ps.line.schmelze.FirstOrDefault(r => r.inp == pr.outp && PlanOn(ps, r.plan)) : null; return sm != null ? sm.outp : pr.outp; }
            var rf = ps.line.raff.FirstOrDefault(r => r.inp == m); return rf != null ? rf.outp : m;
        }
        public static void NewOrder(PState ps)
        {
            var opts = PlanetRaws(ps).Select(m => EndGood(ps, m)).Distinct().ToList(); if (opts.Count == 0) { ps.sv.order = -1; return; }
            int m0 = opts[Rng.Next(opts.Count)]; if (opts.Count > 1 && m0 == ps.sv.order) m0 = opts[(opts.IndexOf(m0) + 1) % opts.Count]; ps.sv.order = m0;
        }

        // ---------- Verkaufsautomat (ab Basisstufe 3): volle Lager werden komplett verkauft, auch offline ----------
        /// <summary>Verkaufsautomat (ab Basisstufe 3): sind die Lager fast voll, startet die Rakete schon halb beladen – verkauft wird trotzdem nur, was im Frachtraum ist.</summary>
        static void AutoDumpStep(PState ps) { }
        public static bool EarlyLaunch(PState ps) => ps.sv.autoDump && Gs(ps) >= 3 && (RohSum(ps) >= RohCap(ps) * .9f || (ps.bld.Contains("prod") && ProdUsed(ps) >= ProdCap(ps) * .9f));
        public static void ToggleAutoDump() { var sv = Cur.sv; sv.autoDump = !sv.autoDump; Toast(sv.autoDump ? "Verkaufsautomat an – bei vollen Lagern startet die Rakete schon halb beladen" : "Verkaufsautomat aus"); }

        // ---------- Produktions-Ampel ----------
        public class FlowItem { public string id, name, why; public int lvl; public string[] fix; }
        /// <summary>11.11.: Lässt sich noch etwas verbessern? (eins der Upgrades nicht voll, oder freie Fahrzeugplätze)</summary>
        static bool CanImprove(PState ps, bool veh, params string[] ups)
        {
            foreach (var id in ups) { if (!GameData.Up.ContainsKey(id)) continue; var u = GameData.Up[id]; if (Lvl(id, ps) < Mathf.Min(u.max, u.vals.Length - 1)) return true; }
            return veh && !Hard && ps.sv.veh.Count < VehSlots(ps);
        }
        public static List<FlowItem> FlowReport(PState ps)
        {
            var o = new List<FlowItem>(); void Add(string id, string name, int lvl, string why, params string[] fix) => o.Add(new FlowItem { id = id, name = name, lvl = lvl, why = why, fix = fix });
            var sv = ps.sv; float rf = RohSum(ps) / Mathf.Max(1, RohCap(ps)), pf = ProdUsed(ps) / Mathf.Max(1, ProdCap(ps));
            if (ps.ef < .99f && (sv.veh.Count > 0 || ps.bld.Contains("presse") || sv.rigs.Count > 0)) Add(ps.bld.Contains("energy") ? "energy" : "hq", "Energie", 2, "Energie reicht nicht – Fahrzeuge, Bänder und Maschinen laufen langsamer.", "energy", "ecap");
            { int fl = sv.rigs.Count(r => r.faultK != ""); if (fl > 0 && TechN(ps) > 0) Add("roh", "Störung", 0, fl + (fl > 1 ? " Bohrtürme/Erzbagger gestört" : " Bohrturm/Erzbagger gestört") + " – die Techniker reparieren das gleich (oder sofort beheben: antippen)."); else if (fl > 0) Add("roh", "Störung", 2, fl + (fl > 1 ? " Bohrtürme/Erzbagger gestört" : " Bohrturm/Erzbagger gestört") + " – Techniker sind unterwegs, oder sofort beheben (antippen).", "rcare"); }
            { int br = sv.veh.Count(v => v.mode == "broken"), due = sv.veh.Count(v => v.wear >= .75f && v.mode != "broken"); if (br > 0) Add(ps.bld.Contains("werkstatt") ? "werkstatt" : "hq", "Fahrzeuge", 2, br + (br > 1 ? " Fahrzeuge" : " Fahrzeug") + " ausgefallen – " + (ps.bld.Contains("werkstatt") ? "Werkstatt öffnen" : "baue eine Roboterwerkstatt") + ".", "vdur"); else if (due > 0) Add(ps.bld.Contains("werkstatt") ? "werkstatt" : "hq", "Fahrzeuge", ps.bld.Contains("werkstatt") ? 0 : 1, due + (due > 1 ? " Fahrzeuge brauchen" : " Fahrzeug braucht") + " bald Wartung.", "vdur"); }
            { int full = sv.rigs.Count(r => Sum(r.buf) >= RigBufCap(r) - .5f); if (full > 0 && !CanImprove(ps, true, "vtank", "vspeed", "urate")) Add("roh", "Bohrtürme", 0, full + (full > 1 ? " Bohrtürme fördern" : " Bohrturm fördert") + " auf Vorrat – mehr Abholung geht nicht mehr."); else if (full > 0) Add("roh", "Bohrtürme", 1, full + (full > 1 ? " Bohrtürme sind" : " Bohrturm ist") + " voll – " + (sv.veh.Count > 0 ? "die Fahrzeuge holen nicht schnell genug ab." : "kauf Fahrzeuge, die abholen."), "vtank", "vspeed", "urate"); } /* Fabian 02.11.: volle Bohrtürme bremsen nur den Abbau – Gelb statt Rot */
            { string s = null; float f = -1; foreach (var s0 in GameData.Stations) if (ps.bld.Contains(s0) && ps.st.ContainsKey(s0)) { float f0 = Sum(ps.st[s0].buf) / Mathf.Max(1, StBuf(ps, s0)); if (f0 > f) { f = f0; s = s0; } } /* eine Entladeanlage: der vollste Teil zählt */
                if (s != null)
                {
                    bool rfull = rf > .97f; string nm = IsUnit(s) ? "Entladeanlage" : GameData.Bld[s].name;
                    if (f > .7f && !rfull && !CanImprove(ps, false, "bspeed", "bcap", "ubuf")) Add(s, nm, 0, "Läuft auf voller Leistung – die Fahrzeuge liefern mehr, als das Band schafft; schneller geht es nicht."); /* 11.11.: im Endausbau kein Fehler */
                    else if (f > .97f) Add(s, nm, 2, "Puffer voll – Fahrzeuge warten, weil " + (rfull ? "das Rohstofflager voll ist." : "das Förderband zu langsam abtransportiert."), rfull ? new[] { "rohcap", "sellcap" } : new[] { "bspeed", "bcap", "ubuf" });
                    else if (f > .7f) Add(s, nm, 1, "Puffer füllt sich – " + (rfull ? "das Rohstofflager ist voll." : "das Förderband kommt nicht ganz hinterher."), rfull ? new[] { "rohcap", "sellcap" } : new[] { "bspeed", "bcap" });
                    else Add(s, nm, 0, "Läuft – Puffer " + Mathf.RoundToInt(f * 100) + " %");
                }
            }
            if (rf > .97f && sv.xport) Add("roh", "Rohstofflager", 0, "Voll – der Export-Frachter nimmt den Überschuss ab (" + XportPct + " des Werts, bisher " + Fmt(sv.xEarn) + " Cr).");
            else if (rf > .97f) Add("roh", "Rohstofflager", 2, "Voll – Maschinen verarbeiten zu wenig. (Ab 97 % geht der Überschuss zur Rakete.)", "rohcap", "mspeed", "sellcap");
            else if (rf > .85f && sv.xport) Add("roh", "Rohstofflager", 0, "Läuft – der Export-Frachter hält Platz frei (bisher " + Fmt(sv.xEarn) + " Cr).");
            else if (rf > .85f) Add("roh", "Rohstofflager", 1, "Fast voll.", "rohcap", "sellcap", "bspeed"); else Add("roh", "Rohstofflager", 0, "Läuft – " + Mathf.RoundToInt(rf * 100) + " % belegt");
            foreach (var m in new[] { "presse", "schmelze", "raff" }) if (ps.bld.Contains(m) && ps.mach.ContainsKey(m))
                {
                    var M = ps.mach[m]; string st = MachStatus(ps, m), nm = GameData.Bld[m].name; float inF = Sum(M.inp) / Mathf.Max(1, MachInCap(ps, m));
                    if (st == "Keine Energie") Add(m, nm, 2, "Steht – keine Energie.", "energy");
                    else if (st == "Ausgang voll") Add(m, nm, 2, "Ausgang voll – " + (pf > .97f ? "das Produktlager ist voll." : "das Band dahinter ist zu langsam."), pf > .97f ? new[] { "prodcap", "sellcap" } : new[] { "bspeed", "bcap", "mcap" });
                    else if (inF > .9f && rf > .85f && !sv.xport) Add(m, nm, 1, "Kommt nicht hinterher – Eingang fast voll, das Rohstofflager läuft voll.", "mspeed", "mcap");
                    else if (inF > .9f) Add(m, nm, 0, "Läuft auf voller Leistung – es kommt mehr an, als sie schafft; der Rest fährt zur Rakete."); /* 11.11.: kein Fehler, solange nichts überläuft */
                    else Add(m, nm, st.StartsWith("Arbeitet") ? 0 : -1, st);
                }
            if (ps.bld.Contains("prod")) { if (pf > .85f && sv.xport) Add("prod", "Produktlager", 0, "Läuft – der Export-Frachter hält Platz frei."); else if (pf > .97f) Add("prod", "Produktlager", 2, "Voll – die Rakete holt zu wenig ab.", "prodcap", "sellcap", "rspeed"); else if (pf > .85f) Add("prod", "Produktlager", 1, "Fast voll.", "prodcap", "sellcap"); else Add("prod", "Produktlager", 0, "Läuft – " + Mathf.RoundToInt(pf * 100) + " % belegt"); }
            if (Gs(ps) >= 2 && ps.bld.Contains("sell")) { float f = Sum(sv.sellBuf) / Mathf.Max(1, SellCap(ps)); if (sv.rk > 0 && f > .97f) Add("sell", "Verkaufsrakete", 1, "Frachtraum voll, Rakete noch unterwegs – die Bänder warten.", "rspeed", "sellcap"); else Add("sell", "Verkaufsrakete", 0, sv.rk > 0 ? "Rakete unterwegs" : "Frachtraum " + Mathf.RoundToInt(f * 100) + " % – startet, wenn voll"); }
            foreach (var b in ps.lay.belts) if (BeltLive(ps, b) && !HiddenBelt(b))
                {
                    string nm = "Band " + GameData.Bld[b.from].name + " → " + GameData.Bld[b.to].name; int cap = Mathf.FloorToInt(b.L / BSP);
                    if (ps.jam.TryGetValue(b.id, out bool j) && j) Add(b.id, nm, 2, "Stau – das Ziel nimmt nichts mehr an.", "bspeed");
                    else if (ps.belts.TryGetValue(b.id, out var it) && it.Count >= cap * .9f) { if (CanImprove(ps, false, "bspeed", "bcap")) Add(b.id, nm, 1, "Am Limit – das Band ist voll belegt.", "bspeed", "bcap"); else Add(b.id, nm, 0, "Läuft auf voller Leistung – schneller geht es nicht."); } /* 11.11.: im Endausbau kein Fehler */
                }
            return o;
        }
        public static int FlowLevel(List<FlowItem> r) => r.Count == 0 ? 0 : Math.Max(0, r.Max(n => n.lvl));
        /// <summary>Betriebsbonus: wächst bei grüner Ampel in 10 min auf +30 %, hält bei Gelb, fällt bei Rot in 2 min.</summary>
        public static double FlowBonus(PState ps) => 1 + .3 * ps.sv.flowB;
        static void FlowTick(PState ps, float dt)
        {
            var sv = ps.sv; if (Gs(ps) < 2 || !ps.bld.Contains("sell")) return; sv.fbT += dt; if (sv.fbT < 5) return; float st = sv.fbT; sv.fbT = 0;
            int lv = FlowLevel(FlowReport(ps)); sv.flowB = lv == 0 ? Mathf.Min(1, sv.flowB + st / 600) : lv == 2 ? Mathf.Max(0, sv.flowB - st / 120) : sv.flowB; sv.flowLv = lv;
        }

        // ---------- Gebäudestufen ----------
        public static readonly Dictionary<string, string[]> BLV_REQ = new Dictionary<string, string[]> {
            { "presse", new[] { "mspeed", "pvol" } }, { "schmelze", new[] { "mspeed", "mcap" } }, { "raff", new[] { "mspeed", "mcap" } },
            { "u1", new[] { "urate", "ubuf" } }, { "u2", new[] { "urate", "ubuf" } }, { "u3", new[] { "urate", "ubuf" } },
            { "roh", new[] { "rohcap" } }, { "prod", new[] { "prodcap" } }, { "sell", new[] { "sellcap" } }, { "energy", new[] { "energy", "ecap" } } };
        public static int Blvl(PState ps, string id) { foreach (var kv in ps.sv.blvl) if (kv.k == id) return kv.v; return 0; }
        public static int BlMax(PState ps) => 6 + ExpN(ps); /* Fabian 01.11.: mehr Gebäudestufen (vorher 3) */
        public static float BlF(PState ps, string id) { int l = Blvl(ps, id); if (l == 0) return 1; return 1 + (GameData.Mach.ContainsKey(id) ? .5f : id == "energy" ? .3f : .4f) * l; }
        public static string BlEffect(string id) => GameData.Mach.ContainsKey(id) ? "Verarbeitungstempo und Puffer" : GameData.Stations.Contains(id) ? "Entladetempo und Trichter" : id == "energy" ? "Solarleistung und Speicher" : "Kapazität";
        public static int BlPct(string id) => GameData.Mach.ContainsKey(id) ? 50 : id == "energy" ? 30 : 40;
        public static string BlWhy(PState ps, string id)
        {
            if (!BLV_REQ.ContainsKey(id)) return "Dieses Gebäude hat keine Stufen"; if (!ps.bld.Contains(id)) return "Erst bauen";
            var miss = BLV_REQ[id].Where(u => GameData.Up.ContainsKey(u) && Lvl(u, ps) < GameData.Up[u].max0).ToList();
            if (miss.Count > 0) return "Erst die Upgrades ausreizen: " + string.Join(", ", miss.Select(u => GameData.Up[u].name + " " + Lvl(u, ps) + "/" + GameData.Up[u].max0));
            if (Blvl(ps, id) >= BlMax(ps)) return "Höchste Gebäudestufe – jede Ausbaustufe des Planeten erlaubt eine mehr"; return "";
        }
        public static double BlCost(PState ps, string id) => Math.Round(Math.Max(4000, GameData.Bld[id].cost * 15) * ps.P.diff * Math.Pow(3, Blvl(ps, id)));
        public static bool BuyBl(string id)
        {
            var ps = Cur; string why = BlWhy(ps, id); if (why != "") return Fail(why); double c = BlCost(ps, id); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; int l = Blvl(ps, id) + 1; var kv = ps.sv.blvl.Find(x => x.k == id); if (kv == null) ps.sv.blvl.Add(new KV { k = id, v = l }); else kv.v = l;
            ps.baseVersion++; Toast(GameData.Bld[id].name + " erreicht Gebäudestufe " + l + " – größer und " + Mathf.RoundToInt((BlF(ps, id) - 1) * 100) + " % stärker", "good"); return true;
        }
    }
}
