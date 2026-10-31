// Spielstand-DTOs (JsonUtility). Neue Felder immer mit Standardwert anlegen – alte Stände dürfen nie kaputtgehen.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class KV { public string k; public int v; }
    [Serializable] public class Stats { public double sold, earned; public float bricks, barren; public int handUnloads, lifts, airlifts, crystals; public float handM2; }

    [Serializable] public class StSave { public string id; public float[] buf; public int[] docks = new int[0]; public int cell = -1, dc = -1; [NonSerialized] public bool busy; }
    [Serializable] public class VehSave { public int cr = -1; public float crT; public int id; public string type; public Vector3 d; public string mode = "seek"; public int cell = -1, ti = -1, last = -1; public float[] tank; public string sid; public float coll, qT, fullT, ph; public float bat = -1, wear, doom, repT; public bool repSlow; public int drone = -1, lift = -1, rep, rig = -1; public float xp; public int spec, skin; public string vname = ""; [NonSerialized] public bool busy, charging; [NonSerialized] public string avoid; [NonSerialized] public float avoidT, idleT, driveT, skipT; [NonSerialized] public int skipCell = -1; }
    [Serializable] public class DroneSave { public int id; public Vector3 d; public string mode = "idle", sid = "", tk = "v", tko = ""; public float[] tank; public int tgt = -1; public float wt; [NonSerialized] public bool busy; }
    [Serializable] public class LifterSave { public int id; public Vector3 d; public string mode = "idle"; public int veh = -1, cell = -1; }
    [Serializable] public class CrystalSave { public int id, cell, t, claim = -1; public float x, y, g; }
    [Serializable] public class RigSave { public int cell, lv, claim = -1, dc = -1, fixedN; public float[] buf; public bool bag; public float mined, faultT; public string faultK = ""; [NonSerialized] public bool busy; }
    [Serializable] public class ActSave { public int id; public float sum; public string grid; }
    [Serializable] public class BeltSave { public string id; public List<float> s = new List<float>(); public List<int> m = new List<int>(); public List<float> a = new List<float>(); }
    [Serializable] public class MachSave { public string id; public float[] inp, outp; public int cur = -1, bat = 1, made; public float prog; public int lastOut = -1; }
    // ---- Vorbereitet für geplante Erweiterungen (docs/IDEEN_SPEC.md) – alle mit Standardwert, noch nicht benutzt ----
    [Serializable] public class EventSave { public string key = ""; public float t; public int cell = -1; public bool done; }
    [Serializable] public class DailySave { public int lastDay, streak; }
    [Serializable] public class RuleSave { public string ifK = "", thenK = "", arg = ""; public float val; public bool on = true; }
    [Serializable] public class SeasonSave { public string key = ""; public float clean; public bool claimed; }
    [Serializable] public class StationSave { public string mod = ""; public int lv; }
    [Serializable] public class RouteSave { public string from = "", to = ""; public int mat = -1; public float t; }

    [Serializable] public class CartSave { public bool owned; public Vector3 d; public string mode = "park", to = ""; public float[] tank; public float wt; }

    [Serializable]
    public class PlanetSave
    {
        public string pid;
        public int freq; // Planetengröße (0 = Stand vor v6.4 → alte Größe)
        public int stage = 1, techF, nextVeh = 1, nRockets;
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
        public CartSave cart = new CartSave();
        public List<DroneSave> drones = new List<DroneSave>();
        public List<StSave> outposts = new List<StSave>();
        public List<LifterSave> lifters = new List<LifterSave>();
        public List<RigSave> rigs = new List<RigSave>();
        public List<CrystalSave> crystals = new List<CrystalSave>(); public float crT; public int crN; // Leuchtkristalle (wachsen nach)
        public float deep;
        public int nextDrone = 1;
        public EventSave ev = new EventSave();            // B1 laufendes Ereignis
        public List<RuleSave> rules = new List<RuleSave>(); // F2 Automatik-Regeln
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public double credits;
        public string planet = "moon";
        public List<KV> upg = new List<KV>();
        public int quest;
        public Stats stats = new Stats();
        public List<PlanetSave> planets = new List<PlanetSave>();
        public long lastActive;
        public bool sound = true;
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
        public List<string> sdNodes = new List<string>();
        public SeasonSave season = new SeasonSave();              // D2
        public List<string> achievements = new List<string>(); public string title = ""; // D3
        public List<StationSave> station = new List<StationSave>(); // C5
        public List<RouteSave> routes = new List<RouteSave>();    // C4
        public List<string> notifyOff = new List<string>();       // F1 abgeschaltete Benachrichtigungen
        public bool music = true, vibrate = true;                 // A4/A5
    }
}
