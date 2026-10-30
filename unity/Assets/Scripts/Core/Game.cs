// Spiellogik – übertragen aus simStep & Co. im Browser-Prototyp. Reines C#, keine GameObjects.
// Eine Simulation für live und offline: Step(dt) wird jedes Frame aufgerufen und offline in 1-s-Schritten.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class Act { public Cell cl; public float[] grid; public float sum, amt; public DirtDef D; public KindDef K; }
    public class BeltItem { public float s; public int m; public float a; }

    public class PState
    {
        public PlanetSave sv; public PlanetData pd; public PlanetDef P; public LayoutDef lay; public LineDef line;
        public bool[] clean, paid; public int nClean;
        public Dictionary<int, Act> act = new Dictionary<int, Act>();
        public HashSet<int> front = new HashSet<int>();
        public HashSet<string> bld = new HashSet<string>();
        public Dictionary<string, int> upg = new Dictionary<string, int>();
        public Dictionary<string, StSave> st = new Dictionary<string, StSave>();
        public Dictionary<string, MachSave> mach = new Dictionary<string, MachSave>();
        public Dictionary<string, List<BeltItem>> belts = new Dictionary<string, List<BeltItem>>();
        public Dictionary<string, bool> jam = new Dictionary<string, bool>();
        public HashSet<int> dirtyCells = new HashSet<int>();
        public int baseVersion;
        public bool[] navG; public string navKey; public Dictionary<long, List<Vector2>> navCache = new Dictionary<long, List<Vector2>>();
        public Dictionary<int, VehNav> vnav = new Dictionary<int, VehNav>();
        public float ef = 1, solar = 1, demand, prodNow, vm = 1, bm = 1, mm = 1; public bool shOn;
        public string pid => sv.pid;
    }
    public class VehNav { public string tk, nk; public List<Vector2> wp; public int i; public Vector2 s; }

    public static class Game
    {
        public static SaveData S;
        public static Dictionary<string, PState> P = new Dictionary<string, PState>();
        public static int NM;
        public static event Action<string, string> OnToast;              // Text, Art (good/bad/"")
        public static event Action<Vector3, string, string> OnFloat;     // Richtung, Text, Art
        public static event Action<string, double> OnRocket;
        public static event Action OnPlanetChanged;
        public static Vector3 CartGoal;                                  // wohin der Sammelwagen fährt (Blickpunkt der Kamera)
        public static bool Offline;
        static readonly System.Random Rng = new System.Random(4711);
        static readonly Dictionary<string, int> GUpg = new Dictionary<string, int>();
        const float VEH_BUFF = 2.6f, BSP = 1.25f;
        const string Key = "lunar-cleanup-tycoon-unity-v1";

        public static PState Cur => P[S.planet];
        public static PlanetDef CurP => GameData.Planets[S.planet];

        // ================= Start, Speichern, Laden =================
        public static void Init()
        {
            GameData.Load(); NM = GameData.Mats.Count;
            S = Load() ?? Fresh();
            GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
            P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps);
            if (!P.ContainsKey(S.planet)) S.planet = "moon";
        }

        static SaveData Fresh() { var s = new SaveData(); s.planets.Add(NewPlanet("moon")); s.lastActive = Now(); return s; }
        public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        static PlanetSave NewPlanet(string pid)
        {
            var ps = new PlanetSave { pid = pid };
            foreach (var id in GameData.BOrder) { var B = GameData.Bld[id]; if (B.auto && B.stage <= 1) ps.bld.Add(id); }
            return ps;
        }

        static float[] Arr(float[] a) { if (a == null || a.Length != NM) { var r = new float[NM]; if (a != null) Array.Copy(a, r, Math.Min(a.Length, NM)); return r; } return a; }

        static PState Attach(PlanetSave sv)
        {
            var ps = new PState { sv = sv, pd = PlanetData.Get(sv.pid), P = GameData.Planets[sv.pid], lay = GameData.Layouts[sv.pid], line = GameData.Lines[sv.pid] };
            sv.hand = Arr(sv.hand); sv.roh = Arr(sv.roh); sv.prod = Arr(sv.prod); sv.sellBuf = Arr(sv.sellBuf);
            if (sv.cart == null) sv.cart = new CartSave(); sv.cart.tank = Arr(sv.cart.tank);
            foreach (var b in sv.bld) ps.bld.Add(b);
            foreach (var kv in sv.upg) ps.upg[kv.k] = kv.v;
            int N = ps.pd.N; ps.clean = new bool[N]; ps.paid = new bool[N];
            if (sv.clean.Length == N) { for (int i = 0; i < N; i++) ps.clean[i] = sv.clean[i] == '1'; }
            else for (int i = 0; i < N; i++) ps.clean[i] = ps.pd.cells[i].IsBase;
            if (sv.paid.Length == N) for (int i = 0; i < N; i++) ps.paid[i] = sv.paid[i] == '1';
            for (int i = 0; i < N; i++) if (ps.clean[i] && !ps.pd.cells[i].IsBase) ps.nClean++;
            foreach (var s in GameData.Stations) { var o = sv.st.Find(x => x.id == s); if (o == null) { o = new StSave { id = s }; sv.st.Add(o); } o.buf = Arr(o.buf); ps.st[s] = o; }
            foreach (var m in new[] { "presse", "schmelze", "raff" }) { var o = sv.mach.Find(x => x.id == m); if (o == null) { o = new MachSave { id = m }; sv.mach.Add(o); } o.inp = Arr(o.inp); o.outp = Arr(o.outp); if (o.cur >= ps.line.Of(m).Count) o.cur = -1; ps.mach[m] = o; }
            foreach (var b in ps.lay.belts)
            {
                var lst = new List<BeltItem>(); var o = sv.belts.Find(x => x.id == b.id);
                if (o != null) for (int i = 0; i < o.s.Count && i < o.m.Count && i < o.a.Count; i++) if (o.m[i] >= 0 && o.m[i] < NM) lst.Add(new BeltItem { s = Mathf.Min(o.s[i], b.L), m = o.m[i], a = o.a[i] });
                ps.belts[b.id] = lst;
            }
            foreach (var a in sv.act)
            {
                if (a.id < 0 || a.id >= N || ps.clean[a.id]) continue;
                var A = NewAct(ps, a.id); var bytes = string.IsNullOrEmpty(a.grid) ? null : Convert.FromBase64String(a.grid);
                if (bytes != null && bytes.Length == A.grid.Length) { float s = 0; for (int k = 0; k < bytes.Length; k++) { A.grid[k] = A.cl.inside[k] ? bytes[k] / 255f : 0; s += A.grid[k]; } A.sum = s; }
                ps.act[a.id] = A;
            }
            foreach (var v in sv.veh) { v.tank = Arr(v.tank); if (v.d.sqrMagnitude < .5f) v.d = ps.pd.PlanDir(ps.lay.start); }
            if (sv.drones == null) sv.drones = new List<DroneSave>(); foreach (var dr in sv.drones) { dr.tank = Arr(dr.tank); if (dr.d.sqrMagnitude < .5f) dr.d = ps.lay.pos.ContainsKey("hangar") ? ps.pd.PlanDir(ps.lay.pos["hangar"]) : ps.pd.PlanDir(ps.lay.start); }
            if (sv.cart.owned && sv.cart.d.sqrMagnitude < .5f) sv.cart.d = ps.pd.PlanDir(ps.lay.start);
            CalcFront(ps);
            return ps;
        }

        static void Detach(PState ps)
        {
            var sv = ps.sv; int N = ps.pd.N;
            var cb = new char[N]; var pb = new char[N]; for (int i = 0; i < N; i++) { cb[i] = ps.clean[i] ? '1' : '0'; pb[i] = ps.paid[i] ? '1' : '0'; }
            sv.clean = new string(cb); sv.paid = new string(pb);
            sv.bld = ps.bld.ToList(); sv.upg = ps.upg.Select(k => new KV { k = k.Key, v = k.Value }).ToList();
            sv.act = new List<ActSave>();
            foreach (var kv in ps.act) { var A = kv.Value; var by = new byte[A.grid.Length]; for (int k = 0; k < by.Length; k++) by[k] = (byte)Mathf.Clamp(Mathf.RoundToInt(A.grid[k] * 255), 0, 255); sv.act.Add(new ActSave { id = kv.Key, sum = A.sum, grid = Convert.ToBase64String(by) }); }
            sv.belts = new List<BeltSave>();
            foreach (var kv in ps.belts) { var b = new BeltSave { id = kv.Key }; foreach (var it in kv.Value) { b.s.Add(it.s); b.m.Add(it.m); b.a.Add(it.a); } sv.belts.Add(b); }
        }

        public static void Save()
        {
            if (S == null) return;
            foreach (var ps in P.Values) Detach(ps);
            S.upg = GUpg.Select(k => new KV { k = k.Key, v = k.Value }).ToList();
            S.lastActive = Now();
            string json = JsonUtility.ToJson(S);
            string old = PlayerPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(old)) PlayerPrefs.SetString(Key + "-bak", old);
            PlayerPrefs.SetString(Key, json); PlayerPrefs.Save();
        }

        static SaveData Load()
        {
            foreach (var k in new[] { Key, Key + "-bak" })
            {
                string j = PlayerPrefs.GetString(k, ""); if (string.IsNullOrEmpty(j)) continue;
                try { var s = JsonUtility.FromJson<SaveData>(j); if (s != null && s.saveVersion >= 1 && s.planets != null && s.planets.Any(p => p.pid == "moon")) { if (s.stats == null) s.stats = new Stats(); if (s.upg == null) s.upg = new List<KV>(); s.planets.RemoveAll(p => !GameData.Planets.ContainsKey(p.pid)); return s; } }
                catch (Exception e) { Debug.LogWarning("Lunar Cleanup: Spielstand " + k + " nicht lesbar: " + e.Message); }
            }
            return null;
        }

        public static void ResetAll() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + "-bak"); S = Fresh(); GUpg.Clear(); P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); OnPlanetChanged?.Invoke(); }

        // ================= Upgrades, Werte =================
        public static int Lvl(string id, PState ps = null) { var u = GameData.Up[id]; ps = ps ?? Cur; return u.loc ? (ps.upg.TryGetValue(id, out var l) ? l : 0) : (GUpg.TryGetValue(id, out var g) ? g : 0); }
        public static float Uv(string id, PState ps = null) { var u = GameData.Up[id]; int l = Mathf.Clamp(Lvl(id, ps), 0, u.vals.Length - 1); return (float)u.vals[l]; }
        public static double UCost(string id, PState ps = null) { var u = GameData.Up[id]; ps = ps ?? Cur; return Math.Round(u.baseCost * Math.Pow(u.grow, Lvl(id, ps)) * (u.loc ? ps.P.diff : 1)); }
        public static string UShow(string id, int l) { var u = GameData.Up[id]; return u.show[Mathf.Clamp(l, 0, u.show.Length - 1)]; }
        public static bool BuyUpgrade(string id)
        {
            var u = GameData.Up[id]; var ps = Cur; int l = Lvl(id, ps);
            if (l >= u.max) return Fail("Höchste Stufe erreicht");
            if (u.stage > 0 && Gs(ps) < u.stage) return Fail("Erst Basisstufe " + u.stage);
            double c = UCost(id, ps); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; if (u.loc) ps.upg[id] = l + 1; else GUpg[id] = l + 1;
            Toast(u.name + " Stufe " + (l + 1), "good"); return true;
        }
        public static int Gs(PState ps) => Math.Max(ps.sv.stage, ps.sv.techF);
        public static float RohCap(PState ps) => Uv("rohcap", ps);
        public static float ProdCap(PState ps) => Uv("prodcap", ps);
        public static float SellCap(PState ps) => Uv("sellcap", ps);
        public static float HandCap() => Uv("handcap");
        public static float Sum(float[] a) { float s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
        public static float RohSum(PState ps) { float s = 0; foreach (int m in GameData.Raw) s += ps.sv.roh[m]; return s; }
        public static float ProdSum(PState ps) { float s = 0; foreach (int m in GameData.Prods) s += ps.sv.prod[m]; return s; }
        public static int DoneCount() => S.planets.Count(p => P.ContainsKey(p.pid) && CleanPct(P[p.pid]) >= .999f);
        public static double MatValue(int m, PState ps)
        {
            var M = GameData.Mats[m]; bool port = P.Values.Any(x => x.bld.Contains("port"));
            return M.v * Uv("price", ps) * (M.raw ? 1 : Uv("mqual", ps)) * (port ? 1.2 : 1) * ps.P.diff * (1 + .1 * DoneCount());
        }
        public static void Earn(double cr) { S.credits += cr; S.stats.earned += cr; }
        public static bool Fail(string t) { Toast(t, "bad"); return false; }
        public static string Lacks(double c) => "Dir fehlen noch " + Fmt(c - S.credits) + " Cr";
        public static void Toast(string t, string k = "") { if (!Offline) OnToast?.Invoke(t, k); }
        static void FloatTxt(Vector3 d, string t, string k) { if (!Offline) OnFloat?.Invoke(d, t, k); }

        public static string Fmt(double v)
        {
            double a = Math.Abs(v); string sgn = v < 0 ? "-" : "";
            if (a < 10000) return sgn + Math.Floor(a).ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
            string[] u = { " k", " Mio", " Mrd", " Bio", " Brd" }; double x = a / 1000; int i = 0; while (x >= 1000 && i < u.Length - 1) { x /= 1000; i++; }
            return sgn + x.ToString(x < 100 ? "0.0" : "0", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + u[i];
        }

        // ================= Parzellen, Grenze =================
        public static float CleanPct(PState ps) { int c = 0, t = 0; foreach (var cl in ps.pd.cells) { if (cl.IsBase) continue; t++; if (ps.clean[cl.id]) c++; } return t > 0 ? (float)c / t : 0; }
        static bool IsFront(PState ps, int id)
        {
            var cl = ps.pd.cells[id]; if (cl.IsBase || ps.clean[id]) return false;
            bool nbc = false; foreach (int b in cl.nb) if (ps.clean[b]) { nbc = true; break; }
            if (!nbc) return false;
            if (GameData.Kinds[cl.kind].key == "anomaly" && !ps.bld.Contains("research")) return false;
            return true;
        }
        public static string FrontWhy(PState ps, int id)
        {
            var cl = ps.pd.cells[id];
            if (cl.IsBase) return "Basisgelände"; if (ps.clean[id]) return "Schon sauber";
            if (!cl.nb.Any(b => ps.clean[b])) return "Erst eine angrenzende Parzelle reinigen";
            if (GameData.Kinds[cl.kind].key == "anomaly" && !ps.bld.Contains("research")) return "Forschungsgebiete brauchen eine Forschungsstation";
            return "";
        }
        public static void CalcFront(PState ps) { ps.front.Clear(); for (int i = 0; i < ps.pd.N; i++) if (IsFront(ps, i)) ps.front.Add(i); }
        public static bool Workable(PState ps, int id) => ps.act.ContainsKey(id) || ps.front.Contains(id);
        public static float ParcelYield(Cell cl) => GameData.Dirt[cl.type].amt * GameData.Kinds[cl.kind].amt;
        static Act NewAct(PState ps, int id)
        {
            var cl = ps.pd.cells[id]; var g = new float[GameData.GRID * GameData.GRID];
            for (int k = 0; k < g.Length; k++) g[k] = cl.inside[k] ? 1 : 0;
            return new Act { cl = cl, grid = g, sum = cl.nIn, amt = ParcelYield(cl), D = GameData.Dirt[cl.type], K = GameData.Kinds[cl.kind] };
        }
        public static Act EnsureAct(PState ps, int id)
        {
            if (ps.act.TryGetValue(id, out var a)) return a;
            if (!ps.front.Contains(id)) return null;
            a = NewAct(ps, id); ps.act[id] = a; ps.dirtyCells.Add(id); return a;
        }
        /// <summary>Nur für Tests im Editor: säubert eine Grenzparzelle sofort (der normale Abschluss läuft dann im nächsten Schritt).</summary>
        public static void DevClean(int id)
        {
            var ps = Cur; var a = EnsureAct(ps, id); if (a == null) return;
            float got = 0; for (int k = 0; k < a.grid.Length; k++) { got += a.grid[k]; a.grid[k] = 0; }
            a.sum = 0; ps.dirtyCells.Add(id); int m = a.D.mat; float room = RohCap(ps) - RohSum(ps); if (room > 0) ps.sv.roh[m] += Mathf.Min(room, a.amt);
        }
        /// <summary>Nur für Tests im Editor: reinigt Welle für Welle die Grenze, bis der Anteil erreicht ist.</summary>
        public static void DevCleanTo(float frac)
        {
            var ps = Cur; int guard = 0;
            while (CleanPct(ps) < frac && ps.front.Count > 0 && guard++ < 5000) { int id = ps.front.First(); ps.paid[id] = true; FinishCell(ps, id); }
        }
        public static float Progress(Act a) => 1 - a.sum / a.cl.nIn;

        public static float CleanAt(PState ps, Act a, float x, float y, float r, float k)
        {
            var cl = a.cl; float H = cl.H, cs = cl.cs, lx = x + H, ly = y + H; int G = GameData.GRID;
            if (lx < -r || ly < -r || lx > 2 * H + r || ly > 2 * H + r) return 0;
            float r2 = r * r, removed = 0;
            int gx0 = Math.Max(0, (int)Math.Floor((lx - r) / cs)), gx1 = Math.Min(G - 1, (int)Math.Floor((lx + r) / cs));
            int gy0 = Math.Max(0, (int)Math.Floor((ly - r) / cs)), gy1 = Math.Min(G - 1, (int)Math.Floor((ly + r) / cs));
            for (int gy = gy0; gy <= gy1; gy++)
            {
                float dy = (gy + .5f) * cs - ly;
                for (int gx = gx0; gx <= gx1; gx++)
                {
                    int idx = gy * G + gx; if (!cl.inside[idx]) continue;
                    float dx = (gx + .5f) * cs - lx, d2 = dx * dx + dy * dy; if (d2 >= r2) continue;
                    float o = a.grid[idx]; if (o <= 0) continue;
                    float nv = o * (1 - k * (1 - d2 / r2)); if (nv < .02f) nv = 0; a.grid[idx] = nv; removed += o - nv;
                }
            }
            if (removed > 0) { a.sum = Mathf.Max(0, a.sum - removed); ps.dirtyCells.Add(cl.id); }
            return removed;
        }

        static void FinishCell(PState ps, int id)
        {
            var cl = ps.pd.cells[id];
            if (!ps.clean[id]) ps.nClean++;
            ps.clean[id] = true; ps.act.Remove(id); ps.dirtyCells.Add(id);
            foreach (var v in ps.sv.veh) if (v.cell == id && (v.mode == "drive" || v.mode == "clean")) { v.mode = "seek"; v.cell = -1; }
            if (!ps.paid[id])
            {
                ps.paid[id] = true;
                double bonus = ParcelYield(cl) * GameData.Mats[GameData.Dirt[cl.type].mat].v * .3 * Uv("bonus", ps) * ps.P.diff; Earn(bonus);
                if (GameData.Kinds[cl.kind].key == "rich") { int m = GameData.Dirt[cl.type].mat; float x = Mathf.Min(ParcelYield(cl) * .2f, RohCap(ps) - RohSum(ps)); if (x > 0) ps.sv.roh[m] += x; }
                FloatTxt(ps.pd.cells[id].c, "+" + Fmt(bonus) + " Cr", "big");
            }
            ps.front.Remove(id); foreach (int b in cl.nb) if (IsFront(ps, b)) ps.front.Add(b);
            foreach (int b in cl.nb) ps.dirtyCells.Add(b);
        }

        // ================= Handgerät =================
        public static float HandSum() => Sum(Cur.sv.hand);
        static int strokeCell = -1, lastCell = -1; static float fullWarnT;
        public static void BeginStroke() { strokeCell = -1; }
        public static bool CanClean(Vector3 d) { var ps = Cur; int c = ps.pd.CellOf(d, lastCell); return Workable(ps, c); }
        /// <summary>Wischen an der Stelle d (Einheitsvektor). Liefert die aufgenommene Menge.</summary>
        public static float Stroke(Vector3 d, float dt, out int mat)
        {
            mat = -1; var ps = Cur; var pd = ps.pd;
            if (HandCap() - HandSum() <= .05f)
            {
                if (Time.realtimeSinceStartup - fullWarnT > 2.5f) { fullWarnT = Time.realtimeSinceStartup; Toast(ps.sv.cart.owned ? "Sammelbehälter voll – der Sammelwagen kommt" : "Sammelbehälter voll – tippe unten links auf den Behälter", "bad"); }
                return 0;
            }
            int c0 = pd.CellOf(d, lastCell); lastCell = c0;
            if (strokeCell < 0 || !Workable(ps, strokeCell)) strokeCell = Workable(ps, c0) ? c0 : -1;
            if (strokeCell < 0) return 0;
            var a = EnsureAct(ps, strokeCell); if (a == null) return 0;
            var l = pd.ToLocal(a.cl, d); float r = Uv("radius");
            if (Mathf.Abs(l.x) > a.cl.H + r || Mathf.Abs(l.y) > a.cl.H + r) return 0;
            float k = 1 - Mathf.Exp(-.35f * Uv("power") * Mathf.Min(dt, .1f) * 60f / (a.D.hard * a.K.hard));
            float got = CleanAt(ps, a, l.x, l.y, r, k);
            if (got <= 0) return 0;
            float amt = Mathf.Min(got / a.cl.nIn * a.amt, HandCap() - HandSum());
            ps.sv.hand[a.D.mat] += amt; mat = a.D.mat; S.stats.handM2 += got / a.cl.nIn * pd.CellArea(a.cl);
            return amt;
        }
        public static float StrokeProgress() { var ps = Cur; return strokeCell >= 0 && ps.act.TryGetValue(strokeCell, out var a) ? Progress(a) : -1; }

        public static void UnloadHand()
        {
            var ps = Cur; float tot = HandSum(); if (tot < .05f) { Fail("Dein Sammelbehälter ist leer"); return; }
            var c = ps.sv.cart;
            if (c.owned && c.mode == "wait") { LoadCart(ps); return; }
            string best = null; float bf = 0;
            foreach (var s in GameData.Stations) if (ps.bld.Contains(s)) { float f = StBuf(ps, s) - Sum(ps.st[s].buf); if (f > bf) { bf = f; best = s; } }
            float free; float[] dst; string name;
            if (best != null && bf >= 1) { free = bf; dst = ps.st[best].buf; name = GameData.Bld[best].name; }
            else { free = RohCap(ps) - RohSum(ps); dst = ps.sv.roh; name = GameData.Bld["roh"].name; best = "roh"; }
            if (free < .05f) { Fail("Kein Platz – Trichter und Lager sind voll"); return; }
            float amt = Mathf.Min(tot, free), k = amt / tot;
            for (int m = 0; m < NM; m++) { float x = ps.sv.hand[m] * k; ps.sv.hand[m] -= x; if (ps.sv.hand[m] < 1e-6f) ps.sv.hand[m] = 0; dst[m] += x; }
            S.stats.handUnloads++;
            FloatTxt(ps.pd.PlanDir(ps.lay.pos[best]), "+" + Fmt(amt) + " entladen", "mat");
            Toast(Fmt(amt) + " E in " + name + (amt < tot - .05f ? " · Rest bleibt im Behälter" : ""), "good");
        }

        // ================= Sammelwagen =================
        public static double CartCost(PState ps) => Math.Round(120 * Math.Pow(ps.P.diff, .85));
        public static float CartCap() => HandCap() * 2;
        public static bool BuyCart()
        {
            var ps = Cur; if (ps.sv.cart.owned) return false; double c = CartCost(ps); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.sv.cart.owned = true; ps.sv.cart.d = ps.pd.PlanDir(ps.lay.start); ps.sv.cart.mode = "park"; Toast("Sammelwagen gekauft – er holt deinen vollen Behälter ab", "good"); return true;
        }
        static void LoadCart(PState ps)
        {
            var c = ps.sv.cart; float tot = HandSum(), free = CartCap() - Sum(c.tank); float amt = Mathf.Min(tot, free); if (amt <= .05f) { Fail("Der Sammelwagen ist voll"); return; }
            float k = amt / tot; for (int m = 0; m < NM; m++) { float x = ps.sv.hand[m] * k; ps.sv.hand[m] -= x; c.tank[m] += x; }
            c.mode = "back"; c.to = ""; S.stats.handUnloads++; Toast(Fmt(amt) + " E in den Sammelwagen", "good");
        }
        static void CartStep(PState ps, float dt)
        {
            var c = ps.sv.cart; if (!c.owned) return; float step = Uv("cartspd") * dt, tot = Sum(c.tank); bool here = !Offline && ps.pid == S.planet; var pd = ps.pd;
            switch (c.mode)
            {
                case "park":
                    if (tot > .05f) { c.mode = "back"; break; }
                    if (here && HandSum() >= HandCap() * .95f) { c.mode = "go"; Toast("Der Sammelwagen kommt zu dir", "good"); break; }
                    NavMove(ps, c, pd.PlanDir(ps.bld.Contains("werkstatt") ? ps.lay.shop : ps.lay.start), step, -1); break;
                case "go":
                    if (!here) { c.mode = tot > .05f ? "back" : "park"; break; }
                    if (pd.Arc(c.d, CartGoal) < 3.2f || NavMove(ps, c, CartGoal, step, -1)) { c.mode = "wait"; c.wt = 0; Toast("Der Sammelwagen ist da – tippe auf deinen Behälter", "good"); }
                    break;
                case "wait":
                    if (!here) { c.mode = tot > .05f ? "back" : "park"; break; }
                    c.wt += dt; if (pd.Arc(c.d, CartGoal) > 8) { c.mode = "go"; break; }
                    if ((HandSum() < .05f && c.wt > 8) || c.wt > 120) c.mode = tot > .05f ? "back" : "park";
                    break;
                case "back":
                    if (tot < .05f) { c.mode = "park"; c.to = ""; break; }
                    if (string.IsNullOrEmpty(c.to) || (c.to != "roh" && !ps.bld.Contains(c.to)))
                    {
                        c.to = "roh"; float bf = 1; foreach (var s in GameData.Stations) if (ps.bld.Contains(s)) { float f = StBuf(ps, s) - Sum(ps.st[s].buf); if (f > bf) { bf = f; c.to = s; } }
                    }
                    { var p = ps.lay.pos[c.to]; var dropAt = c.to == "roh" ? p + new Vector2(-GameData.Bld["roh"].fp - 1.6f, 0) : StLoc(ps, c.to, 6.4f, 0); if (NavMove(ps, c, pd.PlanDir(dropAt), step, -1)) { c.mode = "drop"; c.wt = 0; } }
                    break;
                case "drop":
                    {
                        float[] dst = c.to == "roh" ? ps.sv.roh : ps.st[c.to].buf; float free = c.to == "roh" ? RohCap(ps) - RohSum(ps) : StBuf(ps, c.to) - Sum(dst);
                        float amt = Mathf.Min(tot, free, 30 * dt); if (amt > 0) { float k = amt / tot; for (int m = 0; m < NM; m++) { float x = c.tank[m] * k; c.tank[m] -= x; dst[m] += x; } }
                        c.wt += dt; if (Sum(c.tank) < .05f) { Array.Clear(c.tank, 0, NM); c.mode = "park"; } else if (free <= .05f && c.wt > 4) { c.to = ""; c.mode = "back"; }
                        break;
                    }
            }
        }

        // ================= Basis =================
        public static double StageCost(PState ps) => ps.sv.stage < GameData.Stages.Count ? Math.Round(GameData.Stages[ps.sv.stage].cost * ps.P.diff) : double.PositiveInfinity;
        public static bool BuyStage()
        {
            var ps = Cur; if (ps.sv.stage >= GameData.Stages.Count) return false; double c = StageCost(ps); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.sv.stage++; GrantAuto(ps); ps.baseVersion++; ps.navKey = null;
            Toast("Basisstufe " + ps.sv.stage + ": " + GameData.Stages[ps.sv.stage - 1].name, "good"); return true;
        }
        static void GrantAuto(PState ps) { foreach (var id in GameData.BOrder) { var B = GameData.Bld[id]; if (B.auto && B.stage <= Gs(ps)) ps.bld.Add(id); } }
        public static string BuildWhy(PState ps, string id)
        {
            var B = GameData.Bld[id]; if (ps.bld.Contains(id)) return "Steht schon";
            if (Gs(ps) < B.stage) return "Erst Basisstufe " + B.stage;
            if (!string.IsNullOrEmpty(B.research) && !ResDone(B.research)) return "Erst erforschen: " + (GameData.Res.Find(r => r.id == B.research)?.name ?? B.research);
            if (id == "lifter" || id == "outpost") return "Folgt in der Unity-Version";
            if (B.geo && GeoF(ps.pid) <= 0) return "Hier gibt es keine nutzbare Erdwärme";
            return "";
        }
        public static double BuildCost(PState ps, string id) => Math.Round(GameData.Bld[id].cost * ps.P.diff);
        public static bool Build(string id)
        {
            var ps = Cur; string why = BuildWhy(ps, id); if (why != "") return Fail(why);
            double c = BuildCost(ps, id); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.bld.Add(id); ps.baseVersion++; ps.navKey = null; if (id == "research") CalcFront(ps);
            Toast(GameData.Bld[id].name + " steht", "good"); return true;
        }

        // ================= Stationen, Bänder, Maschinen =================
        public static float StBuf(PState ps, string s) => GameData.Bld[s].st.buf * Uv("ubuf", ps);
        public static int StDocks(PState ps, string s) => GameData.Bld[s].st.docks + (int)Uv("udock", ps);
        public static Vector2 StLoc(PState ps, string s, float a, float b) { float y = ps.lay.yaw.TryGetValue(s, out var yy) ? yy : 0; float c = Mathf.Cos(y), n = Mathf.Sin(y); var p = ps.lay.pos[s]; return new Vector2(p.x + a * c - b * n, p.y + a * n + b * c); }
        public static Vector2 DockPos(PState ps, string s, int k) => StLoc(ps, s, 2.6f, -(k - (GameData.Bld[s].st.docks - 1) / 2f) * 1.8f);
        public static bool BeltLive(PState ps, BeltDef b) => ps.bld.Contains(b.from) && ps.bld.Contains(b.to) && (b.stage == 0 || Gs(ps) >= b.stage);
        static float Packet(PState ps) => 8 * Uv("bcap", ps);
        static float BeltSpeed(PState ps) => 2.4f * Uv("bspeed", ps);
        public static bool Pressable(PState ps, int m) => ps.line.presse.Any(r => r.inp == m);
        static bool SmeltOn(PState ps) => ps.bld.Contains("schmelze");
        static bool RawRoute(PState ps, int m) => (ps.bld.Contains("presse") && Pressable(ps, m)) || (ps.bld.Contains("raff") && ps.line.raff.Any(r => r.inp == m));
        static int MachBatch(PState ps, string id) => id == "presse" ? (int)Uv("pvol", ps) : 1;
        public static float MachInCap(PState ps, string id) => GameData.Mach[id].y * Uv("mcap", ps) * MachBatch(ps, id);
        public static float MachOutCap(PState ps, string id) => GameData.Mach[id].z * Uv("mcap", ps) * MachBatch(ps, id);

        static float DestFree(PState ps, BeltDef b)
        {
            if (b.to == "roh") return RohCap(ps) - RohSum(ps);
            if (b.to == "prod") return ProdCap(ps) - ProdSum(ps);
            if (b.to == "sell") return SellCap(ps) - Sum(ps.sv.sellBuf);
            if (ps.mach.ContainsKey(b.to)) return MachInCap(ps, b.to) - Sum(ps.mach[b.to].inp);
            return 0;
        }

        static bool Pick(float[] o, IEnumerable<int> keys, float min, float cap, bool whole, out int bm, out float ba)
        {
            bm = -1; ba = 0; foreach (int m in keys) if (o[m] >= min && (bm < 0 || o[m] > o[bm])) bm = m;
            if (bm < 0) return false; float a = Mathf.Min(cap, o[bm]); if (whole) a = Mathf.Floor(a + 1e-6f); if (a < min) { bm = -1; return false; }
            o[bm] -= a; if (o[bm] < 1e-6f) o[bm] = 0; ba = a; return true;
        }

        static bool BeltTake(PState ps, BeltDef b, float lim, out int m, out float a)
        {
            m = -1; a = 0; float P0 = Mathf.Min(Packet(ps), lim), PB = Mathf.Min(Mathf.Max(1, Mathf.Round(Packet(ps) / 8)), Mathf.Floor(lim + 1e-6f)); if (P0 <= 1e-6f) return false;
            var sv = ps.sv;
            if (ps.st.ContainsKey(b.from))
            {
                var buf = ps.st[b.from].buf; var keys = Enumerable.Range(0, NM).Where(x => buf[x] > 0 && (b.to != "presse" || Pressable(ps, x))).ToList();
                if (Pick(buf, keys, Mathf.Min(1, P0), P0, false, out m, out a)) return true;
                float tot = 0; foreach (int x in keys) tot += buf[x]; return tot > 0 && tot < 1 && Pick(buf, keys, 1e-6f, P0, false, out m, out a);
            }
            if (b.from == "roh" && b.to == "sell") { bool over = RohSum(ps) >= RohCap(ps) * .97f; return Pick(sv.roh, GameData.Raw.Where(x => !RawRoute(ps, x) || over), 1, P0, false, out m, out a); }
            if (b.from == "prod" && b.to == "sell") return PB >= 1 && Pick(sv.prod, GameData.Prods, 1, PB, true, out m, out a);
            if (b.from == "roh")
            {
                List<Recipe> rl = b.to == "presse" ? ps.line.presse : b.to == "raff" ? ps.line.raff : null; if (rl == null) return false;
                var M = ps.mach[b.to]; int best = -1; float bs = -1;
                foreach (var r in rl) { float x = sv.roh[r.inp]; if (x < 1) continue; float sc = x / (1 + M.inp[r.inp]); if (sc > bs) { bs = sc; best = r.inp; } }
                return best >= 0 && Pick(sv.roh, new[] { best }, 1, P0, false, out m, out a);
            }
            if (ps.mach.ContainsKey(b.from))
            {
                var o = ps.mach[b.from].outp; var outs = ps.line.Of(b.from).Select(r => r.outp).Distinct().ToList();
                if (b.from == "presse")
                {
                    var smIns = ps.line.schmelze.Select(r => r.inp).ToList();
                    if (b.to == "schmelze") return SmeltOn(ps) && Pick(o, outs.Where(x => smIns.Contains(x)), 1, PB, true, out m, out a);
                    return Pick(o, outs.Where(x => !(SmeltOn(ps) && smIns.Contains(x))), 1, PB, true, out m, out a);
                }
                return Pick(o, outs, 1, PB, true, out m, out a);
            }
            return false;
        }

        static bool BeltAccept(PState ps, BeltDef b, int m, float a)
        {
            var sv = ps.sv;
            if (b.to == "roh") { if (RohSum(ps) + a > RohCap(ps) + 1e-6f) return false; sv.roh[m] += a; return true; }
            if (b.to == "prod") { if (ProdSum(ps) + a > ProdCap(ps) + 1e-6f) return false; sv.prod[m] += a; return true; }
            if (b.to == "sell") { if (Sum(sv.sellBuf) + a > SellCap(ps) + 1e-6f) return false; sv.sellBuf[m] += a; return true; }
            if (ps.mach.ContainsKey(b.to))
            {
                var M = ps.mach[b.to]; if (!ps.line.Of(b.to).Any(r => r.inp == m)) return false;
                if (Sum(M.inp) + a > MachInCap(ps, b.to) + 1e-6f) return false; M.inp[m] += a; return true;
            }
            return false;
        }

        static bool BeltStep(PState ps, BeltDef b, float dt, float f)
        {
            if (!BeltLive(ps, b)) return false;
            var items = ps.belts[b.id]; float L = b.L, v = BeltSpeed(ps) * Mathf.Max(.25f, f) * ps.bm; bool moved = false;
            for (int i = 0; i < items.Count; i++) { var it = items[i]; float lim = i == 0 ? L : items[i - 1].s - BSP, ns = Mathf.Min(it.s + v * dt, lim); if (ns > it.s + 1e-4f) moved = true; if (ns > it.s) it.s = ns; }
            bool jam = false;
            while (items.Count > 0 && items[0].s >= L - 1e-3f) { var h = items[0]; if (BeltAccept(ps, b, h.m, h.a)) items.RemoveAt(0); else { jam = true; break; } }
            if (items.Count == 0 || items[items.Count - 1].s >= BSP)
            {
                float tr = 0; foreach (var o in ps.lay.belts) if (o.to == b.to && BeltLive(ps, o)) foreach (var it in ps.belts[o.id]) tr += it.a;
                if (BeltTake(ps, b, DestFree(ps, b) - tr, out int m, out float a)) items.Add(new BeltItem { s = 0, m = m, a = a });
            }
            ps.jam[b.id] = jam; return moved;
        }

        static void MachStep(PState ps, string id, float dt, float f)
        {
            var M = ps.mach[id]; var rec = ps.line.Of(id);
            if (!ps.bld.Contains(id)) return;
            if (M.cur >= rec.Count) M.cur = -1;
            float outSum = Sum(M.outp);
            if (M.cur < 0)
            {
                for (int r = 0; r < rec.Count; r++)
                {
                    var rc = rec[r];
                    if (M.inp[rc.inp] >= rc.n - 1e-6f && outSum + rc.k <= MachOutCap(ps, id))
                    {
                        M.cur = r; M.prog = 0;
                        M.bat = Math.Max(1, Math.Min(MachBatch(ps, id), Math.Min((int)Math.Floor((M.inp[rc.inp] + 1e-6f) / rc.n), (int)Math.Floor((MachOutCap(ps, id) - outSum + 1e-6f) / rc.k))));
                        break;
                    }
                }
                if (M.cur < 0) return;
            }
            var c = rec[M.cur];
            if (f < .05f) return;
            M.prog += dt * Uv("mspeed", ps) * f * ps.mm;
            if (M.prog >= c.t)
            {
                int bt = Math.Max(1, Math.Min(M.bat, (int)Math.Floor((M.inp[c.inp] + 1e-6f) / c.n)));
                M.inp[c.inp] = Mathf.Max(0, M.inp[c.inp] - c.n * bt); M.outp[c.outp] += c.k * bt; M.made += c.k * bt; M.lastOut = c.outp; M.cur = -1; M.prog = 0; M.bat = 1;
                string sh = GameData.Mats[c.outp].shape; if (sh == "ingot") S.stats.barren += c.k * bt; if (sh == "brick") S.stats.bricks += c.k * bt;
            }
        }
        public static string MachStatus(PState ps, string id)
        {
            if (!ps.bld.Contains(id)) return "Nicht gebaut"; var M = ps.mach[id];
            if (M.cur >= 0) return ps.ef < .05f ? "Keine Energie" : ps.ef < .99f ? "Arbeitet langsamer – zu wenig Energie" : "Arbeitet"; return Sum(M.outp) + 1 > MachOutCap(ps, id) ? "Ausgang voll" : "Wartet auf Material";
        }

        // ================= Rakete =================
        public static float RocketTime(PState ps) => 12 / Uv("rspeed", ps);
        public static double CargoValue(PState ps)
        {
            double b = 0; int kinds = 0; for (int m = 0; m < NM; m++) { float u = ps.sv.sellBuf[m]; if (u <= 1e-6f) continue; kinds++; b += u * MatValue(m, ps); }
            return b * Math.Min(1.4, 1 + .08 * Math.Max(0, kinds - 1));
        }
        public static void LaunchRocket() { var ps = Cur; if (ps.sv.rk > 0) { Fail("Die Rakete ist noch unterwegs"); return; } if (Sum(ps.sv.sellBuf) <= 1e-6f) { Fail("Die Rakete ist leer"); return; } ps.sv.rkGo = true; }
        /// <summary>Direktverkauf einer Ware aus Rohstoff- oder Produktlager (wie im Browser: sofort, ohne Rakete).</summary>
        public static double SellMat(int m, bool silent = false)
        {
            var ps = Cur; bool raw = GameData.Mats[m].raw; var store = raw ? ps.sv.roh : ps.sv.prod; float u = store[m]; if (!raw) u = Mathf.Floor(u + 1e-6f);
            if (u <= 0) { if (!silent) Fail("Nichts auf Lager"); return 0; }
            double c = u * MatValue(m, ps); store[m] -= u; Earn(c); ps.sv.soldP += c; S.stats.sold += c;
            if (!silent) { OnRocket?.Invoke(ps.pid, c); Toast(GameData.Mats[m].name + " verkauft · +" + Fmt(c) + " Cr", "good"); }
            return c;
        }
        public static void SellAll()
        {
            double c = 0; for (int m = 0; m < NM; m++) c += SellMat(m, true);
            if (c <= 0) { Fail("Die Lager sind leer"); return; }
            OnRocket?.Invoke(Cur.pid, c); Toast("Alles verkauft · +" + Fmt(c) + " Cr", "good");
        }
        static void SellStep(PState ps, float dt)
        {
            var sv = ps.sv; if (!ps.bld.Contains("sell")) return;
            if (sv.rk > 0) { sv.rk = Mathf.Max(0, sv.rk - dt); return; }
            float tot = Sum(sv.sellBuf); if (tot <= 1e-6f) { sv.rkGo = false; return; }
            if (tot < SellCap(ps) * .98f && !sv.rkGo) return;
            bool man = sv.rkGo; sv.rkGo = false; double val = CargoValue(ps); Array.Clear(sv.sellBuf, 0, NM);
            Earn(val); sv.soldP += val; S.stats.sold += val; sv.nRockets++; sv.rk = RocketTime(ps);
            if (!Offline && ps.pid == S.planet) { OnRocket?.Invoke(ps.pid, val); if (man) Toast("Rakete gestartet · +" + Fmt(val) + " Cr", "good"); }
        }

        // ================= Fahrzeuge =================
        public static double VehCost(PState ps, string t) { int n = ps.sv.veh.Count(v => v.type == t); return Math.Round(GameData.Veh[t].cost * ps.P.diff * Math.Pow(1.22, n)); }
        public static int VehSlots(PState ps) => (Gs(ps) >= 2 ? 4 : 0) + (ps.bld.Contains("werkstatt") ? 4 : 0) + (ps.sv.stage >= 4 ? 4 : 0) + (ps.sv.stage >= 5 ? 6 : 0) + (int)Uv("slots", ps) + DoneCount();
        public static string VehWhy(PState ps, string t)
        {
            var T = GameData.Veh[t]; if (Gs(ps) < T.stage) return "Erst Basisstufe " + T.stage;
            if (!string.IsNullOrEmpty(T.planet) && !P.ContainsKey(T.planet)) return "Erst " + GameData.Planets[T.planet].name + " erreichen";
            if (ps.sv.veh.Count >= VehSlots(ps)) return "Keine freien Fahrzeugplätze";
            return "";
        }
        public static bool BuyVehicle(string t)
        {
            var ps = Cur; string why = VehWhy(ps, t); if (why != "") return Fail(why); double c = VehCost(ps, t); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; var p0 = ps.bld.Contains("werkstatt") ? ps.lay.shop : ps.lay.start;
            ps.sv.veh.Add(new VehSave { id = ps.sv.nextVeh++, type = t, d = ps.pd.PlanDir(p0), tank = new float[NM] });
            Toast(GameData.Veh[t].name + " ist im Einsatz", "good"); return true;
        }
        static float TankCap(PState ps, VehSave v) => GameData.Veh[v.type].tank * Uv("vtank", ps);
        static bool CanUse(VehSave v, string s) => GameData.Bld[s].st.acc.Contains(GameData.Veh[v.type].size);

        static int ChooseCell(PState ps, VehSave v)
        {
            var T = GameData.Veh[v.type]; int best = -1; float bs = 1e9f; var pd = ps.pd;
            foreach (var kv in ps.act)
            {
                var a = kv.Value; if (a.K.tier > T.tier || a.sum < a.cl.nIn * GameData.DONE) continue;
                int crowd = 0; foreach (var o in ps.sv.veh) if (o != v && o.cell == kv.Key && (o.mode == "drive" || o.mode == "clean")) crowd++;
                if (crowd >= (a.K.tier > 0 ? 4 : 3)) continue;
                float left = a.sum / a.cl.nIn, team = crowd > 0 && left > .35f ? -6 * Math.Min(crowd, 2) - (a.K.hard > 1.3f ? 6 : 0) : crowd * 14;
                float sc = pd.Arc(v.d, a.cl.c) + team - (v.last == kv.Key ? 8 : 0); if (sc < bs) { bs = sc; best = kv.Key; }
            }
            foreach (int id in ps.front)
            {
                if (ps.act.ContainsKey(id)) continue; var cl = pd.cells[id]; if (GameData.Kinds[cl.kind].tier > T.tier) continue;
                float sc = pd.Arc(v.d, cl.c) + 6 - (GameData.Kinds[cl.kind].key == "rich" ? 6 : 0); if (sc < bs) { bs = sc; best = id; }
            }
            return best;
        }
        static int PickGridTarget(PState ps, Act a, VehSave v)
        {
            var cl = a.cl; var me = ps.pd.ToLocal(cl, v.d); int best = -1; float bs = -1e9f; int G = GameData.GRID;
            for (int k = 0; k < 44; k++)
            {
                int i = Rng.Next(G * G); float g = a.grid[i]; if (g < .15f) continue;
                float x = -cl.H + ((i % G) + .5f) * cl.cs, y = -cl.H + ((i / G) + .5f) * cl.cs, sc = g - Mathf.Sqrt((x - me.x) * (x - me.x) + (y - me.y) * (y - me.y)) * .04f;
                if (sc > bs) { bs = sc; best = i; }
            }
            if (best < 0) { float bv = 0; for (int i = 0; i < a.grid.Length; i++) if (a.grid[i] > bv) { bv = a.grid[i]; best = i; } }
            return best;
        }
        static string ChooseStation(PState ps, VehSave v)
        {
            string best = null; float bd = 1e9f;
            foreach (var s in GameData.Stations)
            {
                if (!ps.bld.Contains(s) || !CanUse(v, s)) continue;
                int crowd = ps.sv.veh.Count(o => o != v && o.sid == s && (o.mode == "toUnload" || o.mode == "queue" || o.mode == "unload")); int over = Math.Max(0, crowd - StDocks(ps, s) + 1);
                float d = ps.pd.Arc(v.d, ps.pd.PlanDir(ps.lay.pos[s])) + Sum(ps.st[s].buf) / StBuf(ps, s) * 40 + over * 14; if (d < bd) { bd = d; best = s; }
            }
            return best;
        }
        public static Vector2 ParkPos(PState ps, VehSave v) { int i = ps.sv.veh.IndexOf(v); return ps.lay.park + new Vector2((i % 4) * 1.8f, (i / 4) * 1.8f); }

        static void VehStep(PState ps, VehSave v, float dt, float f)
        {
            var T = GameData.Veh[v.type]; var pd = ps.pd; float bf = T.batt > 0 && v.bat >= 0 && v.bat <= 0 ? .3f : 1;
            float hf = HomeF(ps, v), spd = T.speed * Uv("vspeed", ps) * Mathf.Max(.25f, f) * (hf > 1 ? 1 : ps.vm) * bf, tank = Sum(v.tank), cap = TankCap(ps, v); v.busy = false; v.charging = false;
            switch (v.mode)
            {
                case "seek":
                    {
                        if (tank >= cap * .98f || BattLow(ps, v)) { v.mode = "toUnload"; break; }
                        int c = ChooseCell(ps, v);
                        if (c < 0) { if (tank > .5f) { v.mode = "toUnload"; break; } NavMove(ps, v, pd.PlanDir(ParkPos(ps, v)), spd * .7f * dt, v.id); break; }
                        if (EnsureAct(ps, c) == null) break; v.cell = c; v.ti = -1; v.mode = "drive"; break;
                    }
                case "drive":
                case "clean":
                    {
                        if (!ps.act.TryGetValue(v.cell, out var a) || a.sum < a.cl.nIn * GameData.DONE) { v.mode = "seek"; v.cell = -1; break; }
                        if (tank >= cap || BattLow(ps, v)) { v.mode = "toUnload"; break; }
                        v.busy = true; BattUse(ps, v, dt);
                        if (v.ti < 0 || a.grid[v.ti] < .05f) { v.ti = PickGridTarget(ps, a, v); if (v.ti < 0) { v.mode = "seek"; break; } }
                        var gdir = pd.GridDir(a.cl, v.ti);
                        bool arrived = v.mode == "drive" ? NavMove(ps, v, gdir, spd * dt, v.id) : a.cl.ring <= 4 ? NavMove(ps, v, gdir, spd * dt * .6f, v.id) : MoveDir(ref v.d, gdir, spd * dt * .6f, pd.R);
                        if (v.mode == "drive") { var dl = pd.ToLocal(a.cl, v.d); if (arrived || dl.magnitude < a.cl.H * .9f) v.mode = "clean"; break; }
                        v.ph += dt * 4; var loc = pd.ToLocal(a.cl, v.d); float wg = arrived ? .4f : 0;
                        float k = 1 - Mathf.Exp(-T.power * VEH_BUFF * hf * Uv("vpower", ps) * (PowOK(ps) ? 1 : .3f) * dt * (arrived ? 1 : .5f) / (a.D.hard * a.K.hard));
                        float rem = CleanAt(ps, a, loc.x + Mathf.Cos(v.ph) * wg, loc.y + Mathf.Sin(v.ph) * wg, T.radius * Uv("vradius", ps), k);
                        if (rem > 0) { float amt = rem / a.cl.nIn * a.amt; v.tank[a.D.mat] += amt; v.coll += amt; }
                        v.last = v.cell; break;
                    }
                case "toUnload":
                    {
                        if (v.drone >= 0) { v.mode = "wait"; break; }
                        var s = ChooseStation(ps, v);
                        if (s == null) { NavMove(ps, v, pd.PlanDir(ParkPos(ps, v)), spd * .7f * dt, v.id); break; }
                        v.sid = s; v.busy = true; BattUse(ps, v, dt); if (NavMove(ps, v, pd.PlanDir(StLoc(ps, s, 6.4f, 0)), spd * dt, v.id)) v.mode = "queue"; break;
                    }
                case "queue":
                    {
                        var s = v.sid; if (string.IsNullOrEmpty(s) || !ps.bld.Contains(s)) { v.mode = "toUnload"; break; }
                        var st = ps.st[s]; int n = StDocks(ps, s); if (st.docks == null || st.docks.Length != n) { var nd = new int[n]; if (st.docks != null) Array.Copy(st.docks, nd, Math.Min(n, st.docks.Length)); st.docks = nd; }
                        int k = Array.IndexOf(st.docks, v.id); if (k < 0) { k = Array.IndexOf(st.docks, 0); if (k >= 0) st.docks[k] = v.id; }
                        if (k < 0) { v.qT += dt; if (v.qT > 8) { v.qT = 0; v.sid = null; v.mode = "toUnload"; } break; }
                        v.qT = 0; v.busy = true; if (MoveDir(ref v.d, pd.PlanDir(DockPos(ps, s, k)), spd * .6f * dt, pd.R)) v.mode = "unload"; break;
                    }
                case "unload":
                    {
                        var s = v.sid; if (string.IsNullOrEmpty(s) || !ps.bld.Contains(s)) { v.mode = "toUnload"; break; }
                        var st = ps.st[s]; float free = StBuf(ps, s) - Sum(st.buf); bool charged = true;
                        if (T.batt > 0) { float bc = BattCap(ps, v), b0 = Bat(ps, v); if (b0 < bc) { v.bat = Mathf.Min(bc, b0 + bc / 16 * Uv("urate", ps) * Mathf.Max(.25f, f) * dt); v.charging = true; } else v.bat = b0; charged = v.bat >= bc * .97f; }
                        if (free <= .01f && tank > .01f) { v.fullT += dt; if (v.fullT > 4 && !(v.charging && !charged)) { FreeDock(st, v); v.fullT = 0; v.sid = null; v.mode = "toUnload"; } break; }
                        v.fullT = 0; float amt = Mathf.Min(GameData.Bld[s].st.rate * Uv("urate", ps) * dt, Mathf.Min(free, tank)), f0 = tank > 0 ? amt / tank : 0;
                        for (int m = 0; m < NM; m++) { float x = v.tank[m] * f0; v.tank[m] -= x; st.buf[m] += x; }
                        if (Sum(v.tank) < .01f && !charged) { Array.Clear(v.tank, 0, NM); break; }
                        if (Sum(v.tank) < .01f) { Array.Clear(v.tank, 0, NM); FreeDock(st, v); v.mode = "seek"; v.sid = null; if (v.coll >= 1) FloatTxt(pd.PlanDir(ps.lay.pos[s]), "+" + Fmt(v.coll) + " entladen", "mat"); v.coll = 0; }
                        break;
                    }
                case "wait":
                    if (v.drone < 0) v.mode = tank >= cap * .98f ? "toUnload" : "seek"; break;
                default: v.mode = "seek"; break;
            }
        }

        // ================= Drohnen =================
        public static float DroneCap(PState ps) => GameData.DroneCap0 * Uv("dcap", ps);
        public static float DroneSpeed(PState ps) => GameData.DroneSpeed0 * Uv("dspeed", ps);
        public static double DroneCost(PState ps) => Math.Round(2500 * ps.P.diff * Math.Pow(1.35, ps.sv.drones.Count));
        public static Vector3 HangarDir(PState ps) => ps.pd.PlanDir(ps.lay.pos["hangar"]);
        public static bool BuyDrone()
        {
            var ps = Cur; if (!ps.bld.Contains("hangar")) return Fail("Erst den Drohnenhangar bauen"); if (ps.sv.drones.Count >= GameData.DroneMax) return Fail("Hangar ist voll");
            double c = DroneCost(ps); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c;
            ps.sv.drones.Add(new DroneSave { id = ps.sv.nextDrone++, d = HangarDir(ps), tank = new float[NM] }); Toast("Transportdrohne startklar", "good"); return true;
        }
        static string DroneStation(PState ps, string not)
        {
            string best = null; float bf = 10; foreach (var s in GameData.Stations) { if (!ps.bld.Contains(s) || s == not) continue; float fr = StBuf(ps, s) - Sum(ps.st[s].buf); if (fr > bf) { bf = fr; best = s; } } return best;
        }
        static void DroneStep(PState ps, DroneSave dr, float dt, float f)
        {
            var pd = ps.pd; float R = pd.R; var hd = HangarDir(ps); float spd = DroneSpeed(ps) * Mathf.Max(.25f, f) * ps.vm, cap = DroneCap(ps), load = Sum(dr.tank); dr.busy = false;
            VehSave Tgt() { var v = ps.sv.veh.Find(x => x.id == dr.tgt); return v != null && v.drone == dr.id ? v : null; }
            switch (dr.mode)
            {
                case "idle":
                    {
                        if (!MoveDir(ref dr.d, hd, spd * dt, R)) { dr.busy = true; break; }
                        if (load > .05f) { dr.mode = "drop"; break; }
                        VehSave best = null; float bs = 0;
                        foreach (var v in ps.sv.veh)
                        {
                            if (v.drone >= 0) continue; if (!(v.mode == "seek" || v.mode == "drive" || v.mode == "clean" || v.mode == "toUnload" || v.mode == "wait")) continue;
                            float tk = Sum(v.tank), c = TankCap(ps, v); if (tk < c * .4f) continue; float dist = pd.Arc(v.d, hd); if (dist < GameData.DroneMinDist) continue;
                            float sc = tk / c * dist; if (sc > bs) { bs = sc; best = v; }
                        }
                        if (best != null) { best.drone = dr.id; dr.tgt = best.id; dr.mode = "out"; }
                        break;
                    }
                case "out":
                    { var v = Tgt(); if (v == null) { dr.mode = "back"; dr.tgt = -1; break; } dr.busy = true; if (MoveDir(ref dr.d, v.d, spd * dt, R)) dr.mode = "load"; break; }
                case "load":
                    {
                        var v = Tgt(); if (v == null) { dr.mode = "back"; dr.tgt = -1; break; }
                        dr.d = v.d; dr.busy = true; float tk = Sum(v.tank), amt = Mathf.Min(tk, Mathf.Min(cap - load, GameData.DroneRate * dt));
                        if (amt > 0 && tk > 0) { float k = amt / tk; for (int m = 0; m < NM; m++) { float x = v.tank[m] * k; v.tank[m] -= x; dr.tank[m] += x; } }
                        if (Sum(v.tank) < .05f || cap - Sum(dr.tank) < .05f) { if (Sum(v.tank) < .05f) Array.Clear(v.tank, 0, NM); v.drone = -1; dr.tgt = -1; S.stats.lifts++; dr.sid = DroneStation(ps, null) ?? ""; dr.mode = dr.sid != "" ? "toSt" : "back"; }
                        break;
                    }
                case "toSt":
                    { var s = dr.sid; if (string.IsNullOrEmpty(s) || !ps.bld.Contains(s)) { dr.sid = ""; dr.mode = "back"; break; } dr.busy = true; if (MoveDir(ref dr.d, pd.PlanDir(ps.lay.pos[s]), spd * dt, R)) { dr.mode = "stdrop"; dr.wt = 0; } break; }
                case "stdrop":
                    {
                        var s = dr.sid; if (string.IsNullOrEmpty(s) || !ps.bld.Contains(s)) { dr.mode = "back"; break; } var st = ps.st[s]; dr.busy = true;
                        float free = StBuf(ps, s) - Sum(st.buf); if (load < .05f) { Array.Clear(dr.tank, 0, NM); dr.sid = ""; dr.mode = "back"; break; }
                        if (free <= .01f) { dr.wt += dt; if (dr.wt > 3) { var alt = DroneStation(ps, s); dr.sid = alt ?? ""; dr.mode = alt != null ? "toSt" : "back"; } break; }
                        float amt = Mathf.Min(load, Mathf.Min(free, GameData.DroneRate * dt)), k = amt / load; for (int m = 0; m < NM; m++) { float x = dr.tank[m] * k; dr.tank[m] -= x; st.buf[m] += x; }
                        break;
                    }
                case "back": dr.busy = true; if (MoveDir(ref dr.d, hd, spd * dt, R)) dr.mode = "drop"; break;
                case "drop":
                    {
                        if (load < .05f) { Array.Clear(dr.tank, 0, NM); dr.mode = "idle"; break; } float fr = RohCap(ps) - RohSum(ps); if (fr <= .01f) break;
                        float amt = Mathf.Min(load, Mathf.Min(fr, GameData.DroneRate * dt)), k = amt / load; for (int m = 0; m < NM; m++) { float x = dr.tank[m] * k; dr.tank[m] -= x; ps.sv.roh[m] += x; }
                        break;
                    }
                default: dr.mode = "idle"; break;
            }
        }
        public static string DroneStatus(PState ps, DroneSave dr)
        {
            switch (dr.mode)
            {
                case "idle": return dr.busy ? "Fliegt zum Hangar" : "Bereit im Hangar";
                case "out": { var v = ps.sv.veh.Find(x => x.id == dr.tgt); return v != null ? "Fliegt zu " + GameData.Veh[v.type].name + " #" + v.id : "Fliegt los"; }
                case "load": return "Übernimmt Ladung";
                case "toSt": return "Bringt die Ladung zur " + (ps.bld.Contains(dr.sid) ? GameData.Bld[dr.sid].name : "Station");
                case "stdrop": return dr.wt > 0 && !string.IsNullOrEmpty(dr.sid) ? GameData.Bld[dr.sid].name + " ist voll – wartet" : "Entlädt in die " + (string.IsNullOrEmpty(dr.sid) ? "Station" : GameData.Bld[dr.sid].name);
                case "back": return Sum(dr.tank) > .05f ? "Fliegt zum Hangar (Stationen voll)" : "Fliegt zurück";
                case "drop": return RohCap(ps) - RohSum(ps) <= .01f ? "Rohstofflager voll – wartet" : "Lädt ins Rohstofflager";
            }
            return dr.mode;
        }
        static void FreeDock(StSave st, VehSave v) { if (st.docks == null) return; int k = Array.IndexOf(st.docks, v.id); if (k >= 0) st.docks[k] = 0; }
        public static string VehStatus(VehSave v)
        {
            var ps = Cur; var T = GameData.Veh[v.type];
            if (T.batt > 0 && ps.sv.veh.Contains(v))
            {
                if (v.mode == "unload" && v.charging) return Sum(v.tank) > .01f ? "Entlädt und lädt Akku" : "Lädt Akku · " + Mathf.RoundToInt(Bat(ps, v) / BattCap(ps, v) * 100) + " %";
                if (v.mode == "toUnload" && BattLow(ps, v)) return "Akku fast leer – fährt zum Laden";
                if (v.bat == 0) return "Akku leer – fährt langsam";
            }
            switch (v.mode) { case "seek": return "Sucht Arbeit"; case "drive": return "Fährt zum Einsatz" + (v.cell >= 0 && ps.sv.veh.Contains(v) ? " · noch " + Mathf.RoundToInt(ps.pd.Arc(v.d, ps.pd.cells[v.cell].c)) + " m" : ""); case "clean": return "Reinigt"; case "toUnload": return "Fährt zur Entladestation"; case "queue": return "Dockt an"; case "unload": return "Entlädt"; case "wait": return "Wartet auf Drohne"; }
            return v.mode;
        }

        // ---------- Bewegung auf der Kugel ----------
        public static bool MoveDir(ref Vector3 d, Vector3 t, float step, float R)
        {
            float c = Mathf.Clamp(Vector3.Dot(d, t), -1, 1), ang = Mathf.Acos(c); if (ang * R <= step) { d = t; return true; }
            var w = (t - d * c).normalized; float a = step / R; d = (d * Mathf.Cos(a) + w * Mathf.Sin(a)).normalized; return ang * R - step < .35f;
        }
        static bool NavMove(PState ps, CartSave c, Vector3 t, float step, int id) { var d = c.d; bool r = NavMoveD(ps, ref d, t, step, -999); c.d = d; return r; }
        static bool NavMove(PState ps, VehSave v, Vector3 t, float step, int id) { var d = v.d; bool r = NavMoveD(ps, ref d, t, step, id); v.d = d; return r; }

        const float ZONE = 31; const int NN = 66, NOFF = 33;
        static float SegD(Vector2 q, Vector2 a, Vector2 c) { float dx = c.x - a.x, dy = c.y - a.y, l2 = dx * dx + dy * dy; if (l2 == 0) l2 = 1; float t = Mathf.Clamp01(((q.x - a.x) * dx + (q.y - a.y) * dy) / l2); return Mathf.Sqrt((q.x - a.x - dx * t) * (q.x - a.x - dx * t) + (q.y - a.y - dy * t) * (q.y - a.y - dy * t)); }
        static int NavIdx(float x, float y) => Mathf.Clamp((int)Mathf.Floor(y + NOFF), 0, NN - 1) * NN + Mathf.Clamp((int)Mathf.Floor(x + NOFF), 0, NN - 1);
        static Vector2 NavPt(int k) => new Vector2(k % NN - NOFF + .5f, k / NN - NOFF + .5f);
        static bool[] NavGrid(PState ps)
        {
            string key = string.Join(",", GameData.BOrder.Where(ps.bld.Contains)) + "|" + Gs(ps);
            if (ps.navG != null && ps.navKey == key) return ps.navG;
            var g = new bool[NN * NN]; var blds = GameData.BOrder.Where(ps.bld.Contains).ToList(); var belts = ps.lay.belts.Where(b => BeltLive(ps, b)).ToList();
            for (int j = 0; j < NN; j++) for (int i = 0; i < NN; i++)
                {
                    var q = new Vector2(i - NOFF + .5f, j - NOFF + .5f); bool bl = false;
                    foreach (var id in blds) if ((q - ps.lay.pos[id]).magnitude < GameData.Bld[id].fp + .9f) { bl = true; break; }
                    if (!bl) foreach (var b in belts) { for (int k = 0; k < b.rp.Length - 1; k++) if (SegD(q, b.rp[k], b.rp[k + 1]) < 1.15f) { bl = true; break; } if (bl) break; }
                    g[j * NN + i] = bl;
                }
            ps.navG = g; ps.navKey = key; ps.navCache.Clear(); ps.vnav.Clear(); return g;
        }
        static int NavFree(bool[] g, int k)
        {
            if (!g[k]) return k; var seen = new bool[NN * NN]; var q = new Queue<int>(); q.Enqueue(k); seen[k] = true;
            while (q.Count > 0) { int c = q.Dequeue(); if (!g[c]) return c; int x = c % NN, y = c / NN; foreach (var dd in new[] { 1, -1, NN, -NN }) { int nx = x + (dd == 1 ? 1 : dd == -1 ? -1 : 0), ny = y + (dd == NN ? 1 : dd == -NN ? -1 : 0); if (nx < 0 || ny < 0 || nx >= NN || ny >= NN) continue; int n = ny * NN + nx; if (!seen[n]) { seen[n] = true; q.Enqueue(n); } } }
            return -1;
        }
        static bool NavLine(bool[] g, Vector2 a, Vector2 b) { float d = (b - a).magnitude; int n = Mathf.CeilToInt(d / .35f); for (int i = 0; i <= n; i++) { float t = n > 0 ? (float)i / n : 0; if (g[NavIdx(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t)]) return false; } return true; }
        static List<Vector2> NavPath(PState ps, Vector2 a, Vector2 b)
        {
            var g = NavGrid(ps); int s = NavFree(g, NavIdx(a.x, a.y)), t = NavFree(g, NavIdx(b.x, b.y)); if (s < 0 || t < 0) return null;
            long ck = (long)s * NN * NN + t; if (ps.navCache.TryGetValue(ck, out var cached)) return cached;
            var G = new float[NN * NN]; var from = new int[NN * NN]; var closed = new bool[NN * NN]; for (int i = 0; i < G.Length; i++) { G[i] = 1e9f; from[i] = -1; }
            int tx = t % NN, ty = t / NN; Func<int, float> hh = k => { int dx = Math.Abs(k % NN - tx), dy = Math.Abs(k / NN - ty); return Math.Max(dx, dy) + .414f * Math.Min(dx, dy); };
            var heap = new SortedSet<(float, int)>(); G[s] = 0; heap.Add((hh(s), s)); bool found = false;
            while (heap.Count > 0)
            {
                var top = heap.Min; heap.Remove(top); int c = top.Item2; if (closed[c]) continue; closed[c] = true; if (c == t) { found = true; break; }
                int x = c % NN, y = c / NN;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue; int nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= NN || ny >= NN) continue; int n = ny * NN + nx; if (g[n] || closed[n]) continue;
                        if (dx != 0 && dy != 0 && (g[y * NN + nx] || g[ny * NN + x])) continue;
                        float ng = G[c] + (dx != 0 && dy != 0 ? 1.414f : 1); if (ng < G[n]) { if (G[n] < 1e8f) heap.Remove((G[n] + hh(n), n)); G[n] = ng; from[n] = c; heap.Add((ng + hh(n), n)); }
                    }
            }
            List<Vector2> res = null;
            if (found)
            {
                var cells = new List<int>(); for (int c = t; c >= 0; c = from[c]) cells.Add(c); cells.Reverse(); var pts = cells.Select(NavPt).ToList();
                res = new List<Vector2> { pts[0] }; int i0 = 0; while (i0 < pts.Count - 1) { int j = pts.Count - 1; while (j > i0 + 1 && !NavLine(g, pts[i0], pts[j])) j--; res.Add(pts[j]); i0 = j; }
            }
            if (ps.navCache.Count > 600) ps.navCache.Clear(); ps.navCache[ck] = res; return res;
        }
        static bool NavMoveD(PState ps, ref Vector3 d, Vector3 t, float step, int id)
        {
            var pd = ps.pd; var a = pd.PlanLocal(d); var b = pd.PlanLocal(t); float ra = a.magnitude, rb = b.magnitude;
            if (ra >= ZONE && rb >= ZONE)
            {
                ps.vnav.Remove(id);
                if (ra < 1e3f && rb < 1e3f && SegD(Vector2.zero, a, b) < ZONE - .5f)
                {
                    float aa = Mathf.Atan2(a.y, a.x), da = Mathf.Atan2(b.y, b.x) - aa; while (da > Mathf.PI) da -= 2 * Mathf.PI; while (da < -Mathf.PI) da += 2 * Mathf.PI;
                    float ang = aa + Mathf.Sign(da) * Mathf.Min(Mathf.Abs(da), .5f), rr = Mathf.Max(ZONE + 2, Mathf.Min(ra, ZONE + 6));
                    MoveDir(ref d, pd.PlanDir(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr), step, pd.R); return false;
                }
                return MoveDir(ref d, t, step, pd.R);
            }
            var g = NavGrid(ps); string tk = Mathf.Round(b.x * 2) + "," + Mathf.Round(b.y * 2);
            ps.vnav.TryGetValue(id, out var nv);
            if (nv != null && nv.tk == tk && nv.nk == ps.navKey && nv.wp.Count > 0) { int i = Math.Min(nv.i, nv.wp.Count - 1); var p0 = i > 0 ? nv.wp[i - 1] : nv.s; if (SegD(a, p0, nv.wp[i]) > 3) nv = null; }
            if (nv == null || nv.tk != tk || nv.nk != ps.navKey)
            {
                var sa = ra < ZONE ? a : a / ra * (ZONE - 1); var sb = rb < ZONE ? b : b / rb * (ZONE - 1);
                var p = NavPath(ps, sa, sb);
                if (p == null)
                {
                    ps.vnav.Remove(id); float aa = Mathf.Atan2(a.y, a.x), da = Mathf.Atan2(b.y, b.x) - aa; while (da > Mathf.PI) da -= 2 * Mathf.PI; while (da < -Mathf.PI) da += 2 * Mathf.PI;
                    float ang = aa + Mathf.Sign(da == 0 ? 1 : da) * Mathf.Min(Mathf.Abs(da) > 0 ? Mathf.Abs(da) : .4f, .4f), rr = ra >= ZONE - 1 ? Mathf.Max(ZONE + 1.5f, ra) : ra;
                    if (ra < ZONE - 1) { int k = NavFree(g, NavIdx(a.x, a.y)); var fp = k >= 0 ? NavPt(k) : a; MoveDir(ref d, pd.PlanDir(fp), step, pd.R); }
                    else MoveDir(ref d, pd.PlanDir(Mathf.Cos(ang) * rr, Mathf.Sin(ang) * rr), step, pd.R);
                    return false;
                }
                var wp = p.Skip(1).ToList(); if (ra >= ZONE) wp.Insert(0, sa); if (rb >= ZONE) wp.Add(sb);
                nv = new VehNav { tk = tk, nk = ps.navKey, wp = wp, i = 0, s = a }; ps.vnav[id] = nv;
            }
            float rem = step;
            while (rem > 1e-4f && nv.i < nv.wp.Count)
            {
                var q = pd.PlanDir(nv.wp[nv.i]); float dist = pd.Arc(d, q);
                if (dist <= rem + .05f) { d = q; rem -= dist; nv.i++; } else { MoveDir(ref d, q, rem, pd.R); return false; }
            }
            if (rem <= 1e-4f) return false; if (rb < ZONE && g[NavIdx(b.x, b.y)]) return true; return MoveDir(ref d, t, rem, pd.R);
        }

        // ================= Planeten =================
        public static string UnlockWhy(string pid)
        {
            int i = Array.IndexOf(GameData.POrder, pid); if (i <= 0) return ""; string prev = GameData.POrder[i - 1];
            if (!P.ContainsKey(prev)) return "Erst " + GameData.Planets[prev].name + " freischalten";
            float c = CleanPct(P[prev]); if (c < .8f) return "Erst 80 % von " + GameData.Planets[prev].name + " reinigen (jetzt " + Mathf.FloorToInt(c * 100) + " %)";
            return "";
        }
        public static bool Unlock(string pid)
        {
            if (P.ContainsKey(pid)) return false; string why = UnlockWhy(pid); if (why != "") return Fail(why);
            double c = GameData.Planets[pid].unlockCost; if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; var sv = NewPlanet(pid); S.planets.Add(sv); P[pid] = Attach(sv); Travel(pid);
            Toast(GameData.Planets[pid].name + ": neuer Anfang – Basis, Technik und Fahrzeug-Upgrades baust du hier selbst auf.", "good"); return true;
        }
        public static void Travel(string pid) { if (!P.ContainsKey(pid) || S.planet == pid) return; S.planet = pid; strokeCell = -1; lastCell = -1; OnPlanetChanged?.Invoke(); }

        // ================= Ziele =================
        static PState Moon => P["moon"];
        static float StartProg() { var m = Moon; return m.act.TryGetValue(m.pd.start, out var a) ? Progress(a) : m.clean[m.pd.start] ? 1 : 0; }
        public static int QuestsSupported = 19;
        public static float QuestProgress(int q)
        {
            var m = Moon; Func<string, bool> any = id => P.Values.Any(p => p.bld.Contains(id));
            switch (q)
            {
                case 0: return StartProg() / .25f;
                case 1: return S.stats.handUnloads > 0 ? 1 : 0;
                case 2: return m.clean[m.pd.start] ? 1 : Mathf.Min(.99f, StartProg());
                case 3: return S.stats.sold > 0 ? 1 : Mathf.Min(.99f, RohSum(m) / 40);
                case 4: return m.nClean >= 2 ? 1 : Mathf.Min(.99f, m.nClean - 1 + m.act.Values.Select(Progress).DefaultIfEmpty(0).Max());
                case 5: return Lvl("radius") + Lvl("power") > 0 ? 1 : 0;
                case 6: return P.Values.Any(p => p.sv.cart.owned) ? 1 : Mathf.Min(.99f, (float)(S.credits / CartCost(m)));
                case 7: return m.sv.stage >= 2 ? 1 : Mathf.Min(.99f, (float)(S.credits / 200));
                case 8: return m.bld.Contains("u1") ? 1 : 0;
                case 9: return m.sv.veh.Any(v => v.type == "buggy") ? 1 : 0;
                case 10: return m.bld.Contains("presse") ? 1 : 0;
                case 11: return S.stats.bricks > 0 ? 1 : 0;
                case 12: return m.sv.stage >= 3 ? 1 : Mathf.Min(.99f, (float)(S.credits / 5000));
                case 13: return m.bld.Contains("schmelze") ? 1 : 0;
                case 14: return S.stats.barren > 0 ? 1 : 0;
                case 16: return P.Values.Any(p => p.sv.drones.Count > 0) ? 1 : P.Values.Any(p => p.bld.Contains("hangar")) ? .5f : 0;
                case 17: return CleanPct(Moon) / .8f;
                case 18: return P.ContainsKey("mars") ? 1 : 0;
                case 15: return ResDone("drones") ? 1 : S.resAct == "drones" ? S.resT / 300 : 0;
            }
            return 0;
        }
        public static bool ClaimQuest()
        {
            int q = S.quest; if (q >= QuestsSupported || q >= GameData.Quests.Count || QuestProgress(q) < 1) return false;
            Earn(GameData.Quests[q].r); S.quest++; Toast("Ziel erreicht · +" + Fmt(GameData.Quests[q].r) + " Cr", "good"); return true;
        }

        // ================= Energie, Tag und Nacht =================
        public static float DayLen(string pid) => GameData.DayLen.TryGetValue(pid, out var l) ? l : 480;
        public static float GeoF(string pid) => GameData.Geof.TryGetValue(pid, out var g) ? g : 0;
        static float DayT(PState ps) => ps.sv.dayT < 0 ? DayLen(ps.pid) * .08f : ps.sv.dayT;
        public static float SunAng(PState ps) => DayT(ps) % DayLen(ps.pid) / DayLen(ps.pid) * Mathf.PI * 2;
        public static float DayF(PState ps) { float e = Mathf.Sin(SunAng(ps)); return Mathf.Pow(Mathf.Clamp01((e + .08f) / 1.08f), .8f); }
        public static bool IsDay(PState ps) => DayT(ps) % DayLen(ps.pid) < DayLen(ps.pid) / 2;
        public static float DayLeft(PState ps) { float L = DayLen(ps.pid), t = DayT(ps) % L; return t < L / 2 ? L / 2 - t : L - t; }
        public static Vector3 SunDir(PState ps) { var c = ps.pd.cells[ps.pd.baseCell]; float a = SunAng(ps); return (c.c * Mathf.Sin(a) + c.e1 * Mathf.Cos(a)).normalized; }
        public static float EnergyCap(PState ps) => 150 + (ps.bld.Contains("energy") ? 150 : 0) + Uv("ecap", ps) + (ps.bld.Contains("reactor") ? 1000 : 0);
        public static float SolarProd(PState ps) => (3 + (ps.bld.Contains("energy") ? 8 : 0)) * Uv("energy", ps) * ps.solar * GameData.SolarPeak;
        public static float ExtraProd(PState ps) => (ps.bld.Contains("rtg") ? 10 : 0) + (ps.bld.Contains("geo") ? 30 * GeoF(ps.pid) : 0) + (ps.bld.Contains("reactor") ? 150 : 0);
        public static float EnergyProd(PState ps) => SolarProd(ps) + ExtraProd(ps);
        // ================= Forschung =================
        public static bool ResDone(string id) => S.resDone.Contains(id);
        public static ResDef ResActive => string.IsNullOrEmpty(S.resAct) ? null : GameData.Res.Find(r => r.id == S.resAct);
        public static bool HasLab() => P.Values.Any(p => p.bld.Contains("research"));
        public static string ResWhy(ResDef d)
        {
            if (ResDone(d.id)) return "Erforscht"; if (!HasLab()) return "Erst eine Forschungsstation bauen";
            if (ResActive != null) return "Es läuft schon ein Projekt";
            if (!string.IsNullOrEmpty(d.req) && !ResDone(d.req)) return "Erst " + GameData.Res.Find(r => r.id == d.req).name;
            return "";
        }
        public static bool StartResearch(string id)
        {
            var d = GameData.Res.Find(r => r.id == id); if (d == null) return false; string w = ResWhy(d); if (w != "") return Fail(w);
            if (S.credits < d.cost) return Fail(Lacks(d.cost)); S.credits -= d.cost; S.resAct = id; S.resT = 0; Toast("Forschung gestartet: " + d.name); return true;
        }
        public static float ResSpeed() => Uv("research");
        static void ResearchStep(float dt)
        {
            var d = ResActive; if (d == null) { S.resAct = ""; return; }
            S.resT += dt * ResSpeed();
            if (S.resT >= d.time) { if (!S.resDone.Contains(d.id)) S.resDone.Add(d.id); S.resAct = ""; S.resT = 0; foreach (var p in P.Values) p.baseVersion++; if (!Offline) Toast("Forschung abgeschlossen: " + d.name, "good"); }
        }
        public static float BattCap(PState ps, VehSave v) => GameData.Veh[v.type].batt * Uv("vbatt", ps);
        static float Bat(PState ps, VehSave v) => v.bat < 0 ? BattCap(ps, v) : v.bat;
        static bool BattLow(PState ps, VehSave v) => GameData.Veh[v.type].batt > 0 && Bat(ps, v) < BattCap(ps, v) * .15f;
        static void BattUse(PState ps, VehSave v, float dt) { if (GameData.Veh[v.type].batt <= 0) return; v.bat = Mathf.Max(0, Bat(ps, v) - dt); }

        // ================= Stürme =================
        public static bool StormOn(PState ps) => ps.P.storm != null && ps.sv.hzOn > 0;
        public static float StormIn(PState ps) => ps.P.storm == null ? -1 : Mathf.Max(0, ps.P.storm.every - ps.sv.hzT);
        static StormDef StormStep(PState ps, float dt)
        {
            var st = ps.P.storm; var sv = ps.sv; StormDef cur = null;
            if (st != null)
            {
                sv.hzT += dt;
                if (sv.hzOn > 0) { sv.hzOn -= dt; cur = st; if (sv.hzOn <= 0) { sv.hzOn = 0; sv.hzT = 0; } }
                else if (sv.hzT >= st.every) { sv.hzOn = st.dur; sv.hzT = 0; sv.hzWarned = false; cur = st; if (ps.pid == S.planet) Toast(st.name + " zieht auf!", "bad"); }
                if (sv.hzOn <= 0 && !sv.hzWarned && st.every - sv.hzT <= 30) { sv.hzWarned = true; if (ps.pid == S.planet) Toast(st.name + " in 30 s" + (ResDone("shield") ? (sv.shield ? " – Sturmschild ist bereit" : " – Sturmschild einschalten?") : ""), "bad"); }
            }
            ps.shOn = cur != null && sv.shield && ResDone("shield") && sv.energy > 3;
            if (ps.shOn) { float K(float x) => 1 - (1 - x) * .2f; cur = new StormDef { name = cur.name, veh = K(cur.veh), belt = K(cur.belt), mach = K(cur.mach), solar = cur.solar, dust = 1 + (cur.dust - 1) * .2f }; }
            ps.vm = cur?.veh ?? 1; ps.bm = cur?.belt ?? 1; ps.mm = cur?.mach ?? 1;
            return cur;
        }
        public static string StormFx(StormDef s)
        {
            var fx = new List<string>(); if (s.veh != 1) fx.Add("Fahrzeuge −" + Mathf.RoundToInt((1 - s.veh) * 100) + " %"); if (s.belt != 1) fx.Add("Bänder −" + Mathf.RoundToInt((1 - s.belt) * 100) + " %");
            if (s.mach != 1) fx.Add("Maschinen −" + Mathf.RoundToInt((1 - s.mach) * 100) + " %"); if (s.solar != 1) fx.Add("Solar −" + Mathf.RoundToInt((1 - s.solar) * 100) + " %"); if (s.dust != 1) fx.Add(s.dust.ToString("0") + "× Staub");
            return string.Join(" · ", fx);
        }
        public static bool ToggleShield() { var ps = Cur; if (!ResDone("shield")) return Fail("Erst den Sturmschild erforschen"); ps.sv.shield = !ps.sv.shield; Toast(ps.sv.shield ? "Sturmschild aktiv – schaltet sich bei Sturm ein" : "Sturmschild aus", ps.sv.shield ? "good" : ""); return true; }
        // ================= Fahrzeugleistung pro Planet =================
        public static int PowReq(string pid) { int i = Array.IndexOf(GameData.POrder, pid); return i >= 0 && i < GameData.PowReq.Length ? GameData.PowReq[i] : 0; }
        public static bool PowOK(PState ps) => Lvl("vpower", ps) >= PowReq(ps.pid);
        static float HomeF(PState ps, VehSave v) { var h = GameData.Veh[v.type].home; return h != null && Array.IndexOf(h, ps.pid) >= 0 ? 2.2f : 1; }

        static void EnergyStep(PState ps, float dt)
        {
            var sv = ps.sv; var storm = StormStep(ps, dt); sv.dayT = DayT(ps) + dt; ps.solar = ps.P.solar * (storm?.solar ?? 1) * DayF(ps);
            float demand = 0, vE = Uv("veff", ps) * (ResDone("motor") ? .7f : 1);
            foreach (var v in sv.veh) { var T = GameData.Veh[v.type]; if (T.batt > 0) { if (v.charging) demand += T.energy * 2.5f * vE; } else if (v.busy) demand += T.energy * vE; }
            foreach (var dr in sv.drones) if (dr.busy) demand += .4f * vE; if (ps.shOn) demand += 4;
            foreach (var m in new[] { "presse", "schmelze", "raff" }) if (ps.bld.Contains(m) && ps.mach[m].cur >= 0) demand += GameData.Mach[m].x * Uv("meff", ps);
            foreach (var b in ps.lay.belts) if (BeltLive(ps, b) && ps.belts[b.id].Count > 0) demand += .1f * Uv("beff", ps);
            ps.demand = demand * ps.P.energy; ps.prodNow = EnergyProd(ps);
            demand *= ps.P.energy * dt;
            float avail = sv.energy + ps.prodNow * dt, f = demand > 0 ? Mathf.Min(1, avail / demand) : 1;
            sv.energy = Mathf.Max(0, Mathf.Min(EnergyCap(ps), avail - demand * f)); ps.ef = f;
        }

        // ================= Simulation =================
        public static void Step(float dt)
        {
            foreach (var ps in P.Values) StepPlanet(ps, dt);
            ResearchStep(dt);
        }
        static void StepPlanet(PState ps, float dt)
        {
            EnergyStep(ps, dt); float f = ps.ef;
            CartStep(ps, dt);
            foreach (var v in ps.sv.veh) VehStep(ps, v, dt, f);
            if (ps.bld.Contains("hangar")) foreach (var dr in ps.sv.drones) DroneStep(ps, dr, dt, f);
            foreach (var v in ps.sv.veh) if (v.drone >= 0 && !(ps.bld.Contains("hangar") && ps.sv.drones.Any(d => d.tgt == v.id && d.id == v.drone))) v.drone = -1;
            int sub = dt > .3f ? Mathf.CeilToInt(dt / .25f) : 1; float sdt = dt / sub;
            for (int s = 0; s < sub; s++) { foreach (var b in ps.lay.belts) BeltStep(ps, b, sdt, f); foreach (var m in new[] { "presse", "schmelze", "raff" }) MachStep(ps, m, sdt, f); }
            SellStep(ps, dt);
            List<int> done = null; foreach (var kv in ps.act) if (kv.Value.sum < kv.Value.cl.nIn * GameData.DONE) (done = done ?? new List<int>()).Add(kv.Key);
            if (done != null) foreach (int id in done) FinishCell(ps, id);
        }

        public static float OfflineCap => ResDone("off24") ? 86400 : ResDone("off12") ? 43200 : 28800;
    }
}
