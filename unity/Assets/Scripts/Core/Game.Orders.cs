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
    [Serializable] public class VisitOrder { public int id; public string who = "", pid = ""; public List<OrderItem> items = new List<OrderItem>(); public long until; public double reward; public int sd; }

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
            foreach (var t in ps.P.types) { if (t.Value <= 0) continue; int m = GameData.Dirt[t.Key].mat; if (m >= 0 && !l.Contains(m) && (RohRateH(ps, m) > .5f || ps.sv.roh[m] > 5 || !IsDone(ps.pid))) l.Add(m); } /* 08.11.: auf sauberen Planeten nur, was wirklich noch hereinkommt */
            /* nur Rezepte, die wirklich laufen können: Plan freigeschaltet und Zutat vorhanden (2 Runden für Ketten) */
            void Add(string mach, List<Recipe> rs) { if (!ps.bld.Contains(mach) || (mach == "raff" && !PlanOn(ps, "refine"))) return; foreach (var r in rs) if (PlanOn(ps, r.plan) && l.Contains(r.inp) && !l.Contains(r.outp)) l.Add(r.outp); }
            for (int round = 0; round < 2; round++) { Add("presse", ps.line.presse); Add("schmelze", ps.line.schmelze); Add("raff", ps.line.raff); }
            return l;
        }
        /// <summary>Fabian 05.11.: Wie viel von Ware m die Besucher bei Planet ps noch brauchen (nur Waren dieses Planeten).</summary>
        public static float OrderNeed(PState ps, int m)
        {
            if (S == null || S.vorders == null || m < 0) return 0; float n = 0;
            foreach (var o in S.vorders) foreach (var it in o.items) if (it.g == "" && it.m == m && it.src == ps.pid) n += Mathf.Max(0, it.n - it.got);
            return n;
        }
        /// <summary>Bestellte Ware fehlt noch im Produktlager – dann hat sie in der Produktion Vorrang.</summary>
        static bool OrderWants(PState ps, int m) => m >= 0 && OrderNeed(ps, m) > ps.sv.prod[m] + .5f;
        /// <summary>Zutat m wird für eine bestellte Folge-Ware gebraucht (z. B. Metall-Brick für bestellte Barren).</summary>
        static bool ChainWants(PState ps, int m) { foreach (var r in ps.line.schmelze) if (r.inp == m && PlanOn(ps, r.plan) && OrderWants(ps, r.outp)) return true; return false; }
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
                if (!raw) { float rh = ProdRateH(sp, mm); if (rh > 0) want = Mathf.Min(want, rh * hours * .3f); } /* 05.11.: nur so viel, wie die Maschinen in knapp einem Drittel der Zeit schaffen */
                float nn = Mathf.Max(5, Mathf.Round(want / 5) * 5);
                o.items.Add(new OrderItem { src = sp.pid, m = mm, n = nn }); val += nn * MatValue(mm, sp);
            }
            if (o.items.Count == 0) return;
            o.reward = Math.Round(val * (2.2 + r.NextDouble())); o.sd = o.items.Count;
            VO.Add(o); OnOrderNew?.Invoke(o);
            Toast("Neue Bestellung: " + o.who + " auf " + GameData.Planets[o.pid].name + " möchte " + string.Join(", ", o.items.Select(x => Fmt(x.n) + " " + ItemName(x))) + " · Ziele → Bestellungen", "good");
        }
        // ---------- 08.11.: Rohstoff-Zufluss messen (wie viel kommt wirklich ins Lager) ----------
        static readonly Dictionary<PState, float[]> rohAcc = new Dictionary<PState, float[]>();
        static void RohIn(PState ps, int m, float x) { if (ps == null || m < 0 || m >= NM || x <= 0) return; if (!rohAcc.TryGetValue(ps, out var a)) rohAcc[ps] = a = new float[NM]; a[m] += x; }
        static void RohTrack(PState ps, float dt)
        {
            if (dt <= 0) return; var sv = ps.sv; if (sv.rohRate == null || sv.rohRate.Length != NM) { var r = new float[NM]; if (sv.rohRate != null) Array.Copy(sv.rohRate, r, Math.Min(NM, sv.rohRate.Length)); sv.rohRate = r; }
            rohAcc.TryGetValue(ps, out var a); float k = 1 - Mathf.Exp(-dt / 1800f);
            for (int m = 0; m < NM; m++) { float x = a != null ? a[m] / dt : 0; sv.rohRate[m] += (x - sv.rohRate[m]) * k; if (a != null) a[m] = 0; }
        }
        /// <summary>Rohstoff m kommt auf Planet sp pro Stunde ins Lager (gemessen).</summary>
        public static float RohRateH(PState sp, int m) => sp.sv.rohRate != null && m >= 0 && m < sp.sv.rohRate.Length ? sp.sv.rohRate[m] * 3600f : 0;
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
        public static float OrderProgress(VisitOrder o) { float n = 0, g = 0; foreach (var it in o.items) { n += it.n; g += Mathf.Min(it.n, it.got); } return n > 0 ? g / n : 0; }
        /// <summary>Je Schritt (live und offline): neue Besucher, Waren aus den Lagern zurücklegen, fertige Bestellungen bezahlen, abgelaufene verabschieden.</summary>
        public static void DevOrders(int n) { for (int i = 0; i < n && VO.Count < OrderMax(); i++) NewVisitOrder(); }
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
            for (int i = VO.Count - 1; i >= 0; i--)
            {
                var o = VO[i];
                int bad = o.items.RemoveAll(it => !P.ContainsKey(it.g == "" ? it.src : Good(it.g).pid)); /* 05.11.: alte Bestellungen mit Waren von nicht freigeschalteten Planeten bereinigen */
                if (bad > 0 && o.items.Count == 0) { VO.RemoveAt(i); continue; }
                if (now > o.until) { double back = OrderRefund(o); VO.RemoveAt(i); Toast(o.who + " ist abgereist – die Bestellung kam nicht rechtzeitig zusammen" + (back > 0 ? ". Zurückgelegtes verkauft: +" + Fmt(back) + " Cr" : ""), "bad"); continue; }
                bool all = true;
                foreach (var it in o.items)
                {
                    if (it.got >= it.n - 1e-4f) continue; float rate = Mathf.Max(1f, it.n / 300f) * dt, need = it.n - it.got;
                    if (it.g != "") { float t = Mathf.Min(need, Mathf.Min(GoodAmt(it.g), rate)); if (t > 0 && TakeGood(it.g, t)) it.got += t; }
                    else if (P.ContainsKey(it.src) && !Ruined(it.src))
                    {
                        var sv = P[it.src].sv; bool raw = GameData.Mats[it.m].raw;
                        float a = sv.prod[it.m], t = Mathf.Min(need, Mathf.Min(a, rate)); if (t > 0) { sv.prod[it.m] -= t; it.got += t; } /* zuerst aus dem Produktlager (dort liegen auch Verkaufs-Rohstoffe) */
                        need = it.n - it.got; if (raw && need > 0) { float b = Mathf.Min(need, Mathf.Min(sv.roh[it.m], rate)); if (b > 0) { sv.roh[it.m] -= b; it.got += b; } }
                    }
                    if (it.got < it.n - 1e-4f) all = false;
                }
                if (all) { VO.RemoveAt(i); Earn(o.reward); S.stardust += o.sd; OnOrderDone?.Invoke(o); Toast(o.who + " ist zufrieden! Bestellung abgeholt · +" + Fmt(o.reward) + " Cr · +" + o.sd + " Sternenstaub", "good"); }
            }
        }
        static double OrderRefund(VisitOrder o) { double c = 0; foreach (var it in o.items) { if (it.got <= 0) continue; c += it.g != "" ? it.got * GoodValue(it.g) * .5 : it.got * MatValue(it.m, P.ContainsKey(it.src) ? P[it.src] : Cur); } if (c > 0) Earn(c); return c; }
        public static void CancelOrder(int id) { int i = VO.FindIndex(x => x.id == id); if (i < 0) return; var o = VO[i]; double c = OrderRefund(o); VO.RemoveAt(i); Toast("Bestellung abgesagt" + (c > 0 ? " · Zurückgelegtes verkauft: +" + Fmt(c) + " Cr" : ""), ""); }
        public static bool BuyOrderLv() { if (S.voLv >= ORDER_MAXLV) return Fail("Höchste Stufe"); double c = OrderLvCost(); if (S.credits < c) return Fail(Lacks(c)); S.credits -= c; S.voLv++; Toast("Bestelllager ausgebaut · " + OrderMax() + " Bestellungen gleichzeitig", "good"); return true; }
        public static string OrderLvWhy() => S.voLv >= ORDER_MAXLV ? "Höchste Stufe" : S.credits < OrderLvCost() ? Lacks(OrderLvCost()) : "";
        public static string TimeLeft(long until) { long s = Math.Max(0, until - Now()); return s >= 3600 ? (s / 3600) + " h " + (s % 3600 / 60) + " min" : (s / 60) + " min"; }
    }
}
