// Plattform-Dienste: Konto-Speicherstand (Google Play Spiele auf Android, Game Center/iCloud auf iOS) und In-App-Käufe.
// Stand jetzt: Schnittstellen und Platzhalter. Die echten SDKs (Google Play Games Plugin, Apple GameKit, Unity IAP)
// werden beim Einrichten der Handy-Builds eingebunden – dann nur noch die markierten Stellen füllen.
// Regel: Ein Cloud-Stand wird nur übernommen, wenn er neuer ist; der lokale Stand bleibt immer als Sicherung erhalten.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    public interface ICloudSave
    {
        string Name { get; }          // „Google Play Spiele“, „Game Center“ …
        bool SignedIn { get; }
        void SignIn(Action<bool> done);
        void Upload(string json, long stamp);
        void Download(Action<string, long> done);   // json (oder null) + Zeitstempel
    }

    /// <summary>Im Editor und solange kein SDK eingebunden ist: nichts verbunden.</summary>
    public class NoCloud : ICloudSave
    {
        public string Name =>
#if UNITY_ANDROID
            "Google Play Spiele";
#elif UNITY_IOS
            "Game Center";
#else
            "Konto-Speicher";
#endif
        public bool SignedIn => false;
        public void SignIn(Action<bool> done) { done?.Invoke(false); }
        public void Upload(string json, long stamp) { }
        public void Download(Action<string, long> done) { done?.Invoke(null, 0); }
    }

    // TODO Android: class PlayGamesCloud : ICloudSave – PlayGamesPlatform.Activate(), SavedGame-API (OpenWithAutomaticConflictResolution, Snapshot "lunar-cleanup").
    // TODO iOS:     class GameCenterCloud : ICloudSave – GKLocalPlayer.Authenticate, GKSavedGame (fetchSavedGames / saveGameData).

    public class ShopOffer { public string id, name, desc, price; public bool consumable; }

    public static class Platform
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { cloud = null; lastUp = 0; }
        static ICloudSave cloud; static float lastUp;
        public static ICloudSave Cloud => cloud ??= AppleServices.Enabled ? new GameCenterCloud() : (ICloudSave)new NoCloud();
        /// <summary>09.11.: Beim Start bei Game Center anmelden (nur mit LC_STORE).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void StoreInit() { if (!AppleServices.Enabled) return; AppleServices.Authenticate(); AppleServices.CheckAppleId(); }

        /// <summary>Höchstens alle 2 Minuten hochladen (Akku, Datenvolumen).</summary>
        public static void MaybeUpload(string json, long stamp, bool force = false)
        {
            if (!Cloud.SignedIn) return; if (!force && Time.unscaledTime - lastUp < 120) return; lastUp = Time.unscaledTime; Cloud.Upload(json, stamp); AppleServices.ReportScores();
        }

        /// <summary>Fabian 03.11.: Angebote, die zum Spiel passen – Komfort, Zeit und Aussehen. Keine zusätzlichen Fahrzeuge, keine Geld-Pakete.
        /// Fabian 11.11.: Keine Werbung im Spiel. Meteor-Versicherung entfernt. Unterstützer-Paket ohne Spielvorteil (nur Aussehen + Dank).
        /// Kaffee-Spende ohne Gegenleistung. Dauerhafte Käufe gelten fürs ganze Gerät – also auch im Hard Mode (eigener Spielstand).
        /// Preise sind Beispiele; echte Preise kommen später aus dem Store (Unity IAP).</summary>
        public static readonly List<ShopOffer> Offers = new List<ShopOffer>
        {
            new ShopOffer { id = "offline24", name = "Langschläfer", desc = "Deine Basis arbeitet offline bis zu 24 Stunden statt 8 – dauerhaft, auch im Hard Mode", price = "3,99 €" },
            new ShopOffer { id = "timeskip", name = "Zeitsprung 4 h", desc = "Alle Planeten arbeiten sofort 4 Stunden weiter (wie offline)", price = "0,99 €", consumable = true },
            new ShopOffer { id = "cargo2", name = "Großer Frachtraum", desc = "Jede Rakete fasst dauerhaft 50 % mehr – auch im Hard Mode", price = "2,99 €" },
            new ShopOffer { id = "xlic", name = "Export-Lizenz", desc = "Der Export-Frachter zahlt 60 % statt 40 % des Werts – auf allen Planeten, auch im Hard Mode", price = "1,99 €" },
            new ShopOffer { id = "sd50", name = "Sternenstaub-Beutel", desc = "+50 Sternenstaub für Forschung und Sonderwünsche", price = "0,99 €", consumable = true },
            new ShopOffer { id = "skin_gold", name = "Goldene Flotte", desc = "Goldene Lackierung für alle Fahrzeuge (nur Aussehen)", price = "1,50 €" },
            new ShopOffer { id = "hardmode", name = "Hard Mode sofort", desc = "Schaltet den Hard Mode sofort frei, ohne erst alle 18 Planeten zu säubern: eigener Spielstand, du putzt selbst, Perks zum Finden, Anzug-Energie", price = "3,99 €" },
            new ShopOffer { id = "supporter", name = "Unterstützer-Paket", desc = "Goldene Flotte, goldene Anzüge für deine Crew, goldene Fahnenmasten und dein Unterstützer-Abzeichen – kein Spielvorteil, nur Danke", price = "4,99 €" },
            new ShopOffer { id = "coffee", name = "Kaffee für den Entwickler", desc = "Einfach nur nett sein: 1,50 € Spende, keine Gegenleistung – danke!", price = "1,50 €", consumable = true },
        };
        const string AcctKey = "LunarCleanup.Owned"; static HashSet<string> acct;
        /// <summary>Zählt jeden neuen Kauf mit – die Welt baut Fahrzeuge/Crew neu, wenn sich das Aussehen ändert.</summary>
        public static int Version;
        static HashSet<string> Acct { get { if (acct == null) { acct = new HashSet<string>(); foreach (var x in PlayerPrefs.GetString(AcctKey, "").Split(',')) if (x != "") acct.Add(x); } return acct; } }
        static void AcctAdd(string id) { if (Acct.Add(id)) { PlayerPrefs.SetString(AcctKey, string.Join(",", acct)); PlayerPrefs.Save(); } Version++; }
        /// <summary>Gekauft? Gerätweit (PlayerPrefs) oder im Spielstand (ältere Käufe werden dabei gerätweit übernommen).
        /// Das Unterstützer-Paket enthält die Goldene Flotte.</summary>
        public static bool Owned(string id)
        {
            if (Acct.Contains(id)) return true;
            if (id == "hardmode" && Game.HardOpen) return true;
            if (id == "skin_gold" && Acct.Contains("supporter")) return true;
            if (Game.S != null && Game.S.owned != null && Game.S.owned.Contains(id)) { AcctAdd(id); return true; }
            return false;
        }
        public static bool Supporter => Owned("supporter");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetAcct() { acct = null; Version = 0; }
        /// <summary>Wirkung eines bestätigten Kaufs (wird erst nach echter Store-Bestätigung aufgerufen).</summary>
        public static void Grant(string id)
        {
            var S = Game.S;
            switch (id)
            {
                case "timeskip": Game.TimeSkip(4 * 3600); return;
                case "sd50": if (S != null) S.stardust += 50; return;
                case "coffee": Game.Toast("Danke für den Kaffee!", "good"); PlayerPrefs.SetInt("LunarCleanup.Coffee", PlayerPrefs.GetInt("LunarCleanup.Coffee", 0) + 1); PlayerPrefs.Save(); return;
                case "hardmode": AcctAdd(id); Game.UnlockHard(); return;
                case "supporter": AcctAdd(id); AcctAdd("skin_gold"); Game.Toast("Danke für deine Unterstützung!", "good"); return;
            }
            AcctAdd(id); if (S != null && !S.owned.Contains(id)) S.owned.Add(id);
        }
        public static int Coffees => PlayerPrefs.GetInt("LunarCleanup.Coffee", 0);

        /// <summary>Kauf auslösen. Bis Unity IAP eingebunden ist, wird nichts berechnet und nichts vergeben.</summary>
        public static void Buy(ShopOffer o, Action<bool> done)
        {
            // TODO: UnityEngine.Purchasing – StoreController.PurchaseProduct(o.id); Belohnung erst nach bestätigtem Kauf (ProcessPurchase) vergeben.
            done?.Invoke(false);
        }
    }
}
