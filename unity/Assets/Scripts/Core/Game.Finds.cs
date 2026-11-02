// Fabian 02.11.: Der Finger bleibt wichtig. Auf Planeten, die schon gut laufen, glänzt ab und zu ein seltener Fund im Staub.
// Nur du kannst ihn freiwischen (die Fahrzeuge kommen nicht ran) – ein paar Sekunden wischen, dann gibt es eine große Belohnung.
// Nach 2:30 min versinkt er wieder. Nur live (nicht offline), nicht gespeichert.
using System;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public class HandFind { public string pid; public int cell; public Vector3 dir; public float prog, life; public int kind; }
    public static partial class Game
    {
        public static HandFind Find; static float findNext = -1;
        public static event Action<HandFind> OnFindNew; public static event Action<HandFind, bool> OnFindEnd;
        public const float FIND_LIFE = 150, FIND_WIPE = 2.4f;
        static readonly string[] FindNames = { "Alte Raumkapsel", "Funkelnder Meteorit", "Verschollene Frachtkiste" };
        static bool devFindNow; public static void DevFind() { Find = null; findNext = .01f; devFindNow = true; }
        static void FindStep(float dt)
        {
            if (Offline || S == null) return; var ps = Cur;
            if (Find != null)
            {
                if (Find.pid != S.planet) { Find = null; return; } /* weitergereist */
                Find.life -= dt; if (Find.life <= 0) { var f = Find; Find = null; Toast("Der Fund ist wieder im Staub versunken – beim nächsten Mal schneller wischen", "", true); OnFindEnd?.Invoke(f, false); }
                return;
            }
            if (Icp != null || DoomActive) return;
            if (findNext < 0) findNext = UnityEngine.Random.Range(240f, 480f); findNext -= dt; if (findNext > 0) return; findNext = UnityEngine.Random.Range(420f, 900f);
            if (!devFindNow && (ps.sv.veh.Count < 3 || CleanPct(ps) < .5f)) return; devFindNow = false; /* erst wenn die Automatik läuft – vorher wischst du sowieso alles selbst */
            var pd = ps.pd; var cand = Enumerable.Range(0, pd.N).Where(i => ps.clean[i] && !pd.cells[i].IsBase && !pd.cells[i].Wet).ToList(); if (cand.Count == 0) return;
            if (ViewDir.sqrMagnitude > .5f) { var near = cand.Where(i => Vector3.Dot(pd.cells[i].c.normalized, ViewDir) > ViewCos).ToList(); if (near.Count > 0) cand = near; }
            int cell = cand[Rng.Next(cand.Count)];
            Find = new HandFind { pid = ps.pid, cell = cell, dir = pd.cells[cell].c.normalized, life = FIND_LIFE, kind = Rng.Next(3) };
            Toast("Ein seltener Fund glänzt im Staub! Wisch ihn mit dem Finger frei – die Fahrzeuge kommen da nicht ran (2:30 min)", "good"); OnFindNew?.Invoke(Find);
        }
        /// <summary>Liegt der Fund an dieser Stelle (Einheitsvektor)? Etwa 3 m Umkreis.</summary>
        public static bool FindAt(Vector3 d) => Find != null && Find.pid == S.planet && (d.normalized - Find.dir).magnitude * Cur.pd.R < 3.2f;
        public static float FindProgress => Find == null ? 0 : Mathf.Clamp01(Find.prog);
        /// <summary>Beim Wischen: Fortschritt, nach ≈ 2,4 s Wischen geborgen.</summary>
        public static void FindWipe(Vector3 d, float dt)
        {
            if (!FindAt(d)) return; Find.prog += dt / FIND_WIPE; if (Find.prog < 1) return;
            var f = Find; Find = null; string what;
            if (f.kind == 1) { int sd = 6 + Rng.Next(7); S.stardust += sd; what = "+" + sd + " Sternenstaub"; }
            else if (f.kind == 2 && Goods.Any(g => P.ContainsKey(g.pid) && GoodRate(P[g.pid]) > 0)) { var gl = Goods.Where(g => P.ContainsKey(g.pid) && GoodRate(P[g.pid]) > 0).ToList(); var g = gl[Rng.Next(gl.Count)]; int n = 10 + Rng.Next(16); AddGood(g.id, n); what = "+" + n + " " + g.name; }
            else { double c = Math.Max(500, IncomePerMin() * (4 + Rng.Next(5))); Earn(c); what = "+" + Fmt(c) + " Cr"; }
            Toast(FindNames[f.kind] + " geborgen! " + what, "good"); OnFindEnd?.Invoke(f, true); Save();
        }
    }
}
