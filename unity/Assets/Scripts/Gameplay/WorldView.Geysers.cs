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
            var look = GeyserLook(ps.pid);
            var all = new List<(Vector3 d, bool vol)>(); foreach (var d0 in pd.geyserSites) all.Add((d0, false)); foreach (var d0 in pd.volcanoSites) all.Add((d0, true));
            foreach (var (dir, vol) in all) /* Plätze, Schlothügel und Vulkane kommen aus dem Gelände (Planet.Features) */
            {
                var lk = vol ? (new Color(1f, .4f, .08f), new Color(1f, .85f, .3f), 15f, true, "Lava") : look;
                int id = pd.CellOf(dir); var up = pd.SurfNormal(dir, id);
                var go = new GameObject("Geysir"); go.transform.SetParent(geyRoot.transform, false); go.transform.position = pd.Surf(dir, .05f, id); go.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
                var p = go.AddComponent<ParticleSystem>(); p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = p.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local; main.maxParticles = 420;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f); main.startSpeed = new ParticleSystem.MinMaxCurve(lk.Item3 * .7f, lk.Item3 * 1.15f);
                main.startSize = new ParticleSystem.MinMaxCurve(.08f, .22f); /* feine Tröpfchen statt großer Kugeln */ main.startColor = new ParticleSystem.MinMaxGradient(lk.Item1, lk.Item2);
                var sh = p.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 7; sh.radius = .25f; sh.rotation = new Vector3(-90, 0, 0); // Kegel zeigt nach oben (lokal +y)
                var fo = p.forceOverLifetime; fo.enabled = true; fo.space = ParticleSystemSimulationSpace.Local; fo.x = 0; fo.z = 0; fo.y = -lk.Item3 * .55f; // fällt zurück
                var sz = p.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .6f, 1, 2.2f));
                var col = p.colorOverLifetime; col.enabled = true; var grd = new Gradient(); grd.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(.95f, 0), new GradientAlphaKey(.7f, .5f), new GradientAlphaKey(0, 1) }); col.color = grd;
                var em = p.emission; em.rateOverTime = 3;
                var pr = p.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = geyMat; pr.renderMode = ParticleSystemRenderMode.Stretch; pr.velocityScale = .06f; pr.lengthScale = 1.2f; /* gestreckte Strahlen wie echte Fontänen */
                p.Play();
                geysers.Add(new Geyser { ps = p, wait = 4 + Random.value * 60, dir = dir });
            }
            /* spontane Ausbrüche: ein paar freie Fontänen, die irgendwo im Blickfeld einfach aus dem Boden schießen */
            spont.Clear(); for (int i = 0; i < 3; i++) { var go = Object.Instantiate(geysers.Count > 0 ? geysers[0].ps.gameObject : new GameObject("leer"), geyRoot.transform); go.name = "Spontan"; var p = go.GetComponent<ParticleSystem>(); if (p) { var em = p.emission; em.rateOverTime = 0; p.Play(); spont.Add(new Geyser { ps = p, wait = 6 + Random.value * 14 }); } }
        }
        readonly List<Geyser> spont = new List<Geyser>();

        void UpdateGeysers()
        {
            if (ps == null || pd == null) return; if (geyFor != ps.pid) BuildGeysers();
            bool show = !MapMode; if (geyRoot && geyRoot.activeSelf != show) geyRoot.SetActive(show); if (!show) return;
            float dt = Time.deltaTime;
            var view = Game.ViewDir;
            foreach (var g in spont)
            {
                if (!g.ps) continue; var em = g.ps.emission;
                if (g.burst > 0) { g.burst -= dt; em.rateOverTime = g.burst > 0 ? 60 + 220 * Mathf.Clamp01(g.burst / 1.5f) : 0; if (g.burst <= 0) g.wait = 8 + Random.value * 25; continue; }
                g.wait -= dt; if (g.wait > 0 || view.sqrMagnitude < .5f) continue;
                /* zufällige Stelle im Blickfeld, nicht auf der Basis */
                for (int t = 0; t < 8; t++) { var d = (view + Random.insideUnitSphere * .18f).normalized; int c = pd.CellOf(d); if (pd.cells[c].IsBase) continue; g.dir = d; g.ps.transform.position = pd.Surf(d, .05f, c); g.ps.transform.rotation = Quaternion.FromToRotation(Vector3.up, pd.SurfNormal(d, c)); g.burst = 2 + Random.value * 3; Sfx.PlayAt("geyser", g.ps.transform.position, .8f + Random.value * .4f, .5f, 90); if (Game.S != null) Game.S.stats.geysers++; break; }
            }
            foreach (var g in geysers)
            {
                if (!g.ps) continue; var em = g.ps.emission;
                bool seen = Vector3.Dot(g.dir, view) > .55f; if (!seen && g.burst <= 0) { em.rateOverTime = 0; g.wait -= dt; if (g.wait <= 0) g.wait = 10 + Random.value * 60; continue; } /* außer Sicht: nichts rechnen */
                if (g.burst > 0) { g.burst -= dt; float k = Mathf.Clamp01(g.burst / 1.2f); em.rateOverTime = 40 + 260 * k; if (g.burst <= 0) { em.rateOverTime = 3; g.wait = 10 + Random.value * Random.value * 90; } continue; } /* zufällige Abstände statt Takt */
                em.rateOverTime = 3; g.wait -= dt; if (g.wait <= 0) { g.burst = 2 + Random.value * 6; if (Game.S != null) Game.S.stats.geysers++; Sfx.PlayAt("geyser", g.ps.transform.position, .85f + Random.value * .3f, .55f, 90); }
            }
        }
    }
}
