// Flaggen (Fabian 01.11.): Auf jedem Planeten liegen Flaggen unter dem Schmutz – Mond 10, jeder weitere Planet 2 mehr.
// Sichtbar werden sie, sobald ihre Parzelle sauber ist; antippen = einsammeln (Belohnung steigt mit jeder Flagge).
// Gefundene Flaggen gehören dir: du kannst sie beschriften, der Text steht auf dem Fahnentuch.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class FlagSave { public int cell; public float x, y; public bool got; public string text = ""; }

    public static partial class Game
    {
        /// <summary>Zählt hoch, wenn sich etwas an den Flaggen ändert (für die Darstellung).</summary>
        public static int FlagVer;
        public static int FlagCount(string pid) { int i = Array.IndexOf(GameData.POrder, pid); return 10 + 2 * Math.Max(0, i); }
        public static int FlagsGot(PState ps) => ps.sv.flags.Count(f => f.got);
        static void InitFlags(PState ps)
        {
            var sv = ps.sv; if (sv.flags == null) sv.flags = new List<FlagSave>(); if (sv.flagsInit) return; sv.flagsInit = true;
            var pd = ps.pd; int want = FlagCount(ps.pid); var rnd = new System.Random(ps.pid.GetHashCode() ^ 0x71a9); var sites = new HashSet<int>(OutpostSites(ps.pid));
            var cand = pd.cells.Where(c => !c.IsBase && !c.Wet && !sites.Contains(c.id) && c.ring >= 1).Select(c => c.id).ToList(); var used = new List<int>();
            for (int tries = 0; tries < want * 30 && sv.flags.Count < want && cand.Count > 0; tries++)
            {
                int id = cand[rnd.Next(cand.Count)]; if (used.Contains(id)) continue;
                float minD = pd.R * 3.1f / Mathf.Sqrt(want) * (tries < want * 15 ? 1 : .5f); bool near = false; foreach (int u in used) if (pd.Arc(pd.cells[u].c, pd.cells[id].c) < minD) { near = true; break; } if (near) continue; // gleichmäßig verteilt
                used.Add(id); var cl = pd.cells[id]; float a = (float)rnd.NextDouble() * 6.283f, r = (float)rnd.NextDouble() * cl.H * .45f;
                sv.flags.Add(new FlagSave { cell = id, x = Mathf.Cos(a) * r, y = Mathf.Sin(a) * r });
            }
        }
        public static Vector3 FlagDir(PState ps, FlagSave f) { var cl = ps.pd.cells[f.cell]; return ps.pd.ToDir(cl, f.x, f.y); }
        public static bool FlagVisible(PState ps, FlagSave f) => f.got || ps.clean[f.cell];
        public static double FlagReward(PState ps, int nth) => Math.Round(300 * Math.Pow(ps.P.diff, .9) * (1 + nth * .5));
        public static void CollectFlag(PState ps, FlagSave f)
        {
            if (f.got || !ps.clean[f.cell]) return; int n = FlagsGot(ps); double r = FlagReward(ps, n); f.got = true; S.stats.flags++; Earn(r); FlagVer++;
            int all = ps.sv.flags.Count, got = n + 1;
            Toast("Flagge gefunden " + got + "/" + all + " · +" + Fmt(r) + " Cr – antippen zum Beschriften", "good"); FloatTxt(FlagDir(ps, f), "+" + Fmt(r) + " Cr", "big");
            if (got == all) { double b = Math.Round(r * 10); Earn(b); Toast("Alle Flaggen auf " + ps.P.name + " gefunden! Bonus +" + Fmt(b) + " Cr", "good"); }
        }
        public static void SetFlagText(FlagSave f, string t) { f.text = (t ?? "").Trim(); if (f.text.Length > 24) f.text = f.text.Substring(0, 24); FlagVer++; }
    }
}
