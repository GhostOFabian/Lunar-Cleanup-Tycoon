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
        { S.stats.cells++;
            bool live = !Offline && ps.pid == S.planet;
            // Kombo: nur eigene Handreinigung
            if (live && strokeCells.Contains(id))
            {
                float now = Time.realtimeSinceStartup; ComboN = now - comboLast <= FeatureData.COMBO_WINDOW ? Mathf.Min(FeatureData.COMBO_MAX, ComboN + 1) : 1; if (ComboN > S.stats.comboMax) S.stats.comboMax = ComboN; comboLast = now;
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
        public static float MeteorEvery(PState ps) => IsDone(ps.pid) ? Mathf.Max(120, 1600 / Mathf.Sqrt(Mathf.Max(1, ps.nClean))) : Mathf.Max(150, 3000 / Mathf.Sqrt(Mathf.Max(1, ps.nClean))); /* Fabian 01.11.: etwa 3× seltener als im Browser (Handy vibrierte ständig) */
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
            int id = c[fr.Next(c.Count)]; bool done = IsDone(ps.pid);
            if (!Offline && ps.pid == S.planet && ViewDir.sqrMagnitude > .5f && fr.NextDouble() < .8) { var near = c.Where(i => Vector3.Dot(pd.cells[i].c.normalized, ViewDir) > ViewCos).ToList(); if (near.Count > 0) id = near[fr.Next(near.Count)]; else id = c.OrderByDescending(i => Vector3.Dot(pd.cells[i].c.normalized, ViewDir)).First(); } /* live meist dort, wo man hinschaut – sonst verpasst man den Schauer */ int want = done ? 4 + fr.Next(4) : 2 + fr.Next(3); var patch = new List<int> { id };
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
            S.stats.meteors += patch.Count;
            if (!Offline && ps.pid == S.planet) { if (OnMeteors != null) OnMeteors(patch); else foreach (int cid in patch) OnBoom?.Invoke(pd.cells[cid].c); Toast((patch.Count >= 4 ? "Großer " : "") + "Meteoritenschauer: " + patch.Count + " Parzellen – die Fahrzeuge räumen auf", "bad"); }
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

        // ---------- Erfolge (Fabian 01.11.: Stufen Bronze/Silber/Gold, Belohnungen, Kategorien) ----------
        // Erreichte Stufe: „t:<id>:<stufe>“, abgeholte Belohnung: „c:<id>:<stufe>“ in S.achievements (alte Einträge ohne Präfix stören nicht).
        public class AchDef { public string id, cat, name, unit; public double[] tiers; public Func<double> val; public string[] titles; }
        public static readonly string[] AchCats = { "Reinigen", "Planeten", "Fahrzeuge", "Produktion", "Entdecker", "Ereignisse", "Geheim" };
        public static readonly string[] TierName = { "Bronze", "Silber", "Gold" };
        static AchDef A(string cat, string id, string name, string unit, Func<double> val, double b, double s, double g, string title = null) => new AchDef { cat = cat, id = id, name = name, unit = unit, val = val, tiers = new[] { b, s, g }, titles = title != null ? new[] { null, null, title } : null };
        static int Wonders() { int n = 0; foreach (var pid in GameData.POrder) if (PlayerPrefs.GetInt("LunarCleanup.Entdeckt." + pid, 0) == 1) n++; return n; }
        static int Albums() => FeatureData.Finds.Keys.Count(p => FindsOn(p) >= FeatureData.Finds[p].Length);
        public static readonly List<AchDef> Achievements = new List<AchDef>
        {
            A("Reinigen", "cells", "Parzellen gereinigt", "Parzellen", () => TotalClean(), 100, 1000, 10000, "Sternenkehrer"),
            A("Reinigen", "hand", "Von Hand gewischt", "m²", () => S.stats.handM2, 1000, 10000, 100000, "Goldene Hand"),
            A("Reinigen", "unload", "Behälter geleert", "mal", () => S.stats.handUnloads, 10, 100, 1000),
            A("Reinigen", "combo", "Kombo erreicht", "×", () => S.stats.comboMax, 3, 6, 10, "Wischwirbel"),
            A("Ereignisse", "meteor", "Meteoriten-Einschläge erlebt", "Einschläge", () => S.stats.meteors, 10, 100, 1000),
            A("Planeten", "reach", "Planeten erreicht", "Planeten", () => P.Count, 3, 9, 18, "Weltenbummler"),
            A("Planeten", "done", "Planeten komplett sauber", "Planeten", () => S.done.Count, 1, 6, 18, "Planetenretter"),
            A("Planeten", "stage", "Höchste Basisstufe", "", () => P.Values.Max(p => p.sv.stage), 3, 5, 5),
            A("Planeten", "expand", "Ausbaustufen", "Stufen", () => P.Values.Sum(p => p.sv.exp), 1, 5, 20),
            A("Fahrzeuge", "fleet", "Fahrzeuge gleichzeitig", "Fahrzeuge", () => P.Values.Max(p => p.sv.veh.Count), 3, 10, 15, "Flottenchef"),
            A("Fahrzeuge", "vlvl", "Höchste Fahrzeugstufe", "", () => P.Values.SelectMany(p => p.sv.veh).Select(v => (double)VehLvl(v)).DefaultIfEmpty(0).Max(), 2, 4, 6),
            A("Fahrzeuge", "lifts", "Drohnen-Abholungen", "Flüge", () => S.stats.lifts, 25, 250, 2500),
            A("Fahrzeuge", "air", "Lastdrohnen-Flüge", "Flüge", () => S.stats.airlifts, 5, 50, 500),
            A("Produktion", "bricks", "Bricks gepresst", "Bricks", () => S.stats.bricks, 100, 10000, 1e6),
            A("Produktion", "barren", "Barren gegossen", "Barren", () => S.stats.barren, 50, 5000, 5e5),
            A("Produktion", "rockets", "Raketen gestartet", "Starts", () => S.planets.Sum(p => p.nRockets), 10, 100, 1000, "Raumfrachter"),
            A("Produktion", "earned", "Credits verdient", "Cr", () => S.stats.earned, 1e6, 1e9, 1e12, "Tycoon"),
            A("Produktion", "sold", "Waren verkauft", "Cr", () => S.stats.sold, 1e5, 1e8, 1e11),
            A("Entdecker", "finds", "Funde gemacht", "Funde", () => S.finds.Count, 1, 25, 100, "Schatzsucher"),
            A("Entdecker", "albums", "Fundalben komplett", "Alben", () => Albums(), 1, 5, 18),
            A("Entdecker", "wonders", "Entdeckungen gesehen", "Orte", () => Wonders(), 1, 9, 18, "Entdecker"),
            A("Entdecker", "flags", "Flaggen gesammelt", "Flaggen", () => S.stats.flags, 10, 60, 250, "Fahnenträger"),
            A("Entdecker", "crystals", "Leuchtkristalle geerntet", "Kristalle", () => S.stats.crystals, 25, 250, 2500),
            A("Ereignisse", "wrecks", "Wracks und Trümmer geborgen", "Bergungen", () => S.stats.salvage, 1, 10, 50),
            A("Ereignisse", "storms", "Stürme überstanden", "Stürme", () => S.stats.storms, 1, 20, 200),
            A("Ereignisse", "geysers", "Geysir-Ausbrüche gesehen", "Ausbrüche", () => S.stats.geysers, 10, 100, 1000),
            A("Geheim", "reset", "Sternenstaub-Neustarts", "Neustarts", () => S.resets, 1, 3, 10, "Wiedergeboren"),
            A("Geheim", "daily", "Tagesbonus in Folge", "Tage", () => S.daily != null ? S.daily.streak : 0, 3, 5, 7),
            A("Geheim", "night", "Nachts gespielt", "min", () => S.stats.nightMin, 10, 60, 300, "Nachteule"),
        };
        static int TotalClean() => P.Values.Sum(p => p.nClean);
        public static int AchTier(AchDef a) { int t = 0; for (int i = 0; i < 3; i++) if (S.achievements.Contains("t:" + a.id + ":" + i)) t = i + 1; return t; }
        public static bool AchClaimed(AchDef a, int tier) => S.achievements.Contains("c:" + a.id + ":" + tier);
        public static int AchPending() { if (S == null) return 0; int n = 0; foreach (var a in Achievements) for (int i = 0; i < AchTier(a); i++) if (!AchClaimed(a, i)) n++; return n; }
        public static double AchCredits(int tier) => Math.Round(4000 * Math.Pow(Cur.P.diff, .9) * (tier == 0 ? 1 : tier == 1 ? 6 : 30));
        public static int AchDust(int tier) => tier == 0 ? 0 : tier == 1 ? 1 : 3;
        public static string AchRewardText(AchDef a, int tier) => Fmt(AchCredits(tier)) + " Cr" + (AchDust(tier) > 0 ? " + " + AchDust(tier) + " Sternenstaub" : "") + (a.titles != null && tier < a.titles.Length && a.titles[tier] != null ? " + Titel „" + a.titles[tier] + "“" : "");
        public static bool ClaimAch(AchDef a, int tier)
        {
            if (tier >= AchTier(a) || AchClaimed(a, tier)) return false; S.achievements.Add("c:" + a.id + ":" + tier);
            Earn(AchCredits(tier)); S.stardust += AchDust(tier); string tt = a.titles != null && tier < a.titles.Length ? a.titles[tier] : null; if (tt != null) S.title = tt;
            Toast(TierName[tier] + " abgeholt: " + AchRewardText(a, tier), "good"); return true;
        }
        static float achT, nightT;
        /// <summary>Alle 3 s prüfen (nur live). Beim ersten Start nach dem Umbau werden schon erreichte Stufen still eingetragen – Belohnungen zum Abholen.</summary>
        public static void AchievementStep(float dt)
        {
            if (S == null || Offline) return;
            if (Cur != null && !IsDay(Cur)) { nightT += dt; if (nightT >= 60) { nightT = 0; S.stats.nightMin++; } }
            achT += dt; if (achT < 3) return; achT = 0; bool quiet = !S.achievements.Contains("v2"); if (quiet) S.achievements.Add("v2");
            int quietN = 0;
            foreach (var a in Achievements)
            {
                double v; try { v = a.val(); } catch { continue; }
                for (int i = 0; i < 3; i++) { string k = "t:" + a.id + ":" + i; if (S.achievements.Contains(k) || v < a.tiers[i]) continue; S.achievements.Add(k); if (quiet) quietN++; else Toast("Erfolg " + TierName[i] + ": " + a.name + " · " + Fmt(a.tiers[i]) + (a.unit != "" ? " " + a.unit : "") + " – Belohnung unter Ziele → Erfolge", "good"); }
            }
            if (quietN > 0) Toast("Erfolg-System neu: " + quietN + " Stufen schon erreicht – Belohnungen unter Ziele → Erfolge abholen", "good");
        }
    }
}
