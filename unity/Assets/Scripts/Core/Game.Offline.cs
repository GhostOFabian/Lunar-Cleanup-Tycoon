// Fabian 02.11.: Schneller Start nach der Offline-Zeit. Nachgerechnet wird sofort nur der aktuelle Planet (und alles Planetenübergreifende);
// die anderen Planeten merken sich ihre offene Zeit (offPend) und holen sie im Hintergrund nach – oder sofort, wenn man hinreist.
// Es bleibt dieselbe Simulation (StepPlanet), nur in anderer Reihenfolge.
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        /// <summary>Offline-Schritt nur für den aktuellen Planeten plus Planetenübergreifendes.</summary>
        public static void StepOffline(float dt)
        {
            StepPlanet(Cur, dt);
            ResearchStep(dt); DoomStep(dt); ChalStep(dt); OrdersStep(dt); AchievementStep(dt);
        }
        /// <summary>Offene Offline-Zeit an alle anderen Planeten verteilen (vor dem Nachrechnen).</summary>
        public static void DeferOthers(float seconds) { foreach (var q in P.Values) if (q.pid != S.planet) q.sv.offPend = Mathf.Min(q.sv.offPend + seconds, OfflineCap); }
        public static int PendingPlanets => P == null ? 0 : P.Values.Count(q => q.sv.offPend > .5f);
        /// <summary>Einen Planeten nachrechnen, höchstens budgetMs Millisekunden lang. Rückgabe: fertig?</summary>
        public static bool CatchUp(PState ps, float budgetMs)
        {
            if (ps.sv.offPend <= .5f) { ps.sv.offPend = 0; return true; }
            bool off = Offline; Offline = true; float t0 = Time.realtimeSinceStartup * 1000f;
            try
            {
                while (ps.sv.offPend > .5f && Time.realtimeSinceStartup * 1000f - t0 < budgetMs)
                {
                    float step = ps.sv.offPend > 7200 ? 4 : ps.sv.offPend > 1800 ? 3 : ps.sv.offPend > 600 ? 2 : 1; float d = Mathf.Min(step, ps.sv.offPend);
                    StepPlanet(ps, d); ps.sv.offPend -= d;
                }
            }
            finally { Offline = off; }
            if (ps.sv.offPend <= .5f) { ps.sv.offPend = 0; return true; }
            return false;
        }
        /// <summary>Hintergrund: pro Bild ein paar Millisekunden für den nächsten Planeten mit offener Zeit.</summary>
        public static void CatchUpTick(float budgetMs)
        {
            var q = P.Values.Where(x => x.sv.offPend > .5f && x.pid != S.planet).OrderBy(x => x.sv.offPend).FirstOrDefault();
            if (q != null) CatchUp(q, budgetMs);
        }
    }
}
