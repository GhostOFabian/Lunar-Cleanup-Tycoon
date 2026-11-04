// Fabian 04.11.: Leuchtkristalle bekommen einen echten Nutzen. Geerntete Kristalle (antippen oder Fahrzeuge) landen nicht mehr als
// Rohstoff im Behälter, sondern als eigene Währung „Kristalle“ (oben unter dem Geld). Ausgeben im Kristall-Fenster:
// normal – Kristall-Schub (alle Fahrzeuge, Bänder, Maschinen 5 min +50 %), Forschung sofort fertig, Wiederaufbau sofort;
// Hard Mode – zusätzlich Anzug sofort voll, Ulti +20 %. Beim Antippen im Hard Mode gibt jeder Kristall außerdem +25 Anzug-Energie und +3 % Ulti.
using System;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static int Kristalle => S == null ? 0 : Mathf.FloorToInt(S.lk);
        static int CrystalGain(CrystalSave c) => Mathf.Max(1, Mathf.RoundToInt(CrystalTypes[c.t].mul * (.4f + .6f * c.g) * 2));
        static void GiveCrystals(PState ps, CrystalSave c, bool hand)
        {
            int n = CrystalGain(c); S.lk += hand ? n : n * VEH_CR; S.stats.crystals++; /* 04.11.: Fahrzeuge ernten nur ein Fünftel – sonst wird man von Kristallen überschwemmt */
            if (hand && Hard) { if (S.suit < 0) S.suit = SUIT_MAX; S.suit = Mathf.Min(SUIT_MAX, S.suit + 25); AddUlt(.03f); }
            if (hand) FloatTxt(CrystalDir(ps, c), "+" + n + " Kristall" + (n == 1 ? "" : "e") + (Hard ? " · Anzug +25" : ""), "good");
        }
        public const float VEH_CR = .2f; public const int BOOST_COST = 5; public const float BOOST_T = 300;
        public static float BoostLeft(PState ps) => Mathf.Max(0, ps.sv.boostT);
        public static bool CrystalBoost()
        {
            var ps = Cur; if (Kristalle < BOOST_COST) return Fail("Es fehlen Kristalle – " + BOOST_COST + " nötig, du hast " + Kristalle);
            S.lk -= BOOST_COST; ps.sv.boostT = Mathf.Max(0, ps.sv.boostT) + BOOST_T; Toast("Kristall-Schub auf " + ps.P.name + ": alles läuft 5 min lang 50 % schneller", "good"); Save(); return true;
        }
        public static int ResInstantCost() { var d = ResActive; if (d == null) return 0; return Mathf.Max(1, Mathf.CeilToInt((d.time - S.resT) / Mathf.Max(.01f, ResSpeed()) / 120f)); }
        public static bool ResInstant()
        {
            var d = ResActive; if (d == null) return Fail("Gerade läuft keine Forschung"); int c = ResInstantCost(); if (Kristalle < c) return Fail("Es fehlen Kristalle – " + c + " nötig");
            S.lk -= c; S.resT = d.time; Save(); return true;
        }
        public static int RuinInstantCost() => S.ruin != "" ? Mathf.Max(3, Mathf.CeilToInt(RuinLeft(S.ruin) / 300f)) : 0;
        public static bool RuinInstant()
        {
            if (S.ruin == "" || !Ruined(S.ruin)) return Fail("Kein Planet wird gerade wieder aufgebaut"); int c = RuinInstantCost(); if (Kristalle < c) return Fail("Es fehlen Kristalle – " + c + " nötig");
            S.lk -= c; S.ruinUntil = Now(); Save(); return true;
        }
        public static bool SuitFill() { if (!Hard) return false; if (Kristalle < 2) return Fail("Es fehlen Kristalle – 2 nötig"); S.lk -= 2; S.suit = SUIT_MAX; Toast("Anzug voll geladen", "good"); return true; }
        public static bool UltBoost() { if (!Hard) return false; if (S.ult >= 1) return Fail("Die Ulti ist schon voll"); if (Kristalle < 5) return Fail("Es fehlen Kristalle – 5 nötig"); S.lk -= 5; AddUlt(.2f / TalUlt); return true; }
    }
}
