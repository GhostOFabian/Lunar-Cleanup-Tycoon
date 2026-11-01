// Geysire (Fabian 01.11.): echte Fontänen, die aus Schloten im Boden spritzen – kein hingestelltes Modell.
// Jeder Planet hat mehrere davon; sie spucken, was es dort gibt: Lava, Säure, Wasser, Methan, Stickstoff oder Staub.
// Zwischen den Ausbrüchen dampft es nur leicht, alle 20–60 s schießt eine Fontäne hoch.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        class Geyser { public ParticleSystem ps; public float wait, burst; public Vector3 dir; }
        readonly List<Geyser> geysers = new List<Geyser>(); string geyFor; GameObject geyRoot; Material geyMat;

        /// <summary>Was der Planet ausspuckt: Farbe, Kernfarbe, Höhe (Tempo) und ob es glüht.</summary>
        (Color a, Color b, float speed, bool glow, string what) GeyserLook(string pid)
        {
            if (GameData.PLiq.TryGetValue(pid, out var lk)) { var L = GameData.Liq.Find(x => x.key == lk); if (L != null) return lk == "lava" ? (new Color(1f, .45f, .1f), new Color(1f, .85f, .3f), 11, true, L.name) : lk == "acid" ? (new Color(.7f, 1f, .2f), new Color(.95f, 1f, .6f), 10, true, L.name) : lk == "methan" ? (new Color(1f, .7f, .35f), new Color(1f, .9f, .7f), 9, false, L.name) : (new Color(.7f, .9f, 1f), Color.white, 12, false, L.name); }
            switch (pid)
            {
                case "mars": return (new Color(.85f, .45f, .25f), new Color(1f, .75f, .55f), 8, false, "Staub");
                case "moon": return (new Color(.7f, .72f, .78f), new Color(.95f, .95f, 1f), 7, false, "Gas");
                case "ganymed": case "triton": case "pluto": case "eris": case "sedna": return (new Color(.75f, .88f, 1f), Color.white, 10, false, "Stickstoff");
                default: return (new Color(.8f, .85f, .9f), Color.white, 9, false, "Dampf");
            }
        }

        void BuildGeysers()
        {
            geyFor = ps.pid; foreach (var g in geysers) if (g.ps) Destroy(g.ps.gameObject); geysers.Clear(); if (geyRoot) Destroy(geyRoot);
            geyRoot = new GameObject("Geysire"); geyRoot.transform.SetParent(transform, false);
            if (!geyMat) { geyMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default")); geyMat.SetTexture("_BaseMap", MeshKit.GlowTex()); MeshKit.MakeTransparent(geyMat, false); }
            var look = GeyserLook(ps.pid); var rnd = new System.Random(ps.pid.GetHashCode() ^ 0x5e1); var sites = Game.OutpostSites(ps.pid);
            int want = Mathf.Clamp(pd.N / 70, 6, 22); var used = new HashSet<int>(); int wonderCell = wonderAt.sqrMagnitude > .01f ? pd.CellOf(wonderAt) : -1;
            var cand = new List<int>(); foreach (var c in pd.cells) if (!c.IsBase && c.ring >= 2 && !sites.Contains(c.id) && c.id != wonderCell) cand.Add(c.id);
            for (int tries = 0; tries < want * 8 && geysers.Count < want && cand.Count > 0; tries++)
            {
                int id = cand[rnd.Next(cand.Count)]; if (used.Contains(id)) continue; bool near = false; foreach (int u in used) if (pd.Arc(pd.cells[u].c, pd.cells[id].c) < pd.cells[id].H * 4) { near = true; break; } if (near) continue; used.Add(id);
                var cl = pd.cells[id]; float a = (float)rnd.NextDouble() * 6.283f, r = (float)rnd.NextDouble() * cl.H * .4f;
                var dir = pd.ToDir(cl, Mathf.Cos(a) * r, Mathf.Sin(a) * r); var up = pd.SurfNormal(dir, id);
                var go = new GameObject("Geysir"); go.transform.SetParent(geyRoot.transform, false); go.transform.position = pd.Surf(dir, .02f, id); go.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
                // Schlot: flach im Boden, dunkel, mit leicht verfärbtem Rand – sieht aus wie ein Loch, nicht wie ein Modell
                var ring = MeshKit.Cyl(go.transform, MeshKit.Mat(Color.Lerp(look.a, Color.black, .55f), 0, .2f), new Vector3(0, -.02f, 0), .75f, .9f, .04f, 18); NoShadow(ring);
                var hole = MeshKit.Cyl(go.transform, MeshKit.Mat(new Color(.03f, .03f, .035f), 0, .05f, look.glow ? look.a * .8f : Color.black), new Vector3(0, -.005f, 0), .38f, .42f, .04f, 14); NoShadow(hole);
                var p = go.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = p.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 450;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f); main.startSpeed = new ParticleSystem.MinMaxCurve(look.speed * .7f, look.speed * 1.15f);
                main.startSize = new ParticleSystem.MinMaxCurve(.22f, .6f); main.startColor = new ParticleSystem.MinMaxGradient(look.a, look.b);
                var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 7; sh.radius = .25f; sh.rotation = new Vector3(-90, 0, 0); // Kegel zeigt nach oben (lokal +y)
                var fo = p.forceOverLifetime; fo.enabled = true; fo.space = ParticleSystemSimulationSpace.Local; fo.x = 0; fo.z = 0; fo.y = -look.speed * .55f; // fällt zurück
                var sz = p.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .6f, 1, 2.2f));
                var col = p.colorOverLifetime; col.enabled = true; var grd = new Gradient(); grd.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.95f, 0), new GradientAlphaKey(.7f, .5f), new GradientAlphaKey(0, 1) }); col.color = grd;
                var em = p.emission; em.rateOverTime = 3;
                p.GetComponent<ParticleSystemRenderer>().sharedMaterial = geyMat;
                p.Play();
                geysers.Add(new Geyser { ps = p, wait = 4 + (float)rnd.NextDouble() * 40, dir = dir });
            }
        }

        void UpdateGeysers()
        {
            if (ps == null || pd == null) return; if (geyFor != ps.pid) BuildGeysers();
            bool show = !MapMode; if (geyRoot && geyRoot.activeSelf != show) geyRoot.SetActive(show); if (!show) return;
            float dt = Time.deltaTime;
            foreach (var g in geysers)
            {
                if (!g.ps) continue; var em = g.ps.emission;
                if (g.burst > 0) { g.burst -= dt; float k = Mathf.Clamp01(g.burst / 1.2f); em.rateOverTime = 20 + 140 * k; if (g.burst <= 0) { em.rateOverTime = 3; g.wait = 20 + Random.value * 40; } continue; }
                g.wait -= dt; if (g.wait <= 0) g.burst = 3 + Random.value * 4;
            }
        }
    }
}
