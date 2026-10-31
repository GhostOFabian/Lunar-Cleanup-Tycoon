// Flotte: feste Obergrenze je Planet und Selbstzerstörung (um kleine Fahrzeuge gegen größere zu tauschen).
using System;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const int VEH_MAX = 24;          // Obergrenze je Planet (plus Shop-Platz)
        const float BOOM_TIME = 4f, BOOM_REFUND = .3f;
        public static int VehMax(PState ps) => VEH_MAX + (S != null ? S.extraSlots : 0);

        public static void SelfDestruct(VehSave v)
        {
            var ps = Cur; if (!ps.sv.veh.Contains(v) || v.mode == "boom") return;
            if (v.lift >= 0 || v.mode == "lifted") { Fail("Das Fahrzeug hängt gerade an einer Lastdrohne"); return; }
            v.mode = "boom"; v.repT = BOOM_TIME; v.cell = -1; v.busy = false; Toast(VehName(v) + ": Selbstzerstörung in " + (int)BOOM_TIME + " s", "bad");
        }
        public static double BoomRefund(PState ps, VehSave v) => Math.Round(GameData.Veh[v.type].cost * ps.P.diff * BOOM_REFUND);
        static void BoomStep(PState ps, VehSave v, float dt)
        {
            v.repT -= dt; if (v.repT > 0) return;
            ps.sv.veh.Remove(v); double r = BoomRefund(ps, v); S.credits += r;
            if (!Offline && ps.pid == S.planet) { OnBoom?.Invoke(v.d); Toast(VehName(v) + " gesprengt · Schrottwert +" + Fmt(r) + " Cr · Platz frei", "good"); }
        }
    }
}
