// Neue Spielinhalte (docs/IDEEN_SPEC.md): Kombo beim Wischen, Schatzfunde + Album, Ereignisse, Tagesbonus,
// Fahrzeug-Erfahrung und Spezialisierung. Zahlen in FeatureData.cs. Ereignisse laufen nur live auf dem aktuellen Planeten.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public static event Action<string, string, int> OnFind;   // Name, Beschreibung, Album-Fortschritt
        public static int ComboN; static float comboLast = -99;
        static readonly System.Random fr = new System.Random();

        // ---------- Parzelle fertig: Kombo, Funde, Wrack ----------
        static void OnCellDone(PState ps, int id, double bonus)
        {
            bool live = !Offline && ps.pid == S.planet;
            // Kombo: nur eigene Handreinigung
            if (live && strokeCells.Contains(id))
            {
                float now = Time.realtimeSinceStartup; ComboN = now - comboLast <= FeatureData.COMBO_WINDOW ? Mathf.Min(FeatureData.COMBO_MAX, ComboN + 1) : 1; comboLast = now;
                if (ComboN > 1) { double extra = bonus * FeatureData.COMBO_STEP * (ComboN - 1); Earn(extra); FloatTxt(ps.pd.cells[id].c, "Kombo ×" + ComboN + " · +" + Fmt(extra), "good"); }
            }
            // Wrack-Ereignis: rechtzeitig sauber = Bonus
            var ev = ps.sv.ev; if (false && ev != null && ev.key == "wreck" && ev.cell == id && ev.t > 0) { double w = FeatureData.WRECK_BONUS * ps.P.diff + bonus * (FeatureData.WRECK_AMT - 1); Earn(w); if (live) Toast("Wrack geborgen · +" + Fmt(w) + " Cr", "good"); EvEnd(ps, null); }
            // Schatzfund
            if (!FeatureData.Finds.TryGetValue(ps.pid, out var album)) return;
            float ch = (GameData.Kinds[ps.pd.cells[id].kind].key == "anomaly" ? FeatureData.FIND_CHANCE_ANOMALY : FeatureData.FIND_CHANCE) * (Sd("find") ? 1.1f : 1);
            if (fr.NextDouble() >= ch) return;
            int i = fr.Next(album.Length); string key = ps.pid + ":" + i;
            if (S.finds.Contains(key)) { double c = FeatureData.FIND_DUP_CR * ps.P.diff; Earn(c); if (live) FloatTxt(ps.pd.cells[id].c, "Doppelter Fund · +" + Fmt(c), "good"); return; }
            S.finds.Add(key); int have = FindsOn(ps.pid);
            if (live) { OnFind?.Invoke(album[i].name, album[i].desc, have); Toast("Fund: " + album[i].name + " – " + album[i].desc + " (" + have + "/" + album.Length + ")", "good"); if (have == album.Length) Toast("Album " + ps.P.name + " komplett: dauerhaft +5 % Erlös auf diesem Planeten", "good"); }
        }
        public static int FindsOn(string pid) => S == null ? 0 : S.finds.Count(f => f.StartsWith(pid + ":"));
        public static bool HasFind(string pid, int i) => S != null && S.finds.Contains(pid + ":" + i);
        static double AlbumBonus(string pid) => FeatureData.Finds.TryGetValue(pid, out var a) && FindsOn(pid) >= a.Length ? FeatureData.ALBUM_BONUS : 0;
        public static float ComboLeft() => ComboN > 1 ? Mathf.Max(0, FeatureData.COMBO_WINDOW - (Time.realtimeSinceStartup - comboLast)) : 0;

        // ---------- Ereignisse ----------
        public static bool EvOn(PState ps, string key) => ps.sv.ev != null && ps.sv.ev.key == key && ps.sv.ev.t > 0;
        public static EventDef EvDef(string key) => FeatureData.Events.FirstOrDefault(e => e.key == key);
        static void EvEnd(PState ps, string msg) { if (ps.sv.ev == null) return; ps.sv.ev.key = ""; ps.sv.ev.t = 0; ps.sv.ev.cell = -1; if (msg != null && !Offline && ps.pid == S.planet) Toast(msg, "good"); }
        static void EventStep(PState ps, float dt)
        {
            var ev = ps.sv.ev ?? (ps.sv.ev = new EventSave());
            if (ev.key != "")
            {
                ev.t -= dt;
                if (ev.t <= 0) { string n = EvDef(ev.key)?.name ?? "Ereignis"; if ((ev.key == "wreck" || ev.key == "impact") && ps.sv.team.mode != "back") Toast(ev.key == "wreck" ? "Das Wrack ist weggesunken – der Suchtrupp kam zu spät" : "Die Kometentrümmer sind verglüht"); EvEnd(ps, ev.key == "wreck" ? null : n + " ist vorbei"); }
                return;
            }
            if (S.evNext < 0) S.evNext = Mathf.Lerp(FeatureData.EV_MIN, FeatureData.EV_MAX, (float)fr.NextDouble());
            if (ps.nClean < 8) return; // erst wenn der Planet etwas Fläche hat
            S.evNext -= dt * (Sd("event") ? 1.2f : 1); if (S.evNext > 0) return;
            S.evNext = Mathf.Lerp(FeatureData.EV_MIN, FeatureData.EV_MAX, (float)fr.NextDouble());
            StartEvent(ps, null);
        }
        /// <summary>Ereignis starten (key = null: zufällig nach Gewicht). Liefert false, wenn es hier nicht passt.</summary>
        public static bool StartEvent(PState ps, string key)
        {
            var list = FeatureData.Events.Where(e => (e.key != "vein" || ps.sv.rigs.Count > 0) && (e.key != "freighter" || ps.bld.Contains("sell"))).ToList(); if (list.Count == 0) return false;
            EventDef E = key != null ? list.FirstOrDefault(e => e.key == key) : null;
            if (E == null) { float sw = list.Sum(e => e.w), r = (float)fr.NextDouble() * sw; foreach (var e in list) { r -= e.w; if (r <= 0) { E = e; break; } } E ??= list[0]; }
            var ev = ps.sv.ev ?? (ps.sv.ev = new EventSave()); ev.key = E.key; ev.t = E.dur; ev.cell = -1; var pd = ps.pd;
            if (E.key == "wreck")
            {
                var cand = new List<int>(); for (int i = 0; i < pd.N; i++) { var c = pd.cells[i]; if (ps.clean[i] && !c.IsBase && !c.Wet && c.ring >= 3 && !ps.sv.rigs.Any(x => x.cell == i) && !ps.sv.outposts.Any(o => o.cell == i) && !ps.sv.crystals.Any(x => x.cell == i)) cand.Add(i); }
                if (cand.Count == 0) { ev.key = ""; return false; }
                int id = cand[fr.Next(cand.Count)]; ev.cell = id;
                ps.clean[id] = false; ps.nClean = Mathf.Max(0, ps.nClean - 1); ps.paid[id] = false; ps.act.Remove(id); ps.dirtyCells.Add(id);
                if (IsFront(ps, id)) ps.front.Add(id); foreach (int b in pd.cells[id].nb) { ps.dirtyCells.Add(b); if (ps.front.Contains(b) && !IsFront(ps, b)) ps.front.Remove(b); }
                OnBoom?.Invoke(pd.cells[id].c);
            }
            else if (E.key == "impact")
            {
                var cand = new List<int>(); for (int i = 0; i < pd.N; i++) { var c = pd.cells[i]; if (ps.clean[i] && !c.IsBase && !c.Wet && c.ring >= 3 && c.ring <= 9 && !ps.sv.rigs.Any(x => x.cell == i) && !ps.sv.outposts.Any(o => o.cell == i)) cand.Add(i); }
                if (cand.Count == 0) { ev.key = ""; return false; }
                ev.cell = cand[fr.Next(cand.Count)]; OnBoom?.Invoke(pd.cells[ev.cell].c);
            }
            else if (E.key == "comet")
            {
                int n = FeatureData.COMET_MIN + fr.Next(FeatureData.COMET_MAX - FeatureData.COMET_MIN + 1);
                for (int k = 0, tr = 0; k < n && tr < 400; tr++) { int id = fr.Next(pd.N); var cl = pd.cells[id]; if (cl.IsBase || cl.ring < 2 || cl.Wet || !ps.clean[id] || ps.sv.crystals.Any(c => c.cell == id)) continue; float a = (float)fr.NextDouble() * 6.283f, rr = (float)fr.NextDouble() * cl.H * .5f; ps.sv.crystals.Add(new CrystalSave { id = ++ps.sv.crN, cell = id, t = fr.Next(CrystalTypes.Length), x = Mathf.Cos(a) * rr, y = Mathf.Sin(a) * rr, g = 1 }); k++; }
            }
            if (!Offline && ps.pid == S.planet) Toast(E.name + "! " + E.hint, "good"); if (E.key == "wreck" || E.key == "impact") SendTeam(ps);
            return true;
        }
        public static Vector3? EventPlace(PState ps) { var ev = ps.sv.ev; if (ev == null || ev.key == "" || ev.cell < 0) return null; return ps.pd.cells[ev.cell].c; }

        // ---------- Tagesbonus ----------
        static int Today() { var d = DateTime.Now; return d.Year * 10000 + d.Month * 100 + d.Day; }
        static int DayIndex(int yyyymmdd) { try { return (int)(new DateTime(yyyymmdd / 10000, yyyymmdd / 100 % 100, yyyymmdd % 100) - new DateTime(2020, 1, 1)).TotalDays; } catch { return -9999; } }
        /// <summary>Beim Start aufrufen. Liefert den Text der Belohnung oder null (heute schon abgeholt).</summary>
        public static string DailyCheck()
        {
            if (S == null || Offline) return null; var d = S.daily ?? (S.daily = new DailySave()); int t = Today(); if (d.lastDay == t) return null;
            int gap = d.lastDay == 0 ? 99 : DayIndex(t) - DayIndex(d.lastDay);
            d.streak = gap >= 1 && gap <= 1 + FeatureData.DAILY_GRACE_DAYS ? d.streak % FeatureData.DailyMult.Length + 1 : 1; d.lastDay = t;
            var ps = Cur; double cr = Math.Max(FeatureData.DAILY_MIN, 50 * ps.P.diff * ps.sv.stage) * FeatureData.DailyMult[d.streak - 1]; Earn(cr);
            string txt = "Tagesbonus Tag " + d.streak + "/7 · +" + Fmt(cr) + " Cr";
            if (d.streak == FeatureData.DailyMult.Length)
            {
                int n = 0; for (int tr = 0; tr < 300 && n < FeatureData.DAILY_CRYSTALS_DAY7; tr++) { int id = fr.Next(ps.pd.N); var cl = ps.pd.cells[id]; if (cl.IsBase || cl.Wet || !ps.clean[id] || ps.sv.crystals.Any(c => c.cell == id)) continue; ps.sv.crystals.Add(new CrystalSave { id = ++ps.sv.crN, cell = id, t = fr.Next(CrystalTypes.Length), g = 1 }); n++; }
                if (n > 0) txt += " und " + n + " reife Leuchtkristalle";
            }
            return txt;
        }

        // ---------- Fahrzeug-Erfahrung ----------
        public static int VehLvl(VehSave v) { int l = 0; foreach (float x in FeatureData.VehXp) if (v.xp >= x) l++; return l; }
        static void VehXp(VehSave v, float m2) { int l0 = VehLvl(v); v.xp += m2; int l1 = VehLvl(v); if (l1 > l0 && !Offline) Toast(VehName(v) + " erreicht Stufe " + l1 + (l1 == 2 ? " – wähle eine Spezialisierung im Fahrzeugfenster" : ""), "good"); }
        public static string VehName(VehSave v) => !string.IsNullOrEmpty(v.vname) ? v.vname : GameData.Veh[v.type].name + " " + v.id;
        /// <summary>Bonus aus Stufe und Spezialisierung: "speed", "power", "tank", "wear", "energy".</summary>
        public static float VehBonus(VehSave v, string what)
        {
            float lv = 1 + FeatureData.VEH_LVL_BONUS * VehLvl(v);
            switch (what)
            {
                case "speed": return lv * (v.spec == 1 ? 1.15f : 1);
                case "power": return lv;
                case "tank": return v.spec == 1 ? .95f : 1;
                case "wear": return v.spec == 2 ? .6f : 1;
                case "energy": return v.spec == 3 ? .7f : 1;
            }
            return 1;
        }
        public static void VehSetSpec(VehSave v, int spec) { if (VehLvl(v) < 2) { Toast("Spezialisierung ab Stufe 2"); return; } v.spec = Mathf.Clamp(spec, 0, FeatureData.VehSpec.Length - 1); }

        // ---------- Meteoritenschauer (wie im Browser-Prototyp) ----------
        // Ein Schauer bedeckt eine saubere Parzelle und 1–3 Nachbarn (auf fertigen Planeten 3–6) fast ganz mit Staub.
        // Die Parzellen bleiben „sauber“ (Fortschritt sinkt nicht), haben aber wieder Schmutz zum Abbauen – Arbeit für die Flotte.
        public static float MeteorEvery(PState ps) => IsDone(ps.pid) ? Mathf.Max(25, 520 / Mathf.Sqrt(Mathf.Max(1, ps.nClean))) : Mathf.Max(45, 1000 / Mathf.Sqrt(Mathf.Max(1, ps.nClean)));
        static void MeteorStep(PState ps, float dt)
        {
            if (ps.nClean < 3) return; ps.sv.met += dt; float iv = MeteorEvery(ps); int guard = 0;
            while (ps.sv.met >= iv && guard++ < 50) { ps.sv.met -= iv; Meteor(ps); }
        }
        static int MeteorType(PState ps)
        {
            if (fr.NextDouble() < .45) return ps.P.start; float tot = 0; foreach (var t in ps.P.types) if (t.Key != ps.P.start) tot += t.Value;
            float r = (float)fr.NextDouble() * tot; foreach (var t in ps.P.types) { if (t.Key == ps.P.start) continue; r -= t.Value; if (r <= 0) return t.Key; } return ps.P.start;
        }
        public static void Meteor(PState ps)
        {
            var pd = ps.pd; bool Ok(int i) => ps.clean[i] && !pd.cells[i].IsBase && !pd.cells[i].Wet && !(ps.act.TryGetValue(i, out var a) && a.sum > pd.cells[i].nIn * .5f);
            var c = new List<int>(); for (int i = 0; i < pd.N; i++) if (Ok(i)) c.Add(i); if (c.Count == 0) return;
            int id = c[fr.Next(c.Count)]; bool done = IsDone(ps.pid); int want = done ? 4 + fr.Next(4) : 2 + fr.Next(3); var patch = new List<int> { id };
            for (int g = 0; g < want * 3 && patch.Count < want; g++) { int from = patch[fr.Next(patch.Count)]; var nb = pd.cells[from].nb.Where(j => Ok(j) && !patch.Contains(j)).ToList(); if (nb.Count > 0) patch.Add(nb[fr.Next(nb.Count)]); }
            int dt0 = MeteorType(ps); var D = GameData.Dirt[dt0]; int G = GameData.GRID;
            foreach (int cid in patch)
            {
                var cl = pd.cells[cid];
                if (!ps.act.TryGetValue(cid, out var a)) { a = new Act { cl = cl, grid = new float[G * G], sum = 0, amt = D.amt * GameData.Kinds[cl.kind].amt, D = D, K = GameData.Kinds[cl.kind] }; ps.act[cid] = a; }
                int n = 11 + fr.Next(5);
                for (int k = 0; k < n; k++)
                {
                    int gi = 0; for (int t = 0; t < 30; t++) { gi = fr.Next(G * G); if (cl.inside[gi]) break; }
                    float x = -cl.H + ((gi % G) + .5f) * cl.cs, y = -cl.H + ((gi / G) + .5f) * cl.cs, r = 3.4f + (float)fr.NextDouble() * 1.8f, s = .9f + (float)fr.NextDouble() * .08f;
                    DirtyAt(a, x, y, r, s);
                }
                ps.dirtyCells.Add(cid);
            }
            if (!Offline && ps.pid == S.planet) { foreach (int cid in patch) OnBoom?.Invoke(pd.cells[cid].c); Toast((patch.Count >= 4 ? "Großer " : "") + "Meteoritenschauer: " + patch.Count + " Parzellen – die Fahrzeuge räumen auf", "bad"); }
        }
        static void DirtyAt(Act a, float x, float y, float r, float s)
        {
            var cl = a.cl; float H = cl.H, cs = cl.cs, lx = x + H, ly = y + H, r2 = r * r; int G = GameData.GRID; float added = 0;
            int gx0 = Mathf.Max(0, Mathf.FloorToInt((lx - r) / cs)), gx1 = Mathf.Min(G - 1, Mathf.FloorToInt((lx + r) / cs)), gy0 = Mathf.Max(0, Mathf.FloorToInt((ly - r) / cs)), gy1 = Mathf.Min(G - 1, Mathf.FloorToInt((ly + r) / cs));
            for (int gy = gy0; gy <= gy1; gy++) { float dy = (gy + .5f) * cs - ly; for (int gx = gx0; gx <= gx1; gx++) { int idx = gy * G + gx; if (!cl.inside[idx]) continue; float dx = (gx + .5f) * cs - lx, d2 = dx * dx + dy * dy; if (d2 >= r2) continue; float o = a.grid[idx], nv = o + (1 - o) * s * (1 - d2 / r2); a.grid[idx] = nv; added += nv - o; } }
            a.sum += added;
        }

        // ---------- Sternenstaub-Neustart ----------
        public static bool Sd(string node) => S != null && S.sdNodes.Contains(node);
        /// <summary>Sternenstaub, den ein Neustart jetzt bringen würde: je komplett sauberem Planeten (Position in der Reihenfolge + 1), dazu 1 je 10 Funde.</summary>
        public static int StardustPending() { if (S == null) return 0; int n = 0; foreach (var pid in S.done) { int i = Array.IndexOf(GameData.POrder, pid); if (i >= 0) n += i + 1; } return n + S.finds.Count / 10; }
        public static int StardustFree() => S == null ? 0 : (int)S.stardust - S.sdSpent;
        public static string SdWhy(SdNode n) { if (S.sdNodes.Contains(n.id)) return "Gekauft"; if (!string.IsNullOrEmpty(n.needs) && !S.sdNodes.Contains(n.needs)) return "Erst „" + FeatureData.Stardust.First(x => x.id == n.needs).name + "“"; if (StardustFree() < n.cost) return "Zu wenig Sternenstaub"; return ""; }
        public static void SdBuy(SdNode n) { if (SdWhy(n) != "") { Toast(SdWhy(n), "bad"); return; } S.sdSpent += n.cost; S.sdNodes.Add(n.id); Toast(n.name + " freigeschaltet", "good"); Save(); }
        /// <summary>Neustart: Planeten, Credits, Upgrades und Forschung zurück auf Anfang. Bleiben: Sternenstaub, Boni, Funde, Tagesbonus, Einstellungen, Statistik.</summary>
        public static void Prestige()
        {
            int gain = StardustPending(); if (gain <= 0) { Toast("Erst einen Planeten komplett reinigen", "bad"); return; }
            var old = S; var n = Fresh();
            n.stardust = old.stardust + gain; n.sdSpent = old.sdSpent; n.sdNodes = old.sdNodes; n.resets = old.resets + 1; n.finds = old.finds; n.daily = old.daily; n.achievements = old.achievements; n.title = old.title;
            n.sound = old.sound; n.music = old.music; n.vibrate = old.vibrate; n.notifyOff = old.notifyOff; n.stats = old.stats;
            n.credits = Sd("cash3") ? 25000 : Sd("cash2") ? 5000 : Sd("cash1") ? 1000 : 0;
            S = n; GUpg.Clear(); P.Clear(); foreach (var ps in S.planets) P[ps.pid] = Attach(ps);
            var m = Cur; if (Sd("fast") && m.sv.stage < 2) { m.sv.stage = 2; m.baseVersion++; }
            if (Sd("buggy")) m.sv.veh.Add(new VehSave { id = m.sv.nextVeh++, type = "buggy", d = m.pd.PlanDir(m.lay.start), tank = new float[NM] });
            Save(); OnPlanetChanged?.Invoke(); Toast("Neustart Nr. " + n.resets + " · +" + gain + " Sternenstaub (jetzt " + StardustFree() + " frei)", "good");
        }

        // ---------- Erfolge ----------
        public class AchDef { public string id, name, desc; public Func<bool> ok; }
        public static readonly List<AchDef> Achievements = new List<AchDef>
        {
            A("p10", "Frischer Start", "10 Parzellen gereinigt", () => TotalClean() >= 10),
            A("p100", "Putzkolonne", "100 Parzellen gereinigt", () => TotalClean() >= 100),
            A("p1000", "Planetenpfleger", "1.000 Parzellen gereinigt", () => TotalClean() >= 1000),
            A("p10000", "Sternenkehrer", "10.000 Parzellen gereinigt", () => TotalClean() >= 10000),
            A("hand1k", "Fleißige Hände", "1.000 m² von Hand gewischt", () => S.stats.handM2 >= 1000),
            A("brick", "Erster Brick", "Den ersten Brick gepresst", () => S.stats.bricks > 0),
            A("barren", "Erster Barren", "Den ersten Metallbarren gegossen", () => S.stats.barren > 0),
            A("cr1m", "Millionär", "1 Million Credits verdient", () => S.stats.earned >= 1e6),
            A("cr1b", "Milliardär", "1 Milliarde Credits verdient", () => S.stats.earned >= 1e9),
            A("crys25", "Kristallsammler", "25 Leuchtkristalle geerntet", () => S.stats.crystals >= 25),
            A("find1", "Glücksfund", "Den ersten Schatz gefunden", () => S.finds.Count >= 1),
            A("album", "Album komplett", "Ein Fundalbum komplett", () => FeatureData.Finds.Keys.Any(p => FindsOn(p) >= FeatureData.Finds[p].Length)),
            A("combo10", "Wischwirbel", "Kombo ×10 erreicht", () => ComboN >= FeatureData.COMBO_MAX),
            A("mars", "Roter Planet", "Den Mars erreicht", () => P.ContainsKey("mars")),
            A("done1", "Blitzblank", "Einen Planeten komplett gereinigt", () => S.done.Count >= 1),
            A("fleet10", "Flottenchef", "10 Fahrzeuge gleichzeitig im Einsatz", () => P.Values.Any(p => p.sv.veh.Count >= 10)),
            A("vlvl4", "Veteran", "Ein Fahrzeug auf Stufe 4", () => P.Values.Any(p => p.sv.veh.Any(v => VehLvl(v) >= 4))),
            A("reset1", "Wiedergeburt", "Den ersten Sternenstaub-Neustart gemacht", () => S.resets >= 1),
            A("daily7", "Treue Seele", "Tagesbonus 7 Tage in Folge", () => S.daily != null && S.daily.streak >= 7),
        };
        static AchDef A(string id, string n, string d, Func<bool> ok) => new AchDef { id = id, name = n, desc = d, ok = ok };
        static int TotalClean() => P.Values.Sum(p => p.nClean);
        static float achT;
        /// <summary>Alle 3 s prüfen (nur live).</summary>
        public static void AchievementStep(float dt)
        {
            if (S == null || Offline) return; achT += dt; if (achT < 3) return; achT = 0;
            foreach (var a in Achievements) { if (S.achievements.Contains(a.id)) continue; bool ok; try { ok = a.ok(); } catch { ok = false; } if (ok) { S.achievements.Add(a.id); Toast("Erfolg: " + a.name + " – " + a.desc, "good"); } }
        }
    }
}
