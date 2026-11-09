// Fabian 08.11.: „Das Spiel ist nicht innovativ, es hält mich nicht.“ – Experiment auf dem Mond (beide Modi):
// 1. Unter dem Dreck liegt ein Geheimnis: eine leuchtende Sternkarte im Boden, die nur sauber zu sehen ist. Beim Freilegen
//    schaltet das Logbuch Stück für Stück eine Geschichte frei.
// 2. Dreck, der sich wehrt: schwarzer Schleim liegt auf der Karte, breitet sich aus und lässt sich nur von Hand wegwischen –
//    Fahrzeuge meiden ihn. Dafür gibt er Kristalle.
// 3. Ein klares Ende: 3 Sterne (sauber · Geheimnis · unter 45 min Spielzeit auf dem Planeten).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const float GOO_T = 45f, GOO_SEED_T = 150f, STAR_TIME = 2700f; public const int GOO_MAX = 14, GOO_CR = 2;
        public static bool SecretOn(PState ps) => ps != null && Glyphs.ContainsKey(ps.pid) && Logs.ContainsKey(ps.pid); /* 08.11.: alle Planeten */
        /// <summary>08.11.: Das Geheimnis zeigt sich erst, wenn du die ersten Linien selbst freigelegt hast – vorher kein Hinweis.</summary>
        public static bool SecretKnown(PState ps) => SecretOn(ps) && ps.sv.secretBeat > 0;
        public static bool IsGoo(PState ps, int c) => ps.sv.goo != null && ps.sv.goo.Contains(c);
        /// <summary>Anteil der Sternkarte, der sauber und schleimfrei ist (0..1).</summary>
        public static float SecretProgress(PState ps)
        {
            if (!SecretOn(ps)) return 0; var g = ps.pd.GlyphCells; if (g.Count == 0) return 0; int n = 0;
            foreach (int c in g) if (ps.clean[c] && !IsGoo(ps, c) && !ps.act.ContainsKey(c)) n++;
            return n / (float)g.Count;
        }
        public static (string title, string text)[] SecretLog(PState ps) => Logs.TryGetValue(ps.pid, out var l) ? l : new (string, string)[0];
        public static string GlyphName(PState ps) => Glyphs.TryGetValue(ps.pid, out var g) ? g.name : "Zeichen";
        public static int StarCount(PState ps) { int s = ps.sv.stars; return (s & 1) + ((s >> 1) & 1) + ((s >> 2) & 1); }
        public static string StarText(PState ps) { string r = ""; for (int i = 0; i < 3; i++) r += (ps.sv.stars & (1 << i)) != 0 ? "★" : "☆"; return r; }
        public static readonly string[] StarNames = { "Planet komplett sauber", "Geheimnis freigelegt", "Geheimnis in unter 45 min freigelegt" };
        public static event Action<PState> OnStory; public static bool StoryNew; static float gooToastT = -999;

        /// <summary>Je Schritt (live und offline gleich): Schleim säen und ausbreiten, Logbuch und Sterne prüfen.</summary>
        static void SecretStep(PState ps, float dt)
        {
            if (!SecretOn(ps)) return; var sv = ps.sv; if (sv.goo == null) sv.goo = new List<int>();
            var glyph = ps.pd.GlyphCells; if (glyph.Count == 0) return;
            sv.goo.RemoveAll(c => c < 0 || c >= ps.pd.N || (ps.clean[c] && !ps.act.ContainsKey(c)));
            float prog = SecretProgress(ps);
            if (prog < 1 && ps.pid == S.planet) sv.secretT += dt; /* Uhr für den dritten Stern: läuft, solange du auf dem Mond bist und die Karte noch nicht frei ist */
            // säen: solange die Karte noch nicht frei ist, liegt immer etwas Schleim auf ihr
            if (prog < 1)
            {
                sv.gooSeedT += dt;
                if (sv.goo.Count < 3 && (sv.gooSeedT >= GOO_SEED_T || sv.goo.Count == 0))
                {
                    sv.gooSeedT = 0; var cand = glyph.Where(c => !ps.clean[c] && !IsGoo(ps, c) && !ps.pd.cells[c].IsBase).ToList();
                    if (cand.Count > 0 && !Offline && PlayerPrefs.GetInt("LunarCleanup.GooTip", 0) == 0 && ps.pid == S.planet) { PlayerPrefs.SetInt("LunarCleanup.GooTip", 1); Toast("Schwarzer Schleim! Er breitet sich aus und Fahrzeuge meiden ihn – wisch ihn selbst weg, er gibt Kristalle", "bad"); }
                    if (cand.Count > 0) { int n = sv.goo.Count == 0 ? Mathf.Min(3, cand.Count) : 1; for (int i = 0; i < n; i++) { int c = cand[fr.Next(cand.Count)]; if (!sv.goo.Contains(c)) { sv.goo.Add(c); ps.dirtyCells.Add(c); } } }
                }
            }
            // ausbreiten
            sv.gooT += dt;
            if (sv.gooT >= GOO_T)
            {
                sv.gooT = 0; int grown = 0;
                foreach (int c in sv.goo.ToList())
                {
                    if (sv.goo.Count >= GOO_MAX || fr.NextDouble() > .5) continue;
                    var nb = ps.pd.cells[c].nb.Where(b => !ps.pd.cells[b].IsBase && !ps.pd.cells[b].Wet && !IsGoo(ps, b)).OrderByDescending(b => glyph.Contains(b) ? 1 : 0).ThenBy(_ => fr.Next()).DefaultIfEmpty(-1).First();
                    if (nb < 0) continue;
                    if (ps.clean[nb]) GooCover(ps, nb);
                    sv.goo.Add(nb); ps.dirtyCells.Add(nb); grown++;
                }
                if (grown > 0 && !Offline && ps.pid == S.planet && Time.realtimeSinceStartup - gooToastT > 240) { gooToastT = Time.realtimeSinceStartup; Toast("Der schwarze Schleim breitet sich aus – nur du kannst ihn wegwischen", "bad"); }
            }
            // Logbuch
            var log = SecretLog(ps);
            while (sv.secretBeat < log.Length && sv.secretBeat < LogAt.Length && prog >= LogAt[sv.secretBeat] - 1e-4f)
            {
                sv.secretBeat++; if (ps.pid == S.planet) StoryNew = true;
                if (!Offline && ps.pid == S.planet) { Toast("Logbuch " + ps.P.name + ": " + log[sv.secretBeat - 1].title, "good"); OnStory?.Invoke(ps); }
                StoryAchCheck();
            }
            // Sterne (erst, wenn das Geheimnis bekannt ist)
            if (sv.secretBeat == 0) return;
            bool done = ps.nClean >= NonBase(ps) && ps.act.Count == 0; /* jetzt wirklich sauber – nicht „früher einmal geschafft“ (Neustart) */
            if (done) GiveStar(ps, 0);
            if (prog >= 1) GiveStar(ps, 1);
            if (prog >= 1 && sv.secretT <= STAR_TIME) GiveStar(ps, 2);
        }
        /// <summary>09.11.: Geschichts-Erfolge für Game Center (nur mit LC_STORE wirksam).</summary>
        public static void StoryAchCheck()
        {
            if (!AppleServices.Enabled || S == null || Hard) return;
            int known = 0, full = 0, stars = 0; foreach (var q in P.Values) { if (SecretKnown(q)) known++; if (q.sv.secretBeat >= LogAt.Length) full++; stars += StarCount(q); }
            if (known >= 1) AppleServices.ReportAchievement(AppleIds.Story("first"));
            if (P.TryGetValue("moon", out var mp) && mp.sv.secretBeat >= LogAt.Length) AppleServices.ReportAchievement(AppleIds.Story("moon"));
            if (full >= 9) AppleServices.ReportAchievement(AppleIds.Story("half"));
            if (full >= Glyphs.Count) AppleServices.ReportAchievement(AppleIds.Story("all"));
            if (stars >= Glyphs.Count * 3) AppleServices.ReportAchievement(AppleIds.Story("stars"));
        }
        static void GiveStar(PState ps, int i)
        {
            if ((ps.sv.stars & (1 << i)) != 0) return; ps.sv.stars |= 1 << i; S.lk += 5;
            if (!Offline) Toast("Stern " + StarCount(ps) + "/3 auf " + ps.P.name + ": " + StarNames[i] + " · +5 Kristalle", "good"); StoryAchCheck();
        }
        /// <summary>Saubere Parzelle wird vom Schleim überzogen (wie Meteorstaub: wieder putzbar, gibt beim Putzen wieder alles).</summary>
        static void GooCover(PState ps, int cid)
        {
            var cl = ps.pd.cells[cid]; var D = GameData.Dirt[cl.type]; int G = GameData.GRID;
            if (!ps.act.TryGetValue(cid, out var a)) { a = new Act { cl = cl, grid = new float[G * G], sum = 0, amt = D.amt * GameData.Kinds[cl.kind].amt, D = D, K = GameData.Kinds[cl.kind] }; ps.act[cid] = a; }
            float s = 0; for (int i = 0; i < G * G; i++) if (cl.inside[i]) { a.grid[i] = 1; s += 1; } a.sum = s;
        }
        /// <summary>Aus FinishCell: Schleim weggewischt.</summary>
        static void GooCleared(PState ps, int id)
        {
            if (ps.sv.goo == null || !ps.sv.goo.Remove(id)) return;
            S.lk += GOO_CR; if (!Offline) FloatTxt(ps.pd.cells[id].c, "+" + GOO_CR + " Kristalle", "good");
        }
        /// <summary>Test: n Parzellen der Sternkarte mit Schleim bedecken (auch saubere).</summary>
        public static int DevGoo(int n)
        {
            var ps = Cur; if (!SecretOn(ps)) return 0; if (ps.sv.goo == null) ps.sv.goo = new List<int>(); int k = 0;
            foreach (int c in ps.pd.GlyphCells.OrderBy(_ => fr.Next())) { if (k >= n) break; if (IsGoo(ps, c)) continue; if (ps.clean[c]) GooCover(ps, c); ps.sv.goo.Add(c); ps.dirtyCells.Add(c); k++; }
            ps.sv.secretBeat = 0; return k;
        }
        public static int GlyphCount() => Cur.pd.GlyphCells.Count;
        /// <summary>Test: allen Schleim wegputzen wie von Hand (über FinishCell).</summary>
        public static int DevClearGoo() { var ps = Cur; int n = 0; foreach (int c in ps.sv.goo.ToList()) { ps.act.Remove(c); FinishCell(ps, c); n++; } return n; }
        /// <summary>Test: Anteil der Sternkarte sofort sauber machen.</summary>
        public static int DevRevealGlyph(float frac) { var ps = Cur; int k = 0, n = Mathf.RoundToInt(ps.pd.GlyphCells.Count * frac); foreach (int c in ps.pd.GlyphCells) { if (k >= n) break; if (IsGoo(ps, c)) continue; if (!ps.clean[c] || ps.act.ContainsKey(c)) { ps.act.Remove(c); FinishCell(ps, c); } k++; } return k; }
        public static void DevSecretReset() { var ps = Cur; ps.sv.stars = 0; ps.sv.secretBeat = 0; ps.sv.secretT = 0; ps.sv.goo?.Clear(); }
        static readonly Dictionary<PlanetData, int> nbCount = new Dictionary<PlanetData, int>();
        static int NonBase(PState ps) { if (!nbCount.TryGetValue(ps.pd, out int n)) nbCount[ps.pd] = n = ps.pd.cells.Count(x => !x.IsBase); return n; }
        /// <summary>Mitte der Sternkarte (für „Hinsehen“).</summary>
        public static Vector3 GlyphCenter(PState ps) { var f = ps.pd.feats.FirstOrDefault(x => x.kind == 6); return f != null ? f.d : ps.pd.cells[ps.pd.baseCell].c.normalized; }
    }
}
