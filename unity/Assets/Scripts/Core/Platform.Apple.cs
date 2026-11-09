// Fabian 09.11.: App Store vorbereiten – Game Center, Erfolge, Bestenlisten, Spielstand im Konto, „Mit Apple anmelden“.
// NUR VORBEREITET: Alles ist aus, solange das Scripting-Define „LC_STORE“ fehlt. Dann gibt es keine nativen Aufrufe, keine
// zusätzlichen Knöpfe und keine Änderung am Spiel. Einschalten: siehe docs/APPSTORE.md.
//
// Erfolge: jede Stufe (Bronze/Silber/Gold) eines Spiel-Erfolgs ist ein eigener Game-Center-Erfolg, dazu 5 Geschichts-Erfolge.
// IDs: com.fabian.lunarcleanup.ach.<id>.<b|s|g>  bzw.  com.fabian.lunarcleanup.story.<id>
// Bestenlisten: com.fabian.lunarcleanup.lb.<stars|cells|hardlvl|earned>
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_IOS && LC_STORE
using System.Runtime.InteropServices;
#endif

namespace LunarCleanup.Core
{
    public static class AppleIds
    {
        public const string Prefix = "com.fabian.lunarcleanup";
        static readonly string[] Tier = { "b", "s", "g" };
        public static string Ach(string id, int tier) => Prefix + ".ach." + id + "." + Tier[Mathf.Clamp(tier, 0, 2)];
        public static string Story(string id) => Prefix + ".story." + id;
        public static string Board(string id) => Prefix + ".lb." + id;
        /// <summary>Punkte je Stufe (Apple: höchstens 1000 Punkte und 100 Erfolge je Spiel). 30 × (5+10+15) = 900 + 5 × 20 = 1000.</summary>
        public static readonly int[] TierPoints = { 5, 10, 15 };
        public static readonly (string id, string name, string desc)[] StoryAch =
        {
            ("first", "Etwas im Staub", "Das erste verborgene Zeichen auf einem Planeten entdeckt"),
            ("moon", "Die Sternkarte", "Das Geheimnis des Mondes vollständig freigelegt"),
            ("half", "Auf der Spur", "Auf 9 Planeten ein Zeichen freigelegt"),
            ("all", "Die Route der Wanderer", "Auf allen 18 Planeten das Zeichen freigelegt"),
            ("stars", "Sternensammler", "Alle 54 Sterne gesammelt"),
        };
        public static readonly (string id, string name)[] Boards =
        {
            ("stars", "Sterne gesamt"), ("cells", "Parzellen gereinigt"), ("hardlvl", "Putz-Level im Hard Mode"), ("earned", "Credits verdient (in Millionen)"),
        };
    }

    /// <summary>Brücke zu den Apple-Diensten. Ohne LC_STORE: alles still aus.</summary>
    public static class AppleServices
    {
#if UNITY_IOS && LC_STORE
        [DllImport("__Internal")] static extern void LC_GCAuthenticate();
        [DllImport("__Internal")] static extern bool LC_GCIsAuthenticated();
        [DllImport("__Internal")] static extern void LC_GCReportAchievement(string ident, double percent);
        [DllImport("__Internal")] static extern void LC_GCShowDashboard(int state);
        [DllImport("__Internal")] static extern void LC_GCReportScore(string board, long value);
        [DllImport("__Internal")] static extern void LC_GCSaveGame(string data);
        [DllImport("__Internal")] static extern void LC_GCLoadGame();
        [DllImport("__Internal")] static extern void LC_AppleSignIn();
        [DllImport("__Internal")] static extern void LC_AppleCheckState(string user);
        public const bool Enabled = true;
#else
        public const bool Enabled = false;
#endif
        public static bool SignedIn { get; internal set; }
        public static string PlayerName { get; internal set; } = "";
        static readonly HashSet<string> reported = new HashSet<string>();
        public static event Action<bool> OnAuthChanged;
        internal static Action<string> loadCb;

        public static void Authenticate()
        {
            if (!Enabled) return; AppleBridge.Ensure();
#if UNITY_IOS && LC_STORE
            LC_GCAuthenticate();
#endif
        }
        internal static void AuthResult(bool ok, string name) { SignedIn = ok; PlayerName = name ?? ""; OnAuthChanged?.Invoke(ok); if (ok) SyncAll(); }

        public static void ReportAchievement(string ident)
        {
            if (!Enabled || !SignedIn || reported.Contains(ident)) return; reported.Add(ident);
#if UNITY_IOS && LC_STORE
            LC_GCReportAchievement(ident, 100);
#endif
        }
        public static void ReportScore(string board, long value)
        {
            if (!Enabled || !SignedIn) return;
#if UNITY_IOS && LC_STORE
            LC_GCReportScore(AppleIds.Board(board), value);
#endif
        }
        public static void ShowDashboard(int state)
        {
            if (!Enabled) return;
#if UNITY_IOS && LC_STORE
            LC_GCShowDashboard(state);
#endif
        }
        public static void SaveToAccount(string data)
        {
            if (!Enabled || !SignedIn) return;
#if UNITY_IOS && LC_STORE
            LC_GCSaveGame(data);
#endif
        }
        public static void LoadFromAccount(Action<string> done)
        {
            if (!Enabled || !SignedIn) { done?.Invoke(null); return; } loadCb = done;
#if UNITY_IOS && LC_STORE
            LC_GCLoadGame();
#endif
        }
        public static void SignInWithApple()
        {
            if (!Enabled) return; AppleBridge.Ensure();
#if UNITY_IOS && LC_STORE
            LC_AppleSignIn();
#endif
        }
        public static void CheckAppleId()
        {
            if (!Enabled || string.IsNullOrEmpty(PlayerPrefs.GetString("LunarCleanup.AppleUser", ""))) return; AppleBridge.Ensure();
#if UNITY_IOS && LC_STORE
            LC_AppleCheckState(PlayerPrefs.GetString("LunarCleanup.AppleUser", ""));
#endif
        }

        /// <summary>Nach der Anmeldung: alle schon erreichten Stufen und Bestwerte nachmelden.</summary>
        public static void SyncAll()
        {
            if (!Enabled || !SignedIn || Game.S == null) return;
            foreach (var a in Game.Achievements) for (int t = 0; t < Game.AchTier(a); t++) ReportAchievement(AppleIds.Ach(a.id, t));
            Game.StoryAchCheck(); ReportScores();
        }
        public static void ReportScores()
        {
            if (!Enabled || !SignedIn || Game.S == null) return;
            if (Game.Hard) { ReportScore("hardlvl", Game.HardLevel); return; }
            ReportScore("stars", Game.P.Values.Sum(p => Game.StarCount(p)));
            ReportScore("cells", (long)Game.P.Values.Sum(p => p.nClean));
            ReportScore("earned", (long)(Game.S.stats.earned / 1e6));
        }
    }

    /// <summary>Empfängt die Antworten der nativen Seite (UnitySendMessage an „LCApple“).</summary>
    public class AppleBridge : MonoBehaviour
    {
        static AppleBridge inst;
        public static void Ensure() { if (inst) return; var go = new GameObject("LCApple"); DontDestroyOnLoad(go); inst = go.AddComponent<AppleBridge>(); }
        void OnAuth(string msg) { var p = msg.Split('|'); AppleServices.AuthResult(p[0] == "1", p.Length > 1 ? p[1] : ""); }
        void OnSaved(string msg) { if (!msg.StartsWith("1")) Debug.LogWarning("Lunar Cleanup: Konto-Speichern fehlgeschlagen " + msg); }
        void OnLoaded(string data) { var cb = AppleServices.loadCb; AppleServices.loadCb = null; cb?.Invoke(string.IsNullOrEmpty(data) ? null : data); }
        void OnAppleId(string msg)
        {
            var p = msg.Split('|');
            if (p[0] == "1" && p.Length > 1) { PlayerPrefs.SetString("LunarCleanup.AppleUser", p[1]); PlayerPrefs.Save(); Game.Toast("Mit Apple angemeldet" + (p.Length > 2 && p[2] != "" ? " – hallo " + p[2] + "!" : ""), "good"); }
            else Game.Toast("Anmeldung mit Apple hat nicht geklappt", "bad");
        }
        void OnAppleState(string msg) { if (msg != "1") { PlayerPrefs.DeleteKey("LunarCleanup.AppleUser"); PlayerPrefs.Save(); } }
    }

    /// <summary>Konto-Speicher über Game Center (GKSavedGame in iCloud). Gleiche Regel wie überall: nur übernehmen, wenn neuer; lokal bleibt die Sicherung.</summary>
    public class GameCenterCloud : ICloudSave
    {
        public string Name => "Game Center";
        public bool SignedIn => AppleServices.SignedIn;
        public void SignIn(Action<bool> done) { Action<bool> h = null; h = ok => { AppleServices.OnAuthChanged -= h; done?.Invoke(ok); }; AppleServices.OnAuthChanged += h; AppleServices.Authenticate(); }
        public void Upload(string json, long stamp) => AppleServices.SaveToAccount(stamp + "\n" + json);
        public void Download(Action<string, long> done) => AppleServices.LoadFromAccount(s =>
        {
            if (string.IsNullOrEmpty(s)) { done?.Invoke(null, 0); return; }
            int nl = s.IndexOf('\n'); long st = 0; if (nl > 0) long.TryParse(s.Substring(0, nl), out st);
            done?.Invoke(nl > 0 ? s.Substring(nl + 1) : s, st);
        });
    }
}
