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
        public static ICloudSave Cloud => cloud ??= new NoCloud();

        /// <summary>Höchstens alle 2 Minuten hochladen (Akku, Datenvolumen).</summary>
        public static void MaybeUpload(string json, long stamp, bool force = false)
        {
            if (!Cloud.SignedIn) return; if (!force && Time.unscaledTime - lastUp < 120) return; lastUp = Time.unscaledTime; Cloud.Upload(json, stamp);
        }

        /// <summary>Platzhalter-Angebote. Preise sind Beispiele; echte Preise kommen später aus dem Store (Unity IAP).</summary>
        /// <summary>Angebote ohne Geld-Pakete: Komfort, Platz, Zeit und Aussehen – nichts, was man in Minuten selbst erspielt.</summary>
        public static readonly List<ShopOffer> Offers = new List<ShopOffer>
        {
            new ShopOffer { id = "slot", name = "Zusätzlicher Fahrzeugplatz", desc = "+1 Fahrzeug über der Obergrenze – auf jedem Planeten, dauerhaft (bis zu 3-mal)", price = "1,99 €" },
            new ShopOffer { id = "cargo2", name = "Großer Frachtraum", desc = "Jede Rakete fasst dauerhaft 50 % mehr", price = "2,99 €" },
            new ShopOffer { id = "offline24", name = "Langschläfer", desc = "Offline-Zeit dauerhaft auf 24 Stunden", price = "4,99 €" },
            new ShopOffer { id = "timeskip", name = "Zeitsprung 4 h", desc = "Deine Basis arbeitet sofort 4 Stunden (wie offline)", price = "1,99 €", consumable = true },
            new ShopOffer { id = "season", name = "Saisonpass", desc = "Eigene Saisonziele mit Lackierungen, Leuchtfarben und Basis-Themen als Belohnung", price = "4,99 €" },
            new ShopOffer { id = "skin_gold", name = "Goldflotte", desc = "Goldene Lackierung für alle Fahrzeuge und Drohnen", price = "1,99 €" },
            new ShopOffer { id = "theme_neon", name = "Neon-Basis", desc = "Leuchtende Kanten an Gebäuden und Bändern bei Nacht", price = "1,99 €" },
            new ShopOffer { id = "noads", name = "Werbefrei", desc = "Für immer ohne Werbung – Bonus-Belohnungen gibt es trotzdem", price = "3,99 €" },
        };
        /// <summary>Wirkung eines bestätigten Kaufs (wird erst nach echter Store-Bestätigung aufgerufen).</summary>
        public static void Grant(string id)
        {
            var S = Game.S; if (S == null) return;
            if (id == "slot") { if (S.extraSlots < 3) S.extraSlots++; return; }
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
