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

    public class LqNav { public int to = -2; public bool free; public List<int> p; public int i, k; }
    public class PState
    {
        public PlanetSave sv; public PlanetData pd; public PlanetDef P; public LayoutDef lay; public LineDef line; public FactoryPlan plan; public float medT; /* 11.11.: Medaillen-Prüfung */ /* 10.11.: Fabrik-Planer */
        public bool[] clean, paid; public int nClean;
        public Dictionary<int, Act> act = new Dictionary<int, Act>();
        public Dictionary<int, LqNav> lqnav = new Dictionary<int, LqNav>(); // Umwege um Seen (nicht gespeichert)
        public HashSet<int> front = new HashSet<int>();
        public HashSet<string> bld = new HashSet<string>();
        public Dictionary<string, int> upg = new Dictionary<string, int>();
        public Dictionary<string, StSave> st = new Dictionary<string, StSave>();
        public Dictionary<string, MachSave> mach = new Dictionary<string, MachSave>();
        public Dictionary<string, List<BeltItem>> belts = new Dictionary<string, List<BeltItem>>();
        public Dictionary<string, bool> jam = new Dictionary<string, bool>();
        public HashSet<int> dirtyCells = new HashSet<int>();
        public int baseVersion;
        public Dictionary<string, Vector2[]> visBelt = new Dictionary<string, Vector2[]>(); public int visVer; // tatsächlich verlegte Bandlinien (von der Darstellung), damit Fahrzeuge nie durch Bänder fahren
        public bool[] navG; public string navKey; public Dictionary<long, List<Vector2>> navCache = new Dictionary<long, List<Vector2>>();
        public Dictionary<int, VehNav> vnav = new Dictionary<int, VehNav>();
        public float ef = 1, solar = 1, demand, prodNow, vm = 1, bm = 1, mm = 1; public bool shOn;
        public string pid => sv.pid;
    }
    public class VehNav { public string tk, nk; public List<Vector2> wp; public int i; public Vector2 s; }

    public static partial class Game
    {
        /// <summary>„Version 0.5 · Build 53“ – Build-Nummer aus Resources/Data/build.txt (schreibt der iPhone-/Browser-Build).</summary>
        public static string VersionText { get { var b = Resources.Load<TextAsset>("Data/build"); string n = b != null ? b.text.Trim() : ""; return "Version " + Application.version + (n != "" ? " · Build " + n : ""); } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics() { OnToast = null; OnFloat = null; OnRocket = null; OnBoom = null; OnMeteors = null; OnCellFlash = null; OnPlanetChanged = null; Offline = false; }
        public static SaveData S;
        public static Dictionary<string, PState> P = new Dictionary<string, PState>();
        public static int NM;
        public static event Action<string, string> OnToast;              // Text, Art (good/bad/"")
        public static event Action<Vector3, string, string> OnFloat;     // Richtung, Text, Art
        public static event Action<string, double> OnRocket;
        public static event Action<Vector3> OnBoom;
        /// <summary>Meteoritenschauer auf diesen Parzellen (die Ansicht lässt Meteore einschlagen).</summary>
        public static event Action<List<int>> OnMeteors;
        /// <summary>Eine Parzelle ist gerade fertig sauber geworden (nur live auf dem aktuellen Planeten).</summary>
        public static event Action<int> OnCellFlash;
        /// <summary>Wohin die Kamera gerade schaut (Planetenrichtung), von der Ansicht gesetzt.</summary>
        public static Vector3 ViewDir; public static float ViewCos = .97f;
        public static event Action OnPlanetChanged;
        public static Vector3 CartGoal;                                  // wohin der Sammelwagen fährt (Blickpunkt der Kamera)
        public static bool Offline;
        static readonly System.Random Rng = new System.Random(4711);
        static readonly Dictionary<string, int> GUpg = new Dictionary<string, int>();
        const float VEH_BUFF = 2.6f, BSP = 1.25f;
        /// <summary>Im Editor kann ein getrennter Test-Spielstand benutzt werden (Menü Lunar Cleanup → Testmodus), damit Test-Tasten nie deinen echten Stand verändern.</summary>
        public static bool TestMode, AdminWeb; /* AdminWeb: Browser-Admin für Fabian (02.11.) */
        static string Key => "lunar-cleanup-tycoon-unity-v1" + (TestMode ? "-test" : "") + (Hard ? "-hard" : ""); /* 03.11.: Hard Mode eigener Spielstand */

        public static PState Cur => P[S.planet];
        public static PlanetDef CurP => GameData.Planets[S.planet];

        // ================= Start, Speichern, Laden =================
        public static void Init()
        {
            Loc.Init(); GameData.Load(); NM = GameData.Mats.Count; DampVehicleUpgrades(); InitHard();
            S = Load() ?? Fresh();
            GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
            P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); ProcessPrizes(); MigrateWallet(); MigrateSd();
            if (!P.ContainsKey(S.planet)) S.planet = "moon";
        }

        /// <summary>Größe eines Planeten: wie im Spielstand festgehalten, sonst die aktuelle Größe.</summary>
        public static int FreqOf(string pid) { var sv = S?.planets?.Find(p => p.pid == pid); if (sv != null && sv.freq > 0) return sv.freq; return GameData.Planets.TryGetValue(pid, out var P) ? P.freq : 0; }
        /// <summary>Vorschau für die Basis-Prüfung: Planet mit allen Gebäuden, höchster Basisstufe und allen Außenlagern, Mittag.
        /// Wird nie gespeichert und läuft nicht in der Simulation.</summary>
        /// <summary>Gebäude in Kaufreihenfolge (nach Basisstufe, dann Liste) – für Teil-Ausbauten in der Basis-Prüfung.</summary>
        public static List<string> BuildOrder() { var o = GameData.BOrder.ToList(); return o.OrderBy(id => GameData.Bld[id].stage).ThenBy(id => o.IndexOf(id)).ToList(); }
        /// <summary>Vorschau-Basis. upTo &lt; 0: alles maximal; sonst nur die ersten upTo Gebäude der Kaufreihenfolge (ohne Außenlager/Bohrtürme).</summary>
        public static PState Preview(string pid, int freq = 0, int upTo = -1)
        {
            var sv = NewPlanet(pid); if (freq > 0) sv.freq = freq; sv.stage = GameData.Stages.Count;
            if (upTo < 0) { foreach (var id in GameData.BOrder) if (!sv.bld.Contains(id)) sv.bld.Add(id); }
            else { var ord = BuildOrder(); int st = 1; for (int i = 0; i < upTo && i < ord.Count; i++) { if (!sv.bld.Contains(ord[i])) sv.bld.Add(ord[i]); st = Math.Max(st, GameData.Bld[ord[i]].stage); } sv.stage = Math.Max(1, st); }
            sv.dayT = DayLen(pid) * .25f;
            if (upTo < 0 && (freq <= 0 || freq == GameData.Planets[pid].freq))
            {
                foreach (int c in OutpostSites(pid)) sv.outposts.Add(new StSave { id = "o" + c, cell = c, buf = new float[NM] });
                // ein Bohrturm und ein Erzbagger zum Anschauen (Ring 4, trockene Zellen, kein Außenlager-Platz)
                var pd = PlanetData.Get(pid); var sites = OutpostSites(pid); var bc = pd.cells[pd.baseCell];
                var cand = pd.cells.Where(c => c.ring == 4 && !c.Wet && !sites.Contains(c.id)).OrderByDescending(c => Vector3.Dot(c.c, bc.e1)).Take(2).ToList();
                for (int i = 0; i < cand.Count; i++) sv.rigs.Add(new RigSave { cell = cand[i].id, lv = 3, bag = i == 1, buf = new float[NM] });
            }
            return Attach(sv);
        }
        static SaveData Fresh() { var s = new SaveData(); s.planets.Add(NewPlanet("moon")); s.lastActive = Now(); return s; }
        public static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        static PlanetSave NewPlanet(string pid)
        {
            var ps = new PlanetSave { pid = pid, freq = GameData.Planets[pid].freq, tFirst = Now() };
            foreach (var id in GameData.BOrder) { var B = GameData.Bld[id]; if (B.auto && B.stage <= 1) ps.bld.Add(id); }
            return ps;
        }

        static float[] Arr(float[] a) { if (a == null || a.Length != NM) { var r = new float[NM]; if (a != null) Array.Copy(a, r, Math.Min(a.Length, NM)); return r; } return a; }

        static PState Attach(PlanetSave sv)
        {
            if (sv.freq <= 0) sv.freq = GameData.OldFreq.TryGetValue(sv.pid, out int of) ? of : GameData.Planets[sv.pid].freq;
            string sk = GameData.SizeKey(sv.pid, sv.freq);
            var ps = new PState { sv = sv, pd = PlanetData.GetKey(sv.pid, sk), P = GameData.Planets[sv.pid], lay = OneStation(GameData.Layouts[sk]), line = GameData.Lines[sv.pid] };
            ps.lay = PlannedLayout(ps); /* 10.11.: Fabrik-Planer – Gebäude, Anschlüsse, Bänder und Y-Stücke gemeinsam (verschobene Nebengebäude inklusive) */
            sv.hand = Arr(sv.hand); sv.roh = Arr(sv.roh); sv.prod = Arr(sv.prod); sv.sellBuf = Arr(sv.sellBuf);
            if (sv.cart == null) sv.cart = new CartSave(); sv.cart.tank = Arr(sv.cart.tank);
            if (sv.cart.owned && !Hard) { for (int m = 0; m < NM; m++) { sv.roh[m] += sv.cart.tank[m]; RohIn(ps, m, sv.cart.tank[m]); } Array.Clear(sv.cart.tank, 0, NM); sv.cart.owned = false; cartRefund += CartCost(ps); } /* Sammelwagen abgeschafft: Ladung ins Lager, Kaufpreis zurück */
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
                if (a.id < 0 || a.id >= N) continue; /* auch saubere Parzellen mit Meteoritenstaub */
                var A = NewAct(ps, a.id); var bytes = ActUnpack(a.grid, A.grid.Length);
                if (bytes != null && bytes.Length == A.grid.Length) { float s = 0; for (int k = 0; k < bytes.Length; k++) { A.grid[k] = A.cl.inside[k] ? bytes[k] / 255f : 0; s += A.grid[k]; } A.sum = s; }
                ps.act[a.id] = A;
            }
            foreach (var v in sv.veh) { v.tank = Arr(v.tank); if (v.d.sqrMagnitude < .5f) v.d = ps.pd.PlanDir(ps.lay.start); }
            if (sv.outposts == null) sv.outposts = new List<StSave>(); foreach (var o in sv.outposts) { o.buf = Arr(o.buf); if (o.docks == null) o.docks = new int[0]; ps.st[o.id] = o; }
            if (sv.bRoutes == null) sv.bRoutes = new List<BeltRouteSave>(); foreach (var br in sv.bRoutes) { if (br.p == null) br.p = new List<Vector2>(); if (br.h == null) br.h = new List<float>(); while (br.h.Count < br.p.Count) br.h.Add(0); } sv.terra = Mathf.Clamp(sv.terra, 0, 5); /* Build 34 */
            if (sv.rigs == null) sv.rigs = new List<RigSave>(); foreach (var r in sv.rigs) r.buf = Arr(r.buf);
            if (sv.lifters == null) sv.lifters = new List<LifterSave>(); foreach (var L in sv.lifters) if (L.d.sqrMagnitude < .5f) L.d = ps.pd.PlanDir(ps.lay.start);
            if (sv.drones == null) sv.drones = new List<DroneSave>(); foreach (var dr in sv.drones) { dr.tank = Arr(dr.tank); if (dr.d.sqrMagnitude < .5f) dr.d = ps.lay.pos.ContainsKey("hangar") ? ps.pd.PlanDir(ps.lay.pos["hangar"]) : ps.pd.PlanDir(ps.lay.start); }
            if (sv.cart.owned && sv.cart.d.sqrMagnitude < .5f) sv.cart.d = ps.pd.PlanDir(ps.lay.start);
            CalcFront(ps);
            if (S.done == null) S.done = new List<string>(); if (!S.done.Contains(sv.pid) && CleanPct(ps) >= .999f) S.done.Add(sv.pid);
            if (S.done.Contains(sv.pid) && !sv.prize) pendingPrize.Add(ps); /* alte Spielstände: Prämie nachzahlen, wenn der nächste Planet noch fehlt */
            InitFlags(ps); /* Flaggen anlegen (auch für alte Spielstände) */
            return ps;
        }

        static void Detach(PState ps)
        {
            var sv = ps.sv; int N = ps.pd.N;
            var cb = new char[N]; var pb = new char[N]; for (int i = 0; i < N; i++) { cb[i] = ps.clean[i] ? '1' : '0'; pb[i] = ps.paid[i] ? '1' : '0'; }
            sv.clean = new string(cb); sv.paid = new string(pb);
            sv.bld = ps.bld.ToList(); sv.upg = ps.upg.Select(k => new KV { k = k.Key, v = k.Value }).ToList();
            sv.act = new List<ActSave>();
            foreach (var kv in ps.act) { var A = kv.Value; if (kv.Key >= 0 && kv.Key < ps.clean.Length && ps.clean[kv.Key] && A.sum < 1e-3f) continue; /* Fabian 02.11.: leere, saubere Parzellen nicht speichern (Spielstand war 11 MB → Laden/Speichern langsam) */ bool dust = kv.Key >= 0 && kv.Key < ps.clean.Length && ps.clean[kv.Key]; byte[] by = null; if (!dust) { by = new byte[A.grid.Length]; for (int k = 0; k < by.Length; k++) by[k] = (byte)Mathf.Clamp(Mathf.RoundToInt(A.grid[k] * 255), 0, 255); } sv.act.Add(new ActSave { id = kv.Key, sum = A.sum, grid = ActPack(A, by, dust) }); }
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
            string old = PlayerPrefs.GetString(Key, ""), packed = Pack(json);
            /* Fabian 05.11.: Im Browser darf PlayerPrefs nur ~1 MB groß sein – große Spielstände ließen sich nicht mehr speichern.
               Jetzt gezippt (ca. 1/10), und wenn es trotzdem nicht passt, wird zuerst die Sicherung geopfert statt des Spielstands. */
            try { if (!string.IsNullOrEmpty(old)) PlayerPrefs.SetString(Key + "-bak", old); }
            catch (Exception) { PlayerPrefs.DeleteKey(Key + "-bak"); }
            try { PlayerPrefs.SetString(Key, packed); }
            catch (Exception) { PlayerPrefs.DeleteKey(Key + "-bak"); PlayerPrefs.DeleteKey(Key + "-vorImport"); try { PlayerPrefs.SetString(Key, packed); } catch (Exception e2) { Debug.LogWarning("Lunar Cleanup: Speichern fehlgeschlagen: " + e2.Message); } }
            PlayerPrefs.Save();
            if (!TestMode) Platform.MaybeUpload(json, S.lastActive); // Konto-Speicher (Google Play Spiele / Game Center), sobald verbunden
        }

        /// <summary>10.11.: Unberührter Staub (alle Werte fast gleich, nichts angewischt) wird als ein Wert "=v" gespeichert statt 1024 Zufallswerten – Spielstand war 68 MB.</summary>
        static string ActPack(Act A, byte[] by, bool dust)
        {
            if (dust) /* Staub auf sauberer Parzelle: grobes 4×4-Raster reicht, beim Laden geglättet */
            {
                int G = GameData.GRID, B = G / 4; var ch = new char[17]; ch[0] = '~';
                for (int by4 = 0; by4 < 4; by4++) for (int bx4 = 0; bx4 < 4; bx4++)
                    {
                        float s0 = 0; int n0 = 0; for (int y = by4 * B; y < by4 * B + B; y++) for (int x = bx4 * B; x < bx4 * B + B; x++) { int k = y * G + x; if (!A.cl.inside[k]) continue; s0 += A.grid[k] * 255f; n0++; }
                        int q = n0 > 0 ? Mathf.Clamp(Mathf.RoundToInt(s0 / n0 / 17f), 0, 15) : 0; ch[1 + by4 * 4 + bx4] = "0123456789abcdef"[q];
                    }
                return new string(ch);
            }
            int mn = 255, mx = 0; long s = 0, n = 0; for (int k = 0; k < by.Length; k++) { if (!A.cl.inside[k]) continue; int v = by[k]; if (v < mn) mn = v; if (v > mx) mx = v; s += v; n++; }
            if (n > 0 && mn >= 189 && mx - mn <= 66) return "=" + (s / n);
            return Convert.ToBase64String(by);
        }
        static byte[] ActUnpack(string g, int len)
        {
            if (string.IsNullOrEmpty(g)) return null;
            if (g[0] == '~' && g.Length == 17)
            {
                int G = GameData.GRID; if (len != G * G) return null; var b = new byte[len]; var q = new float[16]; for (int i = 0; i < 16; i++) { int h = "0123456789abcdef".IndexOf(g[1 + i]); q[i] = Mathf.Max(0, h) * 17f; }
                for (int y = 0; y < G; y++) for (int x = 0; x < G; x++)
                    {
                        float fx = Mathf.Clamp((x + .5f) / G * 4 - .5f, 0, 3), fy = Mathf.Clamp((y + .5f) / G * 4 - .5f, 0, 3); int x0 = Mathf.Min(2, (int)fx), y0 = Mathf.Min(2, (int)fy); float tx = fx - x0, ty = fy - y0;
                        float v = Mathf.Lerp(Mathf.Lerp(q[y0 * 4 + x0], q[y0 * 4 + x0 + 1], tx), Mathf.Lerp(q[(y0 + 1) * 4 + x0], q[(y0 + 1) * 4 + x0 + 1], tx), ty); b[y * G + x] = (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255);
                    }
                return b;
            }
            if (g[0] == '=') { if (!int.TryParse(g.Substring(1), out int v)) return null; var b = new byte[len]; for (int k = 0; k < len; k++) b[k] = (byte)Mathf.Clamp(v, 0, 255); return b; }
            try { return Convert.FromBase64String(g); } catch (Exception) { return null; }
        }
        public static string DevActStats()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ps in P.Values)
            {
                int n = ps.act.Count, cl = 0, zero = 0, full = 0, lowMin = 0; var hist = new int[6];
                foreach (var kv in ps.act) { var A = kv.Value; if (ps.clean[kv.Key]) cl++; float mn = 9, mx = -1; int z = 0, ni = 0; for (int k = 0; k < A.grid.Length; k++) { if (!A.cl.inside[k]) continue; ni++; float v = A.grid[k]; if (v < mn) mn = v; if (v > mx) mx = v; if (v < .02f) z++; } if (z == ni) zero++; if (mn >= .74f) full++; if (mn < .74f) lowMin++; hist[Mathf.Clamp((int)((mx - mn) * 5), 0, 5)]++; }
                if (n > 300) sb.Append(ps.pid + ": " + n + " act, sauber " + cl + ", leer " + zero + ", voll " + full + ", angewischt " + lowMin + ", Spanne " + string.Join("/", hist) + " | ");
            }
            return sb.ToString();
        }
        public static string DevSaveSizes()
        {
            var sb = new System.Text.StringBuilder(); int full = JsonUtility.ToJson(S).Length;
            void Probe(object o, string pre)
            {
                foreach (var fi in o.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (fi.FieldType.IsPrimitive || fi.IsNotSerialized) continue; var v = fi.GetValue(o); if (v == null) continue;
                    object empty; try { empty = fi.FieldType.IsArray ? Array.CreateInstance(fi.FieldType.GetElementType(), 0) : (fi.FieldType == typeof(string) ? "" : Activator.CreateInstance(fi.FieldType)); } catch { continue; }
                    fi.SetValue(o, empty); int n = JsonUtility.ToJson(S).Length; fi.SetValue(o, v); int d = full - n; if (d > 200000) sb.Append(pre + fi.Name + " " + d / 1024 + " KB; ");
                }
            }
            Probe(S, ""); foreach (var ps in S.planets) Probe(ps, ps.pid + ".");
            return "voll " + full / 1024 + " KB: " + sb;
        }
        public static string DevSaveTiming()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew(); foreach (var ps in P.Values) Detach(ps); long a = sw.ElapsedMilliseconds;
            string json = JsonUtility.ToJson(S); long b = sw.ElapsedMilliseconds; string packed = Pack(json); long c = sw.ElapsedMilliseconds;
            PlayerPrefs.SetString(Key + "-timing", packed); long d = sw.ElapsedMilliseconds; PlayerPrefs.Save(); long e = sw.ElapsedMilliseconds; PlayerPrefs.DeleteKey(Key + "-timing");
            return "detach " + a + " json " + (b - a) + " (" + json.Length / 1024 + " KB) pack " + (c - b) + " (" + packed.Length / 1024 + " KB) set " + (d - c) + " save " + (e - d);
        }
        const string PackPrefix = "LCZ1:";
        /// <summary>Spielstand gezippt als Text (wie der Spielstand-Code). Alte, ungezippte Stände lassen sich weiter lesen.</summary>
        static string Pack(string json)
        {
            try
            {
                var raw = System.Text.Encoding.UTF8.GetBytes(json);
                using (var ms = new System.IO.MemoryStream()) { using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress)) gz.Write(raw, 0, raw.Length); return PackPrefix + Convert.ToBase64String(ms.ToArray()); }
            }
            catch (Exception) { return json; }
        }
        public static string Unpack(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.StartsWith(PackPrefix)) return s;
            var bytes = Convert.FromBase64String(s.Substring(PackPrefix.Length));
            using (var ms = new System.IO.MemoryStream(bytes)) using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress)) using (var rd = new System.IO.StreamReader(gz)) return rd.ReadToEnd();
        }

        static SaveData Load()
        {
            Icp = null; /* Abfang-Minispiel überlebt keinen Neustart */ restN = -1;
            foreach (var k in new[] { Key, Key + "-bak" })
            {
                string j = PlayerPrefs.GetString(k, ""); if (string.IsNullOrEmpty(j)) continue;
                try { j = Unpack(j); var s = JsonUtility.FromJson<SaveData>(j); if (s != null && s.saveVersion >= 1 && s.planets != null && s.planets.Any(p => p.pid == "moon")) { if (s.stats == null) s.stats = new Stats(); if (s.upg == null) s.upg = new List<KV>(); s.planets.RemoveAll(p => !GameData.Planets.ContainsKey(p.pid)); return s; } }
                catch (Exception e) { Debug.LogWarning("Lunar Cleanup: Spielstand " + k + " nicht lesbar: " + e.Message); }
            }
            return null;
        }

        // ================= Spielstand-Code (Sichern/Übertragen zwischen Geräten) =================
        const string CodePrefix = "LCU1:";
        /// <summary>Ganzer Spielstand als Text (gezippt, Base64) – zum Kopieren in Notizen/Nachrichten.</summary>
        public static string ExportCode()
        {
            Save(); var raw = System.Text.Encoding.UTF8.GetBytes(Unpack(PlayerPrefs.GetString(Key, "")));
            using (var ms = new System.IO.MemoryStream()) { using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress)) gz.Write(raw, 0, raw.Length); return CodePrefix + Convert.ToBase64String(ms.ToArray()); }
        }
        /// <summary>Lädt einen Spielstand-Code. Der bisherige Stand bleibt als Sicherung erhalten. Rückgabe: Fehlertext oder "".</summary>
        public static string ImportCode(string code)
        {
            try
            {
                code = (code ?? "").Trim(); if (!code.StartsWith(CodePrefix)) return "Das ist kein Lunar-Cleanup-Code (er beginnt mit " + CodePrefix + ")";
                var bytes = Convert.FromBase64String(code.Substring(CodePrefix.Length).Replace("\n", "").Replace(" ", ""));
                string json; using (var ms = new System.IO.MemoryStream(bytes)) using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress)) using (var rd = new System.IO.StreamReader(gz)) json = rd.ReadToEnd();
                var s = JsonUtility.FromJson<SaveData>(json);
                if (s == null || s.saveVersion < 1 || s.planets == null || !s.planets.Any(p => p.pid == "moon")) return "Code ist unvollständig oder beschädigt";
                Save(); PlayerPrefs.SetString(Key + "-vorImport", PlayerPrefs.GetString(Key, ""));
                if (s.stats == null) s.stats = new Stats(); if (s.upg == null) s.upg = new List<KV>(); s.planets.RemoveAll(p => !GameData.Planets.ContainsKey(p.pid));
                S = s; GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v;
                P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); ProcessPrizes(); MigrateWallet(); MigrateSd(); if (!P.ContainsKey(S.planet)) S.planet = "moon";
                Save(); OnPlanetChanged?.Invoke(); return "";
            }
            catch (Exception e) { return "Code nicht lesbar (" + e.GetType().Name + ")"; }
        }
        /// <summary>Holt den Stand von vor dem letzten Import zurück.</summary>
        public static bool UndoImport() { string j = PlayerPrefs.GetString(Key + "-vorImport", ""); if (j == "") return false; PlayerPrefs.SetString(Key, j); PlayerPrefs.DeleteKey(Key + "-vorImport"); PlayerPrefs.Save(); S = Load() ?? Fresh(); GUpg.Clear(); foreach (var kv in S.upg) GUpg[kv.k] = kv.v; P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); OnPlanetChanged?.Invoke(); return true; }
        public static bool HasImportBackup => PlayerPrefs.GetString(Key + "-vorImport", "") != "";

        public static void ResetAll() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.DeleteKey(Key + "-bak"); S = Fresh(); GUpg.Clear(); P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps); OnPlanetChanged?.Invoke(); }

        // ================= Upgrades, Werte =================
        public static int Lvl(string id, PState ps = null) { var u = GameData.Up[id]; ps = ps ?? Cur; return u.loc ? (ps.upg.TryGetValue(id, out var l) ? l : 0) : (GUpg.TryGetValue(id, out var g) ? g : 0); }
        public static float Uv(string id, PState ps = null) { var u = GameData.Up[id]; int l = Mathf.Clamp(Lvl(id, ps), 0, u.vals.Length - 1); return (float)u.vals[l]; }
        public static double UCost(string id, PState ps = null) { var u = GameData.Up[id]; ps = ps ?? Cur; return Math.Round(u.baseCost * Math.Pow(u.grow, Lvl(id, ps)) * (u.loc ? ps.P.diff : 1)); }
        public static string UShow(string id, int l) { var u = GameData.Up[id]; return u.show[Mathf.Clamp(l, 0, u.show.Length - 1)]; }
        public static bool BuyUpgrade(string id)
        {
            var u = GameData.Up[id]; var ps = Cur; int l = Lvl(id, ps);
            if (!HardUpgOk(u)) return Fail("Im Hard Mode nicht verfügbar");
            if (l >= u.max) return Fail("Höchste Stufe erreicht");
            if (u.stage > 0 && Gs(ps) < u.stage) return Fail("Erst Basisstufe " + u.stage);
            double c = UCost(id, ps); if (S.credits < c) return Fail(Lacks(c));
            var ug = UGoods(id, ps); if (!HasGoods(ug)) return Fail("Für diese Stufe fehlen " + GoodsTxt(ug) + " – sie kommen per Rakete von " + GameData.Planets[Needs(ps.pid)[0]].name);
            S.credits -= c; PayGoods(ps, ug); if (u.loc) ps.upg[id] = l + 1; else GUpg[id] = l + 1;
            Toast(u.name + " Stufe " + (l + 1), "good"); return true;
        }
        public static int Gs(PState ps) => Math.Max(ps.sv.stage, ps.sv.techF);
        public static float RohCap(PState ps) => Uv("rohcap", ps) * BlF(ps, "roh") * ExpF(ps) * SpecF(ps, "store");
        public static float ProdCap(PState ps) => Uv("prodcap", ps) * BlF(ps, "prod") * ExpF(ps) * SpecF(ps, "store");
        public static float SellCap(PState ps) => Uv("sellcap", ps) * BlF(ps, "sell") * (Platform.Owned("cargo2") ? 1.5f : 1);
        public static float HandCap() => Uv("handcap") * HandF; /* 03.11.: Magnet-Behälter */
        public static float Sum(float[] a) { float s = 0; for (int i = 0; i < a.Length; i++) s += a[i]; return s; }
        public static float RohSum(PState ps) { float s = 0; foreach (int m in GameData.Raw) s += ps.sv.roh[m]; return s; }
        /// <summary>Fabian 11.11.: Ware, die für Besucher-Bestellungen bereitliegt, steht im Bestellfach – sie zählt nicht gegen den Platz im Produktlager.
        /// Sonst wird das Lager „rot“, obwohl die Rakete alles abholt, was sie darf.</summary>
        public static float ProdKept(PState ps) { if (S == null || S.vorders == null || S.vorders.Count == 0) return 0; float k = 0; for (int m = 0; m < ps.sv.prod.Length; m++) if (ps.sv.prod[m] > 0) k += Mathf.Min(ps.sv.prod[m], KeepProd(ps, m)); return k; }
        public static float ProdUsed(PState ps) => ProdSum(ps) - ProdKept(ps);
        public static float ProdSum(PState ps) { float s = 0; for (int m = 0; m < ps.sv.prod.Length; m++) s += ps.sv.prod[m]; return s; } /* zählt auch Rohstoffe, die auf dem Weg zur Rakete im Produktlager liegen */
        public static int DoneCount() => S.done.Count;
        public static bool IsDone(string pid) => S.done.Contains(pid);
        public static double MatValue(int m, PState ps)
        {
            var M = GameData.Mats[m]; bool port = P.Values.Any(x => x.bld.Contains("port"));
            return M.v * Uv("price", ps) * (M.raw ? 1 : Uv("mqual", ps)) * (port ? 1.2 : 1) * ps.P.diff * (1 + .1 * DoneCount()) * (1 + AlbumBonus(ps.pid)) * (1 + .05 * SdLv("price")) * MedalPriceF() * RestSellF * (Hard ? HARD_PRICE : 1); /* Fabian 02.11.: jeder ruhende, gerettete Planet +5 %; 03.11.: Hard Mode ×3 */
        }
        public static void Earn(double cr) { HistEarn(cr); S.stats.earned += cr; if (S.debt > 0 && cr > 0) { double pay = Math.Min(S.debt, cr * .5); S.debt -= pay; cr -= pay; if (S.debt < .5) { S.debt = 0; if (!Offline) Toast("Reisekredit abbezahlt", "good"); } } if (earnPid != null && earnPid != S.planet && P.TryGetValue(earnPid, out var eps)) eps.sv.cash += cr; else S.credits += cr; } /* 03.11.: Planetenkasse */
        /// <summary>Fabian 01.11.: Spieler mit sauberem Planeten, aber ohne Geld sollen nie festsitzen.
        /// A) Reiseprämie: Wer einen Planeten komplett reinigt, bekommt einmalig die Reisekosten zum nächsten Planeten.</summary>
        static readonly List<PState> pendingPrize = new List<PState>();
        static double cartRefund;
        /// <summary>Fabian 01.11.: Fahrzeughalle hat nur noch 9 Stufen – darüber gekaufte Stufen werden erstattet.</summary>
        static void HallRefund()
        {
            if (!GUpg.TryGetValue("slots", out int l) || l <= HALL_MAX) return; var u = GameData.Up["slots"]; double r = 0; for (int k = HALL_MAX; k < l; k++) r += Math.Round(u.baseCost * Math.Pow(u.grow, k));
            GUpg["slots"] = HALL_MAX; S.credits += r; Toast("Die Fahrzeughalle geht jetzt bis 24 Fahrzeuge je Planet – " + (l - HALL_MAX) + " Stufen zu viel erstattet: +" + Fmt(r) + " Cr", "good");
        }
        static void ProcessPrizes() { HallRefund(); foreach (var q in pendingPrize) TravelPrize(q, true); pendingPrize.Clear(); if (cartRefund > 0) { S.credits += cartRefund; Toast("Den Sammelwagen gibt es nicht mehr – " + Fmt(cartRefund) + " Cr zurück. Dein Behälter kommt jetzt direkt ins Rohstofflager.", "good"); cartRefund = 0; } }
        static void TravelPrize(PState ps, bool live)
        {
            if (ps.sv.prize) return; ps.sv.prize = true; int i = Array.IndexOf(GameData.POrder, ps.pid); if (i < 0 || i + 1 >= GameData.POrder.Length) return; string nx = GameData.POrder[i + 1]; if (P.ContainsKey(nx)) return;
            double c = UnlockCost(nx); AddCash(ps, c); if (live) Toast("Reiseprämie für den sauberen Planeten: +" + Fmt(c) + " Cr – genug für die Reise nach " + GameData.Planets[nx].name, "good");
        }
        /// <summary>C) Reise auf Kredit: fehlt Geld, wird der Rest geliehen und mit der Hälfte jeder Einnahme abbezahlt.</summary>
        public static bool CanCredit(string pid) => !P.ContainsKey(pid) && UnlockWhy(pid) == "" && S.credits < UnlockCost(pid) && S.debt <= 0;
        public static bool UnlockOnCredit(string pid)
        {
            if (!CanCredit(pid)) return false; double c = UnlockCost(pid), miss = c - S.credits; S.debt += miss * 1.1; S.credits += miss; /* 10 % Gebühr */
            bool ok = Unlock(pid); if (ok) Toast("Reise auf Kredit: " + Fmt(miss * 1.1) + " Cr werden mit der Hälfte deiner Einnahmen abbezahlt", "good"); return ok;
        }
        public static bool Fail(string t) { Toast(t, "bad"); return false; }
        public static string Lacks(double c) => "Dir fehlen noch " + Fmt(c - S.credits) + " Cr";
        /* Fabian 02.11.: ruhigerer Bildschirm. Alles landet im Protokoll (Ziele → Statistik); Kleinkram erscheint nur, wenn gerade wenig los ist. */
        public static readonly List<(string t, string k, long at)> Log = new List<(string, string, long)>();
        static readonly List<float> toastT = new List<float>();
        static readonly List<(string t, string k)> toastWait = new List<(string, string)>(); static float toastWaitT;
        public static bool ToastBusy => toastWait.Count > 0 || Time.realtimeSinceStartup < toastWaitT + 2.4f;
        static void ToastFlush() { if (toastWait.Count == 0 || PuzzleQuiet || Time.realtimeSinceStartup < toastWaitT) return; var w = toastWait[0]; toastWait.RemoveAt(0); toastWaitT = Time.realtimeSinceStartup + 1.6f; toastT.Add(Time.realtimeSinceStartup); OnToast?.Invoke(w.t, w.k); }
        public static void Toast(string t, string k = "") => Toast(t, k, false);
        public static void Toast(string t, string k, bool minor)
        {
            if (Offline) return; Log.Add((t, k, Now())); if (Log.Count > 80) Log.RemoveAt(0);
            if (PuzzleQuiet) { if (!minor && k != "" && toastWait.Count < 6) toastWait.Add((t, k)); return; } /* 10.11.: nichts verdeckt die Puzzle-Animation – wichtige Meldungen kommen danach */
            float now = Time.realtimeSinceStartup; toastT.RemoveAll(x => now - x > 4f);
            if ((minor || k == "") && toastT.Count >= 2) return; /* schon zwei Meldungen sichtbar: Nebensächliches nur ins Protokoll */
            toastT.Add(now); OnToast?.Invoke(t, k);
        }
        static void FloatTxt(Vector3 d, string t, string k) { if (!Offline) OnFloat?.Invoke(d, t, k); }
        /* Schwebende Mengen gleicher Art am selben Ort bündeln: höchstens alle 2 s eine Zahl */
        static readonly Dictionary<string, (Vector3 d, double n, float t0)> floatSum = new Dictionary<string, (Vector3, double, float)>();
        static void FloatAmt(Vector3 d, double n, string label)
        {
            if (Offline || n <= 0) return; string key = label + Mathf.RoundToInt(d.x * 50) + "," + Mathf.RoundToInt(d.y * 50) + "," + Mathf.RoundToInt(d.z * 50);
            if (floatSum.TryGetValue(key, out var e)) floatSum[key] = (e.d, e.n + n, e.t0); else floatSum[key] = (d, n, Time.realtimeSinceStartup);
        }
        static void FloatFlush()
        {
            if (floatSum.Count == 0) return; float now = Time.realtimeSinceStartup; List<string> done = null;
            foreach (var kv in floatSum) if (now - kv.Value.t0 >= 2f) { (done ??= new List<string>()).Add(kv.Key); int i = kv.Key.IndexOfAny("-0123456789".ToCharArray()); OnFloat?.Invoke(kv.Value.d, "+" + Fmt(kv.Value.n) + " " + kv.Key.Substring(0, i), "mat"); }
            if (done != null) foreach (var k in done) floatSum.Remove(k);
        }

        static readonly string[] UnitsDe = { " k", " Mio", " Mrd", " Bio", " Brd" }, UnitsEn = { "k", "M", "B", "T", "Qa" };
        public static string Num(double v) => Math.Floor(v).ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        public static string Fmt(double v)
        {
            double a = Math.Abs(v); string sgn = v < 0 ? "-" : "";
            if (a < 10000) return sgn + Math.Floor(a).ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
            string[] u = Loc.En ? UnitsEn : UnitsDe; /* Zahl bleibt deutsch formatiert, Loc.Tr stellt Komma/Punkt für Englisch um */ double x = a / 1000; int i = 0; while (x >= 1000 && i < u.Length - 1) { x /= 1000; i++; }
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
            a.sum = 0; ps.dirtyCells.Add(id); int m = a.D.mat; float room = RohCap(ps) - RohSum(ps); if (room > 0) { ps.sv.roh[m] += Mathf.Min(room, a.amt); RohIn(ps, m, Mathf.Min(room, a.amt)); }
        }
        /// <summary>Nur für Tests im Editor: reinigt Welle für Welle die Grenze, bis der Anteil erreicht ist.</summary>
        /// <summary>Testmodus: aktuellen Planeten komplett ausbauen – Basisstufe, alle Gebäude, alle Upgrades, alle Forschungen, alle Außenlager, Fahrzeuge bis zum Platzlimit.</summary>
        public static void DevMaxOut()
        {
            if (!TestMode) return; var ps = Cur; S.credits += 1e15;
            foreach (var r in GameData.Res) if (!S.resDone.Contains(r.id)) S.resDone.Add(r.id);
            ps.sv.stage = GameData.Stages.Count;
            foreach (var id in GameData.BOrder) if (ps.lay.pos.ContainsKey(id) && !ps.bld.Contains(id)) ps.bld.Add(id);
            foreach (var u in GameData.Upg) { if (u.loc) ps.upg[u.id] = u.max; else GUpg[u.id] = u.max; }
            foreach (int c in OutpostSites(ps.pid)) if (!ps.sv.outposts.Any(o => o.cell == c)) { var o = new StSave { id = "o" + c, cell = c, buf = new float[NM] }; ps.sv.outposts.Add(o); ps.st[o.id] = o; }
            for (int round = 0; round < 40; round++) { bool any = false; foreach (var t in GameData.VehOrder) if (VehWhy(ps, t) == "") { BuyVehicle(t); any = true; } if (!any) break; }
            ps.baseVersion++; Toast("Testmodus: " + ps.P.name + " ist komplett ausgebaut", "good");
        }
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
            bool redo = ps.clean[id] && ps.paid[id] && ps.act.ContainsKey(id) && !IsPile(ps, id); GooCleared(ps, id); /* 05.11.: neu verdreckt (Meteor, Welle) – gibt beim Reinigen wieder alles */
            if (!ps.clean[id]) { ps.nClean++; if (!Offline && ps.pid == S.planet) OnCellFlash?.Invoke(id); }
            PileDone(ps, id); /* Müllberg-Belohnung auch auf schon bezahlten Parzellen */
            ps.clean[id] = true; ps.act.Remove(id); ps.dirtyCells.Add(id);
            foreach (var v in ps.sv.veh) if (v.cell == id && (v.mode == "drive" || v.mode == "clean")) { v.mode = "seek"; v.cell = -1; }
            if (!ps.paid[id] || redo)
            {
                ps.paid[id] = true;
                double bonus = ParcelYield(cl) * GameData.Mats[GameData.Dirt[cl.type].mat].v * .3 * Uv("bonus", ps) * ps.P.diff * (ps.pid == S.planet ? GoldF : 1); Earn(bonus); /* 03.11.: Goldener Mopp */
                if (GameData.Kinds[cl.kind].key == "rich") { int m = GameData.Dirt[cl.type].mat; float x = Mathf.Min(ParcelYield(cl) * .2f, RohCap(ps) - RohSum(ps)); if (x > 0) { ps.sv.roh[m] += x; RohIn(ps, m, x); } }
                if (strokeCells.Contains(id)) FloatTxt(ps.pd.cells[id].c, "+" + Fmt(bonus) + " Cr", "big"); /* 04.11.: nur für selbst geputzte Parzellen */
                OnCellDone(ps, id, bonus);
            }
            ps.front.Remove(id); foreach (int b in cl.nb) if (IsFront(ps, b)) ps.front.Add(b);
            foreach (int b in cl.nb) ps.dirtyCells.Add(b);
            if (!IsDone(ps.pid) && CleanPct(ps) >= 1) { S.done.Add(ps.pid); TravelPrize(ps, ps.pid == S.planet); if (ps.sv.tDone <= 0) { ps.sv.tDone = ps.sv.tPlay; ps.sv.tDoneAt = Now(); } ps.baseVersion++; if (ps.pid == S.planet) { Toast(ps.P.name + " ist komplett sauber! Dauerhaft +10 % Verkaufspreis und +1 Fahrzeugplatz auf allen Planeten", "good"); Toast("Neu auf " + ps.P.name + ": Tiefenabbau – baue Bohrtürme auf sauberen Parzellen (Basis → Tiefenabbau)", "good"); Toast("Ab jetzt fallen hier öfter Meteore – ihr Staub bringt dreimal so viel", "good"); } }
        }

        // ================= Handgerät =================
        public static float HandSum() => Sum(Cur.sv.hand);
        static int lastCell = -1; static float fullWarnT;
        /// <summary>Ein Wisch darf bis zu zwei Parzellen gleichzeitig reinigen (die erste und eine benachbarte, auf die der Pinsel hinüberstreicht).</summary>
        static readonly List<int> strokeCells = new List<int>(); const int StrokeMax = 3; public static bool InStroke(int id) => strokeCells.Contains(id); /* 04.11.: Ansicht lädt diese Stellen sofort hoch */
        public static void BeginStroke() { strokeCells.Clear(); Attend(); }
        public static bool CanClean(Vector3 d) { var ps = Cur; int c = ps.pd.CellOf(d, lastCell); return Workable(ps, c); }
        /// <summary>Wischen an der Stelle d (Einheitsvektor). Liefert die aufgenommene Menge.</summary>
        public static float Stroke(Vector3 d, float dt, out int mat)
        {
            mat = -1; var ps = Cur; var pd = ps.pd;
            if (HandCap() - HandSum() <= .05f)
            {
                if (Time.realtimeSinceStartup - fullWarnT > 2.5f) { fullWarnT = Time.realtimeSinceStartup; Toast("Sammelbehälter voll – tippe unten links auf den Behälter", "bad"); }
                return 0;
            }
            if (!SuitUse(ps, d, dt)) return 0; /* 04.11.: Hard Mode – Anzug-Energie */
            int c0 = pd.CellOf(d, lastCell); lastCell = c0;
            strokeCells.RemoveAll(c => !Workable(ps, c));
            /* Fabian 08.11.: „Ich will den Finger nicht hochheben müssen“ – bisher nahm ein Wischzug nur 2 Parzellen an, jede weitere blieb liegen.
               Jetzt wandert der Pinsel mit: kommt eine neue Parzelle dazu, fällt die am weitesten entfernte heraus. */
            if (Workable(ps, c0) && !strokeCells.Contains(c0))
            {
                if (strokeCells.Count >= StrokeMax) { int far = -1; float fd = -1; foreach (int sc in strokeCells) { float ad = pd.Arc(d, pd.cells[sc].c); if (ad > fd) { fd = ad; far = sc; } } strokeCells.Remove(far); }
                strokeCells.Add(c0);
            }
            if (strokeCells.Count == 0) return 0;
            float r = Uv("radius") * RadiusF, total = 0; /* 03.11.: Breiter Pinsel */
            foreach (int sc in strokeCells)
            {
                var a = EnsureAct(ps, sc); if (a == null) continue;
                var l = pd.ToLocal(a.cl, d);
                if (Mathf.Abs(l.x) > a.cl.H + r || Mathf.Abs(l.y) > a.cl.H + r) continue;
                float k = 1 - Mathf.Exp(-.35f * Uv("power") * PowerF * (1 + .25f * SdLv("hand")) * Mathf.Min(dt, .1f) * 60f / (a.D.hard * a.K.hard * PileHard(ps, sc)));
                if (IsPile(ps, sc)) { pileHandT = Time.realtimeSinceStartup; pileHandCell = sc; k = Mathf.Min(k, PileK(ps, sc, null, dt)); }
                float got = CleanAt(ps, a, l.x, l.y, r, k); /* Pinsel wieder wie bis Build 21 (Fabian: Build 22 war schlechter) */
                if (got <= 0) continue;
                float amt = Mathf.Min(got / a.cl.nIn * a.amt, HandCap() - HandSum()); if (amt <= 0) break;
                ps.sv.hand[a.D.mat] += amt; mat = a.D.mat; S.stats.handM2 += got / a.cl.nIn * pd.CellArea(a.cl); total += amt;
            }
            return total;
        }
        public static float StrokeProgress() { var ps = Cur; int c = strokeCells.Contains(lastCell) ? lastCell : strokeCells.Count > 0 ? strokeCells[0] : -1; return c >= 0 && ps.act.TryGetValue(c, out var a) ? Progress(a) : -1; }

        public static void UnloadHand()
        {
            var ps = Cur; float tot = HandSum(); if (tot < .05f) { Fail("Dein Sammelbehälter ist leer"); return; }
            if (HardUnload(ps)) return; /* Hard Mode: Sammelwagen */
            /* Fabian 01.11.: kein Sammelwagen mehr – was du selbst sammelst, kommt direkt ins Rohstofflager */
            float free = RohCap(ps) - RohSum(ps); float[] dst = ps.sv.roh; string name = GameData.Bld["roh"].name, best = "roh";
            if (free < .05f) { Fail("Kein Platz – das Rohstofflager ist voll"); return; }
            float amt = Mathf.Min(tot, free), k = amt / tot;
            for (int m = 0; m < NM; m++) { float x = ps.sv.hand[m] * k; ps.sv.hand[m] -= x; if (ps.sv.hand[m] < 1e-6f) ps.sv.hand[m] = 0; dst[m] += x; }
            S.stats.handUnloads++;
            FloatAmt(ps.pd.PlanDir(ps.lay.pos[best]), amt, "entladen");
            if (amt < tot - .05f) Toast(Fmt(amt) + " E in " + name + " · Rest bleibt im Behälter", ""); /* 04.11.: sonst reicht die Schwebezahl */
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
            var c = ps.sv.cart; float tot = HandSum(), free = CartCap() - Sum(c.tank); float amt = Mathf.Min(tot, free); if (amt <= .05f) { Fail(CartName + " ist voll"); return; }
            float k = amt / tot; for (int m = 0; m < NM; m++) { float x = ps.sv.hand[m] * k; ps.sv.hand[m] -= x; c.tank[m] += x; }
            c.mode = "back"; c.to = ""; c.wt = 0; S.stats.handUnloads++; Toast(Fmt(amt) + " E an " + CartName + " übergeben", "good");
        }
        static void CartStep(PState ps, float dt)
        {
            var c = ps.sv.cart; if (!c.owned) return; float step = Uv("cartspd") * dt, tot = Sum(c.tank); bool here = !Offline && ps.pid == S.planet; var pd = ps.pd;
            switch (c.mode)
            {
                case "park":
                    if (tot > .05f) { c.mode = "back"; c.wt = 0; break; }
                    if (here && HandSum() >= HandCap() * .95f) { c.mode = "go"; Toast(CartName + " kommt zu dir", "good"); break; }
                    CartMove(ps, c, pd.PlanDir(ps.bld.Contains("werkstatt") ? ps.lay.shop : ps.lay.start), step, -1); break;
                case "go":
                    if (!here) { c.mode = tot > .05f ? "back" : "park"; c.wt = 0; break; }
                    if (pd.Arc(c.d, CartGoal) < 3.2f || CartMove(ps, c, CartGoal, step, -1)) { c.mode = "wait"; c.wt = 0; Toast(CartName + " ist da – tippe auf deinen Behälter", "good"); }
                    break;
                case "wait":
                    if (!here) { c.mode = tot > .05f ? "back" : "park"; c.wt = 0; break; }
                    c.wt += dt; if (pd.Arc(c.d, CartGoal) > 8) { c.mode = "go"; break; }
                    if ((HandSum() < .05f && c.wt > 8) || c.wt > 120) { c.mode = tot > .05f ? "back" : "park"; c.wt = 0; }
                    break;
                case "back":
                    if (tot < .05f) { c.mode = "park"; c.to = ""; break; }
                    if (string.IsNullOrEmpty(c.to) || (c.to != "roh" && c.to != "depot" && !ps.bld.Contains(c.to)))
                    {
                        c.to = "roh"; float bf = 1; foreach (var s in GameData.Stations) if (ps.bld.Contains(s)) { float f = StBuf(ps, s) - Sum(ps.st[s].buf); if (f > bf) { bf = f; c.to = s; } }
                        if (Hard && c.to == "roh" && RohCap(ps) - RohSum(ps) <= 1) c.to = "depot"; /* Fabian 03.11.: alles voll – ab ins Zwischenlager */
                    }
                    { var dropAt = c.to == "depot" ? DepotPos(ps) : c.to == "roh" ? ps.lay.pos["roh"] + new Vector2(-GameData.Bld["roh"].fp - 1.6f, 0) : StLoc(ps, c.to, 6.4f, 0); var dd = pd.PlanDir(dropAt); c.wt += dt;
                      /* Fabian 01.11.: Wagen blieb stehen („bringt gerade eine Ladung weg“), wenn der Abladeplatz im neuen Layout nicht erreichbar war – nah dran oder nach 40 s wird trotzdem abgeladen */
                      if (CartMove(ps, c, dd, step, -1) || pd.Arc(c.d, dd) < 4f || c.wt > 40) { c.mode = "drop"; c.wt = 0; } }
                    break;
                case "drop":
                    {
                        bool dep = c.to == "depot"; if (dep) HardDepot(ps);
                        float[] dst = dep ? ps.sv.depot : c.to == "roh" ? ps.sv.roh : ps.st[c.to].buf; float free = dep ? float.MaxValue : c.to == "roh" ? RohCap(ps) - RohSum(ps) : StBuf(ps, c.to) - Sum(dst);
                        float amt = Mathf.Min(tot, free, 30 * dt); if (amt > 0) { float k = amt / tot; for (int m = 0; m < NM; m++) { float x = c.tank[m] * k; c.tank[m] -= x; dst[m] += x; } }
                        c.wt += dt; if (Sum(c.tank) < .05f) { Array.Clear(c.tank, 0, NM); c.mode = "park"; } else if (free <= .05f && c.wt > 4) { c.to = ""; c.mode = "back"; c.wt = 0; }
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
            if (!HardBldOk(id)) return "Im Hard Mode nicht nötig – keine Fahrzeuge und Drohnen";
            if (Gs(ps) < B.stage) return "Erst Basisstufe " + B.stage;
            if (id == "u2" && !ps.bld.Contains("u1")) return "Erst die Entladeanlage (Stufe 1) bauen"; if (id == "u3" && !ps.bld.Contains("u2")) return "Erst Stufe 2 der Entladeanlage bauen";
            if (!string.IsNullOrEmpty(B.research) && !ResDone(B.research)) return "Erst erforschen: " + (GameData.Res.Find(r => r.id == B.research)?.name ?? B.research);
            if (B.geo && GeoF(ps.pid) <= 0) return "Hier gibt es keine nutzbare Erdwärme";
            return "";
        }
        public static double BuildCost(PState ps, string id) => Math.Round(GameData.Bld[id].cost * ps.P.diff);
        public static bool Build(string id)
        {
            var ps = Cur; string why = BuildWhy(ps, id); if (why != "") return Fail(why);
            double c = BuildCost(ps, id); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; ps.bld.Add(id); FreeSpotIfBlocked(ps, id); ps.baseVersion++; ps.navKey = null; if (id == "research") CalcFront(ps); /* 03.11.: Standardplatz von verschobenem Gebäude belegt → nächster freier Platz */
            Toast(GameData.Bld[id].name + " steht", "good"); return true;
        }

        // ================= Stationen, Bänder, Maschinen =================
        public static bool IsOut(string s) => !string.IsNullOrEmpty(s) && s[0] == 'o';
        public static bool HasSt(PState ps, string s) => !string.IsNullOrEmpty(s) && (IsOut(s) ? ps.st.ContainsKey(s) : ps.bld.Contains(s));
        public static float StBuf(PState ps, string s) => IsOut(s) ? GameData.OutBuf * Uv("obuf", ps) * (1 + .5f * ExpN(ps)) : GameData.Bld[s].st.buf * Uv("ubuf", ps) * BlF(ps, s);
        public static int StDocks(PState ps, string s) => (IsOut(s) ? GameData.OutDocks : GameData.Bld[s].st.docks) + (int)Uv("udock", ps);
        static float StRate(string s) => IsOut(s) ? GameData.OutRate : GameData.Bld[s].st.rate;
        public static string StName(PState ps, string s) => IsOut(s) ? "Außenlager" : GameData.Bld[s].name;
        /// <summary>Ort einer Station: 'a' Anfahrt, 'd' Andockplatz k, 'c' Mitte. Außenlager stehen auf ihrer Parzelle.</summary>
        public static Vector3 StDir(PState ps, string s, char kind, int k = 0)
        {
            if (IsOut(s)) { var cl = ps.pd.cells[ps.st[s].cell]; var p = kind == 'a' ? new Vector2(6, 0) : kind == 'd' ? new Vector2(2.9f, -(k - (GameData.OutDocks - 1) / 2f) * 2) : Vector2.zero; return ps.pd.ToDir(cl, p.x, p.y); }
            return ps.pd.PlanDir(kind == 'a' ? StLoc(ps, s, 6.4f, 0) : kind == 'd' ? DockPos(ps, s, k) : ps.lay.pos[s]);
        }
        // ---------- Eine Entladeanlage (Fabians Wunsch 31.10.): u1/u2/u3 bleiben intern getrennt (Käufe, Puffer, Andockplätze),
        // stehen aber am selben Platz (dem der kleinen Station) mit gerader Ausrichtung. Ihre Bänder zum Lager bleiben in der Logik, sichtbar ist nur eines.
        public static readonly string[] Units = { "u1", "u2", "u3" };
        public static bool IsUnit(string id) => id == "u1" || id == "u2" || id == "u3";
        public static bool HiddenBelt(BeltDef b) => IsUnit(b.from) && b.from != "u1"; // b_u2, b_u3a, b_u3b laufen unsichtbar mit
        static LayoutDef OneStation(LayoutDef L)
        {
            if (!L.pos.ContainsKey("u1")) return L;
            var c = new LayoutDef { pos = new Dictionary<string, Vector2>(L.pos), yaw = new Dictionary<string, float>(L.yaw), shop = L.shop, start = L.start, park = L.park, belts = L.belts };
            float y = L.yaw.TryGetValue("u1", out var yy) ? yy : 0; y = Mathf.Round(y / (Mathf.PI / 2)) * (Mathf.PI / 2);
            foreach (var u in Units) if (c.pos.ContainsKey(u)) { c.pos[u] = L.pos["u1"]; c.yaw[u] = y; }
            return c;
        }
        /// <summary>Andockplätze aller gebauten Stufen in einer Reihe an einer geraden Seite der Anlage.</summary>
        static int DockSlot(PState ps, string s, int k, out int total)
        {
            total = 0; int at = -1;
            foreach (var u in Units) { if (!ps.bld.Contains(u) || GameData.Bld[u].st == null) continue; if (u == s) at = total + k; total += GameData.Bld[u].st.docks; }
            if (at < 0) { at = total + k; total += k + 1; } return at;
        }
        public static Vector2 StLoc(PState ps, string s, float a, float b) { float y = ps.lay.yaw.TryGetValue(s, out var yy) ? yy : 0; float c = Mathf.Cos(y), n = Mathf.Sin(y); var p = ps.lay.pos[s]; return new Vector2(p.x + a * c - b * n, p.y + a * n + b * c); }
        public static Vector2 DockPos(PState ps, string s, int k) { if (IsUnit(s)) { int slot = DockSlot(ps, s, k, out int tot); return StLoc(ps, s, 3.4f, -(slot - (tot - 1) / 2f) * 2.2f); } return StLoc(ps, s, 2.6f, -(k - (GameData.Bld[s].st.docks - 1) / 2f) * 1.8f); }
        public static bool BeltLive(PState ps, BeltDef b) => ps.bld.Contains(b.from) && ps.bld.Contains(b.to) && (b.stage == 0 || Gs(ps) >= b.stage);
        static float Packet(PState ps) => 8 * Uv("bcap", ps);
        static float BeltSpeed(PState ps) => 2.4f * Uv("bspeed", ps);
        public static bool Pressable(PState ps, int m) => ps.line.presse.Any(r => r.inp == m && RecOn(ps, r));
        /// <summary>09.11.: Rezept läuft, wenn es eingeschaltet ist – oder wenn ein Besucher die Ware bestellt hat (Bestellungen gehen vor).</summary>
        static bool RecOn(PState ps, Recipe r) => PlanOn(ps, r.plan) || WantsOut(ps, r.outp);
        static bool SmeltOn(PState ps) => ps.bld.Contains("schmelze") && (PlanOn(ps, "smelt") || ps.line.schmelze.Any(r => OrderWants(ps, r.outp)));
        static bool RawRoute(PState ps, int m) => (ps.bld.Contains("presse") && Pressable(ps, m)) || (ps.bld.Contains("raff") && PlanOn(ps, "refine") && ps.line.raff.Any(r => r.inp == m));
        static int MachBatch(PState ps, string id) => id == "presse" ? (int)Uv("pvol", ps) : 1;
        public static float MachInCap(PState ps, string id) => GameData.Mach[id].y * Uv("mcap", ps) * BlF(ps, id) * MachBatch(ps, id);
        public static float MachOutCap(PState ps, string id) => GameData.Mach[id].z * Uv("mcap", ps) * BlF(ps, id) * MachBatch(ps, id);

        static float DestFree(PState ps, BeltDef b)
        {
            if (b.to == "roh") return RohCap(ps) - RohSum(ps);
            if (b.to == "prod") return ProdCap(ps) - ProdUsed(ps);
            if (b.to == "sell") return SellCap(ps) - Sum(ps.sv.sellBuf);
            if (ps.mach.ContainsKey(b.to)) return MachInCap(ps, b.to) - Sum(ps.mach[b.to].inp);
            return 0;
        }

        /// <summary>10.11.: wie Pick, aber je Ware bleibt keep(m) im Lager liegen (reserviert für Bestellungen).</summary>
        static bool PickSpare(float[] o, IEnumerable<int> keys, float min, float cap, bool whole, System.Func<int, float> keep, out int bm, out float ba)
        {
            bm = -1; ba = 0; float bs = 0; foreach (int m in keys) { float sp = o[m] - keep(m); if (sp >= min && (bm < 0 || sp > bs)) { bm = m; bs = sp; } }
            if (bm < 0) return false; float a = Mathf.Min(cap, bs); if (whole) a = Mathf.Floor(a + 1e-6f); if (a < min) { bm = -1; return false; }
            o[bm] -= a; if (o[bm] < 1e-6f) o[bm] = 0; ba = a; return true;
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
            if (b.from == "roh" && (b.to == "sell" || b.to == "prod")) { /* Fabian 02.11.: Verkaufs-Rohstoffe gehen erst ins Produktlager */ bool over = RohSum(ps) >= RohCap(ps) * .97f; int om = sv.order; System.Func<int, float> kr = x => KeepRoh(ps, x); if (sv.autoOrder && om >= 0 && GameData.Mats[om].raw && SellOn(ps, om) && sv.roh[om] >= 1 && PickSpare(sv.roh, new[] { om }, 1, P0, false, kr, out m, out a)) return true; return PickSpare(sv.roh, GameData.Raw.Where(x => SellOn(ps, x)), 1, P0, false, kr, out m, out a); /* Verkaufs-Rohstoffe, Bestelltes bleibt liegen */ }
            if (b.from == "prod" && b.to == "sell") { return PB >= 1 && PickSpare(sv.prod, Enumerable.Range(0, NM).Where(x => sv.prod[x] > 0), 1, PB, true, x => KeepProd(ps, x), out m, out a); } /* 10.11.: alles fährt in den Frachter – außer was für Bestellungen bereitliegt */ /* 05.11.: Bestelltes wird nicht verkauft */ /* freigegebene Produkte und alle Verkaufs-Rohstoffe aus dem Produktlager */ /* nur freigegebene Produkte */
            if (b.from == "roh")
            {
                List<Recipe> rl = b.to == "presse" ? ps.line.presse : b.to == "raff" ? ps.line.raff : null; if (rl == null) return false; if (b.to == "raff" && !PlanOn(ps, "refine") && !ps.line.raff.Any(r => OrderWants(ps, r.outp))) return false;
                var M = ps.mach[b.to]; int best = -1; float bs = -1;
                foreach (var r in rl) { if (!RecOn(ps, r)) continue; float x = sv.roh[r.inp] - KeepRoh(ps, r.inp); if (x < 1) continue; /* 10.11.: bestellter Rohstoff bleibt liegen */ float sc = x / (1 + M.inp[r.inp]) * (WantsOut(ps, r.outp) ? 1000 + (float)Math.Min(1e6, OrderPay(ps, r.outp) / 1000) : 1); /* 05.11.: Bestellungen zuerst */ if (sc > bs) { bs = sc; best = r.inp; } }
                return best >= 0 && PickSpare(sv.roh, new[] { best }, 1, P0, false, x => KeepRoh(ps, x), out m, out a);
            }
            if (ps.mach.ContainsKey(b.from))
            {
                var o = ps.mach[b.from].outp; var outs = ps.line.Of(b.from).Select(r => r.outp).Distinct().ToList();
                if (b.from == "presse")
                {
                    var smIns = ps.line.schmelze.Select(r => r.inp).ToList();
                    if (b.to == "schmelze") return SmeltOn(ps) && Pick(o, outs.Where(x => smIns.Contains(x) && !OrderWants(ps, x)), 1, PB, true, out m, out a);
                    return Pick(o, outs.Where(x => !(SmeltOn(ps) && smIns.Contains(x)) || OrderWants(ps, x)), 1, PB, true, out m, out a); /* 05.11.: bestellte Bricks gehen ins Produktlager statt in die Schmelze */
                }
                return Pick(o, outs, 1, PB, true, out m, out a);
            }
            return false;
        }

        static bool BeltAccept(PState ps, BeltDef b, int m, float a)
        {
            var sv = ps.sv;
            if (b.to == "roh") { if (RohSum(ps) + a > RohCap(ps) + 1e-6f) return false; { sv.roh[m] += a; RohIn(ps, m, a); } return true; }
            if (b.to == "prod") { if (ProdUsed(ps) + a > ProdCap(ps) + 1e-6f) return false; sv.prod[m] += a; ProdIn(ps, m, a); return true; }
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
                var ordIx = OrderIdx(ps, rec); /* 09.11.: bestellte Waren nach Bezahlung sortiert */
                for (int r0 = 0; r0 < rec.Count * 2; r0++)
                {
                    int r = r0 < rec.Count ? ordIx[r0] : r0 % rec.Count; var rc = rec[r]; if (!RecOn(ps, rc)) continue;
                    if (r0 < rec.Count && !WantsOut(ps, rc.outp)) continue; /* 05.11.: erste Runde nur bestellte Waren */
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
            M.prog += dt * Uv("mspeed", ps) * BlF(ps, id) * SpecF(ps, "mach") * f * ps.mm;
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
            if (M.cur >= 0) return ps.ef < .05f ? "Keine Energie" : ps.ef < .99f ? "Arbeitet langsamer – zu wenig Energie" : "Arbeitet"; if (Sum(M.outp) + 1 > MachOutCap(ps, id)) return "Ausgang voll"; return ps.line.Of(id).Count > 0 && ps.line.Of(id).All(r => !PlanOn(ps, r.plan)) ? "Pausiert (Plan aus)" : "Wartet auf Material";
        }

        // ================= Rakete =================
        public static float RocketTime(PState ps) => 12 / Uv("rspeed", ps) / SpecF(ps, "rocket");
        public static double CargoValue(PState ps)
        {
            double b = 0; int kinds = 0; for (int m = 0; m < NM; m++) { float u = ps.sv.sellBuf[m]; if (u <= 1e-6f) continue; kinds++; b += u * MatValue(m, ps); }
            if (ps.sv.order < 0) NewOrder(ps); double ob = ps.sv.order >= 0 && ps.sv.order < NM ? ps.sv.sellBuf[ps.sv.order] * MatValue(ps.sv.order, ps) * (ORDER_BONUS - 1) : 0;
            return (b + ob) * Math.Min(1.4, 1 + .08 * Math.Max(0, kinds - 1)) * FlowBonus(ps) * SpecF(ps, "price") * WoRep(ps);
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
        /// <summary>Fabian 01.11.: Ab wann die Rakete startet – so viel, wie in etwa 4 Minuten nachkommt (höchstens der volle Frachtraum). Kleine Planeten warten so nicht ewig.</summary>
        public static float SellTarget(PState ps) { float cap = SellCap(ps); return Mathf.Clamp(ps.sv.sellRate * 240f, Mathf.Min(cap * .98f, 60f), cap * .98f); }
        static void SellStep(PState ps, float dt)
        {
            var sv = ps.sv; if (!ps.bld.Contains("sell")) return;
            if (sv.rk > 0) { sv.rk = Mathf.Max(0, sv.rk - dt); float t2 = Sum(sv.sellBuf), inf = Mathf.Max(0, t2 - sv.sellLast); sv.sellLast = t2; if (dt > 0) sv.sellRate = Mathf.Lerp(sv.sellRate, inf / dt, 1 - Mathf.Exp(-dt / 180f)); return; } /* Zufluss auch messen, während die Rakete unterwegs ist */
            float tot = Sum(sv.sellBuf); if (tot <= 1e-6f) { sv.rkGo = false; sv.sellAge = 0; sv.sellLast = 0; if (dt > 0) sv.sellRate *= Mathf.Exp(-dt / 180f); return; }
            bool jamS = false; foreach (var b in ps.lay.belts) if (b.to == "sell" && ps.jam.TryGetValue(b.id, out bool j) && j) jamS = true; /* passt das nächste Paket nicht mehr hinein, startet sie schon ab 60 % – sonst steht alles */
            float inflow = Mathf.Max(0, tot - sv.sellLast); if (inflow > .01f) sv.sellIdle = 0; else sv.sellIdle += dt; sv.sellLast = tot; sv.sellAge += dt; if (dt > 0) sv.sellRate = Mathf.Lerp(sv.sellRate, inflow / dt, 1 - Mathf.Exp(-dt / 180f));
            /* Fabian 01.11.: Offline blieb die Ladung im Frachtraum liegen, bis die Rakete voll war – das passierte oft nie. Kommt eine Weile nichts Neues dazu, startet sie mit dem, was drin ist (offline nach 45 s, live nach 5 min) – oder spätestens, wenn sie schon lange lädt (offline 10 min, live 30 min). */
            bool stale = tot >= 1 && (sv.sellIdle >= (Offline ? 45 : 300) || sv.sellAge >= (Offline ? 600 : 1800));
            if (tot < SellTarget(ps) && !sv.rkGo && !stale && !(jamS && tot >= SellCap(ps) * .6f) && !(EarlyLaunch(ps) && tot >= SellCap(ps) * .5f)) return;
            sv.sellIdle = 0; sv.sellLast = 0; sv.sellAge = 0;
            bool man = sv.rkGo; sv.rkGo = false; double val = CargoValue(ps) * TerraSellF(ps); if (EvOn(ps, "freighter")) { val *= FeatureData.FREIGHTER_MULT; EvEnd(ps, "Der Frachter zahlt +50 %"); } int om0 = sv.order; bool hadOrder = om0 >= 0 && om0 < NM && sv.sellBuf[om0] > 1e-6f; CountSold(ps, sv.sellBuf); Array.Clear(sv.sellBuf, 0, NM); NewOrder(ps);
            Earn(val); sv.soldP += val; S.stats.sold += val; sv.nRockets++; sv.rk = RocketTime(ps);
            if (!Offline && ps.pid == S.planet) { OnRocket?.Invoke(ps.pid, val); if (man) Toast("Rakete gestartet · +" + Fmt(val) + " Cr" + (hadOrder ? " · Auftrag " + GameData.Mats[om0].name + " erfüllt" : ""), "good"); }
        }

        // ================= Fahrzeuge =================
        public static double VehCost(PState ps, string t) { int n = ps.sv.veh.Count(v => v.type == t); return Math.Round(GameData.Veh[t].cost * ps.P.diff * Math.Pow(1.22, n)); }
        public static int VehSlots(PState ps) => Math.Min(VehMax(ps), (Gs(ps) >= 2 ? 4 : 0) + (ps.bld.Contains("werkstatt") ? 4 : 0) + (ps.sv.stage >= 4 ? 4 : 0) + (ps.sv.stage >= 5 ? 6 : 0) + (int)Uv("slots", ps) + DoneCount() + ExpN(ps));
        public static string VehWhy(PState ps, string t)
        {
            if (Hard) return "Im Hard Mode putzt du selbst – keine Putzfahrzeuge";
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
        static float TankCap(PState ps, VehSave v) => GameData.Veh[v.type].tank * Uv("vtank", ps) * VehBonus(v, "tank");
        static bool CanUse(VehSave v, string s) => IsOut(s) || GameData.Bld[s].st.acc.Contains(GameData.Veh[v.type].size);

        static int ChooseCell(PState ps, VehSave v)
        {
            var T = GameData.Veh[v.type]; int best = -1; float bs = 1e9f; var pd = ps.pd;
            foreach (var kv in ps.act)
            {
                var a = kv.Value; if (a.K.tier > T.tier || a.sum < a.cl.nIn * GameData.DONE || (a.cl.Wet && !T.fly) || !ZoneOk(v, a.cl)) continue;
                int crowd = 0; foreach (var o in ps.sv.veh) if (o != v && o.cell == kv.Key && (o.mode == "drive" || o.mode == "clean")) crowd++;
                bool pile = IsPile(ps, kv.Key); if (crowd >= (pile ? 8 : a.K.tier > 0 ? 4 : 3)) continue;
                float left = a.sum / a.cl.nIn, team = crowd > 0 && left > .35f ? -6 * Math.Min(crowd, 2) - (a.K.hard > 1.3f ? 6 : 0) : crowd * 14;
                if (kv.Key == v.skipCell && v.skipT > 0) continue;
                if (IsGoo(ps, kv.Key)) continue; /* 08.11.: Schleim nur von Hand */
                float sc = pd.Arc(v.d, a.cl.c) + team - (v.last == kv.Key ? 8 : 0) + Spread(ps, v, kv.Key) - (pile ? 400 : 0); /* Müllberg: alle hin */ if (sc < bs) { bs = sc; best = kv.Key; }
            }
            foreach (int id in ps.front)
            {
                if (ps.act.ContainsKey(id)) continue; var cl = pd.cells[id]; if (GameData.Kinds[cl.kind].tier > T.tier || (cl.Wet && !T.fly) || !ZoneOk(v, cl)) continue;
                if (id == v.skipCell && v.skipT > 0) continue; if (IsGoo(ps, id)) continue;
                float sc = pd.Arc(v.d, cl.c) + 6 - (GameData.Kinds[cl.kind].key == "rich" ? 6 : 0) + Spread(ps, v, id); if (sc < bs) { bs = sc; best = id; }
            }
            return best;
        }
        /// <summary>Fahrzeuge verteilen sich: Parzellen in der Nähe anderer Einsätze werden etwas unattraktiver (nicht alle auf einem Fleck).</summary>
        // ---------- Leuchtkristalle: wachsen auf sauberen Parzellen nach, von Hand (antippen) oder von Fahrzeugen abbaubar ----------
        public static readonly (string name, string col, float mul, float w)[] CrystalTypes =
            { ("Lumenkristall", "#5ff0ff", 1f, .38f), ("Glutkristall", "#ff8a3a", 1.3f, .24f), ("Amethyst", "#b07bff", 1.7f, .18f), ("Smaragd", "#3fe58a", 2.3f, .13f), ("Goldkristall", "#ffd23f", 3.5f, .07f) };
        public const float CR_GROW = 240, CR_EVERY = 45;
        /* 05.11.: Kristalle wachsen überall auf dem Planeten, auch im Dreck – nicht nur auf sauberen Parzellen */
        public static int CrystalMax(PState ps) => Mathf.Clamp(ps.pd.N / 22, 6, 80);
        public static float CrystalYield(CrystalSave c) => 10 * CrystalTypes[c.t].mul * (.3f + .7f * c.g);
        public static Vector3 CrystalDir(PState ps, CrystalSave c) => ps.pd.ToDir(ps.pd.cells[c.cell], c.x, c.y);
        static void CrystalStep(PState ps, float dt)
        {
            var sv = ps.sv; if (sv.crystals == null) sv.crystals = new List<CrystalSave>(); foreach (var c in sv.crystals) if (c.g < 1) c.g = Mathf.Min(1, c.g + dt / CR_GROW * (1 + .15f * SdLv("crystal")));
            sv.crT += dt; if (sv.crT < CR_EVERY) return; sv.crT -= CR_EVERY; if (sv.crystals.Count >= CrystalMax(ps)) return;
            var rnd = new System.Random(ps.pid.GetHashCode() ^ (sv.crN * 7919 + 17)); var pd = ps.pd;
            for (int tr = 0; tr < 40; tr++)
            {
                int id = rnd.Next(pd.N); var cl = pd.cells[id];
                if (cl.IsBase || cl.ring < 2 || cl.Wet ||  sv.rigs.Any(r => r.cell == id) || sv.outposts.Any(o => o.cell == id) || sv.crystals.Any(c => c.cell == id)) continue;
                double r = rnd.NextDouble(); int t = 0; for (float acc = 0; t < CrystalTypes.Length - 1; t++) { acc += CrystalTypes[t].w; if (r < acc) break; }
                float a = (float)rnd.NextDouble() * 6.283f, rr = (float)rnd.NextDouble() * cl.H * .5f;
                sv.crystals.Add(new CrystalSave { id = ++sv.crN, cell = id, t = t, x = Mathf.Cos(a) * rr, y = Mathf.Sin(a) * rr, g = 0 }); if (TestMode && !Offline) Debug.Log("Lunar Cleanup: Kristall gewachsen auf " + ps.pid + " – jetzt " + sv.crystals.Count); return;
            }
            sv.crN++;
        }
        static bool PickCrystal(PState ps, VehSave v, float maxDist = 1e9f)
        {
            float room = TankCap(ps, v) - Sum(v.tank); if (room < 3) return false; CrystalSave best = null; float bs = 1e9f;
            foreach (var c in ps.sv.crystals) { if (!ps.clean[c.cell]) continue; /* 05.11.: Kristalle im Dreck nur von Hand */ if (c.claim >= 0 && ps.sv.veh.Any(o => o.id == c.claim && o.cr == c.id) || c.g < 1f) continue; if (!GameData.Veh[v.type].fly && ps.pd.cells[c.cell].Wet) continue; float d = ps.pd.Arc(v.d, CrystalDir(ps, c)) - c.g * 10; if (d + c.g * 10 > maxDist) continue; if (d < bs) { bs = d; best = c; } }
            if (best == null) return false; best.claim = v.id; v.cr = best.id; v.mode = "toCrystal"; return true;
        }
        /// <summary>Kristall von Hand ernten (antippen).</summary>
        public static bool HarvestCrystal(int id)
        {
            var ps = Cur; var c = ps.sv.crystals.Find(x => x.id == id); if (c == null) return false;
            if (c.g < .35f) { Toast(CrystalTypes[c.t].name + " wächst noch (" + Mathf.RoundToInt(c.g * 100) + " %)"); return false; }
            GiveCrystals(ps, c, true); ps.sv.crystals.Remove(c); Attend(); return true; /* 04.11.: eigene Währung statt Rohstoff im Behälter */
        }
        static float Spread(PState ps, VehSave v, int cell)
        {
            float p = 0; var c = ps.pd.cells[cell].c;
            foreach (var o in ps.sv.veh) { if (o == v || o.cell < 0 || o.cell == cell || (o.mode != "drive" && o.mode != "clean")) continue; float d = ps.pd.Arc(ps.pd.cells[o.cell].c, c); if (d < 16) p += 9 * (1 - d / 16); }
            return p;
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
                if (!ps.bld.Contains(s) || !CanUse(v, s) || (v.avoidT > 0 && v.avoid == s)) continue;
                int crowd = ps.sv.veh.Count(o => o != v && o.sid == s && (o.mode == "toUnload" || o.mode == "queue" || o.mode == "unload")); int over = Math.Max(0, crowd - StDocks(ps, s) + 1);
                float d = ps.pd.Arc(v.d, ps.pd.PlanDir(ps.lay.pos[s])) + Sum(ps.st[s].buf) / StBuf(ps, s) * 40 + over * 14; if (d < bd) { bd = d; best = s; }
            }
            foreach (var o in ps.sv.outposts) { if (v.avoidT > 0 && v.avoid == o.id) continue; float fill = Sum(o.buf) / StBuf(ps, o.id); if (fill > .97f) continue; float d = ps.pd.Arc(v.d, StDir(ps, o.id, 'c')) + (fill > .8f ? 60 : 0); if (d < bd) { bd = d; best = o.id; } }
            if (best == null && v.avoidT > 0 && HasSt(ps, v.avoid)) best = v.avoid;
            return best;
        }
        public static Vector2 ParkPos(PState ps, VehSave v) { int i = ps.sv.veh.IndexOf(v); return ps.lay.park + new Vector2((i % 4) * 1.8f, (i / 4) * 1.8f); }

        /* Fabian 01.11.: Fahrzeuge blieben nach einem Neustart zwischen den Förderbändern hängen – wer 25 s unterwegs ist und sich kaum bewegt, steht danach wieder an der Basis */
        static readonly HashSet<string> MovingModes = new HashSet<string> { "drive", "toUnload", "toCrystal", "toRig", "toRoh", "toShop", "sweep" };
        static void StuckCheck(PState ps, VehSave v, float dt)
        {
            if (!MovingModes.Contains(v.mode) || v.lift >= 0) { v.stuckT = 0; v.stuckP = v.d; return; }
            if (ps.pd.Arc(v.d, v.stuckP) > 1.5f) { v.stuckP = v.d; v.stuckT = 0; return; }
            v.stuckT += dt; if (v.stuckT < 25) return;
            v.stuckT = 0; bool live = !Offline && ps.pid == S.planet; if (live) Debug.Log("Lunar Cleanup: " + VehName(v) + " steckte fest (" + v.mode + ") – zurückgesetzt");
            ResetVeh(ps, v, false); v.stuckP = v.d;
        }
        static void VehStep(PState ps, VehSave v, float dt, float f)
        {
            var T = GameData.Veh[v.type]; var pd = ps.pd; float bf = (T.batt > 0 && v.bat >= 0 && v.bat <= 0 ? .3f : 1) * (v.wear > .85f ? .8f : 1);
            float hf = HomeF(ps, v), spd = T.speed * Uv("vspeed", ps) * Mathf.Max(.25f, f) * (hf > 1 ? 1 : ps.vm) * bf * VehBonus(v, "speed") * SpecF(ps, "veh") * (EvOn(ps, "solar") ? FeatureData.SOLAR_VEH : 1), tank = Sum(v.tank), cap = TankCap(ps, v); v.busy = false; v.charging = false; if (v.avoidT > 0) v.avoidT -= dt;
            switch (v.mode)
            {
                case "seek":
                    {
                        if (v.skipT > 0) v.skipT -= dt; v.driveT = 0;
                        if (tank >= cap * .98f || BattLow(ps, v)) { v.mode = "toUnload"; break; }
                        { var rg = PickRig(ps, v, false); if (rg != null) { rg.claim = v.id; v.rig = rg.cell; v.mode = "toRig"; break; } }
                        if (PickCrystal(ps, v, 25f)) break; // reifer Kristall in der Nähe: kurz mitnehmen
                        int c = ChooseCell(ps, v);
                        if (c < 0 && PickCrystal(ps, v)) break; // keine Reinigungsarbeit: Leuchtkristalle abbauen
                        if (c < 0) { var rg = PickRig(ps, v, true); if (rg != null) { rg.claim = v.id; v.rig = rg.cell; v.mode = "toRig"; break; } if (tank > .5f) { v.mode = "toUnload"; break; } if (IdleJob(ps, v)) break; if (StartSweep(ps, v)) break; NavMove(ps, v, pd.PlanDir(ParkPos(ps, v)), spd * .7f * dt, v.id); break; }
                        if (EnsureAct(ps, c) == null) break; v.cell = c; v.ti = -1; if (RequestLift(ps, v, c)) break; v.mode = "drive"; break;
                    }
                case "sweep": SweepStep(ps, v, dt, spd, tank, cap); break;
                case "boom": BoomStep(ps, v, dt); return;
                case "drive":
                case "clean":
                    {
                        if (!ps.act.TryGetValue(v.cell, out var a) || a.sum < a.cl.nIn * GameData.DONE || IsGoo(ps, v.cell)) { v.mode = "seek"; v.cell = -1; break; }
                        if (tank >= cap || BattLow(ps, v)) { v.mode = "toUnload"; break; }
                        v.busy = true; BattUse(ps, v, dt);
                        if (v.ti < 0 || a.grid[v.ti] < .05f) { v.ti = PickGridTarget(ps, a, v); if (v.ti < 0) { v.mode = "seek"; break; } }
                        var gdir = pd.GridDir(a.cl, v.ti);
                        bool arrived = v.mode == "drive" ? NavMove(ps, v, gdir, spd * dt, v.id) : a.cl.ring <= 4 ? NavMove(ps, v, gdir, spd * dt * .6f, v.id) : MoveDir(ref v.d, gdir, spd * dt * .6f, pd.R);
                        if (v.mode == "drive")
                        {
                            var dl = pd.ToLocal(a.cl, v.d); if (arrived || dl.magnitude < a.cl.H * .9f) { v.mode = "clean"; v.driveT = 0; break; }
                            // hängt ein Fahrzeug fest (kommt nicht an), sucht es sich eine andere Parzelle
                            v.driveT += dt; if (v.driveT > 60 + pd.Arc(v.d, a.cl.c) / Mathf.Max(.5f, spd) * 2) { v.skipCell = v.cell; v.skipT = 120; v.driveT = 0; v.mode = "seek"; v.cell = -1; }
                            break;
                        }
                        v.ph += dt * 4; var loc = pd.ToLocal(a.cl, v.d); float wg = arrived ? .4f : 0;
                        float k = 1 - Mathf.Exp(-T.power * VEH_BUFF * hf * Uv("vpower", ps) * VehBonus(v, "power") * SpecF(ps, "veh") * TeamF(ps, v) * (PowOK(ps) ? 1 : .3f) * dt * (arrived ? 1 : .5f) / (a.D.hard * a.K.hard * PileHard(ps, v.cell, v)));
                        if (IsPile(ps, v.cell)) k = Mathf.Min(k, PileK(ps, v.cell, v, dt)); /* Müllberg: Abtrag je Fahrzeug begrenzt – nur viele zusammen schaffen ihn */
                        float rem = CleanAt(ps, a, loc.x + Mathf.Cos(v.ph) * wg, loc.y + Mathf.Sin(v.ph) * wg, T.radius * Uv("vradius", ps), k);
                        if (rem > 0) { float amt = rem / a.cl.nIn * a.amt; v.tank[a.D.mat] += amt; v.coll += amt; VehXp(v, rem / a.cl.nIn * pd.CellArea(a.cl)); }
                        v.last = v.cell; break;
                    }
                case "toUnload":
                    {
                        if (v.drone >= 0) { v.mode = "wait"; break; }
                        var s = ChooseStation(ps, v);
                        if (s == null) { v.mode = "toRoh"; break; } // keine passende Station: direkt ins Rohstofflager
                        v.sid = s; v.busy = true; BattUse(ps, v, dt); if (NavMove(ps, v, StDir(ps, s, 'a'), spd * dt, v.id)) v.mode = "queue"; break;
                    }
                case "queue":
                    {
                        var s = v.sid; if (!HasSt(ps, s)) { v.mode = "toUnload"; break; }
                        var st = ps.st[s]; int n = StDocks(ps, s); if (st.docks == null || st.docks.Length != n) { var nd = new int[n]; if (st.docks != null) Array.Copy(st.docks, nd, Math.Min(n, st.docks.Length)); st.docks = nd; }
                        int k = Array.IndexOf(st.docks, v.id); if (k < 0) { k = Array.IndexOf(st.docks, 0); if (k >= 0) st.docks[k] = v.id; }
                        if (k < 0) { v.qT += dt; if (v.qT > 8) { v.qT = 0; v.avoid = s; v.avoidT = 25; v.sid = null; v.mode = "toUnload"; } break; }
                        v.qT = 0; v.busy = true; if (MoveDir(ref v.d, StDir(ps, s, 'd', k), spd * .6f * dt, pd.R)) v.mode = "unload"; break;
                    }
                case "unload":
                    {
                        var s = v.sid; if (!HasSt(ps, s)) { v.mode = "toUnload"; break; }
                        var st = ps.st[s]; float free = StBuf(ps, s) - Sum(st.buf); bool charged = true;
                        if (T.batt > 0) { float bc = BattCap(ps, v), b0 = Bat(ps, v); if (b0 < bc) { v.bat = Mathf.Min(bc, b0 + bc / 16 * Uv("urate", ps) * Mathf.Max(.25f, f) * dt); v.charging = true; } else v.bat = b0; charged = v.bat >= bc * .97f; }
                        if (free <= .01f && tank > .01f) { v.fullT += dt; if (v.fullT > 4 && !(v.charging && !charged)) { FreeDock(st, v); v.fullT = 0; v.avoid = s; v.avoidT = 40; v.sid = null; v.mode = "toUnload"; } break; }
                        v.fullT = 0; float amt = Mathf.Min(StRate(s) * Uv("urate", ps) * (IsOut(s) ? 1 : BlF(ps, s)) * dt, Mathf.Min(free, tank)), f0 = tank > 0 ? amt / tank : 0;
                        for (int m = 0; m < NM; m++) { float x = v.tank[m] * f0; v.tank[m] -= x; st.buf[m] += x; }
                        if (Sum(v.tank) < .01f && !charged) { Array.Clear(v.tank, 0, NM); break; }
                        if (Sum(v.tank) < .01f) { Array.Clear(v.tank, 0, NM); FreeDock(st, v); v.mode = "seek"; v.sid = null; /* 04.11.: keine Schwebezahl mehr bei jeder Fahrzeug-Entladung */ v.coll = 0; }
                        break;
                    }
                case "wait":
                    if (v.drone < 0) v.mode = tank >= cap * .98f ? "toUnload" : "seek"; break;
                case "toCrystal":
                    {
                        var cr = ps.sv.crystals.Find(x => x.id == v.cr); if (cr == null || cr.claim != v.id) { v.cr = -1; v.mode = "seek"; break; }
                        v.busy = true; BattUse(ps, v, dt); if (NavMove(ps, v, CrystalDir(ps, cr), spd * dt, v.id) || pd.Arc(v.d, CrystalDir(ps, cr)) < 1.4f) { v.mode = "mineCr"; v.crT = 0; }
                        break;
                    }
                case "mineCr":
                    {
                        var cr = ps.sv.crystals.Find(x => x.id == v.cr); if (cr == null) { v.cr = -1; v.mode = "seek"; break; }
                        v.busy = true; v.crT += dt; if (v.crT < 3.5f) break;
                        GiveCrystals(ps, cr, false); ps.sv.crystals.Remove(cr); v.cr = -1; /* 04.11.: Kristalle als Währung */ v.mode = tank >= cap * .9f ? "toUnload" : "seek"; break;
                    }
                case "toRig": { var r = ps.sv.rigs.Find(x => x.cell == v.rig); if (r == null || r.claim != v.id) { v.rig = -1; v.mode = "seek"; break; } v.busy = true; BattUse(ps, v, dt); if (NavMove(ps, v, RigDock(ps, r), spd * dt, v.id)) v.mode = "rigload"; break; }
                case "rigload":
                    {
                        var r = ps.sv.rigs.Find(x => x.cell == v.rig); if (r == null) { v.rig = -1; v.mode = "seek"; break; }
                        float b = Sum(r.buf), amt = Mathf.Min(GameData.RigLoad * Uv("urate", ps) * dt, Mathf.Min(cap - tank, b));
                        if (amt > 0) { float k = amt / b; for (int m = 0; m < NM; m++) { float x = r.buf[m] * k; r.buf[m] -= x; v.tank[m] += x; } v.coll += amt; }
                        if (cap - Sum(v.tank) < .5f || Sum(r.buf) < .05f) { r.claim = -1; v.rig = -1; v.mode = Sum(v.tank) > .5f ? "toUnload" : "seek"; }
                        break;
                    }
                case "toRoh":
                    {
                        if (ChooseStation(ps, v) != null) { v.mode = "toUnload"; break; }
                        v.busy = true; BattUse(ps, v, dt); var dock = ps.lay.pos["roh"] + new Vector2(-GameData.Bld["roh"].fp - 1.6f, 0);
                        if (NavMove(ps, v, pd.PlanDir(dock), spd * dt, v.id)) v.mode = "unloadRoh"; break;
                    }
                case "unloadRoh":
                    {
                        float free = RohCap(ps) - RohSum(ps); if (T.batt > 0) { float bc = BattCap(ps, v), b0 = Bat(ps, v); if (b0 < bc) { v.bat = Mathf.Min(bc, b0 + bc / 30 * Mathf.Max(.25f, f) * dt); v.charging = true; } }
                        if (tank < .01f) { Array.Clear(v.tank, 0, NM); if (!v.charging || Bat(ps, v) >= BattCap(ps, v) * .97f) { /* 04.11.: keine Schwebezahl mehr bei jeder Fahrzeug-Entladung */ v.coll = 0; v.mode = "seek"; } break; }
                        if (free <= .01f) { v.fullT += dt; break; } v.fullT = 0;
                        float amt = Mathf.Min(10 * Uv("urate", ps) * dt, Mathf.Min(free, tank)), f0 = amt / tank; for (int m = 0; m < NM; m++) { float x = v.tank[m] * f0; v.tank[m] -= x; { ps.sv.roh[m] += x; RohIn(ps, m, x); } }
                        break;
                    }
                case "fieldRepair":
                    {
                        v.repT -= dt * (TechOn(ps, "veh", v.id) ? 2f : 1f); /* 10.11.: Techniker hilft */ if (ps.bld.Contains("werkstatt") && v.rep < VM_REPS) { v.mode = "broken"; break; }
                        if (v.repT <= 0) { v.wear = Mathf.Min(v.wear, .5f); v.mode = "seek"; if (ps.pid == S.planet) Toast(T.name + " #" + v.id + " hat sich notdürftig repariert – eine Roboterwerkstatt hält es länger in Schuss", ""); }
                        break;
                    }
                case "broken": v.busy = false; if (!ps.bld.Contains("werkstatt")) { v.mode = "fieldRepair"; v.repT = 90; } break;
                case "toShop": v.busy = true; if (NavMove(ps, v, pd.PlanDir(ps.lay.shop), T.speed * Uv("vspeed", ps) * .5f * dt, v.id)) { v.mode = "repair"; v.repT = VM_TIME * (1 + .5f * v.rep) * (v.repSlow ? 4 : 1); v.repSlow = false; } break;
                case "repair": v.repT -= dt; if (v.repT <= 0) { v.wear = 0; v.rep++; v.mode = "seek"; v.doom = 0; if (ps.pid == S.planet) Toast(T.name + " #" + v.id + " repariert (" + v.rep + "/" + VM_REPS + ")", "good", true); /* Nebensache */ } break;
                case "liftwait":
                case "lifted":
                    if (v.lift < 0 || !ps.sv.lifters.Any(l => l.id == v.lift && l.veh == v.id)) { v.lift = -1; v.mode = "drive"; } break;
                default: v.mode = "seek"; break;
            }
            VehWear(ps, v, dt);
        }

        // ================= Wartung, Verschleiß, Strahlung =================
        const float VM_LIFE = 3 * 3600, VM_TIME = 20, VM_DOOM = 120; const int VM_REPS = 2;
        public static float RadF(PState ps)
        {
            float r = ps.P.rad; if (r <= 0) return 0; var st = ps.P.storm; float sm = ps.sv.hzOn > 0 && st != null && st.rad != 1 ? (ps.shOn ? 1 + (st.rad - 1) * .2f : st.rad) : 1;
            return r * sm / Uv("rshield", ps);
        }
        public static double RepCost(PState ps, VehSave v) => Math.Round(GameData.Veh[v.type].cost * ps.P.diff * .25 * (1 + v.rep));
        public static double ReplaceCost(PState ps, VehSave v) => Math.Round(GameData.Veh[v.type].cost * ps.P.diff * .85) - Math.Round(GameData.Veh[v.type].cost * ps.P.diff * .15);
        public static bool SendToShop(PState ps, VehSave v, bool auto)
        {
            if (!ps.bld.Contains("werkstatt")) return auto ? false : Fail("Erst eine Roboterwerkstatt bauen"); if (v.rep >= VM_REPS) return auto ? false : Fail("Schon zweimal repariert – ersetzen");
            if (v.mode == "toShop" || v.mode == "repair") return false; double c = RepCost(ps, v); if (Cash(ps) < c) return auto ? false : Fail(Lacks(c)); AddCash(ps, -c); /* 03.11.: aus der Kasse dieses Planeten */
            if ((v.mode == "queue" || v.mode == "unload") && HasSt(ps, v.sid)) FreeDock(ps.st[v.sid], v);
            if (v.drone >= 0) v.drone = -1; if (v.rig >= 0) { var r = ps.sv.rigs.Find(x => x.cell == v.rig); if (r != null && r.claim == v.id) r.claim = -1; v.rig = -1; } v.sid = null; v.cell = -1; v.mode = "toShop"; return true;
        }
        public static bool ReplaceVeh(PState ps, VehSave v, bool auto)
        {
            double c = Math.Round(GameData.Veh[v.type].cost * ps.P.diff * .85), scrap = Math.Round(GameData.Veh[v.type].cost * ps.P.diff * .15);
            if (S.credits + scrap < c) return auto ? false : Fail(Lacks(c - scrap)); S.credits += scrap - c;
            if ((v.mode == "queue" || v.mode == "unload") && HasSt(ps, v.sid)) FreeDock(ps.st[v.sid], v);
            v.wear = 0; v.rep = 0; v.doom = 0; v.mode = "seek"; Array.Clear(v.tank, 0, NM); v.d = ps.pd.PlanDir(ps.bld.Contains("werkstatt") ? ps.lay.shop : ps.lay.start); v.sid = null; v.cell = -1; ps.vnav.Remove(v.id); v.bat = -1;
            if (!auto || ps.pid == S.planet) Toast(GameData.Veh[v.type].name + " #" + v.id + " ersetzt", "good"); return true;
        }
        static void VehWear(PState ps, VehSave v, float dt)
        {
            if (Gs(ps) < 3) return; var T = GameData.Veh[v.type];
            if (v.busy && v.mode != "toShop") v.wear += dt * (1 + 3 * RadF(ps)) / (VM_LIFE * Uv("vdur", ps)) * VehBonus(v, "wear"); float w = v.wear;
            if (v.mode != "broken" && v.mode != "toShop" && v.mode != "repair" && v.mode != "lifted" && v.mode != "liftwait" && (w >= 1 || (w > .8f && Rng.NextDouble() < dt / 2400)))
            {
                if ((v.mode == "queue" || v.mode == "unload") && HasSt(ps, v.sid)) FreeDock(ps.st[v.sid], v);
                v.mode = "broken"; v.busy = false; if (v.drone >= 0) v.drone = -1;
                if (ps.pid == S.planet) Toast(T.name + " #" + v.id + " ist ausgefallen" + (ps.bld.Contains("werkstatt") && v.rep < VM_REPS ? " – ab in die Werkstatt" : ""), "bad");
            }
            bool auto = ps.sv.autoMaint;
            if (auto && w >= .75f && v.rep < VM_REPS && (v.mode == "seek" || v.mode == "broken" || (v.mode == "drive" && Sum(v.tank) < 1))) { if (!SendToShop(ps, v, true) && v.mode == "broken" && ps.bld.Contains("werkstatt")) { v.mode = "toShop"; v.sid = null; v.cell = -1; v.repSlow = true; } }
            if (v.rep >= VM_REPS && w >= .75f && auto && (v.mode == "broken" || v.mode == "seek")) ReplaceVeh(ps, v, true);
            if (v.mode == "broken" && v.rep >= VM_REPS)
            {
                v.doom += dt;
                if (v.doom >= VM_DOOM)
                {
                    ps.sv.veh.Remove(v); S.credits += Math.Round(T.cost * ps.P.diff * .15);
                    if (Offline) S.wrecks.Add(ps.P.name + ": " + T.name + " #" + v.id);
                    else if (ps.pid == S.planet) { OnBoom?.Invoke(v.d); Toast(T.name + " #" + v.id + " explodiert – Totalschaden. Schrottwert gutgeschrieben.", "bad"); }
                }
            }
        }
        public static string WearText(VehSave v) => v.mode == "broken" ? (v.rep >= VM_REPS ? "Totalschaden droht in " + Mathf.Max(0, Mathf.CeilToInt(VM_DOOM - v.doom)) + " s – ersetzen!" : "Ausgefallen") : v.wear >= .75f ? "Wartung fällig" : "";
        public static int RepsLeft(VehSave v) => VM_REPS - v.rep;

        // ================= Tiefenabbau =================
        public const int RIG_MAXLV = 5;
        /// <summary>Vorkommen einer Parzelle (0,6–1,6). Gleiche Streuung wie im Browser, aber ohne dessen Vorzeichen-Fehler (dort konnten Werte unter 0 entstehen).</summary>
        public static float RigRich(string pid, int id)
        {
            int idx = Array.IndexOf(GameData.POrder, pid); uint a = (uint)(((ulong)id * 2654435761UL + (ulong)((idx + 1) * 40503)) & 0xffffffffUL);
            uint h = a ^ (a >> 15); h = unchecked(h * 2246822519u); h ^= h >> 13; return .6f + (h % 1000) / 1000f;
        }
        /// <summary>Fabian 01.11.: bis zu 3 Bohrtürme/Bagger an jedem gebauten Außenlager plus 5 freie.</summary>
        public const int RIG_PER_OUTPOST = 3, RIG_FREE = 5;
        public static int RigMax(PState ps) => RIG_FREE + RIG_PER_OUTPOST * ps.sv.outposts.Count;
        /// <summary>Gehört die Parzelle zum Bereich eines Außenlagers (das Lager selbst oder bis 2 Parzellen daneben)? Liefert die Lager-Zelle oder −1.</summary>
        public static int RigOutpost(PState ps, int id)
        {
            foreach (var o in ps.sv.outposts) { if (o.cell == id) return o.cell; foreach (int n in ps.pd.cells[o.cell].nb) { if (n == id) return o.cell; foreach (int n2 in ps.pd.cells[n].nb) if (n2 == id) return o.cell; } }
            return -1;
        }
        public static int RigsAt(PState ps, int outpostCell) => ps.sv.rigs.Count(r => RigOutpost(ps, r.cell) == outpostCell);
        /// <summary>Abriss: 30 % der Baukosten (und des Bagger-Umbaus) kommen zurück.</summary>
        public static double RigRefund(PState ps, RigSave r) => Math.Round(RigCost(ps) / 1.45 * .3 + (r.bag ? BagCost(ps) * .3 : 0));
        public static bool DemolishRig(int cell)
        {
            var ps = Cur; var r = ps.sv.rigs.Find(x => x.cell == cell); if (r == null) return false; double back = RigRefund(ps, r);
            ps.sv.rigs.Remove(r); Earn(back); ps.baseVersion++; Toast((r.bag ? "Erzbagger" : "Bohrturm") + " abgerissen · +" + Fmt(back) + " Cr", "good"); return true;
        }
        public static double RigCost(PState ps) => Math.Round(3000 * ps.P.diff * Math.Pow(1.45, ps.sv.rigs.Count));
        public static int RigMat(PState ps, int id) => GameData.Dirt[ps.pd.cells[id].type].mat;
        public static float RigRate(PState ps, RigSave r) => (IsDone(ps.pid) ? 2f : 1f) /* B) sauberer Planet: Tiefenabbau doppelt */ * GameData.RigBase * RigRich(ps.pid, r.cell) * Uv("rigrate", ps) * (1 + .3f * r.lv) * (r.bag ? 2.5f : 1) * (EvOn(ps, "vein") ? FeatureData.VEIN_MULT : 1) * (1 + .25f * ExpN(ps)) * SpecF(ps, "rig");
        public static float RigBufCap(RigSave r) => r.bag ? 600 : GameData.RigBuf;
        public static double RigUpCost(PState ps, RigSave r) => Math.Round(2500 * ps.P.diff * Math.Pow(1.9, r.lv));
        public static double BagCost(PState ps) => Math.Round(60000 * ps.P.diff);
        public static double FaultFixCost(PState ps, RigSave r) => Math.Round(RigUpCost(ps, r) * .12);
        public static string RichTxt(float x) => x >= 1.3f ? "reich" : x >= .95f ? "gut" : "mager";
        public static List<int> PlanetRaws(PState ps) { var set = new HashSet<int>(ps.P.types.Select(t => GameData.Dirt[t.Key].mat)); return GameData.Raw.Where(set.Contains).ToList(); }
        public static Vector3 RigDock(PState ps, RigSave r) => ps.pd.ToDir(ps.pd.cells[r.cell], 2.7f, 0);
        public static string RigWhy(PState ps, int id)
        {
            if (Hard) return "Im Hard Mode gibt es keine Bohrtürme – ohne Fahrzeuge holt niemand das Erz ab";
            var cl = ps.pd.cells[id];
            if (!IsDone(ps.pid)) return "Erst wenn " + ps.P.name + " komplett sauber ist";
            if (cl.IsBase || !ps.clean[id]) return "Nur auf sauberen Parzellen außerhalb der Basis"; if (cl.ring < 2) return "Zu nah an der Basis";
            if (cl.Wet) return "Nicht auf " + GameData.Liq[cl.liq].name;
            if (ps.sv.rigs.Any(r => r.cell == id)) return "Hier steht schon ein Bohrturm"; if (ps.sv.outposts.Any(o => o.cell == id)) return "Hier steht ein Außenlager";
            if (OutpostSites(ps.pid).Contains(id)) return "Dieser Platz ist für ein Außenlager reserviert";
            int oc = RigOutpost(ps, id);
            if (oc >= 0) { if (RigsAt(ps, oc) >= RIG_PER_OUTPOST) return "An diesem Außenlager stehen schon " + RIG_PER_OUTPOST + " Bohrtürme – reiß einen ab oder bau woanders"; }
            else if (ps.sv.rigs.Count(r => RigOutpost(ps, r.cell) < 0) >= RIG_FREE) return "Höchstens " + RIG_FREE + " freie Bohrtürme – weitere nur direkt an einem Außenlager (je " + RIG_PER_OUTPOST + ")";
            return "";
        }
        public static int BestRigCell(PState ps)
        {
            var cnt = new Dictionary<int, int>(); foreach (var r in ps.sv.rigs) { int q = RigMat(ps, r.cell); cnt[q] = (cnt.TryGetValue(q, out var n) ? n : 0) + 1; }
            int best = -1; float bs = -1; foreach (var cl in ps.pd.cells) { if (cl.IsBase || RigWhy(ps, cl.id) != "") continue; int m = GameData.Dirt[cl.type].mat; float sc = RigRich(ps.pid, cl.id) / (1 + cl.ring * .05f) * (cnt.TryGetValue(m, out var k) ? 1f / (1 + k) : 3); if (sc > bs) { bs = sc; best = cl.id; } }
            return best;
        }
        public static bool BuildRig(int id)
        {
            var ps = Cur; if (id < 0) { id = BestRigCell(ps); if (id < 0) return Fail("Keine freie Parzelle"); }
            string w = RigWhy(ps, id); if (w != "") return Fail(w); double c = RigCost(ps); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c;
            ps.sv.rigs.Add(new RigSave { cell = id, buf = new float[NM] }); ps.baseVersion++;
            Toast("Bohrturm fördert " + GameData.Mats[RigMat(ps, id)].name + " (" + RichTxt(RigRich(ps.pid, id)) + "es Vorkommen)", "good"); return true;
        }
        public static bool RigUp(int cell)
        {
            var ps = Cur; var r = ps.sv.rigs.Find(x => x.cell == cell); if (r == null) return false; if (r.lv >= RIG_MAXLV) return Fail("Höchste Stufe – baue ihn zum Erzbagger um");
            double c = RigUpCost(ps, r); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c; r.lv++; Toast("Bohrturm Stufe " + r.lv + " · +30 % Förderung", "good"); return true;
        }
        public static bool RigToBagger(int cell)
        {
            var ps = Cur; var r = ps.sv.rigs.Find(x => x.cell == cell); if (r == null || r.bag) return false; if (r.lv < RIG_MAXLV) return Fail("Erst Bohrturm-Stufe " + RIG_MAXLV);
            double c = BagCost(ps); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c; r.bag = true; ps.baseVersion++; Toast("Erzbagger – gräbt alle Rohstoffe aus", "good"); return true;
        }
        public static bool FixFault(int cell)
        {
            var ps = Cur; var r = ps.sv.rigs.Find(x => x.cell == cell); if (r == null || r.faultK == "") return false; double c = FaultFixCost(ps, r); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; r.faultK = ""; r.fixedN++; Toast("Störung behoben", "good"); return true;
        }
        static readonly Dictionary<string, string> Faults = new Dictionary<string, string> { { "heat", "Überhitzung" }, { "jam", "Mechanik blockiert" }, { "error", "Steuerung gestört" }, { "storm", "Sturmschaden" }, { "rad", "Strahlenschaden an der Elektronik" }, { "impact", "Einschlagschaden" } };
        static readonly HashSet<string> Hot = new HashSet<string> { "venus", "io", "cancri" };
        public static string FaultName(RigSave r) => Faults.TryGetValue(r.faultK, out var n) ? n : r.faultK;
        static bool RigFault(PState ps, RigSave r, float dt)
        {
            if (r.faultK != "") { r.faultT -= dt * (TechOn(ps, "rig", r.cell) ? 2f : 1f); /* 10.11.: Techniker vor Ort */ if (r.faultT <= 0) { r.faultK = ""; r.fixedN++; } return r.faultK != ""; }
            if (!r.busy) return false; bool storm = ps.sv.hzOn > 0 && !ps.shOn; float rf = RadF(ps);
            double p = dt / 2400.0 * (1 + .15 * r.lv) * (r.bag ? 1.3 : 1) * (storm ? 4 : 1) * (1 + 3 * rf) / Uv("rcare", ps);
            if (Rng.NextDouble() < p)
            {
                string k = rf > 0 && Rng.NextDouble() < rf / (1 + rf) ? "rad" : storm ? "storm" : Hot.Contains(ps.pid) && Rng.NextDouble() < .6 ? "heat" : new[] { "heat", "jam", "error" }[Rng.Next(3)];
                r.faultK = k; r.faultT = ps.bld.Contains("werkstatt") ? 45 : 120;
                if (ps.pid == S.planet) Toast((r.bag ? "Erzbagger" : "Bohrturm") + ": " + (k == "storm" && ps.P.storm != null ? ps.P.storm.name + " – " : "") + FaultName(r), "bad");
                return true;
            }
            return false;
        }
        static void RigStep(PState ps, RigSave r, float dt, float f)
        {
            if (RigFault(ps, r, dt)) { r.busy = false; return; }
            int m = RigMat(ps, r.cell); float cap = RigBufCap(r);
            if (Sum(r.buf) < cap - .01f)
            {
                float x = Mathf.Min(cap - Sum(r.buf), RigRate(ps, r) * f * dt); var raws = PlanetRaws(ps); var by = raws.Where(q => q != m).ToList();
                if (r.bag) foreach (var q in raws) r.buf[q] += x / raws.Count;
                else { r.buf[m] += x * (by.Count > 0 ? .75f : 1); foreach (var q in by) r.buf[q] += x * .25f / by.Count; }
                r.mined += x; ps.sv.deep += x; r.busy = true;
            }
            else r.busy = false;
            if (r.claim >= 0 && !ps.sv.veh.Any(v => v.id == r.claim && v.rig == r.cell && (v.mode == "toRig" || v.mode == "rigload"))) r.claim = -1;
        }
        public static string RigStatus(PState ps, RigSave r) => r.faultK != "" ? "Störung: " + FaultName(r) + " · Techniker in " + Mathf.CeilToInt(r.faultT) + " s" : Sum(r.buf) >= RigBufCap(r) - .5f ? (ps.sv.veh.Count > 0 ? "Lager voll – wartet auf Abholung" : "Lager voll – kein Fahrzeug holt ab") : ps.ef < .6f ? "Bohrt langsam – zu wenig Energie" : "Bohrt" + (r.claim >= 0 ? " · Fahrzeug holt ab" : "");
        static RigSave PickRig(PState ps, VehSave v, bool idle)
        {
            if (ps.sv.rigs.Count == 0) return null; float cap = TankCap(ps, v) - Sum(v.tank); if (cap < 5) return null; RigSave best = null; float bs = 1e9f;
            foreach (var r in ps.sv.rigs) { if (r.claim >= 0 || r.dc >= 0) continue; float b = Sum(r.buf); if (b < (idle ? Mathf.Min(15, RigBufCap(r) * .05f) : RigBufCap(r) * .7f)) continue; float d = ps.pd.Arc(v.d, RigDock(ps, r)) - b * .05f; if (d < bs) { bs = d; best = r; } }
            return best;
        }

        // ================= Außenlager =================
        static readonly Dictionary<string, List<int>> Sites = new Dictionary<string, List<int>>();
        /// <summary>Vier feste Außenlager-Plätze pro Planet: je einer in jeder Richtung um die Basis, auf demselben Ring (wie im Browser).</summary>
        public static List<int> OutpostSites(string pid)
        {
            if (Sites.TryGetValue(pid, out var l)) return l; var pd = PlanetData.Get(pid); var bc = pd.cells[pd.baseCell]; int maxR = pd.cells.Max(c => c.ring);
            int ring = Mathf.Clamp(Mathf.RoundToInt(maxR * .45f), GameData.OutMinRing, Math.Max(GameData.OutMinRing, maxR - 1));
            var cand = pd.cells.Where(c => !c.IsBase && c.ring == ring).ToList(); var o = new List<int>();
            foreach (var u in new[] { bc.e1, bc.e2, -bc.e1, -bc.e2 }) { int best = -1; float bs = -1e9f; foreach (var c in cand) { if (o.Contains(c.id)) continue; float sc = Vector3.Dot(c.c - bc.c, u); if (sc > bs) { bs = sc; best = c.id; } } if (best >= 0) o.Add(best); }
            /* Fabian 01.11.: fünfter Platz genau auf der anderen Seite des Planeten */
            { int anti = -1; float bd = 2; foreach (var c in pd.cells) { if (c.IsBase || c.Wet || o.Contains(c.id)) continue; float d = Vector3.Dot(c.c.normalized, bc.c.normalized); if (d < bd) { bd = d; anti = c.id; } } if (anti >= 0) o.Add(anti); }
            Sites[pid] = o; return o;
        }
        public static double OutpostCost(PState ps) => Math.Round(20000 * ps.P.diff * Math.Pow(1.6, ps.sv.outposts.Count));
        public static string OutpostWhy(PState ps, int id)
        {
            var cl = ps.pd.cells[id];
            if (!OutpostSites(ps.pid).Contains(id)) return "Außenlager nur auf den fünf markierten Außenlager-Plätzen";
            if (!ResDone("outpost")) return "Erst Außenlager-Logistik erforschen"; if (Gs(ps) < 4) return "Erst Basisstufe 4 auf einem Planeten";
            if (!ps.clean[id] || cl.IsBase) return "Nur auf sauberen Parzellen"; if (cl.ring < GameData.OutMinRing) return "Zu nah an der Basis – mindestens " + GameData.OutMinRing + " Parzellen entfernt";
            if (ps.sv.outposts.Count >= OutpostSites(ps.pid).Count) return "Alle Außenlager-Plätze sind belegt";
            if (ps.sv.outposts.Any(o => o.cell == id)) return "Hier steht schon ein Außenlager"; return "";
        }
        public static bool BuildOutpost(int id)
        {
            var ps = Cur; string w = OutpostWhy(ps, id); if (w != "") return Fail(w); double c = OutpostCost(ps); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c;
            var o = new StSave { id = "o" + id, cell = id, buf = new float[NM] }; ps.sv.outposts.Add(o); ps.st[o.id] = o; ps.baseVersion++;
            Toast("Außenlager steht – Fahrzeuge entladen jetzt auch hier", "good"); return true;
        }
        public static float PressF(float fill) => 1 + Mathf.Clamp01((fill - .8f) / .2f);
        public static float PipeRate(PState ps, StSave o) => GameData.OutPipe * Uv("pipe", ps) * PressF(Sum(o.buf) / StBuf(ps, o.id));
        static void OutpostStep(PState ps, StSave o, float dt, float f)
        {
            float b = Sum(o.buf); o.busy = false; if (b <= .001f) return; float fr = RohCap(ps) - RohSum(ps); if (fr <= .01f) return;
            float amt = Mathf.Min(b, Mathf.Min(fr, PipeRate(ps, o) * Mathf.Max(.25f, f) * ps.bm * dt)), k = amt / b;
            for (int m = 0; m < NM; m++) { float x = o.buf[m] * k; o.buf[m] -= x; { ps.sv.roh[m] += x; RohIn(ps, m, x); } } o.busy = true;
        }
        public static string OutpostStatus(PState ps, StSave o) => Sum(o.buf) <= .001f ? "Bereit" : RohCap(ps) - RohSum(ps) <= .01f ? "Rohstofflager voll" : "Pumpt zur Basis";

        // ================= Lastdrohnen =================
        public static double LiftCost(PState ps) => Math.Round(30000 * ps.P.diff * Math.Pow(1.6, ps.sv.lifters.Count));
        public static Vector3 LifterDir(PState ps) => ps.pd.PlanDir(ps.lay.pos["lifter"]);
        public static bool BuyLifter()
        {
            var ps = Cur; if (!ps.bld.Contains("lifter")) return Fail("Erst den Schwerlast-Hangar bauen"); if (ps.sv.lifters.Count >= GameData.LiftMax) return Fail("Der Hangar ist voll");
            double c = LiftCost(ps); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c;
            ps.sv.lifters.Add(new LifterSave { id = ps.sv.lifters.Count == 0 ? 1 : ps.sv.lifters.Max(l => l.id) + 1, d = LifterDir(ps) }); Toast("Lastdrohne startklar", "good"); return true;
        }
        static bool RequestLift(PState ps, VehSave v, int cell)
        {
            if (!ps.bld.Contains("lifter") || GameData.Veh[v.type].size == "s") return false; if (ps.pd.Arc(v.d, ps.pd.cells[cell].c) < GameData.LiftMinDist) return false;
            var L = ps.sv.lifters.Find(l => l.mode == "idle"); if (L == null) return false; L.mode = "fetch"; L.veh = v.id; L.cell = cell; v.lift = L.id; v.mode = "liftwait"; return true;
        }
        static void LifterStep(PState ps, LifterSave L, float dt, float f)
        {
            var pd = ps.pd; float R = pd.R, spd = GameData.LiftSpeed * Uv("dspeed", ps) * Mathf.Max(.25f, f) * ps.vm; var v = L.veh >= 0 ? ps.sv.veh.Find(x => x.id == L.veh) : null;
            switch (L.mode)
            {
                case "idle": MoveDir(ref L.d, LifterDir(ps), spd * dt, R); break;
                case "fetch": if (v == null || v.lift != L.id) { L.mode = "back"; L.veh = -1; break; } if (MoveDir(ref L.d, v.d, spd * dt, R)) { L.mode = "carry"; v.mode = "lifted"; } break;
                case "carry":
                    {
                        if (v == null || v.lift != L.id) { L.mode = "back"; L.veh = -1; break; }
                        bool arr = MoveDir(ref L.d, pd.cells[L.cell].c, spd * dt, R); v.d = L.d;
                        if (arr) { S.stats.airlifts++; v.lift = -1; v.cell = L.cell; v.ti = -1; v.mode = "drive"; ps.vnav.Remove(v.id); L.veh = -1; L.mode = "back"; }
                        break;
                    }
                case "back": if (MoveDir(ref L.d, LifterDir(ps), spd * dt, R)) L.mode = "idle"; break;
                default: L.mode = "idle"; break;
            }
        }
        public static string LifterStatus(PState ps, LifterSave L)
        {
            var v = L.veh >= 0 ? ps.sv.veh.Find(x => x.id == L.veh) : null;
            switch (L.mode) { case "fetch": return v != null ? "Holt " + GameData.Veh[v.type].name + " #" + v.id : "Fliegt los"; case "carry": return v != null ? "Fliegt " + GameData.Veh[v.type].name + " zur Parzelle" : "Fliegt"; case "back": return "Fliegt zurück"; }
            return "Bereit im Hangar";
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
        /* Fabian 01.11.: manche Drohnen standen nur herum – verwaiste Reservierungen (Fahrzeug/Außenlager/Bohrturm wartet auf eine Drohne, die es nicht mehr anfliegt) alle 2 s lösen */
        static float droneSanT;
        static void DroneSanity(PState ps, float dt)
        {
            droneSanT += dt; if (droneSanT < 2) return; droneSanT = 0; var dl = ps.sv.drones;
            foreach (var v in ps.sv.veh) if (v.drone >= 0 && !dl.Any(d => d.id == v.drone && d.tk == "v" && d.tgt == v.id && (d.mode == "out" || d.mode == "load"))) v.drone = -1;
            foreach (var o in ps.sv.outposts) if (o.dc >= 0 && !dl.Any(d => d.id == o.dc && d.tk == "o" && d.tko == o.id && (d.mode == "out" || d.mode == "load"))) o.dc = -1;
            foreach (var r in ps.sv.rigs) if (r.dc >= 0 && !dl.Any(d => d.id == r.dc && d.tk == "r" && d.tgt == r.cell && (d.mode == "out" || d.mode == "load"))) r.dc = -1;
        }
        static void DroneStep(PState ps, DroneSave dr, float dt, float f)
        {
            var pd = ps.pd; float R = pd.R; var hd = HangarDir(ps); float spd = DroneSpeed(ps) * Mathf.Max(.25f, f) * ps.vm, cap = DroneCap(ps), load = Sum(dr.tank); dr.busy = false;
            RigSave TgtR() { if (dr.tk != "r") return null; var r = ps.sv.rigs.Find(x => x.cell == dr.tgt); return r != null && r.dc == dr.id ? r : null; }
            VehSave Tgt() { if (dr.tk == "o" || dr.tk == "r") return null; var v = ps.sv.veh.Find(x => x.id == dr.tgt); return v != null && v.drone == dr.id ? v : null; }
            StSave TgtO() { if (dr.tk != "o") return null; return ps.st.TryGetValue(dr.tko, out var o) && o.dc == dr.id ? o : null; }
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
                            float tk = Sum(v.tank), c = TankCap(ps, v); if (tk < c * .25f) continue; /* früher abholen: weniger Leerlauf im Hangar */ float dist = pd.Arc(v.d, hd); if (dist < GameData.DroneMinDist) continue;
                            float sc = tk / c * dist; if (sc > bs) { bs = sc; best = v; }
                        }
                        StSave bo = null;
                        foreach (var o in ps.sv.outposts) { if (o.dc >= 0) continue; float b = Sum(o.buf), fl = b / StBuf(ps, o.id); if (b < cap * .8f || fl < .5f) continue; float sc = fl * 90; if (sc > bs) { bs = sc; bo = o; best = null; } }
                        RigSave br = null;
                        foreach (var r in ps.sv.rigs) { if (r.dc >= 0 || r.claim >= 0) continue; float b = Sum(r.buf); if (b < Mathf.Min(cap * .8f, RigBufCap(r) * .6f)) continue; float sc = b / RigBufCap(r) * pd.Arc(RigDock(ps, r), hd); if (sc > bs) { bs = sc; br = r; bo = null; best = null; } }
                        if (br != null) { br.dc = dr.id; dr.tk = "r"; dr.tgt = br.cell; dr.mode = "out"; }
                        else if (bo != null) { bo.dc = dr.id; dr.tk = "o"; dr.tko = bo.id; dr.tgt = -1; dr.mode = "out"; }
                        else if (best != null) { best.drone = dr.id; dr.tk = "v"; dr.tgt = best.id; dr.mode = "out"; }
                        break;
                    }
                case "out":
                    {
                        if (dr.tk == "o") { var o = TgtO(); if (o == null) { dr.mode = "back"; dr.tk = "v"; break; } dr.busy = true; if (MoveDir(ref dr.d, StDir(ps, o.id, 'c'), spd * dt, R)) dr.mode = "load"; break; }
                        if (dr.tk == "r") { var rr = TgtR(); if (rr == null) { dr.mode = "back"; dr.tk = "v"; dr.tgt = -1; break; } dr.busy = true; if (MoveDir(ref dr.d, RigDock(ps, rr), spd * dt, R)) dr.mode = "load"; break; }
                        var v = Tgt(); if (v == null) { dr.mode = "back"; dr.tgt = -1; break; } dr.busy = true; if (MoveDir(ref dr.d, v.d, spd * dt, R)) dr.mode = "load"; break;
                    }
                case "load":
                    {
                        if (dr.tk == "r")
                        {
                            var rr = TgtR(); if (rr == null) { dr.mode = "back"; dr.tk = "v"; dr.tgt = -1; break; } dr.busy = true; float rb = Sum(rr.buf), ra = Mathf.Min(rb, Mathf.Min(cap - load, GameData.DroneRate * dt));
                            if (ra > 0 && rb > 0) { float k = ra / rb; for (int m = 0; m < NM; m++) { float x = rr.buf[m] * k; rr.buf[m] -= x; dr.tank[m] += x; } }
                            if (Sum(rr.buf) < .05f || cap - Sum(dr.tank) < .05f) { rr.dc = -1; dr.tk = "v"; dr.tgt = -1; S.stats.lifts++; dr.sid = DroneStation(ps, null) ?? ""; dr.mode = dr.sid != "" ? "toSt" : "back"; }
                            break;
                        }
                        if (dr.tk == "o")
                        {
                            var o = TgtO(); if (o == null) { dr.mode = "back"; dr.tk = "v"; break; } dr.busy = true; float ob = Sum(o.buf), oa = Mathf.Min(ob, Mathf.Min(cap - load, GameData.DroneRate * dt));
                            if (oa > 0 && ob > 0) { float k = oa / ob; for (int m = 0; m < NM; m++) { float x = o.buf[m] * k; o.buf[m] -= x; dr.tank[m] += x; } }
                            if (Sum(o.buf) < .05f || cap - Sum(dr.tank) < .05f) { o.dc = -1; dr.tk = "v"; dr.tko = ""; S.stats.lifts++; dr.sid = DroneStation(ps, null) ?? ""; dr.mode = dr.sid != "" ? "toSt" : "back"; }
                            break;
                        }
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
                        if (load < .05f) { Array.Clear(dr.tank, 0, NM); dr.mode = "idle"; break; } float fr = RohCap(ps) - RohSum(ps); if (fr <= .01f) { dr.wt += dt; if (dr.wt > 4) { dr.wt = 0; var alt = DroneStation(ps, null); if (alt != null) { dr.sid = alt; dr.mode = "toSt"; } } break; } /* Lager voll: Ladung zur Entladeanlage bringen statt zu warten */
                        float amt = Mathf.Min(load, Mathf.Min(fr, GameData.DroneRate * dt)), k = amt / load; for (int m = 0; m < NM; m++) { float x = dr.tank[m] * k; dr.tank[m] -= x; { ps.sv.roh[m] += x; RohIn(ps, m, x); } }
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
                case "out": { if (dr.tk == "o") return "Fliegt zum Außenlager"; if (dr.tk == "r") return "Fliegt zum Bohrturm"; var v = ps.sv.veh.Find(x => x.id == dr.tgt); return v != null ? "Fliegt zu " + GameData.Veh[v.type].name + " #" + v.id : "Fliegt los"; }
                case "load": return "Übernimmt Ladung";
                case "toSt": return "Bringt die Ladung zur " + (ps.bld.Contains(dr.sid) ? GameData.Bld[dr.sid].name : "Station");
                case "stdrop": return dr.wt > 0 && !string.IsNullOrEmpty(dr.sid) ? GameData.Bld[dr.sid].name + " ist voll – wartet" : "Entlädt in die " + (string.IsNullOrEmpty(dr.sid) ? "Station" : GameData.Bld[dr.sid].name);
                case "back": return Sum(dr.tank) > .05f ? "Fliegt zum Hangar (Stationen voll)" : "Fliegt zurück";
                case "drop": return RohCap(ps) - RohSum(ps) <= .01f ? "Rohstofflager voll – wartet" : "Lädt ins Rohstofflager";
            }
            return dr.mode;
        }
        /// <summary>Kein Parzellen-Auftrag: statt herumzustehen erledigt das Fahrzeug Wartung, lädt den Akku oder bringt Restladung weg.</summary>
        static bool IdleJob(PState ps, VehSave v)
        {
            if (ps.bld.Contains("werkstatt") && v.rep < VM_REPS && v.wear >= .5f && S.credits >= RepCost(ps, v)) { if (SendToShop(ps, v, true)) return true; }
            if (GameData.Veh[v.type].batt > 0 && Bat(ps, v) < BattCap(ps, v) * .9f && ChooseStation(ps, v) != null) { v.mode = "toUnload"; return true; }
            if (Sum(v.tank) > .05f) { v.mode = "toUnload"; return true; }
            return false;
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
            switch (v.mode) { case "boom": return "Selbstzerstörung in " + Mathf.CeilToInt(v.repT) + " s"; case "sweep": return v.sweepT > 0 && v.cell >= 0 && ps.pd.Arc(v.d, ps.pd.cells[v.cell].c) < ps.pd.cells[v.cell].H ? "Nachlese: sammelt Reststaub und Kleinteile" : "Fährt zur Nachlese"; case "toCrystal": return "Fährt zu einem Leuchtkristall"; case "mineCr": return "Baut einen Leuchtkristall ab"; case "seek": return ps.sv.veh.Contains(v) && ps.front.Count == 0 && ps.act.Count == 0 ? (ps.sv.rigs.Count > 0 ? "Wartet auf volle Bohrtürme" : "Planet sauber – wartet in der Garage") : "Sucht Arbeit"; case "drive": return "Fährt zum Einsatz" + (v.cell >= 0 && ps.sv.veh.Contains(v) ? " · noch " + Mathf.RoundToInt(ps.pd.Arc(v.d, ps.pd.cells[v.cell].c)) + " m" : ""); case "clean": return "Reinigt"; case "toUnload": return IsOut(v.sid) ? "Fährt zum Außenlager" : "Fährt zur Entladestation"; case "queue": return "Dockt an"; case "unload": return "Entlädt"; case "wait": return "Wartet auf Drohne"; case "liftwait": return "Wartet auf Lastdrohne"; case "lifted": return "Wird eingeflogen"; case "toShop": return "Fährt zur Werkstatt"; case "toRig": return "Fährt zum Bohrturm"; case "toRoh": return "Keine passende Entladestation – fährt direkt zum Rohstofflager"; case "unloadRoh": return "Entlädt ins Rohstofflager"; case "fieldRepair": return "Repariert sich notdürftig · noch " + Mathf.CeilToInt(v.repT) + " s"; case "rigload": return "Lädt am Bohrturm"; case "repair": return "Wird repariert · noch " + Mathf.CeilToInt(v.repT) + " s"; case "broken": return v.rep >= VM_REPS ? WearText(v) : ps.bld.Contains("werkstatt") ? "Ausgefallen – wartet auf die Werkstatt" : "Ausgefallen – baue eine Roboterwerkstatt"; }
            return v.mode;
        }

        // ---------- Bewegung auf der Kugel ----------
        public static bool MoveDir(ref Vector3 d, Vector3 t, float step, float R)
        {
            float c = Mathf.Clamp(Vector3.Dot(d, t), -1, 1), ang = Mathf.Acos(c); if (ang * R <= step) { d = t; return true; }
            var w = (t - d * c).normalized; float a = step / R; d = (d * Mathf.Cos(a) + w * Mathf.Sin(a)).normalized; return ang * R - step < .35f;
        }
        static bool NavMove(PState ps, CartSave c, Vector3 t, float step, int id) { var d = c.d; bool r = NavMoveD(ps, ref d, t, step, -999); c.d = d; return r; }
        static bool NavMove(PState ps, VehSave v, Vector3 t, float step, int id) { var d = v.d; bool r = NavMoveD(ps, ref d, t, step, GameData.Veh[v.type].fly ? -2000 - id : id); v.d = d; return r; }

        // ---------- Seen (Wasser, Lava, Säure, Methan): Bodenfahrzeuge fahren außen herum – genau wie im Browser ----------
        static bool LiqClear(PlanetData pd, Vector3 a, Vector3 b, int ign)
        {
            int n = Mathf.Max(2, Mathf.CeilToInt(pd.Arc(a, b) / .5f)); var sd = Vector3.Cross(a, b - a).normalized / pd.R; int g = -1, g1 = -1, g2 = -1;
            for (int i = 1; i <= n; i++)
            {
                float q = i / (float)n; var p = (a * (1 - q) + b * q).normalized;
                g = pd.CellOf(p, g); if (pd.cells[g].Wet && g != ign) return false;
                g1 = pd.CellOf((p + sd * .9f).normalized, g1); if (pd.cells[g1].Wet && g1 != ign) return false;
                g2 = pd.CellOf((p - sd * .9f).normalized, g2); if (pd.cells[g2].Wet && g2 != ign) return false;
            }
            return true;
        }
        static List<int> LiqPath(PlanetData pd, int from, int to)
        {
            if (from == to) return new List<int> { to }; var prev = new int[pd.N]; for (int i = 0; i < prev.Length; i++) prev[i] = -1; prev[from] = from; var q = new List<int> { from }; int h = 0;
            while (h < q.Count) { int a = q[h++]; if (a == to) break; foreach (int b in pd.cells[a].nb) if (prev[b] < 0 && (!pd.cells[b].Wet || b == to)) { prev[b] = a; q.Add(b); } }
            if (prev[to] < 0) return null; var p = new List<int>(); for (int c = to; c != from; c = prev[c]) p.Add(c); p.Reverse(); return p;
        }
        static bool LiqDetour(PState ps, int id, Vector3 d, Vector3 t, out Vector3 goal)
        {
            goal = t; if (!GameData.PLiq.ContainsKey(ps.pid)) return false; var pd = ps.pd;
            if (!ps.lqnav.TryGetValue(id, out var L)) { L = new LqNav(); ps.lqnav[id] = L; }
            int tc = pd.CellOf(t, L.to >= 0 ? L.to : -1);
            if (L.to != tc) { L.to = tc; L.p = null; L.free = false; }
            if (L.free) return false;
            if (L.p == null) { if (LiqClear(pd, d, t, tc)) { L.free = true; return false; } L.p = LiqPath(pd, pd.CellOf(d), tc); L.i = 0; L.k = 0; if (L.p == null) { L.free = true; return false; } }
            if ((L.k++) % 12 == 0) for (int k = Math.Min(L.p.Count - 1, L.i + 6); k > L.i; k--) if (LiqClear(pd, d, pd.cells[L.p[k]].c, tc)) { L.i = k; break; }
            float dg = pd.Arc(d, pd.cells[L.p[L.i]].c); if (dg < .4f || (dg < 2 && (L.i + 1 >= L.p.Count || LiqClear(pd, d, pd.cells[L.p[L.i + 1]].c, tc)))) L.i++;
            if (L.i >= L.p.Count) { L.p = null; L.free = true; return false; }
            goal = pd.cells[L.p[L.i]].c; return true;
        }

        const float ZONE = 31; const int NN = 66, NOFF = 33;
        static float SegD(Vector2 q, Vector2 a, Vector2 c) { float dx = c.x - a.x, dy = c.y - a.y, l2 = dx * dx + dy * dy; if (l2 == 0) l2 = 1; float t = Mathf.Clamp01(((q.x - a.x) * dx + (q.y - a.y) * dy) / l2); return Mathf.Sqrt((q.x - a.x - dx * t) * (q.x - a.x - dx * t) + (q.y - a.y - dy * t) * (q.y - a.y - dy * t)); }
        static int NavIdx(float x, float y) => Mathf.Clamp((int)Mathf.Floor(y + NOFF), 0, NN - 1) * NN + Mathf.Clamp((int)Mathf.Floor(x + NOFF), 0, NN - 1);
        static Vector2 NavPt(int k) => new Vector2(k % NN - NOFF + .5f, k / NN - NOFF + .5f);
        static bool[] NavGrid(PState ps)
        {
            string key = string.Join(",", GameData.BOrder.Where(ps.bld.Contains)) + "|" + Gs(ps) + "|" + ps.visVer + MoveKey(ps); /* 03.11.: verschobene Gebäude */
            if (ps.navG != null && ps.navKey == key) return ps.navG;
            var g = new bool[NN * NN]; var blds = GameData.BOrder.Where(ps.bld.Contains).ToList(); var belts = ps.lay.belts.Where(b => BeltLive(ps, b) && !HiddenBelt(b)).ToList();
            for (int j = 0; j < NN; j++) for (int i = 0; i < NN; i++)
                {
                    var q = new Vector2(i - NOFF + .5f, j - NOFF + .5f); bool bl = false;
                    foreach (var id in blds) if ((q - ps.lay.pos[id]).magnitude < GameData.Bld[id].fp + .9f) { bl = true; break; }
                    if (!bl) foreach (var b in belts) { var rp = ps.visBelt.TryGetValue(b.id, out var vr) ? vr : b.rp; for (int k = 0; k < rp.Length - 1; k++) if (SegD(q, rp[k], rp[k + 1]) < 1.2f) { bl = true; break; } if (bl) break; }
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
                if (id >= 0 && LiqDetour(ps, id, d, t, out var lg)) { MoveDir(ref d, lg, step, pd.R); return false; }
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
            double c = UnlockCost(pid); if (S.credits < c) return Fail(Lacks(c));
            S.credits -= c; var sv = NewPlanet(pid); S.planets.Add(sv); P[pid] = Attach(sv); string from = S.planet; double carry = CarryTo(from, pid); Travel(pid); if (carry > 0) Toast("Startbudget " + Fmt(carry) + " Cr mitgenommen – der Rest bleibt in der Kasse von " + GameData.Planets[from].name, "good");
            Toast(GameData.Planets[pid].name + ": neuer Anfang – Basis, Technik und Fahrzeug-Upgrades baust du hier selbst auf.", "good"); return true;
        }
        public static void Travel(string pid) { if (!P.ContainsKey(pid) || S.planet == pid) return; if (Icp != null) { Fail("Erst die Meteorbrocken abwehren!"); return; } if (Ruined(pid)) { Fail(GameData.Planets[pid].name + " wird gerade neu aufgebaut – noch " + Mathf.CeilToInt(RuinLeft(pid) / 60f) + " min"); return; } SwapWallet(S.planet, pid); S.planet = pid; strokeCells.Clear(); lastCell = -1; OnPlanetChanged?.Invoke(); } /* 03.11.: Planetenkasse wechselt mit */

        // ================= Ziele =================
        static PState Moon => P["moon"];
        static bool HasVeh(string t) => P.Values.Any(p => p.sv.veh.Any(v => v.type == t));
        static float GateP(string a, string b) => P.ContainsKey(b) ? 1 : P.ContainsKey(a) ? Mathf.Min(.99f, CleanPct(P[a]) / .8f) : 0;
        static float StartProg() { var m = Moon; return m.act.TryGetValue(m.pd.start, out var a) ? Progress(a) : m.clean[m.pd.start] ? 1 : 0; }
        public static int QuestsSupported = 57;
        static float Made(string pid) => P.ContainsKey(pid) ? P[pid].mach.Values.Sum(m => (float)m.made) : 0;
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
                case 6: return Mathf.Min(1f, S.stats.handUnloads / 3f); /* früher: Sammelwagen kaufen (abgeschafft) */
                case 7: return m.sv.stage >= 2 ? 1 : Mathf.Min(.99f, (float)(S.credits / 200));
                case 8: return m.bld.Contains("u1") ? 1 : 0;
                case 9: return Hard || m.sv.veh.Any(v => v.type == "buggy") ? 1 : 0; /* Hard Mode: übersprungen */
                case 10: return m.bld.Contains("presse") ? 1 : 0;
                case 11: return S.stats.bricks > 0 ? 1 : 0;
                case 12: return m.sv.stage >= 3 ? 1 : Mathf.Min(.99f, (float)(S.credits / 5000));
                case 13: return m.bld.Contains("schmelze") ? 1 : 0;
                case 14: return S.stats.barren > 0 ? 1 : 0;
                case 16: return Hard || P.Values.Any(p => p.sv.drones.Count > 0) ? 1 : P.Values.Any(p => p.bld.Contains("hangar")) ? .5f : 0;
                case 17: return CleanPct(Moon) / .8f;
                case 18: return P.ContainsKey("mars") ? 1 : 0;
                case 19: return P.Values.Any(p => p.sv.outposts.Count > 0) ? 1 : ResDone("outpost") ? .3f : 0;
                case 20: return GateP("mars", "europa");
                case 21: return P.Values.Any(p => Lvl("ecap", p) >= 3) ? 1 : P.Values.Max(p => Lvl("ecap", p) / 3f);
                case 22: return any("rtg") ? 1 : ResDone("rtg") ? .5f : 0;
                case 23: return any("raff") ? 1 : ResDone("raff") ? .5f : 0;
                case 24: return HasVeh("eisfraese") ? 1 : 0;
                case 25: return ResDone("shield") ? 1 : 0;
                case 26: return GateP("europa", "titan");
                case 27: return HasVeh("schlammraupe") ? 1 : 0;
                case 28: return any("geo") ? 1 : 0;
                case 29: return GateP("titan", "io");
                case 30: return HasVeh("lavapanzer") ? 1 : 0;
                case 31: return ResDone("nuclear") ? 1 : 0;
                case 32: return any("reactor") ? 1 : 0;
                case 33: return P.Values.Any(p => p.sv.lifters.Count > 0) ? 1 : ResDone("airlift") ? .5f : 0;
                case 34: return GateP("io", "venus");
                case 35: return DoneCount() > 0 ? 1 : P.Values.Max(p => CleanPct(p));
                case 36: return GateP("venus", "ganymed");
                case 37: return HasVeh("magnetgleiter") ? 1 : 0;
                case 38: return GateP("ganymed", "enceladus");
                case 39: return GateP("enceladus", "triton");
                case 40: return HasVeh("kryokoloss") ? 1 : 0;
                case 41: return GateP("triton", "pluto");
                case 42: return P.ContainsKey("pluto") ? CleanPct(P["pluto"]) / .8f : 0;
                case 43: return S.resets > 0 ? 1 : 0;
                case 44: return GateP("pluto", "eris");
                case 45: return Made("eris") > 0 ? 1 : 0;
                case 46: return GateP("eris", "sedna");
                case 47: return GateP("sedna", "proxima");
                case 48: return P.ContainsKey("proxima") ? Lvl("rshield", P["proxima"]) / 3f : 0;
                case 49: return GateP("proxima", "trappist");
                case 50: return GateP("trappist", "kepler");
                case 51: return GateP("kepler", "cancri");
                case 52: return Made("cancri") / 100f;
                case 53: return GateP("cancri", "gliese");
                case 54: return GateP("gliese", "poltergeist");
                case 55: return Made("poltergeist") > 0 ? 1 : 0;
                case 56: return P.ContainsKey("poltergeist") ? CleanPct(P["poltergeist"]) / .8f : 0;
                case 15: return ResDone("drones") ? 1 : S.resAct == "drones" ? S.resT / 300 : 0;
            }
            return 0;
        }
        /// <summary>Wohin ein Tipp auf das aktuelle Ziel führt: "front" (nächste Parzelle), "hand" (Behälter), "sheet:&lt;Fenster&gt;", "build:&lt;Gebäude&gt;", "outpost".</summary>
        public static string QuestTarget(int q)
        {
            var c = Cur; string Gate(string pf) => P.ContainsKey(pf) && CleanPct(P[pf]) < .8f ? (S.planet == pf ? "front" : "sheet:planets") : "sheet:planets";
            string Res(string r, string then) => ResDone(r) ? then : "sheet:research";
            switch (q)
            {
                case 0: case 2: case 4: return "front";
                case 1: return "hand";
                case 3: case 11: case 14: return "sheet:production";
                case 5: return "sheet:upgrades";
                case 9: return "sheet:vehicles"; case 6: return "hand";
                case 7: case 12: return "sheet:base";
                case 8: return "build:u1"; case 10: return "build:presse"; case 13: return "build:schmelze";
                case 15: return "sheet:research"; case 16: return c.bld.Contains("hangar") ? "build:hangar" : Res("drones", "build:hangar");
                case 17: return S.planet == "moon" ? "front" : "sheet:planets";
                case 18: return "sheet:planets";
                case 19: return Res("outpost", "outpost");
                case 20: return Gate("mars"); case 26: return Gate("europa"); case 29: return Gate("titan"); case 34: return Gate("io");
                case 36: return Gate("venus"); case 38: return Gate("ganymed"); case 39: return Gate("enceladus"); case 41: return Gate("triton");
                case 42: return S.planet == "pluto" ? "front" : "sheet:planets";
                case 43: return "sheet:planets";
                case 44: return Gate("pluto"); case 46: return Gate("eris"); case 47: return Gate("sedna"); case 49: return Gate("proxima"); case 50: return Gate("trappist"); case 51: return Gate("kepler"); case 53: return Gate("cancri"); case 54: return Gate("gliese");
                case 45: case 52: case 55: return "sheet:production";
                case 48: return "sheet:upgrades";
                case 56: return "front";
                case 21: return "build:energy";
                case 22: return Res("rtg", "build:rtg"); case 23: return Res("raff", "build:raff"); case 25: return "sheet:research";
                case 24: case 27: case 30: case 37: case 40: return "sheet:vehicles";
                case 28: return "build:geo"; case 31: return "sheet:research"; case 32: return Res("nuclear", "build:reactor");
                case 33: return Res("airlift", "build:lifter");
                case 35: return "front";
            }
            return "front";
        }
        public static bool ClaimQuest()
        {
            int q = S.quest; if (q >= QuestsSupported || q >= GameData.Quests.Count || QuestProgress(q) < 1) return false;
            double qr = Math.Round(GameData.Quests[q].r * GoalF); Earn(qr); S.quest++; Toast("Ziel erreicht · +" + Fmt(qr) + " Cr", "good"); return true;
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
        public static float EnergyCap(PState ps) => (150 + (ps.bld.Contains("energy") ? 150 : 0) + Uv("ecap", ps)) * BlF(ps, "energy") + (ps.bld.Contains("reactor") ? 1000 : 0);
        public static float SolarProd(PState ps) => (3 + (ps.bld.Contains("energy") ? 8 : 0)) * Uv("energy", ps) * BlF(ps, "energy") * ps.solar * GameData.SolarPeak * (EvOn(ps, "solar") ? FeatureData.SOLAR_MULT : 1);
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
            if (!string.IsNullOrEmpty(d.req) && !ResDone(d.req)) return "Erst „" + GameData.Res.Find(r => r.id == d.req).name + "“ erforschen";
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
        static void BattUse(PState ps, VehSave v, float dt) { if (GameData.Veh[v.type].batt <= 0) return; v.bat = Mathf.Max(0, Bat(ps, v) - dt * VehBonus(v, "energy")); }

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
                else if (sv.hzT >= st.every) { sv.hzOn = st.dur; sv.hzT = 0; sv.hzWarned = false; cur = st; S.stats.storms++; if (ps.pid == S.planet) Toast(st.name + " zieht auf!", "bad"); }
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
            foreach (var dr in sv.drones) if (dr.busy) demand += .4f * vE; if (ps.shOn) demand += 4; foreach (var L in sv.lifters) if (L.mode != "idle") demand += 2 * vE; foreach (var o in sv.outposts) if (o.busy) demand += .2f; foreach (var r in sv.rigs) if (r.busy) demand += GameData.RigEnergy * Uv("meff", ps);
            foreach (var m in new[] { "presse", "schmelze", "raff" }) if (ps.bld.Contains(m) && ps.mach[m].cur >= 0) demand += GameData.Mach[m].x * Uv("meff", ps);
            foreach (var b in ps.lay.belts) if (BeltLive(ps, b) && ps.belts[b.id].Count > 0) demand += .1f * Uv("beff", ps);
            if (sv.msLv > 0 && sv.msCool <= 0) demand += .5f * sv.msLv; /* Meteorschild hält sich bereit */ ps.demand = demand * ps.P.energy; ps.prodNow = EnergyProd(ps) * TerraEnergyF(ps);
            demand *= ps.P.energy * dt;
            float avail = sv.energy + ps.prodNow * dt, f = demand > 0 ? Mathf.Min(1, avail / demand) : 1;
            sv.energy = Mathf.Max(0, Mathf.Min(EnergyCap(ps), avail - demand * f)); ps.ef = f;
        }

        // ================= Simulation =================
        public static void Step(float dt)
        {
            foreach (var ps in P.Values) StepPlanet(ps, dt);
            ResearchStep(dt); DoomStep(dt); ChalStep(dt); OrdersStep(dt); FindStep(dt);
            AchievementStep(dt); SuitStep(dt); if (!Offline) { FloatFlush(); ToastFlush(); StatTick(dt); if (Time.frameCount % 600 == 0 || S.contracts == null || S.contracts.list.Count == 0) DailyContracts(); }
        }
        static void StepPlanet(PState ps, float dt) { earnPid = ps.pid; try { StepPlanet0(ps, dt); CrewStep(ps, dt); MedalStep(ps, dt); } finally { earnPid = null; RohTrack(ps, dt); } }
        static void StepPlanet0(PState ps, float dt)
        {
            if (RestStep(ps, dt)) return; /* Fabian 02.11.: ruhender, geretteter Planet – nur Einkommen */
            if (ps.pid == S.planet) ps.sv.tPlay += dt;
            RateTrack(ps, dt);
            if (Hard) { HardCart(ps); CartStep(ps, dt); DepotStep(ps, dt); PerkStep(ps, dt); WaveStep(ps, dt); }
            SecretStep(ps, dt); /* 08.11.: Mond-Geheimnis, beide Modi */ /* Hard Mode: Sammelwagen fährt, Buffs, Putzhelfer */
            ps.sv.careT += dt; if (ps.sv.boostT > 0) ps.sv.boostT -= dt; EnergyStep(ps, dt); PutziStep(ps, dt); Build34Step(ps, dt); float f = ps.ef * (ps.sv.hitT > 0 ? .5f : 1f) * CareF(ps) * (ps.sv.boostT > 0 ? 1.5f : 1f); /* 04.11.: Kristall-Schub */ /* 03.11.: Betreuung */ /* Einschlagschaden: Anlagen auf halber Kraft */
            foreach (var v in ps.sv.veh.ToArray()) { VehStep(ps, v, dt, f); StuckCheck(ps, v, dt); }
            if (ps.bld.Contains("hangar")) { DroneSanity(ps, dt); foreach (var dr in ps.sv.drones) DroneStep(ps, dr, dt, f); }
            if (ps.bld.Contains("lifter")) foreach (var L in ps.sv.lifters) LifterStep(ps, L, dt, f); else foreach (var v in ps.sv.veh) if (v.mode == "liftwait" || v.mode == "lifted") { v.lift = -1; v.mode = "drive"; }
            CrystalStep(ps, dt);
            foreach (var r in ps.sv.rigs) { RigStep(ps, r, dt, f); if (r.dc >= 0 && !(ps.bld.Contains("hangar") && ps.sv.drones.Any(d => d.id == r.dc && d.tk == "r" && d.tgt == r.cell && (d.mode == "out" || d.mode == "load")))) r.dc = -1; }
            foreach (var o in ps.sv.outposts) { OutpostStep(ps, o, dt, f); if (o.dc >= 0 && !(ps.bld.Contains("hangar") && ps.sv.drones.Any(d => d.id == o.dc && d.tk == "o" && d.tko == o.id && (d.mode == "out" || d.mode == "load")))) o.dc = -1; }
            foreach (var v in ps.sv.veh) if (v.drone >= 0 && !(ps.bld.Contains("hangar") && ps.sv.drones.Any(d => d.tgt == v.id && d.id == v.drone))) v.drone = -1;
            int sub = dt > .3f ? Mathf.CeilToInt(dt / .25f) : 1; float sdt = dt / sub;
            for (int s = 0; s < sub; s++) { foreach (var b in ps.lay.belts) BeltStep(ps, b, sdt, f); foreach (var m in new[] { "presse", "schmelze", "raff" }) MachStep(ps, m, sdt, f); }
            SellStep(ps, dt); AutoDumpStep(ps); FlowTick(ps, dt);
            if (!Offline && ps.pid == S.planet) EventStep(ps, dt); TeamStep(ps, dt);
            MeteorStep(ps, dt);
            List<int> done = null; foreach (var kv in ps.act) if (kv.Value.sum < kv.Value.cl.nIn * GameData.DONE) (done = done ?? new List<int>()).Add(kv.Key);
            if (done != null) foreach (int id in done) FinishCell(ps, id);
        }

        public static float OfflineCap => Platform.Owned("offline24") ? 86400 : (ResDone("off24") ? 86400 : ResDone("off12") ? 43200 : 28800) + 7200 * SdLv("offline");
    }
}
