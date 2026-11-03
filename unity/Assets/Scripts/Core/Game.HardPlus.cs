// Fabian 03.11.: Hard Mode überarbeitet – vier Bausteine, alle nur im Hard Mode:
// 1. Ultimate-Leiste: selbst gewischte Parzellen und Kombos laden sie, voll = eine Gratis-Nuke (Slot ganz links in der Perk-Leiste).
// 2. Hard-Ziele: eigene Zielreihe zum Selbstputzen, Belohnung Geld (× Schwierigkeit des Planeten) plus Perk-Ladungen. Immer 3 gleichzeitig offen.
// 3. Putz-Level und Talente: Wischen gibt Erfahrung, jedes Level schaltet ein festes Talent frei (Pinsel, Behälter, Wischkraft, Helfer, Shuttle, Funde, Ultimate).
// 4. Perk-Funde und Schmutzwellen: beim Wischen liegt manchmal ein Perk im Dreck. Alle ~10 min rollt live eine Schmutzwelle auf saubere Grenzparzellen zu –
//    30 s Vorwarnung; wer die dreckigen Nachbarn rechtzeitig putzt, stoppt sie.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        // ---------- Putz-Level ----------
        public static int HardLevel { get { if (S == null) return 1; int l = 1; float x = S.hXp; while (x >= LevelNeed(l) && l < 99) { x -= LevelNeed(l); l++; } return l; } }
        public static float LevelNeed(int l) => 8 + 4 * l;
        public static float LevelXp { get { if (S == null) return 0; int l = 1; float x = S.hXp; while (x >= LevelNeed(l) && l < 99) { x -= LevelNeed(l); l++; } return x; } }
        public class Talent { public int lv; public string name, desc; }
        public static readonly Talent[] Talents =
        {
            new Talent { lv = 2, name = "Breiter Pinsel I", desc = "+10 % Pinselbreite" },
            new Talent { lv = 3, name = "Großer Behälter I", desc = "+25 % Behälter" },
            new Talent { lv = 4, name = "Kräftig wischen I", desc = "+15 % Wischkraft" },
            new Talent { lv = 5, name = "Flinke Helfer", desc = "Putzhelfer +30 % schneller" },
            new Talent { lv = 6, name = "Turbo-Shuttle", desc = "Sammelshuttle +25 % schneller" },
            new Talent { lv = 7, name = "Glückspilz", desc = "Perk-Funde doppelt so oft" },
            new Talent { lv = 8, name = "Breiter Pinsel II", desc = "+10 % Pinselbreite" },
            new Talent { lv = 9, name = "Großer Behälter II", desc = "+25 % Behälter" },
            new Talent { lv = 10, name = "Ultimate-Profi", desc = "Ultimate lädt 30 % schneller" },
            new Talent { lv = 11, name = "Meisterputzer", desc = "ab hier je Level +5 % Wischkraft (bis +50 %)" },
        };
        static bool Tal(int lv) => Hard && HardLevel >= lv;
        public static float TalRadius => (Tal(2) ? 1.1f : 1) * (Tal(8) ? 1.1f : 1);
        public static float TalHand => (Tal(3) ? 1.25f : 1) * (Tal(9) ? 1.25f : 1);
        public static float TalPower => !Hard ? 1 : (Tal(4) ? 1.15f : 1) * (1 + Mathf.Min(.5f, Mathf.Max(0, HardLevel - 10) * .05f));
        public static float TalHelper => Tal(5) ? 1.3f : 1;
        public static float TalShuttle => Tal(6) ? 1.25f : 1;
        static float TalFind => Tal(7) ? 2 : 1;
        static float TalUlt => Tal(10) ? 1.3f : 1;
        public static event Action<int, string> OnLevelUp;
        static void AddXp(float x)
        {
            int l0 = HardLevel; S.hXp += x; int l1 = HardLevel;
            if (l1 > l0) { var t = Talents.FirstOrDefault(q => q.lv == l1) ?? Talents.Last(); OnLevelUp?.Invoke(l1, t.name + " – " + t.desc); Toast("Putz-Level " + l1 + "! Neues Talent: " + t.name + " (" + t.desc + ")", "good"); }
        }

        // ---------- Ultimate ----------
        public const float ULT_CELL = 1f / 30;
        public static float Ult => S == null ? 0 : Mathf.Clamp01(S.ult);
        static void AddUlt(float x) { if (S.ult >= 1) return; S.ult = Mathf.Min(1, S.ult + x * TalUlt); if (S.ult >= 1) { S.ultFulls++; Toast("ULTIMATE BEREIT – tippe links in der Perk-Leiste auf „Ulti“", "good"); } }
        /// <summary>Gratis-Nuke aus der vollen Leiste.</summary>
        public static int UseUlt(Vector3 d)
        {
            if (S.ult < 1) { Fail("Ultimate lädt noch (" + Mathf.FloorToInt(Ult * 100) + " %) – wische selbst, Kombos laden schneller"); return 0; }
            PerkAdd("nuke", 1); int n = UseTool("nuke", d); if (n > 0) S.ult = 0; else PerkAdd("nuke", -1); return n;
        }

        // ---------- eine selbst gewischte Parzelle (live, aus OnCellDone) ----------
        static void HardHandCell(PState ps, int id, int combo)
        {
            if (!Hard) return; S.hHand++; S.hNoPerk++; if (S.hNoPerk > S.hNoPerkBest) S.hNoPerkBest = S.hNoPerk;
            AddXp(1 + .5f * Mathf.Max(0, combo - 1)); AddUlt(ULT_CELL + .005f * Mathf.Max(0, combo - 1));
            /* Perk-Fund im Dreck */
            if (fr.NextDouble() < .05f * TalFind)
            {
                string[] pool = { "bomb", "bomb", "turbo", "wide", "magnet", "foam", "espresso", "goldmop", "rohrpost", "kaercher" };
                string pk = pool[fr.Next(pool.Length)]; if (PerkN(pk) < PerkCap(pk)) { PerkAdd(pk, 1); FloatTxt(ps.pd.cells[id].c, "Fund: " + Perk(pk).name + "!", "good"); Toast("Fund im Dreck: " + Perk(pk).name + " – liegt jetzt unten in der Leiste", "good"); }
            }
        }
        /// <summary>Ein Perk wurde eingesetzt – die Serie „ohne Perk“ beginnt neu.</summary>
        static void PerkUsed() { if (S != null) S.hNoPerk = 0; }

        // ---------- Schmutzwellen (nur live, aktueller Planet) ----------
        public const float WAVE_EVERY = 600, WAVE_WARN = 30;
        public static float WaveIn(PState ps) => WAVE_EVERY - ps.sv.waveT;
        public static int WaveCell(PState ps) => ps.sv.waveCell;
        static bool WaveOk(PState ps, int i) => i >= 0 && ps.clean[i] && !ps.pd.cells[i].IsBase && !ps.pd.cells[i].Wet && ps.pd.cells[i].nb.Any(b => !ps.clean[b] && !ps.pd.cells[b].IsBase);
        static void WaveStep(PState ps, float dt)
        {
            if (!Hard || Offline || ps.pid != S.planet || IsDone(ps.pid) || ps.nClean < 12) return;
            ps.sv.waveT += dt;
            if (ps.sv.waveCell < 0 && WaveIn(ps) <= WAVE_WARN)
            {
                var pd = ps.pd; var c = new List<int>(); for (int i = 0; i < pd.N; i++) if (WaveOk(ps, i)) c.Add(i); if (c.Count == 0) { ps.sv.waveT = 0; return; }
                var near = ViewDir.sqrMagnitude > .5f ? c.Where(i => Vector3.Dot(pd.cells[i].c.normalized, ViewDir) > ViewCos).ToList() : c; if (near.Count == 0) near = c;
                ps.sv.waveCell = near[fr.Next(near.Count)]; Toast("Schmutzwelle in 30 s! Putz die dreckigen Nachbarn der markierten Stelle, dann stoppst du sie (Warnung oben antippen)", "bad");
            }
            if (WaveIn(ps) > 0) return;
            int w = ps.sv.waveCell; ps.sv.waveT = -(float)fr.NextDouble() * 120; ps.sv.waveCell = -1;
            if (!WaveOk(ps, w)) { S.waveStop++; AddXp(5); Toast("Schmutzwelle gestoppt! +5 Erfahrung", "good"); return; }
            var patch = new List<int> { w }; foreach (int b in ps.pd.cells[w].nb) if (patch.Count < 3 && WaveOk(ps, b)) patch.Add(b);
            int G = GameData.GRID;
            foreach (int cid in patch)
            {
                var cl = ps.pd.cells[cid]; var D = GameData.Dirt[cl.type];
                if (!ps.act.TryGetValue(cid, out var a)) { a = new Act { cl = cl, grid = new float[G * G], sum = 0, amt = D.amt * GameData.Kinds[cl.kind].amt, D = D, K = GameData.Kinds[cl.kind] }; ps.act[cid] = a; }
                for (int k = 0; k < 9; k++) { int gi = 0; for (int t = 0; t < 30; t++) { gi = fr.Next(G * G); if (cl.inside[gi]) break; } float x = -cl.H + ((gi % G) + .5f) * cl.cs, y = -cl.H + ((gi / G) + .5f) * cl.cs; DirtyAt(a, x, y, 3.2f + (float)fr.NextDouble() * 1.6f, .85f); }
                ps.dirtyCells.Add(cid);
            }
            PerkFx("wave", ps.pd.cells[w].c, patch); Toast("Schmutzwelle: " + patch.Count + " Parzellen sind wieder staubig", "bad");
        }

        // ---------- Hard-Ziele ----------
        public class HardGoal { public string t; public Func<float> p; public double r; public string perk; public int n; }
        static float Pc(string pid) => P.ContainsKey(pid) ? CleanPct(P[pid]) : 0;
        public static readonly HardGoal[] HardGoals =
        {
            new HardGoal { t = "Wische 10 Parzellen selbst", p = () => S.hHand / 10f, r = 300, perk = "bomb", n = 1 },
            new HardGoal { t = "Gib 3 Ladungen ans Sammelshuttle", p = () => S.stats.handUnloads / 3f, r = 300, perk = "turbo", n = 1 },
            new HardGoal { t = "Schaffe eine Kombo ×5", p = () => S.stats.comboMax / 5f, r = 500, perk = "wide", n = 1 },
            new HardGoal { t = "Erreiche Putz-Level 3", p = () => (HardLevel - 1) / 2f, r = 600, perk = "magnet", n = 1 },
            new HardGoal { t = "Wische 25 Parzellen am Stück ohne Perk", p = () => S.hNoPerkBest / 25f, r = 800, perk = "bomb", n = 2 },
            new HardGoal { t = "Mond zu 50 % sauber", p = () => Pc("moon") / .5f, r = 1000, perk = "foam", n = 1 },
            new HardGoal { t = "Wische 60 Parzellen selbst", p = () => S.hHand / 60f, r = 1200, perk = "kaercher", n = 1 },
            new HardGoal { t = "Lade die Ultimate einmal voll", p = () => S.ultFulls, r = 1500, perk = "goldmop", n = 1 },
            new HardGoal { t = "Schaffe eine Kombo ×8", p = () => S.stats.comboMax / 8f, r = 1500, perk = "espresso", n = 1 },
            new HardGoal { t = "Stoppe eine Schmutzwelle", p = () => S.waveStop, r = 2000, perk = "bomb", n = 2 },
            new HardGoal { t = "Erreiche Putz-Level 6", p = () => (HardLevel - 1) / 5f, r = 2500, perk = "tornado", n = 1 },
            new HardGoal { t = "Schalte den Mars frei", p = () => P.ContainsKey("mars") ? 1 : 0, r = 3000, perk = "rohrpost", n = 1 },
            new HardGoal { t = "Wische 200 Parzellen selbst", p = () => S.hHand / 200f, r = 5000, perk = "bomb", n = 3 },
            new HardGoal { t = "Schaffe die volle Kombo ×10", p = () => S.stats.comboMax / 10f, r = 6000, perk = "kaercher", n = 2 },
            new HardGoal { t = "Stoppe 3 Schmutzwellen", p = () => S.waveStop / 3f, r = 8000, perk = "tornado", n = 1 },
            new HardGoal { t = "Erreiche Putz-Level 10", p = () => (HardLevel - 1) / 9f, r = 10000, perk = "tornado", n = 2 },
            new HardGoal { t = "Mars zu 50 % sauber", p = () => Pc("mars") / .5f, r = 12000, perk = "foam", n = 2 },
            new HardGoal { t = "Wische 500 Parzellen selbst", p = () => S.hHand / 500f, r = 20000, perk = "goldmop", n = 2 },
            new HardGoal { t = "Schalte Europa frei", p = () => P.ContainsKey("europa") ? 1 : 0, r = 30000, perk = "kaercher", n = 3 },
            new HardGoal { t = "Erreiche Putz-Level 15", p = () => (HardLevel - 1) / 14f, r = 50000, perk = "tornado", n = 3 },
        };
        public static bool HardGoalDone(int i) => S.hGoals != null && S.hGoals.Contains(i);
        public static float HardGoalProg(int i) => Mathf.Clamp01(HardGoals[i].p());
        public static double HardGoalReward(int i) => Math.Round(HardGoals[i].r * Cur.P.diff);
        /// <summary>Die drei nächsten offenen Hard-Ziele.</summary>
        public static List<int> HardGoalsOpen() { var o = new List<int>(); if (!Hard) return o; for (int i = 0; i < HardGoals.Length && o.Count < 3; i++) if (!HardGoalDone(i)) o.Add(i); return o; }
        public static int HardGoalsClaimable() => HardGoalsOpen().Count(i => HardGoalProg(i) >= 1);
        public static bool ClaimHardGoal(int i)
        {
            if (!Hard || HardGoalDone(i) || HardGoalProg(i) < 1) return false; var g = HardGoals[i]; double r = HardGoalReward(i);
            (S.hGoals ??= new List<int>()).Add(i); Earn(r); if (!string.IsNullOrEmpty(g.perk)) PerkAdd(g.perk, g.n);
            Toast("Hard-Ziel geschafft · +" + Fmt(r) + " Cr" + (g.perk != "" ? " · " + g.n + "× " + Perk(g.perk).name : ""), "good"); Save(); return true;
        }
    }
}
