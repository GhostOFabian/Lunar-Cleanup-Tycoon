// Spielstand-DTOs (JsonUtility). Neue Felder immer mit Standardwert anlegen – alte Stände dürfen nie kaputtgehen.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class KV { public string k; public int v; }
    [Serializable] public class Stats { public double sold, earned; public float bricks, barren; public int handUnloads, lifts, airlifts, crystals, cells; public float handM2; public int comboMax, meteors, salvage, storms, geysers, nightMin, flags; } // Erfolge 01.11.: neue Zähler, Default 0

    [Serializable] public class StSave { public string id; public float[] buf; public int[] docks = new int[0]; public int cell = -1, dc = -1; [NonSerialized] public bool busy; }
    [Serializable] public class VehSave { public int cr = -1; public float crT; public int id; public string type; public Vector3 d; public string mode = "seek"; public int cell = -1, ti = -1, last = -1; public float[] tank; public string sid; public float coll, qT, fullT, ph; public float bat = -1, wear, doom, repT; public bool repSlow; public int drone = -1, lift = -1, rep, rig = -1; public float xp; public int spec, skin; public string vname = ""; public int zone; [NonSerialized] public bool busy, charging; [NonSerialized] public string avoid; [NonSerialized] public float avoidT, idleT, driveT, skipT; [NonSerialized] public int skipCell = -1; [NonSerialized] public float sweepT, sweepCheck, stuckT; [NonSerialized] public Vector3 stuckP; }
    [Serializable] public class DroneSave { public int id; public Vector3 d; public string mode = "idle", sid = "", tk = "v", tko = ""; public float[] tank; public int tgt = -1; public float wt; [NonSerialized] public bool busy; }
    [Serializable] public class LifterSave { public int id; public Vector3 d; public string mode = "idle"; public int veh = -1, cell = -1; }
    [Serializable] public class CrystalSave { public int id, cell, t, claim = -1; public float x, y, g; }
    [Serializable] public class RigSave { public int cell, lv, claim = -1, dc = -1, fixedN; public float[] buf; public bool bag; public float mined, faultT; public string faultK = ""; [NonSerialized] public bool busy; }
    [Serializable] public class ActSave { public int id; public float sum; public string grid; }
    [Serializable] public class BeltSave { public string id; public List<float> s = new List<float>(); public List<int> m = new List<int>(); public List<float> a = new List<float>(); }
    [Serializable] public class MachSave { public string id; public float[] inp, outp; public int cur = -1, bat = 1, made; public float prog; public int lastOut = -1; }
    // ---- Vorbereitet für geplante Erweiterungen (docs/IDEEN_SPEC.md) – alle mit Standardwert, noch nicht benutzt ----
    [Serializable] public class EventSave { public string key = ""; public float t; public int cell = -1; public bool done; public int mat = -1; public float amt0; /* Händler: Ware; Müllberg: Anfangsmenge */ }
    [Serializable] public class ChalSave { public List<string> offer = new List<string>(); public string act = ""; public double v0; public float left; public long refresh; public int done; } // Fabian 01.11.: Herausforderungen
    [Serializable] public class FKV { public string k = ""; public float v; } // Fabian 02.11.: Spezialgüter im Galaxie-Lager
    [Serializable] public class BPosSave { public string id = ""; public float x, y; } // 03.11.: eigener Gebäudeplatz
    [Serializable] public class BeltRouteSave { public string id = ""; public List<Vector2> p = new List<Vector2>(); public List<float> h = new List<float>(); public float a0 = -999, a1 = -999; public float d0 = -1, d1 = -1; /* 03.11.: Abstand des freien Endpunkts vor der Gebäudewand (−1 = alt) */ /* eigener Aus-/Eingang: Winkel um das Gebäude (−999 = automatisch) */ } // eigener Bandverlauf (Band-Editor)
    [Serializable] public class DailySave { public int lastDay, streak; }
    [Serializable] public class RuleSave { public string ifK = "", thenK = "", arg = ""; public float val; public bool on = true; }
    [Serializable] public class SeasonSave { public string key = ""; public float clean; public bool claimed; }
    [Serializable] public class StationSave { public string mod = ""; public int lv; }
    [Serializable] public class RouteSave { public string from = "", to = ""; public int mat = -1; public float t; }

    [Serializable] public class WorkOrderSave { public int m = -1, n; public float got; public double r; }
    [Serializable] public class ContractItem { public string type = ""; public double target, start, reward; public bool done; }
    [Serializable] public class ContractSave { public int day; public List<ContractItem> list = new List<ContractItem>(); public bool bonus; }
    [Serializable] public class TeamSave { public string mode = "home"; public Vector3 d; public float t; public int cell = -1; public string key = ""; } // Suchtrupp (Bergung von Wracks und Kometentrümmern)
    [Serializable] public class CartSave { public bool owned; public Vector3 d; public string mode = "park", to = ""; public float[] tank; public float wt; }

    [Serializable]
    public class PlanetSave
    {
        public string pid;
        public List<int> cellD = new List<int>(); public int cellDay; /* gereinigte Parzellen je Tag (letzte 14) */
        [NonSerialized] public float sellIdle, sellLast, sellAge; public float sellRate; /* mittlerer Zufluss in den Frachtraum (E/s) */ /* Rakete: wie lange schon nichts Neues in den Frachtraum kam */
        public int freq; // Planetengröße (0 = Stand vor v6.4 → alte Größe)
        public int stage = 1, techF, nextVeh = 1, nRockets;
        public bool prize; // Reiseprämie bei 100 % schon gezahlt
        public List<FlagSave> flags = new List<FlagSave>(); public bool flagsInit; // Flaggen (01.11.)
        public long tFirst, tDoneAt; public double tPlay, tDone; // Fabian 01.11.: Statistik – erster Besuch, Spielzeit auf dem Planeten, Spielzeit bis komplett sauber
        public List<string> bld = new List<string>();
        public List<KV> upg = new List<KV>();
        public float[] hand, roh, prod, sellBuf;
        public List<StSave> st = new List<StSave>();
        public List<VehSave> veh = new List<VehSave>();
        public string clean = "", paid = "";
        public List<ActSave> act = new List<ActSave>();
        public List<BeltSave> belts = new List<BeltSave>();
        public List<MachSave> mach = new List<MachSave>();
        public float rk;
        public float energy = 60, dayT = -1, hzT, hzOn;
        public bool hzWarned, shield, autoMaint = true;
        public bool rkGo;
        public double soldP;
        public bool rest; public double restRate, restEarn, rateEma; [NonSerialized] public double lastSoldP; // Ruhemodus geretteter Planeten (02.11.)
        public CartSave cart = new CartSave();
        public List<DroneSave> drones = new List<DroneSave>();
        public List<StSave> outposts = new List<StSave>();
        public List<LifterSave> lifters = new List<LifterSave>();
        public List<RigSave> rigs = new List<RigSave>();
        public List<CrystalSave> crystals = new List<CrystalSave>(); public float crT; public int crN; // Leuchtkristalle (wachsen nach)
        public float deep;
        public int nextDrone = 1;
        public float met;                                   // Meteoritenschauer-Uhr
        public EventSave ev = new EventSave();            // B1 laufendes Ereignis
        public List<RuleSave> rules = new List<RuleSave>(); // F2 Automatik-Regeln
        // Wirtschaft (wie im Browser-Prototyp): Produktionsplan, Verkaufsband je Produkt, Frachtauftrag, Verkaufsautomat, Betriebsbonus, Gebäudestufen
        public List<string> planOff = new List<string>();   // ausgeschaltete Rezepte (Plan-Schlüssel)
        public TeamSave team = new TeamSave();
        public int exp, woDone; public string spec = ""; public WorkOrderSave wo = new WorkOrderSave(); public List<int> pgDone = new List<int>(); // Ausbaustufe, Ausrichtung, Werksaufträge, Planetenziele
        public List<int> sellYes = new List<int>();         // ausdrücklich zum Verkauf freigegebene Waren
        public List<int> autoOff = new List<int>();         // Produkte, die NICHT aufs Verkaufsband sollen
        public int order = -1; public bool autoOrder = true, autoDump;
        public float flowB, fbT; public int flowLv;
        public List<KV> blvl = new List<KV>();
        /* Fabian 02.11. (Build 34): Abwehr, Lieferketten, Terraforming, Band-Editor – alle mit Default */
        public int msLv, flakLv;                 // Meteorschild, Abwehrraketen
        public float msCool;                     // Schild lädt nach (s)
        public int terra; public float terraHp = 1; // Terraforming-Stufe 0–5 und Zustand 0–1
        public float gAcc, upAcc, hitT;          // Güter-Produktion, Unterhalt, Einschlagschaden (Anlagen gestört, s)
        public bool supOk = true;                // letzter Unterhalt geliefert
        public bool xport; public double xEarn;    // Export-Frachter gekauft, Einnahmen daraus (02.11.)
        public float offPend;                    // Offline-Zeit, die dieser Planet noch nachrechnen muss (s)
        public List<BeltRouteSave> bRoutes = new List<BeltRouteSave>();
        public List<BPosSave> bMove = new List<BPosSave>();
        public List<int> goo = new List<int>(); public float gooT, gooSeedT, secretT; public bool puzzleDone; public int pieceMask, pieceN; /* 09.11.: gefundene Puzzleteile (Bit je Teil) – bleiben gefunden */ public float[] rohRate; /* 08.11.: Rohstoff-Zufluss je s (Mittel über ~30 min) – für machbare Bestellungen */ public int secretBeat, stars; /* 08.11.: Mond-Geheimnis */ public List<HelperSave> helpers = new List<HelperSave>(); public int helperNext; public List<FKV> perkHeat = new List<FKV>(); public float[] depot; public double cash, cashIn; public float careT, boostT, pileT; public bool pile30, pile70; /* 04.11.: Müllberg-Termine */ public float waveT; public int waveCell = -1; public List<int> pylons = new List<int>(); /* 04.11.: Ladesäulen (Hard Mode) */ /* 03.11.: Schmutzwelle (Hard Mode) */ /* 03.11.: eigene Planetenkasse (für nicht aktive Planeten), schon mitgebrachtes Geld, Zeit seit dem letzten Besuch */ /* 03.11.: Zwischenlager des Sammelshuttles (Hard Mode), null = leer */ /* 03.11.: Perk-Preisaufschlag je Kauf, sinkt mit der Zeit */   // 03.11.: Putzhelfer (Hard Mode)          // 03.11.: verschobene Gebäude
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1; public string hClass = ""; /* 05.11.: Hard-Mode-Klasse (leer = noch nicht gewählt) */
        public double credits;
        public string planet = "moon";
        public List<KV> upg = new List<KV>();
        public int quest;
        public Stats stats = new Stats();
        public List<PlanetSave> planets = new List<PlanetSave>();
        public long lastActive;
        public bool sound = true;
        public int lineCol; // Fabian 01.11.: Farbe der Parzellenränder (0 = Grün … , siehe Game.LineColors)
        public ContractSave contracts = new ContractSave();
        public int fleetPlus; // Fabian 01.11.: gekaufte Flottenplätze über 15 hinaus (höchstens 5)
        public bool hard; public int walletV, ordFix; /* 08.11.: ordFix = alte Riesen-Bestellungen einmal verkleinert */ public float lk, hXp, ult, suit = -1; /* 04.11.: lk = Leuchtkristalle (Währung) */ public int dropDry, hHand, hNoPerk, hNoPerkBest, ultFulls, waveStop; public List<int> hGoals = new List<int>(); /* 03.11.: Hard Mode – Putz-Level, Ultimate, Hard-Ziele */ /* 03.11.: 1 = Planetenkassen eingeführt */ public double perkInc; /* 03.11.: geglättetes Einkommen/min für Perk-Preise */ public List<KV> perkN = new List<KV>(); public List<FKV> buffT = new List<FKV>(); // 03.11.: Perk-Shop (Hard Mode)                                          // 03.11.: Spielstand des Hard Mode
        public int extraSlots; public List<string> owned = new List<string>(), freeUnlock = new List<string>(); // Shop: dauerhafte Käufe
        public float volSfx = .8f, volMusic = .6f; // Lautstärke Geräusche / Musik (0–1)
        public List<string> resDone = new List<string>();
        public string resAct = "";
        public List<string> wrecks = new List<string>();
        public List<string> done = new List<string>();
        public float resT;
        // ---- Vorbereitet (docs/IDEEN_SPEC.md) ----
        public float evNext = -1;                                 // B1 Zeit bis zum nächsten Ereignis
        public List<string> finds = new List<string>();           // B2 "pid:index"
        public DailySave daily = new DailySave();                 // B4
        public List<string> resSpec = new List<string>();         // C1 "ast:ebene:wahl"
        public double stardust; public int sdSpent, resets;       // D1
        public double debt; // Fabian 01.11.: Reise auf Kredit – wird mit 50 % jeder Einnahme abbezahlt
        public List<string> sdNodes = new List<string>();
        public SeasonSave season = new SeasonSave();              // D2
        public List<string> achievements = new List<string>(); public string title = ""; // D3
        public List<StationSave> station = new List<StationSave>(); // C5
        public List<RouteSave> routes = new List<RouteSave>();    // C4
        public List<string> notifyOff = new List<string>();       // F1 abgeschaltete Benachrichtigungen
        public bool music = true, vibrate = true;                 // A4/A5
        /* Fabian 01.11. (Build 29): neue Felder, alle mit Default */
        public bool putzi;                                        // Putzi-7 begleitet dich (nach seinem Finale)
        public string ruin = ""; public long ruinUntil;           // zerstörter Planet und bis wann er neu aufgebaut wird
        public float doomNext = -1, doomT = -1; public string doomPid = ""; // Riesenmeteor: Zeit bis zum nächsten, Countdown, Ziel
        public List<double> incH = new List<double>(); public long incHour; // Einnahmen je Stunde (letzte 48)
        public ChalSave chal = new ChalSave();
        /* Fabian 02.11. (Build 34) */
        public List<FKV> goods = new List<FKV>();                 // Spezialgüter aller Planeten
        public int doomN, doomHits;                               // Riesenmeteore bisher, abgefangene Brocken
        public List<VisitOrder> vorders = new List<VisitOrder>(); public float voT = 300; public int voLv, voNext; // Bestellungen von Besuchern (02.11.)
    }
}
