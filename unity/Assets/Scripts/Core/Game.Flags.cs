// Flaggen (Fabian 01.11.): Auf jedem Planeten liegen Flaggen unter dem Schmutz – Mond 10, jeder weitere Planet 2 mehr.
// Sichtbar werden sie, sobald ihre Parzelle sauber ist; antippen = einsammeln (Belohnung steigt mit jeder Flagge).
// Gefundene Flaggen gehören dir: du kannst sie beschriften, der Text steht auf dem Fahnentuch.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    [Serializable] public class FlagSave { public int cell; public float x, y; public bool got; public string text = ""; public int fcol, tcol; /* Fabian 01.11.: Fahnen- und Schriftfarbe (0 = Standard) */ public int slot; /* Fabian 01.11.: Reihenfolge des Öffnens (1 = erste Truhe), 0 = noch nicht vergeben – bestimmt, welches Kapitel darin liegt */ }

    public static partial class Game
    {
        /// <summary>Zählt hoch, wenn sich etwas an den Flaggen ändert (für die Darstellung).</summary>
        public static int FlagVer;
        public static int FlagCount(string pid) { int i = Array.IndexOf(GameData.POrder, pid); return 10 + 2 * Math.Max(0, i); }
        public static int FlagsGot(PState ps) => ps.sv.flags.Count(f => f.got);
        /// <summary>Ältere Spielstände: schon geöffnete Truhen bekommen ihre Plätze in Listenreihenfolge.</summary>
        public static void EnsureSlots(PState ps) { if (ps.sv.flags == null) return; int max = 0; foreach (var f in ps.sv.flags) max = Math.Max(max, f.slot); foreach (var f in ps.sv.flags) if (f.got && f.slot <= 0) f.slot = ++max; }
        /// <summary>Was in dieser Truhe steht (Geschichte, Absender, Randnotiz).</summary>
        public static FlagStories.Entry FlagStory(PState ps, FlagSave f) { EnsureSlots(ps); return FlagStories.Get(ps.pid, Math.Max(0, f.slot - 1), ps.sv.flags.Count); }
        static void InitFlags(PState ps)
        {
            var sv = ps.sv; if (sv.flags == null) sv.flags = new List<FlagSave>(); if (sv.flagsInit || Tut) return; /* 12.11.: keine Truhen im Tutorial */ sv.flagsInit = true;
            var pd = ps.pd; int want = FlagCount(ps.pid); var rnd = new System.Random(ps.pid.GetHashCode() ^ 0x71a9); var sites = new HashSet<int>(OutpostSites(ps.pid));
            var cand = pd.cells.Where(c => !c.IsBase && !c.Wet && !sites.Contains(c.id) && c.ring >= 1).Select(c => c.id).ToList(); var used = new List<int>();
            for (int tries = 0; tries < want * 30 && sv.flags.Count < want && cand.Count > 0; tries++)
            {
                int id = cand[rnd.Next(cand.Count)]; if (used.Contains(id)) continue;
                float minD = pd.R * 3.1f / Mathf.Sqrt(want) * (tries < want * 15 ? 1 : .5f); bool near = false; foreach (int u in used) if (pd.Arc(pd.cells[u].c, pd.cells[id].c) < minD) { near = true; break; } if (near) continue; // gleichmäßig verteilt
                used.Add(id); var cl = pd.cells[id]; float a = (float)rnd.NextDouble() * 6.283f, r = (float)rnd.NextDouble() * cl.H * .45f;
                sv.flags.Add(new FlagSave { cell = id, x = Mathf.Cos(a) * r, y = Mathf.Sin(a) * r });
            }
        }
        public static Vector3 FlagDir(PState ps, FlagSave f) { var cl = ps.pd.cells[f.cell]; return ps.pd.ToDir(cl, f.x, f.y); }
        public static bool FlagVisible(PState ps, FlagSave f) => f.got || ps.clean[f.cell];
        public static double FlagReward(PState ps, int nth) => Math.Round(300 * Math.Pow(ps.P.diff, .9) * (1 + nth * .5));
        public static void CollectFlag(PState ps, FlagSave f)
        {
            if (f.got || !ps.clean[f.cell]) return; EnsureSlots(ps); int n = FlagsGot(ps); double r = FlagReward(ps, n); f.got = true; f.slot = n + 1; S.stats.flags++; Earn(r); FlagVer++;
            int all = ps.sv.flags.Count, got = n + 1;
            Toast("Schatztruhe geöffnet " + got + "/" + all + " · +" + Fmt(r) + " Cr – darin liegt eine Geschichte", "good"); FloatTxt(FlagDir(ps, f), "+" + Fmt(r) + " Cr", "big");
            if (got == all) { double b = Math.Round(r * 10); Earn(b); Toast("Alle Flaggen auf " + ps.P.name + " gefunden! Bonus +" + Fmt(b) + " Cr", "good"); }
        }
        public static readonly (string name, Color col)[] FlagCols = { ("Planetenfarbe", Color.clear), ("Rot", new Color(.9f, .12f, .12f)), ("Orange", new Color(1f, .5f, .05f)), ("Gold", new Color(1f, .78f, .1f)), ("Grün", new Color(.1f, .75f, .3f)), ("Türkis", new Color(0f, .75f, .75f)), ("Blau", new Color(.12f, .35f, .95f)), ("Violett", new Color(.5f, .2f, .85f)), ("Pink", new Color(1f, .3f, .7f)), ("Weiß", new Color(.95f, .95f, .97f)), ("Schwarz", new Color(.08f, .08f, .1f)) };
        public static readonly (string name, Color col)[] TextCols = { ("Weiß", Color.white), ("Schwarz", new Color(.05f, .05f, .08f)), ("Gold", new Color(1f, .82f, .2f)), ("Rot", new Color(1f, .2f, .2f)), ("Grün", new Color(.2f, 1f, .45f)), ("Blau", new Color(.3f, .55f, 1f)), ("Pink", new Color(1f, .4f, .8f)) };
        public static Color FlagCloth(PState ps, FlagSave f) { int i = Mathf.Clamp(f.fcol, 0, FlagCols.Length - 1); return i == 0 ? ps.P.col0 : FlagCols[i].col; }
        public static Color FlagInk(FlagSave f) => TextCols[Mathf.Clamp(f.tcol, 0, TextCols.Length - 1)].col;
        public static void NextFlagCol(FlagSave f) { f.fcol = (f.fcol + 1) % FlagCols.Length; FlagVer++; }
        public static void NextTextCol(FlagSave f) { f.tcol = (f.tcol + 1) % TextCols.Length; FlagVer++; }
        /// <summary>Fabian 11.11.: vorgeschlagene Fahnentexte in beiden Sprachen – gespeichert wird der deutsche, angezeigt in der eingestellten Sprache.</summary>
        public static readonly (string de, string en)[] FlagIdeas = { ("Gänsehaut!", "Goosebumps!"), ("Ich les weiter!", "Reading on!"), ("Unglaublich …", "Unbelievable …"), ("Hier war ich!", "I was here!"), ("Wie geht's weiter?", "What happens next?"), ("Gruß an alle", "Hi everyone") };
        public static string FlagShow(string t) { foreach (var i in FlagIdeas) if (t == i.de || t == i.en) return Loc.En ? i.en : i.de; return t; }
        public static bool FlagIs(FlagSave f, string de) { foreach (var i in FlagIdeas) if (i.de == de) return f.text == i.de || f.text == i.en; return f.text == de; }
        public static void SetFlagText(FlagSave f, string t) { f.text = (t ?? "").Trim(); if (f.text.Length > 24) f.text = f.text.Substring(0, 24); FlagVer++; }
    }
}
