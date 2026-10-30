// Alle Spieldaten – 1:1 aus dem Browser-Prototyp exportiert (Resources/Data/gamedata.json, erzeugt mit tools/export_unity_data.py).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class MatDef { public string key, name, shape; public float v; public bool raw; public Color col; public int index; }
    public class DirtDef { public string key, name, deb; public int mat; public float amt, hard; public Color baseC, darkC, lightC; }
    public class KindDef { public string key, name, desc; public float amt, hard, cost; public int tier; }
    public class StageDef { public int n; public string name, txt; public float cost; }
    public class ResDef { public string id, name, desc, req; public double cost; public float time; }
    public class UpgDef { public string id, cat, name, res, label; public bool loc, hide, done, rad; public float baseCost, grow; public int max, stage; public double[] vals; public string[] show; }
    public class VehDef { public string key, name, size, desc, planet; public float batt, cost, speed, radius, power, tank, energy; public int stage, tier; public string[] home; }
    public class StDef { public int docks; public float buf, rate; public string acc; }
    public class BldDef { public string id, name, research, desc; public int stage; public float cost, fp; public bool auto, mach, geo; public StDef st; }
    public class BeltDef { public string id, from, to; public float h, L; public int stage; public Vector2[] rp; public Vector2 port0, port1; }
    public class LayoutDef { public Dictionary<string, Vector2> pos = new Dictionary<string, Vector2>(); public Dictionary<string, float> yaw = new Dictionary<string, float>(); public Vector2 shop, start, park; public List<BeltDef> belts = new List<BeltDef>(); }
    public class Recipe { public int inp, outp, k; public float n, t; public string plan; }
    public class LineDef { public Dictionary<string, string> names = new Dictionary<string, string>(); public List<Recipe> presse = new List<Recipe>(), schmelze = new List<Recipe>(), raff = new List<Recipe>(); public List<Recipe> Of(string id) => id == "presse" ? presse : id == "schmelze" ? schmelze : raff; }
    public class CellsDef { public int N, baseCell, start; public float R; public int[] type, kind, ring; public Vector3 c0, c7; }
    public class Theme { public Color sky, hemiSky, hemiGround, sun, dust, glow; public float hemiI, sunI, glowOp; public Color[] clean, baseC; public int stars; public bool hasGlow; public string giant; }
    public class StormDef { public string name; public float every, dur, veh = 1, belt = 1, mach = 1, solar = 1, dust = 1, rad = 1; }
    public class PlanetDef
    {
        public string key, name, sub, hzName, hzDesc; public int freq, start; public float diff, energy, unlockCost, solar = 1, dustMul = 1, rad;
        public Color col0, col1; public List<KeyValuePair<int, float>> types = new List<KeyValuePair<int, float>>(); public Theme theme; public StormDef storm;
    }
    public class QuestDef { public string t; public float r; }

    public static class GameData
    {
        public static bool Loaded;
        public static string[] POrder;
        public static Dictionary<string, PlanetDef> Planets = new Dictionary<string, PlanetDef>();
        public static List<MatDef> Mats = new List<MatDef>(); public static Dictionary<string, int> MatIx = new Dictionary<string, int>();
        public static List<DirtDef> Dirt = new List<DirtDef>(); public static Dictionary<string, int> DirtIx = new Dictionary<string, int>();
        public static List<KindDef> Kinds = new List<KindDef>();
        public static List<StageDef> Stages = new List<StageDef>();
        public static List<UpgDef> Upg = new List<UpgDef>(); public static Dictionary<string, UpgDef> Up = new Dictionary<string, UpgDef>();
        public static Dictionary<string, VehDef> Veh = new Dictionary<string, VehDef>(); public static List<string> VehOrder = new List<string>();
        public static Dictionary<string, BldDef> Bld = new Dictionary<string, BldDef>(); public static string[] BOrder, Stations;
        public static Dictionary<string, Vector3> Mach = new Dictionary<string, Vector3>(); // energy, inCap, outCap
        public static Dictionary<string, LineDef> Lines = new Dictionary<string, LineDef>();
        public static Dictionary<string, LayoutDef> Layouts = new Dictionary<string, LayoutDef>();
        public static Dictionary<string, CellsDef> Cells = new Dictionary<string, CellsDef>();
        public static List<QuestDef> Quests = new List<QuestDef>();
        public static int[] Raw;   // Rohstoffe (Mat-Index)
        public static int[] Prods; // Produkte (Mat-Index)
        public const int GRID = 32;
        public static float SolarPeak = 2.6f;
        public static List<ResDef> Res = new List<ResDef>();
        public class StormLook { public Color col, fog; public float size, wind, fall, dens; public bool lightning, embers, geysers, rad; }
        public static Dictionary<string, StormLook> Storm = new Dictionary<string, StormLook>(); public static int[] PowReq = new int[0];
        public static int OutDocks = 2, OutMax = 4, OutMinRing = 4; public static float OutBuf = 1500, OutRate = 20, OutPipe = 8;
        public static float RigBuf = 300, RigLoad = 16, RigEnergy = 1.5f, RigBase = .8f;
        public static int LiftMax = 3; public static float LiftSpeed = 12, LiftMinDist = 45;
        public static int DroneMax = 8; public static float DroneCap0 = 160, DroneSpeed0 = 9, DroneRate = 45, DroneMinDist = 30;
        public static Dictionary<string, float> DayLen = new Dictionary<string, float>(), Geof = new Dictionary<string, float>();
        public const float DONE = 0.03f;

        public static void Load()
        {
            if (Loaded) return;
            var ta = Resources.Load<TextAsset>("Data/gamedata");
            var root = MiniJson.Parse(ta.text);
            POrder = ToStrings(J.G(root, "porder"));
            foreach (var kv in J.O(J.G(root, "mats")))
            {
                var o = kv.Value; var m = new MatDef { key = kv.Key, name = J.S(o, "name"), shape = J.S(o, "shape", "crate"), v = J.F(o, "v"), raw = J.B(o, "raw"), col = Hex(J.S(o, "col", "#ffffff")), index = Mats.Count };
                MatIx[m.key] = Mats.Count; Mats.Add(m);
            }
            Raw = MapMats(J.G(root, "raw")); Prods = MapMats(J.G(root, "prods"));
            foreach (var kv in J.O(J.G(root, "dirt")))
            {
                var o = kv.Value; var d = new DirtDef { key = kv.Key, name = J.S(o, "name"), deb = J.S(o, "deb", "rock"), mat = MatIx[J.S(o, "mat")], amt = J.F(o, "amt"), hard = J.F(o, "hard", 1), baseC = Hex(J.S(o, "base")), darkC = Hex(J.S(o, "dark")), lightC = Hex(J.S(o, "light")) };
                DirtIx[d.key] = Dirt.Count; Dirt.Add(d);
            }
            foreach (var kv in J.O(J.G(root, "kinds")))
            {
                var o = kv.Value; Kinds.Add(new KindDef { key = kv.Key, name = J.S(o, "name"), desc = J.S(o, "desc"), amt = J.F(o, "amt"), hard = J.F(o, "hard", 1), cost = J.F(o, "cost", 1), tier = J.I(o, "tier") });
            }
            foreach (var o in J.A(J.G(root, "stages"))) Stages.Add(new StageDef { n = J.I(o, "n"), name = J.S(o, "name"), txt = J.S(o, "txt"), cost = J.F(o, "cost") });
            foreach (var o in J.A(J.G(root, "upg")))
            {
                var u = new UpgDef { id = J.S(o, "id"), cat = J.S(o, "cat"), name = J.S(o, "name"), res = J.S(o, "res"), label = J.S(o, "label"), loc = J.B(o, "loc"), hide = J.B(o, "hide"), done = J.B(o, "done"), rad = J.B(o, "rad"), baseCost = J.F(o, "base"), grow = J.F(o, "grow"), max = J.I(o, "max"), stage = J.I(o, "stage"), vals = J.Dv(J.G(o, "vals")), show = ToStrings(J.G(o, "show")) };
                Upg.Add(u); Up[u.id] = u;
            }
            foreach (var kv in J.O(J.G(root, "vt")))
            {
                var o = kv.Value; VehOrder.Add(kv.Key);
                Veh[kv.Key] = new VehDef { key = kv.Key, name = J.S(o, "name"), size = J.S(o, "size"), desc = J.S(o, "desc"), planet = J.S(o, "planet"), batt = J.F(o, "batt"), cost = J.F(o, "cost"), speed = J.F(o, "speed"), radius = J.F(o, "radius"), power = J.F(o, "power"), tank = J.F(o, "tank"), energy = J.F(o, "energy"), stage = J.I(o, "stage"), tier = J.I(o, "tier"), home = ToStrings(J.G(o, "home")) };
            }
            foreach (var kv in J.O(J.G(root, "bld")))
            {
                var o = kv.Value; var b = new BldDef { id = kv.Key, name = J.S(o, "name"), research = J.S(o, "research"), desc = J.S(o, "desc"), stage = J.I(o, "stage"), cost = J.F(o, "cost"), fp = J.F(o, "fp"), auto = J.B(o, "auto"), mach = J.B(o, "mach"), geo = J.B(o, "geo") };
                var st = J.G(o, "st"); if (st != null) b.st = new StDef { docks = J.I(st, "docks"), buf = J.F(st, "buf"), rate = J.F(st, "rate"), acc = J.S(st, "acc") };
                Bld[kv.Key] = b;
            }
            BOrder = ToStrings(J.G(root, "border")); Stations = ToStrings(J.G(root, "stations"));
            DayLen.Clear(); Geof.Clear(); Res.Clear();
            var rs = J.G(root, "res"); if (rs != null) foreach (var o in J.A(rs)) Res.Add(new ResDef { id = J.S(o, "id"), name = J.S(o, "name"), desc = J.S(o, "desc"), req = J.S(o, "req"), cost = J.D(o, "cost"), time = J.F(o, "time") });
            var dl = J.G(root, "daylen"); if (dl != null) foreach (var kv in J.O(dl)) DayLen[kv.Key] = Convert.ToSingle(kv.Value);
            var gf = J.G(root, "geof"); if (gf != null) foreach (var kv in J.O(gf)) Geof[kv.Key] = Convert.ToSingle(kv.Value);
            SolarPeak = J.F(J.G(root, "consts"), "SOLAR_PEAK", 2.6f);
            Storm.Clear(); var sl = J.G(root, "stormLook"); if (sl != null) foreach (var kv in J.O(sl)) { var o = kv.Value; Storm[kv.Key] = new StormLook { col = IntC(J.I(o, "col")), fog = IntC(J.I(o, "fog")), size = J.F(o, "size"), wind = J.F(o, "wind"), fall = J.F(o, "fall"), dens = J.F(o, "dens"), lightning = J.I(o, "lightning") > 0, embers = J.I(o, "embers") > 0, geysers = J.I(o, "geysers") > 0, rad = J.I(o, "rad") > 0 }; }
            var pr = J.G(root, "powreq"); if (pr != null) { var l = J.A(pr); PowReq = new int[l.Count]; for (int i = 0; i < l.Count; i++) PowReq[i] = Convert.ToInt32(l[i]); }
            var op = J.G(root, "outpost"); if (op != null) { OutDocks = J.I(op, "docks"); OutMax = J.I(op, "max"); OutMinRing = J.I(op, "minRing"); OutBuf = J.F(op, "buf"); OutRate = J.F(op, "rate"); OutPipe = J.F(op, "pipe"); }
            var rg = J.G(root, "rig"); if (rg != null) { RigBuf = J.F(rg, "buf"); RigLoad = J.F(rg, "load"); RigEnergy = J.F(rg, "energy"); RigBase = J.F(rg, "base"); }
            var lf = J.G(root, "lift"); if (lf != null) { LiftMax = J.I(lf, "max"); LiftSpeed = J.F(lf, "speed"); LiftMinDist = J.F(lf, "minDist"); }
            var dr = J.G(root, "drone"); if (dr != null) { DroneMax = J.I(dr, "max"); DroneCap0 = J.F(dr, "cap"); DroneSpeed0 = J.F(dr, "speed"); DroneRate = J.F(dr, "rate"); DroneMinDist = J.F(dr, "minDist"); }
            foreach (var kv in J.O(J.G(root, "mach"))) Mach[kv.Key] = new Vector3(J.F(kv.Value, "energy"), J.F(kv.Value, "inCap"), J.F(kv.Value, "outCap"));
            foreach (var kv in J.O(J.G(root, "lines")))
            {
                var L = new LineDef(); var nm = J.O(J.G(kv.Value, "names")); if (nm != null) foreach (var n in nm) L.names[n.Key] = n.Value as string;
                foreach (var id in new[] { "presse", "schmelze", "raff" })
                    foreach (var r in J.A(J.G(kv.Value, id)))
                        L.Of(id).Add(new Recipe { inp = MatIx[J.S(r, "in")], outp = MatIx[J.S(r, "out")], n = J.F(r, "n"), k = J.I(r, "k", 1), t = J.F(r, "t"), plan = J.S(r, "plan") });
                Lines[kv.Key] = L;
            }
            foreach (var kv in J.O(J.G(root, "layouts")))
            {
                var o = kv.Value; var L = new LayoutDef();
                foreach (var p in J.O(J.G(o, "pos"))) L.pos[p.Key] = V2(p.Value);
                var yw = J.O(J.G(o, "yaw")); if (yw != null) foreach (var p in yw) L.yaw[p.Key] = (float)(double)p.Value;
                L.shop = V2(J.G(o, "shop")); L.start = V2(J.G(o, "start")); L.park = V2(J.G(o, "park"));
                foreach (var b in J.A(J.G(o, "belts")))
                {
                    var rp = J.A(J.G(b, "rp")); var pts = new Vector2[rp.Count]; for (int i = 0; i < rp.Count; i++) pts[i] = V2(rp[i]);
                    L.belts.Add(new BeltDef { id = J.S(b, "id"), from = J.S(b, "from"), to = J.S(b, "to"), h = J.F(b, "h", .5f), stage = J.I(b, "stage"), L = J.F(b, "L"), rp = pts, port0 = V2(J.G(b, "port0")), port1 = V2(J.G(b, "port1")) });
                }
                Layouts[kv.Key] = L;
            }
            foreach (var kv in J.O(J.G(root, "cells")))
            {
                var o = kv.Value;
                Cells[kv.Key] = new CellsDef { N = J.I(o, "N"), R = J.F(o, "R"), baseCell = J.I(o, "base"), start = J.I(o, "start"), type = Ints(J.G(o, "type")), kind = Ints(J.G(o, "kind")), ring = Ints(J.G(o, "ring")), c0 = V3(J.G(o, "c0")), c7 = V3(J.G(o, "c7")) };
            }
            foreach (var kv in J.O(J.G(root, "planets")))
            {
                var o = kv.Value; var col = J.A(J.G(o, "col"));
                var P = new PlanetDef { key = kv.Key, name = J.S(o, "name"), sub = J.S(o, "sub"), freq = J.I(o, "freq"), diff = J.F(o, "diff", 1), energy = J.F(o, "energy", 1), unlockCost = J.F(o, "unlockCost"), col0 = Hex(col[0] as string), col1 = Hex(col[1] as string), start = DirtIx[J.S(o, "start")] };
                foreach (var t in J.A(J.G(o, "types"))) { var tl = J.A(t); P.types.Add(new KeyValuePair<int, float>(DirtIx[(string)tl[0]], (float)(double)tl[1])); }
                var hz = J.G(o, "hz");
                if (hz != null)
                {
                    P.hzName = J.S(hz, "name"); P.hzDesc = J.S(hz, "desc"); P.solar = J.F(hz, "solar", 1); P.dustMul = J.F(hz, "dust", 1); P.rad = J.F(hz, "rad", 0);
                    var s = J.G(hz, "storm"); if (s != null) P.storm = new StormDef { name = J.S(s, "name"), every = J.F(s, "every"), dur = J.F(s, "dur"), veh = J.F(s, "veh", 1), belt = J.F(s, "belt", 1), mach = J.F(s, "mach", 1), solar = J.F(s, "solar", 1), dust = J.F(s, "dust", 1), rad = J.F(s, "rad", 1) };
                }
                var th = J.G(o, "theme"); var hemi = J.A(J.G(th, "hemi")); var sun = J.A(J.G(th, "sun"));
                P.theme = new Theme
                {
                    sky = IntC(J.G(th, "sky")), hemiSky = IntC(hemi[0]), hemiGround = IntC(hemi[1]), hemiI = (float)(double)hemi[2], sun = IntC(sun[0]), sunI = (float)(double)sun[1],
                    dust = IntC(J.G(th, "dust")), stars = J.I(th, "stars"), hasGlow = J.G(th, "glow") != null, glow = IntC(J.G(th, "glow")), glowOp = J.F(th, "glowOp", .18f), giant = J.S(th, "giant"),
                    clean = HexArr(J.G(th, "clean")), baseC = HexArr(J.G(th, "base"))
                };
                Planets[kv.Key] = P;
            }
            foreach (var q in J.A(J.G(root, "quests"))) Quests.Add(new QuestDef { t = J.S(q, "t"), r = J.F(q, "r") });
            Loaded = true;
        }

        // ---- Helfer ----
        static string[] ToStrings(object o) { var l = J.A(o); if (l == null) return new string[0]; var r = new string[l.Count]; for (int i = 0; i < l.Count; i++) r[i] = l[i] as string ?? ""; return r; }
        static int[] Ints(object o) { var d = J.Dv(o); var r = new int[d.Length]; for (int i = 0; i < d.Length; i++) r[i] = (int)d[i]; return r; }
        static int[] MapMats(object o) { var s = ToStrings(o); var r = new int[s.Length]; for (int i = 0; i < s.Length; i++) r[i] = MatIx[s[i]]; return r; }
        static Vector2 V2(object o) { var d = J.Dv(o); return d.Length >= 2 ? new Vector2((float)d[0], (float)d[1]) : Vector2.zero; }
        static Vector3 V3(object o) { var d = J.Dv(o); return d.Length >= 3 ? new Vector3((float)d[0], (float)d[1], (float)d[2]) : Vector3.zero; }
        static Color[] HexArr(object o) { var s = ToStrings(o); var r = new Color[s.Length]; for (int i = 0; i < s.Length; i++) r[i] = Hex(s[i]); return r; }
        public static Color Hex(string h)
        {
            if (string.IsNullOrEmpty(h)) return Color.white;
            if (h[0] == '#') h = h.Substring(1);
            int v = System.Convert.ToInt32(h, 16); return IntC(v);
        }
        public static Color IntC(object o) { if (o is double d) return IntC((int)d); return Color.black; }
        public static Color IntC(int v) => new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);

        public static UpgDef U(string id) => Up[id];
        public static string MachName(string pid, string id) { var L = Lines[pid]; return L.names.TryGetValue(id, out var n) && !string.IsNullOrEmpty(n) ? n : Bld[id].name; }
    }
}
