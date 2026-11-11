// Fabian 02.11.: Spätes Spiel ohne Stau. Ist das Rohstoff- oder Produktlager voll und sind die Upgrades ausgereizt, stand bisher alles still.
// Der Export-Frachter nimmt den Überschuss ab (ab 90 % Füllung, bis 3 % der Lagergröße je Sekunde) und zahlt 40 % des Verkaufswerts.
// So läuft die Basis weiter – volle Lager sind eine Entscheidung (mehr Lager, mehr Verarbeitung oder billig exportieren), kein toter Punkt.
using System;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const float XPORT_FROM = .9f, XPORT_RATE = .03f;
        public static float XPORT_VALUE => Platform.Owned("xlic") ? .6f : .4f; /* Shop: Export-Lizenz */
        public static string XportPct => Mathf.RoundToInt(XPORT_VALUE * 100) + " %";
        public static bool XportOpen(PState ps) => Gs(ps) >= 3 && ps.bld.Contains("roh");
        public static double XportCost(PState ps) => Math.Round(250000 * Math.Pow(6, Math.Max(0, Gs(ps) - 3)));
        public static string XportWhy(PState ps) => ps.sv.xport ? "" : !XportOpen(ps) ? "Erst Basisstufe 3" : S.credits < XportCost(ps) ? Lacks(XportCost(ps)) : "";
        public static bool BuyXport(PState ps)
        {
            if (ps.sv.xport) return false; string w = XportWhy(ps); if (w != "") return Fail(w);
            S.credits -= XportCost(ps); ps.sv.xport = true; Toast("Export-Frachter auf " + ps.P.name + " · volle Lager laufen nicht mehr über, der Überschuss bringt " + XportPct + " des Werts", "good"); Save(); return true;
        }
        /// <summary>Teil der Simulation (live und offline): Überschuss abnehmen und bezahlen.</summary>
        static void XportStep(PState ps, float dt)
        {
            var sv = ps.sv; if (!sv.xport) return; double pay = 0;
            float rc = RohCap(ps), rs = RohSum(ps);
            if (rc > 0 && rs > rc * XPORT_FROM) { float take = Mathf.Min(rs - rc * (XPORT_FROM - .02f), rc * XPORT_RATE * dt), k = take / rs; for (int m = 0; m < sv.roh.Length; m++) { float t = sv.roh[m] * k; if (t <= 0) continue; sv.roh[m] -= t; pay += t * MatValue(m, ps) * XPORT_VALUE; } }
            if (ps.bld.Contains("prod")) { float pc = ProdCap(ps), p0 = ProdUsed(ps); if (pc > 0 && p0 > pc * XPORT_FROM) { float take = Mathf.Min(p0 - pc * (XPORT_FROM - .02f), pc * XPORT_RATE * dt), k = take / p0; for (int m = 0; m < sv.prod.Length; m++) { float t = Mathf.Max(0, sv.prod[m] - KeepProd(ps, m)) * k; /* 11.11.: Bestelltes bleibt liegen */ if (t <= 0) continue; sv.prod[m] -= t; pay += t * MatValue(m, ps) * XPORT_VALUE; } } }
            if (pay > 0) { Earn(pay); sv.xEarn += pay; }
        }
    }
}
