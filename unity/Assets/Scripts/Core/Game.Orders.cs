// Fabian 02.11.: Bestellungen von Besuchern (Touristen, Händler, Forscher …). Sie landen auf einem deiner Planeten und wollen
// bestimmte Waren – auch von anderen Planeten. Bestellte Waren werden im Bestelllager zurückgelegt, bevor die Rakete sie verkauft.
// Läuft in derselben Simulation (Game.Step), live und offline. 5 Plätze, mit Ausbau bis 10.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class OrderItem { public string src = ""; public int m = -1; public string g = ""; public float n, got; } // src = Herkunftsplanet; m = Ware oder g = Spezialgut
    [Serializable] public class VisitOrder { public int id; public string who = "", pid = ""; public List<OrderItem> items = new List<OrderItem>(); public long until; public double reward; public int sd; public int ext; [NonSerialized] public bool ready, go; } // ext = 09.11.: schon einmal verlängert

    public static partial class Game
    {
        public const int ORDER_BASE = 5, ORDER_MAXLV = 5;
        static readonly string[] Visitors = { "Touristengruppe vom Kreuzfahrtschiff", "Händlerin Nova", "Forschungsschiff „Kepler“", "Sammler Orion", "Hotel „Mondblick“", "Bergbaugilde", "Museum der Raumfahrt", "Kolonie „Neue Hoffnung“", "Rennteam „Komet“", "Botschafterin Lyra" };
        static List<VisitOrder> VO => S.vorders ?? (S.vorders = new List<VisitOrder>());
        public static List<VisitOrder> Orders => VO;
        public static int OrderMax() => ORDER_BASE + Mathf.Clamp(S.voLv, 0, ORDER_MAXLV);
        public static double OrderLvCost() => Math.Round(50000 * Math.Pow(3, S.voLv));
        public static event Action<VisitOrder> OnOrderNew, OnOrderDone;
        public static bool OrdersOpen => S != null && S.planets.Any(p => p.stage >= 2);

        /// <summary>Was ein Planet sicher liefern kann: Rohstoffe seiner Schmutzarten und Produkte gebauter Maschinen.</summary>
        static List<int> Producible(PState ps)
        {
            var l = new List<int>();
            foreach (var t in ps.P.types) { if (t.Value <= 0) continue; int m = GameData.Dirt[t.Key].mat; if (m >= 0 && !l.Contains(m) && (RohRateH(ps, m) > .5f || (!IsDone(ps.pid) && ps.sv.roh[m] > 5))) l.Add(m); } /* 10.11.: Rohstoffe nur, wenn wirklich welche hereinkommen */ /* 08.11.: auf sauberen Planeten nur, was wirklich noch hereinkommt */
            /* nur Rezepte, die wirklich laufen können: Plan freigeschaltet und Zutat vorhanden (2 Runden für Ketten) */
            void Add(string mach, List<Recipe> rs) { if (!ps.bld.Contains(mach)) return; foreach (var r in rs) if (l.Contains(r.inp) && !l.Contains(r.outp)) l.Add(r.outp); } /* 09.11.: was die Maschinen können, wird für Bestellungen auch gemacht */
            for (int round = 0; round < 2; round++) { Add("presse", ps.line.presse); Add("schmelze", ps.line.schmelze); Add("raff", ps.line.raff); }
            return l;
        }
        /// <summary>Fabian 05.11.: Wie viel von Ware m die Besucher bei Planet ps noch brauchen (nur Waren dieses Planeten).</summary>
        public static float OrderNeed(PState ps, int m)
        {
            if (S == null || S.vorders == null || m < 0) return 0; float n = 0;
            foreach (var o in S.vorders) foreach (var it in o.items) if (it.g == "" && it.m == m && it.src == ps.pid) n += it.n; /* 10.11.: Bestelltes bleibt bis zur Abholung im Lager – reserviert ist die ganze Menge */
            return n;
        }
        /// <summary>10.11.: So viel von Ware m bleibt im Produktlager liegen (für Bestellungen) – nur der Rest darf in den Frachter.</summary>
        public static float KeepProd(PState ps, int m) { float need = OrderNeed(ps, m); if (need <= 0) return 0; return GameData.Mats[m].raw ? Mathf.Max(0, need - ps.sv.roh[m]) : need; }
        /// <summary>10.11.: So viel von Rohstoff m bleibt im Rohstofflager (für Bestellungen) – Maschinen und Verkauf nehmen nur den Rest.</summary>
        public static float KeepRoh(PState ps, int m) { float need = OrderNeed(ps, m); return need <= 0 ? 0 : Mathf.Max(0, need - ps.sv.prod[m]); }
        /// <summary>Was im Lager dieses Planeten für Bestellungen bereitliegt (für die Lageranzeige).</summary>
        public static List<(string name, float ready, float n, string who)> OrderReady(PState ps)
        {
            var l = new List<(string, float, float, string)>(); if (S?.vorders == null) return l;
            foreach (var o in S.vorders) foreach (var it in o.items) if (it.g == "" && it.src == ps.pid) l.Add((ItemName(it), Mathf.Min(it.got, it.n), it.n, o.who));
            return l;
        }
        /// <summary>Bestellte Ware fehlt noch im Produktlager – dann hat sie in der Produktion Vorrang.</summary>
        static bool OrderWants(PState ps, int m) => m >= 0 && OrderNeed(ps, m) > ps.sv.prod[m] + .5f;
        /// <summary>Zutat m wird für eine bestellte Folge-Ware gebraucht (z. B. Metall-Brick für bestellte Barren).</summary>
        static bool ChainWants(PState ps, int m) { foreach (var r in ps.line.schmelze) if (r.inp == m && OrderWants(ps, r.outp)) return true; return false; }
        static bool WantsOut(PState ps, int outp) => OrderWants(ps, outp) || ChainWants(ps, outp);
        /// <summary>Darf Planet q Waren für Bestellungen liefern? Nur freigeschaltete, nicht zerstörte Planeten.</summary>
        static bool OrderSrcOk(string pid) => P.ContainsKey(pid) && !Ruined(pid) && S.planets.Any(x => x.pid == pid);
        public static double GoodValue(string gid) { int i = Array.IndexOf(GameData.POrder, gid); return 150 * Math.Pow(2.4, Math.Max(0, i)); }
        public static string ItemName(OrderItem it) => it.g != "" ? Good(it.g).name : GameData.Mats[it.m].name;
        public static string ItemFrom(OrderItem it) => GameData.Planets.ContainsKey(it.src) ? GameData.Planets[it.src].name : "";

        static void NewVisitOrder()
        {
            var land = P.Values.Where(q => Gs(q) >= 2 && OrderSrcOk(q.pid)).ToList(); if (land.Count == 0) return;
            var r = Rng; var lp = land[r.Next(land.Count)]; var srcs = P.Values.Where(q => OrderSrcOk(q.pid) && Gs(q) >= 1).ToList(); /* 05.11.: nur Planeten, die du wirklich hast */
            var o = new VisitOrder { id = ++S.voNext, who = Visitors[r.Next(Visitors.Length)], pid = lp.pid, until = Now() + (long)(3600 * (1.5 + r.NextDouble() * 2.5)) };
            float hours = (o.until - Now()) / 3600f;
            int k = 1 + r.Next(3); double val = 0;
            for (int i = 0; i < k; i++)
            {
                var doneG = Goods.Where(g => OrderSrcOk(g.pid) && GoodRate(P[g.pid]) > 0 && !o.items.Any(x => x.g == g.id)).ToList();
                if (doneG.Count > 0 && r.NextDouble() < .2) { var g = doneG[r.Next(doneG.Count)]; float n = 10 + 5 * r.Next(5); o.items.Add(new OrderItem { src = g.pid, g = g.id, n = n }); val += n * GoodValue(g.id); continue; }
                var sp = r.NextDouble() < .6 || srcs.Count == 1 ? lp : srcs[r.Next(srcs.Count)];
                var mats = Producible(sp).Where(m => !o.items.Any(x => x.m == m && x.src == sp.pid)).ToList(); if (mats.Count == 0) continue;
                int mm = mats[r.Next(mats.Count)]; bool raw = GameData.Mats[mm].raw;
                float cap = raw ? RohCap(sp) : ProdCap(sp); float want = cap * (raw ? .04f + .06f * (float)r.NextDouble() : .08f + .12f * (float)r.NextDouble());
                if (raw) { float rh = RohRateH(sp, mm); want = Mathf.Min(want, rh > .5f ? rh * hours * .3f : 20 + sp.sv.roh[mm] * .5f); } /* 08.11.: nur so viel, wie wirklich hereinkommt */
                if (!raw) { float rh = SupplyH(sp, mm), real = ProdInH(sp, mm); want = Mathf.Min(want, rh > .5f ? rh * hours * .3f : 10 + sp.sv.prod[mm] * .5f); want = Mathf.Min(want, Mathf.Max(real * hours * .6f, Mathf.Min(rh * hours * .1f, 25)) + sp.sv.prod[mm] * .5f); } /* 10.11.: höchstens, was wirklich ankommt (gemessen) – mit Vorrang für Bestelltes */ /* 09.11.: Maschine und Nachschub zählen */ /* 05.11.: nur so viel, wie die Maschinen in knapp einem Drittel der Zeit schaffen */
                float nn = Mathf.Max(5, Mathf.Round(want / 5) * 5);
                o.items.Add(new OrderItem { src = sp.pid, m = mm, n = nn }); val += nn * MatValue(mm, sp);
            }
            if (o.items.Count == 0) return;
            o.reward = Math.Round(val * (2.2 + r.NextDouble())); o.sd = o.items.Count;
            VO.Add(o); OnOrderNew?.Invoke(o);
            Toast("Neue Bestellung: " + o.who + " auf " + GameData.Planets[o.pid].name + " möchte " + string.Join(", ", o.items.Select(x => Fmt(x.n) + " " + ItemName(x))) + " · Ziele → Besucher", "good");
        }
        // ---------- 08.11.: Rohstoff-Zufluss messen (wie viel kommt wirklich ins Lager) ----------
        static readonly Dictionary<PState, float[]> rohAcc = new Dictionary<PState, float[]>();
        static void RohIn(PState ps, int m, float x) { if (ps == null || m < 0 || m >= NM || x <= 0) return; if (!rohAcc.TryGetValue(ps, out var a)) rohAcc[ps] = a = new float[NM]; a[m] += x; }
        static readonly Dictionary<PState, float[]> prodAcc = new Dictionary<PState, float[]>();
        /// <summary>10.11.: Ware m kommt ins Produktlager (gemessen – für realistische Bestellmengen).</summary>
        static void ProdIn(PState ps, int m, float x) { if (ps == null || m < 0 || m >= NM || x <= 0) return; if (!prodAcc.TryGetValue(ps, out var a)) prodAcc[ps] = a = new float[NM]; a[m] += x; }
        static float[] Fit(float[] r) { if (r != null && r.Length == NM) return r; var n = new float[NM]; if (r != null) Array.Copy(r, n, Math.Min(NM, r.Length)); return n; }
        static void RohTrack(PState ps, float dt)
        {
            if (dt <= 0) return; var sv = ps.sv; sv.rohRate = Fit(sv.rohRate); sv.prodRate = Fit(sv.prodRate);
            rohAcc.TryGetValue(ps, out var a); prodAcc.TryGetValue(ps, out var b); float k = 1 - Mathf.Exp(-dt / 1800f);
            for (int m = 0; m < NM; m++) { float x = a != null ? a[m] / dt : 0; sv.rohRate[m] += (x - sv.rohRate[m]) * k; if (a != null) a[m] = 0; float y = b != null ? b[m] / dt : 0; sv.prodRate[m] += (y - sv.prodRate[m]) * k; if (b != null) b[m] = 0; }
        }
        /// <summary>Ware m kommt auf Planet sp pro Stunde ins Produktlager (gemessen).</summary>
        public static float ProdInH(PState sp, int m) => sp.sv.prodRate != null && m >= 0 && m < sp.sv.prodRate.Length ? sp.sv.prodRate[m] * 3600f : 0;
        /// <summary>Rohstoff m kommt auf Planet sp pro Stunde ins Lager (gemessen).</summary>
        public static float RohRateH(PState sp, int m) => sp.sv.rohRate != null && m >= 0 && m < sp.sv.rohRate.Length ? sp.sv.rohRate[m] * 3600f : 0;
        /// <summary>09.11.: Wie viel von m pro Stunde wirklich entstehen kann – Maschine UND Nachschub der Zutat (Rohstoff-Zufluss bzw. Vorprodukt).</summary>
        static float SupplyH(PState sp, int m, int depth = 0)
        {
            if (m < 0 || depth > 3) return 0; if (GameData.Mats[m].raw) return RohRateH(sp, m);
            float best = 0;
            foreach (var id in new[] { "presse", "schmelze", "raff" })
            {
                if (!sp.bld.Contains(id)) continue;
                foreach (var c in sp.line.Of(id))
                {
                    if (c.outp != m || c.t <= 0 || c.n <= 0) continue;
                    float mach = c.k * MachBatch(sp, id) * Uv("mspeed", sp) * sp.mm / c.t * 3600f, inH = SupplyH(sp, c.inp, depth + 1) * c.k / c.n;
                    best = Mathf.Max(best, Mathf.Min(mach, inH));
                }
            }
            return best;
        }
        /// <summary>Wie viel von Produkt m die Maschinen auf Planet sp höchstens pro Stunde schaffen (ohne Engpässe bei Rohstoffen).</summary>
        static float ProdRateH(PState sp, int m)
        {
            float best = 0;
            foreach (var id in new[] { "presse", "schmelze", "raff" })
            {
                if (!sp.bld.Contains(id)) continue;
                foreach (var c in sp.line.Of(id)) if (c.outp == m && PlanOn(sp, c.plan) && c.t > 0) best = Mathf.Max(best, c.k * MachBatch(sp, id) * Uv("mspeed", sp) * sp.mm / c.t * 3600f);
            }
            return best;
        }
        static readonly Dictionary<string, float> reserved = new Dictionary<string, float>();
        /// <summary>Höchste Bezahlung einer offenen Bestellung, die Ware m von Planet ps noch braucht (direkt oder als Zutat für bestellte Barren).</summary>
        static double OrderPay(PState ps, int m)
        {
            if (S == null || S.vorders == null || m < 0) return 0; double best = 0;
            foreach (var o in S.vorders) foreach (var it in o.items) if (it.g == "" && it.src == ps.pid && it.got < it.n - 1e-4f && (it.m == m || ps.line.schmelze.Any(r => r.inp == m && r.outp == it.m))) best = Math.Max(best, o.reward);
            return best;
        }
        static readonly List<int> ordIxBuf = new List<int>();
        static List<int> OrderIdx(PState ps, List<Recipe> rec)
        {
            ordIxBuf.Clear(); for (int i = 0; i < rec.Count; i++) ordIxBuf.Add(i);
            if (S?.vorders != null && S.vorders.Count > 0) ordIxBuf.Sort((a, b) => OrderPay(ps, rec[b].outp).CompareTo(OrderPay(ps, rec[a].outp)));
            return ordIxBuf;
        }
        public static float OrderProgress(VisitOrder o) { float n = 0, g = 0; foreach (var it in o.items) { n += it.n; g += Mathf.Min(it.n, it.got); } return n > 0 ? g / n : 0; }
        /// <summary>Je Schritt (live und offline): neue Besucher, Waren aus den Lagern zurücklegen, fertige Bestellungen bezahlen, abgelaufene verabschieden.</summary>
        public static void DevOrders(int n) { for (int i = 0; i < n && VO.Count < OrderMax(); i++) NewVisitOrder(); }
        public static string DevOrderExact() => S.vorders == null ? "keine" : string.Join(" | ", S.vorders.Select(o => o.who + " R" + Fmt(o.reward) + " " + string.Join(",", o.items.Select(it => (it.g != "" ? "G:" + it.g : it.src + ":" + ItemName(it)) + " " + it.got.ToString("0.0000") + "/" + it.n.ToString("0.0000") + (it.g == "" && P.ContainsKey(it.src) ? " lager " + P[it.src].sv.prod[it.m].ToString("0.0") + (GameData.Mats[it.m].raw ? " roh " + P[it.src].sv.roh[it.m].ToString("0.0") : "") : it.g != "" ? " vorrat " + GoodAmt(it.g).ToString("0.0") : "")))));
        public static string DevOrderInfo() => S.vorders == null ? "keine" : string.Join(" | ", S.vorders.Select(o => o.who + "@" + o.pid + " " + (OrderProgress(o) * 100).ToString("0") + "% " + string.Join(",", o.items.Select(it => ItemName(it) + " " + it.got.ToString("0") + "/" + it.n.ToString("0")))));
        static void OrdersStep(float dt)
        {
            if (!OrdersOpen) return;
            if (S.ordFix < 1 && S.vorders != null) /* 08.11.: alte, kaum machbare Bestellungen einmal verkleinern – Belohnung bleibt */
            {
                S.ordFix = 1; bool any = false;
                foreach (var o in S.vorders) foreach (var it in o.items) { if (it.g != "") continue; float left = it.n - it.got; if (left <= 0) continue; float nn = Mathf.Ceil(it.got + left * (GameData.Mats[it.m].raw ? .3f : .5f)); if (nn < it.n) { it.n = Mathf.Max(it.got, nn); any = true; } }
                if (any && !Offline) Toast("Die Besucher haben ihre Bestellungen verkleinert – zahlen aber dasselbe", "good");
            }
            S.voT -= dt; if (S.voT <= 0) { S.voT = UnityEngine.Random.Range(600f, 1500f); if (VO.Count < OrderMax()) NewVisitOrder(); }
            long now = Now();
            if (S.ordFix < 2 && S.vorders != null) /* 10.11.: bisher Umgebuchtes zurück ins Lager – Bestellungen reservieren jetzt nur noch */
            {
                S.ordFix = 2;
                foreach (var o in S.vorders) foreach (var it in o.items) { if (it.got <= 0) continue; if (it.g != "") AddGood(it.g, it.got); else if (P.ContainsKey(it.src)) P[it.src].sv.prod[it.m] += it.got; it.got = 0; }
            }
            /* Fabian 09.11./10.11.: Bestellungen mit der besten Bezahlung zuerst. Bestellte Waren bleiben im Lager liegen (der Frachter nimmt nur den Überschuss);
               eine schlechter bezahlte Bestellung bekommt nur, was über den Bedarf der besser bezahlten hinaus im Lager liegt. Liegt alles bereit, wird abgeholt. */
            VO.Sort((x, y) => y.reward.CompareTo(x.reward)); reserved.Clear();
            for (int i = 0; i < VO.Count; i++)
            {
                var o = VO[i];
                int bad = o.items.RemoveAll(it => !P.ContainsKey(it.g == "" ? it.src : Good(it.g).pid)); /* 05.11.: alte Bestellungen mit Waren von nicht freigeschalteten Planeten bereinigen */
                if (bad > 0 && o.items.Count == 0) { VO.RemoveAt(i); i--; continue; }
                bool all = true;
                foreach (var it in o.items)
                {
                    string rk = it.g != "" ? "g:" + it.g : it.src + ":" + it.m; reserved.TryGetValue(rk, out float resv);
                    float have = 0;
                    if (it.g != "") have = GoodAmt(it.g);
                    else if (P.ContainsKey(it.src) && !Ruined(it.src)) { var sv = P[it.src].sv; have = sv.prod[it.m] + (GameData.Mats[it.m].raw ? sv.roh[it.m] : 0); }
                    it.got = Mathf.Clamp(have - resv, 0, it.n); reserved[rk] = resv + it.n;
                    if (it.got < it.n - .01f) all = false;
                }
                if (now > o.until && o.ext == 0 && OrderProgress(o) >= .6f) { o.ext = 1; o.until = now + 2700; if (!Offline) Toast(o.who + " wartet noch 45 min – die Bestellung ist fast fertig", "good"); } /* 09.11.: knapp dran = Verlängerung */
                o.ready = all;
                /* Fabian 11.11.: Fertige Bestellungen wartet, bis du sie freigibst – spätestens 10 min vor Ablauf gehen sie von selbst raus */
                if (all && (o.go || now >= o.until - ORDER_AUTO))
                {
                    foreach (var it in o.items)
                    {
                        if (it.g != "") { TakeGood(it.g, Mathf.Min(it.n, GoodAmt(it.g))); continue; }
                        var sv = P[it.src].sv; float t = Mathf.Min(it.n, sv.prod[it.m]); sv.prod[it.m] -= t; float rest = it.n - t; if (rest > 0) sv.roh[it.m] = Mathf.Max(0, sv.roh[it.m] - rest);
                    }
                    VO.RemoveAt(i); i--; Earn(o.reward); S.stardust += o.sd; OnOrderDone?.Invoke(o); Toast(o.who + " ist zufrieden! Bestellung abgeholt · +" + Fmt(o.reward) + " Cr · +" + o.sd + " Sternenstaub", "good"); continue;
                }
                if (now > o.until) { VO.RemoveAt(i); i--; Toast(o.who + " ist abgereist – die Bestellung kam nicht rechtzeitig zusammen. Die zurückgelegte Ware bleibt im Lager.", "bad"); continue; }
            }
        }
        static double OrderRefund(VisitOrder o) { return 0; /* 10.11.: Ware lag nur reserviert im Lager – nichts zu erstatten */ double c = 0; foreach (var it in o.items) { if (it.got <= 0) continue; c += it.g != "" ? it.got * GoodValue(it.g) * .5 : it.got * MatValue(it.m, P.ContainsKey(it.src) ? P[it.src] : Cur); } if (c > 0) Earn(c); return c; }
        public const long ORDER_AUTO = 600;
        /// <summary>Fabian 11.11.: Fertige Bestellung freigeben – der Besucher holt sie beim nächsten Schritt ab.</summary>
        public static void ReleaseOrder(int id) { var o = VO.Find(x => x.id == id); if (o == null || !o.ready) return; o.go = true; }
        public static void CancelOrder(int id) { int i = VO.FindIndex(x => x.id == id); if (i < 0) return; var o = VO[i]; double c = OrderRefund(o); VO.RemoveAt(i); Toast("Bestellung abgesagt" + (c > 0 ? " · Zurückgelegtes verkauft: +" + Fmt(c) + " Cr" : ""), ""); }
        public static bool BuyOrderLv() { if (S.voLv >= ORDER_MAXLV) return Fail("Höchste Stufe"); double c = OrderLvCost(); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c; S.voLv++; Toast("Bestelllager ausgebaut · " + OrderMax() + " Bestellungen gleichzeitig", "good"); return true; }
        public static string OrderLvWhy() => S.voLv >= ORDER_MAXLV ? "Höchste Stufe" : S.credits < OrderLvCost() ? Lacks(OrderLvCost()) : "";
        public static string TimeLeft(long until) { long s = Math.Max(0, until - Now()); return s >= 3600 ? (s / 3600) + " h " + (s % 3600 / 60) + " min" : (s / 60) + " min"; }
    }
}
