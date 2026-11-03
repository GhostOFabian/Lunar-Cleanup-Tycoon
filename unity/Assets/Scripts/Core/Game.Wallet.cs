// Fabian 03.11.: Eigene Planetenkasse – jeder Planet hat sein eigenes Geld. S.credits ist immer die Kasse des Planeten, auf dem du gerade bist;
// die Kassen der anderen Planeten liegen in PlanetSave.cash. Was ein Planet verdient (auch offline), landet in seiner eigenen Kasse.
// Zu einem Planeten darf von außen insgesamt nur ein Startbudget kommen (2× Freischaltpreis, mindestens 25 000 Cr) – beim Freischalten oder als Zuschuss.
// Dazu „Betreuung“: Ohne dich laufen Maschinen und Fahrzeuge nach 30 min langsam schwächer (bis 35 % nach 4,5 h). Antippen oder Wischen bringt sie sofort zurück.
using System;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        static string earnPid; /* Planet, der gerade simuliert wird – seine Einnahmen gehen in seine Kasse */
        /// <summary>Kasse eines Planeten (aktiver Planet = S.credits).</summary>
        public static double Cash(PState ps) => ps.pid == S.planet ? S.credits : ps.sv.cash;
        static void AddCash(PState ps, double c) { if (ps.pid == S.planet) S.credits += c; else ps.sv.cash += c; }
        /// <summary>Wie viel Geld von außen insgesamt zu diesem Planeten darf.</summary>
        public static double ImportCap(string pid) => Math.Max(25000, 2 * GameData.Planets[pid].unlockCost);
        public static double ImportLeft(PState ps) => Math.Max(0, ImportCap(ps.pid) - ps.sv.cashIn);
        /// <summary>Beim Wechsel des Planeten: Kasse des alten Planeten weglegen, die des neuen holen.</summary>
        static void SwapWallet(string from, string to)
        {
            if (from == to) return; if (from != null && P.ContainsKey(from)) P[from].sv.cash = S.credits; S.credits = P.ContainsKey(to) ? P[to].sv.cash : 0; if (P.ContainsKey(to)) P[to].sv.cash = 0;
        }
        /// <summary>Neuer Planet: Startbudget aus der Kasse des alten Planeten mitnehmen (höchstens ImportCap).</summary>
        static double CarryTo(string from, string to)
        {
            if (!P.ContainsKey(from) || !P.ContainsKey(to)) return 0; double take = Math.Min(S.credits, ImportCap(to)); if (take <= 0) return 0;
            S.credits -= take; P[to].sv.cash += take; P[to].sv.cashIn += take; return take;
        }
        /// <summary>Zuschuss vom aktiven Planeten an einen anderen (zählt gegen dessen Startbudget).</summary>
        public static bool SendCash(string pid)
        {
            if (!P.ContainsKey(pid) || pid == S.planet) return false; var t = P[pid]; double amt = Math.Min(S.credits, ImportLeft(t));
            if (ImportLeft(t) < 1) return Fail(t.P.name + " hat sein Startbudget schon bekommen – dort musst du selbst verdienen");
            if (amt < 1) return Fail("In dieser Kasse ist kein Geld");
            S.credits -= amt; t.sv.cash += amt; t.sv.cashIn += amt; Toast(Fmt(amt) + " Cr an " + t.P.name + " geschickt", "good"); Save(); return true;
        }
        /// <summary>Alte Spielstände: alles Geld liegt bisher in einer Kasse. Jeder andere Planet bekommt sein Startbudget daraus, der Rest bleibt hier.</summary>
        static void MigrateWallet()
        {
            if (S.walletV >= 1) return; S.walletV = 1; if (S.planets.Count <= 1) return;
            double moved = 0; foreach (var ps in P.Values) { if (ps.pid == S.planet) continue; double g = Math.Min(S.credits, ImportCap(ps.pid)); S.credits -= g; ps.sv.cash += g; ps.sv.cashIn += g; moved += g; }
            if (moved > 0) Toast("Neu: Jeder Planet hat jetzt seine eigene Kasse. Deine anderen Planeten haben ihr Startbudget bekommen (zusammen " + Fmt(moved) + " Cr), der Rest bleibt hier.", "good");
        }
        // ---------- Betreuung ----------
        public const float CARE_GRACE = 1800, CARE_FALL = 4 * 3600, CARE_MIN = .35f;
        /// <summary>Leistung nach Betreuung: 30 min voll, dann langsam bis 35 %.</summary>
        public static float CareF(PState ps) => ps.sv.careT <= CARE_GRACE ? 1 : Mathf.Max(CARE_MIN, 1 - (ps.sv.careT - CARE_GRACE) / CARE_FALL * (1 - CARE_MIN));
        /// <summary>Spieler tippt oder wischt: die Crew auf diesem Planeten ist wieder voll da.</summary>
        public static void Attend()
        {
            if (S == null) return; var ps = Cur; if (ps.sv.careT > CARE_GRACE + 60 && !Offline) Toast("Die Crew auf " + ps.P.name + " ist wieder voll bei der Sache – " + Mathf.RoundToInt(CareF(ps) * 100) + " % → 100 %", "good");
            ps.sv.careT = 0;
        }
    }
}
