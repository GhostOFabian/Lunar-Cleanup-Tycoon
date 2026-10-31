// Wetter je Planet (nur Optik, keine Regeln): Staubteufel auf dem Mars, Eisnebel auf Europa, Methanregen auf Titan,
// Lavafontänen auf Io. Nutzt den Staub-Partikelsystem-Emitter, immer in der Nähe der Kamera.
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        Vector3[] devils = new Vector3[0]; Vector3[] devilDir = new Vector3[0]; float wxT, wxEmit;

        void UpdateWeather()
        {
            if (ps == null || MapMode || !dust || !FeatureData.Weather.TryGetValue(ps.pid, out var W)) return;
            var cam = Camera.main; if (!cam) return; var cdir = cam.transform.position.normalized; float dt = Time.deltaTime; wxT += dt; wxEmit += dt;
            switch (W.key)
            {
                case "dustdevil":
                    {
                        // Staubteufel ziehen W.dur Sekunden lang über das Gelände, dann Pause bis W.every
                        float ph = wxT % W.every; bool on = ph < W.dur;
                        if (!on) { devils = new Vector3[0]; break; }
                        if (devils.Length == 0) { int n = 2; devils = new Vector3[n]; devilDir = new Vector3[n]; for (int i = 0; i < n; i++) { devils[i] = Offset(cdir, Random.Range(-35f, 35f), Random.Range(-35f, 35f)); devilDir[i] = Random.onUnitSphere; } }
                        var col = GameData.Dirt[ps.P.start].lightC;
                        for (int i = 0; i < devils.Length; i++)
                        {
                            var t = Vector3.Cross(devils[i], devilDir[i]).normalized; devils[i] = (devils[i] + t * dt * 2.5f / pd.R).normalized;
                            if (wxEmit < .04f) continue;
                            for (int k = 0; k < 3; k++)
                            {
                                float h = Random.Range(0f, 7f), a = Time.time * 6 + h + k * 2.1f, rr = .4f + h * .25f; var nrm = devils[i]; var e1 = Vector3.Cross(nrm, Vector3.up).normalized; if (e1.sqrMagnitude < .1f) e1 = Vector3.Cross(nrm, Vector3.right).normalized; var e2 = Vector3.Cross(nrm, e1);
                                var pos = pd.Surf(nrm, h) + (e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a)) * rr;
                                dust.Emit(new ParticleSystem.EmitParams { position = pos, velocity = nrm * 1.2f + (e2 * Mathf.Cos(a) - e1 * Mathf.Sin(a)) * 1.5f, startColor = new Color(col.r, col.g, col.b, .5f), startSize = .35f + h * .06f, startLifetime = 1.2f, applyShapeToPosition = false }, 1);
                            }
                        }
                        break;
                    }
                case "icefog":
                    {
                        if (wxEmit < .08f) break; float dayK = 1 - Mathf.Abs(Mathf.Sin(Game.SunAng(ps))); // morgens/abends dichter
                        int n = Mathf.RoundToInt(2 + 4 * dayK);
                        for (int k = 0; k < n; k++) { var d = Offset(cdir, Random.Range(-40f, 40f), Random.Range(-40f, 40f)); dust.Emit(new ParticleSystem.EmitParams { position = pd.Surf(d, Random.Range(.2f, 1.2f)), velocity = Random.insideUnitSphere * .2f, startColor = new Color(.85f, .93f, 1f, .22f), startSize = Random.Range(2.5f, 4.5f), startLifetime = 4f, applyShapeToPosition = false }, 1); }
                        break;
                    }
                case "methrain":
                    {
                        float ph = wxT % W.every; if (ph > W.dur || wxEmit < .03f) break;
                        for (int k = 0; k < 10; k++) { var d = Offset(cdir, Random.Range(-30f, 30f), Random.Range(-30f, 30f)); dust.Emit(new ParticleSystem.EmitParams { position = pd.Surf(d, Random.Range(8f, 16f)), velocity = -d * 14f, startColor = new Color(.95f, .75f, .45f, .35f), startSize = .08f, startLifetime = .9f, applyShapeToPosition = false }, 1); }
                        break;
                    }
                case "lavafont":
                    {
                        if (wxT % W.every > .05f + dt) break; // kurz alle W.every Sekunden
                        int best = -1; float bd = -2; for (int i = 0; i < pd.N; i++) { var cl = pd.cells[i]; if (!cl.Wet) continue; float d0 = Vector3.Dot(cl.c, cdir) + Random.Range(0f, .02f); if (d0 > bd) { bd = d0; best = i; } }
                        if (best < 0) break; var c0 = pd.cells[best].c;
                        for (int k = 0; k < 60; k++) dust.Emit(new ParticleSystem.EmitParams { position = pd.Surf(c0, .3f), velocity = c0 * Random.Range(6f, 12f) + Random.insideUnitSphere * 2.5f, startColor = Color.Lerp(new Color(1f, .35f, .08f), new Color(1f, .8f, .3f), Random.value), startSize = Random.Range(.25f, .6f), startLifetime = Random.Range(1f, 1.8f), applyShapeToPosition = false }, 1);
                        break;
                    }
            }
            if (wxEmit >= .04f) wxEmit = 0;
        }

        /// <summary>Richtung im Abstand (x, y) Meter vom Punkt d auf der Oberfläche.</summary>
        Vector3 Offset(Vector3 d, float x, float y)
        {
            var e1 = Vector3.Cross(d, Vector3.up); if (e1.sqrMagnitude < .01f) e1 = Vector3.Cross(d, Vector3.right); e1.Normalize(); var e2 = Vector3.Cross(d, e1);
            return (d * pd.R + e1 * x + e2 * y).normalized;
        }
    }
}
