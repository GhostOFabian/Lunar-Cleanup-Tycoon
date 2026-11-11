// Fabian 11.11.: Medaillenbuch – jeder Planet bekommt eine Medaille, die jeden Neustart überlebt.
// Bronze: Planet komplett sauber. Silber: dazu alle Truhen geöffnet und das Puzzle zusammengesetzt.
// Gold: dazu alle 3 Sterne, das Fund-Album komplett und Terraforming bis zum Wald.
// Belohnung beim ersten Mal: 2 / 5 / 10 Sternenstaub. Dauerhaft: Silber +1 %, Gold +2 % Verkaufspreis auf allen Planeten.
// Bestzeit: kürzeste Spielzeit auf dem Planeten, bis er sauber war – in jedem Durchlauf neu zu schlagen.
// Normaler Modus und Hard Mode haben getrennte Spielstände und damit getrennte Bücher.
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static readonly string[] MedalName = { "Keine", "Bronze", "Silber", "Gold" };
        static readonly int[] MedalDust = { 0, 2, 5, 10 };
        static MedalSave Medal(string pid, SaveData s = null)
        {
            s = s ?? S; if (s.medals == null) s.medals = new System.Collections.Generic.List<MedalSave>();
            var m = s.medals.FirstOrDefault(x => x.pid == pid); if (m == null) { m = new MedalSave { pid = pid }; s.medals.Add(m); } return m;
        }
        public static int MedalTier(string pid) => S?.medals?.FirstOrDefault(x => x.pid == pid)?.tier ?? 0;
        public static double MedalBest(string pid) => S?.medals?.FirstOrDefault(x => x.pid == pid)?.best ?? 0;
        public static int MedalRuns(string pid) => S?.medals?.FirstOrDefault(x => x.pid == pid)?.runs ?? 0;
        /// <summary>Was der Planet gerade erfüllt (0–3).</summary>
        public static int MedalNow(PState ps)
        {
            if (!IsDone(ps.pid)) return 0;
            bool chests = ps.sv.flags == null || ps.sv.flags.Count == 0 || ps.sv.flags.All(f => f.got);
            bool puzzle = !SecretOn(ps) || ps.sv.puzzleDone;
            if (!chests || !puzzle) return 1;
            bool stars = !SecretOn(ps) || StarCount(ps) >= 3;
            bool album = !FeatureData.Finds.TryGetValue(ps.pid, out var al) || FindsOn(ps.pid) >= al.Length;
            return stars && album && ps.sv.terra >= TERRA_MAX ? 3 : 2;
        }
        /// <summary>Was zur nächsten Medaille fehlt (für das Buch).</summary>
        public static string MedalNext(PState ps)
        {
            if (ps == null) return "Planet noch nicht erreicht";
            if (!IsDone(ps.pid)) return "Bronze: Planet komplett reinigen (" + Mathf.FloorToInt(CleanPct(ps) * 100) + " %)";
            var miss = new System.Collections.Generic.List<string>();
            int t = Mathf.Max(MedalNow(ps), MedalTier(ps.pid));
            if (t < 2)
            {
                if (ps.sv.flags != null && ps.sv.flags.Any(f => !f.got)) miss.Add("Truhen " + FlagsGot(ps) + "/" + ps.sv.flags.Count);
                if (SecretOn(ps) && !ps.sv.puzzleDone) miss.Add("Puzzle zusammensetzen");
                return "Silber: " + string.Join(" · ", miss);
            }
            if (t < 3)
            {
                if (SecretOn(ps) && StarCount(ps) < 3) miss.Add("Sterne " + StarCount(ps) + "/3");
                if (FeatureData.Finds.TryGetValue(ps.pid, out var al) && FindsOn(ps.pid) < al.Length) miss.Add("Funde " + FindsOn(ps.pid) + "/" + al.Length);
                if (ps.sv.terra < TERRA_MAX) miss.Add("Terraforming " + ps.sv.terra + "/" + TERRA_MAX);
                return "Gold: " + (miss.Count > 0 ? string.Join(" · ", miss) : "gleich geschafft");
            }
            return "Gold – alles geschafft";
        }
        /// <summary>Dauerhafter Bonus aufs Verkaufen: Silber +1 %, Gold +2 % je Planet.</summary>
        public static double MedalPriceF() { if (S?.medals == null) return 1; double f = 1; foreach (var m in S.medals) f += m.tier >= 3 ? .02 : m.tier == 2 ? .01 : 0; return f; }
        public static int MedalCount(int tier) => S?.medals?.Count(m => m.tier >= tier) ?? 0;
        /// <summary>Prüft einen Planeten (aus StepPlanet, höchstens alle 5 s Spielzeit – live und offline gleich).</summary>
        static void MedalStep(PState ps, float dt)
        {
            ps.medT += dt; if (ps.medT < 5) return; ps.medT = 0; MedalCheck(ps);
        }
        static void MedalCheck(PState ps)
        {
            if (S == null || !IsDone(ps.pid)) return; var m = Medal(ps.pid);
            if (ps.sv.tDone > 0 && (m.best <= 0 || ps.sv.tDone < m.best - .5)) { bool rec = m.best > 0; m.best = ps.sv.tDone; if (rec && !Offline) Toast("Neue Bestzeit auf " + ps.P.name + ": " + DurTxt(m.best), "good"); }
            int t = MedalNow(ps); if (t <= m.tier) return;
            int dust = 0; for (int k = m.tier + 1; k <= t; k++) dust += MedalDust[k]; m.tier = t; S.stardust += dust;
            Toast(MedalName[t] + "-Medaille für " + ps.P.name + "! +" + dust + " Sternenstaub" + (t >= 2 ? " · +" + (t >= 3 ? 2 : 1) + " % Verkaufspreis überall" : ""), "good");
        }
        static void MedalAll() { if (S == null) return; foreach (var ps in P.Values) MedalCheck(ps); }
        public static string DurTxt(double sec) { long t = (long)sec; long d = t / 86400, h = t % 86400 / 3600, mi = t % 3600 / 60; return d > 0 ? d + " T " + h + " h" : h > 0 ? h + " h " + mi + " min" : Mathf.Max(1, mi) + " min"; }
    }
}
