// Spielstand-DTOs (JsonUtility). Neue Felder immer mit Standardwert anlegen – alte Stände dürfen nie kaputtgehen.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class KV { public string k; public int v; }
    [Serializable] public class Stats { public double sold, earned; public float bricks, barren; public int handUnloads; public float handM2; }

    [Serializable] public class StSave { public string id; public float[] buf; public int[] docks = new int[0]; }
    [Serializable] public class VehSave { public int id; public string type; public Vector3 d; public string mode = "seek"; public int cell = -1, ti = -1, last = -1; public float[] tank; public string sid; public float coll, qT, fullT, ph; public float bat = -1; [NonSerialized] public bool busy, charging; }
    [Serializable] public class ActSave { public int id; public float sum; public string grid; }
    [Serializable] public class BeltSave { public string id; public List<float> s = new List<float>(); public List<int> m = new List<int>(); public List<float> a = new List<float>(); }
    [Serializable] public class MachSave { public string id; public float[] inp, outp; public int cur = -1, bat = 1, made; public float prog; public int lastOut = -1; }
    [Serializable] public class CartSave { public bool owned; public Vector3 d; public string mode = "park", to = ""; public float[] tank; public float wt; }

    [Serializable]
    public class PlanetSave
    {
        public string pid;
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
        public float energy = 60, dayT = -1;
        public bool rkGo;
        public double soldP;
        public CartSave cart = new CartSave();
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
    }
}
