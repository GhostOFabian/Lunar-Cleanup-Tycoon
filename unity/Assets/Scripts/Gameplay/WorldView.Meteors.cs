// Meteoritenschauer: die Meteore fallen nacheinander schräg vom Himmel (Glutkopf + Schweif), schlagen mit Blitz, Staub,
// Vibration und kurzem Kamera-Ruckeln ein. Danach liegt ein glühender Brocken auf der Parzelle, bis sie wieder sauber ist.
using System.Collections.Generic;
using LunarCleanup.Core;
using UnityEngine;

namespace LunarCleanup.Gameplay
{
    public partial class WorldView
    {
        /// <summary>Kamera-Ruckeln (0..1), klingt von selbst ab. GameRoot.ApplyCam wackelt damit.</summary>
        public static float Shake;
        class Met { public Vector3 from, to; public float t, delay, dur; public GameObject go; public Transform ball; public int cell; public bool done; }
        class MetRock { public int cell; public GameObject go; public Light glow; public float t; }
        readonly List<Met> mets = new List<Met>();
        readonly List<MetRock> metRocks = new List<MetRock>();

        /// <summary>Startet die Animation für einen Meteoritenschauer auf diesen Parzellen.</summary>
        public void MeteorShower(List<int> cells)
        {
            if (pd == null || cells == null || cells.Count == 0) return;
            var cam = Camera.main;
            /* gemeinsame Einflugrichtung für den ganzen Schauer – sieht aus wie ein Schwarm */
            var n0 = pd.cells[cells[0]].c; Vector3 side = cam ? cam.transform.right : pd.cells[cells[0]].e1, up = cam ? cam.transform.up : pd.cells[cells[0]].e2;
            side -= n0 * Vector3.Dot(side, n0); up -= n0 * Vector3.Dot(up, n0); if (side.sqrMagnitude < .01f) side = pd.cells[cells[0]].e1; if (up.sqrMagnitude < .01f) up = pd.cells[cells[0]].e2;
            side.Normalize(); up.Normalize(); float sgn = Random.value < .5f ? -1 : 1;
            float far = cam ? Mathf.Clamp((cam.transform.position - pd.Surf(n0)).magnitude, 40, 400) : 80;
            for (int i = 0; i < cells.Count; i++)
            {
                var cl = pd.cells[cells[i]];
                var off = (cl.e1 * (Random.value - .5f) + cl.e2 * (Random.value - .5f)) * cl.H * .9f; // irgendwo auf der Parzelle
                var to = pd.Surf(cl.c * pd.R + off, .2f, cells[i]);
                var nn = to.normalized;
                var from = to + nn * far * .45f + side * sgn * far * .6f + up * far * (.1f + Random.value * .15f) + Random.insideUnitSphere * far * .05f;
                mets.Add(new Met { from = from, to = to, cell = cells[i], delay = i * .45f + Random.value * .35f, dur = 1.6f + Random.value * .5f });
            }
            Sfx.Play("rocket", 1.4f, .25f);
        }

        void UpdateMeteors()
        {
            Shake = Mathf.Max(0, Shake - Time.deltaTime * 2.2f);
            var cam = Camera.main;
            for (int i = mets.Count - 1; i >= 0; i--)
            {
                var m = mets[i];
                if (m.delay > 0) { m.delay -= Time.deltaTime; continue; }
                if (!m.go) // Meteor erscheint
                {
                    m.go = new GameObject("Meteor"); m.go.transform.SetParent(transform, false); m.go.transform.position = m.from;
                    var ball = MeshKit.Part(m.go.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .62f, .25f, 1)), Vector3.zero, Vector3.one * 6); NoShadow(ball); ball.AddComponent<Billboard>(); m.ball = ball.transform;
                    var core = MeshKit.Part(ball.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(1f, .97f, .85f, 1)), new Vector3(0, 0, -.01f), Vector3.one * .4f); NoShadow(core);
                    var tr = m.go.AddComponent<TrailRenderer>(); tr.time = 1.1f; tr.minVertexDistance = .6f; tr.alignment = LineAlignment.View; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
                    tr.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, .1f)); tr.material = GlowMat(MeshKit.GlowTex(), Color.white);
                    var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(new Color(1f, .75f, .35f), 0), new GradientColorKey(new Color(1f, .35f, .15f), .3f), new GradientColorKey(new Color(.3f, .28f, .27f), 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.6f, .35f), new GradientAlphaKey(0, 1) }); tr.colorGradient = gr;
                    var lg = new GameObject("Glut"); lg.transform.SetParent(m.go.transform, false); var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.color = new Color(1f, .6f, .3f); L.range = 30; L.intensity = 6; L.shadows = LightShadows.None;
                }
                m.t += Time.deltaTime; float k = Mathf.Clamp01(m.t / m.dur);
                m.go.transform.position = Vector3.Lerp(m.from, m.to, k * k * (1.5f - .5f * k));
                if (cam && m.ball) { float dist = (cam.transform.position - m.go.transform.position).magnitude; m.ball.localScale = Vector3.one * Mathf.Max(6f, dist * .1f); var tr = m.go.GetComponent<TrailRenderer>(); if (tr) tr.widthMultiplier = Mathf.Max(1f, dist * .028f); }
                if (k < 1) continue;
                /* Einschlag */
                var d = m.to.normalized; Destroy(m.go); mets.RemoveAt(i);
                EmitDust(d, new Color(1, .54f, .23f), 45); EmitDust(d, new Color(.33f, .33f, .33f), 35);
                Flash(m.to, new Color(1f, .7f, .4f));
                Sfx.Play("pop", .5f + Random.value * .2f, .6f); Sfx.Buzz();
                if (cam) { float dist = (cam.transform.position - m.to).magnitude; Shake = Mathf.Max(Shake, Mathf.Clamp01(1.2f - dist / 250f)); }
                SpawnMetRock(m.cell, m.to);
            }
            /* Brocken: glühen langsam aus, verschwinden, wenn die Parzelle wieder sauber ist (oder nach 3 Minuten) */
            var ps = Game.Cur;
            for (int i = metRocks.Count - 1; i >= 0; i--)
            {
                var r = metRocks[i]; r.t += Time.deltaTime;
                if (r.glow) r.glow.intensity = Mathf.Lerp(4, 0, r.t / 8f);
                bool gone = !r.go || r.t > 180 || (r.t > 3 && ps != null && ps.pd == pd && !ps.act.ContainsKey(r.cell));
                if (!gone) continue;
                if (r.go) { EmitDust(r.go.transform.position.normalized, new Color(.6f, .55f, .5f), 12); Destroy(r.go); }
                metRocks.RemoveAt(i);
            }
        }

        void SpawnMetRock(int cell, Vector3 at)
        {
            var g = LcKit.Event("meteorit", transform);
            if (!g) { g = new GameObject("Meteorit"); g.transform.SetParent(transform, false); MeshKit.Part(g.transform, MeshKit.Sphere, MeshKit.Mat(new Color(.25f, .22f, .2f), 0, .2f), new Vector3(0, .3f, 0), new Vector3(1.1f, .8f, 1f)); }
            var n = at.normalized; g.transform.position = pd.Surf(n, 0, cell);
            g.transform.rotation = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.AngleAxis(Random.value * 360, Vector3.up);
            g.transform.localScale *= .7f + Random.value * .5f;
            var lg = new GameObject("Glut"); lg.transform.SetParent(g.transform, false); lg.transform.localPosition = new Vector3(0, 1.2f, 0); var L = lg.AddComponent<Light>(); L.type = LightType.Point; L.color = new Color(1f, .45f, .2f); L.range = 8; L.intensity = 4; L.shadows = LightShadows.None;
            metRocks.Add(new MetRock { cell = cell, go = g, glow = L });
        }

        /// <summary>Kurzer heller Lichtblitz an einer Stelle.</summary>
        void Flash(Vector3 at, Color c)
        {
            var g = new GameObject("Blitz"); g.transform.SetParent(transform, false); g.transform.position = at + at.normalized * 2;
            var L = g.AddComponent<Light>(); L.type = LightType.Point; L.color = c; L.range = 35; L.intensity = 14; L.shadows = LightShadows.None;
            var q = MeshKit.Part(g.transform, MeshKit.Quad, GlowMat(MeshKit.GlowTex(), new Color(c.r, c.g, c.b, 1)), Vector3.zero, Vector3.one * 10); NoShadow(q); q.AddComponent<Billboard>();
            g.AddComponent<FadeOut>().dur = .45f;
        }

        void ClearMeteors()
        {
            foreach (var m in mets) if (m.go) Destroy(m.go); mets.Clear();
            foreach (var r in metRocks) if (r.go) Destroy(r.go); metRocks.Clear();
        }
    }

    /// <summary>Licht und Glühscheibe schnell ausblenden, dann löschen.</summary>
    public class FadeOut : MonoBehaviour
    {
        public float dur = .5f; float t; Light L; float i0; Vector3 s0; Transform q;
        void Start() { L = GetComponent<Light>(); if (L) i0 = L.intensity; if (transform.childCount > 0) { q = transform.GetChild(0); s0 = q.localScale; } }
        void Update()
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / dur);
            if (L) L.intensity = i0 * (1 - k); if (q) q.localScale = s0 * (1 + k * 1.5f) * (1 - k * .7f);
            if (k >= 1) Destroy(gameObject);
        }
    }
}
