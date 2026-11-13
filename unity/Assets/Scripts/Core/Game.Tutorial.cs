// Fabian 12.11.: Tutorial in einem eigenen Übungsbereich (eigener Spielstand „-tut“ auf dem Mond).
// Nur 20 Parzellen rund um die Basis sind putzbar. Schritt für Schritt: wischen, entladen, sauber machen, Rakete, Upgrade,
// Basis Stufe 2, Buggy, Produktion/Ampel, Belohnung abholen, Planetenkarte, Rest putzen. Zum Schluss schlägt ein Komet ein,
// danach startet das echte Spiel. Beim ersten Mal Pflicht, danach überspringbar (Einstellungen).
// Neue Spieler: Die ersten 7 Hauptziele (bringt das Tutorial bei) gelten als erledigt, ihre Belohnungen gibt es als Startgeld.
// Ereignisse, Meteoritenschauer, Truhen, Geheimnis, Besucher, Funde, Bonus-Aufträge und Erfolge ruhen im Tutorial.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static bool Tut;
        const string TutKey = "LunarCleanup.Tut", TutDoneKey = "LunarCleanup.TutDone", TutHardKey = "LunarCleanup.TutHard";
        public const int TUT_CELLS = 20, TUT_SKIP_QUESTS = 7;
        public static bool TutDoneOnce => PlayerPrefs.GetInt(TutDoneKey, 0) == 1;
        public class TutStep { public string text, why, sheet; public Func<PState, bool> done; public Func<PState, float> prog; }
        public static readonly TutStep[] TutSteps = {
            new TutStep { text = "Wische mit dem Finger über die leuchtende Parzelle", why = "Jede Parzelle ist voller Staub und Schrott. Wischen sammelt ihn in deinen Behälter.", done = p => QuestProgress(0) >= 1, prog = p => QuestProgress(0) },
            new TutStep { text = "Leere deinen Behälter: Tippe unten links auf den Behälter", why = "Was du sammelst, landet im Rohstofflager der Basis.", done = p => S.stats.handUnloads > 0 },
            new TutStep { text = "Mach 3 Parzellen ganz sauber", why = "Saubere Parzellen bleiben für immer sauber. Ihre Nachbarn kannst du sofort putzen.", done = p => TutClean(p) >= 3, prog = p => TutClean(p) / 3f },
            new TutStep { text = "Starte deine erste Rakete, sobald der Frachtraum voll ist", why = "Förderbänder bringen deine Rohstoffe in den Frachtraum der Rakete. Jede Rakete bringt Credits.", done = p => p.sv.nRockets > 0, prog = p => Mathf.Clamp01(Sum(p.sv.sellBuf) / Mathf.Max(1, SellTarget(p))) },
            new TutStep { text = "Kaufe ein Upgrade für dein Handgerät", why = "Upgrades machen dich schneller. Du findest sie unten unter „Upgrades“.", sheet = "upgrades", done = p => QuestProgress(5) >= 1 },
            new TutStep { text = "Baue die Basis auf Stufe 2 aus", why = "Höhere Basisstufen schalten neue Gebäude und Fahrzeuge frei. Unten unter „Basis“.", sheet = "base", done = p => p.sv.stage >= 2 },
            new TutStep { text = "Kaufe einen Reinigungsbuggy", why = "Fahrzeuge putzen von selbst – auch wenn du das Spiel schließt. Unten unter „Fahrzeuge“.", sheet = "vehicles", done = p => p.sv.veh.Count > 0 },
            new TutStep { text = "Öffne die Produktion und sieh dir die Ampel an", why = "Grün heißt: alles läuft. Bei Gelb oder Rot steht dabei, was hakt und welches Upgrade hilft.", sheet = "production", done = p => S.tutSeen.Contains("production") },
            new TutStep { text = "Öffne die Ziele oben rechts und hole eine Belohnung ab", why = "Erledigte Ziele und Aufträge stehen in den Fenstern immer ganz oben unter „Jetzt abholen“.", sheet = "goals", done = p => S.quest > 0 },
            new TutStep { text = "Zoome mit zwei Fingern heraus bis zur Planetenkarte", why = "Auf der Karte siehst du den ganzen Planeten. Mit „Planeten“ reist du später weiter.", done = p => S.tutSeen.Contains("map") },
            new TutStep { text = "Putze alle 20 Übungsparzellen – dein Buggy hilft mit", why = "Im echten Spiel gehört dir der ganze Mond. Bei 80 % öffnet sich der nächste Planet.", done = p => TutClean(p) >= TutCellsOf(p).Count, prog = p => TutClean(p) / (float)Mathf.Max(1, TutCellsOf(p).Count) } };

        static HashSet<int> tutCells; static PlanetData tutPd;
        /// <summary>Die 20 Übungsparzellen: die der Basis am nächsten, ohne Wasser.</summary>
        public static HashSet<int> TutCellsOf(PState ps)
        {
            if (tutCells != null && tutPd == ps.pd) return tutCells;
            var pd = ps.pd; var bc = pd.cells[pd.baseCell].c.normalized;
            tutCells = new HashSet<int>(pd.cells.Where(c => !c.IsBase && !c.Wet).OrderBy(c => c.ring).ThenByDescending(c => Vector3.Dot(c.c.normalized, bc)).Take(TUT_CELLS).Select(c => c.id));
            tutCells.Add(pd.start); tutPd = pd; return tutCells;
        }
        public static bool TutCell(PState ps, int id) => TutCellsOf(ps).Contains(id);
        public static int TutClean(PState ps) => TutCellsOf(ps).Count(id => ps.clean[id]);
        /// <summary>Gibt es schon einen echten Spielstand (normal)?</summary>
        static bool RealSaveExists() { string k = "lunar-cleanup-tycoon-unity-v1" + (TestMode ? "-test" : ""); return PlayerPrefs.HasKey(k) || PlayerPrefs.HasKey(k + "-bak"); }
        /// <summary>Beim Start: läuft gerade das Tutorial, oder ist es ein ganz neuer Spieler?</summary>
        static void InitTut()
        {
            Tut = PlayerPrefs.GetInt(TutKey, 0) == 1 || (!TestMode && !TutDoneOnce && !RealSaveExists());
            if (Tut) { if (PlayerPrefs.GetInt(TutKey, 0) == 0) { PlayerPrefs.SetInt(TutHardKey, Hard ? 1 : 0); PlayerPrefs.SetInt(TutKey, 1); PlayerPrefs.Save(); } Hard = false; }
        }
        public static int TutIndex => S != null ? Mathf.Clamp(S.tutStep, 0, TutSteps.Length) : 0;
        public static bool TutFinale => Tut && S != null && S.tutStep >= TutSteps.Length;
        public static string TutSheet() => Tut && TutIndex < TutSteps.Length ? TutSteps[TutIndex].sheet : null;
        public static float TutProg() { if (!Tut || TutIndex >= TutSteps.Length) return 1; var st = TutSteps[TutIndex]; try { return st.prog != null ? Mathf.Clamp01(st.prog(Cur)) : (st.done(Cur) ? 1 : 0); } catch { return 0; } }
        public static void TutSaw(string what) { if (!Tut || S == null || string.IsNullOrEmpty(what)) return; if (S.tutSeen == null) S.tutSeen = new List<string>(); if (!S.tutSeen.Contains(what)) S.tutSeen.Add(what); }
        /// <summary>Tutorial (noch einmal) starten: echter Spielstand wird gespeichert, ein frischer Übungsstand beginnt.</summary>
        public static void TutStart()
        {
            if (Tut) return; Save(); PlayerPrefs.SetInt(TutHardKey, Hard ? 1 : 0); Hard = false; Tut = true; PlayerPrefs.SetInt(TutKey, 1);
            PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + "-bak"); PlayerPrefs.Save();
            S = Fresh(); Reattach(); TutEnter(0);
        }
        /// <summary>Tutorial beenden (nach dem Kometen) oder überspringen: zurück in den echten Spielstand, neu oder vorhanden.</summary>
        public static void TutFinish(bool skipped)
        {
            if (!Tut) return; PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + "-bak");
            Tut = false; Hard = PlayerPrefs.GetInt(TutHardKey, 0) == 1 && PlayerPrefs.GetInt(HardOpenKey, 0) == 1; PlayerPrefs.SetInt(HardKey, Hard ? 1 : 0);
            PlayerPrefs.SetInt(TutKey, 0); PlayerPrefs.SetInt(TutDoneKey, 1); PlayerPrefs.Save();
            var s = Load(); bool isNew = s == null; S = s ?? Fresh(); if (Hard) S.hard = true;
            Reattach();
            if (isNew && !skipped && !Hard) { double r = 0; for (int i = 0; i < TUT_SKIP_QUESTS && i < GameData.Quests.Count; i++) r += Math.Round(GameData.Quests[i].r * GoalF); S.quest = Math.Max(S.quest, TUT_SKIP_QUESTS); Earn(r); }
            Save();
            Toast(skipped ? "Tutorial übersprungen – viel Spaß!" : "Jetzt geht's richtig los! Der ganze Mond wartet auf dich.", "good");
        }
        static void Reattach()
        {
            Icp = null; Find = null; restN = -1; ResetStats(); tutCells = null;
            GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
            P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); if (!P.ContainsKey(S.planet)) S.planet = "moon"; MigrateWallet(); ResetStats();
            Save(); OnPlanetChanged?.Invoke();
        }
        /// <summary>Neuer Schritt: kurze Erklärung, und genug Übungsgeld für das, was gekauft werden soll.</summary>
        static void TutEnter(int i)
        {
            var ps = Cur;
            if (i >= TutSteps.Length)
            {
                Toast("Geschafft! Doch was ist das … ein Komet hält direkt auf den Übungsbereich zu!", "bad");
                S.doomPid = ps.pid; S.doomT = 12; OnDoomStart?.Invoke(ps.pid); return;
            }
            var st = TutSteps[i]; Toast("Tutorial " + (i + 1) + "/" + TutSteps.Length + ": " + st.why, "good");
            double need = i == 4 ? 300 : i == 5 ? StageCost(ps) + 20 : i == 6 ? VehCost(ps, "buggy") + 20 : 0;
            if (need > 0 && need < 1e9 && S.credits < need) Earn(need - S.credits);
        }
        /// <summary>Editor/Test: einen Schritt weiter.</summary>
        public static void DevTutSkip() => TutFinish(true);
        public static void DevTutNext() { if (!Tut || S.tutStep >= TutSteps.Length) return; S.tutStep++; TutEnter(S.tutStep); }
        /// <summary>Jede Sekunde: ist der aktuelle Schritt erledigt?</summary>
        static void TutTick()
        {
            if (!Tut || S == null || Offline) return; Attend(); /* keine Betreuungs-Warnung im Tutorial */ if (S.tutStep >= TutSteps.Length) return; var ps = Cur; bool ok;
            try { ok = TutSteps[S.tutStep].done(ps); } catch { ok = false; }
            if (!ok) return; S.tutStep++; TutEnter(S.tutStep); Save();
        }
    }
}
