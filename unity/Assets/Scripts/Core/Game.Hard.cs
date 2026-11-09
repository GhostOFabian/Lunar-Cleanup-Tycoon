// Fabian 03.11.: Hard Mode – ein eigener Spielstand, freigeschaltet, wenn im normalen Spiel alle Planeten komplett gesäubert sind (oder per Shop).
// Keine Putzfahrzeuge, keine Drohnen, keine Bohrtürme: du putzt alles selbst. Je Planet fährt ein Sammelwagen deine volle Hand-Ladung zur Basis.
// Upgrades nur für Handgerät, Sammelwagen, Stationen, Bänder, Maschinen, Lager, Basis und Verkauf. Dafür bringt alles dreimal so viel Geld.
// Normaler und Hard-Spielstand liegen getrennt nebeneinander; Wechseln in den Einstellungen.
using System;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static bool Hard;
        public const double HARD_PRICE = 1; /* 04.11.: war ×3 – zu viel Geld im Hard Mode */
        const string HardKey = "LunarCleanup.Hard", HardOpenKey = "LunarCleanup.HardOpen";
        static readonly string[] HardNoCat = { "Fahrzeuge", "Drohnen", "Tiefenabbau", "Strahlenschutz", "Außenlager" };
        static readonly string[] HardNoBld = { "werkstatt", "hangar", "lifter" };
        /// <summary>Beim Start: zuletzt gespielter Modus.</summary>
        public static void InitHard() { Hard = PlayerPrefs.GetInt(HardKey, 0) == 1 && PlayerPrefs.GetInt(HardOpenKey, 0) == 1; }
        /// <summary>Fabian 03.11.: frei, wenn im normalen Spiel alle Planeten komplett gesäubert sind (oder per Shop). Einmal frei bleibt frei.</summary>
        public static bool HardOpen { get { if (PlayerPrefs.GetInt(HardOpenKey, 0) == 1) return true; if (!Hard && S != null && HardDoneN >= GameData.POrder.Length) { PlayerPrefs.SetInt(HardOpenKey, 1); PlayerPrefs.Save(); return true; } return false; } }
        /// <summary>Wie viele Planeten im normalen Spiel schon komplett sauber sind.</summary>
        public static int HardDoneN => Hard || S == null ? GameData.POrder.Length : GameData.POrder.Count(p => S.done.Contains(p));
        /// <summary>Shop „Hard Mode sofort“: freischalten, ohne erst alle Planeten zu haben (gilt für beide Spielstände, im Gerät gemerkt).</summary>
        public static void UnlockHard() { PlayerPrefs.SetInt(HardOpenKey, 1); PlayerPrefs.Save(); Toast("Hard Mode freigeschaltet – Einstellungen → Spielmodus", "good"); }
        public static string HardWhy => HardOpen ? "" : "Säubere im normalen Spiel alle " + GameData.POrder.Length + " Planeten komplett (" + HardDoneN + "/" + GameData.POrder.Length + ") – oder schalte ihn im Shop frei („Hard Mode sofort“)";
        public static bool HardUpgOk(UpgDef u) => !Hard || (Array.IndexOf(HardNoCat, u.cat) < 0 && u.id != "slots");
        public static bool HardBldOk(string id) => !Hard || Array.IndexOf(HardNoBld, id) < 0;
        /// <summary>Spielstand wechseln: der aktuelle wird gespeichert, der andere geladen (oder neu angelegt).</summary>
        public static void SwitchHard(bool on)
        {
            if (on == Hard) return; if (on && !HardOpen) { Fail(HardWhy); return; }
            Save(); Hard = on; PlayerPrefs.SetInt(HardKey, on ? 1 : 0); PlayerPrefs.Save();
            Icp = null; Find = null; restN = -1; ResetStats();
            S = Load() ?? Fresh(); if (Hard) S.hard = true;
            GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
            P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); if (!P.ContainsKey(S.planet)) S.planet = "moon"; MigrateWallet(); ResetStats();
            Save(); OnPlanetChanged?.Invoke();
            Toast(on ? "Hard Mode: du putzt selbst – dein Sammelshuttle bringt die Ladung zur Basis, Perks findest du beim Wischen" : "Zurück im normalen Spiel", "good");
        }
        /// <summary>Fabian 03.11.: im Hard Mode fliegt ein Shuttle statt des Sammelwagens.</summary>
        /// <summary>Fabian 03.11.: im Hard Mode verdienst du weniger nebenbei – Reisen zum nächsten Planeten kostet nur 40 %.</summary>
        /// <summary>Fabian 04.11.: im Hard Mode bringen Ziele, Erfolge, Aufträge kaum Geld (10 %) – man soll nicht durch Abholen die Basis ausbauen.</summary>
        public static double GoalF => Hard ? .1 : 1;
        public static double UnlockCost(string pid) => S != null && S.freeUnlock.Contains(pid) ? 0 : Math.Round(GameData.Planets[pid].unlockCost * (Hard ? .4 : 1)); /* 09.11.: fertiges Puzzle = Koordinaten = kostenlose Reise */
        public static string CartName => Hard ? "Sammelshuttle" : "Sammelwagen";
        /// <summary>Sammelshuttle fliegt geradeaus über alles hinweg (1,4× schneller), der Sammelwagen fährt über die Wege.</summary>
        static bool CartMove(PState ps, CartSave c, Vector3 to, float step, int x)
        {
            if (!Hard) return NavMove(ps, c, to, step, x);
            float rad = step * 1.4f * TalShuttle * ClShuttle / ps.pd.R, ang = Vector3.Angle(c.d, to) * Mathf.Deg2Rad; if (ang <= rad || ang < 1e-4f) { c.d = to.normalized; return true; }
            c.d = Vector3.Slerp(c.d.normalized, to.normalized, rad / ang).normalized; return false;
        }
        static void HardDepot(PState ps) { if (ps.sv.depot == null || ps.sv.depot.Length != NM) { var o = ps.sv.depot; ps.sv.depot = new float[NM]; if (o != null) Array.Copy(o, ps.sv.depot, Math.Min(o.Length, NM)); } }
        public static float DepotSum(PState ps) => ps.sv.depot == null ? 0 : Sum(ps.sv.depot);
        static readonly System.Collections.Generic.Dictionary<string, (int v, Vector2 p)> depotPos = new System.Collections.Generic.Dictionary<string, (int, Vector2)>();
        /// <summary>Platz des Zwischenlagers: freie Basisfläche nahe dem Rohstofflager, mit Abstand zu allen Gebäuden.</summary>
        public static Vector2 DepotPos(PState ps)
        {
            if (depotPos.TryGetValue(ps.pid, out var cp) && cp.v == ps.baseVersion) return cp.p;
            var pd = ps.pd; var roh = ps.lay.pos.ContainsKey("roh") ? ps.lay.pos["roh"] : Vector2.zero; var hq = ps.lay.pos.ContainsKey("hq") ? ps.lay.pos["hq"] : Vector2.zero; var away = (roh - hq).sqrMagnitude > .01f ? (roh - hq).normalized : Vector2.left;
            Vector2 best = roh + away * 10; float bs = float.MaxValue;
            for (float r = 6; r <= 40; r += 2) for (int k = 0; k < 24; k++)
            {
                float a = k * Mathf.PI / 12; var q = roh + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; int cl = pd.CellOf(pd.PlanDir(q)); if (cl < 0 || !pd.cells[cl].IsBase) continue;
                bool ok = true; foreach (var kv in ps.lay.pos) { if (!GameData.Bld.ContainsKey(kv.Key)) continue; if ((kv.Value - q).magnitude < GameData.Bld[kv.Key].fp + 5f) { ok = false; break; } }
                if (!ok) continue; float sc = r - Vector2.Dot(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), away) * 4; if (sc < bs) { bs = sc; best = q; }
            }
            depotPos[ps.pid] = (ps.baseVersion, best); return best;
        }
        /// <summary>Zwischenlager gibt nach und nach ans Rohstofflager ab, sobald dort Platz ist (Simulation, live und offline).</summary>
        static void DepotStep(PState ps, float dt)
        {
            if (ps.sv.depot == null) return; float tot = Sum(ps.sv.depot); if (tot < .01f) return; float free = RohCap(ps) - RohSum(ps); if (free <= .01f) return;
            float amt = Mathf.Min(tot, free, 20 * dt), k = amt / tot; for (int m = 0; m < NM; m++) { float x = ps.sv.depot[m] * k; ps.sv.depot[m] -= x; { ps.sv.roh[m] += x; RohIn(ps, m, x); } }
        }
        /// <summary>Hard Mode: jeder Planet hat seinen Sammelwagen (kostenlos).</summary>
        static void HardCart(PState ps) { if (!Hard) return; var c = ps.sv.cart; if (!c.owned) { c.owned = true; c.d = ps.pd.PlanDir(ps.lay.start); c.mode = "park"; } }
        /// <summary>Hard Mode: Behälter leeren = in den Sammelwagen (er kommt zu dir) – sonst direkt ins Lager wie im normalen Spiel.</summary>
        static bool HardUnload(PState ps)
        {
            if (!Hard) return false; var c = ps.sv.cart; HardCart(ps);
            if (c.mode == "wait" && ps.pd.Arc(c.d, CartGoal) < 9f) { LoadCart(ps); return true; }
            if (c.mode == "back" || c.mode == "drop") { Toast(CartName + " bringt gerade eine Ladung weg – gleich ist es wieder da", ""); return true; }
            c.mode = "go"; Toast(CartName + " kommt zu dir – tippe gleich noch einmal auf den Behälter", ""); return true;
        }
    }
}
