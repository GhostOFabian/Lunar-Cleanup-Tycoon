// Kleine Lebenszeichen: Astronauten laufen zwischen den Gebäuden der Basis hin und her (nur Optik).
using System.Collections.Generic;
using System.Linq;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class Crew { public Transform t; public Vector2 a, b; public float s, len, wait; public Transform legL, legR; }
        readonly List<Crew> crew = new List<Crew>(); string crewFor; int crewVer = -1;

        void BuildCrew()
        {
            foreach (var c in crew) if (c.t) Destroy(c.t.gameObject); crew.Clear(); crewFor = ps.pid; crewVer = ps.baseVersion;
            var ids = ps.bld.Where(id => ps.lay.pos.ContainsKey(id) && !(Game.IsUnit(id) && id != "u1")).ToList(); if (ids.Count < 2) return;
            int n = Mathf.Clamp(ids.Count / 3, 2, 6);
            var suit = MeshKit.Mat(new Color(.93f, .94f, .96f), .1f, .4f); var visor = MeshKit.Mat(new Color(.95f, .7f, .25f), .8f, .9f, new Color(.4f, .25f, .05f)); var pack = MeshKit.Mat(new Color(.55f, .58f, .64f), .4f, .5f);
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("Astronaut"); g.transform.SetParent(baseRoot, false); var t = g.transform;
                if (LcKit.Astronaut(t)) { var c0 = new Crew { t = t, wait = Random.Range(0f, 3f) }; NewTrip(c0, ids); c0.s = Random.Range(0f, c0.len); crew.Add(c0); continue; } /* eigenes Modell (Fabian) */
                MeshKit.Cyl(t, suit, new Vector3(0, .62f, 0), .16f, .18f, .5f, 10);          // Rumpf
                MeshKit.Ball(t, suit, new Vector3(0, 1.02f, 0), .3f);                        // Helm
                MeshKit.Box(t, visor, new Vector3(0, 1.03f, .11f), new Vector3(.2f, .12f, .08f)); // Visier
                MeshKit.Box(t, pack, new Vector3(0, .68f, -.17f), new Vector3(.26f, .34f, .12f)); // Rucksack
                var lL = MeshKit.Cyl(t, suit, new Vector3(-.07f, .2f, 0), .06f, .06f, .38f, 8).transform; var lR = MeshKit.Cyl(t, suit, new Vector3(.07f, .2f, 0), .06f, .06f, .38f, 8).transform;
                var c = new Crew { t = t, legL = lL, legR = lR, wait = Random.Range(0f, 3f) }; NewTrip(c, ids); c.s = Random.Range(0f, c.len); crew.Add(c);
            }
        }
        void NewTrip(Crew c, List<string> ids)
        {
            // nur Wege, die kein Band und kein Gebäude kreuzen; sonst eine kleine Runde um das eigene Gebäude
            for (int k = 0; k < 16; k++)
            {
                string A = ids[Random.Range(0, ids.Count)], B = ids[Random.Range(0, ids.Count)]; if (A == B) continue;
                Vector2 pa = ps.lay.pos[A], pb = ps.lay.pos[B]; var d = (pb - pa).normalized;
                Vector2 a = pa + d * (GameData.Bld[A].fp + .8f), b = pb - d * (GameData.Bld[B].fp + .8f);
                if ((b - a).magnitude < 2 || !CrewClear(a, b)) continue;
                c.a = a; c.b = b; c.len = (b - a).magnitude; c.s = 0; return;
            }
            for (int k = 0; k < 16; k++)
            {
                string A = ids[Random.Range(0, ids.Count)]; var pa = ps.lay.pos[A]; float r = GameData.Bld[A].fp + 1.3f, a0 = Random.Range(0f, 6.283f), a1 = a0 + Random.Range(.6f, 1.4f) * (Random.value < .5f ? 1 : -1);
                Vector2 a = pa + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, b = pa + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
                if (!CrewClear(a, b)) continue; c.a = a; c.b = b; c.len = Mathf.Max(.1f, (b - a).magnitude); c.s = 0; return;
            }
            c.a = c.b = c.t ? (Vector2)pd.PlanLocal(c.t.position.normalized) : Vector2.zero; c.len = .1f; c.s = 0; c.wait = 5; /* nirgends frei: stehen bleiben */
        }
        /// <summary>Kreuzt die Strecke a→b ein Band oder ein Gebäude?</summary>
        bool CrewClear(Vector2 a, Vector2 b)
        {
            float L = (b - a).magnitude; int n = Mathf.Max(2, Mathf.CeilToInt(L / .4f));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)n);
                foreach (var rp in ps.visBelt.Values) for (int k = 0; k < rp.Length - 1; k++) { var ab = rp[k + 1] - rp[k]; float t = Mathf.Clamp01(Vector2.Dot(p - rp[k], ab) / Mathf.Max(1e-6f, ab.sqrMagnitude)); if ((rp[k] + ab * t - p).magnitude < .95f) return false; }
                foreach (var kv in ps.lay.pos) if (ps.bld.Contains(kv.Key) && GameData.Bld.ContainsKey(kv.Key) && (kv.Value - p).magnitude < GameData.Bld[kv.Key].fp + .3f) return false;
            }
            return true;
        }
        void UpdateCrew()
        {
            if (ps == null || MapMode) { foreach (var c in crew) if (c.t) c.t.gameObject.SetActive(false); return; }
            if (crewFor != ps.pid || crewVer != ps.baseVersion) BuildCrew();
            var ids = ps.bld.Where(id => ps.lay.pos.ContainsKey(id) && !(Game.IsUnit(id) && id != "u1")).ToList();
            foreach (var c in crew)
            {
                if (!c.t) continue; c.t.gameObject.SetActive(true);
                if (c.wait > 0) { c.wait -= Time.deltaTime; } else { c.s += Time.deltaTime * 1.3f; if (c.s >= c.len) { c.wait = Random.Range(2f, 6f); NewTrip(c, ids); } }
                var p = Vector2.Lerp(c.a, c.b, Mathf.Clamp01(c.s / c.len)); var dir = pd.PlanDir(p); var fwd = pd.PlanDir(p + (c.b - c.a).normalized * .5f) - dir;
                c.t.position = pd.Surf(dir, c.legL || c.wait > 0 ? 0 : Mathf.Abs(Mathf.Sin(Time.time * 7 + c.len)) * .08f); /* Modell ohne Beine: wippt beim Gehen */ c.t.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, dir).normalized + dir * 1e-4f, dir);
                float sw = c.wait > 0 ? 0 : Mathf.Sin(Time.time * 7 + c.len) * 25; if (c.legL) c.legL.localRotation = Quaternion.Euler(sw, 0, 0); if (c.legR) c.legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            }
        }
    }
}
