// Fabian 02.11.: Der Bogen jedes Planeten: verdreckt → aufgeräumt → Industrie → Terraforming → „Planet gerettet“.
// Ein geretteter Planet (Terraforming Stufe 5) kann ruhen: er liefert dann dauerhaft sein Einkommen (gemessen über die letzten ~10 min),
// jeder ruhende Planet hebt die Verkaufspreise überall um 5 %, und er braucht keine Pflege (kein Verfall, keine Riesenmeteore, keine Rechenzeit).
// Besuchst du ihn, läuft er ganz normal; aufwecken geht jederzeit.
using System;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const float REST_PRICE = .05f;
        static int restN = -1;
        public static int RestCount { get { if (restN < 0) restN = S == null ? 0 : S.planets.Count(p => p.rest); return restN; } }
        public static double RestSellF => 1 + REST_PRICE * RestCount;
        public static bool Saved(PState ps) => ps.sv.terra >= TERRA_MAX;
        public static string RestWhy(PState ps) => ps.sv.rest ? "" : !Saved(ps) ? "Erst Terraforming Stufe " + TERRA_MAX + " (Wald) – dann ist der Planet gerettet" : "";
        public static void SetRest(PState ps, bool on)
        {
            if (on == ps.sv.rest) return; if (on && RestWhy(ps) != "") { Fail(RestWhy(ps)); return; }
            ps.sv.rest = on; restN = -1;
            if (on) { ps.sv.restRate = Math.Max(ps.sv.rateEma, 1); Toast(ps.P.name + " ruht · liefert " + Fmt(ps.sv.restRate * 3600) + " Cr/h, alle Preise +" + Mathf.RoundToInt(REST_PRICE * 100) + " % · ohne Pflege", "good"); }
            else Toast(ps.P.name + " ist wieder ganz im Betrieb", "");
            Save();
        }
        /// <summary>Einkommen dieses Planeten (Cr/s), geglättet über ~10 min – Grundlage für den Ruhemodus.</summary>
        static void RateTrack(PState ps, float dt)
        {
            var sv = ps.sv; if (dt <= 0) return; if (sv.lastSoldP <= 0 || sv.soldP < sv.lastSoldP) { sv.lastSoldP = sv.soldP; return; }
            double r = (sv.soldP - sv.lastSoldP) / dt; sv.lastSoldP = sv.soldP; float k = 1 - Mathf.Exp(-dt / 600f); sv.rateEma += (r - sv.rateEma) * k;
        }
        /// <summary>Ruhender Planet (nicht der aktuelle): nur Einkommen, keine Simulation. Rückgabe true = fertig.</summary>
        static bool RestStep(PState ps, float dt)
        {
            if (!ps.sv.rest || ps.pid == S.planet) return false;
            double c = ps.sv.restRate * dt; if (c > 0) { Earn(c); ps.sv.restEarn += c; ps.sv.soldP += c; ps.sv.lastSoldP = ps.sv.soldP; }
            return true;
        }
    }
}
