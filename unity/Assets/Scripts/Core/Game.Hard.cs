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
        public const double HARD_PRICE = 3;
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
            Icp = null; Find = null; restN = -1;
            S = Load() ?? Fresh(); if (Hard) S.hard = true;
            GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
            P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); if (!P.ContainsKey(S.planet)) S.planet = "moon";
            Save(); OnPlanetChanged?.Invoke();
            Toast(on ? "Hard Mode: du putzt selbst – dein Sammelwagen bringt die Ladung zur Basis, alles bringt dreimal so viel" : "Zurück im normalen Spiel", "good");
        }
        /// <summary>Hard Mode: jeder Planet hat seinen Sammelwagen (kostenlos).</summary>
        static void HardCart(PState ps) { if (!Hard) return; var c = ps.sv.cart; if (!c.owned) { c.owned = true; c.d = ps.pd.PlanDir(ps.lay.start); c.mode = "park"; } }
        /// <summary>Hard Mode: Behälter leeren = in den Sammelwagen (er kommt zu dir) – sonst direkt ins Lager wie im normalen Spiel.</summary>
        static bool HardUnload(PState ps)
        {
            if (!Hard) return false; var c = ps.sv.cart; HardCart(ps);
            if (c.mode == "wait" && ps.pd.Arc(c.d, CartGoal) < 9f) { LoadCart(ps); return true; }
            if (c.mode == "back" || c.mode == "drop") { Toast("Der Sammelwagen bringt gerade eine Ladung weg – gleich ist er wieder da", ""); return true; }
            c.mode = "go"; Toast("Der Sammelwagen kommt zu dir – tippe gleich noch einmal auf den Behälter", ""); return true;
        }
    }
}
