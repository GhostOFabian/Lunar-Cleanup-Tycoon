// Nachlese: Gibt es nichts mehr zu reinigen (Planet 100 % sauber), stehen Fahrzeuge nie herum.
// Sie fahren zu sauberen Parzellen, kehren dort Reststaub und Kleinteile zusammen (kleiner Ertrag) und
// schauen alle 2 s, ob es wieder echte Arbeit gibt (Meteoritenstaub, volle Bohrtürme, reife Kristalle).
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        const float SWEEP_TIME = 25f, SWEEP_RATE = .015f;

        static bool StartSweep(PState ps, VehSave v)
        {
            var pd = ps.pd; bool fly = GameData.Veh[v.type].fly; int best = -1; float bs = 1e9f;
            for (int k = 0; k < 24; k++)
            {
                int id = Rng.Next(pd.N); var cl = pd.cells[id];
                if (!ps.clean[id] || cl.IsBase || (cl.Wet && !fly) || id == v.skipCell) continue;
                float d = pd.Arc(v.d, cl.c); int crowd = ps.sv.veh.Count(o => o != v && o.mode == "sweep" && o.cell == id);
                float sc = Mathf.Abs(d - 45) + crowd * 80 + (cl.ring <= 2 ? 30 : 0);
                if (sc < bs) { bs = sc; best = id; }
            }
            if (best < 0) return false;
            v.cell = best; v.ti = -1; v.sweepT = 0; v.sweepCheck = 0; v.mode = "sweep"; return true;
        }

        static void SweepStep(PState ps, VehSave v, float dt, float spd, float tank, float cap)
        {
            var pd = ps.pd; var T = GameData.Veh[v.type];
            if (tank >= cap * .98f || BattLow(ps, v)) { v.mode = "toUnload"; v.cell = -1; return; }
            if (v.cell < 0 || v.cell >= pd.N || !ps.clean[v.cell]) { v.mode = "seek"; v.cell = -1; return; }
            // regelmäßig prüfen, ob es wieder richtige Arbeit gibt
            v.sweepCheck += dt;
            if (v.sweepCheck >= 2)
            {
                v.sweepCheck = 0;
                if (ps.act.Count > 0 || PickRig(ps, v, true) != null || ps.sv.crystals.Any(c => c.g >= 1f && c.claim < 0)) { v.mode = "seek"; v.cell = -1; return; }
            }
            var cl = pd.cells[v.cell];
            if (v.sweepT <= 0)
            {
                // hinfahren
                if (NavMove(ps, v, cl.c, spd * dt, v.id) || pd.Arc(v.d, cl.c) < cl.H * .6f) v.sweepT = SWEEP_TIME;
                BattUse(ps, v, dt * .5f); return;
            }
            // kehren: kreuz und quer über die Parzelle
            v.busy = true; BattUse(ps, v, dt);
            if (v.ti < 0 || v.ti >= cl.inside.Length || !cl.inside[v.ti]) { for (int t = 0; t < 12; t++) { int g = Rng.Next(cl.inside.Length); if (cl.inside[g]) { v.ti = g; break; } } }
            if (v.ti >= 0) { var gd = pd.GridDir(cl, v.ti); if (MoveDir(ref v.d, gd, spd * .5f * dt, pd.R)) v.ti = -1; }
            int m = GameData.Dirt[cl.type].mat; float amt = T.power * Uv("vpower", ps) * SWEEP_RATE * dt * Mathf.Min(1, (cap - tank) / Mathf.Max(1, cap));
            v.tank[m] += amt; v.coll += amt; VehXp(v, amt * 4);
            v.sweepT -= dt; if (v.sweepT <= 0) { v.mode = "seek"; v.skipCell = v.cell; v.cell = -1; }
        }
    }
}
