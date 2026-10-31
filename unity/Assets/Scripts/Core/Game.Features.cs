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
            var ev = ps.sv.ev; if (ev != null && ev.key == "wreck" && ev.cell == id && ev.t > 0) { double w = FeatureData.WRECK_BONUS * ps.P.diff + bonus * (FeatureData.WRECK_AMT - 1); Earn(w); if (live) Toast("Wrack geborgen · +" + Fmt(w) + " Cr", "good"); EvEnd(ps, null); }
            // Schatzfund
            if (!FeatureData.Finds.TryGetValue(ps.pid, out var album)) return;
            float ch = GameData.Kinds[ps.pd.cells[id].kind].key == "anomaly" ? FeatureData.FIND_CHANCE_ANOMALY : FeatureData.FIND_CHANCE;
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
                if (ev.t <= 0) { string n = EvDef(ev.key)?.name ?? "Ereignis"; if (ev.key == "wreck" && ev.cell >= 0 && !ps.clean[ev.cell]) Toast("Das Wrack ist weggesunken – nächstes Mal schneller!"); EvEnd(ps, ev.key == "wreck" ? null : n + " ist vorbei"); }
                return;
            }
            if (S.evNext < 0) S.evNext = Mathf.Lerp(FeatureData.EV_MIN, FeatureData.EV_MAX, (float)fr.NextDouble());
            if (ps.nClean < 8) return; // erst wenn der Planet etwas Fläche hat
            S.evNext -= dt; if (S.evNext > 0) return;
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
            else if (E.key == "comet")
            {
                int n = FeatureData.COMET_MIN + fr.Next(FeatureData.COMET_MAX - FeatureData.COMET_MIN + 1);
                for (int k = 0, tr = 0; k < n && tr < 400; tr++) { int id = fr.Next(pd.N); var cl = pd.cells[id]; if (cl.IsBase || cl.ring < 2 || cl.Wet || !ps.clean[id] || ps.sv.crystals.Any(c => c.cell == id)) continue; float a = (float)fr.NextDouble() * 6.283f, rr = (float)fr.NextDouble() * cl.H * .5f; ps.sv.crystals.Add(new CrystalSave { id = ++ps.sv.crN, cell = id, t = fr.Next(CrystalTypes.Length), x = Mathf.Cos(a) * rr, y = Mathf.Sin(a) * rr, g = 1 }); k++; }
            }
            if (!Offline && ps.pid == S.planet) Toast(E.name + "! " + E.hint, "good");
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
    }
}
