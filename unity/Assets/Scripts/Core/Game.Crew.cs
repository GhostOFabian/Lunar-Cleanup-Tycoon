// Fabian 10.11.: Die Astronauten der Basis arbeiten mit – Techniker (Störungen gehen doppelt so schnell weg) und Kristallsammler.
// Läuft in der Simulation (live und offline gleich); WorldView.Crew zeigt, wer wohin läuft. Bestellungen verladen ist nur Optik.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LunarCleanup.Core
{
    public static partial class Game
    {
        public const float CREW_REACH = 55f, CREW_SPEED = 1.3f;
        /// <summary>Astronauten auf der Basis: ein Drittel der Gebäude, 2 bis 6.</summary>
        public static int CrewN(PState ps) { int n = 0; foreach (var id in ps.bld) if (ps.lay.pos.ContainsKey(id) && !(IsUnit(id) && id != "u1")) n++; return n < 2 ? 0 : Mathf.Clamp(n / 3, 2, 6); }
        public static int TechN(PState ps) { int n = CrewN(ps); return n >= 5 ? 2 : n >= 2 ? 1 : 0; }
        static bool CrewReach(PState ps, Vector3 d) => ps.pd.PlanLocal(d.normalized).magnitude <= CREW_REACH;
        /// <summary>Störungen in Laufweite, die gerade ein Techniker bearbeitet (Bohrtürme, Fahrzeuge mit Notreparatur).</summary>
        public static List<(string k, int id, Vector3 d)> TechJobs(PState ps)
        {
            var l = new List<(string, int, Vector3)>(); int max = TechN(ps); if (max == 0) return l;
            foreach (var r in ps.sv.rigs) { if (l.Count >= max) break; if (r.faultK == "") continue; var d = ps.pd.cells[r.cell].c.normalized; if (CrewReach(ps, d)) l.Add(("rig", r.cell, d)); }
            foreach (var v in ps.sv.veh) { if (l.Count >= max) break; if (v.mode != "fieldRepair") continue; if (CrewReach(ps, v.d)) l.Add(("veh", v.id, v.d.normalized)); }
            return l;
        }
        public static string DevCrew() { var ps = Cur; int ripe = 0, near = 0; foreach (var c in ps.sv.crystals) { float d = ps.pd.PlanLocal(CrystalDir(ps, c)).magnitude; if (d <= CREW_REACH) { near++; if (c.g >= 1) ripe++; } } return "Crew " + CrewN(ps) + ", Kristalle in Reichweite " + near + " (reif " + ripe + "), Ziel " + ps.sv.crewCr + " in " + ps.sv.crewCrT.ToString("0") + " s, Störungen " + TechJobs(ps).Count + ", Bohrtürme " + ps.sv.rigs.Count + " (gestört " + ps.sv.rigs.Count(r => r.faultK != "") + ")"; }
        public static void DevRigFault() { var ps = Cur; foreach (var r in ps.sv.rigs) { if (!CrewReach(ps, ps.pd.cells[r.cell].c)) continue; r.faultK = "impact"; r.faultT = 120; return; } }
        public static int DevCrystalNear() { var ps = Cur; int best = -1; float bd = 1e9f; for (int i = 0; i < ps.pd.N; i++) { if (ps.pd.cells[i].IsBase) continue; float d = ps.pd.PlanLocal(ps.pd.cells[i].c.normalized).magnitude; if (d < bd) { bd = d; best = i; } } if (best < 0) return -1; int id = ps.sv.crystals.Count > 0 ? ps.sv.crystals.Max(c => c.id) + 1 : 1; ps.sv.crystals.Add(new CrystalSave { id = id, cell = best, t = 0, g = 1 }); ps.sv.crewCrT = 0; return id; }
        static bool TechOn(PState ps, string k, int id) { foreach (var j in TechJobs(ps)) if (j.k == k && j.id == id) return true; return false; }
        /// <summary>Ab 3 Astronauten sammelt einer reife Leuchtkristalle in Laufweite der Basis.</summary>
        static void CrewStep(PState ps, float dt)
        {
            var sv = ps.sv; if (CrewN(ps) < 3) { sv.crewCr = -1; return; }
            if (sv.crewCr >= 0)
            {
                var c = sv.crystals.Find(x => x.id == sv.crewCr); if (c == null) { sv.crewCr = -1; sv.crewCrT = 20; return; } /* ein Fahrzeug war schneller */
                sv.crewCrT -= dt; if (sv.crewCrT <= 0) { GiveCrystals(ps, c, false); if (ps.pid == S.planet) FloatTxt(CrystalDir(ps, c), "Crew: Kristall geerntet", "good"); sv.crystals.Remove(c); sv.crewCr = -1; sv.crewCrT = 45; }
                return;
            }
            if (sv.crewCrT > 0) { sv.crewCrT -= dt; return; }
            CrystalSave best = null; float bd = 1e9f;
            foreach (var c in sv.crystals) { if (c.g < 1f || c.claim >= 0) continue; float d = ps.pd.PlanLocal(CrystalDir(ps, c)).magnitude; if (d <= CREW_REACH && d < bd) { bd = d; best = c; } }
            if (best == null) { sv.crewCrT = 30; return; }
            sv.crewCr = best.id; sv.crewCrT = bd / CREW_SPEED + 4f; /* hinlaufen + ernten */
        }
    }
}
