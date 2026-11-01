// Flotte: feste Obergrenze je Planet und Selbstzerstörung (um kleine Fahrzeuge gegen größere zu tauschen).
using System;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const int VEH_MAX = 15, VEH_CAP = 20; // Fabian 01.11.: Flotte 15 je Planet, mit teuren Flottenplätzen bis 20
        const float BOOM_TIME = 4f, BOOM_REFUND = .3f;
        public static int VehMax(PState ps) => Math.Min(VEH_CAP, VEH_MAX + (S != null ? S.fleetPlus + S.extraSlots : 0));
        /// <summary>Fabian 01.11.: voll ausgebaute Fahrzeuge waren zu schnell – Fahrwerk höchstens ×2, Bürsten höchstens ×4,6 (Kosten bleiben).</summary>
        static void DampVehicleUpgrades()
        {
            var de = new System.Globalization.CultureInfo("de-DE");
            void D(string id, double step) { if (!GameData.Up.TryGetValue(id, out var u)) return; for (int l = 0; l < u.vals.Length; l++) u.vals[l] = 1 + step * l; if (u.show != null) for (int l = 0; l < u.show.Length && l < u.vals.Length; l++) u.show[l] = "×" + u.vals[l].ToString("0.00", de); }
            D("vspeed", .05); D("vpower", .12);
        }
        /// <summary>Preis des nächsten Flottenplatzes: 25 Mio, dann je Platz ×6.</summary>
        public static double FleetPlusCost() => 25e6 * Math.Pow(6, S.fleetPlus);
        public static bool BuyFleetPlus() { if (VehMax(Cur) >= VEH_CAP || S.credits < FleetPlusCost()) return false; S.credits -= FleetPlusCost(); S.fleetPlus++; Toast("Flottenplatz gekauft – jetzt bis " + VehMax(Cur) + " Fahrzeuge je Planet", "good"); return true; }

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
