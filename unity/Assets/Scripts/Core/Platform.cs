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

        /// <summary>Fabian 03.11.: Angebote, die zum Spiel passen – Komfort, Zeit, Schutz und Aussehen. Keine zusätzlichen Fahrzeuge, keine Geld-Pakete.
        /// Preise sind Beispiele; echte Preise kommen später aus dem Store (Unity IAP).</summary>
        public static readonly List<ShopOffer> Offers = new List<ShopOffer>
        {
            new ShopOffer { id = "offline24", name = "Langschläfer", desc = "Deine Basis arbeitet offline bis zu 24 Stunden statt 8 – dauerhaft", price = "3,99 €" },
            new ShopOffer { id = "timeskip", name = "Zeitsprung 4 h", desc = "Alle Planeten arbeiten sofort 4 Stunden weiter (wie offline)", price = "0,99 €", consumable = true },
            new ShopOffer { id = "cargo2", name = "Großer Frachtraum", desc = "Jede Rakete fasst dauerhaft 50 % mehr – weniger Wartezeit bis zum Start", price = "2,99 €" },
            new ShopOffer { id = "xlic", name = "Export-Lizenz", desc = "Der Export-Frachter zahlt 60 % statt 40 % des Werts – auf allen Planeten", price = "1,99 €" },
            new ShopOffer { id = "insure", name = "Meteor-Versicherung", desc = "Kommt ein Riesenmeteor durch, ist der Schaden nur halb so groß und die Lager bleiben voll", price = "2,99 €" },
            new ShopOffer { id = "sd50", name = "Sternenstaub-Beutel", desc = "+50 Sternenstaub für Forschung und Sonderwünsche", price = "0,99 €", consumable = true },
            new ShopOffer { id = "skin_gold", name = "Goldflotte", desc = "Goldene Lackierung für alle Fahrzeuge (nur Aussehen)", price = "1,99 €" },
            new ShopOffer { id = "hardmode", name = "Hard Mode sofort", desc = "Schaltet den Hard Mode gleich frei, ohne erst alle 18 Planeten zu säubern: eigener Spielstand, du putzt selbst, Perks zum Finden, Anzug-Energie", price = "2,99 €" },
            new ShopOffer { id = "supporter", name = "Unterstützer-Paket", desc = "Langschläfer, Großer Frachtraum, Meteor-Versicherung, Goldflotte und 100 Sternenstaub – du unterstützt die Entwicklung", price = "7,99 €" },
        };
        public static bool Owned(string id) => Game.S != null && (Game.S.owned.Contains(id) || (id == "hardmode" && Game.HardOpen));
        /// <summary>Wirkung eines bestätigten Kaufs (wird erst nach echter Store-Bestätigung aufgerufen).</summary>
        public static void Grant(string id)
        {
            var S = Game.S; if (S == null) return;
            switch (id)
            {
                case "timeskip": Game.TimeSkip(4 * 3600); return;
                case "sd50": S.stardust += 50; return;
                case "hardmode": if (!S.owned.Contains(id)) S.owned.Add(id); Game.UnlockHard(); return;
                case "supporter": foreach (var x in new[] { "offline24", "cargo2", "insure", "skin_gold" }) if (!S.owned.Contains(x)) S.owned.Add(x); S.stardust += 100; return;
            }
            if (!S.owned.Contains(id)) S.owned.Add(id);
        }

        /// <summary>Kauf auslösen. Bis Unity IAP eingebunden ist, wird nichts berechnet und nichts vergeben.</summary>
        public static void Buy(ShopOffer o, Action<bool> done)
        {
            // TODO: UnityEngine.Purchasing – StoreController.PurchaseProduct(o.id); Belohnung erst nach bestätigtem Kauf (ProcessPurchase) vergeben.
            done?.Invoke(false);
        }
    }
}
