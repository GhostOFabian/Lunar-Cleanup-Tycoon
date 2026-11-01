// Flotte: feste Obergrenze je Planet und Selbstzerstörung (um kleine Fahrzeuge gegen größere zu tauschen).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const int VEH_MAX = 15, VEH_CAP = 24, HALL_MAX = 9; // Fabian 01.11.: Flotte 15 je Planet, mit der Fahrzeughalle (+1 je Stufe, 9 Stufen) bis 24
        const float BOOM_TIME = 4f, BOOM_REFUND = .3f;
        public static int HallSlots(PState ps = null) => Math.Min(HALL_MAX, (int)Uv("slots", ps));
        public static int VehMax(PState ps) => Math.Min(VEH_CAP, VEH_MAX + HallSlots(ps) + (S != null ? S.fleetPlus + S.extraSlots : 0));
        /// <summary>Fabian 01.11.: voll ausgebaute Fahrzeuge waren zu schnell – Fahrwerk höchstens ×2, Bürsten höchstens ×4,6 (Kosten bleiben).</summary>
        /// <summary>Farben für die Ränder sauberer Parzellen (Einstellungen).</summary>
        public static readonly (string name, Color col)[] LineColors = { ("Grün", new Color(.1f, 1f, .3f)), ("Türkis", new Color(0f, 1f, .9f)), ("Blau", new Color(.1f, .45f, 1f)), ("Violett", new Color(.62f, .2f, 1f)), ("Pink", new Color(1f, .12f, .7f)), ("Rot", new Color(1f, .1f, .08f)), ("Orange", new Color(1f, .48f, 0f)), ("Gold", new Color(1f, .82f, 0f)), ("Weiß", new Color(1f, 1f, 1f)), ("Aus", new Color(0, 0, 0, 0)) }; /* Fabian 01.11.: knalliger */
        public static Color LineColor => S == null ? LineColors[0].col : LineColors[Mathf.Clamp(S.lineCol, 0, LineColors.Length - 1)].col;
        public static void NextLineColor() { S.lineCol = (S.lineCol + 1) % LineColors.Length; foreach (var p in P.Values) for (int i = 0; i < p.pd.N; i++) if (p.clean[i]) p.dirtyCells.Add(i); }
        /// <summary>Fabian 01.11.: Gebäude-Upgrades haben mehr Stufen, damit sich gelbe und rote Ampeln immer beheben lassen. Die alten Höchststufen (max0) bleiben die Hürde für die Gebäudestufen.</summary>
        static void ExtendBuildingUpgrades()
        {
            var more = new Dictionary<string, int> { { "bspeed", 20 }, { "bcap", 16 }, { "mspeed", 20 }, { "mcap", 16 }, { "urate", 16 }, { "ubuf", 16 }, { "rohcap", 22 }, { "prodcap", 20 }, { "sellcap", 22 }, { "rspeed", 20 }, { "energy", 18 }, { "ecap", 18 }, { "rcare", 16 }, { "obuf", 20 }, { "pipe", 20 }, { "vdur", 18 }, { "pvol", 16 } };
            foreach (var u in GameData.Upg) u.max0 = u.max;
            foreach (var kv in more)
            {
                if (!GameData.Up.TryGetValue(kv.Key, out var u) || u.vals == null) continue;
                if (u.vals.Length <= kv.Value) { var nv = new double[kv.Value + 1]; for (int l = 0; l < nv.Length; l++) nv[l] = l < u.vals.Length ? u.vals[l] : u.vals[u.vals.Length - 1] + (u.vals[u.vals.Length - 1] - u.vals[u.vals.Length - 2]) * (l - u.vals.Length + 1); u.vals = nv; }
                u.max = Math.Max(u.max, kv.Value);
                if (u.show == null || u.show.Length < u.vals.Length) { var de = new System.Globalization.CultureInfo("de-DE"); var ns = new string[u.vals.Length]; for (int l = 0; l < ns.Length; l++) ns[l] = u.show != null && l < u.show.Length ? u.show[l] : (u.vals[l] >= 100 ? Math.Round(u.vals[l]).ToString("N0", de) : "×" + u.vals[l].ToString("0.00", de)); u.show = ns; }
            }
            /* Fahrzeughalle: nur noch 9 Stufen zu je +1 Platz (15 → 24); alte Stände mit höheren Stufen bekommen den Rest erstattet (ProcessPrizes) */
            if (GameData.Up.TryGetValue("slots", out var h))
            {
                h.max = HALL_MAX; h.max0 = HALL_MAX; h.label = "Fahrzeugplätze je Planet über 15";
                var v = new double[HALL_MAX + 1]; var sh = new string[HALL_MAX + 1]; for (int l = 0; l <= HALL_MAX; l++) { v[l] = l; sh[l] = (VEH_MAX + l) + " Fahrzeuge"; } h.vals = v; h.show = sh;
            }
        }
        static void DampVehicleUpgrades()
        {
            ExtendBuildingUpgrades();
            var de = new System.Globalization.CultureInfo("de-DE");
            void D(string id, double step) { if (!GameData.Up.TryGetValue(id, out var u)) return; for (int l = 0; l < u.vals.Length; l++) u.vals[l] = 1 + step * l; if (u.show != null) for (int l = 0; l < u.show.Length && l < u.vals.Length; l++) u.show[l] = "×" + u.vals[l].ToString("0.00", de); }
            D("vspeed", .05); D("vpower", .12);
            if (GameData.Up.TryGetValue("cartspd", out var cs0)) cs0.hide = true; /* Sammelwagen abgeschafft */
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
