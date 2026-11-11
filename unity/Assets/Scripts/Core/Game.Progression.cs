// Langzeit-Systeme aus dem Browser-Prototyp: Ausbaustufen nach Basisstufe 5, Ausrichtung pro Planet, Werksaufträge mit Ruf,
// Planetenziele, tägliche Aufträge, Statistik mit Prognose, Teamarbeit und Arbeitsbereich der Fahrzeuge.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        // ---------- Ausbaustufen ----------
        public static int ExpN(PState ps) => ps?.sv?.exp ?? 0;
        public static float ExpF(PState ps) => 1 + .3f * ExpN(ps);
        public static double ExpCost(PState ps) => Math.Round(400000 * ps.P.diff * Math.Pow(2.2, ExpN(ps)));
        public static float ExpNeed(PState ps) => Mathf.Round(4000 * Mathf.Pow(2.2f, ExpN(ps)));
        public static string ExpWhy(PState ps)
        {
            if (ps.sv.stage < 5) return "Erst Basisstufe 5"; if (!IsDone(ps.pid)) return "Erst " + ps.P.name + " komplett reinigen";
            if (ps.sv.deep < ExpNeed(ps)) return "Noch " + Fmt(ExpNeed(ps) - ps.sv.deep) + " E aus der Tiefe fördern"; return "";
        }
        public static bool BuyExp()
        {
            var ps = Cur; string w = ExpWhy(ps); if (w != "") return Fail(w); double c = ExpCost(ps); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.sv.exp++; ps.sv.deep = 0; ps.baseVersion++;
            Toast(ps.P.name + ": Ausbaustufe " + ps.sv.exp + " – +25 % Bohrleistung, +30 % Lager, +1 Fahrzeugplatz, +1 Gebäudestufe", "good"); return true;
        }

        // ---------- Ausrichtung ----------
        public class SpecDef { public string id, name, desc; public float veh, rig, mach, store, price, rocket; }
        public static readonly SpecDef[] Specs = {
            new SpecDef { id = "mine", name = "Bergbau", desc = "Fahrzeuge reinigen und fahren schneller, Bohrtürme fördern mehr.", veh = .25f, rig = .3f },
            new SpecDef { id = "ind", name = "Industrie", desc = "Maschinen arbeiten schneller, Roh- und Produktlager sind größer.", mach = .3f, store = .3f },
            new SpecDef { id = "trade", name = "Handel", desc = "Höhere Verkaufspreise, die Rakete ist schneller zurück.", price = .2f, rocket = .3f } };
        static readonly Dictionary<string, string> SpecFit = new Dictionary<string, string> { { "moon", "trade" }, { "mars", "mine" }, { "europa", "ind" }, { "titan", "ind" }, { "io", "mine" }, { "venus", "trade" }, { "ganymed", "ind" }, { "enceladus", "mine" }, { "triton", "mine" }, { "pluto", "trade" }, { "eris", "ind" }, { "sedna", "trade" }, { "proxima", "mine" }, { "trappist", "ind" }, { "kepler", "mine" }, { "cancri", "trade" }, { "gliese", "ind" }, { "poltergeist", "trade" } };
        public static string SpecFitOf(string pid) => SpecFit.TryGetValue(pid, out var s) ? s : "trade";
        public static float SpecF(PState ps, string key)
        {
            if (ps == null || string.IsNullOrEmpty(ps.sv.spec)) return 1; var d = Specs.FirstOrDefault(x => x.id == ps.sv.spec); if (d == null) return 1;
            float v = key == "veh" ? d.veh : key == "rig" ? d.rig : key == "mach" ? d.mach : key == "store" ? d.store : key == "price" ? d.price : key == "rocket" ? d.rocket : 0;
            return 1 + v * (SpecFitOf(ps.pid) == d.id ? 1.5f : 1);
        }
        public static double SpecCost(PState ps) => string.IsNullOrEmpty(ps.sv.spec) ? 0 : Math.Round(3000 * ps.P.diff);
        public static bool SetSpec(string id)
        {
            var ps = Cur; if (ps.sv.spec == id) return false; if (Gs(ps) < 3) return Fail("Ab Basisstufe 3"); double c = SpecCost(ps); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.sv.spec = id; Toast(ps.P.name + " ist jetzt auf " + Specs.First(x => x.id == id).name + " ausgerichtet" + (SpecFitOf(ps.pid) == id ? " – passt perfekt (×1,5)" : ""), "good"); return true;
        }

        // ---------- Werksaufträge ----------
        public static double WoRep(PState ps) => 1 + .02 * Math.Min(25, ps.sv.woDone);
        public static void NewWorkOrder(PState ps)
        {
            var goods = PlanetRaws(ps).Select(m => EndGood(ps, m)).Distinct().ToList(); if (goods.Count == 0) return; int prev = ps.sv.wo != null ? ps.sv.wo.m : -1;
            var opts = goods.Where(m => m != prev).ToList(); if (opts.Count == 0) opts = goods; int mm = opts[Rng.Next(opts.Count)];
            double V = 2500 * ps.P.diff * Math.Pow(1.55, ps.sv.woDone); int n = Math.Max(GameData.Mats[mm].raw ? 20 : 3, (int)Math.Round(V / Math.Max(.01, MatValue(mm, ps))));
            ps.sv.wo = new WorkOrderSave { m = mm, n = n, got = 0, r = Math.Round(V * .8 * GoalF) };
        }
        static void WoCount(PState ps, int m, float u)
        {
            if (Gs(ps) < 2) return; if (ps.sv.wo == null || ps.sv.wo.n <= 0) NewWorkOrder(ps); var w = ps.sv.wo; if (w == null || m != w.m) return; w.got += u; if (w.got < w.n) return;
            ps.sv.woDone++; Earn(w.r); if (!Offline && ps.pid == S.planet) Toast("Werksauftrag erfüllt · +" + Fmt(w.r) + " Cr · Ruf +2 % auf alle Preise hier", "good"); NewWorkOrder(ps);
        }
        /// <summary>Beim Raketenstart: gelieferte Waren zählen für Werksauftrag und tägliche Aufträge.</summary>
        static void CountSold(PState ps, float[] buf) { for (int m = 0; m < NM; m++) if (buf[m] > 1e-6f) WoCount(ps, m, buf[m]); }

        // Planetenziele: seit 11.11. ersetzt durch die Medaillen-Schritte (Game.Medals.cs).
        /// <summary>Alles, was gerade abholbar ist (Hauptziel, Aufträge, Medaillen-Schritte, Erfolge) – für den Punkt am Ziele-Knopf.</summary>
        public static int ClaimableCount()
        {
            var ps = Cur; if (ps == null) return 0; int n = MStepsReady(ps) + AchPending(); n += HardGoalsClaimable();
            if (S.quest < Mathf.Min(QuestsSupported, GameData.Quests.Count) && QuestProgress(S.quest) >= 1) n++;
            if (S.contracts != null) foreach (var it in S.contracts.list) if (!it.done && ContractProg(it) >= it.target) n++;
            return n;
        }
        // ---------- Tägliche Aufträge ----------
        static double CtrValue(string type) => type == "cells" ? S.stats.cells : type == "sold" ? S.stats.sold : type == "rockets" ? S.planets.Sum(p => p.nRockets) : S.stats.crystals;
        public static void DailyContracts()
        {
            int day = Today(); var c = S.contracts ?? (S.contracts = new ContractSave()); if (c.day == day && c.list.Count > 0) return;
            var r = new System.Random(day * 7 + S.resets * 131); int ms = Math.Max(1, S.planets.Max(p => p.stage)); double dm = S.planets.Max(p => GameData.Planets[p.pid].diff);
            double R0 = Math.Round(300 * ms * ms * dm * GoalF);
            var pool = new List<ContractItem> {
                new ContractItem { type = "cells", target = 3 + ms * 2, reward = R0 },
                new ContractItem { type = "rockets", target = 2 + ms, reward = R0 * .9 },
                new ContractItem { type = "sold", target = Math.Max(1500, Math.Round(S.stats.sold * .08 / 100) * 100), reward = R0 * 1.2 },
                new ContractItem { type = "crystals", target = 3 + ms, reward = R0 * .8 } };
            c.day = day; c.list.Clear(); c.bonus = false;
            foreach (var it in pool.OrderBy(x => r.Next()).Take(3)) { it.start = CtrValue(it.type); c.list.Add(it); }
        }
        public static string ContractText(ContractItem it) => it.type == "cells" ? "Reinige " + it.target + " Parzellen vollständig" : it.type == "rockets" ? "Starte " + it.target + " Raketen" : it.type == "sold" ? "Verkaufe Waren für " + Fmt(it.target) + " Cr" : "Baue " + it.target + " Leuchtkristalle ab";
        public static double ContractProg(ContractItem it) => Math.Max(0, CtrValue(it.type) - it.start);
        public static bool ClaimContract(int i)
        {
            var c = S.contracts; if (c == null || i < 0 || i >= c.list.Count) return false; var it = c.list[i]; if (it.done || ContractProg(it) < it.target) return false;
            it.done = true; Earn(it.reward); string extra = "";
            if (!c.bonus && c.list.All(x => x.done)) { c.bonus = true; double b = c.list.Sum(x => x.reward) * .5; Earn(b); extra = " · alle drei geschafft: +" + Fmt(b) + " Cr Bonus"; }
            Toast("Tagesauftrag erledigt · +" + Fmt(it.reward) + " Cr" + extra, "good"); return true;
        }

        // ---------- Statistik ----------
        static float statT; static readonly Queue<(float t, double earned, int clean)> statHist = new Queue<(float, double, int)>();
        static void StatTick(float dt)
        {
            statT += dt; if (statT < 10) return; statT = 0; statHist.Enqueue((Time.realtimeSinceStartup, S.stats.earned, Cur.nClean)); while (statHist.Count > 31) statHist.Dequeue();
        }
        /// <summary>03.11.: Verlauf leeren (Spielstand gewechselt) – sonst zählt das Einkommen des anderen Spielstands mit.</summary>
        public static float IncomeSpan() => statHist.Count < 2 ? 0 : statHist.Last().t - statHist.Peek().t;
        public static void ResetStats() { statHist.Clear(); statT = 0; }
        public static double IncomePerMin() { if (statHist.Count < 2) return 0; var a = statHist.Peek(); var b = statHist.Last(); float dt = Mathf.Max(1, b.t - a.t); return (b.earned - a.earned) / dt * 60; }
        public static float CellsPerMin() { if (statHist.Count < 2) return 0; var a = statHist.Peek(); var b = statHist.Last(); float dt = Mathf.Max(1, b.t - a.t); return (b.clean - a.clean) / dt * 60; }

        // ---------- Fahrzeuge: Teamarbeit und Arbeitsbereich ----------
        public static float TeamF(PState ps, VehSave v) { int n = 0; foreach (var o in ps.sv.veh) if (o != v && o.cell == v.cell && o.mode == "clean") n++; return 1 + .1f * Mathf.Min(3, n); }
        public static readonly string[] Zones = { "Überall", "Nahe der Basis", "Weit draußen" };
        static bool ZoneOk(VehSave v, Cell cl) => v.zone == 0 || (v.zone == 1 ? cl.ring <= 6 : cl.ring > 6);
    }
}
