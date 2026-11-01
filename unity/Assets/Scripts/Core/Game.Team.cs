// Suchtrupp: fährt bei einem Wrack-Absturz oder Kometeneinschlag von der Basis zur Fundstelle, birgt die Teile und bringt sie zurück.
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        const float TEAM_SPEED = 5.5f, TEAM_SALVAGE = 30f;
        static Vector3 TeamHome(PState ps) => ps.pd.PlanDir(ps.lay.pos.TryGetValue("hq", out var p) ? p + new Vector2(3.5f, -3.5f) : Vector2.zero);

        public static void SendTeam(PState ps)
        {
            var t = ps.sv.team ?? (ps.sv.team = new TeamSave()); var ev = ps.sv.ev; if (ev == null || ev.cell < 0 || (ev.key != "wreck" && ev.key != "impact")) return;
            if (t.mode == "home" || t.d == Vector3.zero) t.d = TeamHome(ps);
            t.mode = "go"; t.cell = ev.cell; t.key = ev.key; t.t = 0;
        }

        static void TeamStep(PState ps, float dt)
        {
            var t = ps.sv.team; if (t == null) return; var pd = ps.pd; var ev = ps.sv.ev;
            if (t.d == Vector3.zero) t.d = TeamHome(ps);
            bool live = ev != null && ev.t > 0 && ev.key == t.key && ev.cell == t.cell;
            switch (t.mode)
            {
                case "go":
                    if (!live) { t.mode = "back"; t.key = ""; break; }
                    { var d = t.d; bool arr = NavMoveD(ps, ref d, pd.cells[t.cell].c, TEAM_SPEED * dt, -555) || pd.Arc(d, pd.cells[t.cell].c) < 2.5f; t.d = d; if (arr) { t.mode = "salvage"; t.t = TEAM_SALVAGE; } }
                    break;
                case "salvage":
                    if (!live) { t.mode = "back"; t.key = ""; break; }
                    t.t -= dt; if (t.t > 0) break;
                    {
                        double cr = (t.key == "wreck" ? FeatureData.WRECK_BONUS * 3 : FeatureData.WRECK_BONUS * 2) * ps.P.diff; Earn(cr);
                        string extra = "";
                        if (t.key == "impact") { for (int k = 0; k < 3; k++) { var cl = pd.cells[t.cell]; ps.sv.crystals.Add(new CrystalSave { id = ++ps.sv.crN, cell = t.cell, g = .2f, t = Rng.Next(5), x = (float)(Rng.NextDouble() - .5) * cl.H, y = (float)(Rng.NextDouble() - .5) * cl.H, claim = -1 }); } extra = " · 3 Leuchtkristalle wachsen an der Einschlagstelle"; }
                        if (!Offline && ps.pid == S.planet) { Toast((t.key == "wreck" ? "Wrack geborgen" : "Kometentrümmer geborgen") + " · +" + Fmt(cr) + " Cr" + extra, "good"); FloatTxt(pd.cells[t.cell].c, "+" + Fmt(cr) + " Cr", "big"); }
                        EvEnd(ps, null); t.mode = "back"; t.key = ""; S.stats.salvage++;
                    }
                    break;
                case "back":
                    { var d = t.d; if (NavMoveD(ps, ref d, TeamHome(ps), TEAM_SPEED * dt, -555)) { t.mode = "home"; } t.d = d; }
                    break;
                default: t.mode = "home"; break;
            }
        }

        /// <summary>Text für die Anzeige oben (null = nichts los).</summary>
        public static string TeamText(PState ps)
        {
            var t = ps.sv.team; var ev = ps.sv.ev; if (t == null || ev == null || ev.t <= 0 || (ev.key != "wreck" && ev.key != "impact")) return null;
            string what = ev.key == "wreck" ? "Wrack-Absturz" : "Kometeneinschlag"; int left = Mathf.CeilToInt(ev.t);
            if (t.mode == "go" && t.cell >= 0) return what + "! Suchtrupp unterwegs · noch " + Mathf.RoundToInt(ps.pd.Arc(t.d, ps.pd.cells[t.cell].c)) + " m · Tippen: hinsehen";
            if (t.mode == "salvage") return what + " · Suchtrupp birgt die Teile · " + Mathf.CeilToInt(t.t) + " s";
            return what + "! Noch " + left / 60 + ":" + (left % 60).ToString("00") + " · Tippen: hinsehen";
        }
    }
}
