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
            string A = ids[Random.Range(0, ids.Count)], B = A; for (int k = 0; k < 8 && B == A; k++) B = ids[Random.Range(0, ids.Count)];
            Vector2 pa = ps.lay.pos[A], pb = ps.lay.pos[B]; var d = (pb - pa).normalized;
            c.a = pa + d * (GameData.Bld[A].fp + .8f); c.b = pb - d * (GameData.Bld[B].fp + .8f); c.len = Mathf.Max(.1f, (c.b - c.a).magnitude); c.s = 0;
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
                c.t.position = pd.Surf(dir, 0); c.t.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(fwd, dir).normalized + dir * 1e-4f, dir);
                float sw = c.wait > 0 ? 0 : Mathf.Sin(Time.time * 7 + c.len) * 25; if (c.legL) c.legL.localRotation = Quaternion.Euler(sw, 0, 0); if (c.legR) c.legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            }
        }
    }
}
