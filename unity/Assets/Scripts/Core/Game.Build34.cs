// Build 34 (Fabian 02.11.): Abwehr gegen den Riesenmeteor (Schild + Abfangen), Lieferketten mit Spezialgütern, Terraforming.
// Alles läuft in derselben Simulation (StepPlanet → Build34Step), live und offline. Nur das Abfang-Minispiel ist live.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class GoodDef { public string id, name, pid; public Color col; }
    /// <summary>Laufendes Abfang-Minispiel (nur live, nur auf dem aktuellen Planeten).</summary>
    public class Intercept { public string pid; public int total, rockets, hits, through, done; public float diff; }

    public static partial class Game
    {
        // =====================================================================
        // Spezialgüter: jeder komplett saubere Planet stellt eins her
        // =====================================================================
        public static readonly List<GoodDef> Goods = new List<GoodDef>
        {
            G("moon", "Regolithziegel", "#c9c4bb"), G("mars", "Rostpulver", "#d0643a"), G("europa", "Eisblöcke", "#9fe3ff"),
            G("titan", "Methan", "#e8b25a"), G("io", "Schwefel", "#f3e04a"), G("venus", "Wolkensäure", "#e7d38f"),
            G("ganymed", "Magnetit", "#7d8796"), G("enceladus", "Geysirwasser", "#b9f2ff"), G("triton", "Stickstoffeis", "#f2c6d9"),
            G("pluto", "Herzeis", "#ffd9c4"), G("eris", "Tiefkühlkerne", "#d6e4ff"), G("sedna", "Tholin", "#b2452f"),
            G("proxima", "Sternenglas", "#ff8f6b"), G("trappist", "Meeresalgen", "#38c99a"), G("kepler", "Urwaldsamen", "#6fd04a"),
            G("cancri", "Diamanten", "#e9f7ff"), G("gliese", "Druckdampf", "#a9c8d8"), G("poltergeist", "Pulsarkristalle", "#c48bff"),
        };
        static GoodDef G(string pid, string name, string hex) => new GoodDef { id = pid, pid = pid, name = name, col = GameData.Hex(hex) };
        public static GoodDef GoodOf(string pid) => Goods.FirstOrDefault(g => g.pid == pid) ?? Goods[0];
        public static GoodDef Good(string id) => Goods.FirstOrDefault(g => g.id == id) ?? Goods[0];
        /// <summary>Jeder Planet braucht die Güter seiner zwei Nachbarn in der Reihe (Mond: Mars + Europa).</summary>
        public static string[] Needs(string pid)
        {
            var o = GameData.POrder; int i = Array.IndexOf(o, pid); if (i < 0 || o.Length < 3) return new[] { "moon", "mars" };
            if (i == 0) return new[] { o[1], o[2] }; if (i == 1) return new[] { o[0], o[2] }; return new[] { o[i - 1], o[i - 2] };
        }
        static List<FKV> GL => S.goods ?? (S.goods = new List<FKV>());
        public static float GoodAmt(string id) { foreach (var g in GL) if (g.k == id) return g.v; return 0; }
        public const float GOOD_CAP = 999;
        public static void AddGood(string id, float n) { foreach (var g in GL) if (g.k == id) { g.v = Mathf.Min(GOOD_CAP, g.v + n); return; } GL.Add(new FKV { k = id, v = Mathf.Min(GOOD_CAP, n) }); }
        static bool TakeGood(string id, float n) { foreach (var g in GL) if (g.k == id) { if (g.v + 1e-4f < n) return false; g.v = Mathf.Max(0, g.v - n); return true; } return n <= 0; }
        /// <summary>Herstellung je Stunde (nur komplett saubere Planeten; Terraforming Stufe 4 verdoppelt).</summary>
        public static float GoodRate(PState ps) => !IsDone(ps.pid) || Ruined(ps.pid) ? 0 : 40f * (ps.sv.terra >= 4 ? 2 : 1);
        /// <summary>Unterhalt je Stunde und je benötigtem Gut (Schild und Terraforming).</summary>
        public static float UpkeepH(PState ps) => ps.sv.terra * 2f + ps.sv.msLv * .5f;
        public static event Action<string, string, int> OnSupply; // Planet, Gut, Menge – Rakete landet (nur live, aktueller Planet)
        static void Delivered(PState ps, string gid, int n) { if (!Offline && ps.pid == S.planet && n > 0) OnSupply?.Invoke(ps.pid, gid, n); }
        /// <summary>Güter für einen Kauf auf diesem Planeten: true, wenn alles da ist (und dann abgebucht).</summary>
        static bool PayGoods(PState ps, (string g, int n)[] cost)
        {
            foreach (var c in cost) if (GoodAmt(c.g) + 1e-4f < c.n) return false;
            foreach (var c in cost) { TakeGood(c.g, c.n); Delivered(ps, c.g, c.n); }
            return true;
        }
        public static bool HasGoods((string g, int n)[] cost) => cost.All(c => GoodAmt(c.g) + 1e-4f >= c.n);
        public static string GoodsTxt((string g, int n)[] cost) => string.Join(" + ", cost.Select(c => c.n + " " + Good(c.g).name));
        static double PV(PState ps) => Math.Max(25000, ps.P.unlockCost);

        // ----- Top-Upgrades: die letzten zwei Stufen großer Upgrades brauchen Güter anderer Planeten -----
        public static (string g, int n)[] UGoods(string id, PState ps = null)
        {
            ps = ps ?? Cur; var u = GameData.Up[id]; if (!u.loc || u.max < 5) return new (string, int)[0];
            int l = Lvl(id, ps); if (l < u.max - 2 || l >= u.max) return new (string, int)[0];
            return new[] { (GoodOf(Needs(ps.pid)[0]).id, 10 * (l - u.max + 3)) };
        }

        // =====================================================================
        // Meteorschild und Abwehrraketen (je Planet)
        // =====================================================================
        public const int MS_MAX = 6, FLAK_MAX = 6; public const float MS_COOL = 1800;
        public static double MsCost(PState ps) => Math.Round(PV(ps) * .5 * Math.Pow(2, ps.sv.msLv));
        public static (string g, int n)[] MsGoods(PState ps) => new[] { (GoodOf(Needs(ps.pid)[0]).id, 10 * (ps.sv.msLv + 1)) };
        public static double FlakCost(PState ps) => Math.Round(PV(ps) * .4 * Math.Pow(2, ps.sv.flakLv));
        public static (string g, int n)[] FlakGoods(PState ps) => ps.sv.flakLv == 0 ? new (string, int)[0] : new[] { (GoodOf(Needs(ps.pid)[1]).id, 8 * ps.sv.flakLv) };
        public static int FlakRockets(PState ps) => ps.sv.flakLv <= 0 ? 0 : 2 + 2 * ps.sv.flakLv;
        public static string MsWhy(PState ps) => ps.sv.msLv >= MS_MAX ? "Höchste Stufe" : !HasGoods(MsGoods(ps)) ? "Es fehlen " + GoodsTxt(MsGoods(ps)) + " (von " + GameData.Planets[Needs(ps.pid)[0]].name + ")" : S.credits < MsCost(ps) ? Lacks(MsCost(ps)) : "";
        public static string FlakWhy(PState ps) => ps.sv.flakLv >= FLAK_MAX ? "Höchste Stufe" : !HasGoods(FlakGoods(ps)) ? "Es fehlen " + GoodsTxt(FlakGoods(ps)) + " (von " + GameData.Planets[Needs(ps.pid)[1]].name + ")" : S.credits < FlakCost(ps) ? Lacks(FlakCost(ps)) : "";
        public static bool BuyShield()
        {
            var ps = Cur; string w = MsWhy(ps); if (w != "") return Fail(w); S.credits -= MsCost(ps); PayGoods(ps, MsGoods(ps)); ps.sv.msLv++;
            Toast("Meteorschild Stufe " + ps.sv.msLv + " · " + Mathf.RoundToInt(ShieldMax(ps) * 100) + " % Abwehrchance", "good"); Save(); return true;
        }
        public static bool BuyFlak()
        {
            var ps = Cur; string w = FlakWhy(ps); if (w != "") return Fail(w); S.credits -= FlakCost(ps); PayGoods(ps, FlakGoods(ps)); ps.sv.flakLv++;
            Toast("Abwehrraketen Stufe " + ps.sv.flakLv + " · " + FlakRockets(ps) + " Raketen je Riesenmeteor", "good"); Save(); return true;
        }
        public static float ShieldMax(PState ps) => .85f * ps.sv.msLv / MS_MAX;
        /// <summary>Chance, dass der Schild den Riesenmeteor ablenkt. Nie 100 %: schwächer bei Sturm, wenig Energie, fehlendem Unterhalt; leere Batterien oder Nachladen = 0.</summary>
        public static float ShieldChance(PState ps)
        {
            var sv = ps.sv; if (sv.msLv <= 0 || sv.msCool > 0 || sv.energy < 2) return 0;
            float c = ShieldMax(ps); if (StormOn(ps) && !ps.shOn) c *= .8f; if (ps.ef < 1) c *= .7f + .3f * Mathf.Max(0, ps.ef); if (!sv.supOk) c *= .85f; /* Fabian 02.11.: Abzüge milder – vorher fiel der Schild nachts fast ganz aus */
            return Mathf.Clamp(c, 0, .85f);
        }
        public static string ShieldState(PState ps)
        {
            var sv = ps.sv; if (sv.msLv <= 0) return "Kein Schild";
            if (sv.msCool > 0) return "Lädt nach · noch " + Mathf.CeilToInt(sv.msCool / 60) + " min";
            if (sv.energy < 2) return "Batterien leer – Schild aus";
            string why = (StormOn(ps) && !ps.shOn ? " · Sturm schwächt" : "") + (ps.ef < 1 ? " · zu wenig Energie" : "") + (!sv.supOk ? " · Unterhalt fehlt" : "");
            return "Bereit · " + Mathf.RoundToInt(ShieldChance(ps) * 100) + " %" + why;
        }

        // =====================================================================
        // Riesenmeteor: Ausgang (Schild → Abfangen → Teilschaden oder Zerstörung)
        // =====================================================================
        public static Intercept Icp;
        public static event Action<string> OnDoomBlocked, OnIcpStart;
        public static event Action<string, int, int> OnIcpEnd; // Planet, durchgekommen, gesamt
        public static float DoomDiff(PState ps) => S.doomN * .6f + ps.sv.terra; /* Fabian 02.11.: langsamer schwerer */
        /// <summary>Wird beim Ablauf des Countdowns aufgerufen.</summary>
        static void DoomOutcome(string pid)
        {
            if (!P.ContainsKey(pid)) return; var ps = P[pid]; S.doomN++;
            float ch = ShieldChance(ps);
            if (ch > 0) { if (Rng.NextDouble() < ch) { ps.sv.msCool = MS_COOL; Toast("Der Meteorschild hat den Riesenmeteor abgelenkt! Er lädt jetzt 30 min nach.", "good"); OnDoomBlocked?.Invoke(pid); Save(); return; } Toast("Der Schild hält nicht (" + Mathf.RoundToInt(ch * 100) + " %) – der Meteor bricht durch!", "bad"); } /* Nachladen nur nach einem echten Treffer */
            if (pid == S.planet && !Offline && FlakRockets(ps) > 0)
            {
                float d = DoomDiff(ps); int n = Mathf.Clamp(4 + Mathf.FloorToInt(d / 3) + (d >= 2 ? Rng.Next(2) : 0), 4, 8);
                Icp = new Intercept { pid = pid, total = n, rockets = FlakRockets(ps), diff = d };
                Toast("Der Meteor zerbricht in " + n + " Brocken – tippe jeden an, wenn sein Ring GRÜN ist!", "bad"); OnIcpStart?.Invoke(pid); return;
            }
            if (pid == S.planet) Toast("Keine Abwehr – Tipp für nächstes Mal: Basis → Schutz (Meteorschild und Abwehrraketen)", "bad");
            OnDoomImpact?.Invoke(pid); // Explosion, danach Ruin()
        }
        /// <summary>Ein Tipp auf einen Brocken (verbraucht eine Rakete).</summary>
        public static bool IcpShot(bool hit)
        {
            var I = Icp; if (I == null || I.rockets <= 0) return false; I.rockets--;
            if (hit) { I.hits++; I.done++; S.doomHits++; S.stardust += 1; }
            return true;
        }
        public static void IcpThrough() { var I = Icp; if (I == null) return; I.through++; I.done++; }
        /// <summary>Alle Brocken sind entschieden. Mehr als die Hälfte durch = Planet zerstört, sonst Teilschaden.</summary>
        public static void IcpResolve()
        {
            var I = Icp; if (I == null) return; Icp = null; string pid = I.pid; if (!P.ContainsKey(pid)) return; var ps = P[pid];
            OnIcpEnd?.Invoke(pid, I.through, I.total);
            if (I.through * 3 > I.total * 2) { Toast(I.through + " von " + I.total + " Brocken eingeschlagen – das hält die Basis nicht aus! Tipp: mehr Raketen und einen stärkeren Schild (Basis → Schutz)", "bad"); OnDoomImpact?.Invoke(pid); return; } /* Fabian 02.11.: erst ab mehr als zwei Dritteln zerstört */
            if (I.through == 0) { Toast("Alle " + I.total + " Brocken abgefangen! +" + I.hits + " Sternenstaub", "good"); Save(); return; }
            DoomDamage(ps, I.through);
            Toast(I.hits + " abgefangen (+" + I.hits + " Sternenstaub), " + I.through + " eingeschlagen: Schmutz, Fahrzeugschäden, gestörte Anlagen", "bad"); Save();
        }
        /// <summary>Teilschaden je durchgekommenem Brocken: Schmutzfelder, kaputte Fahrzeuge, gestörte Bohrtürme, Anlagen 2 min auf halber Kraft je Brocken.</summary>
        public static void DoomDamage(PState ps, int through)
        {
            for (int i = 0; i < through * 2; i++) Meteor(ps);
            var vs = ps.sv.veh.Where(v => v.mode != "broken" && v.mode != "repair" && v.mode != "toShop").OrderBy(_ => Rng.Next()).Take(through * 2).ToList(); foreach (var v in vs) v.wear = 1f;
            foreach (var r in ps.sv.rigs.Where(r => r.faultK == "").OrderBy(_ => Rng.Next()).Take(through)) { r.faultK = "impact"; r.faultT = 180; }
            ps.sv.hitT = Mathf.Max(ps.sv.hitT, 120f * through); ps.sv.energy = 0;
            if (ps.sv.terra > 0) ps.sv.terraHp -= .2f * through;
        }

        // =====================================================================
        // Terraforming: 5 Stufen nach 100 % sauber, Bonus je Stufe, Verfall ohne Pflege
        // =====================================================================
        public const int TERRA_MAX = 5;
        public static readonly string[] TerraName = { "Kahl", "Atmosphäre", "Moos und Flechten", "Gras und Sträucher", "Wasser und junge Bäume", "Wald" };
        public static readonly string[] TerraBonus = { "", "+15 % Energie", "+10 % Raketenerlös", "+20 % Raketenerlös", "Spezialgut doppelt so schnell", "+35 % Raketenerlös und 1 Sternenstaub pro Stunde" };
        public static event Action<string, int, int> OnTerra; // Planet, alt, neu
        public static double TerraCost(PState ps) => Math.Round(PV(ps) * 3 * Math.Pow(2.5, ps.sv.terra));
        public static (string g, int n)[] TerraGoods(PState ps) { var nd = Needs(ps.pid); int k = ps.sv.terra + 1; return new[] { (GoodOf(nd[0]).id, 30 * k), (GoodOf(nd[1]).id, 20 * k) }; }
        public static string TerraWhy(PState ps)
        {
            if (!IsDone(ps.pid)) return "Erst muss der Planet 100 % sauber sein"; if (ps.sv.terra >= TERRA_MAX) return "Höchste Stufe";
            if (!HasGoods(TerraGoods(ps))) return "Es fehlen " + GoodsTxt(TerraGoods(ps)); if (S.credits < TerraCost(ps)) return Lacks(TerraCost(ps)); return "";
        }
        public static bool BuyTerra()
        {
            var ps = Cur; string w = TerraWhy(ps); if (w != "") return Fail(w); S.credits -= TerraCost(ps); PayGoods(ps, TerraGoods(ps));
            int o = ps.sv.terra; ps.sv.terra++; ps.sv.terraHp = 1; OnTerra?.Invoke(ps.pid, o, ps.sv.terra);
            Toast("Terraforming Stufe " + ps.sv.terra + ": " + TerraName[ps.sv.terra] + " · " + TerraBonus[ps.sv.terra], "good"); Save(); return true;
        }
        public static float TerraSellF(PState ps) => 1 + new[] { 0, 0, .1f, .2f, .2f, .35f }[Mathf.Clamp(ps.sv.terra, 0, 5)];
        public static float TerraEnergyF(PState ps) => ps.sv.terra >= 1 ? 1.15f : 1f;
        /// <summary>Warum der Zustand sinkt (leer = alles gut).</summary>
        public static string TerraTrouble(PState ps)
        {
            var l = new List<string>(); if (!ps.sv.supOk) l.Add("Lieferungen fehlen"); if (StormOn(ps) && !ps.shOn) l.Add("Sturm"); if (Dirty(ps)) l.Add("Schmutz auf dem Planeten"); return string.Join(", ", l);
        }
        static readonly Dictionary<PState, int> nonBase = new Dictionary<PState, int>();
        static bool Dirty(PState ps) { if (!nonBase.TryGetValue(ps, out int nb)) { nb = ps.pd.cells.Count(c => !c.IsBase); nonBase[ps] = nb; } return ps.nClean < nb * .97f; }

        /// <summary>Testmodus (Fabian 02.11.): alle Planeten freischalten, komplett sauber, alles gebaut und gekauft, Bohrtürme, Abwehr, Terraforming 5, Güter voll.</summary>
        public static void DevAll()
        {
            if (!TestMode) return; string home = S.planet; bool off = Offline; Offline = true; // keine Meldungsflut
            try
            {
                S.credits += 1e18; S.ruin = ""; S.ruinUntil = 0; S.doomT = -1; S.doomPid = "";
                foreach (var pid in GameData.POrder) if (!P.ContainsKey(pid)) { var sv = NewPlanet(pid); S.planets.Add(sv); P[pid] = Attach(sv); }
                foreach (var pid in GameData.POrder)
                {
                    S.planet = pid; var ps = Cur; DevMaxOut(); { int guard = 0, lim = ps.pd.N * 2 + 10; while (ps.front.Count > 0 && guard++ < lim) { int id = ps.front.First(); ps.paid[id] = true; FinishCell(ps, id); } } /* auch große Planeten ganz (DevCleanTo hört bei 5000 auf) */
                    for (int k = 0; k < 40 && ps.sv.rigs.Count < RigMax(ps); k++) if (!BuildRig(-1)) break;
                    ps.sv.msLv = MS_MAX; ps.sv.flakLv = FLAK_MAX; ps.sv.msCool = 0; ps.sv.terra = TERRA_MAX; ps.sv.terraHp = 1; ps.sv.supOk = true; ps.sv.energy = EnergyCap(ps);
                    if (!S.done.Contains(pid)) S.done.Add(pid); ps.baseVersion++;
                }
                foreach (var g in Goods) AddGood(g.id, GOOD_CAP);
            }
            finally { Offline = off; S.planet = home; }
            Save(); OnPlanetChanged?.Invoke(); Toast("Testmodus: alle " + GameData.POrder.Length + " Planeten freigeschaltet, sauber und voll ausgebaut", "good");
        }
        /// <summary>Nur Editor-Tests.</summary>
        public static void DevTerra(int n, float hp = 1) { var ps = Cur; int o = ps.sv.terra; ps.sv.terra = Mathf.Clamp(n, 0, TERRA_MAX); ps.sv.terraHp = hp; if (o != ps.sv.terra) OnTerra?.Invoke(ps.pid, o, ps.sv.terra); }
        /// <summary>Je Planet und Schritt (live und offline): Güter herstellen, Unterhalt liefern, Schild laden, Terraforming pflegen.</summary>
        static void Build34Step(PState ps, float dt)
        {
            var sv = ps.sv; if (sv.msCool > 0) sv.msCool -= dt; if (sv.hitT > 0) sv.hitT -= dt;
            if (Ruined(ps.pid)) return;
            float gr = GoodRate(ps); if (gr > 0) { sv.gAcc += dt * gr / 3600f; if (sv.gAcc >= 1) { int n = (int)sv.gAcc; sv.gAcc -= n; AddGood(GoodOf(ps.pid).id, n); } }
            float up = UpkeepH(ps);
            if (up <= 0) { sv.supOk = true; sv.upAcc = 0; }
            else
            {
                sv.upAcc += dt * up / 3600f;
                if (sv.upAcc >= 1)
                {
                    int n = (int)sv.upAcc; sv.upAcc -= n; bool ok = true;
                    foreach (var q in Needs(ps.pid)) { var g = GoodOf(q).id; if (GoodAmt(g) >= n) { TakeGood(g, n); Delivered(ps, g, n); } else ok = false; }
                    if (ok != sv.supOk && ps.pid == S.planet) Toast(ok ? "Lieferungen kommen wieder an" : "Lieferung fehlt: " + string.Join(" / ", Needs(ps.pid).Select(x => GoodOf(x).name)) + " – Schild und Terraforming leiden", ok ? "good" : "bad");
                    sv.supOk = ok;
                }
            }
            if (sv.terra > 0)
            {
                float loss = 0; if (!sv.supOk) loss += 1 / 10800f; if (StormOn(ps) && !ps.shOn) loss += 1 / 3600f; if (Dirty(ps)) loss += 1 / 7200f;
                sv.terraHp = loss > 0 ? sv.terraHp - loss * dt : Mathf.Min(1, sv.terraHp + dt / 1800f);
                if (sv.terraHp <= 0)
                {
                    int o = sv.terra; sv.terra--; sv.terraHp = sv.terra > 0 ? .5f : 1; OnTerra?.Invoke(ps.pid, o, sv.terra);
                    Toast("Terraforming auf " + ps.P.name + " fällt zurück auf " + TerraName[sv.terra] + (o == 5 ? " – die Bäume sterben" : ""), "bad");
                }
                if (sv.terra >= 5) S.stardust += dt / 3600.0;
            }
        }
    }
}
