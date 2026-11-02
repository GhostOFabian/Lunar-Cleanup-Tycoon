// Build 29 (Fabian 01.11.): Riesiger Müllberg, Händlerschiff, Herausforderungen, Statistik-Verlauf, Putzi-7 als Begleiter
// und der Riesenmeteor, der eine ganze Basis zerstört. Alles läuft über die eine Simulation (Step/StepPlanet).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        // =====================================================================
        // Riesiger Müllberg – nur mehrere Fahrzeuge zusammen schaffen ihn
        // =====================================================================
        public const float PILE_HARD = 9f, PILE_AMT = 10f; public const int PILE_TEAM = 3;
        public static bool IsPile(PState ps, int cell) => EvOn(ps, "pile") && ps.sv.ev.cell == cell;
        /// <summary>Wie zäh eine Parzelle ist: der Müllberg ist 9× zäher, mit weniger als 3 Fahrzeugen fast unbeweglich.</summary>
        static float PileHard(PState ps, int cell, VehSave v = null)
        {
            if (!IsPile(ps, cell)) return 1; if (v == null) return PILE_HARD * 6; // von Hand: kaum ein Kratzer
            int n = 1; foreach (var o in ps.sv.veh) if (o != v && o.cell == cell && o.mode == "clean") n++;
            return n >= PILE_TEAM ? PILE_HARD : PILE_HARD * 8;
        }
        /// <summary>Höchster Abtrag je Schritt am Müllberg: mit 3+ Fahrzeugen 1,5 %/s je Rasterfeld unter der Bürste, sonst kaum etwas, von Hand fast nichts.</summary>
        static float PileK(PState ps, int cell, VehSave v, float dt) { if (v == null) return dt * .0015f; int n = 1; foreach (var o in ps.sv.veh) if (o != v && o.cell == cell && o.mode == "clean") n++; return dt * (n >= PILE_TEAM ? .015f : .002f); }
        public static int PileCrew(PState ps) { if (!EvOn(ps, "pile")) return 0; int c = ps.sv.ev.cell, n = 0; foreach (var o in ps.sv.veh) if (o.cell == c && (o.mode == "clean" || o.mode == "drive")) n++; return n; }
        public static float PileLeft(PState ps) { if (!EvOn(ps, "pile") || !ps.act.TryGetValue(ps.sv.ev.cell, out var a)) return 0; return Mathf.Clamp01(a.sum / Mathf.Max(1, ps.sv.ev.amt0)); }
        public static double PileReward(PState ps) => Math.Round(4000 * Math.Pow(ps.P.diff, .95) * (1 + ps.sv.stage) * (1 + .5 * ps.sv.veh.Count / 10.0));
        static bool StartPile(PState ps)
        {
            var pd = ps.pd; var cand = new List<int>();
            for (int i = 0; i < pd.N; i++) { var c = pd.cells[i]; if (ps.clean[i] && !c.IsBase && !c.Wet && c.ring >= 2 && c.ring <= 7 && !ps.act.ContainsKey(i) && !ps.sv.rigs.Any(x => x.cell == i) && !ps.sv.outposts.Any(o => o.cell == i) && !ps.sv.crystals.Any(x => x.cell == i)) cand.Add(i); }
            if (cand.Count == 0 || ps.sv.veh.Count < PILE_TEAM) return false;
            int id = cand[fr.Next(cand.Count)]; var cl = pd.cells[id]; int G = GameData.GRID; var D = GameData.Dirt[ps.P.start];
            var a = new Act { cl = cl, grid = new float[G * G], sum = 0, amt = D.amt * GameData.Kinds[cl.kind].amt * PILE_AMT, D = D, K = GameData.Kinds[cl.kind] };
            for (int k = 0; k < a.grid.Length; k++) if (cl.inside[k]) { a.grid[k] = 1; a.sum += 1; }
            ps.act[id] = a; ps.dirtyCells.Add(id); ps.sv.ev.cell = id; ps.sv.ev.amt0 = a.sum; return true;
        }
        static void PileDone(PState ps, int id)
        {
            if (!IsPile(ps, id)) return; double r = PileReward(ps); Earn(r); S.stardust += 1;
            if (!Offline && ps.pid == S.planet) { Toast("Müllberg geschafft! +" + Fmt(r) + " Cr und 1 Sternenstaub", "good"); FloatTxt(ps.pd.cells[id].c, "+" + Fmt(r) + " Cr", "big"); }
            EvEnd(ps, null);
        }

        // =====================================================================
        // Händlerschiff – kauft eine Ware 3 Minuten lang zum dreifachen Preis
        // =====================================================================
        public const float TRADER_MULT = 3f;
        static bool StartTrader(PState ps)
        {
            int best = -1; float bv = 0; var sv = ps.sv;
            for (int m = 0; m < NM; m++) { float have = sv.roh[m] + sv.prod[m]; if (have < 1) continue; float v = have * (float)MatValue(m, ps); if (v > bv) { bv = v; best = m; } }
            if (best < 0) return false; sv.ev.mat = best; return true;
        }
        public static float TraderStock(PState ps) { int m = ps.sv.ev?.mat ?? -1; return m < 0 || m >= NM ? 0 : ps.sv.roh[m] + ps.sv.prod[m]; }
        public static double TraderValue(PState ps) { int m = ps.sv.ev?.mat ?? -1; if (m < 0 || m >= NM) return 0; return Math.Round(TraderStock(ps) * MatValue(m, ps) * TRADER_MULT); }
        public static bool SellToTrader()
        {
            var ps = Cur; if (!EvOn(ps, "trader")) return false; int m = ps.sv.ev.mat; double v = TraderValue(ps); if (v <= 0) return Fail("Davon hast du gerade nichts auf Lager");
            float n = TraderStock(ps); ps.sv.roh[m] = 0; ps.sv.prod[m] = 0; Earn(v); S.stats.sold += v;
            Toast("An den Händler verkauft: " + Fmt(n) + " E " + GameData.Mats[m].name + " · +" + Fmt(v) + " Cr", "good"); EvEnd(ps, null); return true;
        }

        // =====================================================================
        // Statistik-Verlauf: Einnahmen je Stunde, gereinigte Parzellen je Tag und Planet
        // =====================================================================
        static void HistEarn(double cr)
        {
            if (S == null || cr <= 0) return; long h = Now() / 3600; if (S.incH == null) S.incH = new List<double>();
            if (S.incHour == 0) S.incHour = h; while (S.incHour < h && S.incH.Count < 200) { S.incH.Add(0); S.incHour++; } S.incHour = h; if (S.incH.Count == 0) S.incH.Add(0);
            S.incH[S.incH.Count - 1] += cr; while (S.incH.Count > 48) S.incH.RemoveAt(0);
        }
        static void HistCell(PState ps)
        {
            int d = (int)(Now() / 86400); var sv = ps.sv; if (sv.cellD == null) sv.cellD = new List<int>();
            if (sv.cellDay == 0) sv.cellDay = d; while (sv.cellDay < d && sv.cellD.Count < 60) { sv.cellD.Add(0); sv.cellDay++; } sv.cellDay = d; if (sv.cellD.Count == 0) sv.cellD.Add(0);
            sv.cellD[sv.cellD.Count - 1]++; while (sv.cellD.Count > 14) sv.cellD.RemoveAt(0);
        }
        /// <summary>Werte der letzten n Stunden (älteste zuerst), fehlende Stunden = 0.</summary>
        public static double[] IncomeHours(int n)
        {
            var r = new double[n]; if (S?.incH == null || S.incH.Count == 0) return r; long h = Now() / 3600; int lag = (int)Math.Max(0, h - S.incHour);
            for (int i = 0; i < n; i++) { int back = n - 1 - i - lag; int idx = S.incH.Count - 1 - back; if (back >= 0 && idx >= 0 && idx < S.incH.Count) r[i] = S.incH[idx]; } return r;
        }
        public static double[] CellDays(PState ps, int n)
        {
            var r = new double[n]; var sv = ps.sv; if (sv.cellD == null || sv.cellD.Count == 0) return r; int d = (int)(Now() / 86400), lag = Math.Max(0, d - sv.cellDay);
            for (int i = 0; i < n; i++) { int back = n - 1 - i - lag; int idx = sv.cellD.Count - 1 - back; if (back >= 0 && idx >= 0 && idx < sv.cellD.Count) r[i] = sv.cellD[idx]; } return r;
        }

        // =====================================================================
        // Putzi-7 – nach seinem Finale fährt er mit und kehrt ein bisschen Staub zusammen
        // =====================================================================
        public static void CheckPutzi(PState ps, FlagStories.Entry e) { if (S.putzi || !e.finale || FlagStories.ArcTitle(ps.pid).IndexOf("Putzi", StringComparison.OrdinalIgnoreCase) < 0 && FlagStories.ArcTitle(ps.pid).IndexOf("Scrubby", StringComparison.OrdinalIgnoreCase) < 0) return; S.putzi = true; Toast("Putzi-7 ist wieder wach und fährt ab jetzt auf jedem Planeten mit!", "good"); }
        public static float PutziRate(PState ps) => .25f + .25f * Gs(ps); // E/s Feinstaub ins Rohstofflager
        static void PutziStep(PState ps, float dt)
        {
            if (!S.putzi || ps.nClean < 3 || S.ruin == ps.pid) return; float room = RohCap(ps) - RohSum(ps); if (room <= 0) return;
            ps.sv.roh[GameData.Dirt[ps.P.start].mat] += Mathf.Min(room, PutziRate(ps) * dt);
        }

        // =====================================================================
        // Herausforderungen: drei Angebote, eine läuft mit Zeitlimit
        // =====================================================================
        public class ChalDef { public string id, name, desc; public float time; public Func<double> val; public Func<double> target; public int sd; }
        public static readonly List<ChalDef> Challenges = new List<ChalDef>
        {
            new ChalDef { id = "cells", name = "Blitzputz", desc = "Reinige {0} Parzellen", time = 300, val = () => S.stats.cells, target = () => Math.Max(5, Math.Round(CellsPerMin() * 5 * 1.3)) },
            new ChalDef { id = "hand", name = "Handarbeit", desc = "Wische {0} m² selbst", time = 240, val = () => S.stats.handM2, target = () => Math.Round(120 + 40 * Uv("radius") * Uv("radius")) },
            new ChalDef { id = "unload", name = "Fleißige Schaufel", desc = "Entlade deinen Behälter {0}-mal", time = 300, val = () => S.stats.handUnloads, target = () => 5 },
            new ChalDef { id = "rockets", name = "Raketenwelle", desc = "Starte {0} Raketen", time = 600, val = () => S.planets.Sum(p => p.nRockets), target = () => 3, sd = 1 },
            new ChalDef { id = "money", name = "Geldregen", desc = "Verdiene {0} Cr", time = 600, val = () => S.stats.earned, target = () => Math.Max(500, Math.Round(IncomePerMin() * 10 * 1.4)) },
            new ChalDef { id = "crystal", name = "Kristallernte", desc = "Ernte {0} Kristalle", time = 900, val = () => S.stats.crystals, target = () => 4, sd = 1 },
            new ChalDef { id = "meteor", name = "Sternenstaub-Jagd", desc = "Reinige {0} Meteoriten-Parzellen", time = 900, val = () => S.stats.meteors, target = () => 3 },
        };
        public static ChalDef Chal(string id) => Challenges.FirstOrDefault(c => c.id == id);
        static readonly Dictionary<string, double> chalTarget = new Dictionary<string, double>();
        public static double ChalTarget(string id) { if (!chalTarget.TryGetValue(id, out var t)) { t = Chal(id)?.target() ?? 1; chalTarget[id] = t; } return t; }
        public static double ChalReward(string id) { var c = Chal(id); return Math.Round(Math.Max(300 * Cur.P.diff, IncomePerMin() * c.time / 60 * .8)); }
        public static void ChalRefresh(bool force = false)
        {
            var ch = S.chal ?? (S.chal = new ChalSave()); long now = Now();
            if (!force && ch.offer.Count == 3 && now < ch.refresh) return;
            if (ch.act != "") return; ch.offer = Challenges.OrderBy(_ => fr.Next()).Take(3).Select(c => c.id).ToList(); ch.refresh = now + 4 * 3600; chalTarget.Clear();
        }
        public static double ChalProgress() { var ch = S.chal; if (ch == null || ch.act == "") return 0; var c = Chal(ch.act); return c == null ? 0 : (c.val() - ch.v0) / ChalTarget(ch.act); }
        public static bool ChalStart(string id)
        {
            var ch = S.chal; if (ch.act != "") return Fail("Es läuft schon eine Herausforderung"); var c = Chal(id); if (c == null) return false;
            ChalTarget(id); ch.act = id; ch.v0 = c.val(); ch.left = c.time; Toast("Los! " + c.name + ": " + string.Format(Loc.Tr(c.desc), Fmt(ChalTarget(id))) + " in " + Mathf.RoundToInt(c.time / 60) + " min", "good"); return true;
        }
        static void ChalStep(float dt)
        {
            var ch = S.chal; if (ch == null || ch.act == "" || Offline) return; var c = Chal(ch.act); if (c == null) { ch.act = ""; return; }
            ch.left -= dt;
            if (ChalProgress() >= 1) { double r = ChalReward(ch.act); Earn(r); if (c.sd > 0) S.stardust += c.sd; ch.done++; Toast("Herausforderung geschafft: " + c.name + " · +" + Fmt(r) + " Cr" + (c.sd > 0 ? " und " + c.sd + " Sternenstaub" : ""), "good"); ch.offer.Remove(ch.act); ch.act = ""; chalTarget.Remove(c.id); return; }
            if (ch.left <= 0) { Toast("Zeit um – " + c.name + " nicht geschafft. Versuch's noch mal!", "bad"); ch.act = ""; }
        }

        // =====================================================================
        // Riesenmeteor: zerstört ab und zu eine komplette Basis (nur live, nur einer zur Zeit)
        // =====================================================================
        public const float DOOM_COUNT = 60f; public const int DOOM_LOCK = 600;
        public static event Action<string> OnDoomStart, OnDoomImpact;
        public static bool Ruined(string pid) => S != null && S.ruin == pid && Now() < S.ruinUntil;
        public static long RuinLeft(string pid) => Ruined(pid) ? S.ruinUntil - Now() : 0;
        public static bool DoomActive => S != null && S.doomT > 0 && S.doomPid != "";
        static void DoomStep(float dt)
        {
            if (S.ruin != "" && Now() >= S.ruinUntil) { string r = S.ruin; S.ruin = ""; if (P.ContainsKey(r)) Toast(P[r].P.name + " ist wieder aufgebaut – du kannst dort neu anfangen", "good"); }
            if (Offline) return;
            if (S.doomT > 0)
            {
                S.doomT -= dt; if (S.doomT > 0) return;
                Impact(S.doomPid); S.doomT = -1; S.doomPid = ""; return;
            }
            if (S.ruin != "" || P.Count < 2 || Icp != null) return;
            if (S.doomNext < 0) S.doomNext = UnityEngine.Random.Range(2700f, 7200f); // 45–120 min Spielzeit
            S.doomNext -= dt; if (S.doomNext > 0) return;
            /* Fabian 02.11.: trifft zufällig einen freigeschalteten Planeten – fertig oder noch in Arbeit, nicht nur den aktuellen */
            var cand = P.Values.Where(q => !Ruined(q.pid) && q.nClean >= 20 && q.sv.veh.Count >= 2).ToList(); if (cand.Count == 0) { S.doomNext = 600; return; }
            S.doomNext = UnityEngine.Random.Range(2700f, 7200f); StartDoom(cand[UnityEngine.Random.Range(0, cand.Count)].pid);
        }
        public static void StartDoom(string pid)
        {
            if (S.ruin != "" || DoomActive || !P.ContainsKey(pid) || P.Count < 2) return; S.doomPid = pid; S.doomT = DOOM_COUNT;
            var tp = P[pid]; string def = tp.sv.msLv > 0 ? " · Schild " + Mathf.RoundToInt(ShieldChance(tp) * 100) + " %" : ""; if (tp.sv.flakLv > 0) def += " · " + FlakRockets(tp) + " Abwehrraketen" + (pid == S.planet ? "" : " (nur vor Ort)");
            Toast("RIESENMETEOR im Anflug auf " + tp.P.name + "! Einschlag in " + Mathf.RoundToInt(DOOM_COUNT) + " Sekunden" + (def == "" ? " – ohne Abwehr wird die Basis zerstört" : def), "bad"); OnDoomStart?.Invoke(pid);
        }
        static void Impact(string pid)
        {
            if (!P.ContainsKey(pid)) return; DoomOutcome(pid); // Fabian 02.11.: erst Schild, dann Abfangen – sonst Explosion (WorldView) und Ruin()
        }
        /// <summary>Planet zurücksetzen: alles auf Anfang, 10 Minuten gesperrt. Flaggen, Geschichten und Spielzeit bleiben.</summary>
        public static void Ruin(string pid)
        {
            int i = S.planets.FindIndex(p => p.pid == pid); if (i < 0) return; var old = S.planets[i];
            var fresh = NewPlanet(pid); fresh.freq = old.freq; fresh.tFirst = old.tFirst; fresh.tPlay = old.tPlay; fresh.tDone = old.tDone; fresh.tDoneAt = old.tDoneAt; fresh.flags = old.flags; fresh.flagsInit = old.flagsInit; fresh.prize = old.prize; fresh.cellD = old.cellD; fresh.cellDay = old.cellDay; fresh.pgDone = old.pgDone;
            S.planets[i] = fresh; P[pid] = Attach(fresh); S.ruin = pid; S.ruinUntil = Now() + DOOM_LOCK;
            if (S.planet == pid) { var to = S.planets.Where(p => p.pid != pid).OrderByDescending(p => p.tPlay).FirstOrDefault(); if (to != null) Travel(to.pid); }
            Toast(GameData.Planets[pid].name + " wurde zerstört. In 10 Minuten ist der Planet wieder aufgebaut – dann fängst du dort von vorn an.", "bad"); Save();
        }
    }
}
