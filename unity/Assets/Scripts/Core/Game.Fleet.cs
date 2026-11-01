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
            /* Fabian 01.11.: Sammelwagen darf schneller fahren – alle Stufen ×1,6 */
            if (GameData.Up.TryGetValue("cartspd", out var cu) && cu.vals.Length > 0 && cu.vals[0] < 10) { for (int l = 0; l < cu.vals.Length; l++) cu.vals[l] *= 1.6; if (cu.show != null) for (int l = 0; l < cu.show.Length && l < cu.vals.Length; l++) cu.show[l] = cu.vals[l].ToString("0.0", de) + " m/s"; }
        }
        /// <summary>Preis des nächsten Flottenplatzes: 25 Mio, dann je Platz ×6.</summary>
        public static double FleetPlusCost() => 25e6 * Math.Pow(6, S.fleetPlus);
        public static bool BuyFleetPlus() { if (VehMax(Cur) >= VEH_CAP || S.credits < FleetPlusCost()) return false; S.credits -= FleetPlusCost(); S.fleetPlus++; Toast("Flottenplatz gekauft – jetzt bis " + VehMax(Cur) + " Fahrzeuge je Planet", "good"); return true; }

        /// <summary>Fabian 01.11.: Fahrzeug zurücksetzen – steckt es fest, steht es danach wieder an der Garage und sucht sich neue Arbeit. Ladung und Zustand bleiben.</summary>
        public static bool ResetVeh(VehSave v) => ResetVeh(Cur, v, true);
        public static bool ResetVeh(PState ps, VehSave v, bool toast)
        {
            if (!ps.sv.veh.Contains(v) || v.mode == "boom") return false;
            if (v.lift >= 0 || v.mode == "lifted" || v.mode == "liftwait") return toast && Fail("Das Fahrzeug hängt gerade an einer Lastdrohne");
            if ((v.mode == "queue" || v.mode == "unload") && HasSt(ps, v.sid)) FreeDock(ps.st[v.sid], v);
            if (v.cr >= 0) { var c = ps.sv.crystals.Find(x => x.id == v.cr); if (c != null && c.claim == v.id) c.claim = -1; v.cr = -1; }
            /* nebeneinander an der Werkstatt (sonst am Start), damit sie sich nicht gegenseitig blockieren */
            int k = ps.sv.veh.IndexOf(v); var home = ps.bld.Contains("werkstatt") ? ps.lay.shop : ps.lay.start; var off = new Vector2((k % 5 - 2) * 1.6f, (k / 5) * 1.6f);
            v.d = ps.pd.PlanDir(home + off); v.mode = v.mode == "broken" ? "broken" : "seek"; v.sid = null; v.cell = -1; v.ti = -1; v.busy = false;
            ps.vnav.Remove(v.id); ps.lqnav.Remove(v.id); if (toast) Toast(VehName(v) + " steht wieder an der Basis", "good"); return true;
        }
        /// <summary>Alle Fahrzeuge dieses Planeten zurück an die Werkstatt.</summary>
        public static void ResetAllVeh() { var ps = Cur; int n = 0; foreach (var v in ps.sv.veh) if (ResetVeh(ps, v, false)) n++; if (ps.sv.cart.owned && ps.sv.cart.mode != "wait") { ps.sv.cart.d = ps.pd.PlanDir(ps.lay.start); ps.sv.cart.mode = Sum(ps.sv.cart.tank) > .05f ? "back" : "park"; ps.sv.cart.wt = 0; } Toast(n + " Fahrzeuge stehen wieder an der Basis", "good"); }
        /// <summary>Fabian 01.11.: Sammelwagen lässt sich sprengen (30 % zurück), danach leerst du wieder selbst.</summary>
        public static void DestroyCart()
        {
            var ps = Cur; var c = ps.sv.cart; if (!c.owned) return; double r = Math.Round(CartCost(ps) * BOOM_REFUND); Earn(r);
            OnBoom?.Invoke(c.d); c.owned = false; c.mode = "park"; c.to = ""; Array.Clear(c.tank, 0, c.tank.Length); Toast("Sammelwagen gesprengt · +" + Fmt(r) + " Cr", "good");
        }
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
